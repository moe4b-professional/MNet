using System;
using System.Linq;
using System.Text;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using System.Threading.Tasks;

using System.IO;

using System.Net;
using System.Net.Http;

using WebSocketSharp;
using WebSocketSharp.Server;

using RestRequest = WebSocketSharp.Net.HttpListenerRequest;
using RestResponse = WebSocketSharp.Net.HttpListenerResponse;

using System.Security.Cryptography.X509Certificates;

namespace MNet
{
    public class RestClientAPI
    {
        public ushort Port { get; protected set; }

        public RestScheme Scheme { get; protected set; }

        public string IP { get; protected set; }
        public void SetIP(IPAddress address) => SetIP(address.ToString());
        public void SetIP(string value)
        {
            IP = value;
        }

        public string FormatURL(string ip, string path) => $"{Scheme}://{ip}:{Port}{path}";

        public HttpClient Client { get; protected set; }

        public delegate void ResponseDelegate<TResult>(TResult result, RestError error);

        #region GET
        public Task<TResult> GET<TResult>(string path) => GET<TResult>(IP, path);
        public async Task<TResult> GET<TResult>(string ip, string path)
        {
            var url = FormatURL(ip, path);

            var response = await Client.GetAsync(url);
            EnsureSuccess(response);

            var result = await Read<TResult>(response);

            return result;
        }
        #endregion

        #region POST
        public Task<TResult> POST<TRequest, TResult>(string path, TRequest payload) => POST<TRequest, TResult>(IP, path, payload);
        public async Task<TResult> POST<TRequest, TResult>(string ip, string path, TRequest payload)
        {
            var url = FormatURL(ip, path);

            var stream = NetworkWriter.Pool.Take();

            stream.Write(payload);
            var content = new ByteArrayContent(stream.Data, 0, stream.Position);

            var response = await Client.PostAsync(url, content);

            NetworkWriter.Pool.Return(stream);

            EnsureSuccess(response);

            var result = await Read<TResult>(response);

            return result;
        }
        #endregion

        public void CancelPendingRequests() => Client.CancelPendingRequests();

        public RestClientAPI(ushort port, RestScheme scheme)
        {
            this.Port = port;
            this.Scheme = scheme;

            Client = new HttpClient();
        }

        //Static Utility

        static void EnsureSuccess(HttpResponseMessage message)
        {
            if (message.IsSuccessStatusCode) return;

            var code = (RestStatusCode)message.StatusCode;

            throw new RestException(code, message.ReasonPhrase);
        }

        public static ByteArrayContent Write<T>(T payload)
        {
            var binary = NetworkSerializer.Serialize(payload);

            var content = new ByteArrayContent(binary);

            return content;
        }

        public static async Task<TPayload> Read<TPayload>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStreamAsync() as MemoryStream;

            using (NetworkStream.Pool.Lease(out var reader, out var writer))
            {
                writer.Insert(content);

                reader.Assign(writer);

                return reader.Read<TPayload>();
            }
        }
    }

    public static class RestServerAPI
    {
        public static HttpServer Server { get; private set; }

        public static class Router
        {
            public static Dictionary<string, ProcessDelegate> Dictionary { get; private set; }

            public delegate void ProcessDelegate(RestRequest request, RestResponse response);

            public static bool Process(RestRequest request, RestResponse response)
            {
                if (Dictionary.TryGetValue(request.RawUrl, out var callback) == false) return false;

                callback(request, response);
                return true;
            }

            public static void Register(string url, ProcessDelegate callback)
            {
                if (Dictionary.ContainsKey(url))
                    throw new ArgumentException($"URL {url} Already Registerd For Rest API Routing");

                Dictionary.Add(url, callback);
            }

            static Router()
            {
                Dictionary = new Dictionary<string, ProcessDelegate>();
            }
        }

        public static void Configure(ushort port)
        {
            Log.Info($"Configuring Rest API on Port:{port}");

            Server = new HttpServer(IPAddress.Any, port);

            Server.OnGet += RequestCallback;
            Server.OnPut += RequestCallback;
            Server.OnPost += RequestCallback;
            Server.OnOptions += RequestCallback;
        }

        public static void Start()
        {
            Log.Info($"Starting Rest API");

            Server.Start();
        }

        static void RequestCallback(object sender, HttpRequestEventArgs args)
        {
            var request = args.Request;
            var response = args.Response;

            Log.Info($"Rest API: {request.HttpMethod}:{request.Url.AbsolutePath} from {request.UserHostAddress}");

            response.AddHeader("Access-Control-Allow-Origin", "*");

            if (request.HttpMethod == "OPTIONS")
            {
                response.AddHeader("Access-Control-Allow-Headers", "*");
                response.AddHeader("Access-Control-Allow-Methods", "GET,PUT,POST");
                response.StatusCode = 200;
                return;
            }

            if (Router.Process(request, response) == false) Write(response, RestStatusCode.NotFound, "Error 404");
        }

        //Static Utility

        #region Write
        public static void Write(RestResponse response, RestStatusCode code) => Write(response, code, code.ToString());
        public static void Write(RestResponse response, RestStatusCode code, string message)
        {
            response.StatusCode = (int)code;
            response.StatusDescription = message;

            var length = Encoding.UTF8.GetByteCount(message);
            Span<byte> span = length > 1024 ? new byte[length] : stackalloc byte[length];
            length = Encoding.UTF8.GetBytes(message, span);

            response.ContentEncoding = Encoding.UTF8;
            response.WriteContent(span);
            
            response.Close();
        }

        public static void Write<TPayload>(RestResponse response, TPayload payload)
        {
            using (NetworkWriter.Pool.Lease(out var stream))
            {
                response.StatusCode = (int)HttpStatusCode.OK;

                stream.Write(payload);
                var span = stream.AsSpan();
                response.WriteContent(span);

                response.Close();
            }
        }
        #endregion

        #region Read
        public static bool TryRead<TPayload>(RestRequest request, RestResponse response, out TPayload payload)
        {
            using (NetworkStream.Pool.Lease(out var reader, out var writer))
            {
                writer.Insert(request.InputStream, (int)request.ContentLength64);
                reader.Assign(writer);

                try
                {
                    payload = reader.Read<TPayload>();
                }
                catch (Exception)
                {
                    Write(response, RestStatusCode.InvalidPayload, $"Error Reading Request");
                    payload = default;
                    return false;
                }

                return true;
            }
        }
        #endregion

        public static void WriteContent(this RestResponse response, Span<byte> span)
        {
            if (response == null)
                throw new ArgumentNullException("response");

            if (span == null)
                throw new ArgumentNullException("content");

            if (span.Length == 0)
            {
                response.Close();
                return;
            }

            response.ContentLength64 = span.Length;
            response.OutputStream.Write(span);

            try
            {
                response.OutputStream.Close();
            }
            catch (Exception)
            {
                Log.Warning($"Couldn't Close REST Response Stream");
                throw;
            }
        }
    }
}