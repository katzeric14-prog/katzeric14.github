using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vexor.Core;

namespace Vexor.Core
{
    // Minimal InstalledBuild stub so the CurseForge core can be tested without UI/loaders.
    public sealed class InstalledBuild
    {
        public string Id;
        public string GameVersion;
        public string Loader;
        public string LoaderVersion;
        public string ProfileDir;
        public string ModsDir;

        public string LoaderLabel
        {
            get { return Loader; }
        }
    }
}

namespace Vexor.Core.Test
{
    public static class Program
    {
        private const int Port = 18471;
        private const string ApiKey = "test-key";

        private static readonly byte[] MainBytes = Fill(262144, 0x41);
        private static readonly byte[] LibBytes = Fill(1024, 0x42);
        private static int _downloadUrlCalls;
        private static bool _authFailed;

        public static int Main()
        {
            string cachePath = Path.Combine(AppPaths.MetaDir, "curseforge-categories.json");
            string backupPath = cachePath + ".testbackup";
            bool cacheExisted = File.Exists(cachePath);
            bool cacheBackedUp = false;
            if (cacheExisted)
            {
                try
                {
                    File.Copy(cachePath, backupPath, true);
                    cacheBackedUp = true;
                }
                catch
                {
                }
            }
            if (cacheBackedUp)
            {
                try { File.Delete(cachePath); }
                catch
                {
                }
            }

            var listener = new TcpListener(IPAddress.Loopback, Port);
            listener.Start();
            var server = new Thread(delegate() { Serve(listener); });
            server.IsBackground = true;
            server.Start();

            try
            {
                CurseForgeApi.BaseUrl = "http://127.0.0.1:" + Port + "/v1";
                CurseForgeApi.Configure(ApiKey);

                List<CfCategory> categories = CurseForgeApi.GetCategoriesAsync(CancellationToken.None).Result;
                Assert(categories.Count == 2, "categories count = " + categories.Count);
                Assert(categories[0].Name == "Adventure", "categories sorted");
                Assert(categories[1].Id == 412, "category id parsed");

                ModSearchResult search = CurseForgeApi.SearchAsync("mock", "1.20.1", "fabric", 412, 0, 4,
                    CancellationToken.None).Result;
                Assert(search.TotalHits == 1, "total hits");
                Assert(search.Hits.Count == 1, "hit count");
                ModProject project = search.Hits[0];
                Assert(project.ProjectId == "cf:100", "project key " + project.ProjectId);
                Assert(project.ApiId == "100", "api id");
                Assert(project.Author == "MockAuthor", "author");
                Assert(project.Downloads == 12345, "downloads");
                Assert(project.IconUrl == "http://127.0.0.1:" + Port + "/icon.png", "icon");
                Assert(project.Categories.Count == 1 && project.Categories[0] == "Technology", "categories");

                ModVersion version = CurseForgeApi.ResolveAsync(project, "1.20.1", "fabric",
                    CancellationToken.None).Result;
                Assert(version.Id == "1001", "file id");
                Assert(version.ProjectId == "cf:100", "version project key");
                Assert(version.PrimaryFile.FileName == "mock-mod-1.0.jar", "file name");
                Assert(version.PrimaryFile.Url != null && version.PrimaryFile.Url.EndsWith("/files/mock-mod-1.0.jar"),
                    "direct url");
                Assert(version.Dependencies.Count == 1, "only required dependency kept");
                Assert(version.Dependencies[0].ProjectId == "cf:200", "dependency id");

                string dir = Path.Combine(Path.GetTempPath(), "vexor-cf-test-" + Guid.NewGuid().ToString("N"));
                var build = new InstalledBuild();
                build.Id = "cf-test";
                build.GameVersion = "1.20.1";
                build.Loader = "fabric";
                build.ProfileDir = dir;
                build.ModsDir = Path.Combine(dir, "mods");

                ModInstallResult install = CurseForgeApi.InstallAsync(version, build, null,
                    CancellationToken.None).Result;
                Assert(install.Installed == 2, "installed files = " + install.Installed);
                Assert(install.Warnings.Count == 0, "no warnings");
                Assert(File.Exists(Path.Combine(build.ModsDir, "mock-mod-1.0.jar")), "main file exists");
                Assert(File.Exists(Path.Combine(build.ModsDir, "mock-lib-1.0.jar")), "lib file exists");
                Assert(new FileInfo(Path.Combine(build.ModsDir, "mock-mod-1.0.jar")).Length == MainBytes.Length,
                    "main size");
                Assert(new FileInfo(Path.Combine(build.ModsDir, "mock-lib-1.0.jar")).Length == LibBytes.Length,
                    "lib size");
                Assert(_downloadUrlCalls == 1, "download-url endpoint used = " + _downloadUrlCalls);

                string index = File.ReadAllText(Path.Combine(build.ProfileDir, "vexor-mods.txt"));
                Assert(index.IndexOf("cf:100\tmock-mod-1.0.jar") >= 0, "index main");
                Assert(index.IndexOf("cf:200\tmock-lib-1.0.jar") >= 0, "index dependency");

                ModInstallResult again = CurseForgeApi.InstallAsync(version, build, null,
                    CancellationToken.None).Result;
                Assert(again.Installed == 0 && again.AlreadyPresent == 2, "second install is idempotent");

                Assert(CurseForgeApi.EdgeUrl("1234567", "my mod.jar") ==
                    "https://edge.forgecdn.net/files/1234/567/my%20mod.jar", "edge url");

                Assert(!_authFailed, "x-api-key sent on every API call");
                Assert(File.Exists(cachePath), "categories cached to disk");

                Console.WriteLine("ALL TESTS PASSED");
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine("TEST FAILED: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
                return 1;
            }
            finally
            {
                listener.Stop();
                if (!cacheExisted)
                {
                    try { if (File.Exists(cachePath)) File.Delete(cachePath); }
                    catch { }
                }
                else if (cacheBackedUp)
                {
                    try
                    {
                        File.Copy(backupPath, cachePath, true);
                        File.Delete(backupPath);
                    }
                    catch { }
                }
            }
        }

        private static void Serve(TcpListener listener)
        {
            while (true)
            {
                TcpClient client;
                try { client = listener.AcceptTcpClient(); }
                catch { return; }
                ThreadPool.QueueUserWorkItem(delegate(object state) { Handle((TcpClient)state); }, client);
            }
        }

        private static void Handle(TcpClient client)
        {
            try
            {
                using (client)
                using (NetworkStream stream = client.GetStream())
                {
                    var reader = new StreamReader(stream, Encoding.ASCII);
                    string requestLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(requestLine)) return;

                    var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    string line;
                    while (!string.IsNullOrEmpty((line = reader.ReadLine())))
                    {
                        int colon = line.IndexOf(':');
                        if (colon > 0) headers[line.Substring(0, colon).Trim()] = line.Substring(colon + 1).Trim();
                    }

                    string[] parts = requestLine.Split(' ');
                    string target = parts.Length > 1 ? parts[1] : "/";
                    string path = target.Split('?')[0];

                    byte[] body;
                    string contentType = "application/json";
                    int status = 200;

                    if (path.StartsWith("/v1/"))
                    {
                        string key;
                        if (!headers.TryGetValue("x-api-key", out key) || key != ApiKey)
                        {
                            _authFailed = true;
                            status = 403;
                            body = Encoding.UTF8.GetBytes("{\"errorCode\":403,\"errorMessage\":\"API Key missing or invalid\"}");
                            Write(stream, status, contentType, body);
                            return;
                        }
                    }

                    if (path == "/v1/categories")
                    {
                        body = Encoding.UTF8.GetBytes(CategoriesJson());
                    }
                    else if (path == "/v1/mods/search")
                    {
                        body = Encoding.UTF8.GetBytes(SearchJson());
                    }
                    else if (path == "/v1/mods/100/files")
                    {
                        body = Encoding.UTF8.GetBytes(MainFilesJson());
                    }
                    else if (path == "/v1/mods/200/files")
                    {
                        body = Encoding.UTF8.GetBytes(LibFilesJson());
                    }
                    else if (path == "/v1/mods/200/files/2001/download-url")
                    {
                        Interlocked.Increment(ref _downloadUrlCalls);
                        body = Encoding.UTF8.GetBytes("{\"data\":\"http://127.0.0.1:" + Port +
                            "/files/mock-lib-1.0.jar\"}");
                    }
                    else if (path == "/files/mock-mod-1.0.jar")
                    {
                        body = MainBytes;
                        contentType = "application/java-archive";
                    }
                    else if (path == "/files/mock-lib-1.0.jar")
                    {
                        body = LibBytes;
                        contentType = "application/java-archive";
                    }
                    else
                    {
                        status = 404;
                        body = Encoding.UTF8.GetBytes("{\"errorCode\":404,\"errorMessage\":\"Not found\"}");
                    }

                    Write(stream, status, contentType, body);
                }
            }
            catch
            {
            }
        }

