using System.Text.RegularExpressions;

using PlexCopier.Settings;
using PlexCopier.TvDb;

namespace PlexCopier
{
    public class Copier(Arguments arguments, ITvDbClient client, Options options) : ICopier
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Copier));

        private static readonly string InvalidPathCharacters = @"<>:\\/""'!\|\?\*";

        private Arguments arguments = arguments;

        private ITvDbClient client = client;

        private Options options = options;

        private SemaphoreSlim copySemaphore = new 
            (
                arguments.ParallelOperations > 0 
                    ? arguments.ParallelOperations 
                    : int.MaxValue
            );

        public async Task<int> CopyFiles(string? source = null)
        {
            int matches = 0;
            foreach (var file in FindTargetFiles(source ?? arguments.Target))
            {
                if (await CopyFile(file))
                {
                    matches++;
                }
            }

            return matches;
        }

        private async Task<bool> CopyFile(string source)
        {
            var match = await FindSeriesForFile(source);
            if (match != null)
            {
                await CopyFile(source, match);
                return true;
            }

            Log.Warn($"A match could not be found for file {source}");
            return false;
        }

        private async Task CopyFile(string source, SeriesMatch match)
        {
            await copySemaphore.WaitAsync();
            try
            {
                var seriesName = Regex.Replace(match.Info.Name, $"( *[{InvalidPathCharacters}]+ *)+", " ");

                var file = $"{seriesName} - s{match.Season:D2}e{match.Episode:D2}{Path.GetExtension(source)}";
                var directory = Path.Combine(options.Collection, seriesName, $"Season {match.Season:D2}");

                CreateDirectoryIfNeeded(directory);

                var target = Path.Combine(directory, file);
                if (File.Exists(target) && !arguments.Force)
                {
                    Log.Warn($"Skipping existing file {target}");
                    return;
                }

                using var fileLock = new FileLock(source);

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
                        File.Move(source, target);
                        Log.Info($"File moved to: {target}");
                    }
                }
                else
                {
                    Log.Info($"Copying file: {source} -> {target}");
                    if (!arguments.Test)
                    {
                        File.Copy(source, target, arguments.Force);
                        Log.Info($"File copied to: {target}");
                    }
                }

                if (arguments.Verify)
                {
                    var compare = new FileCompare();
                    if (!await compare.AreSame(source, target))
                    {
                        Log.Warn($"Target file did not match source, will be deleted: {target}");
                        File.Delete(target);
                    }
                }
            }
            finally
            {
                copySemaphore.Release();
            }
        }

        private void CreateDirectoryIfNeeded(string directory)
        {
            if (!Directory.Exists(directory))
            {
                Log.Info($"Creating directory {directory}");
                if (!arguments.Test)
                {
                    Directory.CreateDirectory(directory);
                }
            }
        }

        private async Task<SeriesMatch?> FindSeriesForFile(string file)
        {
            foreach (var series in options.Series)
            {
                foreach (var pattern in series.Patterns)
                {
                    var match = pattern.Regex.Match(Path.GetFileNameWithoutExtension(file));
                    if (match.Success)
                    {
                        var seriesInfo = await client.GetSeriesInfo(series.Id);
                        var season = pattern.SeasonStart.GetValueOrDefault(1);
                        var episode = match.Groups.Count > 1 ? int.Parse(match.Groups[1].Value) : 1;
                        episode += pattern.EpisodeOffset.GetValueOrDefault(0);
                        while (episode > seriesInfo.Seasons[season].EpisodeCount && seriesInfo.Seasons.Length > season + 1)
                        {
                            episode -= seriesInfo.Seasons[season].EpisodeCount;
                            season++;
                        }

                        return new SeriesMatch(series, pattern, seriesInfo, season, episode);
                    }                
                }
            }

            return null;
        }

        public IEnumerable<string> FindTargetFiles(string source)
        {
            if (File.Exists(source))
            {
                yield return source;
            }
            else if (!Directory.Exists(source))
            {
                throw new FatalException($"Target does not exist: {source}");
            }
            else if (!arguments.Recursive)
            {
                foreach (var file in Directory.GetFiles(source).OrderBy(f => f))
                {
                    yield return file;
                }
            }
            else
            {
                var stack = new Stack<string>();
                stack.Push(source);
                while (stack.Count > 0)
                {
                    source = stack.Pop();
                    foreach (var directory in Directory.GetDirectories(source).OrderBy(d => d))
                    {
                        stack.Push(directory);
                    }
                    foreach (var file in Directory.GetFiles(source).OrderBy(f => f))
                    {
                        yield return file;
                    }
                }
            }
        }

        private class SeriesMatch
        {
            public Series Series { get; set; }

            public Pattern Pattern { get; set; }

            public SeriesInfo Info { get; set; }

            public int Season { get; set; }

            public int Episode { get; set; }

            public SeriesMatch(Series series, Pattern pattern, SeriesInfo info, int season, int episode)
            {
                Series = series;
                Pattern = pattern;
                Info = info;
                Season = season;
                Episode = episode;
            }
        }
    }
}