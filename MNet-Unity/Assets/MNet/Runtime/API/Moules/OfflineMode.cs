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

            internal static byte? Scene { get; private set; }

            internal static void Configure()
            {
                On = false;

                EntityIDs = new AutoKeyCollection<NetworkEntityID>(NetworkEntityID.Min, NetworkEntityID.Max, NetworkEntityID.Increment, Constants.IdRecycleLifeTime);
            }

            internal static RoomInfo Start(RoomOptions options)
            {
                On = true;

                var id = new RoomID(0);
                RoomInfo = new RoomInfo(id, options.Name, options.Capacity, 1, false, false, options.Attributes);
                Scene = options.Scene;

                return RoomInfo;
            }

            internal static void Stop()
            {
                On = false;

                RoomInfo = default;

                EntityIDs.Clear();
            }
        }
    }
}