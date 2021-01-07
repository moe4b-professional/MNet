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

using Cysharp.Threading.Tasks;

using System.Threading;
using System.Threading.Tasks;

namespace MNet.Example
{
    public class Sandbox : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod]
        static void OnLoad()
        {

        }
    }
}