using CommandLine;

namespace PlexCopier.Settings
{
    public record Arguments
    {
        [Option('i', "ignore", Required = false, HelpText = "One or more paths to ignore when watching for file changes")]
        public required IEnumerable<string> IgnorePaths { get; set; }

        [Option('o', "options", Required = false, HelpText = "Path to configuration file")]
        public string? Options { get; set; }

        [Option('r', "recursive", Required = false, HelpText = "If target is a directory, also look into subfolders")]
        public bool Recursive { get; set; }

        [Option('t', "target", Required = true, HelpText = "Path to target file or directory")]
        public required string Target { get; set; }

        [Option('n', "test", Required = false, HelpText = "Only prints out the actions that would have been executed")]
        public bool Test { get; set; }

        [Option('w', "watch", Required = false, Default = false, HelpText ="Watches for file changes in the specified folder")]
        public bool Watch { get; set; }

        public FileFilter Filter => new FileFilter(IgnorePaths ?? []);

        public static Arguments Parse(string[] args)
        {
            Arguments? result = null;
            _ = Parser.Default
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