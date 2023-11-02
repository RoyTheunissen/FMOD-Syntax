using RoyTheunissen.FMODSyntax;

namespace FMOD.Studio
{
    /// <summary>
    /// Extensions for bus to get the path without an out parameter because to make lambda functions easy to write.
    /// </summary>
    public static class VCAEditorExtensions 
    {
        public static string GetName(this VCA vca)
        {
            return FmodSyntaxUtilities.GetFilteredNameFromPath(vca.getPath());
        }
    }
}
