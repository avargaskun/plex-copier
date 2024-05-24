using System.Reflection;

using log4net;
using log4net.Appender;
using log4net.Config;
using log4net.Repository;

namespace PlexCopier
{
    public class LoggingScope : IDisposable
    {
        public const string DefaultConfigFile = "log4net.config";

        private ILoggerRepository repository;

        public LoggingScope(string? configFile)
        {
            repository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            if (configFile != null && File.Exists(configFile))
            {
                XmlConfigurator.Configure(repository, new FileInfo(configFile));                
            }
            else
            {
                BasicConfigurator.Configure(repository, new log4net.Appender.ConsoleAppender());
            }
        }

        public void Dispose()
        {
            var files = repository.GetAppenders().OfType<FileAppender>().Select(a => a.File).Where(f => f != null).ToList();

            repository.Shutdown();

            // For each appender of type FileAppender delete any generated empty files
            foreach (var file in files)
            {
                var info = new FileInfo(file);
                if (info.Exists && info.Length <= 0)
                {
                    info.Delete();
                }
            }
        }
    }
}