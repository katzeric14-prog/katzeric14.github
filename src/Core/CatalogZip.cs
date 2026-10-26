using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vexor.Core
{
    internal static class CatalogZip
    {
        private const string Base = "https://api.modrinth.com/v2";

        public static async Task<ModSearchResult> SearchAsync(string projectType, string query,
            string gameVersion, int offset, int limit, CancellationToken ct)
        {
            var facets = new StringBuilder();
            facets.Append("[[\"project_type:").Append(projectType).Append("\"]");
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
            CancellationToken ct)
        {
            string url = Base + "/project/" + Uri.EscapeDataString(projectId) + "/version";
            if (!string.IsNullOrEmpty(gameVersion))
                url += "?game_versions=" + Uri.EscapeDataString("[\"" + gameVersion + "\"]");

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
                    ChooseFile(versions[i]) != null) return versions[i];
            }
            for (int i = 0; i < versions.Count; i++)
            {
                if (ChooseFile(versions[i]) != null) return versions[i];
            }
            return null;
        }

        public static async Task<ModVersion> ResolveAsync(ModProject project, string gameVersion,
            string kind, CancellationToken ct)
        {
            if (project == null) throw new ArgumentNullException("project");
            List<ModVersion> versions = await GetVersionsAsync(project.ProjectId, gameVersion, ct).ConfigureAwait(false);
            ModVersion version = ChooseVersion(versions);
            if (version == null)
                throw new InvalidOperationException("Для «" + project.Title + "» нет " + kind +
                    " под Minecraft " + gameVersion + ".");
            return version;
        }

        public static HashSet<string> ReadInstalled(string gameDir, string indexFileName)
        {
            var installed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(gameDir)) return installed;
            Dictionary<string, ModIndexEntry> index = ModIndexStore.Read(Path.Combine(gameDir, indexFileName));
            foreach (var pair in index) installed.Add(pair.Key);
            return installed;
        }

        public static async Task<ModInstallResult> InstallAsync(ModProject project, ModVersion version,
            string gameDir, string folderName, string indexFileName, string logPrefix,
            Action<string> log, CancellationToken ct)
        {
            if (project == null) throw new ArgumentNullException("project");
            if (version == null) throw new ArgumentNullException("version");
            if (string.IsNullOrEmpty(gameDir)) throw new ArgumentNullException("gameDir");

            ModFile file = ChooseFile(version);
            if (file == null)
                throw new InvalidOperationException("У версии " + version.VersionNumber + " нет файла .zip.");

            string targetDir = Path.Combine(gameDir, folderName);
            Directory.CreateDirectory(targetDir);
            string indexPath = Path.Combine(gameDir, indexFileName);
            Dictionary<string, ModIndexEntry> index = ModIndexStore.Read(indexPath);

            string key = string.IsNullOrEmpty(version.ProjectId) ? project.ProjectId : version.ProjectId;
            if (string.IsNullOrEmpty(key)) key = project.Slug;
            if (string.IsNullOrEmpty(key)) key = version.Id;

            string fileName = ModIndexStore.SafeFileName(file.FileName);
            if (fileName.Length == 0) fileName = "pack.zip";
            if (!fileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)) fileName += ".zip";
            string dest = Path.Combine(targetDir, fileName);

            ModIndexEntry previous;
            if (index.TryGetValue(key, out previous) &&
                !string.Equals(previous.File, fileName, StringComparison.OrdinalIgnoreCase))
            {
                ModIndexStore.TryDelete(Path.Combine(targetDir, previous.File));
            }

            var result = new ModInstallResult();
            bool exists = File.Exists(dest) && (file.Size <= 0 || new FileInfo(dest).Length == file.Size);
            if (exists)
            {
                result.AlreadyPresent++;
                if (log != null) log("Уже установлен: " + fileName);
            }
            else
            {
                if (log != null) log(logPrefix + ": " + fileName + " · " + FormatMb(file.Size));
                await Web.DownloadFileAsync(file.Url, dest, file.Sha1, file.Size, null, ct).ConfigureAwait(false);
                result.Installed++;
            }

            result.Files.Add(fileName);
            var entry = new ModIndexEntry();
            entry.File = fileName;
            entry.Title = ModIndexStore.Clean(project.Title);
            index[key] = entry;
            ModIndexStore.Write(indexPath, index);

            if (log != null) log("Готово: " + fileName);
            return result;
        }

        private static ModFile ChooseFile(ModVersion version)
        {
            if (version == null || version.Files.Count == 0) return null;
            ModFile primary = version.PrimaryFile;
            if (primary != null && IsZip(primary.FileName) && !string.IsNullOrEmpty(primary.Url)) return primary;
            for (int i = 0; i < version.Files.Count; i++)
            {
                ModFile file = version.Files[i];
                if (IsZip(file.FileName) && !string.IsNullOrEmpty(file.Url)) return file;
            }
            return null;
        }

        private static bool IsZip(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase);
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
