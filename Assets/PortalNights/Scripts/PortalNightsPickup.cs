using UnityEngine;

namespace PortalNights
{
    public sealed class PortalNightsPickup : MonoBehaviour
    {
        [SerializeField] private PortalNightsPickupKind kind;
        [SerializeField] private int coinAmount = 25;
        [SerializeField] private float lifeTime = 18f;
        [SerializeField] private float collectRadius = 1.65f;
        [SerializeField] private float bobHeight = 0.18f;
        [SerializeField] private float rotationSpeed = 135f;
        [SerializeField] private float collectionCheckInterval = 0.15f;

        private Vector3 startPosition;
        private bool collected;
        private float collectionCheckTimer;

        public void Configure(PortalNightsPickupKind pickupKind, int coins)
        {
            kind = pickupKind;
            coinAmount = Mathf.Max(1, coins);
            startPosition = transform.position;
        }

        private void Awake()
        {
            startPosition = transform.position;
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * 5.5f) * bobHeight);

            lifeTime -= Time.deltaTime;
            if (lifeTime <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            if (collected)
            {
                return;
            }

            collectionCheckTimer -= Time.deltaTime;
            if (collectionCheckTimer > 0f)
            {
                return;
            }

            collectionCheckTimer = Mathf.Max(0.12f, collectionCheckInterval);
            PortalNightsPlayerController player = PortalNightsGameController.Instance == null
                ? null
                : PortalNightsGameController.Instance.GetClosestLivingPlayer(transform.position, collectRadius);
            if (player == null)
            {
                return;
            }

            collected = true;
            PortalNightsGameController.Instance.ApplyPickupServer(kind, player, coinAmount, transform.position);
            Destroy(gameObject);
        }
    }
}
