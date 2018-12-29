using System;
using System.IO;

using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PlexCopier.Settings
{
    public class Options
    {
        public const string DefaultFilename = "settings.yaml";

        public string Collection { get; set; }

        public TvDb TvDb { get; set; }

        public Series[] Series { get; set; }

        public static Options Load(string source)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            using (var reader = new StreamReader(source))
            {
                return deserializer.Deserialize<Options>(reader);
            }
        }
    }
}