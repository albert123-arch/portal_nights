using System;
using UnityEngine;

namespace PortalNights.Scenes
{
    [Serializable]
    public struct PortalNightsPlanetSceneDefinition
    {
        public int planetIndex;
        public string sceneName;
        public string scenePath;
        public string expectedRootName;
        public string displayNameKey;
        public string loadingHintKey;
    }

    [DisallowMultipleComponent]
    public sealed class PortalNightsPlanetSceneRegistry : MonoBehaviour
    {
        [SerializeField] private PortalNightsPlanetSceneDefinition[] definitions;

        private static readonly PortalNightsPlanetSceneDefinition[] DefaultDefinitions =
        {
            new PortalNightsPlanetSceneDefinition
            {
                planetIndex = 1,
                sceneName = "PortalNightsPlanet1_Defense",
                scenePath = "Assets/PortalNights/Scenes/Planets/PortalNightsPlanet1_Defense.unity",
                expectedRootName = "Planet1_Defense",
                displayNameKey = "planet.1.name",
                loadingHintKey = "planet.1.loadingHint"
            },
            new PortalNightsPlanetSceneDefinition
            {
                planetIndex = 2,
                sceneName = "PortalNightsPlanet2_CrystalMoon",
                scenePath = "Assets/PortalNights/Scenes/Planets/PortalNightsPlanet2_CrystalMoon.unity",
                expectedRootName = "Planet2_CrystalMoon",
                displayNameKey = "planet.2.name",
                loadingHintKey = "planet.2.loadingHint"
            },
            new PortalNightsPlanetSceneDefinition
            {
                planetIndex = 3,
                sceneName = "PortalNightsPlanet3_AshRelayStation",
                scenePath = "Assets/PortalNights/Scenes/Planets/PortalNightsPlanet3_AshRelayStation.unity",
                expectedRootName = "Planet3_AshRelayStation",
                displayNameKey = "planet.3.name",
                loadingHintKey = "planet.3.loadingHint"
            },
            new PortalNightsPlanetSceneDefinition
            {
                planetIndex = 4,
                sceneName = "PortalNightsPlanet4_SwarmExpanse",
                scenePath = "Assets/PortalNights/Scenes/Planets/PortalNightsPlanet4_SwarmExpanse.unity",
                expectedRootName = "Planet4_SwarmExpanse",
                displayNameKey = "planet.4.name",
                loadingHintKey = "planet.4.loadingHint"
            },
            new PortalNightsPlanetSceneDefinition
            {
                planetIndex = 5,
                sceneName = "PortalNightsPlanet5_CrimsonSingularity",
                scenePath = "Assets/PortalNights/Scenes/Planets/PortalNightsPlanet5_CrimsonSingularity.unity",
                expectedRootName = "Planet5_CrimsonSingularity",
                displayNameKey = "planet.5.name",
                loadingHintKey = "planet.5.loadingHint"
            }
        };

        public bool TryGetDefinition(int planetIndex, out PortalNightsPlanetSceneDefinition definition)
        {
            PortalNightsPlanetSceneDefinition[] activeDefinitions = GetActiveDefinitions();
            for (int i = 0; i < activeDefinitions.Length; i++)
            {
                if (activeDefinitions[i].planetIndex == planetIndex)
                {
                    definition = activeDefinitions[i];
                    return true;
                }
            }

            definition = default;
            return false;
        }

        public bool TryGetDefinitionBySceneName(string sceneName, out PortalNightsPlanetSceneDefinition definition)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                definition = default;
                return false;
            }

            PortalNightsPlanetSceneDefinition[] activeDefinitions = GetActiveDefinitions();
            for (int i = 0; i < activeDefinitions.Length; i++)
            {
                if (string.Equals(activeDefinitions[i].sceneName, sceneName, StringComparison.Ordinal))
                {
                    definition = activeDefinitions[i];
                    return true;
                }
            }

            definition = default;
            return false;
        }

        public string GetSceneName(int planetIndex)
        {
            return TryGetDefinition(planetIndex, out PortalNightsPlanetSceneDefinition definition)
                ? definition.sceneName
                : string.Empty;
        }

        public string GetScenePath(int planetIndex)
        {
            return TryGetDefinition(planetIndex, out PortalNightsPlanetSceneDefinition definition)
                ? definition.scenePath
                : string.Empty;
        }

        public string GetExpectedRootName(int planetIndex)
        {
            return TryGetDefinition(planetIndex, out PortalNightsPlanetSceneDefinition definition)
                ? definition.expectedRootName
                : string.Empty;
        }

        public bool IsValidPlanetIndex(int planetIndex)
        {
            return TryGetDefinition(planetIndex, out _);
        }

        public PortalNightsPlanetSceneDefinition[] GetDefinitionsCopy()
        {
            PortalNightsPlanetSceneDefinition[] source = GetActiveDefinitions();
            PortalNightsPlanetSceneDefinition[] copy = new PortalNightsPlanetSceneDefinition[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }

        private PortalNightsPlanetSceneDefinition[] GetActiveDefinitions()
        {
            PortalNightsPlanetSceneDefinition[] source = definitions != null && definitions.Length > 0
                ? definitions
                : DefaultDefinitions;

            PortalNightsPlanetSceneDefinition[] resolved = new PortalNightsPlanetSceneDefinition[source.Length];
            for (int i = 0; i < source.Length; i++)
            {
                resolved[i] = MergeWithDefaults(source[i]);
            }

            return resolved;
        }

        private static PortalNightsPlanetSceneDefinition MergeWithDefaults(PortalNightsPlanetSceneDefinition definition)
        {
            if (!TryGetDefaultDefinition(definition.planetIndex, out PortalNightsPlanetSceneDefinition fallback))
            {
                return definition;
            }

            if (string.IsNullOrWhiteSpace(definition.sceneName))
            {
                definition.sceneName = fallback.sceneName;
            }

            if (string.IsNullOrWhiteSpace(definition.scenePath))
            {
                definition.scenePath = fallback.scenePath;
            }

            if (string.IsNullOrWhiteSpace(definition.expectedRootName))
            {
                definition.expectedRootName = fallback.expectedRootName;
            }

            if (string.IsNullOrWhiteSpace(definition.displayNameKey))
            {
                definition.displayNameKey = fallback.displayNameKey;
            }

            if (string.IsNullOrWhiteSpace(definition.loadingHintKey))
            {
                definition.loadingHintKey = fallback.loadingHintKey;
            }

            return definition;
        }

        private static bool TryGetDefaultDefinition(int planetIndex, out PortalNightsPlanetSceneDefinition definition)
        {
            for (int i = 0; i < DefaultDefinitions.Length; i++)
            {
                if (DefaultDefinitions[i].planetIndex == planetIndex)
                {
                    definition = DefaultDefinitions[i];
                    return true;
                }
            }

            definition = default;
            return false;
        }
    }
}
