namespace RoyTheunissen.FMODSyntax
{
    public interface IAudioPlayback
    {
        bool CanBeCleanedUp { get; }

        string Name { get; }
        string SearchKeywords { get; }
        bool IsOneshot { get; }
        float NormalizedProgress { get; }
        float Volume { get; }

        /// <summary>
        /// Cleanup is responsible for finalizing playback and de-allocating whatever resources were used. 
        /// </summary>
        void Cleanup();
    }
}
