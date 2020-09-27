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

namespace Backend
{
    public class RestError
    {
        public long Code { get; protected set; }

        public string Message { get; protected set; }

        public RestError(long code, string message)
        {
            this.Code = code;
            this.Message = message;
        }

        public RestError(UnityWebRequest request) : this(request.responseCode, request.error) { }

        public override string ToString() => $"REST Error: {Message}";
    }

    [Flags]
    public enum RemoteAutority : byte
    {
        /// <summary>
        /// As the name implies, any client will be able to the remote action
        /// </summary>
        Any = 1 << 0,

        /// <summary>
        /// Only the owner of this entity may invoke this this remote action
        /// </summary>
        Owner = 1 << 1,

        /// <summary>
        /// Only the master client may invoke this remote action
        /// </summary>
        Master = 1 << 2,
    }
}