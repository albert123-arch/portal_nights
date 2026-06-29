using System;
using System.Text;
using UnityEngine;

namespace PortalNights
{
    [DefaultExecutionOrder(10000)]
    public sealed class PortalNightsFreezeRecorder : MonoBehaviour
    {
        [SerializeField] private bool enabledInBuilds;
        [SerializeField] private float warningThresholdMs = 50f;
        [SerializeField] private float freezeThresholdMs = 100f;
        [SerializeField] private float debugReportInterval = 5f;
        [SerializeField] private bool deepDiagnostics;

        private Transform planet2Root;
        private Transform planet3Root;
        private Transform planet4Root;
        private Transform planet5Root;
        private float nextDebugReportTime;
        private float nextDeepDiagnosticTime;
        private float nextRateSampleTime;
        private float sampleWindowStart;
        private int hudWritesPerSecond;
        private float projectileSpawnsPerSecond;
        private float burstSpawnsPerSecond;
        private float turretShotsPerSecond;
        private long lastProjectileTotal;
        private long lastBurstTotal;
        private long lastTurretShotTotal;
        private int lastGc0;
        private int lastGc1;
        private int lastGc2;

        private bool IsRecorderEnabled => Application.isEditor || enabledInBuilds;

        private void Awake()
        {
            sampleWindowStart = Time.unscaledTime;
            nextRateSampleTime = sampleWindowStart + 1f;
            nextDebugReportTime = sampleWindowStart + debugReportInterval;
            lastProjectileTotal = PortalNightsProjectile.TotalSpawns;
            lastBurstTotal = PortalNightsVfx.TotalBurstSpawns;
            lastTurretShotTotal = PortalNightsAlly.TotalTurretShots;
            lastGc0 = GC.CollectionCount(0);
            lastGc1 = GC.CollectionCount(1);
            lastGc2 = GC.CollectionCount(2);
        }

        private void Update()
        {
            if (!IsRecorderEnabled)
            {
                return;
            }

            UpdateRates();

            float frameMs = Time.unscaledDeltaTime * 1000f;
            bool spike = frameMs > warningThresholdMs;
            PortalNightsGameController controller = PortalNightsGameController.Instance;
            bool periodicDebug = controller != null && controller.PerformanceDebugEnabled && Time.unscaledTime >= nextDebugReportTime;
            if (!spike && !periodicDebug)
            {
                return;
            }

            if (periodicDebug)
            {
                nextDebugReportTime = Time.unscaledTime + Mathf.Max(1f, debugReportInterval);
            }

            LogReport(spike ? (frameMs > freezeThresholdMs ? "freeze" : "warn") : "debug", frameMs, controller);
        }

        private void UpdateRates()
        {
            if (Time.unscaledTime < nextRateSampleTime)
            {
                return;
            }

            float elapsed = Mathf.Max(0.001f, Time.unscaledTime - sampleWindowStart);
            long projectileTotal = PortalNightsProjectile.TotalSpawns;
            long burstTotal = PortalNightsVfx.TotalBurstSpawns;
            long turretShotTotal = PortalNightsAlly.TotalTurretShots;
            hudWritesPerSecond = Mathf.RoundToInt(PortalNightsHud.ConsumeTextWriteCount() / elapsed);
            projectileSpawnsPerSecond = (projectileTotal - lastProjectileTotal) / elapsed;
            burstSpawnsPerSecond = (burstTotal - lastBurstTotal) / elapsed;
            turretShotsPerSecond = (turretShotTotal - lastTurretShotTotal) / elapsed;
            lastProjectileTotal = projectileTotal;
            lastBurstTotal = burstTotal;
            lastTurretShotTotal = turretShotTotal;
            sampleWindowStart = Time.unscaledTime;
            nextRateSampleTime = sampleWindowStart + 1f;
        }

