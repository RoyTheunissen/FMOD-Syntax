using RoyTheunissen.FMODSyntax.UnityAudioSyntax;

namespace RoyTheunissen.FMODSyntax
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
        string SearchKeywords { get; }
        bool IsOneshot { get; }
        float NormalizedProgress { get; }
        float Volume { get; set; }

        /// <summary>
        /// Cleanup is responsible for finalizing playback and de-allocating whatever resources were used. 
        /// </summary>
        void Cleanup();

        void Stop();
        
        public delegate void AudioClipGenericEventHandler(IAudioPlayback audioPlayback, string eventId);

        IAudioPlayback AddEventHandler(AudioClipEventId @event, AudioClipGenericEventHandler handler);
        IAudioPlayback RemoveEventHandler(AudioClipEventId @event, AudioClipGenericEventHandler handler);
    }
}
