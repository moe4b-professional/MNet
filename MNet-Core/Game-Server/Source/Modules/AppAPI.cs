using System;
using System.Text;
using System.Collections.Generic;

namespace MNet
{
    static class AppsAPI
    {
        public static Dictionary<AppID, AppConfig> Dictionary { get; private set; }

        public static bool TryGet(AppID id, out AppConfig config) => Dictionary.TryGetValue(id, out config);

        public static void Set(AppConfig[] array)
        {
            for (int i = 0; i < array.Length; i++)
                Dictionary[array[i].ID] = array[i];
        }

        public static void Clear()
        {
            Dictionary.Clear();
        }

        static AppsAPI()
        {
            Dictionary = new Dictionary<AppID, AppConfig>();
        }
    }
}