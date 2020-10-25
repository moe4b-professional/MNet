using System;
using System.IO;

namespace MNet
{
    class Program
    {
        static void Main()
        {
            WriteVersion();
        }

        static void WriteVersion()
        {
            var version = Constants.ApiVersion;

            var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.Parent.Parent;

            var file = "Version.txt";

            var path = Path.Combine(directory.FullName, file);

            File.WriteAllText(path, $"v{version}");
        }
    }
}