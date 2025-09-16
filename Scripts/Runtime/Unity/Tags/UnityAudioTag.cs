#if UNITY_AUDIO_SYNTAX

#if SCRIPTABLE_OBJECT_COLLECTION
using BrunoMikoski.ScriptableObjectCollections;
#endif // SCRIPTABLE_OBJECT_COLLECTION

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Scriptable Object that functions as a tag to organize events. In the Unity solution you can attach tags to an
    /// audio event. You could refer to it by name but it's smarter to refer to it via a strongly typed Scriptable
    /// Object instead so the reference doesn't break so easily
    /// (you can for example rename the tag just by renaming the ID asset).
    ///
    /// Optionally you can use the Scriptable Object Collection package by Bruno Mikoski to manage these identifiers,
    /// then as a bonus you can have static access code generated so that you can access tags from C# code using
    /// syntax like `UnityAudioTags.Player`. This part is optional so as not to have a hard dependency on another
    /// package. I can recommend doing it this way though.
    /// </summary>
#if !SCRIPTABLE_OBJECT_COLLECTION
    [UnityEngine.CreateAssetMenu(fileName = "UnityAudioTag", menuName = MenuPaths.CreateScriptableObject + "Unity Audio Tag")]
#endif // !SCRIPTABLE_OBJECT_COLLECTION
    public partial class UnityAudioTag : ScriptableObjectCollectionItem
    {
    }
}
#endif // UNITY_AUDIO_SYNTAX
