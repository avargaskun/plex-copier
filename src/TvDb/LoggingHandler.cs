using System.Net.Http.Headers;
using System.Text;
using log4net.Util;

namespace PlexCopier.TvDb
{
    public class LoggingHandler : DelegatingHandler
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(LoggingHandler));

        public LoggingHandler(HttpMessageHandler? innerHandler = null)
            : base(innerHandler ?? new HttpClientHandler())
        {
        }

        // Used only to set an override in unit-tests!
        protected internal virtual Task AsyncDelay(int millis, CancellationToken ct) => Task.Delay(millis, ct);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Log.DebugExt(() => FormatRequest(request));
            try
            {
                var response = await base.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    Log.DebugExt(() => FormatResponse(response));
                }
                else
                {
                    Log.InfoExt(() => FormatResponse(response));
                }
                return response;
            }
            catch (Exception ex)
            {
                Log.Info("Request exception", ex);
                throw;
            }
        }

        private string FormatRequest(HttpRequestMessage request)
            => $"Sending request: {request.Method} {request.RequestUri}\n{FormatHeaders(request.Headers)}";

        private string FormatResponse(HttpResponseMessage response)
            => $"Server response: {response.StatusCode} {response.ReasonPhrase}\n{FormatHeaders(response.Headers)}";

        private string FormatHeaders(HttpHeaders headers)
        {
            var sb = new StringBuilder();
            foreach (var header in headers)
            {
                sb.AppendFormat("{0}: {1}\n", header.Key, string.Join(",", header.Value.ToArray()));
            }
            return sb.ToString();
        }
    }
}
