using System;

namespace PlexCopier.Settings
{
    public class Series
    {
        public int Id { get; set; }

        public bool? MoveFiles { get; set; }

        public bool? ReplaceExisting { get; set; }

        public Pattern[] Patterns { get; set; }
    }
}