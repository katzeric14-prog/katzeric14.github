using System;
using System.IO;

namespace Vexor.Core
{
    public static class AppPaths
    {
        public static readonly string LauncherDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "VexorLauncher");
        public static readonly string SettingsFile = Path.Combine(LauncherDir, "settings.json");
        public static readonly string MetaDir = Path.Combine(LauncherDir, "meta");
        public static readonly string RuntimeDir = Path.Combine(LauncherDir, "runtime");
        public static readonly string LogsDir = Path.Combine(LauncherDir, "logs");
        public static readonly string DefaultGameDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".vexor");

        public static void EnsureDirectories()
        {
            Directory.CreateDirectory(LauncherDir);
            Directory.CreateDirectory(MetaDir);
            Directory.CreateDirectory(RuntimeDir);
            Directory.CreateDirectory(LogsDir);
        }
    }
}
