using UnityEngine;

using Object = UnityEngine.Object;
using Random = UnityEngine.Random;
using Debug = UnityEngine.Debug;

using Cysharp.Threading.Tasks;

namespace MNet.Example
{
	public class AutoJoin : MonoBehaviour
	{
        static bool tried;

        Core Core => Core.Instance;

        void Start()
        {
            if (tried)
                return;

            tried = true;

            Process().Forget();
        }

        async UniTask Process()
        {
            //Register Server
            {
                var scheme = await NetworkAPI.Server.Master.GetScheme();
                var info = scheme.Info;

                if (info.Servers.Length == 0)
                    return;

                NetworkAPI.Server.Game.Select(info.Servers[0]);
            }

            //Query Lobby
            {
                var info = await NetworkAPI.Lobby.GetInfo();

                if (info.Rooms == null)
                    return;

                for (int i = 0; i < info.Rooms.Count; i++)
                {
                    if (info.Rooms[i].Occupancy == info.Rooms[i].Capacity)
                        continue;

                    if (info.Rooms[i].Locked)
                        continue;

                    NetworkAPI.Room.Join(info.Rooms[i].ID, Core.Network.Profile);
                }
            }
        }
    }
}