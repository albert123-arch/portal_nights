using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;

namespace PortalNights
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(PortalNightsHealth))]
    public sealed class PortalNightsStaffRescue : NetworkBehaviour
    {
        [SerializeField] private string staffId = "Staff";
        [SerializeField] private float moveSpeed = 6.15f;
        [SerializeField] private float followMinDistance = 3f;
        [SerializeField] private float followMaxDistance = 5f;
        [SerializeField] private float catchUpDistance = 18f;
        [SerializeField] private Renderer markerRenderer;
        [SerializeField] private Light markerLight;

        private readonly NetworkVariable<int> state = new NetworkVariable<int>(
            (int)PortalNightsStaffState.Captured,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server);

        private PortalNightsHealth health;

        public string StaffId => staffId;
        public PortalNightsStaffState State => (PortalNightsStaffState)state.Value;
        public PortalNightsHealth Health => health;
        public bool NeedsRelease => State == PortalNightsStaffState.Captured || State == PortalNightsStaffState.Releasing;
        public bool NeedsRevive => State == PortalNightsStaffState.Downed;
        public bool IsAtSphere => State == PortalNightsStaffState.WaitingAtSphere || State == PortalNightsStaffState.Safe;
        public bool IsRescued => State == PortalNightsStaffState.Following || State == PortalNightsStaffState.WaitingAtSphere || State == PortalNightsStaffState.Safe;

        private void Awake()
        {
            health = GetComponent<PortalNightsHealth>();
            health.SetBaseMaxHealth(150f);
            MakeNonBlocking();
            RefreshMarker();
        }

        private void OnEnable()
        {
            PortalNightsGameController.Instance?.RegisterPlanet3Staff(this);
        }

        private void OnDisable()
        {
            PortalNightsGameController.Instance?.UnregisterPlanet3Staff(this);
        }

        public override void OnNetworkSpawn()
        {
            state.OnValueChanged += HandleStateChanged;
            health.Died += HandleDied;
            RefreshMarker();
        }

        public override void OnNetworkDespawn()
        {
            state.OnValueChanged -= HandleStateChanged;
            health.Died -= HandleDied;
        }

        private void Update()
        {
            if (!IsServer || State != PortalNightsStaffState.Following)
            {
                return;
            }

            PortalNightsGameController controller = PortalNightsGameController.Instance;
            PortalNightsPlayerController player = controller == null ? null : controller.GetClosestLivingPlayer(transform.position, 260f);
            if (player == null)
            {
                return;
            }

            Vector3 delta = PortalNightsMath.Flat(player.transform.position - transform.position);
            float distance = delta.magnitude;
            float desiredDistance = Mathf.Lerp(followMinDistance, followMaxDistance, 0.5f);
            if (distance <= desiredDistance)
            {
                return;
            }

            float speed = distance >= catchUpDistance ? moveSpeed * 1.8f : moveSpeed;
            Vector3 nextPosition = transform.position + delta.normalized * (speed * Time.deltaTime);
            if (controller != null)
            {
                controller.TryClampPlayerPositionForCurrentArea(ref nextPosition);
            }

            transform.position = nextPosition;
            if (delta.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta.normalized, Vector3.up), 12f * Time.deltaTime);
            }
        }

        public void Configure(string id, Renderer marker, Light light)
        {
            staffId = id;
            markerRenderer = marker;
            markerLight = light;
            MakeNonBlocking();
            RefreshMarker();
        }

        public void ResetCapturedServer(Vector3 position, Quaternion rotation)
        {
            if (!PortalNightsNet.ServerCanWrite(this))
            {
                return;
            }

            transform.SetPositionAndRotation(position, rotation);
            health.ServerInitialize(150f, true);
            SetStateServer(PortalNightsStaffState.Captured);
        }

        public void BeginReleaseServer()
        {
            if (State == PortalNightsStaffState.Captured)
            {
                SetStateServer(PortalNightsStaffState.Releasing);
            }
        }

        public void CancelReleaseServer()
        {
            if (State == PortalNightsStaffState.Releasing)
            {
                SetStateServer(PortalNightsStaffState.Captured);
            }
        }

        public void ReleaseServer()
        {
            health.ServerInitialize(150f, true);
            SetStateServer(PortalNightsStaffState.Following);
        }

        public void ReviveServer()
        {
            health.ServerInitialize(150f, true);
            health.DamageServer(75f);
            SetStateServer(PortalNightsStaffState.Following);
        }

        public void SetWaitingAtSphereServer()
        {
            SetStateServer(PortalNightsStaffState.WaitingAtSphere);
        }

        public void SetSafeServer()
        {
            SetStateServer(PortalNightsStaffState.Safe);
        }

        private void SetStateServer(PortalNightsStaffState newState)
        {
            if (!PortalNightsNet.ServerCanWrite(this))
            {
                return;
            }

            state.Value = (int)newState;
            RefreshMarker();
        }

        private void HandleDied(PortalNightsHealth deadHealth)
        {
            if (IsServer && State != PortalNightsStaffState.Safe)
            {
                SetStateServer(PortalNightsStaffState.Downed);
            }
        }

        private void HandleStateChanged(int previous, int current)
        {
            RefreshMarker();
        }

        private void RefreshMarker()
        {
            Color color = GetStateColor(State);
            if (markerRenderer != null)
            {
                Material material = Application.isPlaying ? markerRenderer.material : markerRenderer.sharedMaterial;
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
                        material.SetColor("_EmissionColor", color * 2.8f);
                    }
                }
            }

            if (markerLight != null)
            {
                markerLight.color = color;
                markerLight.intensity = State == PortalNightsStaffState.WaitingAtSphere || State == PortalNightsStaffState.Safe ? 3.2f : 1.8f;
            }
        }

        private void MakeNonBlocking()
        {
            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                collider.isTrigger = true;
            }
        }

        private static Color GetStateColor(PortalNightsStaffState staffState)
        {
            switch (staffState)
            {
                case PortalNightsStaffState.Releasing:
                    return new Color(0.25f, 0.82f, 1f, 1f);
                case PortalNightsStaffState.Following:
                    return new Color(1f, 0.78f, 0.22f, 1f);
                case PortalNightsStaffState.WaitingAtSphere:
                case PortalNightsStaffState.Safe:
                    return new Color(0.49f, 1f, 0.7f, 1f);
                case PortalNightsStaffState.Downed:
                    return new Color(1f, 0.18f, 0.18f, 1f);
                default:
                    return new Color(1f, 0.42f, 0.16f, 1f);
            }
        }
    }
}
