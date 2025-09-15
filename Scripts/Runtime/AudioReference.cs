using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

#if FMOD_AUDIO_SYNTAX
using RoyTheunissen.FMODSyntax.UnityAudioSyntax;
#endif // FMOD_AUDIO_SYNTAX

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Inspector reference to FMOD audio config. Can be used to get a simple parameterless audio config.
    /// </summary>
    [Serializable]
    public class AudioReference : IAudioConfig
    {
        public enum Modes
        {
            Unity,
            FMOD,
        }
        
#pragma warning disable CS0414 // Field is assigned but its value is never used
#if FMOD_AUDIO_SYNTAX
        [SerializeField] private Modes mode = Modes.FMOD;
#else
        [SerializeField] private Modes mode = Modes.Unity;
#endif
#pragma warning restore CS0414 // Field is assigned but its value is never used
        
        private Modes Mode => mode;

        [SerializeField] private UnityAudioConfigBase unityAudioConfig;
        public UnityAudioConfigBase UnityAudioConfig => unityAudioConfig;

        [SerializeField] private string fmodEventGuid;
        
        [NonSerialized] private FmodParameterlessAudioConfig cachedFmodAudioConfig;
        [NonSerialized] private string guidFmodAudioConfigIsCachedFor;
        public FmodParameterlessAudioConfig FmodAudioConfig
        {
            get
            {
                if (guidFmodAudioConfigIsCachedFor != fmodEventGuid)
                {
                    guidFmodAudioConfigIsCachedFor = fmodEventGuid;
                    cachedFmodAudioConfig = GetParameterlessEventConfig(fmodEventGuid);
                    if (!string.IsNullOrEmpty(fmodEventGuid) && cachedFmodAudioConfig == null)
                    {
                        Debug.LogError($"FMOD event was assigned to audio reference but its corresponding config could " +
                                  $"not be found at runtime. Did you forget to compile FMOD code?");
                    }
                }
                return cachedFmodAudioConfig;
            }
        }

        public bool IsAssigned
        {
            get
            {
                switch (Mode)
                {
                    case Modes.Unity: return UnityAudioConfig != null;
                    case Modes.FMOD: return FmodAudioConfig != null;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public string Name => IsAssigned ? FmodAudioConfig.Name : "";
        public string Path => IsAssigned ? FmodAudioConfig.Path : "";

        [NonSerialized] private static bool didCacheParameterlessEvents;
        private static readonly Dictionary<string, FmodParameterlessAudioConfig> parameterlessEventsByGuid = new();

#if FMOD_AUDIO_SYNTAX
        public FmodParameterlessAudioPlayback PlayFMOD(Transform source = null)
        {
            return FmodAudioConfig.Play(source);
        }
#endif // FMOD_AUDIO_SYNTAX
        
#if UNITY_AUDIO_SYNTAX
        public UnityAudioPlayback PlayUnity(Transform source = null, float volumeFactor = 1.0f)
        {
            return UnityAudioConfig.PlayGeneric(source, volumeFactor);
        }
#endif // UNITY_AUDIO_SYNTAX
        
        public IAudioPlayback Play(Transform source = null)
        {
            switch (Mode)
            {
                case Modes.Unity:
#if UNITY_AUDIO_SYNTAX
                    return PlayUnity(source);
#else
                    Debug.LogError("Trying to play a Unity Audio Reference while the project is not configured for "+
                                    "Unity native audio. Is the UNITY_AUDIO_SYNTAX scripting define symbol missing?");
                    return null;
#endif
                case Modes.FMOD:
#if FMOD_AUDIO_SYNTAX
                    return PlayFMOD(source);
#else
                    Debug.LogError("Trying to play an FMOD Audio Reference while the project is not configured for "+
                                    "FMOD. Is the FMOD_AUDIO_SYNTAX scripting define symbol missing?");
                    return null;
#endif
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void Initialize()
        {
            parameterlessEventsByGuid.Clear();
            didCacheParameterlessEvents = false;
        }
#endif // UNITY_EDITOR
        
        
        public static FmodParameterlessAudioConfig GetParameterlessEventConfig(string guid)
        {
            CacheParameterlessEventConfigs();
            
            if (parameterlessEventsByGuid.TryGetValue(guid, out FmodParameterlessAudioConfig result))
                return result;

            return null;
        }
        
        private static void CacheParameterlessEventConfigs()
        {
            if (didCacheParameterlessEvents)
                return;

            didCacheParameterlessEvents = true;
            
            parameterlessEventsByGuid.Clear();
            
            const string containerClassName = "AudioParameterlessEvents";
            IEnumerable<Type> containerTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes()).Where(t => t.Name == containerClassName);

            foreach (Type containerType in containerTypes)
            {
                // Get the dictionary of parameter events by GUID.
                FieldInfo eventsByGuidDictionaryField = containerType.GetField(
                    "EventsByGuid", BindingFlags.Public | BindingFlags.Static);
                Dictionary<string, FmodParameterlessAudioConfig> eventsByGuidDictionary = 
                    (Dictionary<string, FmodParameterlessAudioConfig>)eventsByGuidDictionaryField.GetValue(null);
                
                // Compile them all into one big dictionary.
                foreach (KeyValuePair<string,FmodParameterlessAudioConfig> kvp in eventsByGuidDictionary)
                {
                    parameterlessEventsByGuid.Add(kvp.Key, kvp.Value);
                }
            }
        }

        public override string ToString()
        {
            FmodParameterlessAudioConfig config = FmodAudioConfig;
            return config == null ? "" : config.ToString();
        }
    }
}
