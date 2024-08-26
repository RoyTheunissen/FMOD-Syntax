using System;
using System.IO;
using FMOD;
using FMOD.Studio;
using Debug = UnityEngine.Debug;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Non-generic base class for FmodSnapshotPlayback to apply as a type constraint.
    /// </summary>
    public abstract class FmodSnapshotPlaybackBase : FmodPlayablePlaybackBase
    {
        private const string IntensityParameterName = "Intensity";
        
        public float Intensity
        {
            get
            {
                RESULT result = Instance.getParameterByName(IntensityParameterName, out float value);
                if (result != RESULT.OK)
                {
                    Debug.LogWarning($"Tried to get {IntensityParameterName} parameter of snapshot '{Name}' but no " +
                                     $"such parameter was found. Did you forget to set up an " +
                                     $"{IntensityParameterName} parameter in FMOD?");
                }
                return value;
            }
            set
            {
                RESULT result = Instance.setParameterByName(IntensityParameterName, value);
                if (result != RESULT.OK)
                {
                    Debug.LogWarning($"Tried to set {IntensityParameterName} parameter of snapshot '{Name}' but no " +
                                     $"such parameter was found. Did you forget to set up an " +
                                     $"{IntensityParameterName} parameter in FMOD?");
                }
            }
        }
    }
    
    /// <summary>
    /// Playback of a snapshot. Sort of like an event, but simpler.
    /// </summary>
    public abstract class FmodSnapshotPlayback : FmodSnapshotPlaybackBase, IFmodPlayback
    {
        public void Play(EventDescription eventDescription)
        {
            eventDescription.getPath(out string path);
            
            if (!eventDescription.isValid())
            {
                eventDescription.getID(out GUID guid);
                Debug.LogError($"Trying to play invalid FMOD Snapshot guid: '{guid}' path:'{path}'");
                return;
            }
            
            // Events are called something like event:/ but we want to get rid of any prefix like that.
            // Also every 'folder' along the way will be treated like a sort of 'tag'
            SearchKeywords = path.Substring(path.IndexOf("/", StringComparison.Ordinal) + 1).Replace('/', ',');

            Name = Path.GetFileName(path);

            EventDescription = eventDescription;
            eventDescription.createInstance(out EventInstance newInstance);
            Instance = newInstance;

            InitializeParameters();

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
