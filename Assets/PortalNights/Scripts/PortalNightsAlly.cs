using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;

namespace PortalNights
{
    [RequireComponent(typeof(NetworkObject))]
    [RequireComponent(typeof(NetworkTransform))]
    public sealed class PortalNightsAlly : NetworkBehaviour
    {
        [SerializeField] private float range = 18f;
        [SerializeField] private float damage = 18f;
        [SerializeField] private float fireRate = 0.32f;
        [SerializeField] private Transform rotatingHead;
        [SerializeField] private Transform muzzle;
        [SerializeField] private Transform[] levelMuzzleGroups = new Transform[3];
        [SerializeField] private LineRenderer beam;
        [SerializeField] private Transform[] levelVisuals = new Transform[3];
        [SerializeField] private float[] levelRanges = { 18f, 20f, 22f };
        [SerializeField] private float[] levelDamages = { 18f, 26.1f, 39.6f };
        [SerializeField] private float[] levelFireRates = { 0.32f, 0.267f, 0.221f };
        [SerializeField] private float idleTurnSpeed = 38f;
        [SerializeField] private float targetTurnSpeed = 18f;
        [SerializeField] private float fireConeDegrees = 7f;
        [SerializeField] private float levelPopScale = 1.14f;
        [SerializeField] private bool barrelForwardIsNegativeZ = true;

