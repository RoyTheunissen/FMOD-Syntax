namespace FMOD.Studio
{
    /// <summary>
    /// Extensions for bus to get the path without an out parameter to make lambda functions easy to write.
    /// </summary>
    public static class BusExtensions 
    {
        public static string getPath(this FMOD.Studio.Bus bus)
        {
            bus.getPath(out string path);
            return path;
        }
        
        public static float getVolume(this FMOD.Studio.Bus bus)
        {
            bus.getVolume(out float volume);
            return volume;
        }
    }
}
