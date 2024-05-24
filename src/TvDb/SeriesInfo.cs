namespace PlexCopier.TvDb
{
    public class SeriesInfo
    {
        public required SeasonInfo[] Seasons { get; set; }

        public required string Name { get; set; }
    }
}