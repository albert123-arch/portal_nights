using System;
using System.Collections.Generic;
using PortalNights;
using UnityEngine;

namespace PortalNights.Scenes
{
    [DisallowMultipleComponent]
    public sealed class PortalNightsPlanetSceneRoot : MonoBehaviour
    {
        private const int MinPlanetIndex = 1;
        private const int MaxPlanetIndex = 5;

        [SerializeField] private int planetIndex = 1;
        [SerializeField] private string planetDisplayNameKey;
        [SerializeField] private Transform playerArrivalPoint;
        [SerializeField] private Transform[] playerSpawnPoints;
        [SerializeField] private Transform[] enemySpawnPoints;
        [SerializeField] private Transform[] objectiveMarkers;
        [SerializeField] private PortalNightsBuildPoint[] buildPoints;
        [SerializeField] private PortalNightsHealth coreHealth;
        [SerializeField] private Transform portalSpawn;
        [SerializeField] private PortalNightsLanePath leftLanePath;
        [SerializeField] private PortalNightsLanePath rightLanePath;
        [SerializeField] private PortalNightsStaffRescue[] staff;
        [SerializeField] private PortalNightsEnemyRift[] enemyRifts;
        [SerializeField] private PortalNightsPlanet4HiveRift[] hiveRifts;
        [SerializeField] private PortalNightsPlanet5Stabilizer[] stabilizers;
        [SerializeField] private PortalNightsHealth mainObjectiveHealth;
        [SerializeField] private Transform exitPortal;
        [SerializeField] private Transform universePortal;
        [SerializeField] private Vector3 boundsCenter;
        [SerializeField] private Vector3 boundsSize = new Vector3(100f, 40f, 100f);

        public int PlanetIndex => planetIndex;
        public string PlanetDisplayNameKey => planetDisplayNameKey;
        public Transform PlayerArrivalPoint => playerArrivalPoint;
        public Transform[] PlayerSpawnPoints => playerSpawnPoints ?? Array.Empty<Transform>();
        public Transform[] EnemySpawnPoints => enemySpawnPoints ?? Array.Empty<Transform>();
        public Transform[] ObjectiveMarkers => objectiveMarkers ?? Array.Empty<Transform>();
        public PortalNightsBuildPoint[] BuildPoints => buildPoints ?? Array.Empty<PortalNightsBuildPoint>();
        public PortalNightsHealth CoreHealth => coreHealth;
        public Transform PortalSpawn => portalSpawn;
        public PortalNightsLanePath LeftLanePath => leftLanePath;
        public PortalNightsLanePath RightLanePath => rightLanePath;
        public PortalNightsStaffRescue[] Staff => staff ?? Array.Empty<PortalNightsStaffRescue>();
        public PortalNightsEnemyRift[] EnemyRifts => enemyRifts ?? Array.Empty<PortalNightsEnemyRift>();
        public PortalNightsPlanet4HiveRift[] HiveRifts => hiveRifts ?? Array.Empty<PortalNightsPlanet4HiveRift>();
        public PortalNightsPlanet5Stabilizer[] Stabilizers => stabilizers ?? Array.Empty<PortalNightsPlanet5Stabilizer>();
        public PortalNightsHealth MainObjectiveHealth => mainObjectiveHealth;
        public Transform ExitPortal => exitPortal;
        public Transform UniversePortal => universePortal;

