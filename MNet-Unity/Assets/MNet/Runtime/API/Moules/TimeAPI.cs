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
            public static TimeValue Value { get; private set; }

            public static float Milliseconds => Value.Milliseconds;
            public static float Seconds => Value.Seconds;

            static DateTime timestamp;

            static double offset;

            static void Set(RoomTimeResponse response)
            {
                var tripTime = (DateTime.UtcNow - response.RequestTimestamp).TotalMilliseconds / 2;

                Set(response.Time.Value, tripTime);
            }
            static void Set(double value, double tripTime)
            {
                timestamp = DateTime.UtcNow;

                offset = value + tripTime;

                Calculate();
            }

            static void Calculate() => Value.CalculateFrom(timestamp, offset);

            public static void Configure()
            {

            }

            static void Update()
            {
                if (Client.IsReady) Calculate();
            }

            static void ClientReadyCallback(ReadyClientResponse response) => Set(response.Time);

            static void ClientMessageCallback(NetworkMessage message)
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
                Value.Clear();
            }

            static Time()
            {
                Value = new TimeValue();

                Client.OnReady += ClientReadyCallback;
                Client.OnMessage += ClientMessageCallback;
                Client.OnDisconnect += DisconnectCallback;

                OnUpdate += Update;
            }
        }
	}
}