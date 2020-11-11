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

using MNet;

namespace Game
{
    public class Player : NetworkBehaviour
    {
        public MeshRenderer mesh;

        public float speed;

        protected override void OnSpawn()
        {
            base.OnSpawn();

            if (Attributes != null)
            {
                Read(Entity.Attributes, out var position, out var rotation);

                transform.position = position;
                transform.rotation = rotation;
            }
        }

        public static void Write(ref AttributesCollection attributes, Vector3 position, Quaternion rotation)
        {
            attributes.Set(0, position);
            attributes.Set(1, rotation);
        }
        public static void Read(AttributesCollection attributes, out Vector3 position, out Quaternion rotation)
        {
            attributes.TryGetValue(0, out position);
            attributes.TryGetValue(1, out rotation);
        }

        void Start()
        {
            var block = new MaterialPropertyBlock();
            block.SetColor("_Color", IsMine ? Color.green : Color.red);
            mesh.SetPropertyBlock(block);

            if (IsMine) RequestFamiliar();
        }

        #region Familiar
        void RequestFamiliar() => RPC(SpawnFamiliar, NetworkAPI.Room.Master, FamiliarRequestCallback);

        [NetworkRPC(RemoteAuthority.Owner)]
        bool SpawnFamiliar(RpcInfo info)
        {
            NetworkAPI.Client.SpawnEntity("Familiar", null, info.Sender.ID);
            return true;
        }

        void FamiliarRequestCallback(RprResult result, bool value)
        {
            if (result != RprResult.Success)
            {
                Debug.LogError("Familiar Request Failed, Retrying");
                RequestFamiliar();
                return;
            }

            Log.Info($"Familiar Request Result: {value}");
        }
        #endregion

        void Update()
        {
            if (IsMine)
            {
                var direction = new Vector2()
                {
                    x = Input.GetAxisRaw("Horizontal"),
                    y = Input.GetAxisRaw("Vertical"),
                };

                if (direction.magnitude > 0.1f) RPC(RequestMove, NetworkAPI.Room.Master, direction);
            }
        }

        [NetworkRPC(RemoteAuthority.Owner)]
        void RequestMove(Vector2 input, RpcInfo info)
        {
            input = Vector2.ClampMagnitude(input, 1f);

            var direction = new Vector3(input.x, 0f, input.y);

            var velocity = Vector3.ClampMagnitude(direction * speed, speed);

            var position = transform.position + (velocity * Time.deltaTime);
            var rotation = velocity.magnitude > 0.1f ? Quaternion.LookRotation(velocity) : transform.rotation;

            RPC(SetCoordinates, RpcBufferMode.Last, position, rotation);
        }

        [NetworkRPC(RemoteAuthority.Master)]
        void SetCoordinates(Vector3 position, Quaternion rotation, RpcInfo info)
        {
            transform.position = position;
            transform.rotation = rotation;
        }
    }
}