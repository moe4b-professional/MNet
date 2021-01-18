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

using Cysharp.Threading.Tasks;

namespace MNet
{
	public static partial class NetworkAPI
	{
		public static class Ping
		{
            public static List<double> Samples { get; private set; }

            /// <summary>
            /// Maximum Number of Samples to Keep for Calculating Min and Max
            /// </summary>
            public static ushort MaxSamples { get; set; } = 10;

            public static double Average { get; private set; }

            public static double Min { get; private set; }
            public static double Max { get; private set; }

            /// <summary>
            /// Polling Interval In Milliseconds,
            /// 100 for Editor And 1,000 for Builds
            /// </summary>
            public static int PollInterval { get; private set; }

            public static string Text => $"Average: {Average.ToString("N")}ms | Min: {Min.ToString("N")}ms | Max: {Max.ToString("N")}ms";

            internal static void Configure()
            {
                Samples = new List<double>();

                Average = Min = Max = 0d;

                PollInterval = Application.isEditor ? 100 : 1000;

                Poll().Forget();

                Client.MessageDispatcher.RegisterHandler<PingResponse>(Calculate);
                Client.OnDisconnect += ClientDisconnectCallback;
            }

            static async UniTask Poll()
            {
                while (true)
                {
                    if (Client.IsConnected) Request();

                    await UniTask.Delay(PollInterval, ignoreTimeScale: true);
                }
            }

            public static event Action OnChange;
            static void InvokeChange() => OnChange?.Invoke();

            static void Request()
            {
                var request = PingRequest.Write();

                Client.Send(ref request, DeliveryMode.Reliable);
            }

            static void Calculate(ref PingResponse response)
            {
                var span = response.GetTimeSpan();

                Samples.Add(span.TotalMilliseconds);

                if (Samples.Count > MaxSamples) Samples.RemoveRange(0, Samples.Count - MaxSamples);

                Average = Samples.Sum() / Samples.Count;

                Min = Samples.Min();
                Max = Samples.Max();

                InvokeChange();
            }

            static void ClientDisconnectCallback(DisconnectCode code) => Clear();

            static void Clear()
            {
                Samples.Clear();

                Average = Min = Max = 0d;

                InvokeChange();
            }
        }
    }
}