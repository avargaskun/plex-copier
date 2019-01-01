using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using PlexCopier;
using PlexCopier.TvDb;
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
            var options = TestOptions.Default;
            var client = new TestClient();

            TestFiles.CreateFiles(TestArguments.DefaultTarget, TestFiles.SingleSeries, TestFiles.DoubleSeries, TestFiles.LongSeries);

            var copier = new Copier(arguments, client, options);
            var matches = copier.CopyFiles().Result;

            Assert.Equal(TestFiles.SingleSeries.Length + TestFiles.DoubleSeries.Length + TestFiles.LongSeries.Length, matches);

            ValidateFiles(options.Collection, OutputFiles.SingleSeries, OutputFiles.DoubleSeries, OutputFiles.LongSeries);
        }

        [Fact]
        public void TestSingleFileDoesNotMatch()
        {
            var arguments = TestArguments.Default;
            arguments.Target = Path.Combine(TestArguments.DefaultTarget, TestFiles.SingleSeries[0]);

            var options = TestOptions.Default;
            options.Series = new[] { TestOptions.LongSeries };

            var client = new TestClient();

            TestFiles.CreateFiles(TestArguments.DefaultTarget, TestFiles.SingleSeries);

            var copier = new Copier(arguments, client, options);
            var matches = copier.CopyFiles().Result;

            Assert.Equal(0, matches);

            ValidateFiles(options.Collection);
        }

        [Theory]
        // Baseline test without special characters
        [InlineData("Single Series")]
        // Test each individual character separately
        [InlineData("Single < Series")]
        [InlineData("Single > Series")]
        [InlineData("Single : Series")]
        [InlineData("Single \" Series")]
        [InlineData("Single ' Series")]
        [InlineData("Single / Series")]
        [InlineData("Single \\ Series")]
        [InlineData("Single | Series")]
        [InlineData("Single ? Series")]
        [InlineData("Single * Series")]
        // Test multiple invalid characters
        [InlineData("Single <>:\"'/\\|?* Series")]
        // Test with different character placement regarding spaces
        [InlineData("Single! Series")]
        [InlineData("Single !Series")]
        [InlineData("Single ! Series")]
        [InlineData("Single ! ! Series")]
        public void TestInvalidPathCharactersAreRemoved(string seriesName)
        {
            var arguments = TestArguments.Default;
            arguments.Target = Path.Combine(TestArguments.DefaultTarget, TestFiles.SingleSeries[0]);

            var options = TestOptions.Default;
            options.Series = new[] { TestOptions.SingleSeries };

            var client = new TestClient();
            client.SeriesInfos[TestClient.SingleSeriesId].Name = seriesName;

            TestFiles.CreateFiles(TestArguments.DefaultTarget, TestFiles.SingleSeries);

            var copier = new Copier(arguments, client, options);
            var matches = copier.CopyFiles().Result;

            Assert.Equal(1, matches);

            var outputFile = Path.Combine(TestOptions.DefaultCollection, OutputFiles.SingleSeries[0]);
            Assert.True(File.Exists(outputFile));
        }

        [Theory]
        [InlineData(null, null, false)]
        [InlineData(null, false, false)]
        [InlineData(false, null, false)]
        [InlineData(false, false, false)]
        [InlineData(null, true, true)]
        [InlineData(true, null, true)]
        [InlineData(true, true, true)]
        [InlineData(true, false, false)]
        [InlineData(false, true, true)]
        private void TestWhetherFileIsReplacedBasedOnSeriesOptions(
            bool? SeriesReplaceExisting,
            bool? PatternReplaceExisting,
            bool shouldBeReplaced)
        {
            var arguments = TestArguments.Default;
            var options = TestOptions.Default;
            var client = new TestClient();

            foreach(var series in options.Series)
            {
                series.ReplaceExisting = SeriesReplaceExisting;
                foreach (var pattern in series.Patterns)
                {
                    pattern.ReplaceExisting = PatternReplaceExisting;
                }
            }

            TestFiles.CreateFiles(TestArguments.DefaultTarget, TestFiles.SingleSeries);
            
            var outputFile = Path.Combine(TestOptions.DefaultCollection, OutputFiles.SingleSeries[0]);
            var initialContents = TestFiles.CreateFile(outputFile);

            var copier = new Copier(arguments, client, options);
            var matches = copier.CopyFiles().Result;

            Assert.Equal(TestFiles.SingleSeries.Length, matches);

            ValidateFiles(options.Collection, OutputFiles.SingleSeries);

            var finalContents = File.ReadAllText(outputFile);
            if (!shouldBeReplaced)
            {
                Assert.Equal(initialContents, finalContents);
            }
            else
            {
                Assert.NotEqual(initialContents, finalContents);
            }
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
