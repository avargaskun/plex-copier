using CommandLine;
using PlexCopier.Utils;

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

        [Option('m', "move", Required = false, HelpText = "Move files instead of copying, i.e. delete originals after done")]
        public bool MoveFiles { get; set; }

        [Option('f', "force", Required = false, HelpText = "If target file exists, it will be deleted before copying new file over")]
        public bool Force{ get; set; }

        [Option('t', "target", Required = true, HelpText = "Path to target file or directory")]
        public required string Target { get; set; }

        [Option('n', "test", Required = false, HelpText = "Only prints out the actions that would have been executed")]
        public bool Test { get; set; }

        [Option('w', "watch", Required = false, Default = false, HelpText ="Watches for file changes in the specified folder")]
        public bool Watch { get; set; }

        [Option('d', "delay", Required = false, HelpText = "When watching a folder, specifies the time (in seconds) to wait after a file is observed, before beginning the copy operation", Default = (int)30)]
        public int DelayCopy { get; set; }

        [Option('v', "verify", Required = false, HelpText = "Verify the hash of the copied files against the source after copying")]
        public bool Verify { get; set; }

        [Option('p', "parallel", Required = false, HelpText = "Specifies how many copy operations may happen in parallel. Set to 0 for unlimited.", Default = (int)1)]
        public int ParallelOperations { get; set; }

        [Option('l', "lock", Required = false, HelpText = "When set, a lock will be acquired on the source files, to avoid any modifications while copying")]
        public bool LockFiles { get; set; }

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