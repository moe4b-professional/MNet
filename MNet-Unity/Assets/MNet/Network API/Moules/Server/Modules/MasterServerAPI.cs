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

using UnityEngine.Networking;

namespace MNet
{
	public static partial class NetworkAPI
	{
        public static partial class Server
        {
            public static class Master
            {
                public static string Address => NetworkAPI.Address;

                public static DirectedRestAPI Rest { get; private set; }

                public static GameServerInfo[] Servers { get; private set; }

                public static void Configure()
                {
                    Rest = new DirectedRestAPI(Address, Constants.Server.Master.Rest.Port);
                }

                public delegate void InfoDelegate(MasterServerInfoPayload info, RestError error);
                public static event InfoDelegate OnInfo;
                public static void Info()
                {
                    Rest.GET(Constants.Server.Master.Rest.Requests.Info, Callback, false);

                    void Callback(UnityWebRequest request)
                    {
                        RestAPI.Parse(request, out MasterServerInfoPayload info, out var error);

                        Servers = error == null ? info.Servers : null;

                        OnInfo?.Invoke(info, error);
                    }
                }
            }
        }
    }
}