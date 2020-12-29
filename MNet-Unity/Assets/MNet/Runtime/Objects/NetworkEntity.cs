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
using System.Reflection;

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

        #region Ownership
        public NetworkClient Owner { get; protected set; }

        public bool IsMine
        {
            get
            {
                if (IsConnected == false) return false;

                return Owner?.ID == NetworkAPI.Client.ID;
            }
        }

        public delegate void SetOwnerDelegate(NetworkClient client);
        public event SetOwnerDelegate OnSetOwner;
        public void SetOwner(NetworkClient client)
        {
            Owner = client;

            OnSetOwner?.Invoke(Owner);
        }

        public void TakeoverOwnership()
        {
            if (IsMine)
            {
                Debug.LogWarning($"You Already Own Entity {this}, no Need to Takeover it, Ignoring");
                return;
            }

            ChangeOwnership(NetworkAPI.Client.Self);
        }

        public void ChangeOwnership(NetworkClient reciever)
        {
            if (Owner == reciever)
            {
                Debug.LogWarning($"Client: {reciever} Already Owns Entity '{this};, Ignoring Request");
                return;
            }

            var requst = new ChangeEntityOwnerRequest(reciever.ID, ID);

            NetworkAPI.Client.Send(requst);
        }
        #endregion

        public AttributesCollection Attributes { get; protected set; }

        public Dictionary<NetworkBehaviourID, NetworkBehaviour> Behaviours { get; protected set; }

        public bool IsConnected => NetworkAPI.Client.IsConnected;

        public bool IsReady { get; protected set; } = false;

        public NetworkEntityType Type { get; protected set; }

        public PersistanceFlags Persistance { get; protected set; }

        public Scene Scene => gameObject.scene;

        protected virtual void Awake()
        {
            if (Application.isPlaying == false)
            {
                NetworkScene.Register(this);
                return;
            }
        }

        public void Setup(NetworkClient owner, NetworkEntityID id, AttributesCollection attributes, NetworkEntityType type, PersistanceFlags persistance)
        {
            IsReady = true;

            this.ID = id;
            this.Attributes = attributes;
            this.Type = type;
            this.Persistance = persistance;

            Behaviours = new Dictionary<NetworkBehaviourID, NetworkBehaviour>();

            var targets = GetComponentsInChildren<NetworkBehaviour>(true);

            if (targets.Length > byte.MaxValue)
                throw new Exception($"Entity {name} May Only Have Up To {byte.MaxValue} Behaviours, Current Count: {targets.Length}");

            for (byte i = 0; i < targets.Length; i++)
            {
                var a = new NetworkBehaviourID(i);

                targets[i].Setup(this, a);

                Behaviours[a] = targets[i];
            }

            SetOwner(owner);

            if (NetworkAPI.Realtime.IsOnBuffer)
                SpawnAfterBuffer();
            else
                Spawn();
        }

        public void UpdateReadyState()
        {
            ICollection<NetworkBehaviour> collection;

            if (Behaviours == null || Behaviours.Count == 0)
                collection = GetComponentsInChildren<NetworkBehaviour>(true);
            else
                collection = Behaviours.Values;

            foreach (var behaviour in collection) behaviour.UpdateReadyState();
        }

        #region Spawn
        void SpawnAfterBuffer()
        {
            NetworkAPI.Realtime.OnAppliedBuffer += AppliedBufferCallback;
        }
        void AppliedBufferCallback()
        {
            NetworkAPI.Realtime.OnAppliedBuffer -= AppliedBufferCallback;

            Spawn();
        }

        public event Action OnSpawn;
        void Spawn()
        {
            OnSpawn?.Invoke();
        }

        public event Action OnDespawn;
        public virtual void Despawn()
        {
            IsReady = false;

            OnDespawn?.Invoke();
        }
        #endregion

        public void InvokeRPC(RpcCommand command)
        {
            if (Behaviours.TryGetValue(command.Behaviour, out var target) == false)
            {
                Debug.LogWarning($"No Behaviour with ID {command.Behaviour} found to Invoke RPC");
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

        protected virtual void OnDestroy()
        {
            NetworkAPI.Realtime.OnAppliedBuffer -= AppliedBufferCallback;

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
    }
}