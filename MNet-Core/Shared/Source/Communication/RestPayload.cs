using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace MNet
{
    [Preserve]
    [Serializable]
    public struct CreateRoomRequest : INetworkSerializable
    {
        AppID appID;
        public AppID AppID => appID;

        Version version;
        public Version Version => version;

        string name;
        public string Name => name;

        RoomOptions options;
        public RoomOptions Options => options;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref appID);
            context.Select(ref version);

            context.Select(ref name);

            context.Select(ref options);
        }

        public CreateRoomRequest(AppID appID, Version version, string name, RoomOptions options)
        {
            this.appID = appID;
            this.version = version;

            this.name = name;

            this.options = options;
        }
    }

    #region Master Server Scheme
    [Preserve]
    [Serializable]
    public struct MasterServerSchemeRequest : INetworkSerializable
    {
        Version apiVersion;
        public Version ApiVersion => apiVersion;

        AppID appID;
        public AppID AppID => appID;

        Version gameVersion;
        public Version GameVersion => gameVersion;

        public void Select(ref NetworkSerializationContext context)
        {
            //Note to Self
            //Always Keep these in the same order to ensure backwards compatibility
            context.Select(ref apiVersion);
            //End of Note

            context.Select(ref appID);
            context.Select(ref gameVersion);
        }

        public MasterServerSchemeRequest(AppID appID, Version gameVersion) : this()
        {
            apiVersion = Constants.ApiVersion;
            this.appID = appID;
            this.gameVersion = gameVersion;
        }
    }

    [Preserve]
    [Serializable]
    public struct MasterServerSchemeResponse : INetworkSerializable
    {
        AppConfig app;
        public AppConfig App => app;

        RemoteConfig remoteConfig;
        public RemoteConfig RemoteConfig => remoteConfig;

        MasterServerInfoResponse info;
        public MasterServerInfoResponse Info => info;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref app);
            context.Select(ref remoteConfig);
            context.Select(ref info);
        }

        public MasterServerSchemeResponse(AppConfig app, RemoteConfig remoteConfig, MasterServerInfoResponse info)
        {
            this.app = app;
            this.remoteConfig = remoteConfig;
            this.info = info;
        }
    }
    #endregion

    #region Master Server Info
    [Preserve]
    [Serializable]
    public struct MasterServerInfoRequest : INetworkSerializable
    {
        public void Select(ref NetworkSerializationContext context) { }
    }

    [Preserve]
    [Serializable]
    public struct MasterServerInfoResponse : INetworkSerializable
    {
        GameServerInfo[] servers;
        public GameServerInfo[] Servers => servers;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref servers);
        }

        public MasterServerInfoResponse(GameServerInfo[] servers)
        {
            this.servers = servers;
        }
    }
    #endregion

    [Preserve]
    [Serializable]
    public struct GetLobbyInfoRequest : INetworkSerializable
    {
        AppID appID;
        public AppID AppID => appID;

        Version version;
        public Version Version => version;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref appID);
            context.Select(ref version);
        }

        public GetLobbyInfoRequest(AppID appID, Version version)
        {
            this.appID = appID;
            this.version = version;
        }
    }
}