using System.IO;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PortalNights.EditorTools
{
    public static class PortalNightsPlanet3SceneBuilder
    {
        private const string Root = "Assets/PortalNights";
        private const string MaterialsDir = Root + "/Materials";
        private const string ScenePath = Root + "/Scenes/PortalNightsArena.unity";
        private const int BuildPadCost = 120;

        private static Material floor;
        private static Material darkMetal;
        private static Material hotAccent;
        private static Material lava;
        private static Material emergencyRed;
        private static Material techBlue;
        private static Material relayCyan;
        private static Material safeGreen;
        private static Material riftViolet;
        private static Material smokeDark;

        [MenuItem("Portal Nights/Rebuild Planet 3 Ash Relay Station Only")]
        public static void BuildPlanet3AshRelayStation()
        {
            EnsureMaterials();
            Scene scene = SceneManager.GetActiveScene();
            if (!scene.IsValid() || scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            GameObject arenaObject = GameObject.Find("PortalNightsArena");
            if (arenaObject == null)
            {
                Debug.LogError("[PortalNights] PortalNightsArena was not found. Planet 3 was not built.");
                return;
            }

            Transform existing = arenaObject.transform.Find("Planet3_AshRelayStation");
            if (existing != null)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            Transform root = CreateChild(arenaObject.transform, "Planet3_AshRelayStation");
            root.localPosition = new Vector3(0f, 0f, 240f);
            root.localRotation = Quaternion.identity;
            root.localScale = Vector3.one;

            BuildHierarchy(root);
            UpdateControllerDefaults(root);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[PortalNights] Planet 3 Ash Relay Station rebuilt. Footprint: 160 x 135 units.");
        }

        private static void BuildHierarchy(Transform root)
        {
            Transform environment = CreateChild(root, "Environment");
            Transform floors = CreateChild(environment, "Floors");
            Transform boundaries = CreateChild(environment, "Boundaries");
            Transform bridges = CreateChild(environment, "Bridges");
            Transform lavaBelow = CreateChild(environment, "LavaBelow");
            Transform pipes = CreateChild(environment, "Pipes");
            Transform generators = CreateChild(environment, "Generators");
            Transform background = CreateChild(environment, "BackgroundStructures");
            Transform arrival = CreateChild(root, "ArrivalZone");
            Transform central = CreateChild(root, "CentralStation");
            Transform staff1 = CreateChild(root, "Staff1Zone_MedicalLab");
            Transform staff2 = CreateChild(root, "Staff2Zone_EngineeringBay");
            Transform rifts = CreateChild(root, "EnemyRifts");
            Transform waypoints = CreateChild(root, "Waypoints");
            Transform lighting = CreateChild(root, "Lighting");
            Transform vfx = CreateChild(root, "VFX");

            CreateFloors(floors, bridges);
            CreateBoundaries(boundaries);
            CreateLava(lavaBelow);
            CreateArrival(arrival);
            CreateCentralStation(central);
            CreateStaffZone(staff1, "Staff_01", "MEDICAL LAB", new Vector3(-62f, 0f, 25f), techBlue);
            CreateStaffZone(staff2, "Staff_02", "ENGINEERING BAY", new Vector3(62f, 0f, 25f), hotAccent);
            CreateRift(rifts, "NorthEnemyRift", new Vector3(0f, 2.5f, 65f), 12.5f);
            CreateRift(rifts, "WestEnemyRift", new Vector3(-76f, 2.5f, -8f), 9.5f);
            CreateRift(rifts, "EastEnemyRift", new Vector3(76f, 2.5f, -8f), 9.5f);
            CreateWaypoints(waypoints);
            CreateBuildPads(central);
            CreateDecor(pipes, generators, background, vfx);
            CreateLighting(lighting);
        }

        private static void CreateFloors(Transform floors, Transform bridges)
        {
            CreatePrimitive("AshRelay_ContinuousDeck_160x135", PrimitiveType.Cube, floors, new Vector3(0f, -0.28f, 2.5f), new Vector3(160f, 0.5f, 135f), floor, true);
            CreatePrimitive("ArrivalZone_24x18", PrimitiveType.Cube, floors, new Vector3(0f, 0.05f, -58f), new Vector3(24f, 0.12f, 18f), darkMetal, false);
            CreatePrimitive("CentralPlaza_55", PrimitiveType.Cylinder, floors, Vector3.zero, new Vector3(55f, 0.16f, 55f), darkMetal, false);
            CreatePrimitive("DefenseRing_Outer", PrimitiveType.Cylinder, floors, Vector3.up * 0.14f, new Vector3(34f, 0.08f, 34f), hotAccent, false);
            CreatePrimitive("DefenseRing_Inner", PrimitiveType.Cylinder, floors, Vector3.up * 0.2f, new Vector3(24f, 0.08f, 24f), floor, false);
            CreatePrimitive("SphereSafeZone_GreenRing", PrimitiveType.Cylinder, floors, Vector3.up * 0.26f, new Vector3(22f, 0.055f, 22f), safeGreen, false);

            CreatePath(bridges, "Arrival_To_Relay", new Vector3(0f, 0.16f, -30f), new Vector3(14f, 0.12f, 56f), techBlue);
            CreatePath(bridges, "Relay_To_Staff1", new Vector3(-32f, 0.17f, 14f), new Vector3(62f, 0.12f, 13f), techBlue);
            CreatePath(bridges, "Relay_To_Staff2", new Vector3(32f, 0.17f, 14f), new Vector3(62f, 0.12f, 13f), hotAccent);
            CreatePath(bridges, "NorthRift_To_Relay", new Vector3(0f, 0.18f, 37f), new Vector3(14f, 0.12f, 58f), emergencyRed);
            CreatePath(bridges, "WestRift_To_Relay", new Vector3(-39f, 0.18f, -4f), new Vector3(74f, 0.12f, 12f), hotAccent);
            CreatePath(bridges, "EastRift_To_Relay", new Vector3(39f, 0.18f, -4f), new Vector3(74f, 0.12f, 12f), hotAccent);
        }

        private static void CreateBoundaries(Transform parent)
        {
            CreateWall(parent, "NorthBoundary", new Vector3(0f, 1.2f, 70.2f), new Vector3(162f, 2.4f, 1.5f));
            CreateWall(parent, "SouthBoundary", new Vector3(0f, 1.2f, -66.8f), new Vector3(162f, 2.4f, 1.5f));
            CreateWall(parent, "WestBoundary", new Vector3(-81.2f, 1.2f, 2.5f), new Vector3(1.5f, 2.4f, 136f));
            CreateWall(parent, "EastBoundary", new Vector3(81.2f, 1.2f, 2.5f), new Vector3(1.5f, 2.4f, 136f));
        }

        private static void CreateArrival(Transform parent)
        {
            Transform player = CreateChild(parent, "PlayerArrivalPoint");
            player.localPosition = new Vector3(0f, 1.12f, -58f);
            Transform ally = CreateChild(parent, "AllyArrivalPoint");
            ally.localPosition = new Vector3(2.4f, 1.12f, -58f);
            CreateWorldLabel(parent, "ASH RELAY STATION", new Vector3(0f, 1.05f, -65f), hotAccent.color);
        }

        private static void CreateCentralStation(Transform parent)
        {
            GameObject relay = new GameObject("RelaySphere");
            relay.transform.SetParent(parent, false);
            relay.transform.localPosition = Vector3.zero;
            relay.AddComponent<NetworkObject>();
            PortalNightsHealth health = relay.AddComponent<PortalNightsHealth>();
            SetBaseHealthForScene(health, 1800f);
            SphereCollider collider = relay.AddComponent<SphereCollider>();
            collider.center = new Vector3(0f, 2f, 0f);
            collider.radius = 2.5f;
            collider.isTrigger = true;
            CreatePrimitive("RelaySphere_Core", PrimitiveType.Sphere, relay.transform, new Vector3(0f, 2f, 0f), new Vector3(4.4f, 4.4f, 4.4f), relayCyan, false);
            CreatePrimitive("RelaySphere_Shield", PrimitiveType.Sphere, relay.transform, new Vector3(0f, 2f, 0f), new Vector3(8.4f, 8.4f, 8.4f), relayCyan, false);
            Light light = relay.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.39f, 0.97f, 1f);
            light.range = 34f;
            light.intensity = 7f;
            CreateChild(parent, "SphereSafeZone").localPosition = Vector3.zero;
            CreateChild(parent, "DefenseRing").localPosition = Vector3.zero;
        }

        private static void CreateStaffZone(Transform parent, string staffName, string label, Vector3 localPosition, Material accent)
        {
            parent.localPosition = localPosition;
            CreatePrimitive(label.Replace(" ", "_") + "_Floor", PrimitiveType.Cube, parent, Vector3.zero, new Vector3(28f, 0.18f, 24f), darkMetal, false);
            CreatePrimitive("ReleaseConsole", PrimitiveType.Cube, parent, new Vector3(0f, 0.65f, -8f), new Vector3(3.6f, 1.3f, 1.4f), accent, false);
            CreateWorldLabel(parent, label, new Vector3(0f, 1.1f, -11f), accent.color);

            GameObject staff = new GameObject(staffName);
            staff.transform.SetParent(parent, false);
            staff.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            staff.AddComponent<NetworkObject>();
            staff.AddComponent<NetworkTransform>();
            CapsuleCollider capsule = staff.AddComponent<CapsuleCollider>();
            capsule.height = 1.65f;
            capsule.radius = 0.32f;
            capsule.center = new Vector3(0f, 0.78f, 0f);
            capsule.isTrigger = true;
            PortalNightsHealth health = staff.AddComponent<PortalNightsHealth>();
            SetBaseHealthForScene(health, 150f);
            CreatePrimitive("Staff_Body", PrimitiveType.Capsule, staff.transform, new Vector3(0f, 0.78f, 0f), new Vector3(0.65f, 0.85f, 0.65f), accent, false);
            Renderer marker = CreatePrimitive("Marker", PrimitiveType.Sphere, staff.transform, new Vector3(0f, 2.05f, 0f), new Vector3(0.55f, 0.55f, 0.55f), emergencyRed, false).GetComponent<Renderer>();
            Light light = staff.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 6f;
            light.intensity = 1.8f;
            PortalNightsStaffRescue rescue = staff.AddComponent<PortalNightsStaffRescue>();
            rescue.Configure(staffName, marker, light);
        }

        private static void CreateRift(Transform parent, string name, Vector3 localPosition, float diameter)
        {
            GameObject riftObject = new GameObject(name);
            riftObject.transform.SetParent(parent, false);
            riftObject.transform.localPosition = localPosition;
            Vector3 toRelay = PortalNightsMath.Flat(Vector3.zero - localPosition);
            riftObject.transform.localRotation = toRelay.sqrMagnitude <= 0.001f ? Quaternion.identity : Quaternion.LookRotation(toRelay.normalized, Vector3.up);
            riftObject.AddComponent<NetworkObject>();
            CreatePrimitive("RiftPlatform", PrimitiveType.Cylinder, riftObject.transform, new Vector3(0f, -2.35f, 0f), new Vector3(diameter + 4f, 0.22f, diameter + 4f), darkMetal, false);
            Renderer surface = CreatePrimitive("RiftSurface", PrimitiveType.Sphere, riftObject.transform, Vector3.zero, new Vector3(diameter, diameter, 0.42f), riftViolet, false).GetComponent<Renderer>();
            CreatePrimitive("RiftFrame_Left", PrimitiveType.Cube, riftObject.transform, new Vector3(-diameter * 0.58f, 0.6f, -0.45f), new Vector3(1f, diameter * 0.95f, 1.2f), darkMetal, true);
            CreatePrimitive("RiftFrame_Right", PrimitiveType.Cube, riftObject.transform, new Vector3(diameter * 0.58f, 0.6f, -0.45f), new Vector3(1f, diameter * 0.95f, 1.2f), darkMetal, true);
            Transform spawn = CreateChild(riftObject.transform, "SpawnPoint");
            spawn.localPosition = new Vector3(0f, -2.1f, 4.4f);
            Light light = riftObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.12f, 0.42f);
            light.range = 28f;
            light.intensity = 5.5f;
            PortalNightsEnemyRift rift = riftObject.AddComponent<PortalNightsEnemyRift>();
            rift.Configure(name, spawn, surface, light);
        }

        private static void CreateBuildPads(Transform central)
        {
            Transform pads = CreateChild(central, "BuildPads");
            Vector3[] standard =
            {
                new Vector3(-8f, 0.32f, 32f), new Vector3(8f, 0.32f, 32f),
                new Vector3(-58f, 0.32f, -4f), new Vector3(-36f, 0.32f, -6f),
                new Vector3(58f, 0.32f, -4f), new Vector3(36f, 0.32f, -6f),
                new Vector3(-38f, 0.32f, 18f), new Vector3(38f, 0.32f, 18f),
                new Vector3(-13f, 0.32f, -8f), new Vector3(13f, 0.32f, -8f)
            };

            for (int i = 0; i < standard.Length; i++)
            {
                CreateBuildPad(pads, "TurretPad_" + (i + 1).ToString("00"), standard[i], i % 3 == 0 ? emergencyRed : i % 3 == 1 ? hotAccent : techBlue);
            }

            Transform utility = CreateChild(central, "UtilityPads");
            CreateBuildPad(utility, "UtilityPad_Left", new Vector3(-8f, 0.32f, 8f), safeGreen);
            CreateBuildPad(utility, "UtilityPad_Right", new Vector3(8f, 0.32f, 8f), safeGreen);
        }

        private static void CreateBuildPad(Transform parent, string name, Vector3 position, Material accent)
        {
            GameObject pad = new GameObject(name);
            pad.transform.SetParent(parent, false);
            pad.transform.localPosition = position;
            Vector3 facing = PortalNightsMath.Flat(Vector3.zero - position);
            pad.transform.localRotation = facing.sqrMagnitude <= 0.001f ? Quaternion.identity : Quaternion.LookRotation(facing.normalized, Vector3.up);
            pad.AddComponent<NetworkObject>();
            BoxCollider collider = pad.AddComponent<BoxCollider>();
            collider.size = new Vector3(4.8f, 0.55f, 4.8f);
            collider.center = new Vector3(0f, 0.18f, 0f);
            CreatePrimitive("Pad_Base", PrimitiveType.Cylinder, pad.transform, Vector3.zero, new Vector3(4.8f, 0.22f, 4.8f), darkMetal, false);
            Renderer ring = CreatePrimitive("Pad_Ring", PrimitiveType.Cylinder, pad.transform, new Vector3(0f, 0.16f, 0f), new Vector3(4.2f, 0.08f, 4.2f), accent, false).GetComponent<Renderer>();
            CreatePrimitive("Pad_SocketBase", PrimitiveType.Cylinder, pad.transform, new Vector3(0f, 0.27f, 0f), new Vector3(2.1f, 0.12f, 2.1f), floor, false);
            Transform socket = CreateChild(pad.transform, "TurretSocket");
            socket.localPosition = new Vector3(0f, 0.58f, 0f);
            Light light = pad.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 6f;
            light.intensity = 1.8f;
            light.color = accent.color;
            PortalNightsBuildPoint buildPoint = pad.AddComponent<PortalNightsBuildPoint>();
            buildPoint.Configure(BuildPadCost, 180, 260, socket, ring, light, false);
        }

        private static void CreateWaypoints(Transform parent)
        {
            CreateWaypointPath(parent, "NorthRift_To_RelaySphere", new[] { new Vector3(0f, 0.25f, 65f), new Vector3(0f, 0.25f, 32f), new Vector3(0f, 0.25f, 0f) }, emergencyRed);
            CreateWaypointPath(parent, "WestRift_To_RelaySphere", new[] { new Vector3(-76f, 0.25f, -8f), new Vector3(-38f, 0.25f, -5f), new Vector3(0f, 0.25f, 0f) }, hotAccent);
            CreateWaypointPath(parent, "EastRift_To_RelaySphere", new[] { new Vector3(76f, 0.25f, -8f), new Vector3(38f, 0.25f, -5f), new Vector3(0f, 0.25f, 0f) }, hotAccent);
            CreateWaypointPath(parent, "Staff1_To_RelaySphere", new[] { new Vector3(-62f, 0.25f, 25f), new Vector3(-31f, 0.25f, 15f), new Vector3(0f, 0.25f, 0f) }, techBlue);
            CreateWaypointPath(parent, "Staff2_To_RelaySphere", new[] { new Vector3(62f, 0.25f, 25f), new Vector3(31f, 0.25f, 15f), new Vector3(0f, 0.25f, 0f) }, techBlue);
        }

        private static void CreateDecor(Transform pipes, Transform generators, Transform background, Transform vfx)
        {
            for (int i = 0; i < 10; i++)
            {
                float x = -64f + i * 14f;
                CreatePrimitive("HeatPipe_" + i, PrimitiveType.Cylinder, pipes, new Vector3(x, 0.55f, -42f), new Vector3(0.45f, 7f, 0.45f), hotAccent, false).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }

            CreatePrimitive("NorthGenerator_Left", PrimitiveType.Cube, generators, new Vector3(-18f, 1.4f, 50f), new Vector3(5f, 2.8f, 5f), darkMetal, true);
            CreatePrimitive("NorthGenerator_Right", PrimitiveType.Cube, generators, new Vector3(18f, 1.4f, 50f), new Vector3(5f, 2.8f, 5f), darkMetal, true);
            CreatePrimitive("AshSky_Backdrop", PrimitiveType.Cube, background, new Vector3(0f, 22f, 78f), new Vector3(190f, 42f, 1f), smokeDark, false);
            CreatePrimitive("LavaSmokePlane", PrimitiveType.Cube, vfx, new Vector3(0f, -2.8f, 2.5f), new Vector3(166f, 0.08f, 140f), lava, false);
        }

        private static void CreateLava(Transform lavaBelow)
        {
            CreatePrimitive("LavaBelow_MainGlow", PrimitiveType.Cube, lavaBelow, new Vector3(0f, -3.4f, 2.5f), new Vector3(168f, 0.18f, 142f), lava, false);
        }

        private static void CreateLighting(Transform parent)
        {
            CreatePointLight(parent, "Relay_CyanFill", new Vector3(0f, 12f, 0f), new Color(0.39f, 0.97f, 1f), 3.2f, 72f);
            CreatePointLight(parent, "North_RiftFill", new Vector3(0f, 11f, 52f), new Color(1f, 0.15f, 0.35f), 3.6f, 58f);
            CreatePointLight(parent, "West_EmergencyFill", new Vector3(-50f, 8f, -6f), new Color(1f, 0.34f, 0.1f), 2.6f, 46f);
            CreatePointLight(parent, "East_EmergencyFill", new Vector3(50f, 8f, -6f), new Color(1f, 0.34f, 0.1f), 2.6f, 46f);
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

        private static void CreatePath(Transform parent, string name, Vector3 position, Vector3 scale, Material accent)
        {
            CreatePrimitive(name + "_DarkMetal", PrimitiveType.Cube, parent, position, scale, darkMetal, false);
            CreatePrimitive(name + "_GlowLine", PrimitiveType.Cube, parent, position + Vector3.up * 0.08f, new Vector3(scale.x * 0.72f, 0.04f, 0.18f), accent, false);
        }

        private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            CreatePrimitive(name, PrimitiveType.Cube, parent, position, scale, darkMetal, true);
            CreatePrimitive(name + "_HotEdge", PrimitiveType.Cube, parent, position + Vector3.up * 0.85f, scale.x > scale.z ? new Vector3(scale.x - 8f, 0.12f, 0.18f) : new Vector3(0.18f, 0.12f, scale.z - 8f), hotAccent, false);
        }

        private static void CreateWaypointPath(Transform parent, string name, Vector3[] points, Material material)
        {
            Transform path = CreateChild(parent, name);
            for (int i = 0; i < points.Length; i++)
            {
                CreatePrimitive("Waypoint_" + (i + 1).ToString("00"), PrimitiveType.Cylinder, path, points[i], new Vector3(0.8f, 0.05f, 0.8f), material, false);
            }
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

        private static void UpdateControllerDefaults(Transform root)
        {
            PortalNightsGameController controller = Object.FindFirstObjectByType<PortalNightsGameController>();
            if (controller == null)
            {
                return;
            }

            SerializedObject serializedController = new SerializedObject(controller);
            SerializedProperty center = serializedController.FindProperty("planet3Center");
            if (center != null)
            {
                center.vector3Value = root.position;
            }

            SerializedProperty halfExtents = serializedController.FindProperty("planet3HalfExtents");
            if (halfExtents != null)
            {
                halfExtents.vector2Value = new Vector2(82f, 70f);
            }

            SerializedProperty safeZone = serializedController.FindProperty("planet3SafeZoneRadius");
            if (safeZone != null)
            {
                safeZone.floatValue = 11f;
            }

            serializedController.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void SetBaseHealthForScene(PortalNightsHealth health, float value)
        {
            SerializedObject serializedHealth = new SerializedObject(health);
            SerializedProperty baseMaxHealth = serializedHealth.FindProperty("baseMaxHealth");
            if (baseMaxHealth != null)
            {
                baseMaxHealth.floatValue = value;
            }

            serializedHealth.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(health);
        }

        private static void EnsureMaterials()
        {
            Directory.CreateDirectory(MaterialsDir);
            floor = Material("PN_P3_Floor", Hex("1A1715"), new Color(0.04f, 0.025f, 0.018f), 0.62f, 0.58f);
            darkMetal = Material("PN_P3_DarkMetal", Hex("2A2522"), new Color(0.025f, 0.02f, 0.018f), 0.82f, 0.66f);
            hotAccent = Material("PN_P3_HotAccent", Hex("FF6A2A"), new Color(3.2f, 0.85f, 0.22f), 0.2f, 0.82f);
            lava = Material("PN_P3_LavaGlow", Hex("FF3C1C"), new Color(4.5f, 0.45f, 0.12f), 0f, 0.92f);
            emergencyRed = Material("PN_P3_EmergencyRed", Hex("FF2F2F"), new Color(4f, 0.18f, 0.18f), 0.1f, 0.82f);
            techBlue = Material("PN_P3_TechBlue", Hex("3FCBFF"), new Color(0.35f, 2.2f, 3.1f), 0.12f, 0.88f);
            relayCyan = Material("PN_P3_RelayCyan", Hex("63F7FF"), new Color(0.55f, 3.4f, 4.2f), 0f, 0.95f);
            safeGreen = Material("PN_P3_SafeGreen", Hex("7CFFB2"), new Color(0.6f, 3.2f, 1.4f), 0f, 0.88f);
            riftViolet = Material("PN_P3_RiftViolet", new Color(0.68f, 0.12f, 1f), new Color(2.9f, 0.2f, 4.2f), 0f, 0.95f);
            smokeDark = Material("PN_P3_SmokeDark", new Color(0.06f, 0.045f, 0.042f), new Color(0.08f, 0.02f, 0.01f), 0.2f, 0.38f);
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
