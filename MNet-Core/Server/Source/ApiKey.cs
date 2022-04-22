using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace MNet
{
    public static class ApiKey
    {
        public static FixedString64 Token { get; private set; } = default;

        public const string Name = "API Key.txt";

        public const string Default = "You Should Probably Change This Bud";

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