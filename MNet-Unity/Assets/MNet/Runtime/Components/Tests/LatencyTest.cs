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
    [AddComponentMenu(Constants.Path + "Tests/" + "Latency Test")]
    public class LatencyTest : NetworkBehaviour
    {
        const string Payload = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";

        public int maxSamples = 20;

        public List<float> samples = new List<float>();

        public float average = 0f;

        bool isProcessing = false;

        void Update()
        {
            if (isProcessing) return;

            isProcessing = true;
            RPC(Click, Owner, Payload, Time.time);
        }

        [NetworkRPC]
        void Click(string payload, float time, RpcInfo info)
        {
            var elapsed = (Time.time - time) * 1000;

            Process(elapsed);

            isProcessing = false;
        }

        void Process(float rtt)
        {
            samples.Add(rtt);

            if (samples.Count > maxSamples) samples.RemoveRange(0, samples.Count - maxSamples);

            average = samples.Sum() / samples.Count;
        }
    }
}