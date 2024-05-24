using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PlexCopier.Settings
{
    public class Options
    {
        public const string DefaultFilename = "settings.yaml";

        public required string Collection { get; set; }

        public required TvDb TvDb { get; set; }

        public required Series[] Series { get; set; }

        public static Options Load(string source)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(CamelCaseNamingConvention.Instance)
                .Build();

            using var reader = new StreamReader(source);
            return deserializer.Deserialize<Options>(reader);
        }
    }
}