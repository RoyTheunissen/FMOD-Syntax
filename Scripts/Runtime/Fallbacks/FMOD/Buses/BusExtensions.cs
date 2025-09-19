using System;

#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX

namespace FMOD.Studio
{
    /// <summary>
    /// Extensions for bus to get the path without an out parameter to make lambda functions easy to write.
    /// </summary>
    [Obsolete("RoyTheunissen.FMODSyntax version of a class is used. Please open 'Audio Syntax/Migration Wizard' to migrate to RoyTheunissen.AudioSyntax instead.")]
    public static class BusExtensions
    {
        public static string getPath(this FMOD.Studio.Bus bus) => string.Empty;

        public static float getVolume(this FMOD.Studio.Bus bus) => 1.0f;
    }
}
#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
