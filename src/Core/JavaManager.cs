using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Vexor.Core
{
    public sealed class JavaInfo
    {
        public string Path;
        public int Major;
        public string VersionText;
        public bool Is64Bit;

        public string Display
        {
            get { return "Java " + Major + " · " + VersionText; }
        }
    }

    public static class JavaManager
    {
        private static readonly object Sync = new object();
        private static Task<List<JavaInfo>> _scanTask;
        private static List<JavaInfo> _lastResult;

        public static Task<List<JavaInfo>> DetectAsync(LauncherSettings settings, bool force)
        {
            lock (Sync)
            {
                if (force || _scanTask == null)
                {
                    LauncherSettings snapshot = settings;
                    _scanTask = Task.Run(() => Scan(snapshot));
                }
                return _scanTask;
            }
        }

        public static List<JavaInfo> Cached
        {
            get
            {
                lock (Sync) return _lastResult ?? new List<JavaInfo>();
            }
        }

        private static List<JavaInfo> Scan(LauncherSettings settings)
        {
            var results = new List<JavaInfo>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string candidate in Candidates(settings))
            {
                if (string.IsNullOrEmpty(candidate) || !seen.Add(candidate)) continue;
                JavaInfo info = Probe(candidate);
                if (info != null) results.Add(info);
            }
            results.Sort(delegate(JavaInfo a, JavaInfo b)
            {
                if (a.Major != b.Major) return a.Major.CompareTo(b.Major);
                return string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase);
            });
            lock (Sync) _lastResult = results;
            return results;
        }

        private static IEnumerable<string> Candidates(LauncherSettings settings)
        {
            var found = new List<string>();
            Action<string> addJava = delegate(string path)
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path)) found.Add(path);
            };
            Action<string> addHome = delegate(string dir)
            {
                if (!string.IsNullOrEmpty(dir)) addJava(Path.Combine(dir, "bin", "java.exe"));
            };

            if (settings != null && !string.IsNullOrWhiteSpace(settings.JavaPath))
            {
                string custom = settings.JavaPath.Trim();
                if (Directory.Exists(custom)) addHome(custom);
                else addJava(custom);
            }

            addHome(Environment.GetEnvironmentVariable("JAVA_HOME"));

            foreach (string entry in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
            {
                string dir = entry.Trim();
                if (dir.Length > 0) addJava(Path.Combine(dir, "java.exe"));
            }

            // Our downloaded runtimes.
            foreach (string dir in SafeDirectories(AppPaths.RuntimeDir)) addHome(dir);

            // Mojang launcher runtimes: runtime/<component>/windows-x64/<component>/bin/java.exe
            string officialRuntime = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "runtime");
            foreach (string component in SafeDirectories(officialRuntime))
                foreach (string arch in SafeDirectories(component))
                    foreach (string inner in SafeDirectories(arch))
                        addHome(inner);

            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            var roots = new List<string>
            {
                Path.Combine(programFiles, "Java"),
                Path.Combine(programFiles, "Eclipse Adoptium"),
                Path.Combine(programFiles, "Eclipse Foundation"),
                Path.Combine(programFiles, "Microsoft"),
                Path.Combine(programFiles, "Zulu"),
                Path.Combine(programFiles, "BellSoft"),
                Path.Combine(programFiles, "Amazon Corretto"),
                Path.Combine(programFiles, "Semeru"),
                Path.Combine(programFiles, "IBM"),
                Path.Combine(programFiles, "GraalVM"),
                Path.Combine(programFilesX86, "Java"),
                Path.Combine(localAppData, "Programs", "Eclipse Adoptium"),
                Path.Combine(localAppData, "Programs", "Microsoft"),
                Path.Combine(localAppData, "Programs", "Java"),
                Path.Combine(localAppData, "Programs", "Zulu"),
                Path.Combine(localAppData, "Programs", "BellSoft")
            };

            foreach (string root in roots)
            {
                if (!Directory.Exists(root)) continue;
                addHome(root);
                foreach (string sub in SafeDirectories(root)) addHome(sub);
            }
            return found;
        }

        private static IEnumerable<string> SafeDirectories(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path)) return Directory.GetDirectories(path);
            }
            catch
            {
            }
            return new string[0];
        }

        private static JavaInfo Probe(string javaExe)
        {
            try
            {
                var info = new ProcessStartInfo();
                info.FileName = javaExe;
                info.Arguments = "-version";
                info.UseShellExecute = false;
                info.CreateNoWindow = true;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;

                using (var process = Process.Start(info))
                {
                    if (process == null) return null;
                    Task<string> errorTask = process.StandardError.ReadToEndAsync();
                    Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
                    if (!process.WaitForExit(6000))
                    {
                        try { process.Kill(); }
                        catch
                        {
                        }
                        process.WaitForExit(1000);
                    }
                    string text = "";
                    try { text = errorTask.Result + "\n" + outputTask.Result; }
                    catch
                    {
                    }

                    Match match = Regex.Match(text, "version\\s+\"([^\"]+)\"");
                    if (!match.Success) return null;

                    string versionText = match.Groups[1].Value;
                    int major = ParseMajor(versionText);
                    if (major <= 0) return null;

                    var result = new JavaInfo();
                    result.Path = javaExe;
                    result.Major = major;
                    result.VersionText = versionText;
                    result.Is64Bit = text.IndexOf("64-Bit", StringComparison.OrdinalIgnoreCase) >= 0;
                    return result;
                }
            }
            catch
            {
                return null;
            }
        }

        private static int ParseMajor(string version)
        {
            if (version.StartsWith("1.", StringComparison.Ordinal))
            {
                string[] parts = version.Split('.');
                int legacy;
                if (parts.Length > 1 && int.TryParse(parts[1], out legacy)) return legacy;
            }
            string digits = new string(version.TakeWhile(char.IsDigit).ToArray());
            int major;
            if (int.TryParse(digits, out major)) return major;
            return 0;
        }

        public static JavaInfo FindExact(int major)
        {
            JavaInfo best = null;
            foreach (JavaInfo info in Cached)
            {
                if (info.Major != major) continue;
                if (best == null || (info.Is64Bit && !best.Is64Bit)) best = info;
            }
            return best;
        }

        public static async Task<string> EnsureJavaAsync(int major, LauncherSettings settings,
            IProgress<BatchProgress> progress, Action<string> log, CancellationToken ct)
        {
            List<JavaInfo> detected = await DetectAsync(settings, false).ConfigureAwait(false);
            JavaInfo match = null;
            foreach (JavaInfo info in detected)
            {
                if (info.Major != major) continue;
                if (match == null || (info.Is64Bit && !match.Is64Bit)) match = info;
            }
            if (match != null)
            {
                if (log != null) log("Java " + major + " найдена: " + match.VersionText + " · " + match.Path);
                return match.Path;
            }

            string target = Path.Combine(AppPaths.RuntimeDir, "jre-" + major);
            string existing = FindJavaExe(target);
            if (existing != null)
            {
                JavaInfo check = Probe(existing);
                if (check != null && check.Major == major)
                {
                    if (log != null) log("Использую ранее скачанную Java " + major + ".");
                    return existing;
                }
            }

            AppPaths.EnsureDirectories();
            if (log != null) log("Java " + major + " не найдена. Скачиваю Adoptium JRE " + major + "…");

            string zip = Path.Combine(AppPaths.RuntimeDir, "jre-" + major + ".zip");
            var batch = new BatchProgress();
            batch.Status = "Скачивание Java " + major;
            batch.FilesTotal = 1;

            string[] types = { "jre", "jdk" };
            Exception lastError = null;
            bool downloaded = false;
            foreach (string type in types)
            {
                string url = "https://api.adoptium.net/v3/binary/latest/" + major + "/ga/windows/x64/" + type +
                             "/hotspot/normal/eclipse?project=jdk";
                try
                {
                    await Web.DownloadFileAsync(url, zip, null, 0, delegate(long done, long total)
                    {
                        batch.BytesDone = done;
                        batch.BytesTotal = total;
                        if (progress != null) progress.Report(batch);
                    }, ct).ConfigureAwait(false);
                    downloaded = true;
                    break;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                }
            }
            if (!downloaded)
                throw new IOException("Не удалось скачать Java " + major + ": " + (lastError != null ? lastError.Message : "неизвестная ошибка"), lastError);

            if (log != null) log("Распаковка Java " + major + "…");
            string tempDir = Path.Combine(AppPaths.RuntimeDir, "_jre_tmp_" + major);
            try
            {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                ZipFile.ExtractToDirectory(zip, tempDir);
                string javaExe = FindJavaExeRecursive(tempDir);
                if (javaExe == null) throw new IOException("в архиве не найден java.exe");
                string home = Path.GetDirectoryName(Path.GetDirectoryName(javaExe));
                if (Directory.Exists(target)) Directory.Delete(target, true);
                Directory.Move(home, target);
            }
            catch (Exception ex)
            {
                throw new IOException("Не удалось распаковать Java " + major + ": " + ex.Message, ex);
            }
            finally
            {
                TryDelete(zip);
                try
                {
                    if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
                }
                catch
                {
                }
            }

            string result = FindJavaExe(target);
            if (result == null) throw new IOException("Java " + major + " не установилась.");
            JavaInfo verify = Probe(result);
            if (verify == null || verify.Major != major)
                throw new IOException("Java " + major + " установилась, но не запускается.");

            await DetectAsync(settings, true).ConfigureAwait(false);
            if (log != null) log("Java " + major + " готова.");
            return result;
        }

        private static string FindJavaExe(string home)
        {
            if (string.IsNullOrEmpty(home)) return null;
            string direct = Path.Combine(home, "bin", "java.exe");
            if (File.Exists(direct)) return direct;
            try
            {
                if (!Directory.Exists(home)) return null;
                foreach (string sub in Directory.GetDirectories(home))
                {
                    string nested = Path.Combine(sub, "bin", "java.exe");
                    if (File.Exists(nested)) return nested;
                }
            }
            catch
            {
            }
            return null;
        }

        private static string FindJavaExeRecursive(string root)
        {
            try
            {
                return Directory.GetFiles(root, "java.exe", SearchOption.AllDirectories).FirstOrDefault();
            }
            catch
            {
                return null;
            }
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
            }
        }
    }
}
