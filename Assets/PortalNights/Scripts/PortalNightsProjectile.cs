using UnityEngine;

namespace PortalNights
{
    public sealed class PortalNightsProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 36f;
        [SerializeField] private float lifeTime = 0.65f;
        [SerializeField] private LineRenderer trail;

        private Vector3 target;
        private float timer;

        public static void Spawn(Vector3 origin, Vector3 destination, Color color, float width)
        {
            GameObject projectile = new GameObject("PN_EnergyProjectile");
            projectile.transform.position = origin;
            PortalNightsProjectile portalProjectile = projectile.AddComponent<PortalNightsProjectile>();
            portalProjectile.target = destination;
            portalProjectile.timer = portalProjectile.lifeTime;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Glow";
            visual.transform.SetParent(projectile.transform, false);
            visual.transform.localScale = Vector3.one * Mathf.Max(0.12f, width * 3.3f);
            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                Object.Destroy(collider);
            }

            Renderer renderer = visual.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = PortalNightsVfx.CreateRuntimeGlowMaterial(color, color * 3.2f);
            }

            LineRenderer line = projectile.AddComponent<LineRenderer>();
            line.positionCount = 2;
            line.widthMultiplier = width;
            line.material = PortalNightsVfx.CreateRuntimeGlowMaterial(color, color * 4f);
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0f);
            portalProjectile.trail = line;
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
                Destroy(gameObject);
            }
        }
    }
}
