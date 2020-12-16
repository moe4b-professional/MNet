using System;
using System.Collections.Generic;
using System.Text;

namespace MNet
{
    #region Register
    [Preserve]
    [Serializable]
    public struct RegisterGameServerRequest : INetworkSerializable
    {
        GameServerInfo info;
        public GameServerInfo Info => info;

        public GameServerID ID => info.ID;
        public string Name => info.Name;
        public GameServerRegion Region => info.Region;

        string key;
        public string Key => key;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref info);
            context.Select(ref key);
        }

        public RegisterGameServerRequest(GameServerInfo info, string key)
        {
            this.info = info;
            this.key = key;
        }
    }

    [Preserve]
    [Serializable]
    public struct RegisterGameServerResponse : INetworkSerializable
    {
        AppConfig[] apps;
        public AppConfig[] Apps => apps;

        RemoteConfig remoteConfig;
        public RemoteConfig RemoteConfig => remoteConfig;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref apps);
            context.Select(ref remoteConfig);
        }

        public RegisterGameServerResponse(AppConfig[] apps, RemoteConfig remoteConfig)
        {
            this.apps = apps;
            this.remoteConfig = remoteConfig;
        }
    }
    #endregion

    #region Remove
    [Preserve]
    [Serializable]
    public struct RemoveGameServerRequest : INetworkSerializable
    {
        GameServerID id;
        public GameServerID ID => id;

        string key;
        public string Key => key;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref id);
            context.Select(ref key);
        }

        public RemoveGameServerRequest(GameServerID id, string key)
        {
            this.id = id;
            this.key = key;
        }
    }

    [Preserve]
    [Serializable]
    public struct RemoveGameServerResponse : INetworkSerializable
    {
        bool success;
        public bool Success => success;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref success);
        }

        public RemoveGameServerResponse(bool success)
        {
            this.success = success;
        }
    }
    #endregion
}