using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;
using System.Collections.Concurrent;

using System.Net;
using System.Net.Sockets;

using System.Reflection;

namespace MNet
{
    public static class GeneralExtensions
    {
        public static bool TryRemove<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key)
        {
            return dictionary.TryRemove(key, out _);
        }

        public static void DisableNagleAlgorithm(this WebSocketSharp.WebSocket socket)
        {
            var binding = BindingFlags.Instance | BindingFlags.NonPublic;

            var client = socket.GetType().GetField("_tcpClient", binding).GetValue(socket) as TcpClient;

            client.NoDelay = true;
        }

        public static void DisableNagleAlgorithm(this WebSocketSharp.Server.WebSocketServer socket)
        {
            var binding = BindingFlags.Instance | BindingFlags.NonPublic;

            var listener = socket.GetType().GetField("_listener", binding).GetValue(socket) as TcpListener;

            listener.Server.NoDelay = true;
        }
    }
}