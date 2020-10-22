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