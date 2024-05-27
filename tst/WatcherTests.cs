using NSubstitute;
using NSubstitute.Extensions;
using NUnit.Framework;
using PlexCopier;
using PlexCopier.Settings;

namespace tst
{
    [TestFixture]
    public class WatcherTests
    {
        [SetUp]
        public void BeforeTest()
        {
            TestFiles.Cleanup();
            Directory.CreateDirectory(TestArguments.DefaultTarget);
            Directory.CreateDirectory(TestOptions.DefaultCollection);
        }

        [TearDown]
        public void AfterTest()
        {
            TestFiles.Cleanup();
        }

        [Test]
        public async Task FileIsDetectedAfterWritingToRootFolder()
        {
            var arguments = TestArguments.Default;
            var copier = Substitute.For<ICopier>();
            
            using var watcher = CreateWatcher(arguments, copier);
            _ = Task.Run(watcher.Start);
            await WaitHelper.WaitUntil(() => watcher.IsRunning, message: "Waiting for watcher to start");

            var testFile = Path.Combine(TestArguments.DefaultTarget, $"{Guid.NewGuid()}.mp4");
            TestFiles.CreateFile(testFile);
            
            var fullPath = Path.GetFullPath(testFile);
            await WaitHelper.TryUntil(() => copier.Received().CopyFiles(fullPath));

            watcher.Stop();
            await WaitHelper.WaitUntil(() => !watcher.IsRunning, message: "Waiting for starter to end");
        }

        [Test]
        public async Task FileIsDetectedAfterWritingToSubFolderWhenRecursive()
        {
            var arguments = TestArguments.Default;
            var copier = Substitute.For<ICopier>();
            
            using var watcher = CreateWatcher(arguments, copier);
            _ = Task.Run(watcher.Start);
            await WaitHelper.WaitUntil(() => watcher.IsRunning, message: "Waiting for watcher to start");

            var testFile = Path.Combine(TestArguments.DefaultTarget, "TestFolder", $"{Guid.NewGuid()}.mp4");
            var fullPath = Path.GetFullPath(testFile);
            copier.FindTargetFiles(Path.GetDirectoryName(fullPath)!).Returns([fullPath]);

            TestFiles.CreateFile(testFile);
            await WaitHelper.TryUntil(() => copier.Received().CopyFiles(testFile));

            watcher.Stop();
            await WaitHelper.WaitUntil(() => !watcher.IsRunning, message: "Waiting for starter to end");
        }

        [Test]
        public async Task FileIsNotDetectedAfterWritingToSubFolderWhenNotRecursive()
        {
            var arguments = TestArguments.Default with { Recursive = false };
            var copier = Substitute.For<ICopier>();
            
            using var watcher = CreateWatcher(arguments, copier);
            _ = Task.Run(watcher.Start);
            await WaitHelper.WaitUntil(() => watcher.IsRunning, message: "Waiting for watcher to start");

            var testFile = Path.Combine(TestArguments.DefaultTarget, "TestFolder", $"{Guid.NewGuid()}.mp4");
            TestFiles.CreateFile(testFile);

            await Task.Delay(1000);

            await copier.DidNotReceiveWithAnyArgs().CopyFiles(default);

            watcher.Stop();
            await WaitHelper.WaitUntil(() => !watcher.IsRunning, message: "Waiting for starter to end");
        }

        [Test]
        public async Task MultipleFilesDetectedAfterWritingToSubFolder()
        {
            var arguments = TestArguments.Default;
            var copier = Substitute.For<ICopier>();
            
            using var watcher = CreateWatcher(arguments, copier);
            _ = Task.Run(watcher.Start);
            await WaitHelper.WaitUntil(() => watcher.IsRunning, message: "Waiting for watcher to start");

            var testFile = Path.Combine(TestArguments.DefaultTarget, "TestFolder", $"{Guid.NewGuid()}.mp4");
            var fullPath = Path.GetFullPath(testFile);
            copier.FindTargetFiles(Path.GetDirectoryName(fullPath)!).Returns([fullPath]);

            TestFiles.CreateFile(testFile);
            await WaitHelper.TryUntil(() => copier.Received().CopyFiles(fullPath));

            copier.ClearReceivedCalls();
            testFile = Path.Combine(TestArguments.DefaultTarget, "TestFolder", $"{Guid.NewGuid()}.mp4");
            TestFiles.CreateFile(testFile);

            fullPath = Path.GetFullPath(testFile);
            await WaitHelper.TryUntil(() => copier.Received().CopyFiles(fullPath));

            watcher.Stop();
            await WaitHelper.WaitUntil(() => !watcher.IsRunning, message: "Waiting for starter to end");
        }

        private static Watcher CreateWatcher(Arguments arguments, ICopier copier)
        {
            var watcher = Substitute.ForPartsOf<Watcher>(arguments, copier);
            watcher.Configure().AsyncDelay(default, default).ReturnsForAnyArgs(Task.FromResult(0));
            return watcher;
        }
    }
}
