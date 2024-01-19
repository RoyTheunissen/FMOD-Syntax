namespace RoyTheunissen.FMODSyntax
{
    public interface IFmodPlayback
    {
        bool CanBeCleanedUp { get; }

        /// <summary>
        /// Cleanup is responsible for finalizing playback and de-allocating whatever resources were used. 
        /// </summary>
        void Cleanup();

        void Stop();
    }
    
    public interface IAudioPlayback : IFmodPlayback
    {
        string Name { get; }
        string SearchKeywords { get; }
        bool IsOneshot { get; }
        float NormalizedProgress { get; }
        float Volume { get; }
    }
}
