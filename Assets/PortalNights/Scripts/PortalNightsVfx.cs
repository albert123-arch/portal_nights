using UnityEngine;

namespace PortalNights
{
    public static class PortalNightsVfx
    {
        public static Material CreateRuntimeGlowMaterial(Color baseColor, Color emission)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            Material material = new Material(shader);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", baseColor);
            }
            material.color = baseColor;
            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", emission);
            }

            return material;
        }

        public static void SpawnBurst(Vector3 position, Color color, float size = 1f, int particles = 18)
        {
            GameObject burstObject = new GameObject("PN_VFX_Burst");
            burstObject.transform.position = position;
            ParticleSystem particleSystem = burstObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particleSystem.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.48f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.2f * size, 7f * size);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f * size, 0.24f * size);
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = particleSystem.emission;
            emission.rateOverTime = 0f;

            ParticleSystem.ShapeModule shape = particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.18f * size;

            ParticleSystemRenderer renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
            renderer.material = CreateRuntimeGlowMaterial(color, color * 2.6f);
            particleSystem.Emit(particles);
            Object.Destroy(burstObject, 1.25f);
        }

        public static void SpawnFloatingText(Vector3 position, string text, Color color)
        {
            GameObject textObject = new GameObject("PN_FloatingText");
            textObject.transform.position = position;
            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.22f;
            textMesh.fontSize = 46;
            textMesh.color = color;
            textObject.AddComponent<PortalNightsFloatingBillboard>();
        }
    }

    public sealed class PortalNightsFloatingBillboard : MonoBehaviour
    {
        private float timer = 1.1f;
        private Vector3 velocity = new Vector3(0f, 1.6f, 0f);

        private void Update()
        {
            timer -= Time.deltaTime;
            transform.position += velocity * Time.deltaTime;

            Camera camera = Camera.main;
            if (camera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position, Vector3.up);
            }

            TextMesh textMesh = GetComponent<TextMesh>();
            if (textMesh != null)
            {
                Color color = textMesh.color;
                color.a = Mathf.Clamp01(timer);
                textMesh.color = color;
            }

            if (timer <= 0f)
            {
                Destroy(gameObject);
            }
        }
    }
}
