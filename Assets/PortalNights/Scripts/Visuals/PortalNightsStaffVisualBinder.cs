using System.Collections;
using UnityEngine;

namespace PortalNights.Visuals
{
    public sealed class PortalNightsStaffVisualBinder : MonoBehaviour
    {
        private const string StaffResourcePath = "PortalNightsStaffVisuals/PN_Visual_Staff_Ch32";
        private const float StaffTargetHeight = 1.77f;

        private static GameObject cachedStaffPrefab;
        private static bool missingLogged;

        [SerializeField] private Transform visualRoot;
        [SerializeField] private float visualYawOffset;
        [SerializeField] private bool debugAlignment;

        private PortalNightsCharacterVisualAnimator visualAnimator;
        private Transform currentVisualInstance;
        private bool bound;
        private Vector3 previousPosition;
        private Vector3 lockedVisualLocalPosition;
        private Quaternion lockedVisualLocalRotation;
        private Vector3 lockedVisualLocalScale = Vector3.one;
        private bool visualTransformLocked;

        public PortalNightsVisualAlignmentResult LastAlignment { get; private set; }
        public Transform CurrentVisualInstance => currentVisualInstance;

        private void LateUpdate()
        {
            if (!bound || visualAnimator == null)
            {
                RestoreVisualTransformLock();
                previousPosition = transform.position;
                return;
            }

            Vector3 delta = transform.position - previousPosition;
            delta.y = 0f;
            float speed = Time.deltaTime <= 0f ? 0f : delta.magnitude / Time.deltaTime;
            visualAnimator.SetMoving(speed > 0.04f);
            visualAnimator.SetRunning(speed > 5.2f);
            visualAnimator.SetMoveSpeed(Mathf.InverseLerp(0.04f, 7.5f, speed));
            RestoreVisualTransformLock();
            previousPosition = transform.position;
        }

        public void BindCh32()
        {
            if (bound)
            {
                return;
            }

            Transform root = EnsureVisualRoot();
            visualTransformLocked = false;
            ClearChildren(root);
            ClearLegacyPlaceholderVisuals();

            GameObject prefab = LoadStaffPrefab();
            if (prefab == null)
            {
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
            LastAlignment = PortalNightsVisualBindingUtility.AlignVisualToGameplayBottom(transform, instanceTransform, StaffTargetHeight, gameObject.name, debugAlignment, 3);
            CaptureVisualTransformLock(instanceTransform);
            if (Application.isPlaying)
            {
                StartCoroutine(RealignVisualNextFrame(instanceTransform));
            }

            visualAnimator = instance.GetComponentInChildren<PortalNightsCharacterVisualAnimator>(true);
            bound = true;
            previousPosition = transform.position;
            PortalNightsGroundingValidator.AttachAndValidate(gameObject, instanceTransform, 3, "Staff_Ch32");
        }

        public void SetStaffState(PortalNightsStaffState state)
        {
            if (visualAnimator == null)
            {
                return;
            }

            bool movingState = state == PortalNightsStaffState.Following;
            if (!movingState)
            {
                visualAnimator.SetMoving(false);
                visualAnimator.SetRunning(false);
                visualAnimator.SetMoveSpeed(0f);
            }
        }

        public void Reground()
        {
            if (currentVisualInstance == null)
            {
                return;
            }

            visualTransformLocked = false;
            LastAlignment = PortalNightsVisualBindingUtility.AlignVisualToGameplayBottom(transform, currentVisualInstance, StaffTargetHeight, gameObject.name, debugAlignment, 3);
            CaptureVisualTransformLock(currentVisualInstance);
            PortalNightsGroundingValidator.AttachAndValidate(gameObject, currentVisualInstance, 3, "Staff_Ch32");
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

        private void ClearLegacyPlaceholderVisuals()
        {
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                Transform child = transform.GetChild(i);
                if (child == visualRoot || child.name != "Staff_Body")
                {
                    continue;
                }

                PortalNightsVisualBindingUtility.DestroySafe(child.gameObject);
            }
        }

        private static GameObject LoadStaffPrefab()
        {
            if (cachedStaffPrefab != null)
            {
                return cachedStaffPrefab;
            }

            cachedStaffPrefab = Resources.Load<GameObject>(StaffResourcePath);
            if (cachedStaffPrefab == null && !missingLogged)
            {
                missingLogged = true;
                Debug.LogError("[PortalNights] Ch32 staff visual prefab failed to load from Resources path " + StaffResourcePath + ".");
            }

            return cachedStaffPrefab;
        }

        private IEnumerator RealignVisualNextFrame(Transform instance)
        {
            yield return null;
            if (instance != null && instance == currentVisualInstance)
            {
                LastAlignment = PortalNightsVisualBindingUtility.AlignVisualToGameplayBottom(transform, instance, StaffTargetHeight, gameObject.name, debugAlignment, 3);
                CaptureVisualTransformLock(instance);
                PortalNightsGroundingValidator.AttachAndValidate(gameObject, instance, 3, "Staff_Ch32");
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
