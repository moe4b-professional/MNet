using System;
using System.Collections.Generic;
using System.Text;

namespace MNet
{
    static class AppsAPI
    {
        public static Dictionary<AppID, AppConfig> Apps { get; private set; }

        public static bool TryGet(AppID id, out AppConfig config) => Apps.TryGetValue(id, out config);

        public static void Set(AppConfig[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                Apps.Add(array[i].ID, array[i]);
            }
        }

        static AppsAPI()
        {
            Apps = new Dictionary<AppID, AppConfig>();
        }
    }
}