using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PortalNights.Scenes
{
    [DisallowMultipleComponent]
    public sealed class PortalNightsSceneTransitionManager : MonoBehaviour
    {
        [SerializeField] private PortalNightsPlanetSceneRegistry registry;
        [SerializeField] private bool debugLogs;
        [SerializeField] private bool useAdditiveLoading = true;

        private string currentLoadedPlanetSceneName;
        private int currentLoadedPlanetIndex = -1;
        private PortalNightsPlanetSceneRoot currentPlanetRoot;
        private bool isTransitioning;

        public static PortalNightsSceneTransitionManager Instance { get; private set; }

        public bool IsTransitioning => isTransitioning;
        public int CurrentLoadedPlanetIndex => currentLoadedPlanetIndex;
        public PortalNightsPlanetSceneRoot CurrentPlanetRoot => currentPlanetRoot;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            Instance = this;
            if (registry == null)
            {
                registry = GetComponent<PortalNightsPlanetSceneRegistry>();
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public IEnumerator LoadPlanetAsync(int planetIndex)
        {
            if (isTransitioning)
            {
                DebugLog("Ignoring load request because a scene transition is already in progress.");
                yield break;
            }

            isTransitioning = true;

            if (registry == null)
            {
                registry = GetComponent<PortalNightsPlanetSceneRegistry>();
            }

            if (registry == null)
            {
                Debug.LogWarning("[PortalNights][SceneTransition] Missing PortalNightsPlanetSceneRegistry reference.", this);
                isTransitioning = false;
                yield break;
            }

            if (!registry.TryGetDefinition(planetIndex, out PortalNightsPlanetSceneDefinition definition))
            {
                Debug.LogWarning("[PortalNights][SceneTransition] No scene definition exists for planet index " + planetIndex + ".", this);
                isTransitioning = false;
                yield break;
            }

            if (!Application.CanStreamedLevelBeLoaded(definition.sceneName))
            {
                Debug.LogWarning("[PortalNights][SceneTransition] Scene cannot be loaded yet: " + definition.sceneName + ".", this);
                isTransitioning = false;
                yield break;
            }

            if (!string.IsNullOrEmpty(currentLoadedPlanetSceneName))
            {
                if (string.Equals(currentLoadedPlanetSceneName, definition.sceneName, StringComparison.Ordinal))
                {
                    DebugLog("Scene '" + definition.sceneName + "' is already tracked as the current additive planet.");
                    isTransitioning = false;
                    yield break;
                }

                yield return UnloadTrackedPlanetSceneAsync();
            }

            yield return FadeOutIfAvailable();

            if (!useAdditiveLoading)
            {
                DebugLog("useAdditiveLoading is disabled, but Phase 2 intentionally forces additive loading only.");
            }

            AsyncOperation loadOperation = SceneManager.LoadSceneAsync(definition.sceneName, LoadSceneMode.Additive);
            if (loadOperation == null)
            {
                Debug.LogWarning("[PortalNights][SceneTransition] Failed to start additive load for '" + definition.sceneName + "'.", this);
                yield return FadeInIfAvailable();
                isTransitioning = false;
                yield break;
            }

            while (!loadOperation.isDone)
            {
                yield return null;
            }

            Scene loadedScene = SceneManager.GetSceneByName(definition.sceneName);
            if (!loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                Debug.LogWarning("[PortalNights][SceneTransition] Loaded scene handle is invalid for '" + definition.sceneName + "'.", this);
                yield return FadeInIfAvailable();
                isTransitioning = false;
                yield break;
            }

            PortalNightsPlanetSceneRoot loadedRoot = FindPlanetSceneRootInScene(loadedScene, planetIndex, definition.expectedRootName);
            if (loadedRoot == null)
            {
                Debug.LogWarning("[PortalNights][SceneTransition] No PortalNightsPlanetSceneRoot was found inside additive scene '" + definition.sceneName + "'.", this);
                AsyncOperation cleanupOperation = SceneManager.UnloadSceneAsync(loadedScene);
                while (cleanupOperation != null && !cleanupOperation.isDone)
                {
                    yield return null;
                }

                yield return FadeInIfAvailable();
                isTransitioning = false;
                yield break;
            }

            currentLoadedPlanetSceneName = definition.sceneName;
            currentLoadedPlanetIndex = planetIndex;
            currentPlanetRoot = loadedRoot;

            // Phase 2 intentionally stops at discovery only.
            // Scene-placed NetworkObjects inside additive planet scenes are a Phase 5 risk and
            // must be handled with explicit Netcode scene management instead of ad hoc Spawn calls.

            yield return FadeInIfAvailable();
            isTransitioning = false;
        }

        public IEnumerator UnloadCurrentPlanetAsync()
        {
            if (isTransitioning)
            {
                DebugLog("Ignoring unload request because a scene transition is already in progress.");
                yield break;
            }

            if (string.IsNullOrEmpty(currentLoadedPlanetSceneName))
            {
                yield break;
            }

            isTransitioning = true;
            yield return FadeOutIfAvailable();
            yield return UnloadTrackedPlanetSceneAsync();
            yield return FadeInIfAvailable();
            isTransitioning = false;
        }

        public bool TryGetCurrentPlanetRoot(out PortalNightsPlanetSceneRoot root)
        {
            root = currentPlanetRoot;
            return root != null;
        }

        public PortalNightsPlanetSceneRoot FindPlanetSceneRootInScene(Scene scene, int planetIndex, string expectedRootName)
        {
            if (!scene.IsValid() || !scene.isLoaded)
            {
                return null;
            }

            GameObject[] sceneRoots = scene.GetRootGameObjects();
            for (int i = 0; i < sceneRoots.Length; i++)
            {
                GameObject candidateRoot = sceneRoots[i];
                if (!string.IsNullOrEmpty(expectedRootName)
                    && string.Equals(candidateRoot.name, expectedRootName, StringComparison.Ordinal))
                {
                    PortalNightsPlanetSceneRoot exactRoot = candidateRoot.GetComponent<PortalNightsPlanetSceneRoot>();
                    if (exactRoot != null)
                    {
                        return exactRoot;
                    }

                    PortalNightsPlanetSceneRoot nestedExactRoot = candidateRoot.GetComponentInChildren<PortalNightsPlanetSceneRoot>(true);
                    if (nestedExactRoot != null)
                    {
                        return nestedExactRoot;
                    }
                }
            }

            for (int i = 0; i < sceneRoots.Length; i++)
            {
                PortalNightsPlanetSceneRoot[] roots = sceneRoots[i].GetComponentsInChildren<PortalNightsPlanetSceneRoot>(true);
                for (int j = 0; j < roots.Length; j++)
                {
                    if (roots[j] != null && roots[j].PlanetIndex == planetIndex)
                    {
                        return roots[j];
                    }
                }
            }

            for (int i = 0; i < sceneRoots.Length; i++)
            {
                PortalNightsPlanetSceneRoot fallbackRoot = sceneRoots[i].GetComponentInChildren<PortalNightsPlanetSceneRoot>(true);
                if (fallbackRoot != null)
                {
                    return fallbackRoot;
                }
            }

            return null;
        }

        private IEnumerator FadeOutIfAvailable()
        {
            // TODO: Future phases can softly integrate PortalNightsPlanetTransitionDirector here.
            // Phase 2 keeps the additive loader dormant and avoids touching current transition UI flow.
            yield break;
        }

        private IEnumerator FadeInIfAvailable()
        {
            // TODO: Future phases can softly integrate PortalNightsPlanetTransitionDirector here.
            // Phase 2 keeps the additive loader dormant and avoids touching current transition UI flow.
            yield break;
        }

        private IEnumerator UnloadTrackedPlanetSceneAsync()
        {
            if (string.IsNullOrEmpty(currentLoadedPlanetSceneName))
            {
                yield break;
            }

            string sceneNameToUnload = currentLoadedPlanetSceneName;
            if (string.Equals(sceneNameToUnload, "PortalNightsArena", StringComparison.Ordinal))
            {
                Debug.LogWarning("[PortalNights][SceneTransition] Refusing to unload PortalNightsArena from the dormant transition manager.", this);
                ClearTrackedPlanetState();
                yield break;
            }

            Scene sceneToUnload = SceneManager.GetSceneByName(sceneNameToUnload);
            if (sceneToUnload.IsValid() && sceneToUnload.isLoaded)
            {
                AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(sceneToUnload);
                while (unloadOperation != null && !unloadOperation.isDone)
                {
                    yield return null;
                }
            }

            ClearTrackedPlanetState();
        }

        private void ClearTrackedPlanetState()
        {
            currentLoadedPlanetSceneName = string.Empty;
            currentLoadedPlanetIndex = -1;
            currentPlanetRoot = null;
        }

        private void DebugLog(string message)
        {
            if (debugLogs)
            {
                Debug.Log("[PortalNights][SceneTransition] " + message, this);
            }
        }
    }
}
