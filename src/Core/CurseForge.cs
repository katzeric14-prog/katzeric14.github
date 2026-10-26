using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vexor.Core
{
    public sealed class CurseForgeException : Exception
    {
        public CurseForgeException(string message) : base(message)
        {
        }

        public CurseForgeException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    public sealed class CfCategory
    {
        public int Id;
        public string Name;
        public string Slug;
    }

    public static class CurseForgeApi
    {
        internal static string BaseUrl = "https://api.curseforge.com/v1";

        private const int GameId = 432;
        private const int ModsClassId = 6;

        private static string _apiKey = "";
        private static List<CfCategory> _categories;

        public static bool HasKey
        {
            get { return _apiKey.Length > 0; }
        }

        public static void Configure(string key)
        {
            _apiKey = (key ?? "").Trim();
        }

        public static async Task<List<CfCategory>> GetCategoriesAsync(CancellationToken ct)
        {
            if (_categories != null) return _categories;

            string cachePath = Path.Combine(AppPaths.MetaDir, "curseforge-categories.json");
            try
            {
                if (File.Exists(cachePath))
                {
                    List<CfCategory> cached = ParseCategories(JsonParser.Parse(File.ReadAllText(cachePath)));
                    if (cached.Count > 0)
                    {
                        _categories = cached;
                        return cached;
                    }
                }
            }
            catch
            {
            }

            string url = BaseUrl + "/categories?gameId=" + GameId + "&classId=" + ModsClassId;
            string json = await GetAsync(url, ct).ConfigureAwait(false);
            List<CfCategory> categories = ParseCategories(JsonParser.Parse(json));
            if (categories.Count == 0)
                throw new CurseForgeException("CurseForge не вернул категории модов.");
            _categories = categories;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(cachePath));
                File.WriteAllText(cachePath, json, new UTF8Encoding(false));
            }
            catch
            {
            }
            return categories;
        }

        public static async Task<ModSearchResult> SearchAsync(string query, string gameVersion, string loader,
            int categoryId, int offset, int limit, CancellationToken ct)
        {
            var url = new StringBuilder(BaseUrl);
            url.Append("/mods/search?gameId=").Append(GameId);
            url.Append("&classId=").Append(ModsClassId);
            url.Append("&index=").Append(offset);
            url.Append("&pageSize=").Append(limit);
            url.Append("&sortField=6&sortOrder=desc");
            if (!string.IsNullOrEmpty(query)) url.Append("&searchFilter=").Append(Uri.EscapeDataString(query));
            if (!string.IsNullOrEmpty(gameVersion))
                url.Append("&gameVersion=").Append(Uri.EscapeDataString(gameVersion));
            int loaderType = LoaderType(loader);
            if (loaderType > 0) url.Append("&modLoaderType=").Append(loaderType);
            if (categoryId > 0) url.Append("&categoryId=").Append(categoryId);

            JsonValue root = JsonParser.Parse(await GetAsync(url.ToString(), ct).ConfigureAwait(false));
            var result = new ModSearchResult();
            result.TotalHits = root["pagination"]["totalCount"].AsInt();
            result.Offset = root["pagination"]["index"].AsInt(offset);
            result.Limit = root["pagination"]["pageSize"].AsInt(limit);
            foreach (JsonValue item in root["data"].Items)
            {
                ModProject project = ParseProject(item);
                if (project != null) result.Hits.Add(project);
            }
            return result;
        }

        public static async Task<ModVersion> ResolveAsync(ModProject project, string gameVersion, string loader,
            CancellationToken ct)
        {
            if (project == null) throw new ArgumentNullException("project");
            string modId = project.ApiId;
            if (string.IsNullOrEmpty(modId)) modId = StripModId(project.ProjectId);
            if (string.IsNullOrEmpty(modId))
                throw new CurseForgeException("Некорректный идентификатор мода CurseForge.");

            List<ModVersion> files = await GetFilesAsync(modId, gameVersion, loader, ct).ConfigureAwait(false);
            if (files.Count == 0 && !string.IsNullOrEmpty(loader))
                files = await GetFilesAsync(modId, gameVersion, null, ct).ConfigureAwait(false);

            ModVersion version = ChooseVersion(files);
            if (version == null)
                throw new CurseForgeException("Для " + project.Title + " нет файла под " +
                    gameVersion + " (" + LoaderLabel(loader) + ").");
            return version;
        }

        public static async Task<ModInstallResult> InstallAsync(ModVersion version, InstalledBuild build,
            Action<string> log, CancellationToken ct)
        {
            if (version == null) throw new ArgumentNullException("version");
            if (build == null) throw new ArgumentNullException("build");
            Directory.CreateDirectory(build.ProfileDir);
            Directory.CreateDirectory(build.ModsDir);

            string indexPath = Path.Combine(build.ProfileDir, "vexor-mods.txt");
            Dictionary<string, ModIndexEntry> index = ModIndexStore.Read(indexPath);
            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var result = new ModInstallResult();
            await InstallVersionAsync(version, build, false, 0, visited, index, indexPath, result, log, ct)
                .ConfigureAwait(false);
            return result;
        }

        private static async Task InstallVersionAsync(ModVersion version, InstalledBuild build, bool dependency,
            int depth, HashSet<string> visited, Dictionary<string, ModIndexEntry> index, string indexPath,
            ModInstallResult result, Action<string> log, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (version == null || depth > 6) return;

            string key = version.ProjectId;
            if (string.IsNullOrEmpty(key)) key = "cf:" + version.Id;
            if (string.IsNullOrEmpty(key) || !visited.Add(key)) return;

            ModFile file = version.PrimaryFile;
            if (file == null || string.IsNullOrEmpty(file.FileName))
            {
                Warn(result, log, "Пропущен " + version.Name + ": нет файла.");
                return;
            }

            if (string.IsNullOrEmpty(file.Url)) file.Url = await ResolveFileUrlAsync(file, ct).ConfigureAwait(false);
            if (string.IsNullOrEmpty(file.Url))
            {
                string message = "Автор " + version.Name + " запретил прямое скачивание. " +
                    "Скачайте файл вручную со страницы мода на CurseForge.";
                if (!dependency) throw new CurseForgeException(message);
                Warn(result, log, message);
                return;
            }

            string fileName = ModIndexStore.SafeFileName(file.FileName);
            if (fileName.Length == 0) return;
            string dest = Path.Combine(build.ModsDir, fileName);

            ModIndexEntry previous;
            if (index.TryGetValue(key, out previous) && !string.Equals(previous.File, fileName, StringComparison.OrdinalIgnoreCase))
                ModIndexStore.TryDelete(Path.Combine(build.ModsDir, previous.File));

            bool exists = File.Exists(dest) && (file.Size <= 0 || new FileInfo(dest).Length == file.Size);
            if (exists)
            {
                result.AlreadyPresent++;
                if (log != null && !dependency) log("Уже установлен: " + fileName);
            }
            else
            {
                if (log != null)
                    log((dependency ? "Зависимость: " : "Мод: ") + fileName + " · " + FormatMb(file.Size));
                try
                {
                    await Web.DownloadFileAsync(file.Url, dest, file.Sha1, file.Size, null, ct).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    if (!dependency)
                        throw new CurseForgeException("Не удалось скачать " + fileName + ". " +
                            "Возможно, автор ограничил сторонние загрузки: " + Short(ex.Message), ex);
                    Warn(result, log, "Зависимость " + fileName + " не скачана: " + Short(ex.Message));
                    return;
                }
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
                ModDependency info = version.Dependencies[i];
                if (!string.Equals(info.Type, "required", StringComparison.OrdinalIgnoreCase)) continue;

                string dependencyId = StripModId(info.ProjectId);
                ModVersion required = null;
                try
                {
                    if (!string.IsNullOrEmpty(dependencyId))
                    {
                        if (!string.IsNullOrEmpty(info.VersionId))
                        {
                            required = await GetFileAsync(dependencyId, info.VersionId, ct).ConfigureAwait(false);
                        }
                        else
                        {
                            List<ModVersion> files = await GetFilesAsync(dependencyId, build.GameVersion,
                                build.Loader, ct).ConfigureAwait(false);
                            if (files.Count == 0 && !string.IsNullOrEmpty(build.Loader))
                                files = await GetFilesAsync(dependencyId, build.GameVersion, null, ct).ConfigureAwait(false);
                            required = ChooseVersion(files);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    Warn(result, log, "Зависимость #" + dependencyId + " не загружена: " + Short(ex.Message));
                }

                if (required == null)
                {
                    Warn(result, log, "Не найдена обязательная зависимость CurseForge #" + dependencyId + ".");
                    continue;
                }
                await InstallVersionAsync(required, build, true, depth + 1, visited, index, indexPath,
                    result, log, ct).ConfigureAwait(false);
            }
        }

        private static async Task<List<ModVersion>> GetFilesAsync(string modId, string gameVersion, string loader,
            CancellationToken ct)
        {
            var url = new StringBuilder(BaseUrl);
            url.Append("/mods/").Append(Uri.EscapeDataString(modId)).Append("/files?index=0&pageSize=50");
            if (!string.IsNullOrEmpty(gameVersion))
                url.Append("&gameVersion=").Append(Uri.EscapeDataString(gameVersion));
            int loaderType = LoaderType(loader);
            if (loaderType > 0) url.Append("&modLoaderType=").Append(loaderType);

            JsonValue root = JsonParser.Parse(await GetAsync(url.ToString(), ct).ConfigureAwait(false));
            var versions = new List<ModVersion>();
            foreach (JsonValue item in root["data"].Items)
            {
                ModVersion version = ParseVersion(item, modId);
                if (version != null) versions.Add(version);
            }
            return versions;
        }

        private static async Task<ModVersion> GetFileAsync(string modId, string fileId, CancellationToken ct)
        {
            string url = BaseUrl + "/mods/" + Uri.EscapeDataString(modId) + "/files/" +
                Uri.EscapeDataString(fileId);
            JsonValue root = JsonParser.Parse(await GetAsync(url, ct).ConfigureAwait(false));
            return ParseVersion(root["data"], modId);
        }

        private static async Task<string> ResolveFileUrlAsync(ModFile file, CancellationToken ct)
        {
            if (!string.IsNullOrEmpty(file.Url)) return file.Url;
            if (!file.Available) return "";

            try
            {
                string url = BaseUrl + "/mods/" + Uri.EscapeDataString(file.ModId) + "/files/" +
                    Uri.EscapeDataString(file.Id) + "/download-url";
                JsonValue root = JsonParser.Parse(await GetAsync(url, ct).ConfigureAwait(false));
                string direct = root["data"].AsString();
                if (!string.IsNullOrEmpty(direct))
                {
                    file.Url = direct;
                    return direct;
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception)
            {
            }

            file.Url = EdgeUrl(file.Id, file.FileName);
            return file.Url;
        }

        internal static string EdgeUrl(string fileId, string fileName)
        {
            long id;
            if (!long.TryParse(fileId, out id) || id <= 0 || string.IsNullOrEmpty(fileName)) return "";
            return "https://edge.forgecdn.net/files/" + (id / 1000) + "/" + (id % 1000) + "/" +
                Uri.EscapeDataString(fileName);
        }

        private static async Task<string> GetAsync(string url, CancellationToken ct)
        {
            var headers = new Dictionary<string, string>();
            headers["Accept"] = "application/json";
            if (HasKey) headers["x-api-key"] = _apiKey;

            try
            {
                return await Web.GetStringAsync(url, headers, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (!HasKey)
                    throw new CurseForgeException("Ключ CurseForge API не задан. " +
                        "Укажите его в Настройках → CurseForge.", ex);
                string message = Short(ex.Message);
                string chain = "";
                for (Exception inner = ex; inner != null; inner = inner.InnerException)
                    chain += inner.Message + " ";
                if (chain.IndexOf("403", StringComparison.Ordinal) >= 0 ||
                    chain.IndexOf("Forbidden", StringComparison.OrdinalIgnoreCase) >= 0)
                    throw new CurseForgeException("CurseForge отклонил ключ API (403). " +
                        "Проверьте ключ в Настройках.", ex);
                throw new CurseForgeException("CurseForge API: " + message, ex);
            }
        }

        private static ModProject ParseProject(JsonValue json)
        {
            string id = json["id"].AsString();
            if (string.IsNullOrEmpty(id)) return null;

            var project = new ModProject();
            project.ApiId = id;
            project.ProjectId = "cf:" + id;
            project.Source = "curseforge";
            project.Slug = json["slug"].AsString(id);
            project.Title = json["name"].AsString(project.Slug);
            project.Description = json["summary"].AsString("");
            project.Author = json["authors"][0]["name"].AsString("");
            project.IconUrl = json["logo"]["thumbnailUrl"].AsString("");
            if (string.IsNullOrEmpty(project.IconUrl)) project.IconUrl = json["logo"]["url"].AsString("");
            project.Downloads = json["downloadCount"].AsLong();
            foreach (JsonValue category in json["categories"].Items)
            {
                string name = category["name"].AsString();
                if (!string.IsNullOrEmpty(name)) project.Categories.Add(name);
            }
            return project;
        }

        private static ModVersion ParseVersion(JsonValue json, string fallbackModId)
        {
            if (!json.IsObject) return null;
            string fileId = json["id"].AsString();
            if (string.IsNullOrEmpty(fileId)) return null;

            string modId = json["modId"].AsString(fallbackModId);
            var version = new ModVersion();
            version.Id = fileId;
            version.ProjectId = "cf:" + modId;
            version.Source = "curseforge";
            version.Name = json["displayName"].AsString(json["fileName"].AsString(""));
            version.VersionNumber = version.Name;
            int releaseType = json["releaseType"].AsInt(1);
            version.VersionType = releaseType == 1 ? "release" : (releaseType == 2 ? "beta" : "alpha");

            foreach (JsonValue item in json["gameVersions"].Items)
            {
                string value = item.AsString();
                if (!string.IsNullOrEmpty(value)) version.GameVersions.Add(value);
            }

            var file = new ModFile();
            file.Id = fileId;
            file.ModId = modId;
            file.Url = json["downloadUrl"].AsString();
            file.FileName = json["fileName"].AsString();
            file.Sha1 = json["hashes"]["sha1"].AsString();
            file.Size = json["fileLength"].AsLong();
            file.Primary = true;
            file.Available = json["isAvailable"].AsBool(true);
            if (!string.IsNullOrEmpty(file.FileName)) version.Files.Add(file);

            foreach (JsonValue item in json["dependencies"].Items)
            {
                int relation = item["relationType"].AsInt();
                int depModId = item["modId"].AsInt();
                if (relation != 3 || depModId <= 0) continue;
                var dependency = new ModDependency();
                dependency.ProjectId = "cf:" + depModId;
                dependency.VersionId = item["fileId"].AsString();
                dependency.Type = "required";
                version.Dependencies.Add(dependency);
            }
            return version;
        }

        private static List<CfCategory> ParseCategories(JsonValue root)
        {
            var all = new List<CfCategory>();
            foreach (JsonValue item in root["data"].Items)
            {
                var category = new CfCategory();
                category.Id = item["id"].AsInt();
                category.Name = item["name"].AsString("");
                category.Slug = item["slug"].AsString("");
                if (category.Id <= 0 || category.Name.Length == 0) continue;
                if (item["parentCategoryId"].AsInt(ModsClassId) == ModsClassId) all.Add(category);
            }
            if (all.Count == 0)
            {
                foreach (JsonValue item in root["data"].Items)
                {
                    var category = new CfCategory();
                    category.Id = item["id"].AsInt();
                    category.Name = item["name"].AsString("");
                    if (category.Id > 0 && category.Name.Length > 0) all.Add(category);
                }
            }
            all.Sort(delegate(CfCategory a, CfCategory b)
            {
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
            return all;
        }

        private static ModVersion ChooseVersion(List<ModVersion> versions)
        {
            if (versions == null || versions.Count == 0) return null;
            ModVersion bestRelease = null;
            ModVersion bestAny = null;
            long bestReleaseId = -1;
            long bestAnyId = -1;
            foreach (ModVersion version in versions)
            {
                long id;
                if (!long.TryParse(version.Id, out id)) id = 0;
                if (bestAny == null || id > bestAnyId)
                {
                    bestAny = version;
                    bestAnyId = id;
                }
                if (string.Equals(version.VersionType, "release", StringComparison.OrdinalIgnoreCase) && id > bestReleaseId)
                {
                    bestRelease = version;
                    bestReleaseId = id;
                }
            }
            return bestRelease ?? bestAny;
        }

        private static int LoaderType(string loader)
        {
            if (string.Equals(loader, "forge", StringComparison.OrdinalIgnoreCase)) return 1;
            if (string.Equals(loader, "fabric", StringComparison.OrdinalIgnoreCase)) return 4;
            if (string.Equals(loader, "quilt", StringComparison.OrdinalIgnoreCase)) return 5;
            if (string.Equals(loader, "neoforge", StringComparison.OrdinalIgnoreCase)) return 6;
            return 0;
        }

        private static string StripModId(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            if (value.StartsWith("cf:", StringComparison.OrdinalIgnoreCase)) return value.Substring(3);
            return value;
        }

        private static string LoaderLabel(string loader)
        {
            if (string.Equals(loader, "fabric", StringComparison.OrdinalIgnoreCase)) return "Fabric";
            if (string.Equals(loader, "forge", StringComparison.OrdinalIgnoreCase)) return "Forge";
            if (string.Equals(loader, "neoforge", StringComparison.OrdinalIgnoreCase)) return "NeoForge";
            if (string.Equals(loader, "quilt", StringComparison.OrdinalIgnoreCase)) return "Quilt";
            return loader;
        }

        private static string FormatMb(long size)
        {
            if (size <= 0) return "—";
            double mb = size / (1024.0 * 1024.0);
            if (mb >= 1) return mb.ToString("0.0") + " МБ";
            return Math.Max(1, (int)Math.Round(size / 1024.0)) + " КБ";
        }

        private static string Short(string text)
        {
            if (string.IsNullOrEmpty(text)) return "неизвестная ошибка";
            int nl = text.IndexOf('\n');
            if (nl >= 0) text = text.Substring(0, nl);
            if (text.Length > 180) text = text.Substring(0, 177) + "…";
            return text.Trim();
        }

        private static void Warn(ModInstallResult result, Action<string> log, string message)
        {
            result.Warnings.Add(message);
            if (log != null) log(message);
        }
    }
}
