namespace PlexCopier
{
    public interface ICopier
    {
        Task<int> CopyFiles(string? source);
    }
}