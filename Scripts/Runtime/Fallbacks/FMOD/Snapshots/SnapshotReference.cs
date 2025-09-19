#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX

using System;
using UnityEngine;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Inspector reference to FMOD snapshot config. Can be used to specify via the inspector which snapshot to play.
    /// </summary>
    [Serializable]
    [Obsolete("RoyTheunissen.FMODSyntax version of a class is used. Please open 'Audio Syntax/Migration Wizard' to migrate to RoyTheunissen.AudioSyntax instead.")]
    public class SnapshotReference
    {
        [SerializeField] private string fmodSnapshotGuid;
        public bool IsAssigned => false;

        public string Name => string.Empty;
        public string Path => string.Empty;

        public FmodSnapshotPlayback Play()
        {
            return null;
        }
        
        
        public static FmodSnapshotConfigBase GetSnapshotConfig(string guid)
        {
            return null;
        }
    }
}

#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
