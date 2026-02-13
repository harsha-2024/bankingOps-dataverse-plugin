
using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace BankingOps.Plugin
{
    /// <summary>
    /// Validates a banking transaction before it is committed.
    /// Registered: new_banktransaction, Create/Update, Pre-Operation
    /// Uses Pre-Image on Update: PreImageTxn (amount, currency, customer)
    /// </summary>
    public sealed class TransactionValidationPlugin : PluginBase
    {
        private static readonly HashSet<string> DefaultAllowedCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {"USD","EUR","GBP","INR"};

        public TransactionValidationPlugin(string unsecure, string secure) : base(unsecure, secure) {}

        protected override void Execute(ITracingService tracing, IPluginExecutionContext context, IOrganizationService service, IServiceProvider provider)
        {
            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity target))
                return;

            if (!string.Equals(target.LogicalName, "new_banktransaction", StringComparison.Ordinal))
                return;

            // Build idempotency key (covers retries in async scenarios)
            var key = $"txn-validate:{context.PrimaryEntityId}:{context.MessageName}:{context.Stage}";
            if (Idempotency.WasProcessed(service, key))
            {
                tracing.Trace($"Skipping duplicate processing for key {key}");
                return;
            }

            var amount = GetMoney(target, context, tracing, "new_amount");
            var currency = GetString(target, context, tracing, "new_currency");
            var customerRef = GetRef(target, context, tracing, "new_customerid");

            if (amount <= 0)
            {
                throw new InvalidPluginExecutionException("Transaction amount must be greater than zero.");
            }

            // Allowed currencies from env var or unsecure config (comma-separated)
            var cfg = EnvConfig.GetString(service, "pp_AllowedCurrencies", UnsecureConfig);
            var allowed = new HashSet<string>(DefaultAllowedCurrencies, StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(cfg))
            {
                foreach (var c in cfg.Split(new[]{',',';','|' }, StringSplitOptions.RemoveEmptyEntries))
                    allowed.Add(c.Trim());
            }
            if (!allowed.Contains(currency))
            {
                throw new InvalidPluginExecutionException($"Currency '{currency}' is not allowed for transactions.");
            }

            // Check KYC status on customer (account/contact) minimal example
            if (customerRef != null)
            {
                var cust = service.Retrieve(customerRef.LogicalName, customerRef.Id, new ColumnSet("new_kycstatus"));
                var kyc = cust.Contains("new_kycstatus") ? (cust["new_kycstatus"] as OptionSetValue)?.Value : (int?)null;
                // assuming 100000000 = Passed
                if (kyc != 100000000)
                {
                    throw new InvalidPluginExecutionException("Customer KYC is not in a PASSED state.");
                }
            }

            tracing.Trace($"Transaction validation passed. Amount={amount} {currency}, Customer={(customerRef==null?"<none>":customerRef.Id.ToString())}");
            Idempotency.MarkProcessed(service, key, DateTime.UtcNow.AddHours(1));
        }

        private static decimal GetMoney(Entity target, IPluginExecutionContext ctx, ITracingService tracing, string attr)
        {
            if (target.Attributes.ContainsKey(attr) && target[attr] is Money m)
                return m.Value;
            // Try pre image on update
            if (ctx.PreEntityImages != null && ctx.PreEntityImages.Contains("PreImageTxn"))
            {
                var pre = ctx.PreEntityImages["PreImageTxn"];
                if (pre.Attributes.ContainsKey(attr) && pre[attr] is Money pm)
                    return pm.Value;
            }
            return 0m;
        }

        private static string GetString(Entity target, IPluginExecutionContext ctx, ITracingService tracing, string attr)
        {
            if (target.Attributes.ContainsKey(attr))
                return target.GetAttributeValue<string>(attr);
            if (ctx.PreEntityImages != null && ctx.PreEntityImages.Contains("PreImageTxn"))
            {
                var pre = ctx.PreEntityImages["PreImageTxn"];
                if (pre.Attributes.ContainsKey(attr))
                    return pre.GetAttributeValue<string>(attr);
            }
            return null;
        }

        private static EntityReference GetRef(Entity target, IPluginExecutionContext ctx, ITracingService tracing, string attr)
        {
            if (target.Attributes.ContainsKey(attr))
                return target.GetAttributeValue<EntityReference>(attr);
            if (ctx.PreEntityImages != null && ctx.PreEntityImages.Contains("PreImageTxn"))
            {
                var pre = ctx.PreEntityImages["PreImageTxn"];
                if (pre.Attributes.ContainsKey(attr))
                    return pre.GetAttributeValue<EntityReference>(attr);
            }
            return null;
        }
    }
}