        public void AutoDiscoverReferences()
        {
            playerArrivalPoint = FindBestNamedTransform("PlayerArrivalPoint", "ArrivalPoint", "Arrival");
            playerSpawnPoints = FilterNulls(FindTransformsContaining("PN_PlayerSpawn_", "PlayerSpawn"));
            enemySpawnPoints = FilterNulls(FindEnemySpawnTransforms());
            objectiveMarkers = FilterNulls(FindObjectiveMarkerTransforms());
            buildPoints = FilterNulls(FindBuildPoints());
            coreHealth = FindCoreHealth();
            leftLanePath = FindLanePath(PortalNightsLane.Left, "LeftLane");
            rightLanePath = FindLanePath(PortalNightsLane.Right, "RightLane");
            portalSpawn = FindPortalSpawn();
            staff = FilterNulls(GetComponentsInChildren<PortalNightsStaffRescue>(true));
            enemyRifts = FilterNulls(GetComponentsInChildren<PortalNightsEnemyRift>(true));
            hiveRifts = FilterNulls(GetComponentsInChildren<PortalNightsPlanet4HiveRift>(true));
            stabilizers = FilterNulls(GetComponentsInChildren<PortalNightsPlanet5Stabilizer>(true));
            mainObjectiveHealth = FindMainObjectiveHealth();
            if (mainObjectiveHealth == null)
            {
                mainObjectiveHealth = coreHealth;
            }
            else if (coreHealth == null && planetIndex == 1)
            {
                coreHealth = mainObjectiveHealth;
            }

            exitPortal = FindBestNamedTransform("ExitPortal");
            universePortal = FindBestNamedTransform("UniversePortal");

            if (playerArrivalPoint == null && playerSpawnPoints != null && playerSpawnPoints.Length > 0)
            {
                playerArrivalPoint = FindPreferredSpawnTransform(playerSpawnPoints, "PN_PlayerSpawn_1", "PlayerSpawn_1") ?? playerSpawnPoints[0];
            }

            if ((playerSpawnPoints == null || playerSpawnPoints.Length == 0) && playerArrivalPoint != null)
            {
                playerSpawnPoints = new[] { playerArrivalPoint };
            }

            if ((objectiveMarkers == null || objectiveMarkers.Length == 0) && mainObjectiveHealth != null)
            {
                objectiveMarkers = new[] { mainObjectiveHealth.transform };
            }

            if (TryCalculateLocalBounds(out Bounds localBounds))
            {
                boundsCenter = localBounds.center;
                boundsSize = localBounds.size;
            }
        }

        public bool ValidateSetup(bool logWarnings)
        {
            bool valid = true;

            if (planetIndex < MinPlanetIndex || planetIndex > MaxPlanetIndex)
            {
                valid = false;
                Warn(logWarnings, "Planet index is outside the supported 1..5 range.");
            }

            if (playerArrivalPoint == null)
            {
                valid = false;
                Warn(logWarnings, "Player arrival point is missing.");
            }

            if (PlanetExpectsBuildPoints(planetIndex) && (buildPoints == null || buildPoints.Length == 0))
            {
                valid = false;
                Warn(logWarnings, "No build points were discovered for this planet root.");
            }

            if (!HasAnySceneRootReferences())
            {
                valid = false;
                Warn(logWarnings, "No scene root references were discovered under this root.");
            }

            if (!TryGetBounds(out _))
            {
                valid = false;
                Warn(logWarnings, "Bounds could not be derived from serialized data or child geometry.");
            }

            if (planetIndex == 1)
            {
                if (coreHealth == null && mainObjectiveHealth == null)
                {
                    Warn(logWarnings, "Planet1 core health was not discovered under this root.");
                }

                if (portalSpawn == null)
                {
                    Warn(logWarnings, "Planet1 portal spawn was not discovered under this root. Legacy fallback spawn position will be used.");
                }

                if (leftLanePath == null)
                {
                    Warn(logWarnings, "Planet1 left lane path was not discovered under this root.");
                }

                if (rightLanePath == null)
                {
                    Warn(logWarnings, "Planet1 right lane path was not discovered under this root.");
                }
            }

            return valid;
        }

        public Vector3 GetPlayerSpawnPosition(ulong clientId, Vector3 fallback)
        {
            Transform spawn = GetIndexedSpawnTransform(clientId);
            if (spawn != null)
            {
                return spawn.position;
            }

            return playerArrivalPoint != null ? playerArrivalPoint.position : fallback;
        }

        public Quaternion GetPlayerSpawnRotation(ulong clientId, Quaternion fallback)
        {
            Transform spawn = GetIndexedSpawnTransform(clientId);
            if (spawn != null)
            {
                return spawn.rotation;
            }

            return playerArrivalPoint != null ? playerArrivalPoint.rotation : fallback;
        }

