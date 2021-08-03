using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using System.Threading;
using System.Reflection;
using Cysharp.Threading.Tasks;

using MB;

namespace MNet
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder)]
    [AddComponentMenu(Constants.Path + "Network Entity")]
    public partial class NetworkEntity : MonoBehaviour
    {
        public const int ExecutionOrder = -500;

        #region Sync
        [SerializeField]
        SyncProperty sync = default;
        public SyncProperty Sync => sync;
        [Serializable]
        public class SyncProperty
        {
            [SerializeField]
            [SyncInterval(0, 200)]
            [Tooltip("Sync Interval in ms, 1s = 1000ms")]
            int interval = 100;
            public int Interval => interval;

            public List<ISync> List { get; protected set; }

            public event Action OnInvoke;

            public bool Active => List.Count > 0;

            NetworkEntity entity;
            internal void Set(NetworkEntity reference)
            {
                entity = reference;

                List = entity.GetComponentsInChildren<ISync>(true).ToList();

                entity.OnReady += ReadyCallback;
                entity.OnDespawn += DespawnCallback;
            }

            void ReadyCallback()
            {
                if (Active) coroutine = entity.StartCoroutine(Procedure());
            }

            Coroutine coroutine;
            IEnumerator Procedure()
            {
                while (true)
                {
                    if (entity.IsMine) Invoke();

                    yield return new WaitForSecondsRealtime(interval / 1000f);
                }
            }

            void Invoke()
            {
                for (int i = 0; i < List.Count; i++)
                    List[i].Sync();

                OnInvoke?.Invoke();
            }

            void DespawnCallback()
            {
                if (coroutine != null)
                {
                    entity.StopCoroutine(coroutine);
                    coroutine = null;
                }
            }
        }

        public interface ISync
        {
            void Sync();
        }
        #endregion

        public NetworkEntityID ID { get; protected set; }

        public bool IsConnected => NetworkAPI.Client.IsConnected;

        /// <summary>
        /// Boolean value to show if this Entity is ready to send and recieve messages
        /// </summary>
        public bool IsReady { get; protected set; } = false;

        #region Type
        public EntityType Type { get; internal set; }

        public bool IsSceneObject => Type == EntityType.SceneObject;
        public bool IsDynamic => Type == EntityType.Dynamic;
        public bool IsOrphan => Type == EntityType.Orphan;

        /// <summary>
        /// Is this an entity that will always be owned by the master client? such as scene objects and orphans
        /// </summary>
        public bool IsMasterObject => CheckIfMasterObject(Type);
        #endregion

        public PersistanceFlags Persistance { get; protected set; }

        #region Ownership
        public NetworkClient Owner { get; protected set; }

        /// <summary>
        /// Am I (local client) the owner of this entity?
        /// </summary>
        public bool IsMine => Owner == NetworkAPI.Client.Self;

        public delegate void OwnerSetDelegate(NetworkClient client);
        public event OwnerSetDelegate OnOwnerSet;
        internal void SetOwner(NetworkClient client)
        {
            Owner = client;

            OnOwnerSet?.Invoke(Owner);
        }

        public bool Takeover()
        {
            if (IsMasterObject)
            {
                Log.Error($"Master Objects Cannot be Taken Over by Clients");
                return false;
            }

            if (IsMine)
            {
                Debug.LogWarning($"You Already Own Entity {this}, no Need to Takeover it");
                return false;
            }

            var request = new TakeoverEntityRequest(ID);

            return NetworkAPI.Client.Send(ref request);
        }

        public bool TransferTo(NetworkClient client)
        {
            if (IsMasterObject)
            {
                Log.Error($"Master Objects Cannot be Transfered");
                return false;
            }

            if (Owner == client)
            {
                Debug.LogWarning($"Client: {client} Already Owns Entity '{this}'");
                return false;
            }

            if (CheckAuthority(NetworkAPI.Client.Self) == false)
            {
                Debug.LogWarning($"Local Client has no Authority over Entity '{this}', Entity Trasnsfer Invalid");
                return false;
            }

            var payload = new TransferEntityPayload(ID, client.ID);

            return NetworkAPI.Client.Send(ref payload);
        }

        /// <summary>
        /// Checks if client has authority over this Entity
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool CheckAuthority(NetworkClient client) => CheckAuthority(client, RemoteAuthority.Owner | RemoteAuthority.Master);
        /// <summary>
        /// Checks if client has authority over this Entity with RemoteAuthority flags
        /// </summary>
        /// <param name="client"></param>
        /// <returns></returns>
        public bool CheckAuthority(NetworkClient client, RemoteAuthority authority)
        {
            //instantly validate every buffered message
            if (NetworkAPI.Realtime.IsOnBuffer) return true;

            if (authority.HasFlag(RemoteAuthority.Any)) return true;

            if (client == null) return false;

            if (authority.HasFlag(RemoteAuthority.Owner))
            {
                if (client == Owner)
                    return true;
            }

            if (authority.HasFlag(RemoteAuthority.Master))
            {
                if (client == NetworkAPI.Room.Master.Client)
                    return true;
            }

            return false;
        }
        #endregion

        public AttributesCollection Attributes { get; protected set; }

        #region Behaviours
        public DualDictionary<NetworkBehaviourID, MonoBehaviour, Behaviour> Behaviours { get; protected set; }

        public virtual Behaviour GetBehaviour<T>(T reference)
            where T : MonoBehaviour
        {
            if (Behaviours.TryGetValue(reference, out var behaviour) == false)
                throw new Exception($"Cannot Retrieve Network Behaviour for Unregisterd Type of {typeof(T).Name}");

            return behaviour;
        }
        #endregion

        public Scene UnityScene => gameObject.scene;
        public NetworkScene NetworkScene { get; protected set; }

        protected virtual void Awake()
        {
            if (Application.isPlaying == false)
            {
                NetworkScene.RegisterLocal(this);
                return;
            }

            Behaviours = new DualDictionary<NetworkBehaviourID, MonoBehaviour, Behaviour>();

            var targets = GetComponentsInChildren<INetworkBehaviour>(true);

            if (targets.Length > byte.MaxValue)
                throw new Exception($"Entity {name} May Only Have Up To {byte.MaxValue} Behaviours, Current Count: {targets.Length}");

            for (byte i = 0; i < targets.Length; i++)
            {
                var id = new NetworkBehaviourID(i);

                var behaviour = new Behaviour(this, targets[i], id);

                Behaviours[id, behaviour.Component] = behaviour;

                targets[i].Network = behaviour;
                targets[i].OnNetwork();
            }
        }

        public delegate void SetupDelegate();
        public event SetupDelegate OnSetup;
        internal void Setup(NetworkClient owner, EntityType type, PersistanceFlags persistance, AttributesCollection attributes)
        {
            this.Type = type;
            this.Persistance = persistance;
            this.Attributes = attributes;
            this.Owner = owner;

            sync.Set(this);

            OnSetup?.Invoke();

            SetOwner(owner);
        }

        public delegate void SpawnDelegate();
        public event SpawnDelegate OnSpawn;
        internal void Spawn(NetworkEntityID id, NetworkScene scene)
        {
            this.ID = id;
            this.NetworkScene = scene;

            if (IsDynamic || IsOrphan) name += $" {id}";

            IsReady = true;

            OnSpawn?.Invoke();

            if (NetworkAPI.Realtime.IsOnBuffer)
                ReadyAfterBuffer();
            else
                Ready();
        }

        #region Ready After Buffer
        void ReadyAfterBuffer()
        {
            NetworkAPI.Realtime.OnBufferEnd += ReadyAfterBufferCallback;
        }

        void ReadyAfterBufferCallback(IList<NetworkMessage> list)
        {
            NetworkAPI.Realtime.OnBufferEnd -= ReadyAfterBufferCallback;
            Ready();
        }
        #endregion

        public delegate void ReadyDelegate();
        public event ReadyDelegate OnReady;
        void Ready()
        {
            OnReady?.Invoke();
        }

        #region Remote Sync
        public bool InvokeRPC<T>(ref T command)
            where T : IRpcCommand
        {
            if (Behaviours.TryGetValue(command.Behaviour, out var target) == false)
            {
                Debug.LogWarning($"No Behaviour with ID {command.Behaviour} found to Invoke RPC");
                return false;
            }

            return target.InvokeRPC(ref command);
        }

        public void InvokeSyncVar<TCommand>(TCommand command)
            where TCommand : ISyncVarCommand
        {
            if (Behaviours.TryGetValue(command.Behaviour, out var target) == false)
            {
                Debug.LogWarning($"No Behaviour with ID {command.Behaviour} found to invoke RPC");
                return;
            }

            target.InvokeSyncVar(command);
        }
        #endregion

        public delegate void DespawnDelegate();
        public event DespawnDelegate OnDespawn;
        internal virtual void Despawn()
        {
            IsReady = false;

            OnDespawn?.Invoke();
        }

        protected virtual void OnDestroy()
        {
            if (UnityScene.isLoaded && Application.isPlaying == false) NetworkScene.UnregisterLocal(this);

            NetworkAPI.Realtime.OnBufferEnd -= ReadyAfterBufferCallback;
        }

        //Static Utility
        public static bool IsAttachedTo(GameObject gameObject) => gameObject.TryGetComponent<NetworkEntity>(out _);

        public static NetworkEntity Find(NetworkEntityID id)
        {
            TryFind(id, out var entity);

            return entity;
        }

        public static bool TryFind(NetworkEntityID id, out NetworkEntity entity)
        {
            if (NetworkAPI.Client.IsConnected == false)
            {
                entity = null;
                return false;
            }

            if (NetworkAPI.Room.Entities.TryGet(id, out entity)) return true;

            return false;
        }

        public static bool CheckIfMasterObject(EntityType type) => type == EntityType.SceneObject || type == EntityType.Orphan;

        public static NetworkEntity ResolveComponent(GameObject gameObject)
        {
            var component = QueryComponent.InParents<NetworkEntity>(gameObject);

#if UNITY_EDITOR
            if (component == null)
            {
                component = gameObject.AddComponent<NetworkEntity>();
                ComponentUtility.MoveComponentUp(component);
            }
#endif
            return component;
        }
    }
}