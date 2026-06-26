using UnityEngine;

namespace PortalNights
{
    public sealed class PortalNightsPlanet4HiveRift : MonoBehaviour
    {
        [SerializeField] private string riftId = "HiveRift";
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private Renderer[] visualRenderers;
        [SerializeField] private Light[] lights;
        [SerializeField] private int killsRequired = 25;

        private MaterialPropertyBlock propertyBlock;
        private PortalNightsRiftState state = PortalNightsRiftState.Active;
        private int enemiesDefeated;

        public string RiftId => riftId;
        public int EnemiesDefeated => enemiesDefeated;
        public int KillsRequired => killsRequired;
        public PortalNightsRiftState State => state;
        public bool IsClosed => state == PortalNightsRiftState.Closed;
        public bool IsClosable => state == PortalNightsRiftState.Closable;
        public bool CanSpawn => state == PortalNightsRiftState.Active || state == PortalNightsRiftState.Weakening || state == PortalNightsRiftState.Closable;

        private void Awake()
        {
            EnsurePropertyBlock();
            GatherReferences();
            ApplyVisualState();
        }

        public void Configure(string id, int requiredKills)
        {
            riftId = string.IsNullOrWhiteSpace(id) ? gameObject.name : id;
            killsRequired = Mathf.Max(1, requiredKills);
            GatherReferences();
            ApplyVisualState();
        }

        public Transform GetSpawnPoint(int index)
        {
            GatherReferences();
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                return transform;
            }

            return spawnPoints[Mathf.Abs(index) % spawnPoints.Length];
        }

        public void ResetRift()
        {
            enemiesDefeated = 0;
            SetState(PortalNightsRiftState.Active);
        }

        public void AddEnemyDefeated()
        {
            enemiesDefeated++;
            if (state == PortalNightsRiftState.Active && enemiesDefeated >= Mathf.CeilToInt(killsRequired * 0.55f))
            {
                SetState(PortalNightsRiftState.Weakening);
            }

            if (!IsClosed && enemiesDefeated >= killsRequired)
            {
                SetState(PortalNightsRiftState.Closable);
            }
        }

        public void SetState(PortalNightsRiftState newState)
        {
            state = newState;
            ApplyVisualState();
        }

        private void GatherReferences()
        {
            if (string.IsNullOrWhiteSpace(riftId))
            {
                riftId = gameObject.name;
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Transform spawnRoot = transform.Find("SpawnPoints");
                if (spawnRoot != null)
                {
                    spawnPoints = new Transform[spawnRoot.childCount];
                    for (int i = 0; i < spawnRoot.childCount; i++)
                    {
                        spawnPoints[i] = spawnRoot.GetChild(i);
                    }
                }
            }

            if (visualRenderers == null || visualRenderers.Length == 0)
            {
                visualRenderers = GetComponentsInChildren<Renderer>(true);
            }

            if (lights == null || lights.Length == 0)
            {
                lights = GetComponentsInChildren<Light>(true);
            }
        }

        private void ApplyVisualState()
        {
            EnsurePropertyBlock();
            Color baseColor = GetStateColor(state);
            float emission = state == PortalNightsRiftState.Closed ? 0.18f : state == PortalNightsRiftState.Closing ? 1.35f : state == PortalNightsRiftState.Closable ? 4.5f : 3.2f;

            GatherReferences();
            if (visualRenderers != null)
            {
                foreach (Renderer renderer in visualRenderers)
                {
                    if (renderer == null)
                    {
                        continue;
                    }

                    renderer.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetColor("_Color", baseColor);
                    propertyBlock.SetColor("_BaseColor", baseColor);
                    propertyBlock.SetColor("_EmissionColor", baseColor * emission);
                    renderer.SetPropertyBlock(propertyBlock);
                }
            }

            if (lights != null)
            {
                foreach (Light light in lights)
                {
                    if (light == null)
                    {
                        continue;
                    }

                    light.color = baseColor;
                    light.intensity = state == PortalNightsRiftState.Closed ? 0.35f : state == PortalNightsRiftState.Closing ? 2f : state == PortalNightsRiftState.Closable ? 7.5f : 5f;
                }
            }
        }

        private void EnsurePropertyBlock()
        {
            propertyBlock ??= new MaterialPropertyBlock();
        }

        private static Color GetStateColor(PortalNightsRiftState riftState)
        {
            switch (riftState)
            {
                case PortalNightsRiftState.Weakening:
                    return new Color(0.72f, 0.18f, 1f, 1f);
                case PortalNightsRiftState.Closable:
                    return new Color(0.48f, 1f, 0.25f, 1f);
                case PortalNightsRiftState.Closing:
                    return new Color(1f, 0.18f, 0.28f, 1f);
                case PortalNightsRiftState.Closed:
                    return new Color(0.11f, 0.08f, 0.14f, 1f);
                default:
                    return new Color(0.64f, 0.28f, 1f, 1f);
            }
        }
    }
}
