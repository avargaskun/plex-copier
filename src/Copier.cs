using System.Text.RegularExpressions;

using PlexCopier.Settings;
using PlexCopier.TvDb;
using PlexCopier.Utils;

namespace PlexCopier
{
    public class Copier(Arguments arguments, Options options) : ICopier
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Copier));

        private static readonly string InvalidPathCharacters = @"<>:\\/""'!\|\?\*";

        private SemaphoreSlim copySemaphore = new 
        (
            arguments.ParallelOperations > 0 
                ? arguments.ParallelOperations 
                : int.MaxValue
        );

        protected internal IEpisodeFinder EpisodeFinder { get; set; } = new EpisodeFinder(options);
        
        protected internal IPathTraverser PathTraverser { get; set; } = new PathTraverser();

        protected internal IFileManager FileManager { get; set; } = new FileManager(arguments);

        protected internal Func<string, IFileLock> FileLockFactory { get; set; } = path => new FileLock(path);

        public Task<int> CopyFiles(CancellationToken token)
        {
            return CopyFiles(arguments.Target, token);
        }

        public async Task<int> CopyFiles(string source, CancellationToken token)
        {
            int matches = 0;
            var foundFiles = PathTraverser.FindFilesInPath(source ?? arguments.Target, arguments.Recursive);
            var copyTasks = foundFiles.Select(file => CopySingleFile(file, token));
            // The cancellation token is not used here - that is on purpose
            // This should wait for any copy operation that is in flight
            await Task.WhenAll();
            foreach (var task in copyTasks)
            {
                if (task.IsFaulted)
                {
                    Log.Error($"Failed to copy a file in {source}", task.Exception);
                }
                else if (task.Result)
                {
                    matches++;
                }
            }

            return matches;
        }

        private async Task<bool> CopySingleFile(string source, CancellationToken token)
        {
            var match = await EpisodeFinder.FindForFile(source, token);
            if (match != null)
            {
                await copySemaphore.WaitAsync(token);
                try
                {
                    // Once we begin the copy operation there's no turning back. 
                    // The CancellationToken will not be used beyond this point
                    token.ThrowIfCancellationRequested();
                    await CopySingleFile(source, match);
                    return true;
                }
                finally
                {
                    copySemaphore.Release();
                }
            }

            Log.Warn($"A match could not be found for file {source}");
            return false;
        }

        private async Task CopySingleFile(string source, EpisodeMatch match)
        {
            var seriesName = Regex.Replace(match.Info.Name, $"( *[{InvalidPathCharacters}]+ *)+", " ");

            var file = $"{seriesName} - s{match.Season:D2}e{match.Episode:D2}{Path.GetExtension(source)}";
            var directory = Path.Combine(options.Collection, seriesName, $"Season {match.Season:D2}");

            Directory.CreateDirectory(directory);

            var target = Path.Combine(directory, file);
            if (File.Exists(target) && !arguments.Force)
            {
                Log.Warn($"Skipping existing file {target}");
                return;
            }

            using (var fileLock = FileLockFactory(source))
            {
                if (arguments.LockFiles && !fileLock.Acquire())
                {
                    Log.Warn($"Could not lock file, skipping: {target}");
                    return;
                }

                if (arguments.MoveFiles)
                {
                    Log.Info($"Moving file: {source} -> {target}");
                    if (!arguments.Test)
                    {
                        await FileManager.MoveAsync(source, target);
                        Log.Info($"File moved to: {target}");
                    }
                }
                else
                {
                    Log.Info($"Copying file: {source} -> {target}");
                    if (!arguments.Test)
                    {
                        await FileManager.CopyAsync(source, target);
                        Log.Info($"File copied to: {target}");
                    }
                }
            }

            if (arguments.Verify)
            {
                Log.Info($"Verifying file integrity for: {target}");
                var compare = new FileCompare();
                if (!await compare.AreSame(source, target))
                {
                    Log.Warn($"Target file did not match source, will be deleted: {target}");
                    FileManager.Delete(target);
                }
                else
                {
                    Log.Info($"File {target} verified successfully");
                }
            }
        }
    }
}