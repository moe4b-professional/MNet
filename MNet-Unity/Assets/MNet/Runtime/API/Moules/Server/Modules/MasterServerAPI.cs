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

using Cysharp.Threading.Tasks;

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

                public static async UniTask<MasterServerSchemeResponse> GetScheme()
                {
                    var payload = new MasterServerSchemeRequest(AppID, GameVersion);

                    var response = await Rest.POST<MasterServerSchemeRequest, MasterServerSchemeResponse>(Constants.Server.Master.Rest.Requests.Scheme, payload);

                    Server.SetRemoteConfig(response.RemoteConfig);
                    AppAPI.Set(response.App);

                    SetInfo(response.Info);

                    return response;
                }

                static void SetInfo(MasterServerInfoResponse info)
                {
                    Server.Game.Register(info.Servers);
                }

                public static async UniTask<MasterServerInfoResponse> GetInfo()
                {
                    var payload = new MasterServerInfoRequest();

                    var response = await Rest.POST<MasterServerInfoRequest, MasterServerInfoResponse>(Constants.Server.Master.Rest.Requests.Info, payload);

                    SetInfo(response);

                    return response;
                }
            }
        }
    }
}