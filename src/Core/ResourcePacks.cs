using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vexor.Core
{
    public static class ResourcePacksApi
    {
        public const string IndexFileName = "vexor-resourcepacks.txt";
        public const string FolderName = "resourcepacks";

        public static string ResourcePacksDir(string gameDir)
        {
            if (string.IsNullOrEmpty(gameDir)) throw new ArgumentNullException("gameDir");
            return Path.Combine(gameDir, FolderName);
        }

        public static Task<ModSearchResult> SearchAsync(string query, string gameVersion,
            int offset, int limit, CancellationToken ct)
        {
            return CatalogZip.SearchAsync("resourcepack", query, gameVersion, offset, limit, ct);
        }

        public static Task<List<ModVersion>> GetVersionsAsync(string projectId, string gameVersion,
            CancellationToken ct)
        {
            return CatalogZip.GetVersionsAsync(projectId, gameVersion, ct);
        }

        public static ModVersion ChooseVersion(List<ModVersion> versions)
        {
            return CatalogZip.ChooseVersion(versions);
        }

        public static Task<ModVersion> ResolveAsync(ModProject project, string gameVersion,
            CancellationToken ct)
        {
            return CatalogZip.ResolveAsync(project, gameVersion, "ресурспака", ct);
        }

        public static HashSet<string> ReadInstalled(string gameDir)
        {
            return CatalogZip.ReadInstalled(gameDir, IndexFileName);
        }

        public static Task<ModInstallResult> InstallAsync(ModProject project, ModVersion version,
            string gameDir, Action<string> log, CancellationToken ct)
        {
            return CatalogZip.InstallAsync(project, version, gameDir, FolderName, IndexFileName,
                "Ресурспак", log, ct);
        }
    }
}
