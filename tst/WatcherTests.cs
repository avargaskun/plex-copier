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
        public async Task WatcherCompletesWithoutOperationsBeingPerformed()
        {
            var arguments = TestArguments.Default;
            var copier = Substitute.For<ICopier>();
            
            using var watcher = CreateWatcher(arguments, copier);
            var runInstance = Task.Run(watcher.Start);

            await WaitHelper.WaitUntil(() => watcher.IsRunning, message: "Waiting for watcher to start");

            watcher.Stop();
            await WaitHelper.WaitUntil(() => runInstance.IsCompleted, message: "Waiting for watcher instance to complete");
        }

        [Test]
        public async Task FileIsDetectedAfterWritingToRootFolder()
        {
            var arguments = TestArguments.Default;
            var copier = Substitute.For<ICopier>();
            
            using var watcher = CreateWatcher(arguments, copier);
            var runInstance = Task.Run(watcher.Start);

            await WaitHelper.WaitUntil(() => watcher.IsRunning, message: "Waiting for watcher to start");

            var testFile = Path.Combine(TestArguments.DefaultTarget, $"{Guid.NewGuid()}.mp4");
            TestFiles.CreateFile(testFile);
            
            var fullPath = Path.GetFullPath(testFile);
            await WaitHelper.TryUntil(() => copier.Received().CopyFiles(fullPath, Arg.Any<CancellationToken>()));

            watcher.Stop();
            await WaitHelper.WaitUntil(() => runInstance.IsCompleted, message: "Waiting for watcher instance to complete");
        }

        [Test]
        public async Task FileIsDetectedAfterWritingToSubFolderWhenRecursive()
        {
            var arguments = TestArguments.Default;
            var copier = Substitute.For<ICopier>();
            
            using var watcher = CreateWatcher(arguments, copier);
            var runInstance = Task.Run(watcher.Start);

            await WaitHelper.WaitUntil(() => watcher.IsRunning, message: "Waiting for watcher to start");

            var testFile = Path.Combine(TestArguments.DefaultTarget, "TestFolder", $"{Guid.NewGuid()}.mp4");
            var fullPath = Path.GetFullPath(testFile);

            TestFiles.CreateFile(testFile);
            await WaitHelper.TryUntil(() => copier.Received().CopyFiles(testFile, Arg.Any<CancellationToken>()));

            watcher.Stop();
            await WaitHelper.WaitUntil(() => runInstance.IsCompleted, message: "Waiting for watcher instance to complete");
        }

        [Test]
        public async Task FileIsNotDetectedAfterWritingToSubFolderWhenNotRecursive()
        {
            var arguments = TestArguments.Default with { Recursive = false };
            var copier = Substitute.For<ICopier>();
            
            using var watcher = CreateWatcher(arguments, copier);
            var runInstance = Task.Run(watcher.Start);

            await WaitHelper.WaitUntil(() => watcher.IsRunning, message: "Waiting for watcher to start");

            var testFile = Path.Combine(TestArguments.DefaultTarget, "TestFolder", $"{Guid.NewGuid()}.mp4");
            TestFiles.CreateFile(testFile);

            await Task.Delay(1000);

            await copier.DidNotReceiveWithAnyArgs().CopyFiles(default);

            watcher.Stop();
            await WaitHelper.WaitUntil(() => runInstance.IsCompleted, message: "Waiting for watcher instance to complete");
        }

        [Test]
        public async Task MultipleFilesDetectedAfterWritingToSubFolder()
        {
            var arguments = TestArguments.Default;
            var copier = Substitute.For<ICopier>();
            
            using var watcher = CreateWatcher(arguments, copier);
            var runInstance = Task.Run(watcher.Start);

            await WaitHelper.WaitUntil(() => watcher.IsRunning, message: "Waiting for watcher to start");

            var testFile = Path.Combine(TestArguments.DefaultTarget, "TestFolder", $"{Guid.NewGuid()}.mp4");
            var fullPath = Path.GetFullPath(testFile);

            TestFiles.CreateFile(testFile);
            await WaitHelper.TryUntil(() => copier.Received().CopyFiles(fullPath, Arg.Any<CancellationToken>()));

            copier.ClearReceivedCalls();
            testFile = Path.Combine(TestArguments.DefaultTarget, "TestFolder", $"{Guid.NewGuid()}.mp4");
            TestFiles.CreateFile(testFile);

            fullPath = Path.GetFullPath(testFile);
            await WaitHelper.TryUntil(() => copier.Received().CopyFiles(fullPath, Arg.Any<CancellationToken>()));

            watcher.Stop();
            await WaitHelper.WaitUntil(() => runInstance.IsCompleted, message: "Waiting for watcher instance to complete");
        }

        [Test]
        public async Task WatcherWillNotExitUntilAllCopyOperationsComplete()
        {
            var arguments = TestArguments.Default;
            var copier = Substitute.For<ICopier>();
            var copyTasks = new List<TaskCompletionSource<int>>();
            copier.CopyFiles(string.Empty, default).ReturnsForAnyArgs(
                x =>
                {
                    var tcs = new TaskCompletionSource<int>();
                    copyTasks.Add(tcs);
                    return tcs.Task;
                }
            );

            using var watcher = CreateWatcher(arguments, copier);
            var runInstance = Task.Run(watcher.Start);

            await WaitHelper.WaitUntil(() => watcher.IsRunning, message: "Waiting for watcher to start");

            TestFiles.CreateFile(Path.Combine(TestArguments.DefaultTarget, "First Video.mp4"));
            TestFiles.CreateFile(Path.Combine(TestArguments.DefaultTarget, "Second Video.mp4"));

            await WaitHelper.WaitUntil(() => copyTasks.Count == 2, message: "Waiting for both files to be observed");

            watcher.Stop();
            await Task.Delay(TimeSpan.FromSeconds(1));
            Assert.That(runInstance.IsCompleted, Is.False, "Watcher should not complete while a copy task is running");

            copyTasks[0].SetResult(1);
            await Task.Delay(TimeSpan.FromSeconds(1));
            Assert.That(runInstance.IsCompleted, Is.False, "Watcher should not complete while a copy task is running");

            copyTasks[1].SetException(new IOException("Simluated failure to copy file"));
            await WaitHelper.WaitUntil(() => runInstance.IsCompleted, message: "Waiting for watcher instance to complete");
        }

        private static Watcher CreateWatcher(Arguments arguments, ICopier copier)
        {
            var watcher = Substitute.ForPartsOf<Watcher>(arguments, copier);
            watcher.Configure().AsyncDelay(default, default).ReturnsForAnyArgs(Task.FromResult(0));
            return watcher;
        }
    }
}
