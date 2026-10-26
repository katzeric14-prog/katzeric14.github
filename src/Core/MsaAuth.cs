using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vexor.Core
{
    public sealed class MsaException : Exception
    {
        public readonly bool SessionExpired;

        public MsaException(string message)
            : this(message, false, null)
        {
        }

        public MsaException(string message, bool sessionExpired)
            : this(message, sessionExpired, null)
        {
        }

        public MsaException(string message, Exception inner)
            : this(message, false, inner)
        {
        }

        public MsaException(string message, bool sessionExpired, Exception inner)
            : base(message, inner)
        {
            SessionExpired = sessionExpired;
        }
    }

    public sealed class MsaDeviceCode
    {
        public string DeviceCode = "";
        public string UserCode = "";
        public string VerificationUri = "https://www.microsoft.com/link";
        public int IntervalSeconds = 5;
        public int ExpiresInSeconds = 900;
    }

    public sealed class MsaSession
    {
        public string Name = "";
        public string Uuid = "";
        public string Xuid = "";
        public string AccessToken = "";
        public string RefreshToken = "";
        public long ExpiresAtUtc;

        public bool NeedsRefresh
        {
            get
            {
                long now = MsaAuth.NowUnix();
                return ExpiresAtUtc <= 0 || ExpiresAtUtc - now < 300;
            }
        }

        public bool IsLicensed
        {
            get { return !string.IsNullOrEmpty(AccessToken) && !string.IsNullOrEmpty(Uuid); }
        }
    }

    public static class MsaAuth
    {
        // Публичный клиент Prism Launcher, одобренный Mojang для Minecraft Services.
        public const string DefaultClientId = "c36a9fb6-4f2a-41ff-90bd-ae7cc92031eb";

        private const string Scope = "XboxLive.SignIn XboxLive.offline_access";
        private const string DeviceCodeUrl = "https://login.microsoftonline.com/consumers/oauth2/v2.0/devicecode";
        private const string TokenUrl = "https://login.microsoftonline.com/consumers/oauth2/v2.0/token";
        private const string XboxUserUrl = "https://user.auth.xboxlive.com/user/authenticate";
        private const string XstsUrl = "https://xsts.auth.xboxlive.com/xsts/authorize";
        private const string MinecraftLoginUrl = "https://api.minecraftservices.com/authentication/login_with_xbox";
        private const string MinecraftProfileUrl = "https://api.minecraftservices.com/minecraft/profile";

        private static string _clientId = DefaultClientId;
        private static readonly HttpClient Client = CreateClient();

        public static string ClientId
        {
            get { return _clientId; }
        }

        public static void Configure(string clientId)
        {
            _clientId = string.IsNullOrWhiteSpace(clientId) ? DefaultClientId : clientId.Trim();
        }

        private sealed class XboxTicket
        {
            public string Token = "";
            public string Uhs = "";
            public string Xuid = "";
        }

        public static long NowUnix()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }

        public static async Task<MsaDeviceCode> StartDeviceCodeAsync(CancellationToken ct)
        {
            var form = new Dictionary<string, string>();
            form["client_id"] = ClientId;
            form["scope"] = Scope;

            JsonValue json = await PostFormAsync(DeviceCodeUrl, form, ct).ConfigureAwait(false);
            var code = new MsaDeviceCode();
            code.DeviceCode = json["device_code"].AsString("");
            code.UserCode = json["user_code"].AsString("");
            code.VerificationUri = json["verification_uri"].AsString("https://www.microsoft.com/link");
            code.IntervalSeconds = Math.Max(2, json["interval"].AsInt(5));
            code.ExpiresInSeconds = Math.Max(60, json["expires_in"].AsInt(900));

            if (string.IsNullOrEmpty(code.DeviceCode) || string.IsNullOrEmpty(code.UserCode))
                throw new MsaException("Microsoft не выдал код входа. Попробуйте ещё раз.");
            return code;
        }

        public static async Task<MsaSession> LoginAsync(MsaDeviceCode device, Action<string> progress,
            CancellationToken ct)
        {
            if (device == null) throw new ArgumentNullException("device");

            int interval = Math.Max(2, device.IntervalSeconds);
            long deadline = NowUnix() + Math.Max(60, device.ExpiresInSeconds);

            while (NowUnix() < deadline)
            {
                await Task.Delay(TimeSpan.FromSeconds(interval), ct).ConfigureAwait(false);

                var form = new Dictionary<string, string>();
                form["client_id"] = ClientId;
                form["grant_type"] = "urn:ietf:params:oauth:grant-type:device_code";
                form["device_code"] = device.DeviceCode;

                JsonValue token = await PostFormAsync(TokenUrl, form, ct, true).ConfigureAwait(false);
                if (token.Has("access_token"))
                    return await CompleteAsync(token, null, progress, ct).ConfigureAwait(false);

                string error = token["error"].AsString("");
                if (error == "authorization_pending")
                    continue;
                if (error == "slow_down")
                {
                    interval += 5;
                    continue;
                }
                if (error == "expired_token")
                    throw new MsaException("Код входа истёк. Нажмите «Повторить».");
                if (error == "authorization_declined" || error == "access_denied")
                    throw new MsaException("Вход отклонён в окне Microsoft.");
                throw ErrorFor(400, token, "");
            }

            throw new MsaException("Код входа истёк. Нажмите «Повторить».");
        }

        public static async Task<MsaSession> RefreshAsync(MsaSession current, Action<string> progress,
            CancellationToken ct)
        {
            if (current == null || string.IsNullOrEmpty(current.RefreshToken))
                throw new MsaException("Нет сохранённой сессии Microsoft. Войдите снова.", true);

            var form = new Dictionary<string, string>();
            form["client_id"] = ClientId;
            form["grant_type"] = "refresh_token";
            form["refresh_token"] = current.RefreshToken;
            form["scope"] = Scope;

            JsonValue token = await PostFormAsync(TokenUrl, form, ct).ConfigureAwait(false);
            return await CompleteAsync(token, current.RefreshToken, progress, ct).ConfigureAwait(false);
        }

        private static async Task<MsaSession> CompleteAsync(JsonValue token, string fallbackRefresh,
            Action<string> progress, CancellationToken ct)
        {
            string msToken = token["access_token"].AsString("");
            string refresh = token["refresh_token"].AsString(fallbackRefresh ?? "");
            if (string.IsNullOrEmpty(msToken))
                throw new MsaException("Microsoft не вернул токен доступа.");

            if (progress != null) progress("Вход в Xbox Live…");
            XboxTicket xbl = await XboxUserAuthAsync(msToken, ct).ConfigureAwait(false);

            if (progress != null) progress("Проверка профиля Xbox…");
            XboxTicket xsts = await XstsAsync(xbl.Token, ct).ConfigureAwait(false);

            if (progress != null) progress("Вход в Minecraft…");
            JsonValue mc = await MinecraftLoginAsync(xsts, ct).ConfigureAwait(false);
            string mcToken = mc["access_token"].AsString("");
            if (string.IsNullOrEmpty(mcToken))
                throw new MsaException("Minecraft не выдал токен входа.");

            if (progress != null) progress("Загрузка профиля Minecraft…");
            JsonValue profile = await GetJsonAsync(MinecraftProfileUrl, mcToken, ct).ConfigureAwait(false);
            string id = profile["id"].AsString("");
            string name = profile["name"].AsString("");
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name))
                throw new MsaException("У аккаунта Microsoft нет профиля Minecraft. Проверьте, что игра приобретена.");

            var session = new MsaSession();
            session.Name = name;
            session.Uuid = id;
            session.Xuid = string.IsNullOrEmpty(xsts.Xuid) ? xbl.Xuid : xsts.Xuid;
            session.AccessToken = mcToken;
            session.RefreshToken = refresh;
            int expires = mc["expires_in"].AsInt(86400);
            if (expires < 60) expires = 86400;
            session.ExpiresAtUtc = NowUnix() + expires;
            return session;
        }

        private static async Task<XboxTicket> XboxUserAuthAsync(string msToken, CancellationToken ct)
        {
            var sb = new StringBuilder();
            sb.Append("{\"Properties\":{\"AuthMethod\":\"RPS\",\"SiteName\":\"user.auth.xboxlive.com\",\"RpsTicket\":");
            sb.Append(JsonText.Quote("d=" + msToken));
            sb.Append("},\"RelyingParty\":\"http://auth.xboxlive.com\",\"TokenType\":\"JWT\"}");

            JsonValue json = await PostJsonAsync(XboxUserUrl, sb.ToString(), null, ct).ConfigureAwait(false);
            var ticket = new XboxTicket();
            ticket.Token = json["Token"].AsString("");
            if (string.IsNullOrEmpty(ticket.Token))
                throw new MsaException("Xbox Live не выдал токен пользователя.");
            JsonValue xui = json["DisplayClaims"]["xui"][0];
            ticket.Uhs = xui["uhs"].AsString("");
            ticket.Xuid = xui["xid"].AsString("");
            return ticket;
        }

        private static async Task<XboxTicket> XstsAsync(string xblToken, CancellationToken ct)
        {
            var sb = new StringBuilder();
            sb.Append("{\"Properties\":{\"SandboxId\":\"RETAIL\",\"UserTokens\":[");
            sb.Append(JsonText.Quote(xblToken));
            sb.Append("]},\"RelyingParty\":\"rp://api.minecraftservices.com/\",\"TokenType\":\"JWT\"}");

            JsonValue json = await PostJsonAsync(XstsUrl, sb.ToString(), null, ct).ConfigureAwait(false);
            var ticket = new XboxTicket();
            ticket.Token = json["Token"].AsString("");
            if (string.IsNullOrEmpty(ticket.Token))
                throw new MsaException("XSTS не выдал токен для Minecraft.");
            JsonValue xui = json["DisplayClaims"]["xui"][0];
            ticket.Uhs = xui["uhs"].AsString("");
            ticket.Xuid = xui["xid"].AsString("");
            if (string.IsNullOrEmpty(ticket.Uhs))
                throw new MsaException("XSTS не вернул идентификатор пользователя.");
            return ticket;
        }

        private static async Task<JsonValue> MinecraftLoginAsync(XboxTicket xsts, CancellationToken ct)
        {
            string identity = "XBL3.0 x=" + xsts.Uhs + ";" + xsts.Token;
            string body = "{\"identityToken\":" + JsonText.Quote(identity) + "}";
            return await PostJsonAsync(MinecraftLoginUrl, body, null, ct).ConfigureAwait(false);
        }

        private static async Task<JsonValue> GetJsonAsync(string url, string bearer, CancellationToken ct)
        {
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + bearer);
                using (var response = await Client.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct)
                    .ConfigureAwait(false))
                {
                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    JsonValue json = TryParse(body);
                    if (!response.IsSuccessStatusCode)
                        throw ErrorFor((int)response.StatusCode, json, body);
                    if (json == null) throw new MsaException("Сервер Minecraft вернул пустой ответ.");
                    return json;
                }
            }
        }

        private static async Task<JsonValue> PostFormAsync(string url, Dictionary<string, string> form,
            CancellationToken ct, bool allowError = false)
        {
            using (var content = new FormUrlEncodedContent(form))
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = content;
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                using (var response = await Client.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct)
                    .ConfigureAwait(false))
                {
                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    JsonValue json = TryParse(body);
                    if (!response.IsSuccessStatusCode && !allowError)
                        throw ErrorFor((int)response.StatusCode, json, body);
                    if (json == null) throw new MsaException("Microsoft вернул пустой ответ.");
                    return json;
                }
            }
        }

        private static async Task<JsonValue> PostJsonAsync(string url, string body, string bearer,
            CancellationToken ct)
        {
            using (var content = new StringContent(body, Encoding.UTF8, "application/json"))
            using (var request = new HttpRequestMessage(HttpMethod.Post, url))
            {
                request.Content = content;
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                if (!string.IsNullOrEmpty(bearer))
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + bearer);
                using (var response = await Client.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct)
                    .ConfigureAwait(false))
                {
                    string text = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    JsonValue json = TryParse(text);
                    if (!response.IsSuccessStatusCode)
                        throw ErrorFor((int)response.StatusCode, json, text);
                    if (json == null) throw new MsaException("Сервер вернул некорректный ответ.");
                    return json;
                }
            }
        }

        private static JsonValue TryParse(string text)
        {
            if (string.IsNullOrEmpty(text)) return null;
            try
            {
                return JsonParser.Parse(text);
            }
            catch
            {
                return null;
            }
        }

        private static MsaException ErrorFor(int status, JsonValue json, string body)
        {
            string xerr = json != null ? json["XErr"].AsString("") : "";
            string code = json != null ? json["error"].AsString("") : "";
            string description = json != null ? json["error_description"].AsString("") : "";
            string message = json != null ? json["errorMessage"].AsString("") : "";

            if (xerr.Length > 0)
            {
                switch (xerr)
                {
                    case "2148916227":
                        return new MsaException("Аккаунт Xbox Live заблокирован.");
                    case "2148916233":
                        return new MsaException(
                            "У аккаунта Microsoft нет профиля Xbox Live. Создайте его на xbox.com и повторите вход.");
                    case "2148916235":
                        return new MsaException("Xbox Live недоступен в регионе аккаунта.");
                    case "2148916236":
                    case "2148916237":
                        return new MsaException(
                            "Xbox Live требует подтверждения возраста. Проверьте данные на account.microsoft.com.");
                    case "2148916238":
                        return new MsaException(
                            "Детский аккаунт должен быть добавлен в семейную группу Microsoft.");
                    default:
                        return new MsaException("Xbox Live отклонил вход (XErr " + xerr + ").");
                }
            }

            if (code == "invalid_grant" || code == "interaction_required")
                return new MsaException("Сессия Microsoft истекла. Войдите снова.", true);
            if (code == "authorization_declined" || code == "access_denied")
                return new MsaException("Вход отклонён в окне Microsoft.");
            if (code == "invalid_client" || code == "unauthorized_client")
                return new MsaException(
                    "Microsoft отклонил Client ID. Укажите свой Microsoft Client ID в settings.json.");
            if (code == "temporarily_unavailable")
                return new MsaException("Сервис Microsoft временно недоступен. Попробуйте позже.");
            if (status == 404 && code.Length == 0 && message.Length == 0)
                return new MsaException(
                    "У аккаунта нет профиля Minecraft: игра не приобретена или профиль не создан.");
            if (status == 401 && code.Length == 0 && message.Length == 0)
                return new MsaException("Сервис отклонил токен входа. Повторите вход в Microsoft.");

            var sb = new StringBuilder();
            sb.Append("Ошибка входа (HTTP ").Append(status).Append(')');
            if (code.Length > 0) sb.Append(" · ").Append(code);
            if (message.Length > 0) sb.Append(": ").Append(Trim(message, 220));
            else if (description.Length > 0) sb.Append(": ").Append(Trim(description, 220));
            else if (body != null && body.Length > 0 && json == null)
                sb.Append(": ").Append(Trim(body, 160));
            return new MsaException(sb.ToString());
        }

        private static string Trim(string value, int max)
        {
            if (string.IsNullOrEmpty(value)) return "";
            value = value.Replace("\r", " ").Replace("\n", " ").Trim();
            return value.Length <= max ? value : value.Substring(0, max - 1) + "…";
        }

        private static HttpClient CreateClient()
        {
            Web.ConfigureTls();
            var handler = new HttpClientHandler();
            handler.AutomaticDecompression = System.Net.DecompressionMethods.GZip |
                                             System.Net.DecompressionMethods.Deflate;
            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(45);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("VexorLauncher/0.1.0");
            return client;
        }
    }

    public static class MsaStore
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("VexorLauncher.Msa.v1");

        public static string FilePath
        {
            get { return Path.Combine(AppPaths.LauncherDir, "account.bin"); }
        }

        public static MsaSession Load()
        {
            try
            {
                if (!File.Exists(FilePath)) return null;
                byte[] encrypted = File.ReadAllBytes(FilePath);
                byte[] plain = ProtectedData.Unprotect(encrypted, Entropy, DataProtectionScope.CurrentUser);
                JsonValue json = JsonParser.Parse(Encoding.UTF8.GetString(plain));

                var session = new MsaSession();
                session.Name = json["name"].AsString("");
                session.Uuid = json["uuid"].AsString("");
                session.Xuid = json["xuid"].AsString("");
                session.AccessToken = json["accessToken"].AsString("");
                session.RefreshToken = json["refreshToken"].AsString("");
                session.ExpiresAtUtc = json["expiresAt"].AsLong(0);
                if (string.IsNullOrEmpty(session.RefreshToken) || string.IsNullOrEmpty(session.Name))
                    return null;
                return session;
            }
            catch
            {
                return null;
            }
        }

        public static bool Save(MsaSession session)
        {
            if (session == null) return false;
            try
            {
                AppPaths.EnsureDirectories();
                var sb = new StringBuilder();
                sb.AppendLine("{");
                sb.AppendLine("  \"name\": " + JsonText.Quote(session.Name) + ",");
                sb.AppendLine("  \"uuid\": " + JsonText.Quote(session.Uuid) + ",");
                sb.AppendLine("  \"xuid\": " + JsonText.Quote(session.Xuid) + ",");
                sb.AppendLine("  \"accessToken\": " + JsonText.Quote(session.AccessToken) + ",");
                sb.AppendLine("  \"refreshToken\": " + JsonText.Quote(session.RefreshToken) + ",");
                sb.AppendLine("  \"expiresAt\": " + session.ExpiresAtUtc.ToString());
                sb.AppendLine("}");

                byte[] plain = Encoding.UTF8.GetBytes(sb.ToString());
                byte[] encrypted = ProtectedData.Protect(plain, Entropy, DataProtectionScope.CurrentUser);
                string temp = FilePath + ".tmp";
                File.WriteAllBytes(temp, encrypted);
                if (File.Exists(FilePath)) File.Delete(FilePath);
                File.Move(temp, FilePath);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void Clear()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch
            {
            }
        }
    }
}
