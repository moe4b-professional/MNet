using System;
using System.Collections.Generic;
using System.Text;

namespace MNet
{
    #region Register
    [Preserve]
    [Serializable]
    public class RegisterGameServerRequest : INetworkSerializable
    {
        GameServerInfo info;
        public GameServerInfo Info => info;

        public GameServerID ID => info.ID;
        public Version[] Versions => info.Versions;
        public GameServerRegion Region => info.Region;

        string key;
        public string Key => key;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref info);
            context.Select(ref key);
        }

        public RegisterGameServerRequest() { }
        public RegisterGameServerRequest(GameServerInfo info, string key)
        {
            this.info = info;
            this.key = key;
        }
    }

    [Preserve]
    [Serializable]
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
    [Preserve]
    [Serializable]
    public class RemoveGameServerRequest : INetworkSerializable
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

        public RemoveGameServerRequest() { }
        public RemoveGameServerRequest(GameServerID id, string key)
        {
            this.id = id;
            this.key = key;
        }
    }

    [Preserve]
    [Serializable]
    public class RemoveGameServerResult : INetworkSerializable
    {
        bool success;
        public bool Success => success;

        public void Select(INetworkSerializableResolver.Context context)
        {
            context.Select(ref success);
        }

        public RemoveGameServerResult() { }
        public RemoveGameServerResult(bool success)
        {
            this.success = success;
        }
    }
    #endregion
}