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
    [AddComponentMenu(NetworkAPI.Path + "Tests/" + nameof(LatencyTest))]
	public class LatencyTest : NetworkBehaviour
	{
        const string Payload = "XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX";

        public List<float> samples = new List<float>();

        public int maxSampleCount = 20;

        public float average = 0f;

        bool isProcessing = false;

        void Update()
        {
            if (IsMine && Input.GetKey(KeyCode.Mouse0) && isProcessing == false)
            {
                var time = Time.time;

                isProcessing = true;

                RPC(Click, Owner, Payload, time);
            }
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

            if (samples.Count > maxSampleCount) samples.RemoveAt(0);

            average = samples.Sum() / samples.Count;
        }
    }
}