        private void LogReport(string kind, float frameMs, PortalNightsGameController controller)
        {
            bool useDeepDiagnostics = deepDiagnostics && Time.unscaledTime >= nextDeepDiagnosticTime;
            if (useDeepDiagnostics)
            {
                nextDeepDiagnosticTime = Time.unscaledTime + 5f;
                CachePlanetRoots();
            }

            int gc0 = GC.CollectionCount(0);
            int gc1 = GC.CollectionCount(1);
            int gc2 = GC.CollectionCount(2);
            long managedMb = GC.GetTotalMemory(false) / (1024L * 1024L);
            int enemies = useDeepDiagnostics ? CountActive<PortalNightsEnemy>() : controller == null ? -1 : controller.EnemiesAlive;
            int allies = useDeepDiagnostics ? CountActive<PortalNightsAlly>() : -1;
            int pickups = useDeepDiagnostics ? CountActive<PortalNightsPickup>() : -1;
            int projectiles = useDeepDiagnostics ? CountActive<PortalNightsProjectile>() : -1;
            int particles = useDeepDiagnostics ? CountActiveParticleSystems() : -1;
            int lights = useDeepDiagnostics ? CountActiveLights() : -1;
            int renderers = useDeepDiagnostics ? CountActiveRenderers() : -1;

            StringBuilder builder = new StringBuilder(420);
            builder.Append("[PortalNightsFreeze] ");
            builder.Append(kind);
            builder.Append(" dt=");
            builder.Append(frameMs.ToString("0.0"));
            builder.Append("ms state=");
            builder.Append(controller == null ? "none" : controller.GameState.ToString());
            builder.Append(" planet=");
            builder.Append(controller == null ? 0 : controller.ActivePlanetEnvironmentIndex);
            builder.Append(" enemies=");
            builder.Append(FormatOptionalCount(enemies));
            builder.Append(" allies=");
            builder.Append(FormatOptionalCount(allies));
            builder.Append(" pickups=");
            builder.Append(FormatOptionalCount(pickups));
            builder.Append(" projectiles=");
            builder.Append(FormatOptionalCount(projectiles));
            builder.Append(" bursts=");
            builder.Append(PortalNightsVfx.ActiveBurstCount);
            builder.Append(" floating=");
            builder.Append(PortalNightsVfx.ActiveFloatingTextCount);
            builder.Append(" particles=");
            builder.Append(FormatOptionalCount(particles));
            builder.Append(" lights=");
            builder.Append(FormatOptionalCount(lights));
            builder.Append(" renderers=");
            builder.Append(FormatOptionalCount(renderers));
            builder.Append(" gc=");
            builder.Append(gc0 - lastGc0);
            builder.Append("/");
            builder.Append(gc1 - lastGc1);
            builder.Append("/");
            builder.Append(gc2 - lastGc2);
            builder.Append(" mem=");
            builder.Append(managedMb);
            builder.Append("MB hudWrites/s=");
            builder.Append(hudWritesPerSecond);
            builder.Append(" vfxBursts/s=");
            builder.Append(burstSpawnsPerSecond.ToString("0.0"));
            builder.Append(" projectiles/s=");
            builder.Append(projectileSpawnsPerSecond.ToString("0.0"));
            builder.Append(" turretShots/s=");
            builder.Append(turretShotsPerSecond.ToString("0.0"));
            builder.Append(" roots=P2:");
            builder.Append(IsRootActiveForReport(controller, 2, planet2Root) ? "on" : "off");
            builder.Append(" P3:");
            builder.Append(IsRootActiveForReport(controller, 3, planet3Root) ? "on" : "off");
            builder.Append(" P4:");
            builder.Append(IsRootActiveForReport(controller, 4, planet4Root) ? "on" : "off");
            builder.Append(" P5:");
            builder.Append(IsRootActiveForReport(controller, 5, planet5Root) ? "on" : "off");
            builder.Append(" deep=");
            builder.Append(useDeepDiagnostics ? "on" : "off");

            Debug.Log(builder.ToString(), this);
            lastGc0 = gc0;
            lastGc1 = gc1;
            lastGc2 = gc2;
        }

        private void CachePlanetRoots()
        {
            if (planet2Root != null && planet3Root != null && planet4Root != null && planet5Root != null)
            {
                return;
            }

            GameObject arena = GameObject.Find("PortalNightsArena");
            Transform arenaTransform = arena == null ? null : arena.transform;
            if (arenaTransform == null)
            {
                return;
            }

            planet2Root ??= arenaTransform.Find("Planet2_CrystalMoon");
            planet3Root ??= arenaTransform.Find("Planet3_AshRelayStation");
            planet4Root ??= arenaTransform.Find("Planet4_SwarmExpanse");
            planet5Root ??= arenaTransform.Find("Planet5_CrimsonSingularity");
        }

        private static bool IsRootActive(Transform root)
        {
            return root != null && root.gameObject.activeInHierarchy;
        }

        private static bool IsRootActiveForReport(PortalNightsGameController controller, int planetIndex, Transform root)
        {
            return controller != null ? controller.IsPlanetRootActiveForDiagnostics(planetIndex) : IsRootActive(root);
        }

        private static string FormatOptionalCount(int value)
        {
            return value < 0 ? "skip" : value.ToString();
        }

        private static int CountActive<T>() where T : Component
        {
            T[] components = FindObjectsByType<T>(FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < components.Length; i++)
            {
                if (components[i] != null && components[i].gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountActiveParticleSystems()
        {
            ParticleSystem[] particleSystems = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < particleSystems.Length; i++)
            {
                ParticleSystem particleSystem = particleSystems[i];
                if (particleSystem != null && particleSystem.gameObject.activeInHierarchy && (particleSystem.isPlaying || particleSystem.particleCount > 0))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountActiveLights()
        {
            Light[] lights = FindObjectsByType<Light>(FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < lights.Length; i++)
            {
                Light light = lights[i];
                if (light != null && light.enabled && light.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountActiveRenderers()
        {
            Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
            int count = 0;
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
