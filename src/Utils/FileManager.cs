
using PlexCopier.Settings;

namespace PlexCopier.Utils
{
    public class FileManager(Arguments arguments) : IFileManager
    {
        public async Task CopyAsync(string sourceFile, string targetFile)
        {
            using var sourceStream = OpenSource(sourceFile);
            using var targetStream = OpenTarget(targetFile);
            await sourceStream.CopyToAsync(targetStream);
        }

        public async Task MoveAsync(string sourceFile, string targetFile)
        {
            await CopyAsync(sourceFile, targetFile);
            File.Delete(sourceFile);
        }

        public void Delete(string path)
        {
            File.Delete(path);
        }

        private FileStream OpenSource(string source)
        {
            return new FileStream(
                source, 
                FileMode.Open, 
                FileAccess.Read, 
                FileShare.Read, 
                arguments.FileBuffer, 
                FileOptions.Asynchronous | FileOptions.SequentialScan);
        }

        private FileStream OpenTarget(string target)
        {
            var fileOptions = FileOptions.Asynchronous | FileOptions.SequentialScan;
            if (arguments.WriteThrough)
            {
                fileOptions |= FileOptions.WriteThrough;
            }

            return new FileStream(
                target, 
                arguments.Force ? FileMode.Create : FileMode.CreateNew, 
                FileAccess.Write, 
                FileShare.None, 
                arguments.FileBuffer, 
                fileOptions);
        }
    }
}