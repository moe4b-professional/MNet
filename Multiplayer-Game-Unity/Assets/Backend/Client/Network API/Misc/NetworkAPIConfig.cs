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
using Backend;

namespace Game
{
	[CreateAssetMenu]
	public class NetworkAPIConfig : ScriptableObject
	{
        [SerializeField]
        protected string address = "127.0.0.1";
        public string Address => address;

        [SerializeField]
        protected NetworkTransportType transport;
        public NetworkTransportType Transport => transport;
    }
}