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
    [Serializable]
    public class NetworkClient
    {
        public NetworkClientID ID { get; protected set; }

        public NetworkClientProfile Profile { get; protected set; }

        public string Name => Profile == null ? "Anonymous Client" : Profile.Name;

        public AttributesCollection Attributes => Profile.Attributes;

        public bool IsMaster => this == NetworkAPI.Room.Master.Client;

        public HashSet<NetworkEntity> Entities { get; protected set; }

        public NetworkClient(NetworkClientID id, NetworkClientProfile profile)
        {
            this.ID = id;

            this.Profile = profile;

            Entities = new HashSet<NetworkEntity>();
        }

        public override string ToString() => ID.ToString();
    }
}