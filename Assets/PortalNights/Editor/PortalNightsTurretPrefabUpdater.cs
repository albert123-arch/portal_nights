using System.IO;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEngine;

namespace PortalNights.EditorTools
{
    public static class PortalNightsTurretPrefabUpdater
    {
        private const string Root = "Assets/PortalNights";
        private const string PrefabsDir = Root + "/Prefabs";
        private const string MaterialsDir = Root + "/Materials";
        private const string TurretModelsDir = Root + "/Models/Turrets";
        private const string TurretPrefabPath = PrefabsDir + "/PN_LaserTurret.prefab";
        private const float ImportedTurretModelYawOffset = -90f;

        [MenuItem("Portal Nights/Update Turret Prefab From FBX Only")]
        public static GameObject UpdateTurretPrefabFromFbx()
        {
            Directory.CreateDirectory(PrefabsDir);
            Directory.CreateDirectory(MaterialsDir);
            Directory.CreateDirectory(TurretModelsDir);

            AssetDatabase.ImportAsset(TurretModelsDir + "/Turret lvl1.fbx", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(TurretModelsDir + "/Turret lvl2.fbx", ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(TurretModelsDir + "/Turret lvl3.fbx", ImportAssetOptions.ForceUpdate);

            Material darkMetal = CreateMaterial("PN_Turret_GlossGunmetal", new Color(0.055f, 0.071f, 0.09f), new Color(0.1f, 0.18f, 0.28f), 0.92f, 0.96f);
            Material cyan = CreateMaterial("PN_Turret_Lvl1_Cyan", new Color(0.08f, 0.72f, 1f), new Color(0.18f, 0.9f, 1f) * 3.8f, 0.55f, 0.96f);
            Material purple = CreateMaterial("PN_Turret_Lvl2_Purple", new Color(0.55f, 0.16f, 0.95f), new Color(0.85f, 0.24f, 1f) * 4.2f, 0.62f, 0.97f);
            Material amber = CreateMaterial("PN_Turret_Lvl3_Amber", new Color(1f, 0.48f, 0.08f), new Color(1f, 0.72f, 0.16f) * 4.4f, 0.68f, 0.98f);

            GameObject root = new GameObject("PN_LaserTurret");
            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            BoxCollider collider = root.AddComponent<BoxCollider>();
            collider.size = new Vector3(1.8f, 2.35f, 1.8f);
            collider.center = new Vector3(0f, 1.05f, 0f);
            PortalNightsAlly turret = root.AddComponent<PortalNightsAlly>();

            CreatePrimitive("Socket_Base", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.2f, 0f), new Vector3(1.85f, 0.32f, 1.85f), darkMetal, false);
            CreatePrimitive("Socket_Neon_Ring", PrimitiveType.Cylinder, root.transform, new Vector3(0f, 0.42f, 0f), new Vector3(1.46f, 0.08f, 1.46f), cyan, false);

            Transform head = new GameObject("RotatingHead").transform;
            head.SetParent(root.transform, false);
            head.localPosition = Vector3.zero;

            Transform[] visuals =
            {
                CreateLevelVisual(1, head, darkMetal, cyan, 1.65f, 1.9f),
                CreateLevelVisual(2, head, darkMetal, purple, 1.95f, 2.1f),
                CreateLevelVisual(3, head, darkMetal, amber, 2.18f, 2.3f)
            };

            Transform[] muzzleGroups =
            {
                CreateMuzzleGroup(1, head, cyan),
                CreateMuzzleGroup(2, head, purple),
                CreateMuzzleGroup(3, head, amber)
            };
            Transform muzzle = muzzleGroups[0].GetChild(0);

            LineRenderer beam = root.AddComponent<LineRenderer>();
            beam.enabled = false;
            beam.positionCount = 2;
            beam.widthMultiplier = 0.1f;
            beam.material = amber;
            beam.startColor = new Color(1f, 0.72f, 0.18f, 1f);
            beam.endColor = new Color(1f, 0.42f, 0.06f, 0.12f);

            Light light = root.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.3f, 0.85f, 1f);
            light.range = 5.8f;
            light.intensity = 1.6f;

            turret.ConfigureLevels(
                new[] { 18f, 20f, 22f },
                new[] { 18f, 26.1f, 39.6f },
                new[] { 0.32f, 0.267f, 0.221f },
                head,
                muzzle,
                beam,
                visuals,
                muzzleGroups,
                true);

            PrefabUtility.SaveAsPrefabAsset(root, TurretPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PortalNights] Updated turret prefab from FBX models only: " + TurretPrefabPath);
            return AssetDatabase.LoadAssetAtPath<GameObject>(TurretPrefabPath);
        }

