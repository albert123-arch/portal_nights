using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using PortalNights.Visuals;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace PortalNights.EditorTools
{
    public static class PortalNightsCharacterAnimationImporter
    {
        private const string AutoImportMarkerPath = "Assets/PortalNights/Reports/.character_animation_import_requested";
        private const string ReportPath = "Assets/PortalNights/Reports/CharacterAnimationReport.md";

        private const string MovingParameter = "Moving";
        private const string RunningParameter = "Running";
        private const string MoveSpeedParameter = "MoveSpeed";
        private const string AttackParameter = "Attack";
        private const string HitParameter = "Hit";
        private const string DeathParameter = "Death";
        private const string SpecialParameter = "Special";

        private static readonly AnimationImportSpec[] AnimationSpecs =
        {
            new AnimationImportSpec(
                "GenericIdle",
                @"D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ActionAdventure\idle.fbx",
                "Assets/PortalNights/Art/Animations/Imported/Mixamo/ActionAdventure/idle.fbx",
                "PN_Generic_Idle",
                true,
                false),
            new AnimationImportSpec(
                "GenericWalk",
                @"D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ActionAdventure\walking.fbx",
                "Assets/PortalNights/Art/Animations/Imported/Mixamo/ActionAdventure/walking.fbx",
                "PN_Generic_Walk",
                true,
                false),
            new AnimationImportSpec(
                "GenericRun",
                @"D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ActionAdventure\running.fbx",
                "Assets/PortalNights/Art/Animations/Imported/Mixamo/ActionAdventure/running.fbx",
                "PN_Generic_Run",
                true,
                false),
            new AnimationImportSpec(
                "Hit",
                @"D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ProLongbow\standing react small from front.fbx",
                "Assets/PortalNights/Art/Animations/Imported/Mixamo/Combat/standing react small from front.fbx",
                "PN_Generic_Hit",
                false,
                false),
            new AnimationImportSpec(
                "DeathBackward",
                @"D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ProLongbow\standing death backward 01.fbx",
                "Assets/PortalNights/Art/Animations/Imported/Mixamo/Combat/standing death backward 01.fbx",
                "PN_Generic_Death_Backward",
                false,
                false),
            new AnimationImportSpec(
                "DeathForward",
                @"D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ProLongbow\standing death forward 01.fbx",
                "Assets/PortalNights/Art/Animations/Imported/Mixamo/Combat/standing death forward 01.fbx",
                "PN_Generic_Death_Forward",
                false,
                false),
            new AnimationImportSpec(
                "MeleePunch",
                @"D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ProLongbow\standing melee punch.fbx",
                "Assets/PortalNights/Art/Animations/Imported/Mixamo/Combat/standing melee punch.fbx",
                "PN_Generic_Attack_Punch",
                false,
                false),
            new AnimationImportSpec(
                "MeleeKick",
                @"D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ProLongbow\standing melee kick.fbx",
                "Assets/PortalNights/Art/Animations/Imported/Mixamo/Combat/standing melee kick.fbx",
                "PN_Generic_Attack_Kick",
                false,
                false),
            new AnimationImportSpec(
                "MutantPunch",
                @"D:\www\Neon Fighter\Assets\External\Mixamo\Animations\RoleSpecific\Mutant@Mutant Punch.fbx",
                "Assets/PortalNights/Art/Animations/Imported/Mixamo/Mutant/Mutant@Mutant Punch.fbx",
                "PN_Mutant_Attack_Punch",
                false,
                true),
            new AnimationImportSpec(
                "MutantWalk",
                @"D:\www\Neon Fighter\Assets\External\Mixamo\Animations\RoleSpecific\Mutant@Mutant Walking.fbx",
                "Assets/PortalNights/Art/Animations/Imported/Mixamo/Mutant/Mutant@Mutant Walking.fbx",
                "PN_Mutant_Walk",
                true,
                true),
            new AnimationImportSpec(
                "StaffIdle",
                @"D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ProLongbow\standing idle 01.fbx",
                "Assets/PortalNights/Art/Animations/Imported/Mixamo/Staff/standing idle 01.fbx",
                "PN_Staff_Idle",
                true,
                false),
            new AnimationImportSpec(
                "StaffWalk",
                @"D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ProLongbow\standing walk forward.fbx",
                "Assets/PortalNights/Art/Animations/Imported/Mixamo/Staff/standing walk forward.fbx",
                "PN_Staff_Walk",
                true,
                false),
            new AnimationImportSpec(
                "StaffRun",
                @"D:\www\Neon Fighter\Assets\External\Mixamo\Animations\ProLongbow\standing run forward.fbx",
                "Assets/PortalNights/Art/Animations/Imported/Mixamo/Staff/standing run forward.fbx",
                "PN_Staff_Run",
                true,
                false),
            new AnimationImportSpec(
                "StaffPanicIdle",
                @"D:\www\Neon Fighter\Assets\External\Mixamo\NPCs\Animations\Peasant Girl@Standing Idle.fbx",
                "Assets/PortalNights/Art/Animations/Imported/Mixamo/Staff/Peasant Girl@Standing Idle.fbx",
                "PN_Staff_PanicIdle",
                true,
                false),
            new AnimationImportSpec(
                "StaffPanicArguing",
                @"D:\www\Neon Fighter\Assets\External\Mixamo\NPCs\Animations\Peasant Girl@Standing Arguing.fbx",
                "Assets/PortalNights/Art/Animations/Imported/Mixamo/Staff/Peasant Girl@Standing Arguing.fbx",
                "PN_Staff_Panic_Arguing",
                true,
                false),
        };

        private static readonly VisualPrefabAssignment[] VisualPrefabAssignments =
        {
            new VisualPrefabAssignment("Ch40", "Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Ch40.prefab", "Assets/PortalNights/Art/Characters/Monsters/Ch40/Model/Ch40_nonPBR.fbx", "Assets/PortalNights/Animations/Enemies/PN_Enemy_Humanoid.controller"),
            new VisualPrefabAssignment("Parasite", "Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Parasite.prefab", "Assets/PortalNights/Art/Characters/Monsters/Parasite/Model/Parasite L Starkie.fbx", "Assets/PortalNights/Animations/Enemies/PN_Enemy_Humanoid.controller"),
            new VisualPrefabAssignment("Vampire", "Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Vampire.prefab", "Assets/PortalNights/Art/Characters/Monsters/Vampire/Model/Vampire A Lusth.fbx", "Assets/PortalNights/Animations/Enemies/PN_Enemy_Humanoid.controller"),
            new VisualPrefabAssignment("Ch50", "Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Ch50.prefab", "Assets/PortalNights/Art/Characters/Monsters/Ch50/Model/Ch50_nonPBR.fbx", "Assets/PortalNights/Animations/Enemies/PN_Enemy_Humanoid.controller"),
            new VisualPrefabAssignment("Yaku", "Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Yaku.prefab", "Assets/PortalNights/Art/Characters/Monsters/Yaku/Model/Yaku J Ignite.fbx", "Assets/PortalNights/Animations/Bosses/PN_Boss_Yaku.controller"),
            new VisualPrefabAssignment("Pumpkinhulk", "Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Pumpkinhulk.prefab", "Assets/PortalNights/Art/Characters/Monsters/Pumpkinhulk/Model/Pumpkinhulk L Shaw.fbx", "Assets/PortalNights/Animations/Bosses/PN_Boss_Pumpkinhulk.controller"),
            new VisualPrefabAssignment("Maw", "Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Maw.prefab", "Assets/PortalNights/Art/Characters/Monsters/Maw/Model/Maw J Laygo.fbx", "Assets/PortalNights/Animations/Enemies/PN_Enemy_Beast.controller"),
            new VisualPrefabAssignment("Warrok", "Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Warrok.prefab", "Assets/PortalNights/Art/Characters/Monsters/Warrok/Model/Warrok W Kurniawan (1).fbx", "Assets/PortalNights/Animations/Enemies/PN_Enemy_Beast.controller"),
            new VisualPrefabAssignment("Mutant", "Assets/PortalNights/Prefabs/Characters/Monsters/PN_Visual_Mutant.prefab", "Assets/PortalNights/Art/Characters/Monsters/Mutant/Model/Mutant (1).fbx", string.Empty),
            new VisualPrefabAssignment("Staff_Ch32", "Assets/PortalNights/Prefabs/Characters/Staff/PN_Visual_Staff_Ch32.prefab", "Assets/PortalNights/Art/Characters/Staff/Ch32/Model/Ch32_nonPBR.fbx", "Assets/PortalNights/Animations/Staff/PN_Staff.controller"),
        };

        [MenuItem("Portal Nights/Import Character Animation Prefabs")]
        public static void ImportCharacterAnimationPrefabs()
        {
            EnsureFolders();
            CopyAnimationSources();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            List<AnimationImportReport> animationReports = ImportAnimationAssets();
            Dictionary<string, AnimationClip> clips = LoadImportedClips(animationReports);
            Dictionary<string, AnimatorController> controllers = CreateAnimatorControllers(clips);
            List<VisualAnimationReport> visualReports = AssignControllersToVisualPrefabs(controllers, clips);
            WriteReport(animationReports, visualReports);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Portal Nights character animation import complete. Report: {ReportPath}");
        }

        [MenuItem("Portal Nights/Validate Character Animation Prefabs")]
        public static void ValidateCharacterAnimationPrefabs()
        {
            Dictionary<string, AnimatorController> controllers = new Dictionary<string, AnimatorController>();
            foreach (string path in ControllerPaths)
            {
                AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                if (controller != null)
                {
                    controllers[path] = controller;
                }
            }

            List<VisualAnimationReport> visualReports = AssignControllersToVisualPrefabs(controllers, LoadImportedClips(new List<AnimationImportReport>()), false);
            Debug.Log("Portal Nights character animation validation:\n" + string.Join("\n", visualReports.Select(report => $"{report.Name}: {report.StatusSummary}")));
        }

        public static void ImportCharacterAnimationPrefabsBatch()
        {
            ImportCharacterAnimationPrefabs();
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
                ImportCharacterAnimationPrefabs();
            };
        }

        private static IEnumerable<string> ControllerPaths
        {
            get
            {
                yield return "Assets/PortalNights/Animations/Enemies/PN_Enemy_Humanoid.controller";
                yield return "Assets/PortalNights/Animations/Enemies/PN_Enemy_Beast.controller";
                yield return "Assets/PortalNights/Animations/Staff/PN_Staff.controller";
                yield return "Assets/PortalNights/Animations/Bosses/PN_Boss_Pumpkinhulk.controller";
                yield return "Assets/PortalNights/Animations/Bosses/PN_Boss_Yaku.controller";
            }
        }

        private static void EnsureFolders()
        {
            string[] folders =
            {
                "Assets/PortalNights/Art/Animations/Imported/Mixamo",
                "Assets/PortalNights/Animations/Enemies",
                "Assets/PortalNights/Animations/Staff",
                "Assets/PortalNights/Animations/Bosses",
                "Assets/PortalNights/Reports",
            };

            foreach (string folder in folders)
            {
                Directory.CreateDirectory(ToAbsoluteProjectPath(folder));
            }
        }

        private static void CopyAnimationSources()
        {
            foreach (AnimationImportSpec spec in AnimationSpecs)
            {
                string source = ResolveSourcePath(spec.SourcePath);
                if (string.IsNullOrEmpty(source))
                {
                    continue;
                }

                string target = ToAbsoluteProjectPath(spec.TargetPath);
                string directory = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                bool shouldCopy = !File.Exists(target) || new FileInfo(source).Length != new FileInfo(target).Length;
                if (shouldCopy)
                {
                    File.Copy(source, target, true);
                }
            }
        }

        private static List<AnimationImportReport> ImportAnimationAssets()
        {
            List<AnimationImportReport> reports = new List<AnimationImportReport>();
            foreach (AnimationImportSpec spec in AnimationSpecs)
            {
                AnimationImportReport report = new AnimationImportReport(spec);
                report.ResolvedSourcePath = ResolveSourcePath(spec.SourcePath);
                if (string.IsNullOrEmpty(report.ResolvedSourcePath))
                {
                    report.Warnings.Add("Source animation FBX is missing.");
                    reports.Add(report);
                    continue;
                }

                ModelImporter importer = AssetImporter.GetAtPath(spec.TargetPath) as ModelImporter;
                if (importer == null)
                {
                    report.Warnings.Add("Unity did not create a ModelImporter for the copied animation FBX.");
                    reports.Add(report);
                    continue;
                }

                ConfigureAnimationImporter(importer, spec);
                importer.SaveAndReimport();

                AnimationClip[] clips = LoadClipsAtPath(spec.TargetPath);
                report.ImportedClipNames.AddRange(clips.Select(clip => clip.name));
                report.RootMotionDisabled = true;
                report.RigType = spec.UseGenericRig ? "Generic" : "Humanoid";
                if (clips.Length == 0)
                {
                    report.Warnings.Add("No animation clips were imported.");
                }

                reports.Add(report);
            }

            return reports;
        }

        private static void ConfigureAnimationImporter(ModelImporter importer, AnimationImportSpec spec)
        {
            importer.animationType = spec.UseGenericRig ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.Human;
            importer.avatarSetup = spec.UseGenericRig ? ModelImporterAvatarSetup.NoAvatar : ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importConstraints = false;
            importer.importAnimatedCustomProperties = false;
            importer.resampleCurves = true;
            importer.animationCompression = ModelImporterAnimationCompression.Optimal;

            ModelImporterClipAnimation[] clipAnimations = importer.defaultClipAnimations;
            if (clipAnimations.Length == 0)
            {
                clipAnimations = importer.clipAnimations;
            }

            if (clipAnimations.Length > 0)
            {
                for (int i = 0; i < clipAnimations.Length; i++)
                {
                    ModelImporterClipAnimation clip = clipAnimations[i];
                    clip.name = clipAnimations.Length == 1 ? spec.ClipName : $"{spec.ClipName}_{i + 1}";
                    clip.loopTime = spec.LoopTime;
                    clip.loopPose = spec.LoopTime;
                    clip.lockRootRotation = true;
                    clip.keepOriginalOrientation = false;
                    clip.lockRootHeightY = true;
                    clip.keepOriginalPositionY = false;
                    clip.lockRootPositionXZ = true;
                    clip.keepOriginalPositionXZ = false;
                    clip.mirror = false;
                    clip.wrapMode = spec.LoopTime ? WrapMode.Loop : WrapMode.Default;
                    clipAnimations[i] = clip;
                }

                importer.clipAnimations = clipAnimations;
            }
        }

        private static Dictionary<string, AnimationClip> LoadImportedClips(IReadOnlyCollection<AnimationImportReport> reports)
        {
            Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>(StringComparer.Ordinal);
            foreach (AnimationImportSpec spec in AnimationSpecs)
            {
                AnimationClip clip = LoadClipsAtPath(spec.TargetPath).FirstOrDefault();
                if (clip != null)
                {
                    clips[spec.Key] = clip;
                }
            }

            return clips;
        }

        private static Dictionary<string, AnimatorController> CreateAnimatorControllers(IReadOnlyDictionary<string, AnimationClip> clips)
        {
            Dictionary<string, AnimatorController> controllers = new Dictionary<string, AnimatorController>(StringComparer.Ordinal);
            controllers["Humanoid"] = CreateEnemyHumanoidController(clips);
            controllers["Beast"] = CreateEnemyBeastController(clips);
            controllers["Staff"] = CreateStaffController(clips);
            controllers["BossPumpkinhulk"] = CreateBossPumpkinhulkController(clips);
            controllers["BossYaku"] = CreateBossYakuController(clips);
            return controllers;
        }

        private static AnimatorController CreateEnemyHumanoidController(IReadOnlyDictionary<string, AnimationClip> clips)
        {
            AnimatorController controller = CreateEmptyController("Assets/PortalNights/Animations/Enemies/PN_Enemy_Humanoid.controller");
            Dictionary<string, AnimatorState> states = AddStates(controller, new Dictionary<string, AnimationClip>
            {
                ["Idle"] = GetClip(clips, "GenericIdle"),
                ["Move"] = GetClip(clips, "GenericWalk"),
                ["Run"] = GetClip(clips, "GenericRun"),
                ["Attack"] = GetClip(clips, "MeleePunch"),
                ["Hit"] = GetClip(clips, "Hit"),
                ["Death"] = GetClip(clips, "DeathBackward"),
            });
            ConfigureLocomotionTransitions(controller, states, null, null);
            return controller;
        }

        private static AnimatorController CreateEnemyBeastController(IReadOnlyDictionary<string, AnimationClip> clips)
        {
            AnimatorController controller = CreateEmptyController("Assets/PortalNights/Animations/Enemies/PN_Enemy_Beast.controller");
            Dictionary<string, AnimatorState> states = AddStates(controller, new Dictionary<string, AnimationClip>
            {
                ["Idle"] = GetClip(clips, "GenericIdle"),
                ["Move"] = GetClip(clips, "GenericWalk"),
                ["Run"] = GetClip(clips, "GenericRun"),
                ["Attack"] = GetClip(clips, "MeleeKick"),
                ["Hit"] = GetClip(clips, "Hit"),
                ["Death"] = GetClip(clips, "DeathForward"),
            });
            ConfigureLocomotionTransitions(controller, states, null, null);
            return controller;
        }

        private static AnimatorController CreateStaffController(IReadOnlyDictionary<string, AnimationClip> clips)
        {
            AnimatorController controller = CreateEmptyController("Assets/PortalNights/Animations/Staff/PN_Staff.controller");
            Dictionary<string, AnimatorState> states = AddStates(controller, new Dictionary<string, AnimationClip>
            {
                ["Idle"] = GetClip(clips, "StaffIdle"),
                ["Walk"] = GetClip(clips, "StaffWalk"),
                ["Run"] = GetClip(clips, "StaffRun"),
                ["PanicIdle"] = GetClip(clips, "StaffPanicIdle"),
                ["Downed"] = GetClip(clips, "DeathForward"),
                ["Revive"] = GetClip(clips, "StaffIdle"),
                ["SafeIdle"] = GetClip(clips, "StaffIdle"),
            });
            ConfigureStaffTransitions(controller, states);
            return controller;
        }

        private static void ConfigureStaffTransitions(AnimatorController controller, IReadOnlyDictionary<string, AnimatorState> states)
        {
            AddBoolTransition(states["Idle"], states["Walk"], MovingParameter, true);
            AddBoolTransition(states["Walk"], states["Idle"], MovingParameter, false);
            AddBoolTransition(states["Walk"], states["Run"], RunningParameter, true);
            AddBoolTransition(states["Run"], states["Walk"], RunningParameter, false);
            AddBoolTransition(states["Run"], states["Idle"], MovingParameter, false);
            AddAnyStateTrigger(controller, states["PanicIdle"], SpecialParameter, true);
            AddAnyStateTrigger(controller, states["Downed"], DeathParameter, false);
            AddAnyStateTrigger(controller, states["Revive"], HitParameter, true);
            AddExitTransition(states["Revive"], states["SafeIdle"]);
        }

        private static AnimatorController CreateBossPumpkinhulkController(IReadOnlyDictionary<string, AnimationClip> clips)
        {
            AnimatorController controller = CreateEmptyController("Assets/PortalNights/Animations/Bosses/PN_Boss_Pumpkinhulk.controller");
            Dictionary<string, AnimatorState> states = AddStates(controller, new Dictionary<string, AnimationClip>
            {
                ["Idle"] = GetClip(clips, "GenericIdle"),
                ["Move"] = GetClip(clips, "GenericWalk"),
                ["Attack"] = GetClip(clips, "MeleePunch"),
                ["Slam"] = GetClip(clips, "MeleeKick"),
                ["Hit"] = GetClip(clips, "Hit"),
                ["Death"] = GetClip(clips, "DeathForward"),
            });
            ConfigureBossTransitions(controller, states, "Slam");
            return controller;
        }

        private static AnimatorController CreateBossYakuController(IReadOnlyDictionary<string, AnimationClip> clips)
        {
            AnimatorController controller = CreateEmptyController("Assets/PortalNights/Animations/Bosses/PN_Boss_Yaku.controller");
            Dictionary<string, AnimatorState> states = AddStates(controller, new Dictionary<string, AnimationClip>
            {
                ["Idle"] = GetClip(clips, "GenericIdle"),
                ["Move"] = GetClip(clips, "GenericWalk"),
                ["Cast"] = GetClip(clips, "MeleeKick"),
                ["Attack"] = GetClip(clips, "MeleePunch"),
                ["Hit"] = GetClip(clips, "Hit"),
                ["Death"] = GetClip(clips, "DeathBackward"),
            });
            ConfigureBossTransitions(controller, states, "Cast");
            return controller;
        }

        private static AnimatorController CreateEmptyController(string path)
        {
            Directory.CreateDirectory(ToAbsoluteProjectPath(Path.GetDirectoryName(path)?.Replace('\\', '/') ?? string.Empty));
            AssetDatabase.DeleteAsset(path);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter(MovingParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(RunningParameter, AnimatorControllerParameterType.Bool);
            controller.AddParameter(MoveSpeedParameter, AnimatorControllerParameterType.Float);
            controller.AddParameter(AttackParameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(HitParameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(DeathParameter, AnimatorControllerParameterType.Trigger);
            controller.AddParameter(SpecialParameter, AnimatorControllerParameterType.Trigger);
            return controller;
        }

        private static Dictionary<string, AnimatorState> AddStates(AnimatorController controller, IReadOnlyDictionary<string, AnimationClip> definitions)
        {
            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            Dictionary<string, AnimatorState> states = new Dictionary<string, AnimatorState>(StringComparer.Ordinal);
            int index = 0;
            foreach (KeyValuePair<string, AnimationClip> definition in definitions)
            {
                AnimatorState state = stateMachine.AddState(definition.Key, new Vector3(270f, 80f + index * 60f));
                state.motion = definition.Value;
                state.writeDefaultValues = true;
                states[definition.Key] = state;
                if (index == 0)
                {
                    stateMachine.defaultState = state;
                }

                index++;
            }

            return states;
        }

        private static void ConfigureLocomotionTransitions(AnimatorController controller, IReadOnlyDictionary<string, AnimatorState> states, string moveStateOverride, string runStateOverride)
        {
            string moveName = moveStateOverride ?? "Move";
            string runName = runStateOverride ?? "Run";

            AddBoolTransition(states["Idle"], states[moveName], MovingParameter, true);
            AddBoolTransition(states[moveName], states["Idle"], MovingParameter, false);
            if (states.ContainsKey(runName))
            {
                AddBoolTransition(states[moveName], states[runName], RunningParameter, true);
                AddBoolTransition(states[runName], states[moveName], RunningParameter, false);
                AddBoolTransition(states[runName], states["Idle"], MovingParameter, false);
            }

            AddAnyStateTrigger(controller, states["Attack"], AttackParameter, true);
            AddAnyStateTrigger(controller, states["Hit"], HitParameter, true);
            AddAnyStateTrigger(controller, states["Death"], DeathParameter, false);
            AddExitTransition(states["Attack"], states["Idle"]);
            AddExitTransition(states["Hit"], states["Idle"]);
        }

        private static void ConfigureBossTransitions(AnimatorController controller, IReadOnlyDictionary<string, AnimatorState> states, string specialStateName)
        {
            AddBoolTransition(states["Idle"], states["Move"], MovingParameter, true);
            AddBoolTransition(states["Move"], states["Idle"], MovingParameter, false);
            AddAnyStateTrigger(controller, states["Attack"], AttackParameter, true);
            AddAnyStateTrigger(controller, states[specialStateName], SpecialParameter, true);
            AddAnyStateTrigger(controller, states["Hit"], HitParameter, true);
            AddAnyStateTrigger(controller, states["Death"], DeathParameter, false);
            AddExitTransition(states["Attack"], states["Idle"]);
            AddExitTransition(states[specialStateName], states["Idle"]);
            AddExitTransition(states["Hit"], states["Idle"]);
        }

        private static void AddBoolTransition(AnimatorState from, AnimatorState to, string parameterName, bool value)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.12f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(value ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0f, parameterName);
        }

        private static void AddAnyStateTrigger(AnimatorController controller, AnimatorState to, string parameterName, bool returnsToIdle)
        {
            AnimatorStateTransition transition = controller.layers[0].stateMachine.AddAnyStateTransition(to);
            transition.hasExitTime = false;
            transition.duration = 0.08f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, parameterName);

            if (returnsToIdle)
            {
                AddExitTransition(to, controller.layers[0].stateMachine.defaultState);
            }
        }

        private static void AddExitTransition(AnimatorState from, AnimatorState to)
        {
            AnimatorStateTransition transition = from.AddTransition(to);
            transition.hasExitTime = true;
            transition.exitTime = 0.88f;
            transition.duration = 0.12f;
            transition.canTransitionToSelf = false;
        }

        private static List<VisualAnimationReport> AssignControllersToVisualPrefabs(IReadOnlyDictionary<string, AnimatorController> controllers, IReadOnlyDictionary<string, AnimationClip> clips, bool saveChanges = true)
        {
            List<VisualAnimationReport> reports = new List<VisualAnimationReport>();
            foreach (VisualPrefabAssignment assignment in VisualPrefabAssignments)
            {
                VisualAnimationReport report = new VisualAnimationReport(assignment);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assignment.PrefabPath);
                if (prefab == null)
                {
                    report.Warnings.Add("Visual prefab is missing.");
                    reports.Add(report);
                    continue;
                }

                report.AvatarValid = IsAvatarValid(assignment.ModelPath);
                report.AssignedControllerPath = assignment.ControllerPath;

                GameObject contents = PrefabUtility.LoadPrefabContents(assignment.PrefabPath);
                try
                {
                    Animator animator = contents.GetComponentInChildren<Animator>(true);
                    if (animator == null)
                    {
                        GameObject target = contents.transform.childCount > 0 ? contents.transform.GetChild(0).gameObject : contents;
                        animator = target.AddComponent<Animator>();
                        report.Warnings.Add("Animator component was missing and was added to the visual model root.");
                    }

                    animator.applyRootMotion = false;
                    report.RootMotionDisabled = !animator.applyRootMotion;

                    if (string.IsNullOrEmpty(assignment.ControllerPath))
                    {
                        animator.runtimeAnimatorController = null;
                        report.AssignedControllerPath = "none";
                        report.Warnings.Add("No controller assigned. Mutant Humanoid Avatar is invalid, so broken retargeting was intentionally avoided.");
                    }
                    else if (!report.AvatarValid)
                    {
                        animator.runtimeAnimatorController = null;
                        report.AssignedControllerPath = "none";
                        report.Warnings.Add("No controller assigned because the model Avatar is invalid.");
                    }
                    else
                    {
                        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(assignment.ControllerPath);
                        if (controller == null && controllers.TryGetValue(ControllerKeyForPath(assignment.ControllerPath), out AnimatorController createdController))
                        {
                            controller = createdController;
                        }

                        if (controller == null)
                        {
                            report.Warnings.Add("Requested AnimatorController is missing.");
                        }
                        else
                        {
                            animator.runtimeAnimatorController = controller;
                            report.ControllerParameters.AddRange(controller.parameters.Select(parameter => $"{parameter.type} {parameter.name}"));
                            report.ControllerStates.AddRange(controller.layers[0].stateMachine.states.Select(state => state.state.name));
                            report.ClipNames.AddRange(controller.layers[0].stateMachine.states
                                .Select(state => state.state.motion)
                                .OfType<AnimationClip>()
                                .Select(clip => clip.name)
                                .Distinct());
                            report.AssetPreviewStatus = report.ClipNames.Count > 0
                                ? "Asset-level OK: controller states have clips; no scene preview was created."
                                : "Controller assigned, but one or more states have no clip.";
                        }
                    }

                    PortalNightsCharacterVisualAnimator helper = contents.GetComponent<PortalNightsCharacterVisualAnimator>();
                    if (helper == null)
                    {
                        helper = contents.AddComponent<PortalNightsCharacterVisualAnimator>();
                    }

                    helper.SetAnimator(animator);
                    report.ForbiddenComponents.AddRange(FindForbiddenComponents(contents));
                    if (report.ForbiddenComponents.Count > 0)
                    {
                        report.Warnings.Add("Forbidden components found: " + string.Join(", ", report.ForbiddenComponents));
                    }

                    if (saveChanges)
                    {
                        PrefabUtility.SaveAsPrefabAsset(contents, assignment.PrefabPath);
                    }
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(contents);
                }

                reports.Add(report);
            }

            return reports;
        }

        private static List<string> FindForbiddenComponents(GameObject root)
        {
            List<string> forbidden = new List<string>();
            foreach (Component component in root.GetComponentsInChildren<Component>(true))
            {
                if (component == null)
                {
                    continue;
                }

                Type type = component.GetType();
                bool allowedVisualHelper = component is PortalNightsCharacterVisualAnimator;
                if (component is Camera ||
                    component is AudioListener ||
                    component is Canvas ||
                    component is Collider ||
                    component is Rigidbody ||
                    type.FullName == "Unity.Netcode.NetworkObject" ||
                    component is Light ||
                    component is MonoBehaviour && !allowedVisualHelper)
                {
                    forbidden.Add(type.Name);
                }
            }

            return forbidden.Distinct().OrderBy(value => value, StringComparer.Ordinal).ToList();
        }

        private static AnimationClip[] LoadClipsAtPath(string assetPath)
        {
            return AssetDatabase.LoadAllAssetsAtPath(assetPath)
                .OfType<AnimationClip>()
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.Ordinal))
                .ToArray();
        }

        private static AnimationClip GetClip(IReadOnlyDictionary<string, AnimationClip> clips, string key)
        {
            return clips.TryGetValue(key, out AnimationClip clip) ? clip : null;
        }

        private static bool IsAvatarValid(string modelPath)
        {
            Avatar avatar = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<Avatar>().FirstOrDefault();
            return avatar != null && avatar.isValid && avatar.isHuman;
        }

        private static string ControllerKeyForPath(string path)
        {
            if (path.Contains("PN_Enemy_Humanoid", StringComparison.Ordinal))
            {
                return "Humanoid";
            }

            if (path.Contains("PN_Enemy_Beast", StringComparison.Ordinal))
            {
                return "Beast";
            }

            if (path.Contains("PN_Staff", StringComparison.Ordinal))
            {
                return "Staff";
            }

            if (path.Contains("PN_Boss_Pumpkinhulk", StringComparison.Ordinal))
            {
                return "BossPumpkinhulk";
            }

            if (path.Contains("PN_Boss_Yaku", StringComparison.Ordinal))
            {
                return "BossYaku";
            }

            return string.Empty;
        }

        private static string ResolveSourcePath(string preferredPath)
        {
            if (File.Exists(preferredPath))
            {
                return preferredPath;
            }

            string fileName = Path.GetFileName(preferredPath);
            string[] searchRoots =
            {
                @"D:\www\Neon Fighter\Assets\External\Mixamo\Animations",
                @"D:\www\Neon Fighter\Assets\External\Mixamo\NPCs\Animations",
                @"C:\Users\tash3\Downloads",
            };

            foreach (string root in searchRoots)
            {
                if (!Directory.Exists(root))
                {
                    continue;
                }

                string match = Directory.GetFiles(root, fileName, SearchOption.AllDirectories).FirstOrDefault();
                if (!string.IsNullOrEmpty(match))
                {
                    return match;
                }
            }

            return string.Empty;
        }

        private static void WriteReport(IReadOnlyList<AnimationImportReport> animationReports, IReadOnlyList<VisualAnimationReport> visualReports)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# Portal Nights Character Animation Report");
            builder.AppendLine();
            builder.AppendLine("Scope: visual/animation setup only. No scene, spawning, map, movement, shooting, staff rescue, or lazy activation changes.");
            builder.AppendLine();
            builder.AppendLine("## Imported Animation FBX");
            builder.AppendLine();
            builder.AppendLine("| Key | Source | Target | Rig | Clips | Root motion disabled | Warnings |");
            builder.AppendLine("|---|---|---|---|---|---:|---|");
            foreach (AnimationImportReport report in animationReports)
            {
                builder.Append("| ");
                builder.Append(Escape(report.Spec.Key));
                builder.Append(" | ");
                builder.Append(Escape(report.ResolvedSourcePath));
                builder.Append(" | ");
                builder.Append(Escape(report.Spec.TargetPath));
                builder.Append(" | ");
                builder.Append(Escape(report.RigType));
                builder.Append(" | ");
                builder.Append(Escape(report.ImportedClipNames.Count == 0 ? "none" : string.Join(", ", report.ImportedClipNames)));
                builder.Append(" | ");
                builder.Append(report.RootMotionDisabled ? "yes" : "no");
                builder.Append(" | ");
                builder.Append(Escape(report.Warnings.Count == 0 ? "none" : string.Join("; ", report.Warnings.Distinct())));
                builder.AppendLine(" |");
            }

            builder.AppendLine();
            builder.AppendLine("## Visual Prefab Assignments");
            builder.AppendLine();
            builder.AppendLine("| Prefab | Avatar valid | AnimatorController | Parameters | Clips | Root motion disabled | Preview status | Warnings |");
            builder.AppendLine("|---|---:|---|---|---|---:|---|---|");
            foreach (VisualAnimationReport report in visualReports)
            {
                builder.Append("| ");
                builder.Append(Escape(report.Assignment.PrefabPath));
                builder.Append(" | ");
                builder.Append(report.AvatarValid ? "yes" : "no");
                builder.Append(" | ");
                builder.Append(Escape(report.AssignedControllerPath));
                builder.Append(" | ");
                builder.Append(Escape(report.ControllerParameters.Count == 0 ? "none" : string.Join(", ", report.ControllerParameters)));
                builder.Append(" | ");
                builder.Append(Escape(report.ClipNames.Count == 0 ? "none" : string.Join(", ", report.ClipNames)));
                builder.Append(" | ");
                builder.Append(report.RootMotionDisabled ? "yes" : "no");
                builder.Append(" | ");
                builder.Append(Escape(report.AssetPreviewStatus));
                builder.Append(" | ");
                builder.Append(Escape(report.Warnings.Count == 0 ? "none" : string.Join("; ", report.Warnings.Distinct())));
                builder.AppendLine(" |");
            }

            builder.AppendLine();
            builder.AppendLine("## Controller States");
            builder.AppendLine();
            foreach (VisualAnimationReport report in visualReports.Where(report => report.ControllerStates.Count > 0))
            {
                builder.AppendLine($"- `{report.AssignedControllerPath}` for `{report.Name}`: {string.Join(", ", report.ControllerStates)}");
            }

            builder.AppendLine();
            builder.AppendLine("## Notes");
            builder.AppendLine();
            builder.AppendLine("- `Mutant` intentionally has no Humanoid controller because its imported Avatar is invalid (`LeftHand` missing according to Unity).");
            builder.AppendLine("- Preview status is asset-level validation only; no models were placed into `PortalNightsArena`.");

            Directory.CreateDirectory(ToAbsoluteProjectPath(Path.GetDirectoryName(ReportPath)?.Replace('\\', '/') ?? string.Empty));
            File.WriteAllText(ToAbsoluteProjectPath(ReportPath), builder.ToString(), Encoding.UTF8);
        }

        private static string Escape(string value)
        {
            return (value ?? string.Empty).Replace("|", "\\|");
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

        private sealed class AnimationImportSpec
        {
            public AnimationImportSpec(string key, string sourcePath, string targetPath, string clipName, bool loopTime, bool useGenericRig)
            {
                Key = key;
                SourcePath = sourcePath;
                TargetPath = targetPath;
                ClipName = clipName;
                LoopTime = loopTime;
                UseGenericRig = useGenericRig;
            }

            public string Key { get; }
            public string SourcePath { get; }
            public string TargetPath { get; }
            public string ClipName { get; }
            public bool LoopTime { get; }
            public bool UseGenericRig { get; }
        }

        private sealed class AnimationImportReport
        {
            public AnimationImportReport(AnimationImportSpec spec)
            {
                Spec = spec;
            }

            public AnimationImportSpec Spec { get; }
            public string ResolvedSourcePath { get; set; } = string.Empty;
            public string RigType { get; set; } = string.Empty;
            public bool RootMotionDisabled { get; set; }
            public List<string> ImportedClipNames { get; } = new List<string>();
            public List<string> Warnings { get; } = new List<string>();
        }

        private sealed class VisualPrefabAssignment
        {
            public VisualPrefabAssignment(string name, string prefabPath, string modelPath, string controllerPath)
            {
                Name = name;
                PrefabPath = prefabPath;
                ModelPath = modelPath;
                ControllerPath = controllerPath;
            }

            public string Name { get; }
            public string PrefabPath { get; }
            public string ModelPath { get; }
            public string ControllerPath { get; }
        }

        private sealed class VisualAnimationReport
        {
            public VisualAnimationReport(VisualPrefabAssignment assignment)
            {
                Assignment = assignment;
            }

            public VisualPrefabAssignment Assignment { get; }
            public string Name => Assignment.Name;
            public bool AvatarValid { get; set; }
            public string AssignedControllerPath { get; set; } = string.Empty;
            public bool RootMotionDisabled { get; set; }
            public string AssetPreviewStatus { get; set; } = "Not validated";
            public List<string> ControllerParameters { get; } = new List<string>();
            public List<string> ControllerStates { get; } = new List<string>();
            public List<string> ClipNames { get; } = new List<string>();
            public List<string> ForbiddenComponents { get; } = new List<string>();
            public List<string> Warnings { get; } = new List<string>();
            public string StatusSummary => Warnings.Count == 0 ? "OK" : string.Join("; ", Warnings);
        }
    }
}
