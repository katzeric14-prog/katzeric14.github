using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Vexor.Core
{
    public sealed class SkinException : Exception
    {
        public readonly bool SessionExpired;

        public SkinException(string message)
            : this(message, false, null)
        {
        }

        public SkinException(string message, bool sessionExpired)
            : this(message, sessionExpired, null)
        {
        }

        public SkinException(string message, Exception inner)
            : this(message, false, inner)
        {
        }

        public SkinException(string message, bool sessionExpired, Exception inner)
            : base(message, inner)
        {
            SessionExpired = sessionExpired;
        }
    }

    public sealed class SkinInfo
    {
        public string Name = "";
        public string Uuid = "";
        public string SkinUrl = "";
        public string Variant = "classic";
        public string LocalPath = "";

        public bool Slim
        {
            get { return string.Equals(Variant, "slim", StringComparison.OrdinalIgnoreCase); }
        }

        public bool HasSkin
        {
            get { return !string.IsNullOrEmpty(SkinUrl); }
        }
    }

    public sealed class SkinFile
    {
        public string Path = "";
        public int Width;
        public int Height;
        public long Size;
    }

    public static class SkinService
    {
        private const int MaxFileBytes = 2 * 1024 * 1024;
        private const string NameLookupUrl = "https://api.mojang.com/users/profiles/minecraft/";
        private const string SessionProfileUrl = "https://sessionserver.mojang.com/session/minecraft/profile/";
        private const string OwnProfileUrl = "https://api.minecraftservices.com/minecraft/profile";
        private const string UploadUrl = "https://api.minecraftservices.com/minecraft/profile/skins";

        private static readonly HttpClient Client = CreateClient();

        public static string SkinsDir
        {
            get { return Path.Combine(AppPaths.LauncherDir, "skins"); }
        }

        public static string EnsureSkinsDir()
        {
            string dir = SkinsDir;
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static bool IsValidNickname(string nickname)
        {
            string text = (nickname ?? "").Trim();
            if (text.Length < 1 || text.Length > 16) return false;
            foreach (char c in text)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                          (c >= '0' && c <= '9') || c == '_';
                if (!ok) return false;
            }
            return true;
        }

        public static SkinFile InspectFile(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                throw new SkinException("Файл скина не найден.");

            var info = new FileInfo(path);
            if (info.Length <= 0) throw new SkinException("Файл скина пустой.");
            if (info.Length > MaxFileBytes)
                throw new SkinException("Файл скина слишком большой (максимум 2 МБ).");

            byte[] bytes = File.ReadAllBytes(path);
            byte[] signature = { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            if (bytes.Length < signature.Length)
                throw new SkinException("Файл не является PNG-изображением.");
            for (int i = 0; i < signature.Length; i++)
                if (bytes[i] != signature[i])
                    throw new SkinException("Файл не является PNG-изображением.");

            try
            {
                using (var stream = new MemoryStream(bytes))
                using (Image image = Image.FromStream(stream, true, true))
                {
                    var file = new SkinFile();
                    file.Path = Path.GetFullPath(path);
                    file.Width = image.Width;
                    file.Height = image.Height;
                    file.Size = info.Length;
                    bool valid = (file.Width == 64 && (file.Height == 64 || file.Height == 32));
                    if (!valid)
                        throw new SkinException("Нужен скин 64×64 или 64×32 пикселя, а выбран " +
                                                file.Width + "×" + file.Height + ".");
                    return file;
                }
            }
            catch (SkinException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new SkinException("Не удалось прочитать PNG: " + ex.Message, ex);
            }
        }

        public static Image LoadImageCopy(string path)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                using (var stream = new MemoryStream(bytes))
                using (Image source = Image.FromStream(stream, true, true))
                {
                    return new Bitmap(source);
                }
            }
            catch (Exception ex)
            {
                throw new SkinException("Не удалось открыть изображение скина: " + ex.Message, ex);
            }
        }

        public static async Task<SkinInfo> FetchByNicknameAsync(string nickname, CancellationToken ct)
        {
            string nick = (nickname ?? "").Trim();
            if (!IsValidNickname(nick))
                throw new SkinException("Ник: 1–16 символов, латиница, цифры и _.");

            string lookup = NameLookupUrl + Uri.EscapeDataString(nick);
            using (var response = await Client.GetAsync(lookup, HttpCompletionOption.ResponseContentRead, ct)
                .ConfigureAwait(false))
            {
                if (response.StatusCode == HttpStatusCode.NotFound ||
                    response.StatusCode == HttpStatusCode.NoContent)
                    throw new SkinException("Игрок " + nick + " не найден.");
                if (!response.IsSuccessStatusCode)
                    throw new SkinException("Mojang ответил HTTP " + (int)response.StatusCode + " при поиске игрока.");
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                JsonValue json = Parse(body);
                string id = json["id"].AsString("");
                string name = json["name"].AsString(nick);
                if (string.IsNullOrEmpty(id))
                    throw new SkinException("Игрок " + nick + " не найден.");
                return await FetchSessionProfileAsync(id, name, ct).ConfigureAwait(false);
            }
        }

        private static async Task<SkinInfo> FetchSessionProfileAsync(string uuid, string fallbackName,
            CancellationToken ct)
        {
            string url = SessionProfileUrl + Uri.EscapeDataString(uuid);
            using (var response = await Client.GetAsync(url, HttpCompletionOption.ResponseContentRead, ct)
                .ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode)
                    throw new SkinException("Не удалось получить профиль игрока (HTTP " +
                                            (int)response.StatusCode + ").");
                string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                JsonValue json = Parse(body);

                var info = new SkinInfo();
                info.Uuid = json["id"].AsString(uuid);
                info.Name = json["name"].AsString(fallbackName);
                info.Variant = "classic";

                JsonValue properties = json["properties"];
                if (properties.IsArray)
                {
                    foreach (JsonValue property in properties.Items)
                    {
                        if (!string.Equals(property["name"].AsString(""), "textures",
                                StringComparison.OrdinalIgnoreCase)) continue;
                        string value = property["value"].AsString("");
                        if (value.Length == 0) continue;
                        string decoded = DecodeBase64(value);
                        if (decoded.Length == 0) continue;
                        JsonValue textures = Parse(decoded)["textures"];
                        JsonValue skin = textures["SKIN"];
                        string skinUrl = skin["url"].AsString("");
                        if (skinUrl.Length == 0) continue;
                        info.SkinUrl = UpgradeUrl(skinUrl);
                        string model = skin["metadata"]["model"].AsString("");
                        info.Variant = string.Equals(model, "slim", StringComparison.OrdinalIgnoreCase)
                            ? "slim"
                            : "classic";
                        break;
                    }
                }

                if (!info.HasSkin)
                    throw new SkinException("У игрока " + info.Name + " не загружен скин (используется стандартный).");
                return info;
            }
        }

        public static async Task<string> DownloadToLibraryAsync(SkinInfo info, CancellationToken ct)
        {
            if (info == null || !info.HasSkin)
                throw new SkinException("Ссылка на скин отсутствует.");
            if (!IsAllowedUrl(info.SkinUrl))
                throw new SkinException("Скин должен скачиваться по HTTPS.");

            byte[] bytes = await Client.GetByteArrayAsync(info.SkinUrl).ConfigureAwait(false);
            if (bytes == null || bytes.Length == 0)
                throw new SkinException("Сервер отдал пустой файл скина.");
            if (bytes.Length > MaxFileBytes)
                throw new SkinException("Скин с сервера слишком большой.");

            string dir = EnsureSkinsDir();
            string fileName = SafeName(info.Name) + "-" + NowUnix().ToString(CultureInfo.InvariantCulture) + ".png";
            string path = Path.Combine(dir, fileName);
            File.WriteAllBytes(path, bytes);

            try
            {
                SkinFile file = InspectFile(path);
                if (file.Height == 32) info.Variant = "classic";
            }
            catch
            {
                TryDelete(path);
                throw;
            }

            info.LocalPath = path;
            return path;
        }

        public static async Task<SkinInfo> GetOwnAsync(MsaSession session, CancellationToken ct)
        {
            if (session == null || !session.IsLicensed)
                throw new SkinException("Нужен вход через Microsoft.", true);

            using (var request = new HttpRequestMessage(HttpMethod.Get, OwnProfileUrl))
            {
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + session.AccessToken);
                using (var response = await Client.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct)
                    .ConfigureAwait(false))
                {
                    string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                        throw new SkinException("Сессия Minecraft истекла. Войдите через Microsoft снова.", true);
                    if (!response.IsSuccessStatusCode)
                        throw new SkinException("Не удалось получить профиль Minecraft (HTTP " +
                                                (int)response.StatusCode + ").");
                    JsonValue json = Parse(body);
                    var info = new SkinInfo();
                    info.Uuid = json["id"].AsString(session.Uuid);
                    info.Name = json["name"].AsString(session.Name);
                    info.Variant = "classic";

                    JsonValue skins = json["skins"];
                    if (skins.IsArray && skins.Count > 0)
                    {
                        JsonValue active = skins[0];
                        foreach (JsonValue item in skins.Items)
                        {
                            if (string.Equals(item["state"].AsString(""), "ACTIVE",
                                    StringComparison.OrdinalIgnoreCase))
                            {
                                active = item;
                                break;
                            }
                        }
                        string url = active["url"].AsString("");
                        if (url.Length > 0)
                        {
                            info.SkinUrl = UpgradeUrl(url);
                            info.Variant = string.Equals(active["variant"].AsString(""),
                                "slim", StringComparison.OrdinalIgnoreCase)
                                ? "slim"
                                : "classic";
                        }
                    }
                    return info;
                }
            }
        }

        public static async Task<Image> LoadPreviewAsync(SkinInfo info, CancellationToken ct)
        {
            if (info == null || !info.HasSkin) return null;
            if (!IsAllowedUrl(info.SkinUrl))
                throw new SkinException("Скин должен загружаться по HTTPS.");

            byte[] bytes = await Client.GetByteArrayAsync(info.SkinUrl).ConfigureAwait(false);
            if (bytes == null || bytes.Length == 0) return null;
            try
            {
                using (var stream = new MemoryStream(bytes))
                using (Image source = Image.FromStream(stream, true, true))
                {
                    return new Bitmap(source);
                }
            }
            catch (Exception ex)
            {
                throw new SkinException("Не удалось прочитать скин с сервера: " + ex.Message, ex);
            }
        }

        public static async Task UploadAsync(MsaSession session, string pngPath, bool slim, CancellationToken ct)
        {
            if (session == null || !session.IsLicensed)
                throw new SkinException("Для загрузки скина войдите через Microsoft.", true);

            SkinFile file = InspectFile(pngPath);
            if (slim && file.Height != 64)
                throw new SkinException("Slim-скин должен быть 64×64 пикселя.");

            byte[] bytes = File.ReadAllBytes(file.Path);
            using (var form = new MultipartFormDataContent())
            {
                form.Add(new StringContent(slim ? "slim" : "classic"), "variant");
                var image = new ByteArrayContent(bytes);
                image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
                form.Add(image, "file", "skin.png");

                using (var request = new HttpRequestMessage(HttpMethod.Post, UploadUrl))
                {
                    request.Content = form;
                    request.Headers.TryAddWithoutValidation("Accept", "application/json");
                    request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + session.AccessToken);

                    using (var response = await Client.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct)
                        .ConfigureAwait(false))
                    {
                        string body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (response.StatusCode == HttpStatusCode.Unauthorized)
                            throw new SkinException("Сессия Minecraft истекла. Войдите через Microsoft снова.", true);
                        if (response.IsSuccessStatusCode) return;

                        string message = "";
                        try
                        {
                            message = Parse(body)["errorMessage"].AsString("");
                        }
                        catch
                        {
                        }
                        if (message.Length == 0)
                            message = "Mojang отклонил скин (HTTP " + (int)response.StatusCode + ").";
                        throw new SkinException(message);
                    }
                }
            }
        }

        public static void OpenSkinsFolder()
        {
            string dir = EnsureSkinsDir();
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(dir)
                {
                    UseShellExecute = true
                });
            }
            catch
            {
            }
        }

        private static JsonValue Parse(string text)
        {
            try
            {
                return JsonParser.Parse(text ?? "");
            }
            catch (Exception ex)
            {
                throw new SkinException("Сервер вернул некорректный ответ.", ex);
            }
        }

        private static string DecodeBase64(string value)
        {
            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch
            {
                return "";
            }
        }

        private static string UpgradeUrl(string url)
        {
            string text = (url ?? "").Trim();
            if (text.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                text = "https://" + text.Substring(7);
            return text;
        }

        private static bool IsAllowedUrl(string url)
        {
            Uri uri;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri)) return false;
            if (uri.Scheme == Uri.UriSchemeHttps) return true;
            return uri.Scheme == Uri.UriSchemeHttp && uri.IsLoopback;
        }

        private static string SafeName(string name)
        {
            var sb = new StringBuilder();
            foreach (char c in (name ?? ""))
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') ||
                          (c >= '0' && c <= '9') || c == '_' || c == '-';
                if (ok) sb.Append(c);
            }
            string text = sb.ToString();
            return text.Length > 0 ? text : "skin";
        }

        private static long NowUnix()
        {
            return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
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

        private static HttpClient CreateClient()
        {
            Web.ConfigureTls();
            var handler = new HttpClientHandler();
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            var client = new HttpClient(handler);
            client.Timeout = TimeSpan.FromSeconds(45);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("VexorLauncher/" + AppInfo.Version);
            return client;
        }
    }
}
