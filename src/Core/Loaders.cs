using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vexor.Core
{
    public sealed class InstalledBuild
    {
        public string Id;
        public string GameVersion;
        public string Loader;
        public string LoaderVersion;
        public string ProfileDir;
        public DateTime CreatedTime;
        public string Name;
        public string PackProjectId;
        public string PackSlug;
        public string PackVersion;
        public bool IsModpack;

        public string DisplayName
        {
            get { return string.IsNullOrEmpty(Name) ? Id : Name; }
        }

        public string LoaderLabel
        {
            get
            {
                if (string.Equals(Loader, "fabric", StringComparison.OrdinalIgnoreCase)) return "Fabric";
                if (string.Equals(Loader, "forge", StringComparison.OrdinalIgnoreCase)) return "Forge";
                if (string.Equals(Loader, "neoforge", StringComparison.OrdinalIgnoreCase)) return "NeoForge";
                return Loader;
            }
        }

        public string ModsDir
        {
            get { return Path.Combine(ProfileDir, "mods"); }
        }

        public string Summary
        {
            get
            {
                if (IsModpack)
                    return "Модпак «" + DisplayName + "» · " + LoaderLabel + " " + LoaderVersion + " · " + GameVersion;
                return LoaderLabel + " " + LoaderVersion + " · " + GameVersion;
            }
        }
    }

    public static class LoaderInstaller
    {
        public const string Fabric = "fabric";
        public const string Forge = "forge";

        public static string ProfileDir(string gameDir, string buildId)
        {
            return Path.Combine(gameDir, "profiles", buildId);
        }

        public static void PrepareProfile(InstalledBuild build)
        {
            Directory.CreateDirectory(build.ProfileDir);
            Directory.CreateDirectory(build.ModsDir);
        }

        public static InstalledBuild Find(List<InstalledBuild> builds, string id)
        {
            if (builds == null) return null;
            foreach (InstalledBuild build in builds)
            {
                if (string.Equals(build.Id, id, StringComparison.OrdinalIgnoreCase)) return build;
            }
            return null;
        }

        public static List<InstalledBuild> Scan(string gameDir)
        {
            var result = new List<InstalledBuild>();
            string versionsDir = GamePaths.VersionsDir(gameDir);
            if (!Directory.Exists(versionsDir)) return result;

            foreach (string dir in Directory.GetDirectories(versionsDir))
            {
                string id = Path.GetFileName(dir);
                string jsonPath = Path.Combine(dir, id + ".json");
                if (!File.Exists(jsonPath)) continue;
                try
                {
                    JsonValue root = JsonParser.Parse(File.ReadAllText(jsonPath));
                    string inherits = root["inheritsFrom"].AsString();
                    if (string.IsNullOrEmpty(inherits)) continue;

                    string mainClass = root["mainClass"].AsString("");
                    string loader = null;
                    string loaderVersion = "";

                    foreach (JsonValue lib in root["libraries"].Items)
                    {
                        string name = lib["name"].AsString("");
                        if (name.StartsWith("net.fabricmc:fabric-loader:", StringComparison.OrdinalIgnoreCase))
                        {
                            loader = Fabric;
                            loaderVersion = name.Split(':')[2];
                            break;
                        }
                        if (name.StartsWith("net.minecraftforge:forge:", StringComparison.OrdinalIgnoreCase))
                        {
                            loader = Forge;
                            string version = name.Split(':')[2];
                            string prefix = inherits + "-";
                            loaderVersion = version.StartsWith(prefix, StringComparison.Ordinal)
                                ? version.Substring(prefix.Length)
                                : version;
                            break;
                        }
                        if (name.StartsWith("net.neoforged:", StringComparison.OrdinalIgnoreCase))
                        {
                            loader = "neoforge";
                            break;
                        }
                    }

                    if (loader == null)
                    {
                        if (mainClass.IndexOf("fabric", StringComparison.OrdinalIgnoreCase) >= 0) loader = Fabric;
                        else if (mainClass.IndexOf("neoforge", StringComparison.OrdinalIgnoreCase) >= 0) loader = "neoforge";
                        else if (mainClass.IndexOf("forge", StringComparison.OrdinalIgnoreCase) >= 0) loader = Forge;
                    }
                    if (loader == null) continue;
                    if (string.Equals(loader, "neoforge", StringComparison.OrdinalIgnoreCase)) continue;

                    var build = new InstalledBuild();
                    build.Id = id;
                    build.GameVersion = inherits;
                    build.Loader = loader;
                    build.LoaderVersion = loaderVersion;
                    build.ProfileDir = ProfileDir(gameDir, id);
                    try { build.CreatedTime = File.GetLastWriteTime(jsonPath); }
                    catch { build.CreatedTime = DateTime.MinValue; }

                    PackInfo pack = ModpacksApi.ReadInfo(build.ProfileDir);
                    if (pack != null)
                    {
                        build.IsModpack = true;
                        build.Name = string.IsNullOrEmpty(pack.Name) ? pack.Title : pack.Name;
                        build.PackProjectId = pack.ProjectId;
                        build.PackSlug = pack.Slug;
                        build.PackVersion = pack.VersionNumber;
                        if (pack.InstalledTime > DateTime.MinValue)
                            build.CreatedTime = pack.InstalledTime.ToLocalTime();
                    }
                    result.Add(build);
                }
                catch
                {
                }
            }

            result.Sort(delegate(InstalledBuild a, InstalledBuild b)
            {
                return b.CreatedTime.CompareTo(a.CreatedTime);
            });
            return result;
        }

        // ------------------------------------------------------------- fabric

        public static async Task<InstalledBuild> EnsureFabricAsync(string gameId, string gameDir, string loaderVersion,
            IProgress<BatchProgress> progress, Action<string> log, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(loaderVersion))
                loaderVersion = await LatestFabricLoaderAsync(gameId, ct).ConfigureAwait(false);

            if (log != null) log("Fabric: загрузка профиля " + loaderVersion + " для " + gameId + "…");
            string url = "https://meta.fabricmc.net/v2/versions/loader/" +
                Uri.EscapeDataString(gameId) + "/" + Uri.EscapeDataString(loaderVersion) + "/profile/json";
            string json = await Web.GetStringAsync(url, ct).ConfigureAwait(false);
            JsonValue root = JsonParser.Parse(json);

            string id = root["id"].AsString("");
            if (string.IsNullOrEmpty(id)) id = "fabric-loader-" + loaderVersion + "-" + gameId;

            string versionDir = GamePaths.VersionDir(gameDir, id);
            Directory.CreateDirectory(versionDir);
            File.WriteAllText(GamePaths.VersionJson(gameDir, id), json, new UTF8Encoding(false));

            string librariesDir = GamePaths.LibrariesDir(gameDir);
            var items = new List<DownloadItem>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (JsonValue lib in root["libraries"].Items)
            {
                string name = lib["name"].AsString();
                string baseUrl = lib["url"].AsString("https://maven.fabricmc.net/");
                string sha1 = lib["sha1"].AsString();
                long size = lib["size"].AsLong();
                string relative = GameInstaller.MavenPath(name, null);
                if (string.IsNullOrEmpty(relative)) continue;
                if (!baseUrl.EndsWith("/", StringComparison.Ordinal)) baseUrl += "/";
                string dest = Path.Combine(librariesDir, relative.Replace('/', Path.DirectorySeparatorChar));
                if (!seen.Add(dest)) continue;
                items.Add(new DownloadItem(baseUrl + relative, dest, sha1, size));
            }

            if (items.Count > 0)
            {
                if (log != null) log("Fabric: библиотеки (" + items.Count + " файлов)…");
                await Web.DownloadBatchAsync(items, 8, progress, "Библиотеки Fabric", ct).ConfigureAwait(false);
            }

            var build = new InstalledBuild();
            build.Id = id;
            build.GameVersion = gameId;
            build.Loader = Fabric;
            build.LoaderVersion = loaderVersion;
            build.ProfileDir = ProfileDir(gameDir, id);
            build.CreatedTime = DateTime.Now;
            PrepareProfile(build);
            return build;
        }

        private static async Task<string> LatestFabricLoaderAsync(string gameId, CancellationToken ct)
        {
            string url = "https://meta.fabricmc.net/v2/versions/loader/" + Uri.EscapeDataString(gameId);
            string json = await Web.GetStringAsync(url, ct).ConfigureAwait(false);
            JsonValue list = JsonParser.Parse(json);
            if (!list.IsArray || list.Items.Count == 0)
                throw new InvalidOperationException("Fabric пока не поддерживает версию " + gameId + ".");

            string first = null;
            foreach (JsonValue item in list.Items)
            {
                string version = item["loader"]["version"].AsString();
                if (string.IsNullOrEmpty(version)) continue;
                if (first == null) first = version;
                if (item["loader"]["stable"].AsBool()) return version;
            }
            if (first == null)
                throw new InvalidOperationException("Fabric не вернул версии загрузчика для " + gameId + ".");
            return first;
        }

        // -------------------------------------------------------------- forge

        public static Task<InstalledBuild> EnsureForgeAsync(string gameId, string gameDir, string javaPath,
            IProgress<BatchProgress> progress, Action<string> log, CancellationToken ct)
        {
            return EnsureForgeAsync(gameId, gameDir, javaPath, null, progress, log, ct);
        }

        public static async Task<InstalledBuild> EnsureForgeAsync(string gameId, string gameDir, string javaPath,
            string forgeVersion, IProgress<BatchProgress> progress, Action<string> log, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(forgeVersion))
                forgeVersion = await ForgePromotedVersionAsync(gameId, ct).ConfigureAwait(false);
            string id = gameId + "-forge-" + forgeVersion;
            string jsonPath = GamePaths.VersionJson(gameDir, id);

            if (!File.Exists(jsonPath))
            {
                AppPaths.EnsureDirectories();
                string installerDir = Path.Combine(AppPaths.MetaDir, "installers");
                Directory.CreateDirectory(installerDir);
                string installer = Path.Combine(installerDir, "forge-" + gameId + "-" + forgeVersion + "-installer.jar");

                if (!File.Exists(installer))
                {
                    if (log != null) log("Forge: скачивание установщика " + forgeVersion + "…");
                    string url = "https://maven.minecraftforge.net/net/minecraftforge/forge/" +
                        Uri.EscapeDataString(gameId + "-" + forgeVersion) + "/forge-" +
                        Uri.EscapeDataString(gameId + "-" + forgeVersion) + "-installer.jar";
                    await Web.DownloadFileAsync(url, installer, null, 0, delegate(long done, long total)
                    {
                        if (progress != null && total > 0)
                        {
                            var batch = new BatchProgress();
                            batch.Status = "Установщик Forge";
                            batch.FilesDone = 1;
                            batch.FilesTotal = 1;
                            batch.BytesDone = done;
                            batch.BytesTotal = total;
                            progress.Report(batch);
                        }
                    }, ct).ConfigureAwait(false);
                }

                EnsureLauncherProfiles(gameDir);
                if (log != null) log("Forge: установка (несколько минут)…");
                RunForgeInstaller(javaPath, installer, gameDir, log, ct);

                if (!File.Exists(jsonPath))
                    throw new InvalidOperationException("Установщик Forge завершился, но файл версии не создан.");
            }

            var build = new InstalledBuild();
            build.Id = id;
            build.GameVersion = gameId;
            build.Loader = Forge;
            build.LoaderVersion = forgeVersion;
            build.ProfileDir = ProfileDir(gameDir, id);
            build.CreatedTime = DateTime.Now;
            PrepareProfile(build);
            return build;
        }

        private static async Task<string> ForgePromotedVersionAsync(string gameId, CancellationToken ct)
        {
            string cache = Path.Combine(AppPaths.MetaDir, "forge_promotions.json");
            JsonValue promos = null;
            try
            {
                if (File.Exists(cache) && (DateTime.UtcNow - File.GetLastWriteTimeUtc(cache)).TotalHours < 6)
                    promos = JsonParser.Parse(File.ReadAllText(cache));
            }
            catch
            {
                promos = null;
            }

            if (promos == null)
            {
                string json = await Web.GetStringAsync(
                    "https://files.minecraftforge.net/net/minecraftforge/forge/promotions_slim.json", ct).ConfigureAwait(false);
                promos = JsonParser.Parse(json);
                try
                {
                    AppPaths.EnsureDirectories();
                    File.WriteAllText(cache, json);
                }
                catch
                {
                }
            }

            string recommended = promos["promos"][gameId + "-recommended"].AsString();
            string latest = promos["promos"][gameId + "-latest"].AsString();
            string version = !string.IsNullOrEmpty(recommended) ? recommended : latest;
            if (string.IsNullOrEmpty(version))
                throw new InvalidOperationException("Forge пока не поддерживает версию " + gameId + ".");
            return version;
        }

        private static void EnsureLauncherProfiles(string gameDir)
        {
            string path = Path.Combine(gameDir, "launcher_profiles.json");
            if (File.Exists(path)) return;
            Directory.CreateDirectory(gameDir);
            File.WriteAllText(path, "{\"profiles\":{},\"settings\":{},\"version\":3}", new UTF8Encoding(false));
        }

        private static void RunForgeInstaller(string javaPath, string installer, string gameDir,
            Action<string> log, CancellationToken ct)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.FileName = javaPath;
            startInfo.Arguments = "-jar " + Quote(installer) + " --installClient " + Quote(gameDir);
            startInfo.WorkingDirectory = Path.GetDirectoryName(installer);
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.StandardErrorEncoding = Encoding.UTF8;

            using (var process = new Process())
            {
                process.StartInfo = startInfo;
                process.Start();
                Task<string> output = process.StandardOutput.ReadToEndAsync();
                Task<string> error = process.StandardError.ReadToEndAsync();

                bool finished = false;
                for (int i = 0; i < 1200; i++)
                {
                    if (process.WaitForExit(500)) { finished = true; break; }
                    ct.ThrowIfCancellationRequested();
                }

                if (!finished)
                {
                    try { process.Kill(); }
                    catch
                    {
                    }
                    throw new InvalidOperationException("Установщик Forge не завершился за 10 минут.");
                }

                process.WaitForExit();
                string stdout = "";
                string stderr = "";
                try { stdout = output.Result; }
                catch
                {
                }
                try { stderr = error.Result; }
                catch
                {
                }

                if (process.ExitCode != 0)
                {
                    string tail = Tail(stdout + "\n" + stderr, 12);
                    throw new InvalidOperationException("Установщик Forge завершился с кодом " + process.ExitCode +
                        (tail.Length > 0 ? ":" + Environment.NewLine + tail : "."));
                }
                if (log != null) log("Forge: установка завершена.");
            }
        }

        private static string Tail(string text, int lines)
        {
            if (string.IsNullOrEmpty(text)) return "";
            string[] parts = text.Replace("\r", "").Split('\n');
            int start = parts.Length > lines ? parts.Length - lines : 0;
            var sb = new StringBuilder();
            for (int i = start; i < parts.Length; i++)
            {
                if (parts[i].Trim().Length == 0) continue;
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(parts[i].Trim());
            }
            return sb.ToString();
        }

        private static string Quote(string value)
        {
            if (string.IsNullOrEmpty(value)) return "\"\"";
            return value.IndexOf(' ') >= 0 || value.IndexOf('\t') >= 0
                ? "\"" + value + "\""
                : value;
        }
    }
}
