using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Vexor.Core
{
    public sealed class DownloadItem
    {
        public string Url;
        public string Dest;
        public string Sha1;
        public long Size;

        public DownloadItem(string url, string dest, string sha1, long size)
        {
            Url = url;
            Dest = dest;
            Sha1 = sha1;
            Size = size;
        }
    }

    public sealed class BatchProgress
    {
        public string Status = "";
        public int FilesDone;
        public int FilesTotal;
        public long BytesDone;
        public long BytesTotal;

        public double Fraction
        {
            get
            {
                if (BytesTotal > 0)
                {
                    double f = (double)BytesDone / BytesTotal;
                    return f < 0 ? 0 : (f > 1 ? 1 : f);
                }
                if (FilesTotal > 0) return (double)FilesDone / FilesTotal;
                return 0;
            }
        }

        public static string FormatMb(long bytes)
        {
            if (bytes <= 0) return "0 МБ";
            double mb = bytes / (1024.0 * 1024.0);
            if (mb >= 1024) return (mb / 1024.0).ToString("0.0") + " ГБ";
            return mb.ToString("0") + " МБ";
        }
    }

    public static class Web
    {
        private static readonly HttpClient Client;

        static Web()
        {
            ConfigureTls();
            try
            {
                ServicePointManager.DefaultConnectionLimit = 64;
                ServicePointManager.Expect100Continue = false;
            }
            catch
            {
            }

            var handler = new HttpClientHandler();
            handler.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            Client = new HttpClient(handler);
            Client.Timeout = TimeSpan.FromHours(3);
            Client.DefaultRequestHeaders.UserAgent.ParseAdd("VexorLauncher/" + AppInfo.Version);
        }

        // Некоторые сервисы Mojang согласуют только TLS 1.3, поэтому не ограничиваемся 1.2.
        public static void ConfigureTls()
        {
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | (SecurityProtocolType)12288;
                return;
            }
            catch
            {
            }
            try
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            }
            catch
            {
            }
        }

        public static Task<string> GetStringAsync(string url, CancellationToken ct)
        {
            return GetStringAsync(url, null, ct);
        }

        public static async Task<string> GetStringAsync(string url, IDictionary<string, string> headers,
            CancellationToken ct)
        {
            Exception last = null;
            for (int attempt = 1; attempt <= 3; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                bool retry = false;
                try
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                    {
                        if (headers != null)
                        {
                            foreach (var pair in headers)
                                request.Headers.TryAddWithoutValidation(pair.Key, pair.Value);
                        }
                        using (var response = await Client.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct).ConfigureAwait(false))
                        {
                            response.EnsureSuccessStatusCode();
                            return await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    last = ex;
                    retry = attempt < 3;
                }
                if (retry) await Task.Delay(500 * attempt, ct).ConfigureAwait(false);
            }
            throw new IOException("Не удалось получить данные с " + url + ": " + last.Message, last);
        }

        public static async Task DownloadFileAsync(string url, string dest, string sha1, long expectedSize,
            Action<long, long> onProgress, CancellationToken ct)
        {
            string dir = Path.GetDirectoryName(dest);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            string temp = dest + ".part";

            for (int attempt = 1; attempt <= 3; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                bool retry = false;
                try
                {
                    TryDelete(temp);
                    using (var response = await Client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        long total = expectedSize;
                        if (response.Content.Headers.ContentLength.HasValue && response.Content.Headers.ContentLength.Value > 0)
                            total = response.Content.Headers.ContentLength.Value;

                        using (var source = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        using (var target = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None, 65536, true))
                        {
                            byte[] buffer = new byte[65536];
                            long done = 0;
                            long lastReported = 0;
                            int read;
                            while ((read = await source.ReadAsync(buffer, 0, buffer.Length, ct).ConfigureAwait(false)) > 0)
                            {
                                await target.WriteAsync(buffer, 0, read, ct).ConfigureAwait(false);
                                done += read;
                                if (onProgress != null && (done - lastReported > 512 * 1024 || (total > 0 && done >= total)))
                                {
                                    lastReported = done;
                                    onProgress(done, total);
                                }
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(sha1))
                    {
                        string actual = Sha1File(temp);
                        if (!string.Equals(actual, sha1, StringComparison.OrdinalIgnoreCase))
                            throw new IOException("Файл повреждён (SHA1 не совпал): " + Path.GetFileName(dest));
                    }

                    if (File.Exists(dest)) File.Delete(dest);
                    File.Move(temp, dest);
                    if (onProgress != null && expectedSize > 0) onProgress(expectedSize, expectedSize);
                    return;
                }
                catch (OperationCanceledException)
                {
                    TryDelete(temp);
                    throw;
                }
                catch (Exception ex)
                {
                    TryDelete(temp);
                    if (attempt >= 3) throw new IOException("Не удалось скачать " + url + ": " + ex.Message, ex);
                    retry = true;
                }
                if (retry) await Task.Delay(700 * attempt, ct).ConfigureAwait(false);
            }
        }

        public static async Task DownloadBatchAsync(IList<DownloadItem> items, int concurrency,
            IProgress<BatchProgress> progress, string status, CancellationToken ct)
        {
            if (items == null || items.Count == 0) return;
            if (concurrency < 1) concurrency = 1;
            if (concurrency > 16) concurrency = 16;

            long totalBytes = 0;
            foreach (DownloadItem item in items) if (item.Size > 0) totalBytes += item.Size;

            int done = 0;
            long doneBytes = 0;
            var pending = new List<DownloadItem>();
            foreach (DownloadItem item in items)
            {
                if (IsUpToDate(item))
                {
                    done++;
                    if (item.Size > 0) doneBytes += item.Size;
                }
                else
                {
                    pending.Add(item);
                }
            }

            Report(progress, status, items.Count, done, totalBytes, doneBytes);
            if (pending.Count == 0) return;

            object gate = new object();
            long lastTick = 0;

            using (var limiter = new SemaphoreSlim(concurrency))
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(ct))
            {
                var tasks = new Task[pending.Count];
                for (int index = 0; index < pending.Count; index++)
                {
                    DownloadItem item = pending[index];
                    tasks[index] = Task.Run(async () =>
                    {
                        await limiter.WaitAsync(linked.Token).ConfigureAwait(false);
                        try
                        {
                            await DownloadFileAsync(item.Url, item.Dest, item.Sha1, item.Size, null, linked.Token).ConfigureAwait(false);
                            lock (gate)
                            {
                                done++;
                                if (item.Size > 0) doneBytes += item.Size;
                                long tick = Environment.TickCount;
                                if (tick - lastTick > 140 || done == items.Count)
                                {
                                    lastTick = tick;
                                    Report(progress, status, items.Count, done, totalBytes, doneBytes);
                                }
                            }
                        }
                        catch
                        {
                            linked.Cancel();
                            throw;
                        }
                        finally
                        {
                            limiter.Release();
                        }
                    });
                }

                try
                {
                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    if (ct.IsCancellationRequested) throw new OperationCanceledException(ct);
                    throw Unwrap(ex);
                }
            }
        }

        private static Exception Unwrap(Exception ex)
        {
            var aggregate = ex as AggregateException;
            if (aggregate == null) return ex;
            var flat = aggregate.Flatten();
            foreach (Exception inner in flat.InnerExceptions)
            {
                if (!(inner is OperationCanceledException)) return inner;
            }
            return flat.InnerExceptions.Count > 0 ? flat.InnerExceptions[0] : ex;
        }

        private static void Report(IProgress<BatchProgress> progress, string status, int total, int done, long totalBytes, long doneBytes)
        {
            if (progress == null) return;
            var report = new BatchProgress();
            report.Status = status;
            report.FilesTotal = total;
            report.FilesDone = done;
            report.BytesTotal = totalBytes;
            report.BytesDone = doneBytes;
            progress.Report(report);
        }

        public static bool IsUpToDate(DownloadItem item)
        {
            try
            {
                var info = new FileInfo(item.Dest);
                if (!info.Exists) return false;
                if (item.Size > 0) return info.Length == item.Size;
                return info.Length > 0;
            }
            catch
            {
                return false;
            }
        }

        public static string Sha1File(string path)
        {
            using (var sha1 = SHA1.Create())
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                byte[] hash = sha1.ComputeHash(stream);
                var sb = new System.Text.StringBuilder(hash.Length * 2);
                foreach (byte b in hash) sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch
            {
            }
        }
    }
}
