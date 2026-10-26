using System;
using System.Security.Cryptography;
using System.Text;

namespace Vexor.Core
{
    public static class OfflineAuth
    {
        public static string UuidFor(string nickname)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes("OfflinePlayer:" + nickname));
                hash[6] = (byte)((hash[6] & 0x0F) | 0x30);
                hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
                return Format(hash);
            }
        }

        private static string Format(byte[] bytes)
        {
            var sb = new StringBuilder(36);
            for (int i = 0; i < 16; i++)
            {
                if (i == 4 || i == 6 || i == 8 || i == 10) sb.Append('-');
                sb.Append(bytes[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }
}
