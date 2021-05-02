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

using System.Reflection;

using Cysharp.Threading.Tasks;

using System.Threading;

namespace MNet
{
    public interface INetworkBehaviour
    {
        NetworkEntity.Behaviour Network { get; set; }

        void OnNetwork();
    }

    public class NetworkBehaviour : MonoBehaviour, INetworkBehaviour
    {
        public NetworkEntity.Behaviour Network { get; set; }

        public NetworkEntity Entity => Network.Entity;

        public virtual void OnNetwork()
        {

        }
    }
}