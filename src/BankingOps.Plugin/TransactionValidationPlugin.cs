
using System;
using System.Collections.Generic;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace BankingOps.Plugin
{
    public sealed class TransactionValidationPlugin : PluginBase
    {
        private static readonly HashSet<string> DefaultAllowedCurrencies = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {"USD","EUR","GBP","INR"};

        public TransactionValidationPlugin(string unsecure, string secure) : base(unsecure, secure) {}

        protected override void Execute(ITracingService tracing, IPluginExecutionContext context, IOrganizationService service, IServiceProvider provider)
        {
            if (!context.InputParameters.Contains("Target") || !(context.InputParameters["Target"] is Entity target)) return;
            if (!string.Equals(target.LogicalName, Schema.TransactionEntity, StringComparison.Ordinal)) return;

            var key = $"txn-validate:{context.PrimaryEntityId}:{context.MessageName}:{context.Stage}";
            if (Idempotency.WasProcessed(service, key)) { tracing.Trace($"Skip duplicate {key}"); return; }

            var amount = GetMoney(target, context, Schema.TransactionAmount);
            var currency = GetString(target, context, Schema.TransactionCurrency);
            var customer = GetRef(target, context, Schema.TransactionCustomer);

            if (amount <= 0) throw new InvalidPluginExecutionException("Transaction amount must be greater than zero.");

            // Allowed currencies from env var or unsecure config (CSV)
            var cfg = EnvConfig.GetString(service, "pp_AllowedCurrencies", UnsecureConfig);
            var allowed = new HashSet<string>(DefaultAllowedCurrencies, StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(cfg))
            {
                foreach (var c in cfg.Split(new[]{',',';','|' }, StringSplitOptions.RemoveEmptyEntries)) allowed.Add(c.Trim());
            }
            if (!allowed.Contains(currency)) throw new InvalidPluginExecutionException($"Currency '{currency}' is not allowed.");

            if (customer != null)
            {
                var cust = service.Retrieve(customer.LogicalName, customer.Id, new ColumnSet(Schema.CustomerKycField));
                var kyc = cust.Contains(Schema.CustomerKycField) ? (cust[Schema.CustomerKycField] as OptionSetValue)?.Value : (int?)null;
                if (kyc != 100000000) throw new InvalidPluginExecutionException("Customer KYC is not in a PASSED state.");
            }

            tracing.Trace($"Validation OK: {amount} {currency}");
            Idempotency.MarkProcessed(service, key, DateTime.UtcNow.AddHours(1));
        }

        private static decimal GetMoney(Entity target, IPluginExecutionContext ctx, string attr)
        {
            if (target.Attributes.ContainsKey(attr) && target[attr] is Money m) return m.Value;
            if (ctx.PreEntityImages != null && ctx.PreEntityImages.Contains("PreImageTxn"))
            { var pre = ctx.PreEntityImages["PreImageTxn"]; if (pre.Attributes.ContainsKey(attr) && pre[attr] is Money pm) return pm.Value; }
            return 0m;
        }
        private static string GetString(Entity target, IPluginExecutionContext ctx, string attr)
        {
            if (target.Attributes.ContainsKey(attr)) return target.GetAttributeValue<string>(attr);
            if (ctx.PreEntityImages != null && ctx.PreEntityImages.Contains("PreImageTxn"))
            { var pre = ctx.PreEntityImages["PreImageTxn"]; if (pre.Attributes.ContainsKey(attr)) return pre.GetAttributeValue<string>(attr); }
            return null;
        }
        private static EntityReference GetRef(Entity target, IPluginExecutionContext ctx, string attr)
        {
            if (target.Attributes.ContainsKey(attr)) return target.GetAttributeValue<EntityReference>(attr);
            if (ctx.PreEntityImages != null && ctx.PreEntityImages.Contains("PreImageTxn"))
            { var pre = ctx.PreEntityImages["PreImageTxn"]; if (pre.Attributes.ContainsKey(attr)) return pre.GetAttributeValue<EntityReference>(attr); }
            return null;
        }
    }
}
