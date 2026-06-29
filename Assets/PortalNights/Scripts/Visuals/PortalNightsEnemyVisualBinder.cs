using System.Collections;
using UnityEngine;

namespace PortalNights.Visuals
{
    public sealed class PortalNightsEnemyVisualBinder : MonoBehaviour
    {
        private static readonly string[] LegacyVisualChildNames =
        {
            "Body",
            "HeadGlow",
            "LeftClaw",
            "RightClaw",
            "BossBody",
            "SolarWardenVisual",
            "CrimsonBehemothVisual"
        };

        [SerializeField] private Transform visualRoot;
        [SerializeField] private float visualYawOffset;
        [SerializeField] private bool debugAlignment;

        private PortalNightsCharacterVisualAnimator visualAnimator;
        private PortalNightsEnemyVisualKind currentKind = PortalNightsEnemyVisualKind.None;
        private Transform currentVisualInstance;
        private float currentTargetHeight;
        private int currentPlanetIndex;
        private Vector3 previousPosition;
        private Vector3 lockedVisualLocalPosition;
        private Quaternion lockedVisualLocalRotation;
        private Vector3 lockedVisualLocalScale = Vector3.one;
        private bool visualTransformLocked;

        public PortalNightsVisualAlignmentResult LastAlignment { get; private set; }
        public Transform CurrentVisualInstance => currentVisualInstance;
        public float CurrentTargetHeight => currentTargetHeight;

        private void LateUpdate()
        {
            if (visualAnimator == null || currentKind == PortalNightsEnemyVisualKind.None)
            {
                RestoreVisualTransformLock();
                previousPosition = transform.position;
                return;
            }

            Vector3 delta = transform.position - previousPosition;
            delta.y = 0f;
            float speed = Time.deltaTime <= 0f ? 0f : delta.magnitude / Time.deltaTime;
            visualAnimator.SetMoving(speed > 0.05f);
            visualAnimator.SetRunning(speed > 2.8f);
            visualAnimator.SetMoveSpeed(Mathf.InverseLerp(0.05f, 4.5f, speed));
            RestoreVisualTransformLock();
            previousPosition = transform.position;
        }

        public void Bind(PortalNightsEnemyVisualKind kind, PortalNightsEnemyKind enemyKind = PortalNightsEnemyKind.Small)
        {
            Bind(kind, PortalNightsEnemyVisualCatalog.GetTargetHeight(kind, enemyKind));
        }

        public void Bind(PortalNightsEnemyVisualKind kind, float targetHeight)
        {
            Bind(kind, targetHeight, 0);
        }

        public void Bind(PortalNightsEnemyVisualKind kind, float targetHeight, int planetIndex)
        {
            targetHeight = Mathf.Max(0.1f, targetHeight);
            if (kind == PortalNightsEnemyVisualKind.None)
            {
                return;
            }

            if (currentKind == kind && currentVisualInstance != null && Mathf.Abs(currentTargetHeight - targetHeight) <= 0.01f && currentPlanetIndex == planetIndex)
            {
                RegroundCurrentVisual();
                return;
            }

            Transform root = EnsureVisualRoot();
            visualTransformLocked = false;
            ClearChildren(root);
            ClearLegacyVisualChildren();

            GameObject prefab = PortalNightsEnemyVisualCatalog.LoadPrefab(kind);
            if (prefab == null)
            {
                currentKind = PortalNightsEnemyVisualKind.None;
                visualAnimator = null;
                currentVisualInstance = null;
                return;
            }

            GameObject instance = Instantiate(prefab, root);
            instance.name = prefab.name;
            Transform instanceTransform = instance.transform;
            instanceTransform.localPosition = Vector3.zero;
            instanceTransform.localRotation = Quaternion.Euler(0f, visualYawOffset, 0f);
            instanceTransform.localScale = Vector3.one;
            PortalNightsVisualBindingUtility.StripGameplayComponents(instance);
            PortalNightsVisualBindingUtility.DisableRootMotion(instanceTransform);
            currentVisualInstance = instanceTransform;
            currentTargetHeight = targetHeight;
            currentPlanetIndex = planetIndex;
            LastAlignment = PortalNightsVisualBindingUtility.AlignVisualToGameplayBottom(transform, instanceTransform, currentTargetHeight, gameObject.name, debugAlignment, currentPlanetIndex);
            CaptureVisualTransformLock(instanceTransform);
            if (Application.isPlaying)
            {
                StartCoroutine(RealignVisualAfterSpawn(instanceTransform, currentTargetHeight, currentPlanetIndex));
            }

            visualAnimator = instance.GetComponentInChildren<PortalNightsCharacterVisualAnimator>(true);
            currentKind = kind;
            previousPosition = transform.position;
            PortalNightsGroundingValidator.AttachAndValidate(gameObject, instanceTransform, currentPlanetIndex, kind.ToString());
        }

