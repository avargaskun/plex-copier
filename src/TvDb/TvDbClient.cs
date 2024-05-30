using System.Net.Http.Headers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PlexCopier.Settings;

namespace PlexCopier.TvDb
{
    public class TvDbClient : ITvDbClient
    {
        private const int RetryCount = 5;

        private static readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(50);

        private const int IntervalMultiplier = 2;

        private readonly string apikey;

        private readonly string userkey;

        private readonly string username;

        private string? token;

        private Dictionary<int, SeriesInfo> cache;

        public TvDbClient(Options options)
        {
            apikey = options.TvDb.ApiKey;
            userkey = options.TvDb.UserKey;
            username = options.TvDb.UserName;
            cache = new Dictionary<int, SeriesInfo>();
            Client = new HttpClient(new RetryHandler(RetryCount, RetryInterval, IntervalMultiplier, new LoggingHandler()))
            {
                BaseAddress = new Uri("https://api.thetvdb.com")
            };
        }

        protected internal HttpClient Client { get; set; }

        public async Task Login(CancellationToken ct)
        {
            var request = new 
            {
                apikey,
                userkey,
                username
            };
            
            var response = await PostAsync("/login", request, ct);
            var token = response?["token"]?.Value<string>();
            if (string.IsNullOrEmpty(token))
            {
                throw new FatalException("Missing or invalid token in login response");
            }

            this.token = token;
        }

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
                Seasons = seasons.ToArray()
            };

            cache[seriesId] = info;
            return info;
        }

        private Task<JToken> PostAsync(string requestUri, object content, CancellationToken token)
        {
            return SendAsync(HttpMethod.Post, requestUri, content, token);
        }

        private Task<JToken> GetAsync(string requestUri, CancellationToken token)
        {
            return SendAsync(HttpMethod.Get, requestUri, content: null, token);
        }

        private async Task<JToken> SendAsync(HttpMethod method, string requestUri, object? content, CancellationToken ct)
        {
            using (var request = new HttpRequestMessage(method, requestUri))
            {
                if (token != null)
                {
                    request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token}");
                }

                if (content != null)
                {
                    request.Content = new StringContent(JsonConvert.SerializeObject(content));
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                }
                
                using (var response = await Client.SendAsync(request, ct))
                {
                    response.EnsureSuccessStatusCode();
                    if (response.Content == null || response.Content.Headers.ContentLength.GetValueOrDefault(0) == 0)
                    {
                        return new JObject();
                    }

                    return JToken.Parse(await response.Content.ReadAsStringAsync(ct));
                }
            }
        }
    }
}