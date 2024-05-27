using NUnit.Framework;
using PlexCopier.Settings;

namespace tst
{
    public class OptionsTests
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
        public void LoadFromFileWithFullSchema()
        {
            var optionsPath = Path.GetFullPath(Path.Combine(TestArguments.DefaultTarget, "options.yaml"));
            TestFiles.CreateFile(optionsPath,
"""
collection: /path/to/collection
tvDb:
  apiKey: my_api_key
  userKey: my_user_key
  userName: my_user_name
series:
  - id: 1
    patterns:
      - expression: .*First Series, First Season - ([0-9]{2,3}).*
        episodeOffset: 10
      - expression: .*First Series, Second Season - ([0-9]{2,3}).*
        seasonStart: 2
  - id: 2
    patterns:
      - expression: .*Second Series, Third Season - ([0-9]{2,3}).*
        seasonStart: 3
"""
            );

            using var options = Options.Load(optionsPath);
            Assert.That(options.Collection, Is.EqualTo("/path/to/collection"));
            Assert.That(options.TvDb.ApiKey, Is.EqualTo("my_api_key"));
            Assert.That(options.TvDb.UserKey, Is.EqualTo("my_user_key"));
            Assert.That(options.TvDb.UserName, Is.EqualTo("my_user_name"));
            Assert.That(options.Series, Has.Length.EqualTo(2));
            Assert.That(options.Series[0].Id, Is.EqualTo(1));
            Assert.That(options.Series[0].Patterns, Has.Length.EqualTo(2));
            Assert.That(options.Series[0].Patterns[0].Expression, Is.EqualTo(".*First Series, First Season - ([0-9]{2,3}).*"));
            Assert.That(options.Series[0].Patterns[0].SeasonStart, Is.Null);
            Assert.That(options.Series[0].Patterns[0].EpisodeOffset, Is.EqualTo(10));
            Assert.That(options.Series[0].Patterns[1].Expression, Is.EqualTo(".*First Series, Second Season - ([0-9]{2,3}).*"));
            Assert.That(options.Series[0].Patterns[1].SeasonStart, Is.EqualTo(2));
            Assert.That(options.Series[0].Patterns[1].EpisodeOffset, Is.Null);
            Assert.That(options.Series[1].Id, Is.EqualTo(2));
            Assert.That(options.Series[1].Patterns[0].Expression, Is.EqualTo(".*Second Series, Third Season - ([0-9]{2,3}).*"));
            Assert.That(options.Series[1].Patterns[0].SeasonStart, Is.EqualTo(3));
            Assert.That(options.Series[1].Patterns[0].EpisodeOffset, Is.Null);
        }

        [Test]
        public void ReloadFileWithUpdatedSeriesInfo()
        {
            
            var optionsPath = Path.GetFullPath(Path.Combine(TestArguments.DefaultTarget, "options.yaml"));
            TestFiles.CreateFile(optionsPath,
"""
collection: /path/to/collection
tvDb:
  apiKey: my_api_key
  userKey: my_user_key
  userName: my_user_name
series:
  - id: 1
    patterns:
      - expression: .*First Series, First Season - ([0-9]{2,3}).*
"""
            );
            using var options = Options.Load(optionsPath);
            Assert.That(options.Series, Has.Length.EqualTo(1));
            Assert.That(options.Series[0].Patterns, Has.Length.EqualTo(1));
            Assert.That(options.Series[0].Patterns[0].Expression, Is.EqualTo(".*First Series, First Season - ([0-9]{2,3}).*"));

            optionsPath = Path.GetFullPath(Path.Combine(TestArguments.DefaultTarget, "options-new.yaml"));
            TestFiles.CreateFile(optionsPath,
"""
collection: /path/to/collection
tvDb:
  apiKey: my_api_key
  userKey: my_user_key
  userName: my_user_name
series:
  - id: 1
    patterns:
      - expression: .*First Series, First Season (changed) - ([0-9]{2,3}).*
      - expression: .*First Series, Second Season (new) - ([0-9]{2,3}).*
  - id: 2
    patterns:
      - expression: .*Second Series, First Season - ([0-9]{2,3}).*
"""
            );
            
            options.Reload(optionsPath);
            Assert.That(options.Series, Has.Length.EqualTo(2));
            Assert.That(options.Series[0].Patterns, Has.Length.EqualTo(2));
            Assert.That(options.Series[0].Patterns[0].Expression, Is.EqualTo(".*First Series, First Season (changed) - ([0-9]{2,3}).*"));
            Assert.That(options.Series[0].Patterns[1].Expression, Is.EqualTo(".*First Series, Second Season (new) - ([0-9]{2,3}).*"));
            Assert.That(options.Series[1].Patterns, Has.Length.EqualTo(1));
            Assert.That(options.Series[1].Patterns[0].Expression, Is.EqualTo(".*Second Series, First Season - ([0-9]{2,3}).*"));
        }

        [Test]
        public async Task WatchFileAndReloadWithUpdatedSeriesInfo()
        {
            
            var optionsPath = Path.GetFullPath(Path.Combine(TestArguments.DefaultTarget, "options.yaml"));
            TestFiles.CreateFile(optionsPath,
"""
collection: /path/to/collection
tvDb:
  apiKey: my_api_key
  userKey: my_user_key
  userName: my_user_name
series:
  - id: 1
    patterns:
      - expression: .*First Series, First Season - ([0-9]{2,3}).*
"""
            );
            using var options = Options.Load(optionsPath);
            Assert.That(options.Series, Has.Length.EqualTo(1));
            Assert.That(options.Series[0].Patterns, Has.Length.EqualTo(1));
            Assert.That(options.Series[0].Patterns[0].Expression, Is.EqualTo(".*First Series, First Season - ([0-9]{2,3}).*"));

            options.WatchForChanges(optionsPath);
            TestFiles.CreateFile(optionsPath,
"""
collection: /path/to/collection
tvDb:
  apiKey: my_api_key
  userKey: my_user_key
  userName: my_user_name
series:
  - id: 1
    patterns:
      - expression: .*First Series, First Season (changed) - ([0-9]{2,3}).*
      - expression: .*First Series, Second Season (new) - ([0-9]{2,3}).*
  - id: 2
    patterns:
      - expression: .*Second Series, First Season - ([0-9]{2,3}).*
"""
            );
            
            await WaitHelper.WaitUntil(() => options.Series.Length == 2);
            Assert.That(options.Series, Has.Length.EqualTo(2));
            Assert.That(options.Series[0].Patterns, Has.Length.EqualTo(2));
            Assert.That(options.Series[0].Patterns[0].Expression, Is.EqualTo(".*First Series, First Season (changed) - ([0-9]{2,3}).*"));
            Assert.That(options.Series[0].Patterns[1].Expression, Is.EqualTo(".*First Series, Second Season (new) - ([0-9]{2,3}).*"));
            Assert.That(options.Series[1].Patterns, Has.Length.EqualTo(1));
            Assert.That(options.Series[1].Patterns[0].Expression, Is.EqualTo(".*Second Series, First Season - ([0-9]{2,3}).*"));
        }
    }
}