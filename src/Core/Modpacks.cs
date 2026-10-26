using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vexor.Core
{
    public sealed class PackInfo
    {
        public string ProjectId;
        public string Slug;
        public string Title;
        public string IconUrl;
        public string VersionId;
        public string VersionNumber;
        public string Name;
        public string Summary;
        public string Minecraft;
        public string Loader;
        public string LoaderVersion;
        public DateTime InstalledTime;
        public List<string> Files = new List<string>();
    }

    public sealed class PackInstallResult
    {
        public InstalledBuild Build;
        public GameVersion Vanilla;
        public int Files;
        public int Overrides;
        public List<string> Warnings = new List<string>();
    }

    public static class ModpacksApi
    {
        private const string Base = "https://api.modrinth.com/v2";
        public const string InfoFileName = "vexor-pack.json";

        public static async Task<ModSearchResult> SearchAsync(string query, string gameVersion, string loader,
            int offset, int limit, CancellationToken ct)
        {
            var facets = new StringBuilder();
            facets.Append("[[\"project_type:modpack\"]");
            if (!string.IsNullOrEmpty(loader))
                facets.Append(",[\"categories:").Append(loader).Append("\"]");
            else
                facets.Append(",[\"categories:fabric\",\"categories:forge\"]");
            if (!string.IsNullOrEmpty(gameVersion))
                facets.Append(",[\"versions:").Append(gameVersion).Append("\"]");
            facets.Append(']');

            string url = Base + "/search?query=" + Uri.EscapeDataString(query ?? "") +
                "&facets=" + Uri.EscapeDataString(facets.ToString()) +
                "&limit=" + limit + "&offset=" + offset +
                "&index=" + (string.IsNullOrEmpty((query ?? "").Trim()) ? "downloads" : "relevance");

            JsonValue root = JsonParser.Parse(await Web.GetStringAsync(url, ct).ConfigureAwait(false));
            var result = new ModSearchResult();
            result.TotalHits = root["total_hits"].AsInt();
            result.Offset = root["offset"].AsInt(offset);
            result.Limit = root["limit"].AsInt(limit);
            foreach (JsonValue hit in root["hits"].Items)
            {
                ModProject project = ModrinthApi.ParseProject(hit);
                if (project != null) result.Hits.Add(project);
            }
            return result;
        }

        public static async Task<List<ModVersion>> GetVersionsAsync(string projectId, string gameVersion,
            string loader, CancellationToken ct)
        {
            string url = Base + "/project/" + Uri.EscapeDataString(projectId) + "/version" +
                "?game_versions=" + Uri.EscapeDataString("[\"" + gameVersion + "\"]");
            if (!string.IsNullOrEmpty(loader))
                url += "&loaders=" + Uri.EscapeDataString("[\"" + loader + "\"]");

            JsonValue root = JsonParser.Parse(await Web.GetStringAsync(url, ct).ConfigureAwait(false));
            var versions = new List<ModVersion>();
            foreach (JsonValue item in root.Items)
            {
                ModVersion version = ModrinthApi.ParseVersion(item);
                if (version != null) versions.Add(version);
            }
            return versions;
        }

        public static ModVersion ChooseVersion(List<ModVersion> versions)
        {
            if (versions == null || versions.Count == 0) return null;
            for (int i = 0; i < versions.Count; i++)
            {
                if (string.Equals(versions[i].VersionType, "release", StringComparison.OrdinalIgnoreCase) &&
                    IsSupportedVersion(versions[i])) return versions[i];
            }
            for (int i = 0; i < versions.Count; i++)
            {
                if (IsSupportedVersion(versions[i])) return versions[i];
            }
            return null;
        }

        public static async Task<ModVersion> ResolveAsync(ModProject project, string gameVersion, string loader,
            CancellationToken ct)
        {
            if (project == null) throw new ArgumentNullException("project");
            List<ModVersion> versions = await GetVersionsAsync(project.ProjectId, gameVersion, loader, ct)
                .ConfigureAwait(false);
            ModVersion version = ChooseVersion(versions);
            if (version == null)
                throw new InvalidOperationException("Для «" + project.Title + "» нет модпака под Minecraft " +
                    gameVersion + (string.IsNullOrEmpty(loader) ? "." : " (" + LoaderLabel(loader) + ")."));
            return version;
        }

        public static async Task<PackInstallResult> InstallAsync(ModProject project, ModVersion version,
            LauncherSettings settings, IProgress<BatchProgress> progress, Action<string> log, CancellationToken ct)
        {
            if (project == null) throw new ArgumentNullException("project");
            if (version == null) throw new ArgumentNullException("version");
            if (settings == null) throw new ArgumentNullException("settings");

            ModFile file = version.PrimaryFile;
            if (file == null || string.IsNullOrEmpty(file.Url))
                throw new InvalidOperationException("У модпака «" + project.Title + "» нет файла для скачивания.");

            string gameDir = settings.GameDir;
            AppPaths.EnsureDirectories();
            string cacheDir = Path.Combine(AppPaths.MetaDir, "packs");
            Directory.CreateDirectory(cacheDir);
            string mrpackPath = Path.Combine(cacheDir, SafeId(version.Id, "pack") + ".mrpack");

            if (!File.Exists(mrpackPath) || (file.Size > 0 && new FileInfo(mrpackPath).Length != file.Size))
            {
                if (log != null) log("Скачивание " + file.FileName + " · " + FormatMb(file.Size));
                await Web.DownloadFileAsync(file.Url, mrpackPath, file.Sha1, file.Size,
                    delegate(long done, long total)
                    {
                        Report(progress, "Модпак: " + project.Title, done, total);
                    }, ct).ConfigureAwait(false);
            }

            ZipArchive archive = null;
            bool success = false;
            try
            {
                archive = ZipFile.OpenRead(mrpackPath);
                ZipArchiveEntry indexEntry = archive.GetEntry("modrinth.index.json");
                if (indexEntry == null)
                    throw new InvalidOperationException("В файле модпака нет modrinth.index.json.");

                JsonValue index;
                using (Stream stream = indexEntry.Open())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    index = JsonParser.Parse(reader.ReadToEnd());
                }

                JsonValue dependencies = index["dependencies"];
                string minecraft = dependencies["minecraft"].AsString(
                    version.GameVersions.Count > 0 ? version.GameVersions[0] : null);
                if (string.IsNullOrEmpty(minecraft))
                    throw new InvalidOperationException("Модпак не указывает версию Minecraft.");

                string fabricLoader = dependencies["fabric-loader"].AsString();
                string forgeVersion = dependencies["forge"].AsString();
                string quilt = dependencies["quilt-loader"].AsString();
                string neoforge = dependencies["neoforge"].AsString();

                string loader;
                string loaderVersion;
                if (!string.IsNullOrEmpty(fabricLoader))
                {
                    loader = LoaderInstaller.Fabric;
                    loaderVersion = fabricLoader;
                }
                else if (!string.IsNullOrEmpty(forgeVersion))
                {
                    loader = LoaderInstaller.Forge;
                    loaderVersion = NormalizeForgeVersion(forgeVersion, minecraft);
                }
                else if (!string.IsNullOrEmpty(quilt))
                {
                    throw new InvalidOperationException("Модпак использует Quilt — сейчас поддерживаются только Fabric и Forge.");
                }
                else if (!string.IsNullOrEmpty(neoforge))
                {
                    throw new InvalidOperationException("Модпак использует NeoForge — сейчас поддерживаются только Fabric и Forge.");
                }
                else
                {
                    throw new InvalidOperationException("В модпаке не указан загрузчик (Fabric или Forge).");
                }

                string packName = index["name"].AsString(project.Title);
                if (string.IsNullOrEmpty(packName)) packName = project.Title;
                string packVersion = index["versionId"].AsString(version.VersionNumber);
                string summary = index["summary"].AsString(version.Name);
                string slug = string.IsNullOrEmpty(project.Slug) ? project.ProjectId : project.Slug;

                string buildId = UniqueBuildId(gameDir, SafeId(slug, "modpack") + "-" + SafeId(minecraft, "mc"),
                    project.ProjectId);

                if (log != null) log("Версия Minecraft " + minecraft + " · " + LoaderLabel(loader) + " " + loaderVersion);

                GameVersion vanilla = await EnsureVanillaAsync(minecraft, gameDir, log, ct).ConfigureAwait(false);

                InstalledBuild loaderBuild;
                if (loader == LoaderInstaller.Fabric)
                {
                    if (log != null) log("Подготовка Fabric " + loaderVersion + "…");
                    loaderBuild = await LoaderInstaller.EnsureFabricAsync(minecraft, gameDir, loaderVersion,
                        progress, log, ct).ConfigureAwait(false);
                }
                else
                {
                    if (log != null) log("Подготовка Java для установки Forge…");
                    string javaPath = await JavaManager.EnsureJavaAsync(vanilla.JavaMajor, settings, progress, log, ct)
                        .ConfigureAwait(false);
                    if (log != null) log("Подготовка Forge " + loaderVersion + "…");
                    loaderBuild = await LoaderInstaller.EnsureForgeAsync(minecraft, gameDir, javaPath,
                        loaderVersion, progress, log, ct).ConfigureAwait(false);
                }

                CopyVersionForPack(gameDir, loaderBuild, buildId, minecraft);

                string profileDir = LoaderInstaller.ProfileDir(gameDir, buildId);
                Directory.CreateDirectory(profileDir);
                Directory.CreateDirectory(Path.Combine(profileDir, "mods"));
                PackInfo previous = ReadInfo(profileDir);

                var items = new List<DownloadItem>();
                var relativeFiles = new List<string>();
                var warnings = new List<string>();
                foreach (JsonValue fileJson in index["files"].Items)
                {
                    string environment = fileJson["env"]["client"].AsString("required");
                    if (string.Equals(environment, "unsupported", StringComparison.OrdinalIgnoreCase)) continue;

                    string relative = NormalizeRelative(fileJson["path"].AsString());
                    string dest = SafeCombine(profileDir, relative);
                    if (dest == null)
                    {
                        warnings.Add("Пропущен небезопасный путь: " + fileJson["path"].AsString(""));
                        continue;
                    }

                    string url = fileJson["downloads"][0].AsString();
                    if (string.IsNullOrEmpty(url))
                    {
                        warnings.Add("Нет ссылки для " + relative);
                        continue;
                    }

                    relativeFiles.Add(relative);
                    items.Add(new DownloadItem(url, dest, fileJson["hashes"]["sha1"].AsString(),
                        fileJson["fileSize"].AsLong()));
                }

                if (items.Count > 0)
                {
                    if (log != null) log("Файлы модпака: " + items.Count + "…");
                    await Web.DownloadBatchAsync(items, 8, progress, "Файлы модпака", ct).ConfigureAwait(false);
                }
                RemoveStaleFiles(previous, relativeFiles, profileDir);

                if (log != null) log("Распаковка overrides…");
                int overrides = ExtractFolder(archive, "overrides/", profileDir, warnings);
                overrides += ExtractFolder(archive, "client-overrides/", profileDir, warnings);

                var build = new InstalledBuild();
                build.Id = buildId;
                build.GameVersion = minecraft;
                build.Loader = loader;
                build.LoaderVersion = loaderVersion;
                build.ProfileDir = profileDir;
                build.CreatedTime = DateTime.Now;
                build.IsModpack = true;
                build.Name = packName;
                build.PackProjectId = project.ProjectId;
                build.PackSlug = project.Slug;
                build.PackVersion = packVersion;

                WriteInfo(profileDir, project, packName, version.Id, packVersion, summary, minecraft, loader,
                    loaderVersion, relativeFiles);

                var result = new PackInstallResult();
                result.Build = build;
                result.Vanilla = vanilla;
                result.Files = items.Count;
                result.Overrides = overrides;
                result.Warnings = warnings;

                if (log != null)
                    log("Готово: " + packName + " · файлов " + items.Count + " · overrides " + overrides);
                success = true;
                return result;
            }
            finally
            {
                if (archive != null)
                {
                    try { archive.Dispose(); }
                    catch
                    {
                    }
                }
                if (success)
                {
                    try { File.Delete(mrpackPath); }
                    catch
                    {
                    }
                }
            }
        }

        public static PackInfo ReadInfo(string profileDir)
        {
            if (string.IsNullOrEmpty(profileDir)) return null;
            string path = Path.Combine(profileDir, InfoFileName);
            if (!File.Exists(path)) return null;
            try
            {
                JsonValue root = JsonParser.Parse(File.ReadAllText(path));
                var info = new PackInfo();
                info.ProjectId = root["projectId"].AsString();
                info.Slug = root["slug"].AsString();
                info.Title = root["title"].AsString();
                info.IconUrl = root["iconUrl"].AsString();
                info.VersionId = root["versionId"].AsString();
                info.VersionNumber = root["versionNumber"].AsString();
                info.Name = root["name"].AsString();
                info.Summary = root["summary"].AsString();
                info.Minecraft = root["minecraft"].AsString();
                info.Loader = root["loader"].AsString();
                info.LoaderVersion = root["loaderVersion"].AsString();
                string installed = root["installed"].AsString();
                DateTime parsed;
                if (!string.IsNullOrEmpty(installed) && DateTime.TryParse(installed, CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind, out parsed))
                    info.InstalledTime = parsed;
                foreach (JsonValue item in root["files"].Items)
                {
                    string value = item.AsString();
                    if (!string.IsNullOrEmpty(value)) info.Files.Add(value);
                }
                return info;
            }
            catch
            {
                return null;
            }
        }

        private static async Task<GameVersion> EnsureVanillaAsync(string minecraft, string gameDir,
            Action<string> log, CancellationToken ct)
        {
            var entry = new VersionEntry();
            entry.Id = minecraft;
            entry.Type = "release";
            if (!File.Exists(GamePaths.VersionJson(gameDir, minecraft)))
            {
                VersionManifest manifest = await MojangApi.GetManifestAsync(false, ct).ConfigureAwait(false);
                VersionEntry found = null;
                foreach (VersionEntry candidate in manifest.Versions)
                {
                    if (string.Equals(candidate.Id, minecraft, StringComparison.OrdinalIgnoreCase))
                    {
                        found = candidate;
                        break;
                    }
                }
                if (found == null)
                    throw new InvalidOperationException("Версия Minecraft " + minecraft + " не найдена у Mojang.");
                entry = found;
            }
            return await MojangApi.EnsureVersionJsonAsync(entry, gameDir, log, ct).ConfigureAwait(false);
        }

        private static void CopyVersionForPack(string gameDir, InstalledBuild loaderBuild, string buildId,
            string minecraft)
        {
            string source = GamePaths.VersionJson(gameDir, loaderBuild.Id);
            if (!File.Exists(source))
                throw new InvalidOperationException("Не найден файл версии загрузчика " + loaderBuild.Id + ".");

            JsonValue root = JsonParser.Parse(File.ReadAllText(source));
            var properties = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            foreach (var pair in root.Properties) properties[pair.Key] = pair.Value;
            properties["id"] = new JsonValue(buildId);
            if (!properties.ContainsKey("inheritsFrom"))
                properties["inheritsFrom"] = new JsonValue(minecraft);

            string versionDir = GamePaths.VersionDir(gameDir, buildId);
            Directory.CreateDirectory(versionDir);
            string path = GamePaths.VersionJson(gameDir, buildId);
            File.WriteAllText(path, JsonText.Serialize(new JsonValue(properties)), new UTF8Encoding(false));
            try { File.SetLastWriteTime(path, DateTime.Now); }
            catch
            {
            }
        }

        private static void WriteInfo(string profileDir, ModProject project, string name, string versionId,
            string versionNumber, string summary, string minecraft, string loader, string loaderVersion,
            List<string> io)
        {
            var root = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            root["format"] = new JsonValue(1);
            root["projectId"] = new JsonValue(project.ProjectId ?? "");
            root["slug"] = new JsonValue(project.Slug ?? "");
            root["title"] = new JsonValue(project.Title ?? "");
            root["iconUrl"] = new JsonValue(project.IconUrl ?? "");
            root["versionId"] = new JsonValue(versionId ?? "");
            root["versionNumber"] = new JsonValue(versionNumber ?? "");
            root["name"] = new JsonValue(name ?? "");
            root["summary"] = new JsonValue(summary ?? "");
            root["minecraft"] = new JsonValue(minecraft ?? "");
            root["loader"] = new JsonValue(loader ?? "");
            root["loaderVersion"] = new JsonValue(loaderVersion ?? "");
            root["installed"] = new JsonValue(DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture));

            var files = new List<JsonValue>();
            for (int i = 0; i < io.Count; i++) files.Add(new JsonValue(io[i]));
            root["files"] = new JsonValue(files);

            File.WriteAllText(Path.Combine(profileDir, InfoFileName),
                JsonText.Serialize(new JsonValue(root)), new UTF8Encoding(false));
        }

        private static void RemoveStaleFiles(PackInfo previous, List<string> current, string profileDir)
        {
            if (previous == null || previous.Files.Count == 0) return;
            var keep = new HashSet<string>(current, StringComparer.OrdinalIgnoreCase);
            foreach (string old in previous.Files)
            {
                if (string.IsNullOrEmpty(old) || keep.Contains(old)) continue;
                string dest = SafeCombine(profileDir, NormalizeRelative(old));
                if (dest == null) continue;
                try
                {
                    if (File.Exists(dest)) File.Delete(dest);
                }
                catch
                {
                }
            }
        }

        private static int ExtractFolder(ZipArchive archive, string prefix, string root, List<string> warnings)
        {
            int count = 0;
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.FullName == null ||
                    !entry.FullName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                string relative = NormalizeRelative(entry.FullName.Substring(prefix.Length));
                if (string.IsNullOrEmpty(relative) || entry.Name.Length == 0) continue;

                string dest = SafeCombine(root, relative);
                if (dest == null)
                {
                    warnings.Add("Пропущен небезопасный путь: " + entry.FullName);
                    continue;
                }

                try
                {
                    string dir = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    try
                    {
                        if (File.Exists(dest)) File.Delete(dest);
                    }
                    catch
                    {
                    }
                    entry.ExtractToFile(dest, true);
                    count++;
                }
                catch (Exception ex)
                {
                    warnings.Add("Не удалось распаковать " + relative + ": " + ex.Message);
                }
            }
            return count;
        }

        private static bool IsSupportedVersion(ModVersion version)
        {
            foreach (string loader in version.Loaders)
            {
                if (string.Equals(loader, LoaderInstaller.Fabric, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(loader, LoaderInstaller.Forge, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static string NormalizeForgeVersion(string forgeVersion, string minecraft)
        {
            string prefix = minecraft + "-";
            if (forgeVersion.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return forgeVersion.Substring(prefix.Length);
            return forgeVersion;
        }

        private static string UniqueBuildId(string gameDir, string baseId, string projectId)
        {
            string id = baseId;
            for (int i = 2; i < 100; i++)
            {
                if (!Directory.Exists(GamePaths.VersionDir(gameDir, id))) return id;
                PackInfo existing = ReadInfo(LoaderInstaller.ProfileDir(gameDir, id));
                if (existing != null && string.Equals(existing.ProjectId, projectId, StringComparison.OrdinalIgnoreCase))
                    return id;
                id = baseId + "-" + i;
            }
            return baseId + "-" + DateTime.Now.Ticks.ToString(CultureInfo.InvariantCulture);
        }

        private static string SafeId(string value, string fallback)
        {
            var sb = new StringBuilder();
            if (!string.IsNullOrEmpty(value))
            {
                foreach (char c in value)
                {
                    if (char.IsLetterOrDigit(c)) sb.Append(char.ToLowerInvariant(c));
                    else if (sb.Length > 0 && sb[sb.Length - 1] != '-') sb.Append('-');
                }
            }
            while (sb.Length > 0 && sb[sb.Length - 1] == '-') sb.Length--;
            if (sb.Length == 0) sb.Append(fallback);
            if (sb.Length > 48) sb.Length = 48;
            return sb.ToString();
        }

        private static string NormalizeRelative(string path)
        {
            if (string.IsNullOrEmpty(path)) return "";
            return path.Replace('\\', '/').TrimStart('/');
        }

        private static string SafeCombine(string root, string relative)
        {
            if (string.IsNullOrEmpty(root) || string.IsNullOrEmpty(relative)) return null;
            try
            {
                string[] parts = relative.Split('/');
                foreach (string part in parts)
                {
                    if (part.Length == 0 || part == "." || part == "..") return null;
                    if (part.IndexOf(':') >= 0) return null;
                }

                string fullRoot = Path.GetFullPath(root);
                if (!fullRoot.EndsWith(Path.DirectorySeparatorChar.ToString(), StringComparison.Ordinal))
                    fullRoot += Path.DirectorySeparatorChar;

                string combined = Path.GetFullPath(Path.Combine(fullRoot,
                    relative.Replace('/', Path.DirectorySeparatorChar)));
                if (!combined.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)) return null;
                return combined;
            }
            catch
            {
                return null;
            }
        }

        private static void Report(IProgress<BatchProgress> progress, string status, long done, long total)
        {
            if (progress == null) return;
            var report = new BatchProgress();
            report.Status = status;
            report.FilesTotal = 1;
            report.FilesDone = total > 0 && done >= total ? 1 : 0;
            report.BytesDone = done;
            report.BytesTotal = total;
            progress.Report(report);
        }

        private static string LoaderLabel(string loader)
        {
            if (string.Equals(loader, LoaderInstaller.Fabric, StringComparison.OrdinalIgnoreCase)) return "Fabric";
            if (string.Equals(loader, LoaderInstaller.Forge, StringComparison.OrdinalIgnoreCase)) return "Forge";
            return loader;
        }

        private static string FormatMb(long size)
        {
            if (size <= 0) return "—";
            double mb = size / (1024.0 * 1024.0);
            if (mb >= 1) return mb.ToString("0.0", CultureInfo.InvariantCulture) + " МБ";
            return Math.Max(1, (int)Math.Round(size / 1024.0)) + " КБ";
        }
    }
}
