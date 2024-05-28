namespace PlexCopier
{
    public interface ICopier
    {
        Task<int> CopyFiles(CancellationToken token);

        Task<int> CopyFiles(string source, CancellationToken token);
    }
}