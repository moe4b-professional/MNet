using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MNet
{
    [Preserve]
    public enum NetworkSceneLoadMode : byte
    {
        Single = 0,
        Additive = 1
    }

    [Preserve]
    public struct LoadScenesRequest : INetworkSerializable
    {
        byte[] scenes;
        public byte[] Scenes => scenes;

        NetworkSceneLoadMode mode;
        public NetworkSceneLoadMode Mode => mode;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref scenes);
            context.Select(ref mode);
        }

        public LoadScenesRequest(byte[] scenes, NetworkSceneLoadMode mode)
        {
            this.scenes = scenes;
            this.mode = mode;
        }
    }

    [Preserve]
    public struct LoadScenesCommand : INetworkSerializable
    {
        byte[] scenes;
        public byte[] Scenes => scenes;

        NetworkSceneLoadMode mode;
        public NetworkSceneLoadMode Mode => mode;

        public void Select(ref NetworkSerializationContext context)
        {
            context.Select(ref scenes);
            context.Select(ref mode);
        }

        public LoadScenesCommand(byte[] scenes, NetworkSceneLoadMode mode)
        {
            this.scenes = scenes;
            this.mode = mode;
        }

        public static LoadScenesCommand Write(LoadScenesRequest request) => new LoadScenesCommand(request.Scenes, request.Mode);
    }
}