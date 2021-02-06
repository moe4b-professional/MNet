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

using System.Reflection;

using Cysharp.Threading.Tasks;

namespace MNet
{
    public class RprPromise
    {
        public NetworkClient Target { get; protected set; }
        public RprChannelID Channel { get; protected set; }

        public bool Complete { get; protected set; }
        public bool IsComplete() => Complete;

        public RemoteResponseType Response { get; protected set; }

        public byte[] Raw { get; protected set; }
        internal T Read<T>() => Response == RemoteResponseType.Success ? NetworkSerializer.Deserialize<T>(Raw) : default;

        internal void Fullfil(RemoteResponseType response, byte[] raw)
        {
            Complete = true;

            this.Response = response;
            this.Raw = raw;
        }

        public RprPromise(NetworkClient target, RprChannelID channel)
        {
            this.Target = target;
            this.Channel = channel;

            Complete = false;
        }
    }

    public struct RprAnswer<T>
    {
        public RemoteResponseType Response { get; private set; }

        public bool Success => Response == RemoteResponseType.Success;
        public bool Fail => Response != RemoteResponseType.Success;

        public T Value { get; private set; }

        internal RprAnswer(RemoteResponseType response, T value)
        {
            this.Response = response;
            this.Value = value;
        }
        internal RprAnswer(RemoteResponseType response) : this(response, default) { }
        internal RprAnswer(RprPromise promise) : this(promise.Response, promise.Read<T>()) { }
    }
}