        private static void Write(NetworkStream stream, int status, string contentType, byte[] body)
        {
            string head = "HTTP/1.1 " + status + (status == 200 ? " OK" : " ERROR") + "\r\n" +
                "Content-Type: " + contentType + "\r\n" +
                "Content-Length: " + body.Length + "\r\n" +
                "Connection: close\r\n\r\n";
            byte[] headBytes = Encoding.ASCII.GetBytes(head);
            stream.Write(headBytes, 0, headBytes.Length);
            stream.Write(body, 0, body.Length);
            stream.Flush();
        }

        private static string CategoriesJson()
        {
            return "{\"data\":[" +
                "{\"id\":422,\"gameId\":432,\"name\":\"Adventure\",\"slug\":\"adventure\",\"classId\":6,\"parentCategoryId\":6}," +
                "{\"id\":412,\"gameId\":432,\"name\":\"Technology\",\"slug\":\"technology\",\"classId\":6,\"parentCategoryId\":6}," +
                "{\"id\":4485,\"gameId\":432,\"name\":\"Blood Magic\",\"slug\":\"blood-magic\",\"classId\":6,\"parentCategoryId\":426}" +
                "]}";
        }

        private static string SearchJson()
        {
            return "{\"data\":[{" +
                "\"id\":100,\"gameId\":432,\"name\":\"Mock Mod\",\"slug\":\"mock-mod\"," +
                "\"summary\":\"Mock summary\",\"downloadCount\":12345," +
                "\"authors\":[{\"id\":1,\"name\":\"MockAuthor\"}]," +
                "\"logo\":{\"thumbnailUrl\":\"http://127.0.0.1:" + Port + "/icon.png\",\"url\":\"http://127.0.0.1:" + Port + "/icon.png\"}," +
                "\"categories\":[{\"id\":412,\"name\":\"Technology\",\"slug\":\"technology\"}]," +
                "\"links\":{\"websiteUrl\":\"https://www.curseforge.com/minecraft/mc-mods/mock-mod\"}" +
                "}],\"pagination\":{\"index\":0,\"pageSize\":4,\"resultCount\":1,\"totalCount\":1}}";
        }

