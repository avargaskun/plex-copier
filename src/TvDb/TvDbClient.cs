using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PlexCopier.TvDb
{
    public class TvDbClient : ITvDbClient
    {
        private string apikey;

        private string userkey;

        private string username;

        private string token;

        private HttpClient client;

        private Dictionary<int, SeriesInfo> cache;

        public TvDbClient(string apikey, string userkey, string username)
        {
            this.apikey = apikey;
            this.userkey = userkey;
            this.username = username;

            this.client = new HttpClient();
            this.client.BaseAddress = new Uri("https://api.thetvdb.com");

            this.cache = new Dictionary<int, SeriesInfo>();
        }

        public async Task Login()
        {
            var request = new 
            {
                apikey = this.apikey,
                userkey = this.userkey,
                username = this.username
            };
            
            var response = await this.PostAsync("/login", request);
            var token = response?["token"];
            if (token == null || token.Type != JTokenType.String)
            {
                throw new FatalException("Missing or invalid token in login response");
            }

            this.token = token.Value<string>();
        }

        public async Task<SeriesInfo> GetSeriesInfo(int seriesId)
        {
            if (this.cache.ContainsKey(seriesId))
            {
                return this.cache[seriesId];
            }

            var response = await this.GetAsync($"series/{seriesId}");
            var seriesName = response?["data"]?["seriesName"];
            if (seriesName == null || seriesName.Type != JTokenType.String)
            {
                throw new FatalException("Missing or invalid series information");
            }

            response = null;
            var seasons = new List<SeasonInfo>();
            for (int page = 1;; ++page)
            {
                response = await this.GetAsync($"series/{seriesId}/episodes?page={page}");
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
                Name = seriesName.Value<string>(),
                Seasons = seasons.ToArray()
            };

            this.cache[seriesId] = info;
            return info;
        }

        private Task<JToken> PostAsync(string requestUri, object content)
        {
            return this.SendAsync(HttpMethod.Post, requestUri, content);
        }

        private Task<JToken> GetAsync(string requestUri)
        {
            return this.SendAsync(HttpMethod.Get, requestUri);
        }

        private async Task<JToken> SendAsync(HttpMethod method, string requestUri, object content = null)
        {
            using (var request = new HttpRequestMessage(method, requestUri))
            {
                if (this.token != null)
                {
                    request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {this.token}");
                }

                if (content != null)
                {
                    request.Content = new StringContent(JsonConvert.SerializeObject(content));
                    request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                }
                
                using (var response = await this.client.SendAsync(request))
                {
                    response.EnsureSuccessStatusCode();
                    if (response.Content == null || response.Content.Headers.ContentLength.GetValueOrDefault(0) == 0)
                    {
                        return null;
                    }

                    return JToken.Parse(await response.Content.ReadAsStringAsync());
                }
            }
        }
    }
}