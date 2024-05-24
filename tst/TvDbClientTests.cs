using PlexCopier.TvDb;
using Xunit;

namespace tst
{
    public class TvDbClientTests
    {
        private readonly string apiKey = 
            Environment.GetEnvironmentVariable("TVDB_API_KEY") 
            ?? throw new Exception("Missing value for environment variable TVDB_API_KEY");

        private readonly string userKey =
            Environment.GetEnvironmentVariable("TVDB_USER_KEY") 
            ?? throw new Exception("Missing value for environment variable TVDB_USER_KEY");

        private readonly string userName =
            Environment.GetEnvironmentVariable("TVDB_USER_NAME") 
            ?? throw new Exception("Missing value for environment variable TVDB_USER_NAME");

        [Fact]
        public async Task RetrieveSeriesWithMultipleSeasons()
        {
            var client = new TvDbClient(apiKey, userKey, userName);
            await client.Login();

            var series = await client.GetSeriesInfo(79035);

            Assert.Equal("Mahoromatic: Automatic Maiden", series.Name);
            Assert.Equal(3, series.Seasons.Length);
            Assert.Equal(6, series.Seasons[0].EpisodeCount);
            Assert.Equal(12, series.Seasons[1].EpisodeCount);
            Assert.Equal(14, series.Seasons[2].EpisodeCount);
        }
    }
}