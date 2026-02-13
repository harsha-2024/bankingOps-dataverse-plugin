
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;

namespace BankingOps.Plugin
{
    /// <summary>
    /// Enriches a transaction with a fraud score by calling an external API.
    /// Registered: new_banktransaction, Post-Operation, Async (depth must be checked).
    /// </summary>
    public sealed class FraudScoreEnrichmentPlugin : PluginBase
    {
        public FraudScoreEnrichmentPlugin(string unsecure, string secure) : base(unsecure, secure) {}

        protected override void Execute(ITracingService tracing, IPluginExecutionContext context, IOrganizationService service, IServiceProvider provider)
        {
            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity target))
                return;
            if (!string.Equals(target.LogicalName, "new_banktransaction", StringComparison.Ordinal))
                return;

            // Avoid infinite loops
            if (context.Depth > 1) { tracing.Trace("Skipping due to Depth>1"); return; }

            var txnId = context.PrimaryEntityId;
            var key = $"txn-fraudscore:{txnId}:{context.MessageName}:{context.Stage}";
            if (Idempotency.WasProcessed(service, key))
            {
                tracing.Trace($"Duplicate execution avoided for {key}");
                return;
            }

            // Gather data (minimal)
            var amount = target.Contains("new_amount") && target["new_amount"] is Money m ? m.Value : (decimal?)null;
            var currency = target.GetAttributeValue<string>("new_currency") ?? "USD";
            var customer = target.GetAttributeValue<EntityReference>("new_customerid");

            // Config resolution order: Env Var -> Secure Config -> Unsecure
            var apiUrl = EnvConfig.GetString(service, "pp_FraudApiUrl", UnsecureConfig);
            var apiKey = string.IsNullOrWhiteSpace(SecureConfig) ? null : SecureConfig;

            if (string.IsNullOrWhiteSpace(apiUrl))
            {
                tracing.Trace("Fraud API URL not configured; skipping enrichment.");
                return;
            }

            // Call external service
            var score = CallFraudApiAsync(tracing, apiUrl, apiKey, context.CorrelationId, amount ?? 0m, currency, customer?.Id.ToString()).GetAwaiter().GetResult();

            // Update record
            var update = new Entity("new_banktransaction") { Id = txnId };
            update["new_fraudscore"] = score;
            update["new_isfraudrisk"] = score >= 0.8m;
            service.Update(update);

            Idempotency.MarkProcessed(service, key, DateTime.UtcNow.AddHours(1));
            tracing.Trace($"Fraud score updated to {score}");
        }

        private static async Task<decimal> CallFraudApiAsync(ITracingService tracing, string apiUrl, string apiKey, Guid correlationId, decimal amount, string currency, string customerId)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("x-correlation-id", correlationId.ToString());
                if (!string.IsNullOrWhiteSpace(apiKey))
                {
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                }
                var payload = $"{{"amount":{amount},"currency":"{currency}","customerId":"{customerId}"}}";
                var req = new HttpRequestMessage(HttpMethod.Post, apiUrl)
                {
                    Content = new StringContent(payload, Encoding.UTF8, "application/json")
                };
                var resp = await RetryHttp.SendAsync(client, req, 3);
                var body = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode)
                {
                    tracing.Trace($"Fraud API failed: {(int)resp.StatusCode} {body}");
                    throw new InvalidPluginExecutionException("Fraud scoring failed");
                }
                // Expecting response like {"score":0.42}
                var idx = body.IndexOf(":" );
                var end = body.LastIndexOf('}');
                if (idx>0 && end>idx)
                {
                    var num = body.Substring(idx+1, end-(idx+1)).Trim();
                    if (decimal.TryParse(num, out var d)) return d;
                }
                return 0m;
            }
        }
    }
}
