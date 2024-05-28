using PlexCopier.Settings;

namespace PlexCopier.TvDb
{
    public class EpisodeMatch
    {
        public Series Series { get; set; }

        public Pattern Pattern { get; set; }

        public SeriesInfo Info { get; set; }

        public int Season { get; set; }

        public int Episode { get; set; }

        public EpisodeMatch(Series series, Pattern pattern, SeriesInfo info, int season, int episode)
        {
            Series = series;
            Pattern = pattern;
            Info = info;
            Season = season;
            Episode = episode;
        }
    }
}