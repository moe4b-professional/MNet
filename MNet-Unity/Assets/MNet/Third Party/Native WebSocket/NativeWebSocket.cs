#if UNITY_WEBGL && !UNITY_EDITOR
using System;
using System.Collections.Generic;

using AOT;
using System.Runtime.InteropServices;

using UnityEngine.Scripting;

namespace NativeWebSocket
{
    /// <summary>
    /// WebSocket class bound to JSLIB.
    /// </summary>
    [Preserve]
    public class WebSocket
    {
        public int InstanceId { get; protected set; }

        public WebSocketState State
        {
            get
            {
                int state = WebSocketGetState(InstanceId);

                if (state < 0) throw WebSocketHelpers.GetErrorMessageFromCode(state, null);

                switch (state)
                {
                    case 0:
                        return WebSocketState.Connecting;

                    case 1:
                        return WebSocketState.Open;

                    case 2:
                        return WebSocketState.Closing;

                    case 3:
                        return WebSocketState.Closed;

                    default:
                        return WebSocketState.Closed;
                }
            }
        }

        public event WebSocketOpenEventHandler OnOpen;
        public void DelegateOnOpenEvent() => OnOpen?.Invoke();

        public event WebSocketMessageEventHandler OnMessage;
        public void DelegateOnMessageEvent(byte[] data) => OnMessage?.Invoke(data);

        public event WebSocketErrorEventHandler OnError;
        public void DelegateOnErrorEvent(string errorMsg) => OnError?.Invoke(errorMsg);

        public event WebSocketCloseEventHandler OnClose;
        public void DelegateOnCloseEvent(int closeCode) => OnClose?.Invoke(closeCode);

        public void Connect()
        {
            int ret = WebSocketConnect(this.InstanceId);

            if (ret < 0)
                throw WebSocketHelpers.GetErrorMessageFromCode(ret, null);
        }

        public void CancelConnection()
        {
            if (State == WebSocketState.Open)
                Close(1006);
        }

        public void Close(int code = 1000, string reason = null)
        {
            int ret = WebSocketClose(this.InstanceId, (int)code, reason);

            if (ret < 0)
                throw WebSocketHelpers.GetErrorMessageFromCode(ret, null);
        }

        public void Send(byte[] data)
        {
            int ret = WebSocketSend(this.InstanceId, data, data.Length);

            if (ret < 0)
                throw WebSocketHelpers.GetErrorMessageFromCode(ret, null);
        }

        public void SendText(string message)
        {
            int ret = WebSocketSendText(this.InstanceId, message);

            if (ret < 0)
                throw WebSocketHelpers.GetErrorMessageFromCode(ret, null);
        }

        public WebSocket(string url)
        {
            InstanceId = WebSocketFactory.WebSocketAllocate(url);

            WebSocketFactory.instances.Add(InstanceId, this);
        }
        public WebSocket(string url, string subprotocol) : this(url)
        {
            WebSocketFactory.WebSocketAddSubProtocol(InstanceId, subprotocol);
        }
        public WebSocket(string url, List<string> subprotocols) : this(url)
        {
            foreach (string subprotocol in subprotocols)
                WebSocketFactory.WebSocketAddSubProtocol(InstanceId, subprotocol);
        }

        ~WebSocket()
        {
            WebSocketFactory.HandleInstanceDestroy(this.InstanceId);
        }

        /* WebSocket JSLIB functions */
        [DllImport("__Internal")]
        public static extern int WebSocketConnect(int instanceId);

        [DllImport("__Internal")]
        public static extern int WebSocketClose(int instanceId, int code, string reason);

        [DllImport("__Internal")]
        public static extern int WebSocketSend(int instanceId, byte[] dataPtr, int dataLength);

        [DllImport("__Internal")]
        public static extern int WebSocketSendText(int instanceId, string message);

        [DllImport("__Internal")]
        public static extern int WebSocketGetState(int instanceId);
    }

    [Preserve]
    public delegate void WebSocketOpenEventHandler();
    [Preserve]
    public delegate void WebSocketMessageEventHandler(byte[] data);
    [Preserve]
    public delegate void WebSocketErrorEventHandler(string message);
    [Preserve]
    public delegate void WebSocketCloseEventHandler(int closeCode);

    [Preserve]
    public enum WebSocketState
    {
        Connecting,
        Open,
        Closing,
        Closed
    }

    [Preserve]
    public static class WebSocketHelpers
    {
        public static WebSocketException GetErrorMessageFromCode(int code, Exception inner)
        {
            switch (code)
            {
                case -1:
                    return new WebSocketUnexpectedException("WebSocket instance not found.", inner);
                case -2:
                    return new WebSocketInvalidStateException("WebSocket is already connected or in connecting state.", inner);
                case -3:
                    return new WebSocketInvalidStateException("WebSocket is not connected.", inner);
                case -4:
                    return new WebSocketInvalidStateException("WebSocket is already closing.", inner);
                case -5:
                    return new WebSocketInvalidStateException("WebSocket is already closed.", inner);
                case -6:
                    return new WebSocketInvalidStateException("WebSocket is not in open state.", inner);
                case -7:
                    return new WebSocketInvalidArgumentException("Cannot close WebSocket. An invalid code was specified or reason is too long.", inner);
                default:
                    return new WebSocketUnexpectedException("Unknown error.", inner);
            }
        }
    }

