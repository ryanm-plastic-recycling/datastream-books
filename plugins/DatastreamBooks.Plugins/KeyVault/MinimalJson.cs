using System;
using System.Globalization;
using System.Text;

namespace DatastreamBooks.Plugins.KeyVault
{
    // Tiny single-field JSON extractor. Avoids pulling Newtonsoft.Json
    // (which isn't safely present in the plugin sandbox without ILRepack)
    // and DataContractJsonSerializer (which requires DataContract wiring).
    //
    // Limitations (acceptable for our two known response shapes):
    //   - Extracts a single top-level string field at a time (linear scan).
    //   - Does not validate the rest of the document.
    //   - Returns null if the field is not present or not a string.
    //   - Handles standard JSON string escapes including \uXXXX.
    //
    // Anywhere we need richer JSON parsing in the future, replace this
    // with System.Text.Json (via ILRepack) or a properly-tested parser.
    internal static class MinimalJson
    {
        public static string ExtractStringField(string json, string fieldName)
        {
            int valueStart = FindFieldValueStart(json, fieldName);
            if (valueStart < 0) return null;
            if (valueStart >= json.Length || json[valueStart] != '"') return null;
            return ReadJsonString(json, valueStart + 1, out _);
        }

        public static int? ExtractIntField(string json, string fieldName)
        {
            int p = FindFieldValueStart(json, fieldName);
            if (p < 0) return null;
            int start = p;
            int end = p;
            while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '-')) end++;
            if (end == start) return null;
            if (int.TryParse(json.Substring(start, end - start), NumberStyles.Integer, CultureInfo.InvariantCulture, out var v))
                return v;
            return null;
        }

        // --- internals ---

        // Finds the position of the value for "fieldName": ...
        // Returns the index of the first non-whitespace character after
        // the colon, or -1 if not found.
        private static int FindFieldValueStart(string json, string fieldName)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(fieldName)) return -1;
            var needle = "\"" + fieldName + "\"";
            int idx = 0;
            while (true)
            {
                int found = json.IndexOf(needle, idx, StringComparison.Ordinal);
                if (found < 0) return -1;
                // Skip past the closing quote
                int p = found + needle.Length;
                // Skip whitespace
                while (p < json.Length && char.IsWhiteSpace(json[p])) p++;
                if (p < json.Length && json[p] == ':')
                {
                    p++;
                    while (p < json.Length && char.IsWhiteSpace(json[p])) p++;
                    return p;
                }
                idx = found + 1;
            }
        }

        // Reads a JSON string starting at `start` (the char AFTER the opening
        // quote). Handles standard escapes. Returns the decoded string and
        // sets `endOut` to the position after the closing quote.
        private static string ReadJsonString(string json, int start, out int endOut)
        {
            var sb = new StringBuilder();
            int p = start;
            while (p < json.Length)
            {
                char c = json[p++];
                if (c == '"') { endOut = p; return sb.ToString(); }
                if (c == '\\')
                {
                    if (p >= json.Length) break;
                    char esc = json[p++];
                    switch (esc)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (p + 4 > json.Length) { endOut = -1; return null; }
                            var hex = json.Substring(p, 4);
                            p += 4;
                            if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var cp))
                            { endOut = -1; return null; }
                            sb.Append((char)cp);
                            break;
                        default:
                            // Unknown escape — leave as-is and continue.
                            sb.Append(esc);
                            break;
                    }
                }
                else
                {
                    sb.Append(c);
                }
            }
            endOut = -1;
            return null; // unterminated string
        }
    }
}
