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
        public static class Time
        {
            public static NetworkTimeSpan Span { get; private set; }

            public static float Milliseconds => Span.Millisecond;
            public static float Seconds => Span.Seconds;

            public static NetworkTimeSpan Delta { get; private set; }

            #region Delta & Break
            public static bool IsBroken { get; private set; } = false;

            const float DeltaBreakingThreshold = 4.0f;

            static bool ValidateDelta(NetworkTimeSpan span) => span.Ticks >= 0 && Delta.Seconds < DeltaBreakingThreshold;

            static void Break()
            {
                Debug.LogWarning($"Network Time Broken, Delta: {Delta.Seconds}, Requesting New");

                IsBroken = true;

                Delta = default;

                Request();
            }

            static void UnBreak()
            {
                IsBroken = false;
            }
            #endregion

            // UTC Timestamp in client time of when the client last recieved a RoomTime message
            static DateTime stamp;

            // Room's time offset in ticks when timestamp was set + half of round trip time
            static long offset;

            public static void Configure()
            {
                Span = default;

                NetworkAPI.OnProcess += Process;

                Client.OnReady += ClientReadyCallback;
                Client.OnDisconnect += DisconnectCallback;

                Client.RegisterMessageHandler<RoomTimeResponse>(Set);
            }

            static void Process()
            {
                if (Client.IsReady == false) return;

                Calculate();
            }

            public static void Request()
            {
                var payload = RoomTimeRequest.Write();

                Client.Send(payload, DeliveryMode.Reliable);
            }

            static void Set(RoomTimeResponse response)
            {
                var rtt = DateTime.UtcNow - response.RequestTimestamp;

                Set(response.Time, rtt);
            }
            static void Set(NetworkTimeSpan value, TimeSpan rtt)
            {
                stamp = DateTime.UtcNow;
                offset = value.Ticks + (rtt.Ticks / 2);

                Span = NetworkTimeSpan.Calculate(stamp, offset);
                Delta = default;

                if (IsBroken) UnBreak();
            }

            static void Calculate()
            {
                if (IsBroken) return;

                var value = NetworkTimeSpan.Calculate(stamp, offset);

                Delta = new NetworkTimeSpan(value.Ticks - Span.Ticks);

                if (ValidateDelta(Delta) == false)
                {
                    Break();
                    return;
                }

                Span = value;
            }

            static void Clear()
            {
                Span = default;
                Delta = default;

                IsBroken = false;
            }

            #region Callbacks
            static void ClientReadyCallback(ReadyClientResponse response) => Set(response.Time);

            static void DisconnectCallback(DisconnectCode code) => Clear();
            #endregion
        }
    }
}