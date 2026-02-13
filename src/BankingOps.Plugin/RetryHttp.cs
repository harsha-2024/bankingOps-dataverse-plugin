
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace BankingOps.Plugin
{
    public static class RetryHttp
    {
        public static async Task<HttpResponseMessage> SendAsync(HttpClient client, HttpRequestMessage request, int maxAttempts = 3)
        {
            var delay = 500; // ms
            Exception last = null;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    var resp = await client.SendAsync(request);
                    if ((int)resp.StatusCode >= 500)
                    {
                        // transient
                        last = new Exception($"Server error: {(int)resp.StatusCode}");
                    }
                    else
                    {
                        return resp;
                    }
                }
                catch (Exception ex)
                {
                    last = ex;
                }
                await Task.Delay(delay);
                delay *= 2;
            }
            throw last ?? new Exception("HTTP retry failed");
        }
    }
}
