
using System;
using Microsoft.Xrm.Sdk;

namespace BankingOps.Plugin
{
    public sealed class GetFxQuoteCustomApi : PluginBase
    {
        public GetFxQuoteCustomApi(string unsecure, string secure) : base(unsecure, secure) {}
        protected override void Execute(ITracingService tracing, IPluginExecutionContext context, IOrganizationService service, IServiceProvider provider)
        {
            var @base = context.InputParameters.Contains("Base") ? context.InputParameters["Base"] as string : "USD";
            var counter = context.InputParameters.Contains("Counter") ? context.InputParameters["Counter"] as string : "EUR";
            var schema = "pp_StaticFx_" + @base + "_" + counter;
            var rateStr = EnvConfig.GetString(service, schema, UnsecureConfig);
            if (!decimal.TryParse(rateStr, out var rate)) rate = (@base == counter) ? 1m : 0.9m;
            context.OutputParameters["Rate"] = rate;
        }
    }
}
