using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PortalNights.Scenes
{
    [DisallowMultipleComponent]
    public sealed class PortalNightsSceneTransitionManager : MonoBehaviour
    {
        private const string LegacyArenaSceneName = "PortalNightsArena";
        private const string CoreSceneName = "PortalNightsCore";

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

        public bool IsPlanetLoaded(int planetIndex)
        {
            if (!TryGetRegistry(out PortalNightsPlanetSceneRegistry activeRegistry))
            {
                return false;
            }

            if (!activeRegistry.TryGetDefinition(planetIndex, out PortalNightsPlanetSceneDefinition definition))
            {
                return false;
            }

            return IsSceneLoaded(definition.sceneName);
        }

        public bool IsSceneLoaded(string sceneName)
        {
            if (string.IsNullOrWhiteSpace(sceneName))
            {
                return false;
            }

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid() && scene.isLoaded && string.Equals(scene.name, sceneName, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public IEnumerator LoadPlanetAsync(int planetIndex)
        {
            if (isTransitioning)
            {
                DebugLog("Ignoring load request because a scene transition is already in progress.");
                yield break;
            }

            if (!TryGetRegistry(out PortalNightsPlanetSceneRegistry activeRegistry))
            {
                yield break;
            }

            if (!activeRegistry.TryGetDefinition(planetIndex, out PortalNightsPlanetSceneDefinition definition))
            {
                Debug.LogWarning("[PortalNights][SceneTransition] Invalid planet index " + planetIndex + ". No scene definition exists.", this);
                yield break;
            }

            if (currentLoadedPlanetIndex == planetIndex || string.Equals(currentLoadedPlanetSceneName, definition.sceneName, StringComparison.Ordinal))
            {
                DebugLog("Ignoring duplicate load request for planet " + planetIndex + " / scene '" + definition.sceneName + "'.");
                yield break;
            }

            if (IsSceneLoaded(definition.sceneName))
            {
                Debug.LogWarning("[PortalNights][SceneTransition] Scene '" + definition.sceneName + "' is already loaded. Refusing duplicate additive load.", this);
                yield break;
            }

            if (!Application.CanStreamedLevelBeLoaded(definition.sceneName))
            {
                Debug.LogWarning("[PortalNights][SceneTransition] Scene '" + definition.sceneName + "' is not registered in Build Settings and cannot be loaded additively. Expected path: " + definition.scenePath, this);
                yield break;
            }

            isTransitioning = true;
            try
            {
                if (!string.IsNullOrEmpty(currentLoadedPlanetSceneName))
                {
                    yield return UnloadTrackedPlanetSceneAsync();
                }

                yield return FadeOutIfAvailable();

                if (!useAdditiveLoading)
                {
                    DebugLog("useAdditiveLoading is disabled in the inspector, but the dormant transition manager still forces additive loads for Phase 5A validation safety.");
                }

                AsyncOperation loadOperation = SceneManager.LoadSceneAsync(definition.sceneName, LoadSceneMode.Additive);
                if (loadOperation == null)
                {
                    Debug.LogWarning("[PortalNights][SceneTransition] Failed to start additive load for '" + definition.sceneName + "'.", this);
                    yield return FadeInIfAvailable();
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
                    yield break;
                }

                PortalNightsPlanetSceneRoot loadedRoot = FindPlanetSceneRootInScene(loadedScene, planetIndex, definition.expectedRootName);
                if (!IsValidLoadedPlanetRoot(loadedScene, loadedRoot, definition))
                {
                    Debug.LogWarning("[PortalNights][SceneTransition] Loaded scene '" + definition.sceneName + "' did not contain a valid PortalNightsPlanetSceneRoot matching planet " + planetIndex + " and root '" + definition.expectedRootName + "'.", this);
                    yield return UnloadSceneByHandleAsync(loadedScene);
                    yield return FadeInIfAvailable();
                    yield break;
                }

                currentLoadedPlanetSceneName = definition.sceneName;
                currentLoadedPlanetIndex = planetIndex;
                currentPlanetRoot = loadedRoot;

                yield return FadeInIfAvailable();
            }
            finally
            {
                isTransitioning = false;
            }
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
            try
            {
                yield return FadeOutIfAvailable();
                yield return UnloadTrackedPlanetSceneAsync();
                yield return FadeInIfAvailable();
            }
            finally
            {
                isTransitioning = false;
            }
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

            return null;
        }

        private bool TryGetRegistry(out PortalNightsPlanetSceneRegistry activeRegistry)
        {
            activeRegistry = registry;
            if (activeRegistry == null)
            {
                activeRegistry = GetComponent<PortalNightsPlanetSceneRegistry>();
                registry = activeRegistry;
            }

            if (activeRegistry == null)
            {
                Debug.LogWarning("[PortalNights][SceneTransition] Missing PortalNightsPlanetSceneRegistry reference.", this);
                return false;
            }

            return true;
        }

        private static bool IsValidLoadedPlanetRoot(Scene loadedScene, PortalNightsPlanetSceneRoot loadedRoot, PortalNightsPlanetSceneDefinition definition)
        {
            if (loadedRoot == null || !loadedScene.IsValid() || !loadedScene.isLoaded)
            {
                return false;
            }

            if (loadedRoot.gameObject.scene != loadedScene)
            {
                return false;
            }

            if (loadedRoot.PlanetIndex != definition.planetIndex)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(definition.expectedRootName)
                && !string.Equals(loadedRoot.gameObject.name, definition.expectedRootName, StringComparison.Ordinal))
            {
                return false;
            }

            return true;
        }

        private IEnumerator FadeOutIfAvailable()
        {
            yield break;
        }

        private IEnumerator FadeInIfAvailable()
        {
            yield break;
        }

        private IEnumerator UnloadTrackedPlanetSceneAsync()
        {
            if (string.IsNullOrEmpty(currentLoadedPlanetSceneName))
            {
                yield break;
            }

            if (IsProtectedSceneName(currentLoadedPlanetSceneName))
            {
                Debug.LogWarning("[PortalNights][SceneTransition] Refusing to unload protected scene '" + currentLoadedPlanetSceneName + "'. Clearing stale tracked state only.", this);
                ClearTrackedPlanetState();
                yield break;
            }

            Scene sceneToUnload = SceneManager.GetSceneByName(currentLoadedPlanetSceneName);
            yield return UnloadSceneByHandleAsync(sceneToUnload);
            ClearTrackedPlanetState();
        }

        private IEnumerator UnloadSceneByHandleAsync(Scene sceneToUnload)
        {
            if (!sceneToUnload.IsValid() || !sceneToUnload.isLoaded)
            {
                yield break;
            }

            if (IsProtectedSceneName(sceneToUnload.name))
            {
                Debug.LogWarning("[PortalNights][SceneTransition] Refusing to unload protected scene '" + sceneToUnload.name + "'.", this);
                yield break;
            }

            AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(sceneToUnload);
            while (unloadOperation != null && !unloadOperation.isDone)
            {
                yield return null;
            }
        }

        private static bool IsProtectedSceneName(string sceneName)
        {
            return string.Equals(sceneName, LegacyArenaSceneName, StringComparison.Ordinal)
                || string.Equals(sceneName, CoreSceneName, StringComparison.Ordinal);
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
