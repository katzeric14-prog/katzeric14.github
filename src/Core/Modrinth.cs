using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vexor.Core
{
    public static class ModrinthApi
    {
        private const string Base = "https://api.modrinth.com/v2";

        public static async Task<ModSearchResult> SearchAsync(string query, string gameVersion, string loader,
            int offset, int limit, CancellationToken ct)
        {
            var facets = new StringBuilder();
            facets.Append("[[\"project_type:mod\"]");
            if (!string.IsNullOrEmpty(loader)) facets.Append(",[\"categories:").Append(loader).Append("\"]");
            if (!string.IsNullOrEmpty(gameVersion)) facets.Append(",[\"versions:").Append(gameVersion).Append("\"]");
            facets.Append("]");

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
                ModProject project = ParseProject(hit);
                if (project != null) result.Hits.Add(project);
            }
            return result;
        }

        public static async Task<List<ModVersion>> GetVersionsAsync(string projectId, string gameVersion,
            string loader, CancellationToken ct)
        {
            string url = Base + "/project/" + Uri.EscapeDataString(projectId) + "/version" +
                "?game_versions=" + Uri.EscapeDataString("[\"" + gameVersion + "\"]") +
                "&loaders=" + Uri.EscapeDataString("[\"" + loader + "\"]");
            JsonValue root = JsonParser.Parse(await Web.GetStringAsync(url, ct).ConfigureAwait(false));
            var versions = new List<ModVersion>();
            foreach (JsonValue item in root.Items)
            {
                ModVersion version = ParseVersion(item);
                if (version != null) versions.Add(version);
            }
            return versions;
        }

        public static async Task<ModVersion> GetVersionAsync(string id, CancellationToken ct)
        {
            string url = Base + "/version/" + Uri.EscapeDataString(id);
            return ParseVersion(JsonParser.Parse(await Web.GetStringAsync(url, ct).ConfigureAwait(false)));
        }

        public static ModVersion ChooseVersion(List<ModVersion> versions)
        {
            if (versions == null || versions.Count == 0) return null;
            for (int i = 0; i < versions.Count; i++)
            {
                if (string.Equals(versions[i].VersionType, "release", StringComparison.OrdinalIgnoreCase))
                    return versions[i];
            }
            return versions[0];
        }

        public static async Task<ModVersion> ResolveAsync(ModProject project, string gameVersion, string loader,
            CancellationToken ct)
        {
            if (project == null) throw new ArgumentNullException("project");
            List<ModVersion> versions = await GetVersionsAsync(project.ProjectId, gameVersion, loader, ct).ConfigureAwait(false);
            ModVersion version = ChooseVersion(versions);
            if (version == null)
                throw new InvalidOperationException("Для " + project.Title + " нет версии под " +
                    gameVersion + " (" + LoaderLabel(loader) + ").");
            return version;
        }

        public static async Task<ModInstallResult> InstallAsync(ModVersion version, InstalledBuild build,
            Action<string> log, CancellationToken ct)
        {
            if (version == null) throw new ArgumentNullException("version");
            if (build == null) throw new ArgumentNullException("build");
            LoaderInstaller.PrepareProfile(build);

            string indexPath = Path.Combine(build.ProfileDir, "vexor-mods.txt");
            Dictionary<string, ModIndexEntry> index = ModIndexStore.Read(indexPath);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new ModInstallResult();
            await InstallVersionAsync(version, build, false, 0, visited, index, indexPath, result, log, ct)
                .ConfigureAwait(false);
            return result;
        }

        public static HashSet<string> ReadInstalledSlugs(InstalledBuild build)
        {
            var slugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (build == null) return slugs;
            string path = Path.Combine(build.ProfileDir, "vexor-mods.txt");
            Dictionary<string, ModIndexEntry> index = ModIndexStore.Read(path);
            foreach (var pair in index) slugs.Add(pair.Key);
            return slugs;
        }

        private static async Task InstallVersionAsync(ModVersion version, InstalledBuild build, bool dependency,
            int depth, HashSet<string> visited, Dictionary<string, ModIndexEntry> index, string indexPath,
            ModInstallResult result, Action<string> log, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (version == null || depth > 6) return;
            string key = string.IsNullOrEmpty(version.ProjectId) ? version.Id : version.ProjectId;
            if (string.IsNullOrEmpty(key) || !visited.Add(key)) return;

            ModFile file = version.PrimaryFile;
            if (file == null || string.IsNullOrEmpty(file.Url) || string.IsNullOrEmpty(file.FileName))
            {
                if (log != null) log("Пропущен " + version.Name + ": нет файла.");
                return;
            }

            string fileName = ModIndexStore.SafeFileName(file.FileName);
            if (fileName.Length == 0) return;
            string dest = Path.Combine(build.ModsDir, fileName);

            ModIndexEntry previous;
            if (index.TryGetValue(key, out previous) && !string.Equals(previous.File, fileName, StringComparison.OrdinalIgnoreCase))
                ModIndexStore.TryDelete(Path.Combine(build.ModsDir, previous.File));

            bool exists = File.Exists(dest) &&
                (file.Size <= 0 || new FileInfo(dest).Length == file.Size);
            if (exists)
            {
                result.AlreadyPresent++;
                if (log != null && !dependency) log("Уже установлен: " + fileName);
            }
            else
            {
                if (log != null)
                    log((dependency ? "Зависимость: " : "Мод: ") + fileName + " · " + FormatMb(file.Size));
                await Web.DownloadFileAsync(file.Url, dest, file.Sha1, file.Size, null, ct).ConfigureAwait(false);
                result.Installed++;
            }

            result.Files.Add(fileName);
            var entry = new ModIndexEntry();
            entry.File = fileName;
            entry.Title = ModIndexStore.Clean(version.Name);
            index[key] = entry;
            ModIndexStore.Write(indexPath, index);

            for (int i = 0; i < version.Dependencies.Count; i++)
            {
                ModDependency dependencyInfo = version.Dependencies[i];
                if (!string.Equals(dependencyInfo.Type, "required", StringComparison.OrdinalIgnoreCase)) continue;
                ModVersion required = null;
                try
                {
                    if (!string.IsNullOrEmpty(dependencyInfo.VersionId))
                    {
                        required = await GetVersionAsync(dependencyInfo.VersionId, ct).ConfigureAwait(false);
                    }
                    else if (!string.IsNullOrEmpty(dependencyInfo.ProjectId))
                    {
                        List<ModVersion> versions = await GetVersionsAsync(dependencyInfo.ProjectId,
                            build.GameVersion, build.Loader, ct).ConfigureAwait(false);
                        required = ChooseVersion(versions);
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (log != null) log("Зависимость не загружена: " + ex.Message);
                }

                if (required == null)
                {
                    if (log != null) log("Не найдена обязательная зависимость " + dependencyInfo.ProjectId + ".");
                    continue;
                }
                await InstallVersionAsync(required, build, true, depth + 1, visited, index, indexPath, result, log, ct)
                    .ConfigureAwait(false);
            }
        }

        internal static ModProject ParseProject(JsonValue json)
        {
            string id = json["project_id"].AsString();
            if (string.IsNullOrEmpty(id)) id = json["slug"].AsString();
            if (string.IsNullOrEmpty(id)) return null;

            var project = new ModProject();
            project.ProjectId = id;
            project.ApiId = id;
            project.Source = "modrinth";
            project.Slug = json["slug"].AsString(id);
            project.Title = json["title"].AsString(id);
            project.Description = json["description"].AsString("");
            project.Author = json["author"].AsString("");
            project.IconUrl = json["icon_url"].AsString("");
            project.Downloads = json["downloads"].AsLong();
            foreach (JsonValue category in json["categories"].Items)
            {
                string value = category.AsString();
                if (value != null) project.Categories.Add(value);
            }
            return project;
        }

        internal static ModVersion ParseVersion(JsonValue json)
        {
            if (!json.IsObject) return null;
            var version = new ModVersion();
            version.Id = json["id"].AsString();
            version.ProjectId = json["project_id"].AsString();
            version.Name = json["name"].AsString("");
            version.VersionNumber = json["version_number"].AsString("");
            version.VersionType = json["version_type"].AsString("release");
            foreach (JsonValue item in json["game_versions"].Items)
            {
                string value = item.AsString();
                if (value != null) version.GameVersions.Add(value);
            }
            foreach (JsonValue item in json["loaders"].Items)
            {
                string value = item.AsString();
                if (value != null) version.Loaders.Add(value);
            }
            foreach (JsonValue item in json["files"].Items)
            {
                var file = new ModFile();
                file.Url = item["url"].AsString();
                file.FileName = item["filename"].AsString();
                file.Sha1 = item["hashes"]["sha1"].AsString();
                file.Size = item["size"].AsLong();
                file.Primary = item["primary"].AsBool();
                if (!string.IsNullOrEmpty(file.Url)) version.Files.Add(file);
            }
            foreach (JsonValue item in json["dependencies"].Items)
            {
                var dependency = new ModDependency();
                dependency.ProjectId = item["project_id"].AsString();
                dependency.VersionId = item["version_id"].AsString();
                dependency.Type = item["dependency_type"].AsString();
                if (!string.IsNullOrEmpty(dependency.ProjectId) || !string.IsNullOrEmpty(dependency.VersionId))
                    version.Dependencies.Add(dependency);
            }
            return string.IsNullOrEmpty(version.Id) ? null : version;
        }

        private static string LoaderLabel(string loader)
        {
            if (string.Equals(loader, "fabric", StringComparison.OrdinalIgnoreCase)) return "Fabric";
            if (string.Equals(loader, "forge", StringComparison.OrdinalIgnoreCase)) return "Forge";
            if (string.Equals(loader, "neoforge", StringComparison.OrdinalIgnoreCase)) return "NeoForge";
            return loader;
        }

        private static string FormatMb(long size)
        {
            if (size <= 0) return "—";
            double mb = size / (1024.0 * 1024.0);
            if (mb >= 1) return mb.ToString("0.0") + " МБ";
            return Math.Max(1, (int)Math.Round(size / 1024.0)) + " КБ";
        }
    }
}
