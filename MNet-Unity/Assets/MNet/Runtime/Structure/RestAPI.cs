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

using System.Net;

using UnityEngine.Networking;

using MB;

using Cysharp.Threading.Tasks;

namespace MNet
{
    [Preserve]
    public class RestClientAPI
    {
        public ushort Port { get; protected set; }

        public RestScheme Scheme { get; protected set; }

        public string IP { get; protected set; }
        public void SetIP(IPAddress address) => SetIP(address.ToString());
        public void SetIP(string address)
        {
            this.IP = address;
        }

        public string FormatURL(string path) => $"{Scheme}://{IP}:{Port}{path}";

        public delegate void ResponseDelegate<TResult>(TResult result, RestError error);

        public async UniTask<TResult> POST<[NetworkSerializationGenerator] TPayload, [NetworkSerializationGenerator] TResult>(string path, TPayload payload)
        {
            var url = FormatURL(path);

            var binary = NetworkSerializer.Serialize(payload);

            var upload = binary.Length == 0 ? null : new UploadHandlerRaw(binary);
            var download = new DownloadHandlerBuffer();

            var request = new UnityWebRequest(url, "POST", download, upload);

            await request.SendWebRequest();

            var result = Read<TResult>(request);

            request.Dispose();

            return result;
        }

        public RestClientAPI(ushort port, RestScheme scheme)
        {
            this.Port = port;
            this.Scheme = scheme;
        }

        //Static Utility

        public static TResult Read<TResult>(UnityWebRequest request) => NetworkSerializer.Deserialize<TResult>(request.downloadHandler.data);
    }
}