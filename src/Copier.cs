using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using PlexCopier.Settings;
using PlexCopier.TvDb;

namespace PlexCopier
{
    public class Copier
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private static readonly string InvalidPathCharacters = @"<>:\\/""'!\|\?\*";

        private Arguments arguments;

        private ITvDbClient client;

        private Options options;

        public Copier(Arguments arguments, ITvDbClient client, Options options)
        {
            this.arguments = arguments;
            this.client = client;
            this.options = options;
        }

        public async Task<int> CopyFiles()
        {
            int matches = 0;
            foreach (var file in FindTargetFiles())
            {
                var match = await this.FindSeriesForFile(file);
                if (match != null)
                {
                    matches++;
                    this.CopyFile(file, match);
                }
            }

            return matches;
        }

        private void CopyFile(string source, SeriesMatch match)
        {
            var seriesName = Regex.Replace(match.Info.Name, $"( *[{InvalidPathCharacters}]+ *)+", " ");

            var file = $"{seriesName} - s{match.Season:D2}e{match.Episode:D2}{Path.GetExtension(source)}";
            var directory = Path.Combine(this.options.Collection, seriesName, $"Season {match.Season:D2}");

            if (!Directory.Exists(directory))
            {
                Log.Info($"Creating directory {directory}");
                if (!this.arguments.Test)
                {
                    Directory.CreateDirectory(directory);
                }
            }

            var target = Path.Combine(directory, file);
            if (File.Exists(target))
            {
                if (!match.ReplaceExisting)
                {
                    Log.Warn($"Skipping existing file {target}");
                    return;
                }
                else
                {
                    Log.Warn($"Deleting existing file {target}");
                    if (!this.arguments.Test)
                    {
                        File.Delete(target);
                    }
                }
            }

            if (match.MoveFiles)
            {
                Log.Info($"Moving file: {source} -> {target}");
                if (!this.arguments.Test)
                {
                    File.Move(source, target);
                }
            }
            else
            {
                Log.Info($"Copying file: {source} -> {target}");
                if (!this.arguments.Test)
                {
                    File.Copy(source, target);
                }
            }
        }

        private async Task<SeriesMatch> FindSeriesForFile(string file)
        {
            foreach (var series in this.options.Series)
            {
                foreach (var pattern in series.Patterns)
                {
                    var match = pattern.Regex.Match(Path.GetFileNameWithoutExtension(file));
                    if (match.Success)
                    {
                        var seriesInfo = await this.client.GetSeriesInfo(series.Id);
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

        private IEnumerable<string> FindTargetFiles()
        {
            string target = this.arguments.Target;
            if (File.Exists(target))
            {
                yield return target;
            }
            else if (!Directory.Exists(target))
            {
                throw new FatalException($"Target does not exist: {target}");
            }
            else if (!this.arguments.Recursive)
            {
                foreach (var file in Directory.GetFiles(target).OrderBy(f => f))
                {
                    yield return file;
                }
            }
            else
            {
                var stack = new Stack<string>();
                stack.Push(target);
                while (stack.Count > 0)
                {
                    target = stack.Pop();
                    foreach (var directory in Directory.GetDirectories(target).OrderBy(d => d))
                    {
                        stack.Push(directory);
                    }
                    foreach (var file in Directory.GetFiles(target).OrderBy(f => f))
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
                this.Series = series;
                this.Pattern = pattern;
                this.Info = info;
                this.Season = season;
                this.Episode = episode;
            }

            public bool MoveFiles
            {
                get
                {
                    if (this.Pattern.MoveFiles.HasValue)
                    {
                        return this.Pattern.MoveFiles.Value;
                    }
                    else if (this.Series.MoveFiles.HasValue)
                    {
                        return this.Series.MoveFiles.Value;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            public bool ReplaceExisting
            {
                get
                {
                    if (this.Pattern.ReplaceExisting.HasValue)
                    {
                        return this.Pattern.ReplaceExisting.Value;
                    }
                    else if (this.Series.ReplaceExisting.HasValue)
                    {
                        return this.Series.ReplaceExisting.Value;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }
    }
}