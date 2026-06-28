using Unity.Netcode;
using UnityEngine;

namespace PortalNights.Visuals
{
    public struct PortalNightsVisualAlignmentResult
    {
        public bool success;
        public string label;
        public string visualName;
        public string targetSource;
        public float rootY;
        public float targetHeight;
        public float visualScale;
        public float targetBottomY;
        public float visualBottomBeforeY;
        public float deltaY;
        public float visualBottomAfterY;
        public Vector3 finalLocalPosition;

        public float Error => Mathf.Abs(visualBottomAfterY - targetBottomY);
    }

    public static class PortalNightsVisualBindingUtility
    {
        public static void StripGameplayComponents(GameObject instance)
        {
            foreach (NetworkObject networkObject in instance.GetComponentsInChildren<NetworkObject>(true))
            {
                DestroySafe(networkObject);
            }

            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true))
            {
                DestroySafe(body);
            }

            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            {
                DestroySafe(collider);
            }

            DisableRootMotion(instance == null ? null : instance.transform);
        }

        public static void DisableRootMotion(Transform root)
        {
            if (root == null)
            {
                return;
            }

            Animator[] animators = root.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];
                if (animator != null)
                {
                    animator.applyRootMotion = false;
                }
            }
        }

        public static PortalNightsVisualAlignmentResult AlignVisualToGameplayBottom(
            Transform gameplayRoot,
            Transform visualInstance,
            float targetHeight,
            string label,
            bool debugLog)
        {
            return AlignVisualToGameplayBottom(
                gameplayRoot,
                visualInstance,
                targetHeight,
                label,
                debugLog,
                0);
        }

        public static PortalNightsVisualAlignmentResult AlignVisualToGameplayBottom(
            Transform gameplayRoot,
            Transform visualInstance,
            float targetHeight,
            string label,
            bool debugLog,
            int planetIndex)
        {
            PortalNightsVisualAlignmentResult result = new PortalNightsVisualAlignmentResult
            {
                label = label,
                visualName = visualInstance == null ? string.Empty : visualInstance.name,
                rootY = gameplayRoot == null ? 0f : gameplayRoot.position.y,
                targetHeight = Mathf.Max(0.01f, targetHeight)
            };

            if (gameplayRoot == null || visualInstance == null)
            {
                DebugAlignment(result, debugLog, "missing root or visual");
                return result;
            }

            DisableRootMotion(visualInstance);
            ForceAnimatorPose(visualInstance);
            visualInstance.localScale = Vector3.one;
            visualInstance.localPosition = Vector3.zero;
            ForceAnimatorPose(visualInstance);

            if (!TryGetRendererWorldBounds(visualInstance, out Bounds initialWorldBounds) || initialWorldBounds.size.y <= 0.001f)
            {
                DebugAlignment(result, debugLog, "missing renderer world bounds");
                return result;
            }

            float uniformScale = Mathf.Max(0.01f, targetHeight) / initialWorldBounds.size.y;
            visualInstance.localScale = Vector3.one * uniformScale;
            ForceAnimatorPose(visualInstance);

            for (int pass = 0; pass < 2; pass++)
            {
                if (!TryGetRendererWorldBounds(visualInstance, out Bounds scaledWorldBounds) || scaledWorldBounds.size.y <= 0.001f)
                {
                    break;
                }

                float worldHeightCorrection = Mathf.Max(0.01f, targetHeight) / scaledWorldBounds.size.y;
                if (Mathf.Abs(worldHeightCorrection - 1f) > 0.001f)
                {
                    uniformScale *= worldHeightCorrection;
                    visualInstance.localScale = Vector3.one * uniformScale;
                    ForceAnimatorPose(visualInstance);
                }
            }

            if (!TryGetRendererWorldBottom(visualInstance, out float visualBottomBefore))
            {
                DebugAlignment(result, debugLog, "missing renderer world bottom");
                return result;
            }

            PortalNightsGroundingUtility.ResolveFloorY(gameplayRoot.gameObject, planetIndex, out float targetBottom, out string targetSource);
            float visualBottomAfter = visualBottomBefore;
            float totalDeltaY = 0f;
            for (int pass = 0; pass < 3; pass++)
            {
                float deltaY = targetBottom - visualBottomAfter;
                if (Mathf.Abs(deltaY) <= 0.001f)
                {
                    break;
                }

                visualInstance.position += Vector3.up * deltaY;
                totalDeltaY += deltaY;
                ForceAnimatorPose(visualInstance);

                if (!TryGetRendererWorldBottom(visualInstance, out visualBottomAfter))
                {
                    DebugAlignment(result, debugLog, "missing renderer world bottom after alignment");
                    return result;
                }
            }

            result.success = Mathf.Abs(visualBottomAfter - targetBottom) < PortalNightsGroundingUtility.GroundTolerance;
            result.targetSource = targetSource;
            result.visualScale = uniformScale;
            result.targetBottomY = targetBottom;
            result.visualBottomBeforeY = visualBottomBefore;
            result.deltaY = totalDeltaY;
            result.visualBottomAfterY = visualBottomAfter;
            result.finalLocalPosition = visualInstance.localPosition;
            if (!result.success)
            {
                Debug.LogWarning(
                    "[GROUNDING_FAIL] character=" + label
                    + " planet=" + planetIndex
                    + " floorY=" + targetBottom.ToString("F3")
                    + " visualBottom=" + visualBottomAfter.ToString("F3")
                    + " difference=" + Mathf.Abs(visualBottomAfter - targetBottom).ToString("F3"));
            }

            DebugAlignment(result, debugLog, null);
            return result;
        }

        public static bool TryGetLocalRendererBounds(Transform root, out Bounds localBounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            localBounds = default;
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!IsVisualMeshRenderer(renderer))
                {
                    continue;
                }

                if (TryGetRendererObjectWorldBounds(renderer, out Bounds rendererBounds))
                {
                    EncapsulateWorldBounds(root, rendererBounds, ref localBounds, ref hasBounds);
                }
            }

            return hasBounds;
        }

        public static bool TryGetRendererWorldBounds(Transform root, out Bounds worldBounds)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            worldBounds = default;
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!IsVisualMeshRenderer(renderer))
                {
                    continue;
                }

                if (!TryGetRendererObjectWorldBounds(renderer, out Bounds rendererBounds))
                {
                    continue;
                }

                if (!hasBounds)
                {
                    worldBounds = rendererBounds;
                    hasBounds = true;
                    continue;
                }

                worldBounds.Encapsulate(rendererBounds);
            }

            return hasBounds;
        }

        public static bool TryGetRendererWorldBottom(Transform root, out float bottomY)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            bottomY = 0f;
            bool hasBounds = false;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (!IsVisualMeshRenderer(renderer))
                {
                    continue;
                }

                if (!TryGetRendererObjectWorldBounds(renderer, out Bounds rendererBounds))
                {
                    continue;
                }

                float candidate = rendererBounds.min.y;
                if (!hasBounds || candidate < bottomY)
                {
                    bottomY = candidate;
                    hasBounds = true;
                }
            }

            return hasBounds;
        }

        public static void DestroySafe(Object target)
        {
            if (target == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(target);
            }
            else
            {
                Object.DestroyImmediate(target);
            }
        }

        private static bool IsVisualMeshRenderer(Renderer renderer)
        {
            return renderer != null
                && renderer.enabled
                && renderer.gameObject.activeInHierarchy
                && (renderer is SkinnedMeshRenderer || renderer is MeshRenderer);
        }

        private static bool TryGetRendererObjectWorldBounds(Renderer renderer, out Bounds worldBounds)
        {
            worldBounds = default;
            if (renderer is SkinnedMeshRenderer skinnedRenderer && skinnedRenderer.sharedMesh != null)
            {
                Mesh bakedMesh = new Mesh();
                try
                {
                    skinnedRenderer.BakeMesh(bakedMesh);
                    if (TryGetMeshWorldBounds(bakedMesh, skinnedRenderer.transform, out worldBounds))
                    {
                        return true;
                    }
                }
                finally
                {
                    DestroySafe(bakedMesh);
                }
            }

            if (renderer is MeshRenderer meshRenderer)
            {
                MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                if (meshFilter != null && meshFilter.sharedMesh != null && TryGetMeshWorldBounds(meshFilter.sharedMesh, meshRenderer.transform, out worldBounds))
                {
                    return true;
                }
            }

            worldBounds = renderer.bounds;
            return worldBounds.size.sqrMagnitude > 0.000001f;
        }

        private static bool TryGetMeshWorldBounds(Mesh mesh, Transform meshTransform, out Bounds worldBounds)
        {
            worldBounds = default;
            if (mesh == null || meshTransform == null || mesh.vertexCount == 0)
            {
                return false;
            }

            Vector3[] vertices = mesh.vertices;
            bool hasBounds = false;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 worldPoint = meshTransform.TransformPoint(vertices[i]);
                if (!hasBounds)
                {
                    worldBounds = new Bounds(worldPoint, Vector3.zero);
                    hasBounds = true;
                    continue;
                }

                worldBounds.Encapsulate(worldPoint);
            }

            return hasBounds && worldBounds.size.sqrMagnitude > 0.000001f;
        }

        private static void ForceAnimatorPose(Transform root)
        {
            Animator[] animators = root.GetComponentsInChildren<Animator>(true);
            for (int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];
                if (animator != null && animator.isActiveAndEnabled)
                {
                    animator.applyRootMotion = false;
                    animator.Update(0f);
                }
            }
        }

        private static void DebugAlignment(PortalNightsVisualAlignmentResult result, bool debugLog, string failure)
        {
            if (!debugLog)
            {
                return;
            }

            if (!string.IsNullOrEmpty(failure))
            {
                Debug.LogWarning("[PortalNightsVisualAlign] " + result.label + " " + result.visualName + " failed: " + failure);
                return;
            }

            Debug.Log(
                "[PortalNightsVisualAlign] "
                + result.label
                + " visual=" + result.visualName
                + " rootY=" + result.rootY.ToString("F3")
                + " height=" + result.targetHeight.ToString("F3")
                + " scale=" + result.visualScale.ToString("F3")
                + " target=" + result.targetBottomY.ToString("F3")
                + " source=" + result.targetSource
                + " before=" + result.visualBottomBeforeY.ToString("F3")
                + " delta=" + result.deltaY.ToString("F3")
                + " after=" + result.visualBottomAfterY.ToString("F3")
                + " error=" + result.Error.ToString("F3")
                + " local=" + result.finalLocalPosition);
        }

        private static void EncapsulateWorldBounds(Transform root, Bounds worldBounds, ref Bounds localBounds, ref bool hasBounds)
        {
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;
            EncapsulateLocalPoint(root, new Vector3(min.x, min.y, min.z), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(root, new Vector3(min.x, min.y, max.z), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(root, new Vector3(min.x, max.y, min.z), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(root, new Vector3(min.x, max.y, max.z), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(root, new Vector3(max.x, min.y, min.z), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(root, new Vector3(max.x, min.y, max.z), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(root, new Vector3(max.x, max.y, min.z), ref localBounds, ref hasBounds);
            EncapsulateLocalPoint(root, new Vector3(max.x, max.y, max.z), ref localBounds, ref hasBounds);
        }

        private static void EncapsulateLocalPoint(Transform root, Vector3 worldPoint, ref Bounds localBounds, ref bool hasBounds)
        {
            Vector3 localPoint = root.InverseTransformPoint(worldPoint);
            if (!hasBounds)
            {
                localBounds = new Bounds(localPoint, Vector3.zero);
                hasBounds = true;
                return;
            }

            localBounds.Encapsulate(localPoint);
        }
    }
}
