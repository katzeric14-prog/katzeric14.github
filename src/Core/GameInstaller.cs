using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Vexor.Core
{
    public sealed class InstallResult
    {
        public List<string> Classpath = new List<string>();
        public string ClientJar;
        public string NativesDir;
        public string AssetsDir;
        public string AssetIndexId;
        public string GameAssetsDir;
    }

    public static class GameInstaller
    {
        private sealed class NativeArchive
        {
            public string Jar;
            public List<string> Exclude;
        }

        public static async Task<InstallResult> InstallAsync(GameVersion version, string gameDir,
            FeatureSet features, IProgress<BatchProgress> progress, Action<string> log, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(gameDir)) throw new ArgumentException("Не задана папка игры.");
            if (version == null) throw new ArgumentNullException("version");
            if (version.Client == null)
                throw new InvalidOperationException("Для версии " + version.Id + " нет клиентского файла.");

            Directory.CreateDirectory(gameDir);
            string versionDir = GamePaths.VersionDir(gameDir, version.Id);
            Directory.CreateDirectory(versionDir);
            string librariesDir = GamePaths.LibrariesDir(gameDir);
            Directory.CreateDirectory(librariesDir);
            string assetsDir = GamePaths.AssetsDir(gameDir);
            Directory.CreateDirectory(assetsDir);

            var result = new InstallResult();
            result.NativesDir = GamePaths.NativesDir(gameDir, version.Id);
            result.AssetsDir = assetsDir;
            result.ClientJar = GamePaths.ClientJar(gameDir, version.Id);

            var downloadItems = new List<DownloadItem>();
            var clientItem = new DownloadItem(version.Client.Url, result.ClientJar, version.Client.Sha1, version.Client.Size);
            downloadItems.Add(clientItem);

            var classpath = new List<string>();
            classpath.Add(result.ClientJar);

            var nativeArchives = new List<NativeArchive>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (LibraryInfo library in version.Libraries)
            {
                if (!Rules.Allowed(library.Rules, features)) continue;

                if (library.HasNatives && library.Natives.ContainsKey(OsInfo.Name))
                {
                    string classifier = library.Natives[OsInfo.Name];
                    classifier = classifier.Replace("${arch}", OsInfo.Arch == "x64" ? "64" : "32");

                    DownloadInfo nativeInfo;
                    if (!library.Classifiers.TryGetValue(classifier, out nativeInfo) || nativeInfo == null) continue;

                    string relative = string.IsNullOrEmpty(nativeInfo.Path)
                        ? MavenPath(library.Name, classifier)
                        : nativeInfo.Path;
                    if (string.IsNullOrEmpty(relative)) continue;

                    string nativePath = Path.Combine(librariesDir, relative.Replace('/', Path.DirectorySeparatorChar));
                    if (!seen.Add(nativePath)) continue;
                    downloadItems.Add(new DownloadItem(nativeInfo.Url, nativePath, nativeInfo.Sha1, nativeInfo.Size));
                    nativeArchives.Add(new NativeArchive { Jar = nativePath, Exclude = library.ExtractExclude });
                    continue;
                }

                DownloadInfo artifact = library.Artifact;
                string relativeArtifact;
                if (artifact != null)
                {
                    relativeArtifact = artifact.Path;
                    if (string.IsNullOrEmpty(relativeArtifact)) relativeArtifact = MavenPath(library.Name, null);
                }
                else
                {
                    relativeArtifact = MavenPath(library.Name, null);
                    if (string.IsNullOrEmpty(relativeArtifact)) continue;
                    string baseUrl = string.IsNullOrEmpty(library.Url) ? "https://libraries.minecraft.net/" : library.Url;
                    if (!baseUrl.EndsWith("/", StringComparison.Ordinal)) baseUrl += "/";
                    artifact = new DownloadInfo();
                    artifact.Url = baseUrl + relativeArtifact;
                    artifact.Path = relativeArtifact;
                    artifact.Sha1 = library.Sha1;
                }

                if (string.IsNullOrEmpty(relativeArtifact)) continue;
                string jarPath = Path.Combine(librariesDir, relativeArtifact.Replace('/', Path.DirectorySeparatorChar));
                if (!seen.Add(jarPath)) continue;
                if (string.IsNullOrEmpty(artifact.Url))
                {
                    if (!File.Exists(jarPath))
                        throw new FileNotFoundException("Нет файла " + relativeArtifact + ". Переустановите загрузчик.");
                }
                else
                {
                    downloadItems.Add(new DownloadItem(artifact.Url, jarPath, artifact.Sha1, artifact.Size));
                }
                classpath.Add(jarPath);
            }

            if (log != null) log("Проверка клиента и библиотек (" + downloadItems.Count + " файлов)…");
            await Web.DownloadBatchAsync(downloadItems, 8, progress, "Клиент и библиотеки", ct).ConfigureAwait(false);

            if (log != null) log("Подготовка natives…");
            PrepareNatives(result.NativesDir);
            foreach (NativeArchive archive in nativeArchives)
            {
                if (File.Exists(archive.Jar)) ExtractNatives(archive, result.NativesDir);
            }
            foreach (string sub in new[] { "java", "jna", "lwjgl", "netty" })
                Directory.CreateDirectory(Path.Combine(result.NativesDir, sub));

            result.Classpath = classpath;

            await InstallAssetsAsync(version, gameDir, assetsDir, result, progress, log, ct).ConfigureAwait(false);
            return result;
        }

        private static async Task InstallAssetsAsync(GameVersion version, string gameDir, string assetsDir,
            InstallResult result, IProgress<BatchProgress> progress, Action<string> log, CancellationToken ct)
        {
            if (version.AssetIndex == null)
            {
                result.AssetIndexId = version.Assets ?? "";
                result.GameAssetsDir = assetsDir;
                return;
            }

            string indexId = version.AssetIndex.Id;
            if (string.IsNullOrEmpty(indexId)) indexId = version.Assets;
            result.AssetIndexId = string.IsNullOrEmpty(version.Assets) ? indexId : version.Assets;

            string indexPath = GamePaths.AssetIndexPath(gameDir, indexId);
            JsonValue index = null;

            if (File.Exists(indexPath))
            {
                try
                {
                    index = JsonParser.Parse(File.ReadAllText(indexPath));
                }
                catch
                {
                    index = null;
                }
            }

            if (index == null)
            {
                if (string.IsNullOrEmpty(version.AssetIndex.Url))
                {
                    result.GameAssetsDir = assetsDir;
                    return;
                }
                if (log != null) log("Загрузка индекса ресурсов " + indexId + "…");
                await Web.DownloadFileAsync(version.AssetIndex.Url, indexPath, version.AssetIndex.Sha1,
                    version.AssetIndex.Size, null, ct).ConfigureAwait(false);
                index = JsonParser.Parse(File.ReadAllText(indexPath));
            }

            var assetItems = new List<DownloadItem>();
            var seenHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            long totalBytes = 0;
            int count = 0;

            foreach (var pair in index["objects"].Properties)
            {
                JsonValue obj = pair.Value;
                string hash = obj["hash"].AsString();
                long size = obj["size"].AsLong();
                if (string.IsNullOrEmpty(hash) || hash.Length < 3) continue;
                if (!seenHashes.Add(hash)) continue;

                string relative = Path.Combine(hash.Substring(0, 2), hash);
                string dest = Path.Combine(assetsDir, "objects", relative);
                string url = "https://resources.download.minecraft.net/" + hash.Substring(0, 2) + "/" + hash;
                assetItems.Add(new DownloadItem(url, dest, hash, size));
                totalBytes += size;
                count++;
            }

            if (log != null) log("Ресурсы игры: " + count + " файлов · " + BatchProgress.FormatMb(totalBytes));
            await Web.DownloadBatchAsync(assetItems, 10, progress, "Ресурсы игры", ct).ConfigureAwait(false);

            bool isVirtual = index["virtual"].AsBool();
            bool mapToResources = index["map_to_resources"].AsBool();
            string gameAssetsDir = assetsDir;

            if (isVirtual || mapToResources)
            {
                gameAssetsDir = isVirtual
                    ? Path.Combine(assetsDir, "virtual", indexId)
                    : Path.Combine(gameDir, "resources");
                if (log != null) log(isVirtual ? "Подготовка virtual-ресурсов…" : "Копирование ресурсов в папку игры…");

                foreach (var pair in index["objects"].Properties)
                {
                    string name = pair.Key.Replace('\\', '/');
                    if (name.Contains("..")) continue;
                    string hash = pair.Value["hash"].AsString();
                    long size = pair.Value["size"].AsLong();
                    if (string.IsNullOrEmpty(hash) || hash.Length < 3) continue;

                    string source = Path.Combine(assetsDir, "objects", hash.Substring(0, 2), hash);
                    string target = Path.Combine(gameAssetsDir, name.Replace('/', Path.DirectorySeparatorChar));
                    try
                    {
                        if (File.Exists(target) && new FileInfo(target).Length == size) continue;
                        string parent = Path.GetDirectoryName(target);
                        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                        File.Copy(source, target, true);
                    }
                    catch
                    {
                    }
                }
            }

            result.GameAssetsDir = gameAssetsDir;
        }

        private static void PrepareNatives(string nativesDir)
        {
            try
            {
                if (Directory.Exists(nativesDir)) Directory.Delete(nativesDir, true);
            }
            catch
            {
            }
            Directory.CreateDirectory(nativesDir);
        }

        private static void ExtractNatives(NativeArchive archive, string nativesDir)
        {
            try
            {
                using (ZipArchive zip = ZipFile.OpenRead(archive.Jar))
                {
                    foreach (ZipArchiveEntry entry in zip.Entries)
                    {
                        string name = entry.FullName.Replace('\\', '/');
                        if (name.EndsWith("/", StringComparison.Ordinal) || string.IsNullOrEmpty(entry.Name)) continue;

                        bool skip = false;
                        foreach (string exclude in archive.Exclude)
                        {
                            if (!string.IsNullOrEmpty(exclude) &&
                                name.StartsWith(exclude, StringComparison.OrdinalIgnoreCase))
                            {
                                skip = true;
                                break;
                            }
                        }
                        if (skip) continue;

                        string target = Path.Combine(nativesDir, name.Replace('/', Path.DirectorySeparatorChar));
                        string parent = Path.GetDirectoryName(target);
                        if (!string.IsNullOrEmpty(parent)) Directory.CreateDirectory(parent);
                        try
                        {
                            entry.ExtractToFile(target, true);
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }
        }

        public static string MavenPath(string name, string extraClassifier)
        {
            if (string.IsNullOrEmpty(name)) return null;
            string[] parts = name.Split(':');
            if (parts.Length < 3) return null;

            string group = parts[0];
            string artifact = parts[1];
            string version = parts[2];
            string classifier = parts.Length > 3 ? parts[3] : extraClassifier;

            string file = artifact + "-" + version + (string.IsNullOrEmpty(classifier) ? "" : "-" + classifier) + ".jar";
            return group.Replace('.', '/') + "/" + artifact + "/" + version + "/" + file;
        }
    }
}
