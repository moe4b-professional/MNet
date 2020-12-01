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

using MNet;

namespace Game
{
    [AddComponentMenu(Constants.Path + "Tests/" + "Packet Loss Test")]
	public class PacketLossTest : NetworkBehaviour
    {
		public float interval = 0.5f;

		public int sent = 0;

		public int recieved = 0;

		public int loss = 0;

        void Start()
        {
            StartCoroutine(Procedure());
        }

        IEnumerator Procedure()
        {
            while(true)
            {
                yield return new WaitForSeconds(interval);

                if(IsReady)
                {
                    TargetRPC(Call, NetworkAPI.Client.Self);

                    sent += 1;
                }
            }
        }

        [NetworkRPC(Delivery = DeliveryMode.Unreliable)]
        void Call(RpcInfo info)
        {
            recieved += 1;

            loss = sent - recieved;
        }
    }
}