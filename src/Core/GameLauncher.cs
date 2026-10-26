using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vexor.Core
{
    public static class GameLauncher
    {
        public static async Task<int> LaunchAsync(GameVersion version, VersionEntry entry, InstallResult install,
            LauncherSettings settings, MsaSession account, string javaPath, string launchGameDir,
            FeatureSet features, Action<string> log, Action onGameStarted, CancellationToken ct,
            string connectHost, int connectPort)
        {
            if (version == null) throw new ArgumentNullException("version");
            if (string.IsNullOrEmpty(version.MainClass))
                throw new InvalidOperationException("В описании версии не указан главный класс.");

            bool licensed = account != null && account.IsLicensed;
            string nickname;
            string uuid;
            string accessToken;
            string userType;
            string xuid;
            string clientId;
            string authSession;
            if (licensed)
            {
                nickname = account.Name;
                uuid = account.Uuid;
                accessToken = account.AccessToken;
                userType = "msa";
                xuid = account.Xuid ?? "";
                clientId = MsaAuth.ClientId;
                authSession = accessToken;
            }
            else
            {
                nickname = (settings.Nickname ?? "").Trim();
                uuid = OfflineAuth.UuidFor(nickname);
                accessToken = "0";
                userType = "legacy";
                xuid = "";
                clientId = "";
                authSession = "-";
            }
            if (string.IsNullOrEmpty(launchGameDir)) launchGameDir = settings.GameDir;
            string gameDir = settings.GameDir;
            Directory.CreateDirectory(launchGameDir);

            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            map["auth_player_name"] = nickname;
            map["version_name"] = version.Id;
            map["game_directory"] = launchGameDir;
            map["assets_root"] = install.AssetsDir;
            map["assets_index_name"] = install.AssetIndexId;
            map["auth_uuid"] = uuid;
            map["auth_access_token"] = accessToken;
            map["auth_session"] = authSession;
            map["user_type"] = userType;
            map["version_type"] = string.IsNullOrEmpty(entry.Type) ? "release" : entry.Type;
            map["user_properties"] = "{}";
            map["game_assets"] = install.GameAssetsDir;
            map["clientid"] = clientId;
            map["auth_xuid"] = xuid;
            map["library_directory"] = Path.Combine(gameDir, "libraries");
            map["classpath"] = string.Join(";", install.Classpath.ToArray());
            map["classpath_separator"] = ";";
            map["natives_directory"] = install.NativesDir;
            map["launcher_name"] = "VexorLauncher";
            map["launcher_version"] = AppInfo.Version;
            map["resolution_width"] = settings.ResolutionWidth.ToString(CultureInfo.InvariantCulture);
            map["resolution_height"] = settings.ResolutionHeight.ToString(CultureInfo.InvariantCulture);
            map["quickPlayPath"] = "";
            map["quickPlaySingleplayer"] = "";
            map["quickPlayMultiplayer"] = "";
            map["quickPlayRealms"] = "";

            var jvmArgs = new List<string>();
            jvmArgs.Add("-Xms" + settings.RamMb.ToString(CultureInfo.InvariantCulture) + "M");
            jvmArgs.Add("-Xmx" + settings.RamMb.ToString(CultureInfo.InvariantCulture) + "M");

            if (settings.OptimizeJvm)
            {
                jvmArgs.Add("-XX:+UnlockExperimentalVMOptions");
                jvmArgs.Add("-XX:+UseG1GC");
                jvmArgs.Add("-XX:+ParallelRefProcEnabled");
                jvmArgs.Add("-XX:MaxGCPauseMillis=50");
                jvmArgs.Add("-XX:G1NewSizePercent=20");
                jvmArgs.Add("-XX:G1ReservePercent=20");
            }

            foreach (string extra in SplitArguments(settings.ExtraJvmArgs))
                jvmArgs.Add(extra);

            bool modernArguments = version.JvmArgs.Count > 0;
            if (modernArguments)
            {
                foreach (ArgEntry arg in version.JvmArgs)
                {
                    if (!Rules.Allowed(arg.Rules, features)) continue;
                    foreach (string value in arg.Values)
                        jvmArgs.Add(Substitute(value, map));
                }
            }
            else
            {
                jvmArgs.Add("-Djava.library.path=" + install.NativesDir);
                jvmArgs.Add("-Dminecraft.launcher.brand=VexorLauncher");
                jvmArgs.Add("-Dminecraft.launcher.version=" + AppInfo.Version);
                jvmArgs.Add("-cp");
                jvmArgs.Add(string.Join(";", install.Classpath.ToArray()));
            }

            var gameArgs = new List<string>();
            if (version.GameArgs.Count > 0)
            {
                foreach (ArgEntry arg in version.GameArgs)
                {
                    if (!Rules.Allowed(arg.Rules, features)) continue;
                    foreach (string value in arg.Values)
                        gameArgs.Add(Substitute(value, map));
                }
            }
            else if (!string.IsNullOrEmpty(version.LegacyGameArgs))
            {
                foreach (string token in version.LegacyGameArgs.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    gameArgs.Add(Substitute(token, map));
            }

            if (!string.IsNullOrEmpty(connectHost))
            {
                gameArgs.Add("--server");
                gameArgs.Add(connectHost);
                gameArgs.Add("--port");
                gameArgs.Add((connectPort > 0 ? connectPort : 25565).ToString(CultureInfo.InvariantCulture));
            }

            var command = new List<string>();
            command.AddRange(jvmArgs);
            command.Add(version.MainClass);
            command.AddRange(gameArgs);

            string arguments = string.Join(" ", command.Select(QuoteArgument).ToArray());

            if (log != null)
                log("Запуск " + version.Id + " · Java " + version.JavaMajor + " · " + settings.RamMb + " МБ ОЗУ" +
                    (licensed ? " · Microsoft: " + nickname : " · офлайн-профиль") +
                    (string.IsNullOrEmpty(connectHost) ? "" : " · сервер " + connectHost + ":" + connectPort));

            var startInfo = new ProcessStartInfo();
            startInfo.FileName = javaPath;
            startInfo.Arguments = arguments;
            startInfo.WorkingDirectory = launchGameDir;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.RedirectStandardError = true;
            startInfo.StandardOutputEncoding = Encoding.UTF8;
            startInfo.StandardErrorEncoding = Encoding.UTF8;

            var tail = new Queue<string>();
            object gate = new object();

            using (var process = new Process())
            {
                process.StartInfo = startInfo;
                process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data == null) return;
                    lock (gate)
                    {
                        tail.Enqueue(e.Data);
                        if (tail.Count > 80) tail.Dequeue();
                    }
                    if (log != null) log(e.Data);
                };
                process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs e)
                {
                    if (e.Data == null) return;
                    lock (gate)
                    {
                        tail.Enqueue(e.Data);
                        if (tail.Count > 80) tail.Dequeue();
                    }
                    if (log != null) log(e.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                bool exitedEarly = await Task.Run(() => process.WaitForExit(6000), ct).ConfigureAwait(false);
                if (exitedEarly && process.ExitCode != 0)
                {
                    process.WaitForExit();
                    string detail = "";
                    lock (gate)
                    {
                        if (tail.Count > 0) detail = Environment.NewLine + string.Join(Environment.NewLine, tail.ToArray());
                    }
                    throw new InvalidOperationException(
                        "Игра завершилась с ошибкой (код " + process.ExitCode + ")." + detail);
                }

                if (!exitedEarly)
                {
                    if (onGameStarted != null) onGameStarted();
                    if (log != null) log("Minecraft запущен. Приятной игры!");
                }
                else
                {
                    if (log != null) log("Игра завершилась (код " + process.ExitCode + ").");
                }

                await Task.Run(() => process.WaitForExit()).ConfigureAwait(false);
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        private static string Substitute(string template, Dictionary<string, string> values)
        {
            if (string.IsNullOrEmpty(template)) return template ?? "";
            int index = 0;
            var sb = new StringBuilder();
            while (index < template.Length)
            {
                int start = template.IndexOf("${", index, StringComparison.Ordinal);
                if (start < 0)
                {
                    sb.Append(template, index, template.Length - index);
                    break;
                }
                sb.Append(template, index, start - index);
                int end = template.IndexOf('}', start + 2);
                if (end < 0)
                {
                    sb.Append(template, start, template.Length - start);
                    break;
                }
                string key = template.Substring(start + 2, end - start - 2);
                string value;
                if (values.TryGetValue(key, out value) && value != null) sb.Append(value);
                index = end + 1;
            }
            return sb.ToString();
        }

        public static List<string> SplitArguments(string text)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text)) return result;
            var current = new StringBuilder();
            bool quoted = false;
            foreach (char c in text)
            {
                if (c == '"')
                {
                    quoted = !quoted;
                    continue;
                }
                if (char.IsWhiteSpace(c) && !quoted)
                {
                    if (current.Length > 0)
                    {
                        result.Add(current.ToString());
                        current.Length = 0;
                    }
                    continue;
                }
                current.Append(c);
            }
            if (current.Length > 0) result.Add(current.ToString());
            return result;
        }

        private static string QuoteArgument(string argument)
        {
            if (argument == null) return "\"\"";
            if (argument.Length == 0) return "\"\"";

            bool needQuotes = false;
            foreach (char c in argument)
            {
                if (c == ' ' || c == '\t' || c == '"' || c == '\n' || c == '\v')
                {
                    needQuotes = true;
                    break;
                }
            }
            if (!needQuotes) return argument;

            var sb = new StringBuilder();
            sb.Append('"');
            int backslashes = 0;
            foreach (char c in argument)
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
    }
}
