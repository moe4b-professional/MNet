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
    public static partial class NetworkAPI
    {
        public static class Physics
        {
            public static bool AutoSimulate
            {
                get => UnityEngine.Physics.autoSimulation;
                set => UnityEngine.Physics.autoSimulation = value;
            }

            internal static void Configure()
            {
                Realtime.Pause.OnBegin += UpdateState;
                Realtime.Pause.OnEnd += UpdateState;
            }

            static void UpdateState()
            {
                if (Realtime.Pause.Value)
                    AutoSimulate = false;
                else
                    AutoSimulate = true;
            }
        }
    }
}