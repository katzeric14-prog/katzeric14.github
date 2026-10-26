using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Vexor.Core
{
    public static class ShadersApi
    {
        public const string IndexFileName = "vexor-shaders.txt";
        public const string FolderName = "shaderpacks";

        public static string ShadersDir(string gameDir)
        {
            if (string.IsNullOrEmpty(gameDir)) throw new ArgumentNullException("gameDir");
            return Path.Combine(gameDir, FolderName);
        }

        public static Task<ModSearchResult> SearchAsync(string query, string gameVersion,
            int offset, int limit, CancellationToken ct)
        {
            return CatalogZip.SearchAsync("shader", query, gameVersion, offset, limit, ct);
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
            return CatalogZip.ResolveAsync(project, gameVersion, "шейдера", ct);
        }

        public static HashSet<string> ReadInstalled(string gameDir)
        {
            return CatalogZip.ReadInstalled(gameDir, IndexFileName);
        }

        public static Task<ModInstallResult> InstallAsync(ModProject project, ModVersion version,
            string gameDir, Action<string> log, CancellationToken ct)
        {
            return CatalogZip.InstallAsync(project, version, gameDir, FolderName, IndexFileName,
                "Шейдер", log, ct);
        }
    }
}
