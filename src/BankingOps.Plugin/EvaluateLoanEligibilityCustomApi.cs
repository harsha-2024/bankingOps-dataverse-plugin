
using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace BankingOps.Plugin
{
    /// <summary>
    /// Custom API: new_EvaluateLoanEligibility
    /// Inputs: CustomerId (Guid), ProductCode (String), RequestedAmount (Decimal)
    /// Outputs: Eligible (Boolean), Reasons (String)
    /// </summary>
    public sealed class EvaluateLoanEligibilityCustomApi : PluginBase
    {
        public EvaluateLoanEligibilityCustomApi(string unsecure, string secure) : base(unsecure, secure) {}
        protected override void Execute(ITracingService tracing, IPluginExecutionContext context, IOrganizationService service, IServiceProvider provider)
        {
            var reasons = new System.Collections.Generic.List<string>();
            if (!context.InputParameters.Contains("CustomerId") || !(context.InputParameters["CustomerId"] is Guid custId))
                throw new InvalidPluginExecutionException("CustomerId (Guid) is required.");
            var amount = context.InputParameters.Contains("RequestedAmount") && context.InputParameters["RequestedAmount"] is decimal d ? d : 0m;
            var cols = new ColumnSet(Schema.CustomerCreditScoreField, Schema.CustomerMonthlyIncomeField, Schema.CustomerCurrentExposureField);
            var acct = service.Retrieve(Schema.CustomerAccountEntity, custId, cols);
            var score = acct.GetAttributeValue<int?>(Schema.CustomerCreditScoreField) ?? 0;
            var income = acct.Contains(Schema.CustomerMonthlyIncomeField) && acct[Schema.CustomerMonthlyIncomeField] is Money mi ? mi.Value : 0m;
            var exposure = acct.Contains(Schema.CustomerCurrentExposureField) && acct[Schema.CustomerCurrentExposureField] is Money ex ? ex.Value : 0m;

            // Policy from environment variables with fallbacks
            var minScoreStr = EnvConfig.GetString(service, "pp_MinCreditScore", null);
            var maxDtiStr = EnvConfig.GetString(service, "pp_MaxDebtToIncome", null);
            var minScore = int.TryParse(minScoreStr, out var mcs) ? mcs : 650;
            var maxDti = decimal.TryParse(maxDtiStr, out var dti) ? dti : 0.45m; // 45%

            if (score < minScore) reasons.Add($"Credit score {score} below minimum {minScore}.");
            var dtiValue = income > 0m ? (exposure + amount) / income : 1m;
            if (dtiValue > maxDti) reasons.Add($"DTI {dtiValue:P0} exceeds {maxDti:P0}.");

            var eligible = reasons.Count == 0;
            context.OutputParameters["Eligible"] = eligible;
            context.OutputParameters["Reasons"] = string.Join("; ", reasons);
        }
    }
}
