namespace RoyTheunissen.FMODSyntax
{
    public interface IFmodPlayback : IAudioPlayback
    {
    }
    
    public interface IAudioPlayback
    {
        string Name { get; }
        bool CanBeCleanedUp { get; }
        string SearchKeywords { get; }
        bool IsOneshot { get; }
        float NormalizedProgress { get; }
        float Volume { get; }
        
        /// <summary>
        /// Cleanup is responsible for finalizing playback and de-allocating whatever resources were used. 
        /// </summary>
        void Cleanup();

        void Stop();
    }
}
