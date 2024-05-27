using NUnit.Framework;
using PlexCopier;

namespace tst
{
    public class FileComparerTests
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
        public async Task ComparingIdenticalFilesShouldReturnTrue()
        {
            var sourcePath = Path.GetFullPath(Path.Combine("Identical", "Source", "video.mp4"));
            var targetPath = Path.GetFullPath(Path.Combine("Identical", "Target", "video.mp4"));
            var content = Guid.NewGuid().ToString();
            TestFiles.CreateFile(sourcePath, content);
            TestFiles.CreateFile(targetPath, content);
            using var compare = new FileCompare();
            await Assert.ThatAsync(() => compare.AreSame(sourcePath, targetPath), Is.True);
        }

        [Test]
        public async Task ComparingDifferentFilesShouldReturnFalse()
        {
            var sourcePath = Path.GetFullPath(Path.Combine("Different", "Source", "video.mp4"));
            var targetPath = Path.GetFullPath(Path.Combine("Different", "Target", "video.mp4"));
            TestFiles.CreateFile(sourcePath);
            TestFiles.CreateFile(targetPath);
            using var compare = new FileCompare();
            await Assert.ThatAsync(() => compare.AreSame(sourcePath, targetPath), Is.False);
        }
    }
}