namespace PlexCopier.Settings
{
    public class TvDb
    {
        public required string ApiKey { get; set; }

        public required string UserKey { get; set; }

        public required string UserName { get; set; }

        public int TokenExpirationHours { get; set; } = 6;
    }
}