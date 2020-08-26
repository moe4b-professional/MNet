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

        public NetworkClientProfile Player { get; protected set; }

        public List<NetworkEntity> Entities { get; protected set; }

        public NetworkClient(NetworkClientID id, NetworkClientProfile profile)
        {
            this.ID = id;
            this.Player = profile;

            Entities = new List<NetworkEntity>();
        }
    }
}