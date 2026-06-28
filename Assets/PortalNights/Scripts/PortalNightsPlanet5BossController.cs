using System.Collections;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using PortalNights.Visuals;

namespace PortalNights
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(PortalNightsHealth))]
    [RequireComponent(typeof(PortalNightsDamageTarget))]
    public sealed class PortalNightsPlanet5BossController : NetworkBehaviour
    {
        [SerializeField] private string bossName = "Boss";
        [SerializeField] private bool rangedBoss = true;
        [SerializeField] private float moveSpeed = 2.2f;
        [SerializeField] private float preferredRange = 18f;
        [SerializeField] private float attackRange = 28f;
        [SerializeField] private float attackDamage = 28f;
        [SerializeField] private float attackInterval = 1.4f;
        [SerializeField] private float specialInterval = 7.5f;
        [SerializeField] private Transform aimPoint;

        private readonly NetworkVariable<int> visualKind = new NetworkVariable<int>(
            (int)PortalNightsEnemyVisualKind.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> visualTargetHeight = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> visualPlanetIndex = new NetworkVariable<int>(
            5, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private PortalNightsHealth health;
        private PortalNightsDamageTarget damageTarget;
        private PortalNightsEnemyVisualBinder visualBinder;
        private float attackTimer;
        private float specialTimer;
        private float lastObservedHealth;
        private bool finalDeathHandled;

        public string BossName => bossName;
        public PortalNightsHealth Health => health;
        public bool IsFinalDead => finalDeathHandled;

        private void Awake()
        {
            health = GetComponent<PortalNightsHealth>();
            damageTarget = GetComponent<PortalNightsDamageTarget>();
            visualBinder = GetComponent<PortalNightsEnemyVisualBinder>();
        }

        public override void OnNetworkSpawn()
        {
            if (health != null)
            {
                health.Died += HandleDeath;
                health.HealthChanged += HandleHealthChangedVisual;
                lastObservedHealth = health.CurrentHealth;
            }

            visualKind.OnValueChanged += HandleVisualKindChanged;
            visualTargetHeight.OnValueChanged += HandleVisualTargetHeightChanged;
            visualPlanetIndex.OnValueChanged += HandleVisualPlanetIndexChanged;
            ApplyVisualLocal((PortalNightsEnemyVisualKind)visualKind.Value);
        }

        public override void OnNetworkDespawn()
        {
            if (health != null)
            {
                health.Died -= HandleDeath;
                health.HealthChanged -= HandleHealthChangedVisual;
            }

            visualKind.OnValueChanged -= HandleVisualKindChanged;
            visualTargetHeight.OnValueChanged -= HandleVisualTargetHeightChanged;
            visualPlanetIndex.OnValueChanged -= HandleVisualPlanetIndexChanged;
        }

        private void Update()
        {
            if (!IsServer || health == null || health.IsDead || finalDeathHandled)
            {
                return;
            }

            PortalNightsGameController controller = PortalNightsGameController.Instance;
            if (controller == null || !controller.IsPlanet5BossCombatActive)
            {
                return;
            }

            PortalNightsPlayerController player = controller.GetClosestLivingPlayer(transform.position, 140f);
            if (player == null || player.Health == null || player.Health.IsDead)
            {
                return;
            }

            attackTimer -= Time.deltaTime;
            specialTimer -= Time.deltaTime;
            if (rangedBoss)
            {
                UpdateRangedBoss(player);
            }
            else
            {
                UpdateMeleeBoss(player);
            }
        }

        public void Configure(string displayName, bool isRanged, float maxHealth, float speed, float damage, float range, Transform bossAimPoint)
        {
            bossName = string.IsNullOrWhiteSpace(displayName) ? gameObject.name : displayName;
            rangedBoss = isRanged;
            moveSpeed = Mathf.Max(0.1f, speed);
            attackDamage = Mathf.Max(1f, damage);
            attackRange = Mathf.Max(2f, range);
            preferredRange = isRanged ? Mathf.Clamp(range * 0.65f, 12f, 22f) : 3.2f;
            aimPoint = bossAimPoint;
            health = GetComponent<PortalNightsHealth>();
            health.SetBaseMaxHealth(maxHealth);
            health.ServerInitialize(maxHealth, true);
            damageTarget = GetComponent<PortalNightsDamageTarget>();
            damageTarget.Configure(PortalNightsDamageTargetKind.Planet5Boss, bossName, aimPoint, isRanged ? 20 : 18);
        }

        public void ApplyVisualServer(PortalNightsEnemyVisualKind kind)
        {
            ApplyVisualServer(kind, PortalNightsEnemyVisualCatalog.GetBossTargetHeight(kind), 5);
        }

        public void ApplyVisualServer(PortalNightsEnemyVisualKind kind, float targetHeight, int planetIndex)
        {
            int resolvedPlanetIndex = Mathf.Max(1, planetIndex);
            if (IsSpawned && PortalNightsNet.ServerCanWrite(this))
            {
                PortalNightsGroundingUtility.GroundGameplayRoot(gameObject, resolvedPlanetIndex);
                visualTargetHeight.Value = Mathf.Max(0f, targetHeight);
                visualPlanetIndex.Value = resolvedPlanetIndex;
                visualKind.Value = (int)kind;
            }

            ApplyVisualLocal(kind);
            visualBinder?.RegroundCurrentVisual();
        }

        public void SetTargetable(bool value)
        {
            if (damageTarget != null)
            {
                damageTarget.SetTargetable(value);
            }
        }

        private void UpdateRangedBoss(PortalNightsPlayerController player)
        {
            Vector3 toPlayer = PortalNightsMath.Flat(player.transform.position - transform.position);
            float distance = toPlayer.magnitude;
            if (distance > attackRange * 0.85f)
            {
                Move(toPlayer.normalized);
            }
            else if (distance < preferredRange * 0.55f)
            {
                Move(-toPlayer.normalized);
            }
            else if (toPlayer.sqrMagnitude > 0.001f)
            {
                Face(toPlayer.normalized);
            }

            if (attackTimer <= 0f && distance <= attackRange)
            {
                attackTimer = attackInterval;
                player.DamageServer(attackDamage);
                BossAttackClientRpc(transform.position + Vector3.up * 2.2f, player.transform.position + Vector3.up * 1.1f, new Color(1f, 0.78f, 0.22f, 1f), bossName + " LASER");
            }

            if (specialTimer <= 0f && distance <= 18f)
            {
                specialTimer = specialInterval;
                player.DamageServer(attackDamage * 0.65f);
                BossPulseClientRpc(transform.position + Vector3.up * 0.4f, new Color(1f, 0.55f, 0.08f, 1f), bossName + " PULSE");
            }
        }

        private void UpdateMeleeBoss(PortalNightsPlayerController player)
        {
            Vector3 toPlayer = PortalNightsMath.Flat(player.transform.position - transform.position);
            float distance = toPlayer.magnitude;
            if (distance > preferredRange)
            {
                Move(toPlayer.normalized);
            }
            else if (toPlayer.sqrMagnitude > 0.001f)
            {
                Face(toPlayer.normalized);
            }

            if (attackTimer <= 0f && distance <= 4.2f)
            {
                attackTimer = attackInterval;
                player.DamageServer(attackDamage);
                BossPulseClientRpc(player.transform.position + Vector3.up * 0.2f, new Color(1f, 0.12f, 0.08f, 1f), bossName + " HIT");
            }

            if (specialTimer <= 0f && distance <= 7.5f)
            {
                specialTimer = specialInterval;
                player.DamageServer(attackDamage * 1.35f);
                BossPulseClientRpc(transform.position + Vector3.up * 0.25f, new Color(1f, 0.18f, 0.1f, 1f), bossName + " SLAM");
            }
        }

        private void Move(Vector3 direction)
        {
            direction = PortalNightsMath.Flat(direction);
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            direction.Normalize();
            transform.position += direction * (moveSpeed * Time.deltaTime);
            Face(direction);
        }

        private void Face(Vector3 direction)
        {
            direction = PortalNightsMath.Flat(direction);
            if (direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction.normalized, Vector3.up), 240f * Time.deltaTime);
        }

        private void HandleVisualKindChanged(int previous, int current)
        {
            ApplyVisualLocal((PortalNightsEnemyVisualKind)current);
        }

        private void HandleVisualTargetHeightChanged(float previous, float current)
        {
            ApplyVisualLocal((PortalNightsEnemyVisualKind)visualKind.Value);
        }

        private void HandleVisualPlanetIndexChanged(int previous, int current)
        {
            ApplyVisualLocal((PortalNightsEnemyVisualKind)visualKind.Value);
        }

        private void ApplyVisualLocal(PortalNightsEnemyVisualKind kind)
        {
            if (kind == PortalNightsEnemyVisualKind.None)
            {
                return;
            }

            float targetHeight = visualTargetHeight.Value > 0.1f
                ? visualTargetHeight.Value
                : PortalNightsEnemyVisualCatalog.GetBossTargetHeight(kind);
            EnsureVisualBinder().Bind(kind, targetHeight, Mathf.Max(1, visualPlanetIndex.Value));
        }

        private PortalNightsEnemyVisualBinder EnsureVisualBinder()
        {
            if (visualBinder == null)
            {
                visualBinder = GetComponent<PortalNightsEnemyVisualBinder>();
            }

            if (visualBinder == null)
            {
                visualBinder = gameObject.AddComponent<PortalNightsEnemyVisualBinder>();
            }

            return visualBinder;
        }

        private void HandleHealthChangedVisual(PortalNightsHealth changedHealth)
        {
            if (changedHealth == null)
            {
                return;
            }

            float current = changedHealth.CurrentHealth;
            if (current > 0.01f && current < lastObservedHealth - 0.05f)
            {
                EnsureVisualBinder().TriggerHit();
            }

            lastObservedHealth = current;
        }

        private void HandleDeath(PortalNightsHealth deadHealth)
        {
            if (!IsServer || finalDeathHandled)
            {
                return;
            }

            bool revivedOrConsumed = PortalNightsGameController.Instance != null && PortalNightsGameController.Instance.HandlePlanet5BossDeathServer(this);
            if (revivedOrConsumed)
            {
                return;
            }

            finalDeathHandled = true;
            StartCoroutine(DespawnFinalDeath());
        }

        private IEnumerator DespawnFinalDeath()
        {
            BossDeathVisualClientRpc();
            BossPulseClientRpc(transform.position + Vector3.up * 1.6f, new Color(0.85f, 0.28f, 1f, 1f), bossName + " DEFEATED");
            yield return new WaitForSeconds(0.2f);
            NetworkObject networkObject = GetComponent<NetworkObject>();
            if (networkObject != null && networkObject.IsSpawned)
            {
                networkObject.Despawn(true);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        [ClientRpc]
        private void BossAttackClientRpc(Vector3 origin, Vector3 hit, Color color, string label)
        {
            EnsureVisualBinder().TriggerAttack();
            PortalNightsProjectile.Spawn(origin, hit, color, 0.12f);
            PortalNightsVfx.SpawnBurst(origin, color, 0.85f, 18);
            PortalNightsVfx.SpawnFloatingText(hit + Vector3.up * 0.4f, label, color);
        }

        [ClientRpc]
        private void BossPulseClientRpc(Vector3 position, Color color, string label)
        {
            EnsureVisualBinder().TriggerSpecial();
            PortalNightsVfx.SpawnBurst(position, color, 2f, 32);
            PortalNightsVfx.SpawnFloatingText(position + Vector3.up * 1.5f, label, color);
        }

        [ClientRpc]
        private void BossDeathVisualClientRpc()
        {
            EnsureVisualBinder().TriggerDeath();
        }
    }
}
