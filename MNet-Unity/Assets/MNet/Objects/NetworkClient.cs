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
	public class NetworkClient
	{
        public NetworkClientID ID { get; protected set; }
        public bool IsMaster => this == NetworkAPI.Room.Master;

        public NetworkClientProfile Profile { get; protected set; }
        public string Name => Profile.Name;
        public AttributesCollection Attributes => Profile.Attributes;

        public List<NetworkEntity> Entities { get; protected set; }

        public NetworkClient(NetworkClientInfo info) : this(info.ID, info.Profile) { }
        public NetworkClient(NetworkClientID id, NetworkClientProfile profile)
        {
            this.ID = id;

            this.Profile = profile;

            Entities = new List<NetworkEntity>();
        }

        public override string ToString() => ID.ToString();
    }
}