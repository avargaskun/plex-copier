namespace PlexCopier.Utils
{
    public interface IFileCompare
    {
        Task<bool> AreSame(string filePath1, string filePath2);
    }
}