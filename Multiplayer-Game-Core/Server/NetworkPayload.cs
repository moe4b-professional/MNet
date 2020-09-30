using System;
using System.Collections.Generic;
using System.Text;

namespace Backend
{
    public class MasterServerInfoPayload : INetworkSerializable
    {
        GameServerInfo[] servers;
        public GameServerInfo[] Servers => servers;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref servers);
        }

        public MasterServerInfoPayload() { }
        public MasterServerInfoPayload(GameServerInfo[] servers)
        {
            this.servers = servers;
        }
    }

    #region Register
    public class RegisterGameServerRequest : INetworkSerializable
    {
        GameServerID id;
        public GameServerID ID => id;

        GameServerRegion region;
        public GameServerRegion Region => region;

        string key;
        public string Key => key;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref id);
            context.Select(ref region);
            context.Select(ref key);
        }

        public RegisterGameServerRequest() { }
        public RegisterGameServerRequest(GameServerID id, GameServerRegion region, string key)
        {
            this.id = id;
            this.region = region;
            this.key = key;
        }
    }

    public class RegisterGameServerResult : INetworkSerializable
    {
        bool success;
        public bool Success => success;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref success);
        }

        public RegisterGameServerResult() { }
        public RegisterGameServerResult(bool success)
        {
            this.success = success;
        }
    }
    #endregion

    #region Remove
    public class RemoveGameSeverRequest : INetworkSerializable
    {
        GameServerID id;
        public GameServerID ID => id;

        string key;
        public string Key => key;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref id);
            context.Select(ref key);
        }

        public RemoveGameSeverRequest() { }
        public RemoveGameSeverRequest(GameServerID id, string key)
        {
            this.id = id;
            this.key = key;
        }
    }

    public class RemoveGameSeverResult : INetworkSerializable
    {
        bool success;
        public bool Success => success;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref success);
        }

        public RemoveGameSeverResult() { }
        public RemoveGameSeverResult(bool success)
        {
            this.success = success;
        }
    }
    #endregion
}