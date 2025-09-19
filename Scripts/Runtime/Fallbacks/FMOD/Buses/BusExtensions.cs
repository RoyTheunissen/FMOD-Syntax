#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX

namespace FMOD.Studio
{
    /// <summary>
    /// Extensions for bus to get the path without an out parameter to make lambda functions easy to write.
    /// </summary>
    public static class BusExtensions
    {
        public static string getPath(this FMOD.Studio.Bus bus) => string.Empty;

        public static float getVolume(this FMOD.Studio.Bus bus) => 1.0f;
    }
}
#endif // #if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX
