using System;
using System.Text;
using System.Collections.Generic;

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace MNet
{
    public static class PublicIP
    {
        public static string Host = "checkip.amazonaws.com";

        public static string URL = $"http://{Host}";

        public static Task<IPAddress> Retrieve() => Retrieve(4);
        public static async Task<IPAddress> Retrieve(byte retries)
        {
            IPAddress value = null;

            for (int i = 1; i <= retries; i++)
            {
                try
                {
                    value = await Fetch();
                }
                catch (HttpRequestException)
                {
                    if (i < retries)
                        continue;

                    throw;
                }
            }

            return value;
        }

        static async Task<IPAddress> Fetch()
        {
            var client = new HttpClient();

            var response = await client.GetAsync(URL);
            response.EnsureSuccessStatusCode();

            var text = await response.Content.ReadAsStringAsync();
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