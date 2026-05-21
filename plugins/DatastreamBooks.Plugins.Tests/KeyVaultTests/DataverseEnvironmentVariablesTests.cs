using DatastreamBooks.Plugins.KeyVault;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Query;
using System;
using Xunit;

namespace DatastreamBooks.Plugins.Tests.KeyVaultTests
{
    public class DataverseEnvironmentVariablesTests
    {
        // --- Regression guard for decision §63.
        //
        // Background: 1.0.0.2/1.0.0.3 tried to resolve a Secret-type env var
        // via RetrieveEnvironmentVariableSecretValue and threw 0x80040256
        // Access Denied from the sandbox even after impersonating the SYSTEM
        // user. We pivoted to plain Text env vars + a deploy-script that
        // syncs the underlying KV secret into the Dataverse value. THIS test
        // exists so anyone "improving" GetValue by adding a Secret-type
        // branch tomorrow gets a loud failure today.

        [Fact]
        public void GetValue_NeverInvokesExecute_AndNeverIssuesRetrieveEnvironmentVariableSecretValue()
        {
            var fake = new CapturingService();
            var def = new Entity("environmentvariabledefinition")
            {
                ["schemaname"] = "rm_sqlkvclientsecret",
            };
            def["v.value"] = new AliasedValue("environmentvariablevalue", "value", "the-plain-text-value");
            fake.DefinitionToReturn = def;

            var result = DataverseEnvironmentVariables.GetValue(fake, "rm_sqlkvclientsecret");

            result.Should().Be("the-plain-text-value");
            fake.ExecuteCallCount.Should().Be(0,
                "GetValue must NOT invoke any custom Dataverse action. The Secret-type branch was " +
                "removed in decision §63; reintroducing it brings back the 0x80040256 sandbox failure.");
            fake.ExecutedRequestNames.Should().BeEmpty();
        }

        // --- Standard lookup behaviour ---

        [Fact]
        public void GetValue_ReturnsOverrideValue_FromValueTable()
        {
            var fake = new CapturingService();
            var def = new Entity("environmentvariabledefinition")
            {
                ["schemaname"] = "rm_sqlkvurl",
                ["defaultvalue"] = "https://default.example/",
            };
            def["v.value"] = new AliasedValue("environmentvariablevalue", "value", "https://override.example/");
            fake.DefinitionToReturn = def;

            var result = DataverseEnvironmentVariables.GetValue(fake, "rm_sqlkvurl");

            result.Should().Be("https://override.example/");
        }

        [Fact]
        public void GetValue_FallsBackToDefaultValue_WhenOverrideMissing()
        {
            var fake = new CapturingService();
            fake.DefinitionToReturn = new Entity("environmentvariabledefinition")
            {
                ["schemaname"] = "rm_sqlkvurl",
                ["defaultvalue"] = "https://default.example/",
            };

            var result = DataverseEnvironmentVariables.GetValue(fake, "rm_sqlkvurl");

            result.Should().Be("https://default.example/");
        }

        [Fact]
        public void GetValue_FallsBackToDefaultValue_WhenOverrideEmptyString()
        {
            var fake = new CapturingService();
            var def = new Entity("environmentvariabledefinition")
            {
                ["schemaname"] = "rm_sqlkvurl",
                ["defaultvalue"] = "https://default.example/",
            };
            def["v.value"] = new AliasedValue("environmentvariablevalue", "value", "");
            fake.DefinitionToReturn = def;

            var result = DataverseEnvironmentVariables.GetValue(fake, "rm_sqlkvurl");

            result.Should().Be("https://default.example/");
        }

        [Fact]
        public void GetValue_WhenNeitherValueNorDefault_Throws()
        {
            var fake = new CapturingService();
            fake.DefinitionToReturn = new Entity("environmentvariabledefinition")
            {
                ["schemaname"] = "rm_sqlkvurl",
            };

            Action act = () => DataverseEnvironmentVariables.GetValue(fake, "rm_sqlkvurl");

            act.Should().Throw<InvalidPluginExecutionException>()
                .WithMessage("*neither a value*nor a defaultvalue*");
        }

        [Fact]
        public void GetValue_WhenDefinitionMissing_Throws()
        {
            var fake = new CapturingService { DefinitionToReturn = null };

            Action act = () => DataverseEnvironmentVariables.GetValue(fake, "rm_does_not_exist");

            act.Should().Throw<InvalidPluginExecutionException>()
                .WithMessage("*not found*");
        }

        // --- Argument validation ---

        [Fact]
        public void GetValue_NullService_Throws()
        {
            Action act = () => DataverseEnvironmentVariables.GetValue(null, "rm_anything");
            act.Should().Throw<ArgumentNullException>();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public void GetValue_BlankSchemaName_Throws(string schemaName)
        {
            Action act = () => DataverseEnvironmentVariables.GetValue(new CapturingService(), schemaName);
            act.Should().Throw<ArgumentException>();
        }

        // Hand-rolled fake IOrganizationService — small enough that pulling in
        // FakeXrmEasy isn't worth the friction for these tests.
        private sealed class CapturingService : IOrganizationService
        {
            public Entity DefinitionToReturn { get; set; }
            public int ExecuteCallCount { get; private set; }
            public System.Collections.Generic.List<string> ExecutedRequestNames { get; } = new System.Collections.Generic.List<string>();

            public EntityCollection RetrieveMultiple(QueryBase query)
            {
                var col = new EntityCollection();
                if (DefinitionToReturn != null)
                {
                    col.Entities.Add(DefinitionToReturn);
                }
                return col;
            }

            public OrganizationResponse Execute(OrganizationRequest request)
            {
                ExecuteCallCount++;
                ExecutedRequestNames.Add(request?.RequestName);
                return new OrganizationResponse();
            }

            // Unused IOrganizationService surface — explicit-throw stubs.
            public Guid Create(Entity entity) => throw new NotImplementedException();
            public Entity Retrieve(string entityName, Guid id, ColumnSet columnSet) => throw new NotImplementedException();
            public void Update(Entity entity) => throw new NotImplementedException();
            public void Delete(string entityName, Guid id) => throw new NotImplementedException();
            public void Associate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotImplementedException();
            public void Disassociate(string entityName, Guid entityId, Relationship relationship, EntityReferenceCollection relatedEntities) => throw new NotImplementedException();
        }
    }
}
