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

        public static bool TryLoadAudioEventConfigAtRuntime<ConfigType>(string path, out ConfigType config)
            where ConfigType : UnityAudioEventConfigAssetBase
        {
#if UNITY_AUDIO_SYNTAX_ADDRESSABLES
            return UnityAudioEventConfigAssetBase.TryGetLoadedConfig(path, out config);
#else
            path = UnityAudioSyntaxSettings.Instance.UnityAudioConfigRootFolderRelativeToResources + path;
            config = Resources.Load<ConfigType>(path);
            // Could do a null check here, but null checks are expensive and it's *supposed* to exist at this path
            // because everything in Resources is loaded up front. In the rare event that it could not load an audio
            // config this way, it seems fine to me to throw an NRE so you can go and fix it (most likely a path
            // caching issue, although we have various systems in place for making sure they're automatically updated)
            // So long story short: not worth doing costly null checks EVERY TIME to catch a very rare problem that
            // will already be noticeable via an exception anyway.
            return true;
#endif
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
