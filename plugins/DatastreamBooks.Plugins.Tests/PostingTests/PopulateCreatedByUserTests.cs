using DatastreamBooks.Plugins.Posting;
using FakeXrmEasy.Abstractions;
using FakeXrmEasy.Abstractions.Enums;
using FakeXrmEasy.Middleware;
using FakeXrmEasy.Middleware.Crud;
using FakeXrmEasy.Middleware.Messages;
using FakeXrmEasy.Plugins;
using FluentAssertions;
using Microsoft.Xrm.Sdk;
using System;
using Xunit;

namespace DatastreamBooks.Plugins.Tests.PostingTests
{
    public class PopulateCreatedByUserTests
    {
        private static readonly Guid InitiatingUserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        private static readonly Guid ExplicitUserId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        private static readonly Guid EntityId = Guid.Parse("66666666-6666-6666-6666-666666666666");

        private static IXrmFakedContext NewContext(Guid? callingUser = null)
        {
            var ctx = MiddlewareBuilder
                .New()
                .AddCrud()
                .AddFakeMessageExecutors()
                .UseCrud()
                .UseMessages()
                .SetLicense(FakeXrmEasyLicense.NonCommercial)
                .Build();
            ctx.CallerProperties.CallerId = new EntityReference("systemuser", callingUser ?? InitiatingUserId);
            return ctx;
        }

        [Fact]
        public void Create_WhenCreatedByUserIsNull_SetsToInitiatingUser()
        {
            var ctx = NewContext(InitiatingUserId);
            var entityRef = new Entity("rm_entity", EntityId);
            ctx.Initialize(new Entity[] { entityRef });

            var target = new Entity("rm_journalentry")
            {
                ["rm_entity"] = new EntityReference("rm_entity", EntityId),
                ["rm_journaldescription"] = "Test JE"
            };

            ExecuteCreate(ctx, target);

            var result = target.GetAttributeValue<EntityReference>("rm_createdby_user");
            result.Should().NotBeNull();
            result.LogicalName.Should().Be("systemuser");
            result.Id.Should().Be(InitiatingUserId);
        }

        [Fact]
        public void Create_WhenCreatedByUserAlreadySet_DoesNotOverwrite()
        {
            var ctx = NewContext(InitiatingUserId);
            var entityRef = new Entity("rm_entity", EntityId);
            ctx.Initialize(new Entity[] { entityRef });

            var target = new Entity("rm_journalentry")
            {
                ["rm_entity"] = new EntityReference("rm_entity", EntityId),
                ["rm_journaldescription"] = "Test JE",
                ["rm_createdby_user"] = new EntityReference("systemuser", ExplicitUserId)
            };

            ExecuteCreate(ctx, target);

            var result = target.GetAttributeValue<EntityReference>("rm_createdby_user");
            result.Id.Should().Be(ExplicitUserId);
        }

        private static void ExecuteCreate(IXrmFakedContext ctx, Entity target)
        {
            var pluginCtx = ctx.GetDefaultPluginContext();
            pluginCtx.MessageName = "Create";
            pluginCtx.Stage = 20;
            pluginCtx.PrimaryEntityName = "rm_journalentry";
            pluginCtx.InputParameters = new ParameterCollection { { "Target", target } };
            ctx.ExecutePluginWith<PostJournalEntryPlugin>(pluginCtx);
        }
    }
}