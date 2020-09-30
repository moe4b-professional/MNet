using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Backend
{
    public static class ApiKey
    {
        public static string Token { get; private set; } = string.Empty;

        public const string Name = "API Key.txt";

        public const string Default = "XOXOXOXOXOXOXOXOXOXOXOXOXOXOXOXOXO";

        public static void Read()
        {
            if (File.Exists(Name) == false)
            {
                Log.Info($"No {Name} File Found, Using Default API Key");
                Token = Default;
                return;
            }

            Log.Info($"Successfully Read API Key");
            Token = File.ReadAllText(Name);
        }
    }
}