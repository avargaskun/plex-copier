using NSubstitute;
using NSubstitute.Extensions;
using NUnit.Framework;
using PlexCopier;
using PlexCopier.Settings;
using PlexCopier.TvDb;
using PlexCopier.Utils;

namespace tst.Unit
{
    public class CopierTests
    {
        private string sourceFile;

        private string targetFile;

        private SeriesInfo seriesInfo;

        private EpisodeMatch episodeMatch;

        private IFileLock fileLock;

        private CancellationTokenSource cts;

        [SetUp]
        public void BeforeTest()
        {
            seriesInfo = new SeriesInfo
            {
                Name = "Mahoromatic",
                Seasons =
                [
                    // Season 0 - usually contains specials
                    new SeasonInfo
                    {
                        EpisodeCount = 2
                    },
                    new SeasonInfo
                    {
                        EpisodeCount = 10
                    },
                    new SeasonInfo
                    {
                        EpisodeCount = 12
                    },
                ]
            };

            episodeMatch = new EpisodeMatch(seriesInfo, 2, 3);
            sourceFile = "/home/antonio/videos/mahoromatic/video-15.mp4";
            targetFile = Path.Combine(TestOptions.Default.Collection, "Mahoromatic", "Season 02", "Mahoromatic - s02e03.mp4");
            cts = new CancellationTokenSource();
            fileLock = Substitute.For<IFileLock>();
        }

        [TearDown]
        public void AfterTest()
        {
            cts.Dispose();
            fileLock.Dispose();
        }

        [Test]
        public async Task WhenSingleFileIsCopiedSuccessfully()
        {
            var copier = CreateCopier(TestArguments.Default, TestOptions.Default);
            var count = await copier.CopyFiles(sourceFile, cts.Token);
            Assert.That(count, Is.EqualTo(1));
            _ = copier.FileManager.Received(1).CopyAsync(sourceFile, targetFile);
            fileLock.Received().Dispose();
        }

        private Copier CreateCopier(Arguments arguments, Options options)
        {
            var copier = new Copier(arguments, options)
            {
                EpisodeFinder = Substitute.For<IEpisodeFinder>(),
                FileLockFactory = Substitute.For<Func<string, IFileLock>>(),
                FileManager = Substitute.For<IFileManager>(),
                PathTraverser = Substitute.For<IPathTraverser>()
            };

            _ = copier.EpisodeFinder.FindForFile(sourceFile, cts.Token).Returns(Task.FromResult<EpisodeMatch?>(episodeMatch));
            copier.FileLockFactory(sourceFile).Returns(fileLock);
            fileLock.Acquire().Returns(true);
            copier.FileManager.ReturnsForAll((Task)Task.FromResult(0));
            copier.PathTraverser.FindFilesInPath(sourceFile, Arg.Any<bool>()).Returns([sourceFile]);

            return copier;
        }
    }
}