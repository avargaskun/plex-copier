using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PlexCopier.Settings
{
    public class Options : IDisposable
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Options));

        public const string DefaultFilename = "settings.yaml";

        private FileSystemWatcher? watcher;

        public required string Collection { get; set; }

        public required TvDb TvDb { get; set; }

        public required Series[] Series { get; set; }

        public static Options Load(string source)
        {
            Log.Info($"Loading configuration from: {source}");

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            using var reader = new StreamReader(source);
            return deserializer.Deserialize<Options>(reader);
        }

        public void Dispose()
        {
            StopWatching();
            GC.SuppressFinalize(this);
        }

        public void Reload(string source)
        {
            Load(source).CopyTo(this);
        }

        public void WatchForChanges(string source)
        {
            lock (this)
            {
                StopWatching();
                var parentFolder = Path.GetDirectoryName(Path.GetFullPath(source))
                    ?? throw new FatalException($"Could not determine parent folder for {source}");
                var filename = Path.GetFileName(source);
                watcher = new FileSystemWatcher(parentFolder)
                {
                    EnableRaisingEvents = true,
                    Filter = filename,
                };
                watcher.Created += OnCreatedOrChanged;
                watcher.Changed += OnCreatedOrChanged;
            }
        }

        public void StopWatching()
        {
            lock (this)
            {
                if (watcher != null)
                {
                    watcher.Dispose();
                    watcher = null;
                }
            }
        }

        private void OnCreatedOrChanged(object sender, FileSystemEventArgs args)
        {
            Log.Info("A change in configuration was detected");
            Reload(args.FullPath);
        }

        private void OnError(object sender, ErrorEventArgs args)
        {
            Log.Error("File watcher has stopped with an error", args.GetException());
            StopWatching();
        }
    }
}