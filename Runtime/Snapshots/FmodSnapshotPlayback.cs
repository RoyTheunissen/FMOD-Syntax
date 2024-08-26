using System.IO;
using FMOD;
using FMOD.Studio;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Playback of a snapshot. Sort of like an event, but simpler.
    /// </summary>
    public class FmodSnapshotPlayback : FmodPlayablePlaybackBase, IFmodPlayback
    {
        public void Play(EventDescription eventDescription)
        {
            eventDescription.getPath(out string path);
            
            if (!eventDescription.isValid())
            {
                eventDescription.getID(out GUID guid);
                UnityEngine.Debug.LogError($"Trying to play invalid FMOD Snapshot guid: '{guid}' path:'{path}'");
                return;
            }

            Name = Path.GetFileName(path);

            EventDescription = eventDescription;
            eventDescription.createInstance(out EventInstance newInstance);
            Instance = newInstance;

            Instance.start();

            FmodSyntaxSystem.RegisterActiveSnapshotPlayback(this);
        }
        
        public void Stop()
        {
            if (Instance.isValid())
                Instance.stop(STOP_MODE.ALLOWFADEOUT);
        }

        public override void Cleanup()
        {
            if (Instance.isValid())
            {
                if (EventDescription.isValid())
                {
                    Instance.release();
                    Instance.clearHandle();
                }
            }

            FmodSyntaxSystem.UnregisterActiveSnapshotPlayback(this);
        }
    }
}
