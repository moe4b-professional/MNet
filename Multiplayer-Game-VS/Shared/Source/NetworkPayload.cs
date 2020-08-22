using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

using WebSocketSharp;
using WebSocketSharp.Net;

namespace Game.Shared
{
    public static class NetworkPayload
    {
        public const short MinCode = 400;

        public static Dictionary<ushort, Type> Types { get; private set; }

        public static Dictionary<Type, ushort> Codes { get; private set; }

        public static Type GetType(ushort code)
        {
            if (Types.TryGetValue(code, out var type))
                return type;
            else
                throw new Exception($"No NetworkPayload Registerd With Code {code}");
        }

        public static ushort GetCode<T>() => GetCode(typeof(T));
        public static ushort GetCode(object instance) => GetCode(instance.GetType());
        public static ushort GetCode(Type type)
        {
            if (Codes.TryGetValue(type, out var code))
                return code;
            else
                throw new Exception($"Type {type} Not Registered as NetworkPayload");
        }

        #region Register
        public static void Register<T>(ushort code) => Register(code, typeof(T));
        public static void Register(ushort code, Type type)
        {
            ValidateDuplicate(code, type);

            Types.Add(code, type);
            Codes.Add(type, code);
        }

        static void RegisterInternal()
        {
            Register<byte>(0);

            Register<short>(1);
            Register<ushort>(2);

            Register<int>(3);
            Register<uint>(4);

            Register<float>(5);

            Register<bool>(6);

            Register<string>(7);

            Register<Guid>(8);
            Register<DateTime>(9);

            Register<CreateRoomRequest>(10);

            Register<RegisterClientRequest>(11);
            Register<RegisterClientResponse>(12);

            Register<ReadyClientRequest>(13);
            Register<ReadyClientResponse>(14);

            Register<SpawnEntityRequest>(15);
            Register<SpawnEntityCommand>(16);

            Register<DestroyEntityRequest>(17);
            Register<DestroyEntityCommand>(18);

            Register<ClientConnectedPayload>(19);
            Register<ClientDisconnectPayload>(20);

            Register<LobbyInfo>(21);

            Register<RoomBasicInfo>(22);
            Register<RoomInternalInfo>(23);

            Register<NetworkClientInfo>(24);

            Register<NetworkClientProfile>(25);

            Register<RpcRequest>(26);
            Register<RpcCommand>(27);

            Register<AttributesCollection>(28);

            Register<RpcBufferMode>(29);
        }
        #endregion

        #region Validate
        static void ValidateDuplicate(ushort code, Type type)
        {
            ValidateTypeDuplicate(code, type);

            ValidateCodeDuplicate(code, type);
        }

        static void ValidateTypeDuplicate(ushort code, Type type)
        {
            if (Types.TryGetValue(code, out var duplicate))
                throw new Exception($"NetworkPayload Type Duplicate Found, {type} & {duplicate} both registered with code {code}");
        }

        static void ValidateCodeDuplicate(ushort code, Type type)
        {
            if (Codes.TryGetValue(type, out var duplicate))
                throw new Exception($"NetworkPayload Type Duplicate Found, Code {code} & {duplicate} Both Registered to {type}");
        }
        #endregion

        static NetworkPayload()
        {
            Types = new Dictionary<ushort, Type>();

            Codes = new Dictionary<Type, ushort>();

            RegisterInternal();
        }
    }
}