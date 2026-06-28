using System;
using System.Collections.Generic;
using UnityEngine;

namespace PortalNights.Visuals
{
    public struct PortalNightsGroundingValidationResult
    {
        public bool success;
        public int planetIndex;
        public string visualKind;
        public string floorSource;
        public string colliderSource;
        public float floorY;
        public float colliderBottomY;
        public float visualBottomY;
        public float colliderFloorError;
        public float visualFloorError;
        public Vector3 rootPosition;
        public string failure;
    }

    public static class PortalNightsGroundingUtility
    {
        private const float RayStartHeight = 10f;
        private const float RayDistance = 80f;
        private const float SphereRadius = 0.42f;
        private const float MinFloorNormalY = 0.65f;
        public const float GroundTolerance = 0.05f;

        private static readonly string[] ValidFloorTokens =
        {
            "Floor", "Ground", "Path", "Lane", "Bridge", "Platform", "Plaza", "Ring",
            "Road", "Walkable", "Deck", "Plate", "Disc", "Surface", "Moon",
            "Hive", "Station"
        };

        private static readonly string[] InvalidFloorTokens =
        {
            "Wall", "Rail", "Boundary", "PortalSurface", "PortalFrame", "PortalRing",
            "Portal_Ring", "Portal_Frame", "Portal", "Pylon", "Arch", "Frame", "Gate", "Decoration",
            "Decor", "Label", "Marker", "VFX", "BuildPad", "Turret", "Beacon", "Light",
            "Spawn", "Pickup", "Coin", "Objective"
        };

        private static readonly HashSet<int> FallbackWarningPlanets = new HashSet<int>();

        public static bool TryFindPlayableFloor(Vector3 approximatePosition, int planetIndex, out RaycastHit hit)
        {
            return TryFindPlayableFloor(approximatePosition, planetIndex, null, out hit);
        }

        public static bool GroundGameplayRoot(GameObject root, int planetIndex)
        {
            if (root == null)
                return false;

            Physics.SyncTransforms();

            ResolveFloorY(root, planetIndex, out float floorY, out string floorSource);

            float bottomY;
            string colliderSource;
            if (!TryGetMainColliderBottom(root, out bottomY, out colliderSource))
            {
                bottomY = root.transform.position.y;
                colliderSource = "root-position";
            }

            float deltaY = floorY - bottomY;
            if (Mathf.Abs(deltaY) > 0.001f)
                root.transform.position += Vector3.up * deltaY;

            Physics.SyncTransforms();
            return true;
        }

        public static PortalNightsVisualAlignmentResult GroundVisualToFloor(GameObject gameplayRoot, Transform visualInstance, int planetIndex, string visualKind)
        {
            if (gameplayRoot == null || visualInstance == null)
            {
                return new PortalNightsVisualAlignmentResult
                {
                    success = false,
                    label = visualKind ?? "visual",
                    targetSource = "missing-root-or-visual"
                };
            }

            Bounds worldBounds;
            if (!PortalNightsVisualBindingUtility.TryGetRendererWorldBounds(visualInstance, out worldBounds))
            {
                return new PortalNightsVisualAlignmentResult
                {
                    success = false,
                    label = visualKind ?? visualInstance.name,
                    rootY = gameplayRoot.transform.position.y,
                    targetSource = "no-renderer-bounds"
                };
            }

            ResolveFloorY(gameplayRoot, planetIndex, out float targetBottomY, out string targetSource);

            float before = worldBounds.min.y;
            float delta = targetBottomY - before;
            if (Mathf.Abs(delta) > 0.001f)
                visualInstance.position += Vector3.up * delta;

            Physics.SyncTransforms();
            PortalNightsVisualBindingUtility.TryGetRendererWorldBounds(visualInstance, out worldBounds);

            return new PortalNightsVisualAlignmentResult
            {
                success = Mathf.Abs(worldBounds.min.y - targetBottomY) < GroundTolerance,
                label = visualKind ?? visualInstance.name,
                targetSource = targetSource,
                rootY = gameplayRoot.transform.position.y,
                targetHeight = worldBounds.size.y,
                visualScale = visualInstance.localScale.x,
                targetBottomY = targetBottomY,
                visualBottomBeforeY = before,
                deltaY = delta,
                visualBottomAfterY = worldBounds.min.y,
                finalLocalPosition = visualInstance.localPosition
            };
        }

