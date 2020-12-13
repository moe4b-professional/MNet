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

namespace MNet.Example
{
    public class NetworkStressBehaviour : NetworkBehaviour
    {
        public int area;

        public float delay = 0.05f;

        public int pakcets = 0;

        void ReadAttributes(out Vector3 position, out float angle, out int area)
        {
            Attributes.TryGetValue(0, out position);
            Attributes.TryGetValue(1, out angle);
            Attributes.TryGetValue(2, out area);
        }

        void Start()
        {
            name = $"Sample {Entity.ID}";

            ReadAttributes(out var position, out var angle, out area);

            Apply(position, angle);

            if (IsMine) StartCoroutine(Procedure());
        }

        IEnumerator Procedure()
        {
            while (IsConnected)
            {
                Broadcast();

                yield return new WaitForSeconds(delay);
            }
        }

        void Broadcast()
        {
            var position = transform.position + CalculateRandomOffset(1f, 0f, 1f);

            var angle = transform.eulerAngles.y + Random.Range(0, 40);

            BroadcastRPC(Call, position, angle);
        }

        [NetworkRPC(Delivery = DeliveryMode.Unreliable)]
        void Call(Vector3 position, float angle, RpcInfo info)
        {
            Apply(position, angle);

            pakcets += 1;
        }

        void Apply(Vector3 position, float angle)
        {
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        //Static Utility
        public static void CalculateRandomCoords(int area, out Vector3 position, out float angle)
        {
            position = new Vector3()
            {
                x = Random.Range(-area, area),
                y = 0,
                z = Random.Range(-area, area)
            };

            angle = Random.Range(0, 360);
        }

        public static Vector3 CalculateRandomOffset(float x, float y, float z)
        {
            return new Vector3()
            {
                x = x * Random.Range(-1f, 1f),
                y = y * Random.Range(-1f, 1f),
                z = z * Random.Range(-1f, 1f),
            };
        }
    }
}