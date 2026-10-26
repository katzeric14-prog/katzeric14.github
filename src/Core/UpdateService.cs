using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Vexor.Core
{
    public sealed class UpdateException : Exception
    {
        public UpdateException(string message)
            : base(message)
        {
        }

        public UpdateException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    public sealed class UpdateFileInfo
    {
        public string Path = "";
        public string Url = "";
        public string Sha256 = "";
        public long Size;
    }

    public sealed class UpdateManifest
    {
        public string Version = "";
        public string Notes = "";
        public string Executable = "";
        public List<UpdateFileInfo> Files = new List<UpdateFileInfo>();
    }

    public static class UpdateService
    {
        public const string CurrentVersion = AppInfo.Version;

        private const int MoveFileDelayUntilReboot = 0x4;

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool MoveFileEx(string existingFileName, string newFileName, int flags);

        public static bool IsNewer(string version)
        {
            return CompareVersions(CurrentVersion, version) < 0;
        }

        public static int CompareVersions(string current, string server)
        {
            int[] left = ParseVersion(current);
            int[] right = ParseVersion(server);
            int count = Math.Max(left.Length, right.Length);
            for (int i = 0; i < count; i++)
            {
                int a = i < left.Length ? left[i] : 0;
                int b = i < right.Length ? right[i] : 0;
                if (a != b) return a < b ? -1 : 1;
            }
            return 0;
        }

        private static int[] ParseVersion(string version)
        {
            string text = (version ?? "").Trim();
            if (text.StartsWith("v", StringComparison.OrdinalIgnoreCase)) text = text.Substring(1);
            int suffix = text.IndexOfAny(new[] { '-', '+', ' ' });
            if (suffix >= 0) text = text.Substring(0, suffix);
            string[] parts = text.Split('.');
            var numbers = new List<int>();
            foreach (string part in parts)
            {
                int value;
                if (!int.TryParse(part, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)) value = 0;
                if (value < 0) value = 0;
                numbers.Add(value);
            }
            if (numbers.Count == 0) numbers.Add(0);
            return numbers.ToArray();
        }

        public static async Task<UpdateManifest> CheckAsync(string manifestUrl, CancellationToken ct)
        {
            string url = (manifestUrl ?? "").Trim();
            if (url.Length == 0)
                throw new UpdateException("Не задан адрес манифеста обновлений.");
            if (!IsAllowedUrl(url))
                throw new UpdateException("Адрес обновлений должен использовать HTTPS.");

            string separator = url.IndexOf('?') >= 0 ? "&" : "?";
            url = url + separator + "t=" + NowUnix().ToString(CultureInfo.InvariantCulture);

            var headers = new Dictionary<string, string>();
            headers["Cache-Control"] = "no-cache";
            headers["Pragma"] = "no-cache";
            headers["Accept"] = "application/json";
            string json = await Web.GetStringAsync(url, headers, ct).ConfigureAwait(false);
            return ParseManifest(json);
        }

        public static UpdateManifest ParseManifest(string json)
        {
            JsonValue root;
            try
            {
                root = JsonParser.Parse(json ?? "");
            }
            catch (Exception ex)
            {
                throw new UpdateException("Манифест обновления повреждён: " + ex.Message, ex);
            }
            if (!root.IsObject) throw new UpdateException("Манифест обновления повреждён.");

            var manifest = new UpdateManifest();
            manifest.Version = (root["version"].AsString("") ?? "").Trim();
            if (manifest.Version.Length == 0)
                throw new UpdateException("В манифесте не указана версия.");
            manifest.Notes = root["notes"].AsString(root["changelog"].AsString("")) ?? "";
            manifest.Executable = NormalizeRelativePath(root["executable"].AsString(""), false);

            JsonValue files = root["files"];
            if (files.IsArray && files.Count > 0)
            {
                foreach (JsonValue item in files.Items)
                {
                    var file = new UpdateFileInfo();
                    file.Path = NormalizeRelativePath(item["path"].AsString(""), true);
                    file.Url = (item["url"].AsString("") ?? "").Trim();
                    file.Sha256 = (item["sha256"].AsString("") ?? "").Trim();
                    file.Size = item["size"].AsLong(0);
                    manifest.Files.Add(file);
                }
            }
            else if (root.Has("url"))
            {
                var file = new UpdateFileInfo();
                file.Path = manifest.Executable.Length > 0 ? manifest.Executable : "VexorLauncher.exe";
                file.Url = (root["url"].AsString("") ?? "").Trim();
                file.Sha256 = (root["sha256"].AsString("") ?? "").Trim();
                file.Size = root["size"].AsLong(0);
                manifest.Files.Add(file);
            }
            else
            {
                throw new UpdateException("В манифесте нет списка файлов.");
            }

            if (manifest.Executable.Length == 0)
            {
                if (manifest.Files.Count == 1) manifest.Executable = manifest.Files[0].Path;
                else throw new UpdateException("В манифесте не указан executable.");
            }

            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool executableFound = false;
            foreach (UpdateFileInfo file in manifest.Files)
            {
                if (!seen.Add(file.Path))
                    throw new UpdateException("В манифесте повторяется файл: " + file.Path);
                if (!IsAllowedUrl(file.Url))
                    throw new UpdateException("Файл " + file.Path + " должен скачиваться по HTTPS.");
                if (file.Sha256.Length != 64 || !IsHex(file.Sha256))
                    throw new UpdateException("Для файла " + file.Path + " не указан корректный SHA-256.");
                if (file.Size < 0) file.Size = 0;
                if (string.Equals(file.Path, manifest.Executable, StringComparison.OrdinalIgnoreCase))
                    executableFound = true;
            }
            if (!executableFound)
                throw new UpdateException("Исполняемый файл из манифеста отсутствует в списке файлов.");

            return manifest;
        }

        public static async Task<string> DownloadAsync(UpdateManifest manifest, IProgress<BatchProgress> progress,
            Action<string> log, CancellationToken ct)
        {
            if (manifest == null) throw new ArgumentNullException("manifest");
            if (manifest.Files.Count == 0) throw new UpdateException("В манифесте нет файлов.");

            string staging = StagingDirectory(manifest);
            TryDeleteDirectory(staging);
            Directory.CreateDirectory(staging);

            long totalBytes = 0;
            foreach (UpdateFileInfo file in manifest.Files)
                if (file.Size > 0) totalBytes += file.Size;

            long doneBytes = 0;
            try
            {
                for (int i = 0; i < manifest.Files.Count; i++)
                {
                    ct.ThrowIfCancellationRequested();
                    UpdateFileInfo file = manifest.Files[i];
                    string dest = SafeCombine(staging, file.Path);
                    string dir = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

                    if (log != null) log("Скачивание " + file.Path + "…");
                    long baseBytes = doneBytes;
                    string fileName = Path.GetFileName(file.Path);
                    Action<long, long> onProgress = delegate(long done, long fileTotal)
                    {
                        long absolute = baseBytes + done;
                        Report(progress, "Скачивание " + fileName + "…", i + 1, manifest.Files.Count,
                            absolute, totalBytes);
                    };
                    Report(progress, "Скачивание " + fileName + "…", i, manifest.Files.Count, doneBytes, totalBytes);
                    await Web.DownloadFileAsync(file.Url, dest, null, file.Size, onProgress, ct).ConfigureAwait(false);

                    long length = new FileInfo(dest).Length;
                    if (file.Size > 0 && length != file.Size)
                        throw new UpdateException("Размер файла " + file.Path + " не совпадает с манифестом.");
                    string actual = Sha256File(dest);
                    if (!string.Equals(actual, file.Sha256, StringComparison.OrdinalIgnoreCase))
                        throw new UpdateException("Файл " + file.Path + " повреждён: SHA-256 не совпадает.");

                    doneBytes += file.Size > 0 ? file.Size : length;
                    Report(progress, "Проверено " + fileName, i + 1, manifest.Files.Count, doneBytes, totalBytes);
                }

                Report(progress, "Загрузка завершена", manifest.Files.Count, manifest.Files.Count,
                    totalBytes, totalBytes);
                if (log != null) log("Обновление " + manifest.Version + " загружено.");
                return staging;
            }
            catch
            {
                TryDeleteDirectory(staging);
                throw;
            }
        }

        public static string StagingDirectory(UpdateManifest manifest)
        {
            return Path.Combine(AppPaths.RuntimeDir, "update", SafeDirectoryName(manifest.Version));
        }

        public static void CleanupStaging()
        {
            try
            {
                string root = Path.Combine(AppPaths.RuntimeDir, "update");
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
            catch
            {
            }
        }

        public static void ApplyAndRestart(UpdateManifest manifest, string staging)
        {
            if (manifest == null) throw new ArgumentNullException("manifest");
            if (string.IsNullOrEmpty(staging) || !Directory.Exists(staging))
                throw new UpdateException("Папка с загруженным обновлением не найдена.");

            string currentExe = Process.GetCurrentProcess().MainModule.FileName;
            string installDir = Path.GetDirectoryName(currentExe);
            if (string.IsNullOrEmpty(installDir)) throw new UpdateException("Не удалось определить папку лаунчера.");
            string targetExe = SafeCombine(installDir, manifest.Executable);
            if (!File.Exists(targetExe))
                throw new UpdateException("Обновляемый файл не найден: " + manifest.Executable);

            string updaterDir = Path.Combine(Path.GetTempPath(), "VexorLauncherUpdate",
                SafeDirectoryName(manifest.Version) + "-" + Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture));
            Directory.CreateDirectory(updaterDir);
            string updater = Path.Combine(updaterDir, "VexorUpdater.exe");
            File.Copy(currentExe, updater, true);

            string arguments = "--apply-update " +
                               QuoteArgument(Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture)) + " " +
                               QuoteArgument(staging) + " " +
                               QuoteArgument(installDir) + " " +
                               QuoteArgument(manifest.Executable);

            var info = new ProcessStartInfo(updater, arguments);
            info.UseShellExecute = false;
            info.CreateNoWindow = true;
            info.WorkingDirectory = updaterDir;
            Process.Start(info);
            AppendLog("Запущен помощник обновления: " + updater + " · " + manifest.Version);
        }

        public static bool IsApplyCommand(string[] args)
        {
            return args != null && args.Length > 0 &&
                   string.Equals(args[0], "--apply-update", StringComparison.OrdinalIgnoreCase);
        }

        public static int ApplyFromCommandLine(string[] args)
        {
            AppendLog("=== применение обновления ===");
            if (args == null || args.Length < 5)
            {
                AppendLog("Недостаточно аргументов.");
                return 2;
            }

            int parentPid;
            if (!int.TryParse(args[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out parentPid))
            {
                AppendLog("Неверный PID родителя.");
                return 2;
            }

            string staging;
            string installDir;
            string executable;
            try
            {
                staging = Path.GetFullPath(args[2]);
                installDir = Path.GetFullPath(args[3]);
                executable = NormalizeRelativePath(args[4], true);
                string updateRoot = Path.GetFullPath(Path.Combine(AppPaths.RuntimeDir, "update"));
                if (!IsInside(staging, updateRoot))
                    throw new UpdateException("Папка обновления находится вне каталога лаунчера.");
            }
            catch (Exception ex)
            {
                AppendLog("Неверные пути: " + ex.Message);
                return 2;
            }

            string targetExe;
            string backup = null;
            try
            {
                targetExe = SafeCombine(installDir, executable);
                if (!File.Exists(targetExe))
                    throw new UpdateException("Обновляемый файл не найден: " + targetExe);

                try
                {
                    Process parent = Process.GetProcessById(parentPid);
                    if (!parent.WaitForExit(60000))
                        AppendLog("Родительский процесс не завершился за 60 секунд.");
                }
                catch (ArgumentException)
                {
                }
                catch (InvalidOperationException)
                {
                }
                Thread.Sleep(600);

                backup = targetExe + ".update-backup";
                try
                {
                    File.Copy(targetExe, backup, true);
                }
                catch (Exception ex)
                {
                    AppendLog("Не удалось создать резервную копию: " + ex.Message);
                    backup = null;
                }

                CopyDirectory(staging, installDir);
                if (!File.Exists(targetExe))
                    throw new UpdateException("После обновления исполняемый файл отсутствует.");

                if (backup != null) TryDeleteFile(backup);
                TryDeleteDirectory(staging);

                var info = new ProcessStartInfo(targetExe);
                info.WorkingDirectory = installDir;
                info.UseShellExecute = true;
                Process.Start(info);
                AppendLog("Запущена новая версия: " + targetExe);
                return 0;
            }
            catch (Exception ex)
            {
                AppendLog("ОШИБКА обновления: " + ex);
                if (backup != null && File.Exists(backup))
                {
                    try
                    {
                        string restore = Path.Combine(installDir, executable);
                        File.Copy(backup, restore, true);
                        TryDeleteFile(backup);
                        AppendLog("Резервная копия восстановлена.");
                        var info = new ProcessStartInfo(restore);
                        info.WorkingDirectory = installDir;
                        info.UseShellExecute = true;
                        Process.Start(info);
                    }
                    catch (Exception restoreError)
                    {
                        AppendLog("Не удалось восстановить резервную копию: " + restoreError.Message);
                    }
                }
                try
                {
                    MessageBox.Show("Не удалось применить обновление." + Environment.NewLine + ex.Message,
                        "Vexor Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch
                {
                }
                return 1;
            }
            finally
            {
                ScheduleSelfDelete();
            }
        }

        private static void CopyDirectory(string source, string target)
        {
            string sourceRoot = Path.GetFullPath(source).TrimEnd(Path.DirectorySeparatorChar);
            string[] files = Directory.GetFiles(sourceRoot, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                string relative = file.Substring(sourceRoot.Length).TrimStart(Path.DirectorySeparatorChar);
                string dest = SafeCombine(target, relative);
                string dir = Path.GetDirectoryName(dest);
                if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                CopyWithRetry(file, dest);
            }
        }

        private static void CopyWithRetry(string source, string dest)
        {
            Exception last = null;
            for (int attempt = 1; attempt <= 12; attempt++)
            {
                try
                {
                    File.Copy(source, dest, true);
                    return;
                }
                catch (IOException ex)
                {
                    last = ex;
                }
                catch (UnauthorizedAccessException ex)
                {
                    last = ex;
                }
                Thread.Sleep(400);
            }
            throw new UpdateException("Не удалось заменить " + Path.GetFileName(dest) + ": " +
                                      (last != null ? last.Message : "файл занят"), last);
        }

        private static void Report(IProgress<BatchProgress> progress, string status, int filesDone, int filesTotal,
            long doneBytes, long totalBytes)
        {
            if (progress == null) return;
            var report = new BatchProgress();
            report.Status = status;
            report.FilesDone = filesDone;
            report.FilesTotal = filesTotal;
            report.BytesDone = doneBytes;
            report.BytesTotal = totalBytes;
            progress.Report(report);
        }

        public static string Sha256File(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] hash = sha.ComputeHash(stream);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                return sb.ToString();
            }
        }

        private static bool IsAllowedUrl(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) return false;
            if (uri.Scheme == Uri.UriSchemeHttps) return true;
            return uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback;
        }

        private static bool IsHex(string value)
        {
            foreach (char c in value)
            {
                bool hex = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                if (!hex) return false;
            }
            return true;
        }

        private static string NormalizeRelativePath(string path, bool required)
        {
            string text = (path ?? "").Replace('\\', '/').Trim();
            while (text.StartsWith("./", StringComparison.Ordinal)) text = text.Substring(2);
            text = text.Trim('/');
            if (text.Length == 0)
            {
                if (required) throw new UpdateException("В манифесте указан пустой путь к файлу.");
                return "";
            }
            if (text.IndexOf(':') >= 0 || Path.IsPathRooted(text))
                throw new UpdateException("Недопустимый путь в манифесте: " + path);
            string[] parts = text.Split('/');
            foreach (string part in parts)
            {
                if (part.Length == 0 || part == "." || part == "..")
                    throw new UpdateException("Недопустимый путь в манифесте: " + path);
            }
            return string.Join(Path.DirectorySeparatorChar.ToString(), parts);
        }

        private static string SafeCombine(string root, string relative)
        {
            string normalized = NormalizeRelativePath(relative, true);
            string rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            string full = Path.GetFullPath(Path.Combine(rootFull, normalized));
            if (!IsInside(full, rootFull))
                throw new UpdateException("Путь выходит за пределы каталога: " + relative);
            return full;
        }

        private static bool IsInside(string path, string root)
        {
            string full = Path.GetFullPath(path);
            string rootFull = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar);
            if (string.Equals(full, rootFull, StringComparison.OrdinalIgnoreCase)) return true;
            return full.StartsWith(rootFull + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        }

        private static string SafeDirectoryName(string value)
        {
            var sb = new StringBuilder();
            foreach (char c in (value ?? ""))
            {
                bool safe = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                            (c >= '0' && c <= '9') || c == '.' || c == '-' || c == '_';
                sb.Append(safe ? c : '_');
            }
            string text = sb.ToString().Trim('.');
            return text.Length > 0 ? text : "update";
        }

        private static long NowUnix()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        private static string QuoteArgument(string value)
        {
            if (value == null) return "\"\"";
            if (value.Length == 0) return "\"\"";
            bool needQuotes = false;
            foreach (char c in value)
            {
                if (c == ' ' || c == '\t' || c == '"' || c == '\n' || c == '\v')
                {
                    needQuotes = true;
                    break;
                }
            }
            if (!needQuotes) return value;

            var sb = new StringBuilder();
            sb.Append('"');
            int backslashes = 0;
            foreach (char c in value)
            {
                if (c == '\\')
                {
                    backslashes++;
                    continue;
                }
                if (c == '"')
                {
                    sb.Append('\\', backslashes * 2 + 1);
                    sb.Append('"');
                    backslashes = 0;
                    continue;
                }
                if (backslashes > 0)
                {
                    sb.Append('\\', backslashes);
                    backslashes = 0;
                }
                sb.Append(c);
            }
            if (backslashes > 0) sb.Append('\\', backslashes * 2);
            sb.Append('"');
            return sb.ToString();
        }

        private static void ScheduleSelfDelete()
        {
            try
            {
                string self = Process.GetCurrentProcess().MainModule.FileName;
                MoveFileEx(self, null, MoveFileDelayUntilReboot);
            }
            catch
            {
            }
        }

        private static void TryDeleteFile(string path)
        {
            try
            {
                if (File.Exists(path)) File.Delete(path);
            }
            catch
            {
            }
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path)) Directory.Delete(path, true);
            }
            catch
            {
            }
        }

        private static void AppendLog(string text)
        {
            try
            {
                AppPaths.EnsureDirectories();
                string path = Path.Combine(AppPaths.LogsDir, "update.log");
                File.AppendAllText(path,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + text + Environment.NewLine,
                    new UTF8Encoding(false));
            }
            catch
            {
            }
        }
    }
}
