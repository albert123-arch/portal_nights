using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace PortalNights
{
    public sealed class PortalNightsHud : MonoBehaviour
    {
        private const float RefreshInterval = 0.1f;

        [SerializeField] private Text waveText;
        [SerializeField] private Text enemiesText;
        [SerializeField] private Text laneText;
        [SerializeField] private Text coinsText;
        [SerializeField] private Text coreText;
        [SerializeField] private Text playerText;
        [SerializeField] private Text promptText;
        [SerializeField] private Text toastText;
        [SerializeField] private Slider coreSlider;
        [SerializeField] private Slider playerSlider;

        private PortalNightsGameController controller;
        private float toastTimer;
        private float refreshTimer;
        private bool universeCompleteLeaderboardVisible = true;

        private static int textWritesSinceLastSample;
        private static int totalTextWrites;

        public static PortalNightsHud Instance { get; private set; }
        public static int TotalTextWrites => totalTextWrites;

        private void Awake()
        {
            Instance = this;
            EnsureInputSystemUiModule();
            PortalNightsMissionComms.EnsureInstance();
        }

        private void Start()
        {
            EnsureInputSystemUiModule();
            PortalNightsMissionComms.EnsureInstance();
            if (controller == null)
            {
                Bind(PortalNightsGameController.Instance);
            }
        }

        private void Update()
        {
            UpdateLeaderboardToggle();
            refreshTimer -= Time.unscaledDeltaTime;
            if (refreshTimer <= 0f || controller == null)
            {
                refreshTimer = RefreshInterval;
                Refresh();
            }

            UpdateToast();
        }

        private void OnGUI()
        {
            DrawCrosshair();
        }

        public void Bind(PortalNightsGameController gameController)
        {
            controller = gameController;
            refreshTimer = 0f;
            Refresh();
        }

        public void ShowToast(string message)
        {
            if (toastText == null)
            {
                return;
            }

            SetTextIfChanged(toastText, PortalNightsLocalization.LocalizeRuntimeText(message));
            toastTimer = 2.8f;
        }

        public void ShowRunStatus(int universeIndex, int score)
        {
            SetTextIfChanged(laneText, $"{PortalNightsLocalization.Text("hud.universe")} {Mathf.Max(1, universeIndex)}   {PortalNightsLocalization.Text("hud.score")}: {Mathf.Max(0, score)}");
        }

        public void ShowUniverseCompleteSummary(PortalNightsRunState runState, IReadOnlyList<PortalNightsLeaderboardEntry> topEntries)
        {
            if (runState == null)
            {
                return;
            }

            runState.UpdateTotalRunTime();
            SetTextIfChanged(waveText, $"{PortalNightsLocalization.Text("hud.universe")} {Mathf.Max(1, runState.universeIndex)} {PortalNightsLocalization.Text("hud.universeComplete")}");
            SetTextIfChanged(enemiesText, $"{PortalNightsLocalization.Text("hud.score")}: {PortalNightsScoreCalculator.CalculateSummaryScore(runState)}   {PortalNightsLocalization.Text("hud.time")}: {FormatTime(runState.totalRunTime)}");
            SetTextIfChanged(laneText, $"{PortalNightsLocalization.Text("hud.enemies")}: {runState.enemiesKilled + runState.enhancedEnemiesKilled}   {PortalNightsLocalization.Text("hud.bosses")}: {runState.bossesKilled}   {PortalNightsLocalization.Text("hud.staff")}: {runState.staffSaved}   {PortalNightsLocalization.Text("hud.spheres")}: {runState.spheresRestored}");
            ShowLocalLeaderboard(topEntries);
        }

        public void ShowUniverseCompleteSummaryText(PortalNightsRunState runState, string leaderboardText)
        {
            if (runState == null)
            {
                return;
            }

            runState.UpdateTotalRunTime();
            SetTextIfChanged(waveText, PortalNightsLocalization.Text("hud.universeComplete"));
            SetTextIfChanged(enemiesText, $"{PortalNightsLocalization.Text("hud.finalScore")}: {PortalNightsScoreCalculator.CalculateSummaryScore(runState)}   {PortalNightsLocalization.Text("hud.universe")}: {Mathf.Max(1, runState.universeIndex)}   {PortalNightsLocalization.Text("hud.time")}: {FormatTime(runState.totalRunTime)}");
            SetTextIfChanged(laneText, $"{PortalNightsLocalization.Text("hud.enemies")}: {runState.enemiesKilled + runState.enhancedEnemiesKilled}   {PortalNightsLocalization.Text("hud.bosses")}: {runState.bossesKilled}   {PortalNightsLocalization.Text("hud.staff")}: {runState.staffSaved}   {PortalNightsLocalization.Text("hud.spheres")}: {runState.spheresRestored}   {PortalNightsLocalization.Text("hud.deaths")}: {runState.playerDeaths}   {PortalNightsLocalization.Text("hud.turrets")}: {runState.turretsBuilt}/{runState.turretsUpgraded}");
            SetTextIfChanged(promptText, universeCompleteLeaderboardVisible && !string.IsNullOrWhiteSpace(leaderboardText)
                ? PortalNightsLocalization.LocalizeRuntimeText(leaderboardText)
                : BuildUniverseCompleteControlsText(runState));
        }

        public void ShowLocalLeaderboard(IReadOnlyList<PortalNightsLeaderboardEntry> topEntries)
        {
            if (promptText == null || topEntries == null)
            {
                return;
            }

            StringBuilder builder = new StringBuilder(PortalNightsLocalization.Text("hud.localLeaderboard"));
            int count = Mathf.Min(5, topEntries.Count);
            for (int i = 0; i < count; i++)
            {
                PortalNightsLeaderboardEntry entry = topEntries[i];
                if (entry == null)
                {
                    continue;
                }

                builder.AppendLine();
                builder.Append(i + 1);
                builder.Append(". ");
                builder.Append(string.IsNullOrWhiteSpace(entry.playerName) ? PortalNightsLocalization.Text("hud.defaultPlayerName") : entry.playerName);
                builder.Append("  ");
                builder.Append(entry.score);
                builder.Append("  ВС");
                builder.Append(entry.universe);
                builder.Append("  ");
                builder.Append(FormatTime(entry.totalTime));
                builder.Append("  В");
                builder.Append(entry.enemiesKilled);
                builder.Append("  Б");
                builder.Append(entry.bossesKilled);
            }

            SetTextIfChanged(promptText, builder.ToString());
        }

        public static int ConsumeTextWriteCount()
        {
            int count = textWritesSinceLastSample;
            textWritesSinceLastSample = 0;
            return count;
        }

        private void UpdateLeaderboardToggle()
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return;
            }

            if (controller != null && controller.GameState == PortalNightsGameState.Planet5_UniverseComplete)
            {
                if (Keyboard.current.lKey.wasPressedThisFrame)
                {
                    universeCompleteLeaderboardVisible = !universeCompleteLeaderboardVisible;
                    refreshTimer = 0f;
                }
            }
            else
            {
                universeCompleteLeaderboardVisible = true;
            }
#endif
        }

        private static string BuildUniverseCompleteControlsText(PortalNightsRunState runState)
        {
            if (runState == null)
            {
                return PortalNightsLocalization.Format("hud.enterNextUniverse", 2) + "   " + PortalNightsLocalization.Text("hud.showHideLeaderboard");
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine(PortalNightsLocalization.Text("hud.runSummary"));
            builder.Append(PortalNightsLocalization.Text("hud.score"));
            builder.Append(": ");
            builder.Append(PortalNightsScoreCalculator.CalculateSummaryScore(runState));
            builder.Append("   ");
            builder.Append(PortalNightsLocalization.Text("hud.time"));
            builder.Append(": ");
            builder.AppendLine(FormatTime(runState.totalRunTime));
            builder.Append(PortalNightsLocalization.Text("hud.enemiesKilled"));
            builder.Append(": ");
            builder.Append(runState.enemiesKilled + runState.enhancedEnemiesKilled);
            builder.Append("   ");
            builder.Append(PortalNightsLocalization.Text("hud.bossesDefeated"));
            builder.Append(": ");
            builder.AppendLine(runState.bossesKilled.ToString());
            builder.Append(PortalNightsLocalization.Text("hud.staffSaved"));
            builder.Append(": ");
            builder.Append(runState.staffSaved);
            builder.Append("   ");
            builder.Append(PortalNightsLocalization.Text("hud.spheresRestored"));
            builder.Append(": ");
            builder.Append(runState.spheresRestored);
            builder.Append("   ");
            builder.Append(PortalNightsLocalization.Text("hud.deaths"));
            builder.Append(": ");
            builder.AppendLine(runState.playerDeaths.ToString());
            builder.Append(PortalNightsLocalization.Text("hud.turretsBuiltUpgraded"));
            builder.Append(": ");
            builder.Append(runState.turretsBuilt);
            builder.Append("/");
            builder.AppendLine(runState.turretsUpgraded.ToString());
            builder.Append(PortalNightsLocalization.Format("hud.enterNextUniverse", Mathf.Max(2, runState.universeIndex + 1)));
            builder.Append("   ");
            builder.Append(PortalNightsLocalization.Text("hud.showHideLeaderboard"));
            return builder.ToString();
        }

        private void Refresh()
        {
            if (controller == null)
            {
                controller = PortalNightsGameController.Instance;
                if (controller == null)
                {
                    return;
                }
            }

            PortalNightsPlayerController player = GetLocalPlayer();
            bool planet3 = IsPlanet3State(controller.GameState);
            bool planet4 = IsPlanet4State(controller.GameState);
            bool planet5 = IsPlanet5State(controller.GameState);
            if (controller.GameState == PortalNightsGameState.Planet5_UniverseComplete)
            {
                ShowUniverseCompleteSummaryText(controller.RunState, controller.UniverseCompleteLeaderboardText);
                return;
            }

            PortalNightsHealth coreHealth = planet5 ? controller.Planet5SphereHealth : planet3 ? controller.RelaySphereHealth : controller.CoreHealth;
            PortalNightsHealth playerHealth = player == null ? null : player.Health;

            SetTextIfChanged(waveText, BuildWaveText());
            SetTextIfChanged(enemiesText, BuildEnemiesText(planet3, planet4, planet5));
            SetTextIfChanged(laneText, BuildLaneText(planet3, planet4, planet5));
            SetTextIfChanged(coinsText, $"{PortalNightsLocalization.Text("hud.coins")}: {controller.Coins}   ВС{controller.UniverseIndex}");
            UpdateHealth(coreHealth, playerHealth, planet3, planet5);
            SetTextIfChanged(promptText, BuildPrompt(player));
        }

        private string BuildWaveText()
        {
            if (controller.GameOver)
            {
                return PortalNightsLocalization.Text("hud.gameOver");
            }

            switch (controller.GameState)
            {
                case PortalNightsGameState.Planet1_RewardChoice:
                    return PortalNightsLocalization.Text("hud.chooseReward");
                case PortalNightsGameState.Planet1_PortalReady:
                    return PortalNightsLocalization.Text("hud.portalReady");
                case PortalNightsGameState.Planet2_ClearArea:
                    return PortalNightsLocalization.Text("hud.planet2");
                case PortalNightsGameState.Planet2_SphereReady:
                    return PortalNightsLocalization.Text("hud.sphereReady");
                case PortalNightsGameState.Planet2_DefendSphere:
                    return PortalNightsLocalization.Text("hud.defendSphere");
                case PortalNightsGameState.Planet2_Cleared:
                    return PortalNightsLocalization.Text("hud.planetCleared");
                case PortalNightsGameState.Planet3_Arrival:
                case PortalNightsGameState.Planet3_FindStaff:
                case PortalNightsGameState.Planet3_ReleaseStaff:
                case PortalNightsGameState.Planet3_EscortToSphere:
                    return PortalNightsLocalization.Text("hud.planet3");
                case PortalNightsGameState.Planet3_SphereReady:
                case PortalNightsGameState.Planet3_SphereActivation:
                    return PortalNightsLocalization.Text("hud.relaySphereReady");
                case PortalNightsGameState.Planet3_DefensePreparation:
                    return PortalNightsLocalization.Format("hud.prepareDefense", Mathf.CeilToInt(controller.Planet3PreparationTimer));
                case PortalNightsGameState.Planet3_DefendSphere:
                    return PortalNightsLocalization.Format("hud.relayCharge", Mathf.CeilToInt(controller.Planet3RelayCharge));
                case PortalNightsGameState.Planet3_Cleared:
                    return PortalNightsLocalization.Text("hud.planet3Cleared");
                case PortalNightsGameState.Planet3_Failed:
                    return PortalNightsLocalization.Text("hud.relayDestroyed");
                case PortalNightsGameState.Planet4_Arrival:
                case PortalNightsGameState.Planet4_HordeActive:
                case PortalNightsGameState.Planet4_RiftClosing:
                    return PortalNightsLocalization.Text("hud.planet4");
                case PortalNightsGameState.Planet4_ExitPortalReady:
                    return PortalNightsLocalization.Text("hud.planet4Portal");
                case PortalNightsGameState.Planet4_Cleared:
                    return PortalNightsLocalization.Text("hud.planet4Cleared");
                case PortalNightsGameState.Planet4_Failed:
                    return PortalNightsLocalization.Text("hud.swarmOverrun");
                case PortalNightsGameState.Planet5_Arrival:
                case PortalNightsGameState.Planet5_BossIntro:
                    return PortalNightsLocalization.Text("hud.planet5");
                case PortalNightsGameState.Planet5_DestroyHealingSphere:
                    return PortalNightsLocalization.Text("hud.destroyHealingSphere");
                case PortalNightsGameState.Planet5_KillBosses:
                    return PortalNightsLocalization.Text("hud.killBosses");
                case PortalNightsGameState.Planet5_RestoreSphereReady:
                    return PortalNightsLocalization.Text("hud.restoreSphere");
                case PortalNightsGameState.Planet5_RestoringSphere:
                    return PortalNightsLocalization.Text("hud.restoringSphere");
                case PortalNightsGameState.Planet5_SphereRestored:
                    return PortalNightsLocalization.Text("hud.sphereRestored");
                case PortalNightsGameState.Planet5_Failed:
                    return PortalNightsLocalization.Text("hud.crimsonLost");
                case PortalNightsGameState.Failed:
                    return PortalNightsLocalization.Text("hud.sphereLost");
            }

            if (!controller.WaveRunning && controller.NextWaveTimer > 0.05f)
            {
                return $"{PortalNightsLocalization.Text("hud.wait")} {Mathf.CeilToInt(controller.NextWaveTimer)}";
            }

            return $"{PortalNightsLocalization.Text("hud.wave")} {controller.WaveNumber}";
        }

        private string BuildEnemiesText(bool planet3, bool planet4, bool planet5)
        {
            if (planet3)
            {
                return $"{PortalNightsLocalization.Text("hud.objective")}: {PortalNightsLocalization.Text("hud.objectiveRelay")}   {PortalNightsLocalization.Text("hud.enemies")}: {controller.EnemiesRemaining}";
            }

            if (planet4)
            {
                return $"{PortalNightsLocalization.Text("hud.enemiesDefeated")}: {controller.Planet4EnemiesDefeated}/{controller.Planet4TargetKills}";
            }

            if (planet5)
            {
                if (controller.GameState == PortalNightsGameState.Planet5_DestroyHealingSphere)
                {
                    return $"{PortalNightsLocalization.Text("hud.sphereHp")}: {FormatHealth(controller.Planet5SphereHealth)}   {PortalNightsLocalization.Text("hud.bossesCannotDie")}";
                }

                if (controller.GameState == PortalNightsGameState.Planet5_RestoreSphereReady || controller.GameState == PortalNightsGameState.Planet5_RestoringSphere)
                {
                    return $"{PortalNightsLocalization.Text("hud.stabilizers")}: {controller.Planet5StabilizersCompleted}/{controller.Planet5StabilizersTotal}";
                }

                if (controller.GameState == PortalNightsGameState.Planet5_SphereRestored)
                {
                    return $"{PortalNightsLocalization.Text("hud.sphereRestored")}   {PortalNightsLocalization.Text("hud.universeStabilized")}";
                }

                return $"{PortalNightsLocalization.Text("hud.bossesDefeated")}: {controller.Planet5BossesDefeated}/2";
            }

            if (controller.GameState == PortalNightsGameState.Planet2_ClearArea)
            {
                return $"{PortalNightsLocalization.Text("hud.objective")}: {PortalNightsLocalization.Text("hud.objectiveClearArea")}   {PortalNightsLocalization.Text("hud.enemies")}: {controller.EnemiesRemaining}";
            }

            return $"{PortalNightsLocalization.Text("hud.enemies")}: {controller.EnemiesRemaining}";
        }

        private string BuildLaneText(bool planet3, bool planet4, bool planet5)
        {
            if (controller.GameState == PortalNightsGameState.Planet1_Defense || controller.GameState == PortalNightsGameState.Planet1_RewardChoice || controller.GameState == PortalNightsGameState.Planet1_PortalReady)
            {
                string portal = controller.PortalReady ? PortalNightsLocalization.Text("hud.portalReady") : PortalNightsLocalization.Text("hud.portalLocked");
                return $"ВС{controller.UniverseIndex} П{controller.CurrentPlanetIndex}   {PortalNightsLocalization.Text("hud.score")}: {controller.CurrentScore}   {PortalNightsLocalization.Text("hud.turrets")}: {controller.RequiredTurretsBuilt}/{controller.RequiredTurretsTotal}   {PortalNightsLocalization.Text("hud.upgrades")}: {controller.RequiredTurretsMaxed}/{controller.RequiredTurretsTotal}   {portal}";
            }

            if (planet3)
            {
                return $"{PortalNightsLocalization.Text("hud.staffRescued")}: {controller.Planet3StaffRescued}/2   {PortalNightsLocalization.Text("hud.staffAtSphere")}: {controller.Planet3StaffAtSphere}/2   {PortalNightsLocalization.Text("hud.charge")}: {Mathf.CeilToInt(controller.Planet3RelayCharge)}%   {PortalNightsLocalization.Text("hud.rifts")}: {controller.Planet3ActiveRifts}";
            }

            if (planet4)
            {
                return $"{PortalNightsLocalization.Text("hud.riftsClosed")}: {controller.Planet4RiftsClosed}/{controller.Planet4RiftsTotal}   {PortalNightsLocalization.Text("hud.activeRifts")}: {controller.Planet4ActiveRifts}   {PortalNightsLocalization.Text("hud.score")}: {controller.CurrentScore}";
            }

            if (planet5)
            {
                if (controller.GameState == PortalNightsGameState.Planet5_RestoreSphereReady || controller.GameState == PortalNightsGameState.Planet5_RestoringSphere)
                {
                    return $"{PortalNightsLocalization.Text("hud.stabilizers")}: {controller.Planet5StabilizersCompleted}/{controller.Planet5StabilizersTotal}   {PortalNightsLocalization.Text("hud.hold")}: {Mathf.CeilToInt(controller.Planet5StabilizerHoldProgress * 100f)}%";
                }

                return $"{PortalNightsLocalization.Text("hud.solar")}: {FormatHealth(controller.Planet5BossAHealth)}   {PortalNightsLocalization.Text("hud.behemoth")}: {FormatHealth(controller.Planet5BossBHealth)}   {PortalNightsLocalization.Text("hud.allies")}: {FormatHealth(controller.Planet5Helper1Health)} / {FormatHealth(controller.Planet5Helper2Health)}";
            }

            if (controller.GameState == PortalNightsGameState.Planet2_DefendSphere)
            {
                return $"{PortalNightsLocalization.Text("hud.objective")}: {PortalNightsLocalization.Text("hud.defendSphere")}";
            }

            return PortalNightsLocalization.StateText(controller.GameState);
        }

        private void UpdateHealth(PortalNightsHealth coreHealth, PortalNightsHealth playerHealth, bool planet3, bool planet5)
        {
            if (coreHealth != null)
            {
                string healthLabel = planet5 ? PortalNightsLocalization.Text("hud.healingSphere") : planet3 ? PortalNightsLocalization.Text("hud.objectiveRelay") : PortalNightsLocalization.Text("hud.core");
                SetTextIfChanged(coreText, $"{healthLabel} {Mathf.CeilToInt(coreHealth.CurrentHealth)}/{Mathf.CeilToInt(coreHealth.MaxHealth)}");
                if (coreSlider != null)
                {
                    coreSlider.value = coreHealth.Normalized;
                }
            }

            if (playerHealth != null)
            {
                SetTextIfChanged(playerText, $"{PortalNightsLocalization.Text("hud.player")} {Mathf.CeilToInt(playerHealth.CurrentHealth)}/{Mathf.CeilToInt(playerHealth.MaxHealth)}");
                if (playerSlider != null)
                {
                    playerSlider.value = playerHealth.Normalized;
                }
            }
        }

        private string BuildPrompt(PortalNightsPlayerController player)
        {
            if (player == null || controller == null || controller.GameOver)
            {
                return string.Empty;
            }

            if (controller.GameState == PortalNightsGameState.Planet1_RewardChoice)
            {
                return PortalNightsLocalization.Text("hud.rewardOptions");
            }

            if (controller.GameState == PortalNightsGameState.Planet1_PortalReady && controller.IsPlayerNearPortal(player.transform.position))
            {
                return PortalNightsLocalization.Text("hud.enterPortal");
            }

            if (controller.GameState == PortalNightsGameState.Planet2_SphereReady && controller.IsPlayerNearSphere(player.transform.position))
            {
                return PortalNightsLocalization.Text("hud.activateSphere");
            }

            if (controller.GameState == PortalNightsGameState.Failed)
            {
                return PortalNightsLocalization.Text("hud.sphereLost");
            }

            if (controller.GameState == PortalNightsGameState.Planet3_Failed)
            {
                return PortalNightsLocalization.Text("hud.relayDestroyed");
            }

            if (controller.GameState == PortalNightsGameState.Planet5_RestoreSphereReady)
            {
                return PortalNightsLocalization.Text("hud.sphereReadyRestore");
            }

            string planet4Prompt = controller.GetPlanet4InteractionPrompt(player.transform.position);
            if (!string.IsNullOrEmpty(planet4Prompt))
            {
                return planet4Prompt;
            }

            string planet5Prompt = controller.GetPlanet5InteractionPrompt(player.transform.position);
            if (!string.IsNullOrEmpty(planet5Prompt))
            {
                return planet5Prompt;
            }

            string planet3Prompt = controller.GetPlanet3InteractionPrompt(player.transform.position);
            if (!string.IsNullOrEmpty(planet3Prompt))
            {
                return planet3Prompt;
            }

            PortalNightsBuildPoint point = controller.GetClosestBuildPoint(player.transform.position, controller.BuildInteractionRange, false);
            if (point == null || !controller.CanBuildInCurrentState)
            {
                return PortalNightsLocalization.Text("hud.controls");
            }

            return point.GetInteractionPrompt();
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

        private static string FormatHealth(PortalNightsHealth health)
        {
            if (health == null)
            {
                return "--";
            }

            return Mathf.CeilToInt(health.CurrentHealth) + "/" + Mathf.CeilToInt(health.MaxHealth);
        }

        private static void DrawCrosshair()
        {
            float x = Screen.width * 0.5f;
            float y = Screen.height * 0.5f;
            Color previous = GUI.color;
            GUI.color = new Color(0.55f, 0.95f, 1f, 0.92f);
            GUI.DrawTexture(new Rect(x - 18f, y - 1f, 13f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x + 5f, y - 1f, 13f, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - 1f, y - 18f, 2f, 13f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - 1f, y + 5f, 2f, 13f), Texture2D.whiteTexture);
            GUI.color = new Color(1f, 0.64f, 0.18f, 0.95f);
            GUI.DrawTexture(new Rect(x - 2f, y - 2f, 4f, 4f), Texture2D.whiteTexture);
            GUI.color = previous;
        }

        private PortalNightsPlayerController GetLocalPlayer()
        {
            NetworkManager manager = NetworkManager.Singleton;
            if (manager != null && manager.IsListening && manager.SpawnManager != null)
            {
                NetworkObject playerObject = manager.SpawnManager.GetLocalPlayerObject();
                if (playerObject != null)
                {
                    return playerObject.GetComponent<PortalNightsPlayerController>();
                }
            }

            return FindFirstObjectByType<PortalNightsPlayerController>();
        }

        private void UpdateToast()
        {
            if (toastText == null || toastTimer <= 0f)
            {
                return;
            }

            toastTimer -= Time.unscaledDeltaTime;
            if (toastTimer <= 0f)
            {
                SetTextIfChanged(toastText, string.Empty);
            }
        }

        private static void SetTextIfChanged(Text target, string value)
        {
            if (target == null)
            {
                return;
            }

            value ??= string.Empty;
            if (target.text == value)
            {
                return;
            }

            target.text = value;
            textWritesSinceLastSample++;
            totalTextWrites++;
        }

        private static void EnsureInputSystemUiModule()
        {
#if ENABLE_INPUT_SYSTEM
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
            {
                return;
            }

            StandaloneInputModule oldModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (oldModule != null)
            {
                oldModule.enabled = false;
                Destroy(oldModule);
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
#endif
        }

        private static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            int minutes = totalSeconds / 60;
            int remainder = totalSeconds % 60;
            return $"{minutes:00}:{remainder:00}";
        }
    }
}
