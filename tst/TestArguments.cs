using PlexCopier.Settings;

namespace tst
{
    public static class TestArguments
    {
        public static string DefaultTarget = Path.Combine(Environment.CurrentDirectory, "test-files");

        public static string DefaultOptions = Path.Combine(Environment.CurrentDirectory, Options.DefaultFilename);

        public static Arguments Default => new()
        {
            Target = DefaultTarget,
            Recursive = true,
            Test = false,
            Options = DefaultOptions,
            IgnorePaths = [],
            ParallelOperations = 1,
            WriteThrough = true,
            FileBuffer = 4096,
        };
    }
}