namespace PlexCopier.TvDb
{
    public interface ITvDbClient
    {
        Task Login(CancellationToken token);

        Task<SeriesInfo> GetSeriesInfo(int seriesId, CancellationToken token);
    }
}