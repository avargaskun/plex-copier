using System;

namespace PlexCopier
{
    public class FatalException : Exception
    {
        public FatalException(string message) : base(message)
        {
        }
    }
}