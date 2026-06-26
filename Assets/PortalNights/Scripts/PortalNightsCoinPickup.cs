using UnityEngine;
using Unity.Netcode;

namespace PortalNights
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class PortalNightsCoinPickup : NetworkBehaviour
    {
        [SerializeField] private float lifeTime = 2.2f;
        [SerializeField] private float bobHeight = 0.18f;
        [SerializeField] private float rotationSpeed = 190f;

        private readonly NetworkVariable<int> value = new NetworkVariable<int>(
            1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private Vector3 startPosition;
        private float timer;

        private void Awake()
        {
            startPosition = transform.position;
            timer = lifeTime;
        }

        public override void OnNetworkSpawn()
        {
            startPosition = transform.position;
            timer = lifeTime;
            PortalNightsVfx.SpawnBurst(transform.position, new Color(1f, 0.78f, 0.18f, 1f), 0.45f, 8);
        }

        private void Update()
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime, Space.World);
            transform.position = startPosition + Vector3.up * (Mathf.Sin(Time.time * 6f) * bobHeight);

            if (!IsServer)
            {
                return;
            }

            timer -= Time.deltaTime;
            if (timer <= 0f)
            {
                NetworkObject networkObject = GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                {
                    networkObject.Despawn(true);
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }

        public void InitializeServer(int amount)
        {
            if (!PortalNightsNet.ServerCanWrite(this))
            {
                return;
            }

            value.Value = Mathf.Max(1, amount);
        }
    }
}
