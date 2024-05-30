namespace PlexCopier.Utils
{
    public interface IFileLock : IDisposable
    {
        bool Acquire();

        void Release();
    }
}