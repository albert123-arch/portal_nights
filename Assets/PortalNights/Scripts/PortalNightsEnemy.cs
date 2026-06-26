using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

namespace PortalNights
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkTransform))]
    [RequireComponent(typeof(PortalNightsHealth))]
    public sealed class PortalNightsEnemy : NetworkBehaviour
    {
        [Header("Stats")]
        [SerializeField] private PortalNightsEnemyKind enemyKind;
        [SerializeField] private float baseHealth = 70f;
        [SerializeField] private float moveSpeed = 2.6f;
        [SerializeField] private float attackDamage = 12f;
        [SerializeField] private float attackRange = 1.65f;
        [SerializeField] private float attackInterval = 1.05f;
        [SerializeField] private float playerAggroRange = 6.5f;
        [SerializeField] private int coinReward = 18;
        [SerializeField] private Transform aimPoint;

        private PortalNightsHealth health;
        private readonly NetworkVariable<int> assignedLane = new NetworkVariable<int>(
            (int)PortalNightsLane.Left, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private PortalNightsLanePath lanePath;
        private int waypointIndex;
        private float attackTimer;
        private bool deathHandled;

        public PortalNightsHealth Health => health;
        public PortalNightsLane AssignedLane => (PortalNightsLane)assignedLane.Value;
        public Vector3 AimPoint
        {
            get
            {
                if (aimPoint != null && aimPoint != transform)
                {
                    return aimPoint.position;
                }

                float chestHeight = enemyKind == PortalNightsEnemyKind.Brute ? 1.45f : 0.92f;
                return transform.position + Vector3.up * chestHeight;
            }
        }

        private void Awake()
        {
            health = GetComponent<PortalNightsHealth>();
        }

        public override void OnNetworkSpawn()
        {
            health.Died += HandleDeath;
            if (IsServer)
            {
                PortalNightsGameController.Instance?.RegisterEnemy(this);
                if (lanePath == null && PortalNightsGameController.Instance != null)
                {
                    lanePath = PortalNightsGameController.Instance.GetLanePath(AssignedLane);
                }

                if (health.CurrentHealth <= 0.01f || Mathf.Approximately(health.CurrentHealth, health.BaseMaxHealth))
                {
                    health.ServerInitialize(baseHealth, true);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            if (health != null)
            {
                health.Died -= HandleDeath;
            }

            if (IsServer)
            {
                PortalNightsGameController.Instance?.UnregisterEnemy(this);
            }
        }

        private void Update()
        {
            if (!IsServer || health == null || health.IsDead)
            {
                return;
            }

            UpdateServerAi();
        }

        public void ConfigurePrefab(PortalNightsEnemyKind kind, float maxHealth, float speed, float damage, int reward)
        {
            enemyKind = kind;
            baseHealth = maxHealth;
            moveSpeed = speed;
            attackDamage = damage;
            coinReward = reward;
            GetComponent<PortalNightsHealth>()?.SetBaseMaxHealth(maxHealth);
        }

        public void ConfigureForWave(PortalNightsEnemyKind kind, int wave, int reward)
        {
            enemyKind = kind;
            coinReward = reward;
            baseHealth = kind == PortalNightsEnemyKind.Brute ? Mathf.Max(baseHealth, 190f) : Mathf.Max(baseHealth, 52f);
            float healthScale = 1f + Mathf.Max(0, wave - 1) * 0.22f;
            float damageScale = 1f + Mathf.Max(0, wave - 1) * 0.1f;
            health.ServerInitialize(baseHealth * healthScale, true);
            attackDamage = (kind == PortalNightsEnemyKind.Brute ? 15f : 4.5f) * damageScale;
            moveSpeed = (kind == PortalNightsEnemyKind.Brute ? 1.35f : 1.85f) + Mathf.Min(1.15f, wave * 0.05f);
        }

        public void ConfigureLaneServer(PortalNightsLane lane, PortalNightsLanePath path)
        {
            assignedLane.Value = (int)lane;
            lanePath = path;
            waypointIndex = 0;
        }

        private void UpdateServerAi()
        {
            PortalNightsGameController controller = PortalNightsGameController.Instance;
            if (controller == null || controller.GameOver)
            {
                return;
            }

            if (TryFollowLanePath())
            {
                return;
            }

            PortalNightsHealth target = GetTargetHealth(controller);
            if (target == null || target.IsDead)
            {
                return;
            }

            MoveOrAttackTarget(controller, target);
        }

        private bool TryFollowLanePath()
        {
            if (lanePath == null || waypointIndex >= lanePath.WaypointCount)
            {
                return false;
            }

            while (lanePath.TryGetWaypoint(waypointIndex, out Vector3 waypoint))
            {
                Vector3 flatDelta = PortalNightsMath.Flat(waypoint - transform.position);
                if (flatDelta.magnitude > 0.58f)
                {
                    MoveToward(waypoint);
                    return true;
                }

                waypointIndex++;
                if (waypointIndex >= lanePath.WaypointCount)
                {
                    return false;
                }
            }

            return false;
        }

        private void MoveOrAttackTarget(PortalNightsGameController controller, PortalNightsHealth target)
        {
            Vector3 flatDelta = PortalNightsMath.Flat(target.transform.position - transform.position);
            float distance = flatDelta.magnitude;
            if (distance > attackRange)
            {
                MoveToward(target.transform.position);
            }
            else
            {
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    attackTimer = attackInterval;
                    if (target.TryGetComponent(out PortalNightsPlayerController player))
                    {
                        player.DamageServer(attackDamage);
                    }
                    else
                    {
                        controller.DamageObjectiveServer(target, attackDamage, transform.position);
                    }

                    AttackVfxClientRpc(target.transform.position + Vector3.up * 1.1f);
                }
            }
        }

        private void MoveToward(Vector3 targetPosition)
        {
            Vector3 flatDelta = PortalNightsMath.Flat(targetPosition - transform.position);
            Vector3 direction = flatDelta.sqrMagnitude <= 0.001f ? PortalNightsMath.Flat(transform.forward).normalized : flatDelta.normalized;
            if (direction.sqrMagnitude <= 0.001f)
            {
                direction = Vector3.forward;
            }

            transform.position += direction * (moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction, Vector3.up), 10f * Time.deltaTime);
        }

        private PortalNightsHealth GetTargetHealth(PortalNightsGameController controller)
        {
            return controller.GetEnemyTargetHealth(transform.position, playerAggroRange);
        }

        private void HandleDeath(PortalNightsHealth deadHealth)
        {
            if (!IsServer || deathHandled)
            {
                return;
            }

            deathHandled = true;
            PortalNightsGameController.Instance?.EnemyKilled(this, coinReward, transform.position);
            StartCoroutine(DespawnAfterDeath());
        }

        private IEnumerator DespawnAfterDeath()
        {
            DeathVfxClientRpc(transform.position + Vector3.up * 1.1f, enemyKind == PortalNightsEnemyKind.Brute);
            yield return new WaitForSeconds(0.12f);
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
        private void AttackVfxClientRpc(Vector3 hitPosition)
        {
            PortalNightsVfx.SpawnBurst(hitPosition, new Color(1f, 0.28f, 0.18f, 1f), 0.75f, 13);
        }

        [ClientRpc]
        private void DeathVfxClientRpc(Vector3 position, bool brute)
        {
            PortalNightsVfx.SpawnBurst(position, brute ? new Color(1f, 0.34f, 0.96f, 1f) : new Color(0.64f, 0.22f, 1f, 1f), brute ? 1.55f : 1f, brute ? 32 : 21);
        }
    }
}
