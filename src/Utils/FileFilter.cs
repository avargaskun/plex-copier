namespace PlexCopier.Utils
{
    public class FileFilter
    {
        public List<string> foldersToIgnore;

        public FileFilter(IEnumerable<string> foldersToIgnore)
        {
            this.foldersToIgnore = new List<string>();
            foreach (var rawPath in foldersToIgnore)
            {
                if (Directory.Exists(rawPath))
                {
                    this.foldersToIgnore.Add(Path.GetFullPath(rawPath));
                }
            }
        }

        public bool IsIgnored(string source)
        {
            var fullPath = Path.GetFullPath(source);
            foreach (var folder in foldersToIgnore)
            {
                if (fullPath.StartsWith(folder))
                {
                    return true;
                }
            }

            return false;
        }
    }
}