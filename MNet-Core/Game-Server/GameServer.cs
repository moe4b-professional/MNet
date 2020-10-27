using System;
using System.Net;

using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Diagnostics;

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
            catch (Exception)
            {
                throw;
            }
        }

        static void Procedure()
        {
            Console.Title = $"Game Sever | Network API v{Constants.ApiVersion}";

            Log.Info($"Network API Version: {Constants.ApiVersion}");

            ApiKey.Read();

            Config = Config.Read();

            Address = Config.PublicAddress;

            Log.Info($"Server ID: {ID}");
            Log.Info($"Server Name: {Name}");
            Log.Info($"Server Region: {Region}");

            MasterServer.Configure(Config.MasterAddress);
            MasterServer.Register(GetInfo(), ApiKey.Token);

            Rest = new RestAPI(Constants.Server.Game.Rest.Port);
            Rest.Start();

            Realtime = new RealtimeAPI(Config.NetworkTransport);
            Realtime.Start();

            Lobby = new Lobby();
            Lobby.Configure();

            Sandbox.Run();

            while (true) Console.ReadLine();
        }
    }

    public static class Sandbox
    {
#pragma warning disable IDE0051 // Remove unused private members
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
    }
#pragma warning restore IDE0051 // Remove unused private members
}