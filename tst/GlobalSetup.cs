using NUnit.Framework;

[assembly: NonParallelizable]

namespace tst
{
    [SetUpFixture]
    public class GlobalSetup
    {
        [OneTimeSetUp]
        public void BeforeAnyTests()
        {
            using var stream = File.OpenRead("log4net.config");
            log4net.Config.XmlConfigurator.Configure(stream);
        }
    }
}