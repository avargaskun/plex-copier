using System.Security.Cryptography;
using System.Text;

namespace PlexCopier.Utils
{
    public class FileCompare()
    {
        public async Task<bool> AreSame(string filePath1, string filePath2)
        {
            Task<string>[] tasks = [ComputeHash(filePath1), ComputeHash(filePath2)];
            var hashes = await Task.WhenAll(tasks);
            return string.CompareOrdinal(hashes[0], hashes[1]) == 0;
        }

        private async Task<string> ComputeHash(string filePath)
        {
            byte[] hashBytes;
            using (var stream = File.OpenRead(filePath))
            {
                using var md5 = MD5.Create();
                hashBytes = await md5.ComputeHashAsync(stream);
            }
            StringBuilder sb = new StringBuilder();
            foreach (byte bt in hashBytes)
            {
                sb.Append(bt.ToString("x2"));
            }
            return sb.ToString();
        }
    }
}