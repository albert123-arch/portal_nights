using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;

namespace PortalNights.EditorTools
{
    public static class PortalNightsSceneBuilder
    {
        private const string Root = "Assets/PortalNights";
        private const string MaterialsDir = Root + "/Materials";
        private const string PrefabsDir = Root + "/Prefabs";
        private const string ScenesDir = Root + "/Scenes";
        private const string ScenePath = ScenesDir + "/PortalNightsArena.unity";
        private const string NetworkPrefabsPath = Root + "/PortalNightsNetworkPrefabs.asset";
        private const int BuildPadCost = 120;

        private static Material darkMetal;
        private static Material warmMetal;
        private static Material platformMetal;
        private static Material neonBlue;
        private static Material neonPurple;
        private static Material neonOrange;
        private static Material coreBlue;
        private static Material shieldBlue;
        private static Material enemyPurple;
        private static Material bruteArmor;
        private static Material coinGold;
        private static Material glassDark;
        private static Material planet2FloorDark;
        private static Material planet2SecondaryMetal;
        private static Material planet2IndigoWall;
        private static Material planet2CyanCrystal;
        private static Material planet2VioletCrystal;
        private static Material planet2LeftPortal;
        private static Material planet2LeftPortalDormant;
        private static Material planet2RightPortal;
        private static Material planet2RightPortalDormant;
        private static Material planet2Gold;
        private static Material planet2ColdWhite;
        private static Material planet2GlassCyan;
        private static Font hudFont;

        [MenuItem("Portal Nights/Rebuild Arena Defense Scene")]
        public static void RebuildArenaDefenseScene()
        {
            EnsureFolders();
            LoadOrCreateMaterials();

            GameObject playerPrefab = CreatePlayerPrefab();
            GameObject smallEnemyPrefab = CreateEnemyPrefab("PN_Enemy_Small", PortalNightsEnemyKind.Small);
            GameObject bruteEnemyPrefab = CreateEnemyPrefab("PN_Enemy_Brute", PortalNightsEnemyKind.Brute);
            GameObject turretPrefab = CreateTurretPrefab();
            GameObject coinPrefab = CreateCoinPrefab();
            NetworkPrefabsList networkPrefabs = CreateNetworkPrefabs(playerPrefab, smallEnemyPrefab, bruteEnemyPrefab, turretPrefab, coinPrefab);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "PortalNightsArena";

            SetupLightingAndRendering();
            Transform portal = CreateArena(out PortalNightsLanePath leftLanePath, out PortalNightsLanePath rightLanePath);
            PortalNightsHealth coreHealth = CreateCore();
            PortalNightsBuildPoint[] buildPoints = CreateBuildPads();
            Transform[] spawnPoints = CreatePlayerSpawnPoints();
            PortalNightsHud hud = CreateHud();
            CreateCamera();
            CreateNetworkManager(playerPrefab, networkPrefabs);
            CreateGameController(coreHealth, hud, portal, leftLanePath, rightLanePath, spawnPoints, smallEnemyPrefab, bruteEnemyPrefab, turretPrefab, coinPrefab);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AddSceneToBuildSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[PortalNights] Arena defense scene rebuilt at " + ScenePath);
        }

        public static void BuildFromCommandLine()
        {
            RebuildArenaDefenseScene();
        }

