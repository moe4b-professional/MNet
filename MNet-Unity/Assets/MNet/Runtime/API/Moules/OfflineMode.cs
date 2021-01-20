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
        public static class OfflineMode
        {
            public static bool On { get; private set; }

            internal static RoomInfo RoomInfo { get; private set; }

            internal static AutoKeyCollection<NetworkEntityID> EntityIDs { get; private set; }

            internal static void Configure()
            {
                On = false;

                EntityIDs = new AutoKeyCollection<NetworkEntityID>(NetworkEntityID.Increment);
            }

            internal static RoomInfo Start(string name, byte capacity, bool visibile, AttributesCollection attributes)
            {
                On = true;

                RoomInfo = new RoomInfo(default, name, capacity, 1, visibile, attributes);

                return RoomInfo;
            }

            internal static void Stop()
            {
                Clear();
            }

            internal static void Clear()
            {
                On = false;

                RoomInfo = default;

                EntityIDs.Clear();
            }
        }
    }
}