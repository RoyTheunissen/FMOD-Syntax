using RoyTheunissen.FMODWrapper;

namespace FMOD.Studio
{
    /// <summary>
    /// Extensions for bus to get the path without an out parameter because to make lambda functions easy to write.
    /// </summary>
    public static class BusEditorExtensions 
    {
        public static string GetName(this Bus bus)
        {
            return FmodWrapperUtilities.GetFilteredNameFromPath(bus.getPath());
        }
    }
}
