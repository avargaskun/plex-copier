using System.Runtime.InteropServices;
using PlexCopier.Settings;
using PlexCopier.Utils;

namespace PlexCopier
{
    public class Watcher : IDisposable
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Watcher));

        private readonly Arguments arguments;

        private readonly AutoResetEvent stopped;

        private readonly ICopier copier;

        private readonly bool isLinux;

        private readonly object watcherLock = new();

        private readonly object runLock = new();

        private readonly PathTraverser pathTraverser = new();

        private bool started = false;

        private List<FileSystemWatcher> watchers;

        private HashSet<string> watchedFolders;

        private CountdownEvent runningOps = new(1);

        private CancellationTokenSource runningCts = new();

        public Watcher(Arguments arguments, ICopier copier)
        {
            if (!Directory.Exists(arguments.Target))
            {
                throw new FatalException("The specified directory does not exist!");
            }

            this.arguments = arguments;
            this.copier = copier;
            isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            stopped = new AutoResetEvent(false);
            watchers = [];
            watchedFolders = [];
        }

        public event EventHandler<EventArgs>? Started;

        public bool IsRunning => started && !runningCts.IsCancellationRequested;
        
        public void Dispose()
        {
            Stop();
            stopped.Dispose();
            runningOps.Dispose();
            runningCts.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            lock (runLock)
            {
                if (IsRunning)
                {
                    throw new FatalException("This watcher is already running");
                }

                started = true;
            }

            SetupWatcherFor(arguments.Target);
            Started?.Invoke(this, EventArgs.Empty);

            Log.Debug("File watcher started, waiting for termination signal");
            stopped.WaitOne();
            if (!runningOps.IsSet)
            {
                Log.Info($"Waiting for pending copy operations before exiting");
                runningOps.Wait();
            }
        }

        public void Stop()
        {
            lock (runLock)
            {
                if (IsRunning)
                {
                    Log.Info("Stopping the file watchers");
                    StopWatchers();
                    stopped.Set();
                    runningCts.Cancel();
                    runningOps.Signal();
                }
            }
        }
        
        // Used only to set an override in unit-tests!
        protected internal virtual Task AsyncDelay(int millis, CancellationToken ct)
        {
            Log.DebugFormat("Waiting {0} seconds before copying file", millis);
            return Task.Delay(millis, ct);
        }

        private async void OnCreated(object sender, FileSystemEventArgs args)
        {
            try
            {
                if (arguments.Filter.IsIgnored(args.FullPath))
                {
                    Log.Info($"Ignoring event for path that is filtered explicitly: {args.FullPath}");
                }
                else if (Directory.Exists(args.FullPath) && arguments.Recursive && isLinux)
                {
                    SetupWatcherFor(args.FullPath);
                    await CopyExistingFiles(args.FullPath);
                }
                else if (File.Exists(args.FullPath))
                {
                    await PerformCopy([args.FullPath]);
                }
                else
                {
                    Log.Info($"Event was ignored for path: {args.FullPath}");
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Unhandled exception occurred on file watcher handler for: {args.FullPath}", ex);
                Stop();
            }
        }

        private async Task CopyExistingFiles(string directoryPath)
        {
            var files = pathTraverser.FindFilesInPath(directoryPath, arguments.Recursive);
            await PerformCopy(files);
        }

        private async Task PerformCopy(IEnumerable<string> sources)
        {
            if (arguments.DelayCopy > 0)
            {
                await AsyncDelay(arguments.DelayCopy * 1000, runningCts.Token);
            }

            if (IsRunning)
            {
                runningOps.AddCount();
                try
                {
                    var tasks = sources.Select(source => copier.CopyFiles(source, runningCts.Token));
                    await Task.WhenAll(tasks);
                }
                finally
                {
                    runningOps.Signal();
                }
            }
        }

        private void OnError(object sender, ErrorEventArgs args)
        {
            Log.Error("File watcher has stopped with an error", args.GetException());
            Stop();
        }

        private void SetupWatcherFor(string source)
        {
            lock(watcherLock)
            {
                var fullPath = Path.GetFullPath(source);
                if (!watchedFolders.Contains(fullPath)) {
                    var watcher = new FileSystemWatcher(fullPath)
                    {
                        IncludeSubdirectories = arguments.Recursive,
                        EnableRaisingEvents = true,
                    };
                    watcher.Created += OnCreated;
                    watcher.Error += OnError;
                    watchers.Add(watcher);
                    watchedFolders.Add(fullPath);
                    Log.Info($"Started file watcher for {fullPath}");
                }
            }
        }

        private void StopWatchers()
        {
            lock (watcherLock)
            {
                Log.Info("Stoping file watchers");
                watchers.ForEach(w => w.Dispose());
                watchers = [];
                watchedFolders = [];
            }
        }
    }
}