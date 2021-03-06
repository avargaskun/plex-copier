using System;
using System.Threading.Tasks;

namespace PlexCopier.TvDb
{
    public interface ITvDbClient
    {
        Task Login();

        Task<SeriesInfo> GetSeriesInfo(int seriesId);
    }
}