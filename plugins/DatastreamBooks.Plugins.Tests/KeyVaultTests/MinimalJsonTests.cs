using DatastreamBooks.Plugins.KeyVault;
using FluentAssertions;
using Xunit;

namespace DatastreamBooks.Plugins.Tests.KeyVaultTests
{
    // Internal-but-tested: we keep this thin parser honest because the
    // entire plugin's credential-acquisition path depends on it correctly
    // extracting access_token and value fields from Azure responses.
    public class MinimalJsonTests
    {
        // --- access_token (typical Azure AD token response) ---
        [Fact]
        public void ExtractsAccessToken_FromTypicalTokenResponse()
        {
            var json = "{\"token_type\":\"Bearer\",\"expires_in\":3599,\"ext_expires_in\":3599,\"access_token\":\"eyJ0eXAi.payload.sig\"}";
            // Reflection trick: MinimalJson is internal, but the tests project shares the assembly's
            // friend-or-internal status via InternalsVisibleTo only if configured. We test via
            // the public KeyVaultSecretReader path or a public wrapper. Use Type.GetType.
            // Simpler: re-declare the extractor in tests is overkill — let's just use the public
            // KeyVaultSecretReader's behavior. But we want unit-level coverage of MinimalJson.
            // Solution: invoke MinimalJson via reflection.
            var token = InvokeExtractString(json, "access_token");
            token.Should().Be("eyJ0eXAi.payload.sig");
        }

        [Fact]
        public void ExtractsValue_FromTypicalKeyVaultSecretResponse()
        {
            var json = "{\"value\":\"Server=tcp:plasticrecycling.database.windows.net,1433;Initial Catalog=DatastreamBooks-Dev;User ID=dsb_app;Password=AbC123#\",\"id\":\"https://kv-datastream-books.vault.azure.net/secrets/dsb-app-connection-string/v1\",\"attributes\":{\"enabled\":true}}";
            var v = InvokeExtractString(json, "value");
            v.Should().Be("Server=tcp:plasticrecycling.database.windows.net,1433;Initial Catalog=DatastreamBooks-Dev;User ID=dsb_app;Password=AbC123#");
        }

        [Fact]
        public void ReturnsNull_WhenFieldMissing()
        {
            InvokeExtractString("{\"foo\":\"bar\"}", "missing").Should().BeNull();
        }

        [Fact]
        public void HandlesJsonEscapes()
        {
            // Backslash-escaped quote, newline, and unicode in a value.
            var json = "{\"value\":\"a\\\"b\\nc\\u0041\"}";
            var v = InvokeExtractString(json, "value");
            v.Should().Be("a\"b\ncA");
        }

        [Fact]
        public void HandlesWhitespaceAroundColon()
        {
            InvokeExtractString("{ \"value\" :  \"x\" }", "value").Should().Be("x");
        }

        [Fact]
        public void IsNotConfusedByFieldNameSubstring()
        {
            // "value_pretender" is NOT "value". We must not match the prefix.
            var json = "{\"value_pretender\":\"WRONG\",\"value\":\"RIGHT\"}";
            InvokeExtractString(json, "value").Should().Be("RIGHT");
        }

        [Fact]
        public void ExtractsInt_FromExpiresIn()
        {
            var n = InvokeExtractInt("{\"expires_in\":3599}", "expires_in");
            n.Should().Be(3599);
        }

        [Fact]
        public void ExtractsInt_Negative()
        {
            var n = InvokeExtractInt("{\"delta\":-42}", "delta");
            n.Should().Be(-42);
        }

        // --- reflection helpers (MinimalJson is internal) ---
        private static string InvokeExtractString(string json, string field)
        {
            var t = typeof(KeyVaultSecretReader).Assembly.GetType("DatastreamBooks.Plugins.KeyVault.MinimalJson");
            var m = t.GetMethod("ExtractStringField", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (string)m.Invoke(null, new object[] { json, field });
        }
        private static int? InvokeExtractInt(string json, string field)
        {
            var t = typeof(KeyVaultSecretReader).Assembly.GetType("DatastreamBooks.Plugins.KeyVault.MinimalJson");
            var m = t.GetMethod("ExtractIntField", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            return (int?)m.Invoke(null, new object[] { json, field });
        }
    }
}
