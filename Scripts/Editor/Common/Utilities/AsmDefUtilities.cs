using System;
using System.IO;
using UnityEditor;
using UnityEditorInternal;

namespace RoyTheunissen.AudioSyntax
{
    public static class AsmDefUtilities
    {
        public static  AssemblyDefinitionAsset GetAsmDefInFolder(string path)
        {
            string[] asmDefsGuids = AssetDatabase.FindAssets("t:asmdef", new[] { path });
            for (int i = 0; i < asmDefsGuids.Length; i++)
            {
                string asmDefPath = AssetDatabase.GUIDToAssetPath(asmDefsGuids[i]);
                string asmDefDirectory = Path.GetDirectoryName(asmDefPath).ToUnityPath();
            
                if (string.Equals(asmDefDirectory, path, StringComparison.Ordinal))
                    return AssetDatabase.LoadAssetAtPath<AssemblyDefinitionAsset>(asmDefPath);
            }

            return null;
        }
    
        public static AssemblyDefinitionAsset GetAsmDefInFolderOrParent(string path)
        {
            string currentPath = path;

            // First check if there's an asmdef in the start folder.
            AssemblyDefinitionAsset asmDefInStartFolder = GetAsmDefInFolder(path);
            if (asmDefInStartFolder != null)
                return asmDefInStartFolder;

            // Now keep checking every parent folder if there's an asmdef there.
            while (currentPath.HasParentDirectory())
            {
                // Go to the parent folder.
                currentPath = currentPath.GetParentDirectory();
            
                // See if there's an asmdef in this parent folder.
                AssemblyDefinitionAsset asmDefInFolder = GetAsmDefInFolder(currentPath);
                if (asmDefInFolder != null)
                    return asmDefInFolder;
            }

            return null;
        }
    }
}
