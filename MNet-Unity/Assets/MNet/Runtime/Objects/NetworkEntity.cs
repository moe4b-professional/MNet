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

namespace MNet
{
    [ExecuteInEditMode]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(ExecutionOrder)]
    [AddComponentMenu(Constants.Path + "Network Entity")]
    public class NetworkEntity : MonoBehaviour
    {
        public const int ExecutionOrder = -400;

        public NetworkEntityID ID { get; protected set; }

        public bool IsConnected => NetworkAPI.Client.IsConnected;

        public NetworkEntityType Type { get; internal set; }

        public bool IsSceneObject => Type == NetworkEntityType.SceneObject;
        public bool IsDynamic => Type == NetworkEntityType.Dynamic;
        public bool IsOrphan => Type == NetworkEntityType.Orphan;

        /// <summary>
        /// Is this an entity that will always be owned by the master client? such as scene objects and orphans
        /// </summary>
        public bool IsMasterObject => Type == NetworkEntityType.SceneObject | Type == NetworkEntityType.Orphan;

        public PersistanceFlags Persistance { get; protected set; }

        #region Ownership
        public NetworkClient Owner { get; protected set; }

        /// <summary>
        /// Am I the owner of this entity?
        /// </summary>
        public bool IsMine => Owner == NetworkAPI.Client.Self;

        public delegate void OwnerSetDelegate(NetworkClient client);
        public event OwnerSetDelegate OnOwnerSet;
        internal void SetOwner(NetworkClient client)
        {
            Owner = client;

            OnOwnerSet?.Invoke(Owner);
        }

        public bool TakeoverOwnership()
        {
            if (IsMine)
            {
                Debug.LogWarning($"You Already Own Entity {this}, no Need to Takeover it, Ignoring");
                return false;
            }

            return ChangeOwnership(NetworkAPI.Client.Self);
        }

        public bool ChangeOwnership(NetworkClient reciever)
        {
            if (IsMasterObject)
            {
                Log.Error($"Master Objects Cannot be Taken Over by Clients, Will not Send Change Ownership Request");
                return false;
            }

            if (Owner == reciever)
            {
                Debug.LogWarning($"Client: {reciever} Already Owns Entity '{this};, Ignoring Request");
                return false;
            }

            var requst = new ChangeEntityOwnerRequest(reciever.ID, ID);

            return NetworkAPI.Client.Send(ref requst);
        }
        #endregion

        public AttributesCollection Attributes { get; protected set; }

        public Dictionary<NetworkBehaviourID, NetworkBehaviour> Behaviours { get; protected set; }

        public bool IsReady { get; protected set; } = false;

        public Scene Scene => gameObject.scene;

        protected virtual void Awake()
        {
            if (Application.isPlaying == false)
            {
                NetworkScene.Register(this);
                return;
            }
        }

        internal void Setup()
        {
            Behaviours = new Dictionary<NetworkBehaviourID, NetworkBehaviour>();

            var targets = GetComponentsInChildren<NetworkBehaviour>(true);

            if (targets.Length > byte.MaxValue)
                throw new Exception($"Entity {name} May Only Have Up To {byte.MaxValue} Behaviours, Current Count: {targets.Length}");

            for (byte i = 0; i < targets.Length; i++)
            {
                var id = new NetworkBehaviourID(i);

                targets[i].Setup(this, id);

                Behaviours[id] = targets[i];
            }
        }

        internal void Load(NetworkClient owner, NetworkEntityID id, AttributesCollection attributes, NetworkEntityType type, PersistanceFlags persistance)
        {
            IsReady = true;

            this.ID = id;
            this.Attributes = attributes;
            this.Type = type;
            this.Persistance = persistance;

            SetOwner(owner);

            foreach (var behaviour in Behaviours.Values) behaviour.Load();

            if (NetworkAPI.Realtime.IsOnBuffer)
                SpawnAfterBuffer();
            else
                Spawn();
        }

        #region Spawn
        void SpawnAfterBuffer()
        {
            NetworkAPI.Realtime.OnBufferEnd += AppliedBufferCallback;
        }
        void AppliedBufferCallback(IList<NetworkMessage> list)
        {
            NetworkAPI.Realtime.OnBufferEnd -= AppliedBufferCallback;

            Spawn();
        }

        public event Action OnSpawn;
        void Spawn()
        {
            OnSpawn?.Invoke();
        }
        #endregion

        #region Despawn
        public event Action OnDespawn;
        internal virtual void Despawn()
        {
            IsReady = false;

            OnDespawn?.Invoke();
        }
        #endregion

        #region Remote Sync
        public void InvokeRPC(RpcCommand command)
        {
            if (Behaviours.TryGetValue(command.Behaviour, out var target) == false)
            {
                Debug.LogWarning($"No Behaviour with ID {command.Behaviour} found to Invoke RPC");
                if (command.Type == RpcType.Query) NetworkAPI.Client.RPR.Respond(command, RemoteResponseType.FatalFailure);
                return;
            }

            target.InvokeRPC(command);
        }

        public void InvokeSyncVar(SyncVarCommand command)
        {
            if (Behaviours.TryGetValue(command.Behaviour, out var target) == false)
            {
                Debug.LogWarning($"No Behaviour with ID {command.Behaviour} found to invoke RPC");
                return;
            }

            target.InvokeSyncVar(command);
        }
        #endregion

        protected virtual void OnDestroy()
        {
            NetworkAPI.Realtime.OnBufferEnd -= AppliedBufferCallback;

            if (Scene.isLoaded && Application.isPlaying == false) NetworkScene.Unregister(this);
        }

        //Static Utility
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

            if (NetworkAPI.Room.Entities.TryGetValue(id, out entity)) return true;

            return false;
        }

        public static bool CheckIfMasterObject(NetworkEntityType type) => type == NetworkEntityType.SceneObject || type == NetworkEntityType.Orphan;
    }
}