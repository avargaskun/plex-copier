using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using log4net.Config;
using PlexCopier.Settings;
using PlexCopier.TvDb;

namespace PlexCopier
{
    public class Program
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private const string LogConfigFile = "log4net.config";

        public static void Main(string[] args)
        {
            ConfigureLog();

            try
            {
                Log.Debug($"New run with command line: {Environment.CommandLine}");
                Log.Debug($"Current directory: {Environment.CurrentDirectory}");

                var arguments = Arguments.Parse(args);
                if (arguments == null)
                {
                    return;
                }

                Options options;
                if (!string.IsNullOrEmpty(arguments.Options))
                {
                    options = Options.Load(arguments.Options);
                }
                else
                {
                    options = LoadOptions();
                }

                var client = new TvDbClient(options.TvDb.ApiKey, options.TvDb.UserKey, options.TvDb.UserName);
                client.Login().Wait();

                var copier = new Copier(arguments, client, options);
                copier.CopyFiles().Wait();
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

        static Options LoadOptions()
        {
            var file = FindFile(Options.DefaultFilename);
            if (file == null)
            {
                throw new FatalException($"File {Options.DefaultFilename} could not be found.");
            }

            return Options.Load(file);
        }

        static void ConfigureLog()
        {
            var logRepository = log4net.LogManager.GetRepository(Assembly.GetEntryAssembly());
            var config = FindFile(LogConfigFile);
            if (config != null)
            {
                XmlConfigurator.Configure(logRepository, new FileInfo(config));                
            }
            else
            {
                BasicConfigurator.Configure(logRepository, new log4net.Appender.ConsoleAppender());
            }
        }

        static string FindFile(string file)
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