        public bool TryGetBounds(out Bounds bounds)
        {
            if (HasStoredBounds())
            {
                bounds = BuildWorldBounds(new Bounds(boundsCenter, boundsSize));
                return true;
            }

            if (TryCalculateLocalBounds(out Bounds localBounds))
            {
                bounds = BuildWorldBounds(localBounds);
                return true;
            }

            bounds = default;
            return false;
        }

        [ContextMenu("Auto Discover References")]
        private void AutoDiscoverReferencesContextMenu()
        {
            AutoDiscoverReferences();
        }

        [ContextMenu("Validate Setup")]
        private void ValidateSetupContextMenu()
        {
            ValidateSetup(true);
        }

        private Transform GetIndexedSpawnTransform(ulong clientId)
        {
            Transform[] spawns = PlayerSpawnPoints;
            if (spawns.Length == 0)
            {
                return null;
            }

            int index = (int)(clientId % (ulong)spawns.Length);
            return spawns[index];
        }

        private bool HasAnySceneRootReferences()
        {
            return playerArrivalPoint != null
                || PlayerSpawnPoints.Length > 0
                || EnemySpawnPoints.Length > 0
                || ObjectiveMarkers.Length > 0
                || BuildPoints.Length > 0
                || coreHealth != null
                || portalSpawn != null
                || leftLanePath != null
                || rightLanePath != null
                || Staff.Length > 0
                || EnemyRifts.Length > 0
                || HiveRifts.Length > 0
                || Stabilizers.Length > 0
                || mainObjectiveHealth != null
                || exitPortal != null
                || universePortal != null;
        }

        private static bool PlanetExpectsBuildPoints(int index)
        {
            return index >= MinPlanetIndex && index <= MaxPlanetIndex;
        }

        private static T[] FilterNulls<T>(T[] values) where T : UnityEngine.Object
        {
            if (values == null || values.Length == 0)
            {
                return Array.Empty<T>();
            }

            List<T> filtered = new List<T>(values.Length);
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != null)
                {
                    filtered.Add(values[i]);
                }
            }

