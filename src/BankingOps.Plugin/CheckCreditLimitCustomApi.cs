
using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace BankingOps.Plugin
{
    /// <summary>
    /// Custom API: new_CheckCreditLimit
    /// Inputs: CustomerId (Guid), RequestedAmount (Decimal)
    /// Outputs: IsWithinLimit (Boolean), AvailableLimit (Decimal)
    /// </summary>
    public sealed class CheckCreditLimitCustomApi : PluginBase
    {
        public CheckCreditLimitCustomApi(string unsecure, string secure) : base(unsecure, secure) {}
        protected override void Execute(ITracingService tracing, IPluginExecutionContext context, IOrganizationService service, IServiceProvider provider)
        {
            var hasCustomer = context.InputParameters.Contains("CustomerId") && context.InputParameters["CustomerId"] is Guid;
            var hasAmount = context.InputParameters.Contains("RequestedAmount") && context.InputParameters["RequestedAmount"] is decimal;
            if (!hasCustomer || !hasAmount) throw new InvalidPluginExecutionException("CustomerId (Guid) and RequestedAmount (Decimal) are required.");

            var customerId = (Guid)context.InputParameters["CustomerId"];
            var requested = (decimal)context.InputParameters["RequestedAmount"];

            var cols = new ColumnSet(Schema.CustomerCreditLimitField, Schema.CustomerCurrentExposureField);
            var acct = service.Retrieve(Schema.CustomerAccountEntity, customerId, cols);
            var limit = acct.Contains(Schema.CustomerCreditLimitField) && acct[Schema.CustomerCreditLimitField] is Money lm ? lm.Value : 0m;
            var exposure = acct.Contains(Schema.CustomerCurrentExposureField) && acct[Schema.CustomerCurrentExposureField] is Money ex ? ex.Value : 0m;

            var available = Math.Max(0m, limit - exposure);
            context.OutputParameters["AvailableLimit"] = available;
            context.OutputParameters["IsWithinLimit"] = requested <= available;
        }
    }
}
