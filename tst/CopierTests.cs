using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using PlexCopier;
using Xunit;

namespace tst
{
    public class CopierTests : IDisposable
    {
        public CopierTests()
        {
            Cleanup();
            Directory.CreateDirectory(TestArguments.DefaultTarget);
            Directory.CreateDirectory(TestOptions.DefaultCollection);
        }

        public void Dispose()
        {
            Cleanup();
        }

        [Fact]
        public void TestAllFiles()
        {
            var arguments = TestArguments.Default;
            var options = TestOptions.AllSeries;
            var client = new TestClient();

            TestFiles.CreateFiles(TestArguments.DefaultTarget, TestFiles.SingleSeries, TestFiles.DoubleSeries, TestFiles.LongSeries);

            var copier = new Copier(arguments, client, options);
            copier.CopyFiles().Wait();

            ValidateFiles(options.Collection, OutputFiles.SingleSeries, OutputFiles.DoubleSeries, OutputFiles.LongSeries);
        }

        private static void ValidateFiles(string root, params string[][] multipleTargets)
        {
            Assert.True(Directory.Exists(root), $"Root directory does not exist: {root}");

            var allFiles = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories);
            Console.WriteLine($"Existing files under {root}:");
            foreach (var file in allFiles.Select(f => f.Substring(root.Length)))
            {
                Console.WriteLine($"\t{file}");
            }

            var allTargets = new List<string>();
            foreach (var targets in multipleTargets)
            {
                foreach (var target in targets)
                {
                    var file = Path.Combine(root, target);
                    allTargets.Add(file);
                    Assert.True(File.Exists(file), $"Could not find file {file}");
                }
            }

            foreach (var file in allFiles)
            {
                Assert.Contains(file, allTargets);
            }
        }

        private static void Cleanup()
        {
            if (Directory.Exists(TestArguments.DefaultTarget))
            {
                Directory.Delete(TestArguments.DefaultTarget, true);
            }

            if (Directory.Exists(TestOptions.DefaultCollection))
            {
                Directory.Delete(TestOptions.DefaultCollection, true);
            }
        }

        private static class OutputFiles
        {
            public static readonly string[] SingleSeries = new[]
            {
                Path.Combine("Single Series", "Season 01", "Single Series - s01e01.mkv"),
                Path.Combine("Single Series", "Season 01", "Single Series - s01e02.mkv"),
                Path.Combine("Single Series", "Season 01", "Single Series - s01e03.mkv")
            };
            public static readonly string[] DoubleSeries = new[]
            {
                Path.Combine("Double Series", "Season 01", "Double Series - s01e01.mkv"),
                Path.Combine("Double Series", "Season 01", "Double Series - s01e02.mkv"),
                Path.Combine("Double Series", "Season 01", "Double Series - s01e03.mkv"),
                Path.Combine("Double Series", "Season 02", "Double Series - s02e01.mkv"),
                Path.Combine("Double Series", "Season 02", "Double Series - s02e02.mkv"),
                Path.Combine("Double Series", "Season 02", "Double Series - s02e03.mkv")
            };
            public static readonly string[] LongSeries = new[]
            {
                Path.Combine("Long Series", "Season 01", "Long Series - s01e01.mp4"),
                Path.Combine("Long Series", "Season 01", "Long Series - s01e02.mp4"),
                Path.Combine("Long Series", "Season 01", "Long Series - s01e03.mp4"),
                Path.Combine("Long Series", "Season 02", "Long Series - s02e01.mp4"),
                Path.Combine("Long Series", "Season 02", "Long Series - s02e02.mp4"),
                Path.Combine("Long Series", "Season 02", "Long Series - s02e03.mp4")
            };
        }
    }
}
