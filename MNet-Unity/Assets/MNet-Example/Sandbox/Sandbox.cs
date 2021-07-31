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
        public override void OnNetwork()
        {
            base.OnNetwork();

            Network.OnSpawn += SpawnCallback;
        }

        void SpawnCallback()
        {
            
        }

        [RuntimeInitializeOnLoadMethod]
        static void OnLoad()
        {
            
        }

        struct SampleStruct : ISampleInterface
        {
            public string Text { get; set; }
        }

        interface ISampleInterface
        {
            string Text { get; set; }
        }

#if UNITY_EDITOR
        [MenuItem("Sandbox/Execute")]
        static void Excute()
        {
            var collection = new HashSet<ISampleInterface>();

            var sample = new SampleStruct();

            collection.Add(sample);

            sample.Text = "Hello World";

            collection.Add(sample);

            Debug.Log(collection.First().Text);
            Debug.Log(collection.Last().Text);
        }
#endif
    }
}