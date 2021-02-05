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
    [RequireComponent(typeof(NetworkEntity))]
    public class NetworkEntitySyncTimer : MonoBehaviour
    {
        [SerializeField]
        [SyncInterval(0, 200)]
        [Tooltip("Sync Interval in ms, 1s = 1000ms")]
        int interval = 100;
        public int Interval => interval;

        public event Action OnInvoke;
        void Invoke() => OnInvoke?.Invoke();

        NetworkEntity entity;

        void Awake()
        {
            entity = GetComponent<NetworkEntity>();

            entity.OnReady += ReadyCallback;
            entity.OnDespawn += DespawnCallback;
        }

        void ReadyCallback()
        {
            coroutine = entity.StartCoroutine(Procedure());
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

        void DespawnCallback()
        {
            if (coroutine != null)
            {
                entity.StopCoroutine(coroutine);
                coroutine = null;
            }
        }

        //Static Utility
#if UNITY_EDITOR
        public static NetworkEntitySyncTimer Resolve(NetworkEntity entity)
        {
            var component = Dependancy.Get<NetworkEntitySyncTimer>(entity.gameObject, Dependancy.Scope.CurrentToParents);

            if (component == null)
            {
                component = entity.gameObject.AddComponent<NetworkEntitySyncTimer>();
                ComponentUtility.MoveComponentUp(component);
            }

            return component;
        }
#endif
    }
}