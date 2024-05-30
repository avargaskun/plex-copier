namespace PlexCopier.TvDb
{
    public interface IEpisodeFinder
    {
        Task<EpisodeMatch?> FindForFile(string file, CancellationToken token);
    }
}