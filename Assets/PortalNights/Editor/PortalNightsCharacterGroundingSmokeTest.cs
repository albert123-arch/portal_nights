#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using PortalNights.Visuals;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PortalNights.EditorTools
{
    public static class PortalNightsCharacterGroundingSmokeTest
    {
        private const float Tolerance = 0.05f;
        private const string BatchRunningKey = "PortalNights.GroundingSmoke.BatchRunning";
        private const string BatchHasResultKey = "PortalNights.GroundingSmoke.HasResult";
        private const string BatchResultKey = "PortalNights.GroundingSmoke.Result";
        private const string BatchRanInPlayModeKey = "PortalNights.GroundingSmoke.RanInPlayMode";

        private static bool batchPlayModeRunning;
        private static bool batchPlayModeResult;
        private static bool batchPlayModeHasResult;

        [InitializeOnLoadMethod]
        private static void ResumeBatchPlayModeAfterReload()
        {
            if (!SessionState.GetBool(BatchRunningKey, false))
            {
                return;
            }

            batchPlayModeRunning = true;
            EditorApplication.playModeStateChanged -= HandleBatchPlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandleBatchPlayModeStateChanged;
            EditorApplication.delayCall += ResumeBatchPlayModeDelayed;
        }

        private static void ResumeBatchPlayModeDelayed()
        {
            if (!SessionState.GetBool(BatchRunningKey, false))
            {
                return;
            }

            if (SessionState.GetBool(BatchHasResultKey, false) && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                CompleteBatchPlayMode();
                return;
            }

            if (EditorApplication.isPlaying && !SessionState.GetBool(BatchRanInPlayModeKey, false))
            {
                RunPlayModeSmokeAndExit();
            }
        }

        private struct GroundingCase
        {
            public int planet;
            public string label;
            public PortalNightsEnemyVisualKind kind;
            public float height;
            public bool staff;
        }

        [MenuItem("Portal Nights/Tests/Character Grounding Smoke Test")]
        public static void RunMenu()
        {
            bool passed = RunSmokeTest(out string report);
            Debug.Log(report);
            if (!passed)
            {
                Debug.LogError("[PortalNightsCharacterGroundingSmokeTest] Grounding smoke test failed.");
            }
        }

        public static void RunBatch()
        {
            bool passed = RunSmokeTest(out string report);
            Debug.Log(report);
            EditorApplication.Exit(passed ? 0 : 1);
        }

        public static void RunBatchArenaPortalBridge()
        {
            bool passed = RunArenaPortalBridgeTest(out string report);
            Debug.Log(report);
            EditorApplication.Exit(passed ? 0 : 1);
        }

        public static void RunBatchPlayMode()
        {
            batchPlayModeRunning = true;
            batchPlayModeResult = false;
            batchPlayModeHasResult = false;
            SessionState.SetBool(BatchRunningKey, true);
            SessionState.SetBool(BatchHasResultKey, false);
            SessionState.SetBool(BatchResultKey, false);
            SessionState.SetBool(BatchRanInPlayModeKey, false);
            EditorApplication.playModeStateChanged += HandleBatchPlayModeStateChanged;
            EditorApplication.EnterPlaymode();
        }

        private static void HandleBatchPlayModeStateChanged(PlayModeStateChange state)
        {
            if (!batchPlayModeRunning)
            {
                return;
            }

            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                RunPlayModeSmokeAndExit();
                return;
            }

            if (state == PlayModeStateChange.EnteredEditMode && (batchPlayModeHasResult || SessionState.GetBool(BatchHasResultKey, false)))
            {
                CompleteBatchPlayMode();
            }
        }

        private static void RunPlayModeSmokeAndExit()
        {
            if (SessionState.GetBool(BatchRanInPlayModeKey, false))
            {
                return;
            }

            SessionState.SetBool(BatchRanInPlayModeKey, true);
            batchPlayModeResult = RunSmokeTest(out string report);
            batchPlayModeHasResult = true;
            SessionState.SetBool(BatchResultKey, batchPlayModeResult);
            SessionState.SetBool(BatchHasResultKey, true);
            Debug.Log("[PLAYMODE GROUNDING SMOKE]\n" + report);
            EditorApplication.ExitPlaymode();
        }

        private static void CompleteBatchPlayMode()
        {
            EditorApplication.playModeStateChanged -= HandleBatchPlayModeStateChanged;
            bool result = batchPlayModeHasResult ? batchPlayModeResult : SessionState.GetBool(BatchResultKey, false);
            batchPlayModeRunning = false;
            batchPlayModeHasResult = false;
            SessionState.SetBool(BatchRunningKey, false);
            SessionState.SetBool(BatchHasResultKey, false);
            SessionState.SetBool(BatchResultKey, false);
            SessionState.SetBool(BatchRanInPlayModeKey, false);
            EditorApplication.Exit(result ? 0 : 1);
        }

        public static bool RunSmokeTest(out string report)
        {
            Scene previousScene = SceneManager.GetActiveScene();
            if (Application.isPlaying)
            {
                SceneManager.SetActiveScene(SceneManager.CreateScene("PortalNights_GroundingSmokeRuntime"));
            }
            else
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            var lines = new StringBuilder();
            int failures = 0;
            lines.AppendLine("# Portal Nights Character Grounding Smoke Test");
            lines.AppendLine();
            lines.AppendLine("| Planet | Visual | Target height | Root Y | Floor Y | Collider bottom | Visual bottom | Visual error | Result |");
            lines.AppendLine("| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | --- |");

            var cases = new List<GroundingCase>
            {
                EnemyCase(1, "P1_Ch40", PortalNightsEnemyVisualKind.Ch40, false),
                EnemyCase(1, "P1_Warrok", PortalNightsEnemyVisualKind.Warrok, false),
                EnemyCase(2, "P2_Parasite", PortalNightsEnemyVisualKind.Parasite, false),
                EnemyCase(2, "P2_Maw", PortalNightsEnemyVisualKind.Maw, false),
                EnemyCase(2, "P2_Vampire", PortalNightsEnemyVisualKind.Vampire, false),
                EnemyCase(3, "P3_Mutant", PortalNightsEnemyVisualKind.Mutant, false),
                EnemyCase(3, "P3_Ch50", PortalNightsEnemyVisualKind.Ch50, false),
                EnemyCase(3, "P3_Staff_Ch32", PortalNightsEnemyVisualKind.None, true),
                EnemyCase(4, "P4_Parasite", PortalNightsEnemyVisualKind.Parasite, false),
                EnemyCase(4, "P4_Warrok", PortalNightsEnemyVisualKind.Warrok, false),
                EnemyCase(4, "P4_PumpkinhulkNormal", PortalNightsEnemyVisualKind.Pumpkinhulk, false),
                EnemyCase(5, "P5_YakuNormalReference", PortalNightsEnemyVisualKind.Yaku, false),
                BossCase(5, "P5_YakuBoss", PortalNightsEnemyVisualKind.Yaku),
                BossCase(5, "P5_PumpkinhulkBoss", PortalNightsEnemyVisualKind.Pumpkinhulk),
                BossCase(5, "P5_MawBossReference", PortalNightsEnemyVisualKind.Maw),
                BossCase(5, "P5_WarrokBossReference", PortalNightsEnemyVisualKind.Warrok)
            };

            for (int i = 0; i < cases.Count; i++)
            {
                if (!RunCase(cases[i], i, lines))
                {
                    failures++;
                }
            }

            report = lines.ToString();

            if (!Application.isPlaying)
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (previousScene.IsValid())
                {
                    SceneManager.SetActiveScene(SceneManager.GetActiveScene());
                }
            }

            return failures == 0;
        }

        public static bool RunArenaPortalBridgeTest(out string report)
        {
            const string ScenePath = "Assets/PortalNights/Scenes/PortalNightsArena.unity";
            var lines = new StringBuilder();
            int failures = 0;

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            lines.AppendLine("# Portal Nights Arena Portal Bridge Grounding Test");
            lines.AppendLine();
            lines.AppendLine("| Probe | Source | Root Y | Floor Y | Collider bottom | Visual bottom | Visual error | Result |");
            lines.AppendLine("| --- | --- | ---: | ---: | ---: | ---: | ---: | --- |");

            Vector3[] probes =
            {
                new Vector3(0f, 5.5f, 29.2f),
                new Vector3(-1.25f, 5.5f, 27.4f),
                new Vector3(1.25f, 5.5f, 27.4f),
                new Vector3(0f, 5.5f, 24.5f)
            };

            for (int i = 0; i < probes.Length; i++)
            {
                if (!RunArenaProbe(i + 1, probes[i], lines))
                {
                    failures++;
                }
            }

            report = lines.ToString();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return failures == 0;
        }

        private static GroundingCase EnemyCase(int planet, string label, PortalNightsEnemyVisualKind kind, bool staff)
        {
            return new GroundingCase
            {
                planet = planet,
                label = label,
                kind = kind,
                height = staff ? 1.77f : PortalNightsEnemyVisualCatalog.GetTargetHeightForPlanet(planet, kind),
                staff = staff
            };
        }

        private static GroundingCase BossCase(int planet, string label, PortalNightsEnemyVisualKind kind)
        {
            return new GroundingCase
            {
                planet = planet,
                label = label,
                kind = kind,
                height = PortalNightsEnemyVisualCatalog.GetBossTargetHeight(kind),
                staff = false
            };
        }

        private static bool RunCase(GroundingCase testCase, int index, StringBuilder lines)
        {
            Vector3 basePosition = new Vector3(index * 18f, 0f, 0f);
            CreateFloorRig(testCase.planet, basePosition);

            GameObject root = new GameObject("SmokeRoot_" + testCase.label);
            root.transform.position = basePosition + new Vector3(0f, 4f, 0f);
            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.name = "GameplayCapsule";
            capsule.height = testCase.staff ? 1.85f : Mathf.Max(1.8f, testCase.height * 0.82f);
            capsule.radius = testCase.staff ? 0.35f : Mathf.Clamp(testCase.height * 0.18f, 0.3f, 0.9f);
            capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);

            PortalNightsGroundingUtility.GroundGameplayRoot(root, testCase.planet);

            Transform visual = null;
            float targetHeight = testCase.height;
            if (testCase.staff)
            {
                PortalNightsStaffVisualBinder staffBinder = root.AddComponent<PortalNightsStaffVisualBinder>();
                staffBinder.BindCh32();
                visual = staffBinder.CurrentVisualInstance;
            }
            else
            {
                PortalNightsEnemyVisualBinder enemyBinder = root.AddComponent<PortalNightsEnemyVisualBinder>();
                enemyBinder.Bind(testCase.kind, testCase.height, testCase.planet);
                visual = enemyBinder.CurrentVisualInstance;
            }

            PortalNightsGroundingValidationResult validation = PortalNightsGroundingUtility.ValidateGrounding(root, visual, testCase.planet, testCase.label);
            bool rootMotionOff = ValidateRootMotionOff(visual);
            bool success = validation.success && rootMotionOff && Mathf.Abs(validation.visualFloorError) <= Tolerance;

            lines.Append("| ")
                .Append(testCase.planet).Append(" | ")
                .Append(testCase.label).Append(" | ")
                .Append(targetHeight.ToString("0.000")).Append(" | ")
                .Append(root.transform.position.y.ToString("0.000")).Append(" | ")
                .Append(validation.floorY.ToString("0.000")).Append(" | ")
                .Append(validation.colliderBottomY.ToString("0.000")).Append(" | ")
                .Append(validation.visualBottomY.ToString("0.000")).Append(" | ")
                .Append(validation.visualFloorError.ToString("0.000")).Append(" | ")
                .Append(success ? "PASS" : "FAIL");

            if (!rootMotionOff)
            {
                lines.Append(" rootMotion enabled");
            }

            if (!string.IsNullOrEmpty(validation.failure) && !validation.success)
            {
                lines.Append(" ").Append(validation.failure);
            }

            lines.AppendLine(" |");
            return success;
        }

        private static bool RunArenaProbe(int index, Vector3 position, StringBuilder lines)
        {
            GameObject root = new GameObject("ArenaPortalGroundingProbe_" + index.ToString("00"));
            root.transform.position = position;
            CapsuleCollider capsule = root.AddComponent<CapsuleCollider>();
            capsule.name = "GameplayCapsule";
            capsule.height = 1.85f;
            capsule.radius = 0.42f;
            capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);

            PortalNightsEnemyVisualBinder binder = root.AddComponent<PortalNightsEnemyVisualBinder>();
            float targetHeight = PortalNightsEnemyVisualCatalog.GetTargetHeightForPlanet(1, PortalNightsEnemyVisualKind.Ch40);
            if (index % 2 == 1)
            {
                binder.Bind(PortalNightsEnemyVisualKind.Ch40, targetHeight, 1);
                PortalNightsGroundingUtility.GroundGameplayRoot(root, 1);
                binder.Bind(PortalNightsEnemyVisualKind.Ch40, targetHeight, 1);
            }
            else
            {
                PortalNightsGroundingUtility.GroundGameplayRoot(root, 1);
                binder.Bind(PortalNightsEnemyVisualKind.Ch40, targetHeight, 1);
            }

            PortalNightsGroundingValidationResult validation = PortalNightsGroundingUtility.ValidateGrounding(root, binder.CurrentVisualInstance, 1, "ArenaPortalProbe");
            bool rejectedPortalGeometry = !ContainsUnsafeFloorSource(validation.floorSource);
            bool floorHeightLooksLikeBridge = validation.floorY > 0.45f && validation.floorY < 1.25f;
            bool success = validation.success && rejectedPortalGeometry && floorHeightLooksLikeBridge;

            lines.Append("| ")
                .Append(index).Append(" | ")
                .Append(validation.floorSource).Append(" | ")
                .Append(root.transform.position.y.ToString("0.000")).Append(" | ")
                .Append(validation.floorY.ToString("0.000")).Append(" | ")
                .Append(validation.colliderBottomY.ToString("0.000")).Append(" | ")
                .Append(validation.visualBottomY.ToString("0.000")).Append(" | ")
                .Append(validation.visualFloorError.ToString("0.000")).Append(" | ")
                .Append(success ? "PASS" : "FAIL");

            if (!rejectedPortalGeometry)
            {
                lines.Append(" portal-geometry-selected");
            }

            if (!floorHeightLooksLikeBridge)
            {
                lines.Append(" unexpected-floor-height");
            }

            if (!string.IsNullOrEmpty(validation.failure) && !validation.success)
            {
                lines.Append(" ").Append(validation.failure);
            }

            lines.AppendLine(" |");
            Object.DestroyImmediate(root);
            return success;
        }

        private static bool ContainsUnsafeFloorSource(string source)
        {
            if (string.IsNullOrEmpty(source))
            {
                return false;
            }

            return source.Contains("Portal")
                || source.Contains("Pylon")
                || source.Contains("Frame")
                || source.Contains("Arch");
        }

        private static void CreateFloorRig(int planet, Vector3 basePosition)
        {
            GameObject arenaRoot = new GameObject("PortalNightsArena");
            arenaRoot.transform.position = basePosition;

            GameObject portalArea = new GameObject("PortalArea");
            portalArea.transform.SetParent(arenaRoot.transform, false);

            GameObject portalRoot = new GameObject("PN_Glowing_Portal");
            portalRoot.transform.SetParent(portalArea.transform, false);

            GameObject invalidPortalFrame = GameObject.CreatePrimitive(PrimitiveType.Cube);
            invalidPortalFrame.name = "Top_Pylon";
            invalidPortalFrame.transform.SetParent(portalRoot.transform, false);
            invalidPortalFrame.transform.localPosition = new Vector3(0f, 5.2f, 0f);
            invalidPortalFrame.transform.localScale = new Vector3(2f, 0.65f, 2f);

            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = GetFloorName(planet);
            floor.transform.SetParent(arenaRoot.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.1f, 0f);
            floor.transform.localScale = new Vector3(12f, 0.2f, 12f);

            int walkableLayer = LayerMask.NameToLayer("WalkableFloor");
            if (walkableLayer >= 0)
            {
                floor.layer = walkableLayer;
            }
        }

        private static string GetFloorName(int planet)
        {
            return planet switch
            {
                1 => "LeftEnemyLane_Surface_Floor_RuntimeSmoke",
                2 => "CrystalMoon_Floor_RuntimeSmoke",
                3 => "AshRelay_RiftPlatform_RuntimeSmoke",
                4 => "HiveLane_Surface_RuntimeSmoke",
                5 => "CrimsonArena_Platform_RuntimeSmoke",
                _ => "WalkableFloor_RuntimeSmoke"
            };
        }

        private static bool ValidateRootMotionOff(Transform visual)
        {
            if (visual == null)
            {
                return false;
            }

            Animator[] animators = visual.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                if (animators[i] != null && animators[i].applyRootMotion)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
#endif
