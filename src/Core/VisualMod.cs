using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Vexor.Core
{
    public static class VisualMod
    {
        public const string SupportedGameVersion = "1.21.11";

        private const string ResourceName = "Vexor.Visual.jar";
        private const string ModFileName = "vexor-visual.jar";
        private const string ModFilePrefix = "vexor-visual";
        private const string FabricApiPrefix = "fabric-api";

        public static string CacheDir
        {
            get { return Path.Combine(AppPaths.LauncherDir, "visual"); }
        }

        public static bool SupportsGameVersion(string gameVersion)
        {
            return string.Equals(gameVersion, SupportedGameVersion, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsSupported(InstalledBuild build)
        {
            return build != null
                && string.Equals(build.Loader, LoaderInstaller.Fabric, StringComparison.OrdinalIgnoreCase)
                && SupportsGameVersion(build.GameVersion);
        }

        public static bool HasInstalled(InstalledBuild build)
        {
            return build != null && HasFile(build.ModsDir, ModFilePrefix);
        }

        public static async Task<bool> SyncAsync(IEnumerable<InstalledBuild> builds, bool enabled, Action<string> log,
            CancellationToken ct)
        {
            var targets = new List<InstalledBuild>();
            int removed = 0;
            foreach (InstalledBuild build in builds)
            {
                if (build == null) continue;
                if (enabled && IsSupported(build)) targets.Add(build);
                else if (RemoveFrom(build)) removed++;
            }

            if (!enabled)
            {
                if (removed > 0 && log != null)
                    log("Визуал выключен — мод убран из " + removed + " " + Plural(removed, "сборки", "сборок", "сборок") + ".");
                return false;
            }

            if (targets.Count == 0)
            {
                if (log != null) log("Визуал: нужна Fabric-сборка " + SupportedGameVersion + " — мод не установлен.");
                return false;
            }

            string apiPath;
            string apiName;
            try
            {
                FabricApiFile api = await EnsureFabricApiAsync(log, ct).ConfigureAwait(false);
                apiPath = api.Path;
                apiName = api.Name;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                if (log != null)
                    log("Визуал: Fabric API не загружен (" + ex.Message + ") — запуск без визуала.");
                return false;
            }

            string jar;
            try
            {
                jar = EnsureJar();
            }
            catch (Exception ex)
            {
                if (log != null) log("Визуал: " + ex.Message);
                return false;
            }

            int installed = 0;
            foreach (InstalledBuild build in targets)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    LoaderInstaller.PrepareProfile(build);
                    RemoveFiles(build.ModsDir, ModFilePrefix);
                    File.Copy(jar, Path.Combine(build.ModsDir, ModFileName), true);
                    if (!HasFile(build.ModsDir, FabricApiPrefix))
                        File.Copy(apiPath, Path.Combine(build.ModsDir, apiName), true);
                    installed++;
                }
                catch (Exception ex)
                {
                    if (log != null) log("Визуал: не удалось установить в " + build.DisplayName + " — " + ex.Message);
                }
            }

            if (installed > 0 && log != null)
                log("Визуал включён · " + installed + " " + Plural(installed, "сборка", "сборки", "сборок") +
                    " Fabric " + SupportedGameVersion + ".");
            return installed > 0;
        }

        public static bool RemoveFrom(InstalledBuild build)
        {
            if (build == null) return false;
            try
            {
                return RemoveFiles(build.ModsDir, ModFilePrefix);
            }
            catch
            {
                return false;
            }
        }

        public static string EnsureJar()
        {
            Assembly assembly = typeof(VisualMod).Assembly;
            using (Stream stream = assembly.GetManifestResourceStream(ResourceName))
            {
                if (stream == null)
                    throw new InvalidOperationException("в лаунчере нет встроенного мода визуала.");
                Directory.CreateDirectory(CacheDir);
                string path = Path.Combine(CacheDir, ModFileName);
                string temp = path + ".part";
                if (File.Exists(temp))
                {
                    try { File.Delete(temp); }
                    catch
                    {
                    }
                }
                using (var target = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.CopyTo(target);
                }
                if (File.Exists(path))
                {
                    try { File.Delete(path); }
                    catch
                    {
                    }
                }
                File.Move(temp, path);
                return path;
            }
        }

        private sealed class FabricApiFile
        {
            public string Path;
            public string Name;
        }

        private static async Task<FabricApiFile> EnsureFabricApiAsync(Action<string> log, CancellationToken ct)
        {
            Directory.CreateDirectory(CacheDir);
            foreach (string cached in Directory.GetFiles(CacheDir, FabricApiPrefix + "-*.jar"))
            {
                string cachedName = Path.GetFileName(cached);
                if (cachedName.IndexOf(SupportedGameVersion, StringComparison.OrdinalIgnoreCase) < 0) continue;
                if (new FileInfo(cached).Length <= 65536) continue;
                var hit = new FabricApiFile();
                hit.Path = cached;
                hit.Name = cachedName;
                return hit;
            }

            List<ModVersion> versions = await ModrinthApi.GetVersionsAsync(FabricApiPrefix, SupportedGameVersion,
                LoaderInstaller.Fabric, ct).ConfigureAwait(false);
            ModVersion version = ModrinthApi.ChooseVersion(versions);
            if (version == null || version.PrimaryFile == null || string.IsNullOrEmpty(version.PrimaryFile.Url))
                throw new InvalidOperationException("Fabric API для " + SupportedGameVersion + " не найден.");

            string fileName = ModIndexStore.SafeFileName(version.PrimaryFile.FileName);
            if (string.IsNullOrEmpty(fileName))
                fileName = FabricApiPrefix + "-" + SupportedGameVersion + ".jar";
            string path = Path.Combine(CacheDir, fileName);

            if (log != null) log("Визуал: загрузка Fabric API " + version.VersionNumber + "…");
            await Web.DownloadFileAsync(version.PrimaryFile.Url, path, version.PrimaryFile.Sha1,
                version.PrimaryFile.Size, null, ct).ConfigureAwait(false);

            var api = new FabricApiFile();
            api.Path = path;
            api.Name = fileName;
            return api;
        }

        private static bool HasFile(string dir, string prefix)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return false;
            foreach (string file in Directory.GetFiles(dir, "*.jar"))
            {
                string name = Path.GetFileName(file);
                if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
        }

        private static bool RemoveFiles(string dir, string prefix)
        {
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return false;
            bool removed = false;
            foreach (string file in Directory.GetFiles(dir, "*.jar"))
            {
                string name = Path.GetFileName(file);
                if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                try
                {
                    File.Delete(file);
                    removed = true;
                }
                catch
                {
                }
            }
            return removed;
        }

        private static string Plural(int count, string one, string few, string many)
        {
            int mod100 = count % 100;
            int mod10 = count % 10;
            if (mod100 >= 11 && mod100 <= 14) return many;
            if (mod10 == 1) return one;
            if (mod10 >= 2 && mod10 <= 4) return few;
            return many;
        }
    }
}
