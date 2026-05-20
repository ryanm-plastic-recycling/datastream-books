using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace DatastreamBooks.Plugins.KeyVault
{
    // Reads secrets from Azure Key Vault using a service-principal client
    // credential (tenant + appId + appSecret). Implemented over plain
    // HttpClient + raw OAuth + raw KV REST so the plugin assembly stays
    // free of Azure SDK package references — no ILRepack required.
    //
    // Why this exists:
    //   The Dataverse plugin sandbox loads only the single registered
    //   plugin assembly. Pulling in Azure.Identity and
    //   Azure.Security.KeyVault.Secrets would require ILRepacking all
    //   transitive dependencies (Azure.Core, System.Memory, etc.) into
    //   our signed DLL, which is a non-trivial build configuration this
    //   project has not yet set up. HttpClient + System.Net are BCL,
    //   already available in the sandbox.
    //
    // Sandbox network requirements:
    //   - Outbound HTTPS:443 to login.microsoftonline.com
    //   - Outbound HTTPS:443 to {vault}.vault.azure.net
    //   Dataverse Online sandbox allows both by default since 2019+.
    //
    // Caching:
    //   Both the OAuth bearer token and the per-secret value are cached
    //   in process-static fields with TTLs short enough that Key Vault
    //   rotations propagate quickly (5 minutes for secrets) but long
    //   enough that the typical plugin invocation does not pay the
    //   network cost.
    public static class KeyVaultSecretReader
    {
        // --- Singleton HttpClient — DNS caching is fine for this lifetime
        //     since vault hostnames don't change. ---
        private static readonly HttpClient Http = CreateHttpClient();

        private static HttpClient CreateHttpClient()
        {
            // Force TLS 1.2 — required by Azure AD and Key Vault. net462's
            // SecurityProtocolType enum does not yet include Tls13; the
            // negotiation falls back to whatever the underlying schannel
            // supports, which is fine for our endpoints.
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            var c = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(15),
            };
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            return c;
        }

        // --- Token cache (keyed by tenant + clientId; we expect one in practice). ---
        private static readonly object TokenLock = new object();
        private static string _cachedToken;
        private static DateTime _cachedTokenExpiresUtc = DateTime.MinValue;
        private static string _cachedTokenKey;

        // --- Secret cache (keyed by vault + name; expect one entry today). ---
        private static readonly object SecretLock = new object();
        private static readonly Dictionary<string, CachedSecret> _secretCache = new Dictionary<string, CachedSecret>();

        public static readonly TimeSpan SecretTtl = TimeSpan.FromMinutes(5);
        public static readonly TimeSpan TokenSafetyMargin = TimeSpan.FromMinutes(5);

        private class CachedSecret
        {
            public string Value;
            public DateTime FetchedAtUtc;
        }

        public static string GetSecret(
            string tenantId,
            string clientId,
            string clientSecret,
            string vaultUrl,
            string secretName)
        {
            if (string.IsNullOrWhiteSpace(tenantId)) throw new ArgumentException("tenantId is required", nameof(tenantId));
            if (string.IsNullOrWhiteSpace(clientId)) throw new ArgumentException("clientId is required", nameof(clientId));
            if (string.IsNullOrWhiteSpace(clientSecret)) throw new ArgumentException("clientSecret is required", nameof(clientSecret));
            if (string.IsNullOrWhiteSpace(vaultUrl)) throw new ArgumentException("vaultUrl is required", nameof(vaultUrl));
            if (string.IsNullOrWhiteSpace(secretName)) throw new ArgumentException("secretName is required", nameof(secretName));

            var vault = vaultUrl.TrimEnd('/');
            var cacheKey = vault + "|" + secretName;

            lock (SecretLock)
            {
                if (_secretCache.TryGetValue(cacheKey, out var cached)
                    && (DateTime.UtcNow - cached.FetchedAtUtc) < SecretTtl)
                {
                    return cached.Value;
                }
            }

            var token = GetAccessToken(tenantId, clientId, clientSecret);
            var value = FetchSecret(vault, secretName, token);

            lock (SecretLock)
            {
                _secretCache[cacheKey] = new CachedSecret { Value = value, FetchedAtUtc = DateTime.UtcNow };
            }
            return value;
        }

        // Test/admin hook — wipes both caches. Useful if a rotation
        // happens mid-process and we want the next call to re-fetch.
        public static void InvalidateCaches()
        {
            lock (TokenLock) { _cachedToken = null; _cachedTokenKey = null; _cachedTokenExpiresUtc = DateTime.MinValue; }
            lock (SecretLock) { _secretCache.Clear(); }
        }

        // ---------- OAuth client_credentials ----------
        private static string GetAccessToken(string tenantId, string clientId, string clientSecret)
        {
            var key = tenantId + "|" + clientId;
            lock (TokenLock)
            {
                if (_cachedToken != null
                    && _cachedTokenKey == key
                    && DateTime.UtcNow + TokenSafetyMargin < _cachedTokenExpiresUtc)
                {
                    return _cachedToken;
                }
            }

            var tokenUrl = "https://login.microsoftonline.com/" + Uri.EscapeDataString(tenantId) + "/oauth2/v2.0/token";
            var body =
                "client_id=" + Uri.EscapeDataString(clientId) +
                "&client_secret=" + Uri.EscapeDataString(clientSecret) +
                "&scope=" + Uri.EscapeDataString("https://vault.azure.net/.default") +
                "&grant_type=client_credentials";

            using (var req = new HttpRequestMessage(HttpMethod.Post, tokenUrl))
            {
                req.Content = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");
                HttpResponseMessage resp;
                try
                {
                    resp = Http.SendAsync(req).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    throw new KeyVaultSecretReaderException(
                        "Network failure acquiring Azure AD token from " + tokenUrl + ": " + ex.Message, ex);
                }
                using (resp)
                {
                    var payload = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!resp.IsSuccessStatusCode)
                    {
                        throw new KeyVaultSecretReaderException(
                            "Azure AD token endpoint returned " + (int)resp.StatusCode +
                            ": " + Truncate(payload, 400));
                    }

                    var token = MinimalJson.ExtractStringField(payload, "access_token");
                    var expiresIn = MinimalJson.ExtractIntField(payload, "expires_in") ?? 3600;
                    if (string.IsNullOrEmpty(token))
                    {
                        throw new KeyVaultSecretReaderException(
                            "Azure AD token endpoint response did not contain access_token. " +
                            "First 200 chars: " + Truncate(payload, 200));
                    }

                    lock (TokenLock)
                    {
                        _cachedToken = token;
                        _cachedTokenKey = key;
                        _cachedTokenExpiresUtc = DateTime.UtcNow.AddSeconds(expiresIn);
                    }
                    return token;
                }
            }
        }

        // ---------- Key Vault GET secret ----------
        private static string FetchSecret(string vaultUrl, string secretName, string accessToken)
        {
            // API version 7.4 is GA as of late 2023.
            var url = vaultUrl + "/secrets/" + Uri.EscapeDataString(secretName) + "?api-version=7.4";
            using (var req = new HttpRequestMessage(HttpMethod.Get, url))
            {
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                HttpResponseMessage resp;
                try
                {
                    resp = Http.SendAsync(req).GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    throw new KeyVaultSecretReaderException(
                        "Network failure calling " + url + ": " + ex.Message, ex);
                }
                using (resp)
                {
                    var payload = resp.Content.ReadAsStringAsync().GetAwaiter().GetResult();
                    if (!resp.IsSuccessStatusCode)
                    {
                        throw new KeyVaultSecretReaderException(
                            "Key Vault GET " + url + " returned " + (int)resp.StatusCode +
                            ": " + Truncate(payload, 400));
                    }
                    var value = MinimalJson.ExtractStringField(payload, "value");
                    if (value == null)
                    {
                        throw new KeyVaultSecretReaderException(
                            "Key Vault response did not contain 'value' field. First 200 chars: " +
                            Truncate(payload, 200));
                    }
                    return value;
                }
            }
        }

        private static string Truncate(string s, int max)
        {
            if (s == null) return null;
            return s.Length <= max ? s : s.Substring(0, max) + "...";
        }
    }

    public class KeyVaultSecretReaderException : Exception
    {
        public KeyVaultSecretReaderException(string message) : base(message) { }
        public KeyVaultSecretReaderException(string message, Exception inner) : base(message, inner) { }
    }
}
