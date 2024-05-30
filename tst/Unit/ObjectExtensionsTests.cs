using System.Text.Json;
using NUnit.Framework;
using PlexCopier.Utils;

namespace tst.Unit
{
    public class ObjectExtensionsTests
    {
        [Test]
        public void AllPropertiesAreCopiedIncludingNull()
        {
            var replacement = new TestObject
            {
                IntProp = 5,
                IntRefProp = null, // Test that null values are copied
                StringProp = "I'm the new object",
                NestedProp = new TestObject
                {
                    StringProp = "I'm nested!"
                }
            };

            var original = new TestObject
            {
                IntProp = 10,
                IntRefProp = 15,
                StringProp = "I'm the old object",
                NestedProp = new TestObject
                {
                    IntProp = 20,
                    StringProp = "I will disappear :(",
                }
            };

            var replacementJson = JsonSerializer.Serialize(replacement);
            
            replacement.CopyTo(original);

            var copiedJson = JsonSerializer.Serialize(original);

            Assert.That(copiedJson, Is.EqualTo(replacementJson));
            Assert.That(original.NestedProp, Is.SameAs(replacement.NestedProp));
        }
        [Test]
        public void AllPropertiesAreCopiedExceptNull()
        {
            var replacement = new TestObject
            {
                IntProp = 5,
                IntRefProp = null, // Test that null values are copied
                StringProp = "I'm the new object",
                NestedProp = null
            };

            var original = new TestObject
            {
                IntProp = 10,
                IntRefProp = 15,
                StringProp = "I'm the old object",
                NestedProp = new TestObject
                {
                    IntProp = 20,
                    StringProp = "I will disappear :(",
                }
            };
            
            replacement.CopyTo(original, skipNull: true);

            Assert.That(original.IntProp, Is.EqualTo(5));
            Assert.That(original.StringProp, Is.EqualTo("I'm the new object"));
            Assert.That(original.IntRefProp, Is.EqualTo(15));
            Assert.That(original.NestedProp, Is.Not.Null);
        }

        public class TestObject
        {
            public int IntProp { get; set; }

            public int? IntRefProp { get; set; }

            public string? StringProp { get; set; }

            public TestObject? NestedProp { get; set; }
        }
    }
}