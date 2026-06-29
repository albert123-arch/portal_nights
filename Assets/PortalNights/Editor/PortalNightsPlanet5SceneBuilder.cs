using System.IO;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PortalNights.EditorTools
{
    public static class PortalNightsPlanet5SceneBuilder
    {
        private const string Root = "Assets/PortalNights";
        private const string MaterialsDir = Root + "/Materials";
        private const string ScenePath = Root + "/Scenes/PortalNightsArena.unity";
        private const int BuildPadCost = 120;

        private static Material floor;
        private static Material darkMetal;
        private static Material blackStone;
        private static Material corruptedYellow;
        private static Material corruptedRed;
        private static Material bossPurple;
        private static Material restoredCyan;
        private static Material restoredWhite;
        private static Material lava;

        [MenuItem("Portal Nights/Rebuild Planet 5 Crimson Singularity Only")]
        public static void BuildPlanet5CrimsonSingularity()
        {
            EnsureMaterials();

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
                Debug.LogError("[PortalNights] PortalNightsArena was not found. Planet 5 was not built.");
                return;
            }

            Transform existing = arenaObject.transform.Find("Planet5_CrimsonSingularity");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            Transform root = CreateChild(arenaObject.transform, "Planet5_CrimsonSingularity");
            root.localPosition = new Vector3(0f, 0f, 620f);
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;

            BuildHierarchy(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PortalNights] Planet 5 Crimson Singularity rebuilt. Footprint: 170 x 145 units.");
        }

        public static void BuildAndCapturePlanet5CrimsonSingularity()
        {
            BuildPlanet5CrimsonSingularity();
            CapturePlanet5CrimsonSingularityScreenshots();
        }

        [MenuItem("Portal Nights/Capture Planet 5 Crimson Singularity Screenshots")]
        public static void CapturePlanet5CrimsonSingularityScreenshots()
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

            Transform root = GameObject.Find("PortalNightsArena")?.transform.Find("Planet5_CrimsonSingularity");
            if (root == null)
            {
                Debug.LogError("[PortalNights] Planet5_CrimsonSingularity was not found. Build the map before capturing screenshots.");
                return;
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string captureDirectory = Path.Combine(projectRoot ?? Application.dataPath, "Logs/PortalNightsCaptures");
            Directory.CreateDirectory(captureDirectory);

            CaptureView(root, Path.Combine(captureDirectory, "planet5_arrival_corrupted_sphere.png"), new Vector3(0f, 20f, -74f), new Vector3(0f, 4.5f, 4f), 70f);
            CaptureView(root, Path.Combine(captureDirectory, "planet5_central_boss_positions.png"), new Vector3(0f, 28f, -28f), new Vector3(0f, 3.5f, 26f), 76f);
            CaptureView(root, Path.Combine(captureDirectory, "planet5_stabilizers.png"), new Vector3(0f, 26f, -30f), new Vector3(0f, 2.4f, 4f), 82f);

            Debug.Log("[PortalNights] Planet 5 screenshots saved to " + captureDirectory);
        }

        private static void BuildHierarchy(Transform root)
        {
            Transform environment = CreateChild(root, "Environment");
            Transform floors = CreateChild(environment, "Floors");
            Transform boundaries = CreateChild(environment, "Boundaries");
            Transform lavaBelow = CreateChild(environment, "LavaBelow");
            Transform backgroundStructures = CreateChild(environment, "BackgroundStructures");
            Transform corruptedEnergy = CreateChild(environment, "CorruptedEnergy");

            Transform arrivalZone = CreateChild(root, "ArrivalZone");
            Transform centralArena = CreateChild(root, "CentralArena");
            Transform sphere = CreateChild(centralArena, "CorruptedHealingSphere");
            Transform stabilizers = CreateChild(centralArena, "RestorationStabilizers");
            Transform buildPads = CreateChild(centralArena, "BuildPads");
            Transform utilityPads = CreateChild(centralArena, "UtilityPads");
            Transform bosses = CreateChild(root, "Bosses");
            Transform rifts = CreateChild(root, "Rifts");
            Transform waypoints = CreateChild(root, "Waypoints");
            Transform lighting = CreateChild(root, "Lighting");
            Transform vfx = CreateChild(root, "VFX");

            CreateFloors(floors);
            CreateBoundaries(boundaries);
            CreateLava(lavaBelow);
            CreateArrival(arrivalZone);
            CreateCorruptedSphere(sphere);
            CreateRestorationStabilizers(stabilizers);
            CreateBuildPads(buildPads, utilityPads);
            CreateBossMarkers(bosses);
            CreateRifts(rifts);
            CreateWaypoints(waypoints);
            CreateDecor(backgroundStructures, corruptedEnergy, vfx);
            CreateLighting(lighting);
        }

        private static void CreateFloors(Transform parent)
        {
            CreatePrimitive("CrimsonSingularity_ContinuousFloor_170x145", PrimitiveType.Cube, parent, new Vector3(0f, -0.28f, 0f), new Vector3(170f, 0.5f, 145f), floor, true);
            CreatePrimitive("ArrivalZone_30x22", PrimitiveType.Cube, parent, new Vector3(0f, 0.05f, -60f), new Vector3(30f, 0.12f, 22f), darkMetal, false);
            CreatePrimitive("CentralArena_78_DarkDeck", PrimitiveType.Cylinder, parent, Vector3.up * 0.05f, new Vector3(78f, 0.16f, 78f), darkMetal, false);
            CreatePrimitive("CentralArena_CorruptionRing", PrimitiveType.Cylinder, parent, Vector3.up * 0.18f, new Vector3(58f, 0.08f, 58f), corruptedRed, false);
            CreatePrimitive("CentralArena_InnerCombatPlate", PrimitiveType.Cylinder, parent, Vector3.up * 0.24f, new Vector3(44f, 0.08f, 44f), floor, false);

            CreatePath(parent, "Arrival_To_CentralArena", new Vector3(0f, 0.14f, -60f), new Vector3(0f, 0.14f, -18f), 18f, corruptedYellow);
            CreatePath(parent, "Central_To_NorthBossGate", new Vector3(0f, 0.14f, 14f), new Vector3(0f, 0.14f, 58f), 17f, bossPurple);
            CreatePath(parent, "Central_To_WestMinorRift", new Vector3(-16f, 0.14f, -4f), new Vector3(-72f, 0.14f, -5f), 15f, corruptedRed);
            CreatePath(parent, "Central_To_EastMinorRift", new Vector3(16f, 0.14f, -4f), new Vector3(72f, 0.14f, -5f), 15f, corruptedYellow);
            CreatePath(parent, "BossStart_LeftRoute", new Vector3(-10f, 0.15f, 12f), new Vector3(-32f, 0.15f, 20f), 14f, bossPurple);
            CreatePath(parent, "BossStart_RightRoute", new Vector3(10f, 0.15f, 12f), new Vector3(32f, 0.15f, 20f), 14f, corruptedRed);
        }

        private static void CreateBoundaries(Transform parent)
        {
            CreateWall(parent, "NorthVolcanicWall", new Vector3(0f, 1.4f, 73.5f), new Vector3(174f, 2.8f, 2f), bossPurple);
            CreateWall(parent, "SouthVolcanicWall", new Vector3(0f, 1.4f, -73.5f), new Vector3(174f, 2.8f, 2f), corruptedYellow);
            CreateWall(parent, "WestVolcanicWall", new Vector3(-86f, 1.4f, 0f), new Vector3(2f, 2.8f, 146f), corruptedRed);
            CreateWall(parent, "EastVolcanicWall", new Vector3(86f, 1.4f, 0f), new Vector3(2f, 2.8f, 146f), corruptedRed);
        }

        private static void CreateLava(Transform parent)
        {
            CreatePrimitive("LavaBelow_RedGoldGlow", PrimitiveType.Cube, parent, new Vector3(0f, -3.5f, 0f), new Vector3(176f, 0.16f, 151f), lava, false);
        }

        private static void CreateArrival(Transform parent)
        {
            parent.localPosition = new Vector3(0f, 0f, -60f);

            Transform player = CreateChild(parent, "PlayerArrivalPoint");
            player.localPosition = new Vector3(0f, 1.12f, 0f);
            Transform helper1 = CreateChild(parent, "Helper1ArrivalPoint");
            helper1.localPosition = new Vector3(-4f, 1.12f, -2f);
            Transform helper2 = CreateChild(parent, "Helper2ArrivalPoint");
            helper2.localPosition = new Vector3(4f, 1.12f, -2f);

            CreateWorldLabel(parent, "PLANET 5 - CRIMSON SINGULARITY", new Vector3(0f, 1.08f, -9f), corruptedYellow.color);
            CreatePrimitive("ArrivalCorruptedBeacon_Left", PrimitiveType.Cylinder, parent, new Vector3(-10f, 5.5f, -4f), new Vector3(0.42f, 5.5f, 0.42f), corruptedRed, false);
            CreatePrimitive("ArrivalCorruptedBeacon_Right", PrimitiveType.Cylinder, parent, new Vector3(10f, 5.5f, -4f), new Vector3(0.42f, 5.5f, 0.42f), corruptedYellow, false);
        }

        private static void CreateCorruptedSphere(Transform parent)
        {
            parent.localPosition = new Vector3(0f, 3.5f, 0f);
            parent.gameObject.AddComponent<NetworkObject>();

            CreatePrimitive("SpherePedestal", PrimitiveType.Cylinder, parent, new Vector3(0f, -3.15f, 0f), new Vector3(13f, 0.55f, 13f), darkMetal, true);
            CreatePrimitive("PedestalCorruptionRing", PrimitiveType.Cylinder, parent, new Vector3(0f, -2.78f, 0f), new Vector3(10f, 0.08f, 10f), corruptedRed, false);

            Transform corrupted = CreateChild(parent, "VisualState_Corrupted");
            CreatePrimitive("Corrupted_YellowShell", PrimitiveType.Sphere, corrupted, Vector3.zero, new Vector3(7.2f, 7.2f, 7.2f), corruptedYellow, false);
            CreatePrimitive("Corrupted_RedCore", PrimitiveType.Sphere, corrupted, Vector3.zero, new Vector3(4.2f, 4.2f, 4.2f), corruptedRed, false);
            CreateOrbitalBand(corrupted, "Corrupted_OrbitBand_Red", 4.9f, corruptedRed, 0f);
            CreateOrbitalBand(corrupted, "Corrupted_OrbitBand_Yellow", 5.5f, corruptedYellow, 90f);

            Transform damaged = CreateChild(parent, "VisualState_DamagedCore");
            CreatePrimitive("Damaged_RedCore", PrimitiveType.Sphere, damaged, Vector3.zero, new Vector3(4.8f, 4.8f, 4.8f), corruptedRed, false);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                GameObject shard = CreatePrimitive("DamagedShard_" + i.ToString("00"), PrimitiveType.Cube, damaged, new Vector3(Mathf.Cos(angle) * 3.1f, Mathf.Sin(angle) * 1.2f, Mathf.Sin(angle) * 3.1f), new Vector3(0.35f, 1.4f, 0.35f), blackStone, false);
                shard.transform.localRotation = Quaternion.Euler(25f, angle * Mathf.Rad2Deg, 12f);
            }

            damaged.gameObject.SetActive(false);

            Transform restored = CreateChild(parent, "VisualState_Restored");
            CreatePrimitive("Restored_CyanShell", PrimitiveType.Sphere, restored, Vector3.zero, new Vector3(7.2f, 7.2f, 7.2f), restoredCyan, false);
            CreatePrimitive("Restored_WhiteCore", PrimitiveType.Sphere, restored, Vector3.zero, new Vector3(3.6f, 3.6f, 3.6f), restoredWhite, false);
            CreatePrimitive("Restored_CyanBeam", PrimitiveType.Cylinder, restored, new Vector3(0f, 14f, 0f), new Vector3(0.55f, 14f, 0.55f), restoredCyan, false);
            restored.gameObject.SetActive(false);

            Light light = parent.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.36f, 0.13f);
            light.range = 45f;
            light.intensity = 8f;
        }

        private static void CreateRestorationStabilizers(Transform parent)
        {
            CreateStabilizer(parent, "NorthStabilizer", new Vector3(0f, 0f, 22f), corruptedYellow);
            CreateStabilizer(parent, "WestStabilizer", new Vector3(-24f, 0f, -12f), corruptedRed);
            CreateStabilizer(parent, "EastStabilizer", new Vector3(24f, 0f, -12f), corruptedRed);
        }

        private static void CreateStabilizer(Transform parent, string name, Vector3 position, Material accent)
        {
            Transform stabilizer = CreateChild(parent, name);
            stabilizer.localPosition = position;
            CreatePrimitive("StabilizerBase", PrimitiveType.Cylinder, stabilizer, new Vector3(0f, 0.16f, 0f), new Vector3(5.2f, 0.16f, 5.2f), darkMetal, false);
            CreatePrimitive("InactiveCore", PrimitiveType.Sphere, stabilizer, new Vector3(0f, 1.2f, 0f), new Vector3(1.25f, 1.25f, 1.25f), accent, false);
            CreatePrimitive("StabilizerSpine", PrimitiveType.Cylinder, stabilizer, new Vector3(0f, 1.1f, 0f), new Vector3(0.35f, 1.1f, 0.35f), blackStone, false);
            CreateWorldLabel(stabilizer, name.ToUpperInvariant(), new Vector3(0f, 0.9f, -3.7f), accent.color);
        }

        private static void CreateBuildPads(Transform standardParent, Transform utilityParent)
        {
            Vector3[] standard =
            {
                new Vector3(-10f, 0.35f, -42f), new Vector3(10f, 0.35f, -42f),
                new Vector3(-42f, 0.35f, -4f), new Vector3(-34f, 0.35f, 22f),
                new Vector3(42f, 0.35f, -4f), new Vector3(34f, 0.35f, 22f),
                new Vector3(-14f, 0.35f, 34f), new Vector3(14f, 0.35f, 34f)
            };

            for (int i = 0; i < standard.Length; i++)
            {
                Material accent = i < 2 ? corruptedYellow : i < 6 ? corruptedRed : bossPurple;
                CreateBuildPad(standardParent, "TurretPad_" + (i + 1).ToString("00"), standard[i], accent);
            }

            CreateBuildPad(utilityParent, "UtilityPad_West", new Vector3(-12f, 0.35f, -16f), restoredCyan);
            CreateBuildPad(utilityParent, "UtilityPad_East", new Vector3(12f, 0.35f, -16f), restoredCyan);
        }

        private static void CreateBossMarkers(Transform parent)
        {
            CreateBossMarker(parent, "BossA_SolarWarden_Start", new Vector3(-32f, 0f, 20f), corruptedYellow);
            CreateBossMarker(parent, "BossB_CrimsonBehemoth_Start", new Vector3(32f, 0f, 20f), corruptedRed);
        }

        private static void CreateBossMarker(Transform parent, string name, Vector3 position, Material accent)
        {
            Transform marker = CreateChild(parent, name);
            marker.localPosition = position;
            CreatePrimitive("BossStartDisk", PrimitiveType.Cylinder, marker, new Vector3(0f, 0.18f, 0f), new Vector3(8f, 0.14f, 8f), accent, false);
            CreatePrimitive("BossSpawnBeacon", PrimitiveType.Cylinder, marker, new Vector3(0f, 5f, 0f), new Vector3(0.44f, 5f, 0.44f), accent, false);
            CreateWorldLabel(marker, name.Replace("_Start", "").Replace('_', ' ').ToUpperInvariant(), new Vector3(0f, 1.05f, -5.8f), accent.color);
        }

        private static void CreateRifts(Transform parent)
        {
            CreatePortal(parent, "NorthBossGate", new Vector3(0f, 2.5f, 58f), 14f, bossPurple, corruptedRed);
            CreatePortal(parent, "WestMinorRift", new Vector3(-72f, 2.5f, -5f), 10f, corruptedRed, bossPurple);
            CreatePortal(parent, "EastMinorRift", new Vector3(72f, 2.5f, -5f), 10f, corruptedYellow, bossPurple);
        }

        private static void CreatePortal(Transform parent, string name, Vector3 position, float diameter, Material primary, Material secondary)
        {
            Transform portal = CreateChild(parent, name);
            portal.localPosition = position;
            Vector3 toCenter = Vector3.zero - position;
            toCenter.y = 0f;
            portal.localRotation = toCenter.sqrMagnitude <= 0.001f ? Quaternion.identity : Quaternion.LookRotation(toCenter.normalized, Vector3.up);
            CreatePrimitive("PortalPlatform", PrimitiveType.Cylinder, portal, new Vector3(0f, -2.28f, 0f), new Vector3(diameter + 5f, 0.22f, diameter + 5f), darkMetal, false);
            CreatePrimitive("PortalSurface", PrimitiveType.Sphere, portal, Vector3.zero, new Vector3(diameter, diameter, 0.42f), primary, false);

            for (int i = 0; i < 16; i++)
            {
                float angle = i * Mathf.PI * 2f / 16f;
                Vector3 segmentPosition = new Vector3(Mathf.Cos(angle) * diameter * 0.58f, Mathf.Sin(angle) * diameter * 0.58f, -0.48f);
                GameObject segment = CreatePrimitive("PortalFrame_" + i.ToString("00"), PrimitiveType.Cube, portal, segmentPosition, new Vector3(0.6f, 1f, 0.86f), i % 3 == 0 ? secondary : blackStone, false);
                segment.transform.localRotation = Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg);
            }

            Transform spawns = CreateChild(portal, "SpawnPoints");
            CreatePrimitive("SpawnMarker_01", PrimitiveType.Cylinder, spawns, new Vector3(-2.6f, -2.1f, 4f), new Vector3(0.8f, 0.06f, 0.8f), secondary, false);
            CreatePrimitive("SpawnMarker_02", PrimitiveType.Cylinder, spawns, new Vector3(2.6f, -2.1f, 4f), new Vector3(0.8f, 0.06f, 0.8f), secondary, false);

            Light light = portal.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = primary.color;
            light.range = diameter * 3.4f;
            light.intensity = 5.5f;
        }

        private static void CreateWaypoints(Transform parent)
        {
            CreateWaypointPath(parent, "Arrival_To_CentralArena", new[] { new Vector3(0f, 0.28f, -60f), new Vector3(0f, 0.28f, -25f), new Vector3(0f, 0.28f, 0f) }, corruptedYellow);
            CreateWaypointPath(parent, "NorthBossGate_To_CentralArena", new[] { new Vector3(0f, 0.28f, 58f), new Vector3(0f, 0.28f, 30f), new Vector3(0f, 0.28f, 0f) }, bossPurple);
            CreateWaypointPath(parent, "WestMinorRift_To_CentralArena", new[] { new Vector3(-72f, 0.28f, -5f), new Vector3(-36f, 0.28f, -3f), new Vector3(0f, 0.28f, 0f) }, corruptedRed);
            CreateWaypointPath(parent, "EastMinorRift_To_CentralArena", new[] { new Vector3(72f, 0.28f, -5f), new Vector3(36f, 0.28f, -3f), new Vector3(0f, 0.28f, 0f) }, corruptedYellow);
        }

        private static void CreateDecor(Transform background, Transform corruptedEnergy, Transform vfx)
        {
            for (int i = 0; i < 9; i++)
            {
                float x = -80f + i * 20f;
                float height = i % 2 == 0 ? 18f : 12f;
                CreatePrimitive("CrimsonBackdropTower_" + i.ToString("00"), PrimitiveType.Cube, background, new Vector3(x, height * 0.5f, 82f), new Vector3(8f, height, 6f), blackStone, false);
            }

            for (int i = 0; i < 12; i++)
            {
                float angle = i * Mathf.PI * 2f / 12f;
                float radius = i % 2 == 0 ? 48f : 62f;
                GameObject spike = CreatePrimitive("CorruptedEnergySpike_" + i.ToString("00"), PrimitiveType.Cylinder, corruptedEnergy, new Vector3(Mathf.Cos(angle) * radius, 3.6f, Mathf.Sin(angle) * radius), new Vector3(0.42f, 3.6f, 0.42f), i % 3 == 0 ? corruptedYellow : corruptedRed, false);
                spike.transform.localRotation = Quaternion.Euler(18f, -angle * Mathf.Rad2Deg, 0f);
            }

            CreatePrimitive("CrimsonHeatHazePlane", PrimitiveType.Cube, vfx, new Vector3(0f, 0.02f, 0f), new Vector3(166f, 0.04f, 141f), lava, false);
        }

        private static void CreateLighting(Transform parent)
        {
            CreatePointLight(parent, "CorruptedSphere_Key", new Vector3(0f, 14f, 0f), new Color(1f, 0.42f, 0.1f), 5.8f, 76f);
            CreatePointLight(parent, "BossPurple_NorthFill", new Vector3(0f, 11f, 52f), new Color(0.54f, 0.18f, 1f), 4.5f, 64f);
            CreatePointLight(parent, "Arrival_GoldFill", new Vector3(0f, 8f, -56f), new Color(1f, 0.78f, 0.22f), 2.8f, 48f);
            CreatePointLight(parent, "LeftRedFill", new Vector3(-52f, 8f, 8f), new Color(1f, 0.15f, 0.08f), 3.2f, 48f);
            CreatePointLight(parent, "RightRedFill", new Vector3(52f, 8f, 8f), new Color(1f, 0.15f, 0.08f), 3.2f, 48f);

            GameObject key = new GameObject("CrimsonSingularity_DirectionalKey");
            key.transform.SetParent(parent, false);
            key.transform.localRotation = Quaternion.Euler(48f, -32f, 0f);
            Light light = key.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.42f, 0.24f);
            light.intensity = 0.62f;
        }

        private static void CreateBuildPad(Transform parent, string name, Vector3 position, Material accent)
        {
            GameObject pad = new GameObject(name);
            pad.transform.SetParent(parent, false);
            pad.transform.localPosition = position;
            Vector3 facing = Vector3.zero - position;
            facing.y = 0f;
            pad.transform.localRotation = facing.sqrMagnitude <= 0.001f ? Quaternion.identity : Quaternion.LookRotation(facing.normalized, Vector3.up);
            pad.AddComponent<NetworkObject>();

            BoxCollider collider = pad.AddComponent<BoxCollider>();
            collider.size = new Vector3(5f, 0.56f, 5f);
            collider.center = new Vector3(0f, 0.18f, 0f);

            CreatePrimitive("Pad_Base", PrimitiveType.Cylinder, pad.transform, Vector3.zero, new Vector3(5f, 0.22f, 5f), darkMetal, false);
            Renderer ring = CreatePrimitive("Pad_CrimsonRing", PrimitiveType.Cylinder, pad.transform, new Vector3(0f, 0.16f, 0f), new Vector3(4.35f, 0.08f, 4.35f), accent, false).GetComponent<Renderer>();
            CreatePrimitive("Pad_DarkSocket", PrimitiveType.Cylinder, pad.transform, new Vector3(0f, 0.28f, 0f), new Vector3(2.1f, 0.12f, 2.1f), floor, false);

            Transform socket = CreateChild(pad.transform, "TurretSocket");
            socket.localPosition = new Vector3(0f, 0.62f, 0f);

            Light light = pad.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 6.5f;
            light.intensity = 1.9f;
            light.color = accent.color;

            PortalNightsBuildPoint buildPoint = pad.AddComponent<PortalNightsBuildPoint>();
            buildPoint.Configure(BuildPadCost, 180, 260, socket, ring, light, false);
        }

        private static void CreatePath(Transform parent, string name, Vector3 start, Vector3 end, float width, Material accent)
        {
            CreateSegment(name + "_DarkPlate", parent, start, end, width, 0.12f, darkMetal, false, 0f);
            CreateSegment(name + "_CenterGlow", parent, start + Vector3.up * 0.09f, end + Vector3.up * 0.09f, 0.42f, 0.05f, accent, false, 0f);
            CreateSegment(name + "_LeftEdgeGlow", parent, start + Vector3.up * 0.1f, end + Vector3.up * 0.1f, 0.2f, 0.05f, accent, false, -width * 0.42f);
            CreateSegment(name + "_RightEdgeGlow", parent, start + Vector3.up * 0.1f, end + Vector3.up * 0.1f, 0.2f, 0.05f, accent, false, width * 0.42f);
        }

        private static GameObject CreateSegment(string name, Transform parent, Vector3 start, Vector3 end, float width, float thickness, Material material, bool solid, float sideOffset)
        {
            Vector3 direction = end - start;
            direction.y = 0f;
            float length = direction.magnitude;
            if (length <= 0.001f)
            {
                return null;
            }

            Vector3 forward = direction / length;
            Vector3 side = Vector3.Cross(Vector3.up, forward).normalized;
            Vector3 midpoint = (start + end) * 0.5f + side * sideOffset;
            GameObject segment = CreatePrimitive(name, PrimitiveType.Cube, parent, midpoint, new Vector3(width, thickness, length), material, solid);
            segment.transform.localRotation = Quaternion.LookRotation(forward, Vector3.up);
            return segment;
        }

        private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale, Material accent)
        {
            CreatePrimitive(name, PrimitiveType.Cube, parent, position, scale, blackStone, true);
            Vector3 glowScale = scale.x > scale.z ? new Vector3(Mathf.Max(1f, scale.x - 10f), 0.12f, 0.18f) : new Vector3(0.18f, 0.12f, Mathf.Max(1f, scale.z - 10f));
            CreatePrimitive(name + "_CorruptedEdge", PrimitiveType.Cube, parent, position + Vector3.up * 1.05f, glowScale, accent, false);
        }

        private static void CreateOrbitalBand(Transform parent, string name, float radius, Material material, float yaw)
        {
            Transform band = CreateChild(parent, name);
            band.localRotation = Quaternion.Euler(0f, yaw, 0f);
            for (int i = 0; i < 18; i++)
            {
                float angle = i * Mathf.PI * 2f / 18f;
                GameObject segment = CreatePrimitive("BandSegment_" + i.ToString("00"), PrimitiveType.Cube, band, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f), new Vector3(0.2f, 0.2f, 1.35f), material, false);
                segment.transform.localRotation = Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg);
            }
        }

        private static void CreateWaypointPath(Transform parent, string name, Vector3[] points, Material material)
        {
            Transform path = CreateChild(parent, name);
            for (int i = 0; i < points.Length; i++)
            {
                CreatePrimitive("Waypoint_" + (i + 1).ToString("00"), PrimitiveType.Cylinder, path, points[i], new Vector3(0.85f, 0.05f, 0.85f), material, false);
            }
        }

        private static void CreatePointLight(Transform parent, string name, Vector3 position, Color color, float intensity, float range)
        {
            GameObject lightObject = new GameObject(name);
            lightObject.transform.SetParent(parent, false);
            lightObject.transform.localPosition = position;
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
        }

        private static Transform CreateWorldLabel(Transform parent, string text, Vector3 position, Color color)
        {
            GameObject label = new GameObject("Label_" + text.Replace(" ", "_"));
            label.transform.SetParent(parent, false);
            label.transform.localPosition = position;
            TextMesh textMesh = label.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 64;
            textMesh.characterSize = 0.08f;
            textMesh.color = color;
            return label.transform;
        }

        private static GameObject CreatePrimitive(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, bool solid)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localScale = localScale;

            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = material;
            }

            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = !solid;
            }

            return gameObject;
        }

        private static Transform CreateChild(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child.transform;
        }

        private static void CaptureView(Transform root, string path, Vector3 localCameraPosition, Vector3 localLookAt, float fieldOfView)
        {
            GameObject cameraObject = new GameObject("PN_TempPlanet5CaptureCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("070408");
            camera.fieldOfView = fieldOfView;
            camera.nearClipPlane = 0.05f;
            camera.farClipPlane = 260f;
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

        private static void EnsureMaterials()
        {
            Directory.CreateDirectory(MaterialsDir);
            floor = Material("PN_P5_Floor", Hex("161016"), new Color(0.02f, 0.008f, 0.014f), 0.56f, 0.58f);
            darkMetal = Material("PN_P5_DarkMetal", Hex("241B22"), new Color(0.035f, 0.018f, 0.024f), 0.82f, 0.66f);
            blackStone = Material("PN_P5_BlackStone", Hex("09070A"), new Color(0.012f, 0.006f, 0.008f), 0.25f, 0.38f);
            corruptedYellow = Material("PN_P5_CorruptedYellow", Hex("FFD242"), new Color(3.4f, 2.4f, 0.38f), 0f, 0.9f);
            corruptedRed = Material("PN_P5_CorruptedRed", Hex("FF2F1F"), new Color(4f, 0.34f, 0.18f), 0f, 0.88f);
            bossPurple = Material("PN_P5_BossPurple", Hex("8A2CFF"), new Color(1.35f, 0.3f, 4f), 0f, 0.94f);
            restoredCyan = Material("PN_P5_RestoredCyan", Hex("63F7FF"), new Color(0.62f, 3.4f, 4.2f), 0f, 0.95f);
            restoredWhite = Material("PN_P5_RestoredWhite", Color.white, new Color(2.2f, 2.4f, 2.55f), 0f, 0.86f);
            lava = Material("PN_P5_LavaBelow", Hex("5A100A"), new Color(2.6f, 0.22f, 0.08f), 0f, 0.84f);
        }

        private static Material Material(string name, Color color, Color emission, float metallic, float smoothness)
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

            material.color = color;
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", metallic);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", smoothness);
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        private static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString("#" + value, out Color color) ? color : Color.white;
        }
    }
}
