using System;
using System.IO;

using PlexCopier.Settings;

namespace tst
{
    public static class TestArguments
    {
        public static string DefaultTarget = Path.Combine(Environment.CurrentDirectory, "test-files");

        public static string DefaultOptions = Path.Combine(Environment.CurrentDirectory, Options.DefaultFilename);

        public static Arguments Default => new Arguments
        {
            Target = DefaultTarget,
            Recursive = true,
            Test = false,
            Options = DefaultOptions
        };
    }
}