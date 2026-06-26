using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.InputSystem;

namespace PortalNights
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class PortalNightsGameController : NetworkBehaviour
    {
        [Header("Network")]
        [SerializeField] private bool autoStartHostInEditor = true;
        [SerializeField] private bool randomizeEditorHostPort = true;

        [Header("Scene")]
        [SerializeField] private PortalNightsHealth coreHealth;
        [SerializeField] private PortalNightsHud hud;
        [SerializeField] private Transform portalSpawn;
        [SerializeField] private PortalNightsLanePath leftLanePath;
        [SerializeField] private PortalNightsLanePath rightLanePath;
        [SerializeField] private Transform[] playerSpawnPoints;

        [Header("Prefabs")]
        [SerializeField] private GameObject smallEnemyPrefab;
        [SerializeField] private GameObject bruteEnemyPrefab;
        [SerializeField] private GameObject turretPrefab;
        [SerializeField] private GameObject coinPickupPrefab;

        [Header("Economy")]
        [SerializeField] private int startingCoins = 260;
        [SerializeField] private int smallEnemyReward = 18;
        [SerializeField] private int bruteEnemyReward = 42;
        [SerializeField] private float buildInteractionRange = 4.2f;

        [Header("Waves")]
        [SerializeField] private float firstWaveDelay = 12f;
        [SerializeField] private float waveBreakDuration = 8f;
        [SerializeField] private float spawnSpread = 3.2f;

        [Header("Progression")]
        [SerializeField] private int planet1FinalWave = 10;
        [SerializeField] private bool countAllBuildPadsAsRequired = true;
        [SerializeField] private float portalInteractionRange = 5.5f;
        [SerializeField] private Vector3 planet1BoundsMin = new Vector3(-15.5f, 1.12f, -14.6f);
        [SerializeField] private Vector3 planet1BoundsMax = new Vector3(15.5f, 8f, 30.5f);
        [SerializeField] private Vector3 planet2Center = new Vector3(0f, 0f, 92f);
        [SerializeField] private float planet2Radius = 42f;
        [SerializeField] private float pickupDropChance = 0.72f;
        [SerializeField] private float temporaryBoostDuration = 30f;

        private readonly List<PortalNightsEnemy> enemies = new List<PortalNightsEnemy>();
        private readonly List<PortalNightsBuildPoint> buildPoints = new List<PortalNightsBuildPoint>();
        private readonly List<PortalNightsAlly> allies = new List<PortalNightsAlly>();

        private readonly NetworkVariable<int> waveNumber = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> enemiesAlive = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> enemiesRemaining = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> leftLaneEnemies = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> rightLaneEnemies = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> coins = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> nextWaveTimer = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> waveRunning = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<bool> gameOver = new NetworkVariable<bool>(
            false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> gameState = new NetworkVariable<int>(
            (int)PortalNightsGameState.Planet1_Defense, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> requiredTurretsBuilt = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> requiredTurretsMaxed = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> requiredTurretsTotal = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        private PortalNightsWaveDefinition activeWave;
        private float spawnTimer;
        private int spawnedSmall;
        private int spawnedBrute;
        private int leftSmallQuota;
        private int rightSmallQuota;
        private int leftBruteQuota;
        private int rightBruteQuota;
        private int spawnedLeftSmall;
        private int spawnedRightSmall;
        private int spawnedLeftBrute;
        private int spawnedRightBrute;
        private bool oddWaveExtraToLeft = true;
        private bool chargeVfxShown;
        private bool initialized;
        private bool planet2AreaCreated;
        private int planet2EnemiesToSpawn;
        private int planet2BrutesToSpawn;
        private int planet2SpawnedSmall;
        private int planet2SpawnedBrute;
        private int planet2DefenseWaveIndex;
        private float planet2SpawnTimer;
        private float turretDamageBoostTimer;
        private float turretRunDamageMultiplier = 1f;
        private Transform portalPrompt;
        private Transform planet2Root;
        private Transform planet2ArrivalPoint;
        private Transform planet2Sphere;
        private PortalNightsHealth planet2SphereHealth;
        private Transform[] planet2EnemySpawnPoints;
        private Coroutine initializeServerStateRoutine;

        public static PortalNightsGameController Instance { get; private set; }

        public PortalNightsHealth CoreHealth => coreHealth;
        public PortalNightsHud Hud => hud;
        public int WaveNumber => Mathf.Max(1, waveNumber.Value);
        public int EnemiesAlive => enemiesAlive.Value;
        public int EnemiesRemaining => enemiesRemaining.Value;
        public int LeftLaneEnemies => leftLaneEnemies.Value;
        public int RightLaneEnemies => rightLaneEnemies.Value;
        public int Coins => coins.Value;
        public bool WaveRunning => waveRunning.Value;
        public bool GameOver => gameOver.Value;
        public float NextWaveTimer => nextWaveTimer.Value;
        public float BuildInteractionRange => buildInteractionRange;
        public PortalNightsGameState GameState => (PortalNightsGameState)gameState.Value;
        public int RequiredTurretsBuilt => requiredTurretsBuilt.Value;
        public int RequiredTurretsMaxed => requiredTurretsMaxed.Value;
        public int RequiredTurretsTotal => requiredTurretsTotal.Value;
        public bool PortalReady => GameState == PortalNightsGameState.Planet1_PortalReady;
        public float TurretDamageMultiplier => Mathf.Max(1f, turretRunDamageMultiplier) * (turretDamageBoostTimer > 0f ? 1.2f : 1f);
        public bool IsRewardChoiceActive => GameState == PortalNightsGameState.Planet1_RewardChoice;

        private void Awake()
        {
            Instance = this;
            CacheSceneReferences();
        }

        private void Start()
        {
            EnsurePlanet2Area();
            MakeDefenseObjectsPlayerFriendly();
            hud?.Bind(this);

            NetworkManager manager = NetworkManager.Singleton;
            if (autoStartHostInEditor && manager != null && !manager.IsListening)
            {
                ConfigureEditorHostPort(manager);
                if (!manager.StartHost())
                {
                    Debug.LogError("[PortalNights] Failed to start host. Check for transport errors in the Console.", this);
                }
            }
        }

        private void ConfigureEditorHostPort(NetworkManager manager)
        {
#if UNITY_EDITOR
            if (!randomizeEditorHostPort || manager == null)
            {
                return;
            }

            UnityTransport transport = manager.NetworkConfig.NetworkTransport as UnityTransport;
            if (transport == null)
            {
                transport = manager.GetComponent<UnityTransport>();
            }

            if (transport == null)
            {
                return;
            }

            ushort port = (ushort)Random.Range(24000, 65000);
            transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
            Debug.Log($"[PortalNights] Editor host port set to {port}.", this);
#endif
        }

        public override void OnNetworkSpawn()
        {
            CacheSceneReferences();
            EnsurePlanet2Area();
            if (IsServer)
            {
                if (initializeServerStateRoutine != null)
                {
                    StopCoroutine(initializeServerStateRoutine);
                }

                initializeServerStateRoutine = StartCoroutine(InitializeServerStateAfterSpawn());
            }
        }

        public override void OnNetworkDespawn()
        {
            if (coreHealth != null)
            {
                coreHealth.Died -= HandleCoreDeath;
            }
        }

        private void Update()
        {
            UpdateRuntimeVisuals();

            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && GameOver && IsServer)
            {
                RestartServer();
            }

            if (!IsServer || !initialized || GameOver)
            {
                return;
            }

            turretDamageBoostTimer = Mathf.Max(0f, turretDamageBoostTimer - Time.deltaTime);

            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && GameState == PortalNightsGameState.Failed)
            {
                RetryPlanet2Server();
                return;
            }

            if (GameState == PortalNightsGameState.Planet1_RewardChoice)
            {
                UpdateRewardChoiceInputServer();
                return;
            }

            if (GameState == PortalNightsGameState.Planet2_ClearArea || GameState == PortalNightsGameState.Planet2_DefendSphere)
            {
                UpdatePlanet2CombatServer();
                return;
            }

            if (GameState == PortalNightsGameState.Planet1_Defense || GameState == PortalNightsGameState.Planet1_PortalReady)
            {
                UpdateWaveServer();
            }
        }

        public void RegisterEnemy(PortalNightsEnemy enemy)
        {
            if (enemy != null && !enemies.Contains(enemy))
            {
                enemies.Add(enemy);
                UpdateEnemyCountServer();
            }
        }

        public void UnregisterEnemy(PortalNightsEnemy enemy)
        {
            if (enemy != null && enemies.Remove(enemy))
            {
                UpdateEnemyCountServer();
            }
        }

        public void RegisterBuildPoint(PortalNightsBuildPoint buildPoint)
        {
            if (buildPoint != null && !buildPoints.Contains(buildPoint))
            {
                buildPoints.Add(buildPoint);
            }
        }

        public void UnregisterBuildPoint(PortalNightsBuildPoint buildPoint)
        {
            buildPoints.Remove(buildPoint);
        }

        public void RegisterAlly(PortalNightsAlly ally)
        {
            if (ally != null && !allies.Contains(ally))
            {
                allies.Add(ally);
            }
        }

        public void UnregisterAlly(PortalNightsAlly ally)
        {
            allies.Remove(ally);
        }

        public void EnemyKilled(PortalNightsEnemy enemy, int reward, Vector3 position)
        {
            if (!IsServer)
            {
                return;
            }

            UnregisterEnemy(enemy);
            enemiesRemaining.Value = Mathf.Max(0, enemiesRemaining.Value - 1);
            if (GameState == PortalNightsGameState.Planet1_Defense || GameState == PortalNightsGameState.Planet1_PortalReady || GameState == PortalNightsGameState.Planet1_RewardChoice)
            {
                if (enemy.AssignedLane == PortalNightsLane.Left)
                {
                    leftLaneEnemies.Value = Mathf.Max(0, leftLaneEnemies.Value - 1);
                }
                else
                {
                    rightLaneEnemies.Value = Mathf.Max(0, rightLaneEnemies.Value - 1);
                }
            }

            coins.Value += Mathf.Max(0, reward);
            SpawnCoinPickupServer(position + Vector3.up * 0.7f, reward);
            TrySpawnDropPickupServer(position + Vector3.up * 0.75f, reward);
            EnemyKilledClientRpc(position, reward);
            HandleProgressionEnemyKilledServer();
        }

        public void DamageCoreServer(float amount, Vector3 position)
        {
            if (!IsServer || coreHealth == null || gameOver.Value)
            {
                return;
            }

            coreHealth.DamageServer(amount);
            CoreHitClientRpc(position, amount);
        }

        public void DamageObjectiveServer(PortalNightsHealth target, float amount, Vector3 position)
        {
            if (!IsServer || target == null || gameOver.Value)
            {
                return;
            }

            if (target == planet2SphereHealth)
            {
                planet2SphereHealth.DamageServer(amount);
                CoreHitClientRpc(position, amount);
                return;
            }

            DamageCoreServer(amount, position);
        }

        public PortalNightsEnemy GetClosestEnemy(Vector3 position, float range)
        {
            PortalNightsEnemy best = null;
            float bestSqr = range * range;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                PortalNightsEnemy enemy = enemies[i];
                if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
                {
                    enemies.RemoveAt(i);
                    continue;
                }

                float sqr = (enemy.transform.position - position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = enemy;
                }
            }

            return best;
        }

        public PortalNightsEnemy GetShotTarget(Vector3 origin, Vector3 direction, float range, float radius)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                PortalNightsEnemy rayEnemy = hit.collider.GetComponentInParent<PortalNightsEnemy>();
                if (rayEnemy != null && rayEnemy.Health != null && !rayEnemy.Health.IsDead)
                {
                    return rayEnemy;
                }
            }

            PortalNightsEnemy best = null;
            float bestProjection = range;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                PortalNightsEnemy enemy = enemies[i];
                if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
                {
                    enemies.RemoveAt(i);
                    continue;
                }

                Vector3 toEnemy = enemy.AimPoint - origin;
                float projection = Vector3.Dot(direction, toEnemy);
                if (projection <= 0f || projection > range)
                {
                    continue;
                }

                float missDistance = Vector3.Cross(direction, toEnemy).magnitude;
                float allowed = radius + projection * 0.018f;
                if (missDistance <= allowed && projection < bestProjection)
                {
                    bestProjection = projection;
                    best = enemy;
                }
            }

            return best;
        }

        public PortalNightsPlayerController GetClosestLivingPlayer(Vector3 position, float range)
        {
            PortalNightsPlayerController best = null;
            float bestSqr = range * range;
            PortalNightsPlayerController[] players = FindObjectsByType<PortalNightsPlayerController>(FindObjectsSortMode.None);
            foreach (PortalNightsPlayerController player in players)
            {
                if (player == null || player.Health == null || player.Health.IsDead)
                {
                    continue;
                }

                float sqr = (player.transform.position - position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = player;
                }
            }

            return best;
        }

        public PortalNightsHealth GetEnemyTargetHealth(Vector3 position, float playerRange)
        {
            if (GameState == PortalNightsGameState.Planet2_ClearArea)
            {
                PortalNightsPlayerController planet2Player = GetClosestLivingPlayer(position, 120f);
                return planet2Player == null ? null : planet2Player.Health;
            }

            if (GameState == PortalNightsGameState.Planet2_DefendSphere || GameState == PortalNightsGameState.Failed)
            {
                return planet2SphereHealth;
            }

            PortalNightsPlayerController player = GetClosestLivingPlayer(position, playerRange);
            if (player != null)
            {
                return player.Health;
            }

            return coreHealth;
        }

        public PortalNightsLanePath GetLanePath(PortalNightsLane lane)
        {
            return lane == PortalNightsLane.Left ? leftLanePath : rightLanePath;
        }

        public float GetLanePathLength(PortalNightsLane lane)
        {
            PortalNightsLanePath path = GetLanePath(lane);
            return path == null ? 0f : path.PathLength;
        }

        public PortalNightsBuildPoint GetClosestBuildPoint(Vector3 position, float range, bool requireAvailable)
        {
            PortalNightsBuildPoint best = null;
            float bestSqr = range * range;
            for (int i = buildPoints.Count - 1; i >= 0; i--)
            {
                PortalNightsBuildPoint point = buildPoints[i];
                if (point == null)
                {
                    buildPoints.RemoveAt(i);
                    continue;
                }

                if (requireAvailable && !point.CanInteract)
                {
                    continue;
                }

                float sqr = (point.transform.position - position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = point;
                }
            }

            return best;
        }

        public void TryInteractNearServer(Vector3 playerPosition, ulong clientId)
        {
            if (!IsServer || GameOver)
            {
                return;
            }

            if (GameState == PortalNightsGameState.Planet1_PortalReady && IsNearPortal(playerPosition))
            {
                EnterPortalToPlanet2();
                return;
            }

            if (GameState == PortalNightsGameState.Planet2_SphereReady && IsNearSphere(playerPosition))
            {
                ActivateSphere();
                return;
            }

            TryBuildNearServer(playerPosition, clientId);
        }

        public void TryBuildNearServer(Vector3 playerPosition, ulong clientId)
        {
            if (!IsServer || GameOver || GameState != PortalNightsGameState.Planet1_Defense && GameState != PortalNightsGameState.Planet1_PortalReady)
            {
                return;
            }

            PortalNightsBuildPoint point = GetClosestBuildPoint(playerPosition, buildInteractionRange, false);
            if (point == null)
            {
                SendToast(clientId, PortalNightsLocalization.Text("toast.noPad"));
                return;
            }

            if (point.IsMaxed)
            {
                SendToast(clientId, PortalNightsLocalization.Text("toast.turretMaxed"));
                return;
            }

            int interactionCost = point.Cost;
            int nextLevel = point.NextLevel;
            if (coins.Value < interactionCost)
            {
                SendToast(clientId, PortalNightsLocalization.Text("toast.notEnoughCoins"));
                return;
            }

            if (point.IsAvailable)
            {
                PortalNightsAlly turret = SpawnTurretServer(point.BuildPosition, point.BuildRotation, clientId);
                if (turret == null)
                {
                    SendToast(clientId, PortalNightsLocalization.Text("toast.turretMissing"));
                    return;
                }

                coins.Value -= interactionCost;
                turret.SetLevelServer(1);
                point.AttachTurretServer(turret, 1);
                RefreshRequiredTurretProgressServer();
                CheckPortalUnlock();
                SendToast(clientId, PortalNightsLocalization.Text("toast.turretBuilt"));
            }
            else if (!point.IsMaxed)
            {
                PortalNightsAlly turret = point.SpawnedTurret;
                if (turret == null)
                {
                    turret = SpawnTurretServer(point.BuildPosition, point.BuildRotation, clientId);
                    if (turret == null)
                    {
                        SendToast(clientId, PortalNightsLocalization.Text("toast.turretMissing"));
                        return;
                    }

                    point.AttachTurretServer(turret, point.Level);
                }

                coins.Value -= interactionCost;
                turret.SetLevelServer(nextLevel);
                point.SetLevelServer(nextLevel);
                RefreshRequiredTurretProgressServer();
                CheckPortalUnlock();
                SendToast(clientId, PortalNightsLocalization.Text("toast.turretUpgraded"));
            }
            else
            {
                SendToast(clientId, PortalNightsLocalization.Text("toast.turretMaxed"));
            }
        }

        public Vector3 GetPlayerSpawnPosition(ulong clientId)
        {
            if (IsPlanet2State(GameState))
            {
                Vector3 basePosition = planet2ArrivalPoint == null ? planet2Center + new Vector3(0f, 1.12f, -30f) : planet2ArrivalPoint.position;
                return basePosition + new Vector3((int)(clientId % 3) * 1.8f, 0f, 0f);
            }

            if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
            {
                return new Vector3(0f, 1.55f, -10.75f);
            }

            int index = (int)(clientId % (ulong)playerSpawnPoints.Length);
            Vector3 position = playerSpawnPoints[index] == null ? new Vector3(0f, 1.55f, -10.75f) : playerSpawnPoints[index].position;
            position.y = Mathf.Max(position.y, 1.55f);
            position.z = Mathf.Max(position.z, -10.75f);
            return position;
        }

        public Quaternion GetPlayerSpawnRotation(ulong clientId)
        {
            if (IsPlanet2State(GameState))
            {
                return Quaternion.LookRotation(Vector3.forward, Vector3.up);
            }

            if (playerSpawnPoints == null || playerSpawnPoints.Length == 0)
            {
                return Quaternion.identity;
            }

            int index = (int)(clientId % (ulong)playerSpawnPoints.Length);
            return playerSpawnPoints[index] == null ? Quaternion.identity : playerSpawnPoints[index].rotation;
        }

        private void InitializeServerState()
        {
            initialized = true;
            EnsurePlanet2Area();
            waveNumber.Value = 0;
            coins.Value = startingCoins;
            enemiesAlive.Value = 0;
            enemiesRemaining.Value = 0;
            leftLaneEnemies.Value = 0;
            rightLaneEnemies.Value = 0;
            waveRunning.Value = false;
            gameOver.Value = false;
            nextWaveTimer.Value = Mathf.Max(firstWaveDelay, 12f);
            chargeVfxShown = false;
            planet2DefenseWaveIndex = 0;
            turretDamageBoostTimer = 0f;
            turretRunDamageMultiplier = 1f;
            SetGameState(PortalNightsGameState.Planet1_Defense);

            if (coreHealth != null)
            {
                coreHealth.Died -= HandleCoreDeath;
                coreHealth.Died += HandleCoreDeath;
                coreHealth.ServerInitialize(Mathf.Max(coreHealth.BaseMaxHealth, 1500f), true);
            }

            ClearRuntimeEnemiesServer();
            ClearRuntimeAlliesServer();
            foreach (PortalNightsPlayerController player in FindObjectsByType<PortalNightsPlayerController>(FindObjectsSortMode.None))
            {
                player.ResetRunProgressionServer();
            }
            RefreshRequiredTurretProgressServer();
            if (planet2SphereHealth != null)
            {
                planet2SphereHealth.ServerInitialize(500f, true);
                planet2SphereHealth.gameObject.SetActive(false);
            }
        }

        private IEnumerator InitializeServerStateAfterSpawn()
        {
            yield return null;
            InitializeServerState();
            initializeServerStateRoutine = null;
        }

        private void UpdateWaveServer()
        {
            if (!waveRunning.Value)
            {
                if (waveNumber.Value >= planet1FinalWave)
                {
                    nextWaveTimer.Value = 0f;
                    CheckPortalUnlock();
                    return;
                }

                nextWaveTimer.Value = Mathf.Max(0f, nextWaveTimer.Value - Time.deltaTime);
                if (!chargeVfxShown && nextWaveTimer.Value <= 3f)
                {
                    chargeVfxShown = true;
                    PortalChargingClientRpc(portalSpawn == null ? new Vector3(0f, 2f, 24f) : portalSpawn.position + Vector3.up * 2f);
                }

                if (nextWaveTimer.Value <= 0f)
                {
                    StartNextWaveServer();
                }
                return;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f && HasEnemiesLeftToSpawn())
            {
                SpawnNextEnemyServer();
                spawnTimer = activeWave.spawnInterval;
            }

            if (!HasEnemiesLeftToSpawn() && enemiesAlive.Value <= 0)
            {
                CompletePlanet1Wave(waveNumber.Value);
            }
        }

        private void StartNextWaveServer()
        {
            if (waveNumber.Value >= planet1FinalWave)
            {
                CheckPortalUnlock();
                return;
            }

            waveNumber.Value += 1;
            int smallCount = 3 + waveNumber.Value * 2;
            int bruteCount = waveNumber.Value < 3 ? 0 : Mathf.Max(1, waveNumber.Value / 3);
            float interval = Mathf.Max(0.72f, 1.55f - waveNumber.Value * 0.045f);
            activeWave = new PortalNightsWaveDefinition(smallCount, bruteCount, interval);
            spawnedSmall = 0;
            spawnedBrute = 0;
            spawnedLeftSmall = 0;
            spawnedRightSmall = 0;
            spawnedLeftBrute = 0;
            spawnedRightBrute = 0;
            SplitLaneQuota(smallCount, ref oddWaveExtraToLeft, out leftSmallQuota, out rightSmallQuota);
            SplitLaneQuota(bruteCount, ref oddWaveExtraToLeft, out leftBruteQuota, out rightBruteQuota);
            leftLaneEnemies.Value = 0;
            rightLaneEnemies.Value = 0;
            enemiesRemaining.Value = activeWave.TotalEnemies;
            waveRunning.Value = true;
            spawnTimer = 0.15f;
        }

        private bool HasEnemiesLeftToSpawn()
        {
            return spawnedSmall < activeWave.smallCount || spawnedBrute < activeWave.bruteCount;
        }

        private void SpawnNextEnemyServer()
        {
            bool shouldSpawnBrute = spawnedBrute < activeWave.bruteCount && (spawnedSmall >= activeWave.smallCount || (spawnedSmall + spawnedBrute) % 5 == 4);
            PortalNightsLane lane = GetNextSpawnLane(shouldSpawnBrute);
            PortalNightsLanePath lanePath = GetLanePath(lane);
            GameObject prefab = shouldSpawnBrute ? bruteEnemyPrefab : smallEnemyPrefab;
            if (prefab == null)
            {
                Debug.LogWarning("[PortalNights] Enemy prefab is missing.", this);
                return;
            }

            Vector3 pathStart = portalSpawn == null ? new Vector3(0f, 0.2f, 24f) : portalSpawn.position;
            if (lanePath != null && lanePath.TryGetWaypoint(0, out Vector3 waypointStart))
            {
                pathStart = waypointStart;
            }

            Vector2 offset2D = Random.insideUnitCircle * Mathf.Min(spawnSpread, 1.2f);
            Vector3 spawnPosition = pathStart + new Vector3(offset2D.x, 0f, offset2D.y);
            Quaternion spawnRotation = portalSpawn == null ? Quaternion.identity : portalSpawn.rotation;
            GameObject enemyObject = Instantiate(prefab, spawnPosition, spawnRotation);
            PortalNightsEnemy enemy = enemyObject.GetComponent<PortalNightsEnemy>();
            NetworkObject networkObject = enemyObject.GetComponent<NetworkObject>();
            if (networkObject != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                networkObject.Spawn(true);
            }

            if (enemy != null)
            {
                enemy.ConfigureForWave(shouldSpawnBrute ? PortalNightsEnemyKind.Brute : PortalNightsEnemyKind.Small, waveNumber.Value, shouldSpawnBrute ? bruteEnemyReward : smallEnemyReward);
                enemy.ConfigureLaneServer(lane, lanePath);
            }

            if (shouldSpawnBrute)
            {
                spawnedBrute++;
                if (lane == PortalNightsLane.Left)
                {
                    spawnedLeftBrute++;
                    leftLaneEnemies.Value++;
                }
                else
                {
                    spawnedRightBrute++;
                    rightLaneEnemies.Value++;
                }
            }
            else
            {
                spawnedSmall++;
                if (lane == PortalNightsLane.Left)
                {
                    spawnedLeftSmall++;
                    leftLaneEnemies.Value++;
                }
                else
                {
                    spawnedRightSmall++;
                    rightLaneEnemies.Value++;
                }
            }
        }

        private static void SplitLaneQuota(int total, ref bool extraToLeft, out int left, out int right)
        {
            left = total / 2;
            right = total / 2;
            if (total % 2 != 0)
            {
                if (extraToLeft)
                {
                    left++;
                }
                else
                {
                    right++;
                }

                extraToLeft = !extraToLeft;
            }
        }

        private PortalNightsLane GetNextSpawnLane(bool brute)
        {
            if (brute)
            {
                bool leftAvailable = spawnedLeftBrute < leftBruteQuota;
                bool rightAvailable = spawnedRightBrute < rightBruteQuota;
                if (leftAvailable && rightAvailable)
                {
                    return spawnedLeftBrute <= spawnedRightBrute ? PortalNightsLane.Left : PortalNightsLane.Right;
                }

                return leftAvailable ? PortalNightsLane.Left : PortalNightsLane.Right;
            }

            bool canSpawnLeft = spawnedLeftSmall < leftSmallQuota;
            bool canSpawnRight = spawnedRightSmall < rightSmallQuota;
            if (canSpawnLeft && canSpawnRight)
            {
                return spawnedLeftSmall <= spawnedRightSmall ? PortalNightsLane.Left : PortalNightsLane.Right;
            }

            return canSpawnLeft ? PortalNightsLane.Left : PortalNightsLane.Right;
        }

        private PortalNightsAlly SpawnTurretServer(Vector3 position, Quaternion rotation, ulong ownerClientId)
        {
            if (turretPrefab == null)
            {
                Debug.LogWarning("[PortalNights] Turret prefab is missing.", this);
                return null;
            }

            GameObject turretObject = Instantiate(turretPrefab, position, rotation);
            NetworkObject networkObject = turretObject.GetComponent<NetworkObject>();
            if (networkObject != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                networkObject.Spawn(true);
            }

            return turretObject.GetComponent<PortalNightsAlly>();
        }

        private void SpawnCoinPickupServer(Vector3 position, int amount)
        {
            if (coinPickupPrefab == null)
            {
                return;
            }

            GameObject coinObject = Instantiate(coinPickupPrefab, position, Quaternion.identity);
            PortalNightsCoinPickup coinPickup = coinObject.GetComponent<PortalNightsCoinPickup>();
            if (coinPickup != null)
            {
                coinPickup.InitializeServer(amount);
            }

            NetworkObject networkObject = coinObject.GetComponent<NetworkObject>();
            if (networkObject != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                networkObject.Spawn(true);
            }
        }

        private void UpdateEnemyCountServer()
        {
            if (!IsServer)
            {
                return;
            }

            int count = 0;
            for (int i = enemies.Count - 1; i >= 0; i--)
            {
                PortalNightsEnemy enemy = enemies[i];
                if (enemy == null || enemy.Health == null || enemy.Health.IsDead)
                {
                    enemies.RemoveAt(i);
                    continue;
                }

                count++;
            }

            enemiesAlive.Value = count;
        }

        private void HandleCoreDeath(PortalNightsHealth deadCore)
        {
            if (!IsServer || gameOver.Value)
            {
                return;
            }

            gameOver.Value = true;
            waveRunning.Value = false;
            nextWaveTimer.Value = 0f;
            ShowGameOverClientRpc();
        }

        private void RestartServer()
        {
            InitializeServerState();
        }

        public void StartPlanet1()
        {
            if (!IsServer)
            {
                return;
            }

            InitializeServerState();
        }

        private void SetGameState(PortalNightsGameState newState)
        {
            if (!PortalNightsNet.ServerCanWrite(this))
            {
                return;
            }

            if (GameState == newState)
            {
                return;
            }

            gameState.Value = (int)newState;
            Debug.Log("[PortalNights] State -> " + newState, this);
            StateChangedClientRpc(newState.ToString());
        }

        private void CompletePlanet1Wave(int completedWave)
        {
            waveRunning.Value = false;
            chargeVfxShown = false;
            int bonus = GetWaveCompletionBonus(completedWave);
            if (bonus > 0)
            {
                coins.Value += bonus;
            }

            WaveClearedClientRpc(completedWave, bonus);
            if (completedWave == 3 || completedWave == 6 || completedWave == planet1FinalWave)
            {
                nextWaveTimer.Value = 0f;
                SetGameState(PortalNightsGameState.Planet1_RewardChoice);
                RewardChoiceClientRpc(completedWave);
                return;
            }

            if (completedWave >= planet1FinalWave)
            {
                nextWaveTimer.Value = 0f;
                CheckPortalUnlock();
                return;
            }

            nextWaveTimer.Value = waveBreakDuration;
            CheckPortalUnlock();
        }

        private static int GetWaveCompletionBonus(int completedWave)
        {
            if (completedWave <= 0)
            {
                return 0;
            }

            if (completedWave <= 3)
            {
                return 75;
            }

            if (completedWave <= 6)
            {
                return 125;
            }

            return 175;
        }

        private void UpdateRewardChoiceInputServer()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame)
            {
                ApplyRewardChoiceServer(1);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame)
            {
                ApplyRewardChoiceServer(2);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame)
            {
                ApplyRewardChoiceServer(3);
            }
        }

        private void ApplyRewardChoiceServer(int choice)
        {
            if (choice == 1)
            {
                foreach (PortalNightsPlayerController player in FindObjectsByType<PortalNightsPlayerController>(FindObjectsSortMode.None))
                {
                    player.ApplyWeaponDamageRunBonusServer(0.15f);
                }

                BroadcastToastClientRpc("+15% PLAYER WEAPON DAMAGE");
            }
            else if (choice == 2)
            {
                turretRunDamageMultiplier += 0.15f;
                BroadcastToastClientRpc("+15% TURRET DAMAGE");
            }
            else
            {
                coreHealth?.HealServer(150f);
                BroadcastToastClientRpc("CORE REPAIRED +150");
            }

            if (waveNumber.Value >= planet1FinalWave)
            {
                CheckPortalUnlock();
                if (GameState != PortalNightsGameState.Planet1_PortalReady)
                {
                    SetGameState(PortalNightsGameState.Planet1_Defense);
                }
            }
            else
            {
                SetGameState(PortalNightsGameState.Planet1_Defense);
                nextWaveTimer.Value = waveBreakDuration;
            }
        }

        public PortalNightsBuildPoint[] GetRequiredBuildPads()
        {
            List<PortalNightsBuildPoint> required = new List<PortalNightsBuildPoint>();
            for (int i = buildPoints.Count - 1; i >= 0; i--)
            {
                PortalNightsBuildPoint point = buildPoints[i];
                if (point == null)
                {
                    buildPoints.RemoveAt(i);
                    continue;
                }

                if (countAllBuildPadsAsRequired || point.RequiredForPortal)
                {
                    required.Add(point);
                }
            }

            return required.ToArray();
        }

        public bool AreAllRequiredTurretsBuilt()
        {
            PortalNightsBuildPoint[] required = GetRequiredBuildPads();
            if (required.Length == 0)
            {
                return false;
            }

            foreach (PortalNightsBuildPoint point in required)
            {
                if (point == null || point.Level <= 0)
                {
                    return false;
                }
            }

            return true;
        }

        public bool AreAllRequiredTurretsMaxLevel()
        {
            PortalNightsBuildPoint[] required = GetRequiredBuildPads();
            if (required.Length == 0)
            {
                return false;
            }

            foreach (PortalNightsBuildPoint point in required)
            {
                if (point == null || !point.IsMaxed)
                {
                    return false;
                }
            }

            return true;
        }

        public bool CanOpenPortal()
        {
            return waveNumber.Value >= planet1FinalWave
                && enemiesRemaining.Value <= 0
                && enemiesAlive.Value <= 0
                && AreAllRequiredTurretsBuilt()
                && AreAllRequiredTurretsMaxLevel()
                && coreHealth != null
                && !coreHealth.IsDead;
        }

        private void CheckPortalUnlock()
        {
            RefreshRequiredTurretProgressServer();
            if (CanOpenPortal())
            {
                bool wasReady = GameState == PortalNightsGameState.Planet1_PortalReady;
                nextWaveTimer.Value = 0f;
                waveRunning.Value = false;
                SetGameState(PortalNightsGameState.Planet1_PortalReady);
                if (!wasReady)
                {
                    PortalReadyClientRpc(portalSpawn == null ? Vector3.zero : portalSpawn.position + Vector3.up * 3.5f);
                }
            }
            else if (waveNumber.Value >= planet1FinalWave && GameState != PortalNightsGameState.Planet1_RewardChoice)
            {
                SetGameState(PortalNightsGameState.Planet1_Defense);
            }
        }

        private void RefreshRequiredTurretProgressServer()
        {
            PortalNightsBuildPoint[] required = GetRequiredBuildPads();
            int built = 0;
            int maxed = 0;
            foreach (PortalNightsBuildPoint point in required)
            {
                if (point == null)
                {
                    continue;
                }

                if (point.Level > 0)
                {
                    built++;
                }

                if (point.IsMaxed)
                {
                    maxed++;
                }
            }

            requiredTurretsTotal.Value = required.Length;
            requiredTurretsBuilt.Value = built;
            requiredTurretsMaxed.Value = maxed;
        }

        private bool IsNearPortal(Vector3 position)
        {
            if (portalSpawn == null)
            {
                return false;
            }

            return PortalNightsMath.Flat(portalSpawn.position - position).sqrMagnitude <= portalInteractionRange * portalInteractionRange;
        }

        public bool IsPlayerNearPortal(Vector3 position)
        {
            return IsNearPortal(position);
        }

        private bool IsNearSphere(Vector3 position)
        {
            if (planet2Sphere == null)
            {
                return false;
            }

            return PortalNightsMath.Flat(planet2Sphere.position - position).sqrMagnitude <= portalInteractionRange * portalInteractionRange;
        }

        public bool IsPlayerNearSphere(Vector3 position)
        {
            return IsNearSphere(position);
        }

        public void EnterPortalToPlanet2()
        {
            if (!IsServer || GameState != PortalNightsGameState.Planet1_PortalReady)
            {
                return;
            }

            SetGameState(PortalNightsGameState.PortalTravel);
            ClearRuntimeEnemiesServer();
            waveRunning.Value = false;
            nextWaveTimer.Value = 0f;
            TeleportPlayersToPlanet2Server();
            StartPlanet2ClearArea();
        }

        public void StartPlanet2ClearArea()
        {
            if (!IsServer)
            {
                return;
            }

            EnsurePlanet2Area();
            if (planet2SphereHealth != null)
            {
                planet2SphereHealth.gameObject.SetActive(true);
                planet2SphereHealth.ServerInitialize(500f, true);
            }

            planet2DefenseWaveIndex = 0;
            SpawnPlanet2GroupServer(6, 2);
            SetGameState(PortalNightsGameState.Planet2_ClearArea);
            BroadcastToastClientRpc("PLANET 2: CRYSTAL MOON");
        }

        public void ActivateSphere()
        {
            if (!IsServer || GameState != PortalNightsGameState.Planet2_SphereReady)
            {
                return;
            }

            if (planet2SphereHealth != null)
            {
                planet2SphereHealth.ServerInitialize(500f, true);
            }

            StartSphereDefense();
        }

        public void StartSphereDefense()
        {
            if (!IsServer)
            {
                return;
            }

            planet2DefenseWaveIndex = 0;
            SetGameState(PortalNightsGameState.Planet2_DefendSphere);
            StartNextSphereDefenseWaveServer();
            BroadcastToastClientRpc("DEFEND THE SPHERE");
        }

        private void StartNextSphereDefenseWaveServer()
        {
            planet2DefenseWaveIndex++;
            if (planet2DefenseWaveIndex == 1)
            {
                SpawnPlanet2GroupServer(6, 0);
            }
            else if (planet2DefenseWaveIndex == 2)
            {
                SpawnPlanet2GroupServer(8, 0);
            }
            else if (planet2DefenseWaveIndex == 3)
            {
                SpawnPlanet2GroupServer(10, 1);
            }
            else
            {
                CompletePlanet2();
            }
        }

        public void CompletePlanet2()
        {
            if (!IsServer)
            {
                return;
            }

            SetGameState(PortalNightsGameState.Planet2_Cleared);
            BroadcastToastClientRpc("PLANET CLEARED - NEXT PORTAL COMING SOON");
        }

        private void RetryPlanet2Server()
        {
            if (!IsServer)
            {
                return;
            }

            ClearRuntimeEnemiesServer();
            TeleportPlayersToPlanet2Server();
            StartPlanet2ClearArea();
        }

        private void UpdatePlanet2CombatServer()
        {
            planet2SpawnTimer -= Time.deltaTime;
            if (planet2SpawnTimer <= 0f && HasPlanet2EnemiesLeftToSpawn())
            {
                SpawnNextPlanet2EnemyServer();
                planet2SpawnTimer = 0.72f;
            }

            if (!HasPlanet2EnemiesLeftToSpawn() && enemiesAlive.Value <= 0)
            {
                if (GameState == PortalNightsGameState.Planet2_ClearArea)
                {
                    SetGameState(PortalNightsGameState.Planet2_SphereReady);
                    BroadcastToastClientRpc("SPHERE READY");
                    SphereReadyClientRpc(planet2Sphere == null ? Vector3.zero : planet2Sphere.position + Vector3.up * 1.4f);
                }
                else if (GameState == PortalNightsGameState.Planet2_DefendSphere)
                {
                    StartNextSphereDefenseWaveServer();
                }
            }
        }

        private void SpawnPlanet2GroupServer(int smallCount, int bruteCount)
        {
            ClearRuntimeEnemiesServer();
            planet2EnemiesToSpawn = Mathf.Max(0, smallCount);
            planet2BrutesToSpawn = Mathf.Max(0, bruteCount);
            planet2SpawnedSmall = 0;
            planet2SpawnedBrute = 0;
            enemiesRemaining.Value = planet2EnemiesToSpawn + planet2BrutesToSpawn;
            leftLaneEnemies.Value = 0;
            rightLaneEnemies.Value = 0;
            planet2SpawnTimer = 0.15f;
        }

        private bool HasPlanet2EnemiesLeftToSpawn()
        {
            return planet2SpawnedSmall < planet2EnemiesToSpawn || planet2SpawnedBrute < planet2BrutesToSpawn;
        }

        private void SpawnNextPlanet2EnemyServer()
        {
            bool brute = planet2SpawnedBrute < planet2BrutesToSpawn && (planet2SpawnedSmall >= planet2EnemiesToSpawn || (planet2SpawnedSmall + planet2SpawnedBrute) % 5 == 4);
            GameObject prefab = brute ? bruteEnemyPrefab : smallEnemyPrefab;
            if (prefab == null)
            {
                return;
            }

            Transform spawn = GetPlanet2SpawnPoint(planet2SpawnedSmall + planet2SpawnedBrute);
            Vector3 spawnPosition = spawn == null ? planet2Center + new Vector3(0f, 0.4f, -12f) : spawn.position;
            spawnPosition += new Vector3(Random.Range(-1.4f, 1.4f), 0f, Random.Range(-1.4f, 1.4f));
            GameObject enemyObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
            PortalNightsEnemy enemy = enemyObject.GetComponent<PortalNightsEnemy>();
            NetworkObject networkObject = enemyObject.GetComponent<NetworkObject>();
            if (networkObject != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                networkObject.Spawn(true);
            }

            if (enemy != null)
            {
                enemy.ConfigureForWave(brute ? PortalNightsEnemyKind.Brute : PortalNightsEnemyKind.Small, Mathf.Max(1, waveNumber.Value), brute ? bruteEnemyReward : smallEnemyReward);
                enemy.ConfigureLaneServer(PortalNightsLane.Left, null);
            }

            if (brute)
            {
                planet2SpawnedBrute++;
            }
            else
            {
                planet2SpawnedSmall++;
            }
        }

        private Transform GetPlanet2SpawnPoint(int index)
        {
            if (planet2EnemySpawnPoints == null || planet2EnemySpawnPoints.Length == 0)
            {
                return null;
            }

            return planet2EnemySpawnPoints[Mathf.Abs(index) % planet2EnemySpawnPoints.Length];
        }

        private void HandleProgressionEnemyKilledServer()
        {
            if (GameState == PortalNightsGameState.Planet1_Defense || GameState == PortalNightsGameState.Planet1_PortalReady)
            {
                CheckPortalUnlock();
            }
        }

        private void TeleportPlayersToPlanet2Server()
        {
            EnsurePlanet2Area();
            PortalNightsPlayerController[] players = FindObjectsByType<PortalNightsPlayerController>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                PortalNightsPlayerController player = players[i];
                if (player == null)
                {
                    continue;
                }

                Vector3 offset = new Vector3((i - players.Length * 0.5f) * 1.8f, 0f, 0f);
                Vector3 basePosition = planet2ArrivalPoint == null ? planet2Center + new Vector3(0f, 1.12f, -30f) : planet2ArrivalPoint.position;
                player.transform.SetPositionAndRotation(basePosition + offset, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            }

            TeleportPlayersClientRpc();
        }

        public bool TryClampPlayerPositionForCurrentArea(ref Vector3 position)
        {
            if (IsPlanet2State(GameState))
            {
                Vector2 center = new Vector2(planet2Center.x, planet2Center.z);
                Vector2 flat = new Vector2(position.x, position.z);
                Vector2 delta = flat - center;
                if (delta.magnitude > planet2Radius)
                {
                    flat = center + delta.normalized * planet2Radius;
                    position.x = flat.x;
                    position.z = flat.y;
                }

                return true;
            }

            position.x = Mathf.Clamp(position.x, planet1BoundsMin.x, planet1BoundsMax.x);
            position.z = Mathf.Clamp(position.z, planet1BoundsMin.z, planet1BoundsMax.z);
            return true;
        }

        public void ApplyPickupServer(PortalNightsPickupKind kind, PortalNightsPlayerController player, int coinAmount, Vector3 position)
        {
            if (!IsServer || player == null)
            {
                return;
            }

            if (kind == PortalNightsPickupKind.Coin)
            {
                coins.Value += Mathf.Max(1, coinAmount);
                BroadcastToastClientRpc("COINS +" + Mathf.Max(1, coinAmount));
            }
            else if (kind == PortalNightsPickupKind.Heal)
            {
                player.ApplyHealServer(40f);
                BroadcastToastClientRpc("HEAL +40");
            }
            else if (kind == PortalNightsPickupKind.Armor)
            {
                player.ApplyArmorBoostServer(temporaryBoostDuration);
                BroadcastToastClientRpc("ARMOR +30% 30S");
            }
            else if (kind == PortalNightsPickupKind.WeaponDamageBoost)
            {
                player.ApplyWeaponDamageBoostServer(temporaryBoostDuration);
                BroadcastToastClientRpc("WEAPON DAMAGE +20% 30S");
            }
            else if (kind == PortalNightsPickupKind.TurretDamageBoost)
            {
                turretDamageBoostTimer = Mathf.Max(turretDamageBoostTimer, temporaryBoostDuration);
                BroadcastToastClientRpc("TURRET DAMAGE +20% 30S");
            }

            PickupCollectedClientRpc(position, (int)kind);
        }

        private void TrySpawnDropPickupServer(Vector3 position, int reward)
        {
            if (Random.value > pickupDropChance)
            {
                return;
            }

            PortalNightsPickupKind kind = RollPickupKind();
            CreatePickupVisualServer(kind, position, kind == PortalNightsPickupKind.Coin ? Mathf.Max(8, reward) : 1);
        }

        private static PortalNightsPickupKind RollPickupKind()
        {
            float roll = Random.value;
            if (roll < 0.65f)
            {
                return PortalNightsPickupKind.Coin;
            }

            if (roll < 0.77f)
            {
                return PortalNightsPickupKind.Heal;
            }

            if (roll < 0.87f)
            {
                return PortalNightsPickupKind.Armor;
            }

            if (roll < 0.95f)
            {
                return PortalNightsPickupKind.WeaponDamageBoost;
            }

            return PortalNightsPickupKind.TurretDamageBoost;
        }

        private void CreatePickupVisualServer(PortalNightsPickupKind kind, Vector3 position, int coinAmount)
        {
            GameObject pickup = new GameObject("PN_Pickup_" + kind);
            pickup.transform.position = position;
            PortalNightsPickup pickupComponent = pickup.AddComponent<PortalNightsPickup>();
            pickupComponent.Configure(kind, coinAmount);

            Color color = GetPickupColor(kind);
            GameObject core = GameObject.CreatePrimitive(kind == PortalNightsPickupKind.Coin ? PrimitiveType.Cylinder : PrimitiveType.Sphere);
            core.name = "Visual";
            core.transform.SetParent(pickup.transform, false);
            core.transform.localScale = kind == PortalNightsPickupKind.Coin ? new Vector3(0.42f, 0.08f, 0.42f) : new Vector3(0.38f, 0.38f, 0.38f);
            Renderer renderer = core.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material = PortalNightsVfx.CreateRuntimeGlowMaterial(color, color * 2.8f);
            }

            foreach (Collider collider in pickup.GetComponentsInChildren<Collider>())
            {
                collider.isTrigger = true;
            }
        }

        private static Color GetPickupColor(PortalNightsPickupKind kind)
        {
            if (kind == PortalNightsPickupKind.Heal)
            {
                return new Color(0.25f, 1f, 0.35f, 1f);
            }

            if (kind == PortalNightsPickupKind.Armor)
            {
                return new Color(0.18f, 0.58f, 1f, 1f);
            }

            if (kind == PortalNightsPickupKind.WeaponDamageBoost)
            {
                return new Color(1f, 0.48f, 0.12f, 1f);
            }

            if (kind == PortalNightsPickupKind.TurretDamageBoost)
            {
                return new Color(0.1f, 0.92f, 1f, 1f);
            }

            return new Color(1f, 0.78f, 0.18f, 1f);
        }

        private static bool IsPlanet2State(PortalNightsGameState state)
        {
            return state == PortalNightsGameState.PortalTravel
                || state == PortalNightsGameState.Planet2_ClearArea
                || state == PortalNightsGameState.Planet2_SphereReady
                || state == PortalNightsGameState.Planet2_DefendSphere
                || state == PortalNightsGameState.Planet2_Cleared
                || state == PortalNightsGameState.Failed;
        }

        private void EnsurePlanet2Area()
        {
            if (planet2AreaCreated)
            {
                return;
            }

            planet2Radius = Mathf.Max(planet2Radius, 42f);

            Transform arenaRoot = GameObject.Find("PortalNightsArena")?.transform;
            if (arenaRoot == null)
            {
                GameObject root = new GameObject("PortalNightsArena");
                arenaRoot = root.transform;
            }

            Transform existing = arenaRoot.Find("Planet2_CrystalMoon");
            if (existing != null)
            {
                planet2Root = existing;
                planet2ArrivalPoint = existing.Find("ArrivalPoint");
                planet2Sphere = existing.Find("SphereObjective");
                planet2SphereHealth = planet2Sphere == null ? null : planet2Sphere.GetComponent<PortalNightsHealth>();
                if (planet2SphereHealth != null)
                {
                    planet2SphereHealth.Died -= HandleSphereDeath;
                    planet2SphereHealth.Died += HandleSphereDeath;
                }
                Transform spawns = existing.Find("EnemySpawnPoints");
                planet2EnemySpawnPoints = spawns == null ? null : GetChildren(spawns);
                planet2AreaCreated = true;
                return;
            }

            Material darkGround = PortalNightsVfx.CreateRuntimeGlowMaterial(new Color(0.045f, 0.055f, 0.085f, 1f), new Color(0.01f, 0.04f, 0.08f, 1f));
            Material cyanCrystal = PortalNightsVfx.CreateRuntimeGlowMaterial(new Color(0.1f, 0.85f, 1f, 1f), new Color(0.1f, 1.3f, 1.9f, 1f));
            Material purpleCrystal = PortalNightsVfx.CreateRuntimeGlowMaterial(new Color(0.45f, 0.16f, 1f, 1f), new Color(0.8f, 0.15f, 2.2f, 1f));

            GameObject rootObject = new GameObject("Planet2_CrystalMoon");
            rootObject.transform.SetParent(arenaRoot, false);
            rootObject.transform.position = planet2Center;
            planet2Root = rootObject.transform;

            GameObject floor = CreateRuntimePrimitive("CrystalMoon_Floor", PrimitiveType.Cylinder, planet2Root, Vector3.zero, new Vector3(86f, 0.36f, 86f), darkGround, true);
            floor.transform.localPosition = Vector3.down * 0.36f;

            CreateSegmentedRing("CrystalMoon_OuterRim", planet2Root, 42.5f, 36, new Vector3(5.4f, 0.12f, 0.34f), cyanCrystal, 0.08f);
            CreateSegmentedRing("CrystalMoon_InnerPulseRing", planet2Root, 18.5f, 20, new Vector3(3.4f, 0.08f, 0.22f), purpleCrystal, 0.12f);
            CreateRuntimePrimitive("CrystalMoon_CenterPlate", PrimitiveType.Cylinder, planet2Root, new Vector3(0f, 0.02f, 6f), new Vector3(12f, 0.1f, 12f), purpleCrystal, false);
            CreateRuntimePrimitive("CrystalMoon_NorthLane", PrimitiveType.Cube, planet2Root, new Vector3(0f, 0.06f, 17f), new Vector3(1.4f, 0.08f, 28f), cyanCrystal, false);
            CreateRuntimePrimitive("CrystalMoon_WestLane", PrimitiveType.Cube, planet2Root, new Vector3(-16f, 0.06f, 4f), new Vector3(28f, 0.08f, 1.1f), purpleCrystal, false);
            CreateRuntimePrimitive("CrystalMoon_EastLane", PrimitiveType.Cube, planet2Root, new Vector3(16f, 0.06f, 4f), new Vector3(28f, 0.08f, 1.1f), cyanCrystal, false);

            planet2ArrivalPoint = new GameObject("ArrivalPoint").transform;
            planet2ArrivalPoint.SetParent(planet2Root, false);
            planet2ArrivalPoint.localPosition = new Vector3(0f, 1.12f, -30f);

            GameObject arrivalPortal = CreateRuntimePrimitive("ArrivalPortal", PrimitiveType.Cylinder, planet2Root, new Vector3(0f, 0.12f, -31.5f), new Vector3(6.4f, 0.22f, 6.4f), purpleCrystal, false);
            arrivalPortal.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            GameObject sphere = new GameObject("SphereObjective");
            sphere.transform.SetParent(planet2Root, false);
            sphere.transform.localPosition = new Vector3(0f, 1.25f, 8f);
            planet2Sphere = sphere.transform;
            sphere.AddComponent<NetworkObject>();
            planet2SphereHealth = sphere.AddComponent<PortalNightsHealth>();
            planet2SphereHealth.SetBaseMaxHealth(500f);
            SphereCollider sphereCollider = sphere.AddComponent<SphereCollider>();
            sphereCollider.radius = 1.35f;
            sphereCollider.isTrigger = true;
            CreateRuntimePrimitive("Sphere_Core", PrimitiveType.Sphere, sphere.transform, Vector3.zero, new Vector3(2.2f, 2.2f, 2.2f), cyanCrystal, false);
            Light sphereLight = sphere.AddComponent<Light>();
            sphereLight.type = LightType.Point;
            sphereLight.color = new Color(0.25f, 0.9f, 1f);
            sphereLight.range = 9f;
            sphereLight.intensity = 1.35f;
            planet2SphereHealth.Died += HandleSphereDeath;

            Transform spawnRoot = new GameObject("EnemySpawnPoints").transform;
            spawnRoot.SetParent(planet2Root, false);
            planet2EnemySpawnPoints = new Transform[4];
            planet2EnemySpawnPoints[0] = new GameObject("Spawn_NorthWest").transform;
            planet2EnemySpawnPoints[0].SetParent(spawnRoot, false);
            planet2EnemySpawnPoints[0].localPosition = new Vector3(-24f, 0.35f, 27f);
            planet2EnemySpawnPoints[1] = new GameObject("Spawn_NorthEast").transform;
            planet2EnemySpawnPoints[1].SetParent(spawnRoot, false);
            planet2EnemySpawnPoints[1].localPosition = new Vector3(24f, 0.35f, 27f);
            planet2EnemySpawnPoints[2] = new GameObject("Spawn_WestRift").transform;
            planet2EnemySpawnPoints[2].SetParent(spawnRoot, false);
            planet2EnemySpawnPoints[2].localPosition = new Vector3(-31f, 0.35f, -5f);
            planet2EnemySpawnPoints[3] = new GameObject("Spawn_EastRift").transform;
            planet2EnemySpawnPoints[3].SetParent(spawnRoot, false);
            planet2EnemySpawnPoints[3].localPosition = new Vector3(31f, 0.35f, -5f);

            Transform crystals = new GameObject("DecorativeCrystals").transform;
            crystals.SetParent(planet2Root, false);
            for (int i = 0; i < 28; i++)
            {
                float angle = i * Mathf.PI * 2f / 28f;
                float radius = i % 3 == 0 ? 36f : i % 3 == 1 ? 30f : 23f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0.28f, Mathf.Sin(angle) * radius);
                GameObject crystal = CreateRuntimePrimitive("Crystal_" + i, PrimitiveType.Cylinder, crystals, position, new Vector3(0.75f, 2.2f + (i % 4) * 0.55f, 0.75f), i % 2 == 0 ? cyanCrystal : purpleCrystal, false);
                crystal.transform.localRotation = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-10f, 10f));
            }

            Transform boundaries = new GameObject("Boundaries").transform;
            boundaries.SetParent(planet2Root, false);
            for (int i = 0; i < 32; i++)
            {
                float angle = i * Mathf.PI * 2f / 32f;
                Vector3 position = new Vector3(Mathf.Cos(angle) * 43.5f, 0.95f, Mathf.Sin(angle) * 43.5f);
                GameObject wall = CreateRuntimePrimitive("Boundary_" + i, PrimitiveType.Cube, boundaries, position, new Vector3(5.2f, 1.9f, 0.45f), darkGround, true);
                wall.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
            }

            planet2AreaCreated = true;
            if (planet2SphereHealth != null)
            {
                planet2SphereHealth.gameObject.SetActive(false);
            }
        }

        private static Transform[] GetChildren(Transform parent)
        {
            Transform[] children = new Transform[parent.childCount];
            for (int i = 0; i < parent.childCount; i++)
            {
                children[i] = parent.GetChild(i);
            }

            return children;
        }

        private static GameObject CreateRuntimePrimitive(string name, PrimitiveType type, Transform parent, Vector3 localPosition, Vector3 localScale, Material material, bool solid)
        {
            GameObject gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localScale = localScale;
            Renderer renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null && material != null)
            {
                renderer.material = material;
            }

            Collider collider = gameObject.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = !solid;
            }

            return gameObject;
        }

        private static void CreateSegmentedRing(string name, Transform parent, float radius, int segments, Vector3 segmentScale, Material material, float y)
        {
            Transform ring = new GameObject(name).transform;
            ring.SetParent(parent, false);
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 position = new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
                GameObject segment = CreateRuntimePrimitive(name + "_Segment_" + i, PrimitiveType.Cube, ring, position, segmentScale, material, false);
                segment.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
            }
        }

        private void HandleSphereDeath(PortalNightsHealth health)
        {
            if (!IsServer || GameState != PortalNightsGameState.Planet2_DefendSphere)
            {
                return;
            }

            ClearRuntimeEnemiesServer();
            SetGameState(PortalNightsGameState.Failed);
            BroadcastToastClientRpc("SPHERE LOST - PRESS R TO RETRY");
        }

        private void UpdateRuntimeVisuals()
        {
            if (portalPrompt == null && portalSpawn != null)
            {
                portalPrompt = CreateWorldLabel("PortalPrompt", portalSpawn, new Vector3(0f, 2.8f, -1.2f), "E ENTER PORTAL", new Color(0.9f, 0.72f, 1f, 1f));
            }

            if (portalPrompt != null)
            {
                portalPrompt.gameObject.SetActive(PortalReady);
            }

            if (planet2Sphere != null)
            {
                float pulse = GameState == PortalNightsGameState.Planet2_SphereReady || GameState == PortalNightsGameState.Planet2_DefendSphere
                    ? 1f + Mathf.Sin(Time.time * 4f) * 0.05f
                    : 1f;
                planet2Sphere.localScale = Vector3.one * pulse;
            }
        }

        private static Transform CreateWorldLabel(string name, Transform parent, Vector3 localPosition, string text, Color color)
        {
            GameObject label = new GameObject(name);
            label.transform.SetParent(parent, false);
            label.transform.localPosition = localPosition;
            TextMesh textMesh = label.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.anchor = TextAnchor.MiddleCenter;
            textMesh.alignment = TextAlignment.Center;
            textMesh.fontSize = 56;
            textMesh.characterSize = 0.08f;
            textMesh.color = color;
            return label.transform;
        }

        private void ClearRuntimeEnemiesServer()
        {
            foreach (PortalNightsEnemy enemy in enemies.ToArray())
            {
                if (enemy == null)
                {
                    continue;
                }

                NetworkObject networkObject = enemy.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                {
                    networkObject.Despawn(true);
                }
                else
                {
                    Destroy(enemy.gameObject);
                }
            }

            enemies.Clear();
            enemiesAlive.Value = 0;
            leftLaneEnemies.Value = 0;
            rightLaneEnemies.Value = 0;
        }

        private void ClearRuntimeAlliesServer()
        {
            foreach (PortalNightsAlly ally in allies.ToArray())
            {
                if (ally == null)
                {
                    continue;
                }

                NetworkObject networkObject = ally.GetComponent<NetworkObject>();
                if (networkObject != null && networkObject.IsSpawned)
                {
                    networkObject.Despawn(true);
                }
                else
                {
                    Destroy(ally.gameObject);
                }
            }

            allies.Clear();
            foreach (PortalNightsBuildPoint point in buildPoints)
            {
                if (point != null)
                {
                    point.SetBuiltServer(false);
                }
            }
        }

        private void CacheSceneReferences()
        {
            if (coreHealth == null)
            {
                coreHealth = FindFirstObjectByType<PortalNightsHealth>();
            }

            if (hud == null)
            {
                hud = FindFirstObjectByType<PortalNightsHud>();
            }

            if (leftLanePath == null || rightLanePath == null)
            {
                PortalNightsLanePath[] lanePaths = FindObjectsByType<PortalNightsLanePath>(FindObjectsSortMode.None);
                foreach (PortalNightsLanePath lanePath in lanePaths)
                {
                    if (lanePath == null)
                    {
                        continue;
                    }

                    if (lanePath.Lane == PortalNightsLane.Left)
                    {
                        leftLanePath = lanePath;
                    }
                    else
                    {
                        rightLanePath = lanePath;
                    }
                }
            }

            buildPoints.Clear();
            buildPoints.AddRange(FindObjectsByType<PortalNightsBuildPoint>(FindObjectsSortMode.None));
        }

        private void MakeDefenseObjectsPlayerFriendly()
        {
            if (coreHealth != null)
            {
                foreach (Collider collider in coreHealth.GetComponentsInChildren<Collider>())
                {
                    collider.isTrigger = true;
                }
            }

            foreach (PortalNightsBuildPoint point in FindObjectsByType<PortalNightsBuildPoint>(FindObjectsSortMode.None))
            {
                point.MakeNonBlocking();
            }
        }

        private void SendToast(ulong clientId, string message)
        {
            ClientRpcParams target = new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            };
            ToastClientRpc(message, target);
        }

        [ClientRpc]
        private void ToastClientRpc(string message, ClientRpcParams rpcParams = default)
        {
            PortalNightsHud.Instance?.ShowToast(message);
        }

        [ClientRpc]
        private void EnemyKilledClientRpc(Vector3 position, int reward)
        {
            PortalNightsVfx.SpawnBurst(position + Vector3.up * 0.9f, new Color(1f, 0.72f, 0.18f, 1f), 1.15f, 24);
            PortalNightsVfx.SpawnFloatingText(position + Vector3.up * 2.4f, "+" + reward, new Color(1f, 0.82f, 0.25f, 1f));
        }

        [ClientRpc]
        private void CoreHitClientRpc(Vector3 position, float amount)
        {
            PortalNightsVfx.SpawnBurst(position + Vector3.up * 1.2f, new Color(0.1f, 0.68f, 1f, 1f), 0.9f, 16);
        }

        [ClientRpc]
        private void WaveClearedClientRpc(int completedWave, int bonus)
        {
            string message = bonus > 0
                ? $"{PortalNightsLocalization.Text("toast.waveClear")}  +{bonus} COINS"
                : PortalNightsLocalization.Text("toast.waveClear");
            PortalNightsHud.Instance?.ShowToast(message);
        }

        [ClientRpc]
        private void PortalChargingClientRpc(Vector3 position)
        {
            PortalNightsVfx.SpawnBurst(position, new Color(0.8f, 0.15f, 1f, 1f), 2.1f, 34);
            PortalNightsHud.Instance?.ShowToast("PORTAL CHARGING - BOTH LANES");
        }

        [ClientRpc]
        private void ShowGameOverClientRpc()
        {
            PortalNightsHud.Instance?.ShowToast(PortalNightsLocalization.Text("toast.coreLost"));
        }

        [ClientRpc]
        private void StateChangedClientRpc(string stateName)
        {
            PortalNightsHud.Instance?.ShowToast(stateName.Replace('_', ' '));
        }

        [ClientRpc]
        private void RewardChoiceClientRpc(int completedWave)
        {
            PortalNightsHud.Instance?.ShowToast($"WAVE {completedWave} REWARD - PRESS 1 / 2 / 3");
        }

        [ClientRpc]
        private void PortalReadyClientRpc(Vector3 position)
        {
            PortalNightsVfx.SpawnBurst(position, new Color(0.86f, 0.24f, 1f, 1f), 3.2f, 54);
            PortalNightsHud.Instance?.ShowToast("PORTAL READY");
        }

        [ClientRpc]
        private void SphereReadyClientRpc(Vector3 position)
        {
            PortalNightsVfx.SpawnBurst(position, new Color(0.18f, 0.9f, 1f, 1f), 2.1f, 34);
            PortalNightsHud.Instance?.ShowToast("E ACTIVATE SPHERE");
        }

        [ClientRpc]
        private void TeleportPlayersClientRpc()
        {
            PortalNightsHud.Instance?.ShowToast("PLANET 2: CRYSTAL MOON");
        }

        [ClientRpc]
        private void BroadcastToastClientRpc(string message)
        {
            PortalNightsHud.Instance?.ShowToast(message);
        }

        [ClientRpc]
        private void PickupCollectedClientRpc(Vector3 position, int pickupKind)
        {
            PortalNightsPickupKind kind = (PortalNightsPickupKind)pickupKind;
            PortalNightsVfx.SpawnBurst(position, GetPickupColor(kind), 0.85f, 16);
        }
    }
}
