using FMODUnity;

namespace RoyTheunissen.FMODSyntax
{
    /// <summary>
    /// Useful extension methods for EditorEventRef.
    /// </summary>
    public static class EditorEventRefExtensions
    {
        public const string EventPrefix = "event:/";
        public const string SnapshotPrefix = "snapshot:/";
        
        public static string GetDisplayName(this EditorEventRef @event)
        {
            return FmodSyntaxUtilities.GetDisplayNameFromPath(@event.name);
        }
        
        public static string GetFilteredName(this EditorEventRef @event)
        {
            return FmodSyntaxUtilities.GetFilteredNameFromPath(@event.name);
        }
        
        public static string GetFieldName(this EditorEventRef @event)
        {
            return FmodSyntaxUtilities.GetFilteredNameFromPathLowerCase(@event.name);
        }
        
        public static string GetFilteredPath(this EditorEventRef @event, bool stripSpecialCharacters = false)
        {
            return FmodSyntaxUtilities.GetFilteredPath(@event.Path, stripSpecialCharacters);
        }
    }
}
