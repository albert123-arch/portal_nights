using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PortalNights.EditorTools
{
    public static class PortalNightsPlayerPrefabUpdater
    {
        private const string PlayerPrefabPath = "Assets/PortalNights/Prefabs/PN_PlayerHero.prefab";
        private const string SwatModelPath = "Assets/PortalNights/Models/Characters/Swat.fbx";
        private const string AssaultRiflePrefabPath = "Assets/Shooter/Art/Weapons/AssaultRifle/Pfb_assaultRifle.prefab";
        private const string ShooterAnimatorPath = "Assets/Shooter/Art/Animator/ShooterAnimator.controller";
        private const string ArenaScenePath = "Assets/PortalNights/Scenes/PortalNightsArena.unity";
        private const string ReportPath = "Logs/PortalNightsPlayerVisualValidation.txt";
        private const string AutoRunRequestPath = "Temp/PortalNightsPlayerVisualRebuild.request";
        private const string PreviewScreenshotPath = "Logs/PortalNights_Player_SWAT_Preview.png";

        private const string VisualRootName = "VisualRoot";
        private const string SwatVisualName = "SWAT_Character";
        private const string WeaponSocketName = "RightHand_WeaponSocket";
        private const string RifleName = "AssaultRifle";
        private const float ColliderHeightCoverage = 0.93f;

        [InitializeOnLoadMethod]
        private static void RunRequestedRebuildAfterScriptReload()
        {
            if (!File.Exists(AutoRunRequestPath))
            {
                return;
            }

            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(AutoRunRequestPath))
                {
                    return;
                }

                File.Delete(AutoRunRequestPath);
                RebuildPlayerHeroWithSwatVisual();
            };
        }

        [MenuItem("Portal Nights/Rebuild Player Hero SWAT Visual")]
        public static GameObject RebuildPlayerHeroWithSwatVisual()
        {
            EnsureSwatImporterSettings();

            GameObject playerRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                EnsureRootComponents(playerRoot);

                Transform visualRoot = EnsureChild(playerRoot.transform, VisualRootName);
                ClearChildren(visualRoot);

                CharacterController characterController = playerRoot.GetComponent<CharacterController>();
                RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ShooterAnimatorPath);
                if (controller == null)
                {
                    throw new InvalidOperationException("ShooterAnimator.controller was not found at " + ShooterAnimatorPath);
                }

                GameObject swatVisual = CreateSwatVisual(visualRoot, controller, playerRoot.layer);
                FitVisualToCollider(swatVisual, playerRoot.transform, characterController);
                AttachAssaultRifle(swatVisual, playerRoot.layer);

                Transform cameraAnchor = EnsureChild(playerRoot.transform, "CameraAnchor");
                cameraAnchor.localPosition = new Vector3(0f, 1.55f, 0.35f);
                cameraAnchor.localRotation = Quaternion.identity;
                cameraAnchor.localScale = Vector3.one;

                PortalNightsPlayerController player = playerRoot.GetComponent<PortalNightsPlayerController>();
                Animator animator = swatVisual.GetComponentInChildren<Animator>(true);
                SetSerializedObject(player, "cameraAnchor", cameraAnchor);
                SetSerializedObject(player, "visualRoot", visualRoot);
                SetSerializedObject(player, "animator", animator);
                SetSerializedFloat(player, "visualYawOffset", 0f);

                string report = BuildValidationReport(playerRoot, swatVisual);
                WriteProjectText(ReportPath, report);

                PrefabUtility.SaveAsPrefabAsset(playerRoot, PlayerPrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log("[PortalNights] Rebuilt PN_PlayerHero with SWAT visual and assault rifle.\n" + report);
                return AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(playerRoot);
            }
        }

        [MenuItem("Portal Nights/Validate Player Hero SWAT Visual")]
        public static void ValidatePlayerHeroSwatVisual()
        {
            GameObject playerRoot = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Transform visualRoot = playerRoot.transform.Find(VisualRootName);
                GameObject swatVisual = visualRoot == null ? null : visualRoot.Find(SwatVisualName)?.gameObject;
                if (swatVisual == null)
                {
                    throw new InvalidOperationException("PN_PlayerHero does not contain VisualRoot/SWAT_Character.");
                }

                string report = BuildValidationReport(playerRoot, swatVisual);
                WriteProjectText(ReportPath, report);
                Debug.Log("[PortalNights] Player visual validation report:\n" + report);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(playerRoot);
            }
        }

        [MenuItem("Portal Nights/Capture Player Hero SWAT Preview")]
        public static void CapturePlayerHeroSwatPreview()
        {
            EditorSceneManager.OpenScene(ArenaScenePath, OpenSceneMode.Single);

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (playerPrefab == null)
            {
                throw new InvalidOperationException("Player prefab was not found at " + PlayerPrefabPath);
            }

            GameObject previewPlayer = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            if (previewPlayer == null)
            {
                throw new InvalidOperationException("Could not instantiate PN_PlayerHero for preview capture.");
            }

            previewPlayer.name = "PN_PlayerHero_SWAT_Preview";
            Transform core = GameObject.Find("PN_Central_Core")?.transform;
            Transform buildPad = GameObject.Find("CoreUtility_BuildPad_Right")?.transform ??
                GameObject.Find("RightLane_BuildPad_01")?.transform;

            Vector3 corePosition = core == null ? new Vector3(0f, 1.2f, 0f) : core.position;
            Vector3 previewPosition = buildPad == null ? new Vector3(2.1f, 1.12f, -5.2f) : buildPad.position + new Vector3(-1.35f, 0f, -0.95f);
            previewPosition.y = 1.12f;
            Vector3 lookDirection = corePosition - previewPosition;
            lookDirection.y = 0f;
            previewPlayer.transform.SetPositionAndRotation(
                previewPosition,
                lookDirection.sqrMagnitude > 0.01f ? Quaternion.LookRotation(lookDirection.normalized, Vector3.up) : Quaternion.identity);
            PoseAnimatorForRiflePreview(previewPlayer);

            GameObject cameraObject = new GameObject("PN_SWAT_PreviewCamera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.fieldOfView = 43f;
            camera.nearClipPlane = 0.03f;
            camera.farClipPlane = 200f;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.transform.position = previewPosition + new Vector3(4.2f, 2.35f, -4.6f);
            Vector3 focus = Vector3.Lerp(previewPosition + Vector3.up * 1.05f, corePosition + Vector3.up * 0.85f, 0.35f);
            camera.transform.rotation = Quaternion.LookRotation(focus - camera.transform.position, Vector3.up);

            RenderTexture renderTexture = new RenderTexture(1600, 900, 24, RenderTextureFormat.ARGB32);
            Texture2D screenshot = new Texture2D(1600, 900, TextureFormat.RGB24, false);
            RenderTexture previousActive = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            try
            {
                camera.targetTexture = renderTexture;
                RenderTexture.active = renderTexture;
                camera.Render();
                screenshot.ReadPixels(new Rect(0, 0, 1600, 900), 0, 0);
                screenshot.Apply();
                string absoluteScreenshotPath = ProjectPath(PreviewScreenshotPath);
                Directory.CreateDirectory(Path.GetDirectoryName(absoluteScreenshotPath));
                File.WriteAllBytes(absoluteScreenshotPath, screenshot.EncodeToPNG());
                Debug.Log("[PortalNights] Captured SWAT player preview screenshot: " + absoluteScreenshotPath);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                UnityEngine.Object.DestroyImmediate(screenshot);
                UnityEngine.Object.DestroyImmediate(renderTexture);
                UnityEngine.Object.DestroyImmediate(cameraObject);
                UnityEngine.Object.DestroyImmediate(previewPlayer);
            }
        }

        private static void PoseAnimatorForRiflePreview(GameObject previewPlayer)
        {
            Animator animator = previewPlayer.GetComponentInChildren<Animator>(true);
            if (animator == null || animator.runtimeAnimatorController == null)
            {
                return;
            }

            animator.Rebind();
            animator.Update(0f);
            SetAnimatorBool(animator, "Grounded", true);
            SetAnimatorBool(animator, "FreeFall", false);
            SetAnimatorBool(animator, "IsAiming", true);
            SetAnimatorBool(animator, "IsReloading", false);
            SetAnimatorBool(animator, "IsSwitchingWeapon", false);
            SetAnimatorBool(animator, "IsColliding", false);
            SetAnimatorInt(animator, "WeaponTypeID", 0);
            SetAnimatorFloat(animator, "Speed", 0f);
            SetAnimatorFloat(animator, "MotionSpeed", 1f);
            SetAnimatorFloat(animator, "ReloadSpeed", 1f);
            SetAnimatorFloat(animator, "StrafeX", 0f);
            SetAnimatorFloat(animator, "StrafeY", 0f);
            animator.Update(0.35f);
        }

        private static void SetAnimatorBool(Animator animator, string parameterName, bool value)
        {
            if (HasAnimatorParameter(animator, parameterName, AnimatorControllerParameterType.Bool))
            {
                animator.SetBool(parameterName, value);
            }
        }

        private static void SetAnimatorInt(Animator animator, string parameterName, int value)
        {
            if (HasAnimatorParameter(animator, parameterName, AnimatorControllerParameterType.Int))
            {
                animator.SetInteger(parameterName, value);
            }
        }

        private static void SetAnimatorFloat(Animator animator, string parameterName, float value)
        {
            if (HasAnimatorParameter(animator, parameterName, AnimatorControllerParameterType.Float))
            {
                animator.SetFloat(parameterName, value);
            }
        }

        private static bool HasAnimatorParameter(Animator animator, string parameterName, AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
            {
                if (parameter.name == parameterName && parameter.type == type)
                {
                    return true;
                }
            }

            return false;
        }

        [MenuItem("Portal Nights/Update Player Hero From Shooter Visual")]
        public static GameObject UpdatePlayerHeroFromShooterVisual()
        {
            return RebuildPlayerHeroWithSwatVisual();
        }

        private static void EnsureSwatImporterSettings()
        {
            ModelImporter importer = AssetImporter.GetAtPath(SwatModelPath) as ModelImporter;
            if (importer == null)
            {
                throw new InvalidOperationException("SWAT model was not found at " + SwatModelPath);
            }

            bool changed = false;
            if (importer.animationType != ModelImporterAnimationType.Human)
            {
                importer.animationType = ModelImporterAnimationType.Human;
                changed = true;
            }

            if (importer.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
            {
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                changed = true;
            }

            if (importer.importAnimation)
            {
                importer.importAnimation = false;
                changed = true;
            }

            if (importer.importCameras)
            {
                importer.importCameras = false;
                changed = true;
            }

            if (importer.importLights)
            {
                importer.importLights = false;
                changed = true;
            }

            if (changed)
            {
                importer.SaveAndReimport();
            }
        }

        private static void EnsureRootComponents(GameObject playerRoot)
        {
            if (!playerRoot.TryGetComponent(out NetworkObject _))
            {
                playerRoot.AddComponent<NetworkObject>();
            }

            if (!playerRoot.TryGetComponent(out NetworkTransform _))
            {
                playerRoot.AddComponent<NetworkTransform>();
            }

            CharacterController controller = playerRoot.GetComponent<CharacterController>();
            if (controller == null)
            {
                controller = playerRoot.AddComponent<CharacterController>();
            }

            controller.radius = 0.42f;
            controller.height = 1.85f;
            controller.center = new Vector3(0f, 0.95f, 0f);

            PortalNightsHealth health = playerRoot.GetComponent<PortalNightsHealth>();
            if (health == null)
            {
                health = playerRoot.AddComponent<PortalNightsHealth>();
            }

            health.SetBaseMaxHealth(250f);

            if (!playerRoot.TryGetComponent(out PortalNightsPlayerController _))
            {
                playerRoot.AddComponent<PortalNightsPlayerController>();
            }
        }

        private static GameObject CreateSwatVisual(Transform visualRoot, RuntimeAnimatorController controller, int layer)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SwatModelPath);
            if (source == null)
            {
                throw new InvalidOperationException("SWAT model was not found at " + SwatModelPath);
            }

            GameObject visual = PrefabUtility.InstantiatePrefab(source, visualRoot) as GameObject;
            if (visual == null)
            {
                throw new InvalidOperationException("Could not instantiate SWAT model prefab.");
            }

            visual.name = SwatVisualName;
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            PrefabUtility.UnpackPrefabInstance(visual, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            StripNonVisualComponents(visual);
            SetLayerRecursively(visual.transform, layer);
            SetSkinnedRenderersAlwaysVisible(visual);

            Animator animator = visual.GetComponentInChildren<Animator>(true);
            if (animator == null)
            {
                animator = visual.AddComponent<Animator>();
            }

            if (animator.avatar == null)
            {
                animator.avatar = AssetDatabase.LoadAllAssetsAtPath(SwatModelPath).OfType<Avatar>().FirstOrDefault();
            }

            if (animator.avatar == null || !animator.avatar.isValid || !animator.avatar.isHuman)
            {
                throw new InvalidOperationException("Swat.fbx does not have a valid Humanoid Avatar.");
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            if (visual.GetComponent<PortalNightsAnimationEventSink>() == null)
            {
                visual.AddComponent<PortalNightsAnimationEventSink>();
            }

            ValidateSwatMaterials(visual);
            return visual;
        }

        private static void FitVisualToCollider(GameObject visual, Transform playerRoot, CharacterController controller)
        {
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;

            if (!TryGetRendererBounds(visual, null, out Bounds bounds))
            {
                throw new InvalidOperationException("SWAT visual has no renderers to measure.");
            }

            float targetHeight = controller.height * ColliderHeightCoverage;
            float scale = targetHeight / Mathf.Max(0.001f, bounds.size.y);
            visual.transform.localScale = Vector3.one * scale;

            if (!TryGetRendererBounds(visual, null, out bounds))
            {
                throw new InvalidOperationException("SWAT visual bounds could not be measured after scaling.");
            }

            float colliderBottom = playerRoot.position.y + controller.center.y - controller.height * 0.5f;
            Vector3 offset = new Vector3(
                playerRoot.position.x - bounds.center.x,
                colliderBottom - bounds.min.y,
                playerRoot.position.z - bounds.center.z);
            visual.transform.position += offset;
        }

        private static void AttachAssaultRifle(GameObject swatVisual, int layer)
        {
            Animator animator = swatVisual.GetComponentInChildren<Animator>(true);
            Transform rightHand = animator == null ? null : animator.GetBoneTransform(HumanBodyBones.RightHand);
            if (rightHand == null)
            {
                throw new InvalidOperationException("SWAT humanoid RightHand bone was not found.");
            }

            Transform existingSocket = rightHand.Find(WeaponSocketName);
            if (existingSocket != null)
            {
                UnityEngine.Object.DestroyImmediate(existingSocket.gameObject);
            }

            Transform socket = new GameObject(WeaponSocketName).transform;
            socket.SetParent(rightHand, false);
            socket.localPosition = new Vector3(0.02f, 0.025f, 0.02f);
            socket.localRotation = Quaternion.Euler(0f, 90f, -90f);
            socket.localScale = Vector3.one;

            GameObject rifleSource = AssetDatabase.LoadAssetAtPath<GameObject>(AssaultRiflePrefabPath);
            if (rifleSource == null)
            {
                throw new InvalidOperationException("Assault rifle prefab was not found at " + AssaultRiflePrefabPath);
            }

            GameObject rifle = PrefabUtility.InstantiatePrefab(rifleSource, socket) as GameObject;
            if (rifle == null)
            {
                throw new InvalidOperationException("Could not instantiate assault rifle prefab.");
            }

            rifle.name = RifleName;
            rifle.transform.localPosition = Vector3.zero;
            rifle.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            rifle.transform.localScale = Vector3.one;
            PrefabUtility.UnpackPrefabInstance(rifle, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            StripWeaponGameplayComponents(rifle);
            SetLayerRecursively(rifle.transform, layer);
            EnsureForegripMarker(rifle.transform);
        }

        private static void EnsureForegripMarker(Transform rifle)
        {
            if (rifle.Find("LeftHand_ForegripTarget") != null)
            {
                return;
            }

            Transform marker = new GameObject("LeftHand_ForegripTarget").transform;
            marker.SetParent(rifle, false);
            marker.localPosition = new Vector3(0.22f, 0.12f, 0f);
            marker.localRotation = Quaternion.identity;
            marker.localScale = Vector3.one;
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                child = new GameObject(name).transform;
                child.SetParent(parent, false);
            }

            child.localPosition = Vector3.zero;
            child.localRotation = Quaternion.identity;
            child.localScale = Vector3.one;
            return child;
        }

        private static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(root.GetChild(i).gameObject);
            }
        }

        private static void StripNonVisualComponents(GameObject root)
        {
            foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
            {
                UnityEngine.Object.DestroyImmediate(camera.gameObject);
            }

            foreach (AudioListener listener in root.GetComponentsInChildren<AudioListener>(true))
            {
                UnityEngine.Object.DestroyImmediate(listener);
            }

            foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
            {
                UnityEngine.Object.DestroyImmediate(canvas.gameObject);
            }

            foreach (AudioSource audioSource in root.GetComponentsInChildren<AudioSource>(true))
            {
                UnityEngine.Object.DestroyImmediate(audioSource);
            }

            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            foreach (Rigidbody rigidbody in root.GetComponentsInChildren<Rigidbody>(true))
            {
                UnityEngine.Object.DestroyImmediate(rigidbody);
            }
        }

        private static void StripWeaponGameplayComponents(GameObject root)
        {
            foreach (MonoBehaviour behaviour in root.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour != null)
                {
                    UnityEngine.Object.DestroyImmediate(behaviour);
                }
            }

            foreach (Collider collider in root.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.DestroyImmediate(collider);
            }

            foreach (Rigidbody rigidbody in root.GetComponentsInChildren<Rigidbody>(true))
            {
                UnityEngine.Object.DestroyImmediate(rigidbody);
            }
        }

        private static void SetLayerRecursively(Transform root, int layer)
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                child.gameObject.layer = layer;
            }
        }

        private static void SetSkinnedRenderersAlwaysVisible(GameObject root)
        {
            foreach (SkinnedMeshRenderer renderer in root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                renderer.updateWhenOffscreen = true;
            }
        }

        private static bool TryGetRendererBounds(GameObject root, Transform ignoredRoot, out Bounds bounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bounds = new Bounds(root.transform.position, Vector3.zero);
            bool found = false;
            foreach (Renderer renderer in renderers)
            {
                if (!renderer.enabled || (ignoredRoot != null && renderer.transform.IsChildOf(ignoredRoot)))
                {
                    continue;
                }

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

        private static void ValidateSwatMaterials(GameObject swatVisual)
        {
            Renderer[] renderers = swatVisual.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                throw new InvalidOperationException("SWAT visual has no renderers.");
            }

            foreach (Renderer renderer in renderers)
            {
                if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
                {
                    throw new InvalidOperationException("SWAT renderer has no material slots: " + renderer.name);
                }

                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    if (renderer.sharedMaterials[i] == null)
                    {
                        throw new InvalidOperationException($"SWAT renderer {renderer.name} has a missing material at slot {i}.");
                    }
                }
            }
        }

        private static string BuildValidationReport(GameObject playerRoot, GameObject swatVisual)
        {
            CharacterController controller = playerRoot.GetComponent<CharacterController>();
            Animator[] animators = playerRoot.GetComponentsInChildren<Animator>(true);
            SkinnedMeshRenderer[] skinnedRenderers = swatVisual.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            Transform weaponSocket = FindDeepChild(swatVisual.transform, WeaponSocketName);
            Transform rifle = weaponSocket == null ? null : weaponSocket.Find(RifleName);

            TryGetRendererBounds(swatVisual, rifle, out Bounds swatBounds);
            float colliderBottom = playerRoot.transform.position.y + controller.center.y - controller.height * 0.5f;
            float feet = swatBounds.min.y;
            float feetDelta = Mathf.Abs(feet - colliderBottom);

            int materialSlots = 0;
            int textureRefs = 0;
            foreach (Renderer renderer in swatVisual.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null)
                    {
                        continue;
                    }

                    materialSlots++;
                    textureRefs += material.GetTexturePropertyNames().Count(propertyName => material.GetTexture(propertyName) != null);
                }
            }

            StringBuilder report = new StringBuilder();
            report.AppendLine("PN_PlayerHero SWAT visual validation");
            report.AppendLine("player collider height: " + F(controller.height));
            report.AppendLine("SWAT renderer world bounds height: " + F(swatBounds.size.y));
            report.AppendLine("VisualRoot scale: " + V(playerRoot.transform.Find(VisualRootName).localScale));
            report.AppendLine("SWAT local scale: " + V(swatVisual.transform.localScale));
            report.AppendLine("feet world position: " + F(feet));
            report.AppendLine("collider bottom world position: " + F(colliderBottom));
            report.AppendLine("feet/collider bottom delta: " + F(feetDelta));
            report.AppendLine("animator count under player: " + animators.Length);
            report.AppendLine("active animator count under player: " + animators.Count(animator => animator.gameObject.activeInHierarchy && animator.enabled));
            report.AppendLine("SWAT skinned renderer count: " + skinnedRenderers.Length);
            report.AppendLine("SWAT material slot count: " + materialSlots);
            report.AppendLine("SWAT texture references detected: " + textureRefs);
            report.AppendLine("weapon socket found: " + (weaponSocket != null));
            report.AppendLine("rifle found under socket: " + (rifle != null));
            report.AppendLine("rifle local scale: " + (rifle == null ? "n/a" : V(rifle.localScale)));
            return report.ToString();
        }

        private static Transform FindDeepChild(Transform root, string childName)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == childName)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeepChild(root.GetChild(i), childName);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
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

        private static string F(float value)
        {
            return value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string V(Vector3 value)
        {
            return "(" + F(value.x) + ", " + F(value.y) + ", " + F(value.z) + ")";
        }

        private static void WriteProjectText(string relativePath, string text)
        {
            string absolutePath = ProjectPath(relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllText(absolutePath, text);
        }

        private static string ProjectPath(string relativePath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", relativePath));
        }
    }
}
