namespace tst
{
    public static class WaitHelper
    {
        public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(5);
        
        public static readonly TimeSpan DefaultPollingInterval = TimeSpan.FromMilliseconds(100);

        public static async Task WaitUntil
        (
            Func<bool> condition, 
            TimeSpan? timeout = null, 
            TimeSpan? pollingInterval = null, 
            string message = "The condition was not met within the specified timeout."
        )
        {
            var timeoutTask = Task.Delay(timeout ?? DefaultTimeout);
            while (!condition())
            {
                if (await Task.WhenAny(Task.Delay(pollingInterval ?? DefaultPollingInterval), timeoutTask) == timeoutTask)
                {
                    throw new TimeoutException(message);
                }
            }
        }

        public static async Task TryUntil(Action condition, TimeSpan? timeout = null, TimeSpan? pollingInterval = null)
        {
            var timeoutTask = Task.Delay(timeout ?? DefaultTimeout);
            var completed = false;
            do
            {
                try
                {
                    condition();
                    completed = true;
                }
                catch
                {
                    if (await Task.WhenAny(Task.Delay(pollingInterval ?? DefaultPollingInterval), timeoutTask) == timeoutTask)
                    {
                        throw;
                    }
                }
            }
            while (!completed);
        }
    }
}