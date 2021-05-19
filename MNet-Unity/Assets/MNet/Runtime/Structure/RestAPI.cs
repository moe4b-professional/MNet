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

        public void POST<TPayload, TResult>(string path, TPayload payload, ResponseDelegate<TResult> callback)
        {
            var url = FormatURL(path);

            var binary = NetworkSerializer.Serialize(payload);

            var upload = binary.Length == 0 ? null : new UploadHandlerRaw(binary);
            var download = new DownloadHandlerBuffer();

            var request = new UnityWebRequest(url, "POST", download, upload);

            var coroutine = Process(request, callback);

            GlobalCoroutine.Start(coroutine);
        }

        IEnumerator Process<TResult>(UnityWebRequest request, ResponseDelegate<TResult> callback)
        {
            yield return request.SendWebRequest();

            ReadResult(request, out TResult result, out var error);

            if (error != null) Debug.LogError(error);

            request.Dispose();

            callback(result, error);
        }

        public RestClientAPI(ushort port, RestScheme scheme)
        {
            this.Port = port;
            this.Scheme = scheme;
        }

        //Static Utility

        public static void ReadResult<TResult>(UnityWebRequest request, out TResult payload, out RestError error)
        {
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.DataProcessingError)
            {
                var code = (RestStatusCode)request.responseCode;

                error = new RestError(code, request.error);
                payload = default;
            }
            else
            {
                error = null;
                payload = NetworkSerializer.Deserialize<TResult>(request.downloadHandler.data);
            }
        }
    }
}