    /// <summary>
    /// Class providing static access methods to work with JSLIB WebSocket or WebSocketSharp interface
    /// </summary>
    [Preserve]
    public static class WebSocketFactory
    {
        /* Map of websocket instances */
        public static Dictionary<Int32, WebSocket> instances = new Dictionary<Int32, WebSocket>();

        /* Delegates */
        public delegate void OnOpenCallback(int instanceId);
        public delegate void OnMessageCallback(int instanceId, IntPtr msgPtr, int msgSize);
        public delegate void OnErrorCallback(int instanceId, IntPtr errorPtr);
        public delegate void OnCloseCallback(int instanceId, int closeCode);

        /* WebSocket JSLIB callback setters and other functions */
        [DllImport("__Internal")]
        public static extern int WebSocketAllocate(string url);

        [DllImport("__Internal")]
        public static extern int WebSocketAddSubProtocol(int instanceId, string subprotocol);

        [DllImport("__Internal")]
        public static extern void WebSocketFree(int instanceId);

        [DllImport("__Internal")]
        public static extern void WebSocketSetOnOpen(OnOpenCallback callback);

        [DllImport("__Internal")]
        public static extern void WebSocketSetOnMessage(OnMessageCallback callback);

        [DllImport("__Internal")]
        public static extern void WebSocketSetOnError(OnErrorCallback callback);

        [DllImport("__Internal")]
        public static extern void WebSocketSetOnClose(OnCloseCallback callback);

        /// <summary>
        /// Called when instance is destroyed (by destructor)
        /// Method removes instance from map and free it in JSLIB implementation
        /// </summary>
        /// <param name="instanceId">Instance identifier.</param>
        public static void HandleInstanceDestroy(int instanceId)
        {
            instances.Remove(instanceId);
            WebSocketFree(instanceId);
        }

        [MonoPInvokeCallback(typeof(OnOpenCallback))]
        public static void DelegateOnOpenEvent(int instanceId)
        {
            WebSocket instanceRef;

            if (instances.TryGetValue(instanceId, out instanceRef))
            {
                instanceRef.DelegateOnOpenEvent();
            }
        }

        [MonoPInvokeCallback(typeof(OnMessageCallback))]
        public static void DelegateOnMessageEvent(int instanceId, IntPtr msgPtr, int msgSize)
        {
            WebSocket instanceRef;

            if (instances.TryGetValue(instanceId, out instanceRef))
            {
                byte[] msg = new byte[msgSize];
                Marshal.Copy(msgPtr, msg, 0, msgSize);

                instanceRef.DelegateOnMessageEvent(msg);
            }
        }

        [MonoPInvokeCallback(typeof(OnErrorCallback))]
        public static void DelegateOnErrorEvent(int instanceId, IntPtr errorPtr)
        {
            WebSocket instanceRef;

            if (instances.TryGetValue(instanceId, out instanceRef))
            {

                string errorMsg = Marshal.PtrToStringAuto(errorPtr);
                instanceRef.DelegateOnErrorEvent(errorMsg);

            }
        }

        [MonoPInvokeCallback(typeof(OnCloseCallback))]
        public static void DelegateOnCloseEvent(int instanceId, int closeCode)
        {
            WebSocket instanceRef;

            if (instances.TryGetValue(instanceId, out instanceRef))
            {
                instanceRef.DelegateOnCloseEvent(closeCode);
            }
        }

        static WebSocketFactory()
        {
            WebSocketSetOnOpen(DelegateOnOpenEvent);
            WebSocketSetOnMessage(DelegateOnMessageEvent);
            WebSocketSetOnError(DelegateOnErrorEvent);
            WebSocketSetOnClose(DelegateOnCloseEvent);
        }
    }

#region Exceptions
    [Preserve]
    public class WebSocketException : Exception
    {
        public WebSocketException() { }
        public WebSocketException(string message) : base(message) { }
        public WebSocketException(string message, Exception inner) : base(message, inner) { }
    }

    [Preserve]
    public class WebSocketUnexpectedException : WebSocketException
    {
        public WebSocketUnexpectedException() { }
        public WebSocketUnexpectedException(string message) : base(message) { }
        public WebSocketUnexpectedException(string message, Exception inner) : base(message, inner) { }
    }

    [Preserve]
    public class WebSocketInvalidArgumentException : WebSocketException
    {
        public WebSocketInvalidArgumentException() { }
        public WebSocketInvalidArgumentException(string message) : base(message) { }
        public WebSocketInvalidArgumentException(string message, Exception inner) : base(message, inner) { }
    }

    [Preserve]
    public class WebSocketInvalidStateException : WebSocketException
    {
        public WebSocketInvalidStateException() { }
        public WebSocketInvalidStateException(string message) : base(message) { }
        public WebSocketInvalidStateException(string message, Exception inner) : base(message, inner) { }
    }
#endregion
}
#endif