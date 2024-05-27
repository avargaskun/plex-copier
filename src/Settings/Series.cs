namespace PlexCopier.Settings
{
    public class Series
    {
        public int Id { get; set; }

        public required Pattern[] Patterns { get; set; }
    }
}