        private static string MainFilesJson()
        {
            return "{\"data\":[{" +
                "\"id\":1001,\"gameId\":432,\"modId\":100,\"displayName\":\"Mock Mod 1.0\"," +
                "\"fileName\":\"mock-mod-1.0.jar\",\"releaseType\":1,\"fileLength\":" + MainBytes.Length + "," +
                "\"downloadUrl\":\"http://127.0.0.1:" + Port + "/files/mock-mod-1.0.jar\"," +
                "\"isAvailable\":true,\"hashes\":{\"sha1\":\"" + Sha1(MainBytes) + "\"}," +
                "\"gameVersions\":[\"1.20.1\",\"Fabric\"]," +
                "\"dependencies\":[{\"modId\":200,\"relationType\":3},{\"modId\":300,\"relationType\":2}]" +
                "}],\"pagination\":{\"index\":0,\"pageSize\":50,\"resultCount\":1,\"totalCount\":1}}";
        }

        private static string LibFilesJson()
        {
            return "{\"data\":[{" +
                "\"id\":2001,\"gameId\":432,\"modId\":200,\"displayName\":\"Mock Library 1.0\"," +
                "\"fileName\":\"mock-lib-1.0.jar\",\"releaseType\":1,\"fileLength\":" + LibBytes.Length + "," +
                "\"downloadUrl\":null,\"isAvailable\":true," +
                "\"hashes\":{\"sha1\":\"" + Sha1(LibBytes) + "\"}," +
                "\"gameVersions\":[\"1.20.1\",\"Fabric\"],\"dependencies\":[]" +
                "}],\"pagination\":{\"index\":0,\"pageSize\":50,\"resultCount\":1,\"totalCount\":1}}";
        }

        private static byte[] Fill(int size, byte value)
        {
            byte[] data = new byte[size];
            for (int i = 0; i < size; i++) data[i] = value;
            return data;
        }

        private static string Sha1(byte[] data)
        {
            using (var sha = SHA1.Create())
            {
                byte[] hash = sha.ComputeHash(data);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static void Assert(bool condition, string message)
        {
            if (!condition) throw new Exception("assert failed: " + message);
        }
    }
}
