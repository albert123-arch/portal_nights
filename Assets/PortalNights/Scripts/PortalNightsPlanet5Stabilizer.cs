using UnityEngine;

namespace PortalNights
{
    public sealed class PortalNightsPlanet5Stabilizer : MonoBehaviour
    {
        [SerializeField] private string stabilizerId = "Stabilizer";
        [SerializeField] private bool available;
        [SerializeField] private bool completed;

        private Renderer[] renderers;
        private Light[] lights;

        public string StabilizerId => string.IsNullOrWhiteSpace(stabilizerId) ? gameObject.name : stabilizerId;
        public bool IsAvailable => available;
        public bool IsCompleted => completed;
        public bool CanInteract => available && !completed;
        public Vector3 InteractionPoint => transform.position + Vector3.up * 1.1f;

        private void Awake()
        {
            CacheVisuals();
            ApplyVisual();
        }

        public void Configure(string id)
        {
            stabilizerId = string.IsNullOrWhiteSpace(id) ? gameObject.name : id;
            CacheVisuals();
            ApplyVisual();
        }

        public void SetState(bool isAvailable, bool isCompleted)
        {
            available = isAvailable;
            completed = isCompleted;
            ApplyVisual();
        }

        private void CacheVisuals()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            lights = GetComponentsInChildren<Light>(true);
        }

        private void ApplyVisual()
        {
            if (renderers == null)
            {
                CacheVisuals();
            }

            Color baseColor = completed
                ? new Color(0.39f, 0.97f, 1f, 1f)
                : available
                ? new Color(0.95f, 0.88f, 0.55f, 1f)
                : new Color(0.55f, 0.12f, 0.08f, 1f);
            Color emission = completed
                ? new Color(0.39f, 0.97f, 1f, 1f) * 3.2f
                : available
                ? new Color(1f, 0.7f, 0.24f, 1f) * 1.8f
                : new Color(0.45f, 0.04f, 0.02f, 1f) * 0.8f;

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                renderer.GetPropertyBlock(block);
                block.SetColor("_BaseColor", baseColor);
                block.SetColor("_Color", baseColor);
                block.SetColor("_EmissionColor", emission);
                renderer.SetPropertyBlock(block);
            }

            if (lights == null)
            {
                lights = GetComponentsInChildren<Light>(true);
            }

            foreach (Light light in lights)
            {
                if (light == null)
                {
                    continue;
                }

                light.color = completed ? new Color(0.39f, 0.97f, 1f, 1f) : baseColor;
                light.intensity = completed ? 4.5f : available ? 2f : 0.35f;
                light.range = completed ? 16f : 9f;
            }
        }
    }
}
