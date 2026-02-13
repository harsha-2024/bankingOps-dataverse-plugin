
using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace BankingOps.Plugin
{
    public static class Idempotency
    {
        public static bool WasProcessed(IOrganizationService service, string key)
        {
            var query = new QueryExpression("bkg_plugincache") { ColumnSet = new ColumnSet(false), TopCount = 1 };
            query.Criteria.AddCondition("bkg_name", ConditionOperator.Equal, key);
            return service.RetrieveMultiple(query).Entities.Count > 0;
        }
        public static void MarkProcessed(IOrganizationService service, string key, DateTime? expiresOn = null)
        {
            var entity = new Entity("bkg_plugincache");
            entity["bkg_name"] = key;
            if (expiresOn.HasValue) entity["bkg_expirationon"] = expiresOn.Value.ToUniversalTime();
            service.Create(entity);
        }
    }
}
