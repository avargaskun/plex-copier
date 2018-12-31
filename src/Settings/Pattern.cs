using System;
using System.Text.RegularExpressions;

namespace PlexCopier.Settings
{
    public class Pattern
    {
        private Regex regex;

        public string Expression { get; set; }

        public int? SeasonStart { get; set; }

        public int? EpisodeOffset { get; set; }

        public bool? MoveFiles { get; set; }

        public bool? ReplaceExisting { get; set; }

        public Regex Regex
        {
            get
            {
                if (this.regex == null)
                {
                    this.regex = new Regex(this.Expression);
                }

                return this.regex;
            }
        }
    }
}