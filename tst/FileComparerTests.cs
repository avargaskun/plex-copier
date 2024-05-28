using NUnit.Framework;
using PlexCopier.Utils;

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
            var sourcePath = Path.GetFullPath(Path.Combine(TestArguments.DefaultTarget, "Identical", "Source", "video.mp4"));
            var targetPath = Path.GetFullPath(Path.Combine(TestArguments.DefaultTarget, "Identical", "Target", "video.mp4"));
            var content = Guid.NewGuid().ToString();
            TestFiles.CreateFile(sourcePath, content);
            TestFiles.CreateFile(targetPath, content);
            var compare = new FileCompare();
            await Assert.ThatAsync(() => compare.AreSame(sourcePath, targetPath), Is.True);
        }

        [Test]
        public async Task ComparingDifferentFilesShouldReturnFalse()
        {
            var sourcePath = Path.GetFullPath(Path.Combine(TestArguments.DefaultTarget, "Different", "Source", "video.mp4"));
            var targetPath = Path.GetFullPath(Path.Combine(TestArguments.DefaultTarget, "Different", "Target", "video.mp4"));
            TestFiles.CreateFile(sourcePath);
            TestFiles.CreateFile(targetPath);
            var compare = new FileCompare();
            await Assert.ThatAsync(() => compare.AreSame(sourcePath, targetPath), Is.False);
        }
    }
}