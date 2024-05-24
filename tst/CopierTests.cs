using NUnit.Framework;
using PlexCopier;

namespace tst
{

    [TestFixture]
    public class CopierTests
    {
        [SetUp]
        public void BeforeTest()
        {
            Cleanup();
            Directory.CreateDirectory(TestArguments.DefaultTarget);
            Directory.CreateDirectory(TestOptions.DefaultCollection);
        }

        [TearDown]
        public void AfterTest()
        {
            Cleanup();
        }

        [Test]
        public async Task TestAllFiles()
        {
            var arguments = TestArguments.Default;
            var options = TestOptions.Default;
            var client = new TestClient();

            TestFiles.CreateFiles(TestArguments.DefaultTarget, TestFiles.SingleSeries, TestFiles.DoubleSeries, TestFiles.LongSeries);

            var copier = new Copier(arguments, client, options);
            var matches = await copier.CopyFiles();

            Assert.That(matches, Is.EqualTo(TestFiles.SingleSeries.Length + TestFiles.DoubleSeries.Length + TestFiles.LongSeries.Length));

            ValidateFiles(options.Collection, OutputFiles.SingleSeries, OutputFiles.DoubleSeries, OutputFiles.LongSeries);
        }

        [Test]
        public async Task TestSingleFileDoesNotMatch()
        {
            var arguments = TestArguments.Default;
            arguments.Target = Path.Combine(TestArguments.DefaultTarget, TestFiles.SingleSeries[0]);

            var options = TestOptions.Default;
            options.Series = [TestOptions.LongSeries];

            var client = new TestClient();

            TestFiles.CreateFiles(TestArguments.DefaultTarget, TestFiles.SingleSeries);

            var copier = new Copier(arguments, client, options);
            var matches = await copier.CopyFiles();

            Assert.That(matches, Is.Zero);

            ValidateFiles(options.Collection);
        }

        [Test]
        public async Task TestMovieFile()
        {
            var arguments = TestArguments.Default;

            var options = TestOptions.Default;
            options.Series = [TestOptions.MovieSpecial];

            var client = new TestClient();

            TestFiles.CreateFiles(TestArguments.DefaultTarget, TestFiles.SeriesWithSpecials);

            var copier = new Copier(arguments, client, options);
            var matches = await copier.CopyFiles();

            Assert.That(matches, Is.EqualTo(1));

            var outputFile = Path.Combine(TestOptions.DefaultCollection, OutputFiles.SingleMovie[0]);
            Assert.That(File.Exists(outputFile));
        }

        [Test]
        // Baseline test without special characters
        [TestCase("Single Series")]
        // Test each individual character separately
        [TestCase("Single < Series")]
        [TestCase("Single > Series")]
        [TestCase("Single : Series")]
        [TestCase("Single \" Series")]
        [TestCase("Single ' Series")]
        [TestCase("Single / Series")]
        [TestCase("Single \\ Series")]
        [TestCase("Single | Series")]
        [TestCase("Single ? Series")]
        [TestCase("Single * Series")]
        // Test multiple invalid characters
        [TestCase("Single <>:\"'/\\|?* Series")]
        // Test with different character placement regarding spaces
        [TestCase("Single! Series")]
        [TestCase("Single !Series")]
        [TestCase("Single ! Series")]
        [TestCase("Single ! ! Series")]
        public async Task TestInvalidPathCharactersAreRemoved(string seriesName)
        {
            var arguments = TestArguments.Default;
            arguments.Target = Path.Combine(TestArguments.DefaultTarget, TestFiles.SingleSeries[0]);

            var options = TestOptions.Default;
            options.Series = [TestOptions.SingleSeries];

            var client = new TestClient();
            client.SeriesInfos[TestClient.SingleSeriesId].Name = seriesName;

            TestFiles.CreateFiles(TestArguments.DefaultTarget, TestFiles.SingleSeries);

            var copier = new Copier(arguments, client, options);
            var matches = await copier.CopyFiles();

            Assert.That(matches, Is.EqualTo(1));

            var outputFile = Path.Combine(TestOptions.DefaultCollection, OutputFiles.SingleSeries[0]);
            Assert.That(File.Exists(outputFile));
        }

        [Test]
        [TestCase(null, null, false)]
        [TestCase(null, false, false)]
        [TestCase(false, null, false)]
        [TestCase(false, false, false)]
        [TestCase(null, true, true)]
        [TestCase(true, null, true)]
        [TestCase(true, true, true)]
        [TestCase(true, false, false)]
        [TestCase(false, true, true)]
        public async Task TestWhetherFileIsReplacedBasedOnSeriesOptions(
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
            var matches = await copier.CopyFiles();

            Assert.That(matches, Is.EqualTo(TestFiles.SingleSeries.Length));

            ValidateFiles(options.Collection, OutputFiles.SingleSeries);

            var finalContents = File.ReadAllText(outputFile);
            if (!shouldBeReplaced)
            {
                Assert.That(finalContents, Is.EqualTo(initialContents));
            }
            else
            {
                Assert.That(finalContents, Is.Not.EqualTo(initialContents));
            }
        }

        private static void ValidateFiles(string root, params string[][] multipleTargets)
        {
            Assert.That(Directory.Exists(root), $"Root directory does not exist: {root}");

            var allFiles = Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories);
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
                    Assert.That(File.Exists(file), $"Could not find file {file}");
                }
            }

            foreach (var file in allFiles)
            {
                Assert.That(allTargets, Contains.Item(file));
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
            public static readonly string[] SingleSeries =
            [
                Path.Combine("Single Series", "Season 01", "Single Series - s01e01.mkv"),
                Path.Combine("Single Series", "Season 01", "Single Series - s01e02.mkv"),
                Path.Combine("Single Series", "Season 01", "Single Series - s01e03.mkv")
            ];
            public static readonly string[] DoubleSeries =
            [
                Path.Combine("Double Series", "Season 01", "Double Series - s01e01.mkv"),
                Path.Combine("Double Series", "Season 01", "Double Series - s01e02.mkv"),
                Path.Combine("Double Series", "Season 01", "Double Series - s01e03.mkv"),
                Path.Combine("Double Series", "Season 02", "Double Series - s02e01.mkv"),
                Path.Combine("Double Series", "Season 02", "Double Series - s02e02.mkv"),
                Path.Combine("Double Series", "Season 02", "Double Series - s02e03.mkv")
            ];
            public static readonly string[] LongSeries =
            [
                Path.Combine("Long Series", "Season 01", "Long Series - s01e01.mp4"),
                Path.Combine("Long Series", "Season 01", "Long Series - s01e02.mp4"),
                Path.Combine("Long Series", "Season 01", "Long Series - s01e03.mp4"),
                Path.Combine("Long Series", "Season 02", "Long Series - s02e01.mp4"),
                Path.Combine("Long Series", "Season 02", "Long Series - s02e02.mp4"),
                Path.Combine("Long Series", "Season 02", "Long Series - s02e03.mp4")
            ];
            public static readonly string[] SingleMovie =
            [
                Path.Combine("Series With Specials", "Season 00", "Series With Specials - s00e05.mkv")
            ];
        }
    }
}
