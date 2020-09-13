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

namespace Backend
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

        public bool IsReady { get; protected set; } = false;

        public NetworkEntityType Type { get; protected set; }

        public Scene Scene => gameObject.scene;

        protected virtual void Awake()
        {
            NetworkScene.Register(this);
        }

        public void Configure(NetworkClient owner, NetworkEntityID id, AttributesCollection attributes, NetworkEntityType type)
        {
            IsReady = true;

            SetOwner(owner);
            this.ID = id;
            this.Attributes = attributes;
            this.Type = type;

            RegisterBehaviours();

            OnSpawn();
        }

        void RegisterBehaviours()
        {
            Behaviours = new Dictionary<NetworkBehaviourID, NetworkBehaviour>();

            var targets = GetComponentsInChildren<NetworkBehaviour>();

            if (targets.Length > byte.MaxValue)
                throw new Exception($"Entity {name} May Only Have Up To {byte.MaxValue} Behaviours, Current Count: {targets.Length}");

            var count = (byte)targets.Length;

            for (byte i = 0; i < count; i++)
            {
                var id = new NetworkBehaviourID(i);

                targets[i].Configure(this, id);

                Behaviours.Add(id, targets[i]);
            }
        }

        #region RPC
        public void InvokeRpc(RpcCommand command)
        {
            if (Behaviours.TryGetValue(command.Behaviour, out var target) == false)
            {
                Debug.LogWarning($"No Behaviour with ID {command.Behaviour} found to invoke RPC");
                return;
            }

            target.InvokeRPC(command);
        }

        public IDCollection<RpcCallback> RpcCallbacks { get; protected set; }

        public RpcCallback RegisterRpcCallback(MethodInfo method, object target)
        {
            var code = RpcCallbacks.Reserve();

            var callback = new RpcCallback(code, method, target);

            RpcCallbacks.Assign(callback, code);

            return callback;
        }

        public void InvokeRpcCallback(RpcCallbackPayload payload)
        {
            if (payload == null) throw new ArgumentNullException(nameof(payload), "RPC Callback Payload is Null");

            if (RpcCallbacks.TryGetValue(payload.Callback, out var callback) == false)
            {
                Debug.LogError($"Couldn't Find RPC Callback with Code {payload.Callback} to Invoke On Entity {name}");
                return;
            }

            object[] arguments;
            try
            {
                arguments = callback.ParseArguments(payload);
            }
            catch (Exception e)
            {
                var text = $"Error trying to read RPC Callback Argument of {name}'s {payload.Callback} callback as {callback.Type}, Invalid Data Sent Most Likely \n" +
                    $"Exception: \n" +
                    $"{e.ToString()}";

                Debug.LogError(text, this);
                return;
            }

            try
            {
                callback.Invoke(arguments);
            }
            catch(TargetInvocationException)
            {
                throw;
            }
            catch (ArgumentException)
            {
                var text = $"Error Trying to Invoke RPC Callback '{ callback.Method.Name}' on '{ callback.Target}', " +
                    $"Please Ensure Callback Method is Implemented Correctly to Consume Recieved Argument: {arguments.GetType()}";

                Debug.LogError(text, this);
            }

            RpcCallbacks.Remove(callback);
        }
        #endregion

        protected virtual void OnSpawn()
        {

        }

        protected virtual void OnDestroy()
        {
            if(Scene.isLoaded) NetworkScene.Unregister(this);
        }

        public NetworkEntity()
        {
            RpcCallbacks = new IDCollection<RpcCallback>();
        }
    }
}