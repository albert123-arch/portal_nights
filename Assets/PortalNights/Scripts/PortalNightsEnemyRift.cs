using Unity.Netcode;
using UnityEngine;

namespace PortalNights
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class PortalNightsEnemyRift : NetworkBehaviour
    {
        [SerializeField] private string riftId = "Rift";
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private Renderer surfaceRenderer;
        [SerializeField] private Light riftLight;

        private readonly NetworkVariable<int> state = new NetworkVariable<int>(
            (int)PortalNightsRiftState.Dormant,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        public string RiftId => riftId;
        public PortalNightsRiftState State => (PortalNightsRiftState)state.Value;
        public Transform SpawnPoint => spawnPoint == null ? transform : spawnPoint;

        private void Awake()
        {
            RefreshVisual();
        }

        private void OnEnable()
        {
            PortalNightsGameController.Instance?.RegisterPlanet3Rift(this);
        }

        private void OnDisable()
        {
            PortalNightsGameController.Instance?.UnregisterPlanet3Rift(this);
        }

        public override void OnNetworkSpawn()
        {
            state.OnValueChanged += HandleStateChanged;
            RefreshVisual();
        }

        public override void OnNetworkDespawn()
        {
            state.OnValueChanged -= HandleStateChanged;
        }

        public void Configure(string id, Transform spawn, Renderer surface, Light light)
        {
            riftId = id;
            spawnPoint = spawn;
            surfaceRenderer = surface;
            riftLight = light;
            RefreshVisual();
        }

        public void SetStateServer(PortalNightsRiftState newState)
        {
            if (!PortalNightsNet.ServerCanWrite(this))
            {
                return;
            }

            state.Value = (int)newState;
            RefreshVisual();
        }

        private void HandleStateChanged(int previous, int current)
        {
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            Color color = GetStateColor(State);
            if (surfaceRenderer != null)
            {
                Material material = Application.isPlaying ? surfaceRenderer.material : surfaceRenderer.sharedMaterial;
                if (material != null)
                {
                    if (material.HasProperty("_BaseColor"))
                    {
                        material.SetColor("_BaseColor", color);
                    }

                    if (material.HasProperty("_Color"))
                    {
                        material.color = color;
                    }

                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", color * (State == PortalNightsRiftState.Dormant ? 0.9f : 4.2f));
                    }
                }
            }

            if (riftLight != null)
            {
                riftLight.color = color;
                riftLight.intensity = State == PortalNightsRiftState.Dormant ? 1.2f : State == PortalNightsRiftState.Closing ? 2.2f : 7.5f;
            }
        }

        private static Color GetStateColor(PortalNightsRiftState riftState)
        {
            switch (riftState)
            {
                case PortalNightsRiftState.Charging:
                    return new Color(1f, 0.3f, 0.08f, 1f);
                case PortalNightsRiftState.Active:
                    return new Color(1f, 0.1f, 0.55f, 1f);
                case PortalNightsRiftState.Closing:
                    return new Color(0.38f, 0.18f, 1f, 1f);
                default:
                    return new Color(0.42f, 0.08f, 0.08f, 1f);
            }
        }
    }
}
