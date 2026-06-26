using UnityEngine;

namespace PortalNights
{
    public sealed class PortalNightsPlanet5HealingSphere : MonoBehaviour
    {
        [SerializeField] private Transform corruptedState;
        [SerializeField] private Transform damagedCoreState;
        [SerializeField] private Transform restoredState;

        public PortalNightsSphereVisualState CurrentState { get; private set; } = PortalNightsSphereVisualState.Corrupted;

        private void Awake()
        {
            CacheStates();
            SetVisualState(CurrentState);
        }

        public void SetVisualState(PortalNightsSphereVisualState state)
        {
            CurrentState = state;
            CacheStates();
            if (corruptedState != null)
            {
                corruptedState.gameObject.SetActive(state == PortalNightsSphereVisualState.Corrupted);
            }

            if (damagedCoreState != null)
            {
                damagedCoreState.gameObject.SetActive(state == PortalNightsSphereVisualState.DamagedCore);
            }

            if (restoredState != null)
            {
                restoredState.gameObject.SetActive(state == PortalNightsSphereVisualState.Restored);
            }
        }

        private void CacheStates()
        {
            if (corruptedState == null)
            {
                corruptedState = transform.Find("VisualState_Corrupted");
            }

            if (damagedCoreState == null)
            {
                damagedCoreState = transform.Find("VisualState_DamagedCore");
            }

            if (restoredState == null)
            {
                restoredState = transform.Find("VisualState_Restored");
            }
        }
    }
}
