
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;

namespace BankingOps.Plugin
{
    public static class EnvConfig
    {
        public static string GetString(IOrganizationService service, string schemaName, string fallback)
        {
            if (service == null || string.IsNullOrWhiteSpace(schemaName)) return fallback;
            var defQuery = new QueryExpression("environmentvariabledefinition")
            { ColumnSet = new ColumnSet("environmentvariabledefinitionid") };
            defQuery.Criteria.AddCondition("schemaname", ConditionOperator.Equal, schemaName);
            var defs = service.RetrieveMultiple(defQuery);
            if (defs.Entities.Count == 0) return fallback;
            var defId = defs.Entities[0].Id;
            var valQuery = new QueryExpression("environmentvariablevalue")
            { ColumnSet = new ColumnSet("value") };
            valQuery.Criteria.AddCondition("environmentvariabledefinitionid", ConditionOperator.Equal, defId);
            var vals = service.RetrieveMultiple(valQuery);
            if (vals.Entities.Count > 0 && vals.Entities[0].Contains("value"))
                return (string)vals.Entities[0]["value"];
            return fallback;
        }
    }
}
