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
	public class NetworkObject : MonoBehaviour
	{
        [ReadOnly]
        [SerializeField]
        protected string _ID = string.Empty;
        public string ID
        {
            get => _ID;
            set => _ID = value;
        }

        protected virtual void Awake()
        {

        }
    }
}