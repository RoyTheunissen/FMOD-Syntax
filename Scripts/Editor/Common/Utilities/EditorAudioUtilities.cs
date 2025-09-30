using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    /// <summary>
    /// Utilities to help play audio at editor time.
    /// 
    /// Courtesy of Thom_Denick_1 on the Unity forums:
    /// https://discussions.unity.com/t/way-to-play-audio-in-editor-using-an-editor-script/473638/14
    /// </summary>
    public static class EditorAudioUtilities
    {
        private const string AudioUtilClassName = "UnityEditor.AudioUtil";
        
        public static void PlayClip(AudioClip clip, bool loop = false, int startSample = 0)
        {
#if UNITY_2020_1_OR_NEWER
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;
     
            Type audioUtilClass = unityEditorAssembly.GetType(AudioUtilClassName);
            MethodInfo method = audioUtilClass.GetMethod(
                "PlayPreviewClip",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                null
            );
            
            method.Invoke(
                null,
                new object[] { clip, startSample, loop }
            );
#else
            Debug.LogError($"Playing audio clips in Unity versions before 2020 is not currently supported.");
#endif
        }

        public static void StopAllClips()
        {
#if UNITY_2020_1_OR_NEWER
            Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

            Type audioUtilClass = unityEditorAssembly.GetType(AudioUtilClassName);
            MethodInfo method = audioUtilClass.GetMethod(
                "StopAllPreviewClips",
                BindingFlags.Static | BindingFlags.Public,
                null,
                new Type[] { },
                null
            );
            
            method.Invoke(
                null,
                new object[] { }
            );
#else
            Debug.LogError($"Stopping audio clips in Unity versions before 2020 is not currently supported.");
#endif
        }
    }
}
