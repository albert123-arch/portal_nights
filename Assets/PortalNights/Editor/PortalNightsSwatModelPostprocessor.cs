using System;
using System.IO;
using UnityEditor;

namespace PortalNights.EditorTools
{
    public sealed class PortalNightsSwatModelPostprocessor : AssetPostprocessor
    {
        private const string SwatModelPath = "Assets/PortalNights/Models/Characters/Swat.fbx";
        private const string SwatImportSessionKey = "PortalNights.SwatModelImportChecked";

        [InitializeOnLoadMethod]
        private static void ReimportSwatAfterScriptReload()
        {
            if (SessionState.GetBool(SwatImportSessionKey, false) || !File.Exists(SwatModelPath))
            {
                return;
            }

            SessionState.SetBool(SwatImportSessionKey, true);
            ReimportSwatCharacter();
        }

        private void OnPreprocessModel()
        {
            if (!assetPath.Equals(SwatModelPath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            ModelImporter importer = (ModelImporter)assetImporter;
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
        }

        [MenuItem("Portal Nights/Reimport SWAT Character")]
        public static void ReimportSwatCharacter()
        {
            AssetDatabase.ImportAsset(SwatModelPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);
            AssetDatabase.SaveAssets();
        }
    }
}
