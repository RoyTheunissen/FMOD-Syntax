#if !UNITY_AUDIO_SYNTAX && !FMOD_AUDIO_SYNTAX

namespace FMOD.Studio
{
    /// <summary>
    /// Extensions for VCA to get the path without an out parameter to make lambda functions easy to write.
    /// </summary>
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