        private static Transform CreateLevelVisual(int level, Transform parent, Material metal, Material accent, float targetHeight, float targetFootprint)
        {
            GameObject wrapper = new GameObject("Turret_Lvl" + level + "_Visual");
            wrapper.transform.SetParent(parent, false);
            wrapper.SetActive(level == 1);

            string modelPath = TurretModelsDir + "/Turret lvl" + level + ".fbx";
            GameObject modelAsset = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (modelAsset != null)
            {
                GameObject model = PrefabUtility.InstantiatePrefab(modelAsset) as GameObject;
                if (model != null)
                {
                    model.name = "FBX_Model";
                    model.transform.SetParent(wrapper.transform, false);
                    PrefabUtility.UnpackPrefabInstance(model, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
                    StripImportedBlenderObjects(model);
                    ApplyThemedMaterials(model, metal, accent);
                    RemoveColliders(model);
                    NormalizeModel(model, targetHeight, targetFootprint);
                    model.transform.localRotation = Quaternion.Euler(0f, ImportedTurretModelYawOffset, 0f);
                }
            }
            else
            {
                CreateFallbackTurret(level, wrapper.transform, metal, accent);
            }

            CreatePrimitive("Level_" + level + "_GlowRing", PrimitiveType.Cylinder, wrapper.transform, new Vector3(0f, 0.55f, 0f), new Vector3(1.35f + level * 0.18f, 0.05f, 1.35f + level * 0.18f), accent, false);
            CreatePrimitive("Level_" + level + "_PowerCore", PrimitiveType.Sphere, wrapper.transform, new Vector3(0f, 0.92f + level * 0.07f, 0.28f), new Vector3(0.22f + level * 0.03f, 0.22f + level * 0.03f, 0.22f + level * 0.03f), accent, false);

            Light levelLight = wrapper.AddComponent<Light>();
            levelLight.type = LightType.Point;
            levelLight.color = accent.HasProperty("_BaseColor") ? accent.GetColor("_BaseColor") : accent.color;
            levelLight.range = 3.6f + level * 0.7f;
            levelLight.intensity = 0.7f + level * 0.35f;

            return wrapper.transform;
        }

        private static Transform CreateMuzzleGroup(int level, Transform parent, Material accent)
        {
            Transform group = new GameObject("MuzzleGroup_Lvl" + level).transform;
            group.SetParent(parent, false);
            group.gameObject.SetActive(level == 1);

            Vector3[] muzzlePositions = GetMuzzleLocalPositions(level);

            for (int i = 0; i < muzzlePositions.Length; i++)
            {
                Transform muzzle = new GameObject("Muzzle_" + (i + 1)).transform;
                muzzle.SetParent(group, false);
                muzzle.localPosition = muzzlePositions[i];
            }

            return group;
        }

        private static Vector3[] GetMuzzleLocalPositions(int level)
        {
            if (level <= 1)
            {
                return new[] { new Vector3(0f, 0.98f, -0.36f) };
            }

            if (level == 2)
            {
                return new[]
                {
                    new Vector3(-0.17f, 1.6f, -1.1f),
                    new Vector3(0.17f, 1.6f, -1.1f)
                };
            }

            return new[]
            {
                new Vector3(-0.18f, 1.74f, -1.19f),
                new Vector3(0f, 1.74f, -1.19f),
                new Vector3(0.18f, 1.74f, -1.19f)
            };
        }

        private static void CreateFallbackTurret(int level, Transform parent, Material metal, Material accent)
        {
            CreatePrimitive("Fallback_Body", PrimitiveType.Cube, parent, new Vector3(0f, 0.98f, 0f), new Vector3(0.88f + level * 0.18f, 0.42f, 0.82f + level * 0.12f), metal, false);
            CreatePrimitive("Fallback_Barrel", PrimitiveType.Cylinder, parent, new Vector3(0f, 1f, 0.7f), new Vector3(0.16f + level * 0.03f, 0.85f + level * 0.18f, 0.16f + level * 0.03f), accent, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePrimitive("Fallback_Sight", PrimitiveType.Sphere, parent, new Vector3(0f, 1.28f, 0.12f), new Vector3(0.24f, 0.16f, 0.24f), accent, false);
        }

        private static Material CreateMaterial(string name, Color baseColor, Color emission, float metallic, float smoothness)
        {
            string path = MaterialsDir + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                material = new Material(shader == null ? Shader.Find("Standard") : shader);
                AssetDatabase.CreateAsset(material, path);
            }

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }
            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", baseColor);
            }
            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }
            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, bool keepCollider)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localScale = localScale;
            if (material != null && gameObject.TryGetComponent(out Renderer renderer))
            {
                renderer.sharedMaterial = material;
            }

            if (!keepCollider && gameObject.TryGetComponent(out Collider collider))
            {
                Object.DestroyImmediate(collider);
            }

            return gameObject;
        }

        private static void ApplyThemedMaterials(GameObject model, Material metal, Material accent)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                string rendererName = renderers[i].name.ToLowerInvariant();
                bool accentRenderer = rendererName.Contains("glow") || rendererName.Contains("light") || rendererName.Contains("emissive") || rendererName.Contains("energy") || rendererName.Contains("neon");
                Material[] materials = renderers[i].sharedMaterials;
                if (materials == null || materials.Length == 0)
                {
                    renderers[i].sharedMaterial = accentRenderer ? accent : metal;
                    continue;
                }

                for (int j = 0; j < materials.Length; j++)
                {
                    materials[j] = accentRenderer || (j == materials.Length - 1 && materials.Length > 1) ? accent : metal;
                }

                renderers[i].sharedMaterials = materials;
            }
        }

        private static void RemoveColliders(GameObject model)
        {
            Collider[] colliders = model.GetComponentsInChildren<Collider>(true);
            foreach (Collider collider in colliders)
            {
                Object.DestroyImmediate(collider);
            }
        }

        private static void StripImportedBlenderObjects(GameObject model)
        {
            Camera[] cameras = model.GetComponentsInChildren<Camera>(true);
            foreach (Camera camera in cameras)
            {
                Object.DestroyImmediate(camera.gameObject);
            }

            AudioListener[] listeners = model.GetComponentsInChildren<AudioListener>(true);
            foreach (AudioListener listener in listeners)
            {
                Object.DestroyImmediate(listener);
            }

            Light[] lights = model.GetComponentsInChildren<Light>(true);
            foreach (Light light in lights)
            {
                Object.DestroyImmediate(light.gameObject);
            }

            Transform[] children = model.GetComponentsInChildren<Transform>(true);
            for (int i = children.Length - 1; i >= 0; i--)
            {
                Transform child = children[i];
                if (child == null || child == model.transform)
                {
                    continue;
                }

                string lowerName = child.name.ToLowerInvariant();
                bool blenderHelper =
                    lowerName == "camera" ||
                    lowerName.StartsWith("camera.") ||
                    lowerName == "area" ||
                    lowerName.StartsWith("area.") ||
                    lowerName == "sun" ||
                    lowerName.StartsWith("sun.") ||
                    lowerName == "point" ||
                    lowerName.StartsWith("point.") ||
                    lowerName == "spot" ||
                    lowerName.StartsWith("spot.");

                if (blenderHelper)
                {
                    Object.DestroyImmediate(child.gameObject);
                }
            }
        }

        private static void NormalizeModel(GameObject model, float targetHeight, float targetFootprint)
        {
            model.transform.localPosition = Vector3.zero;
            model.transform.localRotation = Quaternion.identity;
            model.transform.localScale = Vector3.one;

            if (!TryGetBounds(model, out Bounds bounds))
            {
                return;
            }

            float height = Mathf.Max(0.001f, bounds.size.y);
            float footprint = Mathf.Max(0.001f, Mathf.Max(bounds.size.x, bounds.size.z));
            float scale = Mathf.Min(targetHeight / height, targetFootprint / footprint);
            model.transform.localScale = Vector3.one * scale;

            if (!TryGetBounds(model, out bounds))
            {
                return;
            }

            Vector3 offset = new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
            model.transform.position += offset;
        }

        private static bool TryGetBounds(GameObject root, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bounds = new Bounds(root.transform.position, Vector3.zero);
            bool found = false;
            foreach (Renderer renderer in renderers)
            {
                if (!found)
                {
                    bounds = renderer.bounds;
                    found = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return found;
        }
    }
}
