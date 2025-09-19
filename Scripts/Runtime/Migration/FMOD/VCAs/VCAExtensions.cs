namespace FMOD.Studio
{
    /// <summary>
    /// Extensions for VCA to get the path without an out parameter to make lambda functions easy to write.
    /// </summary>
    public static class VCAExtensions 
    {
        public static string getPath(this FMOD.Studio.VCA vca)
        {
            vca.getPath(out string path);
            return path;
        }
        
        public static float getVolume(this FMOD.Studio.VCA vca)
        {
            vca.getVolume(out float volume);
            return volume;
        }
    }
}
