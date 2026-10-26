using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vexor.Core;

internal static class LegacyTest
{
    private sealed class ProgressLogger : IProgress<BatchProgress>
    {
        private int _last = -1;

        public void Report(BatchProgress value)
        {
            if (value == null) return;
            int percent = (int)Math.Round(value.Fraction * 100);
            if (percent == _last && value.FilesDone != value.FilesTotal) return;
            _last = percent;
            Console.WriteLine("  [" + percent.ToString(CultureInfo.InvariantCulture) + "%] " +
                value.Status + " · " + value.FilesDone + "/" + value.FilesTotal);
        }
    }

    private static int Main(string[] args)
    {
        Console.OutputEncoding = Encoding.UTF8;
        try
        {
            return Run(args).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            Console.WriteLine("FAIL: " + ex);
            return 1;
        }
    }

    private static async Task<int> Run(string[] args)
    {
        string versionId = args.Length > 0 ? args[0] : "1.8.9";
        string mode = args.Length > 1 ? args[1] : "args";
        string javaOverride = args.Length > 2 ? args[2] : null;

        var settings = new LauncherSettings();
        settings.Nickname = "Player";
        settings.GameDir = AppPaths.DefaultGameDir;
        settings.RamMb = 2048;
        settings.OptimizeJvm = true;
        settings.ResolutionWidth = 854;
        settings.ResolutionHeight = 480;
        AppPaths.EnsureDirectories();

        var features = new FeatureSet();
        features.HasCustomResolution = false;
        var progress = new ProgressLogger();
        Action<string> log = delegate(string line) { Console.WriteLine("  . " + line); };
        CancellationToken ct = CancellationToken.None;

        Console.WriteLine("== Manifest ==");
        VersionManifest manifest = await MojangApi.GetManifestAsync(false, ct).ConfigureAwait(false);
        VersionEntry entry = null;
        foreach (VersionEntry candidate in manifest.Versions)
        {
            if (string.Equals(candidate.Id, versionId, StringComparison.Ordinal)) { entry = candidate; break; }
        }
        if (entry == null) throw new InvalidOperationException("Version not found: " + versionId);
        Console.WriteLine("  " + entry.Id + " · " + entry.Type + " · " + entry.ReleaseDateText);

        Console.WriteLine("== Version JSON ==");
        GameVersion version = await MojangApi.EnsureVersionJsonAsync(entry, settings.GameDir, log, ct).ConfigureAwait(false);
        Console.WriteLine("  mainClass=" + version.MainClass + " java=" + version.JavaMajor +
            " jvmArgs=" + version.JvmArgs.Count + " gameArgs=" + version.GameArgs.Count +
            " legacy=" + (version.LegacyGameArgs ?? ""));
        if (version.JavaMajor != 8) throw new InvalidOperationException("Expected Java 8, got " + version.JavaMajor);

        int desiredMajor = version.JavaMajor;
        if (mode.StartsWith("java:", StringComparison.OrdinalIgnoreCase))
            desiredMajor = int.Parse(mode.Substring(5), CultureInfo.InvariantCulture);
        Console.WriteLine("== Java " + desiredMajor + " ==");
        string javaPath;
        if (javaOverride != null)
        {
            javaPath = javaOverride;
            Console.WriteLine("  override: " + javaPath);
        }
        else
        {
            javaPath = await JavaManager.EnsureJavaAsync(desiredMajor, settings, progress, log, ct).ConfigureAwait(false);
        }
        Console.WriteLine("  => " + javaPath);

        if (mode.StartsWith("java:", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("OK: java provisioned");
            return 0;
        }

        Console.WriteLine("== Install ==");
        InstallResult install = await GameInstaller.InstallAsync(version, settings.GameDir, features, progress, log, ct).ConfigureAwait(false);
        Console.WriteLine("  client=" + install.ClientJar);
        Console.WriteLine("  natives=" + install.NativesDir);
        Console.WriteLine("  assets=" + install.AssetsDir + " index=" + install.AssetIndexId);
        Console.WriteLine("  classpath entries=" + install.Classpath.Count);
        if (!File.Exists(install.ClientJar)) throw new InvalidOperationException("Client jar missing");
        if (Directory.GetFiles(install.NativesDir).Length == 0) throw new InvalidOperationException("Natives dir empty");

        if (string.Equals(mode, "install", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("OK: install only");
            return 0;
        }

        Console.WriteLine("== Launch (" + mode + ") ==");
        int exit = await GameLauncher.LaunchAsync(version, entry, install, settings, javaPath, features, log,
            delegate { Console.WriteLine("  [callback] game started"); }, ct).ConfigureAwait(false);
        Console.WriteLine("  exit=" + exit);

        if (string.Equals(mode, "args", StringComparison.OrdinalIgnoreCase))
        {
            string argsFile = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(javaPath)), "args.txt");
            Console.WriteLine("  args file: " + argsFile);
            if (!File.Exists(argsFile)) throw new InvalidOperationException("Stub java did not write args.txt");
            string line = File.ReadAllText(argsFile);
            File.WriteAllText(Path.Combine(Path.GetTempPath(), "vexor-args-" + versionId + ".txt"), line, Encoding.UTF8);
            Console.WriteLine("  " + line);
            Validate(line, version, settings, install);
            Console.WriteLine("OK: command line valid");
        }
        return 0;
    }

    private static void Validate(string line, GameVersion version, LauncherSettings settings, InstallResult install)
    {
        var required = new List<string>();
        required.Add("-Djava.library.path=");
        required.Add("-cp ");
        required.Add(version.MainClass);
        required.Add("--username Player");
        required.Add("--version " + version.Id);
        required.Add("--gameDir ");
        required.Add("--assetsDir ");
        required.Add("--assetIndex " + install.AssetIndexId);
        required.Add("--uuid ");
        required.Add("--accessToken 0");
        required.Add("--userProperties {}");
        required.Add("--userType legacy");
        foreach (string needle in required)
        {
            if (line.IndexOf(needle, StringComparison.Ordinal) < 0)
                throw new InvalidOperationException("Missing token: " + needle);
        }
        if (line.IndexOf(install.NativesDir, StringComparison.Ordinal) < 0)
            throw new InvalidOperationException("Natives dir not in command line");
    }
}
