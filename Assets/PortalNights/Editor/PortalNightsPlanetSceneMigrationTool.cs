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
        private const string ReportPath = "Assets/PortalNights/Reports/SceneMigrationPhase3Report.md";

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

        private static List<DryRunResult> lastDryRunResults = new List<DryRunResult>();
        private static List<GenerationResult> lastGenerationResults = new List<GenerationResult>();
        private static List<ValidationResult> lastValidationResults = new List<ValidationResult>();

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

        private static void EnsureOutputFolders()
        {
            Directory.CreateDirectory(GeneratedScenesFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "Assets/PortalNights/Reports");
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

            File.WriteAllText(ReportPath, report.ToString(), Encoding.UTF8);
            AssetDatabase.ImportAsset(ReportPath);
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
    }
}
#endif
