using System.Collections;
using PortalNights;
using UnityEngine;

namespace PortalNights.Scenes
{
    [DisallowMultipleComponent]
    public sealed class PortalNightsSceneModeBootstrap : MonoBehaviour
    {
        [SerializeField] private bool enableSceneMode = false;
        [SerializeField] private int initialPlanetIndex = 1;
        [SerializeField] private PortalNightsSceneTransitionManager transitionManager;
        [SerializeField] private PortalNightsPlanetSceneRegistry registry;
        [SerializeField] private PortalNightsGameController gameController;
        [SerializeField] private bool debugLogs = true;

        private bool hasBootstrapped;
        private PortalNightsPlanetSceneRoot loadedInitialPlanetRoot;

        public bool EnableSceneMode => enableSceneMode;
        public int InitialPlanetIndex => initialPlanetIndex;
        public bool HasBootstrapped => hasBootstrapped;
        public PortalNightsPlanetSceneRoot LoadedInitialPlanetRoot => loadedInitialPlanetRoot;

        private IEnumerator Start()
        {
            if (!enableSceneMode)
            {
                DebugLog("Experimental scene mode bootstrap is disabled. Legacy PortalNightsArena flow remains active.");
                yield break;
            }

            transitionManager = ResolveReference(transitionManager);
            registry = ResolveReference(registry);
            gameController = ResolveReference(gameController);

            if (transitionManager == null)
            {
                Debug.LogWarning("[PortalNights][SceneModeBootstrap] Missing PortalNightsSceneTransitionManager. Experimental scene mode bootstrap cannot continue.", this);
                yield break;
            }

            if (registry == null)
            {
                Debug.LogWarning("[PortalNights][SceneModeBootstrap] Missing PortalNightsPlanetSceneRegistry. Experimental scene mode bootstrap cannot continue.", this);
                yield break;
            }

            if (!registry.IsValidPlanetIndex(initialPlanetIndex))
            {
                Debug.LogWarning("[PortalNights][SceneModeBootstrap] Invalid initial planet index " + initialPlanetIndex + ". Experimental scene mode bootstrap cannot continue.", this);
                yield break;
            }

            DebugLog("Experimental scene mode bootstrap enabled. Requesting additive load for planet " + initialPlanetIndex + " without teleporting, unloading, or gameplay rewiring.");
            yield return transitionManager.LoadPlanetAsync(initialPlanetIndex);

            if (!transitionManager.TryGetCurrentPlanetRoot(out loadedInitialPlanetRoot) || loadedInitialPlanetRoot == null)
            {
                Debug.LogWarning("[PortalNights][SceneModeBootstrap] Experimental scene mode bootstrap did not receive a loaded planet root after requesting planet " + initialPlanetIndex + ".", this);
                yield break;
            }

            hasBootstrapped = true;
            DebugLog("Experimental scene mode bootstrap loaded planet root '" + loadedInitialPlanetRoot.name + "'. Gameplay remains on legacy logic until a future phase binds scene-root references.");
        }

        private T ResolveReference<T>(T existing) where T : Component
        {
            if (existing != null)
            {
                return existing;
            }

            T local = GetComponent<T>();
            if (local != null)
            {
                return local;
            }

            return Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        }

        private void DebugLog(string message)
        {
            if (debugLogs)
            {
                Debug.Log("[PortalNights][SceneModeBootstrap] " + message, this);
            }
        }
    }
}
