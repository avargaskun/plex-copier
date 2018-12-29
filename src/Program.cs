using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using PlexCopier.Settings;
using PlexCopier.TvDb;

namespace PlexCopier
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var arguments = Arguments.Parse(args);

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

        static Options LoadOptions(string target)
        {
            var parent = Directory.GetParent(target);
            while (parent != null)
            {
                string optionsFile = Path.Combine(parent.FullName, Options.DefaultFilename);
                if (File.Exists(optionsFile))
                {
                    return Options.Load(optionsFile);
                }
                else
                {
                    parent = parent.Parent;
                }
            }

            throw new Exception($"File {Options.DefaultFilename} could not be found for {target}");
        }
    }
}
