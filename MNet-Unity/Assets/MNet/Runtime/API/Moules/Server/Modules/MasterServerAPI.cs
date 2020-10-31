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

                public static RestAPI Rest { get; private set; }

                public static Dictionary<GameServerID, GameServerInfo> Servers { get; private set; }

                public static void Configure()
                {
                    Rest = new RestAPI(Constants.Server.Master.Rest.Port, NetworkAPI.Config.RestScheme);
                    Rest.SetIP(Address);

                    Servers = new Dictionary<GameServerID, GameServerInfo>();
                }

                public delegate void InfoDelegate(MasterServerInfoResponse info, RestError error);
                public static event InfoDelegate OnInfo;
                public static void GetInfo()
                {
                    var payload = new MasterServerInfoRequest(NetworkAPI.AppID, NetworkAPI.Version);

                    Rest.POST(Constants.Server.Master.Rest.Requests.Info, payload, Callback);

                    void Callback(UnityWebRequest request)
                    {
                        RestAPI.Parse(request, out MasterServerInfoResponse info, out var error);

                        if (error == null) Register(info.Servers);

                        OnInfo?.Invoke(info, error);
                    }
                }

                static void Register(IList<GameServerInfo> list)
                {
                    Servers.Clear();

                    for (int i = 0; i < list.Count; i++) Servers.Add(list[i].ID, list[i]);
                }
            }
        }
    }
}