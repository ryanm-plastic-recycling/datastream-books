using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;

namespace DatastreamBooks.Plugins.KeyVault
{
    // Reads Dataverse Environment Variables by schema name.
    //
    // The `environmentvariabledefinition` table holds the definition
    // (schemaname, defaultvalue) and the `environmentvariablevalue`
    // table holds the per-environment override. The override wins when
    // present; otherwise we fall back to the definition's default.
    //
    // *** All env vars consumed by Datastream Books plugins are plain
    // *** Text (type 100000000). The Secret-type path is intentionally
    // *** NOT supported here — see decision §63 in
    // *** docs/decisions/datastream-books-decisions.md. The sandbox
    // *** identity does not hold prvReadEnvironmentVariableSecretValue
    // *** even when impersonating the SYSTEM user via
    // *** OrgSvcFactory.CreateOrganizationService(null), so
    // *** RetrieveEnvironmentVariableSecretValue returns 0x80040256
    // *** Access Denied from a plugin context. Key Vault remains the
    // *** source of truth for the SP client secret; the deploy script
    // *** scripts/sync-sp-secret-to-dataverse.ps1 mirrors the current
    // *** KV value into rm_sqlkvclientsecret after each rotation.
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
            public const string KvClientSecret = "rm_sqlkvclientsecret";   // plain Text — see §63
            public const string KvUrl = "rm_sqlkvurl";
            public const string KvSecretName = "rm_sqlkvsecretname";
        }

        public static string GetValue(IOrganizationService svc, string schemaName)
        {
            if (svc == null) throw new ArgumentNullException(nameof(svc));
            if (string.IsNullOrWhiteSpace(schemaName)) throw new ArgumentException("schemaName is required", nameof(schemaName));

            var query = new QueryExpression(DefinitionEntity)
            {
                ColumnSet = new ColumnSet("schemaname", "defaultvalue"),
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
                    $"Dataverse Environment Variable '{schemaName}' not found.");
            }

            var def = results[0];

            // Override (environmentvariablevalue.value) wins over the definition's default.
            var aliased = def.GetAttributeValue<AliasedValue>("v.value");
            var overrideValue = aliased?.Value as string;
            if (!string.IsNullOrEmpty(overrideValue)) return overrideValue;

            var defaultValue = def.GetAttributeValue<string>("defaultvalue");
            if (!string.IsNullOrEmpty(defaultValue)) return defaultValue;

            throw new InvalidPluginExecutionException(
                $"Dataverse Environment Variable '{schemaName}' is defined but has neither a value " +
                $"nor a defaultvalue. Populate it via the maker portal or scripts/sync-sp-secret-to-dataverse.ps1.");
        }
    }
}
