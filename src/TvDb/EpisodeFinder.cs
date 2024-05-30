using PlexCopier.Settings;

namespace PlexCopier.TvDb
{
    public class EpisodeFinder(Options options) : IEpisodeFinder
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(EpisodeFinder));

        protected internal ITvDbClient Client { get; set; } = new TvDbClient(options);

        public async Task<EpisodeMatch?> FindForFile(string file, CancellationToken token)
        {
            foreach (var series in options.Series)
            {
                foreach (var pattern in series.Patterns)
                {
                    var match = pattern.Regex.Match(Path.GetFileNameWithoutExtension(file));
                    if (match.Success)
                    {
                        var seriesInfo = await Client.GetSeriesInfo(series.Id, token);
                        var season = pattern.SeasonStart.GetValueOrDefault(1);
                        var episode = match.Groups.Count > 1 ? int.Parse(match.Groups[1].Value) : 1;
                        episode += pattern.EpisodeOffset.GetValueOrDefault(0);
                        while
                        (
                            season < seriesInfo.Seasons.Length &&
                            episode > seriesInfo.Seasons[season].EpisodeCount &&
                            seriesInfo.Seasons.Length > season + 1
                        )
                        {
                            episode -= seriesInfo.Seasons[season].EpisodeCount;
                            season++;
                        }

                        if (season >= seriesInfo.Seasons.Length)
                        {
                            Log.Info($"No valid season found for {file}");
                            return null;
                        }

                        return new EpisodeMatch(seriesInfo, season, episode);
                    }
                }
            }

            return null;
        }
    }
}