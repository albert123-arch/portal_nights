using System.IO;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PortalNights.EditorTools
{
    public static class PortalNightsPlanet4SceneBuilder
    {
        private const string Root = "Assets/PortalNights";
        private const string MaterialsDir = Root + "/Materials";
        private const string ScenePath = Root + "/Scenes/PortalNightsArena.unity";
        private const int BuildPadCost = 120;

        private static Material ground;
        private static Material darkMetal;
        private static Material blackStone;
        private static Material toxicGreen;
        private static Material hivePurple;
        private static Material bloodRed;
        private static Material portalCore;
        private static Material stormIndigo;

        [MenuItem("Portal Nights/Rebuild Planet 4 Swarm Expanse Only")]
        public static void BuildPlanet4SwarmExpanse()
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
                Debug.LogError("[PortalNights] PortalNightsArena was not found. Planet 4 was not built.");
                return;
            }

            Transform existing = arenaObject.transform.Find("Planet4_SwarmExpanse");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            Transform root = CreateChild(arenaObject.transform, "Planet4_SwarmExpanse");
            root.localPosition = new Vector3(0f, 0f, 420f);
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;

            BuildHierarchy(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PortalNights] Planet 4 Swarm Expanse rebuilt. Footprint: 180 x 150 units. Rifts: N(0,2.5,58), W(-72,2.5,10), E(72,2.5,10), S(0,2.5,-28).");
        }

        public static void BuildAndCapturePlanet4SwarmExpanse()
        {
            BuildPlanet4SwarmExpanse();
            CapturePlanet4SwarmExpanseScreenshots();
        }

        [MenuItem("Portal Nights/Capture Planet 4 Swarm Expanse Screenshots")]
        public static void CapturePlanet4SwarmExpanseScreenshots()
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

            Transform root = GameObject.Find("PortalNightsArena")?.transform.Find("Planet4_SwarmExpanse");
            if (root == null)
            {
                Debug.LogError("[PortalNights] Planet4_SwarmExpanse was not found. Build the map before capturing screenshots.");
                return;
            }

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string captureDirectory = Path.Combine(projectRoot ?? Application.dataPath, "Logs/PortalNightsCaptures");
            Directory.CreateDirectory(captureDirectory);

            CaptureView(root, Path.Combine(captureDirectory, "planet4_arrival_zone.png"), new Vector3(0f, 17f, -76f), new Vector3(0f, 2.5f, 18f), 66f);
            CaptureView(root, Path.Combine(captureDirectory, "planet4_central_battlefield.png"), new Vector3(0f, 25f, -28f), new Vector3(0f, 2.5f, 32f), 72f);
            CaptureView(root, Path.Combine(captureDirectory, "planet4_two_rifts_visible.png"), new Vector3(0f, 24f, -58f), new Vector3(0f, 2.5f, 18f), 92f);

            Debug.Log("[PortalNights] Planet 4 screenshots saved to " + captureDirectory);
        }

        private static void BuildHierarchy(Transform root)
        {
            Transform environment = CreateChild(root, "Environment");
            Transform floors = CreateChild(environment, "Floors");
            Transform boundaries = CreateChild(environment, "Boundaries");
            Transform hiveGrowths = CreateChild(environment, "HiveGrowths");
            Transform crystals = CreateChild(environment, "Crystals");
            Transform backgroundStructures = CreateChild(environment, "BackgroundStructures");
            Transform stormVfx = CreateChild(environment, "StormVFX");

            Transform arrivalZone = CreateChild(root, "ArrivalZone");
            Transform centralBattlefield = CreateChild(root, "CentralBattlefield");
            Transform centralPads = CreateChild(centralBattlefield, "BuildPads");
            Transform utilityPads = CreateChild(centralBattlefield, "UtilityPads");
            Transform hiveRifts = CreateChild(root, "HiveRifts");
            Transform waypoints = CreateChild(root, "Waypoints");
            Transform exitPortal = CreateChild(root, "ExitPortalToPlanet5");
            Transform lighting = CreateChild(root, "Lighting");
            Transform vfx = CreateChild(root, "VFX");

            CreateFloors(floors);
            CreateBoundaries(boundaries);
            CreateArrival(arrivalZone);
            CreateCentralBattlefield(centralBattlefield);
            CreateBuildPads(centralPads, utilityPads);
            CreateHiveRifts(hiveRifts);
            CreateWaypoints(waypoints);
            CreateExitPortal(exitPortal);
            CreateDecor(hiveGrowths, crystals, backgroundStructures, stormVfx, vfx);
            CreateLighting(lighting);
        }

        private static void CreateFloors(Transform parent)
        {
            CreatePrimitive("SwarmExpanse_ContinuousGround_180x150", PrimitiveType.Cube, parent, new Vector3(0f, -0.28f, 0f), new Vector3(180f, 0.5f, 150f), ground, true);

            CreatePrimitive("ArrivalZone_28x20", PrimitiveType.Cube, parent, new Vector3(0f, 0.04f, -62f), new Vector3(28f, 0.12f, 20f), darkMetal, false);
            CreatePrimitive("CentralBattlefield_70x55", PrimitiveType.Cube, parent, Vector3.up * 0.05f, new Vector3(70f, 0.12f, 55f), darkMetal, false);
            CreatePrimitive("CentralBattlefield_BloodHiveRing", PrimitiveType.Cylinder, parent, new Vector3(0f, 0.16f, 0f), new Vector3(58f, 0.08f, 58f), bloodRed, false);
            CreatePrimitive("CentralBattlefield_DarkInnerDeck", PrimitiveType.Cylinder, parent, new Vector3(0f, 0.22f, 0f), new Vector3(46f, 0.08f, 46f), ground, false);

            CreatePathSegment(parent, "Arrival_To_Central", new Vector3(0f, 0.12f, -62f), new Vector3(0f, 0.12f, -14f), 16f, toxicGreen);
            CreatePathSegment(parent, "Central_To_NorthHive", new Vector3(0f, 0.13f, 14f), new Vector3(0f, 0.13f, 58f), 16f, hivePurple);
            CreatePathSegment(parent, "Central_To_SouthHive", new Vector3(0f, 0.13f, -4f), new Vector3(0f, 0.13f, -28f), 16f, bloodRed);
            CreatePathSegment(parent, "Central_To_WestHive", new Vector3(-14f, 0.13f, 4f), new Vector3(-72f, 0.13f, 10f), 15f, hivePurple);
            CreatePathSegment(parent, "Central_To_EastHive", new Vector3(14f, 0.13f, 4f), new Vector3(72f, 0.13f, 10f), 15f, hivePurple);

            CreatePathSegment(parent, "LoopRoute_North", new Vector3(-56f, 0.15f, 36f), new Vector3(56f, 0.15f, 36f), 12f, toxicGreen);
            CreatePathSegment(parent, "LoopRoute_South", new Vector3(-52f, 0.15f, -38f), new Vector3(52f, 0.15f, -38f), 12f, toxicGreen);
            CreatePathSegment(parent, "LoopRoute_West", new Vector3(-56f, 0.15f, -38f), new Vector3(-56f, 0.15f, 36f), 12f, toxicGreen);
            CreatePathSegment(parent, "LoopRoute_East", new Vector3(56f, 0.15f, -38f), new Vector3(56f, 0.15f, 36f), 12f, toxicGreen);

            CreateFiringPlatform(parent, "ElevatedFiringPlatform_NorthWest", new Vector3(-34f, 0.24f, 26f), new Vector3(18f, 0.22f, 12f), hivePurple);
            CreateFiringPlatform(parent, "ElevatedFiringPlatform_NorthEast", new Vector3(34f, 0.24f, 26f), new Vector3(18f, 0.22f, 12f), hivePurple);
            CreateFiringPlatform(parent, "ElevatedFiringPlatform_SouthWest", new Vector3(-32f, 0.24f, -24f), new Vector3(18f, 0.22f, 12f), toxicGreen);
            CreateFiringPlatform(parent, "ElevatedFiringPlatform_SouthEast", new Vector3(32f, 0.24f, -24f), new Vector3(18f, 0.22f, 12f), toxicGreen);
        }

        private static void CreateBoundaries(Transform parent)
        {
            CreateWall(parent, "NorthCliffWall", new Vector3(0f, 1.35f, 76f), new Vector3(184f, 2.7f, 2f), hivePurple);
            CreateWall(parent, "SouthCliffWall", new Vector3(0f, 1.35f, -76f), new Vector3(184f, 2.7f, 2f), toxicGreen);
            CreateWall(parent, "WestCliffWall", new Vector3(-91f, 1.35f, 0f), new Vector3(2f, 2.7f, 152f), bloodRed);
            CreateWall(parent, "EastCliffWall", new Vector3(91f, 1.35f, 0f), new Vector3(2f, 2.7f, 152f), bloodRed);

            for (int i = 0; i < 10; i++)
            {
                float z = -62f + i * 14f;
                CreatePrimitive("WestCliffTooth_" + i.ToString("00"), PrimitiveType.Cylinder, parent, new Vector3(-86.5f, 1.3f, z), new Vector3(1.4f, 2.2f, 1.4f), i % 2 == 0 ? hivePurple : toxicGreen, true);
                CreatePrimitive("EastCliffTooth_" + i.ToString("00"), PrimitiveType.Cylinder, parent, new Vector3(86.5f, 1.3f, z), new Vector3(1.4f, 2.2f, 1.4f), i % 2 == 0 ? toxicGreen : hivePurple, true);
            }
        }

        private static void CreateArrival(Transform parent)
        {
            parent.localPosition = new Vector3(0f, 0f, -62f);

            Transform player = CreateChild(parent, "PlayerArrivalPoint");
            player.localPosition = new Vector3(0f, 1.12f, 0f);
            Transform ally = CreateChild(parent, "AllyArrivalPoint");
            ally.localPosition = new Vector3(3.2f, 1.12f, 0f);

            CreateWorldLabel(parent, "PLANET 4 - SWARM EXPANSE", new Vector3(0f, 1.08f, -8.2f), toxicGreen.color);
            CreatePrimitive("ArrivalBeacon_Green", PrimitiveType.Cylinder, parent, new Vector3(-9f, 4.5f, -4f), new Vector3(0.38f, 4.5f, 0.38f), toxicGreen, false);
            CreatePrimitive("ArrivalBeacon_Violet", PrimitiveType.Cylinder, parent, new Vector3(9f, 4.5f, -4f), new Vector3(0.38f, 4.5f, 0.38f), hivePurple, false);
        }

        private static void CreateCentralBattlefield(Transform parent)
        {
            parent.localPosition = Vector3.zero;
            CreateWorldLabel(parent, "KILL THE SWARM", new Vector3(0f, 1.1f, -23f), bloodRed.color);

            CreatePrimitive("CentralToxicWell", PrimitiveType.Cylinder, parent, new Vector3(0f, 0.35f, 0f), new Vector3(10f, 0.18f, 10f), toxicGreen, false);
            CreatePrimitive("CentralHiveCrown_Base", PrimitiveType.Cylinder, parent, new Vector3(0f, 0.62f, 0f), new Vector3(6f, 0.32f, 6f), blackStone, false);

            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 2f / 8f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 8f, 1.05f, Mathf.Sin(angle) * 8f);
                GameObject spike = CreatePrimitive("CentralHiveSpike_" + i.ToString("00"), PrimitiveType.Cylinder, parent, position, new Vector3(0.42f, 2.2f, 0.42f), i % 2 == 0 ? bloodRed : hivePurple, false);
                spike.transform.localRotation = Quaternion.Euler(22f, -angle * Mathf.Rad2Deg, 0f);
            }
        }

        private static void CreateBuildPads(Transform standardParent, Transform utilityParent)
        {
            Vector3[] standard =
            {
                new Vector3(-10f, 0.35f, 42f), new Vector3(10f, 0.35f, 42f),
                new Vector3(-62f, 0.35f, 2f), new Vector3(-48f, 0.35f, 22f),
                new Vector3(62f, 0.35f, 2f), new Vector3(48f, 0.35f, 22f),
                new Vector3(-12f, 0.35f, -34f), new Vector3(12f, 0.35f, -34f),
                new Vector3(-26f, 0.35f, 12f), new Vector3(26f, 0.35f, 12f),
                new Vector3(-24f, 0.35f, -12f), new Vector3(24f, 0.35f, -12f)
            };

            for (int i = 0; i < standard.Length; i++)
            {
                Material accent = i < 2 ? hivePurple : i < 8 ? toxicGreen : bloodRed;
                CreateBuildPad(standardParent, "TurretPad_" + (i + 1).ToString("00"), standard[i], accent);
            }

            CreateBuildPad(utilityParent, "UtilityPad_West", new Vector3(-11f, 0.35f, 0f), toxicGreen);
            CreateBuildPad(utilityParent, "UtilityPad_East", new Vector3(11f, 0.35f, 0f), hivePurple);
        }

        private static void CreateHiveRifts(Transform parent)
        {
            CreateHiveRift(parent, "NorthHiveRift", new Vector3(0f, 2.5f, 58f), 14f, hivePurple, toxicGreen);
            CreateHiveRift(parent, "WestHiveRift", new Vector3(-72f, 2.5f, 10f), 12f, hivePurple, bloodRed);
            CreateHiveRift(parent, "EastHiveRift", new Vector3(72f, 2.5f, 10f), 12f, hivePurple, bloodRed);
            CreateHiveRift(parent, "SouthHiveRift", new Vector3(0f, 2.5f, -28f), 11f, bloodRed, toxicGreen);
        }

        private static void CreateHiveRift(Transform parent, string name, Vector3 localPosition, float diameter, Material primary, Material secondary)
        {
            GameObject riftObject = new GameObject(name);
            riftObject.transform.SetParent(parent, false);
            riftObject.transform.localPosition = localPosition;
            Vector3 toCenter = Vector3.zero - localPosition;
            toCenter.y = 0f;
            riftObject.transform.localRotation = toCenter.sqrMagnitude <= 0.001f ? Quaternion.identity : Quaternion.LookRotation(toCenter.normalized, Vector3.up);
            riftObject.AddComponent<NetworkObject>();

            CreatePrimitive("RiftPlatform", PrimitiveType.Cylinder, riftObject.transform, new Vector3(0f, -2.3f, 0f), new Vector3(diameter + 5f, 0.22f, diameter + 5f), darkMetal, false);
            CreatePrimitive("RiftSurface_GreenVioletCore", PrimitiveType.Sphere, riftObject.transform, Vector3.zero, new Vector3(diameter, diameter, 0.45f), portalCore, false);
            CreatePrimitive("RiftSurface_PurpleStormLayer", PrimitiveType.Sphere, riftObject.transform, new Vector3(0f, 0f, -0.18f), new Vector3(diameter * 1.05f, diameter * 1.05f, 0.18f), primary, false);

            for (int i = 0; i < 18; i++)
            {
                float angle = i * Mathf.PI * 2f / 18f;
                float radius = diameter * 0.56f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, -0.5f);
                GameObject segment = CreatePrimitive("RiftFrameSegment_" + i.ToString("00"), PrimitiveType.Cube, riftObject.transform, position, new Vector3(0.62f, 1.02f, 0.9f), i % 3 == 0 ? secondary : darkMetal, true);
                segment.transform.localRotation = Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg);
            }

            CreatePrimitive("RiftBeacon_28m", PrimitiveType.Cylinder, riftObject.transform, new Vector3(0f, 13.6f, -0.4f), new Vector3(0.58f, 13.6f, 0.58f), primary, false);

            Transform spawnPoints = CreateChild(riftObject.transform, "SpawnPoints");
            CreateMarker(spawnPoints, "SpawnPoint_01", new Vector3(-2.8f, -2.1f, 4.8f), secondary);
            CreateMarker(spawnPoints, "SpawnPoint_02", new Vector3(2.8f, -2.1f, 4.8f), secondary);

            Transform marker = CreateChild(riftObject.transform, "WorldSpaceMarker");
            marker.localPosition = new Vector3(0f, diameter * 0.5f + 2.8f, 0f);
            CreatePrimitive("MarkerGlow", PrimitiveType.Sphere, marker, Vector3.zero, new Vector3(1.4f, 1.4f, 1.4f), secondary, false);

            Light light = riftObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = primary.color;
            light.range = diameter * 3.2f;
            light.intensity = 5.5f;
        }

        private static void CreateWaypoints(Transform parent)
        {
            CreateWaypointPath(parent, "NorthRift_To_Central", new[] { new Vector3(0f, 0.28f, 58f), new Vector3(0f, 0.28f, 31f), new Vector3(0f, 0.28f, 0f) }, hivePurple);
            CreateWaypointPath(parent, "WestRift_To_Central", new[] { new Vector3(-72f, 0.28f, 10f), new Vector3(-38f, 0.28f, 8f), new Vector3(0f, 0.28f, 0f) }, toxicGreen);
            CreateWaypointPath(parent, "EastRift_To_Central", new[] { new Vector3(72f, 0.28f, 10f), new Vector3(38f, 0.28f, 8f), new Vector3(0f, 0.28f, 0f) }, toxicGreen);
            CreateWaypointPath(parent, "SouthRift_To_Central", new[] { new Vector3(0f, 0.28f, -28f), new Vector3(0f, 0.28f, -12f), new Vector3(0f, 0.28f, 0f) }, bloodRed);
        }

        private static void CreateExitPortal(Transform parent)
        {
            parent.localPosition = new Vector3(0f, 1.5f, 70f);
            parent.localRotation = Quaternion.Euler(0f, 180f, 0f);

            CreatePrimitive("DormantExitPortal_Platform", PrimitiveType.Cylinder, parent, new Vector3(0f, -1.3f, 0f), new Vector3(18f, 0.22f, 18f), darkMetal, false);
            CreatePrimitive("DormantExitPortal_Surface", PrimitiveType.Sphere, parent, Vector3.zero, new Vector3(10f, 10f, 0.35f), stormIndigo, false);

            for (int i = 0; i < 14; i++)
            {
                float angle = i * Mathf.PI * 2f / 14f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 5.8f, Mathf.Sin(angle) * 5.8f, -0.4f);
                GameObject segment = CreatePrimitive("DormantExitPortal_Frame_" + i.ToString("00"), PrimitiveType.Cube, parent, position, new Vector3(0.55f, 0.95f, 0.78f), i % 2 == 0 ? toxicGreen : darkMetal, false);
                segment.transform.localRotation = Quaternion.Euler(0f, 0f, -angle * Mathf.Rad2Deg);
            }

            Transform interact = CreateChild(parent, "PortalInteractionPoint");
            interact.localPosition = new Vector3(0f, -1.05f, 3.5f);
            CreatePrimitive("DormantMarker", PrimitiveType.Cylinder, interact, Vector3.zero, new Vector3(1.4f, 0.08f, 1.4f), toxicGreen, false);
        }

        private static void CreateDecor(Transform hiveGrowths, Transform crystals, Transform background, Transform stormVfx, Transform vfx)
        {
            Vector3[] growthAnchors =
            {
                new Vector3(-46f, 0.2f, 46f), new Vector3(46f, 0.2f, 46f),
                new Vector3(-68f, 0.2f, -18f), new Vector3(68f, 0.2f, -18f),
                new Vector3(-20f, 0.2f, -52f), new Vector3(20f, 0.2f, -52f)
            };

            for (int i = 0; i < growthAnchors.Length; i++)
            {
                CreateHiveGrowthCluster(hiveGrowths, "HiveGrowthCluster_" + i.ToString("00"), growthAnchors[i], i % 2 == 0 ? bloodRed : hivePurple);
            }

            CreateCrystalCluster(crystals, "ToxicCrystal_NorthWest", new Vector3(-28f, 0.3f, 48f), toxicGreen);
            CreateCrystalCluster(crystals, "ToxicCrystal_NorthEast", new Vector3(28f, 0.3f, 48f), toxicGreen);
            CreateCrystalCluster(crystals, "VioletCrystal_West", new Vector3(-76f, 0.3f, -36f), hivePurple);
            CreateCrystalCluster(crystals, "VioletCrystal_East", new Vector3(76f, 0.3f, -36f), hivePurple);

            for (int i = 0; i < 8; i++)
            {
                float x = -84f + i * 24f;
                float height = i % 2 == 0 ? 12f : 18f;
                CreatePrimitive("DistantHiveSilhouette_North_" + i.ToString("00"), PrimitiveType.Cube, background, new Vector3(x, height * 0.5f, 83f), new Vector3(9f, height, 5f), blackStone, false);
                CreatePrimitive("DistantHiveSilhouette_South_" + i.ToString("00"), PrimitiveType.Cube, background, new Vector3(x, height * 0.38f, -83f), new Vector3(8f, height * 0.75f, 5f), blackStone, false);
            }

            for (int i = 0; i < 7; i++)
            {
                float x = -66f + i * 22f;
                CreatePrimitive("StormColumn_" + i.ToString("00"), PrimitiveType.Cylinder, stormVfx, new Vector3(x, 10f, 65f), new Vector3(0.45f, 10f, 0.45f), i % 2 == 0 ? toxicGreen : hivePurple, false);
            }

            CreatePrimitive("LowToxicMist_Field", PrimitiveType.Cube, vfx, new Vector3(0f, 0.02f, 0f), new Vector3(176f, 0.04f, 146f), stormIndigo, false);
        }

        private static void CreateLighting(Transform parent)
        {
            CreatePointLight(parent, "SwarmCentral_ToxicFill", new Vector3(0f, 10f, 0f), new Color(0.49f, 1f, 0.31f), 2.8f, 78f);
            CreatePointLight(parent, "NorthRift_PurpleStormLight", new Vector3(0f, 12f, 56f), new Color(0.64f, 0.28f, 1f), 5.2f, 70f);
            CreatePointLight(parent, "WestRift_BloodGlow", new Vector3(-64f, 9f, 10f), new Color(0.9f, 0.12f, 0.18f), 3.4f, 48f);
            CreatePointLight(parent, "EastRift_BloodGlow", new Vector3(64f, 9f, 10f), new Color(0.9f, 0.12f, 0.18f), 3.4f, 48f);
            CreatePointLight(parent, "Arrival_ReadabilityFill", new Vector3(0f, 8f, -60f), new Color(0.49f, 1f, 0.31f), 2.2f, 42f);

            GameObject moon = new GameObject("PurpleStorm_KeyLight");
            moon.transform.SetParent(parent, false);
            moon.transform.localRotation = Quaternion.Euler(50f, -25f, 0f);
            Light light = moon.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.45f, 0.32f, 0.9f);
            light.intensity = 0.55f;
        }

        private static void CreateFiringPlatform(Transform parent, string name, Vector3 position, Vector3 scale, Material accent)
        {
            CreatePrimitive(name, PrimitiveType.Cube, parent, position, scale, darkMetal, false);
            CreatePrimitive(name + "_EdgeGlow", PrimitiveType.Cube, parent, position + Vector3.up * 0.14f, new Vector3(scale.x * 0.85f, 0.04f, 0.28f), accent, false);
        }

        private static void CreatePathSegment(Transform parent, string name, Vector3 start, Vector3 end, float width, Material accent)
        {
            CreateSegment(name + "_DarkPlate", parent, start, end, width, 0.12f, darkMetal, false, 0f);
            CreateSegment(name + "_CenterGlow", parent, start + Vector3.up * 0.09f, end + Vector3.up * 0.09f, 0.36f, 0.05f, accent, false, 0f);
            CreateSegment(name + "_LeftEdgeGlow", parent, start + Vector3.up * 0.1f, end + Vector3.up * 0.1f, 0.18f, 0.05f, accent, false, -width * 0.42f);
            CreateSegment(name + "_RightEdgeGlow", parent, start + Vector3.up * 0.1f, end + Vector3.up * 0.1f, 0.18f, 0.05f, accent, false, width * 0.42f);
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
            CreatePrimitive(name + "_BioGlowEdge", PrimitiveType.Cube, parent, position + Vector3.up * 1.05f, glowScale, accent, false);
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
            Renderer ring = CreatePrimitive("Pad_BioEmissiveRing", PrimitiveType.Cylinder, pad.transform, new Vector3(0f, 0.16f, 0f), new Vector3(4.35f, 0.08f, 4.35f), accent, false).GetComponent<Renderer>();
            CreatePrimitive("Pad_DarkSocket", PrimitiveType.Cylinder, pad.transform, new Vector3(0f, 0.28f, 0f), new Vector3(2.1f, 0.12f, 2.1f), ground, false);

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

        private static void CreateMarker(Transform parent, string name, Vector3 position, Material material)
        {
            Transform marker = CreateChild(parent, name);
            marker.localPosition = position;
            CreatePrimitive("MarkerDisk", PrimitiveType.Cylinder, marker, Vector3.zero, new Vector3(0.8f, 0.06f, 0.8f), material, false);
        }

        private static void CreateWaypointPath(Transform parent, string name, Vector3[] points, Material material)
        {
            Transform path = CreateChild(parent, name);
            for (int i = 0; i < points.Length; i++)
            {
                CreatePrimitive("Waypoint_" + (i + 1).ToString("00"), PrimitiveType.Cylinder, path, points[i], new Vector3(0.85f, 0.05f, 0.85f), material, false);
            }
        }

        private static void CreateHiveGrowthCluster(Transform parent, string name, Vector3 anchor, Material material)
        {
            Transform cluster = CreateChild(parent, name);
            cluster.localPosition = anchor;
            CreatePrimitive("GrowthRoot", PrimitiveType.Sphere, cluster, new Vector3(0f, 0.25f, 0f), new Vector3(4.2f, 0.8f, 3.4f), material, false);

            for (int i = 0; i < 5; i++)
            {
                float angle = i * Mathf.PI * 2f / 5f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 1.8f, 1.1f + i * 0.15f, Mathf.Sin(angle) * 1.4f);
                GameObject tendril = CreatePrimitive("Tendril_" + i.ToString("00"), PrimitiveType.Cylinder, cluster, position, new Vector3(0.32f, 1.6f + i * 0.22f, 0.32f), material, false);
                tendril.transform.localRotation = Quaternion.Euler(18f, angle * Mathf.Rad2Deg, 12f);
            }
        }

        private static void CreateCrystalCluster(Transform parent, string name, Vector3 anchor, Material material)
        {
            Transform cluster = CreateChild(parent, name);
            cluster.localPosition = anchor;

            for (int i = 0; i < 6; i++)
            {
                float angle = i * Mathf.PI * 2f / 6f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * (0.8f + i * 0.18f), 0.85f + i * 0.2f, Mathf.Sin(angle) * (0.8f + i * 0.15f));
                GameObject shard = CreatePrimitive("CrystalShard_" + i.ToString("00"), PrimitiveType.Cylinder, cluster, position, new Vector3(0.38f, 1.2f + i * 0.24f, 0.38f), material, false);
                shard.transform.localRotation = Quaternion.Euler(8f + i * 5f, angle * Mathf.Rad2Deg, 16f);
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
            GameObject cameraObject = new GameObject("PN_TempPlanet4CaptureCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("070A14");
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
            ground = Material("PN_P4_Ground", Hex("15121E"), new Color(0.015f, 0.01f, 0.03f), 0.55f, 0.58f);
            darkMetal = Material("PN_P4_DarkMetal", Hex("242030"), new Color(0.025f, 0.018f, 0.045f), 0.82f, 0.68f);
            blackStone = Material("PN_P4_BlackStone", Hex("0D0B12"), new Color(0.01f, 0.006f, 0.018f), 0.25f, 0.42f);
            toxicGreen = Material("PN_P4_ToxicGreen", Hex("7CFF4F"), new Color(0.7f, 3.4f, 0.42f), 0f, 0.9f);
            hivePurple = Material("PN_P4_HivePurple", Hex("A448FF"), new Color(1.45f, 0.5f, 3.5f), 0f, 0.93f);
            bloodRed = Material("PN_P4_BloodRed", Hex("C1272D"), new Color(2.3f, 0.2f, 0.24f), 0.1f, 0.72f);
            portalCore = Material("PN_P4_PortalCore_GreenViolet", Hex("58FF85"), new Color(0.62f, 3.2f, 1.2f), 0f, 0.95f);
            stormIndigo = Material("PN_P4_StormIndigo", Hex("120B28"), new Color(0.18f, 0.06f, 0.38f), 0.12f, 0.55f);
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
