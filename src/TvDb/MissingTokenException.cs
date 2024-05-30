namespace PlexCopier.TvDb
{
    public class MissingTokenException : Exception
    {
        public MissingTokenException()
            : base("Missing or invalid token, likely due to authentication failure with TVDB")
        {
        }
    }
}