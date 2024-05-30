using System.Security.Cryptography;
using System.Text;

namespace PlexCopier.Utils
{
    public class FileCompare : IFileCompare
    {
        private const int ReadBuffer = 65536; // 64KB

        public async Task<bool> AreSame(string filePath1, string filePath2)
        {
            Task<string>[] tasks = [ComputeHash(filePath1), ComputeHash(filePath2)];
            var hashes = await Task.WhenAll(tasks);
            return string.CompareOrdinal(hashes[0], hashes[1]) == 0;
        }

        private static async Task<string> ComputeHash(string filePath)
        {
            byte[] hashBytes;
            using (var stream = OpenSource(filePath))
            {
                using var md5 = MD5.Create();
                hashBytes = await md5.ComputeHashAsync(stream);
            }
            StringBuilder sb = new();
            foreach (byte bt in hashBytes)
            {
                sb.Append(bt.ToString("x2"));
            }
            return sb.ToString();
        }

        private static FileStream OpenSource(string source)
        {
            return new FileStream(
                source, 
                FileMode.Open, 
                FileAccess.Read, 
                FileShare.Read, 
                ReadBuffer, 
                FileOptions.Asynchronous | FileOptions.SequentialScan);
        }
    }
}