        public void TriggerAttack()
        {
            visualAnimator?.TriggerAttack();
        }

        public void TriggerHit()
        {
            visualAnimator?.TriggerHit();
        }

        public void TriggerDeath()
        {
            visualAnimator?.TriggerDeath();
        }

        public void TriggerSpecial()
        {
            visualAnimator?.TriggerSpecial();
        }

        public void RegroundCurrentVisual()
        {
            if (currentVisualInstance == null || currentKind == PortalNightsEnemyVisualKind.None)
            {
                return;
            }

            visualTransformLocked = false;
            LastAlignment = PortalNightsVisualBindingUtility.AlignVisualToGameplayBottom(transform, currentVisualInstance, currentTargetHeight, gameObject.name, debugAlignment, currentPlanetIndex);
            CaptureVisualTransformLock(currentVisualInstance);
            PortalNightsGroundingValidator.AttachAndValidate(gameObject, currentVisualInstance, currentPlanetIndex, currentKind.ToString());
        }

        private Transform EnsureVisualRoot()
        {
            if (visualRoot != null)
            {
                return visualRoot;
            }

            Transform existing = transform.Find("VisualRoot");
            if (existing != null)
            {
                visualRoot = existing;
                return visualRoot;
            }

            GameObject root = new GameObject("VisualRoot");
            visualRoot = root.transform;
            visualRoot.SetParent(transform, false);
            return visualRoot;
        }

        private void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                PortalNightsVisualBindingUtility.DestroySafe(root.GetChild(i).gameObject);
            }
        }

        private void ClearLegacyVisualChildren()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == visualRoot || !IsLegacyVisualChild(child.name))
                {
                    continue;
                }

                PortalNightsVisualBindingUtility.DestroySafe(child.gameObject);
            }
        }

        private static bool IsLegacyVisualChild(string childName)
        {
            for (int i = 0; i < LegacyVisualChildNames.Length; i++)
            {
                if (childName == LegacyVisualChildNames[i])
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator RealignVisualAfterSpawn(Transform instance, float targetHeight, int planetIndex)
        {
            for (int i = 0; i < 4; i++)
            {
                yield return null;
                if (instance != null && instance == currentVisualInstance)
                {
                    LastAlignment = PortalNightsVisualBindingUtility.AlignVisualToGameplayBottom(transform, instance, targetHeight, gameObject.name, debugAlignment, planetIndex);
                    CaptureVisualTransformLock(instance);
                    PortalNightsGroundingValidator.AttachAndValidate(gameObject, instance, planetIndex, currentKind.ToString());
                }
            }
        }

        private void CaptureVisualTransformLock(Transform instance)
        {
            if (instance == null)
            {
                visualTransformLocked = false;
                return;
            }

            lockedVisualLocalPosition = instance.localPosition;
            lockedVisualLocalRotation = instance.localRotation;
            lockedVisualLocalScale = instance.localScale;
            visualTransformLocked = true;
        }

        private void RestoreVisualTransformLock()
        {
            if (!visualTransformLocked || currentVisualInstance == null)
            {
                return;
            }

            currentVisualInstance.localPosition = lockedVisualLocalPosition;
            currentVisualInstance.localRotation = lockedVisualLocalRotation;
            currentVisualInstance.localScale = lockedVisualLocalScale;
        }
    }
}
