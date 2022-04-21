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

using MB;
using System.Reflection;
using System.Runtime.InteropServices;

using MNet;

namespace MNet.Example
{
    [Preserve]
    public class Sandbox : NetworkBehaviour
    {
        public override void OnNetwork()
        {
            base.OnNetwork();

            Network.OnSpawn += SpawnCallback;
        }

        void SpawnCallback()
        {
            
        }

#if UNITY_EDITOR
        [MenuItem("Sandbox/Execute")]
        static void Execute()
        {
            
        }
#endif
    }
}