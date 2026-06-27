using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace PortalNights.EditorTools
{
    public static class PortalNightsCharacterVisualImporter
    {
        private const string ReportPath = "Assets/PortalNights/Reports/CharacterImportReport.md";
        private const string AutoImportMarkerPath = "Assets/PortalNights/Reports/.character_visual_import_requested";
        private const string UrpLitShaderName = "Universal Render Pipeline/Lit";

        private static readonly CharacterImportSpec[] CharacterSpecs =
        {
            new CharacterImportSpec(
                "Ch40",
                @"C:\Users\tash3\Downloads\Monsters\Ch40_nonPBR.fbx",
                "Assets/PortalNights/Art/Characters/Monsters/Ch40",
                "Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Ch40.prefab",
                1.86f),
            new CharacterImportSpec(
                "Parasite",
                @"C:\Users\tash3\Downloads\Monsters\Parasite L Starkie.fbx",
                "Assets/PortalNights/Art/Characters/Monsters/Parasite",
                "Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Parasite.prefab",
                2.03f),
            new CharacterImportSpec(
                "Pumpkinhulk",
                @"C:\Users\tash3\Downloads\Monsters\Pumpkinhulk L Shaw.fbx",
                "Assets/PortalNights/Art/Characters/Monsters/Pumpkinhulk",
                "Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Pumpkinhulk.prefab",
                2.04f),
            new CharacterImportSpec(
                "Maw",
                @"C:\Users\tash3\Downloads\Monsters\Maw J Laygo.fbx",
                "Assets/PortalNights/Art/Characters/Monsters/Maw",
                "Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Maw.prefab",
                2.38f),
            new CharacterImportSpec(
                "Vampire",
                @"C:\Users\tash3\Downloads\Monsters\Vampire A Lusth.fbx",
                "Assets/PortalNights/Art/Characters/Monsters/Vampire",
                "Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Vampire.prefab",
                2.11f),
            new CharacterImportSpec(
                "Mutant",
                @"C:\Users\tash3\Downloads\Monsters\Mutant (1).fbx",
                "Assets/PortalNights/Art/Characters/Monsters/Mutant",
                "Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Mutant.prefab",
                1.86f),
            new CharacterImportSpec(
                "Warrok",
                @"C:\Users\tash3\Downloads\Monsters\Warrok W Kurniawan (1).fbx",
                "Assets/PortalNights/Art/Characters/Monsters/Warrok",
                "Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Warrok.prefab",
                2.38f),
            new CharacterImportSpec(
                "Ch50",
                @"C:\Users\tash3\Downloads\Monsters\Ch50_nonPBR.fbx",
                "Assets/PortalNights/Art/Characters/Monsters/Ch50",
                "Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Ch50.prefab",
                1.77f),
            new CharacterImportSpec(
                "Yaku",
                @"C:\Users\tash3\Downloads\Monsters\Yaku J Ignite.fbx",
                "Assets/PortalNights/Art/Characters/Monsters/Yaku",
                "Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Yaku.prefab",
                2.06f),
            new CharacterImportSpec(
                "Staff_Ch32",
                @"C:\Users\tash3\Downloads\Ch32_nonPBR.fbx",
                "Assets/PortalNights/Art/Characters/Staff/Ch32",
                "Assets/PortalNights/Prefabs/Characters/Staff/PN_Visual_Staff_Ch32.prefab",
                1.77f),
        };

        [MenuItem("Portal Nights/Import Character Visual Prefabs")]
        public static void ImportCharacterVisualPrefabs()
        {
            Directory.CreateDirectory(ToAbsoluteProjectPath("Assets/PortalNights/Reports"));
            Directory.CreateDirectory(ToAbsoluteProjectPath("Assets/PortalNights/Prefabs/Characters/Monsters"));
            Directory.CreateDirectory(ToAbsoluteProjectPath("Assets/PortalNights/Prefabs/Characters/Staff"));

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (CharacterImportSpec spec in CharacterSpecs)
                {
                    EnsureCharacterFolders(spec);
                    CopyModelIfNeeded(spec);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            List<CharacterImportReport> reports = new List<CharacterImportReport>();
            foreach (CharacterImportSpec spec in CharacterSpecs)
            {
                CharacterImportReport report = ImportOneCharacter(spec);
                reports.Add(report);
            }

            WriteReport(reports);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Portal Nights character visual import complete. Report: {ReportPath}");
        }

        [InitializeOnLoadMethod]
        private static void RunPendingImportRequestInOpenEditor()
        {
            EditorApplication.delayCall += () =>
            {
                string markerPath = ToAbsoluteProjectPath(AutoImportMarkerPath);
                if (!File.Exists(markerPath))
                {
                    return;
                }

                File.Delete(markerPath);
                ImportCharacterVisualPrefabs();
            };
        }

        [MenuItem("Portal Nights/Validate Character Visual Prefabs")]
        public static void ValidateCharacterVisualPrefabs()
        {
            List<string> lines = new List<string>();
            foreach (CharacterImportSpec spec in CharacterSpecs)
            {
                CharacterImportReport report = ValidatePrefab(spec);
                string status = report.Warnings.Count == 0 ? "OK" : string.Join("; ", report.Warnings);
                lines.Add($"{spec.Name}: {status}");
            }

            Debug.Log("Portal Nights character visual prefab validation:\n" + string.Join("\n", lines));
        }

        public static void ImportCharacterVisualPrefabsBatch()
        {
            ImportCharacterVisualPrefabs();
        }

        public static void ValidateCharacterVisualPrefabsBatch()
        {
            ValidateCharacterVisualPrefabs();
        }

        private static CharacterImportReport ImportOneCharacter(CharacterImportSpec spec)
        {
            CharacterImportReport report = new CharacterImportReport(spec);
            report.TargetModelPath = spec.TargetModelPath;

            if (!File.Exists(spec.SourcePath))
            {
                report.Warnings.Add("Source FBX is missing.");
                return report;
            }

            ModelImporter importer = AssetImporter.GetAtPath(spec.TargetModelPath) as ModelImporter;
            if (importer == null)
            {
                report.Warnings.Add("Unity did not create a ModelImporter for the copied FBX.");
                return report;
            }

            ConfigureModelImporter(importer);
            importer.SaveAndReimport();

            report.TextureExtractionStatus = TryExtractTextures(importer, spec.TexturesFolder, report.Warnings);
            report.MaterialExtractionStatus = "Will create external prefab materials from imported slots";
            AssetDatabase.ImportAsset(spec.TargetModelPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);

            report.MaterialCount = CountMaterialSlots(spec.TargetModelPath);
            report.AvatarValid = IsAvatarValid(spec.TargetModelPath);
            if (!report.AvatarValid)
            {
                report.Warnings.Add("Humanoid avatar is invalid. The model remains visual-ready, but humanoid retargeting needs source rig repair or a Generic fallback.");
            }

            CreateVisualPrefab(spec, report);
            CharacterImportReport validation = ValidatePrefab(spec);
            report.HasForbiddenComponents = validation.HasForbiddenComponents;
            report.Warnings.AddRange(validation.Warnings.Where(w => !report.Warnings.Contains(w)));
            return report;
        }

        private static void ConfigureModelImporter(ModelImporter importer)
        {
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = true;
            importer.optimizeMeshPolygons = true;
            importer.optimizeMeshVertices = true;
        }

        private static string TryExtractTextures(ModelImporter importer, string texturesFolder, List<string> warnings)
        {
            try
            {
                Directory.CreateDirectory(ToAbsoluteProjectPath(texturesFolder));
                bool extracted = importer.ExtractTextures(texturesFolder);
                AssetDatabase.Refresh();
                return extracted ? "Extracted" : "No embedded textures extracted or already external";
            }
            catch (Exception ex)
            {
                warnings.Add("Texture extraction failed: " + ex.Message);
                return "Failed";
            }
        }

        private static void ConfigureExtractedMaterials(CharacterImportSpec spec, CharacterImportReport report)
        {
            Shader urpLit = Shader.Find(UrpLitShaderName);
            if (urpLit == null)
            {
                report.Warnings.Add($"URP/Lit shader was not found. Materials kept with their imported shaders.");
                return;
            }

            string[] materialGuids = AssetDatabase.FindAssets("t:Material", new[] { spec.MaterialsFolder });
            foreach (string guid in materialGuids)
            {
                string materialPath = AssetDatabase.GUIDToAssetPath(guid);
                Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                if (material == null)
                {
                    continue;
                }

                Texture baseMap = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : null;
                if (baseMap == null && material.HasProperty("_MainTex"))
                {
                    baseMap = material.GetTexture("_MainTex");
                }

                Color baseColor = Color.white;
                if (material.HasProperty("_BaseColor"))
                {
                    baseColor = material.GetColor("_BaseColor");
                }
                else if (material.HasProperty("_Color"))
                {
                    baseColor = material.GetColor("_Color");
                }

                material.shader = urpLit;
                if (baseMap != null && material.HasProperty("_BaseMap"))
                {
                    material.SetTexture("_BaseMap", baseMap);
                }

                if (material.HasProperty("_BaseColor"))
                {
                    material.SetColor("_BaseColor", baseColor);
                }

                if (material.HasProperty("_Metallic"))
                {
                    material.SetFloat("_Metallic", 0f);
                }

                if (material.HasProperty("_Smoothness"))
                {
                    material.SetFloat("_Smoothness", Mathf.Min(material.GetFloat("_Smoothness"), 0.45f));
                }

                EditorUtility.SetDirty(material);
            }
        }

        private static void AssignExternalMaterials(GameObject root, CharacterImportSpec spec, CharacterImportReport report)
        {
            Directory.CreateDirectory(ToAbsoluteProjectPath(spec.MaterialsFolder));

            Dictionary<Material, Material> materialMap = new Dictionary<Material, Material>();
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material source = materials[i];
                    if (source == null)
                    {
                        continue;
                    }

                    if (!materialMap.TryGetValue(source, out Material external))
                    {
                        external = CreateOrUpdateExternalMaterial(source, spec, report);
                        materialMap[source] = external;
                    }

                    materials[i] = external;
                }

                renderer.sharedMaterials = materials;
            }

            report.MaterialExtractionStatus = $"Created/remapped {materialMap.Count} external materials";
            ConfigureExtractedMaterials(spec, report);
            AssetDatabase.SaveAssets();
        }

        private static Material CreateOrUpdateExternalMaterial(Material source, CharacterImportSpec spec, CharacterImportReport report)
        {
            string safeName = MakeSafeAssetName(string.IsNullOrWhiteSpace(source.name) ? "Material" : source.name);
            string materialPath = AssetDatabase.GenerateUniqueAssetPath($"{spec.MaterialsFolder}/{safeName}.mat");
            Material material = null;

            string[] existingGuids = AssetDatabase.FindAssets($"{safeName} t:Material", new[] { spec.MaterialsFolder });
            foreach (string guid in existingGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path).Equals(safeName, StringComparison.OrdinalIgnoreCase))
                {
                    material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    materialPath = path;
                    break;
                }
            }

            if (material == null)
            {
                material = new Material(source)
                {
                    name = safeName,
                };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            else
            {
                material.CopyPropertiesFromMaterial(source);
                material.name = safeName;
            }

            ConfigureMaterialForProject(material, report);
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureMaterialForProject(Material material, CharacterImportReport report)
        {
            Shader urpLit = Shader.Find(UrpLitShaderName);
            if (urpLit == null)
            {
                report.Warnings.Add($"URP/Lit shader was not found for material {material.name}. Imported shader kept.");
                return;
            }

            Texture baseMap = material.HasProperty("_BaseMap") ? material.GetTexture("_BaseMap") : null;
            if (baseMap == null && material.HasProperty("_MainTex"))
            {
                baseMap = material.GetTexture("_MainTex");
            }

            Color baseColor = Color.white;
            if (material.HasProperty("_BaseColor"))
            {
                baseColor = material.GetColor("_BaseColor");
            }
            else if (material.HasProperty("_Color"))
            {
                baseColor = material.GetColor("_Color");
            }

            material.shader = urpLit;
            if (baseMap != null && material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", baseMap);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", Mathf.Min(material.GetFloat("_Smoothness"), 0.45f));
            }
        }

        private static void CreateVisualPrefab(CharacterImportSpec spec, CharacterImportReport report)
        {
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(spec.TargetModelPath);
            if (modelAsset == null)
            {
                report.Warnings.Add("Model asset could not be loaded for prefab creation.");
                return;
            }

            GameObject root = new GameObject("VisualRoot");
            try
            {
                GameObject modelInstance = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
                if (modelInstance == null)
                {
                    report.Warnings.Add("PrefabUtility could not instantiate the model.");
                    return;
                }

                modelInstance.name = spec.Name + "_Model";
                modelInstance.transform.SetParent(root.transform, false);
                modelInstance.transform.localPosition = Vector3.zero;
                modelInstance.transform.localRotation = Quaternion.identity;
                modelInstance.transform.localScale = Vector3.one;

                RemoveForbiddenComponents(root, report.Warnings);
                AssignExternalMaterials(root, spec, report);

                float initialHeight = TryGetLocalBounds(root, out Bounds initialBounds) ? initialBounds.size.y : 0f;
                if (initialHeight > 0f && (initialHeight < spec.TargetHeight * 0.5f || initialHeight > spec.TargetHeight * 2f))
                {
                    float multiplier = spec.TargetHeight / initialHeight;
                    modelInstance.transform.localScale *= multiplier;
                }

                AlignFeetToLocalZero(root, modelInstance);

                if (TryGetLocalBounds(root, out Bounds finalBounds))
                {
                    report.FinalVisualHeight = finalBounds.size.y;
                    if (Mathf.Abs(finalBounds.min.y) > 0.05f)
                    {
                        report.Warnings.Add($"Feet are not near local Y=0 after alignment. minY={finalBounds.min.y:F3}");
                    }
                }
                else
                {
                    report.Warnings.Add("No renderer bounds were found on prefab.");
                }

                report.FinalLocalScale = modelInstance.transform.localScale;
                Directory.CreateDirectory(ToAbsoluteProjectPath(Path.GetDirectoryName(spec.PrefabPath)?.Replace('\\', '/') ?? string.Empty));
                PrefabUtility.SaveAsPrefabAsset(root, spec.PrefabPath);
                report.PrefabCreated = true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static CharacterImportReport ValidatePrefab(CharacterImportSpec spec)
        {
            CharacterImportReport report = new CharacterImportReport(spec)
            {
                TargetModelPath = spec.TargetModelPath,
            };

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(spec.PrefabPath);
            if (prefab == null)
            {
                report.Warnings.Add("Prefab is missing.");
                return report;
            }

            GameObject contents = PrefabUtility.LoadPrefabContents(spec.PrefabPath);
            try
            {
                SkinnedMeshRenderer[] skinnedMeshes = contents.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                if (skinnedMeshes.Length == 0)
                {
                    report.Warnings.Add("Prefab has no SkinnedMeshRenderer.");
                }

                List<string> forbidden = FindForbiddenComponents(contents);
                report.HasForbiddenComponents = forbidden.Count > 0;
                if (forbidden.Count > 0)
                {
                    report.Warnings.Add("Forbidden components found: " + string.Join(", ", forbidden));
                }

                if (TryGetLocalBounds(contents, out Bounds bounds))
                {
                    report.FinalVisualHeight = bounds.size.y;
                    report.Warnings.AddRange(ValidateBounds(spec, bounds));
                }
                else
                {
                    report.Warnings.Add("Could not compute prefab renderer bounds.");
                }

                Transform model = contents.transform.childCount > 0 ? contents.transform.GetChild(0) : null;
                report.FinalLocalScale = model != null ? model.localScale : Vector3.one;
                report.PrefabCreated = true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }

            return report;
        }

        private static IEnumerable<string> ValidateBounds(CharacterImportSpec spec, Bounds bounds)
        {
            if (bounds.size.y < spec.TargetHeight * 0.5f || bounds.size.y > spec.TargetHeight * 2f)
            {
                yield return $"Height {bounds.size.y:F3} is outside reasonable range for target {spec.TargetHeight:F2}.";
            }

            if (Mathf.Abs(bounds.min.y) > 0.08f)
            {
                yield return $"Feet are not near local Y=0. minY={bounds.min.y:F3}.";
            }
        }

        private static void RemoveForbiddenComponents(GameObject root, List<string> warnings)
        {
            Component[] components = root.GetComponentsInChildren<Component>(true);
            foreach (Component component in components)
            {
                if (component == null || component is Transform || component is SkinnedMeshRenderer || component is MeshRenderer || component is MeshFilter)
                {
                    continue;
                }

                Type type = component.GetType();
                bool forbidden =
                    component is Camera ||
                    component is Light ||
                    component is AudioListener ||
                    component is Canvas ||
                    component is Collider ||
                    component is Rigidbody ||
                    type.FullName == "Unity.Netcode.NetworkObject" ||
                    component is MonoBehaviour;

                if (!forbidden)
                {
                    continue;
                }

                warnings.Add("Removed forbidden component from visual prefab: " + type.Name);
                UnityEngine.Object.DestroyImmediate(component);
            }
        }

        private static List<string> FindForbiddenComponents(GameObject root)
        {
            List<string> found = new List<string>();
            Component[] components = root.GetComponentsInChildren<Component>(true);
            foreach (Component component in components)
            {
                if (component == null)
                {
                    continue;
                }

                Type type = component.GetType();
                if (component is Camera ||
                    component is Light ||
                    component is AudioListener ||
                    component is Canvas ||
                    component is Collider ||
                    component is Rigidbody ||
                    type.FullName == "Unity.Netcode.NetworkObject" ||
                    component is MonoBehaviour)
                {
                    found.Add(type.Name);
                }
            }

            return found.Distinct().OrderBy(name => name, StringComparer.Ordinal).ToList();
        }

        private static bool TryGetLocalBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            bool initialized = false;
            bounds = default;
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                Bounds worldBounds = renderer.bounds;
                Vector3 localMin = root.transform.InverseTransformPoint(worldBounds.min);
                Vector3 localMax = root.transform.InverseTransformPoint(worldBounds.max);
                Bounds localBounds = new Bounds();
                localBounds.SetMinMax(Vector3.Min(localMin, localMax), Vector3.Max(localMin, localMax));

                if (!initialized)
                {
                    bounds = localBounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(localBounds);
                }
            }

            return initialized;
        }

        private static void AlignFeetToLocalZero(GameObject root, GameObject modelInstance)
        {
            if (!TryGetLocalBounds(root, out Bounds bounds))
            {
                return;
            }

            modelInstance.transform.localPosition -= Vector3.up * bounds.min.y;
        }

        private static bool IsAvatarValid(string modelPath)
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();
            return avatar != null && avatar.isValid && avatar.isHuman;
        }

        private static int CountMaterialSlots(string modelPath)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (model == null)
            {
                return 0;
            }

            return model.GetComponentsInChildren<Renderer>(true).Sum(renderer => renderer.sharedMaterials.Length);
        }

        private static void EnsureCharacterFolders(CharacterImportSpec spec)
        {
            Directory.CreateDirectory(ToAbsoluteProjectPath(spec.ModelFolder));
            Directory.CreateDirectory(ToAbsoluteProjectPath(spec.MaterialsFolder));
            Directory.CreateDirectory(ToAbsoluteProjectPath(spec.TexturesFolder));
            Directory.CreateDirectory(ToAbsoluteProjectPath(spec.LocalPrefabFolder));
        }

        private static void CopyModelIfNeeded(CharacterImportSpec spec)
        {
            if (!File.Exists(spec.SourcePath))
            {
                Debug.LogWarning($"Portal Nights source FBX missing: {spec.SourcePath}");
                return;
            }

            string target = ToAbsoluteProjectPath(spec.TargetModelPath);
            string targetDirectory = Path.GetDirectoryName(target);
            if (!string.IsNullOrEmpty(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            bool shouldCopy = !File.Exists(target) || new FileInfo(spec.SourcePath).Length != new FileInfo(target).Length;
            if (shouldCopy)
            {
                File.Copy(spec.SourcePath, target, true);
            }
        }

        private static void WriteReport(IReadOnlyList<CharacterImportReport> reports)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# Portal Nights Character Import Report");
            builder.AppendLine();
            builder.AppendLine("Generated by `PortalNightsCharacterVisualImporter`.");
            builder.AppendLine("Scope: visual assets only. No gameplay prefabs or scenes are linked by this importer.");
            builder.AppendLine();
            builder.AppendLine("| Model | Source | Target FBX | Prefab | Rig | Avatar valid | Height | Scale | Materials | Material status | Textures | Forbidden components | Warnings |");
            builder.AppendLine("|---|---|---|---|---|---:|---:|---|---:|---|---|---:|---|");
            foreach (CharacterImportReport report in reports)
            {
                builder.Append("| ");
                builder.Append(Escape(report.Spec.Name));
                builder.Append(" | ");
                builder.Append(Escape(report.Spec.SourcePath));
                builder.Append(" | ");
                builder.Append(Escape(report.TargetModelPath));
                builder.Append(" | ");
                builder.Append(Escape(report.Spec.PrefabPath));
                builder.Append(" | Humanoid / Create From This Model | ");
                builder.Append(report.AvatarValid ? "yes" : "no");
                builder.Append(" | ");
                builder.Append(report.FinalVisualHeight.ToString("F3", CultureInfo.InvariantCulture));
                builder.Append(" | ");
                builder.Append(FormatVector(report.FinalLocalScale));
                builder.Append(" | ");
                builder.Append(report.MaterialCount.ToString(CultureInfo.InvariantCulture));
                builder.Append(" | ");
                builder.Append(Escape(report.MaterialExtractionStatus));
                builder.Append(" | ");
                builder.Append(Escape(report.TextureExtractionStatus));
                builder.Append(" | ");
                builder.Append(report.HasForbiddenComponents ? "yes" : "no");
                builder.Append(" | ");
                builder.Append(Escape(report.Warnings.Count == 0 ? "none" : string.Join("; ", report.Warnings.Distinct())));
                builder.AppendLine(" |");
            }

            builder.AppendLine();
            builder.AppendLine("## Validation Rules");
            builder.AppendLine();
            builder.AppendLine("- Prefab exists.");
            builder.AppendLine("- Contains at least one `SkinnedMeshRenderer`.");
            builder.AppendLine("- Contains no `NetworkObject`, `Rigidbody`, gameplay collider, camera, audio listener, canvas, light, or script component.");
            builder.AppendLine("- Feet should be near local `Y = 0`.");
            builder.AppendLine("- Model root keeps identity rotation so gameplay can later control forward direction from the parent root.");

            Directory.CreateDirectory(ToAbsoluteProjectPath(Path.GetDirectoryName(ReportPath)?.Replace('\\', '/') ?? string.Empty));
            File.WriteAllText(ToAbsoluteProjectPath(ReportPath), builder.ToString(), Encoding.UTF8);
        }

        private static string FormatVector(Vector3 value)
        {
            return $"({value.x:F4}, {value.y:F4}, {value.z:F4})";
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("|", "\\|");
        }

        private static string MakeSafeAssetName(string value)
        {
            char[] invalidChars = Path.GetInvalidFileNameChars();
            StringBuilder builder = new StringBuilder(value.Length);
            foreach (char character in value)
            {
                builder.Append(invalidChars.Contains(character) ? '_' : character);
            }

            return builder.ToString().Trim();
        }

        private static string ToAbsoluteProjectPath(string assetPath)
        {
            string normalized = assetPath.Replace('\\', '/');
            if (Path.IsPathRooted(normalized))
            {
                return normalized;
            }

            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
            return Path.Combine(projectRoot, normalized);
        }

        private sealed class CharacterImportSpec
        {
            public CharacterImportSpec(string name, string sourcePath, string characterRoot, string prefabPath, float targetHeight)
            {
                Name = name;
                SourcePath = sourcePath;
                CharacterRoot = characterRoot;
                PrefabPath = prefabPath;
                TargetHeight = targetHeight;
            }

            public string Name { get; }
            public string SourcePath { get; }
            public string CharacterRoot { get; }
            public string PrefabPath { get; }
            public float TargetHeight { get; }
            public string ModelFolder => CharacterRoot + "/Model";
            public string MaterialsFolder => CharacterRoot + "/Materials";
            public string TexturesFolder => CharacterRoot + "/Textures";
            public string LocalPrefabFolder => CharacterRoot + "/Prefab";
            public string TargetModelPath => ModelFolder + "/" + Path.GetFileName(SourcePath).Replace('\\', '/');
        }

        private sealed class CharacterImportReport
        {
            public CharacterImportReport(CharacterImportSpec spec)
            {
                Spec = spec;
            }

            public CharacterImportSpec Spec { get; }
            public string TargetModelPath { get; set; } = string.Empty;
            public string TextureExtractionStatus { get; set; } = "Not attempted";
            public string MaterialExtractionStatus { get; set; } = "Not attempted";
            public bool AvatarValid { get; set; }
            public float FinalVisualHeight { get; set; }
            public Vector3 FinalLocalScale { get; set; } = Vector3.one;
            public int MaterialCount { get; set; }
            public bool PrefabCreated { get; set; }
            public bool HasForbiddenComponents { get; set; }
            public List<string> Warnings { get; } = new List<string>();
        }
    }
}
