﻿using System;
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

    public enum EntityAuthorityType : byte
    {
        /// <summary>
        /// As the name implies, any client will be able to invoke this RPC
        /// </summary>
        Any,

        /// <summary>
        /// Only the owner of this entity may invoke this RPC
        /// </summary>
        Owner,

        /// <summary>
        /// Only the master client may invoke this RPC
        /// </summary>
        Master,
    }
}