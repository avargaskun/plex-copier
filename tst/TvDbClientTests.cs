using NUnit.Framework;
using PlexCopier.TvDb;

namespace tst
{
    [TestFixture]
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

        [Test]
        public async Task RetrieveSeriesWithMultipleSeasons()
        {
            var client = new TvDbClient(apiKey, userKey, userName);
            await client.Login(CancellationToken.None);

            var series = await client.GetSeriesInfo(79035, CancellationToken.None);

            Assert.That(series.Name, Is.EqualTo("Mahoromatic: Automatic Maiden"));
            Assert.That(series.Seasons.Length, Is.EqualTo(3));
            Assert.That(series.Seasons[0].EpisodeCount, Is.EqualTo(6));
            Assert.That(series.Seasons[1].EpisodeCount, Is.EqualTo(12));
            Assert.That(series.Seasons[2].EpisodeCount, Is.EqualTo(14));
        }
    }
}