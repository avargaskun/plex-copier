using System.Net;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using NSubstitute.Extensions;
using NUnit.Framework;
using PlexCopier.TvDb;

namespace tst.Unit
{
    public class TvDbClientTests
    {
        private static readonly string AuthToken = "auth_token";

        private static readonly string SeriesName = "Mahoromatic";

        private TestMessageHandler messageHandler;

        private Func<DateTime> timeProvider;

        private CancellationTokenSource cts;

        [SetUp]
        public void BeforeEachTest()
        {
            messageHandler = Substitute.ForPartsOf<TestMessageHandler>();
            timeProvider = Substitute.For<Func<DateTime>>();
            cts = new CancellationTokenSource();
        }

        [TearDown]
        public void AfterEachTest()
        {
            messageHandler.Dispose();
            cts.Dispose();
        }

        [Test]
        public void WhenFirstLoginCallFailsThenErrorPropagates()
        {
            var client = CreateClient();
            messageHandler.LoginRequest()
                .Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));

            Assert.ThrowsAsync<MissingTokenException>(() => client.GetSeriesInfo(123, cts.Token));
        }

        [Test]
        public async Task WhenTokenExpiresThenItIsRetrievedAgain()
        {
            var token = "auth1";
            var time = DateTime.UtcNow;
            var client = CreateClient();
            client.TimeProvider = Substitute.For<Func<DateTime>>();;
            client.TimeProvider().Returns(x => time);
            var seriesData = new object[] { new { airedSeason = 1} };
            messageHandler.LoginRequest().Returns(x => LoginResponse(token));
            messageHandler.GetSeriesRequest().Returns(x => SeriesResponse(SeriesName));
            messageHandler.GetEpisodesRequest().Returns(x => EpisodesResponse(seriesData, next: null));

            var series = await client.GetSeriesInfo(123, cts.Token);
            Assert.That(series, Is.Not.Null);

            _ = messageHandler.Received(1).GetSeriesRequest(token: "auth1");
            messageHandler.ClearReceivedCalls();

            time += TimeSpan.FromHours(TestOptions.Default.TvDb.TokenExpirationHours + 1);
            token = "auth2";
            
            series = await client.GetSeriesInfo(456, cts.Token);
            Assert.That(series, Is.Not.Null);

            _ = messageHandler.Received(1).LoginRequest();
            _ = messageHandler.Received(1).GetSeriesRequest(token: "auth2");
        }

        [Test]
        public async Task WhenTokenExpiresAndRefreshFailsThenOldValueIsReused()
        {
            var time = DateTime.UtcNow;
            var client = CreateClient();
            client.TimeProvider = Substitute.For<Func<DateTime>>();;
            client.TimeProvider().Returns(x => time);
            var seriesData = new object[] { new { airedSeason = 1} };
            messageHandler.LoginRequest().Returns(x => LoginResponse(AuthToken));
            messageHandler.GetSeriesRequest().Returns(x => SeriesResponse(SeriesName));
            messageHandler.GetEpisodesRequest().Returns(x => EpisodesResponse(seriesData, next: null));

            var series = await client.GetSeriesInfo(123, cts.Token);
            Assert.That(series, Is.Not.Null);

            _ = messageHandler.Received(1).LoginRequest();
            _ = messageHandler.Received(1).GetSeriesRequest(token: AuthToken);

            time += TimeSpan.FromHours(TestOptions.Default.TvDb.TokenExpirationHours + 1);
            messageHandler.Configure().LoginRequest().Returns(Task.FromResult(new HttpResponseMessage(HttpStatusCode.Unauthorized)));
            messageHandler.ClearReceivedCalls();
            
            series = await client.GetSeriesInfo(456, cts.Token);
            Assert.That(series, Is.Not.Null);

            _ = messageHandler.Received(1).LoginRequest();
            _ = messageHandler.Received(1).GetSeriesRequest(token: AuthToken);
        }

        [Test]
        public async Task AfterFirstLoginSucceedsThenSubsequentCallsUseCachedToken()
        {
            var client = CreateClient();
            var seriesData = new object[] { new { airedSeason = 1} };
            messageHandler.LoginRequest().Returns(LoginResponse(AuthToken));
            messageHandler.GetSeriesRequest(123).Returns(x => SeriesResponse("Series1"));
            messageHandler.GetSeriesRequest(456).Returns(x => SeriesResponse("Series2"));
            messageHandler.GetEpisodesRequest().Returns(x => EpisodesResponse(seriesData, next: null));

            var series = await client.GetSeriesInfo(123, cts.Token);
            Assert.That(series, Is.Not.Null);
            Assert.That(series.Name, Is.EqualTo("Series1"));

            series = await client.GetSeriesInfo(456, cts.Token);
            Assert.That(series, Is.Not.Null);
            Assert.That(series.Name, Is.EqualTo("Series2"));

            _ = messageHandler.Received(1).LoginRequest();
            _ = messageHandler.Received(2).GetSeriesRequest();
            _ = messageHandler.Received(2).GetEpisodesRequest();
        }

        [Test]
        public async Task WhenSameSeriesIsRequestedMultipleTimesThenCachedValueIsReturned()
        {
            var client = CreateClient();
            var seriesData = new object[] { new { airedSeason = 1} };
            messageHandler.LoginRequest().Returns(LoginResponse(AuthToken));
            messageHandler.GetSeriesRequest().Returns(x => SeriesResponse(SeriesName));
            messageHandler.GetEpisodesRequest().Returns(x => EpisodesResponse(seriesData, next: null));

            var series = await client.GetSeriesInfo(123, cts.Token);
            Assert.That(series, Is.Not.Null);

            series = await client.GetSeriesInfo(123, cts.Token);
            Assert.That(series, Is.Not.Null);

            _ = messageHandler.Received(1).LoginRequest();
            _ = messageHandler.Received(1).GetSeriesRequest();
            _ = messageHandler.Received(1).GetEpisodesRequest();
        }

        [Test]
        public async Task WhenSeriesIncludesEmptySeasonsInBetweenThenTotalSeasonCountIsPadded()
        {
            var client = CreateClient();
            messageHandler.LoginRequest().Returns(LoginResponse(AuthToken));
            messageHandler.GetSeriesRequest().Returns(x => SeriesResponse(SeriesName));
            var seriesData = new object[]
            {
                // season 0 is implicit even if it has no episodes
                new { airedSeason = 1 },
                new { airedSeason = 1 },
                // season 2 is implicit even if it has no episodes
                new { airedSeason = 3 },
            };

            messageHandler.GetEpisodesRequest().Returns(EpisodesResponse(seriesData, next: null));

            var series = await client.GetSeriesInfo(123, cts.Token);
           
            Assert.That(series.Name, Is.EqualTo(SeriesName));
            Assert.That(series.Seasons, Has.Length.EqualTo(4));
            Assert.That(series.Seasons[0].EpisodeCount, Is.EqualTo(0));
            Assert.That(series.Seasons[1].EpisodeCount, Is.EqualTo(2));
            Assert.That(series.Seasons[2].EpisodeCount, Is.EqualTo(0));
            Assert.That(series.Seasons[3].EpisodeCount, Is.EqualTo(1));
        }

        [Test]
        public async Task WhenEpisodesSpanMultiplePagesThenClientIteratesOver()
        {
            var client = CreateClient();
            messageHandler.LoginRequest().Returns(LoginResponse(AuthToken));
            messageHandler.GetSeriesRequest().Returns(x => SeriesResponse(SeriesName));
            var page1 = new object[]
            {
                // season 0 is implicit even if it has no episodes
                new { airedSeason = 1 },
                new { airedSeason = 1 },
            };
            var page2 = new object[]
            {
                new { airedSeason = 1 },
            };

            messageHandler.GetEpisodesRequest(page: 1).Returns(EpisodesResponse(page1, next: 2));
            messageHandler.GetEpisodesRequest(page: 2).Returns(EpisodesResponse(page2, next: null));

            var series = await client.GetSeriesInfo(123, cts.Token);
           
            Assert.That(series.Name, Is.EqualTo(SeriesName));
            Assert.That(series.Seasons, Has.Length.EqualTo(2));
            Assert.That(series.Seasons[0].EpisodeCount, Is.EqualTo(0));
            Assert.That(series.Seasons[1].EpisodeCount, Is.EqualTo(3));

            _ = messageHandler.Received(2).GetEpisodesRequest();
        }

        private TvDbClient CreateClient()
        {
            return new TvDbClient(TestOptions.Default)
            {
                TimeProvider = timeProvider,
                Client = new HttpClient(messageHandler)
                {
                    BaseAddress = new Uri("https://test"),
                }
            };
        }

        private static Task<HttpResponseMessage> LoginResponse(string token)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var data = new { token };
            var content = new StringContent(JsonConvert.SerializeObject(data));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            response.Content = content;
            return Task.FromResult(response);
        }

        private static Task<HttpResponseMessage> SeriesResponse(string seriesName)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var data = new { seriesName };
            var content = new StringContent(JsonConvert.SerializeObject(new { data }));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            response.Content = content;
            return Task.FromResult(response);
        }

        private static Task<HttpResponseMessage> EpisodesResponse(object data, int? next)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            var links = new { next };
            var content = new StringContent(JsonConvert.SerializeObject(new { data, links }));
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            response.Content = content;
            return Task.FromResult(response);
        }
    }

    public class TestMessageHandler : HttpMessageHandler
    {
        public virtual Task<HttpResponseMessage> SubSendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return SubSendAsync(request, cancellationToken);
        }
    }

    public static partial class MessageHandlerExtensions
    {
        public static Task<HttpResponseMessage>? LoginRequest(this TestMessageHandler handler)
        {
            return handler.SubSendAsync(
                Arg.Is<HttpRequestMessage>(x => x.IsLoginRequest()), 
                Arg.Any<CancellationToken>()
            );
        }

        public static Task<HttpResponseMessage>? GetSeriesRequest(this TestMessageHandler handler, int? seriesId = null, string? token = null)
        {
            return handler.SubSendAsync(
                Arg.Is<HttpRequestMessage>(x => x.IsGetSeriesRequest(seriesId, token)), 
                Arg.Any<CancellationToken>()
            );
        }
        public static Task<HttpResponseMessage>? GetEpisodesRequest(this TestMessageHandler handler, int? seriesId = null, int? page = null, string? token = null)
        {
            return handler.SubSendAsync(
                Arg.Is<HttpRequestMessage>(x => x.IsGetEpisodesRequest(seriesId, page, token)), 
                Arg.Any<CancellationToken>()
            );
        }

        [GeneratedRegex("/series/([0-9]+)$")]
        private static partial Regex SeriesRegex();

        [GeneratedRegex("/series/([0-9]+)/episodes\\?page=([0-9]+)$")]
        private static partial Regex EpisodesRegex();

        private static bool CheckAuthToken(this HttpRequestMessage message, string? token)
            => string.IsNullOrEmpty(token) || message.Headers.Authorization?.Parameter == token;

        private static bool IsLoginRequest(this HttpRequestMessage message) 
            => message?.Method == HttpMethod.Post
            && message.RequestUri!.PathAndQuery.EndsWith("/login");

        private static bool IsGetSeriesRequest(this HttpRequestMessage message, int? seriesId, string? token)
        {
            if (message?.Method != HttpMethod.Get)
            {
                return false;
            }

            if (!message.CheckAuthToken(token))
            {
                return false;
            }

            var match = SeriesRegex().Match(message.RequestUri!.PathAndQuery);
            if (!match.Success)
            {
                return false;
            }

            if (seriesId != null && match.Groups[1].Value != seriesId.ToString())
            {
                return false;
            }

            return true;
        }

        private static bool IsGetEpisodesRequest(this HttpRequestMessage message, int? seriesId, int? page, string? token) 
        {
            if (message?.Method != HttpMethod.Get)
            {
                return false;
            }

            if (!message.CheckAuthToken(token))
            {
                return false;
            }
            
            var match = EpisodesRegex().Match(message.RequestUri!.PathAndQuery);
            if (!match.Success)
            {
                return false;
            }

            if (seriesId != null && match.Groups[1].Value != seriesId.ToString())
            {
                return false;
            }

            if (page != null && match.Groups[2].Value != page.ToString())
            {
                return false;
            }

            return true;
        }
    }
}