            return filtered.ToArray();
        }

        private Transform[] FindEnemySpawnTransforms()
        {
            Transform group = FindBestNamedTransform("EnemySpawnPoints");
            if (group != null)
            {
                List<Transform> children = new List<Transform>();
                for (int i = 0; i < group.childCount; i++)
                {
                    Transform child = group.GetChild(i);
                    if (child != null)
                    {
                        children.Add(child);
                    }
                }

                return children.ToArray();
            }

            return FindTransformsContaining("EnemySpawn", "PortalSpawn", "RiftSpawn");
        }

        private Transform[] FindObjectiveMarkerTransforms()
        {
            Transform[] markers = FindTransformsContaining("ObjectiveMarker", "Marker");
            if (markers.Length > 0)
            {
                return markers;
            }

            return Array.Empty<Transform>();
        }

        private Transform FindBestNamedTransform(params string[] searchTerms)
        {
            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            for (int termIndex = 0; termIndex < searchTerms.Length; termIndex++)
            {
                string searchTerm = searchTerms[termIndex];
                for (int i = 0; i < transforms.Length; i++)
                {
                    Transform candidate = transforms[i];
                    if (candidate != null && string.Equals(candidate.name, searchTerm, StringComparison.OrdinalIgnoreCase))
                    {
                        return candidate;
                    }
                }
            }

            for (int termIndex = 0; termIndex < searchTerms.Length; termIndex++)
            {
                string searchTerm = searchTerms[termIndex];
                for (int i = 0; i < transforms.Length; i++)
                {
                    Transform candidate = transforms[i];
                    if (candidate != null && candidate.name.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        private Transform[] FindTransformsContaining(params string[] searchTerms)
        {
            Transform[] transforms = GetComponentsInChildren<Transform>(true);
            List<Transform> matches = new List<Transform>();
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate == null || candidate == transform)
                {
                    continue;
                }

                if (MatchesAny(candidate.name, searchTerms))
                {
                    matches.Add(candidate);
                }
            }

            return matches.ToArray();
        }

        private static Transform FindPreferredSpawnTransform(Transform[] transforms, params string[] preferredNames)
        {
            if (transforms == null || transforms.Length == 0)
            {
                return null;
            }

            for (int preferredIndex = 0; preferredIndex < preferredNames.Length; preferredIndex++)
            {
                string preferredName = preferredNames[preferredIndex];
                for (int i = 0; i < transforms.Length; i++)
                {
                    Transform candidate = transforms[i];
                    if (candidate != null && string.Equals(candidate.name, preferredName, StringComparison.OrdinalIgnoreCase))
                    {
                        return candidate;
                    }
                }
            }

            return null;
        }

        private PortalNightsBuildPoint[] FindBuildPoints()
        {
            Transform buildPadRoot = FindBestNamedTransform("PN_BuildPads", "BuildPads");
            if (buildPadRoot != null)
            {
                return buildPadRoot.GetComponentsInChildren<PortalNightsBuildPoint>(true);
            }

            return GetComponentsInChildren<PortalNightsBuildPoint>(true);
        }

        private PortalNightsHealth FindCoreHealth()
        {
            Transform centralCoreRoot = FindBestNamedTransform("PN_Central_Core", "Central_Core", "CentralCore", "Core");
            if (centralCoreRoot != null)
            {
                PortalNightsHealth preferredCore = centralCoreRoot.GetComponentInChildren<PortalNightsHealth>(true);
                if (preferredCore != null)
                {
                    return preferredCore;
                }
            }

            PortalNightsHealth[] healths = GetComponentsInChildren<PortalNightsHealth>(true);
            for (int i = 0; i < healths.Length; i++)
            {
                PortalNightsHealth candidate = healths[i];
                if (candidate != null && candidate.name.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return candidate;
                }
            }

            return null;
        }

        private Transform FindPortalSpawn()
        {
            Transform directMatch = FindBestNamedTransform("PortalSpawn", "EnemyPortalSpawn", "EnemySpawn", "SpawnPoint");
            if (directMatch != null)
            {
                return directMatch;
            }

            Transform portalArea = FindBestNamedTransform("PortalArea", "PN_Glowing_Portal");
            if (portalArea != null)
            {
                Transform[] portalChildren = portalArea.GetComponentsInChildren<Transform>(true);
                for (int i = 0; i < portalChildren.Length; i++)
                {
                    Transform candidate = portalChildren[i];
                    if (candidate != null && candidate != portalArea && MatchesAny(candidate.name, new[] { "PortalSpawn", "EnemySpawn", "SpawnPoint" }))
                    {
                        return candidate;
                    }
                }
            }

            Transform[] discoveredEnemySpawns = EnemySpawnPoints;
            return discoveredEnemySpawns.Length > 0 ? discoveredEnemySpawns[0] : null;
        }

        private PortalNightsLanePath FindLanePath(PortalNightsLane expectedLane, params string[] preferredNameTerms)
        {
            PortalNightsLanePath[] lanePaths = GetComponentsInChildren<PortalNightsLanePath>(true);
            for (int i = 0; i < lanePaths.Length; i++)
            {
                PortalNightsLanePath candidate = lanePaths[i];
                if (candidate != null && candidate.Lane == expectedLane && MatchesAny(candidate.name, preferredNameTerms))
                {
                    return candidate;
                }
            }

            for (int i = 0; i < lanePaths.Length; i++)
            {
                PortalNightsLanePath candidate = lanePaths[i];
                if (candidate != null && MatchesAny(candidate.name, preferredNameTerms))
                {
                    return candidate;
                }
            }

            for (int i = 0; i < lanePaths.Length; i++)
            {
                PortalNightsLanePath candidate = lanePaths[i];
                if (candidate != null && candidate.Lane == expectedLane)
                {
                    return candidate;
                }
            }

            return null;
        }

        private PortalNightsHealth FindMainObjectiveHealth()
        {
            PortalNightsHealth[] healths = GetComponentsInChildren<PortalNightsHealth>(true);
            PortalNightsHealth fallback = null;
            for (int i = 0; i < healths.Length; i++)
            {
                PortalNightsHealth candidate = healths[i];
                if (candidate == null)
                {
                    continue;
                }

                GameObject owner = candidate.gameObject;
                if (owner.GetComponent<PortalNightsBuildPoint>() != null
                    || owner.GetComponent<PortalNightsStaffRescue>() != null
                    || owner.GetComponent<PortalNightsEnemyRift>() != null
                    || owner.GetComponent<PortalNightsPlanet4HiveRift>() != null
                    || owner.GetComponent<PortalNightsPlanet5Stabilizer>() != null)
                {
                    continue;
                }

                if (fallback == null)
                {
                    fallback = candidate;
                }

                string name = owner.name;
                if (name.IndexOf("Objective", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Sphere", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Relay", StringComparison.OrdinalIgnoreCase) >= 0
                    || name.IndexOf("Core", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return candidate;
                }
            }

            return fallback;
        }

        private bool TryCalculateLocalBounds(out Bounds localBounds)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            bool initialized = false;
            localBounds = default;

            for (int i = 0; i < renderers.Length; i++)
            {
                EncapsulateLocalBounds(renderers[i].bounds, ref initialized, ref localBounds);
            }

            for (int i = 0; i < colliders.Length; i++)
            {
                EncapsulateLocalBounds(colliders[i].bounds, ref initialized, ref localBounds);
            }

            return initialized;
        }

        private void EncapsulateLocalBounds(Bounds worldBounds, ref bool initialized, ref Bounds localBounds)
        {
            Vector3 min = worldBounds.min;
            Vector3 max = worldBounds.max;
            Vector3[] corners =
            {
                new Vector3(min.x, min.y, min.z),
                new Vector3(min.x, min.y, max.z),
                new Vector3(min.x, max.y, min.z),
                new Vector3(min.x, max.y, max.z),
                new Vector3(max.x, min.y, min.z),
                new Vector3(max.x, min.y, max.z),
                new Vector3(max.x, max.y, min.z),
                new Vector3(max.x, max.y, max.z)
            };

            for (int i = 0; i < corners.Length; i++)
            {
                Vector3 localPoint = transform.InverseTransformPoint(corners[i]);
                if (!initialized)
                {
                    localBounds = new Bounds(localPoint, Vector3.zero);
                    initialized = true;
                }
                else
                {
                    localBounds.Encapsulate(localPoint);
                }
            }
        }

        private Bounds BuildWorldBounds(Bounds localBounds)
        {
            Vector3 localMin = localBounds.min;
            Vector3 localMax = localBounds.max;
            Vector3[] localCorners =
            {
                new Vector3(localMin.x, localMin.y, localMin.z),
                new Vector3(localMin.x, localMin.y, localMax.z),
                new Vector3(localMin.x, localMax.y, localMin.z),
                new Vector3(localMin.x, localMax.y, localMax.z),
                new Vector3(localMax.x, localMin.y, localMin.z),
                new Vector3(localMax.x, localMin.y, localMax.z),
                new Vector3(localMax.x, localMax.y, localMin.z),
                new Vector3(localMax.x, localMax.y, localMax.z)
            };

            Bounds worldBounds = new Bounds(transform.TransformPoint(localCorners[0]), Vector3.zero);
            for (int i = 1; i < localCorners.Length; i++)
            {
                worldBounds.Encapsulate(transform.TransformPoint(localCorners[i]));
            }

            return worldBounds;
        }

        private bool HasStoredBounds()
        {
            return boundsSize.x > 0.001f && boundsSize.y > 0.001f && boundsSize.z > 0.001f;
        }

        private void Warn(bool logWarnings, string message)
        {
            if (logWarnings)
            {
                Debug.LogWarning("[PortalNights][SceneRoot] " + name + ": " + message, this);
            }
        }

        private static bool MatchesAny(string value, string[] searchTerms)
        {
            if (string.IsNullOrEmpty(value) || searchTerms == null)
            {
                return false;
            }

            for (int i = 0; i < searchTerms.Length; i++)
            {
                string searchTerm = searchTerms[i];
                if (!string.IsNullOrEmpty(searchTerm) && value.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
