
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xrm.Sdk;

namespace BankingOps.Plugin
{
    public sealed class FraudScoreEnrichmentPlugin : PluginBase
    {
        public FraudScoreEnrichmentPlugin(string unsecure, string secure) : base(unsecure, secure) {}

        protected override void Execute(ITracingService tracing, IPluginExecutionContext context, IOrganizationService service, IServiceProvider provider)
        {
            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity target)) return;
            if (!string.Equals(target.LogicalName, Schema.TransactionEntity, StringComparison.Ordinal)) return;
            if (context.Depth > 1) { tracing.Trace("Depth>1; skip"); return; }

            var txnId = context.PrimaryEntityId;
            var key = $"txn-fraudscore:{txnId}:{context.MessageName}:{context.Stage}";
            if (Idempotency.WasProcessed(service, key)) { tracing.Trace($"Skip duplicate {key}"); return; }

            var amount = target.Contains(Schema.TransactionAmount) && target[Schema.TransactionAmount] is Money m ? m.Value : 0m;
            var currency = target.GetAttributeValue<string>(Schema.TransactionCurrency) ?? "USD";
            var customer = target.GetAttributeValue<EntityReference>(Schema.TransactionCustomer);

            var apiUrl = EnvConfig.GetString(service, "pp_FraudApiUrl", UnsecureConfig);
            var apiKey = string.IsNullOrWhiteSpace(SecureConfig) ? null : SecureConfig;
            if (string.IsNullOrWhiteSpace(apiUrl)) { tracing.Trace("No Fraud API configured."); return; }

            var score = CallFraudApiAsync(tracing, apiUrl, apiKey, context.CorrelationId, amount, currency, customer?.Id.ToString()).GetAwaiter().GetResult();
            var update = new Entity(Schema.TransactionEntity) { Id = txnId };
            update[Schema.TransactionFraudScore] = score;
            update[Schema.TransactionIsFraud] = score >= 0.8m;
            service.Update(update);

            Idempotency.MarkProcessed(service, key, DateTime.UtcNow.AddHours(1));
        }

        private static async Task<decimal> CallFraudApiAsync(ITracingService tracing, string apiUrl, string apiKey, Guid correlationId, decimal amount, string currency, string customerId)
        {
            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("x-correlation-id", correlationId.ToString());
                if (!string.IsNullOrWhiteSpace(apiKey)) client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
                var payload = $"{{"amount":{amount},"currency":"{currency}","customerId":"{customerId}"}}";
                var resp = await new HttpClient().PostAsync(apiUrl, new StringContent(payload, Encoding.UTF8, "application/json"));
                var body = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode) throw new InvalidPluginExecutionException($"Fraud API failed: {(int)resp.StatusCode} {body}");
                var idx = body.IndexOf(":" ); var end = body.LastIndexOf('}');
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
