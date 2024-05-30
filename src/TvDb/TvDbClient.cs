using System.Net.Http.Headers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PlexCopier.Settings;

namespace PlexCopier.TvDb
{
    public class TvDbClient : ITvDbClient
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(TvDbClient));

        private const int RetryCount = 5;

        private static readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(50);

        private const int IntervalMultiplier = 2;

        private readonly Options options;

        private readonly SemaphoreSlim tokenLock;

        private string? token;

        private DateTime tokenExpiresAt;

        private Dictionary<int, SeriesInfo> cache;

        public TvDbClient(Options options)
        {
            this.options = options;
            tokenLock = new SemaphoreSlim(1);
            cache = new Dictionary<int, SeriesInfo>();
            Client = new HttpClient(new RetryHandler(RetryCount, RetryInterval, IntervalMultiplier, new LoggingHandler()))
            {
                BaseAddress = new Uri("https://api.thetvdb.com")
            };
        }

        protected internal HttpClient Client { get; set; }

        protected internal Func<DateTime> TimeProvider { get; set; } = () =>DateTime.UtcNow;

        public async Task<SeriesInfo> GetSeriesInfo(int seriesId, CancellationToken token)
        {
            if (cache.ContainsKey(seriesId))
            {
                return cache[seriesId];
            }

            var response = await GetAsync($"series/{seriesId}", token);
            var seriesName = response?["data"]?["seriesName"]?.Value<string>();
            if (seriesName == null)
            {
                throw new FatalException("Missing or invalid series information");
            }

            var seasons = new List<SeasonInfo>();
            for (int page = 1;; ++page)
            {
                response = await GetAsync($"series/{seriesId}/episodes?page={page}", token);
                var episodes = response?["data"];
                if (episodes == null || episodes.Type != JTokenType.Array)
                {
                    throw new FatalException("Missing or invalid episodes information");
                }

                foreach (var episode in episodes)
                {
                    var season = episode["airedSeason"];
                    if (season == null || season.Type != JTokenType.Integer)
                    {
                        throw new FatalException("Missing or invalid season number");
                    }

                    var seasonInt = season.Value<int>();
                    while (seasonInt >= seasons.Count)
                    {
                        seasons.Add(new SeasonInfo());
                    }

                    seasons[seasonInt].EpisodeCount++;
                }

                var next = response?["links"]?["next"];
                if (next == null || next.Type == JTokenType.Null)
                {
                    break;
                }
            }

            var info = new SeriesInfo
            {
                Name = seriesName,
                Seasons = [.. seasons]
            };

            cache[seriesId] = info;
            return info;
        }

        private async Task<string> Login(CancellationToken ct)
        {
            var request = new 
            {
                apikey = options.TvDb.ApiKey,
                userkey = options.TvDb.UserKey,
                username = options.TvDb.UserName
            };
            
            var response = await PostAsync("/login", request, ct, noAuth: true);
            var token = response?["token"]?.Value<string>();
            if (string.IsNullOrEmpty(token))
            {
                throw new FatalException("Missing or invalid token in login response");
            }

            return token;
        }

        private Task<JToken> PostAsync(string requestUri, object content, CancellationToken ct, bool noAuth = false)
        {
            return SendAsync(HttpMethod.Post, requestUri, content, ct, noAuth);
        }

        private Task<JToken> GetAsync(string requestUri, CancellationToken ct)
        {
            return SendAsync(HttpMethod.Get, requestUri, content: null, ct);
        }

        private async Task<string?> TryGetToken(CancellationToken ct)
        {
            if (token != null && tokenExpiresAt > TimeProvider())
            {
                return token;
            }

            await tokenLock.WaitAsync(ct);

            try
            {
                if (token == null || tokenExpiresAt <= TimeProvider())
                {
                    Log.Debug("Missing or expired TVDB token. Re-authenticating now.");
                    token = await Login(ct);
                    tokenExpiresAt = TimeProvider() + TimeSpan.FromHours(options.TvDb.TokenExpirationHours);
                }  
            }
            catch (Exception ex)
            {
                Log.Error("Failed to authenticate with TVDB", ex);
                tokenExpiresAt = TimeProvider() + TimeSpan.FromHours(1);
            }
            finally
            {
                tokenLock.Release();
            }

            return token;
        }

        private async Task<JToken> SendAsync(HttpMethod method, string requestUri, object? content, CancellationToken ct, bool noAuth = false)
        {
            using var request = new HttpRequestMessage(method, requestUri);
            if (!noAuth)
            {
                var token = await TryGetToken(ct);
                if (string.IsNullOrEmpty(token))
                {
                    throw new MissingTokenException();
                }
                request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
            }

            if (content != null)
            {
                request.Content = new StringContent(JsonConvert.SerializeObject(content));
                request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            }

            using var response = await Client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
            if (response.Content == null || response.Content.Headers.ContentLength.GetValueOrDefault(0) == 0)
            {
                return new JObject();
            }

            return JToken.Parse(await response.Content.ReadAsStringAsync(ct));
        }
    }
}