        public static PortalNightsGroundingValidationResult ValidateGrounding(GameObject gameplayRoot, Transform visualInstance, int planetIndex, string visualKind)
        {
            var result = new PortalNightsGroundingValidationResult
            {
                success = false,
                planetIndex = planetIndex,
                visualKind = visualKind ?? string.Empty,
                rootPosition = gameplayRoot != null ? gameplayRoot.transform.position : Vector3.zero,
                failure = string.Empty
            };

            if (gameplayRoot == null)
            {
                result.failure = "missing gameplay root";
                return result;
            }

            ResolveFloorY(gameplayRoot, planetIndex, out float floorY, out string floorSource);

            float colliderBottom;
            string colliderSource;
            if (!TryGetMainColliderBottom(gameplayRoot, out colliderBottom, out colliderSource))
            {
                colliderBottom = gameplayRoot.transform.position.y;
                colliderSource = "root-position";
            }

            if (visualInstance == null)
            {
                result.floorY = floorY;
                result.floorSource = floorSource;
                result.colliderSource = colliderSource;
                result.colliderBottomY = colliderBottom;
                result.visualBottomY = colliderBottom;
                result.colliderFloorError = Mathf.Abs(colliderBottom - floorY);
                result.visualFloorError = float.PositiveInfinity;
                result.failure = "missing visual";
                return result;
            }

            float visualBottom;
            if (!PortalNightsVisualBindingUtility.TryGetRendererWorldBottom(visualInstance, out visualBottom))
            {
                result.failure = "missing visual renderer bounds";
                visualBottom = colliderBottom;
            }

            result.floorY = floorY;
            result.floorSource = floorSource;
            result.colliderSource = colliderSource;
            result.colliderBottomY = colliderBottom;
            result.visualBottomY = visualBottom;
            result.colliderFloorError = Mathf.Abs(colliderBottom - floorY);
            result.visualFloorError = Mathf.Abs(visualBottom - floorY);
            result.success = result.colliderFloorError < GroundTolerance && result.visualFloorError < GroundTolerance;

            if (!result.success && string.IsNullOrEmpty(result.failure))
                result.failure = $"collider error {result.colliderFloorError:0.000}, visual error {result.visualFloorError:0.000}";

            return result;
        }

        public static bool ResolveFloorY(GameObject gameplayRoot, int planetIndex, out float floorY, out string source)
        {
            floorY = 0f;
            source = "none";
            if (gameplayRoot == null)
            {
                return false;
            }

            if (TryGetGroundTargetY(gameplayRoot, planetIndex, false, out floorY, out source))
            {
                return true;
            }

            floorY = GetFallbackFloorY(planetIndex, gameplayRoot.transform.position);
            source = "fallback";
            LogFallbackOnce(planetIndex, gameplayRoot.name, gameplayRoot.transform.position, floorY);
            return false;
        }

        public static bool TryGetGroundTargetY(GameObject gameplayRoot, int planetIndex, bool allowColliderFallback, out float targetY, out string source)
        {
            targetY = 0f;
            source = "none";

            if (gameplayRoot == null)
                return false;

            RaycastHit hit;
            if (TryFindPlayableFloor(gameplayRoot.transform.position, planetIndex, gameplayRoot.transform, out hit))
            {
                targetY = hit.point.y;
                source = hit.collider != null ? $"floor:{hit.collider.name}" : "floor";
                return true;
            }

            if (allowColliderFallback && TryGetMainColliderBottom(gameplayRoot, out targetY, out source))
            {
                source = $"collider:{source}";
                return true;
            }

            return false;
        }

        public static bool TryGetMainColliderBottom(GameObject root, out float bottomY, out string source)
        {
            bottomY = 0f;
            source = "none";

            if (root == null)
                return false;

            CharacterController controller = root.GetComponentInChildren<CharacterController>();
            if (controller != null && controller.enabled)
            {
                bottomY = controller.bounds.min.y;
                source = controller.name;
                return true;
            }

            Collider bestRootCollider = GetBestCollider(root.GetComponents<Collider>());
            if (bestRootCollider != null)
            {
                bottomY = bestRootCollider.bounds.min.y;
                source = bestRootCollider.name;
                return true;
            }

            Collider bestCollider = null;
            float bestVolume = -1f;
            Collider[] colliders = root.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider candidate = colliders[i];
                if (candidate == null || !candidate.enabled || candidate.isTrigger)
                    continue;

                if (candidate.transform != root.transform && LooksLikeVisualOnlyCollider(candidate))
                    continue;

                Bounds bounds = candidate.bounds;
                float volume = Mathf.Max(0.001f, bounds.size.x * bounds.size.y * bounds.size.z);
                if (volume > bestVolume)
                {
                    bestVolume = volume;
                    bestCollider = candidate;
                }
            }

            if (bestCollider == null)
                return false;

            bottomY = bestCollider.bounds.min.y;
            source = bestCollider.name;
            return true;
        }

