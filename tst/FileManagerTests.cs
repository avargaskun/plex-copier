using NUnit.Framework;
using PlexCopier.Utils;

namespace tst
{
    public class FileManagerTests
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
        public async Task FileIsCopiedSuccessfully()
        {
            var arguments = TestArguments.Default with { Force = false };
            var manager = new FileManager(arguments);

            var sourceFile = Path.Combine(TestArguments.DefaultTarget, "Source.mp4");
            var targetFile = Path.Combine(TestArguments.DefaultTarget, "Target.mp4");
            var content = TestFiles.CreateFile(sourceFile);
            
            await manager.CopyAsync(sourceFile, targetFile);

            Assert.That(File.ReadAllText(targetFile), Is.EqualTo(content));
            Assert.That(File.Exists(sourceFile), Is.True);
        }

        [Test]
        public async Task FileIsMovedSuccessfully()
        {
            var arguments = TestArguments.Default with { Force = false };
            var manager = new FileManager(arguments);

            var sourceFile = Path.Combine(TestArguments.DefaultTarget, "Source.mp4");
            var targetFile = Path.Combine(TestArguments.DefaultTarget, "Target.mp4");
            var content = TestFiles.CreateFile(sourceFile);
            
            await manager.MoveAsync(sourceFile, targetFile);

            Assert.That(File.ReadAllText(targetFile), Is.EqualTo(content));
            Assert.That(File.Exists(sourceFile), Is.False);
        }

        [Test]
        public void FileFailsToCopyIfAlreadyExists()
        {
            var arguments = TestArguments.Default with { Force = false };
            var manager = new FileManager(arguments);

            var sourceFile = Path.Combine(TestArguments.DefaultTarget, "Source.mp4");
            var targetFile = Path.Combine(TestArguments.DefaultTarget, "Target.mp4");
            TestFiles.CreateFile(sourceFile);
            TestFiles.CreateFile(targetFile);
            
            Assert.CatchAsync(() => manager.CopyAsync(sourceFile, targetFile), "Copy operation should fail to replace existing file");
        }

        [Test]
        public async Task FileIsCopiedAndReplacesExisting()
        {
            var arguments = TestArguments.Default with { Force = true };
            var manager = new FileManager(arguments);

            var sourceFile = Path.Combine(TestArguments.DefaultTarget, "Source.mp4");
            var targetFile = Path.Combine(TestArguments.DefaultTarget, "Target.mp4");
            var content = TestFiles.CreateFile(sourceFile);
            TestFiles.CreateFile(targetFile);
            
            await manager.CopyAsync(sourceFile, targetFile);

            Assert.That(File.ReadAllText(targetFile), Is.EqualTo(content));
            Assert.That(File.Exists(sourceFile), Is.True);
        }
    }
}