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

namespace Backend
{
	public class NetworkClient
	{
        public NetworkClientInfo Info { get; protected set; }

        public NetworkClientID ID => Info.ID;

        public NetworkClientProfile Profile => Info.Profile;

        public string Name => Profile.Name;
        public AttributesCollection Attributes => Profile.Attributes;

        public List<NetworkEntity> Entities { get; protected set; }

        public NetworkClient(NetworkClientInfo info)
        {
            this.Info = info;

            Entities = new List<NetworkEntity>();
        }

        public override string ToString() => ID.ToString();
    }
}