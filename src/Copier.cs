using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using PlexCopier.Settings;
using PlexCopier.TvDb;

namespace PlexCopier
{
    public class Copier
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private Arguments arguments;

        private ITvDbClient client;

        private Options options;

        public Copier(Arguments arguments, ITvDbClient client, Options options)
        {
            this.arguments = arguments;
            this.client = client;
            this.options = options;
        }

        public async Task CopyFiles()
        {
            foreach (var file in FindTargetFiles())
            {
                var match = await this.FindSeriesForFile(file);
                if (match != null)
                {
                    this.CopyFile(file, match);
                }
            }
        }

        private void CopyFile(string source, SeriesMatch match)
        {
            var seriesName = match.Info.Name.Replace(":", " ");
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
                if (!match.Series.ReplaceExisting)
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

            if (match.Series.MoveFiles)
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
                        var episode = int.Parse(match.Groups[1].Value);
                        episode += pattern.EpisodeOffset.GetValueOrDefault(0);
                        while (episode > seriesInfo.Seasons[season].EpisodeCount && seriesInfo.Seasons.Length > season + 1)
                        {
                            episode -= seriesInfo.Seasons[season].EpisodeCount;
                            season++;
                        }

                        return new SeriesMatch(series, seriesInfo, season, episode);
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
            public SeriesInfo Info { get; set; }

            public int Season { get; set; }

            public int Episode { get; set; }

            public SeriesMatch(Series series, SeriesInfo info, int season, int episode)
            {
                this.Series = series;
                this.Info = info;
                this.Season = season;
                this.Episode = episode;
            }
        }
    }
}