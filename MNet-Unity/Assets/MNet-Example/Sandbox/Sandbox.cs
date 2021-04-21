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

using Cysharp.Threading.Tasks;

using System.Threading;
using System.Threading.Tasks;
using System.Text;

namespace MNet.Example
{
    public class Sandbox : NetworkBehaviour
    {
        [SyncVar]
        [SerializeField]
        VarInt number = default;

        protected override void OnSpawn()
        {
            base.OnSpawn();

            if (NetworkAPI.Client.IsMaster) BroadcastSyncVar(nameof(number), number, new VarInt(4200));
        }

        [RuntimeInitializeOnLoadMethod]
        static void OnLoad()
        {

        }

#if UNITY_EDITOR
        [MenuItem("Sandbox/Execute")]
        static void Excute()
        {
            
        }
#endif

        static void Measure(Action action, string name = null)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();

            action.Invoke();

            watch.Stop();

            if (name == null) name = action.Method.Name;

            Debug.Log($"{name} Took {watch.ElapsedMilliseconds}ms");
        }
    }
}