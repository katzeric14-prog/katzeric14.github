using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Vexor.Core
{
    internal sealed class ModIndexEntry
    {
        public string File;
        public string Title;
    }

    internal static class ModIndexStore
    {
        public static Dictionary<string, ModIndexEntry> Read(string path)
        {
            var index = new Dictionary<string, ModIndexEntry>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(path)) return index;
            try
            {
                foreach (string line in File.ReadAllLines(path))
                {
                    string[] parts = line.Split('\t');
                    if (parts.Length < 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1])) continue;
                    var entry = new ModIndexEntry();
                    entry.File = parts[1];
                    entry.Title = parts.Length > 2 ? parts[2] : "";
                    index[parts[0]] = entry;
                }
            }
            catch
            {
            }
            return index;
        }

        public static void Write(string path, Dictionary<string, ModIndexEntry> index)
        {
            try
            {
                var text = new StringBuilder();
                foreach (var pair in index)
                    text.Append(pair.Key).Append('\t').Append(pair.Value.File).Append('\t')
                        .Append(pair.Value.Title).Append("\r\n");
                File.WriteAllText(path, text.ToString(), new UTF8Encoding(false));
            }
            catch
            {
            }
        }

        public static string SafeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            string file = Path.GetFileName(name.Replace('/', Path.DirectorySeparatorChar));
            foreach (char c in Path.GetInvalidFileNameChars()) file = file.Replace(c, '_');
            return file.Trim();
        }

        public static string Clean(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            return text.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
        }

        public static void TryDelete(string path)
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
