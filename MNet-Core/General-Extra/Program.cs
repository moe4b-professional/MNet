using MNet;

using System;
using System.IO;

WriteVersion();

#if DEBUG
Sandbox.Execute();
#else
Benchmarks.Execute();
#endif

static void WriteVersion()
{
    var version = Constants.ApiVersion;

    var directory = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory).Parent.Parent.Parent.Parent.Parent;

    var file = "Version.txt";

    var path = Path.Combine(directory.FullName, file);

    File.WriteAllText(path, $"v{version}");
}