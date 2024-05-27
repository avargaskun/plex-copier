namespace PlexCopier
{
    public class FileLock(string filePath) : IDisposable
    {
        private static readonly log4net.ILog Log = log4net.LogManager.GetLogger(typeof(FileLock));

        private readonly string filePath = filePath;

        private FileStream? lockStream = null;

        public bool Acquire()
        {
            lock(this)
            {
                if (lockStream != null)
                {
                    throw new Exception($"The lock for file {filePath} is already being held");
                }

                try
                {
                    lockStream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
                    Log.Debug($"Lock acquired for file: {filePath}");
                    return true;
                }
                catch (Exception ex)
                {
                    Log.Info($"Failed to acquire log for file {filePath}", ex);
                    return false;
                }
            }
        }

        public void Dispose()
        {
            Release();
            GC.SuppressFinalize(this);
        }

        public void Release()
        {
            lock(this)
            {
                if (lockStream != null)
                {
                    lockStream.Dispose();
                    lockStream = null;
                    Log.Debug($"Lock released for file: {filePath}");
                }
            }
        }
    }
}