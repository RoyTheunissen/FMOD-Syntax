#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX

using System;

namespace FMOD.Studio
{
    /// <summary>
    /// Extensions for VCA to get the path without an out parameter to make lambda functions easy to write.
    /// </summary>
    [Obsolete("RoyTheunissen.FMODSyntax version of a class is used. Please open 'Audio Syntax/Migration Wizard' to migrate to RoyTheunissen.AudioSyntax instead.")]
    public static class VCAExtensions 
    {
        public static string getPath(this FMOD.Studio.VCA vca)
        {
            return string.Empty;
        }
        
        public static float getVolume(this FMOD.Studio.VCA vca)
        {
            return 1.0f;
        }
    }
}
#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
