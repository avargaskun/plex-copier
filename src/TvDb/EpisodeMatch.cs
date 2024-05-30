using PlexCopier.Settings;

namespace PlexCopier.TvDb
{
    public record EpisodeMatch(SeriesInfo Info, int Season, int Episode)
    {
        public SeriesInfo Info { get; set; } = Info;

        public int Season { get; set; } = Season;

        public int Episode { get; set; } = Episode;
    }
}