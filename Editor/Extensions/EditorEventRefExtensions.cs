using FMODUnity;

namespace RoyTheunissen.FMODWrapper
{
    /// <summary>
    /// Useful extension methods for EditorEventRef.
    /// </summary>
    public static class EditorEventRefExtensions
    {
        public const string EventPrefix = "event:/";
        
        public static string GetDisplayName(this EditorEventRef @event)
        {
            return FmodWrapperUtilities.GetDisplayNameFromPath(@event.name);
        }
        
        public static string GetFilteredName(this EditorEventRef @event)
        {
            return FmodWrapperUtilities.GetFilteredNameFromPath(@event.name);
        }
        
        public static string GetFieldName(this EditorEventRef @event)
        {
            return FmodWrapperUtilities.GetFilteredNameFromPathLowerCase(@event.name);
        }
        
        public static string GetFilteredPath(this EditorEventRef @event)
        {
            return FmodWrapperUtilities.GetFilteredPath(@event.Path, false);
        }
    }
}
