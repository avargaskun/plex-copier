using System.Runtime.InteropServices;
using PlexCopier.Settings;

namespace PlexCopier
{
    public class Watcher : IDisposable
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Watcher));

        private readonly Arguments arguments;

        private readonly AutoResetEvent stopped;

        private readonly ICopier copier;

        private readonly bool isLinux;

        private List<FileSystemWatcher> watchers;

        private HashSet<string> watchedFolders;

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

        public bool IsRunning { get; set; }
        
        public void Dispose()
        {
            Stop();
            stopped.Dispose();
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            Stop();

            SetupWatcherFor(arguments.Target);
            IsRunning = true;
            Started?.Invoke(this, EventArgs.Empty);

            stopped.WaitOne();
            IsRunning = false;
        }

        public void Stop()
        {
            if (IsRunning)
            {
                Log.Info("Stoping file watchers");
                lock (this)
                {
                    watchers.ForEach(w => w.Dispose());
                    watchers = [];
                    watchedFolders = [];
                }
                stopped.Set();
            }
            else
            {
                Log.Debug("The file logger is not running");
            }
        }
        
        // Used only to set an override in unit-tests!
        protected internal virtual Task AsyncDelay(int millis, CancellationToken ct)
        {
            Log.DebugFormat("Waiting {0} seconds before copying file", millis);
            return Task.Delay(millis, ct);
        }

        private async void OnCreatedOrChanged(object sender, FileSystemEventArgs args)
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
                    if (arguments.DelayCopy > 0)
                    {
                        await AsyncDelay(arguments.DelayCopy * 1000, CancellationToken.None);
                    }
                    await copier.CopyFiles(args.FullPath);
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
            var files = copier.FindTargetFiles(directoryPath);
            if (arguments.DelayCopy > 0)
            {
                await AsyncDelay(arguments.DelayCopy * 1000, CancellationToken.None);
            }

            foreach (var file in files)
            {
                await copier.CopyFiles(file);
            }
        }

        private void OnError(object sender, ErrorEventArgs args)
        {
            Log.Error("File watcher has stopped with an error", args.GetException());
            Stop();
        }

        private void SetupWatcherFor(string source)
        {
            lock(this)
            {
                var fullPath = Path.GetFullPath(source);
                if (!watchedFolders.Contains(fullPath)) {
                    var watcher = new FileSystemWatcher(fullPath)
                    {
                        IncludeSubdirectories = arguments.Recursive,
                        EnableRaisingEvents = true,
                    };
                    watcher.Created += OnCreatedOrChanged;
                    watcher.Error += OnError;
                    watchers.Add(watcher);
                    watchedFolders.Add(fullPath);
                    Log.Info($"Started file watcher for {fullPath}");
                }
            }
        }
    }
}