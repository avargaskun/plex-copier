using NSubstitute;
using PlexCopier;
using Xunit;

namespace tst
{
    public class WatcherTests : IDisposable
    {
        public WatcherTests()
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
        public async Task FileIsDetectedAfterWritingToRootFolder()
        {
            var arguments = TestArguments.Default;
            var copier = Substitute.For<ICopier>();
            
            using var watcher = new Watcher(arguments, copier);
            _ = Task.Run(watcher.Start);
            await WaitHelper.WaitUntil(() => watcher.IsRunning, message: "Waiting for watcher to start");

            var testFile = Path.Combine(TestArguments.DefaultTarget, $"{Guid.NewGuid()}.mp4");
            TestFiles.CreateFile(testFile);
            
            var fullPath = Path.GetFullPath(testFile);
            await WaitHelper.TryUntil(() => copier.Received().CopyFiles(fullPath));

            watcher.Stop();
            await WaitHelper.WaitUntil(() => !watcher.IsRunning, message: "Waiting for starter to end");
        }

        [Fact]
        public async Task FileIsDetectedAfterWritingToSubFolderWhenRecursive()
        {
            var arguments = TestArguments.Default;
            var copier = Substitute.For<ICopier>();
            
            using var watcher = new Watcher(arguments, copier);
            _ = Task.Run(watcher.Start);
            await WaitHelper.WaitUntil(() => watcher.IsRunning, message: "Waiting for watcher to start");

            var testFile = Path.Combine(TestArguments.DefaultTarget, "TestFolder", $"{Guid.NewGuid()}.mp4");
            TestFiles.CreateFile(testFile);
            
            var fullPath = Path.GetFullPath(testFile);
            await WaitHelper.TryUntil(() => copier.Received().CopyFiles(Path.GetDirectoryName(fullPath)));

            watcher.Stop();
            await WaitHelper.WaitUntil(() => !watcher.IsRunning, message: "Waiting for starter to end");
        }

        [Fact]
        public async Task FileIsNotDetectedAfterWritingToSubFolderWhenNotRecursive()
        {
            var arguments = TestArguments.Default with { Recursive = false };
            var copier = Substitute.For<ICopier>();
            
            using var watcher = new Watcher(arguments, copier);
            _ = Task.Run(watcher.Start);
            await WaitHelper.WaitUntil(() => watcher.IsRunning, message: "Waiting for watcher to start");

            var testFile = Path.Combine(TestArguments.DefaultTarget, "TestFolder", $"{Guid.NewGuid()}.mp4");
            TestFiles.CreateFile(testFile);

            await Task.Delay(1000);

            await copier.DidNotReceiveWithAnyArgs().CopyFiles(default);

            watcher.Stop();
            await WaitHelper.WaitUntil(() => !watcher.IsRunning, message: "Waiting for starter to end");
        }

        [Fact]
        public async Task MultipleFilesDetectedAfterWritingToSubFolder()
        {
            var arguments = TestArguments.Default;
            var copier = Substitute.For<ICopier>();
            
            using var watcher = new Watcher(arguments, copier);
            _ = Task.Run(watcher.Start);
            await WaitHelper.WaitUntil(() => watcher.IsRunning, message: "Waiting for watcher to start");

            var testFile = Path.Combine(TestArguments.DefaultTarget, "TestFolder", $"{Guid.NewGuid()}.mp4");
            TestFiles.CreateFile(testFile);
            
            var fullPath = Path.GetFullPath(testFile);
            await WaitHelper.TryUntil(() => copier.Received().CopyFiles(Path.GetDirectoryName(fullPath)));

            copier.ClearReceivedCalls();
            testFile = Path.Combine(TestArguments.DefaultTarget, "TestFolder", $"{Guid.NewGuid()}.mp4");
            TestFiles.CreateFile(testFile);

            fullPath = Path.GetFullPath(testFile);
            await WaitHelper.TryUntil(() => copier.Received().CopyFiles(fullPath));

            watcher.Stop();
            await WaitHelper.WaitUntil(() => !watcher.IsRunning, message: "Waiting for starter to end");
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
    }
}
