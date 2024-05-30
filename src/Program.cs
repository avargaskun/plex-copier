using System.Runtime.CompilerServices;
using PlexCopier.Settings;
using PlexCopier.TvDb;

[assembly: InternalsVisibleTo("tst")]

namespace PlexCopier
{
    public class Program
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(Program));

        public static void Main(string[] args)
        {
            using (new LoggingScope(FindFile(LoggingScope.DefaultConfigFile)))
            {
                try
                {
                    Log.Debug($"New run with command line: {Environment.CommandLine}");
                    Log.Debug($"Current directory: {Environment.CurrentDirectory}");

                    var arguments = Arguments.Parse(args);
                    var optionsFile = arguments.Options 
                        ?? FindFile(Options.DefaultFilename) 
                        ?? throw new FatalException($"File {Options.DefaultFilename} could not be found.");
                    using var options = Options.Load(optionsFile);
                    
                    var copier = new Copier(arguments, options);
                    if (arguments.Watch)
                    {
                        options.WatchForChanges(optionsFile);
                        var watcher = new Watcher(arguments, copier);
                        Console.CancelKeyPress += (sender, args) =>
                        {
                            args.Cancel = true;
                            watcher.Stop();
                        };
                        watcher.Start();
                    }
                    else
                    {
                        copier.CopyFiles(CancellationToken.None).Wait();
                    }

                    Log.Debug("Program is exiting normally");
                }
                catch (FatalException fe)
                {
                    Log.Error(fe.Message);
                }
                catch (AggregateException ae)
                {
                    if (ae.InnerException is FatalException)
                    {
                        Log.Error(ae.InnerException.Message);
                    }
                    else
                    {
                        Log.Error(ae);
                    }
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        static string? FindFile(string file)
        {
            if (File.Exists(file))
            {
                return file;
            }
            else
            {
                file = Path.Combine(AppContext.BaseDirectory, file);
                if (File.Exists(file))
                {
                    return file;
                }
            }

            return null;
        }
    }
}
