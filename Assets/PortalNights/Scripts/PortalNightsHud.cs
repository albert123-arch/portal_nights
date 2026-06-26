using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace PortalNights
{
    public sealed class PortalNightsHud : MonoBehaviour
    {
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
        private GUIStyle controlsStyle;
        private bool universeCompleteLeaderboardVisible = true;

        public static PortalNightsHud Instance { get; private set; }

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
            Refresh();
            UpdateToast();
        }

        private void OnGUI()
        {
            DrawCrosshair();
            DrawControlsHint();
        }

        public void Bind(PortalNightsGameController gameController)
        {
            controller = gameController;
            Refresh();
        }

        public void ShowToast(string message)
        {
            if (toastText == null)
            {
                return;
            }

            toastText.text = message;
            toastTimer = 2.8f;
        }

        public void ShowRunStatus(int universeIndex, int score)
        {
            if (laneText != null)
            {
                laneText.text = $"UNIVERSE {Mathf.Max(1, universeIndex)}   SCORE: {Mathf.Max(0, score)}";
            }
        }

        public void ShowUniverseCompleteSummary(PortalNightsRunState runState, IReadOnlyList<PortalNightsLeaderboardEntry> topEntries)
        {
            if (runState == null)
            {
                return;
            }

            runState.UpdateTotalRunTime();
            if (waveText != null)
            {
                waveText.text = $"UNIVERSE {Mathf.Max(1, runState.universeIndex)} COMPLETE";
            }

            if (enemiesText != null)
            {
                enemiesText.text = $"SCORE: {PortalNightsScoreCalculator.CalculateSummaryScore(runState)}   TIME: {FormatTime(runState.totalRunTime)}";
            }

            if (laneText != null)
            {
                laneText.text = $"ENEMIES: {runState.enemiesKilled + runState.enhancedEnemiesKilled}   BOSSES: {runState.bossesKilled}   STAFF: {runState.staffSaved}   SPHERES: {runState.spheresRestored}";
            }

            ShowLocalLeaderboard(topEntries);
        }

        public void ShowUniverseCompleteSummaryText(PortalNightsRunState runState, string leaderboardText)
        {
            if (runState == null)
            {
                return;
            }

            runState.UpdateTotalRunTime();
            if (waveText != null)
            {
                waveText.text = "UNIVERSE COMPLETE";
            }

            if (enemiesText != null)
            {
                enemiesText.text = $"FINAL SCORE: {PortalNightsScoreCalculator.CalculateSummaryScore(runState)}   UNIVERSE: {Mathf.Max(1, runState.universeIndex)}   TIME: {FormatTime(runState.totalRunTime)}";
            }

            if (laneText != null)
            {
                laneText.text = $"ENEMIES: {runState.enemiesKilled + runState.enhancedEnemiesKilled}   BOSSES: {runState.bossesKilled}   STAFF: {runState.staffSaved}   SPHERES: {runState.spheresRestored}   DEATHS: {runState.playerDeaths}   TURRETS: {runState.turretsBuilt}/{runState.turretsUpgraded}";
            }

            if (promptText != null)
            {
                promptText.text = universeCompleteLeaderboardVisible && !string.IsNullOrWhiteSpace(leaderboardText)
                    ? leaderboardText
                    : BuildUniverseCompleteControlsText(runState);
            }
        }

        public void ShowLocalLeaderboard(IReadOnlyList<PortalNightsLeaderboardEntry> topEntries)
        {
            if (promptText == null || topEntries == null)
            {
                return;
            }

            StringBuilder builder = new StringBuilder("LOCAL LEADERBOARD");
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
                builder.Append(string.IsNullOrWhiteSpace(entry.playerName) ? "Commander" : entry.playerName);
                builder.Append("  ");
                builder.Append(entry.score);
                builder.Append("  U");
                builder.Append(entry.universe);
                builder.Append("  ");
                builder.Append(FormatTime(entry.totalTime));
                builder.Append("  E");
                builder.Append(entry.enemiesKilled);
                builder.Append("  B");
                builder.Append(entry.bossesKilled);
            }

            promptText.text = builder.ToString();
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
                return "E ENTER NEXT UNIVERSE   L LOCAL LEADERBOARD";
            }

            StringBuilder builder = new StringBuilder();
            builder.AppendLine("RUN SUMMARY");
            builder.Append("SCORE: ");
            builder.Append(PortalNightsScoreCalculator.CalculateSummaryScore(runState));
            builder.Append("   TIME: ");
            builder.AppendLine(FormatTime(runState.totalRunTime));
            builder.Append("ENEMIES KILLED: ");
            builder.Append(runState.enemiesKilled + runState.enhancedEnemiesKilled);
            builder.Append("   BOSSES DEFEATED: ");
            builder.AppendLine(runState.bossesKilled.ToString());
            builder.Append("STAFF SAVED: ");
            builder.Append(runState.staffSaved);
            builder.Append("   SPHERES RESTORED: ");
            builder.Append(runState.spheresRestored);
            builder.Append("   DEATHS: ");
            builder.AppendLine(runState.playerDeaths.ToString());
            builder.Append("TURRETS BUILT/UPGRADED: ");
            builder.Append(runState.turretsBuilt);
            builder.Append("/");
            builder.AppendLine(runState.turretsUpgraded.ToString());
            builder.Append("E NEAR PORTAL: ENTER UNIVERSE ");
            builder.Append(Mathf.Max(2, runState.universeIndex + 1));
            builder.Append("   L: LOCAL LEADERBOARD");
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

            if (waveText != null)
            {
                if (controller.GameOver)
                {
                    waveText.text = PortalNightsLocalization.Text("hud.gameOver");
                }
                else if (controller.GameState == PortalNightsGameState.Planet1_RewardChoice)
                {
                    waveText.text = "CHOOSE REWARD";
                }
                else if (controller.GameState == PortalNightsGameState.Planet1_PortalReady)
                {
                    waveText.text = "PORTAL READY";
                }
                else if (controller.GameState == PortalNightsGameState.Planet2_ClearArea)
                {
                    waveText.text = "PLANET 2: CRYSTAL MOON";
                }
                else if (controller.GameState == PortalNightsGameState.Planet2_SphereReady)
                {
                    waveText.text = "SPHERE READY";
                }
                else if (controller.GameState == PortalNightsGameState.Planet2_DefendSphere)
                {
                    waveText.text = "DEFEND THE SPHERE";
                }
                else if (controller.GameState == PortalNightsGameState.Planet2_Cleared)
                {
                    waveText.text = "PLANET CLEARED";
                }
                else if (controller.GameState == PortalNightsGameState.Planet3_Arrival || controller.GameState == PortalNightsGameState.Planet3_FindStaff || controller.GameState == PortalNightsGameState.Planet3_ReleaseStaff || controller.GameState == PortalNightsGameState.Planet3_EscortToSphere)
                {
                    waveText.text = "PLANET 3 - ASH RELAY STATION";
                }
                else if (controller.GameState == PortalNightsGameState.Planet3_SphereReady || controller.GameState == PortalNightsGameState.Planet3_SphereActivation)
                {
                    waveText.text = "RELAY SPHERE READY";
                }
                else if (controller.GameState == PortalNightsGameState.Planet3_DefensePreparation)
                {
                    waveText.text = $"PREPARE DEFENSE {Mathf.CeilToInt(controller.Planet3PreparationTimer)}";
                }
                else if (controller.GameState == PortalNightsGameState.Planet3_DefendSphere)
                {
                    waveText.text = $"RELAY CHARGE {Mathf.CeilToInt(controller.Planet3RelayCharge)}%";
                }
                else if (controller.GameState == PortalNightsGameState.Planet3_Cleared)
                {
                    waveText.text = "PLANET 3 CLEARED";
                }
                else if (controller.GameState == PortalNightsGameState.Planet3_Failed)
                {
                    waveText.text = "RELAY DESTROYED - PRESS R";
                }
                else if (controller.GameState == PortalNightsGameState.Planet4_Arrival || controller.GameState == PortalNightsGameState.Planet4_HordeActive || controller.GameState == PortalNightsGameState.Planet4_RiftClosing)
                {
                    waveText.text = "PLANET 4 - SWARM EXPANSE";
                }
                else if (controller.GameState == PortalNightsGameState.Planet4_ExitPortalReady)
                {
                    waveText.text = "PLANET 5 PORTAL OPEN";
                }
                else if (controller.GameState == PortalNightsGameState.Planet4_Cleared)
                {
                    waveText.text = "PLANET 4 CLEARED";
                }
                else if (controller.GameState == PortalNightsGameState.Planet4_Failed)
                {
                    waveText.text = "SWARM OVERRUN - PRESS R";
                }
                else if (controller.GameState == PortalNightsGameState.Planet5_Arrival || controller.GameState == PortalNightsGameState.Planet5_BossIntro)
                {
                    waveText.text = "PLANET 5 - CRIMSON SINGULARITY";
                }
                else if (controller.GameState == PortalNightsGameState.Planet5_DestroyHealingSphere)
                {
                    waveText.text = "DESTROY THE HEALING SPHERE";
                }
                else if (controller.GameState == PortalNightsGameState.Planet5_KillBosses)
                {
                    waveText.text = "HEALING DISABLED - KILL THE BOSSES";
                }
                else if (controller.GameState == PortalNightsGameState.Planet5_RestoreSphereReady)
                {
                    waveText.text = "BOSSES DEFEATED - RESTORE THE SPHERE";
                }
                else if (controller.GameState == PortalNightsGameState.Planet5_RestoringSphere)
                {
                    waveText.text = "RESTORING SPHERE";
                }
                else if (controller.GameState == PortalNightsGameState.Planet5_SphereRestored)
                {
                    waveText.text = "SPHERE RESTORED";
                }
                else if (controller.GameState == PortalNightsGameState.Planet5_Failed)
                {
                    waveText.text = "CRIMSON SINGULARITY LOST - PRESS R";
                }
                else if (controller.GameState == PortalNightsGameState.Failed)
                {
                    waveText.text = "SPHERE LOST - PRESS R";
                }
                else if (!controller.WaveRunning && controller.NextWaveTimer > 0.05f)
                {
                    waveText.text = $"{PortalNightsLocalization.Text("hud.wait")} {Mathf.CeilToInt(controller.NextWaveTimer)}";
                }
                else
                {
                    waveText.text = $"{PortalNightsLocalization.Text("hud.wave")} {controller.WaveNumber}";
                }
            }

            if (enemiesText != null)
            {
                enemiesText.text = planet3
                    ? $"OBJECTIVE: RELAY SPHERE   ENEMIES: {controller.EnemiesRemaining}"
                    : planet4
                    ? $"ENEMIES DEFEATED: {controller.Planet4EnemiesDefeated}/{controller.Planet4TargetKills}"
                    : planet5
                    ? controller.GameState == PortalNightsGameState.Planet5_DestroyHealingSphere
                        ? $"SPHERE HP: {FormatHealth(controller.Planet5SphereHealth)}   BOSSES CANNOT DIE"
                        : controller.GameState == PortalNightsGameState.Planet5_RestoreSphereReady || controller.GameState == PortalNightsGameState.Planet5_RestoringSphere
                        ? $"STABILIZERS: {controller.Planet5StabilizersCompleted}/{controller.Planet5StabilizersTotal}"
                        : controller.GameState == PortalNightsGameState.Planet5_SphereRestored
                        ? "SPHERE RESTORED   UNIVERSE STABILIZED"
                        : $"BOSSES DEFEATED: {controller.Planet5BossesDefeated}/2"
                    : controller.GameState == PortalNightsGameState.Planet2_ClearArea
                    ? $"OBJECTIVE: CLEAR THE AREA   ENEMIES: {controller.EnemiesRemaining}"
                    : $"{PortalNightsLocalization.Text("hud.enemies")}: {controller.EnemiesRemaining}";
            }

            if (laneText != null)
            {
                if (controller.GameState == PortalNightsGameState.Planet1_Defense || controller.GameState == PortalNightsGameState.Planet1_RewardChoice || controller.GameState == PortalNightsGameState.Planet1_PortalReady)
                {
                    string portal = controller.PortalReady ? "PORTAL READY" : "PORTAL LOCKED";
                    laneText.text = $"U{controller.UniverseIndex} P{controller.CurrentPlanetIndex}   SCORE: {controller.CurrentScore}   TURRETS: {controller.RequiredTurretsBuilt}/{controller.RequiredTurretsTotal} BUILT   UPGRADES: {controller.RequiredTurretsMaxed}/{controller.RequiredTurretsTotal} MAX   {portal}";
                }
                else
                {
                    laneText.text = planet3
                        ? $"STAFF RESCUED: {controller.Planet3StaffRescued}/2   STAFF AT SPHERE: {controller.Planet3StaffAtSphere}/2   CHARGE: {Mathf.CeilToInt(controller.Planet3RelayCharge)}%   RIFTS: {controller.Planet3ActiveRifts}"
                        : planet4
                        ? $"RIFTS CLOSED: {controller.Planet4RiftsClosed}/{controller.Planet4RiftsTotal}   ACTIVE RIFTS: {controller.Planet4ActiveRifts}   SCORE: {controller.CurrentScore}"
                        : planet5
                        ? controller.GameState == PortalNightsGameState.Planet5_RestoreSphereReady || controller.GameState == PortalNightsGameState.Planet5_RestoringSphere
                            ? $"STABILIZERS: {controller.Planet5StabilizersCompleted}/{controller.Planet5StabilizersTotal}   HOLD: {Mathf.CeilToInt(controller.Planet5StabilizerHoldProgress * 100f)}%"
                            : $"SOLAR: {FormatHealth(controller.Planet5BossAHealth)}   BEHEMOTH: {FormatHealth(controller.Planet5BossBHealth)}   ALLIES: {FormatHealth(controller.Planet5Helper1Health)} / {FormatHealth(controller.Planet5Helper2Health)}"
                        : controller.GameState == PortalNightsGameState.Planet2_DefendSphere
                        ? "OBJECTIVE: DEFEND THE SPHERE"
                        : controller.GameState.ToString().Replace('_', ' ');
                }
            }

            if (coinsText != null)
            {
                coinsText.text = $"{PortalNightsLocalization.Text("hud.coins")}: {controller.Coins}   U{controller.UniverseIndex}";
            }

            if (coreHealth != null)
            {
                if (coreText != null)
                {
                    string healthLabel = planet5 ? "HEALING SPHERE" : planet3 ? "RELAY SPHERE" : PortalNightsLocalization.Text("hud.core");
                    coreText.text = $"{healthLabel} {Mathf.CeilToInt(coreHealth.CurrentHealth)}/{Mathf.CeilToInt(coreHealth.MaxHealth)}";
                }
                if (coreSlider != null)
                {
                    coreSlider.value = coreHealth.Normalized;
                }
            }

            if (playerHealth != null)
            {
                if (playerText != null)
                {
                    playerText.text = $"{PortalNightsLocalization.Text("hud.player")} {Mathf.CeilToInt(playerHealth.CurrentHealth)}/{Mathf.CeilToInt(playerHealth.MaxHealth)}";
                }
                if (playerSlider != null)
                {
                    playerSlider.value = playerHealth.Normalized;
                }
            }

            if (promptText != null)
            {
                promptText.text = BuildPrompt(player);
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
                return "1  +15% WEAPON DAMAGE     2  +15% TURRET DAMAGE     3  REPAIR CORE +150";
            }

            if (controller.GameState == PortalNightsGameState.Planet1_PortalReady && controller.IsPlayerNearPortal(player.transform.position))
            {
                return "E ENTER PORTAL";
            }

            if (controller.GameState == PortalNightsGameState.Planet2_SphereReady && controller.IsPlayerNearSphere(player.transform.position))
            {
                return "E ACTIVATE SPHERE";
            }

            if (controller.GameState == PortalNightsGameState.Failed)
            {
                return "SPHERE LOST - PRESS R TO RETRY";
            }

            if (controller.GameState == PortalNightsGameState.Planet3_Failed)
            {
                return "RELAY SPHERE DESTROYED - PRESS R TO RETRY";
            }

            if (controller.GameState == PortalNightsGameState.Planet5_RestoreSphereReady)
            {
                return "SPHERE READY FOR RESTORATION";
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
                return "WASD MOVE   MOUSE AIM   LMB/SPACE FIRE   SHIFT SPRINT   E BUILD";
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

        private void DrawControlsHint()
        {
            if (controlsStyle == null)
            {
                controlsStyle = new GUIStyle(GUI.skin.label)
                {
                    alignment = TextAnchor.LowerRight,
                    fontSize = Mathf.Clamp(Screen.height / 42, 18, 28),
                    fontStyle = FontStyle.Bold,
                    normal =
                    {
                        textColor = new Color(0.9f, 0.98f, 1f, 0.92f)
                    }
                };
            }

            string text = controller != null && controller.GameOver
                ? "R RESTART   ESC CURSOR"
                : "WASD MOVE   MOUSE AIM   LMB FIRE   E BUILD";
            GUI.Label(new Rect(Screen.width - 620f, Screen.height - 52f, 590f, 34f), text, controlsStyle);
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
                toastText.text = string.Empty;
            }
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
