#if UNITY_AUDIO_SYNTAX

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif // #if UNITY_EDITOR

#if UNITY_AUDIO_SYNTAX_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif // UNITY_AUDIO_SYNTAX_ADDRESSABLES

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// System to be able to play back Unity native audio with a syntax similar to FMOD-Syntax.
    /// </summary>
    public sealed class UnityAudioSyntaxSystem 
    {
        public enum LoadAudioEventConfigResults
        {
            Success,
            RequiresAsynchronousLoading,
        }
        
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
        private const string EditorTimeAudioSourceName = "AUDIOSYNTAX_EDITOR_TIME_AUDIOSOURCE";
        
        [InitializeOnLoadMethod]
        private static void EditorInitializeOnload()
        {
            onEventPlaybackCallbackReceivers.Clear();
            
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.update += OnEditorUpdate;

            // Necessary otherwise it's easy for these to get lost and pile up.
            ClearExistingEditorTimeAudioSources();
            
            AudioSyntaxSystem.StopAllActivePlaybacks();
        }

        private static void ClearExistingEditorTimeAudioSources()
        {
            AudioSource[] allAudioSources = Resources.FindObjectsOfTypeAll<AudioSource>();
            for (int i = 0; i < allAudioSources.Length; i++)
            {
                if (string.Equals(
                        allAudioSources[i].name, EditorTimeAudioSourceName, StringComparison.OrdinalIgnoreCase))
                {
                    Object.DestroyImmediate(allAudioSources[i].gameObject);
                }
            }
        }
        
        private static void OnEditorUpdate()
        {
            if (!Application.isPlaying)
                AudioSyntaxSystem.Update();
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

            int defaultCapacity = 4;
#if UNITY_EDITOR
            defaultCapacity = 0;
#endif

            audioSourcesPool = new Pool<AudioSource>(() =>
            {
                Transform container = Application.isPlaying ? AudioSourceContainer : null;
                AudioSource audioSource = Object.Instantiate(audioSourcePooledPrefab, container);

#if DEBUG_AUDIO_SOURCE_POOLING && UNITY_EDITOR
                audioSource.name = "AudioSource - (Unused)";
#endif
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    audioSource.gameObject.hideFlags |= HideFlags.DontSave | HideFlags.NotEditable;
                    
#if !UNITY_AUDIO_SYNTAX_DEBUG_EDITOR_TIME_PLAYBACK
                    audioSource.gameObject.hideFlags |= HideFlags.HideInHierarchy;
#endif // !UNITY_AUDIO_SYNTAX_DEBUG_EDITOR_TIME_PLAYBACK
                    
                    audioSource.name = EditorTimeAudioSourceName;
                }
#endif // UNITY_EDITOR
                
                return audioSource;
            }, null, null, null, defaultCapacity);
        }
        
        public void Update()
        {
            for (int i = 0; i < activePlaybacks.Count; i++)
            {
                activePlaybacks[i].Update();
            }
        }
        
        public AudioSource GetAudioSourceForPlayback()
        {
            AudioSource audioSource = audioSourcesPool.Get();
            
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

        public static LoadAudioEventConfigResults TryLoadAudioEventConfigAtRuntime<ConfigType>(string path, out ConfigType config)
            where ConfigType : UnityAudioEventConfigAssetBase
        {
            bool wasLoaded = UnityAudioEventConfigAssetBase.TryGetLoadedConfig(path, out config);
            if (wasLoaded)
                return LoadAudioEventConfigResults.Success;
            
#if UNITY_AUDIO_SYNTAX_ADDRESSABLES
            return LoadAudioEventConfigResults.RequiresAsynchronousLoading;
#else
            path = UnityAudioSyntaxSettings.Instance.UnityAudioEventConfigAssetRootFolderRelativeToResources + path;
            config = Resources.Load<ConfigType>(path);
            
            // Could do a null check here, but null checks are expensive and it's *supposed* to exist at this path
            // because everything in Resources is loaded up front. In the rare event that it could not load an audio
            // config this way, it seems fine to me to throw an NRE so you can go and fix it (most likely a path
            // caching issue, although we have various systems in place for making sure they're automatically updated)
            // So long story short: not worth doing costly null checks EVERY TIME to catch a very rare problem that
            // will already be noticeable via an exception anyway.
            return LoadAudioEventConfigResults.Success;
#endif
        }

#if UNITY_AUDIO_SYNTAX_ADDRESSABLES
        public delegate void AudioEventConfigLoadedAsyncHandler<ConfigType>(ConfigType config)
            where ConfigType : UnityAudioEventConfigAssetBase;
        
        public static AsyncOperationHandle<ConfigType> TryLoadAudioEventConfigFromAddressables<ConfigType>(
            string path, AudioEventConfigLoadedAsyncHandler<ConfigType> callback)
            where ConfigType : UnityAudioEventConfigAssetBase
        {
            bool didFindAddress =
                UnityAudioSyntaxSettings.Instance.GetAddressForAudioEventPath(path, out string address);
            if (!didFindAddress)
            {
                Debug.LogError($"Tried to lazy load event with path '{path}' but no corresponding address was " +
                               $"found. Something may have gone wrong with caching the addresses. It's supposed " +
                               $"to do this automatically whenever you build Addressables. Please re-build " +
                               $"Addressables and check the UnityAudioSyntaxSettings asset to verify that the " +
                               $"path you're looking for is in there.");
                return default;
            }
            
            AsyncOperationHandle<ConfigType> handle = Addressables.LoadAssetAsync<ConfigType>(address);
            if (handle.IsDone)
                callback?.Invoke(handle.Result);
            else
                handle.Completed += operationHandle => callback?.Invoke(operationHandle.Result);
            
            return handle;
        }
#endif
        
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
        
        public static void ClearAllPlaybacks()
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
