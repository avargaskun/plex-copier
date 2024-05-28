using PlexCopier.TvDb;

namespace tst
{
    public class TestClient : ITvDbClient
    {
        public const int SingleSeriesId = 1;

        public const int DoubleSeriesId = 2;

        public const int LongSeriesId = 3;

        public const int SeriesWithSpecials = 4;

        private Dictionary<int, SeriesInfo> seriesInfos = new Dictionary<int, SeriesInfo>
        {
            {
                SingleSeriesId, 
                new SeriesInfo
                {
                    Name = "Single Series",
                    Seasons = [
                        new SeasonInfo { EpisodeCount = 0 },
                        new SeasonInfo { EpisodeCount = 3 }
                    ]
                }
            },
            {
                DoubleSeriesId, 
                new SeriesInfo
                {
                    Name = "Double Series",
                    Seasons = [
                        new SeasonInfo { EpisodeCount = 0 },
                        new SeasonInfo { EpisodeCount = 3 },
                        new SeasonInfo { EpisodeCount = 3 }
                    ]
                }
            },
            {
                LongSeriesId, 
                new SeriesInfo
                {
                    Name = "Long Series",
                    Seasons = [
                        new SeasonInfo { EpisodeCount = 0 },
                        new SeasonInfo { EpisodeCount = 3 },
                        new SeasonInfo { EpisodeCount = 3 }
                    ]
                }
            },
            {
                SeriesWithSpecials,
                new SeriesInfo
                {
                    Name = "Series With Specials",
                    Seasons = [
                        new SeasonInfo { EpisodeCount = 5 },
                        new SeasonInfo { EpisodeCount = 3 }
                    ]
                }
            }
        };

        public Dictionary<int, SeriesInfo> SeriesInfos => this.seriesInfos;

        public Task<SeriesInfo> GetSeriesInfo(int seriesId, CancellationToken token)
        {
            return Task.FromResult(SeriesInfos[seriesId]);
        }

        public Task Login(CancellationToken token)
        {
            return Task.FromResult(true);
        }
    }
}