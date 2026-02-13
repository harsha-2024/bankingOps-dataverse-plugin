
using System;
using Microsoft.Xrm.Sdk;

namespace BankingOps.Plugin
{
    /// <summary>
    /// Implements a Custom API (new_GetFxQuote) that returns a simple FX rate
    /// Input parameters: Base (string), Counter (string)
    /// Output parameter: Rate (decimal)
    /// </summary>
    public sealed class GetFxQuoteCustomApi : PluginBase
    {
        public GetFxQuoteCustomApi(string unsecure, string secure) : base(unsecure, secure) {}

        protected override void Execute(ITracingService tracing, IPluginExecutionContext context, IOrganizationService service, IServiceProvider provider)
        {
            // Custom API passes parameters via InputParameters
            var @base = context.InputParameters.Contains("Base") ? context.InputParameters["Base"] as string : "USD";
            var counter = context.InputParameters.Contains("Counter") ? context.InputParameters["Counter"] as string : "EUR";

            var schema = "pp_StaticFx_" + @base + "_" + counter; // env var like pp_StaticFx_USD_EUR
            var rateStr = EnvConfig.GetString(service, schema, UnsecureConfig);
            if (!decimal.TryParse(rateStr, out var rate))
            {
                // fallback simple heuristic
                rate = (@base == counter) ? 1m : 0.9m;
            }

            context.OutputParameters["Rate"] = rate;
            tracing.Trace($"GetFxQuote {@base}/{counter} -> {rate}");
        }
    }
}
