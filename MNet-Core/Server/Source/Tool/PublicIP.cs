using System;
using System.Text;
using System.Collections.Generic;

using System.Net;
using System.Net.Http;

namespace MNet
{
    public static class PublicIP
    {
        public static string Host = "checkip.amazonaws.com";

        public static string URL = $"http://{Host}";

        public static IPAddress Retrieve() => Retrieve(4);
        public static IPAddress Retrieve(byte retries)
        {
            IPAddress value = null;

            for (int i = 1; i <= retries; i++)
            {
                try
                {
                    value = Fetch();
                }
                catch
                {
                    if (i < retries)
                        continue;

                    throw;
                }
            }

            return value;
        }

        static IPAddress Fetch()
        {
            var client = new HttpClient();

            HttpResponseMessage response;

            try
            {
                response = client.GetAsync(URL).Result;
            }
            catch
            {
                throw;
            }

            if (response.StatusCode != HttpStatusCode.OK) throw new Exception(response.ReasonPhrase);

            var text = response.Content.ReadAsStringAsync().Result;

            text = FormatText(text);

            if (IPAddress.TryParse(text, out var ip) == false) throw new Exception($"Cannot Parse '{text}' as an IP Address");

            return ip;
        }

        static string FormatText(string text)
        {
            text = text.Replace("\n", "");
            text = text.Replace("\r", "");
            text = text.Replace("\n\r", "");

            text = text.Split(',')[0];

            text = text.Trim();
            text = text.Trim('"');

            return text;
        }
    }
}