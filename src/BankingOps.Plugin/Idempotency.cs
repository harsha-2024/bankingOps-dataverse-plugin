
using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace BankingOps.Plugin
{
    /// <summary>
    /// Simple idempotency guard using a lightweight custom table (logical name: new_plugincache)
    /// with columns: new_name (string, key), new_expirationon (datetime).
    /// </summary>
    public static class Idempotency
    {
        public static bool WasProcessed(IOrganizationService service, string key)
        {
            var query = new QueryExpression("new_plugincache")
            {
                ColumnSet = new ColumnSet("new_plugincacheid"),
                TopCount = 1
            };
            query.Criteria.AddCondition("new_name", ConditionOperator.Equal, key);
            var result = service.RetrieveMultiple(query);
            return result.Entities.Count > 0;
        }

        public static void MarkProcessed(IOrganizationService service, string key, DateTime? expiresOn = null)
        {
            var entity = new Entity("new_plugincache");
            entity["new_name"] = key;
            if (expiresOn.HasValue)
            {
                entity["new_expirationon"] = expiresOn.Value.ToUniversalTime();
            }
            service.Create(entity);
        }
    }
}