        private readonly NetworkVariable<int> level = new NetworkVariable<int>(
            1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private float fireTimer;
        private float visualPulse;
        private float visualAimHoldTimer;
        private Vector3 lastAimDirection = Vector3.forward;

        public int Level => Mathf.Clamp(level.Value, 1, 3);
        public float CurrentDamage => damage;
        public float CurrentRange => range;
        public float CurrentFireInterval => fireRate;

        private void Awake()
        {
            if (beam == null)
            {
                beam = GetComponentInChildren<LineRenderer>();
            }

            RefreshLevelVisual();
            ApplyLevelStats(Level);
        }

        public override void OnNetworkSpawn()
        {
            level.OnValueChanged += HandleLevelChanged;
            RefreshLevelVisual();
            ApplyLevelStats(Level);

            if (IsServer)
            {
                PortalNightsGameController.Instance?.RegisterAlly(this);
            }
        }

        public override void OnNetworkDespawn()
        {
            level.OnValueChanged -= HandleLevelChanged;

            if (IsServer)
            {
                PortalNightsGameController.Instance?.UnregisterAlly(this);
            }
        }

        private void Update()
        {
            AnimateTurretVisual();

            if (IsServer)
            {
                UpdateServerCombat();
            }
        }

        private void UpdateServerCombat()
        {
            fireTimer -= Time.deltaTime;
            PortalNightsGameController controller = PortalNightsGameController.Instance;
            PortalNightsDamageTarget damageTarget = controller == null ? null : controller.GetClosestDamageTarget(transform.position, range);
            if (damageTarget != null && damageTarget.Health != null && !damageTarget.Health.IsDead)
            {
                UpdateServerCombatTarget(controller, damageTarget);
                return;
            }

            PortalNightsEnemy target = controller == null ? null : controller.GetClosestEnemy(transform.position, range);
            if (target == null || target.Health == null || target.Health.IsDead)
            {
                return;
            }

            Vector3 targetPoint = target.AimPoint;
            if (PortalNightsMath.TryFlatDirection(transform.position, targetPoint, out Vector3 direction))
            {
                lastAimDirection = direction;
                AimVisual(direction, false);
            }

            if (fireTimer > 0f)
            {
                return;
            }

            Transform head = rotatingHead == null ? transform : rotatingHead;
            float angleToTarget = Vector3.Angle(GetBarrelForward(head), lastAimDirection);
            if (angleToTarget > fireConeDegrees)
            {
                return;
            }

            fireTimer = fireRate;
            AimVisual(lastAimDirection, true);
            float damageMultiplier = PortalNightsGameController.Instance == null ? 1f : PortalNightsGameController.Instance.TurretDamageMultiplier;
            target.Health.DamageServer(damage * damageMultiplier);
            int muzzleCount = GetActiveMuzzleOrigins(out Vector3 originA, out Vector3 originB, out Vector3 originC);
            FireClientRpc(originA, originB, originC, muzzleCount, targetPoint, Level);
        }

        private void UpdateServerCombatTarget(PortalNightsGameController controller, PortalNightsDamageTarget target)
        {
            Vector3 targetPoint = target.AimPoint;
            if (PortalNightsMath.TryFlatDirection(transform.position, targetPoint, out Vector3 direction))
            {
                lastAimDirection = direction;
                AimVisual(direction, false);
            }

            if (fireTimer > 0f)
            {
                return;
            }

            Transform head = rotatingHead == null ? transform : rotatingHead;
            float angleToTarget = Vector3.Angle(GetBarrelForward(head), lastAimDirection);
            if (angleToTarget > fireConeDegrees)
            {
                return;
            }

            fireTimer = fireRate;
            AimVisual(lastAimDirection, true);
            float damageMultiplier = controller == null ? 1f : controller.TurretDamageMultiplier;
            float finalDamage = damage * damageMultiplier;
            target.Health.DamageServer(finalDamage);
            controller?.NotifyDamageTargetHitServer(target, finalDamage, false);
            int muzzleCount = GetActiveMuzzleOrigins(out Vector3 originA, out Vector3 originB, out Vector3 originC);
            FireClientRpc(originA, originB, originC, muzzleCount, targetPoint, Level);
        }

        public void Configure(float turretRange, float turretDamage, float turretFireRate, Transform head, Transform muzzlePoint, LineRenderer beamRenderer)
        {
            range = turretRange;
            damage = turretDamage;
            fireRate = turretFireRate;
            rotatingHead = head;
            muzzle = muzzlePoint;
            beam = beamRenderer;
        }

        public void ConfigureLevels(float[] ranges, float[] damages, float[] fireRates, Transform head, Transform muzzlePoint, LineRenderer beamRenderer, Transform[] visuals)
        {
            ConfigureLevels(ranges, damages, fireRates, head, muzzlePoint, beamRenderer, visuals, null, false);
        }

        public void ConfigureLevels(float[] ranges, float[] damages, float[] fireRates, Transform head, Transform muzzlePoint, LineRenderer beamRenderer, Transform[] visuals, Transform[] muzzleGroups, bool useNegativeZBarrels)
        {
            levelRanges = ranges;
            levelDamages = damages;
            levelFireRates = fireRates;
            rotatingHead = head;
            muzzle = muzzlePoint;
            beam = beamRenderer;
            levelVisuals = visuals;
            levelMuzzleGroups = muzzleGroups;
            barrelForwardIsNegativeZ = useNegativeZBarrels;
            RefreshLevelVisual();
            ApplyLevelStats(Level);
        }

        public void SetLevelServer(int targetLevel)
        {
            if (!PortalNightsNet.ServerCanWrite(this))
            {
                return;
            }

            int clampedLevel = Mathf.Clamp(targetLevel, 1, 3);
            level.Value = clampedLevel;
            ApplyLevelStats(clampedLevel);
            RefreshLevelVisual();
            if (IsSpawned)
            {
                LevelChangedClientRpc(clampedLevel, transform.position + Vector3.up * 0.85f);
            }
        }

        [ClientRpc]
        private void FireClientRpc(Vector3 originA, Vector3 originB, Vector3 originC, int muzzleCount, Vector3 targetPoint, int shotLevel)
        {
            Color color = GetLevelColor(shotLevel);
            if (PortalNightsMath.TryFlatDirection(transform.position, targetPoint, out Vector3 direction))
            {
                AimVisual(direction, true);
            }

            int count = Mathf.Clamp(muzzleCount, 1, 3);
            float projectileWidth = shotLevel >= 3 ? 0.115f : 0.075f;
            for (int i = 0; i < count; i++)
            {
                Vector3 origin = GetOriginByIndex(i, originA, originB, originC);
                PortalNightsProjectile.Spawn(origin, targetPoint, color, projectileWidth);
                PortalNightsVfx.SpawnBurst(origin, color, 0.48f + shotLevel * 0.12f, 8 + shotLevel * 4);
            }

            visualPulse = 0.18f;

            if (beam != null)
            {
                beam.startColor = color;
                beam.endColor = new Color(color.r, color.g, color.b, 0.12f);
                StopAllCoroutines();
                StartCoroutine(ShowBeams(originA, originB, originC, count, targetPoint, color, projectileWidth));
            }
        }

        [ClientRpc]
        private void LevelChangedClientRpc(int newLevel, Vector3 position)
        {
            ApplyLevelStats(newLevel);
            RefreshLevelVisual();
            visualPulse = 0.32f;
            PortalNightsVfx.SpawnBurst(position, GetLevelColor(newLevel), 0.95f + newLevel * 0.18f, 18 + newLevel * 6);
            PortalNightsVfx.SpawnFloatingText(position + Vector3.up * 0.75f, "LVL " + newLevel, GetLevelColor(newLevel));
        }

        private System.Collections.IEnumerator ShowBeams(Vector3 originA, Vector3 originB, Vector3 originC, int muzzleCount, Vector3 targetPoint, Color color, float width)
        {
            beam.enabled = true;
            beam.positionCount = 2;
            beam.SetPosition(0, originA);
            beam.SetPosition(1, targetPoint);
            LineRenderer[] extraBeams = CreateExtraBeams(originB, originC, muzzleCount, targetPoint, color, width);
            yield return new WaitForSeconds(0.06f);
            beam.enabled = false;

            for (int i = 0; i < extraBeams.Length; i++)
            {
                if (extraBeams[i] != null)
                {
                    Destroy(extraBeams[i].gameObject);
                }
            }
        }

        private void HandleLevelChanged(int previous, int current)
        {
            ApplyLevelStats(current);
            RefreshLevelVisual();
            visualPulse = 0.22f;
        }

        private void ApplyLevelStats(int targetLevel)
        {
            int index = Mathf.Clamp(targetLevel, 1, 3) - 1;
            range = GetArrayValue(levelRanges, index, range);
            damage = GetArrayValue(levelDamages, index, damage);
            fireRate = GetArrayValue(levelFireRates, index, fireRate);
        }

        private void RefreshLevelVisual()
        {
            if (levelVisuals == null)
            {
                return;
            }

            int activeIndex = Level - 1;
            for (int i = 0; i < levelVisuals.Length; i++)
            {
                if (levelVisuals[i] != null)
                {
                    levelVisuals[i].gameObject.SetActive(i == activeIndex);
                }
            }

            if (levelMuzzleGroups != null)
            {
                for (int i = 0; i < levelMuzzleGroups.Length; i++)
                {
                    if (levelMuzzleGroups[i] != null)
                    {
                        levelMuzzleGroups[i].gameObject.SetActive(i == activeIndex);
                    }
                }
            }

            if (muzzle != null && GetActiveMuzzleGroup() == null)
            {
                muzzle.localPosition = GetLegacyMuzzleLocalPosition(Level);
            }
        }

        private void AnimateTurretVisual()
        {
            visualAimHoldTimer = Mathf.Max(0f, visualAimHoldTimer - Time.deltaTime);
            Transform head = rotatingHead == null ? transform : rotatingHead;
            if (head == null)
            {
                return;
            }

            if ((!IsServer || lastAimDirection.sqrMagnitude <= 0.001f) && visualAimHoldTimer <= 0f)
            {
                head.Rotate(Vector3.up, idleTurnSpeed * Time.deltaTime, Space.World);
            }

            if (visualPulse > 0f)
            {
                visualPulse = Mathf.Max(0f, visualPulse - Time.deltaTime);
                float pulse = 1f + (levelPopScale - 1f) * Mathf.Sin((visualPulse / 0.32f) * Mathf.PI);
                head.localScale = Vector3.one * pulse;
            }
            else if (head.localScale != Vector3.one)
            {
                head.localScale = Vector3.Lerp(head.localScale, Vector3.one, 18f * Time.deltaTime);
            }
        }

        private void AimVisual(Vector3 direction, bool snap)
        {
            Transform head = rotatingHead == null ? transform : rotatingHead;
            direction = PortalNightsMath.Flat(direction);
            if (head == null || direction.sqrMagnitude <= 0.001f)
            {
                return;
            }

            Vector3 headForward = barrelForwardIsNegativeZ ? -direction.normalized : direction.normalized;
            Quaternion targetRotation = Quaternion.LookRotation(headForward, Vector3.up);
            if (snap)
            {
                head.rotation = targetRotation;
            }
            else
            {
                float turnDegrees = Mathf.Max(360f, targetTurnSpeed * 60f) * Time.deltaTime;
                head.rotation = Quaternion.RotateTowards(head.rotation, targetRotation, turnDegrees);
            }

            visualAimHoldTimer = 0.65f;
        }

        private int GetActiveMuzzleOrigins(out Vector3 originA, out Vector3 originB, out Vector3 originC)
        {
            Vector3 fallback = muzzle == null ? GetFallbackMuzzlePosition() : muzzle.position;
            originA = fallback;
            originB = fallback;
            originC = fallback;

            Transform group = GetActiveMuzzleGroup();
            if (group == null || group.childCount <= 0)
            {
                return 1;
            }

            int count = Mathf.Clamp(group.childCount, 1, 3);
            originA = group.GetChild(0).position;
            originB = count > 1 ? group.GetChild(1).position : originA;
            originC = count > 2 ? group.GetChild(2).position : originB;
            return count;
        }

        private Transform GetActiveMuzzleGroup()
        {
            int index = Level - 1;
            if (levelMuzzleGroups == null || index < 0 || index >= levelMuzzleGroups.Length)
            {
                return null;
            }

            return levelMuzzleGroups[index];
        }

        private Vector3 GetFallbackMuzzlePosition()
        {
            Vector3 direction = lastAimDirection.sqrMagnitude > 0.001f ? lastAimDirection.normalized : GetBarrelForward(rotatingHead == null ? transform : rotatingHead);
            return transform.position + Vector3.up * 1.35f + direction * 0.85f;
        }

        private Vector3 GetBarrelForward(Transform head)
        {
            if (head == null)
            {
                return Vector3.forward;
            }

            Vector3 forward = barrelForwardIsNegativeZ ? -head.forward : head.forward;
            forward = PortalNightsMath.Flat(forward);
            return forward.sqrMagnitude <= 0.001f ? Vector3.forward : forward.normalized;
        }

        private static Vector3 GetLegacyMuzzleLocalPosition(int targetLevel)
        {
            return new Vector3(0f, 1.14f + Mathf.Clamp(targetLevel, 1, 3) * 0.05f, -1.4f);
        }

        private static Vector3 GetOriginByIndex(int index, Vector3 originA, Vector3 originB, Vector3 originC)
        {
            if (index == 1)
            {
                return originB;
            }

            if (index >= 2)
            {
                return originC;
            }

            return originA;
        }

        private LineRenderer[] CreateExtraBeams(Vector3 originB, Vector3 originC, int muzzleCount, Vector3 targetPoint, Color color, float width)
        {
            int extraCount = Mathf.Clamp(muzzleCount, 1, 3) - 1;
            if (extraCount <= 0)
            {
                return new LineRenderer[0];
            }

            LineRenderer[] extraBeams = new LineRenderer[extraCount];
            for (int i = 0; i < extraCount; i++)
            {
                GameObject beamObject = new GameObject("PN_Turret_ExtraBeam");
                LineRenderer line = beamObject.AddComponent<LineRenderer>();
                line.positionCount = 2;
                line.widthMultiplier = width;
                line.material = beam.material;
                line.startColor = color;
                line.endColor = new Color(color.r, color.g, color.b, 0.08f);
                line.SetPosition(0, i == 0 ? originB : originC);
                line.SetPosition(1, targetPoint);
                extraBeams[i] = line;
            }

            return extraBeams;
        }

        private static float GetArrayValue(float[] values, int index, float fallback)
        {
            if (values == null || index < 0 || index >= values.Length)
            {
                return fallback;
            }

            return values[index];
        }

        private static Color GetLevelColor(int targetLevel)
        {
            if (targetLevel <= 1)
            {
                return new Color(0.18f, 0.86f, 1f, 1f);
            }

            if (targetLevel == 2)
            {
                return new Color(0.82f, 0.28f, 1f, 1f);
            }

            return new Color(1f, 0.72f, 0.18f, 1f);
        }
    }
}
