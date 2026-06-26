using UnityEngine;
using Unity.Netcode;

namespace PortalNights
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class PortalNightsBuildPoint : NetworkBehaviour
    {
        [SerializeField] private int cost = 120;
        [SerializeField] private int upgradeLevel2Cost = 180;
        [SerializeField] private int upgradeLevel3Cost = 260;
        [SerializeField] private Transform turretSocket;
        [SerializeField] private Renderer indicator;
        [SerializeField] private Light padLight;
        [SerializeField] private float gizmoCoverageRadius = 18f;
        [SerializeField] private bool requiredForPortal = true;

        private readonly NetworkVariable<int> turretLevel = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private PortalNightsAlly spawnedTurret;

        public int Level => turretLevel.Value;
        public int MaxLevel => 3;
        public int NextLevel => Mathf.Clamp(Level + 1, 1, MaxLevel);
        public int Cost => GetCostForNextLevel(NextLevel);
        public bool IsAvailable => Level <= 0;
        public bool CanInteract => Level < MaxLevel;
        public bool IsMaxed => Level >= MaxLevel;
        public bool RequiredForPortal => requiredForPortal;
        public PortalNightsAlly SpawnedTurret => spawnedTurret;
        public Vector3 BuildPosition => turretSocket == null ? transform.position + Vector3.up * 0.35f : turretSocket.position;
        public Quaternion BuildRotation => turretSocket == null ? transform.rotation : turretSocket.rotation;

        private void OnEnable()
        {
            MakeNonBlocking();
            PortalNightsGameController.Instance?.RegisterBuildPoint(this);
        }

        private void OnDisable()
        {
            PortalNightsGameController.Instance?.UnregisterBuildPoint(this);
        }

        public override void OnNetworkSpawn()
        {
            turretLevel.OnValueChanged += HandleLevelChanged;
            RefreshVisual();
        }

        public override void OnNetworkDespawn()
        {
            turretLevel.OnValueChanged -= HandleLevelChanged;
        }

        public void Configure(int buildCost, Transform socket, Renderer padIndicator, Light light)
        {
            Configure(buildCost, Mathf.RoundToInt(buildCost * 1.5f), Mathf.RoundToInt(buildCost * 2.15f), socket, padIndicator, light);
        }

        public void Configure(int buildCost, int level2Cost, int level3Cost, Transform socket, Renderer padIndicator, Light light)
        {
            cost = buildCost;
            upgradeLevel2Cost = level2Cost;
            upgradeLevel3Cost = level3Cost;
            turretSocket = socket;
            indicator = padIndicator;
            padLight = light;
            MakeNonBlocking();
            RefreshVisual();
        }

        public string GetInteractionPrompt()
        {
            if (IsAvailable)
            {
                return $"E BUILD TURRET   COST: {Cost}";
            }

            if (IsMaxed)
            {
                return "TURRET MAX LEVEL";
            }

            return $"E UPGRADE TURRET LVL {NextLevel}   COST: {Cost}";
        }

        public void MakeNonBlocking()
        {
            foreach (Collider collider in GetComponentsInChildren<Collider>())
            {
                collider.isTrigger = true;
            }
        }

        public void SetBuiltServer(bool value)
        {
            if (!PortalNightsNet.ServerCanWrite(this))
            {
                return;
            }

            turretLevel.Value = value ? Mathf.Max(1, turretLevel.Value) : 0;
            if (!value)
            {
                spawnedTurret = null;
            }

            RefreshVisual();
        }

        public void AttachTurretServer(PortalNightsAlly turret, int level)
        {
            if (!PortalNightsNet.ServerCanWrite(this))
            {
                return;
            }

            spawnedTurret = turret;
            SetLevelServer(level);
        }

        public void SetLevelServer(int level)
        {
            if (!PortalNightsNet.ServerCanWrite(this))
            {
                return;
            }

            turretLevel.Value = Mathf.Clamp(level, 0, MaxLevel);
            RefreshVisual();
        }

        private int GetCostForNextLevel(int nextLevel)
        {
            if (nextLevel <= 1)
            {
                return cost;
            }

            if (nextLevel == 2)
            {
                return upgradeLevel2Cost;
            }

            return upgradeLevel3Cost;
        }

        private void HandleLevelChanged(int previous, int current)
        {
            RefreshVisual();
        }

        private void RefreshVisual()
        {
            if (indicator == null)
            {
                indicator = GetComponentInChildren<Renderer>();
            }

            Color color = GetIndicatorColor();
            if (indicator != null)
            {
                indicator.material.color = color;
                if (indicator.material.HasProperty("_BaseColor"))
                {
                    indicator.material.SetColor("_BaseColor", color);
                }
                if (indicator.material.HasProperty("_EmissionColor"))
                {
                    indicator.material.EnableKeyword("_EMISSION");
                    indicator.material.SetColor("_EmissionColor", color * (IsAvailable ? 2.8f : 1.2f));
                }
            }

            if (padLight != null)
            {
                padLight.color = color;
                padLight.intensity = IsAvailable ? 2.2f : IsMaxed ? 1.45f : 0.95f;
            }
        }

        private Color GetIndicatorColor()
        {
            if (Level <= 0)
            {
                return new Color(0.1f, 0.78f, 1f, 0.88f);
            }

            if (Level == 1)
            {
                return new Color(0.18f, 0.86f, 1f, 0.78f);
            }

            if (Level == 2)
            {
                return new Color(0.82f, 0.28f, 1f, 0.84f);
            }

            return new Color(1f, 0.72f, 0.18f, 0.94f);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = IsAvailable ? new Color(0.1f, 0.85f, 1f, 0.5f) : new Color(1f, 0.55f, 0.12f, 0.45f);
            Gizmos.DrawWireSphere(BuildPosition, gizmoCoverageRadius);
            Gizmos.DrawLine(transform.position + Vector3.up * 0.2f, BuildPosition + Vector3.up * 0.2f);
        }
    }
}
