using System.IO;
using FMOD;
using FMOD.Studio;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Playback of a snapshot. Sort of like an event, but simpler. No parameters, for example.
    /// </summary>
    public sealed class FmodSnapshotPlayback : IFmodPlayback
    {
        private EventInstance instance;

        private EventDescription eventDescription;

        public bool CanBeCleanedUp
        {
            get
            {
                if (!instance.isValid())
                    return true;
                
                instance.getPlaybackState(out PLAYBACK_STATE playbackState);
                return playbackState == PLAYBACK_STATE.STOPPED;
            }
        }

        private string name;
        public string Name => name;

        public void Play(EventDescription eventDescription)
        {
            eventDescription.getPath(out string path);
            
            if (!eventDescription.isValid())
            {
                eventDescription.getID(out GUID guid);
                UnityEngine.Debug.LogError($"Trying to play invalid FMOD Snapshot guid: '{guid}' path:'{path}'");
                return;
            }

            name = Path.GetFileName(path);

            this.eventDescription = eventDescription;
            eventDescription.createInstance(out instance);

            instance.start();

            FmodSyntaxSystem.RegisterActiveSnapshotPlayback(this);
        }
        
        public void Stop()
        {
            if (instance.isValid())
                instance.stop(STOP_MODE.ALLOWFADEOUT);
        }

        public void Cleanup()
        {
            if (instance.isValid())
            {
                if (eventDescription.isValid())
                {
                    instance.release();
                    instance.clearHandle();
                }
            }

            FmodSyntaxSystem.UnregisterActiveSnapshotPlayback(this);
        }
    }
}
