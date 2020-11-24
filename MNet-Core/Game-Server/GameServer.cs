using System;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

using System.Net.Http;
using System.Threading;

namespace MNet
{
    static class GameServer
    {
        public static Config Config { get; private set; }

        public static GameServerID ID { get; private set; }
        public static IPAddress Address
        {
            get => ID.Value;
            private set => ID = new GameServerID(value);
        }

        public static string Name => Config.Name;

        public static GameServerRegion Region => Config.Region;

        public static GameServerInfo GetInfo() => new GameServerInfo(ID, Name, Region);

        public static RestAPI Rest { get; private set; }
        public static RealtimeAPI Realtime { get; private set; }

        public static Lobby Lobby { get; private set; }

        static void Main()
        {
            try
            {
                Procedure();
            }
            catch
            {
                throw;
            }

            while (true) Console.ReadLine();
        }

        static void Procedure()
        {
            Console.Title = $"Game Sever | Network API v{Constants.ApiVersion}";

            Log.Info($"Network API Version: {Constants.ApiVersion}");

            ApiKey.Read();

            Config = Config.Read();

            ResolveAddress();

            Log.Info($"Server ID: {ID}");
            Log.Info($"Server Name: {Name}");
            Log.Info($"Server Region: {Region}");

            MasterServer.Configure(Config.MasterAddress);

            if (Register() == false) return;

            Rest = new RestAPI(Constants.Server.Game.Rest.Port);
            Rest.Start();

            Realtime = new RealtimeAPI(Config.Transport);
            Realtime.Start();

            Lobby = new Lobby();
            Lobby.Configure();

            Sandbox.Run();
        }

        static void ResolveAddress()
        {
            if (Config.PublicAddress != null)
            {
                Address = Config.PublicAddress;
                return;
            }

            if (Config.Region == GameServerRegion.Local)
            {
                Address = IPAddress.Parse("127.0.0.1");
                return;
            }

            Log.Info("Retrieving Public IP");
            try
            {
                Address = PublicIP.Retrieve();
            }
            catch (Exception ex)
            {
                var text = $"Could not Retrieve Public IP for Server, " +
                    $"Please Explicitly Set {nameof(Config.PublicAddress)} Property in {Config.FileName} Config File";

                Log.Error(text);

                Log.Error($"Public IP Retrieval Exception: {ex}");
            }
        }

        static bool Register()
        {
            if (MasterServer.Register(GetInfo(), ApiKey.Token, out var response) == false)
            {
                Log.Error("Server Registeration Failed");
                return false;
            }

            Log.Info("Server Registeration Success");

            Config.Append(response.RemoteConfig);

            return true;
        }
    }

    public static class Sandbox
    {
#pragma warning disable IDE0051
        public static void Run()
        {

        }

        static void NullableTupleSerialization()
        {
            Tuple<DateTime?, Guid?, int?> tuple = new Tuple<DateTime?, Guid?, int?>(DateTime.Now, null, 42);

            var type = tuple.GetType();

            var binary = NetworkSerializer.Serialize(tuple);

            var value = NetworkSerializer.Deserialize(binary, type) as ITuple;

            for (int i = 0; i < value.Length; i++) Log.Info(value[i]);
        }

        static void CreateRooms()
        {
            var app = AppID.Parse("AAAAAAAA-AAAA-AAAA-AAAA-AAAAAAAAAAAA");

            var version = Version.Create(0, 1);

            var attributes = new AttributesCollection();
            attributes.Set(0, "Level 1");

            GameServer.Lobby.CreateRoom(app, version, "Game Room #1", 4, attributes);
            GameServer.Lobby.CreateRoom(app, version, "Game Room #2", 8, attributes);
        }

        static void NullableSerialization()

        {
            NetworkClientID? value = new NetworkClientID(20);

            var binary = NetworkSerializer.Serialize(value);

            Log.Info(binary.ToPrettyString());

            var instance = NetworkSerializer.Deserialize<NetworkClientID?>(binary);

            Log.Info(instance);
        }

        static void NullableListSerialization()
        {
            var list = new List<int?>()
            {
                42,
                null,
                12,
                420,
                null,
                69
            };

            var type = list.GetType();

            var binary = NetworkSerializer.Serialize(list);

            Log.Info(binary.ToPrettyString());

            var value = NetworkSerializer.Deserialize(binary, type) as IList;

            Log.Info(value.ToPrettyString());
        }

        static void NetTupleSerialization()
        {
            var tuple = NetTuple.Create("Hello World", 4, DateTime.Now, Guid.NewGuid());
            var type = tuple.GetType();
            var binary = NetworkSerializer.Serialize(tuple);

            var payload = NetworkSerializer.Deserialize(binary, type) as INetTuple;
            foreach (var item in payload) Log.Info(item);
        }

        static void ObjectArraySerialization()
        {
            var array = new object[]
            {
                "Hello World",
                20,
                Guid.NewGuid(),
                DateTime.Now,
            };

            var type = array.GetType();

            var binary = NetworkSerializer.Serialize(array);

            var value = NetworkSerializer.Deserialize(binary, type) as Array;

            foreach (var item in value) Log.Info(item);
        }

        static void ObjectListSerialization()
        {
            var list = new List<object>
            {
                "Hello World",
                20,
                Guid.NewGuid(),
                DateTime.Now,
            };

            var type = list.GetType();

            var binary = NetworkSerializer.Serialize(list);

            var value = NetworkSerializer.Deserialize(binary, type) as IList;

            foreach (var item in value) Log.Info(item);
        }
#pragma warning restore IDE0051
    }
}