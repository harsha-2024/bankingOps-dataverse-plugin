
using System;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace BankingOps.Plugin
{
    /// <summary>
    /// Reads environment variables (Dataverse: environmentvariabledefinition/environmentvariablevalue).
    /// Falls back to Unsecure/Secure config when not found.
    /// </summary>
    public static class EnvConfig
    {
        public static string GetString(IOrganizationService service, string schemaName, string fallback)
        {
            if (service == null || string.IsNullOrWhiteSpace(schemaName)) return fallback;

            // Look up EnvironmentVariableDefinition by schema name
            var defQuery = new QueryExpression("environmentvariabledefinition")
            {
                ColumnSet = new ColumnSet("environmentvariabledefinitionid")
            };
            defQuery.Criteria.AddCondition("schemaname", ConditionOperator.Equal, schemaName);

            var defs = service.RetrieveMultiple(defQuery);
            if (defs.Entities.Count == 0)
            {
                return fallback;
            }

            var defId = defs.Entities[0].Id;

            // Read current value (environmentvariablevalue)
            var valQuery = new QueryExpression("environmentvariablevalue")
            {
                ColumnSet = new ColumnSet("value"),
            };
            valQuery.Criteria.AddCondition("environmentvariabledefinitionid", ConditionOperator.Equal, defId);
            var vals = service.RetrieveMultiple(valQuery);
            if (vals.Entities.Count > 0 && vals.Entities[0].Contains("value"))
            {
                return (string)vals.Entities[0]["value"];
            }

            return fallback;
        }
    }
}
