using MNet;

using System.Net;

new Thread(() =>
{
    Span<byte> span = stackalloc byte[8_192*2];
    Console.WriteLine("Allocated");
},1024).Start();

var server = new WebSocketServer(IPAddress.Any, 8000);
server.Start();

server.OnConnect += (client) =>
{
    Log.Info($"Client {client.ID} Connected");
};

server.OnMessage += (client, packet) =>
{
    Log.Info($"Client {client.ID} Message");
};

server.OnDisconnect += (client, code, message) =>
{
    Log.Info($"Client {client.ID} Disconnected, Code: {code}");
};

Console.ReadKey();