using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace DatastreamBooks.Plugins.KeyVault
{
    // Reads Dataverse Environment Variables by schema name.
    //
    // The `environmentvariabledefinition` table holds the definition
    // (schemaname, type, defaultvalue) and the `environmentvariablevalue`
    // table holds the per-environment override. The override wins when
    // present; otherwise we fall back to the definition's default.
    //
    // For Secret-type env vars, Dataverse stores the value encrypted
    // and returns it decrypted to callers with the right privileges.
    // Plugins reading Secret env vars typically need to use the System
    // user context (via OrgSvcFactory.CreateOrganizationService(null)).
    public static class DataverseEnvironmentVariables
    {
        public const string DefinitionEntity = "environmentvariabledefinition";
        public const string ValueEntity = "environmentvariablevalue";

        // Schema names used by Datastream Books — central place so a typo
        // doesn't sneak into a string literal in plugin code.
        public static class Schema
        {
            public const string KvTenantId = "rm_sqlkvtenantid";
            public const string KvClientId = "rm_sqlkvclientid";
            public const string KvClientSecret = "rm_sqlkvclientsecret";   // Secret type
            public const string KvUrl = "rm_sqlkvurl";
            public const string KvSecretName = "rm_sqlkvsecretname";
        }

        public static string GetValue(IOrganizationService svc, string schemaName)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));
            if (string.IsNullOrWhiteSpace(schemaName)) throw new ArgumentException("schemaName is required", nameof(schemaName));

            var query = new QueryExpression(DefinitionEntity)
            {
                ColumnSet = new ColumnSet("schemaname", "defaultvalue", "type"),
                Criteria = new FilterExpression(),
                TopCount = 1,
            };
            query.Criteria.AddCondition("schemaname", ConditionOperator.Equal, schemaName);

            var link = query.AddLink(
                ValueEntity,
                "environmentvariabledefinitionid",
                "environmentvariabledefinitionid",
                JoinOperator.LeftOuter);
            link.EntityAlias = "v";
            link.Columns = new ColumnSet("value");

            var results = svc.RetrieveMultiple(query).Entities;
            if (results.Count == 0)
            {
                throw new InvalidPluginExecutionException(
                    $"Dataverse Environment Variable definition '{schemaName}' was not found. " +
                    $"It must be defined in the DatastreamBooks solution before the posting plugin can run.");
            }

            var def = results[0];
            var aliased = def.GetAttributeValue<AliasedValue>("v.value");
            var overrideValue = aliased?.Value as string;
            if (!string.IsNullOrEmpty(overrideValue)) return overrideValue;

            var defaultValue = def.GetAttributeValue<string>("defaultvalue");
            if (!string.IsNullOrEmpty(defaultValue)) return defaultValue;

            // Defined but no value — fail fast with a precise error.
            throw new InvalidPluginExecutionException(
                $"Dataverse Environment Variable '{schemaName}' is defined but has neither a value " +
                $"nor a defaultvalue. Populate it via the maker portal or the rotation deploy script.");
        }
    }
}
