using System;

using System.Collections;
using System.Collections.Generic;

using System.Diagnostics;

using MNet;

namespace Sandbox
{
    class Program
    {
        static void Main()
        {
            Procedure();

            while (true) Console.ReadKey();
        }

        static void Procedure()
        {
            DeliverySegments();
        }

        static void DeliverySegments()
        {
            var delivery = new MessageSendQueue.Delivery(DeliveryMode.Unreliable);

            for (byte i = 1; i <= 20; i++)
            {
                var payload = RpcRequest.Write(new NetworkEntityID(i), new NetworkBehaviourID(i), new RpxMethodID("Method"), RpcBufferMode.All, new byte[50]);

                var message = NetworkMessage.Write(payload);

                delivery.Add(message);
            }

            Log.Info("Delivery Size: " + delivery.Count);

            int counter = 1;

            foreach (var segment in delivery.Serialize(500))
            {
                Log.Info(segment.Length);

                var messages = NetworkMessage.ReadAll(segment);

                foreach (var instance in messages)
                {
                    var payload = instance.Read<RpcRequest>();

                    Log.Info(payload.Entity + " " + counter); ;

                    counter += 1;
                }
            }
        }

        static void Measure(Action action)
        {
            var watch = Stopwatch.StartNew();

            action();

            watch.Stop();

            Console.WriteLine($"{action.Method.Name} Took: {watch.ElapsedTicks} Ticks, {watch.ElapsedMilliseconds} ms");
        }
    }
}