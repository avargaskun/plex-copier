namespace PlexCopier.Utils
{
    public interface IFileManager
    {
        Task CopyAsync(string source, string target);

        Task MoveAsync(string source, string target);

        void Delete(string path);
    }
}