using System;
using System.IO;

using PlexCopier.Settings;

namespace tst
{
    public static class TestOptions
    {
        public static string DefaultCollection => Path.Combine(Environment.CurrentDirectory, "test-collection");

        public static Options AllSeries
        {
            get
            {
                return new Options
                {
                    Collection = DefaultCollection,
                    Series = new[] { SingleSeries, DoubleSeries, LongSeries }
                };
            }
        }

        public static Series SingleSeries => new Series
        {
            Id = TestClient.SingleSeriesId,
            Patterns = new[]
            {
                new Pattern
                {
                    Expression = ".*Single - ([0-9]{1,2}).*"
                }
            }
        };

        public static Series DoubleSeries => new Series
        {
            Id = TestClient.DoubleSeriesId,
            Patterns = new[]
            {
                new Pattern
                {
                    Expression = ".*Double S1 - ([0-9]{1,2}).*"
                },
                new Pattern
                {
                    Expression = ".*Double S2 - ([0-9]{1,2}).*",
                    SeasonStart = 2
                }
            }
        };

        public static Series LongSeries => new Series
        {
            Id = TestClient.LongSeriesId,
            Patterns = new[]
            {
                new Pattern
                {
                    Expression = ".*Long - ([0-9]{1,2}).*"
                }
            }
        };
    }
}