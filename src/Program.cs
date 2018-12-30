using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using PlexCopier.Settings;
using PlexCopier.TvDb;

[assembly: log4net.Config.XmlConfigurator(ConfigFile = "log4net.config")]

namespace PlexCopier
{
    public class Program
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void Main(string[] args)
        {
            try
            {
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
                    options = LoadOptions(arguments.Target);
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

        static Options LoadOptions(string target)
        {
            var parent = Directory.GetParent(target);
            while (parent != null)
            {
                string optionsFile = Path.Combine(parent.FullName, Options.DefaultFilename);
                if (File.Exists(optionsFile))
                {
                    Log.Info($"Loading options from: {optionsFile}");
                    return Options.Load(optionsFile);
                }
                else
                {
                    parent = parent.Parent;
                }
            }

            string defaultFile = Path.Combine(AppContext.BaseDirectory, Options.DefaultFilename);
            if (File.Exists(defaultFile))
            {
                Log.Info($"Loading options from: {defaultFile}");
                return Options.Load(defaultFile);
            }

            throw new FatalException($"File {Options.DefaultFilename} could not be found for {target}");
        }
    }
}
