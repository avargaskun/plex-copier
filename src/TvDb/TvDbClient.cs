using System.Net.Http.Headers;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PlexCopier.TvDb
{
    public class TvDbClient : ITvDbClient
    {
        private readonly string apikey;

        private string userkey;

        private string username;

        private string? token;

        private readonly HttpClient client;

        private Dictionary<int, SeriesInfo> cache;

        public TvDbClient(string apikey, string userkey, string username)
        {
            this.apikey = apikey;
            this.userkey = userkey;
            this.username = username;

            client = new HttpClient
            {
                BaseAddress = new Uri("https://api.thetvdb.com")
            };

            cache = new Dictionary<int, SeriesInfo>();
        }

        public async Task Login()
        {
            var request = new 
            {
                apikey,
                userkey,
                username
            };
            
            var response = await PostAsync("/login", request);
            var token = response?["token"]?.Value<string>();
            if (string.IsNullOrEmpty(token))
            {
                throw new FatalException("Missing or invalid token in login response");
            }

            this.token = token;
        }

        public async Task<SeriesInfo> GetSeriesInfo(int seriesId)
        {
            if (cache.ContainsKey(seriesId))
            {
                return cache[seriesId];
            }

            var response = await GetAsync($"series/{seriesId}");
            var seriesName = response?["data"]?["seriesName"]?.Value<string>();
            if (seriesName == null)
            {
                throw new FatalException("Missing or invalid series information");
            }

            var seasons = new List<SeasonInfo>();
            for (int page = 1;; ++page)
            {
                response = await GetAsync($"series/{seriesId}/episodes?page={page}");
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

        private Task<JToken> PostAsync(string requestUri, object content)
        {
            return SendAsync(HttpMethod.Post, requestUri, content);
        }

        private Task<JToken> GetAsync(string requestUri)
        {
            return SendAsync(HttpMethod.Get, requestUri);
        }

        private async Task<JToken> SendAsync(HttpMethod method, string requestUri, object? content = null)
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
                
                using (var response = await client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    if (response.Content == null || response.Content.Headers.ContentLength.GetValueOrDefault(0) == 0)
                    {
                        return new JObject();
                    }

                    return JToken.Parse(await response.Content.ReadAsStringAsync());
                }
            }
        }
    }
}