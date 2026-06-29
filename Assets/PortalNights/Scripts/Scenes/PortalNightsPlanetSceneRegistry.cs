using System;
using UnityEngine;

namespace PortalNights.Scenes
{
    [Serializable]
    public struct PortalNightsPlanetSceneDefinition
    {
        public int planetIndex;
        public string sceneName;
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
                expectedRootName = "Planet1_Defense",
                displayNameKey = "planet.1.name",
                loadingHintKey = "planet.1.loadingHint"
            },
            new PortalNightsPlanetSceneDefinition
            {
                planetIndex = 2,
                sceneName = "PortalNightsPlanet2_CrystalMoon",
                expectedRootName = "Planet2_CrystalMoon",
                displayNameKey = "planet.2.name",
                loadingHintKey = "planet.2.loadingHint"
            },
            new PortalNightsPlanetSceneDefinition
            {
                planetIndex = 3,
                sceneName = "PortalNightsPlanet3_AshRelayStation",
                expectedRootName = "Planet3_AshRelayStation",
                displayNameKey = "planet.3.name",
                loadingHintKey = "planet.3.loadingHint"
            },
            new PortalNightsPlanetSceneDefinition
            {
                planetIndex = 4,
                sceneName = "PortalNightsPlanet4_SwarmExpanse",
                expectedRootName = "Planet4_SwarmExpanse",
                displayNameKey = "planet.4.name",
                loadingHintKey = "planet.4.loadingHint"
            },
            new PortalNightsPlanetSceneDefinition
            {
                planetIndex = 5,
                sceneName = "PortalNightsPlanet5_CrimsonSingularity",
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

        public string GetSceneName(int planetIndex)
        {
            return TryGetDefinition(planetIndex, out PortalNightsPlanetSceneDefinition definition)
                ? definition.sceneName
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
            return definitions != null && definitions.Length > 0
                ? definitions
                : DefaultDefinitions;
        }
    }
}
