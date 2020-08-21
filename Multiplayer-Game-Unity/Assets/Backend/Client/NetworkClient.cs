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

using Game.Shared;

namespace Game
{
	public class NetworkClient
	{
        public NetworkClientID ID { get; set; }

        public NetworkClientInfo Info { get; protected set; }

        public NetworkClient(string name) : this(NetworkClientID.Empty, new NetworkClientInfo(name)) { }
        public NetworkClient(NetworkClientID id, NetworkClientInfo info)
        {
            this.ID = id;
            this.Info = info;
        }
    }
}