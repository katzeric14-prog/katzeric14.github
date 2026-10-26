using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;

internal static class WpfDecodeTest
{
    private static void Main(string[] args)
    {
        string path = args[0];
        Exception mtaError = null;
        int w = 0, h = 0;
        var thread = new Thread(delegate()
        {
            try
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    BitmapSource source = decoder.Frames[0];
                    if (source.Format != System.Windows.Media.PixelFormats.Bgra32)
                        source = new FormatConvertedBitmap(source, System.Windows.Media.PixelFormats.Bgra32, null, 0);
                    w = source.PixelWidth;
                    h = source.PixelHeight;
                    using (var bitmap = new Bitmap(w, h, PixelFormat.Format32bppArgb))
                    {
                        BitmapData data = bitmap.LockBits(new Rectangle(0, 0, w, h), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
                        try { source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Stride * h, data.Stride); }
                        finally { bitmap.UnlockBits(data); }
                        bitmap.Save(Path.Combine(Path.GetDirectoryName(path), "wpf_decoded.png"), ImageFormat.Png);
                    }
                }
            }
            catch (Exception ex) { mtaError = ex; }
        });
        thread.SetApartmentState(ApartmentState.MTA);
        thread.Start();
        thread.Join();
        if (mtaError != null)
        {
            Console.WriteLine("MTA FAIL: " + mtaError);
            Environment.Exit(1);
        }
        Console.WriteLine("MTA OK " + w + "x" + h);
    }
}
