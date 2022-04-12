using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace MNet
{
    public class WebSocketServer
    {
        public ConcurrentDictionary<int, WebSocketClient> Clients { get; private set; }

        public bool IsRunning { get; private set; }

        public int Timeout = 10 * 1000;
        public int PingInterval = 4 * 1000;

        public int PollingInterval = 5;

        public bool NoDelay = false;

        Stopwatch Stopwatch;
        public long GetTime() => Stopwatch.ElapsedMilliseconds;

        TcpListener listener;

        #region Connect
        public event ConnectDelegate OnConnect;
        public delegate void ConnectDelegate(WebSocketClient client);

        internal void InvokeConnect(WebSocketClient client)
        {
            OnConnect?.Invoke(client);
        }
        #endregion

        #region Message
        public event MessageDelegate OnMessage;
        public delegate void MessageDelegate(WebSocketClient client, WebSocketPacket packet);

        internal void InvokeMessage(WebSocketClient client, WebSocketPacket packet)
        {
            OnMessage?.Invoke(client, packet);
        }
        #endregion

        #region Disconnect
        public event DisconnectDelegate OnDisconnect;
        public delegate void DisconnectDelegate(WebSocketClient client, WebSocketCloseCode code, string message);

        internal void InvokeDisconnect(WebSocketClient client, WebSocketCloseCode code, string message)
        {
            Clients.Remove(client.ID, out _);

            OnDisconnect?.Invoke(client, code, message);
        }
        #endregion

        int IDIndex;

        public void Start()
        {
            if (IsRunning)
                throw new InvalidOperationException("Server Already Running");

            IsRunning = true;

            listener.Start();
            Stopwatch.Start();

            new Thread(Listen).Start();
        }

        void Listen()
        {
            while (true)
            {
                if (PollIncomingRequest() == false)
                    break;

                var socket = listener.AcceptSocket();

                ThreadPool.QueueUserWorkItem(ProcessIncomingSocket, socket, false);
            }

            Close();
        }

        bool PollIncomingRequest()
        {
            while(true)
            {
                if (IsRunning == false)
                    return false;

                if (listener.Pending())
                    return true;

                Thread.Sleep(PollingInterval);
            }
        }
        void ProcessIncomingSocket(Socket socket)
        {
            try
            {
                if (WebSocketHandshake.TryPerform(socket, out var url) == false)
                    return;

                Log.Info("WebSocket Handshake Complete");

                Register(socket, url);
            }
            catch (Exception)
            {
                socket.Close();
            }
        }

        WebSocketClient Register(Socket socket, string url)
        {
            socket.NoDelay = NoDelay;

            var ID = Interlocked.Increment(ref IDIndex);

            var client = new WebSocketClient(this, socket, ID, url);

            if (IsRunning)
                Clients.TryAdd(ID, client);
            else
                client.Disconnect(WebSocketCloseCode.EndpointUnavailable);

            return client;
        }

        public void Stop(WebSocketCloseCode code) => Stop(code, string.Empty);
        public void Stop(WebSocketCloseCode code, string message)
        {
            if (IsRunning == false)
                throw new InvalidOperationException("Server not Running");

            IsRunning = false;

            var clients = Clients.Values.ToArray();

            for (int i = 0; i < clients.Length; i++)
                clients[i].Disconnect(code, message);

            Stopwatch.Reset();
        }

        public void Close()
        {
            listener.Stop();
        }

        public WebSocketServer(IPAddress address, int port)
        {
            listener = new TcpListener(address, port);

            Stopwatch = new Stopwatch();

            Clients = new();
        }
    }

    public class WebSocketClient
    {
        readonly WebSocketServer Server;
        readonly Socket Socket;

        public int ID { get; }
        public string URL { get; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        volatile WebSocketState state;
        public WebSocketState State => state;

        public object Tag;

        long LastPingSendTime;
        long LastPongReceiveTime;

        public const int MaxThreadStackSize = 16 * 1024;

        internal void Start()
        {
            //Time
            {
                var time = Server.GetTime();

                LastPingSendTime = time;
                LastPongReceiveTime = time;
            }

            Server.InvokeConnect(this);

            RunningThreads = 2;

            ReceiveThread = new Thread(ReceiveLoop, MaxThreadStackSize);
            ReceiveThread.IsBackground = true;
            ReceiveThread.Name = $"Client {ID} Receive Loop";
            ReceiveThread.Start();

            SendThread = new Thread(SendLoop, MaxThreadStackSize);
            SendThread.IsBackground = true;
            SendThread.Name = $"Client {ID} Send Loop";
            SendThread.Start();
        }

        #region Recieve
        Thread ReceiveThread;

        void ReceiveLoop()
        {
            var opCode = WebSocketOPCode.Binary;

            var packet = WebSocketPacket.Lease();

            while (true)
            {
                if (ReceivePoll() == false)
                    break;

                if (WebSocketFrame.TryReadHeader(Socket, out var header) == false)
                {
                    Disconnect(WebSocketCloseCode.ProtocolError);
                    break;
                }
                if (header.Mask.HasValue == false)
                {
                    Disconnect(WebSocketCloseCode.ProtocolError);
                    break;
                }

                if (WebSocketFrame.IsControlOpCode(header.OpCode))
                {
                    if (HandleControlFrame(header) == false)
                        break;
                }
                else
                {
                    if (HandlePayloadFrame(header, ref opCode, ref packet) == false)
                        break;
                }
            }

            packet.Recycle();

            CloseThread();
        }

        bool ReceivePoll()
        {
            while (true)
            {
                if (State != WebSocketState.Open)
                    return false;

                try
                {
                    if (Socket.Available > 0)
                        return true;
                }
                catch (Exception ex)
                {
                    if (WebSocketExtensions.IsRemoteDisconnectException(ex))
                        return false;

                    throw;
                }

                Thread.Sleep(Server.PollingInterval);
            }
        }

        bool HandlePayloadFrame(WebSocketHeader header, ref WebSocketOPCode opCode, ref WebSocketPacket packet)
        {
            if (header.OpCode == WebSocketOPCode.Text)
            {
                Disconnect(WebSocketCloseCode.InvalidMessageType);
                return false;
            }

            if (WebSocketFrame.TryReadPayload(Socket, header, ref packet) == false)
            {
                Disconnect(WebSocketCloseCode.ProtocolError);
                return false;
            }

            if (header.OpCode != WebSocketOPCode.Continuation)
                opCode = header.OpCode;

            if (header.Final)
            {
                switch (opCode)
                {
                    case WebSocketOPCode.Binary:
                        HandleBinaryPacket(packet);
                        break;

                    default:
                        throw new InvalidOperationException();
                }

                packet = WebSocketPacket.Lease();
            }

            return true;
        }
        void HandleBinaryPacket(WebSocketPacket packet)
        {
            Server.InvokeMessage(this, packet);
        }

        bool HandleControlFrame(WebSocketHeader header)
        {
            if (header.PayloadLength > 125)
            {
                Disconnect(WebSocketCloseCode.ProtocolError);
                return false;
            }

            Span<byte> span = stackalloc byte[header.PayloadLength];

            if (WebSocketFrame.TryReadPayload(Socket, header, ref span) == false)
            {
                Disconnect(WebSocketCloseCode.ProtocolError);
                return false;
            }

            switch (header.OpCode)
            {
                case WebSocketOPCode.Close:
                    return HandleCloseFrame(span);

                case WebSocketOPCode.Ping:
                    return HandlePingFrame(span);

                case WebSocketOPCode.Pong:
                    return HandlePongFrame(span);

                default:
                    throw new InvalidOperationException();
            }
        }
        bool HandleCloseFrame(Span<byte> span)
        {
            WebSocketFrame.ReadCloseFrameData(span, out var code, out var message);

            Disconnect(code, message);
            return false;
        }
        bool HandlePingFrame(Span<byte> span)
        {
            Send(WebSocketOPCode.Pong, span);
            return true;
        }
        bool HandlePongFrame(Span<byte> span)
        {
            LastPongReceiveTime = Server.GetTime();

            return true;
        }
        #endregion

        #region Send Loop
        Thread SendThread;

        readonly ConcurrentQueue<SendPacket> PayloadSendQueue;
        readonly ConcurrentQueue<SendPacket> ControlSendQueue;
        readonly record struct SendPacket(WebSocketOPCode opCode, WebSocketPacket packet);

        DisconnectPacketData DisconnectPacket;
        readonly record struct DisconnectPacketData(WebSocketCloseCode code, string message);

        void SendLoop()
        {
            while (true)
            {
                if (state != WebSocketState.Open) break;

                if (ProcessKeepAlive() == false)
                    break;

                if (SendQueuesImmediate(ControlSendQueue, PayloadSendQueue) == false)
                    break;

                Thread.Sleep(Server.PollingInterval);
            }

            ClearCommandQueue(ControlSendQueue);
            ClearCommandQueue(PayloadSendQueue);

            if (state == WebSocketState.Open)
                Stop(WebSocketCloseCode.Abnormal);
            else
                ValidateSendLoopClosure();

            CloseThread();
        }

        bool ProcessKeepAlive()
        {
            var time = Server.GetTime();

            //Pong
            {
                var timespan = time - LastPongReceiveTime;

                if (timespan > Server.Timeout)
                {
                    Disconnect(WebSocketCloseCode.Abnormal);
                    return false;
                }
            }

            //Ping
            {
                var timespan = time - LastPingSendTime;

                if (timespan > Server.PingInterval)
                {
                    LastPingSendTime = time;

                    if (SendImmediate(WebSocketOPCode.Ping, default) == false)
                        return false;
                }
            }

            return true;
        }

        bool SendQueuesImmediate(ConcurrentQueue<SendPacket> control, ConcurrentQueue<SendPacket> payload)
        {
            if (SendQueueImmediate(control) == false)
                return false;

            while (PayloadSendQueue.TryDequeue(out var command))
            {
                if (SendQueueImmediate(control) == false)
                    return false;

                command.Deconstruct(out var opCode, out var packet);

                var span = packet.AsSpan();

                try
                {
                    if (SendImmediate(opCode, span) == false)
                        return false;
                }
                finally
                {
                    packet.Recycle();
                }
            }

            return true;
        }
        bool SendQueueImmediate(ConcurrentQueue<SendPacket> queue)
        {
            while (queue.TryDequeue(out var command))
            {
                command.Deconstruct(out var opCode, out var packet);

                var payload = packet.AsSpan();

                try
                {
                    if (SendImmediate(opCode, payload) == false)
                        return false;
                }
                finally
                {
                    packet.Recycle();
                }
            }

            return true;
        }

        bool SendImmediate(WebSocketOPCode opCode, Span<byte> span)
        {
            if (state != WebSocketState.Open && opCode != WebSocketOPCode.Close)
                return false;

            try
            {
                //Header
                {
                    var header = new WebSocketHeader(true, opCode, span.Length, default);

                    Span<byte> buffer = stackalloc byte[WebSocketFrame.MaxServerHeaderSize];
                    var raw = WebSocketFrame.WriteHeader(header, ref buffer);

                    Socket.Send(raw);
                }

                Socket.Send(span);
            }
            catch (Exception ex)
            {
                if (WebSocketExtensions.IsRemoteDisconnectException(ex))
                    return false;

                throw;
            }

            return true;
        }

        void ValidateSendLoopClosure()
        {
            if (state != WebSocketState.Closing)
                return;

            DisconnectPacket.Deconstruct(out var code, out var message);

            var packet = WebSocketPacket.Lease();
            WebSocketFrame.WriteCloseFrame(packet, code, message);
            var span = packet.AsSpan();

            if (SendImmediate(WebSocketOPCode.Close, span) == false)
            {
                code = WebSocketCloseCode.Abnormal;
                message = string.Empty;
            }

            Stop(code, message);
            packet.Recycle();
        }

        void ClearCommandQueue(ConcurrentQueue<SendPacket> queue)
        {
            while (queue.TryDequeue(out var command))
                command.packet.Recycle();
        }
        #endregion

        #region Send Commands
        public bool SendBinary(Span<byte> span) => Send(WebSocketOPCode.Binary, span);

        internal bool Send(WebSocketOPCode opCode, Span<byte> span)
        {
            if (state != WebSocketState.Open)
                return false;

            var packet = WebSocketPacket.Lease();
            packet.Insert(span);

            var command = new SendPacket(opCode, packet);

            if (WebSocketFrame.IsControlOpCode(opCode))
                ControlSendQueue.Enqueue(command);
            else
                PayloadSendQueue.Enqueue(command);

            return true;
        }
        #endregion

        #region Disconnect
        public bool Disconnect(WebSocketCloseCode code) => Disconnect(code, string.Empty);

        public bool Disconnect(WebSocketCloseCode code, string message)
        {
            if (state != WebSocketState.Open)
                return false;

            DisconnectPacket = new DisconnectPacketData(code, message);
            Socket.Shutdown(SocketShutdown.Receive);
            state = WebSocketState.Closing;

            return true;
        }
        #endregion

        #region Stop
        void Stop(WebSocketCloseCode code) => Stop(code, string.Empty);
        void Stop(WebSocketCloseCode code, string message)
        {
            if (state == WebSocketState.Closed)
                throw new InvalidOperationException($"Socket Already Closed");

            Log.Info("WebSocket Stopped");

            state = WebSocketState.Closed;

            Server.InvokeDisconnect(this, code, message);
        }
        #endregion

        #region Close
        int RunningThreads;

        void CloseThread()
        {
            var value = Interlocked.Decrement(ref RunningThreads);

            if (value == 0)
                Close();
            else if (value < 0)
                throw new InvalidOperationException($"More Threads Close Calls than Running");
        }

        void Close()
        {
            Log.Info("WebSocket Closed");

            Socket.Dispose();
        }
        #endregion

        public WebSocketClient(WebSocketServer server, Socket socket, int id, string url)
        {
            this.Server = server;
            this.Socket = socket;
            this.ID = id;
            this.URL = url;

            state = WebSocketState.Open;

            PayloadSendQueue = new();
            ControlSendQueue = new();

            Start();
        }
    }

    public static class WebSocketFrame
    {
        public const int MaxServerHeaderSize = 4;
        public const int MaxClientHeaderSize = 8;

        public static ReadOnlySpan<byte> WriteHeader(WebSocketHeader header, ref Span<byte> span)
        {
            //OP Code
            {
                span[0] = (byte)header.OpCode;
            }

            //Final
            {
                if (header.Final)
                    span[0] |= 0b10000000;
            }

            var written = 2;

            //Length
            {
                if (header.PayloadLength <= 125)
                {
                    span[1] = (byte)header.PayloadLength;
                }
                else if (header.PayloadLength <= ushort.MaxValue)
                {
                    span[1] = 126;
                    span[2] = (byte)(header.PayloadLength >> 8);
                    span[3] = (byte)header.PayloadLength;

                    written += 2;
                }
                else
                {
                    throw new InvalidDataException($"Trying to send a message larger than {ushort.MaxValue} bytes");
                }
            }

            //Mask
            {
                if (header.Mask.HasValue)
                    throw new InvalidOperationException($"Cannot Write Header when Acing as Server");
            }

            return span.Slice(0, written);
        }

        public static bool TryReadHeader(Socket socket, out WebSocketHeader header)
        {
            Span<byte> span = stackalloc byte[MaxClientHeaderSize];

            if (socket.TryReadExact(ref span, 0, 2) == false)
            {
                header = default;
                return false;
            }

            WebSocketOPCode opCode;
            //OP Code
            {
                opCode = (WebSocketOPCode)(span[0] & 0b00001111);
            }

            bool final;
            //Final
            {
                final = (span[0] & 0b10000000) > 0;
            }

            var position = 2;

            int payloadLength;
            //Payload Length
            {
                var index = span[1] & 0b01111111;

                if (index == 127)
                {
                    header = default;
                    return false;
                }
                else if (index == 126)
                {
                    if (socket.TryReadExact(ref span, 2, 2) == false)
                    {
                        header = default;
                        return false;
                    }

                    // header is 4 bytes long
                    payloadLength = 0;
                    payloadLength |= (ushort)(span[2] << 8);
                    payloadLength |= span[3];

                    position += 2;
                }
                else
                {
                    payloadLength = index;
                }
            }

            int? mask;
            //Mask
            {
                if ((span[1] & 0b10000000) > 0)
                {
                    if (socket.TryReadExact(ref span, position, 4) == false)
                    {
                        header = default;
                        return false;
                    }

                    var raw = span.Slice(position, 4);
                    mask = BitConverter.ToInt32(raw);

                    position += 4;
                }
                else
                {
                    mask = default;
                }
            }

            header = new WebSocketHeader(final, opCode, payloadLength, mask);
            return true;
        }

        public static bool TryReadPayload(Socket socket, WebSocketHeader header, ref WebSocketPacket packet)
        {
            var span = packet.TakeSpan(header.PayloadLength);
            return TryReadPayload(socket, header, ref span);
        }
        public static bool TryReadPayload(Socket socket, WebSocketHeader header, ref Span<byte> span)
        {
            if (socket.TryReadExact(ref span) == false)
                return false;

            ToggleMask(span, header.Mask.Value);

            return true;
        }

        public static void ToggleMask(Span<byte> span, int mask)
        {
            Span<byte> key = stackalloc byte[4];

            if (BitConverter.TryWriteBytes(key, mask) == false)
                throw new InvalidOperationException();

            for (int i = 0; i < span.Length; i++)
                span[i] ^= key[i % 4];
        }

        public static bool IsControlOpCode(WebSocketOPCode code)
        {
            switch (code)
            {
                case WebSocketOPCode.Close:
                case WebSocketOPCode.Ping:
                case WebSocketOPCode.Pong:
                    return true;
            }

            return false;
        }

        public static void WriteCloseFrame(WebSocketPacket packet, WebSocketCloseCode code, string message)
        {
            //Code
            {
                var span = packet.TakeSpan(2);

                if (BitConverter.TryWriteBytes(span, (ushort)code) == false)
                    throw new InvalidOperationException();

                if (BitConverter.IsLittleEndian)
                    span.Reverse();
            }

            //Message
            if (string.IsNullOrEmpty(message) == false)
            {
                var length = Encoding.UTF8.GetByteCount(message);

                if (length > 125)
                    throw new InvalidOperationException("Message Too Big");

                var span = packet.TakeSpan(length);

                Encoding.UTF8.GetBytes(message, span);
            }
        }

        public static void ReadCloseFrameData(ReadOnlySpan<byte> data, out WebSocketCloseCode code, out string message)
        {
            if (data.Length < 2)
            {
                code = WebSocketCloseCode.Empty;
                message = String.Empty;
                return;
            }

            ReadCloseFrameCode(data, out code);

            //Message
            {
                var span = data.Slice(2, data.Length - 2);

                if (span.Length == 0)
                    message = String.Empty;
                else
                    message = Encoding.UTF8.GetString(span);
            }
        }
        public static void ReadCloseFrameCode(ReadOnlySpan<byte> data, out WebSocketCloseCode code)
        {
            if (data.Length < 2)
            {
                code = WebSocketCloseCode.Empty;
                return;
            }

            //Code
            {
                Span<byte> span = stackalloc byte[2];
                data.Slice(0, 2).CopyTo(span);

                if (BitConverter.IsLittleEndian)
                    span.Reverse();

                var value = BitConverter.ToUInt16(span);

                code = (WebSocketCloseCode)value;
            }
        }
    }

    public static class WebSocketExtensions
    {
        public static bool TryReadExact(this Socket socket, ref Span<byte> span, int offset, int length)
        {
            var segment = span.Slice(offset, length);
            return TryReadExact(socket, ref segment);
        }
        public static bool TryReadExact(this Socket socket, ref Span<byte> span)
        {
            var position = 0;

            while (position < span.Length)
            {
                var slice = span.Slice(position, span.Length - position);

                int read;

                try
                {
                    read = socket.Receive(slice);
                }
                catch (Exception ex)
                {
                    if (IsRemoteDisconnectException(ex))
                        return false;

                    throw;
                }

                if (read <= 0)
                    return false;

                position += read;
            }

            return true;
        }

        public static bool IsRemoteDisconnectException(Exception exception)
        {
            if (exception is SocketException so)
            {
                Log.Warning($"WebSocket Socket Exception, Code: {so.SocketErrorCode}|{so.ErrorCode}|{so.NativeErrorCode}");
                return true;
            }

            return false;
        }
    }

    public record struct WebSocketHeader(bool Final, WebSocketOPCode OpCode, int PayloadLength, int? Mask);

    public enum WebSocketOPCode : byte
    {
        Continuation = 0x0,
        Text = 0x1,
        Binary = 0x2,

        Close = 0x8,

        Ping = 0x9,
        Pong = 0xA,
    }

    public enum WebSocketCloseCode : ushort
    {
        //
        // Summary:
        //     (1000) The connection has closed after the request was fulfilled.
        Normal = 1000,

        //
        // Summary:
        //     (1001) Indicates an endpoint is being removed. Either the server or client will
        //     become unavailable.
        EndpointUnavailable = 1001,

        //
        // Summary:
        //     (1002) The client or server is terminating the connection because of a protocol
        //     error.
        ProtocolError = 1002,

        //
        // Summary:
        //     (1003) The client or server is terminating the connection because it cannot accept
        //     the data type it received.
        InvalidMessageType = 1003,

        //
        // Summary:
        //     No error specified.
        Empty = 1005,

        //
        // Summary:
        //     Connection closed with no closing frame
        Abnormal = 1006,

        //
        // Summary:
        //     (1007) The client or server is terminating the connection because it has received
        //     data inconsistent with the message type.
        InvalidPayloadData = 1007,

        //
        // Summary:
        //     (1008) The connection will be closed because an endpoint has received a message
        //     that violates its policy.
        PolicyViolation = 1008,

        //
        // Summary:
        //     (1009) The client or server is terminating the connection because it has received
        //     a message that is too big for it to process.
        MessageTooBig = 1009,

        //
        // Summary:
        //     (1010) The client is terminating the connection because it expected the server
        //     to negotiate an extension.
        MandatoryExtension = 1010,

        //
        // Summary:
        //     (1011) The connection will be closed by the server because of an error on the
        //     server.
        InternalServerError = 1011
    }

    public enum WebSocketState
    {
        Open,
        Closing,
        Closed
    }

    public class WebSocketPacket
    {
        byte[] data;
        int position;

        public void Insert(Span<byte> span)
        {
            Fit(span.Length);

            var destination = new Span<byte>(data, position, span.Length);
            span.CopyTo(destination);

            position += span.Length;
        }

        public void Insert(ArraySegment<byte> segment) => Insert(segment.Array, segment.Offset, segment.Count);
        public void Insert(byte[] array, int offset, int count)
        {
            Fit(count);

            Buffer.BlockCopy(array, offset, data, position, count);

            position += count;
        }

        public void Fit(int size)
        {
            if (size > data.Length - position)
            {
                var required = size - (data.Length - position);
                var factor = (int)Math.Ceiling(required / 1f / 256);

                var destination = new byte[data.Length + factor * 256];

                Buffer.BlockCopy(data, 0, destination, 0, position);

                data = destination;
            }
        }

        public ArraySegment<byte> AsSegment() => new ArraySegment<byte>(data, 0, position);
        public Span<byte> AsSpan() => new Span<byte>(data, 0, position);

        public Span<byte> TakeSpan(int length)
        {
            Fit(length);

            var span = new Span<byte>(data, position, length);

            position += length;

            return span;
        }

        public void Reset()
        {
            position = 0;
        }

        public void Recycle()
        {
            Reset();

            ObjectPool<WebSocketPacket>.Return(this);
        }

        public WebSocketPacket()
        {
            data = new byte[1024];
            position = 0;
        }

        public static WebSocketPacket Lease() => ObjectPool<WebSocketPacket>.Lease();
    }

    public static class WebSocketHandshake
    {
        public const int Timeout = 10 * 1000;

        public static readonly byte[] ResponsePrefixTemplate;

        public static bool TryPerform(Socket socket, out string url)
        {
            var timeout = socket.ReceiveTimeout;
            socket.ReceiveTimeout = Timeout;

            if (EnsureGET(socket) == false)
            {
                url = default;
                return false;
            }

            Span<char> characters = stackalloc char[1024];
            if (TryParseHTTPRequest(socket, ref characters) == false)
            {
                url = default;
                return false;
            }

            socket.ReceiveTimeout = timeout;

            if (EnsureWebSocketUpgradeRequest(characters) == false)
            {
                url = default;
                return false;
            }

            //URL
            {
                var span = TryParseURL(characters);

                if (span.Length == 0)
                {
                    url = default;
                    return false;
                }

                url = span.ToString();
            }

            var key = TryParseSecKey(characters, "Sec-WebSocket-Key:");
            if (key.Length == 0)
                return false;

            Span<char> secret = stackalloc char[28];
            ComputeKeySecret(key, ref secret);

            Span<byte> response = stackalloc byte[1024];
            WriteResponse(ref response, secret);

            socket.Send(response);

            return true;
        }

        static bool EnsureGET(Socket socket)
        {
            Span<byte> span = stackalloc byte[3];

            if (socket.TryReadExact(ref span) == false)
                return false;

            if ((char)span[0] != 'G' && (char)span[0] != 'g') return false;
            if ((char)span[1] != 'E' && (char)span[1] != 'e') return false;
            if ((char)span[2] != 'T' && (char)span[2] != 't') return false;

            return true;
        }

        static bool TryParseHTTPRequest(Socket socket, ref Span<char> characters)
        {
            const string Exit = "\r\n\r\n";

            Span<byte> buffer = stackalloc byte[128];

            var index = 0;

            while(true)
            {
                var read = socket.Receive(buffer);

                if (read <= 0)
                    return false;

                for (int i = 0; i < read; i++, index++)
                    characters[index] = (char)buffer[i];

                var slice = characters.Slice(0, index);
                if (slice.EndsWith(Exit))
                {
                    characters = slice;
                    return true;
                }
            }

            return false;
        }

        static bool EnsureWebSocketUpgradeRequest(ReadOnlySpan<char> characters)
        {
            const string Header1 = "Connection: Upgrade";
            if (characters.Contains(Header1, StringComparison.OrdinalIgnoreCase) == false)
                return false;

            const string Header2 = "Upgrade: websocket";
            if (characters.Contains(Header2, StringComparison.OrdinalIgnoreCase) == false)
                return false;

            return true;
        }

        static ReadOnlySpan<char> TryParseSecKey(ReadOnlySpan<char> characters, string key)
        {
            var start = characters.IndexOf(key);
            start += key.Length;

            if (start == -1)
                return default;

            int end;

            for (end = start; end < characters.Length; end++)
            {
                if (characters[end] == '\r')
                    break;
            }

            return characters.Slice(start, end - start).Trim();
        }

        static ReadOnlySpan<char> TryParseURL(ReadOnlySpan<char> characters)
        {
            int? start = default;

            for (int i = 0; i < characters.Length; i++)
            {
                if (characters[i] == ' ')
                {
                    if (start == default)
                        start = i;
                    else
                        return characters.Slice(start.Value, i).Trim();
                }
            }

            return default;
        }

        static void ComputeKeySecret(ReadOnlySpan<char> key, ref Span<char> secret)
        {
            const string GUID = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

            Span<byte> binary = stackalloc byte[key.Length + GUID.Length];
            Encoding.ASCII.GetBytes(key, binary);
            Encoding.ASCII.GetBytes(GUID, binary.Slice(key.Length));

            Span<byte> sha1 = stackalloc byte[20];
            if (SHA1.TryHashData(binary, sha1, out var length) == false)
                throw new InvalidOperationException();

            if (Convert.TryToBase64Chars(sha1, secret, out var written) == false)
                throw new InvalidOperationException();
        }

        static void WriteResponse(ref Span<byte> buffer, ReadOnlySpan<char> secret)
        {
            var position = 0;

            //Template
            {
                var span = new Span<byte>(ResponsePrefixTemplate);
                span.CopyTo(buffer);
                position += span.Length;
            }

            //Secret
            {
                for (int i = 0; i < secret.Length; i++)
                    buffer[i + position] = (byte)secret[i];

                position += secret.Length;
            }

            //EOL
            {
                buffer[position] = (byte)'\r';
                position += 1;

                buffer[position] = (byte)'\n';
                position += 1;

                buffer[position] = (byte)'\r';
                position += 1;

                buffer[position] = (byte)'\n';
                position += 1;
            }

            buffer = buffer.Slice(0, position);
        }

        static WebSocketHandshake()
        {
            const string EOL = "\r\n";

            const string Text = "HTTP/1.1 101 Switching Protocols" + EOL
                + "Connection: Upgrade" + EOL
                + "Upgrade: websocket" + EOL
                + $"Sec-WebSocket-Accept: ";

            ResponsePrefixTemplate = Encoding.ASCII.GetBytes(Text);
        }
    }
}