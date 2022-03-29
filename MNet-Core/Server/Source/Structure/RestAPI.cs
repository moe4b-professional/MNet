using System;
using System.Linq;
using System.Text;

using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using RestRequest = System.Net.HttpListenerRequest;
using RestResponse = System.Net.HttpListenerResponse;

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

            var result = Read<TResult>(response);

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

            var result = Read<TResult>(response);

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

        public static TPayload Read<TPayload>(HttpResponseMessage response)
        {
            var stream = response.Content.ReadAsStream();

            using (NetworkStream.Pool.Lease(out var reader, out var writer))
            {
                writer.Insert(stream);

                reader.Assign(writer);

                return reader.Read<TPayload>();
            }
        }
    }

    public static class RestServerAPI
    {
        static HttpListener Listener;

        static Dictionary<string, SynchronousDelegate> SynchronusDictionary;
        public delegate void SynchronousDelegate(HttpListenerContext context);
        public class SynchronusJob
        {
            public HttpListenerContext Context;

            public SynchronousDelegate Action;

            public readonly WaitCallback Callback;
            public void Execute(object state)
            {
                Action(Context);

                Context = null;

                ObjectPool<SynchronusJob>.Return(this);
            }

            public SynchronusJob()
            {
                Callback = new WaitCallback(Execute);
            }

            public static SynchronusJob Lease() => ObjectPool<SynchronusJob>.Lease();
        }

        static Dictionary<string, AsynchronousDelegate> AsynchronusDictionary;
        public delegate Task AsynchronousDelegate(HttpListenerContext context);
        public class AsynchronusJob
        {
            public HttpListenerContext Context;

            public AsynchronousDelegate Action;

            public readonly WaitCallback Callback;
            public void Execute(object state)
            {
                Procedure();
                async void Procedure()
                {
                    await Action(Context);

                    Context = default;

                    ObjectPool<AsynchronusJob>.Return(this);
                }
            }

            public AsynchronusJob()
            {
                Callback = new WaitCallback(Execute);
            }

            public static AsynchronusJob Lease() => ObjectPool<AsynchronusJob>.Lease();
        }

        static Thread Thread;

        public static void Configure(ushort port)
        {
            if (Listener != null && Listener.IsListening)
                throw new InvalidDataException();

            Log.Info($"Configuring Rest API on Port:{port}");

            Listener = new HttpListener();

            Listener.Prefixes.Add($"http://+:{port}/");

            SynchronusDictionary = new Dictionary<string, SynchronousDelegate>();
            AsynchronusDictionary = new Dictionary<string, AsynchronousDelegate>();
        }

        public static void Start()
        {
            if (Listener.IsListening)
                throw new InvalidDataException();

            Log.Info($"Starting Rest API");

            Listener.Start();

            Thread = new Thread(Poll);
            Thread.Start();
        }

        static void Poll()
        {
            while (true)
            {
                HttpListenerContext context;

                context = Listener.GetContext();

                RegisterHttpRequest(context);
            }
        }

        static void RegisterHttpRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            Log.Info($"Rest API: {request.HttpMethod}:{request.Url.AbsolutePath} from {request.UserHostAddress}");

            response.AddHeader("Access-Control-Allow-Origin", "*");

            if (request.HttpMethod == "OPTIONS")
            {
                response.AddHeader("Access-Control-Allow-Headers", "*");
                response.AddHeader("Access-Control-Allow-Methods", "GET,PUT,POST");
                response.StatusCode = 200;
            }
            else if (SynchronusDictionary.TryGetValue(context.Request.RawUrl, out var sync))
            {
                var job = SynchronusJob.Lease();

                job.Action = sync;
                job.Context = context;

                ThreadPool.UnsafeQueueUserWorkItem(job.Callback, false);

                return;
            }
            else if (AsynchronusDictionary.TryGetValue(context.Request.RawUrl, out var async))
            {
                var job = AsynchronusJob.Lease();

                job.Action = async;
                job.Context = context;

                ThreadPool.UnsafeQueueUserWorkItem(job.Callback, false);

                return;
            }
            else
            {
                context.Response.StatusCode = 400;
                context.Response.Close();
            }
        }

        public static void Register(string text, SynchronousDelegate action)
        {
            SynchronusDictionary.Add(text, action);
        }
        public static void Register(string text, AsynchronousDelegate action)
        {
            AsynchronusDictionary.Add(text, action);
        }

        //Utility

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
    }
}