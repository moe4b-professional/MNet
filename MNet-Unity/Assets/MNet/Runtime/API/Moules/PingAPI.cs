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
		public static class Ping
		{
            public static List<double> Samples { get; private set; }

            public static ushort MaxSamples { get; set; } = 20;

            public static double Average { get; private set; }

            public static double Min { get; private set; }
            public static double Max { get; private set; }

            static bool SendLock = false;

            public static string Text => $"Average: {Average.ToString("N")}ms | Min: {Min.ToString("N")}ms | Max: {Max.ToString("N")}ms";

            internal static void Configure()
            {
                Samples = new List<double>();

                Average = Min = Max = 0d;

                NetworkAPI.OnProcess += Process;

                Client.MessageDispatcher.RegisterHandler<PingResponse>(Register);
                Client.OnDisconnect += ClientDisconnectCallback;
            }

            static void Process()
            {
                if (Client.IsConnected == false) return;
                if (Client.IsRegistered == false) return;

                if (SendLock == false) Send();
            }

            public static event Action OnChange;
            static void InvokeChange() => OnChange?.Invoke();

            static void Send()
            {
                var request = PingRequest.Write();

                Client.Send(ref request, DeliveryMode.Reliable);

                SendLock = true;
            }

            static void Register(ref PingResponse response)
            {
                var span = response.GetTimeSpan();

                Samples.Add(span.TotalMilliseconds);

                if (Samples.Count > MaxSamples) Samples.RemoveRange(0, Samples.Count - MaxSamples);

                Average = Samples.Sum() / Samples.Count;

                Min = Samples.Min();
                Max = Samples.Max();

                SendLock = false;

                InvokeChange();
            }

            static void Clear()
            {
                Samples.Clear();

                Average = Min = Max = 0d;

                SendLock = false;

                InvokeChange();
            }

            static void ClientDisconnectCallback(DisconnectCode code) => Clear();
        }
    }
}