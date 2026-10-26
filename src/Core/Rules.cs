using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Vexor.Core
{
    public sealed class FeatureSet
    {
        public bool HasCustomResolution;

        public bool IsEnabled(string name)
        {
            if (string.Equals(name, "has_custom_resolution", StringComparison.Ordinal)) return HasCustomResolution;
            return false;
        }
    }

    public sealed class Rule
    {
        public string Action = "allow";
        public string OsName;
        public string OsArch;
        public string OsVersionRegex;
        public string OsVersionMin;
        public string OsVersionMax;
        public Dictionary<string, bool> Features = new Dictionary<string, bool>(StringComparer.Ordinal);

        public static Rule Parse(JsonValue json)
        {
            var rule = new Rule();
            rule.Action = json["action"].AsString("allow");

            JsonValue os = json["os"];
            if (os.IsObject)
            {
                rule.OsName = os["name"].AsString();
                rule.OsArch = os["arch"].AsString();
                rule.OsVersionRegex = os["version"].AsString();
                JsonValue range = os["versionRange"];
                if (range.IsObject)
                {
                    rule.OsVersionMin = range["min"].AsString();
                    rule.OsVersionMax = range["max"].AsString();
                }
            }

            JsonValue features = json["features"];
            if (features.IsObject)
            {
                foreach (KeyValuePair<string, JsonValue> pair in features.Properties)
                    rule.Features[pair.Key] = pair.Value.AsBool();
            }
            return rule;
        }

        public bool Applies(FeatureSet features)
        {
            if (!string.IsNullOrEmpty(OsName) && !string.Equals(OsName, OsInfo.Name, StringComparison.OrdinalIgnoreCase)) return false;
            if (!string.IsNullOrEmpty(OsArch) && !string.Equals(OsArch, OsInfo.Arch, StringComparison.OrdinalIgnoreCase)) return false;

            if (!string.IsNullOrEmpty(OsVersionRegex))
            {
                try
                {
                    if (!Regex.IsMatch(OsInfo.VersionText, OsVersionRegex)) return false;
                }
                catch
                {
                    return false;
                }
            }

            if (!string.IsNullOrEmpty(OsVersionMin) || !string.IsNullOrEmpty(OsVersionMax))
            {
                if (!OsInfo.InVersionRange(OsVersionMin, OsVersionMax)) return false;
            }

            if (Features.Count > 0)
            {
                foreach (KeyValuePair<string, bool> pair in Features)
                {
                    bool actual = features != null && features.IsEnabled(pair.Key);
                    if (actual != pair.Value) return false;
                }
            }
            return true;
        }
    }

    public static class Rules
    {
        public static bool Allowed(List<Rule> rules, FeatureSet features)
        {
            if (rules == null || rules.Count == 0) return true;
            bool allowed = false;
            foreach (Rule rule in rules)
            {
                if (rule.Applies(features))
                    allowed = string.Equals(rule.Action, "allow", StringComparison.OrdinalIgnoreCase);
            }
            return allowed;
        }
    }

    public static class OsInfo
    {
        public static string Name
        {
            get { return "windows"; }
        }

        public static string Arch
        {
            get { return Environment.Is64BitOperatingSystem ? "x64" : "x86"; }
        }

        public static Version Version
        {
            get { return Environment.OSVersion.Version; }
        }

        public static string VersionText
        {
            get { return Version.ToString(); }
        }

        public static bool InVersionRange(string min, string max)
        {
            Version current = Version;
            if (!string.IsNullOrEmpty(min))
            {
                Version parsed;
                if (Version.TryParse(min, out parsed) && current < parsed) return false;
            }
            if (!string.IsNullOrEmpty(max))
            {
                Version parsed;
                if (Version.TryParse(max, out parsed) && current > parsed) return false;
            }
            return true;
        }
    }
}
