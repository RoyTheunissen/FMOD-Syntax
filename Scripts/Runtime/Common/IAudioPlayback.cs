namespace RoyTheunissen.AudioSyntax
{
    public interface IFmodPlayback
    {
        bool CanBeCleanedUp { get; }
        
        void Cleanup();
    }
    
    public interface IFmodAudioPlayback : IFmodPlayback, IAudioPlayback
    {
    }
    
    public interface IAudioPlayback
    {
        string Name { get; }
        bool CanBeCleanedUp { get; }
        
        /// <summary>
        /// Useful for things like quickly filtering out a subgroup of active events while debugging.
        /// </summary>
        string SearchKeywords { get; }
        bool IsOneshot { get; }
        float NormalizedProgress { get; }
        float Volume { get; set; }

        /// <summary>
        /// Cleanup is responsible for finalizing playback and de-allocating whatever resources were used. 
        /// </summary>
        void Cleanup();

        void Stop();
        
        void Restart();
        
        public delegate void AudioClipGenericEventHandler(IAudioPlayback audioPlayback, string eventId);

        IAudioPlayback AddTimelineEventHandler(AudioTimelineEventId @event, AudioClipGenericEventHandler handler);
        IAudioPlayback RemoveTimelineEventHandler(AudioTimelineEventId @event, AudioClipGenericEventHandler handler);
    }
}