        [MenuItem("Portal Nights/Rebuild Planet 2 Crystal Moon Only")]
        public static void BuildPlanet2CrystalMoon()
        {
            EnsureFolders();
            LoadOrCreateMaterials();

            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                if (!File.Exists(ScenePath))
                {
                    Debug.LogError("[PortalNights] Portal Nights scene not found at " + ScenePath);
                    return;
                }

                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            GameObject arenaObject = GameObject.Find("PortalNightsArena");
            if (arenaObject == null)
            {
                Debug.LogError("[PortalNights] PortalNightsArena root was not found. Planet 1 was not modified.");
                return;
            }

            Transform existingPlanet2 = arenaObject.transform.Find("Planet2_CrystalMoon");
            if (existingPlanet2 != null)
            {
                Object.DestroyImmediate(existingPlanet2.gameObject);
            }

            Transform planet2Root = CreateChild(arenaObject.transform, "Planet2_CrystalMoon");
            planet2Root.localPosition = new Vector3(0f, 0f, 92f);
            planet2Root.localRotation = Quaternion.identity;
            planet2Root.localScale = Vector3.one;

            BuildPlanet2CrystalMoonHierarchy(planet2Root);
            UpdatePlanet2ControllerDefaults(planet2Root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[PortalNights] Planet 2 Crystal Moon rebuilt only under PortalNightsArena/Planet2_CrystalMoon. Footprint: 110 x 90 units.");
        }

        [MenuItem("Portal Nights/Capture Planet 2 Crystal Moon Screenshots")]
        public static void CapturePlanet2CrystalMoonScreenshots()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                if (!File.Exists(ScenePath))
                {
                    Debug.LogError("[PortalNights] Portal Nights scene not found at " + ScenePath);
                    return;
                }

                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            Transform root = GameObject.Find("PortalNightsArena")?.transform.Find("Planet2_CrystalMoon");
            if (root == null)
            {
                Debug.LogError("[PortalNights] Planet2_CrystalMoon was not found. Build the map before capturing screenshots.");
                return;
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string captureDirectory = Path.Combine(projectRoot ?? Application.dataPath, "Logs/PortalNightsCaptures");
            Directory.CreateDirectory(captureDirectory);
            CapturePlanet2View(root, Path.Combine(captureDirectory, "planet2_arrival_zone.png"), new Vector3(0f, 13f, -53f), new Vector3(0f, 3.8f, 28f), 66f);
            CapturePlanet2View(root, Path.Combine(captureDirectory, "planet2_central_sphere.png"), new Vector3(0f, 14f, -7f), new Vector3(0f, 3.8f, 34f), 68f);
            Debug.Log("[PortalNights] Planet 2 screenshots saved to " + captureDirectory);
        }

        private static void CapturePlanet2View(Transform root, string path, Vector3 localCameraPosition, Vector3 localLookAt, float fieldOfView)
        {
            GameObject cameraObject = new GameObject("PN_TempPlanet2CaptureCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = HexColor("070B1C");
            camera.fieldOfView = fieldOfView;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 220f;
            camera.transform.position = root.TransformPoint(localCameraPosition);
            Vector3 target = root.TransformPoint(localLookAt);
            camera.transform.rotation = Quaternion.LookRotation((target - camera.transform.position).normalized, Vector3.up);

            RenderTexture renderTexture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            Texture2D screenshot = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            camera.targetTexture = renderTexture;
            RenderTexture.active = renderTexture;
            camera.Render();
            screenshot.ReadPixels(new Rect(0f, 0f, 1920f, 1080f), 0, 0);
            screenshot.Apply();
            File.WriteAllBytes(path, screenshot.EncodeToPNG());
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            Object.DestroyImmediate(screenshot);
            Object.DestroyImmediate(renderTexture);
            Object.DestroyImmediate(cameraObject);
        }

        private static void EnsureFolders()
        {
            foreach (string path in new[] { Root, MaterialsDir, PrefabsDir, ScenesDir, Root + "/References", Root + "/Editor", Root + "/Scripts" })
            {
                Directory.CreateDirectory(path);
            }
        }

        private static void LoadOrCreateMaterials()
        {
            darkMetal = CreateMaterial("PN_DarkMetal", new Color(0.08f, 0.095f, 0.12f, 1f), Color.black, 0.75f, 0.72f);
            warmMetal = CreateMaterial("PN_WarmGunmetal", new Color(0.28f, 0.24f, 0.2f, 1f), new Color(0.08f, 0.04f, 0.02f, 1f), 0.7f, 0.6f);
            platformMetal = CreateMaterial("PN_PlatformMetal", new Color(0.18f, 0.2f, 0.23f, 1f), new Color(0.01f, 0.02f, 0.03f, 1f), 0.86f, 0.58f);
            neonBlue = CreateMaterial("PN_NeonBlue", new Color(0.04f, 0.58f, 1f, 1f), new Color(0.08f, 1.4f, 2.6f, 1f), 0f, 0.9f);
            neonPurple = CreateMaterial("PN_NeonPurple", new Color(0.62f, 0.08f, 1f, 1f), new Color(1.3f, 0.08f, 2.7f, 1f), 0f, 0.9f);
            neonOrange = CreateMaterial("PN_NeonOrange", new Color(1f, 0.42f, 0.08f, 1f), new Color(2.5f, 0.78f, 0.08f, 1f), 0f, 0.75f);
            coreBlue = CreateMaterial("PN_CoreBlue", new Color(0.12f, 0.75f, 1f, 1f), new Color(0.12f, 1.4f, 2.9f, 1f), 0.25f, 0.94f);
            shieldBlue = CreateMaterial("PN_ShieldBlue", new Color(0.15f, 0.72f, 1f, 0.23f), new Color(0.15f, 1.25f, 2.7f, 1f), 0f, 0.92f, true);
            enemyPurple = CreateMaterial("PN_EnemyPurple", new Color(0.22f, 0.05f, 0.34f, 1f), new Color(0.75f, 0.05f, 1.35f, 1f), 0.15f, 0.48f);
            bruteArmor = CreateMaterial("PN_BruteArmor", new Color(0.34f, 0.22f, 0.16f, 1f), new Color(0.9f, 0.18f, 1.05f, 1f), 0.65f, 0.62f);
            coinGold = CreateMaterial("PN_CoinGold", new Color(1f, 0.72f, 0.12f, 1f), new Color(2.5f, 1.35f, 0.18f, 1f), 0.55f, 0.82f);
            glassDark = CreateMaterial("PN_GlassDark", new Color(0.03f, 0.055f, 0.09f, 0.7f), new Color(0.02f, 0.12f, 0.22f, 1f), 0.1f, 0.95f, true);
            planet2FloorDark = CreateMaterial("PN_P2_FloorDark", HexColor("141A2A"), new Color(0.01f, 0.014f, 0.035f, 1f), 0.55f, 0.62f);
            planet2SecondaryMetal = CreateMaterial("PN_P2_SecondaryMetal", HexColor("26324A"), new Color(0.02f, 0.035f, 0.07f, 1f), 0.78f, 0.7f);
            planet2IndigoWall = CreateMaterial("PN_P2_DarkIndigo", HexColor("070B1C"), new Color(0.005f, 0.01f, 0.035f, 1f), 0.5f, 0.56f);
            planet2CyanCrystal = CreateMaterial("PN_P2_CyanCrystal", HexColor("62F4FF"), new Color(0.44f, 2.5f, 3.2f, 1f), 0f, 0.92f);
            planet2VioletCrystal = CreateMaterial("PN_P2_VioletCrystal", HexColor("A448FF"), new Color(1.45f, 0.5f, 3.2f, 1f), 0f, 0.9f);
            planet2LeftPortal = CreateMaterial("PN_P2_LeftPortal_Active", HexColor("A448FF"), new Color(1.45f, 0.5f, 3.45f, 1f), 0f, 0.95f);
            planet2LeftPortalDormant = CreateMaterial("PN_P2_LeftPortal_Dormant", HexColor("5C2C8E"), new Color(0.36f, 0.12f, 0.75f, 1f), 0f, 0.82f);
            planet2RightPortal = CreateMaterial("PN_P2_RightPortal_Active", HexColor("FF783C"), new Color(3.2f, 1.0f, 0.25f, 1f), 0f, 0.92f);
            planet2RightPortalDormant = CreateMaterial("PN_P2_RightPortal_Dormant", HexColor("9A4A27"), new Color(0.8f, 0.28f, 0.08f, 1f), 0f, 0.8f);
            planet2Gold = CreateMaterial("PN_P2_RightPortal_Gold", HexColor("FFD05A"), new Color(3.1f, 1.95f, 0.45f, 1f), 0.18f, 0.86f);
            planet2ColdWhite = CreateMaterial("PN_P2_ColdWhite", new Color(0.86f, 0.98f, 1f, 1f), new Color(1.3f, 2.4f, 2.55f, 1f), 0.05f, 0.88f);
            planet2GlassCyan = CreateMaterial("PN_P2_GlassCyan", new Color(0.38f, 0.96f, 1f, 0.28f), new Color(0.24f, 1.8f, 2.2f, 1f), 0f, 0.94f, true);
        }

        private static Color HexColor(string hex)
        {
            if (!hex.StartsWith("#"))
            {
                hex = "#" + hex;
            }

            return ColorUtility.TryParseHtmlString(hex, out Color color) ? color : Color.white;
        }

        private static Material CreateMaterial(string name, Color baseColor, Color emission, float metallic, float smoothness, bool transparent = false)
        {
            string path = MaterialsDir + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null)
                {
                    shader = Shader.Find("Standard");
                }
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.name = name;
            material.color = baseColor;
            SetColor(material, "_BaseColor", baseColor);
            SetFloat(material, "_Metallic", metallic);
            SetFloat(material, "_Smoothness", smoothness);
            SetFloat(material, "_Glossiness", smoothness);
            if (emission.maxColorComponent > 0.001f)
            {
                material.EnableKeyword("_EMISSION");
                SetColor(material, "_EmissionColor", emission);
            }

            if (transparent)
            {
                material.SetFloat("_Surface", 1f);
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0f);
                material.renderQueue = (int)RenderQueue.Transparent;
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static void SetColor(Material material, string property, Color value)
        {
            if (material.HasProperty(property))
            {
                material.SetColor(property, value);
            }
        }

        private static void SetFloat(Material material, string property, float value)
        {
            if (material.HasProperty(property))
            {
                material.SetFloat(property, value);
            }
        }

        private static GameObject CreatePlayerPrefab()
        {
            GameObject root = new GameObject("PN_PlayerHero");
            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            CharacterController controller = root.AddComponent<CharacterController>();
            controller.radius = 0.42f;
            controller.height = 1.85f;
            controller.center = new Vector3(0f, 0.95f, 0f);
            PortalNightsHealth health = root.AddComponent<PortalNightsHealth>();
            health.SetBaseMaxHealth(250f);
            PortalNightsPlayerController player = root.AddComponent<PortalNightsPlayerController>();

            Transform visualRoot = new GameObject("VisualRoot").transform;
            visualRoot.SetParent(root.transform, false);
            visualRoot.localPosition = Vector3.zero;
            visualRoot.localRotation = Quaternion.identity;

            Transform body = CreatePrimitive("Body_Armor", PrimitiveType.Capsule, visualRoot, new Vector3(0f, 0.92f, 0f), new Vector3(0.78f, 0.92f, 0.78f), darkMetal, false).transform;
            CreatePrimitive("Chest_Neon", PrimitiveType.Cube, body, new Vector3(0f, 0.42f, 0.42f), new Vector3(0.48f, 0.12f, 0.05f), neonBlue, false);
            CreatePrimitive("Visor", PrimitiveType.Cube, visualRoot, new Vector3(0f, 1.88f, 0.26f), new Vector3(0.42f, 0.11f, 0.05f), neonBlue, false);
            CreatePrimitive("Head", PrimitiveType.Sphere, visualRoot, new Vector3(0f, 1.76f, 0f), new Vector3(0.46f, 0.46f, 0.46f), warmMetal, false);
            CreatePrimitive("Left_Shoulder", PrimitiveType.Cube, visualRoot, new Vector3(-0.62f, 1.38f, 0f), new Vector3(0.42f, 0.24f, 0.42f), platformMetal, false);
            CreatePrimitive("Right_Shoulder", PrimitiveType.Cube, visualRoot, new Vector3(0.62f, 1.38f, 0f), new Vector3(0.42f, 0.24f, 0.42f), platformMetal, false);
            CreatePrimitive("Backpack_Glow", PrimitiveType.Cube, visualRoot, new Vector3(0f, 1.16f, -0.44f), new Vector3(0.55f, 0.7f, 0.16f), neonBlue, false);

            GameObject rifle = CreatePrimitive("Assault_Rifle", PrimitiveType.Cube, visualRoot, new Vector3(0.55f, 1.25f, 0.68f), new Vector3(0.18f, 0.16f, 1.05f), warmMetal, false);
            CreatePrimitive("Rifle_Core", PrimitiveType.Cube, rifle.transform, new Vector3(0f, 0f, 0.08f), new Vector3(0.22f, 0.22f, 0.34f), neonBlue, false);
            CreatePrimitive("Muzzle_Glow_Visual", PrimitiveType.Sphere, rifle.transform, new Vector3(0f, 0f, 0.62f), new Vector3(0.13f, 0.13f, 0.13f), neonBlue, false);
            Transform cameraAnchor = new GameObject("CameraAnchor").transform;
            cameraAnchor.SetParent(root.transform, false);
            cameraAnchor.localPosition = new Vector3(0f, 1.55f, 0.35f);

            SetSerializedObject(player, "cameraAnchor", cameraAnchor);
            SetSerializedObject(player, "visualRoot", visualRoot);
            SetSerializedFloat(player, "visualYawOffset", 0f);

            SavePrefab(root, PrefabsDir + "/PN_PlayerHero.prefab");
            return PortalNightsPlayerPrefabUpdater.UpdatePlayerHeroFromShooterVisual();
        }

        private static GameObject CreateEnemyPrefab(string prefabName, PortalNightsEnemyKind kind)
        {
            bool brute = kind == PortalNightsEnemyKind.Brute;
            GameObject root = new GameObject(prefabName);
            root.AddComponent<NetworkObject>();
            root.AddComponent<NetworkTransform>();
            CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
            collider.radius = brute ? 0.68f : 0.38f;
            collider.height = brute ? 2.35f : 1.45f;
            collider.center = new Vector3(0f, collider.height * 0.5f, 0f);
            PortalNightsHealth health = root.AddComponent<PortalNightsHealth>();
            health.SetBaseMaxHealth(brute ? 230f : 74f);
            PortalNightsEnemy enemy = root.AddComponent<PortalNightsEnemy>();
            enemy.ConfigurePrefab(kind, brute ? 230f : 74f, brute ? 1.78f : 2.75f, brute ? 25f : 11f, brute ? 42 : 18);

            Material main = brute ? bruteArmor : enemyPurple;
            CreatePrimitive("Body", PrimitiveType.Capsule, root.transform, new Vector3(0f, brute ? 1.12f : 0.72f, 0f), brute ? new Vector3(1.28f, 1.25f, 1.28f) : new Vector3(0.72f, 0.74f, 0.72f), main, false);
            CreatePrimitive("HeadGlow", PrimitiveType.Sphere, root.transform, new Vector3(0f, brute ? 2.25f : 1.47f, 0.08f), brute ? new Vector3(0.72f, 0.62f, 0.72f) : new Vector3(0.42f, 0.38f, 0.42f), neonPurple, false);
            CreatePrimitive("LeftClaw", PrimitiveType.Cube, root.transform, new Vector3(brute ? -0.86f : -0.48f, brute ? 1.2f : 0.8f, 0.2f), brute ? new Vector3(0.28f, 0.75f, 0.25f) : new Vector3(0.17f, 0.45f, 0.16f), neonPurple, false);
            CreatePrimitive("RightClaw", PrimitiveType.Cube, root.transform, new Vector3(brute ? 0.86f : 0.48f, brute ? 1.2f : 0.8f, 0.2f), brute ? new Vector3(0.28f, 0.75f, 0.25f) : new Vector3(0.17f, 0.45f, 0.16f), neonPurple, false);
            Transform aimPoint = new GameObject("AimPoint").transform;
            aimPoint.SetParent(root.transform, false);
            aimPoint.localPosition = new Vector3(0f, brute ? 1.72f : 1.05f, 0f);
            SetSerializedObject(enemy, "aimPoint", aimPoint);

            return SavePrefab(root, PrefabsDir + "/" + prefabName + ".prefab");
        }

        private static GameObject CreateTurretPrefab()
        {
            return PortalNightsTurretPrefabUpdater.UpdateTurretPrefabFromFbx();
        }

        private static GameObject CreateCoinPrefab()
        {
            GameObject root = new GameObject("PN_CoinPickup");
            root.AddComponent<NetworkObject>();
            root.AddComponent<PortalNightsCoinPickup>();
            GameObject coin = CreatePrimitive("Coin", PrimitiveType.Cylinder, root.transform, Vector3.zero, new Vector3(0.55f, 0.08f, 0.55f), coinGold, false);
            coin.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            CreatePrimitive("CoinCore", PrimitiveType.Sphere, root.transform, Vector3.zero, new Vector3(0.28f, 0.28f, 0.28f), neonOrange, false);
            return SavePrefab(root, PrefabsDir + "/PN_CoinPickup.prefab");
        }

        private static NetworkPrefabsList CreateNetworkPrefabs(params GameObject[] prefabs)
        {
            AssetDatabase.DeleteAsset(NetworkPrefabsPath);
            NetworkPrefabsList list = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            AssetDatabase.CreateAsset(list, NetworkPrefabsPath);
            foreach (GameObject prefab in prefabs)
            {
                if (prefab != null)
                {
                    list.Add(new NetworkPrefab { Prefab = prefab });
                }
            }

            EditorUtility.SetDirty(list);
            return list;
        }

        private static void SetupLightingAndRendering()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.27f, 0.38f, 0.52f);
            RenderSettings.ambientEquatorColor = new Color(0.12f, 0.16f, 0.22f);
            RenderSettings.ambientGroundColor = new Color(0.035f, 0.04f, 0.06f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.06f, 0.1f, 0.18f);
            RenderSettings.fogDensity = 0.012f;

            Material skybox = AssetDatabase.LoadAssetAtPath<Material>(MaterialsDir + "/PN_ProceduralSkybox.mat");
            if (skybox == null)
            {
                skybox = new Material(Shader.Find("Skybox/Procedural"));
                AssetDatabase.CreateAsset(skybox, MaterialsDir + "/PN_ProceduralSkybox.mat");
            }
            skybox.SetColor("_SkyTint", new Color(0.15f, 0.36f, 0.64f));
            skybox.SetColor("_GroundColor", new Color(0.03f, 0.045f, 0.08f));
            skybox.SetFloat("_Exposure", 1.25f);
            RenderSettings.skybox = skybox;

            GameObject sun = new GameObject("PN_Sun_KeyLight");
            Light sunLight = sun.AddComponent<Light>();
            sunLight.type = LightType.Directional;
            sunLight.color = new Color(0.78f, 0.9f, 1f);
            sunLight.intensity = 1.65f;
            sun.transform.rotation = Quaternion.Euler(44f, -32f, 0f);

            GameObject volumeObject = new GameObject("PN_GlobalVolume_Bloom");
            Volume volume = volumeObject.AddComponent<Volume>();
            volume.isGlobal = true;
            string volumeProfilePath = Root + "/Data_PortalNightsVolumeProfile.asset";
            AssetDatabase.DeleteAsset(volumeProfilePath);
            VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, volumeProfilePath);
            Bloom bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.78f);
            bloom.threshold.Override(0.68f);
            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.contrast.Override(18f);
            color.saturation.Override(9f);
            color.postExposure.Override(0.12f);
            Vignette vignette = profile.Add<Vignette>(true);
            vignette.intensity.Override(0.23f);
            vignette.smoothness.Override(0.55f);
            volume.profile = profile;
        }

        private static void BuildPlanet2CrystalMoonHierarchy(Transform root)
        {
            Transform environment = CreateChild(root, "Environment");
            Transform floors = CreateChild(environment, "Floors");
            Transform boundaries = CreateChild(environment, "Boundaries");
            Transform crystals = CreateChild(environment, "Crystals");
            Transform background = CreateChild(environment, "Background");
            Transform arrivalZone = CreateChild(root, "ArrivalZone");
            Transform centralPlaza = CreateChild(root, "CentralPlaza");
            Transform leftLane = CreateChild(root, "LeftLane");
            Transform rightLane = CreateChild(root, "RightLane");
            Transform leftPortal = CreateChild(root, "LeftEnemyPortal");
            Transform rightPortal = CreateChild(root, "RightEnemyPortal");
            Transform rewardPoints = CreateChild(root, "FutureRewardPoints");

            CreatePlanet2Floors(floors);
            CreatePlanet2Boundaries(boundaries);
            CreatePlanet2Background(background);
            CreatePlanet2Arrival(arrivalZone, root);
            CreatePlanet2CentralPlaza(centralPlaza, root);
            CreatePlanet2Lanes(leftLane, rightLane);
            CreatePlanet2EnemyPortal(leftPortal, "Left", new Vector3(-42f, 2.5f, 34f), planet2LeftPortal, planet2LeftPortalDormant, planet2VioletCrystal, new Color(0.64f, 0.28f, 1f));
            CreatePlanet2EnemyPortal(rightPortal, "Right", new Vector3(42f, 2.5f, 34f), planet2RightPortal, planet2RightPortalDormant, planet2Gold, new Color(1f, 0.47f, 0.2f));
            CreatePlanet2EnemySpawnMarkers(root);
            CreatePlanet2Crystals(crystals);
            CreatePlanet2RewardMarkers(rewardPoints);
            CreatePlanet2Lighting(root);
        }

        private static void CreatePlanet2Floors(Transform floors)
        {
            CreatePrimitive("CrystalMoon_ContinuousWalkableDeck_110x90", PrimitiveType.Cube, floors, new Vector3(0f, -0.22f, -3f), new Vector3(110f, 0.44f, 90f), planet2FloorDark, true);
            CreatePrimitive("CrystalMoon_Deck_Skirt", PrimitiveType.Cube, floors, new Vector3(0f, -0.94f, -3f), new Vector3(113f, 1.15f, 93f), planet2IndigoWall, false);
            CreatePrimitive("ArrivalZone_MetalPlate_18x14", PrimitiveType.Cube, floors, new Vector3(0f, 0.045f, -40f), new Vector3(18f, 0.1f, 14f), planet2SecondaryMetal, false);
            CreatePrimitive("ArrivalZone_GlowInset", PrimitiveType.Cube, floors, new Vector3(0f, 0.115f, -40f), new Vector3(13.5f, 0.055f, 0.28f), planet2CyanCrystal, false);
            CreatePrimitive("ArrivalZone_BackGlowInset", PrimitiveType.Cube, floors, new Vector3(0f, 0.12f, -46.1f), new Vector3(14.5f, 0.055f, 0.22f), planet2VioletCrystal, false);
            CreatePrimitive("CentralPlaza_Disc_40", PrimitiveType.Cylinder, floors, new Vector3(0f, 0.02f, 8f), new Vector3(40f, 0.13f, 40f), planet2SecondaryMetal, false);
            CreatePrimitive("CentralPlaza_InnerCyanRing", PrimitiveType.Cylinder, floors, new Vector3(0f, 0.11f, 8f), new Vector3(27f, 0.055f, 27f), planet2FloorDark, false);
            CreatePrimitive("CentralPlaza_CoreGlow", PrimitiveType.Cylinder, floors, new Vector3(0f, 0.17f, 8f), new Vector3(13f, 0.045f, 13f), planet2CyanCrystal, false);

            CreatePlanet2Path(floors, "ArrivalRoute", new[]
            {
                new Vector3(0f, 0.12f, -40f),
                new Vector3(0f, 0.12f, -22f),
                new Vector3(0f, 0.12f, -4f),
                new Vector3(0f, 0.12f, 8f)
            }, 12f, planet2SecondaryMetal, planet2CyanCrystal);

            CreatePlanet2Path(floors, "LeftEnemyLane_Surface", Planet2LeftRoute(), 11.5f, planet2SecondaryMetal, planet2VioletCrystal);
            CreatePlanet2Path(floors, "RightEnemyLane_Surface", Planet2RightRoute(), 11.5f, planet2SecondaryMetal, planet2RightPortal);
            CreatePlanet2Path(floors, "CentralCrossRoute", new[]
            {
                new Vector3(-34f, 0.14f, 22f),
                new Vector3(-16f, 0.14f, 20f),
                new Vector3(0f, 0.14f, 20f),
                new Vector3(16f, 0.14f, 20f),
                new Vector3(34f, 0.14f, 22f)
            }, 9.5f, planet2SecondaryMetal, planet2ColdWhite);

            CreatePlanet2FloorGrid(floors);
        }

        private static void CreatePlanet2FloorGrid(Transform floors)
        {
            for (int i = -5; i <= 5; i++)
            {
                float x = i * 10f;
                CreatePrimitive("Deck_Subtle_Longitudinal_Glow_" + i, PrimitiveType.Cube, floors, new Vector3(x, 0.035f, -3f), new Vector3(0.08f, 0.045f, 84f), i % 2 == 0 ? planet2CyanCrystal : planet2VioletCrystal, false);
            }

            for (int i = -4; i <= 4; i++)
            {
                float z = -3f + i * 10f;
                CreatePrimitive("Deck_Subtle_Cross_Glow_" + i, PrimitiveType.Cube, floors, new Vector3(0f, 0.04f, z), new Vector3(100f, 0.045f, 0.08f), i % 2 == 0 ? planet2SecondaryMetal : planet2IndigoWall, false);
            }
        }

        private static void CreatePlanet2Boundaries(Transform boundaries)
        {
            CreateBoundaryWall(boundaries, "North_VisibleCliffWall", new Vector3(0f, 1.12f, 42.2f), new Vector3(112f, 2.25f, 1.4f), planet2SecondaryMetal, planet2CyanCrystal);
            CreateBoundaryWall(boundaries, "South_VisibleCliffWall", new Vector3(0f, 1.12f, -48.2f), new Vector3(112f, 2.25f, 1.4f), planet2SecondaryMetal, planet2VioletCrystal);
            CreateBoundaryWall(boundaries, "West_VisibleCliffWall", new Vector3(-55.2f, 1.12f, -3f), new Vector3(1.4f, 2.25f, 91f), planet2SecondaryMetal, planet2VioletCrystal);
            CreateBoundaryWall(boundaries, "East_VisibleCliffWall", new Vector3(55.2f, 1.12f, -3f), new Vector3(1.4f, 2.25f, 91f), planet2SecondaryMetal, planet2RightPortal);
            CreatePrimitive("North_Cliff_DropShadow", PrimitiveType.Cube, boundaries, new Vector3(0f, -1.35f, 44f), new Vector3(115f, 2.2f, 3.8f), planet2IndigoWall, false);
            CreatePrimitive("South_Cliff_DropShadow", PrimitiveType.Cube, boundaries, new Vector3(0f, -1.35f, -50f), new Vector3(115f, 2.2f, 3.8f), planet2IndigoWall, false);
            CreatePrimitive("West_Cliff_DropShadow", PrimitiveType.Cube, boundaries, new Vector3(-57f, -1.35f, -3f), new Vector3(3.8f, 2.2f, 94f), planet2IndigoWall, false);
            CreatePrimitive("East_Cliff_DropShadow", PrimitiveType.Cube, boundaries, new Vector3(57f, -1.35f, -3f), new Vector3(3.8f, 2.2f, 94f), planet2IndigoWall, false);
        }

        private static void CreateBoundaryWall(Transform parent, string name, Vector3 position, Vector3 scale, Material wallMaterial, Material glowMaterial)
        {
            GameObject wall = CreatePrimitive(name, PrimitiveType.Cube, parent, position, scale, wallMaterial, true);
            Vector3 glowScale = scale.x > scale.z
                ? new Vector3(scale.x - 6f, 0.11f, 0.14f)
                : new Vector3(0.14f, 0.11f, scale.z - 6f);
            Vector3 glowOffset = scale.x > scale.z ? new Vector3(0f, 0.72f, -0.72f) : new Vector3(-0.72f, 0.72f, 0f);
            CreatePrimitive(name + "_ReadableEdgeGlow", PrimitiveType.Cube, wall.transform, glowOffset, glowScale, glowMaterial, false);
        }

        private static void CreatePlanet2Background(Transform background)
        {
            CreatePrimitive("DarkIndigo_Sky_Canopy", PrimitiveType.Cube, background, new Vector3(0f, 34f, -3f), new Vector3(130f, 0.6f, 105f), planet2IndigoWall, false);
            CreatePrimitive("Far_North_Indigo_Backdrop", PrimitiveType.Cube, background, new Vector3(0f, 15f, 52f), new Vector3(128f, 30f, 1.1f), planet2IndigoWall, false);
            CreatePrimitive("Far_West_Indigo_Backdrop", PrimitiveType.Cube, background, new Vector3(-66f, 14f, -3f), new Vector3(1.1f, 28f, 100f), planet2IndigoWall, false);
            CreatePrimitive("Far_East_Indigo_Backdrop", PrimitiveType.Cube, background, new Vector3(66f, 14f, -3f), new Vector3(1.1f, 28f, 100f), planet2IndigoWall, false);
        }

        private static void CreatePlanet2Arrival(Transform arrivalZone, Transform root)
        {
            Transform playerArrival = CreateChild(arrivalZone, "PlayerArrivalPoint");
            playerArrival.localPosition = new Vector3(0f, 1.12f, -40f);
            Transform allyArrival = CreateChild(arrivalZone, "AllyArrivalPoint");
            allyArrival.localPosition = new Vector3(2.2f, 1.12f, -40f);
            Transform compatibilityArrival = CreateChild(root, "ArrivalPoint");
            compatibilityArrival.localPosition = playerArrival.localPosition;
            CreatePrimitive("ArrivalZone_LowGate_Left", PrimitiveType.Cube, arrivalZone, new Vector3(-9.2f, 0.75f, -40f), new Vector3(0.45f, 1.5f, 12.5f), planet2SecondaryMetal, true);
            CreatePrimitive("ArrivalZone_LowGate_Right", PrimitiveType.Cube, arrivalZone, new Vector3(9.2f, 0.75f, -40f), new Vector3(0.45f, 1.5f, 12.5f), planet2SecondaryMetal, true);
            CreateWorldLabel(arrivalZone, "ARRIVAL ZONE", new Vector3(0f, 0.95f, -44f), new Color(0.68f, 0.96f, 1f));
        }

        private static void CreatePlanet2CentralPlaza(Transform centralPlaza, Transform root)
        {
            CreatePrimitive("SphereRing_OuterColdWhite", PrimitiveType.Cylinder, centralPlaza, new Vector3(0f, 0.24f, 8f), new Vector3(15.5f, 0.1f, 15.5f), planet2ColdWhite, false);
            CreatePrimitive("SphereRing_InnerDarkMetal", PrimitiveType.Cylinder, centralPlaza, new Vector3(0f, 0.31f, 8f), new Vector3(10.2f, 0.1f, 10.2f), planet2FloorDark, false);

            GameObject sphereObjective = new GameObject("SphereObjective");
            sphereObjective.transform.SetParent(root, false);
            sphereObjective.transform.localPosition = new Vector3(0f, 0f, 8f);
            sphereObjective.AddComponent<NetworkObject>();
            PortalNightsHealth sphereHealth = sphereObjective.AddComponent<PortalNightsHealth>();
            sphereHealth.SetBaseMaxHealth(500f);
            SphereCollider collider = sphereObjective.AddComponent<SphereCollider>();
            collider.center = new Vector3(0f, 1.45f, 0f);
            collider.radius = 1.55f;
            collider.isTrigger = true;

            CreatePrimitive("Sphere_Pedestal", PrimitiveType.Cylinder, sphereObjective.transform, new Vector3(0f, 0.32f, 0f), new Vector3(5.8f, 0.62f, 5.8f), planet2SecondaryMetal, false);
            CreatePrimitive("Sphere_Core_Cyan", PrimitiveType.Sphere, sphereObjective.transform, new Vector3(0f, 1.45f, 0f), new Vector3(2.7f, 2.7f, 2.7f), planet2CyanCrystal, false);
            CreatePrimitive("Sphere_Shield_Glass", PrimitiveType.Sphere, sphereObjective.transform, new Vector3(0f, 1.45f, 0f), new Vector3(5.2f, 5.2f, 5.2f), planet2GlassCyan, false);

            Light sphereLight = sphereObjective.AddComponent<Light>();
            sphereLight.type = LightType.Point;
            sphereLight.color = HexColor("62F4FF");
            sphereLight.range = 28f;
            sphereLight.intensity = 7.2f;

            Transform utilityPads = CreateChild(centralPlaza, "UtilityBuildPads");
            CreateVisualBuildPad(utilityPads, "SphereUtilityPad_Left", new Vector3(-11f, 0.26f, 3.5f), planet2CyanCrystal);
            CreateVisualBuildPad(utilityPads, "SphereUtilityPad_Right", new Vector3(11f, 0.26f, 3.5f), planet2VioletCrystal);
            CreateWorldLabel(centralPlaza, "CRYSTAL SPHERE", new Vector3(0f, 1.1f, -2.5f), new Color(0.75f, 0.98f, 1f));
        }

        private static void CreatePlanet2Lanes(Transform leftLane, Transform rightLane)
        {
            Transform leftPath = CreateChild(leftLane, "Path");
            Transform leftWaypoints = CreateChild(leftLane, "Waypoints");
            Transform leftPads = CreateChild(leftLane, "BuildPads");
            Transform leftDecorations = CreateChild(leftLane, "Decorations");
            Transform rightPath = CreateChild(rightLane, "Path");
            Transform rightWaypoints = CreateChild(rightLane, "Waypoints");
            Transform rightPads = CreateChild(rightLane, "BuildPads");
            Transform rightDecorations = CreateChild(rightLane, "Decorations");

            Vector3[] leftRoute = Planet2LeftRoute();
            Vector3[] rightRoute = Planet2RightRoute();
            CreatePlanet2Path(leftPath, "LeftLane_ReadableRoute", leftRoute, 10.8f, planet2SecondaryMetal, planet2VioletCrystal);
            CreatePlanet2Path(rightPath, "RightLane_ReadableRoute", rightRoute, 10.8f, planet2SecondaryMetal, planet2RightPortal);
            CreatePlanet2Waypoints(leftWaypoints, "Left", leftRoute, planet2VioletCrystal);
            CreatePlanet2Waypoints(rightWaypoints, "Right", rightRoute, planet2RightPortal);

            CreateVisualBuildPad(leftPads, "LeftLane_TurretPad_01", new Vector3(-15f, 0.28f, 13f), planet2VioletCrystal);
            CreateVisualBuildPad(leftPads, "LeftLane_TurretPad_02", new Vector3(-27f, 0.28f, 25f), planet2VioletCrystal);
            CreateVisualBuildPad(leftPads, "LeftLane_TurretPad_03", new Vector3(-42f, 0.28f, 27.5f), planet2VioletCrystal);
            CreateVisualBuildPad(rightPads, "RightLane_TurretPad_01", new Vector3(15f, 0.28f, 13f), planet2RightPortal);
            CreateVisualBuildPad(rightPads, "RightLane_TurretPad_02", new Vector3(27f, 0.28f, 25f), planet2RightPortal);
            CreateVisualBuildPad(rightPads, "RightLane_TurretPad_03", new Vector3(42f, 0.28f, 27.5f), planet2RightPortal);

            CreateWorldLabel(leftLane, "LEFT CRYSTAL LANE", new Vector3(-23f, 1.1f, 20f), new Color(0.77f, 0.5f, 1f));
            CreateWorldLabel(rightLane, "RIGHT SOLAR LANE", new Vector3(23f, 1.1f, 20f), new Color(1f, 0.76f, 0.34f));
            CreateLaneDecoration(leftDecorations, -1f, planet2VioletCrystal);
            CreateLaneDecoration(rightDecorations, 1f, planet2RightPortal);
        }

        private static Vector3[] Planet2LeftRoute()
        {
            return new[]
            {
                new Vector3(0f, 0.16f, 8f),
                new Vector3(-10f, 0.16f, 15f),
                new Vector3(-24f, 0.16f, 28f),
                new Vector3(-36f, 0.16f, 25f),
                new Vector3(-42f, 0.16f, 34f)
            };
        }

        private static Vector3[] Planet2RightRoute()
        {
            return new[]
            {
                new Vector3(0f, 0.16f, 8f),
                new Vector3(10f, 0.16f, 15f),
                new Vector3(24f, 0.16f, 28f),
                new Vector3(36f, 0.16f, 25f),
                new Vector3(42f, 0.16f, 34f)
            };
        }

        private static void CreatePlanet2Path(Transform parent, string name, Vector3[] points, float width, Material plateMaterial, Material accentMaterial)
        {
            for (int i = 0; i < points.Length - 1; i++)
            {
                CreatePlanet2PathSegment(name + "_Plate_" + (i + 1).ToString("00"), parent, points[i], points[i + 1], width, 0.08f, plateMaterial, false, 0f);
                CreatePlanet2PathSegment(name + "_CenterGlow_" + (i + 1).ToString("00"), parent, points[i] + Vector3.up * 0.07f, points[i + 1] + Vector3.up * 0.07f, 0.38f, 0.055f, accentMaterial, false, 0f);
                CreatePlanet2PathSegment(name + "_LeftEdgeGlow_" + (i + 1).ToString("00"), parent, points[i] + Vector3.up * 0.08f, points[i + 1] + Vector3.up * 0.08f, 0.2f, 0.05f, accentMaterial, false, -width * 0.42f);
                CreatePlanet2PathSegment(name + "_RightEdgeGlow_" + (i + 1).ToString("00"), parent, points[i] + Vector3.up * 0.08f, points[i + 1] + Vector3.up * 0.08f, 0.2f, 0.05f, accentMaterial, false, width * 0.42f);
            }
        }

        private static GameObject CreatePlanet2PathSegment(string name, Transform parent, Vector3 start, Vector3 end, float width, float thickness, Material material, bool keepCollider, float sideOffset)
        {
            Vector3 flatDirection = PortalNightsMath.Flat(end - start);
            float length = flatDirection.magnitude;
            Vector3 direction = length <= 0.001f ? Vector3.forward : flatDirection / length;
            Vector3 side = Vector3.Cross(Vector3.up, direction).normalized * sideOffset;
            Vector3 midpoint = (start + end) * 0.5f + side;
            GameObject segment = CreatePrimitive(name, PrimitiveType.Cube, parent, midpoint, new Vector3(width, thickness, length), material, keepCollider);
            segment.transform.localRotation = Quaternion.LookRotation(direction, Vector3.up);
            return segment;
        }

        private static void CreatePlanet2Waypoints(Transform parent, string laneName, Vector3[] points, Material markerMaterial)
        {
            for (int i = 0; i < points.Length; i++)
            {
                Transform waypoint = CreateChild(parent, laneName + "_Waypoint_" + (i + 1).ToString("00"));
                waypoint.localPosition = points[i] + Vector3.up * 0.24f;
                CreatePrimitive("WaypointMarker", PrimitiveType.Cylinder, waypoint, Vector3.zero, new Vector3(0.65f, 0.04f, 0.65f), markerMaterial, false);
            }
        }

        private static void CreateVisualBuildPad(Transform parent, string name, Vector3 position, Material accent)
        {
            GameObject pad = new GameObject(name);
            pad.transform.SetParent(parent, false);
            pad.transform.localPosition = position;
            Vector3 facing = PortalNightsMath.Flat(new Vector3(0f, 0f, 8f) - position);
            pad.transform.localRotation = facing.sqrMagnitude <= 0.001f ? Quaternion.identity : Quaternion.LookRotation(facing.normalized, Vector3.up);
            pad.AddComponent<NetworkObject>();
            BoxCollider collider = pad.AddComponent<BoxCollider>();
            collider.size = new Vector3(4.8f, 0.55f, 4.8f);
            collider.center = new Vector3(0f, 0.18f, 0f);

            CreatePrimitive("Pad_Base", PrimitiveType.Cylinder, pad.transform, Vector3.zero, new Vector3(4.8f, 0.22f, 4.8f), planet2SecondaryMetal, false);
            Renderer ring = CreatePrimitive("Pad_Emissive_Ring", PrimitiveType.Cylinder, pad.transform, new Vector3(0f, 0.16f, 0f), new Vector3(4.25f, 0.08f, 4.25f), accent, false).GetComponent<Renderer>();
            CreatePrimitive("Pad_Center_DarkSocket", PrimitiveType.Cylinder, pad.transform, new Vector3(0f, 0.27f, 0f), new Vector3(2.1f, 0.12f, 2.1f), planet2FloorDark, false);
            Transform socket = CreateChild(pad.transform, "TurretSocket");
            socket.localPosition = new Vector3(0f, 0.58f, 0f);

            Color lightColor = Color.cyan;
            if (accent != null)
            {
                if (accent.HasProperty("_BaseColor"))
                {
                    lightColor = accent.GetColor("_BaseColor");
                }
                else if (accent.HasProperty("_Color"))
                {
                    lightColor = accent.GetColor("_Color");
                }
            }

            Light light = pad.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 6.5f;
            light.intensity = 1.65f;
            light.color = lightColor;

            PortalNightsBuildPoint buildPoint = pad.AddComponent<PortalNightsBuildPoint>();
            buildPoint.Configure(BuildPadCost, 180, 260, socket, ring, light, false);
        }

        private static void CreateLaneDecoration(Transform parent, float sideSign, Material accent)
        {
            for (int i = 0; i < 5; i++)
            {
                float z = 8f + i * 7f;
                float x = sideSign * (18f + i * 6f);
                CreatePrimitive("Lane_LowRail_" + i, PrimitiveType.Cube, parent, new Vector3(x, 0.55f, z), new Vector3(0.45f, 1.1f, 4.8f), planet2SecondaryMetal, true);
                CreatePrimitive("Lane_RailGlow_" + i, PrimitiveType.Cube, parent, new Vector3(x - sideSign * 0.26f, 1.18f, z), new Vector3(0.12f, 0.16f, 3.8f), accent, false);
            }
        }

        private static void CreatePlanet2EnemyPortal(Transform portalRoot, string side, Vector3 localPosition, Material activeMaterial, Material dormantMaterial, Material secondaryAccent, Color lightColor)
        {
            portalRoot.localPosition = localPosition;
            Vector3 toSphere = PortalNightsMath.Flat(new Vector3(0f, localPosition.y, 8f) - localPosition);
            portalRoot.localRotation = toSphere.sqrMagnitude <= 0.001f ? Quaternion.identity : Quaternion.LookRotation(toSphere.normalized, Vector3.up);
            CreatePrimitive(side + "_PortalPlatform_16", PrimitiveType.Cylinder, portalRoot, new Vector3(0f, -2.42f, 0f), new Vector3(16f, 0.24f, 16f), planet2SecondaryMetal, false);
            CreatePrimitive(side + "_PortalPlatform_GlowRing", PrimitiveType.Cylinder, portalRoot, new Vector3(0f, -2.22f, 0f), new Vector3(13.6f, 0.075f, 13.6f), secondaryAccent, false);
            CreatePrimitive(side + "_PortalSurface_Active", PrimitiveType.Sphere, portalRoot, Vector3.zero, new Vector3(8.4f, 8.4f, 0.34f), activeMaterial, false);
            GameObject dormantSurface = CreatePrimitive(side + "_PortalSurface_DormantSupport", PrimitiveType.Sphere, portalRoot, new Vector3(0f, 0f, -0.18f), new Vector3(8.6f, 8.6f, 0.16f), dormantMaterial, false);
            dormantSurface.SetActive(false);

            for (int i = 0; i < 28; i++)
            {
                float angle = i * Mathf.PI * 2f / 28f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 5.25f, Mathf.Sin(angle) * 5.25f, -0.34f);
                GameObject segment = CreatePrimitive(side + "_PortalRing_Segment_" + i.ToString("00"), PrimitiveType.Cube, portalRoot, position, new Vector3(0.58f, 0.88f, 0.72f), i % 4 == 0 ? secondaryAccent : planet2SecondaryMetal, false);
                segment.transform.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
            }

            CreatePrimitive(side + "_Portal_LeftPylon", PrimitiveType.Cube, portalRoot, new Vector3(-6.4f, 0.7f, -0.52f), new Vector3(1.05f, 10.8f, 1.35f), planet2SecondaryMetal, true);
            CreatePrimitive(side + "_Portal_RightPylon", PrimitiveType.Cube, portalRoot, new Vector3(6.4f, 0.7f, -0.52f), new Vector3(1.05f, 10.8f, 1.35f), planet2SecondaryMetal, true);
            CreatePrimitive(side + "_Portal_TopCap", PrimitiveType.Cube, portalRoot, new Vector3(0f, 6.2f, -0.52f), new Vector3(12f, 1.05f, 1.35f), planet2SecondaryMetal, true);
            CreatePrimitive(side + "_Portal_Beacon_27m", PrimitiveType.Cylinder, portalRoot, new Vector3(0f, 13.5f, -0.2f), new Vector3(0.55f, 13.5f, 0.55f), activeMaterial, false);

            Transform marker = CreateChild(portalRoot, "WorldSpaceMarker");
            marker.localPosition = Vector3.zero;
            CreatePrimitive("Marker_GlowPin", PrimitiveType.Sphere, marker, new Vector3(0f, 6.8f, -0.1f), new Vector3(0.85f, 0.85f, 0.85f), secondaryAccent, false);

            Transform spawns = CreateChild(portalRoot, "SpawnPoints");
            for (int i = 0; i < 2; i++)
            {
                Transform spawn = CreateChild(spawns, side + "_PortalSpawn_" + (i + 1).ToString("00"));
                spawn.localPosition = new Vector3(i == 0 ? -1.8f : 1.8f, -2.15f, -3.5f);
            }

            Light portalLight = portalRoot.gameObject.AddComponent<Light>();
            portalLight.type = LightType.Point;
            portalLight.color = lightColor;
            portalLight.range = 32f;
            portalLight.intensity = 8.5f;

            ParticleSystem particles = portalRoot.gameObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.75f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.42f);
            main.startColor = lightColor;
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 42f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 4.1f;
            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = activeMaterial;
        }

        private static void CreatePlanet2EnemySpawnMarkers(Transform root)
        {
            Transform spawnRoot = CreateChild(root, "EnemySpawnPoints");
            Vector3[] spawnPositions =
            {
                new Vector3(-42f, 0.35f, 30f),
                new Vector3(-37.5f, 0.35f, 32f),
                new Vector3(42f, 0.35f, 30f),
                new Vector3(37.5f, 0.35f, 32f)
            };
            string[] names =
            {
                "LeftPortal_Spawn_A",
                "LeftPortal_Spawn_B",
                "RightPortal_Spawn_A",
                "RightPortal_Spawn_B"
            };

            for (int i = 0; i < spawnPositions.Length; i++)
            {
                Transform spawn = CreateChild(spawnRoot, names[i]);
                spawn.localPosition = spawnPositions[i];
                CreatePrimitive("SpawnMarker_Glow", PrimitiveType.Cylinder, spawn, Vector3.zero, new Vector3(0.95f, 0.05f, 0.95f), i < 2 ? planet2VioletCrystal : planet2RightPortal, false);
            }
        }

        private static void CreatePlanet2Crystals(Transform crystals)
        {
            Vector3[] anchors =
            {
                new Vector3(-48f, 0.25f, -35f),
                new Vector3(48f, 0.25f, -35f),
                new Vector3(-50f, 0.25f, 10f),
                new Vector3(50f, 0.25f, 10f),
                new Vector3(-30f, 0.25f, 39f),
                new Vector3(30f, 0.25f, 39f),
                new Vector3(-8f, 0.25f, -31f),
                new Vector3(8f, 0.25f, -31f)
            };

            for (int i = 0; i < anchors.Length; i++)
            {
                CreateCrystalCluster(crystals, "CrystalCluster_" + i.ToString("00"), anchors[i], i % 2 == 0 ? planet2CyanCrystal : planet2VioletCrystal);
            }
        }

        private static void CreateCrystalCluster(Transform parent, string name, Vector3 anchor, Material material)
        {
            Transform cluster = CreateChild(parent, name);
            cluster.localPosition = anchor;
            for (int i = 0; i < 4; i++)
            {
                float angle = i * 88f + 14f;
                Vector3 position = new Vector3(Mathf.Cos(angle * Mathf.Deg2Rad) * (0.45f + i * 0.16f), 0.85f + i * 0.18f, Mathf.Sin(angle * Mathf.Deg2Rad) * (0.45f + i * 0.16f));
                GameObject crystal = CreatePrimitive("CrystalShard_" + i, PrimitiveType.Cylinder, cluster, position, new Vector3(0.42f, 1.1f + i * 0.32f, 0.42f), material, false);
                crystal.transform.localRotation = Quaternion.Euler(8f + i * 3f, angle, 11f - i * 2f);
            }
        }

        private static void CreatePlanet2RewardMarkers(Transform parent)
        {
            CreatePrimitive("FutureRewardPoint_Left", PrimitiveType.Cylinder, parent, new Vector3(-6.5f, 0.2f, -10f), new Vector3(2.2f, 0.1f, 2.2f), planet2ColdWhite, false);
            CreatePrimitive("FutureRewardPoint_Right", PrimitiveType.Cylinder, parent, new Vector3(6.5f, 0.2f, -10f), new Vector3(2.2f, 0.1f, 2.2f), planet2ColdWhite, false);
        }

        private static void CreatePlanet2Lighting(Transform root)
        {
            GameObject fill = new GameObject("CrystalMoon_ReadabilityFill");
            fill.transform.SetParent(root, false);
            fill.transform.localPosition = new Vector3(0f, 16f, -12f);
            Light fillLight = fill.AddComponent<Light>();
            fillLight.type = LightType.Point;
            fillLight.color = new Color(0.35f, 0.55f, 0.75f);
            fillLight.range = 85f;
            fillLight.intensity = 1.2f;

            GameObject mistObject = new GameObject("CrystalMoon_SubtleLocalMist");
            mistObject.transform.SetParent(root, false);
            mistObject.transform.localPosition = new Vector3(0f, 0.4f, -1f);
            ParticleSystem mist = mistObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = mist.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(6f, 10f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.8f, 4.4f);
            main.startColor = new Color(0.3f, 0.78f, 1f, 0.08f);
            ParticleSystem.EmissionModule emission = mist.emission;
            emission.rateOverTime = 10f;
            ParticleSystem.ShapeModule shape = mist.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(104f, 1f, 84f);
            ParticleSystemRenderer renderer = mist.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = planet2GlassCyan;
        }

        private static void UpdatePlanet2ControllerDefaults(Transform planet2Root)
        {
            PortalNightsGameController controller = Object.FindFirstObjectByType<PortalNightsGameController>();
            if (controller == null)
            {
                return;
            }

            SerializedObject serializedController = new SerializedObject(controller);
            SetSerializedVector3(serializedController, "planet2Center", planet2Root.position);
            SetSerializedFloat(serializedController, "planet2Radius", 76f);
            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void SetSerializedVector3(SerializedObject serializedObject, string fieldName, Vector3 value)
        {
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property != null)
            {
                property.vector3Value = value;
            }
        }

        private static void SetSerializedFloat(SerializedObject serializedObject, string fieldName, float value)
        {
            SerializedProperty property = serializedObject.FindProperty(fieldName);
            if (property != null)
            {
                property.floatValue = value;
            }
        }

        private static Transform CreateArena(out PortalNightsLanePath leftLanePath, out PortalNightsLanePath rightLanePath)
        {
            GameObject arena = new GameObject("PortalNightsArena");
            Transform environment = CreateChild(arena.transform, "Environment");
            Transform portalArea = CreateChild(arena.transform, "PortalArea");
            Transform entranceBridge = CreateChild(arena.transform, "EntranceBridge");
            Transform laneFork = CreateChild(arena.transform, "LaneFork");
            Transform leftLane = CreateChild(arena.transform, "LeftLane");
            Transform rightLane = CreateChild(arena.transform, "RightLane");
            Transform coreArena = CreateChild(arena.transform, "CoreArena");
            Transform background = CreateChild(arena.transform, "Background");
            Transform vfx = CreateChild(arena.transform, "VFX");

            CreatePrimitive("CoreArena_Metal_Disc", PrimitiveType.Cylinder, coreArena, new Vector3(0f, 0.35f, -2.6f), new Vector3(24f, 0.7f, 24f), platformMetal, true);
            CreatePrimitive("PlayerRingPath_Outer", PrimitiveType.Cylinder, coreArena, new Vector3(0f, 0.74f, -2.6f), new Vector3(17.4f, 0.13f, 17.4f), darkMetal, false);
            CreatePrimitive("PlayerRingPath_InnerGlow", PrimitiveType.Cylinder, coreArena, new Vector3(0f, 0.83f, -2.6f), new Vector3(11.2f, 0.06f, 11.2f), neonBlue, false);
            CreatePrimitive("LaneFork_Turntable", PrimitiveType.Cylinder, laneFork, new Vector3(0f, 0.72f, 11f), new Vector3(10.2f, 0.2f, 10.2f), darkMetal, true);
            CreatePrimitive("Fork_Divider_Spine", PrimitiveType.Cube, laneFork, new Vector3(0f, 1.02f, 5.1f), new Vector3(0.42f, 0.62f, 9.2f), neonPurple, false);

            Vector3[] sharedEntrance =
            {
                new Vector3(0f, 0.96f, 29.2f),
                new Vector3(0f, 0.96f, 21.2f),
                new Vector3(0f, 0.96f, 11f)
            };
            Vector3[] leftPoints =
            {
                sharedEntrance[0],
                sharedEntrance[1],
                sharedEntrance[2],
                new Vector3(-5.8f, 0.96f, 7f),
                new Vector3(-10.2f, 0.96f, 1f),
                new Vector3(-8.8f, 0.96f, -5f),
                new Vector3(-3.8f, 0.96f, -8.2f),
                new Vector3(-2.2f, 0.96f, -3f)
            };
            Vector3[] rightPoints =
            {
                sharedEntrance[0],
                sharedEntrance[1],
                sharedEntrance[2],
                new Vector3(5.8f, 0.96f, 7f),
                new Vector3(10.2f, 0.96f, 1f),
                new Vector3(8.8f, 0.96f, -5f),
                new Vector3(3.8f, 0.96f, -8.2f),
                new Vector3(2.2f, 0.96f, -3f)
            };

            CreatePathSurface("EntranceBridge", entranceBridge, sharedEntrance, 6.2f, neonPurple);
            CreatePathSurface("LeftLane_Surface", leftLane, leftPoints, 5.2f, neonBlue);
            CreatePathSurface("RightLane_Surface", rightLane, rightPoints, 5.2f, neonOrange);
            leftLanePath = CreateLanePath(leftLane, PortalNightsLane.Left, leftPoints, new Color(0.1f, 0.72f, 1f, 0.95f));
            rightLanePath = CreateLanePath(rightLane, PortalNightsLane.Right, rightPoints, new Color(1f, 0.52f, 0.12f, 0.95f));

            CreateLaneRails(leftLane, leftPoints, -1f, neonBlue);
            CreateLaneRails(rightLane, rightPoints, 1f, neonOrange);
            CreateWorldLabel(leftLane, "LEFT LANE", new Vector3(-7.2f, 1.55f, 7.4f), new Color(0.45f, 0.92f, 1f));
            CreateWorldLabel(rightLane, "RIGHT LANE", new Vector3(7.2f, 1.55f, 7.4f), new Color(1f, 0.66f, 0.28f));

            for (int i = 0; i < 36; i++)
            {
                float angle = i * Mathf.PI * 2f / 36f;
                Vector3 position = new Vector3(Mathf.Sin(angle) * 13.3f, 0.84f, -2.6f + Mathf.Cos(angle) * 13.3f);
                GameObject strip = CreatePrimitive("CoreRing_Neon_Strip_" + i, PrimitiveType.Cube, coreArena, position, new Vector3(0.15f, 0.05f, 1.55f), i % 4 == 0 ? neonOrange : neonBlue, false);
                strip.transform.localRotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);
            }

            for (int i = 0; i < 8; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float z = 19f - i * 3.4f;
                CreatePrimitive("Bridge_Support_" + i, PrimitiveType.Cube, environment, new Vector3(side * 4.1f, 0.1f, z), new Vector3(0.55f, 1.8f, 1.2f), darkMetal, false);
            }

            Transform portal = CreatePortal(portalArea);
            CreateBackgroundStructures(background);
            CreateAmbientVfx(vfx);
            return portal;
        }

        private static Transform CreatePortal(Transform parent)
        {
            GameObject portalRoot = new GameObject("PN_Glowing_Portal");
            portalRoot.transform.SetParent(parent, false);
            portalRoot.transform.localPosition = new Vector3(0f, 0.8f, 29.2f);
            portalRoot.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            for (int i = 0; i < 28; i++)
            {
                float angle = i * Mathf.PI * 2f / 28f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 4.5f, Mathf.Sin(angle) * 4.5f + 4.4f, 0f);
                GameObject segment = CreatePrimitive("Portal_Ring_" + i, PrimitiveType.Cube, portalRoot.transform, position, new Vector3(0.58f, 0.78f, 0.85f), i % 4 == 0 ? neonOrange : darkMetal, false);
                segment.transform.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg);
            }

            CreatePrimitive("Portal_Energy_Disc", PrimitiveType.Sphere, portalRoot.transform, new Vector3(0f, 4.4f, 0.08f), new Vector3(7.4f, 7.4f, 0.18f), neonPurple, false);
            CreatePrimitive("Portal_Inner_Blue", PrimitiveType.Sphere, portalRoot.transform, new Vector3(0f, 4.4f, 0.02f), new Vector3(4.4f, 4.4f, 0.12f), neonBlue, false);
            CreatePrimitive("Portal_Base_Platform", PrimitiveType.Cylinder, portalRoot.transform, new Vector3(0f, -0.18f, 0.25f), new Vector3(8.8f, 0.34f, 5.6f), darkMetal, true);
            CreatePrimitive("Portal_Frame_OuterArch", PrimitiveType.Cube, portalRoot.transform, new Vector3(0f, 4.4f, -0.35f), new Vector3(8.9f, 9.7f, 0.75f), glassDark, false);
            CreatePrimitive("Left_Pylon", PrimitiveType.Cube, portalRoot.transform, new Vector3(-5.6f, 3.2f, -0.1f), new Vector3(1.15f, 7.2f, 1.4f), darkMetal, true);
            CreatePrimitive("Right_Pylon", PrimitiveType.Cube, portalRoot.transform, new Vector3(5.6f, 3.2f, -0.1f), new Vector3(1.15f, 7.2f, 1.4f), darkMetal, true);
            CreatePrimitive("Top_Pylon", PrimitiveType.Cube, portalRoot.transform, new Vector3(0f, 8.15f, -0.1f), new Vector3(7.4f, 1f, 1.4f), darkMetal, true);

            Light portalLight = portalRoot.AddComponent<Light>();
            portalLight.type = LightType.Point;
            portalLight.color = new Color(0.88f, 0.2f, 1f);
            portalLight.intensity = 7.6f;
            portalLight.range = 22f;

            ParticleSystem particles = portalRoot.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.8f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 1.25f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.28f);
            main.startColor = new Color(0.85f, 0.18f, 1f, 0.86f);
            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 125f;
            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 3.1f;
            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.material = neonPurple;

            return portalRoot.transform;
        }

        private static PortalNightsHealth CreateCore()
        {
            GameObject core = new GameObject("PN_Central_Core");
            core.transform.position = new Vector3(0f, 0.82f, -2.6f);
            core.AddComponent<NetworkObject>();
            PortalNightsHealth health = core.AddComponent<PortalNightsHealth>();
            health.SetBaseMaxHealth(900f);
            SphereCollider collider = core.AddComponent<SphereCollider>();
            collider.radius = 2.2f;
            collider.center = Vector3.up * 1.2f;

            CreatePrimitive("Core_Pedestal_Lower", PrimitiveType.Cylinder, core.transform, new Vector3(0f, 0.04f, 0f), new Vector3(5.4f, 0.35f, 5.4f), darkMetal, false);
            CreatePrimitive("Core_Pedestal_Upper", PrimitiveType.Cylinder, core.transform, new Vector3(0f, 0.32f, 0f), new Vector3(4.2f, 0.38f, 4.2f), platformMetal, false);
            CreatePrimitive("Core_Base", PrimitiveType.Cylinder, core.transform, new Vector3(0f, 0.58f, 0f), new Vector3(3.8f, 0.28f, 3.8f), darkMetal, false);
            CreatePrimitive("Core_Reactor", PrimitiveType.Sphere, core.transform, new Vector3(0f, 1.55f, 0f), new Vector3(2.1f, 2.1f, 2.1f), coreBlue, false);
            CreatePrimitive("Core_Shield", PrimitiveType.Sphere, core.transform, new Vector3(0f, 1.55f, 0f), new Vector3(3.55f, 3.55f, 3.55f), shieldBlue, false);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                Vector3 position = new Vector3(Mathf.Sin(angle) * 2.4f, 0.72f, Mathf.Cos(angle) * 2.4f);
                GameObject fin = CreatePrimitive("Core_Energy_Fin_" + i, PrimitiveType.Cube, core.transform, position, new Vector3(0.14f, 1.1f, 0.42f), neonBlue, false);
                fin.transform.localRotation = Quaternion.Euler(0f, angle * Mathf.Rad2Deg, 0f);
            }

            Light coreLight = core.AddComponent<Light>();
            coreLight.type = LightType.Point;
            coreLight.color = new Color(0.18f, 0.76f, 1f);
            coreLight.intensity = 6.4f;
            coreLight.range = 15f;
            return health;
        }

        private static PortalNightsBuildPoint[] CreateBuildPads()
        {
            List<PortalNightsBuildPoint> pads = new List<PortalNightsBuildPoint>();
            GameObject padsRoot = new GameObject("PN_BuildPads");
            Vector3[] positions =
            {
                new Vector3(-5.4f, 0.98f, 8.2f),
                new Vector3(-12.1f, 0.98f, 1.4f),
                new Vector3(-7.8f, 0.98f, -8f),
                new Vector3(5.4f, 0.98f, 8.2f),
                new Vector3(12.1f, 0.98f, 1.4f),
                new Vector3(7.8f, 0.98f, -8f),
                new Vector3(-4.2f, 0.98f, -0.6f),
                new Vector3(4.2f, 0.98f, -0.6f)
            };
            string[] names =
            {
                "LeftLane_BuildPad_01",
                "LeftLane_BuildPad_02",
                "LeftLane_BuildPad_03",
                "RightLane_BuildPad_01",
                "RightLane_BuildPad_02",
                "RightLane_BuildPad_03",
                "CoreUtility_BuildPad_Left",
                "CoreUtility_BuildPad_Right"
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject pad = new GameObject(names[i]);
                pad.transform.SetParent(padsRoot.transform, false);
                pad.transform.position = positions[i];
                Vector3 facing = PortalNightsMath.Flat(new Vector3(0f, 0f, 2f) - positions[i]);
                pad.transform.rotation = facing.sqrMagnitude <= 0.001f ? Quaternion.identity : Quaternion.LookRotation(facing.normalized, Vector3.up);
                pad.AddComponent<NetworkObject>();
                BoxCollider collider = pad.AddComponent<BoxCollider>();
                collider.size = new Vector3(2.6f, 0.4f, 2.6f);
                collider.center = Vector3.up * 0.1f;
                Material padAccent = i < 3 ? neonBlue : i < 6 ? neonOrange : neonPurple;
                Renderer ring = CreatePrimitive("Pad_Ring", PrimitiveType.Cylinder, pad.transform, Vector3.zero, new Vector3(2.9f, 0.14f, 2.9f), padAccent, false).GetComponent<Renderer>();
                CreatePrimitive("Pad_Base", PrimitiveType.Cylinder, pad.transform, new Vector3(0f, -0.08f, 0f), new Vector3(3.5f, 0.22f, 3.5f), darkMetal, false);
                Transform socket = new GameObject("TurretSocket").transform;
                socket.SetParent(pad.transform, false);
                socket.localPosition = new Vector3(0f, 0.36f, 0f);
                Light light = pad.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 5.4f;
                light.color = new Color(0.1f, 0.74f, 1f);
                light.intensity = 1.8f;
                PortalNightsBuildPoint buildPoint = pad.AddComponent<PortalNightsBuildPoint>();
                buildPoint.Configure(BuildPadCost, socket, ring, light);
                pads.Add(buildPoint);
            }

            return pads.ToArray();
        }

        private static Transform[] CreatePlayerSpawnPoints()
        {
            List<Transform> points = new List<Transform>();
            Vector3[] positions =
            {
                new Vector3(-2.2f, 1.2f, -13.5f),
                new Vector3(2.2f, 1.2f, -13.5f),
                new Vector3(-5.8f, 1.2f, -11.1f),
                new Vector3(5.8f, 1.2f, -11.1f),
                new Vector3(-9f, 1.2f, -7.5f),
                new Vector3(9f, 1.2f, -7.5f)
            };

            for (int i = 0; i < positions.Length; i++)
            {
                GameObject point = new GameObject("PN_PlayerSpawn_" + (i + 1));
                point.transform.position = positions[i];
                point.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
                points.Add(point.transform);
            }

            return points.ToArray();
        }

        private static PortalNightsHud CreateHud()
        {
            hudFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (hudFont == null)
            {
                hudFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            GameObject canvasObject = new GameObject("PN_HUD_Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            PortalNightsHud hud = canvasObject.AddComponent<PortalNightsHud>();

            Text wave = CreateText(canvasObject.transform, "WaveText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(430f, 62f), 42, TextAnchor.MiddleCenter, Color.white, FontStyle.BoldAndItalic);
            Text enemies = CreateText(canvasObject.transform, "EnemiesText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(460f, 38f), 25, TextAnchor.MiddleCenter, new Color(1f, 0.58f, 0.2f), FontStyle.Bold);
            Text lanes = CreateText(canvasObject.transform, "LaneText", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(560f, 32f), 21, TextAnchor.MiddleCenter, new Color(0.72f, 0.96f, 1f), FontStyle.Bold);
            Text coins = CreateText(canvasObject.transform, "CoinsText", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-155f, -52f), new Vector2(250f, 52f), 34, TextAnchor.MiddleRight, new Color(1f, 0.84f, 0.26f), FontStyle.Bold);
            Text core = CreateText(canvasObject.transform, "CoreText", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-210f, -156f), new Vector2(340f, 38f), 25, TextAnchor.MiddleRight, Color.white, FontStyle.Bold);
            Slider coreSlider = CreateSlider(canvasObject.transform, "CoreHealth", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-230f, -205f), new Vector2(320f, 24f), new Color(0.38f, 1f, 0.18f), new Color(0.05f, 0.2f, 0.28f, 0.9f));
            Text player = CreateText(canvasObject.transform, "PlayerText", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(176f, 116f), new Vector2(350f, 38f), 26, TextAnchor.MiddleLeft, Color.white, FontStyle.Bold);
            Slider playerSlider = CreateSlider(canvasObject.transform, "PlayerHealth", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(190f, 74f), new Vector2(320f, 24f), new Color(0.35f, 1f, 0.24f), new Color(0.06f, 0.18f, 0.14f, 0.88f));
            Text prompt = CreateText(canvasObject.transform, "PromptText", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 138f), new Vector2(620f, 44f), 29, TextAnchor.MiddleCenter, new Color(0.92f, 0.98f, 1f), FontStyle.Bold);
            Text toast = CreateText(canvasObject.transform, "ToastText", new Vector2(0.5f, 0.74f), new Vector2(0.5f, 0.74f), Vector2.zero, new Vector2(720f, 46f), 28, TextAnchor.MiddleCenter, new Color(0.9f, 0.98f, 1f), FontStyle.Bold);

            SetSerializedObject(hud, "waveText", wave);
            SetSerializedObject(hud, "enemiesText", enemies);
            SetSerializedObject(hud, "laneText", lanes);
            SetSerializedObject(hud, "coinsText", coins);
            SetSerializedObject(hud, "coreText", core);
            SetSerializedObject(hud, "playerText", player);
            SetSerializedObject(hud, "promptText", prompt);
            SetSerializedObject(hud, "toastText", toast);
            SetSerializedObject(hud, "coreSlider", coreSlider);
            SetSerializedObject(hud, "playerSlider", playerSlider);

            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();

            return hud;
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 62f;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 420f;
            cameraObject.AddComponent<AudioListener>();
            UniversalAdditionalCameraData data = cameraObject.AddComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = true;
            cameraObject.transform.position = new Vector3(0f, 9f, -18f);
            cameraObject.transform.rotation = Quaternion.Euler(24f, 0f, 0f);
        }

        private static void CreateNetworkManager(GameObject playerPrefab, NetworkPrefabsList networkPrefabs)
        {
            GameObject networkObject = new GameObject("NetworkManager");
            NetworkManager manager = networkObject.AddComponent<NetworkManager>();
            UnityTransport transport = networkObject.AddComponent<UnityTransport>();
            if (manager.NetworkConfig == null)
            {
                manager.NetworkConfig = new NetworkConfig();
            }

            manager.NetworkConfig.NetworkTransport = transport;
            manager.NetworkConfig.PlayerPrefab = playerPrefab;
            manager.NetworkConfig.TickRate = 30;
            manager.NetworkConfig.EnableSceneManagement = false;
            manager.NetworkConfig.ConnectionApproval = false;
            manager.NetworkConfig.AutoSpawnPlayerPrefabClientSide = true;
            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Clear();
            manager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(networkPrefabs);
        }

        private static void CreateGameController(PortalNightsHealth coreHealth, PortalNightsHud hud, Transform portal, PortalNightsLanePath leftLanePath, PortalNightsLanePath rightLanePath, Transform[] spawns, GameObject smallEnemy, GameObject bruteEnemy, GameObject turret, GameObject coin)
        {
            GameObject controllerObject = new GameObject("PN_GameController");
            controllerObject.AddComponent<NetworkObject>();
            PortalNightsGameController controller = controllerObject.AddComponent<PortalNightsGameController>();
            SetSerializedObject(controller, "coreHealth", coreHealth);
            SetSerializedObject(controller, "hud", hud);
            SetSerializedObject(controller, "portalSpawn", portal);
            SetSerializedObject(controller, "leftLanePath", leftLanePath);
            SetSerializedObject(controller, "rightLanePath", rightLanePath);
            SetSerializedObject(controller, "playerSpawnPoints", spawns);
            SetSerializedObject(controller, "smallEnemyPrefab", smallEnemy);
            SetSerializedObject(controller, "bruteEnemyPrefab", bruteEnemy);
            SetSerializedObject(controller, "turretPrefab", turret);
            SetSerializedObject(controller, "coinPickupPrefab", coin);
        }

        private static void CreateBackgroundStructures(Transform parent)
        {
            for (int i = 0; i < 16; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float z = -10f + i * 2.9f;
                float height = 1.6f + (i % 4) * 0.65f;
                GameObject wall = CreatePrimitive("Arena_WallPanel_" + i, PrimitiveType.Cube, parent, new Vector3(side * 18.5f, 1.2f + height * 0.5f, z), new Vector3(1.2f, height, 5.2f), darkMetal, true);
                wall.transform.localRotation = Quaternion.Euler(0f, side > 0f ? -10f : 10f, 0f);
                CreatePrimitive("Wall_Neon_" + i, PrimitiveType.Cube, wall.transform, new Vector3(0f, 0.25f, 0f), new Vector3(1.04f, 0.08f, 0.72f), i % 3 == 0 ? neonOrange : neonBlue, false);
            }
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void CreatePathSurface(string prefix, Transform parent, Vector3[] points, float width, Material routeMaterial)
        {
            int startSegment = prefix.Contains("Lane") ? 2 : 0;
            for (int i = startSegment; i < points.Length - 1; i++)
            {
                CreateSegment(prefix + "_Floor_" + i, parent, points[i], points[i + 1], width, 0.22f, darkMetal, true, 0f);
                CreateSegment(prefix + "_LeftRouteLight_" + i, parent, points[i], points[i + 1], 0.11f, 0.055f, routeMaterial, false, -width * 0.28f);
                CreateSegment(prefix + "_RightRouteLight_" + i, parent, points[i], points[i + 1], 0.11f, 0.055f, routeMaterial, false, width * 0.28f);
            }
        }

        private static void CreateLaneRails(Transform parent, Vector3[] points, float supportSide, Material accent)
        {
            for (int i = 2; i < points.Length - 1; i++)
            {
                CreateSegment("Lane_Rail_Outer_" + i, parent, points[i], points[i + 1], 0.18f, 0.34f, platformMetal, false, 2.95f * supportSide);
                CreateSegment("Lane_Rail_Inner_" + i, parent, points[i], points[i + 1], 0.12f, 0.22f, accent, false, 1.95f * supportSide);
                Vector3 midpoint = (points[i] + points[i + 1]) * 0.5f;
                CreatePrimitive("Lane_Support_" + i, PrimitiveType.Cube, parent, new Vector3(midpoint.x + 2.95f * supportSide, 0.08f, midpoint.z), new Vector3(0.38f, 1.3f, 0.38f), darkMetal, false);
            }
        }

        private static GameObject CreateSegment(string name, Transform parent, Vector3 start, Vector3 end, float width, float thickness, Material material, bool keepCollider, float sideOffset)
        {
            Vector3 flatDirection = PortalNightsMath.Flat(end - start);
            float length = flatDirection.magnitude;
            Vector3 direction = length <= 0.001f ? Vector3.forward : flatDirection / length;
            Vector3 side = Vector3.Cross(Vector3.up, direction).normalized * sideOffset;
            Vector3 midpoint = (start + end) * 0.5f + side;
            midpoint.y = Mathf.Max(start.y, end.y) - 0.2f;

            GameObject segment = CreatePrimitive(name, PrimitiveType.Cube, parent, midpoint, new Vector3(width, thickness, length), material, keepCollider);
            segment.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
            return segment;
        }

        private static PortalNightsLanePath CreateLanePath(Transform laneRoot, PortalNightsLane lane, Vector3[] points, Color color)
        {
            Transform waypointsRoot = CreateChild(laneRoot, "Waypoints");
            Transform[] waypoints = new Transform[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                GameObject waypoint = new GameObject("Waypoint_" + (i + 1).ToString("00"));
                waypoint.transform.SetParent(waypointsRoot, false);
                waypoint.transform.position = points[i];
                waypoints[i] = waypoint.transform;
            }

            PortalNightsLanePath path = waypointsRoot.gameObject.AddComponent<PortalNightsLanePath>();
            path.Configure(lane, waypoints, color);
            return path;
        }

        private static void CreateWorldLabel(Transform parent, string text, Vector3 position, Color color)
        {
            GameObject labelObject = new GameObject(text.Replace(' ', '_') + "_Label");
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.position = position;
            labelObject.transform.rotation = Quaternion.Euler(68f, 0f, 0f);
            TextMesh label = labelObject.AddComponent<TextMesh>();
            label.text = text;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.fontSize = 48;
            label.characterSize = 0.18f;
            label.color = color;
        }

        private static void CreateAmbientVfx(Transform parent)
        {
            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                GameObject vent = CreatePrimitive("SteamVent_" + i, PrimitiveType.Cylinder, parent, new Vector3(side * (5.5f + i), 0.86f, 14f - i * 3.6f), new Vector3(0.65f, 0.16f, 0.65f), darkMetal, false);
                ParticleSystem particles = vent.AddComponent<ParticleSystem>();
                ParticleSystem.MainModule main = particles.main;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, 1.4f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.32f);
                main.startColor = new Color(0.55f, 0.84f, 1f, 0.22f);
                ParticleSystem.EmissionModule emission = particles.emission;
                emission.rateOverTime = 6f;
                ParticleSystem.ShapeModule shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 18f;
                shape.radius = 0.22f;
            }
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, bool keepCollider)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localScale = localScale;
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.sharedMaterial = material;
            }

            if (!keepCollider)
            {
                Collider collider = gameObject.GetComponent<Collider>();
                if (collider != null)
                {
                    UnityEngine.Object.DestroyImmediate(collider);
                }
            }

            return gameObject;
        }

        private static Text CreateText(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, int fontSize, TextAnchor alignment, Color color, FontStyle style)
        {
            GameObject textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Text text = textObject.AddComponent<Text>();
            text.font = hudFont;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.fontStyle = style;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = Mathf.Max(12, fontSize - 8);
            text.resizeTextMaxSize = fontSize;

            Outline outline = textObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0.05f, 0.1f, 0.82f);
            outline.effectDistance = new Vector2(2f, -2f);
            return text;
        }

        private static Slider CreateSlider(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 size, Color fillColor, Color backgroundColor)
        {
            GameObject sliderObject = new GameObject(name);
            sliderObject.transform.SetParent(parent, false);
            RectTransform sliderRect = sliderObject.AddComponent<RectTransform>();
            sliderRect.anchorMin = anchorMin;
            sliderRect.anchorMax = anchorMax;
            sliderRect.anchoredPosition = anchoredPosition;
            sliderRect.sizeDelta = size;

            Slider slider = sliderObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;
            slider.transition = Selectable.Transition.None;

            GameObject background = new GameObject("Background");
            background.transform.SetParent(sliderObject.transform, false);
            RectTransform backgroundRect = background.AddComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;
            Image backgroundImage = background.AddComponent<Image>();
            backgroundImage.color = backgroundColor;

            GameObject fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillArea.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = new Vector2(2f, 2f);
            fillAreaRect.offsetMax = new Vector2(-2f, -2f);

            GameObject fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            RectTransform fillRect = fill.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            Image fillImage = fill.AddComponent<Image>();
            fillImage.color = fillColor;
            slider.fillRect = fillRect;
            slider.targetGraphic = fillImage;
            return slider;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void SetSerializedObject(UnityEngine.Object target, string propertyName, UnityEngine.Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetSerializedFloat(UnityEngine.Object target, string propertyName, float value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.floatValue = value;
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void SetSerializedObject(UnityEngine.Object target, string propertyName, Transform[] values)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }
                serializedObject.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>();
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
            {
                if (scene.path != scenePath)
                {
                    scenes.Add(scene);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
