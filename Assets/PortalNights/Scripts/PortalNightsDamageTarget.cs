using UnityEngine;

namespace PortalNights
{
    [RequireComponent(typeof(PortalNightsHealth))]
    public sealed class PortalNightsDamageTarget : MonoBehaviour
    {
        [SerializeField] private PortalNightsDamageTargetKind targetKind = PortalNightsDamageTargetKind.Generic;
        [SerializeField] private string targetId = "Target";
        [SerializeField] private Transform aimPoint;
        [SerializeField] private bool targetable = true;
        [SerializeField] private int priority = 0;

        private PortalNightsHealth health;

        public PortalNightsDamageTargetKind TargetKind => targetKind;
        public string TargetId => targetId;
        public PortalNightsHealth Health => health == null ? health = GetComponent<PortalNightsHealth>() : health;
        public bool IsTargetable => targetable && Health != null && !Health.IsDead;
        public int Priority => priority;
        public Vector3 AimPoint => aimPoint == null ? transform.position + Vector3.up * 1.5f : aimPoint.position;

        private void Awake()
        {
            health = GetComponent<PortalNightsHealth>();
        }

        private void OnEnable()
        {
            PortalNightsGameController.Instance?.RegisterDamageTarget(this);
        }

        private void OnDisable()
        {
            PortalNightsGameController.Instance?.UnregisterDamageTarget(this);
        }

        public void Configure(PortalNightsDamageTargetKind kind, string id, Transform targetAimPoint, int targetPriority)
        {
            targetKind = kind;
            targetId = string.IsNullOrWhiteSpace(id) ? gameObject.name : id;
            aimPoint = targetAimPoint;
            priority = targetPriority;
            health = GetComponent<PortalNightsHealth>();
        }

        public void SetTargetable(bool value)
        {
            targetable = value;
        }
    }
}
