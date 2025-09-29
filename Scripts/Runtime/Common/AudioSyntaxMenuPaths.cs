namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Defines useful constants for menu paths so that we don't redefine those over and over.
    /// 
    /// NOTE: A duplicate of this class exists in the pre-compiled DLL for the Setup / Migration wizard. If you update
    /// this class, please update that class too.
    /// </summary>
    public static class AudioSyntaxMenuPaths
    {
        public const string ProjectName = "Audio Syntax";
        public const string ProjectNameNoSpace = "AudioSyntax";
        public const string Root = ProjectName + "/";
        
        public const string CreateUnityAudioConfig = "Audio/";
        
        public const string CreateSocCollection = "ScriptableObject Collection/Collections/" + ProjectName + "/";

        public const int CreateMenuPriority = 100;
        public const int CreateMenuPriorityMiscellaneous = CreateMenuPriority + 200;
    }
}
