#if UNITY_AUDIO_SYNTAX

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// System to be able to play back Unity native audio with a syntax similar to FMOD-Syntax.
    /// </summary>
    public sealed class UnityAudioSyntaxSystem 
    {
        private Pool<AudioSource> audioSourcesPool;
        
        private Transform cachedAudioSourceContainer;
        private bool didCacheAudioSourceContainer;
        private Transform AudioSourceContainer
        {
            get
            {
                if (!didCacheAudioSourceContainer)
                {
                    didCacheAudioSourceContainer = true;
                    cachedAudioSourceContainer = new GameObject("Audio Sources").transform;
                    Object.DontDestroyOnLoad(cachedAudioSourceContainer.gameObject);
                }
                return cachedAudioSourceContainer;
            }
        }
        
        [NonSerialized] private AudioSource audioSourcePooledPrefab;
        [NonSerialized] private AudioMixerGroup defaultMixerGroup;
        
        [NonSerialized] private static readonly List<UnityAudioPlayback> activePlaybacks = new();
        
        private static readonly List<IOnUnityPlaybackRegistered> onEventPlaybackCallbackReceivers = new();

        [NonSerialized] private bool didInitialize;
        
        [NonSerialized] private static UnityAudioSyntaxSystem cachedInstance;
        public static UnityAudioSyntaxSystem Instance
        {
            get
            {
                if (cachedInstance == null)
                {
                    cachedInstance = new UnityAudioSyntaxSystem();
                    cachedInstance.Initialize();
                }
                return cachedInstance;
            }
        }
        
#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void EditorInitializeOnload()
        {
            onEventPlaybackCallbackReceivers.Clear();
        }
#endif // UNITY_EDITOR
        
        public void Initialize()
        {
            if (didInitialize)
                return;

            didInitialize = true;
            
            onEventPlaybackCallbackReceivers.Clear();

            this.audioSourcePooledPrefab = UnityAudioSyntaxSettings.Instance.AudioSourcePooledPrefab;
            this.defaultMixerGroup = UnityAudioSyntaxSettings.Instance.DefaultMixerGroup;
            
            audioSourcesPool = new Pool<AudioSource>(() =>
            {
                AudioSource audioSource = Object.Instantiate(audioSourcePooledPrefab, AudioSourceContainer);

#if DEBUG_AUDIO_SOURCE_POOLING && UNITY_EDITOR
                audioSource.name = "AudioSource - (Unused)";
#endif
                return audioSource;
            });
        }
        
        public void Update()
        {
            for (int i = 0; i < activePlaybacks.Count; i++)
            {
                activePlaybacks[i].Update();
            }
        }
        
        public AudioSource GetAudioSourceForPlayback(UnityAudioEventConfigAssetBase audioEventConfig)
        {
            AudioSource audioSource = audioSourcesPool.Get();
            
            // Assign the mixer group.
            if (audioEventConfig.MixerGroup == null)
                audioSource.outputAudioMixerGroup = defaultMixerGroup;
            else
                audioSource.outputAudioMixerGroup = audioEventConfig.MixerGroup;
            
            // Reset these to the default Unity values, just in case...
            audioSource.clip = null;
            audioSource.pitch = 1.0f;
            audioSource.volume = 1.0f;
            audioSource.loop = false;
            audioSource.bypassEffects = false;
            audioSource.bypassListenerEffects = false;
            audioSource.bypassReverbZones = false;
            audioSource.maxDistance = 500;
            audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
            audioSource.minDistance = 1;

            return audioSource;
        }
        
        public void ReturnAudioSourceForPlayback(AudioSource audioSource)
        {
            audioSourcesPool.Return(audioSource);

            audioSource.clip = null;

#if DEBUG_AUDIO_SOURCE_POOLING && UNITY_EDITOR
            audioSource.name = "AudioSource - AVAILABLE";
#endif // DEBUG_AUDIO_SOURCE_POOLING
        }

        public static ConfigType LoadAudioEventConfigAtRuntime<ConfigType>(string path)
            where ConfigType : UnityAudioEventConfigAssetBase
        {
            // TODO: Support loading this via Addressables.
         
            path = UnityAudioSyntaxSettings.Instance.UnityAudioConfigRootFolderRelativeToResources + path;
            return Resources.Load<ConfigType>(path);
        }
        
        /// <summary>
        /// Do not call this yourself. Call AudioSyntaxSystem.RegisterActiveEventPlayback instead.
        /// Your application is preferably agnostic about the underlying audio implementation.
        /// </summary>
        public static void OnActiveEventPlaybackRegistered(UnityAudioPlayback playback)
        {
            activePlaybacks.Add(playback);
            
            for (int i = 0; i < onEventPlaybackCallbackReceivers.Count; i++)
            {
                onEventPlaybackCallbackReceivers[i].OnUnityPlaybackRegistered(playback);
            }
        }
        
        /// <summary>
        /// Do not call this yourself. Call AudioSyntaxSystem.UnregisterActiveEventPlayback instead.
        /// Your application is preferably agnostic about the underlying audio implementation.
        /// </summary>
        public static void OnActiveEventPlaybackUnregistered(UnityAudioPlayback playback)
        {
            activePlaybacks.Remove(playback);
            
            for (int i = 0; i < onEventPlaybackCallbackReceivers.Count; i++)
            {
                onEventPlaybackCallbackReceivers[i].OnUnityPlaybackUnregistered(playback);
            }
        }
        
        public static void ClearPlaybacks()
        {
            for (int i = activePlaybacks.Count - 1; i >= 0; i--)
            {
                activePlaybacks[i].Cleanup();
            }
            activePlaybacks.Clear();
        }
        
        public static void RegisterEventPlaybackCallbackReceiver(IOnUnityPlaybackRegistered callbackReceiver)
        {
            onEventPlaybackCallbackReceivers.Add(callbackReceiver);
        }
        
        public static void UnregisterEventPlaybackCallbackReceiver(IOnUnityPlaybackRegistered callbackReceiver)
        {
            onEventPlaybackCallbackReceivers.Remove(callbackReceiver);
        }
    }
}

#endif // UNITY_AUDIO_SYNTAX
