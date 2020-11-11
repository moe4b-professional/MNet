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

            /// <summary>
            /// UTC Timestamp in client time of when the client last recieved a RoomTime message
            /// </summary>
            static DateTime stamp;

            /// <summary>
            /// Room's time offset in ticks when timestamp was set + half of round trip time
            /// </summary>
            static long offset;

            static void Set(RoomTimeResponse response) => Set(response.Time, DateTime.UtcNow - response.RequestTimestamp);
            static void Set(NetworkTimeSpan value, TimeSpan rtt)
            {
                stamp = DateTime.UtcNow;

                offset = value.Ticks + (rtt.Ticks / 2);

                Calculate();
            }

            static void Calculate() => Span = NetworkTimeSpan.Calculate(stamp, offset);

            public static void Configure()
            {

            }

            static void Update()
            {
                if (Client.IsReady) Calculate();
            }

            static void ClientReadyCallback(ReadyClientResponse response) => Set(response.Time);

            static void ClientMessageCallback(NetworkMessage message, DeliveryMode mode)
            {
                if(message.Is<RoomTimeResponse>())
                {
                    var response = message.Read<RoomTimeResponse>();

                    Set(response);
                }
            }

            static void DisconnectCallback(DisconnectCode code) => Clear();

            static void Clear()
            {
                Span = default;
            }

            static Time()
            {
                Span = default;

                Client.OnReady += ClientReadyCallback;
                Client.OnMessage += ClientMessageCallback;
                Client.OnDisconnect += DisconnectCallback;

                OnUpdate += Update;
            }
        }
	}
}