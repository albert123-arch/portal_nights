using System.Collections.Generic;
using UnityEngine;

namespace PortalNights
{
    public static class PortalNightsVfx
    {
        private const int MaxActiveBursts = 24;
        private const int MaxBurstPoolSize = 64;
        private const int MaxActiveFloatingText = 30;

        private static readonly Dictionary<string, Material> runtimeGlowMaterials = new Dictionary<string, Material>();
        private static readonly Queue<ParticleSystem> burstPool = new Queue<ParticleSystem>();
        private static Shader runtimeGlowShader;
        private static int activeBurstCount;
        private static int burstSpawnsSinceLastSample;
        private static int activeFloatingTextCount;
        private static long totalBurstSpawns;

        public static int ActiveBurstCount => activeBurstCount;
        public static int ActiveFloatingTextCount => activeFloatingTextCount;
        public static long TotalBurstSpawns => totalBurstSpawns;

        public static Material GetRuntimeGlowMaterial(Color baseColor, Color emission)
        {
            Shader shader = GetRuntimeGlowShader();
            if (shader == null)
            {
                return null;
            }

            string shaderName = shader.name;
            string key = shaderName + "|" + QuantizeColor(baseColor) + "|" + QuantizeColor(emission);
            if (runtimeGlowMaterials.TryGetValue(key, out Material cachedMaterial) && cachedMaterial != null)
            {
                return cachedMaterial;
            }

            Material material = new Material(shader);
            material.name = "PN_RuntimeGlow_" + runtimeGlowMaterials.Count;
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

            runtimeGlowMaterials[key] = material;
            return material;
        }

        private static Shader GetRuntimeGlowShader()
        {
            if (runtimeGlowShader != null)
            {
                return runtimeGlowShader;
            }

            runtimeGlowShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (runtimeGlowShader == null)
            {
                runtimeGlowShader = Shader.Find("Universal Render Pipeline/Lit");
            }
            if (runtimeGlowShader == null)
            {
                runtimeGlowShader = Shader.Find("Sprites/Default");
            }
            if (runtimeGlowShader == null)
            {
                runtimeGlowShader = Shader.Find("Standard");
            }

            return runtimeGlowShader;
        }

        public static Material CreateRuntimeGlowMaterial(Color baseColor, Color emission)
        {
            return GetRuntimeGlowMaterial(baseColor, emission);
        }

        public static int ConsumeBurstSpawnCount()
        {
            int count = burstSpawnsSinceLastSample;
            burstSpawnsSinceLastSample = 0;
            return count;
        }

        public static void SpawnBurst(Vector3 position, Color color, float size = 1f, int particles = 18)
        {
            if (activeBurstCount >= MaxActiveBursts)
            {
                return;
            }

            ParticleSystem particleSystem = GetBurstFromPool();
            if (particleSystem == null)
            {
                return;
            }

            GameObject burstObject = particleSystem.gameObject;
            burstObject.transform.position = position;
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
            renderer.sharedMaterial = GetRuntimeGlowMaterial(color, color * 2.6f);
            PortalNightsPooledBurst pooledBurst = burstObject.GetComponent<PortalNightsPooledBurst>();
            if (pooledBurst == null)
            {
                pooledBurst = burstObject.AddComponent<PortalNightsPooledBurst>();
            }

            activeBurstCount++;
            burstSpawnsSinceLastSample++;
            totalBurstSpawns++;
            pooledBurst.Activate(particleSystem, 1.25f);
            particleSystem.Emit(particles);
        }

        public static void SpawnFloatingText(Vector3 position, string text, Color color)
        {
            if (activeFloatingTextCount >= MaxActiveFloatingText)
            {
                return;
            }

            GameObject textObject = new GameObject("PN_FloatingText");
            textObject.transform.position = position;
            TextMesh textMesh = textObject.AddComponent<TextMesh>();
            textMesh.text = PortalNightsLocalization.LocalizeRuntimeText(text);
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.characterSize = 0.22f;
            textMesh.fontSize = 46;
            textMesh.color = color;
            activeFloatingTextCount++;
            textObject.AddComponent<PortalNightsFloatingBillboard>();
        }

        internal static void ReturnBurstToPool(ParticleSystem particleSystem)
        {
            activeBurstCount = Mathf.Max(0, activeBurstCount - 1);
            if (particleSystem == null)
            {
                return;
            }

            particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            GameObject burstObject = particleSystem.gameObject;
            if (burstPool.Count >= MaxBurstPoolSize)
            {
                DestroyPooledObject(burstObject);
                return;
            }

            burstObject.SetActive(false);
            burstPool.Enqueue(particleSystem);
        }

        internal static void NotifyFloatingTextDestroyed()
        {
            activeFloatingTextCount = Mathf.Max(0, activeFloatingTextCount - 1);
        }

        private static ParticleSystem GetBurstFromPool()
        {
            while (burstPool.Count > 0)
            {
                ParticleSystem pooled = burstPool.Dequeue();
                if (pooled != null)
                {
                    pooled.gameObject.SetActive(true);
                    return pooled;
                }
            }

            GameObject burstObject = new GameObject("PN_VFX_Burst");
            return burstObject.AddComponent<ParticleSystem>();
        }

        private static void DestroyPooledObject(GameObject targetObject)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(targetObject);
                return;
            }
#endif

            Object.Destroy(targetObject);
        }

        private static string QuantizeColor(Color color)
        {
            int r = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 4095);
            int g = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 4095);
            int b = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 4095);
            int a = Mathf.Clamp(Mathf.RoundToInt(color.a * 255f), 0, 4095);
            return r.ToString("X3") + g.ToString("X3") + b.ToString("X3") + a.ToString("X3");
        }
    }

    public sealed class PortalNightsFloatingBillboard : MonoBehaviour
    {
        private static Camera cachedCamera;

        private float timer = 1.1f;
        private Vector3 velocity = new Vector3(0f, 1.6f, 0f);
        private TextMesh textMesh;

        private void Awake()
        {
            textMesh = GetComponent<TextMesh>();
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            transform.position += velocity * Time.deltaTime;

            Camera camera = GetCachedCamera();
            if (camera != null)
            {
                transform.rotation = Quaternion.LookRotation(transform.position - camera.transform.position, Vector3.up);
            }

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

        private void OnDestroy()
        {
            PortalNightsVfx.NotifyFloatingTextDestroyed();
        }

        private static Camera GetCachedCamera()
        {
            if (cachedCamera == null || !cachedCamera.isActiveAndEnabled)
            {
                cachedCamera = Camera.main;
            }

            return cachedCamera;
        }
    }

    public sealed class PortalNightsPooledBurst : MonoBehaviour
    {
        private ParticleSystem pooledParticleSystem;
        private float timer;
        private bool active;

        public void Activate(ParticleSystem targetParticleSystem, float lifetime)
        {
            pooledParticleSystem = targetParticleSystem;
            timer = Mathf.Max(0.05f, lifetime);
            active = true;
        }

        private void Update()
        {
            if (!active)
            {
                return;
            }

            timer -= Time.deltaTime;
            if (timer > 0f)
            {
                return;
            }

            active = false;
            PortalNightsVfx.ReturnBurstToPool(pooledParticleSystem);
        }
    }
}
