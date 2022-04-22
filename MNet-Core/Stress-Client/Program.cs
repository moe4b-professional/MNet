using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using LiteNetLib;

namespace MNet
{
    static class Client
    {
        static RestClientAPI RestAPI;

        static string IP = "10.0.0.10";

        static AppID AppID = new AppID("Game 1 (Global)");

        static Version GameVersion = new Version(1, 0, 0);

        public static List<Player> Players;

        public static byte[] Payload;

        static void Main(string[] args)
        {
            Run();

            while (true)
            {
                var key = Console.ReadKey();

                if (key.Key == ConsoleKey.Escape)
                {
                    Disconnect();
                    Environment.Exit(0);
                }
            }
        }

        static async void Run()
        {
            RestAPI = new RestClientAPI(Constants.Server.Game.Rest.Port, RestScheme.HTTP);
            RestAPI.SetIP(IP);

            Players = new List<Player>();

            Payload = new byte[14];

            for (byte i = 0; i < Payload.Length; i++)
                Payload[i] = i;

            Console.Write("How many Rooms to Create: ");
            var rooms = int.Parse(Console.ReadLine());

            Console.Write("How many Players per Room: ");
            var occupancy = int.Parse(Console.ReadLine());

            for (int i = 1; i <= rooms; i++)
            {
                if (i % 50 == 0) await Task.Delay(8000);

                Stress(i, occupancy);
            }
        }

        static async void Stress(int index, int occupancy)
        {
            var name = new FixedString32($"Stress Room {index}");
            var capacity = byte.MaxValue;

            var room = await CreateRoom(name, capacity);

            for (int i = 0; i < occupancy; i++)
            {
                var player = new Player(i);

                player.Connect(IP, room.ID);
            }
        }

        static async Task<RoomInfo> CreateRoom(FixedString32 name, byte capacity)
        {
            var attributes = new AttributesCollection();
            attributes.Set(0, (byte)0);

            var options = new RoomOptions()
            {
                Name = name,
                Visible = true,
                Capacity = capacity,
                MigrationPolicy = MigrationPolicy.Continue,
                Password = default,
                Attributes = attributes,
                Scene = 1,
            };

            var request = new CreateRoomRequest(AppID, GameVersion, options);

            var info = await RestAPI.POST<CreateRoomRequest, RoomInfo>(Constants.Server.Game.Rest.Requests.Room.Create, request);

            return info;
        }

        static void Disconnect()
        {
            Players.ForEach(Action);
            void Action(Player player) => player.Disconnect();
        }
    }

    class Player : INetEventListener
    {
        int Index;

        NetManager Socket;
        NetPeer Peer;

        NetworkClientID ClientID;
        NetworkEntityID EntityID;

        bool IsReady = false;

        NetworkWriter NetworkWriter;
        NetworkReader NetworkReader;

        public Player(int index)
        {
            NetworkWriter = NetworkWriter.Pool.Take();
            NetworkReader = NetworkReader.Pool.Take();

            this.Index = index;

            Client.Players.Add(this);

            Socket = new NetManager(this);
            Socket.DisconnectTimeout = 20 * 1000;
            Socket.Start();

            new Thread(Run).Start();
        }

        public void Connect(string address, RoomID room)
        {
            var key = $"{room}";

            Peer = Socket.Connect(address, NetworkTransportUtility.LiteNetLib.Port, key);
        }

        public void OnPeerConnected(NetPeer peer)
        {
            RegisterClient();
        }

        void RegisterClient()
        {
            var name = new FixedString32($"Player {Index}");

            var profile = new NetworkClientProfile(ref name);

            var request = new RegisterClientRequest(profile, default, default);

            Send(ref request, DeliveryMethod.ReliableOrdered);
        }
        void RegisterClientResponse(RegisterClientResponse response)
        {
            ClientID = response.ID;

            SpawnEntity();
        }

        void SpawnEntity()
        {
            var request = SpawnEntityRequest.Write(0, new EntitySpawnToken(0), PersistanceFlags.None, null);

            Send(ref request, DeliveryMethod.ReliableOrdered);
        }
        void SpawnEntityResponse(SpawnEntityResponse response)
        {
            EntityID = response.ID;

            IsReady = true;
        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader packet, DeliveryMethod deliveryMethod, byte deliveryChannel)
        {
            var segment = packet.GetRemainingBytesSegment();

            NetworkReader.Assign(segment);

            using (NetworkReader)
            {
                var type = NetworkReader.Read<Type>();

                if (type == typeof(RegisterClientResponse))
                {
                    var response = NetworkReader.Read<RegisterClientResponse>();
                    RegisterClientResponse(response);
                }
                else if (type == typeof(SpawnEntityResponse))
                {
                    var response = NetworkReader.Read<SpawnEntityResponse>();
                    SpawnEntityResponse(response);
                }
            }

            packet.Recycle();
        }

        void Run()
        {
            while (true)
            {
                Socket.PollEvents();

                if (IsReady)
                {
                    SendBroadcastRPC(DeliveryMethod.ReliableOrdered);
                    SendBroadcastRPC(DeliveryMethod.Unreliable);

                    SendSyncVar(DeliveryMethod.ReliableOrdered);
                    SendSyncVar(DeliveryMethod.Unreliable);
                }

                Thread.Sleep(50);
            }
        }

        void SendBroadcastRPC(DeliveryMethod delivery)
        {
            var chunk = new ByteChunk(Client.Payload);
            var request = BroadcastRpcRequest.Write(EntityID, default, default, RemoteBufferMode.Last, default, null, chunk);

            Send(ref request, delivery);
        }

        void SendSyncVar(DeliveryMethod delivery)
        {
            var chunk = new ByteChunk(Client.Payload);
            var request = BroadcastSyncVarRequest.Write(EntityID, default, default, default, chunk);

            Send(ref request, delivery);
        }

        void Send<T>(ref T payload, DeliveryMethod delivery)
        {
            using (NetworkWriter)
            {
                NetworkWriter.Write(typeof(T));
                NetworkWriter.Write(payload);

                var segment = NetworkWriter.AsSegment();

                Peer.Send(segment.Array, segment.Offset, segment.Count, delivery);
            }
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            IsReady = false;

            NetworkWriter.Pool.Return(NetworkWriter);
            NetworkReader.Pool.Return(NetworkReader);

            Log.Error($"Player Disconnected: {disconnectInfo.Reason}");
        }

        public void Disconnect()
        {
            IsReady = false;

            if (Peer == null) return;

            Peer.Disconnect();
        }

        #region Unused Callbacks
        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) { }
        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType) { }
        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) { }
        public void OnConnectionRequest(ConnectionRequest request) { }
        #endregion
    }
}