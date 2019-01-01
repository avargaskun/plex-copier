using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using PlexCopier.TvDb;

namespace tst
{
    public class TestClient : ITvDbClient
    {
        public const int SingleSeriesId = 1;

        public const int DoubleSeriesId = 2;

        public const int LongSeriesId = 3;

        private Dictionary<int, SeriesInfo> seriesInfos = new Dictionary<int, SeriesInfo>
        {
            {
                SingleSeriesId, 
                new SeriesInfo
                {
                    Name = "Single Series",
                    Seasons = new[] {
                        new SeasonInfo { EpisodeCount = 0 },
                        new SeasonInfo { EpisodeCount = 3 }
                    }
                }
            },
            {
                DoubleSeriesId, 
                new SeriesInfo
                {
                    Name = "Double Series",
                    Seasons = new[] {
                        new SeasonInfo { EpisodeCount = 0 },
                        new SeasonInfo { EpisodeCount = 3 },
                        new SeasonInfo { EpisodeCount = 3 }
                    }
                }
            },
            {
                LongSeriesId, 
                new SeriesInfo
                {
                    Name = "Long Series",
                    Seasons = new[] {
                        new SeasonInfo { EpisodeCount = 0 },
                        new SeasonInfo { EpisodeCount = 3 },
                        new SeasonInfo { EpisodeCount = 3 }
                    }
                }
            },
        };

        public Dictionary<int, SeriesInfo> SeriesInfos => this.seriesInfos;

        public Task<SeriesInfo> GetSeriesInfo(int seriesId)
        {
            return Task.FromResult(SeriesInfos[seriesId]);
        }

        public Task Login()
        {
            return Task.FromResult(true);
        }
    }
}