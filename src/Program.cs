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
                    var options = LoadOptions(arguments.Options);
                    
                    var client = new TvDbClient(options.TvDb.ApiKey, options.TvDb.UserKey, options.TvDb.UserName);
                    client.Login().Wait();

                    var copier = new Copier(arguments, client, options);

                    if (arguments.Watch)
                    {
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
                        copier.CopyFiles().Wait();
                    }
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

        static Options LoadOptions(string? file)
        {
            file = file ?? FindFile(Options.DefaultFilename);
            if (file == null)
            {
                throw new FatalException($"File {Options.DefaultFilename} could not be found.");
            }

            return Options.Load(file);
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