        private static Collider GetBestCollider(Collider[] colliders)
        {
            if (colliders == null || colliders.Length == 0)
                return null;

            Collider best = null;
            float bestVolume = -1f;
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider candidate = colliders[i];
                if (candidate == null || !candidate.enabled || candidate.isTrigger)
                    continue;

                Bounds bounds = candidate.bounds;
                float volume = Mathf.Max(0.001f, bounds.size.x * bounds.size.y * bounds.size.z);
                if (volume > bestVolume)
                {
                    bestVolume = volume;
                    best = candidate;
                }
            }

            return best;
        }

        private static bool TryFindPlayableFloor(Vector3 approximatePosition, int planetIndex, Transform ignoreRoot, out RaycastHit hit)
        {
            Physics.SyncTransforms();

            Vector3 origin = approximatePosition + Vector3.up * RayStartHeight;
            RaycastHit[] rayHits = Physics.RaycastAll(origin, Vector3.down, RayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            if (TryPickFloorHit(rayHits, planetIndex, ignoreRoot, out hit))
                return true;

            RaycastHit[] sphereHits = Physics.SphereCastAll(origin, SphereRadius, Vector3.down, RayDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            return TryPickFloorHit(sphereHits, planetIndex, ignoreRoot, out hit);
        }

        private static bool TryPickFloorHit(RaycastHit[] hits, int planetIndex, Transform ignoreRoot, out RaycastHit hit)
        {
            hit = default;
            if (hits == null || hits.Length == 0)
                return false;

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            for (int i = 0; i < hits.Length; i++)
            {
                if (IsValidFloorHit(hits[i], planetIndex, ignoreRoot))
                {
                    hit = hits[i];
                    return true;
                }
            }

            return false;
        }

        private static bool IsValidFloorHit(RaycastHit hit, int planetIndex, Transform ignoreRoot)
        {
            Collider collider = hit.collider;
            if (collider == null || !collider.enabled || collider.isTrigger)
                return false;

            if (ignoreRoot != null && collider.transform.IsChildOf(ignoreRoot))
                return false;

            if (hit.normal.y < MinFloorNormalY)
                return false;

            if (collider.GetComponentInParent<PortalNightsEnemy>() != null ||
                collider.GetComponentInParent<PortalNightsPlayerController>() != null ||
                collider.GetComponentInParent<PortalNightsCoinPickup>() != null ||
                collider.GetComponentInParent<PortalNightsBuildPoint>() != null)
                return false;

            string colliderName = collider.name;
            if (ContainsAny(colliderName, InvalidFloorTokens))
                return false;

            string localNameStack = BuildLocalNameStack(collider.transform);
            if (ContainsAny(localNameStack, InvalidFloorTokens))
                return false;

            string layerName = LayerMask.LayerToName(collider.gameObject.layer);
            if (layerName.Equals("WalkableFloor", StringComparison.OrdinalIgnoreCase))
                return true;

            if (ContainsAny(colliderName, ValidFloorTokens))
                return true;

            return ContainsAny(localNameStack, ValidFloorTokens);
        }

        private static bool LooksLikeVisualOnlyCollider(Collider collider)
        {
            if (collider == null)
                return true;

            string localNameStack = BuildLocalNameStack(collider.transform);
            return ContainsAny(collider.name, InvalidFloorTokens) || ContainsAny(localNameStack, InvalidFloorTokens);
        }

        private static string BuildLocalNameStack(Transform transform)
        {
            if (transform == null)
                return string.Empty;

            string result = string.Empty;
            Transform current = transform;
            int guard = 0;
            while (current != null && guard < 5)
            {
                if (IsGlobalArenaRoot(current.name))
                    break;

                result += " " + current.name;
                current = current.parent;
                guard++;
            }

            return result;
        }

        private static bool IsGlobalArenaRoot(string name)
        {
            return name.Equals("PortalNightsArena", StringComparison.OrdinalIgnoreCase)
                || name.Equals("PortalNightsArena(Clone)", StringComparison.OrdinalIgnoreCase);
        }

        private static bool ContainsAny(string text, string[] tokens)
        {
            if (string.IsNullOrEmpty(text) || tokens == null)
                return false;

            for (int i = 0; i < tokens.Length; i++)
            {
                if (text.IndexOf(tokens[i], StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        private static float GetFallbackFloorY(int planetIndex, Vector3 approximatePosition)
        {
            return 0f;
        }

        private static void LogFallbackOnce(int planetIndex, string objectName, Vector3 approximatePosition, float fallbackY)
        {
            if (FallbackWarningPlanets.Contains(planetIndex))
                return;

            FallbackWarningPlanets.Add(planetIndex);
            Debug.LogWarning($"[PortalNightsGrounding] No valid playable floor found for planet {planetIndex} near {objectName} at {approximatePosition}. Using fallback y={fallbackY:0.###}.");
        }
    }
}
