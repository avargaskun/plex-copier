using System.Text.RegularExpressions;

namespace PlexCopier.Settings
{
    public class Pattern
    {
        private Regex? regex;

        public required string Expression { get; set; }

        public int? SeasonStart { get; set; }

        public int? EpisodeOffset { get; set; }

        public Regex Regex
        {
            get
            {
                if (regex == null)
                {
                    regex = new Regex(Expression);
                }

                return regex;
            }
        }
    }
}