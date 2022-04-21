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
    public partial class NetworkEntity : MonoBehaviour, PreAwake.IInterface
    {
        public const int ExecutionOrder = -500;

        public NetworkEntityID ID { get; protected set; }

        public bool IsConnected => NetworkAPI.Client.IsConnected;

        /// <summary>
        /// Boolean value to show if this Entity is ready to send and recieve messages
        /// </summary>
        public bool IsReady { get; protected set; } = false;

        public PersistanceFlags Persistance { get; protected set; }

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
            if (NetworkAPI.Client.Buffer.IsOn) return true;

            if (authority.HasFlagFast(RemoteAuthority.Any)) return true;

            if (client == null) return false;

            if (authority.HasFlagFast(RemoteAuthority.Owner))
            {
                if (client == Owner)
                    return true;
            }

            if (authority.HasFlagFast(RemoteAuthority.Master))
            {
                if (client == NetworkAPI.Room.Master.Client)
                    return true;
            }

            return false;
        }
        #endregion

        public AttributesCollection Attributes { get; protected set; }

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

            [SerializeField, DebugOnly]
            NetworkEntity Entity;

            [SerializeField, ReadOnly]
            List<Component> components;
            public List<ISync> Targets { get; private set; }

            internal void PreAwake(NetworkEntity reference)
            {
                Entity = reference;

                using (ComponentQuery.Collection.NonAlloc.InHierarchy<ISync>(reference, out var targets))
                {
                    components = new List<Component>(targets.Count);

                    for (int i = 0; i < targets.Count; i++)
                        components.Add(targets[i] as Component);
                }
            }

            internal void Setup()
            {
                if (components.Count == 0) return;

                Targets = new List<ISync>(components.Count);

                for (int i = 0; i < components.Count; i++)
                    Targets.Add(components[i] as ISync);

                Entity.OnReady += ReadyCallback;
                Entity.OnDespawn += DespawnCallback;
            }

            void ReadyCallback()
            {
                routine = MRoutine.Create(Procedure).Start();
            }

            MRoutine.Handle routine;
            IEnumerator Procedure()
            {
                while (true)
                {
                    if (Entity.IsMine) Invoke();

                    yield return MRoutine.Wait.Seconds(interval / 1000f);
                }
            }

            void Invoke()
            {
                for (int i = 0; i < Targets.Count; i++)
                    Targets[i].Sync();
            }

            void DespawnCallback()
            {
                if (routine.IsValid) MRoutine.Stop(routine);
            }
        }

        public interface ISync
        {
            void Sync();
        }

        [SerializeField]
        BehavioursProperty behaviours;
        public BehavioursProperty Behaviours => behaviours;
        [Serializable]
        public class BehavioursProperty
        {
            [SerializeField, DebugOnly]
            NetworkEntity Entity;

            [SerializeField,ReadOnly]
            List<Component> components;

            public DualDictionary<NetworkBehaviourID, MonoBehaviour, Behaviour> Dictionary { get; protected set; }
            public virtual Behaviour Get<T>(T reference)
                where T : MonoBehaviour
            {
                if (Dictionary.TryGetValue(reference, out var behaviour) == false)
                    throw new Exception($"Cannot Retrieve Network Behaviour for Unregisterd Type of {typeof(T).Name}");

                return behaviour;
            }

            internal void PreAwake(NetworkEntity reference)
            {
                Entity = reference;

                using (ComponentQuery.Collection.NonAlloc.InHierarchy<INetworkBehaviour>(Entity, out var targets))
                {
                    if (targets.Count > byte.MaxValue)
                        throw new Exception($"Entity {Entity} May Only Have Up To {byte.MaxValue} Behaviours, Current Count: {targets.Count}");

                    components = new List<Component>(targets.Count);

                    for (int i = 0; i < targets.Count; i++)
                        components.Add(targets[i] as Component);
                }
            }

            internal void Awake()
            {
                Dictionary = new DualDictionary<NetworkBehaviourID, MonoBehaviour, Behaviour>();

                for (byte i = 0; i < components.Count; i++)
                {
                    var id = new NetworkBehaviourID(i);

                    var target = components[i] as INetworkBehaviour;

                    var behaviour = new Behaviour(Entity, target, id);

                    Dictionary[id, behaviour.Component] = behaviour;

                    target.Network = behaviour;
                    target.OnNetwork();
                }
            }
        }

        [SerializeField]
        ListenersProperty listeners;
        public ListenersProperty Listeners => listeners;
        [Serializable]
        public class ListenersProperty
        {
            [SerializeField, DebugOnly]
            NetworkEntity Entity;

            [SerializeField, ReadOnly]
            List<Component> components;

            internal void PreAwake(NetworkEntity reference)
            {
                Entity = reference;

                using (ComponentQuery.Collection.NonAlloc.InHierarchy<INetworkListener>(Entity, out var targets))
                {
                    components = new List<Component>(targets.Count);

                    for (int i = 0; i < targets.Count; i++)
                        components.Add(targets[i] as Component);
                }
            }

            internal void Awake()
            {
                for (int i = 0; i < components.Count; i++)
                {
                    var target = components[i] as INetworkListener;

                    target.Entity = Entity;
                    target.OnNetwork();
                }
            }
        }

        public Scene UnityScene => gameObject.scene;
        public NetworkScene NetworkScene { get; protected set; }

        public virtual void PreAwake()
        {
            sync.PreAwake(this);
            behaviours.PreAwake(this);
            listeners.PreAwake(this);
        }

        protected virtual void Awake()
        {
            if (Application.isPlaying == false)
            {
                NetworkScene.RegisterLocal(this);
                return;
            }

            behaviours.Awake();
            listeners.Awake();
        }

        public delegate void SetupDelegate();
        public event SetupDelegate OnSetup;
        internal void Setup(NetworkClient owner, EntityType type, PersistanceFlags persistance, AttributesCollection attributes)
        {
            this.Type = type;
            this.Persistance = persistance;
            this.Attributes = attributes;
            this.Owner = owner;

            sync.Setup();

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

            if (NetworkAPI.Client.Buffer.IsOn)
                ReadyAfterBuffer();
            else
                Ready();
        }

        #region Ready After Buffer
        void ReadyAfterBuffer()
        {
            NetworkAPI.Client.Buffer.OnEnd += ReadyAfterBufferCallback;
        }

        void ReadyAfterBufferCallback()
        {
            NetworkAPI.Client.Buffer.OnEnd -= ReadyAfterBufferCallback;
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
            where T : struct, IRpcCommand
        {
            if (Behaviours.Dictionary.TryGetValue(command.Behaviour, out var target) == false)
            {
                Debug.LogWarning($"No Behaviour with ID {command.Behaviour} found to Invoke RPC");
                return false;
            }

            return target.InvokeRPC(ref command);
        }

        public void InvokeSyncVar<TCommand>(TCommand command)
            where TCommand : ISyncVarCommand
        {
            if (Behaviours.Dictionary.TryGetValue(command.Behaviour, out var target) == false)
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

            NetworkAPI.Client.Buffer.OnEnd -= ReadyAfterBufferCallback;
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
            var component = ComponentQuery.Single.InParents<NetworkEntity>(gameObject);

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