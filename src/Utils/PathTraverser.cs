namespace PlexCopier.Utils
{
    public class PathTraverser
    {
        public IEnumerable<string> FindFilesInPath(string source, bool recursive)
        {
            if (File.Exists(source))
            {
                yield return source;
            }
            else if (!Directory.Exists(source))
            {
                throw new FatalException($"Target does not exist: {source}");
            }
            else if (!recursive)
            {
                foreach (var file in Directory.GetFiles(source).OrderBy(f => f))
                {
                    yield return file;
                }
            }
            else
            {
                var stack = new Stack<string>();
                stack.Push(source);
                while (stack.Count > 0)
                {
                    source = stack.Pop();
                    foreach (var directory in Directory.GetDirectories(source).OrderBy(d => d))
                    {
                        stack.Push(directory);
                    }
                    foreach (var file in Directory.GetFiles(source).OrderBy(f => f))
                    {
                        yield return file;
                    }
                }
            }
        }
    }
}