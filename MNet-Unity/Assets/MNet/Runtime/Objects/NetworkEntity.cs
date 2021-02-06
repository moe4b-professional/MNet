﻿using System;
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
        /// Am I the owner of this entity?
        /// </summary>
        public bool IsMine => Owner == NetworkAPI.Client.Self;

        public delegate void OwnerSetDelegate(NetworkClient client);
        public event OwnerSetDelegate OnOwnerSet;
        internal void SetOwner(NetworkClient client)
        {
            Owner = client;

            foreach (var behaviour in Behaviours.Values) behaviour.SetOwner(Owner);

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

        public Dictionary<NetworkBehaviourID, NetworkBehaviour> Behaviours { get; protected set; }

        public Scene Scene => gameObject.scene;

        protected virtual void Awake()
        {
            if (Application.isPlaying == false)
            {
                NetworkScene.Register(this);
                return;
            }

            Behaviours = new Dictionary<NetworkBehaviourID, NetworkBehaviour>();

            var targets = GetComponentsInChildren<NetworkBehaviour>(true);

            if (targets.Length > byte.MaxValue)
                throw new Exception($"Entity {name} May Only Have Up To {byte.MaxValue} Behaviours, Current Count: {targets.Length}");

            for (byte i = 0; i < targets.Length; i++)
            {
                var id = new NetworkBehaviourID(i);

                Behaviours[id] = targets[i];

                targets[i].Set(this, id);
            }
        }

        internal void Setup(NetworkClient owner, EntityType type, PersistanceFlags persistance, AttributesCollection attributes)
        {
            this.Type = type;
            this.Persistance = persistance;
            this.Attributes = attributes;
            this.Owner = owner;

            foreach (var behaviour in Behaviours.Values)
                behaviour.Setup();

            SetOwner(owner);
        }

        public event Action OnSpawn;
        internal void Spawn(NetworkEntityID id)
        {
            this.ID = id;

            if (IsDynamic || IsOrphan) name += $" {id}";

            IsReady = true;

            foreach (var behaviour in Behaviours.Values) behaviour.Spawn();

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

        public event Action OnReady;
        void Ready()
        {
            foreach (var behaviour in Behaviours.Values) behaviour.Ready();

            OnReady?.Invoke();
        }

        #region Remote Sync
        public bool InvokeRPC(IRpcCommand command)
        {
            if (Behaviours.TryGetValue(command.Behaviour, out var target) == false)
            {
                Debug.LogWarning($"No Behaviour with ID {command.Behaviour} found to Invoke RPC");
                return false;
            }

            return target.InvokeRPC(command);
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

        public event Action OnDespawn;
        internal virtual void Despawn()
        {
            IsReady = false;

            foreach (var behaviour in Behaviours.Values) behaviour.Despawn();

            OnDespawn?.Invoke();
        }

        protected virtual void OnDestroy()
        {
            if (Scene.isLoaded && Application.isPlaying == false) NetworkScene.Unregister(this);

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

#if UNITY_EDITOR
        public static NetworkEntity ResolveComponent(GameObject gameObject)
        {
            var component = Dependancy.Get<NetworkEntity>(gameObject, Dependancy.Scope.CurrentToParents);

            if (component == null)
            {
                component = gameObject.AddComponent<NetworkEntity>();
                ComponentUtility.MoveComponentUp(component);
            }

            return component;
        }
#endif
    }
}