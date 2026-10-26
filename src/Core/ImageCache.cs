using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WpfImaging = System.Windows.Media.Imaging;
using WpfMedia = System.Windows.Media;

namespace Vexor.Core
{
    public static class ImageCache
    {
        private const int MaxMemory = 180;

        private static readonly object Gate = new object();
        private static readonly Dictionary<string, Image> Memory =
            new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private static readonly Dictionary<string, Task<Image>> Inflight =
            new Dictionary<string, Task<Image>>(StringComparer.OrdinalIgnoreCase);
        private static readonly HashSet<string> Failed =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly Queue<string> Order = new Queue<string>();

        public static Image Cached(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            lock (Gate)
            {
                Image image;
                return Memory.TryGetValue(key, out image) ? image : null;
            }
        }

        public static async Task<Image> LoadAsync(string url, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(url)) return null;
            Image cached = Cached(url);
            if (cached != null) return cached;

            Task<Image> task;
            bool owner = false;
            lock (Gate)
            {
                if (Failed.Contains(url)) return null;
                if (!Inflight.TryGetValue(url, out task))
                {
                    task = LoadCoreAsync(url, ct);
                    Inflight[url] = task;
                    owner = true;
                }
            }

            try
            {
                return await task.ConfigureAwait(false);
            }
            finally
            {
                if (owner)
                {
                    lock (Gate) { Inflight.Remove(url); }
                }
            }
        }

        public static Image LoadEmbedded(string key, string resourceName)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(resourceName)) return null;
            Image cached = Cached(key);
            if (cached != null) return cached;
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return null;
                    using (var image = Image.FromStream(stream))
                    {
                        var bitmap = new Bitmap(image);
                        Remember(key, bitmap);
                        return bitmap;
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private static async Task<Image> LoadCoreAsync(string url, CancellationToken ct)
        {
            string path = CachePath(url);
            Image image = null;
            try
            {
                if (File.Exists(path) && new FileInfo(path).Length > 0)
                {
                    image = await DecodeAsync(path).ConfigureAwait(false);
                    if (image == null) TryDelete(path);
                }

                if (image == null)
                {
                    await Web.DownloadFileAsync(url, path, null, 0, null, ct).ConfigureAwait(false);
                    image = await DecodeAsync(path).ConfigureAwait(false);
                    if (image == null)
                    {
                        TryDelete(path);
                        throw new InvalidOperationException("Не удалось прочитать изображение " + url);
                    }
                }

                Remember(url, image);
                return image;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch
            {
                lock (Gate) { Failed.Add(url); }
                return null;
            }
        }

        private static Task<Image> DecodeAsync(string path)
        {
            return Task.Factory.StartNew(delegate { return Decode(path); },
                CancellationToken.None, TaskCreationOptions.None, TaskScheduler.Default);
        }

        private static Image Decode(string path)
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var image = Image.FromStream(stream))
                {
                    return new Bitmap(image);
                }
            }
            catch
            {
            }

            try
            {
                return DecodeWpf(path);
            }
            catch
            {
                return null;
            }
        }

        private static Image DecodeWpf(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                WpfImaging.BitmapDecoder decoder = WpfImaging.BitmapDecoder.Create(stream,
                    WpfImaging.BitmapCreateOptions.PreservePixelFormat,
                    WpfImaging.BitmapCacheOption.OnLoad);
                WpfImaging.BitmapSource source = decoder.Frames[0];
                if (source.Format != WpfMedia.PixelFormats.Bgra32)
                    source = new WpfImaging.FormatConvertedBitmap(source, WpfMedia.PixelFormats.Bgra32, null, 0);

                int width = source.PixelWidth;
                int height = source.PixelHeight;
                var bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
                BitmapData data = bitmap.LockBits(new Rectangle(0, 0, width, height),
                    ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                try
                {
                    source.CopyPixels(System.Windows.Int32Rect.Empty, data.Scan0, data.Stride * height, data.Stride);
                }
                finally
                {
                    bitmap.UnlockBits(data);
                }
                return bitmap;
            }
        }

        private static void Remember(string key, Image image)
        {
            lock (Gate)
            {
                if (!Memory.ContainsKey(key)) Order.Enqueue(key);
                Memory[key] = image;
                while (Memory.Count > MaxMemory && Order.Count > 0)
                {
                    string old = Order.Dequeue();
                    if (!string.Equals(old, key, StringComparison.OrdinalIgnoreCase))
                        Memory.Remove(old);
                }
            }
        }

        private static string CachePath(string url)
        {
            return Path.Combine(AppPaths.MetaDir, "icons", Hash(url) + ".img");
        }

        private static string Hash(string text)
        {
            using (var sha = SHA1.Create())
            {
                byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(text));
                var builder = new StringBuilder(bytes.Length * 2);
                for (int i = 0; i < bytes.Length; i++) builder.Append(bytes[i].ToString("x2"));
                return builder.ToString();
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
