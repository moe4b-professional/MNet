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

                public static RestClientAPI Rest { get; private set; }

                internal static void Configure()
                {
                    Rest = new RestClientAPI(Constants.Server.Master.Rest.Port, NetworkAPI.Config.RestScheme);
                    Rest.SetIP(Address);
                }

                public delegate void SchemeDelegate(MasterServerSchemeResponse response, RestError error);
                public static event SchemeDelegate OnScheme;
                public static void GetScheme(SchemeDelegate handler = null)
                {
                    var payload = new MasterServerSchemeRequest(NetworkAPI.AppID, NetworkAPI.GameVersion);

                    Rest.POST<MasterServerSchemeRequest, MasterServerSchemeResponse>(Constants.Server.Master.Rest.Requests.Scheme, payload, Callback);

                    void Callback(MasterServerSchemeResponse response, RestError error)
                    {
                        handler?.Invoke(response, error);
                        OnScheme?.Invoke(response, error);
                    }
                }

                public delegate void InfoDelegate(MasterServerInfoResponse info, RestError error);
                public static event InfoDelegate OnInfo;
                public static void GetInfo(InfoDelegate handler = null)
                {
                    var payload = new MasterServerInfoRequest();

                    Rest.POST<MasterServerInfoRequest, MasterServerInfoResponse>(Constants.Server.Master.Rest.Requests.Info, payload, Callback);

                    void Callback(MasterServerInfoResponse info, RestError error)
                    {
                        handler?.Invoke(info, error);
                        OnInfo?.Invoke(info, error);
                    }
                }
            }
        }
    }
}