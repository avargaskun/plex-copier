namespace PlexCopier.TvDb
{
    public interface ITvDbClient
    {
        Task<SeriesInfo> GetSeriesInfo(int seriesId, CancellationToken token);
    }
}