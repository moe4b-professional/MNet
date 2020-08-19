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

namespace Game
{
    [RequireComponent(typeof(NetworkIdentity))]
    public class NetworkBehaviour : NetworkObject
	{
        public void GenerateID()
        {
            ID = Guid.NewGuid().ToString("N");
        }

        public NetworkIdentity Identity { get; protected set; }
        public void Set(NetworkIdentity reference)
        {
            Identity = reference;
        }

        protected virtual void Reset()
        {
            GenerateID();
        }

        protected virtual void OnValidate()
        {
            if (string.IsNullOrEmpty(ID)) GenerateID();
        }
    }
}