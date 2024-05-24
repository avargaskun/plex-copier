using PlexCopier.Settings;

namespace tst
{
    public static class TestOptions
    {
        public static string DefaultCollection => Path.Combine(Environment.CurrentDirectory, "test-collection");

        public static Options Default => new Options
        {
            Collection = DefaultCollection,
            Series = [SingleSeries, DoubleSeries, LongSeries],
            TvDb = new TvDb
            {
                ApiKey = string.Empty,
                UserKey = string.Empty,
                UserName = string.Empty,
            }
        };

        public static Series MovieSpecial => new Series
        {
            Id = TestClient.SeriesWithSpecials,
            Patterns =
            [
                new Pattern
                {
                    Expression = ".*Specials - Movie.*",
                    SeasonStart = 0,
                    EpisodeOffset = 4
                }
            ]
        };

        public static Series SingleSeries => new Series
        {
            Id = TestClient.SingleSeriesId,
            Patterns =
            [
                new Pattern
                {
                    Expression = ".*Single - ([0-9]{1,2}).*"
                }
            ]
        };

        public static Series DoubleSeries => new Series
        {
            Id = TestClient.DoubleSeriesId,
            Patterns =
            [
                new Pattern
                {
                    Expression = ".*Double S1 - ([0-9]{1,2}).*"
                },
                new Pattern
                {
                    Expression = ".*Double S2 - ([0-9]{1,2}).*",
                    SeasonStart = 2
                }
            ]
        };

        public static Series LongSeries => new Series
        {
            Id = TestClient.LongSeriesId,
            Patterns =
            [
                new Pattern
                {
                    Expression = ".*Long - ([0-9]{1,2}).*"
                }
            ]
        };
    }
}