using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEngine.InputSystem;

namespace PortalNights
{
    [RequireComponent(typeof(NetworkObject))]
    public sealed class PortalNightsGameController : NetworkBehaviour
    {
        private enum PortalNightsPlanet3HoldKind
        {
            None,
            ReleaseStaff,
            ReviveStaff,
            ActivateRelay
        }

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
        [SerializeField] private Vector3 planet3Center = new Vector3(0f, 0f, 240f);
        [SerializeField] private Vector2 planet3HalfExtents = new Vector2(82f, 70f);
        [SerializeField] private float planet3SafeZoneRadius = 11f;
        [SerializeField] private Vector3 planet4Center = new Vector3(0f, 0f, 420f);
        [SerializeField] private Vector2 planet4HalfExtents = new Vector2(92f, 78f);
        [SerializeField] private int planet4TargetKills = 140;
        [SerializeField] private int planet4RiftKillRequirement = 25;
        [SerializeField] private int planet4MaxAliveEnemies = 42;
        [SerializeField] private float planet4RiftInteractRange = 6f;
        [SerializeField] private float planet4RiftCloseHoldTime = 4f;
        [SerializeField] private float planet4ExitPortalRange = 7f;
        [SerializeField] private Vector3 planet5Center = new Vector3(0f, 0f, 620f);
        [SerializeField] private Vector2 planet5HalfExtents = new Vector2(86f, 74f);
        [SerializeField] private float planet5SphereBaseHealth = 5000f;
        [SerializeField] private float planet5BossAHealth = 9000f;
        [SerializeField] private float planet5BossBHealth = 12000f;
        [SerializeField] private float planet5BossHealInterval = 12f;
        [SerializeField] private float planet5BossHealFraction = 0.08f;
        [SerializeField] private float planet5BossDeathProtectThreshold = 0.1f;
        [SerializeField] private float planet5BossDeathProtectHealTo = 0.25f;
        [SerializeField] private float planet5StabilizerInteractRange = 5.5f;
        [SerializeField] private float planet5StabilizerHoldTime = 3f;
        [SerializeField] private float planet5UniversePortalRange = 7f;
        [SerializeField] private float pickupDropChance = 0.72f;
        [SerializeField] private float temporaryBoostDuration = 30f;

        [Header("Run State")]
        [SerializeField] private PortalNightsRunState runState = new PortalNightsRunState();

        [Header("Diagnostics")]
        [SerializeField] private bool performanceDebug;
        [SerializeField] private bool debugObjectiveReminders;
        [SerializeField] private bool hardDisableInactivePlanetRoots = true;

