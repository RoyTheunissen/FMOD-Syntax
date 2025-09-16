using System;
using System.IO;
using FMOD;
using FMOD.Studio;
using Debug = UnityEngine.Debug;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Non-generic base class for FmodSnapshotPlayback to apply as a type constraint.
    /// </summary>
    public abstract class FmodSnapshotPlaybackBase : FmodPlayablePlaybackBase
    {
        private const string IntensityParameterName = "Intensity";
        private const float IntensityParameterRange = 100;
        
        /// <summary>
        /// For snapshots it's very useful to define a parameter to control the Intensity setting of the snapshot and
        /// thus blend it in and out. This can easily be created by right-clicking the Intensity control in the right
        /// of the snapshot editor and choosing Expose As Parameter.
        /// NOTE: By default the intensity parameter's values go from 0 - 100 so we respect that.
        /// Use IntensityNormalized if you prefer to set it as number between 0 and 1.
        /// </summary>
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

        public float IntensityNormalized
        {
            get => Intensity / IntensityParameterRange;
            set => Intensity = value * IntensityParameterRange;
        }
    }
    
    /// <summary>
    /// Playback of a snapshot. Sort of like an event, but simpler.
    /// </summary>
    public abstract class FmodSnapshotPlayback : FmodSnapshotPlaybackBase, IFmodPlayback
    {
        public bool IsOneshot => false;

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

            FmodAudioSyntaxSystem.RegisterActiveSnapshotPlayback(this);
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
                Instance.stop(STOP_MODE.IMMEDIATE);
                
                if (EventDescription.isValid())
                {
                    Instance.release();
                    Instance.clearHandle();
                }
            }

            FmodAudioSyntaxSystem.UnregisterActiveSnapshotPlayback(this);
        }
    }
}
