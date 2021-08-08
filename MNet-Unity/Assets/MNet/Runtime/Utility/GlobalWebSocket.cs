#if UNITY_WEBGL && !UNITY_EDITOR
#define NATIVE
#endif

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

#if NATIVE
	using NativeWebSocket;
#else
using WebSocketSharp;
using ErrorEventArgs = WebSocketSharp.ErrorEventArgs;
#endif

namespace MNet
{
	[Preserve]
	public class GlobalWebSocket
	{
#if NATIVE
		WebSocket socket;
#else
		WebSocket socket;
#endif

		public bool IsConnected
		{
			get
			{
#if NATIVE
				return socket.State == WebSocketState.Open;
#else
				return socket.ReadyState == WebSocketState.Open;
#endif
			}
		}

		public delegate void ConnectDelegate();
		public event ConnectDelegate OnConnect;
		void InvokeConnect() => OnConnect?.Invoke();

		public delegate void MessageDelegate(byte[] raw);
		public event MessageDelegate OnMessage;
		void InvokeMessage(byte[] raw) => OnMessage?.Invoke(raw);

		public delegate void ErrorDelegate(string message);
		public event ErrorDelegate OnError;
		void InvokeError(string message) => OnError?.Invoke(message);

		public delegate void DisconnectDelegate(DisconnectCode code, string reason);
		public event DisconnectDelegate OnDisconnect;
		void InvokeDisconnect(DisconnectCode code, string reason) => OnDisconnect?.Invoke(code, reason);

		public void Connect()
		{
#if NATIVE
			socket.Connect();
#else
			socket.Connect();
#endif
		}

		public void Send(byte[] raw)
		{
#if NATIVE
			socket.Send(raw);
#else
			socket.Send(raw);
#endif
		}

		public void Disconnect(DisconnectCode code = DisconnectCode.Normal, string reason = default)
		{
			var value = NetworkTransportUtility.WebSocket.Disconnect.CodeToValue(code);

#if NATIVE
			socket.Close(value, reason);
#else
			socket.Close(value, reason);
#endif
		}

		public GlobalWebSocket(string url)
		{
#if NATIVE
			socket = new WebSocket(url);

			socket.OnOpen += OpenCallback;
			void OpenCallback() => InvokeConnect();

			socket.OnMessage += MessageCallback;
			void MessageCallback(byte[] data) => InvokeMessage(data);

			socket.OnError += ErrorCallback;
			void ErrorCallback(string message) => InvokeError(message);

			socket.OnClose += CloseCallback;
			void CloseCallback(int code)
			{
				var value = (ushort)code;

				var status = NetworkTransportUtility.WebSocket.Disconnect.ValueToCode(value);

				InvokeDisconnect(status, status.ToPrettyString());
			}
#else
			socket = new WebSocket(url);

			socket.OnOpen += OpenCallback;
			void OpenCallback(object sender, EventArgs args) => InvokeConnect();

			socket.OnMessage += MessageCallback;
			void MessageCallback(object sender, MessageEventArgs args) => InvokeMessage(args.RawData);

			socket.OnError += ErrorCallback;
			void ErrorCallback(object sender, ErrorEventArgs args) => InvokeError(args.Message);

            socket.OnClose += CloseCallback;
			void CloseCallback(object sender, CloseEventArgs args)
			{
				var code = NetworkTransportUtility.WebSocket.Disconnect.ValueToCode(args.Code);

				InvokeDisconnect(code, args.Reason);
			}
#endif
		}
	}
}