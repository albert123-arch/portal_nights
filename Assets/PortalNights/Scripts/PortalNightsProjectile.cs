using UnityEngine;
using System.Collections.Generic;

namespace PortalNights
{
    public sealed class PortalNightsProjectile : MonoBehaviour
    {
        private const int MaxPoolSize = 96;
        private static readonly Stack<PortalNightsProjectile> pool = new Stack<PortalNightsProjectile>();
        private static int spawnsSinceLastSample;

        [SerializeField] private float speed = 36f;
        [SerializeField] private float lifeTime = 0.65f;
        [SerializeField] private LineRenderer trail;

        private Renderer glowRenderer;
        private Transform glow;
        private Vector3 target;
        private float timer;
        private bool returningToPool;

        public static int ConsumeSpawnCount()
        {
            int count = spawnsSinceLastSample;
            spawnsSinceLastSample = 0;
            return count;
        }

        public static void Spawn(Vector3 origin, Vector3 destination, Color color, float width)
        {
            PortalNightsProjectile portalProjectile = GetOrCreate();
            portalProjectile.Configure(origin, destination, color, width);
            spawnsSinceLastSample++;
        }

        private static PortalNightsProjectile GetOrCreate()
        {
            while (pool.Count > 0)
            {
                PortalNightsProjectile pooled = pool.Pop();
                if (pooled != null)
                {
                    pooled.gameObject.SetActive(true);
                    return pooled;
                }
            }

            GameObject projectile = new GameObject("PN_EnergyProjectile");
            PortalNightsProjectile portalProjectile = projectile.AddComponent<PortalNightsProjectile>();
            portalProjectile.CreateVisuals();
            return portalProjectile;
        }

        private void CreateVisuals()
        {
            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Glow";
            visual.transform.SetParent(transform, false);
            glow = visual.transform;
            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                DestroyProjectileObject(collider);
            }

            glowRenderer = visual.GetComponent<Renderer>();
            trail = gameObject.AddComponent<LineRenderer>();
            trail.positionCount = 2;
        }

        private void Configure(Vector3 origin, Vector3 destination, Color color, float width)
        {
            returningToPool = false;
            transform.position = origin;
            target = destination;
            timer = lifeTime;

            if (glow == null || trail == null)
            {
                CreateVisuals();
            }

            if (glow != null)
            {
                glow.localPosition = Vector3.zero;
                glow.localScale = Vector3.one * Mathf.Max(0.12f, width * 3.3f);
            }

            if (glowRenderer != null)
            {
                glowRenderer.sharedMaterial = PortalNightsVfx.GetRuntimeGlowMaterial(color, color * 3.2f);
            }

            if (trail != null)
            {
                trail.enabled = true;
                trail.positionCount = 2;
                trail.widthMultiplier = width;
                trail.sharedMaterial = PortalNightsVfx.GetRuntimeGlowMaterial(color, color * 4f);
                trail.startColor = color;
                trail.endColor = new Color(color.r, color.g, color.b, 0f);
                trail.SetPosition(0, origin);
                trail.SetPosition(1, Vector3.Lerp(origin, destination, 0.4f));
            }
        }

        private void Update()
        {
            timer -= Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);

            if (trail != null)
            {
                trail.SetPosition(0, transform.position);
                trail.SetPosition(1, Vector3.Lerp(transform.position, target, 0.4f));
            }

            if (timer <= 0f || Vector3.Distance(transform.position, target) <= 0.15f)
            {
                ReturnToPool();
            }
        }

        private void ReturnToPool()
        {
            if (returningToPool)
            {
                return;
            }

            returningToPool = true;
            if (trail != null)
            {
                trail.enabled = false;
            }

            if (pool.Count >= MaxPoolSize)
            {
                DestroyProjectileObject(gameObject);
                return;
            }

            gameObject.SetActive(false);
            pool.Push(this);
        }

        private static void DestroyProjectileObject(Object targetObject)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(targetObject);
                return;
            }
#endif

            Destroy(targetObject);
        }
    }
}
