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
        public ConcurrentDictionary<int, WebSocket> Clients { get; private set; }

        public bool IsRunning { get; private set; }

        public int Timeout = 10 * 1000;
        public int PingInterval = 4 * 1000;

        Stopwatch Stopwatch;
        public long GetTime() => Stopwatch.ElapsedMilliseconds;

        TcpListener listener;

        #region Connect
        public event ConnectDelegate OnConnect;
        public delegate void ConnectDelegate(WebSocket socket);

        internal void InvokeConnect(WebSocket socket)
        {
            OnConnect?.Invoke(socket);
        }
        #endregion

        #region Message
        public event MessageDelegate OnMessage;
        public delegate void MessageDelegate(WebSocket socket, WebSocketPacket packet);

        internal void InvokeMessage(WebSocket socket, WebSocketPacket packet)
        {
            OnMessage?.Invoke(socket, packet);
        }
        #endregion

        #region Disconnect
        public event DisconnectDelegate OnDisconnect;
        public delegate void DisconnectDelegate(WebSocket socket, WebSocketCloseCode code, string message);

        internal void InvokeDisconnect(WebSocket socket, WebSocketCloseCode code, string message)
        {
            Clients.Remove(socket.ID, out _);

            OnDisconnect?.Invoke(socket, code, message);
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
                TcpClient client;

                try
                {
                    client = listener.AcceptTcpClient();
                }
                catch (Exception ex)
                {
                    if (ex is SocketException || ex is IOException)
                        break;
                    else
                        throw;
                }

                Log.Info("Received TCP Request");

                ThreadPool.QueueUserWorkItem(ProcessIncomingSocket, client, false);
            }
        }

        void ProcessIncomingSocket(TcpClient client)
        {
            try
            {
                var stream = client.GetStream();

                if (WebSocketHandshake.TryPerform(client, stream, out var url) == false)
                    return;

                Log.Info("WebSocket Handshake Complete");

                var socket = Register(client, stream, url);
            }
            catch (Exception)
            {
                client.Close();
            }
        }

        WebSocket Register(TcpClient client, Stream stream, string url)
        {
            var ID = Interlocked.Increment(ref IDIndex);

            var socket = new WebSocket(this, client, stream, ID, url);

            Clients.TryAdd(ID, socket);

            return socket;
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

            listener.Stop();
        }

        public WebSocketServer(IPAddress address, int port)
        {
            listener = new TcpListener(address, port);

            Stopwatch = new Stopwatch();

            Clients = new();
        }
    }

    public class WebSocket
    {
        readonly WebSocketServer Server;

        readonly TcpClient Client;
        readonly Stream Stream;

        public int ID { get; }
        public string URL { get; }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public volatile WebSocketState State;

        public object Tag;

        long LastPingSendTime;
        long LastPongReceiveTime;

        internal void Start()
        {
            //Time
            {
                var time = Server.GetTime();

                LastPingSendTime = time;
                LastPongReceiveTime = time;
            }

            Server.InvokeConnect(this);

            ReceiveThread = new Thread(ReceiveLoop);
            ReceiveThread.IsBackground = true;
            ReceiveThread.Name = $"Client {ID} Receive Loop";
            ReceiveThread.Start();

            SendThread = new Thread(SendLoop);
            SendThread.IsBackground = true;
            SendThread.Name = $"Client {ID} Send Loop";
            SendThread.Start();
        }

        #region Recieve
        Thread ReceiveThread;

        void ReceiveLoop()
        {
            var packet = WebSocketPacket.Lease();

            var opCode = WebSocketOPCode.Binary;

            try
            {
                while (true)
                {
                    if (WebSocketFrame.TryReadHeader(Stream, out var header) == false)
                    {
                        Disconnect(WebSocketCloseCode.ProtocolError);
                        return;
                    }
                    if (header.Mask.HasValue == false)
                    {
                        Disconnect(WebSocketCloseCode.ProtocolError);
                        return;
                    }

                    if (WebSocketFrame.IsControlOpCode(header.OpCode))
                    {
                        if (HandleControlFrame(header, Stream) == false)
                            break;
                    }
                    else
                    {
                        if (HandlePayloadFrame(header, ref opCode, ref packet) == false)
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is IOException || ex is SocketException || ex is ObjectDisposedException)
                {
                    if (State == WebSocketState.Open)
                        Close(WebSocketCloseCode.Abnormal, default);

                    Log.Error(ex);
                }
                else
                {
                    throw;
                }
            }
            finally
            {
                packet.Recycle();

                Log.Info($"Recieve Loop Closed");
            }
        }

        bool HandlePayloadFrame(WebSocketHeader header, ref WebSocketOPCode opCode, ref WebSocketPacket packet)
        {
            if (header.OpCode == WebSocketOPCode.Text)
            {
                Disconnect(WebSocketCloseCode.InvalidMessageType);
                return false;
            }

            if (WebSocketFrame.TryReadPayload(Stream, header, ref packet) == false)
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
                        HandleBinaryPacket(header, packet);
                        break;

                    default:
                        throw new InvalidOperationException();
                }

                packet = WebSocketPacket.Lease();
            }

            return true;
        }
        void HandleBinaryPacket(WebSocketHeader header, WebSocketPacket packet)
        {
            Server.InvokeMessage(this, packet);
        }

        bool HandleControlFrame(WebSocketHeader header, Stream stream)
        {
            if (header.PayloadLength > 125)
            {
                Disconnect(WebSocketCloseCode.ProtocolError);
                return false;
            }

            Span<byte> span = stackalloc byte[header.PayloadLength];

            if (WebSocketFrame.TryReadPayload(Stream, header, ref span) == false)
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
                    return HandlePongFrame();

                default:
                    throw new InvalidOperationException();
            }
        }
        bool HandleCloseFrame(Span<byte> span)
        {
            WebSocketFrame.ReadCloseFrameCode(span, out var code);

            Disconnect(span);
            return false;
        }
        bool HandlePingFrame(Span<byte> span)
        {
            Send(WebSocketOPCode.Pong, span);
            return true;
        }
        bool HandlePongFrame()
        {
            LastPongReceiveTime = Server.GetTime();

            return true;
        }
        #endregion

        #region Send Loop
        Thread SendThread;

        readonly ConcurrentQueue<SendCommand> PayloadSendQueue;
        readonly ConcurrentQueue<SendCommand> ControlSendQueue;

        WebSocketPacket DisconnectPacket;

        readonly record struct SendCommand(WebSocketOPCode opCode, WebSocketPacket packet);

        void SendLoop()
        {
            while (true)
            {
                if (State != WebSocketState.Open) break;

                try
                {
                    if (ProcessKeepAlive() == false)
                        break;

                    if (SendQueuesImmediate(ControlSendQueue, PayloadSendQueue) == false)
                        break;
                }
                catch (Exception ex)
                {
                    if (ex is SocketException || ex is IOException)
                    {
                        Log.Error(ex);

                        break;
                    }
                    else
                    {
                        throw;
                    }
                }

                Thread.Sleep(10);
            }

            ValidateSendLoopClosure();

            ClearCommandQueue(ControlSendQueue);
            ClearCommandQueue(PayloadSendQueue);

            Log.Info($"Send Loop Closed");
        }

        bool ProcessKeepAlive()
        {
            var time = Server.GetTime();

            //Pong
            {
                var timespan = time - LastPongReceiveTime;

                if (timespan > Server.Timeout)
                {
                    Close(WebSocketCloseCode.EndpointUnavailable, string.Empty);
                    return false;
                }
            }

            //Ping
            {
                var timespan = time - LastPingSendTime;

                if (timespan < Server.PingInterval)
                    return true;

                LastPingSendTime = time;
                SendImmediate(WebSocketOPCode.Ping, default);
            }

            return true;
        }

        bool SendQueuesImmediate(ConcurrentQueue<SendCommand> control, ConcurrentQueue<SendCommand> payload)
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
        bool SendQueueImmediate(ConcurrentQueue<SendCommand> queue)
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
            if (State != WebSocketState.Open)
                if (opCode != WebSocketOPCode.Close)
                    return false;

            //Header
            {
                var header = new WebSocketHeader(true, opCode, span.Length, default);

                Span<byte> buffer = stackalloc byte[WebSocketFrame.MaxServerHeaderSize];
                var raw = WebSocketFrame.WriteHeader(header, ref buffer);

                Stream.Write(raw);
            }

            Stream.Write(span);

            return true;
        }

        void ValidateSendLoopClosure()
        {
            switch (State)
            {
                case WebSocketState.Open:
                    throw new Exception($"Send Loop Clsoed while Socket is still Open");

                case WebSocketState.Closing:
                {
                    var span = DisconnectPacket.AsSpan();

                    WebSocketFrame.ReadCloseFrameData(span, out var code, out var message);

                    try
                    {
                        SendImmediate(WebSocketOPCode.Close, span);
                    }
                    catch (Exception)
                    {
                        code = WebSocketCloseCode.Abnormal;
                        message = string.Empty;
                    }
                    finally
                    {
                        Close(code, message);
                        DisconnectPacket.Recycle();
                    }
                }
                break;
            }
        }

        void ClearCommandQueue(ConcurrentQueue<SendCommand> queue)
        {
            while (queue.TryDequeue(out var command))
                command.packet.Recycle();
        }
        #endregion

        #region Send Comamnds
        public bool SendBinary(Span<byte> span) => Send(WebSocketOPCode.Binary, span);

        internal bool Send(WebSocketOPCode opCode, Span<byte> span)
        {
            if (State != WebSocketState.Open)
                return false;

            var packet = WebSocketPacket.Lease();
            packet.Insert(span);

            var command = new SendCommand(opCode, packet);

            if (WebSocketFrame.IsControlOpCode(opCode))
                ControlSendQueue.Enqueue(command);
            else
                PayloadSendQueue.Enqueue(command);

            return true;
        }
        #endregion

        public bool Disconnect(WebSocketCloseCode code) => Disconnect(code, string.Empty);
        public bool Disconnect(WebSocketCloseCode code, string message)
        {
            var packet = WebSocketPacket.Lease();
            WebSocketFrame.WriteCloseFrame(packet, code, message);
            return Disconnect(packet);
        }
        internal bool Disconnect(Span<byte> span)
        {
            var packet = WebSocketPacket.Lease();
            packet.Insert(span);
            return Disconnect(packet);
        }
        internal bool Disconnect(WebSocketPacket packet)
        {
            if (State != WebSocketState.Open)
                return false;

            State = WebSocketState.Closing;
            DisconnectPacket = packet;

            return true;
        }

        internal void Close(WebSocketCloseCode code, string message)
        {
            if (State == WebSocketState.Closed)
                throw new InvalidOperationException($"Socket Already Closed");

            State = WebSocketState.Closed;
            Client.Close();

            Server.InvokeDisconnect(this, code, message);
        }

        public WebSocket(WebSocketServer server, TcpClient client, Stream stream, int id, string url)
        {
            this.Server = server;
            this.Client = client;
            this.Stream = stream;
            this.ID = id;
            this.URL = url;

            State = WebSocketState.Open;

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

        public static bool TryReadHeader(Stream stream, out WebSocketHeader header)
        {
            Span<byte> span = stackalloc byte[MaxClientHeaderSize];

            if (stream.TryReadExact(ref span, 0, 2) == false)
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
                    if (stream.TryReadExact(ref span, 2, 2) == false)
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
                    if (stream.TryReadExact(ref span, position, 4) == false)
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

        public static bool TryReadPayload(Stream stream, WebSocketHeader header, ref WebSocketPacket packet)
        {
            var span = packet.TakeSpan(header.PayloadLength);

            return TryReadPayload(stream, header, ref span);
        }
        public static bool TryReadPayload(Stream stream, WebSocketHeader header, ref Span<byte> span)
        {
            if (stream.TryReadExact(ref span) == false)
                return false;

            ToggleMask(span, header.Mask.Value);

            return true;
        }

        public static bool TryReadExact(this Stream stream, ref Span<byte> span, int offset, int length)
        {
            var segment = span.Slice(offset, length);

            return TryReadExact(stream, ref segment);
        }
        public static bool TryReadExact(this Stream stream, ref Span<byte> span)
        {
            var position = 0;

            while (position < span.Length)
            {
                var slice = span.Slice(position, span.Length - position);

                var read = stream.Read(slice);

                if (read <= 0)
                    return false;

                position += read;
            }

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
        public const int Timeout = 10_000;

        public static readonly byte[] ResponsePrefixTemplate;

        public static bool TryPerform(TcpClient client, Stream stream, out string url)
        {
            var timeout = stream.ReadTimeout;

            stream.ReadTimeout = Timeout;

            try
            {
                if (EnsureGET(stream) == false)
                {
                    url = default;
                    return false;
                }

                Span<char> characters = stackalloc char[1024];
                if (TryParseHTTPRequest(stream, ref characters) == false)
                {
                    url = default;
                    return false;
                }

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

                stream.Write(response);

                return true;
            }
            finally
            {
                stream.ReadTimeout = timeout;
            }
        }

        static bool EnsureGET(Stream stream)
        {
            Span<byte> span = stackalloc byte[3];

            if (stream.TryReadExact(ref span) == false)
                return false;

            if ((char)span[0] != 'G' && (char)span[0] != 'g') return false;
            if ((char)span[1] != 'E' && (char)span[1] != 'e') return false;
            if ((char)span[2] != 'T' && (char)span[2] != 't') return false;

            return true;
        }

        static bool TryParseHTTPRequest(Stream stream, ref Span<char> characters)
        {
            const string Exit = "\r\n\r\n";

            for (var i = 0; i < characters.Length; i++)
            {
                var next = stream.ReadByte();
                if (next < 0)
                    return false;

                characters[i] = (char)next;

                var slice = characters.Slice(0, i + 1);
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