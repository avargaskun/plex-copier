using System.Net.Sockets;
using log4net.Repository.Hierarchy;

namespace PlexCopier.TvDb
{
    public class RetryHandler : DelegatingHandler
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(RetryHandler));

        private readonly int retryCount;

        private readonly TimeSpan retryInterval;

        private readonly int intervalMultiplier;

        public RetryHandler
        (
            int retryCount, 
            TimeSpan retryInterval, 
            int intervalMultiplier, 
            HttpMessageHandler? innerHandler = null
        ) : base(innerHandler ?? new HttpClientHandler())
        {
            if (retryCount < 0)
            {
                throw new ArgumentException("Value must be 0 or higher", "retryCount");
            }
            if (retryInterval < TimeSpan.Zero)
            {
                throw new ArgumentException("Value must be 0 or higher", "retryInterval");
            }
            if (intervalMultiplier < 1)
            {
                throw new ArgumentException("Value must be 1 or higher", "intervalMultiplier");
            }

            this.retryCount = retryCount;
            this.retryInterval = retryInterval;
            this.intervalMultiplier = intervalMultiplier;
        }

        // Used only to set an override in unit-tests!
        protected internal virtual Task AsyncDelay(int millis, CancellationToken ct)
        {
            Log.InfoFormat("Retrying request in {0} ms", millis);
            return Task.Delay(millis, ct);
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var attempts = 0;
            while (true)
            {
                var interval = retryInterval.Milliseconds * (int)Math.Pow(intervalMultiplier, attempts++);
                try
                {
                    var response = await base.SendAsync(request, cancellationToken);
    
                    if (IsRequestError(response) && attempts <= retryCount)
                    {
                        await AsyncDelay(interval, cancellationToken);
                        continue;
                    }
    
                    return response;
                }
                catch (Exception ex) when(IsNetworkError(ex))
                {
                    if (attempts > retryCount)
                    {
                        throw;
                    }

                    await AsyncDelay(interval, cancellationToken);
                }
            }
        }

        private static bool IsRequestError(HttpResponseMessage response)
        {
            return (int)response.StatusCode >= 500;
        }
    
        private static bool IsNetworkError(Exception ex)
        {
            // Check if it's a network error
            if (ex is HttpRequestException)
                return true;
            if (ex is SocketException)
                return true;
            if (ex.InnerException != null)
                return IsNetworkError(ex.InnerException);
            return false;
        }
    }
}
