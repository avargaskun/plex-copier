using System;

using CommandLine;

namespace PlexCopier.Settings
{
    public class Arguments
    {
        [Option('o', "options", Required = false, HelpText = "Path to configuration file")]
        public string Options { get; set; }

        [Option('r', "recursive", Required = false, HelpText = "If target is a directory, also look into subfolders")]
        public bool Recursive { get; set; }

        [Option('t', "target",  Required = true, HelpText = "Path to target file or directory")]
        public string Target { get; set; }

        [Option('n', "test", Required = false, HelpText = "Only prints out the actions that would have been executed")]
        public bool Test { get; set; }

        public static Arguments Parse(string[] args)
        {
            Arguments result = null;
            CommandLine.Parser.Default
                .ParseArguments<Arguments>(args)
                .WithParsed(parsed => result = parsed);
            
            if (result == null)
            {
                throw new FatalException("Failed to parse command line arguments");
            }
            
            return result;
        }
    }
}