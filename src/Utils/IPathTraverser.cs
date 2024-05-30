namespace PlexCopier.Utils
{
    public interface IPathTraverser
    {
        IEnumerable<string> FindFilesInPath(string source, bool recursive);
    }
}