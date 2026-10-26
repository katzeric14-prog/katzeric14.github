using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vexor.Core
{
    public sealed class VersionEntry
    {
        public string Id;
        public string Type;
        public string Url;
        public string Sha1;
        public DateTime ReleaseTime;
        public string DisplayName;
        public bool IsModpack;

        public string DisplayNameOrId
        {
            get { return string.IsNullOrEmpty(DisplayName) ? Id : DisplayName; }
        }

        public string ReleaseDateText
        {
            get
            {
                if (ReleaseTime == DateTime.MinValue) return "—";
                return ReleaseTime.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
            }
        }

        public string TypeLabel
        {
            get
            {
                string label;
                switch (Type)
                {
                    case "release": label = "Релиз"; break;
                    case "snapshot": label = "Снапшот"; break;
                    case "old_beta": label = "Бета"; break;
                    case "old_alpha": label = "Альфа"; break;
                    case "fabric": label = "Fabric"; break;
                    case "forge": label = "Forge"; break;
                    case "neoforge": label = "NeoForge"; break;
                    default: label = string.IsNullOrEmpty(Type) ? "Версия" : Type; break;
                }
                return IsModpack ? "Модпак · " + label : label;
            }
        }
    }

    public sealed class VersionManifest
    {
        public string LatestRelease = "";
        public string LatestSnapshot = "";
        public System.Collections.Generic.List<VersionEntry> Versions =
            new System.Collections.Generic.List<VersionEntry>();
    }

    public sealed class AssetIndexInfo
    {
        public string Id;
        public string Url;
        public string Sha1;
        public long Size;
    }

    public sealed class DownloadInfo
    {
        public string Url;
        public string Sha1;
        public string Path;
        public long Size;

        public static DownloadInfo Parse(JsonValue json)
        {
            if (!json.IsObject) return null;
            var info = new DownloadInfo();
            info.Url = json["url"].AsString();
            info.Sha1 = json["sha1"].AsString();
            info.Path = json["path"].AsString();
            info.Size = json["size"].AsLong();
            if (string.IsNullOrEmpty(info.Url)) return null;
            return info;
        }
    }

    public sealed class LibraryInfo
    {
        public string Name;
        public string Url;
        public string Sha1;
        public List<Rule> Rules = new List<Rule>();
        public DownloadInfo Artifact;
        public Dictionary<string, DownloadInfo> Classifiers = new Dictionary<string, DownloadInfo>(StringComparer.Ordinal);
        public Dictionary<string, string> Natives = new Dictionary<string, string>(StringComparer.Ordinal);
        public List<string> ExtractExclude = new List<string>();

        public bool HasNatives
        {
            get { return Natives.Count > 0; }
        }
    }

    public sealed class ArgEntry
    {
        public List<Rule> Rules = new List<Rule>();
        public List<string> Values = new List<string>();
    }

    public sealed class GameVersion
    {
        public string Id;
        public string Type;
        public string MainClass;
        public string Assets;
        public string InheritsFrom;
        public int JavaMajor;
        public AssetIndexInfo AssetIndex;
        public DownloadInfo Client;
        public string LegacyGameArgs;
        public List<LibraryInfo> Libraries = new List<LibraryInfo>();
        public List<ArgEntry> JvmArgs = new List<ArgEntry>();
        public List<ArgEntry> GameArgs = new List<ArgEntry>();
    }

    public static class GamePaths
    {
        public static string VersionsDir(string gameDir) { return Path.Combine(gameDir, "versions"); }
        public static string VersionDir(string gameDir, string id) { return Path.Combine(VersionsDir(gameDir), id); }
        public static string VersionJson(string gameDir, string id) { return Path.Combine(VersionDir(gameDir, id), id + ".json"); }
        public static string ClientJar(string gameDir, string id) { return Path.Combine(VersionDir(gameDir, id), id + ".jar"); }
        public static string NativesDir(string gameDir, string id) { return Path.Combine(VersionDir(gameDir, id), "natives"); }
        public static string LibrariesDir(string gameDir) { return Path.Combine(gameDir, "libraries"); }
        public static string AssetsDir(string gameDir) { return Path.Combine(gameDir, "assets"); }
        public static string AssetIndexPath(string gameDir, string indexId) { return Path.Combine(AssetsDir(gameDir), "indexes", indexId + ".json"); }
    }

    public static class MojangApi
    {
        public const string ManifestUrl = "https://launchermeta.mojang.com/mc/game/version_manifest_v2.json";

        public static VersionManifest ParseManifest(string json)
        {
            JsonValue root = JsonParser.Parse(json);
            var manifest = new VersionManifest();
            manifest.LatestRelease = root["latest"]["release"].AsString("");
            manifest.LatestSnapshot = root["latest"]["snapshot"].AsString("");

            foreach (JsonValue item in root["versions"].Items)
            {
                var entry = new VersionEntry();
                entry.Id = item["id"].AsString();
                if (string.IsNullOrEmpty(entry.Id)) continue;
                entry.Type = item["type"].AsString("release");
                entry.Url = item["url"].AsString();
                entry.Sha1 = item["sha1"].AsString();

                string releaseTime = item["releaseTime"].AsString();
                DateTime parsed;
                if (!string.IsNullOrEmpty(releaseTime) &&
                    DateTime.TryParse(releaseTime, CultureInfo.InvariantCulture,
                        DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out parsed))
                    entry.ReleaseTime = parsed;

                manifest.Versions.Add(entry);
            }
            return manifest;
        }

        public static GameVersion ParseVersion(string json)
        {
            JsonValue root = JsonParser.Parse(json);
            var version = new GameVersion();
            version.Id = root["id"].AsString();
            version.Type = root["type"].AsString("release");
            version.MainClass = root["mainClass"].AsString();
            version.Assets = root["assets"].AsString("");
            version.InheritsFrom = root["inheritsFrom"].AsString();
            version.JavaMajor = root["javaVersion"]["majorVersion"].AsInt(0);

            JsonValue assetIndex = root["assetIndex"];
            if (assetIndex.IsObject)
            {
                version.AssetIndex = new AssetIndexInfo();
                version.AssetIndex.Id = assetIndex["id"].AsString();
                version.AssetIndex.Url = assetIndex["url"].AsString();
                version.AssetIndex.Sha1 = assetIndex["sha1"].AsString();
                version.AssetIndex.Size = assetIndex["size"].AsLong();
            }

            version.Client = DownloadInfo.Parse(root["downloads"]["client"]);
            version.LegacyGameArgs = root["minecraftArguments"].AsString(null);

            foreach (JsonValue libJson in root["libraries"].Items)
            {
                LibraryInfo library = ParseLibrary(libJson);
                if (library != null) version.Libraries.Add(library);
            }

            JsonValue args = root["arguments"];
            if (args.IsObject)
            {
                foreach (JsonValue argJson in args["jvm"].Items)
                {
                    ArgEntry entry = ParseArg(argJson);
                    if (entry != null) version.JvmArgs.Add(entry);
                }
                foreach (JsonValue argJson in args["game"].Items)
                {
                    ArgEntry entry = ParseArg(argJson);
                    if (entry != null) version.GameArgs.Add(entry);
                }
            }
            return version;
        }

        private static LibraryInfo ParseLibrary(JsonValue json)
        {
            string name = json["name"].AsString();
            if (string.IsNullOrEmpty(name)) return null;

            var library = new LibraryInfo();
            library.Name = name;
            library.Url = json["url"].AsString();
            library.Sha1 = json["sha1"].AsString();

            foreach (JsonValue ruleJson in json["rules"].Items)
                library.Rules.Add(Rule.Parse(ruleJson));

            library.Artifact = DownloadInfo.Parse(json["downloads"]["artifact"]);

            JsonValue classifiers = json["downloads"]["classifiers"];
            if (classifiers.IsObject)
            {
                foreach (var pair in classifiers.Properties)
                {
                    DownloadInfo info = DownloadInfo.Parse(pair.Value);
                    if (info != null) library.Classifiers[pair.Key] = info;
                }
            }

            JsonValue natives = json["natives"];
            if (natives.IsObject)
            {
                foreach (var pair in natives.Properties)
                {
                    string classifier = pair.Value.AsString();
                    if (!string.IsNullOrEmpty(classifier)) library.Natives[pair.Key] = classifier;
                }
            }

            JsonValue exclude = json["extract"]["exclude"];
            if (exclude.IsArray)
            {
                foreach (JsonValue item in exclude.Items)
                {
                    string value = item.AsString();
                    if (value != null) library.ExtractExclude.Add(value);
                }
            }
            return library;
        }

        private static ArgEntry ParseArg(JsonValue json)
        {
            if (json.IsString)
            {
                var entry = new ArgEntry();
                entry.Values.Add(json.AsString());
                return entry;
            }
            if (!json.IsObject) return null;

            var result = new ArgEntry();
            foreach (JsonValue ruleJson in json["rules"].Items)
                result.Rules.Add(Rule.Parse(ruleJson));

            JsonValue value = json["value"];
            if (value.IsString)
            {
                result.Values.Add(value.AsString());
            }
            else if (value.IsArray)
            {
                foreach (JsonValue item in value.Items)
                {
                    string text = item.AsString();
                    if (text != null) result.Values.Add(text);
                }
            }
            return result;
        }

        public static async Task<VersionManifest> GetManifestAsync(bool forceRefresh, CancellationToken ct)
        {
            string cache = Path.Combine(AppPaths.MetaDir, "version_manifest_v2.json");
            bool fresh = false;
            try
            {
                if (File.Exists(cache))
                {
                    double minutes = (DateTime.UtcNow - File.GetLastWriteTimeUtc(cache)).TotalMinutes;
                    fresh = minutes < 30;
                }
            }
            catch
            {
            }

            if (!forceRefresh && fresh)
            {
                try
                {
                    return ParseManifest(File.ReadAllText(cache));
                }
                catch
                {
                }
            }

            try
            {
                string json = await Web.GetStringAsync(ManifestUrl, ct).ConfigureAwait(false);
                VersionManifest manifest = ParseManifest(json);
                AppPaths.EnsureDirectories();
                File.WriteAllText(cache, json);
                return manifest;
            }
            catch
            {
                if (File.Exists(cache))
                {
                    try
                    {
                        return ParseManifest(File.ReadAllText(cache));
                    }
                    catch
                    {
                    }
                }
                throw;
            }
        }

        public static async Task<GameVersion> EnsureVersionJsonAsync(VersionEntry entry, string gameDir,
            Action<string> log, CancellationToken ct)
        {
            string versionDir = GamePaths.VersionDir(gameDir, entry.Id);
            Directory.CreateDirectory(versionDir);
            string path = GamePaths.VersionJson(gameDir, entry.Id);

            if (File.Exists(path))
            {
                try
                {
                    return ResolveInheritance(ParseVersion(File.ReadAllText(path)), gameDir, ct);
                }
                catch
                {
                    try { File.Delete(path); }
                    catch
                    {
                    }
                }
            }

            if (string.IsNullOrEmpty(entry.Url))
                throw new InvalidOperationException("Для версии " + entry.Id + " не найден файл описания. Переустановите сборку.");

            if (log != null) log("Загрузка описания версии " + entry.Id + "…");
            await Web.DownloadFileAsync(entry.Url, path, entry.Sha1, 0, null, ct).ConfigureAwait(false);
            return ResolveInheritance(ParseVersion(File.ReadAllText(path)), gameDir, ct);
        }

        private static GameVersion ResolveInheritance(GameVersion version, string gameDir, CancellationToken ct)
        {
            if (version == null) return null;
            if (string.IsNullOrEmpty(version.InheritsFrom))
            {
                if (version.JavaMajor <= 0) version.JavaMajor = 8;
                return version;
            }

            var chain = new List<GameVersion>();
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            visited.Add(version.Id ?? "");
            GameVersion current = version;
            while (current != null && !string.IsNullOrEmpty(current.InheritsFrom))
            {
                string parentId = current.InheritsFrom;
                if (!visited.Add(parentId))
                    throw new InvalidOperationException("Циклическое наследование версий: " + parentId);

                string parentPath = GamePaths.VersionJson(gameDir, parentId);
                if (!File.Exists(parentPath))
                    throw new InvalidOperationException(
                        "Не найдена базовая версия " + parentId + ". Сначала установите её в лаунчере.");

                GameVersion parent = ParseVersion(File.ReadAllText(parentPath));
                chain.Add(parent);
                current = parent;
            }

            // Nearest parent first in the chain; merge from the root down so the
            // child's own values always win.
            for (int i = chain.Count - 1; i >= 0; i--)
                MergeInto(version, chain[i]);

            if (version.JavaMajor <= 0) version.JavaMajor = 8;
            return version;
        }

        private static void MergeInto(GameVersion child, GameVersion parent)
        {
            if (string.IsNullOrEmpty(child.MainClass)) child.MainClass = parent.MainClass;
            if (string.IsNullOrEmpty(child.Assets)) child.Assets = parent.Assets;
            if (child.AssetIndex == null) child.AssetIndex = parent.AssetIndex;
            if (child.Client == null) child.Client = parent.Client;
            if (string.IsNullOrEmpty(child.LegacyGameArgs)) child.LegacyGameArgs = parent.LegacyGameArgs;
            if (child.JavaMajor <= 0) child.JavaMajor = parent.JavaMajor;

            var merged = new List<LibraryInfo>();
            var indexByKey = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (LibraryInfo library in parent.Libraries)
            {
                string key = LibraryKey(library.Name);
                if (key != null) indexByKey[key] = merged.Count;
                merged.Add(library);
            }
            foreach (LibraryInfo library in child.Libraries)
            {
                string key = LibraryKey(library.Name);
                int index;
                if (key != null && indexByKey.TryGetValue(key, out index))
                {
                    merged[index] = library;
                }
                else
                {
                    if (key != null) indexByKey[key] = merged.Count;
                    merged.Add(library);
                }
            }
            child.Libraries = merged;

            var jvm = new List<ArgEntry>(parent.JvmArgs);
            jvm.AddRange(child.JvmArgs);
            child.JvmArgs = jvm;

            var game = new List<ArgEntry>(parent.GameArgs);
            game.AddRange(child.GameArgs);
            child.GameArgs = game;
        }

        private static string LibraryKey(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string[] parts = name.Split(':');
            if (parts.Length < 2) return null;
            string classifier = parts.Length >= 4 ? parts[3] : "";
            return parts[0] + ":" + parts[1] + ":" + classifier;
        }
    }
}
