using NUnit.Framework;
using PlexCopier.Utils;

namespace tst
{
    public class FileLockTests
    {
        [SetUp]
        public void BeforeTest()
        {
            TestFiles.Cleanup();
        }

        [TearDown]
        public void AfterTest()
        {
            TestFiles.Cleanup();
        }

        [Test]
        public void LockingUnlockedFileShouldSucceed()
        {
            var filePath = Path.GetFullPath(Path.Join("Unlocked", "File.mp4"));
            TestFiles.CreateFile(Path.Combine(TestArguments.DefaultTarget, filePath));
            using var fileLock = new FileLock(filePath);
            Assert.That(fileLock.Acquire(), Is.True);
        }

        [Test]
        public void LockingLockedFileShouldFailWhileFileIsNotSharedForRead()
        {
            var filePath = Path.GetFullPath(Path.Join("Locked", "File.mp4"));
            TestFiles.CreateFile(Path.Combine(TestArguments.DefaultTarget, filePath));
            using var fileLock = new FileLock(filePath);
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Assert.That(fileLock.Acquire(), Is.False);
            }
            Assert.That(fileLock.Acquire(), Is.True);
        }

        [Test]
        [Ignore("Locks are not behaving as expected in Linux")]
        public void LockingLockedFileShouldFailWhileFileIsOpenForWrite()
        {
            var filePath = Path.GetFullPath(Path.Join("Locked", "File.mp4"));
            TestFiles.CreateFile(Path.Combine(TestArguments.DefaultTarget, filePath));
            using var fileLock = new FileLock(filePath);
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read))
            {
                Assert.That(fileLock.Acquire(), Is.False);
            }
            Assert.That(fileLock.Acquire(), Is.True);
        }
    }
}