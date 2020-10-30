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
    public class NetworkEntity : MonoBehaviour
    {
        public const int ExecutionOrder = -200;

        public NetworkEntityID ID { get; protected set; }

        public NetworkClient Owner { get; protected set; }
        public void SetOwner(NetworkClient client)
        {
            Owner = client;
        }

        public bool IsMine => Owner?.ID == NetworkAPI.Client.ID;

        public AttributesCollection Attributes { get; protected set; }

        public Dictionary<NetworkBehaviourID, NetworkBehaviour> Behaviours { get; protected set; }
        public bool TryGetBehaviour(NetworkBehaviourID id, out NetworkBehaviour behaviour) => Behaviours.TryGetValue(id, out behaviour);

        public bool IsReady { get; protected set; } = false;

        public NetworkEntityType Type { get; protected set; }

        public Scene Scene => gameObject.scene;

        protected virtual void Awake()
        {
            if (Application.isPlaying == false)
            {
                NetworkScene.Register(this);
                return;
            }
        }

        public void Configure()
        {
            Behaviours = new Dictionary<NetworkBehaviourID, NetworkBehaviour>();

            var targets = GetComponentsInChildren<NetworkBehaviour>(true);

            if (targets.Length > byte.MaxValue)
                throw new Exception($"Entity {name} May Only Have Up To {byte.MaxValue} Behaviours, Current Count: {targets.Length}");

            for (byte i = 0; i < targets.Length; i++)
            {
                var id = new NetworkBehaviourID(i);

                targets[i].Configure(this, id);

                Behaviours[id] = targets[i];
            }
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
        public event Action OnSpawn;
        public void Spawn(NetworkClient owner, NetworkEntityID id, AttributesCollection attributes, NetworkEntityType type)
        {
            SetOwner(owner);

            this.ID = id;
            this.Attributes = attributes;
            this.Type = type;

            IsReady = true;

            OnSpawn?.Invoke();
        }

        public event Action OnDespawn;
        public virtual void Despawn()
        {
            IsReady = false;

            OnDespawn?.Invoke();
        }
        #endregion

        #region RPR
        public AutoKeyDictionary<ushort, RprBind> RPRs { get; protected set; }

        public RprBind RegisterRPR(MethodInfo method, object target)
        {
            var code = RPRs.Reserve();

            var callback = new RprBind(code, method, target);

            RPRs.Assign(code, callback);

            return callback;
        }

        public void InvokeRPR(RprCommand command)
        {
            if (command == null) throw new ArgumentNullException(nameof(command), "RPC Callback Payload is Null");

            if (RPRs.TryGetValue(command.Callback, out var bind) == false)
            {
                Debug.LogError($"Couldn't Find RPR with Code {command.Callback} to Invoke On Entity {name}");
                return;
            }

            object[] arguments;
            try
            {
                arguments = bind.ParseArguments(command);
            }
            catch (Exception e)
            {
                var text = $"Error trying to read RPR Argument of {name}'s {command.Callback} callback as {bind.ReturnType}, Invalid Data Sent Most Likely \n" +
                    $"Exception: \n" +
                    $"{e.ToString()}";

                Debug.LogError(text, this);
                return;
            }

            try
            {
                bind.Invoke(arguments);
            }
            catch (TargetInvocationException)
            {
                throw;
            }
            catch (ArgumentException)
            {
                var text = $"Error Trying to Invoke RPR '{ bind.Method.Name}' on '{ bind.Target}', " +
                    $"Please Ensure Callback Method is Implemented Correctly to Consume Recieved Argument: {arguments.GetType()}";

                Debug.LogError(text, this);
            }

            UnregisterRPR(bind);
        }

        public void UnregisterRPR(RprBind bind)
        {
            RPRs.Remove(bind.ID);
        }
        #endregion

        public void InvokeRPC(RpcCommand command)
        {
            if (Behaviours.TryGetValue(command.Behaviour, out var target) == false)
            {
                Debug.LogWarning($"No Behaviour with ID {command.Behaviour} found to invoke RPC");

                NetworkAPI.Room.ResolveRPC(command, RprResult.InvalidBehaviour);

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
            if (Scene.isLoaded && Application.isPlaying == false) NetworkScene.Unregister(this);
        }

        public NetworkEntity()
        {
            RPRs = new AutoKeyDictionary<ushort, RprBind>(RprBind.Increment);
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