        private readonly List<PortalNightsEnemy> enemies = new List<PortalNightsEnemy>();
        private readonly List<PortalNightsPlayerController> registeredPlayers = new List<PortalNightsPlayerController>();
        private readonly List<PortalNightsBuildPoint> buildPoints = new List<PortalNightsBuildPoint>();
        private readonly List<PortalNightsAlly> allies = new List<PortalNightsAlly>();
        private readonly List<PortalNightsStaffRescue> planet3Staff = new List<PortalNightsStaffRescue>();
        private readonly List<PortalNightsEnemyRift> planet3Rifts = new List<PortalNightsEnemyRift>();
        private readonly List<PortalNightsPlanet4HiveRift> planet4Rifts = new List<PortalNightsPlanet4HiveRift>();
        private readonly List<PortalNightsDamageTarget> damageTargets = new List<PortalNightsDamageTarget>();
        private readonly List<PortalNightsPlanet5Stabilizer> planet5Stabilizers = new List<PortalNightsPlanet5Stabilizer>();
        private readonly Dictionary<PortalNightsEnemy, int> planet4EnemyRiftIndex = new Dictionary<PortalNightsEnemy, int>();
        private readonly Dictionary<PortalNightsEnemy, PortalNightsPlanet4EnemyVariant> planet4EnemyVariants = new Dictionary<PortalNightsEnemy, PortalNightsPlanet4EnemyVariant>();
        private readonly Dictionary<Transform, Transform> objectiveMarkers = new Dictionary<Transform, Transform>();
        private readonly Dictionary<Transform, PlanetEnvironmentCache> planetEnvironmentCaches = new Dictionary<Transform, PlanetEnvironmentCache>();
        private readonly PortalNightsLeaderboardService leaderboardService = new PortalNightsLocalLeaderboardService();

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
        private readonly NetworkVariable<int> planet3StaffRescued = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> planet3StaffAtSphere = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> planet3RelayCharge = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> planet3PreparationTimer = new NetworkVariable<float>(
            0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> planet3ActiveRifts = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> planet4EnemiesDefeated = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> planet4RiftsClosed = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> planet4ActiveRifts = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> planet5BossesDefeated = new NetworkVariable<int>(
            0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<int> planet5StabilizersCompleted = new NetworkVariable<int>(
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
        private Transform arenaRoot;
        private Transform planet2Root;
        private Transform planet2ArrivalPoint;
        private Transform planet2Sphere;
        private PortalNightsHealth planet2SphereHealth;
        private Transform[] planet2EnemySpawnPoints;
        private Transform planet3Root;
        private Transform planet3ArrivalPoint;
        private Transform planet3RelaySphere;
        private PortalNightsHealth planet3RelayHealth;
        private Coroutine initializeServerStateRoutine;
        private float planet3DefenseElapsed;
        private float planet3RelayUnderAttackTimer;
        private int planet3NextAttackIndex;
        private bool planet3CoinsGranted;
        private ulong planet3HoldClientId = ulong.MaxValue;
        private PortalNightsPlanet3HoldKind planet3HoldKind = PortalNightsPlanet3HoldKind.None;
        private PortalNightsStaffRescue planet3HoldStaff;
        private float planet3HoldProgress;
        private Transform planet4Root;
        private Transform planet4ArrivalPoint;
        private Transform planet4ExitPortal;
        private float planet4DirectorTimer;
        private float planet4RiftHoldProgress;
        private ulong planet4RiftHoldClientId = ulong.MaxValue;
        private PortalNightsPlanet4HiveRift planet4HoldRift;
        private int planet4SpawnSerial;
        private Transform planet5Root;
        private Transform planet5ArrivalPoint;
        private Transform planet5Helper1ArrivalPoint;
        private Transform planet5Helper2ArrivalPoint;
        private PortalNightsPlanet5HealingSphere planet5SphereVisual;
        private PortalNightsDamageTarget planet5SphereTarget;
        private PortalNightsHealth planet5SphereHealth;
        private PortalNightsPlanet5BossController planet5BossA;
        private PortalNightsPlanet5BossController planet5BossB;
        private PortalNightsHealth planet5Helper1Health;
        private PortalNightsHealth planet5Helper2Health;
        private Transform planet5UniversePortal;
        private PortalNightsPlanet5Stabilizer planet5HoldStabilizer;
        private ulong planet5StabilizerHoldClientId = ulong.MaxValue;
        private float planet5BossIntroTimer;
        private float planet5HealTimer;
        private float planet5SphereIgnoreTimer;
        private float planet5SphereLastHealth;
        private float planet5StabilizerHoldProgress;
        private float planet2SphereReadyReminderTimer;
        private float planet3NoStaffReminderTimer;
        private float planet3EscortStaffReminderTimer;
        private float planet4CloseRiftReminderTimer;
        private float planet5BossPressureReminderTimer;
        private float planet5RestoreSphereReminderTimer;
        private float missionObjectiveSyncTimer;
        private string lastSyncedObjective = string.Empty;
        private string lastSyncedProgress = string.Empty;
        private int lastSyncedSeverity = -1;
        private bool planet2SphereReadyReminderStarted;
        private bool planet3NoStaffReminderStarted;
        private bool planet3EscortStaffReminderStarted;
        private bool planet4CloseRiftReminderStarted;
        private bool planet5BossPressureReminderStarted;
        private bool planet5RestoreSphereReminderStarted;
        private bool planet5FirstSphereHintShown;
        private bool planet5SecondSphereHintShown;
        private bool planet5SphereDestroyed;
        private bool planet5UniverseCompleteSubmitted;
        private bool pendingPlanet4RiftCounterRefresh;
        private bool warnedLazyPlanetNetworkSpawnFailure;
        private int activePlanetEnvironmentIndex = -1;
        private float performanceDebugTimer;
        private float nextPlayerCacheRefreshTime;
        private string universeCompleteLeaderboardText = string.Empty;

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
        public bool CanBuildInCurrentState => IsBuildAllowedState(GameState);
        public PortalNightsHealth RelaySphereHealth => planet3RelayHealth;
        public int Planet3StaffRescued => planet3StaffRescued.Value;
        public int Planet3StaffAtSphere => planet3StaffAtSphere.Value;
        public float Planet3RelayCharge => planet3RelayCharge.Value;
        public float Planet3PreparationTimer => planet3PreparationTimer.Value;
        public int Planet3ActiveRifts => planet3ActiveRifts.Value;
        public float Planet3HoldProgress => planet3HoldProgress;
        public int Planet4EnemiesDefeated => planet4EnemiesDefeated.Value;
        public int Planet4TargetKills => planet4TargetKills;
        public int Planet4RiftsClosed => planet4RiftsClosed.Value;
        public int Planet4RiftsTotal => 4;
        public int Planet4ActiveRifts => planet4ActiveRifts.Value;
        public float Planet4RiftHoldProgress => planet4RiftCloseHoldTime <= 0f ? 0f : Mathf.Clamp01(planet4RiftHoldProgress / planet4RiftCloseHoldTime);
        public bool Planet4ExitPortalReady => GameState == PortalNightsGameState.Planet4_ExitPortalReady;
        public int CurrentScore => runState == null ? 0 : runState.score;
        public int UniverseIndex => runState == null ? 1 : Mathf.Max(1, runState.universeIndex);
        public int CurrentPlanetIndex => runState == null ? 1 : Mathf.Max(1, runState.currentPlanetIndex);
        public PortalNightsHealth Planet5SphereHealth => planet5SphereHealth;
        public PortalNightsHealth Planet5BossAHealth => planet5BossA == null ? null : planet5BossA.Health;
        public PortalNightsHealth Planet5BossBHealth => planet5BossB == null ? null : planet5BossB.Health;
        public PortalNightsHealth Planet5Helper1Health => planet5Helper1Health;
        public PortalNightsHealth Planet5Helper2Health => planet5Helper2Health;
        public int Planet5BossesDefeated => planet5BossesDefeated.Value;
        public int Planet5StabilizersCompleted => planet5StabilizersCompleted.Value;
        public int Planet5StabilizersTotal => Mathf.Max(3, planet5Stabilizers.Count);
        public float Planet5StabilizerHoldProgress => planet5StabilizerHoldTime <= 0f ? 0f : Mathf.Clamp01(planet5StabilizerHoldProgress / planet5StabilizerHoldTime);
        public PortalNightsRunState RunState => runState;
        public string UniverseCompleteLeaderboardText => universeCompleteLeaderboardText;
        public bool IsPlanet5BossCombatActive => GameState == PortalNightsGameState.Planet5_DestroyHealingSphere || GameState == PortalNightsGameState.Planet5_KillBosses;
        public bool PerformanceDebugEnabled => performanceDebug;
        public int ActivePlanetEnvironmentIndex => activePlanetEnvironmentIndex;
        public bool IsPlanetRootActiveForDiagnostics(int planetIndex)
        {
            CachePlanetRootReferencesOnly();
            Transform root = GetPlanetRoot(planetIndex);
            return root != null && root.gameObject.activeInHierarchy;
        }

        private PortalNightsUniverseScaling CurrentScaling => PortalNightsUniverseScaling.ForUniverse(runState == null ? 1 : runState.universeIndex);

        private void Awake()
        {
            Instance = this;
            EnsureFreezeRecorder();
            CachePlanetRootReferencesOnly();
            DisableFuturePlanetRootsBeforeNetworkStart();
            CacheSceneReferences();
        }

        private void EnsureFreezeRecorder()
        {
            if (GetComponent<PortalNightsFreezeRecorder>() == null)
            {
                gameObject.AddComponent<PortalNightsFreezeRecorder>();
            }
        }

        private void CachePlanetRootReferencesOnly()
        {
            if (arenaRoot == null)
            {
                GameObject arenaObject = GameObject.Find("PortalNightsArena");
                arenaRoot = arenaObject == null ? null : arenaObject.transform;
            }

            if (arenaRoot == null)
            {
                return;
            }

            planet2Root ??= arenaRoot.Find("Planet2_CrystalMoon");
            planet3Root ??= arenaRoot.Find("Planet3_AshRelayStation");
            planet4Root ??= arenaRoot.Find("Planet4_SwarmExpanse");
            planet5Root ??= arenaRoot.Find("Planet5_CrimsonSingularity");
        }

        private void DisableFuturePlanetRootsBeforeNetworkStart()
        {
            CachePlanetRootReferencesOnly();
            if (GetPlanetIndexForState(GameState) != 1)
            {
                return;
            }

            SetRootActiveFast(planet2Root, false);
            SetRootActiveFast(planet3Root, false);
            SetRootActiveFast(planet4Root, false);
            SetRootActiveFast(planet5Root, false);
        }

        private static void SetRootActiveFast(Transform root, bool active)
        {
            if (root != null && root.gameObject.activeSelf != active)
            {
                root.gameObject.SetActive(active);
            }
        }

        private void Start()
        {
            CachePlanetRootReferencesOnly();
            DisableFuturePlanetRootsBeforeNetworkStart();
            SetActivePlanetEnvironment(GetPlanetIndexForState(GameState));
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
            CachePlanetRootReferencesOnly();
            DisableFuturePlanetRootsBeforeNetworkStart();
            CacheSceneReferences();
            SetActivePlanetEnvironment(GetPlanetIndexForState(GameState));
            if (IsServer)
            {
                if (initializeServerStateRoutine != null)
                {
                    StopCoroutine(initializeServerStateRoutine);
                }

                initializeServerStateRoutine = StartCoroutine(InitializeServerStateAfterSpawn());
                if (pendingPlanet4RiftCounterRefresh)
                {
                    pendingPlanet4RiftCounterRefresh = false;
                    RefreshPlanet4RiftCountersServer();
                }
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
            UpdatePerformanceDebug();

            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && GameOver && IsServer)
            {
                RestartServer();
            }

            if (!IsServer || !initialized || GameOver)
            {
                return;
            }

            turretDamageBoostTimer = Mathf.Max(0f, turretDamageBoostTimer - Time.deltaTime);
            UpdateObjectiveRemindersServer();
            UpdateMissionObjectiveSyncServer();

            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && GameState == PortalNightsGameState.Failed)
            {
                RetryPlanet2Server();
                return;
            }

            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && GameState == PortalNightsGameState.Planet3_Failed)
            {
                RetryPlanet3Server();
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

            if (IsPlanet3State(GameState))
            {
                UpdatePlanet3Server();
                return;
            }

            if (IsPlanet4State(GameState))
            {
                UpdatePlanet4Server();
                return;
            }

            if (IsPlanet5State(GameState))
            {
                UpdatePlanet5Server();
                return;
            }

            if (GameState == PortalNightsGameState.Planet1_Defense || GameState == PortalNightsGameState.Planet1_PortalReady)
            {
                UpdateWaveServer();
            }
        }

        private void UpdateObjectiveRemindersServer()
        {
            if (!IsServer || GameState == PortalNightsGameState.PortalTravel)
            {
                ResetObjectiveReminderTimersServer("transition");
                return;
            }

            UpdatePlanet2ObjectiveReminderServer();
            UpdatePlanet3ObjectiveReminderServer();
            UpdatePlanet4ObjectiveReminderServer();
            UpdatePlanet5ObjectiveReminderServer();
        }

        private void UpdatePlanet2ObjectiveReminderServer()
        {
            if (GameState != PortalNightsGameState.Planet2_SphereReady)
            {
                ResetReminderTimer(ref planet2SphereReadyReminderTimer, ref planet2SphereReadyReminderStarted, "P2 activate sphere");
                return;
            }

            TickReminderTimer(ref planet2SphereReadyReminderTimer, ref planet2SphereReadyReminderStarted, "P2 activate sphere");
            if (planet2SphereReadyReminderTimer >= 20f)
            {
                PlayReminderDialogueServer("p02_activate_sphere_hint_001", PortalNightsLocalization.Text("objective.activateSphere"), string.Empty, PortalNightsObjectiveSeverity.Warning);
                planet2SphereReadyReminderTimer = 0f;
            }
        }

        private void UpdatePlanet3ObjectiveReminderServer()
        {
            bool rescueState = GameState == PortalNightsGameState.Planet3_FindStaff
                || GameState == PortalNightsGameState.Planet3_ReleaseStaff
                || GameState == PortalNightsGameState.Planet3_EscortToSphere;
            if (!rescueState)
            {
                ResetReminderTimer(ref planet3NoStaffReminderTimer, ref planet3NoStaffReminderStarted, "P3 find staff");
                ResetReminderTimer(ref planet3EscortStaffReminderTimer, ref planet3EscortStaffReminderStarted, "P3 escort staff");
                return;
            }

            int following = CountPlanet3StaffByState(PortalNightsStaffState.Following);
            bool noStaffProgress = planet3StaffRescued.Value <= 0 && planet3StaffAtSphere.Value <= 0 && following <= 0;
            if (noStaffProgress)
            {
                TickReminderTimer(ref planet3NoStaffReminderTimer, ref planet3NoStaffReminderStarted, "P3 find staff");
                if (planet3NoStaffReminderTimer >= 30f)
                {
                    PlayReminderDialogueServer("p03_rescue_staff_hint_001", PortalNightsLocalization.Text("objective.rescueStaff"), PortalNightsLocalization.Format("progress.staffAtSphere", planet3StaffAtSphere.Value), PortalNightsObjectiveSeverity.Warning);
                    planet3NoStaffReminderTimer = 0f;
                }
            }
            else
            {
                ResetReminderTimer(ref planet3NoStaffReminderTimer, ref planet3NoStaffReminderStarted, "P3 find staff");
            }

            bool escortNeeded = following > 0 && planet3StaffAtSphere.Value < 2;
            if (escortNeeded)
            {
                TickReminderTimer(ref planet3EscortStaffReminderTimer, ref planet3EscortStaffReminderStarted, "P3 escort staff");
                if (planet3EscortStaffReminderTimer >= 25f)
                {
                    PlayReminderDialogueServer("p03_escort_staff_hint_001", PortalNightsLocalization.Text("objective.rescueStaff"), PortalNightsLocalization.Format("progress.staffAtSphere", planet3StaffAtSphere.Value), PortalNightsObjectiveSeverity.Warning);
                    planet3EscortStaffReminderTimer = 0f;
                }
            }
            else
            {
                ResetReminderTimer(ref planet3EscortStaffReminderTimer, ref planet3EscortStaffReminderStarted, "P3 escort staff");
            }
        }

        private void UpdatePlanet4ObjectiveReminderServer()
        {
            bool closableRiftWaiting = false;
            if (GameState == PortalNightsGameState.Planet4_HordeActive || GameState == PortalNightsGameState.Planet4_RiftClosing)
            {
                foreach (PortalNightsPlanet4HiveRift rift in planet4Rifts)
                {
                    if (rift != null && rift.IsClosable)
                    {
                        closableRiftWaiting = true;
                        break;
                    }
                }
            }

            if (!closableRiftWaiting)
            {
                ResetReminderTimer(ref planet4CloseRiftReminderTimer, ref planet4CloseRiftReminderStarted, "P4 close rift");
                return;
            }

            TickReminderTimer(ref planet4CloseRiftReminderTimer, ref planet4CloseRiftReminderStarted, "P4 close rift");
            if (planet4CloseRiftReminderTimer >= 20f)
            {
                PlayReminderDialogueServer("p04_close_rift_hint_001", PortalNightsLocalization.Text("objective.closeRifts"), PortalNightsLocalization.Text("hud.riftsClosed") + ": " + planet4RiftsClosed.Value + "/4", PortalNightsObjectiveSeverity.Warning);
                planet4CloseRiftReminderTimer = 0f;
            }
        }

        private void UpdatePlanet5ObjectiveReminderServer()
        {
            if (GameState != PortalNightsGameState.Planet5_RestoreSphereReady)
            {
                ResetReminderTimer(ref planet5RestoreSphereReminderTimer, ref planet5RestoreSphereReminderStarted, "P5 restore sphere");
                return;
            }

            TickReminderTimer(ref planet5RestoreSphereReminderTimer, ref planet5RestoreSphereReminderStarted, "P5 restore sphere");
            if (planet5RestoreSphereReminderTimer >= 20f)
            {
                PlayReminderDialogueServer("p05_restore_sphere_hint_001", PortalNightsLocalization.Text("objective.restoreSphere"), PortalNightsLocalization.Format("progress.stabilizers", planet5StabilizersCompleted.Value, Planet5StabilizersTotal), PortalNightsObjectiveSeverity.Warning);
                planet5RestoreSphereReminderTimer = 0f;
            }
        }

        private int CountPlanet3StaffByState(PortalNightsStaffState staffState)
        {
            int count = 0;
            foreach (PortalNightsStaffRescue staff in planet3Staff)
            {
                if (staff != null && staff.State == staffState)
                {
                    count++;
                }
            }

            return count;
        }

        private void PlayReminderDialogueServer(string dialogueId, string objectiveMain, string objectiveProgress, PortalNightsObjectiveSeverity severity)
        {
            if (debugObjectiveReminders)
            {
                Debug.Log("[PortalNights] Objective reminder plays: " + dialogueId, this);
            }

            MissionDialogueClientRpc(dialogueId, objectiveMain ?? string.Empty, objectiveProgress ?? string.Empty, (int)severity);
        }

        private void TickReminderTimer(ref float timer, ref bool started, string label)
        {
            if (!started)
            {
                if (debugObjectiveReminders)
                {
                    Debug.Log("[PortalNights] Objective reminder timer started: " + label, this);
                }

                started = true;
                timer = 0f;
            }

            timer += Time.deltaTime;
        }

        private void ResetReminderTimer(ref float timer, ref bool started, string label)
        {
            if (started)
            {
                if (debugObjectiveReminders)
                {
                    Debug.Log("[PortalNights] Objective reminder timer reset: " + label, this);
                }
            }

            timer = 0f;
            started = false;
        }

        private void ResetObjectiveReminderTimersServer(string reason)
        {
            ResetReminderTimer(ref planet2SphereReadyReminderTimer, ref planet2SphereReadyReminderStarted, "P2 activate sphere " + reason);
            ResetReminderTimer(ref planet3NoStaffReminderTimer, ref planet3NoStaffReminderStarted, "P3 find staff " + reason);
            ResetReminderTimer(ref planet3EscortStaffReminderTimer, ref planet3EscortStaffReminderStarted, "P3 escort staff " + reason);
            ResetReminderTimer(ref planet4CloseRiftReminderTimer, ref planet4CloseRiftReminderStarted, "P4 close rift " + reason);
            ResetReminderTimer(ref planet5BossPressureReminderTimer, ref planet5BossPressureReminderStarted, "P5 boss pressure " + reason);
            ResetReminderTimer(ref planet5RestoreSphereReminderTimer, ref planet5RestoreSphereReminderStarted, "P5 restore sphere " + reason);
        }

        private void UpdateMissionObjectiveSyncServer()
        {
            missionObjectiveSyncTimer -= Time.deltaTime;
            if (missionObjectiveSyncTimer > 0f)
            {
                return;
            }

            missionObjectiveSyncTimer = 0.5f;
            if (TryBuildMissionObjective(out string main, out string progress, out PortalNightsObjectiveSeverity severity))
            {
                int severityValue = (int)severity;
                if (main == lastSyncedObjective && progress == lastSyncedProgress && severityValue == lastSyncedSeverity)
                {
                    return;
                }

                lastSyncedObjective = main;
                lastSyncedProgress = progress;
                lastSyncedSeverity = severityValue;
                MissionObjectiveClientRpc(main, progress, severityValue);
            }
        }

        private bool TryBuildMissionObjective(out string main, out string progress, out PortalNightsObjectiveSeverity severity)
        {
            main = string.Empty;
            progress = string.Empty;
            severity = PortalNightsObjectiveSeverity.Normal;

            switch (GameState)
            {
                case PortalNightsGameState.Planet1_Defense:
                    main = PortalNightsLocalization.Text("objective.defendCore");
                    progress = waveRunning.Value ? PortalNightsLocalization.Format("progress.wave", WaveNumber, planet1FinalWave) : PortalNightsLocalization.Format("progress.nextWave", Mathf.CeilToInt(nextWaveTimer.Value));
                    return true;
                case PortalNightsGameState.Planet1_RewardChoice:
                    main = PortalNightsLocalization.Text("objective.chooseReward");
                    progress = PortalNightsLocalization.Text("progress.pressRewards");
                    severity = PortalNightsObjectiveSeverity.Warning;
                    return true;
                case PortalNightsGameState.Planet1_PortalReady:
                    main = PortalNightsLocalization.Text("objective.enterPortal");
                    progress = PortalNightsLocalization.Text("progress.requiredTurrets");
                    severity = PortalNightsObjectiveSeverity.Warning;
                    return true;
                case PortalNightsGameState.Planet2_ClearArea:
                    main = PortalNightsLocalization.Text("objective.clearArea");
                    progress = PortalNightsLocalization.Format("progress.enemies", enemiesRemaining.Value);
                    return true;
                case PortalNightsGameState.Planet2_SphereReady:
                    main = PortalNightsLocalization.Text("objective.activateSphere");
                    progress = PortalNightsLocalization.Text("progress.holdCore");
                    severity = PortalNightsObjectiveSeverity.Warning;
                    return true;
                case PortalNightsGameState.Planet2_DefendSphere:
                    main = PortalNightsLocalization.Text("objective.defendSphere");
                    progress = PortalNightsLocalization.Format("progress.wave", Mathf.Clamp(planet2DefenseWaveIndex, 1, 3), 3);
                    severity = PortalNightsObjectiveSeverity.Warning;
                    return true;
                case PortalNightsGameState.Planet2_Cleared:
                    main = PortalNightsLocalization.Text("objective.areaCleared");
                    progress = PortalNightsLocalization.Text("progress.nextPlanetOnline");
                    return true;
                case PortalNightsGameState.Planet3_FindStaff:
                case PortalNightsGameState.Planet3_ReleaseStaff:
                case PortalNightsGameState.Planet3_EscortToSphere:
                    main = PortalNightsLocalization.Text("objective.rescueStaff");
                    progress = PortalNightsLocalization.Format("progress.staffAtSphere", planet3StaffAtSphere.Value);
                    return true;
                case PortalNightsGameState.Planet3_SphereReady:
                case PortalNightsGameState.Planet3_SphereActivation:
                    main = PortalNightsLocalization.Text("objective.activateSphere");
                    progress = PortalNightsLocalization.Format("progress.staffAtSphere", planet3StaffAtSphere.Value);
                    severity = PortalNightsObjectiveSeverity.Warning;
                    return true;
                case PortalNightsGameState.Planet3_DefensePreparation:
                    main = PortalNightsLocalization.Text("objective.defendSphere");
                    progress = PortalNightsLocalization.Format("progress.buildTime", Mathf.CeilToInt(planet3PreparationTimer.Value));
                    severity = PortalNightsObjectiveSeverity.Warning;
                    return true;
                case PortalNightsGameState.Planet3_DefendSphere:
                    main = PortalNightsLocalization.Text("objective.defendSphere");
                    progress = PortalNightsLocalization.Format("progress.relayCharge", Mathf.CeilToInt(planet3RelayCharge.Value));
                    severity = planet3RelayUnderAttackTimer > 0.01f ? PortalNightsObjectiveSeverity.Danger : PortalNightsObjectiveSeverity.Warning;
                    return true;
                case PortalNightsGameState.Planet4_HordeActive:
                case PortalNightsGameState.Planet4_RiftClosing:
                    main = PortalNightsLocalization.Text("objective.closeRifts");
                    progress = PortalNightsLocalization.Format("progress.killsRifts", planet4EnemiesDefeated.Value, planet4TargetKills, planet4RiftsClosed.Value);
                    severity = PortalNightsObjectiveSeverity.Warning;
                    return true;
                case PortalNightsGameState.Planet4_ExitPortalReady:
                    main = PortalNightsLocalization.Text("objective.enterPortal");
                    progress = PortalNightsLocalization.Text("progress.planet5PortalOpen");
                    severity = PortalNightsObjectiveSeverity.Warning;
                    return true;
                case PortalNightsGameState.Planet5_Arrival:
                case PortalNightsGameState.Planet5_BossIntro:
                case PortalNightsGameState.Planet5_DestroyHealingSphere:
                    main = PortalNightsLocalization.Text("objective.destroyCorruptedSphere");
                    progress = PortalNightsLocalization.Format("progress.sphere", FormatObjectiveHealth(planet5SphereHealth));
                    severity = PortalNightsObjectiveSeverity.Danger;
                    return true;
                case PortalNightsGameState.Planet5_KillBosses:
                    main = PortalNightsLocalization.Text("objective.killBothBosses");
                    progress = PortalNightsLocalization.Format("progress.bossesDefeated", planet5BossesDefeated.Value);
                    severity = PortalNightsObjectiveSeverity.Danger;
                    return true;
                case PortalNightsGameState.Planet5_RestoreSphereReady:
                case PortalNightsGameState.Planet5_RestoringSphere:
                    main = PortalNightsLocalization.Text("objective.restoreSphere");
                    progress = PortalNightsLocalization.Format("progress.stabilizers", planet5StabilizersCompleted.Value, Planet5StabilizersTotal);
                    severity = PortalNightsObjectiveSeverity.Warning;
                    return true;
                case PortalNightsGameState.Planet5_SphereRestored:
                case PortalNightsGameState.Planet5_UniverseComplete:
                    main = PortalNightsLocalization.Text("objective.universeComplete");
                    progress = PortalNightsLocalization.Text("progress.enterNextUniverse");
                    return true;
                case PortalNightsGameState.Failed:
                case PortalNightsGameState.Planet3_Failed:
                case PortalNightsGameState.Planet4_Failed:
                case PortalNightsGameState.Planet5_Failed:
                    main = PortalNightsLocalization.Text("objective.failed");
                    progress = PortalNightsLocalization.Text("progress.pressRetry");
                    severity = PortalNightsObjectiveSeverity.Danger;
                    return true;
                default:
                    return false;
            }
        }

        private static string FormatObjectiveHealth(PortalNightsHealth health)
        {
            if (health == null || health.MaxHealth <= 0f)
            {
                return "--";
            }

            return Mathf.CeilToInt(health.CurrentHealth) + "/" + Mathf.CeilToInt(health.MaxHealth);
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

        public void RegisterPlayer(PortalNightsPlayerController player)
        {
            if (player != null && !registeredPlayers.Contains(player))
            {
                registeredPlayers.Add(player);
            }
        }

        public void UnregisterPlayer(PortalNightsPlayerController player)
        {
            registeredPlayers.Remove(player);
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

        public void RegisterDamageTarget(PortalNightsDamageTarget target)
        {
            if (target != null && !damageTargets.Contains(target))
            {
                damageTargets.Add(target);
            }
        }

        public void UnregisterDamageTarget(PortalNightsDamageTarget target)
        {
            damageTargets.Remove(target);
        }

        public void RegisterPlanet3Staff(PortalNightsStaffRescue staff)
        {
            if (staff != null && !planet3Staff.Contains(staff))
            {
                planet3Staff.Add(staff);
            }
        }

        public void UnregisterPlanet3Staff(PortalNightsStaffRescue staff)
        {
            planet3Staff.Remove(staff);
        }

        public void RegisterPlanet3Rift(PortalNightsEnemyRift rift)
        {
            if (rift != null && !planet3Rifts.Contains(rift))
            {
                planet3Rifts.Add(rift);
            }
        }

        public void UnregisterPlanet3Rift(PortalNightsEnemyRift rift)
        {
            planet3Rifts.Remove(rift);
        }

        public void EnemyKilled(PortalNightsEnemy enemy, int reward, Vector3 position)
        {
            if (!IsServer)
            {
                return;
            }

            bool planet4Kill = IsPlanet4State(GameState);
            UnregisterEnemy(enemy);
            if (planet4Kill)
            {
                HandlePlanet4EnemyKilledServer(enemy);
            }

            if (!planet4Kill)
            {
                enemiesRemaining.Value = Mathf.Max(0, enemiesRemaining.Value - 1);
            }
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

            if (target == planet3RelayHealth)
            {
                planet3RelayUnderAttackTimer = 1.25f;
                planet3RelayHealth.DamageServer(amount);
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

        public PortalNightsDamageTarget GetShotDamageTarget(Vector3 origin, Vector3 direction, float range, float radius)
        {
            if (Physics.Raycast(origin, direction, out RaycastHit hit, range, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            {
                PortalNightsDamageTarget rayTarget = hit.collider.GetComponentInParent<PortalNightsDamageTarget>();
                if (rayTarget != null && rayTarget.IsTargetable)
                {
                    return rayTarget;
                }
            }

            PortalNightsDamageTarget best = null;
            float bestProjection = range;
            for (int i = damageTargets.Count - 1; i >= 0; i--)
            {
                PortalNightsDamageTarget target = damageTargets[i];
                if (target == null)
                {
                    damageTargets.RemoveAt(i);
                    continue;
                }

                if (!target.IsTargetable)
                {
                    continue;
                }

                Vector3 toTarget = target.AimPoint - origin;
                float projection = Vector3.Dot(direction, toTarget);
                if (projection <= 0f || projection > range)
                {
                    continue;
                }

                float missDistance = Vector3.Cross(direction, toTarget).magnitude;
                float allowed = radius + projection * 0.018f;
                if (missDistance <= allowed && projection < bestProjection)
                {
                    bestProjection = projection;
                    best = target;
                }
            }

            return best;
        }

        public PortalNightsDamageTarget GetClosestDamageTarget(Vector3 position, float range)
        {
            PortalNightsDamageTarget best = null;
            float bestSqr = range * range;
            int bestPriority = int.MinValue;
            for (int i = damageTargets.Count - 1; i >= 0; i--)
            {
                PortalNightsDamageTarget target = damageTargets[i];
                if (target == null)
                {
                    damageTargets.RemoveAt(i);
                    continue;
                }

                if (!target.IsTargetable)
                {
                    continue;
                }

                float sqr = PortalNightsMath.Flat(target.transform.position - position).sqrMagnitude;
                if (sqr > range * range)
                {
                    continue;
                }

                if (target.Priority > bestPriority || (target.Priority == bestPriority && sqr < bestSqr))
                {
                    bestPriority = target.Priority;
                    bestSqr = sqr;
                    best = target;
                }
            }

            return best;
        }

        public void NotifyDamageTargetHitServer(PortalNightsDamageTarget target, float amount, bool fromPlayer)
        {
            if (!IsServer || target == null)
            {
                return;
            }

            if (runState != null)
            {
                runState.damageDealt += Mathf.Max(0f, amount);
            }

            if (target.TargetKind == PortalNightsDamageTargetKind.Planet5HealingSphere && IsPlanet5State(GameState))
            {
                planet5SphereIgnoreTimer = 0f;
                planet5FirstSphereHintShown = false;
                planet5SecondSphereHintShown = false;
                ResetReminderTimer(ref planet5BossPressureReminderTimer, ref planet5BossPressureReminderStarted, "P5 boss pressure sphere damaged");
            }

            if (target.TargetKind == PortalNightsDamageTargetKind.Planet5Boss && GameState == PortalNightsGameState.Planet5_DestroyHealingSphere && !planet5SphereDestroyed)
            {
                TickReminderTimer(ref planet5BossPressureReminderTimer, ref planet5BossPressureReminderStarted, "P5 boss pressure");
            }
        }

        public PortalNightsPlayerController GetClosestLivingPlayer(Vector3 position, float range)
        {
            RefreshRegisteredPlayersIfNeeded();
            PortalNightsPlayerController best = null;
            float bestSqr = range * range;
            for (int i = registeredPlayers.Count - 1; i >= 0; i--)
            {
                PortalNightsPlayerController player = registeredPlayers[i];
                if (player == null || player.Health == null || player.Health.IsDead)
                {
                    if (player == null)
                    {
                        registeredPlayers.RemoveAt(i);
                    }

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

        private void RefreshRegisteredPlayersIfNeeded()
        {
            if (registeredPlayers.Count > 0 || Time.time < nextPlayerCacheRefreshTime)
            {
                return;
            }

            nextPlayerCacheRefreshTime = Time.time + 1f;
            PortalNightsPlayerController[] players = FindObjectsByType<PortalNightsPlayerController>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                RegisterPlayer(players[i]);
            }
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

            if (GameState == PortalNightsGameState.Planet3_DefensePreparation || GameState == PortalNightsGameState.Planet3_DefendSphere || GameState == PortalNightsGameState.Planet3_Failed)
            {
                return planet3RelayHealth;
            }

            if (IsPlanet4State(GameState))
            {
                PortalNightsPlayerController planet4Player = GetClosestLivingPlayer(position, 160f);
                return planet4Player == null ? null : planet4Player.Health;
            }

            if (IsPlanet5State(GameState))
            {
                PortalNightsPlayerController planet5Player = GetClosestLivingPlayer(position, 160f);
                return planet5Player == null ? null : planet5Player.Health;
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

            if (GameState == PortalNightsGameState.Planet4_ExitPortalReady && IsNearPlanet4ExitPortal(playerPosition))
            {
                EnterPortalToPlanet5Server();
                return;
            }

            if (GameState == PortalNightsGameState.Planet5_UniverseComplete && IsNearPlanet5UniversePortal(playerPosition))
            {
                EnterNextUniverseServer();
                return;
            }

            TryBuildNearServer(playerPosition, clientId);
        }

        public void TryBuildNearServer(Vector3 playerPosition, ulong clientId)
        {
            if (!IsServer || GameOver || !IsBuildAllowedState(GameState))
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
                if (runState != null)
                {
                    runState.turretsBuilt++;
                }

                RefreshPortalProgressAfterBuild(point);
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
                if (runState != null)
                {
                    runState.turretsUpgraded++;
                }

                RefreshPortalProgressAfterBuild(point);
                SendToast(clientId, PortalNightsLocalization.Text("toast.turretUpgraded"));
            }
            else
            {
                SendToast(clientId, PortalNightsLocalization.Text("toast.turretMaxed"));
            }
        }

        public void UpdateHeldInteractServer(Vector3 playerPosition, ulong clientId, float deltaTime)
        {
            if (!IsServer || GameOver)
            {
                return;
            }

            if (IsPlanet4State(GameState))
            {
                UpdatePlanet4HeldInteractServer(playerPosition, clientId, deltaTime);
                return;
            }

            if (IsPlanet5State(GameState))
            {
                UpdatePlanet5HeldInteractServer(playerPosition, clientId, deltaTime);
                return;
            }

            if (!IsPlanet3State(GameState))
            {
                return;
            }

            PortalNightsPlanet3HoldKind nextKind = PortalNightsPlanet3HoldKind.None;
            PortalNightsStaffRescue nextStaff = null;
            float requiredTime = 0f;

            if (GameState == PortalNightsGameState.Planet3_FindStaff || GameState == PortalNightsGameState.Planet3_ReleaseStaff || GameState == PortalNightsGameState.Planet3_EscortToSphere)
            {
                nextStaff = GetClosestPlanet3StaffForInteraction(playerPosition, 5.2f);
                if (nextStaff != null)
                {
                    if (nextStaff.NeedsRevive)
                    {
                        nextKind = PortalNightsPlanet3HoldKind.ReviveStaff;
                        requiredTime = 4f;
                    }
                    else if (nextStaff.NeedsRelease)
                    {
                        nextKind = PortalNightsPlanet3HoldKind.ReleaseStaff;
                        requiredTime = 3f;
                    }
                }
            }

            if (nextKind == PortalNightsPlanet3HoldKind.None && (GameState == PortalNightsGameState.Planet3_SphereReady || GameState == PortalNightsGameState.Planet3_SphereActivation) && IsNearRelaySphere(playerPosition))
            {
                nextKind = PortalNightsPlanet3HoldKind.ActivateRelay;
                requiredTime = 4f;
            }

            if (nextKind == PortalNightsPlanet3HoldKind.None)
            {
                CancelHeldInteractServer(clientId);
                return;
            }

            bool sameHold = planet3HoldClientId == clientId && planet3HoldKind == nextKind && planet3HoldStaff == nextStaff;
            if (!sameHold)
            {
                CancelPlanet3Hold();
                planet3HoldClientId = clientId;
                planet3HoldKind = nextKind;
                planet3HoldStaff = nextStaff;
                planet3HoldProgress = 0f;
                if (planet3HoldKind == PortalNightsPlanet3HoldKind.ReleaseStaff && planet3HoldStaff != null)
                {
                    planet3HoldStaff.BeginReleaseServer();
                    SetGameState(PortalNightsGameState.Planet3_ReleaseStaff);
                }
                else if (planet3HoldKind == PortalNightsPlanet3HoldKind.ActivateRelay)
                {
                    SetGameState(PortalNightsGameState.Planet3_SphereActivation);
                }
            }

            planet3HoldProgress = Mathf.Min(requiredTime, planet3HoldProgress + Mathf.Max(0f, deltaTime));
            if (planet3HoldProgress < requiredTime)
            {
                return;
            }

            CompletePlanet3Hold(clientId);
        }

        public void CancelHeldInteractServer(ulong clientId)
        {
            if (!IsServer)
            {
                return;
            }

            if (planet4RiftHoldClientId == clientId)
            {
                CancelPlanet4RiftHold();
            }

            if (planet5StabilizerHoldClientId == clientId)
            {
                CancelPlanet5StabilizerHold();
            }

            if (planet3HoldClientId == clientId)
            {
                CancelPlanet3Hold();
            }
        }

        private void CompletePlanet3Hold(ulong clientId)
        {
            if (planet3HoldKind == PortalNightsPlanet3HoldKind.ReleaseStaff && planet3HoldStaff != null)
            {
                planet3HoldStaff.ReleaseServer();
                BroadcastToastClientRpc(planet3HoldStaff.StaffId + " RELEASED - ESCORT TO RELAY");
                CancelPlanet3Hold(false);
                SetGameState(PortalNightsGameState.Planet3_EscortToSphere);
                RefreshPlanet3StaffProgressServer();
                return;
            }

            if (planet3HoldKind == PortalNightsPlanet3HoldKind.ReviveStaff && planet3HoldStaff != null)
            {
                planet3HoldStaff.ReviveServer();
                BroadcastToastClientRpc(planet3HoldStaff.StaffId + " REVIVED");
                CancelPlanet3Hold(false);
                RefreshPlanet3StaffProgressServer();
                return;
            }

            if (planet3HoldKind == PortalNightsPlanet3HoldKind.ActivateRelay)
            {
                CancelPlanet3Hold(false);
                ActivatePlanet3RelaySphereServer(clientId);
            }
        }

        private void CancelPlanet3Hold(bool restoreReleaseState = true)
        {
            if (restoreReleaseState && planet3HoldKind == PortalNightsPlanet3HoldKind.ReleaseStaff && planet3HoldStaff != null)
            {
                planet3HoldStaff.CancelReleaseServer();
                if (GameState == PortalNightsGameState.Planet3_ReleaseStaff)
                {
                    SetGameState(PortalNightsGameState.Planet3_FindStaff);
                }
            }
            else if (restoreReleaseState && planet3HoldKind == PortalNightsPlanet3HoldKind.ActivateRelay && GameState == PortalNightsGameState.Planet3_SphereActivation)
            {
                SetGameState(PortalNightsGameState.Planet3_SphereReady);
            }

            planet3HoldClientId = ulong.MaxValue;
            planet3HoldKind = PortalNightsPlanet3HoldKind.None;
            planet3HoldStaff = null;
            planet3HoldProgress = 0f;
        }

        public Vector3 GetPlayerSpawnPosition(ulong clientId)
        {
            if (IsPlanet5State(GameState))
            {
                Vector3 basePosition = planet5ArrivalPoint == null ? planet5Center + new Vector3(0f, 1.12f, -60f) : planet5ArrivalPoint.position;
                return basePosition + new Vector3((int)(clientId % 3) * 1.9f, 0f, 0f);
            }

            if (IsPlanet4State(GameState))
            {
                Vector3 basePosition = planet4ArrivalPoint == null ? planet4Center + new Vector3(0f, 1.12f, -62f) : planet4ArrivalPoint.position;
                return basePosition + new Vector3((int)(clientId % 3) * 1.9f, 0f, 0f);
            }

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
            if (IsPlanet5State(GameState))
            {
                return Quaternion.LookRotation(Vector3.forward, Vector3.up);
            }

            if (IsPlanet4State(GameState))
            {
                return Quaternion.LookRotation(Vector3.forward, Vector3.up);
            }

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
            CachePlanetRootReferencesOnly();
            SetActivePlanetEnvironment(1);
            if (runState == null)
            {
                runState = new PortalNightsRunState();
            }

            runState.StartRun(1, 1);
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
            ResetPlanet3RuntimeState(false);
            ResetPlanet4RuntimeState(false);
            ResetPlanet5RuntimeState(false);
            turretDamageBoostTimer = 0f;
            turretRunDamageMultiplier = 1f;
            missionObjectiveSyncTimer = 0f;
            lastSyncedObjective = string.Empty;
            lastSyncedProgress = string.Empty;
            lastSyncedSeverity = -1;
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
            PortalNightsUniverseScaling scaling = CurrentScaling;
            int smallCount = scaling.ScaleEnemyCount(3 + waveNumber.Value * 2);
            int bruteCount = waveNumber.Value < 3 ? 0 : scaling.ScaleEnemyCount(Mathf.Max(1, waveNumber.Value / 3));
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
                ApplyUniverseScalingToWaveEnemy(enemy, shouldSpawnBrute);
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
            NetworkObject networkObject = coinObject.GetComponent<NetworkObject>();
            if (networkObject != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                networkObject.Spawn(true);
            }

            PortalNightsCoinPickup coinPickup = coinObject.GetComponent<PortalNightsCoinPickup>();
            if (coinPickup != null)
            {
                coinPickup.InitializeServer(amount);
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
            SetActivePlanetEnvironment(GetPlanetIndexForState(newState));
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

                if (!point.RequiredForPortal)
                {
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

        private void RefreshPortalProgressAfterBuild(PortalNightsBuildPoint point)
        {
            if (point == null || !point.RequiredForPortal)
            {
                return;
            }

            RefreshRequiredTurretProgressServer();
            CheckPortalUnlock();
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

        public string GetPlanet3InteractionPrompt(Vector3 playerPosition)
        {
            if (!IsPlanet3State(GameState))
            {
                return string.Empty;
            }

            PortalNightsStaffRescue staff = GetClosestPlanet3StaffForInteraction(playerPosition, 5.2f);
            if (staff != null)
            {
                string progress = planet3HoldStaff == staff && planet3HoldProgress > 0.01f ? $" {Mathf.CeilToInt(planet3HoldProgress * 100f / (staff.NeedsRevive ? 4f : 3f))}%" : string.Empty;
                return staff.NeedsRevive
                    ? PortalNightsLocalization.Format("prompt.holdRevive", progress)
                    : PortalNightsLocalization.Format("prompt.holdReleaseStaff", staff.StaffId, progress);
            }

            if ((GameState == PortalNightsGameState.Planet3_SphereReady || GameState == PortalNightsGameState.Planet3_SphereActivation) && IsNearRelaySphere(playerPosition))
            {
                string progress = planet3HoldKind == PortalNightsPlanet3HoldKind.ActivateRelay && planet3HoldProgress > 0.01f ? $" {Mathf.CeilToInt(planet3HoldProgress * 25f)}%" : string.Empty;
                return PortalNightsLocalization.Format("prompt.holdActivateRelay", progress);
            }

            return string.Empty;
        }

        public void EnterPortalToPlanet2()
        {
            if (!IsServer || GameState != PortalNightsGameState.Planet1_PortalReady)
            {
                return;
            }

            BeginPlanetTransitionClientRpc(1, 2);
            SetGameState(PortalNightsGameState.PortalTravel);
            PreparePlanetForEntry(2);
            ClearRuntimeEnemiesServer();
            waveRunning.Value = false;
            nextWaveTimer.Value = 0f;
            TeleportPlayersToPlanet2Server();
            StartPlanet2ClearArea();
            CompletePlanetTransitionClientRpc(2);
        }

        public void StartPlanet2ClearArea()
        {
            if (!IsServer)
            {
                return;
            }

            PreparePlanetForEntry(2);
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
            MissionToastClientRpc("SPHERE ACTIVATED");
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
            BroadcastToastClientRpc("PLANET CLEARED - ASH RELAY STATION ONLINE");
            BeginPlanetTransitionClientRpc(2, 3);
            PreparePlanetForEntry(3);
            StartPlanet3ArrivalServer();
            CompletePlanetTransitionClientRpc(3);
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
                    MissionToastClientRpc("AREA CLEARED");
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
            PortalNightsUniverseScaling scaling = CurrentScaling;
            planet2EnemiesToSpawn = scaling.ScaleEnemyCount(Mathf.Max(0, smallCount));
            planet2BrutesToSpawn = scaling.ScaleEnemyCount(Mathf.Max(0, bruteCount));
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
                ApplyUniverseScalingToWaveEnemy(enemy, brute);
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

        private void ApplyUniverseScalingToWaveEnemy(PortalNightsEnemy enemy, bool brute)
        {
            if (enemy == null)
            {
                return;
            }

            PortalNightsUniverseScaling scaling = CurrentScaling;
            enemy.ApplyUniverseScalingServer(scaling.enemyHpMultiplier, scaling.enemyDamageMultiplier, scaling.ScaleCoins(brute ? bruteEnemyReward : smallEnemyReward));
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
            if (IsPlanet5State(GameState))
            {
                position.x = Mathf.Clamp(position.x, planet5Center.x - planet5HalfExtents.x, planet5Center.x + planet5HalfExtents.x);
                position.z = Mathf.Clamp(position.z, planet5Center.z - planet5HalfExtents.y, planet5Center.z + planet5HalfExtents.y);
                return true;
            }

            if (IsPlanet4State(GameState))
            {
                position.x = Mathf.Clamp(position.x, planet4Center.x - planet4HalfExtents.x, planet4Center.x + planet4HalfExtents.x);
                position.z = Mathf.Clamp(position.z, planet4Center.z - planet4HalfExtents.y, planet4Center.z + planet4HalfExtents.y);
                return true;
            }

            if (IsPlanet3State(GameState))
            {
                position.x = Mathf.Clamp(position.x, planet3Center.x - planet3HalfExtents.x, planet3Center.x + planet3HalfExtents.x);
                position.z = Mathf.Clamp(position.z, planet3Center.z - planet3HalfExtents.y, planet3Center.z + planet3HalfExtents.y);
                return true;
            }

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
                renderer.sharedMaterial = PortalNightsVfx.CreateRuntimeGlowMaterial(color, color * 2.8f);
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

        private void EnsurePlanet4Area()
        {
            CachePlanetRootReferencesOnly();
            planet4Root ??= arenaRoot == null ? null : arenaRoot.Find("Planet4_SwarmExpanse");
            if (planet4Root == null)
            {
                return;
            }

            planet4Center = planet4Root.position;
            planet4ArrivalPoint = planet4Root.Find("ArrivalZone/PlayerArrivalPoint");
            planet4ExitPortal = planet4Root.Find("ExitPortalToPlanet5");
            planet4Rifts.Clear();

            Transform riftRoot = planet4Root.Find("HiveRifts");
            if (riftRoot != null)
            {
                for (int i = 0; i < riftRoot.childCount; i++)
                {
                    Transform child = riftRoot.GetChild(i);
                    PortalNightsPlanet4HiveRift rift = child.GetComponent<PortalNightsPlanet4HiveRift>();
                    if (rift == null)
                    {
                        rift = child.gameObject.AddComponent<PortalNightsPlanet4HiveRift>();
                    }

                    rift.Configure(child.name, planet4RiftKillRequirement);
                    planet4Rifts.Add(rift);
                }
            }

            RefreshPlanet4RiftCountersServer();
        }

        private void StartPlanet4ArrivalServer()
        {
            if (!IsServer)
            {
                return;
            }

            PreparePlanetForEntry(4);
            if (planet4Root == null || planet4Rifts.Count == 0)
            {
                Debug.LogWarning("[PortalNights] Planet 4 map missing. Build Planet4_SwarmExpanse before starting the horde.", this);
                BroadcastToastClientRpc("PLANET 4 MAP MISSING");
                return;
            }

            ClearRuntimeEnemiesServer();
            ResetPlanet4RuntimeState(true);
            TeleportPlayersToPlanet4Server();
            if (runState != null)
            {
                runState.currentPlanetIndex = 4;
            }

            SetGameState(PortalNightsGameState.Planet4_HordeActive);
            BroadcastToastClientRpc("PLANET 4 - SWARM EXPANSE: KILL THE SWARM");
            SpawnPlanet4DirectorGroupServer(true);
        }

        private void ResetPlanet4RuntimeState(bool resetRifts)
        {
            planet4EnemyRiftIndex.Clear();
            planet4EnemyVariants.Clear();
            planet4EnemiesDefeated.Value = 0;
            planet4RiftsClosed.Value = 0;
            planet4ActiveRifts.Value = 0;
            enemiesRemaining.Value = planet4TargetKills;
            planet4DirectorTimer = Random.Range(5f, 8f);
            planet4SpawnSerial = 0;
            CancelPlanet4RiftHold();

            if (resetRifts)
            {
                for (int i = 0; i < planet4Rifts.Count; i++)
                {
                    if (planet4Rifts[i] == null)
                    {
                        continue;
                    }

                    planet4Rifts[i].ResetRift();
                    SetPlanet4RiftVisualClientRpc(i, (int)PortalNightsRiftState.Active);
                }
            }

            RefreshPlanet4RiftCountersServer();
            SetPlanet4ExitPortalVisualClientRpc(false);
        }

        private void TeleportPlayersToPlanet4Server()
        {
            PortalNightsPlayerController[] players = FindObjectsByType<PortalNightsPlayerController>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                PortalNightsPlayerController player = players[i];
                if (player == null)
                {
                    continue;
                }

                Vector3 basePosition = planet4ArrivalPoint == null ? planet4Center + new Vector3(0f, 1.12f, -62f) : planet4ArrivalPoint.position;
                Vector3 offset = new Vector3((i - players.Length * 0.5f) * 1.9f, 0f, 0f);
                player.transform.SetPositionAndRotation(basePosition + offset, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            }

            TeleportPlayersClientRpc();
        }

        private void UpdatePlanet4Server()
        {
            if (planet4Root == null)
            {
                return;
            }

            if (GameState == PortalNightsGameState.Planet4_HordeActive || GameState == PortalNightsGameState.Planet4_RiftClosing)
            {
                UpdatePlanet4DirectorServer();
                CheckPlanet4CompletionServer();
            }
        }

        private void UpdatePlanet4DirectorServer()
        {
            if (planet4EnemiesDefeated.Value >= planet4TargetKills && AreAllPlanet4OpenRiftsClosableOrClosed())
            {
                return;
            }

            planet4DirectorTimer -= Time.deltaTime;
            if (planet4DirectorTimer > 0f)
            {
                return;
            }

            planet4DirectorTimer = Random.Range(5f, 8f);
            if (enemiesAlive.Value >= planet4MaxAliveEnemies)
            {
                return;
            }

            SpawnPlanet4DirectorGroupServer(false);
        }

        private void SpawnPlanet4DirectorGroupServer(bool forceSmallOpeningGroup)
        {
            int riftIndex = GetPlanet4SpawnRiftIndex();
            if (riftIndex < 0 || riftIndex >= planet4Rifts.Count)
            {
                return;
            }

            PortalNightsPlanet4HiveRift rift = planet4Rifts[riftIndex];
            if (rift == null)
            {
                return;
            }

            PortalNightsUniverseScaling scaling = PortalNightsUniverseScaling.ForUniverse(runState == null ? 1 : runState.universeIndex);
            int killCount = planet4EnemiesDefeated.Value;
            int baseGroup = forceSmallOpeningGroup ? 8 : killCount < 40 ? Random.Range(6, 9) : killCount < 80 ? Random.Range(7, 10) : killCount < 120 ? Random.Range(8, 11) : Random.Range(9, 12);
            int groupSize = Mathf.Min(scaling.ScaleEnemyCount(baseGroup), Mathf.Max(0, planet4MaxAliveEnemies - enemiesAlive.Value));
            for (int i = 0; i < groupSize; i++)
            {
                SpawnPlanet4EnemyServer(ChoosePlanet4Variant(killCount, forceSmallOpeningGroup), riftIndex);
            }
        }

        private int GetPlanet4SpawnRiftIndex()
        {
            if (planet4Rifts.Count == 0)
            {
                return -1;
            }

            int start = Mathf.Abs(planet4SpawnSerial++) % planet4Rifts.Count;
            for (int i = 0; i < planet4Rifts.Count; i++)
            {
                int index = (start + i) % planet4Rifts.Count;
                PortalNightsPlanet4HiveRift rift = planet4Rifts[index];
                if (rift != null && rift.CanSpawn && (!rift.IsClosable || planet4EnemiesDefeated.Value < planet4TargetKills))
                {
                    return index;
                }
            }

            for (int i = 0; i < planet4Rifts.Count; i++)
            {
                PortalNightsPlanet4HiveRift rift = planet4Rifts[i];
                if (rift != null && rift.CanSpawn)
                {
                    return i;
                }
            }

            return -1;
        }

        private static PortalNightsPlanet4EnemyVariant ChoosePlanet4Variant(int killCount, bool openingGroup)
        {
            if (openingGroup || killCount < 40)
            {
                return Random.value < 0.86f ? PortalNightsPlanet4EnemyVariant.Swarmer : PortalNightsPlanet4EnemyVariant.Runner;
            }

            if (killCount < 80)
            {
                return Random.value < 0.68f ? PortalNightsPlanet4EnemyVariant.Swarmer : PortalNightsPlanet4EnemyVariant.Runner;
            }

            if (killCount < 120)
            {
                float roll = Random.value;
                return roll < 0.48f ? PortalNightsPlanet4EnemyVariant.Swarmer : roll < 0.82f ? PortalNightsPlanet4EnemyVariant.Runner : PortalNightsPlanet4EnemyVariant.Brute;
            }

            float finalRoll = Random.value;
            return finalRoll < 0.42f ? PortalNightsPlanet4EnemyVariant.Swarmer : finalRoll < 0.72f ? PortalNightsPlanet4EnemyVariant.Runner : PortalNightsPlanet4EnemyVariant.Brute;
        }

        private void SpawnPlanet4EnemyServer(PortalNightsPlanet4EnemyVariant variant, int riftIndex)
        {
            GameObject prefab = variant == PortalNightsPlanet4EnemyVariant.Brute && bruteEnemyPrefab != null ? bruteEnemyPrefab : smallEnemyPrefab;
            if (prefab == null || riftIndex < 0 || riftIndex >= planet4Rifts.Count)
            {
                return;
            }

            PortalNightsPlanet4HiveRift rift = planet4Rifts[riftIndex];
            Transform spawn = rift == null ? null : rift.GetSpawnPoint(planet4SpawnSerial++);
            Vector3 spawnPosition = spawn == null ? planet4Center + Vector3.forward * 44f : spawn.position;
            spawnPosition += new Vector3(Random.Range(-2.2f, 2.2f), 0f, Random.Range(-2.2f, 2.2f));

            PortalNightsPlayerController nearbyPlayer = GetClosestLivingPlayer(spawnPosition, 14f);
            if (nearbyPlayer != null)
            {
                Vector3 away = PortalNightsMath.Flat(spawnPosition - nearbyPlayer.transform.position);
                if (away.sqrMagnitude <= 0.001f)
                {
                    away = Random.onUnitSphere;
                    away.y = 0f;
                }

                spawnPosition += away.normalized * 10f;
            }

            GameObject enemyObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
            PortalNightsEnemy enemy = enemyObject.GetComponent<PortalNightsEnemy>();
            NetworkObject networkObject = enemyObject.GetComponent<NetworkObject>();
            if (networkObject != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                networkObject.Spawn(true);
            }

            if (enemy == null)
            {
                return;
            }

            ConfigurePlanet4EnemyStats(enemyObject, enemy, variant);
            enemy.ConfigureLaneServer(riftIndex == 2 ? PortalNightsLane.Right : PortalNightsLane.Left, null);
            planet4EnemyRiftIndex[enemy] = riftIndex;
            planet4EnemyVariants[enemy] = variant;
            enemiesRemaining.Value = Mathf.Max(0, planet4TargetKills - planet4EnemiesDefeated.Value);
        }

        private void ConfigurePlanet4EnemyStats(GameObject enemyObject, PortalNightsEnemy enemy, PortalNightsPlanet4EnemyVariant variant)
        {
            PortalNightsUniverseScaling scaling = PortalNightsUniverseScaling.ForUniverse(runState == null ? 1 : runState.universeIndex);
            if (variant == PortalNightsPlanet4EnemyVariant.Runner)
            {
                enemy.ConfigureDirectServer(PortalNightsEnemyKind.Small, 82f * scaling.enemyHpMultiplier, 4.25f, 7f * scaling.enemyDamageMultiplier, scaling.ScaleCoins(24));
                enemyObject.transform.localScale *= 0.92f;
                return;
            }

            if (variant == PortalNightsPlanet4EnemyVariant.Brute)
            {
                enemy.ConfigureDirectServer(PortalNightsEnemyKind.Brute, 285f * scaling.enemyHpMultiplier, 1.55f, 20f * scaling.enemyDamageMultiplier, scaling.ScaleCoins(48));
                enemyObject.transform.localScale *= 1.22f;
                return;
            }

            enemy.ConfigureDirectServer(PortalNightsEnemyKind.Small, 48f * scaling.enemyHpMultiplier, 3.15f, 5f * scaling.enemyDamageMultiplier, scaling.ScaleCoins(16));
            enemyObject.transform.localScale *= 0.78f;
        }

        private void HandlePlanet4EnemyKilledServer(PortalNightsEnemy enemy)
        {
            if (enemy == null)
            {
                return;
            }

            PortalNightsPlanet4EnemyVariant variant = planet4EnemyVariants.TryGetValue(enemy, out PortalNightsPlanet4EnemyVariant storedVariant)
                ? storedVariant
                : enemy.EnemyKind == PortalNightsEnemyKind.Brute ? PortalNightsPlanet4EnemyVariant.Brute : PortalNightsPlanet4EnemyVariant.Swarmer;

            planet4EnemyVariants.Remove(enemy);
            if (runState != null)
            {
                if (variant == PortalNightsPlanet4EnemyVariant.Brute)
                {
                    runState.enhancedEnemiesKilled++;
                    runState.score += PortalNightsScoreCalculator.EnhancedEnemyKilled * PortalNightsUniverseScaling.ForUniverse(runState.universeIndex).scoreMultiplier;
                }
                else
                {
                    runState.enemiesKilled++;
                    runState.score += PortalNightsScoreCalculator.NormalEnemyKilled * PortalNightsUniverseScaling.ForUniverse(runState.universeIndex).scoreMultiplier;
                }
            }

            planet4EnemiesDefeated.Value = Mathf.Min(planet4TargetKills, planet4EnemiesDefeated.Value + 1);
            enemiesRemaining.Value = Mathf.Max(0, planet4TargetKills - planet4EnemiesDefeated.Value);

            if (planet4EnemyRiftIndex.TryGetValue(enemy, out int riftIndex) && riftIndex >= 0 && riftIndex < planet4Rifts.Count)
            {
                PortalNightsPlanet4HiveRift rift = planet4Rifts[riftIndex];
                if (rift != null && !rift.IsClosed)
                {
                    PortalNightsRiftState previous = rift.State;
                    rift.AddEnemyDefeated();
                    if (rift.State != previous)
                    {
                        SetPlanet4RiftVisualClientRpc(riftIndex, (int)rift.State);
                        if (rift.State == PortalNightsRiftState.Closable)
                        {
                            BroadcastToastClientRpc(rift.RiftId + " WEAK - HOLD E TO CLOSE");
                        }
                    }
                }
            }

            planet4EnemyRiftIndex.Remove(enemy);
            RefreshPlanet4RiftCountersServer();
            CheckPlanet4CompletionServer();
        }

        private void UpdatePlanet4HeldInteractServer(Vector3 playerPosition, ulong clientId, float deltaTime)
        {
            PortalNightsPlanet4HiveRift rift = GetClosestPlanet4ClosableRift(playerPosition);
            if (rift == null)
            {
                CancelPlanet4RiftHold();
                return;
            }

            bool sameHold = planet4RiftHoldClientId == clientId && planet4HoldRift == rift;
            if (!sameHold)
            {
                CancelPlanet4RiftHold();
                planet4RiftHoldClientId = clientId;
                planet4HoldRift = rift;
                planet4RiftHoldProgress = 0f;
                rift.SetState(PortalNightsRiftState.Closing);
                SetPlanet4RiftVisualClientRpc(GetPlanet4RiftIndex(rift), (int)PortalNightsRiftState.Closing);
                SetGameState(PortalNightsGameState.Planet4_RiftClosing);
            }

            planet4RiftHoldProgress = Mathf.Min(planet4RiftCloseHoldTime, planet4RiftHoldProgress + Mathf.Max(0f, deltaTime));
            if (planet4RiftHoldProgress >= planet4RiftCloseHoldTime)
            {
                CompletePlanet4RiftClose(rift);
            }
        }

        private void CompletePlanet4RiftClose(PortalNightsPlanet4HiveRift rift)
        {
            int riftIndex = GetPlanet4RiftIndex(rift);
            if (rift == null || riftIndex < 0)
            {
                CancelPlanet4RiftHold();
                return;
            }

            rift.SetState(PortalNightsRiftState.Closed);
            SetPlanet4RiftVisualClientRpc(riftIndex, (int)PortalNightsRiftState.Closed);
            CancelPlanet4RiftHold(false);
            RefreshPlanet4RiftCountersServer();
            BroadcastToastClientRpc(rift.RiftId + " CLOSED");
            MissionToastClientRpc("RIFT CLOSED");
            if (GameState == PortalNightsGameState.Planet4_RiftClosing)
            {
                SetGameState(PortalNightsGameState.Planet4_HordeActive);
            }

            CheckPlanet4CompletionServer();
        }

        private void CancelPlanet4RiftHold(bool restoreState = true)
        {
            if (restoreState && planet4HoldRift != null && planet4HoldRift.State == PortalNightsRiftState.Closing)
            {
                planet4HoldRift.SetState(PortalNightsRiftState.Closable);
                SetPlanet4RiftVisualClientRpc(GetPlanet4RiftIndex(planet4HoldRift), (int)PortalNightsRiftState.Closable);
                if (GameState == PortalNightsGameState.Planet4_RiftClosing)
                {
                    SetGameState(PortalNightsGameState.Planet4_HordeActive);
                }
            }

            planet4RiftHoldClientId = ulong.MaxValue;
            planet4HoldRift = null;
            planet4RiftHoldProgress = 0f;
        }

        private PortalNightsPlanet4HiveRift GetClosestPlanet4ClosableRift(Vector3 position)
        {
            PortalNightsPlanet4HiveRift best = null;
            float bestSqr = planet4RiftInteractRange * planet4RiftInteractRange;
            foreach (PortalNightsPlanet4HiveRift rift in planet4Rifts)
            {
                if (rift == null || (!rift.IsClosable && rift.State != PortalNightsRiftState.Closing))
                {
                    continue;
                }

                float sqr = PortalNightsMath.Flat(rift.transform.position - position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = rift;
                }
            }

            return best;
        }

        private int GetPlanet4RiftIndex(PortalNightsPlanet4HiveRift rift)
        {
            for (int i = 0; i < planet4Rifts.Count; i++)
            {
                if (planet4Rifts[i] == rift)
                {
                    return i;
                }
            }

            return -1;
        }

        private void RefreshPlanet4RiftCountersServer()
        {
            if (!PortalNightsNet.ServerCanWrite(this))
            {
                return;
            }

            if (!IsSpawned)
            {
                pendingPlanet4RiftCounterRefresh = true;
                return;
            }

            int active = 0;
            int closed = 0;
            foreach (PortalNightsPlanet4HiveRift rift in planet4Rifts)
            {
                if (rift == null)
                {
                    continue;
                }

                if (rift.IsClosed)
                {
                    closed++;
                }
                else
                {
                    active++;
                }
            }

            planet4ActiveRifts.Value = active;
            planet4RiftsClosed.Value = closed;
        }

        private bool AreAllPlanet4OpenRiftsClosableOrClosed()
        {
            foreach (PortalNightsPlanet4HiveRift rift in planet4Rifts)
            {
                if (rift != null && !rift.IsClosed && !rift.IsClosable)
                {
                    return false;
                }
            }

            return true;
        }

        private void CheckPlanet4CompletionServer()
        {
            if (!IsServer || GameState == PortalNightsGameState.Planet4_ExitPortalReady || GameState == PortalNightsGameState.Planet4_Cleared)
            {
                return;
            }

            if (planet4EnemiesDefeated.Value < planet4TargetKills || planet4RiftsClosed.Value < 4)
            {
                return;
            }

            ClearRuntimeEnemiesServer();
            enemiesRemaining.Value = 0;
            planet4DirectorTimer = float.PositiveInfinity;
            if (runState != null)
            {
                runState.MarkPlanetCleared(4);
                runState.score += PortalNightsScoreCalculator.Planet4Cleared * PortalNightsUniverseScaling.ForUniverse(runState.universeIndex).scoreMultiplier;
            }

            SetPlanet4ExitPortalVisualClientRpc(true);
            SetGameState(PortalNightsGameState.Planet4_ExitPortalReady);
            BroadcastToastClientRpc("PLANET 5 PORTAL OPEN - ENTER THE FINAL WORLD");
        }

        private bool IsNearPlanet4ExitPortal(Vector3 position)
        {
            return planet4ExitPortal != null && PortalNightsMath.Flat(planet4ExitPortal.position - position).sqrMagnitude <= planet4ExitPortalRange * planet4ExitPortalRange;
        }

        public bool IsPlayerNearPlanet4ExitPortal(Vector3 position)
        {
            return IsNearPlanet4ExitPortal(position);
        }

        public string GetPlanet4InteractionPrompt(Vector3 playerPosition)
        {
            if (!IsPlanet4State(GameState))
            {
                return string.Empty;
            }

            if (GameState == PortalNightsGameState.Planet4_ExitPortalReady && IsNearPlanet4ExitPortal(playerPosition))
            {
                return PortalNightsLocalization.Text("prompt.enterFinalWorld");
            }

            PortalNightsPlanet4HiveRift rift = GetClosestPlanet4ClosableRift(playerPosition);
            if (rift != null)
            {
                string progress = planet4HoldRift == rift && Planet4RiftHoldProgress > 0.01f ? $" {Mathf.CeilToInt(Planet4RiftHoldProgress * 100f)}%" : string.Empty;
                return PortalNightsLocalization.Format("prompt.holdCloseRift", rift.RiftId.ToUpperInvariant(), progress);
            }

            return string.Empty;
        }

        private void EnterPortalToPlanet5Server()
        {
            if (!IsServer || GameState != PortalNightsGameState.Planet4_ExitPortalReady)
            {
                return;
            }

            CachePlanetRootReferencesOnly();
            if (planet5Root == null)
            {
                Debug.LogWarning("[PortalNights] Planet 5 transition requested, but Planet5_CrimsonSingularity is not built yet.", this);
                BroadcastToastClientRpc("PLANET 5 MAP NOT BUILT YET");
                return;
            }

            BeginPlanetTransitionClientRpc(4, 5);
            SetGameState(PortalNightsGameState.Planet4_Cleared);
            StartPlanet5ArrivalServer();
            CompletePlanetTransitionClientRpc(5);
        }

        private void EnsurePlanet5Area()
        {
            CachePlanetRootReferencesOnly();
            planet5Root ??= arenaRoot == null ? null : arenaRoot.Find("Planet5_CrimsonSingularity");
            if (planet5Root == null)
            {
                return;
            }

            planet5Center = planet5Root.position;
            planet5ArrivalPoint = planet5Root.Find("ArrivalZone/PlayerArrivalPoint");
            planet5Helper1ArrivalPoint = planet5Root.Find("ArrivalZone/Helper1ArrivalPoint");
            planet5Helper2ArrivalPoint = planet5Root.Find("ArrivalZone/Helper2ArrivalPoint");
            planet5UniversePortal = planet5Root.Find("Universe2Portal");
            Transform sphereTransform = planet5Root.Find("CentralArena/CorruptedHealingSphere");
            if (sphereTransform != null)
            {
                planet5SphereVisual = sphereTransform.GetComponent<PortalNightsPlanet5HealingSphere>();
                if (planet5SphereVisual == null)
                {
                    planet5SphereVisual = sphereTransform.gameObject.AddComponent<PortalNightsPlanet5HealingSphere>();
                }

                planet5SphereHealth = sphereTransform.GetComponent<PortalNightsHealth>();
                if (planet5SphereHealth == null)
                {
                    planet5SphereHealth = sphereTransform.gameObject.AddComponent<PortalNightsHealth>();
                }

                planet5SphereHealth.SetBaseMaxHealth(planet5SphereBaseHealth);
                planet5SphereHealth.Died -= HandlePlanet5SphereDestroyed;
                planet5SphereHealth.Died += HandlePlanet5SphereDestroyed;
                planet5SphereHealth.HealthChanged -= HandlePlanet5SphereHealthChanged;
                planet5SphereHealth.HealthChanged += HandlePlanet5SphereHealthChanged;

                planet5SphereTarget = sphereTransform.GetComponent<PortalNightsDamageTarget>();
                if (planet5SphereTarget == null)
                {
                    planet5SphereTarget = sphereTransform.gameObject.AddComponent<PortalNightsDamageTarget>();
                }

                planet5SphereTarget.Configure(PortalNightsDamageTargetKind.Planet5HealingSphere, "Corrupted Healing Sphere", sphereTransform, 100);
            }

            planet5Stabilizers.Clear();
            Transform stabilizersRoot = planet5Root.Find("CentralArena/RestorationStabilizers");
            if (stabilizersRoot != null)
            {
                string[] names = { "NorthStabilizer", "WestStabilizer", "EastStabilizer" };
                for (int i = 0; i < names.Length; i++)
                {
                    Transform stabilizerTransform = stabilizersRoot.Find(names[i]);
                    if (stabilizerTransform == null)
                    {
                        continue;
                    }

                    PortalNightsPlanet5Stabilizer stabilizer = stabilizerTransform.GetComponent<PortalNightsPlanet5Stabilizer>();
                    if (stabilizer == null)
                    {
                        stabilizer = stabilizerTransform.gameObject.AddComponent<PortalNightsPlanet5Stabilizer>();
                    }

                    stabilizer.Configure(names[i]);
                    planet5Stabilizers.Add(stabilizer);
                }
            }
        }

        private void StartPlanet5ArrivalServer()
        {
            if (!IsServer)
            {
                return;
            }

            PreparePlanetForEntry(5);
            if (planet5Root == null || planet5SphereHealth == null)
            {
                Debug.LogWarning("[PortalNights] Planet 5 map missing. Build Planet5_CrimsonSingularity before starting final boss gameplay.", this);
                BroadcastToastClientRpc("PLANET 5 MAP MISSING");
                return;
            }

            ClearRuntimeEnemiesServer();
            ClearRuntimeAlliesServer();
            ResetPlanet5RuntimeState(true);
            TeleportPlayersToPlanet5Server();
            SpawnPlanet5HelpersServer();
            SpawnPlanet5BossesServer();
            if (runState != null)
            {
                runState.currentPlanetIndex = 5;
            }

            planet5BossIntroTimer = 2.2f;
            SetGameState(PortalNightsGameState.Planet5_BossIntro);
            BroadcastToastClientRpc("PLANET 5 - CRIMSON SINGULARITY");
        }

        private void ResetPlanet5RuntimeState(bool resetSphere)
        {
            planet5BossesDefeated.Value = 0;
            planet5StabilizersCompleted.Value = 0;
            planet5BossIntroTimer = 0f;
            planet5HealTimer = planet5BossHealInterval;
            planet5SphereIgnoreTimer = 0f;
            planet5SphereLastHealth = planet5SphereHealth == null ? planet5SphereBaseHealth : planet5SphereHealth.CurrentHealth;
            planet5StabilizerHoldProgress = 0f;
            planet5BossPressureReminderTimer = 0f;
            planet5RestoreSphereReminderTimer = 0f;
            planet5StabilizerHoldClientId = ulong.MaxValue;
            planet5HoldStabilizer = null;
            planet5BossPressureReminderStarted = false;
            planet5RestoreSphereReminderStarted = false;
            planet5FirstSphereHintShown = false;
            planet5SecondSphereHintShown = false;
            planet5SphereDestroyed = false;
            planet5UniverseCompleteSubmitted = false;
            universeCompleteLeaderboardText = string.Empty;

            if (planet5SphereHealth != null && resetSphere)
            {
                planet5SphereHealth.ServerInitialize(planet5SphereBaseHealth * PortalNightsUniverseScaling.ForUniverse(runState == null ? 1 : runState.universeIndex).bossHpMultiplier, true);
                planet5SphereLastHealth = planet5SphereHealth.CurrentHealth;
            }

            if (planet5SphereTarget != null)
            {
                planet5SphereTarget.SetTargetable(true);
            }

            planet5SphereVisual?.SetVisualState(PortalNightsSphereVisualState.Corrupted);
            SetPlanet5SphereVisualClientRpc((int)PortalNightsSphereVisualState.Corrupted);
            SetPlanet5StabilizersServer(false, false);
            SetPlanet5UniversePortalVisualClientRpc(false);
        }

        private void TeleportPlayersToPlanet5Server()
        {
            PortalNightsPlayerController[] players = FindObjectsByType<PortalNightsPlayerController>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                PortalNightsPlayerController player = players[i];
                if (player == null)
                {
                    continue;
                }

                Vector3 basePosition = planet5ArrivalPoint == null ? planet5Center + new Vector3(0f, 1.12f, -60f) : planet5ArrivalPoint.position;
                Vector3 offset = new Vector3((i - players.Length * 0.5f) * 1.9f, 0f, 0f);
                player.transform.SetPositionAndRotation(basePosition + offset, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            }

            TeleportPlayersClientRpc();
        }

        private void SpawnPlanet5HelpersServer()
        {
            planet5Helper1Health = CreatePlanet5Helper("Helper_01", planet5Helper1ArrivalPoint == null ? planet5Center + new Vector3(-4f, 1.12f, -62f) : planet5Helper1ArrivalPoint.position, new Color(0.45f, 0.95f, 1f, 1f));
            planet5Helper2Health = CreatePlanet5Helper("Helper_02", planet5Helper2ArrivalPoint == null ? planet5Center + new Vector3(4f, 1.12f, -62f) : planet5Helper2ArrivalPoint.position, new Color(1f, 0.85f, 0.25f, 1f));
        }

        private PortalNightsHealth CreatePlanet5Helper(string helperName, Vector3 position, Color color)
        {
            GameObject helper = new GameObject(helperName);
            helper.transform.position = position;
            helper.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            helper.AddComponent<NetworkObject>();
            helper.AddComponent<NetworkTransform>();
            PortalNightsHealth health = helper.AddComponent<PortalNightsHealth>();
            health.SetBaseMaxHealth(450f);
            health.ServerInitialize(450f, true);

            Transform body = CreateRuntimePrimitive(helperName + "_Body", PrimitiveType.Capsule, helper.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.75f, 0.95f, 0.75f), PortalNightsVfx.CreateRuntimeGlowMaterial(color * 0.45f, color * 1.5f), false).transform;
            Transform head = CreateRuntimePrimitive(helperName + "_ScepterPivot", PrimitiveType.Sphere, helper.transform, new Vector3(0f, 1.55f, 0.35f), new Vector3(0.55f, 0.55f, 0.55f), PortalNightsVfx.CreateRuntimeGlowMaterial(color, color * 2.4f), false).transform;
            Transform muzzle = CreateRuntimePrimitive(helperName + "_ScepterMuzzle", PrimitiveType.Sphere, head, new Vector3(0f, 0f, 0.7f), new Vector3(0.2f, 0.2f, 0.2f), PortalNightsVfx.CreateRuntimeGlowMaterial(color, color * 3f), false).transform;
            LineRenderer beam = helper.AddComponent<LineRenderer>();
            beam.enabled = false;

            PortalNightsAlly ally = helper.AddComponent<PortalNightsAlly>();
            ally.Configure(26f, 9f, 0.85f, head, muzzle, beam);
            body.name = "ReplaceableHelperVisual";

            NetworkObject networkObject = helper.GetComponent<NetworkObject>();
            if (networkObject != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                networkObject.Spawn(true);
            }

            return health;
        }

        private void SpawnPlanet5BossesServer()
        {
            PortalNightsUniverseScaling scaling = PortalNightsUniverseScaling.ForUniverse(runState == null ? 1 : runState.universeIndex);
            Transform startA = planet5Root == null ? null : planet5Root.Find("Bosses/BossA_SolarWarden_Start");
            Transform startB = planet5Root == null ? null : planet5Root.Find("Bosses/BossB_CrimsonBehemoth_Start");
            planet5BossA = CreatePlanet5Boss("Solar Warden", true, startA == null ? planet5Center + new Vector3(-32f, 0f, 20f) : startA.position, planet5BossAHealth * scaling.bossHpMultiplier, 2.35f, 26f * scaling.enemyDamageMultiplier, 32f, new Color(1f, 0.78f, 0.22f, 1f));
            planet5BossB = CreatePlanet5Boss("Crimson Behemoth", false, startB == null ? planet5Center + new Vector3(32f, 0f, 20f) : startB.position, planet5BossBHealth * scaling.bossHpMultiplier, 2.15f, 34f * scaling.enemyDamageMultiplier, 5f, new Color(1f, 0.16f, 0.08f, 1f));
        }

        private PortalNightsPlanet5BossController CreatePlanet5Boss(string bossName, bool ranged, Vector3 position, float maxHealth, float speed, float damage, float range, Color color)
        {
            GameObject boss = new GameObject("Planet5Boss_" + bossName.Replace(" ", ""));
            boss.transform.position = position + Vector3.up * 0.08f;
            boss.transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            boss.AddComponent<NetworkObject>();
            boss.AddComponent<NetworkTransform>();
            PortalNightsHealth health = boss.AddComponent<PortalNightsHealth>();
            CapsuleCollider capsule = boss.AddComponent<CapsuleCollider>();
            capsule.height = ranged ? 3.2f : 4.1f;
            capsule.radius = ranged ? 0.85f : 1.15f;
            capsule.center = new Vector3(0f, capsule.height * 0.5f, 0f);

            Transform visual = CreateRuntimePrimitive("BossBody", ranged ? PrimitiveType.Capsule : PrimitiveType.Cylinder, boss.transform, new Vector3(0f, capsule.height * 0.5f, 0f), ranged ? new Vector3(1.35f, 1.55f, 1.35f) : new Vector3(2.1f, 2.05f, 2.1f), PortalNightsVfx.CreateRuntimeGlowMaterial(color * 0.55f, color * 2.1f), false).transform;
            Transform aimPoint = CreateRuntimePrimitive("AimPoint", PrimitiveType.Sphere, boss.transform, new Vector3(0f, capsule.height * 0.82f, 0f), Vector3.one * 0.38f, PortalNightsVfx.CreateRuntimeGlowMaterial(color, color * 3f), false).transform;
            visual.name = ranged ? "SolarWardenVisual" : "CrimsonBehemothVisual";

            PortalNightsDamageTarget target = boss.AddComponent<PortalNightsDamageTarget>();
            target.Configure(PortalNightsDamageTargetKind.Planet5Boss, bossName, aimPoint, ranged ? 60 : 55);
            PortalNightsPlanet5BossController controller = boss.AddComponent<PortalNightsPlanet5BossController>();
            controller.Configure(bossName, ranged, maxHealth, speed, damage, range, aimPoint);
            health.ServerInitialize(maxHealth, true);

            NetworkObject networkObject = boss.GetComponent<NetworkObject>();
            if (networkObject != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
            {
                networkObject.Spawn(true);
            }

            return controller;
        }

        private void UpdatePlanet5Server()
        {
            EnsurePlanet5Area();
            if (planet5Root == null)
            {
                return;
            }

            if (GameState == PortalNightsGameState.Planet5_BossIntro)
            {
                planet5BossIntroTimer = Mathf.Max(0f, planet5BossIntroTimer - Time.deltaTime);
                if (planet5BossIntroTimer <= 0.01f)
                {
                    SetGameState(PortalNightsGameState.Planet5_DestroyHealingSphere);
                    BroadcastToastClientRpc("DESTROY THE HEALING SPHERE - BOSS HEALING ACTIVE");
                }
            }

            if (GameState == PortalNightsGameState.Planet5_DestroyHealingSphere)
            {
                UpdatePlanet5HealingSphereServer();
                UpdatePlanet5HelperDialogueServer();
            }

            if (GameState == PortalNightsGameState.Planet5_KillBosses && planet5BossesDefeated.Value >= 2)
            {
                ActivatePlanet5StabilizersServer();
                SetGameState(PortalNightsGameState.Planet5_RestoreSphereReady);
                BroadcastToastClientRpc("BOSSES DEFEATED - RESTORE THE SPHERE");
                MissionToastClientRpc("BOSSES DEFEATED");
            }
        }

        private void UpdatePlanet5HealingSphereServer()
        {
            if (planet5SphereDestroyed || planet5SphereHealth == null || planet5SphereHealth.IsDead)
            {
                return;
            }

            planet5HealTimer = Mathf.Max(0f, planet5HealTimer - Time.deltaTime);
            ProtectPlanet5BossFromDeath(planet5BossA);
            ProtectPlanet5BossFromDeath(planet5BossB);
            if (planet5HealTimer > 0.01f)
            {
                return;
            }

            planet5HealTimer = planet5BossHealInterval;
            HealPlanet5Boss(planet5BossA);
            HealPlanet5Boss(planet5BossB);
            Planet5HealingPulseClientRpc(planet5SphereHealth.transform.position + Vector3.up * 1.5f);
        }

        private void ProtectPlanet5BossFromDeath(PortalNightsPlanet5BossController boss)
        {
            if (boss == null || boss.Health == null)
            {
                return;
            }

            if (boss.Health.CurrentHealth <= boss.Health.MaxHealth * planet5BossDeathProtectThreshold)
            {
                boss.Health.ServerSetCurrentHealth(boss.Health.MaxHealth * planet5BossDeathProtectHealTo);
            }
        }

        private void HealPlanet5Boss(PortalNightsPlanet5BossController boss)
        {
            if (boss == null || boss.Health == null || boss.Health.IsDead)
            {
                return;
            }

            boss.Health.HealServer(boss.Health.MaxHealth * planet5BossHealFraction);
        }

        private void UpdatePlanet5HelperDialogueServer()
        {
            planet5SphereIgnoreTimer += Time.deltaTime;
            if (!planet5FirstSphereHintShown && planet5SphereIgnoreTimer >= 18f)
            {
                planet5FirstSphereHintShown = true;
                PlayReminderDialogueServer("p05_destroy_sphere_hint_001", PortalNightsLocalization.Text("objective.destroyCorruptedSphere"), string.Empty, PortalNightsObjectiveSeverity.Danger);
            }

            if (planet5BossPressureReminderStarted)
            {
                planet5BossPressureReminderTimer += Time.deltaTime;
            }

            if (!planet5SecondSphereHintShown && planet5BossPressureReminderStarted && planet5BossPressureReminderTimer >= 25f)
            {
                planet5SecondSphereHintShown = true;
                PlayReminderDialogueServer("p05_bosses_healed_hint_001", PortalNightsLocalization.Text("objective.destroyCorruptedSphere"), PortalNightsLocalization.Text("progress.bossesHealing"), PortalNightsObjectiveSeverity.Danger);
            }
        }

        private void HandlePlanet5SphereHealthChanged(PortalNightsHealth health)
        {
            if (!IsServer || health == null || !IsPlanet5State(GameState))
            {
                return;
            }

            if (health.CurrentHealth < planet5SphereLastHealth - 0.5f)
            {
                planet5SphereIgnoreTimer = 0f;
                planet5FirstSphereHintShown = false;
                planet5SecondSphereHintShown = false;
                ResetReminderTimer(ref planet5BossPressureReminderTimer, ref planet5BossPressureReminderStarted, "P5 boss pressure sphere damaged");
            }

            planet5SphereLastHealth = health.CurrentHealth;
        }

        private void HandlePlanet5SphereDestroyed(PortalNightsHealth health)
        {
            if (!IsServer || planet5SphereDestroyed || !IsPlanet5State(GameState))
            {
                return;
            }

            planet5SphereDestroyed = true;
            planet5HealTimer = 0f;
            ResetReminderTimer(ref planet5BossPressureReminderTimer, ref planet5BossPressureReminderStarted, "P5 boss pressure sphere destroyed");
            if (planet5SphereTarget != null)
            {
                planet5SphereTarget.SetTargetable(false);
            }

            planet5SphereVisual?.SetVisualState(PortalNightsSphereVisualState.DamagedCore);
            SetPlanet5SphereVisualClientRpc((int)PortalNightsSphereVisualState.DamagedCore);
            SetGameState(PortalNightsGameState.Planet5_KillBosses);
            BroadcastToastClientRpc("HEALING DISABLED - KILL THE BOSSES");
        }

        public bool HandlePlanet5BossDeathServer(PortalNightsPlanet5BossController boss)
        {
            if (!IsServer || boss == null)
            {
                return true;
            }

            if (!planet5SphereDestroyed)
            {
                boss.Health?.ServerSetCurrentHealth(boss.Health.MaxHealth * planet5BossDeathProtectHealTo);
                BroadcastToastClientRpc("THE SPHERE SAVED " + boss.BossName.ToUpperInvariant());
                Planet5HealingPulseClientRpc(planet5SphereHealth == null ? boss.transform.position : planet5SphereHealth.transform.position + Vector3.up * 1.5f);
                return true;
            }

            planet5BossesDefeated.Value = Mathf.Clamp(planet5BossesDefeated.Value + 1, 0, 2);
            if (runState != null)
            {
                runState.bossesKilled++;
                runState.score += PortalNightsScoreCalculator.BossKilled * PortalNightsUniverseScaling.ForUniverse(runState.universeIndex).scoreMultiplier;
            }

            if (planet5BossesDefeated.Value >= 2 && GameState == PortalNightsGameState.Planet5_KillBosses)
            {
                ActivatePlanet5StabilizersServer();
                SetGameState(PortalNightsGameState.Planet5_RestoreSphereReady);
                BroadcastToastClientRpc("BOSSES DEFEATED - RESTORE THE SPHERE");
                MissionToastClientRpc("BOSSES DEFEATED");
            }

            return false;
        }

        private void ActivatePlanet5StabilizersServer()
        {
            SetPlanet5StabilizersServer(true, false);
            planet5StabilizersCompleted.Value = CountPlanet5CompletedStabilizers();
            BroadcastToastClientRpc("STABILIZERS ONLINE - HOLD E TO STABILIZE");
        }

        private void SetPlanet5StabilizersServer(bool available, bool completed)
        {
            EnsurePlanet5Area();
            for (int i = 0; i < planet5Stabilizers.Count; i++)
            {
                PortalNightsPlanet5Stabilizer stabilizer = planet5Stabilizers[i];
                if (stabilizer == null)
                {
                    continue;
                }

                stabilizer.SetState(available, completed);
                SetPlanet5StabilizerVisualClientRpc(i, available, completed);
            }
        }

        private void UpdatePlanet5HeldInteractServer(Vector3 playerPosition, ulong clientId, float deltaTime)
        {
            if (GameState != PortalNightsGameState.Planet5_RestoreSphereReady && GameState != PortalNightsGameState.Planet5_RestoringSphere)
            {
                CancelPlanet5StabilizerHold();
                return;
            }

            PortalNightsPlanet5Stabilizer stabilizer = GetClosestPlanet5Stabilizer(playerPosition, planet5StabilizerInteractRange, true);
            if (stabilizer == null)
            {
                CancelPlanet5StabilizerHold();
                return;
            }

            bool sameHold = planet5StabilizerHoldClientId == clientId && planet5HoldStabilizer == stabilizer;
            if (!sameHold)
            {
                CancelPlanet5StabilizerHold(false);
                planet5StabilizerHoldClientId = clientId;
                planet5HoldStabilizer = stabilizer;
                planet5StabilizerHoldProgress = 0f;
                SetGameState(PortalNightsGameState.Planet5_RestoringSphere);
                ResetReminderTimer(ref planet5RestoreSphereReminderTimer, ref planet5RestoreSphereReminderStarted, "P5 restore sphere started");
                SendToast(clientId, PortalNightsLocalization.Format("prompt.holdStabilize", string.Empty, string.Empty));
            }

            planet5StabilizerHoldProgress = Mathf.Min(planet5StabilizerHoldTime, planet5StabilizerHoldProgress + Mathf.Max(0f, deltaTime));
            if (planet5StabilizerHoldProgress < planet5StabilizerHoldTime)
            {
                return;
            }

            CompletePlanet5StabilizerServer(stabilizer);
        }

        private void CompletePlanet5StabilizerServer(PortalNightsPlanet5Stabilizer stabilizer)
        {
            if (stabilizer == null || stabilizer.IsCompleted)
            {
                CancelPlanet5StabilizerHold();
                return;
            }

            stabilizer.SetState(true, true);
            int index = GetPlanet5StabilizerIndex(stabilizer);
            SetPlanet5StabilizerVisualClientRpc(index, true, true);
            planet5StabilizersCompleted.Value = CountPlanet5CompletedStabilizers();
            Planet5StabilizerCompletedClientRpc(stabilizer.InteractionPoint, stabilizer.StabilizerId, planet5StabilizersCompleted.Value, Mathf.Max(3, planet5Stabilizers.Count));
            CancelPlanet5StabilizerHold(false);

            if (planet5StabilizersCompleted.Value >= Mathf.Max(3, planet5Stabilizers.Count))
            {
                CompletePlanet5SphereRestorationServer();
                return;
            }

            SetGameState(PortalNightsGameState.Planet5_RestoreSphereReady);
        }

        private void CompletePlanet5SphereRestorationServer()
        {
            if (!IsServer || planet5UniverseCompleteSubmitted)
            {
                return;
            }

            planet5UniverseCompleteSubmitted = true;
            planet5SphereVisual?.SetVisualState(PortalNightsSphereVisualState.Restored);
            if (planet5SphereTarget != null)
            {
                planet5SphereTarget.SetTargetable(false);
            }

            SetPlanet5SphereVisualClientRpc((int)PortalNightsSphereVisualState.Restored);
            Planet5RestoredPulseClientRpc(planet5SphereVisual == null ? planet5Center + Vector3.up * 3f : planet5SphereVisual.transform.position + Vector3.up * 2.4f);

            if (runState == null)
            {
                runState = new PortalNightsRunState();
                runState.StartRun(1, 5);
            }

            runState.UpdateTotalRunTime();
            runState.currentPlanetIndex = 5;
            runState.MarkPlanetCleared(5);
            runState.spheresRestored += 1;
            runState.bossesKilled = Mathf.Max(runState.bossesKilled, 2);
            runState.universeCompleted = true;
            PortalNightsUniverseScaling scaling = PortalNightsUniverseScaling.ForUniverse(runState.universeIndex);
            int completionScore = PortalNightsScoreCalculator.Planet5Cleared
                + PortalNightsScoreCalculator.SphereRestored
                + PortalNightsScoreCalculator.GetUniverseCompleteScore(runState.universeIndex);
            runState.score += completionScore * scaling.scoreMultiplier;

            PortalNightsLeaderboardEntry entry = PortalNightsLeaderboardEntry.FromRunState(runState);
            leaderboardService.SubmitScore(entry);
            universeCompleteLeaderboardText = BuildLeaderboardText(leaderboardService.GetTopEntries(5), runState.universeIndex + 1);

            CreatePlanet5UniversePortalServer();
            SetGameState(PortalNightsGameState.Planet5_SphereRestored);
            BroadcastToastClientRpc("SPHERE RESTORED - UNIVERSE STABILIZED");
            MissionToastClientRpc("SPHERE RESTORED");
            SetGameState(PortalNightsGameState.Planet5_UniverseComplete);
            UniverseCompleteSummaryClientRpc(universeCompleteLeaderboardText);
        }

        private void CancelPlanet5StabilizerHold(bool restoreReadyState = true)
        {
            if (restoreReadyState && GameState == PortalNightsGameState.Planet5_RestoringSphere)
            {
                SetGameState(PortalNightsGameState.Planet5_RestoreSphereReady);
            }

            planet5StabilizerHoldClientId = ulong.MaxValue;
            planet5HoldStabilizer = null;
            planet5StabilizerHoldProgress = 0f;
        }

        private PortalNightsPlanet5Stabilizer GetClosestPlanet5Stabilizer(Vector3 position, float range, bool requireInteractable)
        {
            EnsurePlanet5Area();
            PortalNightsPlanet5Stabilizer best = null;
            float bestSqr = range * range;
            for (int i = planet5Stabilizers.Count - 1; i >= 0; i--)
            {
                PortalNightsPlanet5Stabilizer stabilizer = planet5Stabilizers[i];
                if (stabilizer == null)
                {
                    planet5Stabilizers.RemoveAt(i);
                    continue;
                }

                if (requireInteractable && !stabilizer.CanInteract)
                {
                    continue;
                }

                float sqr = PortalNightsMath.Flat(stabilizer.InteractionPoint - position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = stabilizer;
                }
            }

            return best;
        }

        private int CountPlanet5CompletedStabilizers()
        {
            int count = 0;
            for (int i = 0; i < planet5Stabilizers.Count; i++)
            {
                if (planet5Stabilizers[i] != null && planet5Stabilizers[i].IsCompleted)
                {
                    count++;
                }
            }

            return count;
        }

        private int GetPlanet5StabilizerIndex(PortalNightsPlanet5Stabilizer stabilizer)
        {
            return stabilizer == null ? -1 : planet5Stabilizers.IndexOf(stabilizer);
        }

        public string GetPlanet5InteractionPrompt(Vector3 playerPosition)
        {
            if (!IsPlanet5State(GameState))
            {
                return string.Empty;
            }

            if (GameState == PortalNightsGameState.Planet5_UniverseComplete && IsNearPlanet5UniversePortal(playerPosition))
            {
                return PortalNightsLocalization.Format("hud.enterNextUniverse", (runState == null ? 1 : runState.universeIndex) + 1);
            }

            if (GameState != PortalNightsGameState.Planet5_RestoreSphereReady && GameState != PortalNightsGameState.Planet5_RestoringSphere)
            {
                return string.Empty;
            }

            PortalNightsPlanet5Stabilizer stabilizer = GetClosestPlanet5Stabilizer(playerPosition, planet5StabilizerInteractRange, true);
            if (stabilizer == null)
            {
                return string.Empty;
            }

            string progress = planet5HoldStabilizer == stabilizer && Planet5StabilizerHoldProgress > 0.01f
                ? " " + Mathf.CeilToInt(Planet5StabilizerHoldProgress * 100f) + "%"
                : string.Empty;
            return PortalNightsLocalization.Format("prompt.holdStabilize", string.Empty, progress);
        }

        private bool IsNearPlanet5UniversePortal(Vector3 position)
        {
            return planet5UniversePortal != null && PortalNightsMath.Flat(planet5UniversePortal.position - position).sqrMagnitude <= planet5UniversePortalRange * planet5UniversePortalRange;
        }

        private void CreatePlanet5UniversePortalServer()
        {
            planet5UniversePortal = CreatePlanet5UniversePortalVisualLocal();
            SetPlanet5UniversePortalVisualClientRpc(true);
        }

        private void EnterNextUniverseServer()
        {
            if (!IsServer || GameState != PortalNightsGameState.Planet5_UniverseComplete)
            {
                return;
            }

            if (runState == null)
            {
                runState = new PortalNightsRunState();
                runState.StartRun(1, 1);
            }

            BeginPlanetTransitionClientRpc(5, 1);
            SetActivePlanetEnvironment(1);
            runState.EnterNextUniverse();
            runState.currentPlanetIndex = 1;
            int universe = runState.universeIndex;
            PortalNightsUniverseScaling scaling = PortalNightsUniverseScaling.ForUniverse(universe);

            ClearRuntimeEnemiesServer();
            ClearRuntimeAlliesServer();
            ResetPlanet3RuntimeState(false);
            ResetPlanet4RuntimeState(false);
            ResetPlanet5RuntimeState(true);

            waveNumber.Value = 0;
            enemiesAlive.Value = 0;
            enemiesRemaining.Value = 0;
            leftLaneEnemies.Value = 0;
            rightLaneEnemies.Value = 0;
            waveRunning.Value = false;
            gameOver.Value = false;
            nextWaveTimer.Value = Mathf.Max(firstWaveDelay, 12f);
            chargeVfxShown = false;
            activeWave = default;
            spawnedSmall = 0;
            spawnedBrute = 0;
            spawnedLeftSmall = 0;
            spawnedRightSmall = 0;
            spawnedLeftBrute = 0;
            spawnedRightBrute = 0;
            planet2DefenseWaveIndex = 0;
            planet2EnemiesToSpawn = 0;
            planet2BrutesToSpawn = 0;
            planet2SpawnedSmall = 0;
            planet2SpawnedBrute = 0;
            planet2SpawnTimer = 0f;
            coins.Value = startingCoins;
            turretDamageBoostTimer = 0f;

            if (coreHealth != null)
            {
                coreHealth.ServerInitialize(Mathf.Max(coreHealth.BaseMaxHealth, 1500f), true);
            }

            if (planet2SphereHealth != null)
            {
                planet2SphereHealth.ServerInitialize(500f, true);
                planet2SphereHealth.gameObject.SetActive(false);
            }

            foreach (PortalNightsPlayerController player in FindObjectsByType<PortalNightsPlayerController>(FindObjectsSortMode.None))
            {
                if (player == null)
                {
                    continue;
                }

                player.ResetTemporaryBoostsServer();
                if (player.Health != null)
                {
                    player.Health.ServerInitialize(player.Health.BaseMaxHealth, true);
                }
            }

            SetPlanet5UniversePortalVisualClientRpc(false);
            SetGameState(PortalNightsGameState.Planet1_Defense);
            TeleportPlayersToPlanet1Server();
            RefreshRequiredTurretProgressServer();
            UniverseEnteredClientRpc(universe, scaling.enemyHpMultiplier, scaling.enemyDamageMultiplier, scaling.scoreMultiplier);
            CompletePlanetTransitionClientRpc(1);
        }

        private void TeleportPlayersToPlanet1Server()
        {
            PortalNightsPlayerController[] players = FindObjectsByType<PortalNightsPlayerController>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                PortalNightsPlayerController player = players[i];
                if (player == null)
                {
                    continue;
                }

                Vector3 position;
                Quaternion rotation;
                if (playerSpawnPoints != null && playerSpawnPoints.Length > 0)
                {
                    int index = i % playerSpawnPoints.Length;
                    position = playerSpawnPoints[index] == null ? new Vector3(0f, 1.55f, -10.75f) : playerSpawnPoints[index].position;
                    rotation = playerSpawnPoints[index] == null ? Quaternion.identity : playerSpawnPoints[index].rotation;
                }
                else
                {
                    position = new Vector3(0f, 1.55f, -10.75f) + new Vector3((i - players.Length * 0.5f) * 1.8f, 0f, 0f);
                    rotation = Quaternion.identity;
                }

                position.y = Mathf.Max(position.y, 1.55f);
                player.transform.SetPositionAndRotation(position, rotation);
            }
        }

        private Transform CreatePlanet5UniversePortalVisualLocal()
        {
            if (!IsPlanetEnvironmentActive(5))
            {
                return null;
            }

            EnsurePlanet5Area();
            if (planet5Root == null)
            {
                return null;
            }

            Transform existing = planet5Root.Find("Universe2Portal");
            if (existing != null)
            {
                existing.gameObject.SetActive(true);
                return existing;
            }

            GameObject portal = new GameObject("Universe2Portal");
            portal.transform.SetParent(planet5Root, false);
            portal.transform.localPosition = new Vector3(0f, 0.4f, -48f);
            portal.transform.localRotation = Quaternion.identity;
            portal.transform.localScale = Vector3.one;

            Material cyan = PortalNightsVfx.CreateRuntimeGlowMaterial(new Color(0.39f, 0.97f, 1f, 1f), new Color(0.39f, 0.97f, 1f, 1f) * 3.8f);
            Material white = PortalNightsVfx.CreateRuntimeGlowMaterial(Color.white, new Color(0.8f, 1f, 1f, 1f) * 2.2f);
            CreateRuntimePrimitive("UniversePortal_Platform", PrimitiveType.Cylinder, portal.transform, Vector3.zero, new Vector3(10f, 0.22f, 10f), cyan, false);
            CreateRuntimePrimitive("UniversePortal_Core", PrimitiveType.Sphere, portal.transform, new Vector3(0f, 3.2f, 0f), new Vector3(5.6f, 5.6f, 0.5f), cyan, false);
            CreateSegmentedRing("UniversePortal_Ring", portal.transform, 4.1f, 24, new Vector3(1f, 0.18f, 0.35f), cyan, 3.2f);
            CreateRuntimePrimitive("UniversePortal_Beam", PrimitiveType.Cylinder, portal.transform, new Vector3(0f, 14f, 0f), new Vector3(0.38f, 14f, 0.38f), white, false);
            CreateWorldLabel("UniversePortal_Label", portal.transform, new Vector3(0f, 0.7f, -6f), PortalNightsLocalization.Format("hud.enterNextUniverse", (runState == null ? 1 : runState.universeIndex) + 1), new Color(0.39f, 0.97f, 1f, 1f));

            Light light = portal.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.39f, 0.97f, 1f, 1f);
            light.range = 28f;
            light.intensity = 5.5f;
            return portal.transform;
        }

        private static string BuildLeaderboardText(IReadOnlyList<PortalNightsLeaderboardEntry> topEntries, int nextUniverse)
        {
            System.Text.StringBuilder builder = new System.Text.StringBuilder();
            builder.AppendLine(PortalNightsLocalization.Text("hud.localLeaderboard"));
            builder.AppendLine(PortalNightsLocalization.Text("hud.leaderboardHeader"));
            if (topEntries == null || topEntries.Count == 0)
            {
                builder.AppendLine(PortalNightsLocalization.Text("hud.noLeaderboard"));
            }
            else
            {
                int count = Mathf.Min(5, topEntries.Count);
                for (int i = 0; i < count; i++)
                {
                    PortalNightsLeaderboardEntry entry = topEntries[i];
                    if (entry == null)
                    {
                        continue;
                    }

                    builder.Append(i + 1);
                    builder.Append("     ");
                    string playerName = string.IsNullOrWhiteSpace(entry.playerName) ? PortalNightsLocalization.Text("hud.defaultPlayerName") : entry.playerName;
                    builder.Append(playerName.Length > 10 ? playerName.Substring(0, 10) : playerName.PadRight(10));
                    builder.Append("  ");
                    builder.Append(entry.score);
                    builder.Append("  ");
                    builder.Append(entry.universe);
                    builder.Append("  ");
                    builder.Append(FormatLeaderboardTime(entry.totalTime));
                    builder.Append("  ");
                    builder.Append(entry.enemiesKilled);
                    builder.Append("       ");
                    builder.Append(entry.bossesKilled);
                    builder.AppendLine();
                }
            }

            builder.AppendLine();
            builder.Append(PortalNightsLocalization.Format("hud.enterNextUniverse", Mathf.Max(2, nextUniverse)));
            builder.Append("   ");
            builder.Append(PortalNightsLocalization.Text("hud.leaderboardToggle"));
            return builder.ToString();
        }

        private static string FormatLeaderboardTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.RoundToInt(seconds));
            int minutes = totalSeconds / 60;
            int secs = totalSeconds % 60;
            return minutes.ToString("00") + ":" + secs.ToString("00");
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

        private static bool IsPlanet3State(PortalNightsGameState state)
        {
            return state == PortalNightsGameState.Planet3_Arrival
                || state == PortalNightsGameState.Planet3_FindStaff
                || state == PortalNightsGameState.Planet3_ReleaseStaff
                || state == PortalNightsGameState.Planet3_EscortToSphere
                || state == PortalNightsGameState.Planet3_SphereReady
                || state == PortalNightsGameState.Planet3_SphereActivation
                || state == PortalNightsGameState.Planet3_DefensePreparation
                || state == PortalNightsGameState.Planet3_DefendSphere
                || state == PortalNightsGameState.Planet3_Cleared
                || state == PortalNightsGameState.Planet3_Failed;
        }

        private static bool IsPlanet4State(PortalNightsGameState state)
        {
            return state == PortalNightsGameState.Planet4_Arrival
                || state == PortalNightsGameState.Planet4_HordeActive
                || state == PortalNightsGameState.Planet4_RiftClosing
                || state == PortalNightsGameState.Planet4_ExitPortalReady
                || state == PortalNightsGameState.Planet4_Cleared
                || state == PortalNightsGameState.Planet4_Failed;
        }

        private static bool IsPlanet5State(PortalNightsGameState state)
        {
            return state == PortalNightsGameState.Planet5_Arrival
                || state == PortalNightsGameState.Planet5_BossIntro
                || state == PortalNightsGameState.Planet5_DestroyHealingSphere
                || state == PortalNightsGameState.Planet5_KillBosses
                || state == PortalNightsGameState.Planet5_RestoreSphereReady
                || state == PortalNightsGameState.Planet5_Failed
                || state == PortalNightsGameState.Planet5_RestoringSphere
                || state == PortalNightsGameState.Planet5_SphereRestored
                || state == PortalNightsGameState.Planet5_UniverseComplete;
        }

        private static bool IsBuildAllowedState(PortalNightsGameState state)
        {
            return state == PortalNightsGameState.Planet1_Defense
                || state == PortalNightsGameState.Planet1_PortalReady
                || state == PortalNightsGameState.Planet2_DefendSphere
                || state == PortalNightsGameState.Planet3_DefensePreparation
                || state == PortalNightsGameState.Planet3_DefendSphere
                || state == PortalNightsGameState.Planet4_HordeActive
                || state == PortalNightsGameState.Planet4_RiftClosing
                || state == PortalNightsGameState.Planet4_ExitPortalReady
                || state == PortalNightsGameState.Planet5_DestroyHealingSphere
                || state == PortalNightsGameState.Planet5_KillBosses
                || state == PortalNightsGameState.Planet5_RestoreSphereReady;
        }

        private void SetActivePlanetEnvironment(int planetIndex)
        {
            CachePlanetRootReferencesOnly();
            int clampedPlanet = Mathf.Clamp(planetIndex, 1, 5);
            activePlanetEnvironmentIndex = clampedPlanet;
            SetPlanetRootEnvironment(planet2Root, clampedPlanet == 2);
            SetPlanetRootEnvironment(planet3Root, clampedPlanet == 3);
            SetPlanetRootEnvironment(planet4Root, clampedPlanet == 4);
            SetPlanetRootEnvironment(planet5Root, clampedPlanet == 5);
            HideAllObjectiveMarkers();
        }

        private bool PreparePlanetForEntry(int planetIndex)
        {
            int clampedPlanet = Mathf.Clamp(planetIndex, 1, 5);
            CachePlanetRootReferencesOnly();
            SetActivePlanetEnvironment(clampedPlanet);

            switch (clampedPlanet)
            {
                case 2:
                    EnsurePlanet2Area();
                    SpawnUnspawnedNetworkObjectsForPlanetRoot(planet2Root);
                    return planet2Root != null;
                case 3:
                    EnsurePlanet3Area();
                    SpawnUnspawnedNetworkObjectsForPlanetRoot(planet3Root);
                    return planet3Root != null;
                case 4:
                    EnsurePlanet4Area();
                    SpawnUnspawnedNetworkObjectsForPlanetRoot(planet4Root);
                    return planet4Root != null;
                case 5:
                    EnsurePlanet5Area();
                    SpawnUnspawnedNetworkObjectsForPlanetRoot(planet5Root);
                    return planet5Root != null;
                default:
                    return true;
            }
        }

        private Transform GetPlanetRoot(int planetIndex)
        {
            switch (planetIndex)
            {
                case 2: return planet2Root;
                case 3: return planet3Root;
                case 4: return planet4Root;
                case 5: return planet5Root;
                default: return null;
            }
        }

        private void SpawnUnspawnedNetworkObjectsForPlanetRoot(Transform root)
        {
            NetworkManager manager = NetworkManager.Singleton;
            if (!IsServer || root == null || manager == null || !manager.IsListening)
            {
                return;
            }

            NetworkObject[] networkObjects = root.GetComponentsInChildren<NetworkObject>(true);
            for (int i = 0; i < networkObjects.Length; i++)
            {
                NetworkObject networkObject = networkObjects[i];
                if (networkObject == null || networkObject.IsSpawned)
                {
                    continue;
                }

                if (networkObject.gameObject.scene.IsValid())
                {
                    continue;
                }

                try
                {
                    networkObject.Spawn();
                }
                catch (System.Exception exception)
                {
                    if (!warnedLazyPlanetNetworkSpawnFailure)
                    {
                        warnedLazyPlanetNetworkSpawnFailure = true;
                        Debug.LogWarning("[PortalNights] Lazy planet NetworkObject spawn failed. The object will stay local until Netcode owns it: " + exception.Message, this);
                    }
                }
            }
        }

        private static int GetPlanetIndexForState(PortalNightsGameState state)
        {
            if (IsPlanet5State(state))
            {
                return 5;
            }

            if (IsPlanet4State(state))
            {
                return 4;
            }

            if (IsPlanet3State(state))
            {
                return 3;
            }

            if (IsPlanet2State(state))
            {
                return 2;
            }

            return 1;
        }

        private void SetPlanetRootEnvironment(Transform root, bool active)
        {
            if (root == null)
            {
                return;
            }

            if (!active && hardDisableInactivePlanetRoots)
            {
                SetRootActiveFast(root, false);
                return;
            }

            if (active && !root.gameObject.activeSelf)
            {
                root.gameObject.SetActive(true);
            }

            if (!planetEnvironmentCaches.TryGetValue(root, out PlanetEnvironmentCache cache))
            {
                cache = new PlanetEnvironmentCache(root);
                planetEnvironmentCaches[root] = cache;
            }

            cache.Apply(active);
        }

        private void UpdatePerformanceDebug()
        {
            if (!performanceDebug)
            {
                return;
            }

            performanceDebugTimer -= Time.unscaledDeltaTime;
            if (performanceDebugTimer > 0f)
            {
                return;
            }

            performanceDebugTimer = 2f;
            int projectileCount = FindObjectsByType<PortalNightsProjectile>(FindObjectsSortMode.None).Length;
            int burstCount = CountActiveNamedObjects("PN_VFX_Burst");
            int floatingTextCount = CountActiveNamedObjects("PN_FloatingText");
            int extraBeamCount = CountActiveNamedObjects("PN_Turret_ExtraBeam");
            int pickupCount = FindObjectsByType<PortalNightsPickup>(FindObjectsSortMode.None).Length;
            int enemyCount = FindObjectsByType<PortalNightsEnemy>(FindObjectsSortMode.None).Length;
            int particleCount = FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Length;
            int lightCount = FindObjectsByType<Light>(FindObjectsSortMode.None).Length;
            int rendererCount = FindObjectsByType<Renderer>(FindObjectsSortMode.None).Length;
            float sampleSeconds = 2f;
            float projectileSpawnsPerSecond = PortalNightsProjectile.ConsumeSpawnCount() / sampleSeconds;
            float burstSpawnsPerSecond = PortalNightsVfx.ConsumeBurstSpawnCount() / sampleSeconds;
            float turretShotsPerSecond = PortalNightsAlly.ConsumeTurretShotCount() / sampleSeconds;
            float fps = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            Debug.Log(
                "[PortalNightsPerf] fps=" + Mathf.RoundToInt(fps)
                + " state=" + GameState
                + " projectiles=" + projectileCount
                + " bursts=" + burstCount
                + " floatingText=" + floatingTextCount
                + " extraBeams=" + extraBeamCount
                + " pickups=" + pickupCount
                + " enemies=" + enemyCount
                + " turrets=" + CountActiveAllies()
                + " particles=" + particleCount
                + " lights=" + lightCount
                + " renderers=" + rendererCount
                + " projectileSpawns/s=" + projectileSpawnsPerSecond.ToString("0.0")
                + " burstSpawns/s=" + burstSpawnsPerSecond.ToString("0.0")
                + " turretShots/s=" + turretShotsPerSecond.ToString("0.0")
                + " activePlanet=" + activePlanetEnvironmentIndex
                + " Planet2_CrystalMoon=" + IsPlanetRootLoadActive(planet2Root)
                + " Planet3_AshRelayStation=" + IsPlanetRootLoadActive(planet3Root)
                + " Planet4_SwarmExpanse=" + IsPlanetRootLoadActive(planet4Root)
                + " Planet5_CrimsonSingularity=" + IsPlanetRootLoadActive(planet5Root),
                this);
        }

        private int CountActiveAllies()
        {
            int count = 0;
            for (int i = allies.Count - 1; i >= 0; i--)
            {
                PortalNightsAlly ally = allies[i];
                if (ally == null)
                {
                    allies.RemoveAt(i);
                    continue;
                }

                if (ally.gameObject.activeInHierarchy)
                {
                    count++;
                }
            }

            return count;
        }

        private static int CountActiveNamedObjects(string objectName)
        {
            int count = 0;
            Transform[] transforms = FindObjectsByType<Transform>(FindObjectsSortMode.None);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != null && candidate.gameObject.activeInHierarchy && candidate.name == objectName)
                {
                    count++;
                }
            }

            return count;
        }

        private bool IsPlanetRootLoadActive(Transform root)
        {
            return root != null
                && root.gameObject.activeInHierarchy
                && (!planetEnvironmentCaches.TryGetValue(root, out PlanetEnvironmentCache cache) || cache.RenderLoadActive);
        }

        private bool IsPlanetEnvironmentActive(int planetIndex)
        {
            Transform root = GetPlanetRoot(planetIndex);
            return root != null && root.gameObject.activeInHierarchy && activePlanetEnvironmentIndex == planetIndex;
        }

        private static bool ShouldTogglePlanetBehaviour(MonoBehaviour behaviour)
        {
            if (behaviour == null || behaviour is NetworkBehaviour)
            {
                return false;
            }

            return behaviour is PortalNightsPlanet4HiveRift
                || behaviour is PortalNightsPlanet5HealingSphere
                || behaviour is PortalNightsPlanet5Stabilizer
                || behaviour is PortalNightsDamageTarget;
        }

        private sealed class PlanetEnvironmentCache
        {
            private readonly Renderer[] renderers;
            private readonly bool[] rendererEnabled;
            private readonly Light[] lights;
            private readonly bool[] lightEnabled;
            private readonly Collider[] colliders;
            private readonly bool[] colliderEnabled;
            private readonly ParticleSystem[] particleSystems;
            private readonly bool[] particleShouldPlayWhenActive;
            private readonly MonoBehaviour[] behaviours;
            private readonly bool[] behaviourEnabled;

            public bool RenderLoadActive { get; private set; }

            public PlanetEnvironmentCache(Transform root)
            {
                renderers = root.GetComponentsInChildren<Renderer>(true);
                rendererEnabled = new bool[renderers.Length];
                for (int i = 0; i < renderers.Length; i++)
                {
                    rendererEnabled[i] = renderers[i] != null && renderers[i].enabled;
                }

                lights = root.GetComponentsInChildren<Light>(true);
                lightEnabled = new bool[lights.Length];
                for (int i = 0; i < lights.Length; i++)
                {
                    lightEnabled[i] = lights[i] != null && lights[i].enabled;
                }

                colliders = root.GetComponentsInChildren<Collider>(true);
                colliderEnabled = new bool[colliders.Length];
                for (int i = 0; i < colliders.Length; i++)
                {
                    colliderEnabled[i] = colliders[i] != null && colliders[i].enabled;
                }

                particleSystems = root.GetComponentsInChildren<ParticleSystem>(true);
                particleShouldPlayWhenActive = new bool[particleSystems.Length];
                for (int i = 0; i < particleSystems.Length; i++)
                {
                    if (particleSystems[i] == null)
                    {
                        continue;
                    }

                    ParticleSystem.MainModule main = particleSystems[i].main;
                    particleShouldPlayWhenActive[i] = particleSystems[i].isPlaying || main.loop;
                }

                List<MonoBehaviour> toggledBehaviours = new List<MonoBehaviour>();
                MonoBehaviour[] allBehaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                for (int i = 0; i < allBehaviours.Length; i++)
                {
                    MonoBehaviour behaviour = allBehaviours[i];
                    if (ShouldTogglePlanetBehaviour(behaviour))
                    {
                        toggledBehaviours.Add(behaviour);
                    }
                }

                behaviours = toggledBehaviours.ToArray();
                behaviourEnabled = new bool[behaviours.Length];
                for (int i = 0; i < behaviours.Length; i++)
                {
                    behaviourEnabled[i] = behaviours[i] != null && behaviours[i].enabled;
                }
            }

            public void Apply(bool active)
            {
                RenderLoadActive = active;
                for (int i = 0; i < renderers.Length; i++)
                {
                    if (renderers[i] != null)
                    {
                        renderers[i].enabled = active && rendererEnabled[i];
                    }
                }

                for (int i = 0; i < lights.Length; i++)
                {
                    if (lights[i] != null)
                    {
                        lights[i].enabled = active && lightEnabled[i];
                    }
                }

                for (int i = 0; i < colliders.Length; i++)
                {
                    if (colliders[i] != null)
                    {
                        colliders[i].enabled = active && colliderEnabled[i];
                    }
                }

                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] != null)
                    {
                        behaviours[i].enabled = active && behaviourEnabled[i];
                    }
                }

                for (int i = 0; i < particleSystems.Length; i++)
                {
                    ParticleSystem particleSystem = particleSystems[i];
                    if (particleSystem == null)
                    {
                        continue;
                    }

                    if (active && particleShouldPlayWhenActive[i])
                    {
                        particleSystem.Play(true);
                    }
                    else if (!active)
                    {
                        particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    }
                }
            }
        }

        private void EnsurePlanet3Area()
        {
            CachePlanetRootReferencesOnly();
            planet3Root ??= arenaRoot == null ? null : arenaRoot.Find("Planet3_AshRelayStation");
            if (planet3Root == null)
            {
                return;
            }

            planet3Center = planet3Root.position;
            planet3ArrivalPoint = planet3Root.Find("ArrivalZone/PlayerArrivalPoint");
            planet3RelaySphere = planet3Root.Find("CentralStation/RelaySphere");
            planet3RelayHealth = planet3RelaySphere == null ? null : planet3RelaySphere.GetComponent<PortalNightsHealth>();
            if (planet3RelayHealth != null)
            {
                planet3RelayHealth.Died -= HandlePlanet3RelaySphereDeath;
                planet3RelayHealth.Died += HandlePlanet3RelaySphereDeath;
            }

            planet3Staff.Clear();
            foreach (PortalNightsStaffRescue staff in FindObjectsByType<PortalNightsStaffRescue>(FindObjectsSortMode.None))
            {
                if (staff != null && staff.transform.IsChildOf(planet3Root) && !planet3Staff.Contains(staff))
                {
                    planet3Staff.Add(staff);
                }
            }

            planet3Rifts.Clear();
            foreach (PortalNightsEnemyRift rift in FindObjectsByType<PortalNightsEnemyRift>(FindObjectsSortMode.None))
            {
                if (rift != null && rift.transform.IsChildOf(planet3Root) && !planet3Rifts.Contains(rift))
                {
                    planet3Rifts.Add(rift);
                }
            }
        }

        private void StartPlanet3ArrivalServer()
        {
            if (!IsServer)
            {
                return;
            }

            PreparePlanetForEntry(3);
            if (planet3Root == null || planet3RelayHealth == null)
            {
                BroadcastToastClientRpc("PLANET 3 MAP MISSING");
                return;
            }

            ClearRuntimeEnemiesServer();
            ResetPlanet3RuntimeState(true);
            TeleportPlayersToPlanet3Server();
            SetGameState(PortalNightsGameState.Planet3_FindStaff);
            BroadcastToastClientRpc("PLANET 3 - ASH RELAY STATION");
        }

        private void RetryPlanet3Server()
        {
            if (!IsServer)
            {
                return;
            }

            StartPlanet3ArrivalServer();
        }

        private void ResetPlanet3RuntimeState(bool resetStaffPositions)
        {
            planet3DefenseElapsed = 0f;
            planet3RelayUnderAttackTimer = 0f;
            planet3NextAttackIndex = 0;
            planet3CoinsGranted = false;
            planet3RelayCharge.Value = 0f;
            planet3PreparationTimer.Value = 0f;
            planet3ActiveRifts.Value = 0;
            planet3StaffRescued.Value = 0;
            planet3StaffAtSphere.Value = 0;
            CancelPlanet3Hold(false);
            SetAllPlanet3RiftsServer(PortalNightsRiftState.Dormant);

            if (planet3RelayHealth != null)
            {
                planet3RelayHealth.ServerInitialize(1800f, true);
            }

            if (resetStaffPositions)
            {
                foreach (PortalNightsStaffRescue staff in planet3Staff.ToArray())
                {
                    if (staff == null)
                    {
                        planet3Staff.Remove(staff);
                        continue;
                    }

                    Vector3 position = GetPlanet3StaffHomePosition(staff.StaffId);
                    Vector3 toCenter = PortalNightsMath.Flat(planet3Center - position);
                    Quaternion rotation = toCenter.sqrMagnitude <= 0.001f ? Quaternion.identity : Quaternion.LookRotation(toCenter.normalized, Vector3.up);
                    staff.ResetCapturedServer(position, rotation);
                }
            }
        }

        private void TeleportPlayersToPlanet3Server()
        {
            PortalNightsPlayerController[] players = FindObjectsByType<PortalNightsPlayerController>(FindObjectsSortMode.None);
            for (int i = 0; i < players.Length; i++)
            {
                PortalNightsPlayerController player = players[i];
                if (player == null)
                {
                    continue;
                }

                Vector3 basePosition = planet3ArrivalPoint == null ? planet3Center + new Vector3(0f, 1.12f, -58f) : planet3ArrivalPoint.position;
                Vector3 offset = new Vector3((i - players.Length * 0.5f) * 1.9f, 0f, 0f);
                player.transform.SetPositionAndRotation(basePosition + offset, Quaternion.LookRotation(Vector3.forward, Vector3.up));
            }

            TeleportPlayersClientRpc();
        }

        private void UpdatePlanet3Server()
        {
            if (planet3Root == null)
            {
                return;
            }

            planet3RelayUnderAttackTimer = Mathf.Max(0f, planet3RelayUnderAttackTimer - Time.deltaTime);
            RefreshPlanet3StaffProgressServer();

            if ((GameState == PortalNightsGameState.Planet3_FindStaff || GameState == PortalNightsGameState.Planet3_ReleaseStaff || GameState == PortalNightsGameState.Planet3_EscortToSphere)
                && planet3StaffAtSphere.Value >= 2)
            {
                CancelPlanet3Hold(false);
                SetGameState(PortalNightsGameState.Planet3_SphereReady);
                BroadcastToastClientRpc("RELAY SPHERE READY - HOLD E TO ACTIVATE");
            }

            if (GameState == PortalNightsGameState.Planet3_DefensePreparation)
            {
                planet3PreparationTimer.Value = Mathf.Max(0f, planet3PreparationTimer.Value - Time.deltaTime);
                if (planet3PreparationTimer.Value <= 0.01f)
                {
                    planet3DefenseElapsed = 0f;
                    planet3NextAttackIndex = 0;
                    SetGameState(PortalNightsGameState.Planet3_DefendSphere);
                    BroadcastToastClientRpc("DEFEND RELAY SPHERE");
                }
            }

            if (GameState == PortalNightsGameState.Planet3_DefendSphere)
            {
                UpdatePlanet3DefenseServer();
            }
        }

        private void RefreshPlanet3StaffProgressServer()
        {
            int rescued = 0;
            int atSphere = 0;
            foreach (PortalNightsStaffRescue staff in planet3Staff.ToArray())
            {
                if (staff == null)
                {
                    planet3Staff.Remove(staff);
                    continue;
                }

                if (staff.IsRescued)
                {
                    rescued++;
                }

                if (staff.State == PortalNightsStaffState.Following && IsInsidePlanet3SafeZone(staff.transform.position))
                {
                    staff.SetWaitingAtSphereServer();
                    BroadcastToastClientRpc(staff.StaffId + " AT RELAY SPHERE");
                }

                if (staff.IsAtSphere)
                {
                    atSphere++;
                }
            }

            planet3StaffRescued.Value = rescued;
            planet3StaffAtSphere.Value = atSphere;
        }

        private void ActivatePlanet3RelaySphereServer(ulong clientId)
        {
            if (!IsServer || planet3RelayHealth == null || planet3StaffAtSphere.Value < 2)
            {
                return;
            }

            planet3RelayHealth.ServerInitialize(1800f, true);
            planet3RelayCharge.Value = 0f;
            planet3PreparationTimer.Value = 20f;
            planet3DefenseElapsed = 0f;
            planet3NextAttackIndex = 0;
            planet3RelayUnderAttackTimer = 0f;
            if (!planet3CoinsGranted)
            {
                coins.Value += 350;
                planet3CoinsGranted = true;
            }

            SetAllPlanet3RiftsServer(PortalNightsRiftState.Charging);
            SetGameState(PortalNightsGameState.Planet3_DefensePreparation);
            BroadcastToastClientRpc("RELAY ONLINE +350 COINS - BUILD DEFENSES");
        }

        private void UpdatePlanet3DefenseServer()
        {
            planet3DefenseElapsed += Time.deltaTime;
            while (planet3NextAttackIndex < 6 && planet3DefenseElapsed >= GetPlanet3AttackTime(planet3NextAttackIndex))
            {
                SpawnPlanet3ScheduledAttackServer(planet3NextAttackIndex);
                planet3NextAttackIndex++;
            }

            float chargeRate = Time.deltaTime / 150f * 100f;
            if (HasEnemyInsidePlanet3DangerRadius())
            {
                chargeRate *= 0.5f;
            }

            if (planet3RelayUnderAttackTimer > 0.01f)
            {
                chargeRate = 0f;
            }

            planet3RelayCharge.Value = Mathf.Clamp(planet3RelayCharge.Value + chargeRate, 0f, 100f);
            if (planet3RelayHealth == null || planet3RelayHealth.IsDead)
            {
                FailPlanet3Server();
                return;
            }

            if (planet3RelayCharge.Value >= 99.99f && enemiesAlive.Value <= 0 && planet3StaffAtSphere.Value >= 2)
            {
                CompletePlanet3Server();
            }
        }

        private void SpawnPlanet3ScheduledAttackServer(int attackIndex)
        {
            switch (attackIndex)
            {
                case 0:
                    SpawnPlanet3RiftGroupServer("North", 6, 0);
                    break;
                case 1:
                    SpawnPlanet3RiftGroupServer("West", 5, 0);
                    SpawnPlanet3RiftGroupServer("East", 5, 0);
                    break;
                case 2:
                    SpawnPlanet3RiftGroupServer("North", 8, 0);
                    SpawnPlanet3RiftGroupServer("West", 4, 0);
                    SpawnPlanet3RiftGroupServer("East", 4, 0);
                    break;
                case 3:
                    SpawnPlanet3RiftGroupServer("West", 7, 1);
                    SpawnPlanet3RiftGroupServer("East", 7, 1);
                    break;
                case 4:
                    SpawnPlanet3RiftGroupServer("North", 10, 1);
                    SpawnPlanet3RiftGroupServer("West", 6, 0);
                    SpawnPlanet3RiftGroupServer("East", 6, 0);
                    break;
                case 5:
                    SpawnPlanet3RiftGroupServer("North", 6, 0);
                    SpawnPlanet3RiftGroupServer("West", 4, 0);
                    SpawnPlanet3RiftGroupServer("East", 4, 0);
                    break;
            }

            RefreshPlanet3ActiveRiftsServer();
        }

        private void SpawnPlanet3RiftGroupServer(string riftId, int count, int enhancedCount)
        {
            PortalNightsEnemyRift rift = GetPlanet3Rift(riftId);
            if (rift != null)
            {
                rift.SetStateServer(PortalNightsRiftState.Active);
            }

            PortalNightsUniverseScaling scaling = CurrentScaling;
            int scaledCount = scaling.ScaleEnemyCount(Mathf.Max(0, count));
            int scaledEnhancedCount = scaling.ScaleEnemyCount(Mathf.Max(0, enhancedCount));
            int total = scaledCount + scaledEnhancedCount;
            enemiesRemaining.Value += total;
            for (int i = 0; i < total; i++)
            {
                bool enhanced = i >= scaledCount;
                GameObject prefab = enhanced && bruteEnemyPrefab != null ? bruteEnemyPrefab : smallEnemyPrefab;
                if (prefab == null)
                {
                    continue;
                }

                Vector3 spawnPosition = rift == null ? planet3Center + Vector3.forward * 34f : rift.SpawnPoint.position;
                spawnPosition += new Vector3(Random.Range(-1.6f, 1.6f), 0f, Random.Range(-1.6f, 1.6f));
                GameObject enemyObject = Instantiate(prefab, spawnPosition, Quaternion.identity);
                PortalNightsEnemy enemy = enemyObject.GetComponent<PortalNightsEnemy>();
                NetworkObject networkObject = enemyObject.GetComponent<NetworkObject>();
                if (networkObject != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening)
                {
                    networkObject.Spawn(true);
                }

                if (enemy != null)
                {
                    enemy.ConfigureForWave(enhanced ? PortalNightsEnemyKind.Brute : PortalNightsEnemyKind.Small, Mathf.Max(6, waveNumber.Value + attackIndexOffset()), enhanced ? bruteEnemyReward : smallEnemyReward);
                    ApplyUniverseScalingToWaveEnemy(enemy, enhanced);
                    enemy.ConfigureLaneServer(riftId == "East" ? PortalNightsLane.Right : PortalNightsLane.Left, null);
                    if (enhanced)
                    {
                        enemy.ApplyEnhancedServer(1.5f, 1.2f, 1.15f);
                    }
                }
            }
        }

        private int attackIndexOffset()
        {
            return Mathf.Max(1, planet3NextAttackIndex + 1);
        }

        private void CompletePlanet3Server()
        {
            if (!IsServer || GameState == PortalNightsGameState.Planet3_Cleared)
            {
                return;
            }

            planet3RelayCharge.Value = 100f;
            SetAllPlanet3RiftsServer(PortalNightsRiftState.Closing);
            foreach (PortalNightsStaffRescue staff in planet3Staff)
            {
                staff?.SetSafeServer();
            }

            SetGameState(PortalNightsGameState.Planet3_Cleared);
            BroadcastToastClientRpc("PLANET 3 CLEARED - STAFF RESCUED - RELAY STABILIZED");
            BeginPlanetTransitionClientRpc(3, 4);
            StartPlanet4ArrivalServer();
            CompletePlanetTransitionClientRpc(4);
        }

        private void FailPlanet3Server()
        {
            if (!IsServer || GameState == PortalNightsGameState.Planet3_Failed)
            {
                return;
            }

            ClearRuntimeEnemiesServer();
            SetAllPlanet3RiftsServer(PortalNightsRiftState.Dormant);
            SetGameState(PortalNightsGameState.Planet3_Failed);
            BroadcastToastClientRpc("RELAY SPHERE DESTROYED - PRESS R TO RETRY");
        }

        private void HandlePlanet3RelaySphereDeath(PortalNightsHealth health)
        {
            if (IsServer && IsPlanet3State(GameState))
            {
                FailPlanet3Server();
            }
        }

        private PortalNightsStaffRescue GetClosestPlanet3StaffForInteraction(Vector3 position, float range)
        {
            PortalNightsStaffRescue best = null;
            float bestSqr = range * range;
            foreach (PortalNightsStaffRescue staff in planet3Staff)
            {
                if (staff == null || (!staff.NeedsRelease && !staff.NeedsRevive))
                {
                    continue;
                }

                float sqr = PortalNightsMath.Flat(staff.transform.position - position).sqrMagnitude;
                if (sqr < bestSqr)
                {
                    bestSqr = sqr;
                    best = staff;
                }
            }

            return best;
        }

        private bool IsNearRelaySphere(Vector3 position)
        {
            return planet3RelaySphere != null && PortalNightsMath.Flat(planet3RelaySphere.position - position).sqrMagnitude <= 5.8f * 5.8f;
        }

        private bool IsInsidePlanet3SafeZone(Vector3 position)
        {
            return planet3RelaySphere != null && PortalNightsMath.Flat(position - planet3RelaySphere.position).sqrMagnitude <= planet3SafeZoneRadius * planet3SafeZoneRadius;
        }

        private bool HasEnemyInsidePlanet3DangerRadius()
        {
            if (planet3RelaySphere == null)
            {
                return false;
            }

            float dangerSqr = 13.5f * 13.5f;
            foreach (PortalNightsEnemy enemy in enemies)
            {
                if (enemy != null && enemy.Health != null && !enemy.Health.IsDead && PortalNightsMath.Flat(enemy.transform.position - planet3RelaySphere.position).sqrMagnitude <= dangerSqr)
                {
                    return true;
                }
            }

            return false;
        }

        private PortalNightsEnemyRift GetPlanet3Rift(string id)
        {
            foreach (PortalNightsEnemyRift rift in planet3Rifts)
            {
                if (rift != null && rift.RiftId.Contains(id))
                {
                    return rift;
                }
            }

            return null;
        }

        private void SetAllPlanet3RiftsServer(PortalNightsRiftState state)
        {
            foreach (PortalNightsEnemyRift rift in planet3Rifts)
            {
                rift?.SetStateServer(state);
            }

            RefreshPlanet3ActiveRiftsServer();
        }

        private void RefreshPlanet3ActiveRiftsServer()
        {
            int active = 0;
            foreach (PortalNightsEnemyRift rift in planet3Rifts)
            {
                if (rift != null && (rift.State == PortalNightsRiftState.Charging || rift.State == PortalNightsRiftState.Active))
                {
                    active++;
                }
            }

            planet3ActiveRifts.Value = active;
        }

        private Vector3 GetPlanet3StaffHomePosition(string staffId)
        {
            Vector3 local = staffId != null && staffId.Contains("02") ? new Vector3(62f, 1.1f, 25f) : new Vector3(-62f, 1.1f, 25f);
            return planet3Root == null ? planet3Center + local : planet3Root.TransformPoint(local);
        }

        private static float GetPlanet3AttackTime(int index)
        {
            switch (index)
            {
                case 0: return 0f;
                case 1: return 25f;
                case 2: return 50f;
                case 3: return 80f;
                case 4: return 110f;
                case 5: return 135f;
                default: return float.PositiveInfinity;
            }
        }

        private void EnsurePlanet2Area()
        {
            if (planet2AreaCreated)
            {
                return;
            }

            planet2Radius = Mathf.Max(planet2Radius, 42f);

            CachePlanetRootReferencesOnly();
            if (arenaRoot == null)
            {
                GameObject root = new GameObject("PortalNightsArena");
                arenaRoot = root.transform;
            }

            Transform existing = planet2Root != null ? planet2Root : arenaRoot.Find("Planet2_CrystalMoon");
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
                renderer.sharedMaterial = material;
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
                portalPrompt = CreateWorldLabel("PortalPrompt", portalSpawn, new Vector3(0f, 2.8f, -1.2f), PortalNightsLocalization.Text("hud.enterPortal"), new Color(0.9f, 0.72f, 1f, 1f));
            }

            if (portalPrompt != null)
            {
                SetObjectActiveIfChanged(portalPrompt.gameObject, PortalReady);
            }

            if (planet2Sphere != null)
            {
                float pulse = GameState == PortalNightsGameState.Planet2_SphereReady || GameState == PortalNightsGameState.Planet2_DefendSphere
                    ? 1f + Mathf.Sin(Time.time * 4f) * 0.05f
                    : 1f;
                planet2Sphere.localScale = Vector3.one * pulse;
            }

            UpdateObjectiveWorldMarkers();
        }

        private void UpdateObjectiveWorldMarkers()
        {
            PortalNightsGameState state = GameState;
            SetObjectiveMarker(planet2Sphere, state == PortalNightsGameState.Planet2_SphereReady, PortalNightsLocalization.Text("marker.activate"), new Color(0.39f, 0.97f, 1f, 1f), 3.3f);

            if (IsPlanet3State(state))
            {
                SetObjectiveMarker(planet3RelaySphere, state == PortalNightsGameState.Planet3_SphereReady || state == PortalNightsGameState.Planet3_SphereActivation || state == PortalNightsGameState.Planet3_DefendSphere, PortalNightsLocalization.Text("marker.relay"), new Color(0.39f, 0.97f, 1f, 1f), 4.2f);
                foreach (PortalNightsStaffRescue staff in planet3Staff)
                {
                    bool active = staff != null && (staff.NeedsRelease || staff.NeedsRevive || staff.State == PortalNightsStaffState.Following);
                    SetObjectiveMarker(staff == null ? null : staff.transform, active, PortalNightsLocalization.Text("marker.staff"), new Color(0.52f, 1f, 0.68f, 1f), 2.6f);
                }
            }

            if (IsPlanet4State(state))
            {
                foreach (PortalNightsPlanet4HiveRift rift in planet4Rifts)
                {
                    bool active = rift != null && rift.IsClosable;
                    SetObjectiveMarker(rift == null ? null : rift.transform, active, PortalNightsLocalization.Text("marker.closeRift"), new Color(1f, 0.76f, 0.22f, 1f), 5.8f);
                }
            }

            if (IsPlanet5State(state))
            {
                SetObjectiveMarker(planet5SphereVisual == null ? null : planet5SphereVisual.transform, state == PortalNightsGameState.Planet5_DestroyHealingSphere, PortalNightsLocalization.Text("marker.corruptedSphere"), new Color(1f, 0.24f, 0.2f, 1f), 5.4f);
                bool stabilizersVisible = state == PortalNightsGameState.Planet5_RestoreSphereReady || state == PortalNightsGameState.Planet5_RestoringSphere;
                if (stabilizersVisible)
                {
                    foreach (PortalNightsPlanet5Stabilizer stabilizer in planet5Stabilizers)
                    {
                        bool active = stabilizer != null && !stabilizer.IsCompleted;
                        SetObjectiveMarker(stabilizer == null ? null : stabilizer.transform, active, PortalNightsLocalization.Text("marker.stabilizer"), new Color(0.39f, 0.97f, 1f, 1f), 2.7f);
                    }
                }
            }
        }

        private void SetObjectiveMarker(Transform target, bool active, string text, Color color, float height)
        {
            if (target == null)
            {
                return;
            }

            if (!objectiveMarkers.TryGetValue(target, out Transform marker) || marker == null)
            {
                marker = CreateWorldLabel("ObjectiveMarker_" + target.name, target, new Vector3(0f, height, 0f), text, color);
                objectiveMarkers[target] = marker;
            }

            SetObjectActiveIfChanged(marker.gameObject, active);
            if (!active)
            {
                return;
            }

            marker.localPosition = new Vector3(0f, height + Mathf.Sin(Time.time * 4.2f) * 0.12f, 0f);
            TextMesh textMesh = marker.GetComponent<TextMesh>();
            if (textMesh != null)
            {
                if (textMesh.text != text)
                {
                    textMesh.text = text;
                }

                if (textMesh.color != color)
                {
                    textMesh.color = color;
                }
            }
        }

        private void HideAllObjectiveMarkers()
        {
            foreach (Transform marker in objectiveMarkers.Values)
            {
                if (marker != null)
                {
                    SetObjectActiveIfChanged(marker.gameObject, false);
                }
            }
        }

        private static void SetObjectActiveIfChanged(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
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
            CachePlanetRootReferencesOnly();
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
            PortalNightsHud.Instance?.ShowToast(PortalNightsLocalization.LocalizeRuntimeText(message));
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
                ? $"{PortalNightsLocalization.Text("toast.waveClear")}  +{bonus} {PortalNightsLocalization.Text("hud.coins")}"
                : PortalNightsLocalization.Text("toast.waveClear");
            PortalNightsMissionComms.EnsureInstance().ShowMissionToast(message);
        }

        [ClientRpc]
        private void PortalChargingClientRpc(Vector3 position)
        {
            PortalNightsVfx.SpawnBurst(position, new Color(0.8f, 0.15f, 1f, 1f), 2.1f, 34);
            PortalNightsHud.Instance?.ShowToast(PortalNightsLocalization.Text("toast.portalCharging"));
        }

        [ClientRpc]
        private void ShowGameOverClientRpc()
        {
            PortalNightsHud.Instance?.ShowToast(PortalNightsLocalization.Text("toast.coreLost"));
        }

        [ClientRpc]
        private void StateChangedClientRpc(string stateName)
        {
            if (System.Enum.TryParse(stateName, out PortalNightsGameState parsedState))
            {
                int planetIndex = GetPlanetIndexForState(parsedState);
                if (planetIndex > 1)
                {
                    PreparePlanetForEntry(planetIndex);
                }
                else
                {
                    SetActivePlanetEnvironment(planetIndex);
                }

                PortalNightsHud.Instance?.ShowToast(PortalNightsLocalization.StateText(parsedState));
                return;
            }

            PortalNightsHud.Instance?.ShowToast(PortalNightsLocalization.LocalizeRuntimeText(stateName.Replace('_', ' ')));
        }

        [ClientRpc]
        private void RewardChoiceClientRpc(int completedWave)
        {
            PortalNightsHud.Instance?.ShowToast(PortalNightsLocalization.Format("toast.rewardChoice", completedWave));
        }

        [ClientRpc]
        private void PortalReadyClientRpc(Vector3 position)
        {
            PortalNightsVfx.SpawnBurst(position, new Color(0.86f, 0.24f, 1f, 1f), 3.2f, 54);
            PortalNightsHud.Instance?.ShowToast(PortalNightsLocalization.Text("toast.portalReady"));
        }

        [ClientRpc]
        private void SphereReadyClientRpc(Vector3 position)
        {
            PortalNightsVfx.SpawnBurst(position, new Color(0.18f, 0.9f, 1f, 1f), 2.1f, 34);
            PortalNightsHud.Instance?.ShowToast(PortalNightsLocalization.Text("toast.activateSphere"));
        }

        [ClientRpc]
        private void SetPlanet4RiftVisualClientRpc(int riftIndex, int riftState)
        {
            if (!IsPlanetEnvironmentActive(4))
            {
                return;
            }

            EnsurePlanet4Area();
            if (riftIndex < 0 || riftIndex >= planet4Rifts.Count || planet4Rifts[riftIndex] == null)
            {
                return;
            }

            PortalNightsRiftState state = (PortalNightsRiftState)riftState;
            planet4Rifts[riftIndex].SetState(state);
            if (state == PortalNightsRiftState.Closed)
            {
                PortalNightsVfx.SpawnBurst(planet4Rifts[riftIndex].transform.position + Vector3.up * 2f, new Color(0.48f, 1f, 0.25f, 1f), 2.6f, 34);
            }
        }

        [ClientRpc]
        private void SetPlanet4ExitPortalVisualClientRpc(bool active)
        {
            if (!IsPlanetEnvironmentActive(4))
            {
                return;
            }

            EnsurePlanet4Area();
            if (planet4ExitPortal == null)
            {
                return;
            }

            Color color = active ? new Color(0.48f, 1f, 0.25f, 1f) : new Color(0.18f, 0.08f, 0.34f, 1f);
            foreach (Renderer renderer in planet4ExitPortal.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null)
                {
                    continue;
                }

                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                block.SetColor("_Color", color);
                block.SetColor("_BaseColor", color);
                block.SetColor("_EmissionColor", color * (active ? 4f : 0.7f));
                renderer.SetPropertyBlock(block);
            }

            foreach (Light light in planet4ExitPortal.GetComponentsInChildren<Light>(true))
            {
                if (light == null)
                {
                    continue;
                }

                light.color = color;
                light.intensity = active ? 7f : 0.5f;
            }

            if (active)
            {
                PortalNightsVfx.SpawnBurst(planet4ExitPortal.position + Vector3.up * 2f, color, 3.4f, 52);
            }
        }

        [ClientRpc]
        private void SetPlanet5SphereVisualClientRpc(int sphereState)
        {
            if (!IsPlanetEnvironmentActive(5))
            {
                return;
            }

            EnsurePlanet5Area();
            PortalNightsSphereVisualState state = (PortalNightsSphereVisualState)sphereState;
            if (planet5SphereVisual != null)
            {
                planet5SphereVisual.SetVisualState(state);
                Color color = state == PortalNightsSphereVisualState.Restored
                    ? new Color(0.39f, 0.97f, 1f, 1f)
                    : state == PortalNightsSphereVisualState.DamagedCore
                    ? new Color(1f, 0.34f, 0.18f, 1f)
                    : new Color(0.62f, 0.08f, 1f, 1f);
                foreach (Light light in planet5SphereVisual.GetComponentsInChildren<Light>(true))
                {
                    if (light == null)
                    {
                        continue;
                    }

                    light.color = color;
                    light.intensity = state == PortalNightsSphereVisualState.Restored ? 9f : 6f;
                }

                PortalNightsVfx.SpawnBurst(planet5SphereVisual.transform.position + Vector3.up * 2.4f, color, 2.4f, 34);
            }
        }

        [ClientRpc]
        private void Planet5HealingPulseClientRpc(Vector3 position)
        {
            PortalNightsVfx.SpawnBurst(position, new Color(1f, 0.1f, 0.35f, 1f), 2.4f, 34);
            PortalNightsVfx.SpawnFloatingText(position + Vector3.up * 1.4f, PortalNightsLocalization.Text("toast.bossHeal"), new Color(1f, 0.42f, 0.3f, 1f));
        }

        [ClientRpc]
        private void SetPlanet5StabilizerVisualClientRpc(int stabilizerIndex, bool available, bool completed)
        {
            if (!IsPlanetEnvironmentActive(5))
            {
                return;
            }

            EnsurePlanet5Area();
            if (stabilizerIndex < 0 || stabilizerIndex >= planet5Stabilizers.Count || planet5Stabilizers[stabilizerIndex] == null)
            {
                return;
            }

            planet5Stabilizers[stabilizerIndex].SetState(available, completed);
        }

        [ClientRpc]
        private void Planet5StabilizerCompletedClientRpc(Vector3 position, string stabilizerId, int completed, int total)
        {
            Color color = new Color(0.39f, 0.97f, 1f, 1f);
            PortalNightsVfx.SpawnBurst(position, color, 1.9f, 28);
            PortalNightsVfx.SpawnFloatingText(position + Vector3.up * 1.8f, PortalNightsLocalization.Format("toast.online", stabilizerId.ToUpperInvariant(), completed, total), color);
        }

        [ClientRpc]
        private void Planet5RestoredPulseClientRpc(Vector3 position)
        {
            Color color = new Color(0.39f, 0.97f, 1f, 1f);
            PortalNightsVfx.SpawnBurst(position, color, 4.2f, 70);
            PortalNightsVfx.SpawnFloatingText(position + Vector3.up * 2f, PortalNightsLocalization.Text("toast.sphereRestored"), color);
        }

        [ClientRpc]
        private void SetPlanet5UniversePortalVisualClientRpc(bool active)
        {
            if (!IsPlanetEnvironmentActive(5))
            {
                return;
            }

            EnsurePlanet5Area();
            if (active)
            {
                planet5UniversePortal = CreatePlanet5UniversePortalVisualLocal();
                return;
            }

            if (planet5UniversePortal != null)
            {
                planet5UniversePortal.gameObject.SetActive(false);
            }
        }

        [ClientRpc]
        private void BeginPlanetTransitionClientRpc(int fromPlanet, int toPlanet)
        {
            PortalNightsPlanetTransitionDirector.EnsureInstance().BeginPlanetTransition(fromPlanet, toPlanet);
        }

        [ClientRpc]
        private void CompletePlanetTransitionClientRpc(int toPlanet)
        {
            PortalNightsPlanetTransitionDirector.EnsureInstance().CompletePlanetTransition(toPlanet);
        }

        [ClientRpc]
        private void MissionDialogueClientRpc(string dialogueId, string objectiveMain, string objectiveProgress, int objectiveSeverity)
        {
            PortalNightsPlanetTransitionDirector director = PortalNightsPlanetTransitionDirector.Instance;
            if (director != null && director.IsTransitionActive)
            {
                return;
            }

            PortalNightsMissionComms comms = PortalNightsMissionComms.EnsureInstance();
            if (!string.IsNullOrWhiteSpace(dialogueId))
            {
                comms.PlayDialogueById(dialogueId);
            }

            if (!string.IsNullOrWhiteSpace(objectiveMain))
            {
                comms.SetObjective(objectiveMain, objectiveProgress ?? string.Empty, (PortalNightsObjectiveSeverity)Mathf.Clamp(objectiveSeverity, 0, 2));
            }
        }

        [ClientRpc]
        private void MissionObjectiveClientRpc(string objectiveMain, string objectiveProgress, int objectiveSeverity)
        {
            PortalNightsPlanetTransitionDirector director = PortalNightsPlanetTransitionDirector.Instance;
            if (director != null && director.IsTransitionActive)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(objectiveMain))
            {
                return;
            }

            PortalNightsMissionComms.EnsureInstance().SetObjective(objectiveMain, objectiveProgress ?? string.Empty, (PortalNightsObjectiveSeverity)Mathf.Clamp(objectiveSeverity, 0, 2));
        }

        [ClientRpc]
        private void MissionToastClientRpc(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            PortalNightsMissionComms.EnsureInstance().ShowMissionToast(text);
        }

        [ClientRpc]
        private void UniverseCompleteSummaryClientRpc(string leaderboardText)
        {
            universeCompleteLeaderboardText = leaderboardText ?? string.Empty;
            PortalNightsMissionComms.EnsureInstance().ShowMissionToast(PortalNightsLocalization.Text("toast.universeComplete"));
        }

        [ClientRpc]
        private void UniverseEnteredClientRpc(int universeIndex, float enemyHpMultiplier, float enemyDamageMultiplier, int scoreMultiplier)
        {
            universeCompleteLeaderboardText = string.Empty;
            PortalNightsHud.Instance?.ShowToast(PortalNightsLocalization.Format("toast.universeEntered", universeIndex));
            PortalNightsHud.Instance?.ShowRunStatus(universeIndex, Instance == null ? 0 : Instance.CurrentScore);
            PortalNightsVfx.SpawnFloatingText(new Vector3(0f, 4.5f, -8f), PortalNightsLocalization.Format("toast.multiplier", enemyHpMultiplier.ToString("0.00"), enemyDamageMultiplier.ToString("0.00"), scoreMultiplier), new Color(0.39f, 0.97f, 1f, 1f));
        }

        [ClientRpc]
        private void TeleportPlayersClientRpc()
        {
            PortalNightsGameState state = Instance == null ? GameState : Instance.GameState;
            string message = IsPlanet5State(state)
                ? PortalNightsLocalization.Text("hud.planet5")
                : IsPlanet4State(state)
                ? PortalNightsLocalization.Text("hud.planet4")
                : IsPlanet3State(state)
                ? PortalNightsLocalization.Text("hud.planet3")
                : PortalNightsLocalization.Text("hud.planet2");
            PortalNightsHud.Instance?.ShowToast(message);
        }

        [ClientRpc]
        private void BroadcastToastClientRpc(string message)
        {
            PortalNightsHud.Instance?.ShowToast(PortalNightsLocalization.LocalizeRuntimeText(message));
        }

        [ClientRpc]
        private void PickupCollectedClientRpc(Vector3 position, int pickupKind)
        {
            PortalNightsPickupKind kind = (PortalNightsPickupKind)pickupKind;
            PortalNightsVfx.SpawnBurst(position, GetPickupColor(kind), 0.85f, 16);
        }
    }
}
