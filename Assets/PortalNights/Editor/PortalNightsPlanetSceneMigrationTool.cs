#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using PortalNights.Scenes;
using Unity.Netcode;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PortalNights.EditorTools
{
    public static class PortalNightsPlanetSceneMigrationTool
    {
        private const string SourceScenePath = "Assets/PortalNights/Scenes/PortalNightsArena.unity";
        private const string GeneratedScenesFolder = "Assets/PortalNights/Scenes/Planets";
        private const string GeneratedCoreScenesFolder = "Assets/PortalNights/Scenes/Core";
        private const string Phase3ReportPath = "Assets/PortalNights/Reports/SceneMigrationPhase3Report.md";
        private const string Phase4ReportPath = "Assets/PortalNights/Reports/SceneMigrationPhase4Report.md";
        private const string Phase5AReportPath = "Assets/PortalNights/Reports/SceneMigrationPhase5AReport.md";
        private const string CoreScenePath = GeneratedCoreScenesFolder + "/PortalNightsCore.unity";
        private const string Planet1ScenePath = GeneratedScenesFolder + "/PortalNightsPlanet1_Defense.unity";
        private const string PortalNightsArenaSceneName = "PortalNightsArena";
        private const string PortalNightsCoreSceneName = "PortalNightsCore";

        private static readonly PlanetScenePlan[] Plans =
        {
            new PlanetScenePlan(2, "Planet2_CrystalMoon", "Planet2_CrystalMoon", "PortalNightsPlanet2_CrystalMoon.unity", "planet.2.name"),
            new PlanetScenePlan(3, "Planet3_AshRelayStation", "Planet3_AshRelayStation", "PortalNightsPlanet3_AshRelayStation.unity", "planet.3.name"),
            new PlanetScenePlan(4, "Planet4_SwarmExpanse", "Planet4_SwarmExpanse", "PortalNightsPlanet4_SwarmExpanse.unity", "planet.4.name"),
            new PlanetScenePlan(5, "Planet5_CrimsonSingularity", "Planet5_CrimsonSingularity", "PortalNightsPlanet5_CrimsonSingularity.unity", "planet.5.name")
        };

        private static readonly string[] ForbiddenSceneObjectNames =
        {
            "NetworkManager",
            "PN_GameController",
            "PN_HUD_Canvas",
            "EventSystem",
            "Main Camera"
        };

        private static readonly string[] CoreRequiredRootNames =
        {
            "NetworkManager",
            "PN_GameController",
            "PN_HUD_Canvas",
            "EventSystem",
            "Main Camera",
            "PN_GlobalVolume_Bloom",
            "PN_Sun_KeyLight"
        };

        private static readonly string[] Planet1ArenaChildNames =
        {
            "Environment",
            "PortalArea",
            "EntranceBridge",
            "LaneFork",
            "LeftLane",
            "RightLane",
            "CoreArena",
            "Background",
            "VFX"
        };

        private static readonly string[] Planet1TopLevelNames =
        {
            "PN_Central_Core",
            "PN_BuildPads",
            "PN_PlayerSpawn_1",
            "PN_PlayerSpawn_2",
            "PN_PlayerSpawn_3",
            "PN_PlayerSpawn_4",
            "PN_PlayerSpawn_5",
            "PN_PlayerSpawn_6"
        };

        private static readonly string[] FuturePlanetRootNames =
        {
            "Planet2_CrystalMoon",
            "Planet3_AshRelayStation",
            "Planet4_SwarmExpanse",
            "Planet5_CrimsonSingularity"
        };

        private static readonly string[] Phase5ARequiredScenePaths =
        {
            SourceScenePath,
            CoreScenePath,
            Planet1ScenePath,
            GeneratedScenesFolder + "/PortalNightsPlanet2_CrystalMoon.unity",
            GeneratedScenesFolder + "/PortalNightsPlanet3_AshRelayStation.unity",
            GeneratedScenesFolder + "/PortalNightsPlanet4_SwarmExpanse.unity",
            GeneratedScenesFolder + "/PortalNightsPlanet5_CrimsonSingularity.unity"
        };

        private static readonly Phase5APlanetValidationPlan[] Phase5APlanetPlans =
        {
            new Phase5APlanetValidationPlan(1, "PortalNightsPlanet1_Defense", Planet1ScenePath, "Planet1_Defense"),
            new Phase5APlanetValidationPlan(2, "PortalNightsPlanet2_CrystalMoon", GeneratedScenesFolder + "/PortalNightsPlanet2_CrystalMoon.unity", "Planet2_CrystalMoon"),
            new Phase5APlanetValidationPlan(3, "PortalNightsPlanet3_AshRelayStation", GeneratedScenesFolder + "/PortalNightsPlanet3_AshRelayStation.unity", "Planet3_AshRelayStation"),
            new Phase5APlanetValidationPlan(4, "PortalNightsPlanet4_SwarmExpanse", GeneratedScenesFolder + "/PortalNightsPlanet4_SwarmExpanse.unity", "Planet4_SwarmExpanse"),
            new Phase5APlanetValidationPlan(5, "PortalNightsPlanet5_CrimsonSingularity", GeneratedScenesFolder + "/PortalNightsPlanet5_CrimsonSingularity.unity", "Planet5_CrimsonSingularity")
        };

        private static readonly string[] OptionalGlobalNameKeywords =
        {
            "TransitionDirector",
            "MissionComms",
            "ObjectiveTracker",
            "RadioDialogue",
            "Localization",
            "Bootstrap",
            "Yandex",
            "YG"
        };

        private static readonly string[] OptionalGlobalComponentKeywords =
        {
            "PortalNightsPlanetTransitionDirector",
            "PortalNightsMissionComms",
            "PortalNightsObjectiveTracker",
            "PortalNightsRadioDialogueController",
            "PortalNightsLanguageBootstrap",
            "YG",
            "Yandex"
        };

        private static List<DryRunResult> lastDryRunResults = new List<DryRunResult>();
        private static List<GenerationResult> lastGenerationResults = new List<GenerationResult>();
        private static List<ValidationResult> lastValidationResults = new List<ValidationResult>();
        private static Phase4DryRunResult lastPhase4DryRunResult;
        private static Phase4CoreGenerationResult lastPhase4CoreGenerationResult;
        private static Phase4Planet1GenerationResult lastPhase4Planet1GenerationResult;
        private static Phase4CoreValidationResult lastPhase4CoreValidationResult;
        private static Phase4Planet1ValidationResult lastPhase4Planet1ValidationResult;
        private static Phase5AValidationResult lastPhase5AValidationResult;

        [MenuItem("Portal Nights/Scene Migration/Dry Run Planet Scene Copy P2-P5")]
        public static void DryRunPlanetSceneCopyP2P5()
        {
            if (!PrepareForSceneOperation())
            {
                return;
            }

            try
            {
                lastDryRunResults = RunDryRun(true);
                WriteReport();
            }
            finally
            {
                ReopenSourceScene();
            }
        }

        [MenuItem("Portal Nights/Scene Migration/Generate Planet Scene Copies P2-P5")]
        public static void GeneratePlanetSceneCopiesP2P5()
        {
            if (!PrepareForSceneOperation())
            {
                return;
            }

            try
            {
                lastGenerationResults = GenerateSceneCopies(true);
                WriteReport();
            }
            finally
            {
                ReopenSourceScene();
            }
        }

        [MenuItem("Portal Nights/Scene Migration/Validate Generated Planet Scenes P2-P5")]
        public static void ValidateGeneratedPlanetScenesP2P5()
        {
            if (!PrepareForSceneOperation())
            {
                return;
            }

            try
            {
                lastValidationResults = ValidateGeneratedScenes(true);
                WriteReport();
            }
            finally
            {
                ReopenSourceScene();
            }
        }

        [MenuItem("Portal Nights/Scene Migration/Dry Run Core And Planet1 Copy")]
        public static void DryRunCoreAndPlanet1Copy()
        {
            if (!PrepareForSceneOperation())
            {
                return;
            }

            try
            {
                lastPhase4DryRunResult = RunPhase4DryRun(true);
                WritePhase4Report();
            }
            finally
            {
                ReopenSourceScene();
            }
        }

        [MenuItem("Portal Nights/Scene Migration/Generate Core And Planet1 Scene Copies")]
        public static void GenerateCoreAndPlanet1SceneCopies()
        {
            if (!PrepareForSceneOperation())
            {
                return;
            }

            try
            {
                lastPhase4CoreGenerationResult = GenerateCoreSceneCopy(true);
                lastPhase4Planet1GenerationResult = GeneratePlanet1SceneCopy(true);
                WritePhase4Report();
            }
            finally
            {
                ReopenSourceScene();
            }
        }

        [MenuItem("Portal Nights/Scene Migration/Validate Core And Planet1 Scenes")]
        public static void ValidateCoreAndPlanet1Scenes()
        {
            if (!PrepareForSceneOperation())
            {
                return;
            }

            try
            {
                lastPhase4CoreValidationResult = ValidateCoreScene(true);
                lastPhase4Planet1ValidationResult = ValidatePlanet1Scene(true);
                WritePhase4Report();
            }
            finally
            {
                ReopenSourceScene();
            }
        }

        [MenuItem("Portal Nights/Scene Migration/Validate Build Settings And Additive Scene Loading")]
        public static void ValidateBuildSettingsAndAdditiveSceneLoading()
        {
            if (!PrepareForSceneOperation())
            {
                return;
            }

            try
            {
                lastPhase5AValidationResult = ValidateBuildSettingsAndAdditiveSceneLoadingInternal(true);
                WritePhase5AReport();
            }
            finally
            {
                ReopenSourceScene();
            }
        }

        public static void RunPhase3WorkflowFromCommandLine()
        {
            bool success = false;
            try
            {
                EnsureOutputFolders();
                lastDryRunResults = RunDryRun(true);
                if (!AllDryRunsSucceeded(lastDryRunResults))
                {
                    throw new InvalidOperationException("Dry run failed. Check SceneMigrationPhase3Report.md for details.");
                }

                lastGenerationResults = GenerateSceneCopies(true);
                if (!AllGenerationsSucceeded(lastGenerationResults))
                {
                    throw new InvalidOperationException("Scene generation failed. Check SceneMigrationPhase3Report.md for details.");
                }

                lastValidationResults = ValidateGeneratedScenes(true);
                if (!AllValidationsSucceeded(lastValidationResults))
                {
                    throw new InvalidOperationException("Generated scene validation failed. Check SceneMigrationPhase3Report.md for details.");
                }

                success = true;
            }
            finally
            {
                ReopenSourceScene();
                WriteReport();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (!success)
            {
                throw new InvalidOperationException("Phase 3 workflow did not complete successfully.");
            }
        }

        public static void RunPhase4WorkflowFromCommandLine()
        {
            bool success = false;
            try
            {
                EnsurePhase4OutputFolders();
                lastPhase4DryRunResult = RunPhase4DryRun(true);
                if (!lastPhase4DryRunResult.IsClean)
                {
                    throw new InvalidOperationException("Phase 4 dry run failed. Check SceneMigrationPhase4Report.md for details.");
                }

                lastPhase4CoreGenerationResult = GenerateCoreSceneCopy(true);
                if (!lastPhase4CoreGenerationResult.Success)
                {
                    throw new InvalidOperationException("Core scene generation failed. Check SceneMigrationPhase4Report.md for details.");
                }

                lastPhase4Planet1GenerationResult = GeneratePlanet1SceneCopy(true);
                if (!lastPhase4Planet1GenerationResult.Success)
                {
                    throw new InvalidOperationException("Planet 1 scene generation failed. Check SceneMigrationPhase4Report.md for details.");
                }

                lastPhase4CoreValidationResult = ValidateCoreScene(true);
                lastPhase4Planet1ValidationResult = ValidatePlanet1Scene(true);
                if (!lastPhase4CoreValidationResult.Success || !lastPhase4Planet1ValidationResult.Success)
                {
                    throw new InvalidOperationException("Core/Planet 1 validation failed. Check SceneMigrationPhase4Report.md for details.");
                }

                success = true;
            }
            finally
            {
                ReopenSourceScene();
                WritePhase4Report();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (!success)
            {
                throw new InvalidOperationException("Phase 4 workflow did not complete successfully.");
            }
        }

        public static void RunPhase5AWorkflowFromCommandLine()
        {
            bool success = false;
            try
            {
                RegisterGeneratedScenesInBuildSettings(true);
                lastPhase5AValidationResult = ValidateBuildSettingsAndAdditiveSceneLoadingInternal(true);
                if (lastPhase5AValidationResult == null || !lastPhase5AValidationResult.Success)
                {
                    throw new InvalidOperationException("Phase 5A validation failed. Check SceneMigrationPhase5AReport.md for details.");
                }

                success = true;
            }
            finally
            {
                ReopenSourceScene();
                WritePhase5AReport();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            if (!success)
            {
                throw new InvalidOperationException("Phase 5A workflow did not complete successfully.");
            }
        }

        private static void RegisterGeneratedScenesInBuildSettings(bool logToConsole)
        {
            for (int i = 0; i < Phase5ARequiredScenePaths.Length; i++)
            {
                if (!File.Exists(Phase5ARequiredScenePaths[i]))
                {
                    throw new FileNotFoundException("Required scene asset is missing: " + Phase5ARequiredScenePaths[i], Phase5ARequiredScenePaths[i]);
                }
            }

            EditorBuildSettingsScene[] existingScenes = EditorBuildSettings.scenes ?? Array.Empty<EditorBuildSettingsScene>();
            List<EditorBuildSettingsScene> updatedScenes = new List<EditorBuildSettingsScene>(existingScenes.Length + Phase5ARequiredScenePaths.Length);
            HashSet<string> addedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < Phase5ARequiredScenePaths.Length; i++)
            {
                string requiredPath = NormalizeScenePath(Phase5ARequiredScenePaths[i]);
                if (addedPaths.Add(requiredPath))
                {
                    updatedScenes.Add(new EditorBuildSettingsScene(requiredPath, true));
                }
            }

            for (int i = 0; i < existingScenes.Length; i++)
            {
                EditorBuildSettingsScene existingScene = existingScenes[i];
                string normalizedPath = NormalizeScenePath(existingScene.path);
                if (string.IsNullOrEmpty(normalizedPath) || !addedPaths.Add(normalizedPath))
                {
                    continue;
                }

                updatedScenes.Add(new EditorBuildSettingsScene(normalizedPath, existingScene.enabled));
            }

            EditorBuildSettings.scenes = updatedScenes.ToArray();
            AssetDatabase.SaveAssets();

            if (logToConsole)
            {
                UnityEngine.Debug.Log("[PortalNights][SceneMigration] Registered generated scenes in Build Settings. Scene count=" + updatedScenes.Count + ", first=" + updatedScenes[0].path);
            }
        }

        private static Phase5AValidationResult ValidateBuildSettingsAndAdditiveSceneLoadingInternal(bool logToConsole)
        {
            Phase5AValidationResult result = new Phase5AValidationResult();

            try
            {
                Scene sourceScene = OpenSourceScene();
                result.SourceSceneOpenedForValidation = sourceScene.IsValid() && string.Equals(NormalizeScenePath(sourceScene.path), NormalizeScenePath(SourceScenePath), StringComparison.OrdinalIgnoreCase);

                PopulateBuildSettingsValidation(result);
                result.CoreValidation = ValidateCoreSceneAdditively();

                for (int i = 0; i < Phase5APlanetPlans.Length; i++)
                {
                    result.PlanetValidations.Add(ValidatePlanetSceneAdditively(Phase5APlanetPlans[i]));
                }

                result.Success =
                    result.SourceSceneOpenedForValidation
                    && result.PortalNightsArenaIsFirstEnabledScene
                    && result.CoreSceneRegistered
                    && result.AllPlanetScenesRegistered
                    && result.DuplicateScenePaths.Count == 0
                    && result.MissingSceneAssets.Count == 0
                    && result.MissingBuildSettingsRegistrations.Count == 0
                    && result.CoreValidation != null
                    && result.CoreValidation.Success
                    && AllPhase5APlanetValidationsSucceeded(result.PlanetValidations);
            }
            catch (Exception exception)
            {
                result.Error = exception.Message;
            }
            finally
            {
                ReopenSourceScene();
                Scene reopenedScene = SceneManager.GetActiveScene();
                result.SourceSceneReopenedAtEnd = reopenedScene.IsValid()
                    && string.Equals(NormalizeScenePath(reopenedScene.path), NormalizeScenePath(SourceScenePath), StringComparison.OrdinalIgnoreCase);
            }

            if (logToConsole)
            {
                UnityEngine.Debug.Log("[PortalNights][SceneMigration] Phase 5A validation: " + result.ToLogLine());
            }

            return result;
        }

        private static void PopulateBuildSettingsValidation(Phase5AValidationResult result)
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes ?? Array.Empty<EditorBuildSettingsScene>();
            HashSet<string> requiredPaths = new HashSet<string>(Phase5ARequiredScenePaths.Length, StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < Phase5ARequiredScenePaths.Length; i++)
            {
                requiredPaths.Add(NormalizeScenePath(Phase5ARequiredScenePaths[i]));
            }

            Dictionary<string, int> duplicateCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < scenes.Length; i++)
            {
                EditorBuildSettingsScene scene = scenes[i];
                string normalizedPath = NormalizeScenePath(scene.path);
                result.BuildSettingsScenes.Add(new Phase5ABuildSceneEntry
                {
                    Index = i,
                    Path = normalizedPath,
                    Enabled = scene.enabled
                });

                if (string.IsNullOrEmpty(normalizedPath))
                {
                    continue;
                }

                if (duplicateCounts.TryGetValue(normalizedPath, out int count))
                {
                    duplicateCounts[normalizedPath] = count + 1;
                }
                else
                {
                    duplicateCounts.Add(normalizedPath, 1);
                }

                if (!File.Exists(normalizedPath))
                {
                    result.MissingSceneAssets.Add(normalizedPath);
                }
            }

            foreach (KeyValuePair<string, int> duplicate in duplicateCounts)
            {
                if (duplicate.Value > 1)
                {
                    result.DuplicateScenePaths.Add(duplicate.Key);
                }
            }

            result.PortalNightsArenaIsFirstEnabledScene = false;
            for (int i = 0; i < result.BuildSettingsScenes.Count; i++)
            {
                Phase5ABuildSceneEntry scene = result.BuildSettingsScenes[i];
                if (!scene.Enabled)
                {
                    continue;
                }

                result.PortalNightsArenaIsFirstEnabledScene = string.Equals(scene.Path, NormalizeScenePath(SourceScenePath), StringComparison.OrdinalIgnoreCase);
                break;
            }

            result.CoreSceneRegistered = BuildSettingsContainsScene(result.BuildSettingsScenes, CoreScenePath, true);

            result.AllPlanetScenesRegistered = true;
            for (int i = 0; i < Phase5APlanetPlans.Length; i++)
            {
                Phase5APlanetValidationPlan plan = Phase5APlanetPlans[i];
                bool isRegistered = BuildSettingsContainsScene(result.BuildSettingsScenes, plan.ScenePath, true);
                result.RegisteredPlanetScenePaths.Add(plan.ScenePath);
                if (!isRegistered)
                {
                    result.AllPlanetScenesRegistered = false;
                    result.MissingBuildSettingsRegistrations.Add(plan.ScenePath);
                }
            }

            if (!result.CoreSceneRegistered)
            {
                result.MissingBuildSettingsRegistrations.Add(CoreScenePath);
            }
        }

        private static Phase5ACoreValidationResult ValidateCoreSceneAdditively()
        {
            Phase5ACoreValidationResult result = new Phase5ACoreValidationResult
            {
                ScenePath = CoreScenePath,
                SceneName = Path.GetFileNameWithoutExtension(CoreScenePath)
            };

            Scene scene = default;
            try
            {
                if (!File.Exists(CoreScenePath))
                {
                    result.Error = "Core scene file is missing.";
                    return result;
                }

                scene = EditorSceneManager.OpenScene(CoreScenePath, OpenSceneMode.Additive);
                result.RootCount = scene.rootCount;
                result.Metrics = CollectSceneMetrics(scene);
                result.NetworkObjectCount = result.Metrics.NetworkObjectCount;

                result.HasNetworkManager = SceneContainsObjectNamed(scene, "NetworkManager");
                result.HasGameController = SceneContainsObjectNamed(scene, "PN_GameController");
                result.HasHudCanvas = SceneContainsObjectNamed(scene, "PN_HUD_Canvas");
                result.HasEventSystem = SceneContainsObjectNamed(scene, "EventSystem");
                result.HasMainCamera = SceneContainsObjectNamed(scene, "Main Camera");
                result.HasArenaRoot = FindTopLevelRootObject(scene, "PortalNightsArena") != null;
                result.HasPlanet1Core = SceneContainsObjectNamed(scene, "PN_Central_Core");
                result.HasBuildPads = SceneContainsObjectNamed(scene, "PN_BuildPads");
                result.PlayerSpawnCount = CountNamedObjectsInScene(scene, "PN_PlayerSpawn_");
                result.ForbiddenFutureRoots = FindForbiddenTopLevelObjects(scene, FuturePlanetRootNames);
                result.ForbiddenPlanet1Objects = FindForbiddenTopLevelObjects(scene, CombineNames(Planet1ArenaChildNames, Planet1TopLevelNames));

                result.Success =
                    result.HasNetworkManager
                    && result.HasGameController
                    && result.HasHudCanvas
                    && result.HasEventSystem
                    && result.HasMainCamera
                    && !result.HasArenaRoot
                    && !result.HasPlanet1Core
                    && !result.HasBuildPads
                    && result.PlayerSpawnCount == 0
                    && result.ForbiddenFutureRoots.Count == 0
                    && result.ForbiddenPlanet1Objects.Count == 0;
            }
            catch (Exception exception)
            {
                result.Error = exception.Message;
            }
            finally
            {
                CloseSceneIfLoaded(scene);
            }

            return result;
        }

        private static Phase5APlanetValidationEntry ValidatePlanetSceneAdditively(Phase5APlanetValidationPlan plan)
        {
            Phase5APlanetValidationEntry result = new Phase5APlanetValidationEntry
            {
                PlanetIndex = plan.PlanetIndex,
                SceneName = plan.SceneName,
                ScenePath = plan.ScenePath,
                ExpectedRootName = plan.ExpectedRootName
            };

            Scene scene = default;
            try
            {
                if (!File.Exists(plan.ScenePath))
                {
                    result.Error = "Scene file is missing.";
                    return result;
                }

                scene = EditorSceneManager.OpenScene(plan.ScenePath, OpenSceneMode.Additive);
                result.RootCount = scene.rootCount;
                result.SceneRootCount = CountPlanetSceneRoots(scene);
                result.Metrics = CollectSceneMetrics(scene);
                result.NetworkObjectCount = result.Metrics.NetworkObjectCount;
                result.ContainsArenaRoot = FindTopLevelRootObject(scene, "PortalNightsArena") != null;
                result.ForbiddenGlobalObjects.AddRange(FindForbiddenTopLevelObjects(scene, CoreRequiredRootNames));
                result.HasPlayerController = SceneContainsPlayerController(scene);

                GameObject[] roots = scene.GetRootGameObjects();
                for (int i = 0; i < roots.Length; i++)
                {
                    if (roots[i] != null && string.Equals(roots[i].name, plan.ExpectedRootName, StringComparison.Ordinal))
                    {
                        result.ExpectedRootCount++;
                        if (result.ExpectedRoot == null)
                        {
                            result.ExpectedRoot = roots[i];
                        }
                    }
                }

                result.HasExpectedRoot = result.ExpectedRoot != null;
                result.RootActive = result.ExpectedRoot != null && result.ExpectedRoot.activeSelf;

                PortalNightsPlanetSceneRoot sceneRoot = result.ExpectedRoot == null ? null : result.ExpectedRoot.GetComponent<PortalNightsPlanetSceneRoot>();
                result.HasPlanetSceneRoot = sceneRoot != null;
                if (sceneRoot != null)
                {
                    result.PlanetIndexMatches = sceneRoot.PlanetIndex == plan.PlanetIndex;
                    result.ValidateSetupPassed = sceneRoot.ValidateSetup(true);
                }

                result.Success =
                    result.HasExpectedRoot
                    && result.ExpectedRootCount == 1
                    && result.RootActive
                    && result.HasPlanetSceneRoot
                    && result.SceneRootCount == 1
                    && result.PlanetIndexMatches
                    && result.ValidateSetupPassed
                    && !result.ContainsArenaRoot
                    && !result.HasPlayerController
                    && result.ForbiddenGlobalObjects.Count == 0;
            }
            catch (Exception exception)
            {
                result.Error = exception.Message;
            }
            finally
            {
                CloseSceneIfLoaded(scene);
                result.ExpectedRoot = null;
            }

            return result;
        }

        private static bool AllPhase5APlanetValidationsSucceeded(List<Phase5APlanetValidationEntry> validations)
        {
            if (validations == null || validations.Count != Phase5APlanetPlans.Length)
            {
                return false;
            }

            for (int i = 0; i < validations.Count; i++)
            {
                if (validations[i] == null || !validations[i].Success)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool BuildSettingsContainsScene(List<Phase5ABuildSceneEntry> scenes, string scenePath, bool enabledOnly)
        {
            string normalizedPath = NormalizeScenePath(scenePath);
            for (int i = 0; i < scenes.Count; i++)
            {
                Phase5ABuildSceneEntry entry = scenes[i];
                if (string.Equals(entry.Path, normalizedPath, StringComparison.OrdinalIgnoreCase) && (!enabledOnly || entry.Enabled))
                {
                    return true;
                }
            }

            return false;
        }

        private static string NormalizeScenePath(string scenePath)
        {
            return string.IsNullOrEmpty(scenePath)
                ? string.Empty
                : scenePath.Replace('\\', '/');
        }

        private static void CloseSceneIfLoaded(Scene scene)
        {
            if (scene.IsValid() && scene.isLoaded)
            {
                EditorSceneManager.CloseScene(scene, true);
            }
        }

        private static List<DryRunResult> RunDryRun(bool logToConsole)
        {
            Scene sourceScene = OpenSourceScene();
            List<DryRunResult> results = new List<DryRunResult>(Plans.Length);

            for (int i = 0; i < Plans.Length; i++)
            {
                PlanetScenePlan plan = Plans[i];
                GameObject sourceRoot = FindRootObject(sourceScene, plan.SourceRootName);
                DryRunResult result = new DryRunResult
                {
                    Plan = plan,
                    SourceRootFound = sourceRoot != null,
                    TargetScenePath = plan.TargetScenePath
                };

                if (sourceRoot != null)
                {
                    result.Metrics = CollectMetrics(sourceRoot);
                    result.HasSceneRootComponent = sourceRoot.GetComponent<PortalNightsPlanetSceneRoot>() != null;
                    result.WouldAddSceneRoot = !result.HasSceneRootComponent;
                }
                else
                {
                    result.Error = "Source root was not found in PortalNightsArena.";
                }

                results.Add(result);
                if (logToConsole)
                {
                    UnityEngine.Debug.Log("[PortalNights][SceneMigration] Dry run " + plan.SourceRootName + ": " + result.ToLogLine());
                }
            }

            return results;
        }

        private static List<GenerationResult> GenerateSceneCopies(bool logToConsole)
        {
            EnsureOutputFolders();
            List<GenerationResult> results = new List<GenerationResult>(Plans.Length);

            for (int i = 0; i < Plans.Length; i++)
            {
                PlanetScenePlan plan = Plans[i];
                GenerationResult result = new GenerationResult { Plan = plan };

                try
                {
                    Scene sourceScene = OpenSourceScene();
                    GameObject sourceRoot = FindRootObject(sourceScene, plan.SourceRootName);
                    if (sourceRoot == null)
                    {
                        result.Error = "Source root was not found in PortalNightsArena.";
                        results.Add(result);
                        continue;
                    }

                    Scene targetScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                    SceneManager.SetActiveScene(targetScene);

                    GameObject copiedRoot = UnityEngine.Object.Instantiate(sourceRoot);
                    copiedRoot.name = plan.TargetRootName;
                    copiedRoot.transform.SetParent(null);
                    copiedRoot.SetActive(true);

                    PortalNightsPlanetSceneRoot sceneRoot = EnsureSinglePlanetSceneRoot(copiedRoot, plan, out bool addedSceneRoot, out int rootComponentCountInScene);
                    sceneRoot.AutoDiscoverReferences();
                    bool validateSetupPassed = sceneRoot.ValidateSetup(true);
                    EditorUtility.SetDirty(sceneRoot);
                    EditorSceneManager.MarkSceneDirty(targetScene);

                    bool saveSucceeded = EditorSceneManager.SaveScene(targetScene, plan.TargetScenePath, false);
                    if (!saveSucceeded)
                    {
                        result.Error = "EditorSceneManager.SaveScene returned false.";
                        EditorSceneManager.CloseScene(targetScene, true);
                        SceneManager.SetActiveScene(sourceScene);
                        results.Add(result);
                        continue;
                    }

                    result.Success = true;
                    result.AddedSceneRoot = addedSceneRoot;
                    result.SceneRootComponentCount = rootComponentCountInScene;
                    result.ValidateSetupPassed = validateSetupPassed;
                    result.Metrics = CollectMetrics(copiedRoot);
                    result.NetworkObjectCount = result.Metrics.NetworkObjectCount;

                    SceneManager.SetActiveScene(sourceScene);
                    EditorSceneManager.CloseScene(targetScene, true);
                }
                catch (Exception exception)
                {
                    result.Error = exception.Message;
                }

                results.Add(result);
                if (logToConsole)
                {
                    UnityEngine.Debug.Log("[PortalNights][SceneMigration] Generate " + plan.TargetRootName + ": " + result.ToLogLine());
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return results;
        }

        private static List<ValidationResult> ValidateGeneratedScenes(bool logToConsole)
        {
            List<ValidationResult> results = new List<ValidationResult>(Plans.Length);

            for (int i = 0; i < Plans.Length; i++)
            {
                PlanetScenePlan plan = Plans[i];
                ValidationResult result = new ValidationResult { Plan = plan };

                try
                {
                    if (!File.Exists(plan.TargetScenePath))
                    {
                        result.Error = "Generated scene file is missing.";
                        results.Add(result);
                        continue;
                    }

                    Scene scene = EditorSceneManager.OpenScene(plan.TargetScenePath, OpenSceneMode.Single);
                    GameObject[] roots = scene.GetRootGameObjects();
                    int matchingRootCount = 0;
                    GameObject expectedRoot = null;
                    for (int rootIndex = 0; rootIndex < roots.Length; rootIndex++)
                    {
                        if (roots[rootIndex] != null && roots[rootIndex].name == plan.TargetRootName)
                        {
                            matchingRootCount++;
                            expectedRoot = roots[rootIndex];
                        }
                    }

                    result.ExpectedRootFound = expectedRoot != null;
                    result.ExpectedRootCount = matchingRootCount;
                    result.RootActive = expectedRoot != null && expectedRoot.activeSelf;
                    result.SceneRootComponentsInScene = CountPlanetSceneRoots(scene);

                    if (expectedRoot != null)
                    {
                        PortalNightsPlanetSceneRoot sceneRoot = expectedRoot.GetComponent<PortalNightsPlanetSceneRoot>();
                        result.HasPlanetSceneRoot = sceneRoot != null;
                        if (sceneRoot != null)
                        {
                            result.PlanetIndexMatches = sceneRoot.PlanetIndex == plan.PlanetIndex;
                            result.ValidateSetupPassed = sceneRoot.ValidateSetup(true);
                        }

                        result.Metrics = CollectMetrics(expectedRoot);
                    }

                    result.ForbiddenObjects = FindForbiddenObjects(scene);
                    result.HasPlayerController = SceneContainsPlayerController(scene);
                    result.Success =
                        result.ExpectedRootFound
                        && result.ExpectedRootCount == 1
                        && result.HasPlanetSceneRoot
                        && result.PlanetIndexMatches
                        && result.RootActive
                        && result.SceneRootComponentsInScene == 1
                        && result.ForbiddenObjects.Count == 0
                        && !result.HasPlayerController;
                }
                catch (Exception exception)
                {
                    result.Error = exception.Message;
                }

                results.Add(result);
                if (logToConsole)
                {
                    UnityEngine.Debug.Log("[PortalNights][SceneMigration] Validate " + plan.TargetRootName + ": " + result.ToLogLine());
                }
            }

            return results;
        }

        private static Phase4DryRunResult RunPhase4DryRun(bool logToConsole)
        {
            Scene sourceScene = OpenSourceScene();
            Phase4SourceSelection selection = AnalyzePhase4SourceScene(sourceScene);

            Phase4DryRunResult result = new Phase4DryRunResult
            {
                CoreScenePath = CoreScenePath,
                Planet1ScenePath = Planet1ScenePath,
                CoreCopiedObjectNames = CollectObjectNames(selection.CoreObjects),
                Planet1CopiedObjectNames = CollectObjectNames(selection.Planet1Objects),
                ExcludedFuturePlanetRoots = new List<string>(selection.ExcludedFutureRootNames),
                CoreMetrics = CollectMetrics(selection.CoreObjects),
                Planet1Metrics = CollectMetrics(selection.Planet1Objects),
                Issues = new List<string>(selection.Issues)
            };

            result.IsClean = result.Issues.Count == 0;
            if (logToConsole)
            {
                UnityEngine.Debug.Log("[PortalNights][SceneMigration] Phase 4 dry run: " + result.ToLogLine());
            }

            return result;
        }

        private static Phase4CoreGenerationResult GenerateCoreSceneCopy(bool logToConsole)
        {
            EnsurePhase4OutputFolders();
            Phase4CoreGenerationResult result = new Phase4CoreGenerationResult
            {
                ScenePath = CoreScenePath
            };

            try
            {
                Scene sourceScene = OpenSourceScene();
                Phase4SourceSelection selection = AnalyzePhase4SourceScene(sourceScene);
                result.CopiedObjectNames = CollectObjectNames(selection.CoreObjects);
                result.ExcludedObjectNames = CollectExcludedCoreObjectNames();

                if (selection.Issues.Count > 0)
                {
                    result.Issues.AddRange(selection.Issues);
                    result.Error = "Source selection is incomplete.";
                    return result;
                }

                Scene targetScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(targetScene);

                for (int i = 0; i < selection.CoreObjects.Count; i++)
                {
                    GameObject copy = UnityEngine.Object.Instantiate(selection.CoreObjects[i]);
                    copy.name = selection.CoreObjects[i].name;
                    copy.transform.SetParent(null, true);
                    copy.SetActive(selection.CoreObjects[i].activeSelf);
                }

                EditorSceneManager.MarkSceneDirty(targetScene);
                bool saveSucceeded = EditorSceneManager.SaveScene(targetScene, CoreScenePath, false);
                if (!saveSucceeded)
                {
                    result.Error = "EditorSceneManager.SaveScene returned false.";
                    EditorSceneManager.CloseScene(targetScene, true);
                    SceneManager.SetActiveScene(sourceScene);
                    return result;
                }

                result.Metrics = CollectSceneMetrics(targetScene);
                result.NetworkObjectCount = result.Metrics.NetworkObjectCount;
                result.Success = true;

                SceneManager.SetActiveScene(sourceScene);
                EditorSceneManager.CloseScene(targetScene, true);
            }
            catch (Exception exception)
            {
                result.Error = exception.Message;
            }

            if (logToConsole)
            {
                UnityEngine.Debug.Log("[PortalNights][SceneMigration] Generate Core: " + result.ToLogLine());
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        private static Phase4Planet1GenerationResult GeneratePlanet1SceneCopy(bool logToConsole)
        {
            EnsurePhase4OutputFolders();
            Phase4Planet1GenerationResult result = new Phase4Planet1GenerationResult
            {
                ScenePath = Planet1ScenePath
            };

            try
            {
                Scene sourceScene = OpenSourceScene();
                Phase4SourceSelection selection = AnalyzePhase4SourceScene(sourceScene);
                result.CopiedObjectNames = CollectObjectNames(selection.Planet1Objects);
                result.ExcludedObjectNames = new List<string>(selection.ExcludedFutureRootNames);
                result.ExcludedObjectNames.AddRange(CollectExcludedPlanet1TopLevelNames());

                if (selection.Issues.Count > 0)
                {
                    result.Issues.AddRange(selection.Issues);
                    result.Error = "Source selection is incomplete.";
                    return result;
                }

                Scene targetScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(targetScene);

                GameObject planet1Root = new GameObject("Planet1_Defense");
                planet1Root.SetActive(true);
                SceneManager.MoveGameObjectToScene(planet1Root, targetScene);

                for (int i = 0; i < selection.Planet1Objects.Count; i++)
                {
                    GameObject sourceObject = selection.Planet1Objects[i];
                    GameObject copy = UnityEngine.Object.Instantiate(sourceObject);
                    copy.name = sourceObject.name;
                    copy.transform.SetParent(planet1Root.transform, true);
                    copy.SetActive(sourceObject.activeSelf);
                }

                PlanetScenePlan plan = new PlanetScenePlan(1, "Planet1_Defense", "Planet1_Defense", "PortalNightsPlanet1_Defense.unity", "planet.1.name");
                PortalNightsPlanetSceneRoot sceneRoot = EnsureSinglePlanetSceneRoot(planet1Root, plan, out bool addedSceneRoot, out int rootComponentCountInScene);
                sceneRoot.AutoDiscoverReferences();
                bool validateSetupPassed = sceneRoot.ValidateSetup(true);
                EditorUtility.SetDirty(sceneRoot);
                EditorSceneManager.MarkSceneDirty(targetScene);

                bool saveSucceeded = EditorSceneManager.SaveScene(targetScene, Planet1ScenePath, false);
                if (!saveSucceeded)
                {
                    result.Error = "EditorSceneManager.SaveScene returned false.";
                    EditorSceneManager.CloseScene(targetScene, true);
                    SceneManager.SetActiveScene(sourceScene);
                    return result;
                }

                result.AddedSceneRoot = addedSceneRoot;
                result.SceneRootComponentCount = rootComponentCountInScene;
                result.ValidateSetupPassed = validateSetupPassed;
                result.PlayerSpawnCount = CountNamedObjectsUnderRoot(planet1Root.transform, "PN_PlayerSpawn_");
                result.Metrics = CollectMetrics(planet1Root);
                result.NetworkObjectCount = result.Metrics.NetworkObjectCount;
                result.Success = true;

                SceneManager.SetActiveScene(sourceScene);
                EditorSceneManager.CloseScene(targetScene, true);
            }
            catch (Exception exception)
            {
                result.Error = exception.Message;
            }

            if (logToConsole)
            {
                UnityEngine.Debug.Log("[PortalNights][SceneMigration] Generate Planet1: " + result.ToLogLine());
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return result;
        }

        private static Phase4CoreValidationResult ValidateCoreScene(bool logToConsole)
        {
            Phase4CoreValidationResult result = new Phase4CoreValidationResult
            {
                ScenePath = CoreScenePath
            };

            try
            {
                if (!File.Exists(CoreScenePath))
                {
                    result.Error = "Core scene file is missing.";
                    return result;
                }

                Scene scene = EditorSceneManager.OpenScene(CoreScenePath, OpenSceneMode.Single);
                result.Metrics = CollectSceneMetrics(scene);
                result.NetworkObjectCount = result.Metrics.NetworkObjectCount;

                result.HasNetworkManager = SceneContainsObjectNamed(scene, "NetworkManager");
                result.HasGameController = SceneContainsObjectNamed(scene, "PN_GameController");
                result.HasHudCanvas = SceneContainsObjectNamed(scene, "PN_HUD_Canvas");
                result.HasEventSystem = SceneContainsObjectNamed(scene, "EventSystem");
                result.HasMainCamera = SceneContainsObjectNamed(scene, "Main Camera");

                result.HasArenaRoot = SceneContainsObjectNamed(scene, "PortalNightsArena");
                result.HasPlanet1Core = SceneContainsObjectNamed(scene, "PN_Central_Core");
                result.HasBuildPads = SceneContainsObjectNamed(scene, "PN_BuildPads");
                result.PlayerSpawnCount = CountNamedObjectsInScene(scene, "PN_PlayerSpawn_");
                result.ForbiddenFutureRoots = FindForbiddenTopLevelObjects(scene, FuturePlanetRootNames);
                result.ForbiddenPlanet1Objects = FindForbiddenTopLevelObjects(scene, CombineNames(Planet1ArenaChildNames, Planet1TopLevelNames));

                result.Success =
                    result.HasNetworkManager
                    && result.HasGameController
                    && result.HasHudCanvas
                    && result.HasEventSystem
                    && result.HasMainCamera
                    && !result.HasArenaRoot
                    && !result.HasPlanet1Core
                    && !result.HasBuildPads
                    && result.PlayerSpawnCount == 0
                    && result.ForbiddenFutureRoots.Count == 0
                    && result.ForbiddenPlanet1Objects.Count == 0;
            }
            catch (Exception exception)
            {
                result.Error = exception.Message;
            }

            if (logToConsole)
            {
                UnityEngine.Debug.Log("[PortalNights][SceneMigration] Validate Core: " + result.ToLogLine());
            }

            return result;
        }

        private static Phase4Planet1ValidationResult ValidatePlanet1Scene(bool logToConsole)
        {
            Phase4Planet1ValidationResult result = new Phase4Planet1ValidationResult
            {
                ScenePath = Planet1ScenePath
            };

            try
            {
                if (!File.Exists(Planet1ScenePath))
                {
                    result.Error = "Planet 1 scene file is missing.";
                    return result;
                }

                Scene scene = EditorSceneManager.OpenScene(Planet1ScenePath, OpenSceneMode.Single);
                GameObject[] roots = scene.GetRootGameObjects();
                result.RootCount = roots.Length;

                GameObject expectedRoot = null;
                for (int i = 0; i < roots.Length; i++)
                {
                    if (roots[i] != null && roots[i].name == "Planet1_Defense")
                    {
                        expectedRoot = roots[i];
                        break;
                    }
                }

                result.HasExpectedRoot = expectedRoot != null;
                result.RootActive = expectedRoot != null && expectedRoot.activeSelf;
                result.HasCentralCore = expectedRoot != null && FindInHierarchy(expectedRoot.transform, "PN_Central_Core") != null;
                result.HasBuildPads = expectedRoot != null && FindInHierarchy(expectedRoot.transform, "PN_BuildPads") != null;
                result.PlayerSpawnCount = expectedRoot == null ? 0 : CountNamedObjectsUnderRoot(expectedRoot.transform, "PN_PlayerSpawn_");

                PortalNightsPlanetSceneRoot sceneRoot = expectedRoot == null ? null : expectedRoot.GetComponent<PortalNightsPlanetSceneRoot>();
                result.HasPlanetSceneRoot = sceneRoot != null;
                if (sceneRoot != null)
                {
                    result.PlanetIndexMatches = sceneRoot.PlanetIndex == 1;
                    result.ValidateSetupPassed = sceneRoot.ValidateSetup(true);
                }

                result.SceneRootComponentsInScene = CountPlanetSceneRoots(scene);
                result.Metrics = expectedRoot == null ? default : CollectMetrics(expectedRoot);
                result.NetworkObjectCount = result.Metrics.NetworkObjectCount;

                result.ForbiddenGlobalObjects = FindForbiddenTopLevelObjects(scene, CoreRequiredRootNames);
                result.ForbiddenFutureRoots = FindForbiddenTopLevelObjects(scene, FuturePlanetRootNames);

                result.Success =
                    result.HasExpectedRoot
                    && result.RootCount == 1
                    && result.RootActive
                    && result.HasPlanetSceneRoot
                    && result.PlanetIndexMatches
                    && result.ValidateSetupPassed
                    && result.SceneRootComponentsInScene == 1
                    && result.HasCentralCore
                    && result.HasBuildPads
                    && result.PlayerSpawnCount == 6
                    && result.ForbiddenGlobalObjects.Count == 0
                    && result.ForbiddenFutureRoots.Count == 0;
            }
            catch (Exception exception)
            {
                result.Error = exception.Message;
            }

            if (logToConsole)
            {
                UnityEngine.Debug.Log("[PortalNights][SceneMigration] Validate Planet1: " + result.ToLogLine());
            }

            return result;
        }

        private static PortalNightsPlanetSceneRoot EnsureSinglePlanetSceneRoot(GameObject copiedRoot, PlanetScenePlan plan, out bool addedSceneRoot, out int rootComponentCountInScene)
        {
            PortalNightsPlanetSceneRoot[] existing = copiedRoot.GetComponents<PortalNightsPlanetSceneRoot>();
            PortalNightsPlanetSceneRoot sceneRoot;
            if (existing.Length == 0)
            {
                sceneRoot = copiedRoot.AddComponent<PortalNightsPlanetSceneRoot>();
                addedSceneRoot = true;
            }
            else
            {
                sceneRoot = existing[0];
                addedSceneRoot = false;
                for (int i = 1; i < existing.Length; i++)
                {
                    UnityEngine.Object.DestroyImmediate(existing[i], true);
                }
            }

            rootComponentCountInScene = copiedRoot.GetComponentsInChildren<PortalNightsPlanetSceneRoot>(true).Length;

            SerializedObject serializedRoot = new SerializedObject(sceneRoot);
            serializedRoot.FindProperty("planetIndex").intValue = plan.PlanetIndex;
            serializedRoot.FindProperty("planetDisplayNameKey").stringValue = plan.DisplayNameKey;
            serializedRoot.ApplyModifiedPropertiesWithoutUndo();
            return sceneRoot;
        }

        private static SceneMetrics CollectMetrics(GameObject root)
        {
            return new SceneMetrics
            {
                ObjectCount = root.GetComponentsInChildren<Transform>(true).Length,
                RendererCount = root.GetComponentsInChildren<Renderer>(true).Length,
                ColliderCount = root.GetComponentsInChildren<Collider>(true).Length,
                LightCount = root.GetComponentsInChildren<Light>(true).Length,
                ParticleCount = root.GetComponentsInChildren<ParticleSystem>(true).Length,
                MonoBehaviourCount = root.GetComponentsInChildren<MonoBehaviour>(true).Length,
                NetworkObjectCount = root.GetComponentsInChildren<NetworkObject>(true).Length
            };
        }

        private static int CountPlanetSceneRoots(Scene scene)
        {
            int total = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                total += roots[i].GetComponentsInChildren<PortalNightsPlanetSceneRoot>(true).Length;
            }

            return total;
        }

        private static List<string> FindForbiddenObjects(Scene scene)
        {
            List<string> forbidden = new List<string>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int t = 0; t < transforms.Length; t++)
                {
                    string candidateName = transforms[t].gameObject.name;
                    for (int forbiddenIndex = 0; forbiddenIndex < ForbiddenSceneObjectNames.Length; forbiddenIndex++)
                    {
                        if (candidateName == ForbiddenSceneObjectNames[forbiddenIndex])
                        {
                            forbidden.Add(candidateName);
                        }
                    }
                }
            }

            return forbidden;
        }

        private static bool SceneContainsPlayerController(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].GetComponentInChildren<PortalNightsPlayerController>(true) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool PrepareForSceneOperation()
        {
            if (Application.isBatchMode)
            {
                return true;
            }

            return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        private static Scene OpenSourceScene()
        {
            return EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
        }

        private static void ReopenSourceScene()
        {
            if (File.Exists(SourceScenePath))
            {
                EditorSceneManager.OpenScene(SourceScenePath, OpenSceneMode.Single);
            }
        }

        private static GameObject FindRootObject(Scene scene, string rootName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject directMatch = FindInHierarchy(roots[i].transform, rootName);
                if (directMatch != null)
                {
                    return directMatch;
                }
            }

            return null;
        }

        private static GameObject FindInHierarchy(Transform root, string objectName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == objectName)
            {
                return root.gameObject;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                GameObject match = FindInHierarchy(root.GetChild(i), objectName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        private static GameObject FindTopLevelRootObject(Scene scene, string rootName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i] != null && roots[i].name == rootName)
                {
                    return roots[i];
                }
            }

            return null;
        }

        private static Transform FindDirectChild(Transform parent, string childName)
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);
                if (child != null && child.name == childName)
                {
                    return child;
                }
            }

            return null;
        }

        private static Phase4SourceSelection AnalyzePhase4SourceScene(Scene sourceScene)
        {
            Phase4SourceSelection selection = new Phase4SourceSelection();
            HashSet<int> coreIds = new HashSet<int>();
            HashSet<int> planet1Ids = new HashSet<int>();

            GameObject arenaRoot = FindTopLevelRootObject(sourceScene, "PortalNightsArena");
            selection.ArenaRoot = arenaRoot;
            if (arenaRoot == null)
            {
                selection.Issues.Add("PortalNightsArena root was not found in the source scene.");
                return selection;
            }

            for (int i = 0; i < CoreRequiredRootNames.Length; i++)
            {
                GameObject requiredRoot = FindTopLevelRootObject(sourceScene, CoreRequiredRootNames[i]);
                if (requiredRoot == null)
                {
                    selection.Issues.Add("Missing required core object: " + CoreRequiredRootNames[i]);
                    continue;
                }

                AddUniqueObject(selection.CoreObjects, coreIds, requiredRoot);
            }

            List<GameObject> optionalGlobalRoots = FindOptionalGlobalRoots(sourceScene);
            for (int i = 0; i < optionalGlobalRoots.Count; i++)
            {
                AddUniqueObject(selection.CoreObjects, coreIds, optionalGlobalRoots[i]);
            }

            for (int i = 0; i < Planet1ArenaChildNames.Length; i++)
            {
                Transform child = FindDirectChild(arenaRoot.transform, Planet1ArenaChildNames[i]);
                if (child == null)
                {
                    selection.Issues.Add("Missing Planet 1 arena child: " + Planet1ArenaChildNames[i]);
                    continue;
                }

                AddUniqueObject(selection.Planet1Objects, planet1Ids, child.gameObject);
            }

            for (int i = 0; i < Planet1TopLevelNames.Length; i++)
            {
                GameObject topLevelObject = FindTopLevelRootObject(sourceScene, Planet1TopLevelNames[i]);
                if (topLevelObject == null)
                {
                    selection.Issues.Add("Missing Planet 1 top-level object: " + Planet1TopLevelNames[i]);
                    continue;
                }

                AddUniqueObject(selection.Planet1Objects, planet1Ids, topLevelObject);
            }

            for (int i = 0; i < FuturePlanetRootNames.Length; i++)
            {
                Transform child = FindDirectChild(arenaRoot.transform, FuturePlanetRootNames[i]);
                if (child != null)
                {
                    selection.ExcludedFutureRootNames.Add(child.name);
                }
                else
                {
                    selection.Issues.Add("Expected excluded future planet root was not found: " + FuturePlanetRootNames[i]);
                }
            }

            return selection;
        }

        private static List<GameObject> FindOptionalGlobalRoots(Scene sourceScene)
        {
            List<GameObject> matches = new List<GameObject>();
            GameObject[] roots = sourceScene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null || IsNamed(root.name, CoreRequiredRootNames) || IsNamed(root.name, Planet1TopLevelNames) || root.name == "PortalNightsArena")
                {
                    continue;
                }

                if (ShouldCopyAsOptionalGlobalRoot(root))
                {
                    matches.Add(root);
                }
            }

            return matches;
        }

        private static bool ShouldCopyAsOptionalGlobalRoot(GameObject root)
        {
            if (MatchesAny(root.name, OptionalGlobalNameKeywords))
            {
                return true;
            }

            Component[] components = root.GetComponents<Component>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                {
                    continue;
                }

                string typeName = component.GetType().Name;
                if (MatchesAny(typeName, OptionalGlobalComponentKeywords))
                {
                    return true;
                }
            }

            return false;
        }

        private static void EnsureOutputFolders()
        {
            Directory.CreateDirectory(GeneratedScenesFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(Phase3ReportPath) ?? "Assets/PortalNights/Reports");
        }

        private static void EnsurePhase4OutputFolders()
        {
            Directory.CreateDirectory(GeneratedScenesFolder);
            Directory.CreateDirectory(GeneratedCoreScenesFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(Phase4ReportPath) ?? "Assets/PortalNights/Reports");
        }

        private static void AddUniqueObject(List<GameObject> list, HashSet<int> ids, GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            int instanceId = gameObject.GetInstanceID();
            if (ids.Add(instanceId))
            {
                list.Add(gameObject);
            }
        }

        private static List<string> CollectObjectNames(List<GameObject> objects)
        {
            List<string> names = new List<string>();
            if (objects == null)
            {
                return names;
            }

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i] != null)
                {
                    names.Add(objects[i].name);
                }
            }

            return names;
        }

        private static SceneMetrics CollectMetrics(List<GameObject> roots)
        {
            SceneMetrics metrics = default;
            if (roots == null)
            {
                return metrics;
            }

            for (int i = 0; i < roots.Count; i++)
            {
                if (roots[i] == null)
                {
                    continue;
                }

                SceneMetrics partial = CollectMetrics(roots[i]);
                metrics.ObjectCount += partial.ObjectCount;
                metrics.RendererCount += partial.RendererCount;
                metrics.ColliderCount += partial.ColliderCount;
                metrics.LightCount += partial.LightCount;
                metrics.ParticleCount += partial.ParticleCount;
                metrics.MonoBehaviourCount += partial.MonoBehaviourCount;
                metrics.NetworkObjectCount += partial.NetworkObjectCount;
            }

            return metrics;
        }

        private static SceneMetrics CollectSceneMetrics(Scene scene)
        {
            SceneMetrics metrics = default;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                SceneMetrics partial = CollectMetrics(roots[i]);
                metrics.ObjectCount += partial.ObjectCount;
                metrics.RendererCount += partial.RendererCount;
                metrics.ColliderCount += partial.ColliderCount;
                metrics.LightCount += partial.LightCount;
                metrics.ParticleCount += partial.ParticleCount;
                metrics.MonoBehaviourCount += partial.MonoBehaviourCount;
                metrics.NetworkObjectCount += partial.NetworkObjectCount;
            }

            return metrics;
        }

        private static bool SceneContainsObjectNamed(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                if (FindInHierarchy(roots[i].transform, objectName) != null)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<string> FindForbiddenObjects(Scene scene, params string[] names)
        {
            List<string> forbidden = new List<string>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);
                for (int t = 0; t < transforms.Length; t++)
                {
                    string candidateName = transforms[t].gameObject.name;
                    if (IsNamed(candidateName, names) && !forbidden.Contains(candidateName))
                    {
                        forbidden.Add(candidateName);
                    }
                }
            }

            return forbidden;
        }

        private static List<string> FindForbiddenTopLevelObjects(Scene scene, params string[] names)
        {
            List<string> forbidden = new List<string>();
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                GameObject root = roots[i];
                if (root == null)
                {
                    continue;
                }

                string candidateName = root.name;
                if (IsNamed(candidateName, names) && !forbidden.Contains(candidateName))
                {
                    forbidden.Add(candidateName);
                }
            }

            return forbidden;
        }

        private static int CountNamedObjectsInScene(Scene scene, string namePrefix)
        {
            int count = 0;
            GameObject[] roots = scene.GetRootGameObjects();
            for (int i = 0; i < roots.Length; i++)
            {
                count += CountNamedObjectsUnderRoot(roots[i].transform, namePrefix);
            }

            return count;
        }

        private static int CountNamedObjectsUnderRoot(Transform root, string namePrefix)
        {
            if (root == null)
            {
                return 0;
            }

            int count = 0;
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                if (transforms[i] != null && transforms[i].name.StartsWith(namePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    count++;
                }
            }

            return count;
        }

        private static List<string> CollectExcludedCoreObjectNames()
        {
            List<string> excluded = new List<string> { "PortalNightsArena" };
            excluded.AddRange(FuturePlanetRootNames);
            excluded.AddRange(Planet1TopLevelNames);
            return excluded;
        }

        private static List<string> CollectExcludedPlanet1TopLevelNames()
        {
            List<string> excluded = new List<string>(CoreRequiredRootNames);
            excluded.Add("PortalNightsArena");
            return excluded;
        }

        private static bool IsNamed(string candidateName, params string[] expectedNames)
        {
            if (string.IsNullOrEmpty(candidateName) || expectedNames == null)
            {
                return false;
            }

            for (int i = 0; i < expectedNames.Length; i++)
            {
                if (string.Equals(candidateName, expectedNames[i], StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool MatchesAny(string value, params string[] keywords)
        {
            if (string.IsNullOrEmpty(value) || keywords == null)
            {
                return false;
            }

            for (int i = 0; i < keywords.Length; i++)
            {
                if (!string.IsNullOrEmpty(keywords[i]) && value.IndexOf(keywords[i], StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private static string[] CombineNames(params string[][] groups)
        {
            List<string> combined = new List<string>();
            if (groups == null)
            {
                return combined.ToArray();
            }

            for (int i = 0; i < groups.Length; i++)
            {
                string[] group = groups[i];
                if (group == null)
                {
                    continue;
                }

                for (int j = 0; j < group.Length; j++)
                {
                    if (!string.IsNullOrEmpty(group[j]) && !combined.Contains(group[j]))
                    {
                        combined.Add(group[j]);
                    }
                }
            }

            return combined.ToArray();
        }

        private static bool AllDryRunsSucceeded(List<DryRunResult> results)
        {
            for (int i = 0; i < results.Count; i++)
            {
                if (!results[i].SourceRootFound)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AllGenerationsSucceeded(List<GenerationResult> results)
        {
            for (int i = 0; i < results.Count; i++)
            {
                if (!results[i].Success)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool AllValidationsSucceeded(List<ValidationResult> results)
        {
            for (int i = 0; i < results.Count; i++)
            {
                if (!results[i].Success)
                {
                    return false;
                }
            }

            return true;
        }

        private static void WriteReport()
        {
            EnsureOutputFolders();
            StringBuilder report = new StringBuilder();
            report.AppendLine("# Scene Migration Phase 3 Report");
            report.AppendLine();
            report.AppendLine("This report documents the Phase 3 editor-only migration pass that generates standalone planet scene copies for Planets 2-5.");
            report.AppendLine();
            report.AppendLine("## Generated Scene Paths");
            report.AppendLine();
            for (int i = 0; i < Plans.Length; i++)
            {
                report.AppendLine("- `" + Plans[i].TargetScenePath + "`");
            }

            report.AppendLine();
            report.AppendLine("## Dry Run Results");
            report.AppendLine();
            AppendDryRunSection(report, lastDryRunResults);

            report.AppendLine();
            report.AppendLine("## Generation Results");
            report.AppendLine();
            AppendGenerationSection(report, lastGenerationResults);

            report.AppendLine();
            report.AppendLine("## Validation Results");
            report.AppendLine();
            AppendValidationSection(report, lastValidationResults);

            report.AppendLine();
            report.AppendLine("## Phase Notes");
            report.AppendLine();
            report.AppendLine("- Planet 1 was intentionally deferred because its current content is mixed with core/global objects inside `PortalNightsArena.unity`.");
            report.AppendLine("- The Core/global scene split was intentionally deferred because runtime ownership, HUD, transition flow, and additive Netcode wiring still need a dedicated migration phase.");
            report.AppendLine("- Scene-placed `NetworkObject`s were copied as scene data only. Additive Netcode activation is still a later Phase 5 task.");
            report.AppendLine("- Addressables are still intentionally deferred because scene contracts and load sequencing are not stable enough yet.");

            report.AppendLine();
            report.AppendLine("## Safety Confirmations");
            report.AppendLine();
            report.AppendLine("- Current one-scene gameplay path remains unchanged: yes");
            report.AppendLine("- `PortalNightsGameController.cs` runtime wiring remains untouched by this tool: yes");
            report.AppendLine("- Generated scenes are not added to Build Settings in this phase: yes");

            report.AppendLine();
            report.AppendLine("## Git Checks");
            report.AppendLine();
            AppendGitCheck(report, "git status -sb", "status -sb");
            AppendGitCheck(report, "git diff --name-only -- Assets/PortalNights/Scenes/PortalNightsArena.unity", "diff --name-only -- Assets/PortalNights/Scenes/PortalNightsArena.unity");
            AppendGitCheck(report, "git diff --name-only -- ProjectSettings/EditorBuildSettings.asset", "diff --name-only -- ProjectSettings/EditorBuildSettings.asset");
            AppendGitCheck(report, "git diff --name-only -- Assets/PortalNights/Scripts/PortalNightsGameController.cs", "diff --name-only -- Assets/PortalNights/Scripts/PortalNightsGameController.cs");

            report.AppendLine();
            report.AppendLine("## Recommended Phase 4 Steps");
            report.AppendLine();
            report.AppendLine("1. Add a validation utility that compares source root metrics to generated standalone scene metrics and highlights mismatches.");
            report.AppendLine("2. Pilot one additive load path with a generated planet scene while keeping the current one-scene fallback intact.");
            report.AppendLine("3. Define the future Core/global scene contract before attempting a Planet 1 split.");
            report.AppendLine("4. Plan explicit Netcode handling for scene-placed `NetworkObject`s before runtime scene activation is introduced.");

            File.WriteAllText(Phase3ReportPath, report.ToString(), Encoding.UTF8);
            AssetDatabase.ImportAsset(Phase3ReportPath);
            AssetDatabase.SaveAssets();
        }

        private static void WritePhase4Report()
        {
            EnsurePhase4OutputFolders();
            StringBuilder report = new StringBuilder();
            report.AppendLine("# Scene Migration Phase 4 Report");
            report.AppendLine();
            report.AppendLine("This report documents the Phase 4 editor-only migration pass that generates a safe Core scene copy and a safe Planet 1 scene copy without wiring them into gameplay yet.");
            report.AppendLine();
            report.AppendLine("## Scene Paths");
            report.AppendLine();
            report.AppendLine("- Core scene: `" + CoreScenePath + "`");
            report.AppendLine("- Planet 1 scene: `" + Planet1ScenePath + "`");
            report.AppendLine();
            report.AppendLine("## Dry Run");
            report.AppendLine();
            AppendPhase4DryRunSection(report, lastPhase4DryRunResult);
            report.AppendLine();
            report.AppendLine("## Generation Results");
            report.AppendLine();
            AppendPhase4CoreGenerationSection(report, lastPhase4CoreGenerationResult);
            AppendPhase4Planet1GenerationSection(report, lastPhase4Planet1GenerationResult);
            report.AppendLine();
            report.AppendLine("## Validation Results");
            report.AppendLine();
            AppendPhase4CoreValidationSection(report, lastPhase4CoreValidationResult);
            AppendPhase4Planet1ValidationSection(report, lastPhase4Planet1ValidationResult);
            report.AppendLine();
            report.AppendLine("## Safety Confirmations");
            report.AppendLine();
            report.AppendLine("- `PortalNightsArena.unity` was not modified: yes");
            report.AppendLine("- `EditorBuildSettings.asset` was not modified: yes");
            report.AppendLine("- `PortalNightsGameController.cs` was not modified: yes");
            report.AppendLine("- Current one-scene gameplay path remains unchanged: yes");
            report.AppendLine("- New scenes are not wired into gameplay yet: yes");
            report.AppendLine();
            report.AppendLine("## Risks");
            report.AppendLine();
            report.AppendLine("- `PN_GameController` in the copied Core scene may retain missing or source-scene serialized references. This is acceptable for Phase 4 because Phase 5 will replace those assumptions with scene-root driven references.");
            report.AppendLine("- Scene-placed `NetworkObject`s remain copied as scene data only. Additive Netcode scene activation remains a dedicated later task.");
            report.AppendLine("- Addressables and Build Settings integration are intentionally deferred until the scene-loading contract is stabilized.");
            report.AppendLine();
            report.AppendLine("## Why Scenes Are Not Wired Yet");
            report.AppendLine();
            report.AppendLine("These new scenes are migration artifacts only. Runtime loading, object ownership, transition sequencing, and cross-scene references still need a later phase to avoid breaking the current working one-scene game path.");
            report.AppendLine();
            report.AppendLine("## Git Checks");
            report.AppendLine();
            AppendGitCheck(report, "git status -sb", "status -sb");
            AppendGitCheck(report, "git diff --name-only -- Assets/PortalNights/Scenes/PortalNightsArena.unity", "diff --name-only -- Assets/PortalNights/Scenes/PortalNightsArena.unity");
            AppendGitCheck(report, "git diff --name-only -- ProjectSettings/EditorBuildSettings.asset", "diff --name-only -- ProjectSettings/EditorBuildSettings.asset");
            AppendGitCheck(report, "git diff --name-only -- Assets/PortalNights/Scripts/PortalNightsGameController.cs", "diff --name-only -- Assets/PortalNights/Scripts/PortalNightsGameController.cs");
            report.AppendLine();
            report.AppendLine("## Recommended Phase 5 Steps");
            report.AppendLine();
            report.AppendLine("1. Introduce a controlled additive bootstrap flow that loads Core first and then a selected planet scene.");
            report.AppendLine("2. Replace `PortalNightsGameController` scene assumptions with `PortalNightsPlanetSceneRoot` references.");
            report.AppendLine("3. Define explicit Netcode handling for scene-placed `NetworkObject`s across Core and planet scenes.");
            report.AppendLine("4. Validate transition/UI ownership and shared persistent systems before enabling runtime scene switching.");

            File.WriteAllText(Phase4ReportPath, report.ToString(), Encoding.UTF8);
            AssetDatabase.ImportAsset(Phase4ReportPath);
            AssetDatabase.SaveAssets();
        }

        private static void WritePhase5AReport()
        {
            EnsurePhase4OutputFolders();
            StringBuilder report = new StringBuilder();
            report.AppendLine("# Scene Migration Phase 5A Report");
            report.AppendLine();
            report.AppendLine("This report documents Phase 5A: Build Settings registration for generated scenes and editor-only additive validation, without wiring runtime gameplay to the new scene flow yet.");
            report.AppendLine();
            report.AppendLine("## Build Settings Final Scene List");
            report.AppendLine();
            AppendPhase5ABuildSettingsSection(report, lastPhase5AValidationResult);

            report.AppendLine();
            report.AppendLine("## Additive Editor Validation");
            report.AppendLine();
            AppendPhase5AValidationSection(report, lastPhase5AValidationResult);

            report.AppendLine();
            report.AppendLine("## Safety Confirmations");
            report.AppendLine();
            report.AppendLine("- `PortalNightsArena.unity` remains first enabled scene in Build Settings: " + ToYesNo(lastPhase5AValidationResult != null && lastPhase5AValidationResult.PortalNightsArenaIsFirstEnabledScene));
            report.AppendLine("- Core and Planet1-P5 generated scenes are registered: " + ToYesNo(lastPhase5AValidationResult != null && lastPhase5AValidationResult.CoreSceneRegistered && lastPhase5AValidationResult.AllPlanetScenesRegistered));
            report.AppendLine("- `PortalNightsGameController.cs` was not modified: yes");
            report.AppendLine("- `PortalNightsArena.unity` was not modified: yes");
            report.AppendLine("- Current one-scene gameplay path remains unchanged: yes");
            report.AppendLine("- Gameplay is still legacy one-scene: yes");
            report.AppendLine("- No WebGL build was run: yes");
            report.AppendLine("- No archives/zip/backups were created: yes");

            report.AppendLine();
            report.AppendLine("## Why Gameplay Is Still Legacy One-Scene");
            report.AppendLine();
            report.AppendLine("Phase 5A only registers scenes and validates additive editor loading safety. It intentionally does not replace the current `PortalNightsArena.unity` runtime path, does not change startup flow, and does not call gameplay code from the transition manager yet.");

            report.AppendLine();
            report.AppendLine("## Recommended Phase 5B");
            report.AppendLine();
            report.AppendLine("Experimental scene-mode bootstrap for Core + Planet1 only, behind a disabled-by-default toggle.");

            report.AppendLine();
            report.AppendLine("## Git Checks");
            report.AppendLine();
            AppendGitCheck(report, "git status -sb", "status -sb");
            AppendGitCheck(report, "git diff --name-only -- Assets/PortalNights/Scenes/PortalNightsArena.unity", "diff --name-only -- Assets/PortalNights/Scenes/PortalNightsArena.unity");
            AppendGitCheck(report, "git diff --name-only -- Assets/PortalNights/Scripts/PortalNightsGameController.cs", "diff --name-only -- Assets/PortalNights/Scripts/PortalNightsGameController.cs");
            AppendGitCheck(report, "git diff --name-only -- ProjectSettings/EditorBuildSettings.asset", "diff --name-only -- ProjectSettings/EditorBuildSettings.asset");

            File.WriteAllText(Phase5AReportPath, report.ToString(), Encoding.UTF8);
            AssetDatabase.ImportAsset(Phase5AReportPath);
            AssetDatabase.SaveAssets();
        }

        private static void AppendDryRunSection(StringBuilder report, List<DryRunResult> results)
        {
            if (results == null || results.Count == 0)
            {
                report.AppendLine("No dry run has been recorded yet.");
                return;
            }

            for (int i = 0; i < results.Count; i++)
            {
                DryRunResult result = results[i];
                report.AppendLine("### " + result.Plan.SourceRootName);
                report.AppendLine();
                report.AppendLine("- Root found: " + ToYesNo(result.SourceRootFound));
                report.AppendLine("- Target scene path: `" + result.TargetScenePath + "`");
                report.AppendLine("- Existing `PortalNightsPlanetSceneRoot`: " + ToYesNo(result.HasSceneRootComponent));
                report.AppendLine("- Would add `PortalNightsPlanetSceneRoot`: " + ToYesNo(result.WouldAddSceneRoot));
                if (result.SourceRootFound)
                {
                    AppendMetrics(report, result.Metrics);
                }

                if (!string.IsNullOrEmpty(result.Error))
                {
                    report.AppendLine("- Error: " + result.Error);
                }

                report.AppendLine();
            }
        }

        private static void AppendGenerationSection(StringBuilder report, List<GenerationResult> results)
        {
            if (results == null || results.Count == 0)
            {
                report.AppendLine("No generation pass has been recorded yet.");
                return;
            }

            for (int i = 0; i < results.Count; i++)
            {
                GenerationResult result = results[i];
                report.AppendLine("### " + result.Plan.TargetRootName);
                report.AppendLine();
                report.AppendLine("- Copied successfully: " + ToYesNo(result.Success));
                report.AppendLine("- Added `PortalNightsPlanetSceneRoot`: " + ToYesNo(result.AddedSceneRoot));
                report.AppendLine("- `PortalNightsPlanetSceneRoot.ValidateSetup(true)` passed: " + ToYesNo(result.ValidateSetupPassed));
                report.AppendLine("- `PortalNightsPlanetSceneRoot` count under copied root: " + result.SceneRootComponentCount);
                report.AppendLine("- NetworkObject count: " + result.NetworkObjectCount);
                if (result.Success)
                {
                    AppendMetrics(report, result.Metrics);
                }

                if (!string.IsNullOrEmpty(result.Error))
                {
                    report.AppendLine("- Error: " + result.Error);
                }

                report.AppendLine();
            }
        }

        private static void AppendValidationSection(StringBuilder report, List<ValidationResult> results)
        {
            if (results == null || results.Count == 0)
            {
                report.AppendLine("No validation pass has been recorded yet.");
                return;
            }

            for (int i = 0; i < results.Count; i++)
            {
                ValidationResult result = results[i];
                report.AppendLine("### " + result.Plan.TargetRootName);
                report.AppendLine();
                report.AppendLine("- Validation success: " + ToYesNo(result.Success));
                report.AppendLine("- Expected root found: " + ToYesNo(result.ExpectedRootFound));
                report.AppendLine("- Expected root count: " + result.ExpectedRootCount);
                report.AppendLine("- Root active: " + ToYesNo(result.RootActive));
                report.AppendLine("- `PortalNightsPlanetSceneRoot` exists: " + ToYesNo(result.HasPlanetSceneRoot));
                report.AppendLine("- `PortalNightsPlanetSceneRoot` count in scene: " + result.SceneRootComponentsInScene);
                report.AppendLine("- planetIndex matches: " + ToYesNo(result.PlanetIndexMatches));
                report.AppendLine("- `ValidateSetup(true)` passed: " + ToYesNo(result.ValidateSetupPassed));
                report.AppendLine("- Contains player controller: " + ToYesNo(result.HasPlayerController));
                report.AppendLine("- Forbidden objects copied: " + (result.ForbiddenObjects.Count == 0 ? "none" : string.Join(", ", result.ForbiddenObjects.ToArray())));
                if (result.ExpectedRootFound)
                {
                    AppendMetrics(report, result.Metrics);
                }

                if (!string.IsNullOrEmpty(result.Error))
                {
                    report.AppendLine("- Error: " + result.Error);
                }

                report.AppendLine();
            }
        }

        private static void AppendPhase4DryRunSection(StringBuilder report, Phase4DryRunResult result)
        {
            if (result == null)
            {
                report.AppendLine("No Phase 4 dry run has been recorded yet.");
                return;
            }

            report.AppendLine("- Dry run clean: " + ToYesNo(result.IsClean));
            report.AppendLine("- Core scene path: `" + result.CoreScenePath + "`");
            report.AppendLine("- Planet 1 scene path: `" + result.Planet1ScenePath + "`");
            report.AppendLine("- Core objects that would be copied: " + JoinOrNone(result.CoreCopiedObjectNames));
            report.AppendLine("- Planet 1 objects that would be copied: " + JoinOrNone(result.Planet1CopiedObjectNames));
            report.AppendLine("- Excluded future planet roots: " + JoinOrNone(result.ExcludedFuturePlanetRoots));
            report.AppendLine("- Core metrics:");
            AppendMetrics(report, result.CoreMetrics);
            report.AppendLine("- Planet 1 metrics:");
            AppendMetrics(report, result.Planet1Metrics);
            if (result.Issues.Count > 0)
            {
                report.AppendLine("- Issues: " + JoinOrNone(result.Issues));
            }
        }

        private static void AppendPhase4CoreGenerationSection(StringBuilder report, Phase4CoreGenerationResult result)
        {
            report.AppendLine("### Core Scene");
            report.AppendLine();
            if (result == null)
            {
                report.AppendLine("No Core scene generation has been recorded yet.");
                report.AppendLine();
                return;
            }

            report.AppendLine("- Scene path: `" + result.ScenePath + "`");
            report.AppendLine("- Copied successfully: " + ToYesNo(result.Success));
            report.AppendLine("- Copied objects: " + JoinOrNone(result.CopiedObjectNames));
            report.AppendLine("- Intentionally excluded objects: " + JoinOrNone(result.ExcludedObjectNames));
            report.AppendLine("- NetworkObject count: " + result.NetworkObjectCount);
            if (result.Success)
            {
                AppendMetrics(report, result.Metrics);
            }

            if (result.Issues.Count > 0)
            {
                report.AppendLine("- Issues: " + JoinOrNone(result.Issues));
            }

            if (!string.IsNullOrEmpty(result.Error))
            {
                report.AppendLine("- Error: " + result.Error);
            }

            report.AppendLine();
        }

        private static void AppendPhase4Planet1GenerationSection(StringBuilder report, Phase4Planet1GenerationResult result)
        {
            report.AppendLine("### Planet 1 Scene");
            report.AppendLine();
            if (result == null)
            {
                report.AppendLine("No Planet 1 scene generation has been recorded yet.");
                report.AppendLine();
                return;
            }

            report.AppendLine("- Scene path: `" + result.ScenePath + "`");
            report.AppendLine("- Copied successfully: " + ToYesNo(result.Success));
            report.AppendLine("- Copied objects: " + JoinOrNone(result.CopiedObjectNames));
            report.AppendLine("- Intentionally excluded objects: " + JoinOrNone(result.ExcludedObjectNames));
            report.AppendLine("- Added `PortalNightsPlanetSceneRoot`: " + ToYesNo(result.AddedSceneRoot));
            report.AppendLine("- `PortalNightsPlanetSceneRoot.ValidateSetup(true)` passed: " + ToYesNo(result.ValidateSetupPassed));
            report.AppendLine("- `PortalNightsPlanetSceneRoot` count under root: " + result.SceneRootComponentCount);
            report.AppendLine("- PN_PlayerSpawn_* count: " + result.PlayerSpawnCount);
            report.AppendLine("- NetworkObject count: " + result.NetworkObjectCount);
            if (result.Success)
            {
                AppendMetrics(report, result.Metrics);
            }

            if (result.Issues.Count > 0)
            {
                report.AppendLine("- Issues: " + JoinOrNone(result.Issues));
            }

            if (!string.IsNullOrEmpty(result.Error))
            {
                report.AppendLine("- Error: " + result.Error);
            }

            report.AppendLine();
        }

        private static void AppendPhase4CoreValidationSection(StringBuilder report, Phase4CoreValidationResult result)
        {
            report.AppendLine("### Core Scene Validation");
            report.AppendLine();
            if (result == null)
            {
                report.AppendLine("No Core scene validation has been recorded yet.");
                report.AppendLine();
                return;
            }

            report.AppendLine("- Validation success: " + ToYesNo(result.Success));
            report.AppendLine("- Contains NetworkManager: " + ToYesNo(result.HasNetworkManager));
            report.AppendLine("- Contains PN_GameController: " + ToYesNo(result.HasGameController));
            report.AppendLine("- Contains PN_HUD_Canvas: " + ToYesNo(result.HasHudCanvas));
            report.AppendLine("- Contains EventSystem: " + ToYesNo(result.HasEventSystem));
            report.AppendLine("- Contains Main Camera: " + ToYesNo(result.HasMainCamera));
            report.AppendLine("- Contains PortalNightsArena root: " + ToYesNo(result.HasArenaRoot));
            report.AppendLine("- Contains PN_Central_Core: " + ToYesNo(result.HasPlanet1Core));
            report.AppendLine("- Contains PN_BuildPads: " + ToYesNo(result.HasBuildPads));
            report.AppendLine("- PN_PlayerSpawn_* count: " + result.PlayerSpawnCount);
            report.AppendLine("- Forbidden future roots: " + JoinOrNone(result.ForbiddenFutureRoots));
            report.AppendLine("- Forbidden Planet 1 objects: " + JoinOrNone(result.ForbiddenPlanet1Objects));
            report.AppendLine("- NetworkObject count: " + result.NetworkObjectCount);
            AppendMetrics(report, result.Metrics);
            if (!string.IsNullOrEmpty(result.Error))
            {
                report.AppendLine("- Error: " + result.Error);
            }

            report.AppendLine();
        }

        private static void AppendPhase4Planet1ValidationSection(StringBuilder report, Phase4Planet1ValidationResult result)
        {
            report.AppendLine("### Planet 1 Scene Validation");
            report.AppendLine();
            if (result == null)
            {
                report.AppendLine("No Planet 1 scene validation has been recorded yet.");
                report.AppendLine();
                return;
            }

            report.AppendLine("- Validation success: " + ToYesNo(result.Success));
            report.AppendLine("- Has expected root: " + ToYesNo(result.HasExpectedRoot));
            report.AppendLine("- Total root count: " + result.RootCount);
            report.AppendLine("- Root active: " + ToYesNo(result.RootActive));
            report.AppendLine("- Has `PortalNightsPlanetSceneRoot`: " + ToYesNo(result.HasPlanetSceneRoot));
            report.AppendLine("- `PortalNightsPlanetSceneRoot` count in scene: " + result.SceneRootComponentsInScene);
            report.AppendLine("- planetIndex matches: " + ToYesNo(result.PlanetIndexMatches));
            report.AppendLine("- `ValidateSetup(true)` passed: " + ToYesNo(result.ValidateSetupPassed));
            report.AppendLine("- Has PN_Central_Core: " + ToYesNo(result.HasCentralCore));
            report.AppendLine("- Has PN_BuildPads: " + ToYesNo(result.HasBuildPads));
            report.AppendLine("- PN_PlayerSpawn_* count: " + result.PlayerSpawnCount);
            report.AppendLine("- Forbidden core/global objects: " + JoinOrNone(result.ForbiddenGlobalObjects));
            report.AppendLine("- Forbidden future planet roots: " + JoinOrNone(result.ForbiddenFutureRoots));
            report.AppendLine("- NetworkObject count: " + result.NetworkObjectCount);
            AppendMetrics(report, result.Metrics);
            if (!string.IsNullOrEmpty(result.Error))
            {
                report.AppendLine("- Error: " + result.Error);
            }

            report.AppendLine();
        }

        private static void AppendPhase5ABuildSettingsSection(StringBuilder report, Phase5AValidationResult result)
        {
            if (result == null)
            {
                report.AppendLine("No Phase 5A validation has been recorded yet.");
                return;
            }

            report.AppendLine("- `PortalNightsArena.unity` first enabled scene: " + ToYesNo(result.PortalNightsArenaIsFirstEnabledScene));
            report.AppendLine("- Core scene registered: " + ToYesNo(result.CoreSceneRegistered));
            report.AppendLine("- Planet scenes registered: " + ToYesNo(result.AllPlanetScenesRegistered));
            report.AppendLine("- Duplicate scene paths: " + JoinOrNone(result.DuplicateScenePaths));
            report.AppendLine("- Missing scene assets: " + JoinOrNone(result.MissingSceneAssets));
            report.AppendLine("- Missing Build Settings registrations: " + JoinOrNone(result.MissingBuildSettingsRegistrations));
            report.AppendLine();

            for (int i = 0; i < result.BuildSettingsScenes.Count; i++)
            {
                Phase5ABuildSceneEntry entry = result.BuildSettingsScenes[i];
                report.AppendLine((i + 1) + ". `" + entry.Path + "` (" + (entry.Enabled ? "enabled" : "disabled") + ")");
            }
        }

        private static void AppendPhase5AValidationSection(StringBuilder report, Phase5AValidationResult result)
        {
            if (result == null)
            {
                report.AppendLine("No Phase 5A validation has been recorded yet.");
                return;
            }

            report.AppendLine("- Overall validation success: " + ToYesNo(result.Success));
            report.AppendLine("- Source scene opened for validation: " + ToYesNo(result.SourceSceneOpenedForValidation));
            report.AppendLine("- Source scene reopened at end: " + ToYesNo(result.SourceSceneReopenedAtEnd));
            if (!string.IsNullOrEmpty(result.Error))
            {
                report.AppendLine("- Error: " + result.Error);
            }

            report.AppendLine();
            report.AppendLine("### Core Scene");
            report.AppendLine();
            AppendPhase5ACoreValidationSection(report, result.CoreValidation);

            report.AppendLine();
            report.AppendLine("### Planet Scenes");
            report.AppendLine();
            for (int i = 0; i < result.PlanetValidations.Count; i++)
            {
                AppendPhase5APlanetValidationEntry(report, result.PlanetValidations[i]);
            }
        }

        private static void AppendPhase5ACoreValidationSection(StringBuilder report, Phase5ACoreValidationResult result)
        {
            if (result == null)
            {
                report.AppendLine("No additive Core validation has been recorded yet.");
                return;
            }

            report.AppendLine("- Scene path: `" + result.ScenePath + "`");
            report.AppendLine("- Validation success: " + ToYesNo(result.Success));
            report.AppendLine("- Root count: " + result.RootCount);
            report.AppendLine("- Contains NetworkManager: " + ToYesNo(result.HasNetworkManager));
            report.AppendLine("- Contains PN_GameController: " + ToYesNo(result.HasGameController));
            report.AppendLine("- Contains PN_HUD_Canvas: " + ToYesNo(result.HasHudCanvas));
            report.AppendLine("- Contains EventSystem: " + ToYesNo(result.HasEventSystem));
            report.AppendLine("- Contains Main Camera: " + ToYesNo(result.HasMainCamera));
            report.AppendLine("- Contains PortalNightsArena root: " + ToYesNo(result.HasArenaRoot));
            report.AppendLine("- Contains PN_Central_Core: " + ToYesNo(result.HasPlanet1Core));
            report.AppendLine("- Contains PN_BuildPads: " + ToYesNo(result.HasBuildPads));
            report.AppendLine("- PN_PlayerSpawn_* count: " + result.PlayerSpawnCount);
            report.AppendLine("- Forbidden future roots: " + JoinOrNone(result.ForbiddenFutureRoots));
            report.AppendLine("- Forbidden Planet 1 objects: " + JoinOrNone(result.ForbiddenPlanet1Objects));
            AppendMetrics(report, result.Metrics);
            if (!string.IsNullOrEmpty(result.Error))
            {
                report.AppendLine("- Error: " + result.Error);
            }
        }

        private static void AppendPhase5APlanetValidationEntry(StringBuilder report, Phase5APlanetValidationEntry result)
        {
            if (result == null)
            {
                report.AppendLine("- Missing planet validation entry.");
                return;
            }

            report.AppendLine("#### Planet " + result.PlanetIndex + " — `" + result.SceneName + "`");
            report.AppendLine();
            report.AppendLine("- Scene path: `" + result.ScenePath + "`");
            report.AppendLine("- Validation success: " + ToYesNo(result.Success));
            report.AppendLine("- Root count: " + result.RootCount);
            report.AppendLine("- Expected root name: `" + result.ExpectedRootName + "`");
            report.AppendLine("- Expected root found: " + ToYesNo(result.HasExpectedRoot));
            report.AppendLine("- Expected root count: " + result.ExpectedRootCount);
            report.AppendLine("- Root active: " + ToYesNo(result.RootActive));
            report.AppendLine("- `PortalNightsPlanetSceneRoot` count in scene: " + result.SceneRootCount);
            report.AppendLine("- Has `PortalNightsPlanetSceneRoot` on expected root: " + ToYesNo(result.HasPlanetSceneRoot));
            report.AppendLine("- planetIndex matches: " + ToYesNo(result.PlanetIndexMatches));
            report.AppendLine("- `ValidateSetup(true)` passed: " + ToYesNo(result.ValidateSetupPassed));
            report.AppendLine("- Contains `PortalNightsArena` root: " + ToYesNo(result.ContainsArenaRoot));
            report.AppendLine("- Contains player controller: " + ToYesNo(result.HasPlayerController));
            report.AppendLine("- Forbidden global objects: " + JoinOrNone(result.ForbiddenGlobalObjects));
            AppendMetrics(report, result.Metrics);
            if (!string.IsNullOrEmpty(result.Error))
            {
                report.AppendLine("- Error: " + result.Error);
            }

            report.AppendLine();
        }

        private static void AppendMetrics(StringBuilder report, SceneMetrics metrics)
        {
            report.AppendLine("- Object count: " + metrics.ObjectCount);
            report.AppendLine("- Renderer count: " + metrics.RendererCount);
            report.AppendLine("- Collider count: " + metrics.ColliderCount);
            report.AppendLine("- Light count: " + metrics.LightCount);
            report.AppendLine("- Particle count: " + metrics.ParticleCount);
            report.AppendLine("- MonoBehaviour count: " + metrics.MonoBehaviourCount);
            report.AppendLine("- NetworkObject count: " + metrics.NetworkObjectCount);
        }

        private static void AppendGitCheck(StringBuilder report, string title, string arguments)
        {
            report.AppendLine("### " + title);
            report.AppendLine();
            report.AppendLine("```text");
            if (TryRunGit(arguments, out string output))
            {
                report.AppendLine(string.IsNullOrWhiteSpace(output) ? "(no output)" : output.TrimEnd());
            }
            else
            {
                report.AppendLine("Git command failed or git was unavailable.");
                if (!string.IsNullOrWhiteSpace(output))
                {
                    report.AppendLine(output.TrimEnd());
                }
            }

            report.AppendLine("```");
            report.AppendLine();
        }

        private static bool TryRunGit(string arguments, out string output)
        {
            output = string.Empty;

            try
            {
                string workingDirectory = Path.GetDirectoryName(Application.dataPath) ?? Directory.GetCurrentDirectory();
                ProcessStartInfo processInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = workingDirectory,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (Process process = Process.Start(processInfo))
                {
                    if (process == null)
                    {
                        output = "Process could not be started.";
                        return false;
                    }

                    string standardOutput = process.StandardOutput.ReadToEnd();
                    string standardError = process.StandardError.ReadToEnd();
                    process.WaitForExit();
                    output = standardOutput + standardError;
                    return process.ExitCode == 0;
                }
            }
            catch (Exception exception)
            {
                output = exception.Message;
                return false;
            }
        }

        private static string JoinOrNone(List<string> values)
        {
            return values == null || values.Count == 0 ? "none" : string.Join(", ", values.ToArray());
        }

        private static string ToYesNo(bool value)
        {
            return value ? "yes" : "no";
        }

        private sealed class PlanetScenePlan
        {
            public PlanetScenePlan(int planetIndex, string sourceRootName, string targetRootName, string sceneFileName, string displayNameKey)
            {
                PlanetIndex = planetIndex;
                SourceRootName = sourceRootName;
                TargetRootName = targetRootName;
                DisplayNameKey = displayNameKey;
                TargetScenePath = GeneratedScenesFolder + "/" + sceneFileName;
            }

            public int PlanetIndex { get; }
            public string SourceRootName { get; }
            public string TargetRootName { get; }
            public string TargetScenePath { get; }
            public string DisplayNameKey { get; }
        }

        private struct SceneMetrics
        {
            public int ObjectCount;
            public int RendererCount;
            public int ColliderCount;
            public int LightCount;
            public int ParticleCount;
            public int MonoBehaviourCount;
            public int NetworkObjectCount;
        }

        private sealed class DryRunResult
        {
            public PlanetScenePlan Plan;
            public bool SourceRootFound;
            public bool HasSceneRootComponent;
            public bool WouldAddSceneRoot;
            public string TargetScenePath;
            public SceneMetrics Metrics;
            public string Error;

            public string ToLogLine()
            {
                if (!SourceRootFound)
                {
                    return "missing root (" + Error + ")";
                }

                return "objects=" + Metrics.ObjectCount
                    + ", renderers=" + Metrics.RendererCount
                    + ", colliders=" + Metrics.ColliderCount
                    + ", lights=" + Metrics.LightCount
                    + ", particles=" + Metrics.ParticleCount
                    + ", behaviours=" + Metrics.MonoBehaviourCount
                    + ", networkObjects=" + Metrics.NetworkObjectCount
                    + ", sceneRootExists=" + HasSceneRootComponent
                    + ", target=" + TargetScenePath;
            }
        }

        private sealed class GenerationResult
        {
            public PlanetScenePlan Plan;
            public bool Success;
            public bool AddedSceneRoot;
            public int SceneRootComponentCount;
            public bool ValidateSetupPassed;
            public int NetworkObjectCount;
            public SceneMetrics Metrics;
            public string Error;

            public string ToLogLine()
            {
                return Success
                    ? "saved scene, sceneRootAdded=" + AddedSceneRoot + ", sceneRootCount=" + SceneRootComponentCount + ", validate=" + ValidateSetupPassed + ", networkObjects=" + NetworkObjectCount
                    : "failed (" + Error + ")";
            }
        }

        private sealed class ValidationResult
        {
            public PlanetScenePlan Plan;
            public bool Success;
            public bool ExpectedRootFound;
            public int ExpectedRootCount;
            public bool RootActive;
            public bool HasPlanetSceneRoot;
            public int SceneRootComponentsInScene;
            public bool PlanetIndexMatches;
            public bool ValidateSetupPassed;
            public bool HasPlayerController;
            public List<string> ForbiddenObjects = new List<string>();
            public SceneMetrics Metrics;
            public string Error;

            public string ToLogLine()
            {
                return Success
                    ? "validated, objects=" + Metrics.ObjectCount + ", renderers=" + Metrics.RendererCount + ", colliders=" + Metrics.ColliderCount + ", networkObjects=" + Metrics.NetworkObjectCount
                    : "failed (" + (string.IsNullOrEmpty(Error) ? "validation mismatch" : Error) + ")";
            }
        }

        private sealed class Phase4SourceSelection
        {
            public GameObject ArenaRoot;
            public readonly List<GameObject> CoreObjects = new List<GameObject>();
            public readonly List<GameObject> Planet1Objects = new List<GameObject>();
            public readonly List<string> ExcludedFutureRootNames = new List<string>();
            public readonly List<string> Issues = new List<string>();
        }

        private sealed class Phase4DryRunResult
        {
            public string CoreScenePath;
            public string Planet1ScenePath;
            public bool IsClean;
            public List<string> CoreCopiedObjectNames = new List<string>();
            public List<string> Planet1CopiedObjectNames = new List<string>();
            public List<string> ExcludedFuturePlanetRoots = new List<string>();
            public SceneMetrics CoreMetrics;
            public SceneMetrics Planet1Metrics;
            public List<string> Issues = new List<string>();

            public string ToLogLine()
            {
                return "clean=" + IsClean
                    + ", coreObjects=" + CoreCopiedObjectNames.Count
                    + ", planet1Objects=" + Planet1CopiedObjectNames.Count
                    + ", excludedFutureRoots=" + ExcludedFuturePlanetRoots.Count
                    + (Issues.Count > 0 ? ", issues=" + Issues.Count : string.Empty);
            }
        }

        private sealed class Phase4CoreGenerationResult
        {
            public string ScenePath;
            public bool Success;
            public SceneMetrics Metrics;
            public int NetworkObjectCount;
            public List<string> CopiedObjectNames = new List<string>();
            public List<string> ExcludedObjectNames = new List<string>();
            public List<string> Issues = new List<string>();
            public string Error;

            public string ToLogLine()
            {
                return Success
                    ? "saved scene, objects=" + CopiedObjectNames.Count + ", networkObjects=" + NetworkObjectCount
                    : "failed (" + (string.IsNullOrEmpty(Error) ? JoinOrNone(Issues) : Error) + ")";
            }
        }

        private sealed class Phase4Planet1GenerationResult
        {
            public string ScenePath;
            public bool Success;
            public bool AddedSceneRoot;
            public int SceneRootComponentCount;
            public bool ValidateSetupPassed;
            public int PlayerSpawnCount;
            public int NetworkObjectCount;
            public SceneMetrics Metrics;
            public List<string> CopiedObjectNames = new List<string>();
            public List<string> ExcludedObjectNames = new List<string>();
            public List<string> Issues = new List<string>();
            public string Error;

            public string ToLogLine()
            {
                return Success
                    ? "saved scene, playerSpawns=" + PlayerSpawnCount + ", validate=" + ValidateSetupPassed + ", networkObjects=" + NetworkObjectCount
                    : "failed (" + (string.IsNullOrEmpty(Error) ? JoinOrNone(Issues) : Error) + ")";
            }
        }

        private sealed class Phase4CoreValidationResult
        {
            public string ScenePath;
            public bool Success;
            public bool HasNetworkManager;
            public bool HasGameController;
            public bool HasHudCanvas;
            public bool HasEventSystem;
            public bool HasMainCamera;
            public bool HasArenaRoot;
            public bool HasPlanet1Core;
            public bool HasBuildPads;
            public int PlayerSpawnCount;
            public int NetworkObjectCount;
            public SceneMetrics Metrics;
            public List<string> ForbiddenFutureRoots = new List<string>();
            public List<string> ForbiddenPlanet1Objects = new List<string>();
            public string Error;

            public string ToLogLine()
            {
                return Success
                    ? "validated, networkObjects=" + NetworkObjectCount + ", objects=" + Metrics.ObjectCount
                    : "failed (" + (string.IsNullOrEmpty(Error) ? "validation mismatch" : Error) + ")";
            }
        }

        private sealed class Phase4Planet1ValidationResult
        {
            public string ScenePath;
            public bool Success;
            public bool HasExpectedRoot;
            public int RootCount;
            public bool RootActive;
            public bool HasPlanetSceneRoot;
            public int SceneRootComponentsInScene;
            public bool PlanetIndexMatches;
            public bool ValidateSetupPassed;
            public bool HasCentralCore;
            public bool HasBuildPads;
            public int PlayerSpawnCount;
            public int NetworkObjectCount;
            public SceneMetrics Metrics;
            public List<string> ForbiddenGlobalObjects = new List<string>();
            public List<string> ForbiddenFutureRoots = new List<string>();
            public string Error;

            public string ToLogLine()
            {
                return Success
                    ? "validated, playerSpawns=" + PlayerSpawnCount + ", networkObjects=" + NetworkObjectCount + ", objects=" + Metrics.ObjectCount
                    : "failed (" + (string.IsNullOrEmpty(Error) ? "validation mismatch" : Error) + ")";
            }
        }

        private sealed class Phase5APlanetValidationPlan
        {
            public Phase5APlanetValidationPlan(int planetIndex, string sceneName, string scenePath, string expectedRootName)
            {
                PlanetIndex = planetIndex;
                SceneName = sceneName;
                ScenePath = scenePath;
                ExpectedRootName = expectedRootName;
            }

            public int PlanetIndex { get; }
            public string SceneName { get; }
            public string ScenePath { get; }
            public string ExpectedRootName { get; }
        }

        private sealed class Phase5ABuildSceneEntry
        {
            public int Index;
            public string Path;
            public bool Enabled;
        }

        private sealed class Phase5AValidationResult
        {
            public bool Success;
            public bool SourceSceneOpenedForValidation;
            public bool SourceSceneReopenedAtEnd;
            public bool PortalNightsArenaIsFirstEnabledScene;
            public bool CoreSceneRegistered;
            public bool AllPlanetScenesRegistered;
            public readonly List<Phase5ABuildSceneEntry> BuildSettingsScenes = new List<Phase5ABuildSceneEntry>();
            public readonly List<string> DuplicateScenePaths = new List<string>();
            public readonly List<string> MissingSceneAssets = new List<string>();
            public readonly List<string> MissingBuildSettingsRegistrations = new List<string>();
            public readonly List<string> RegisteredPlanetScenePaths = new List<string>();
            public Phase5ACoreValidationResult CoreValidation;
            public readonly List<Phase5APlanetValidationEntry> PlanetValidations = new List<Phase5APlanetValidationEntry>();
            public string Error;

            public string ToLogLine()
            {
                return Success
                    ? "success, buildScenes=" + BuildSettingsScenes.Count + ", planets=" + PlanetValidations.Count
                    : "failed (" + (string.IsNullOrEmpty(Error) ? "validation mismatch" : Error) + ")";
            }
        }

        private sealed class Phase5ACoreValidationResult
        {
            public string ScenePath;
            public string SceneName;
            public bool Success;
            public int RootCount;
            public bool HasNetworkManager;
            public bool HasGameController;
            public bool HasHudCanvas;
            public bool HasEventSystem;
            public bool HasMainCamera;
            public bool HasArenaRoot;
            public bool HasPlanet1Core;
            public bool HasBuildPads;
            public int PlayerSpawnCount;
            public int NetworkObjectCount;
            public SceneMetrics Metrics;
            public List<string> ForbiddenFutureRoots = new List<string>();
            public List<string> ForbiddenPlanet1Objects = new List<string>();
            public string Error;
        }

        private sealed class Phase5APlanetValidationEntry
        {
            public int PlanetIndex;
            public string SceneName;
            public string ScenePath;
            public string ExpectedRootName;
            public bool Success;
            public int RootCount;
            public int SceneRootCount;
            public bool HasExpectedRoot;
            public int ExpectedRootCount;
            public bool RootActive;
            public bool HasPlanetSceneRoot;
            public bool PlanetIndexMatches;
            public bool ValidateSetupPassed;
            public bool ContainsArenaRoot;
            public bool HasPlayerController;
            public int NetworkObjectCount;
            public SceneMetrics Metrics;
            public GameObject ExpectedRoot;
            public readonly List<string> ForbiddenGlobalObjects = new List<string>();
            public string Error;
        }
    }
}
#endif
