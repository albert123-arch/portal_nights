using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
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

        public static PortalNightsHud Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            EnsureInputSystemUiModule();
        }

        private void Start()
        {
            EnsureInputSystemUiModule();
            if (controller == null)
            {
                Bind(PortalNightsGameController.Instance);
            }
        }

        private void Update()
        {
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
            PortalNightsHealth coreHealth = controller.CoreHealth;
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
                enemiesText.text = controller.GameState == PortalNightsGameState.Planet2_ClearArea
                    ? $"OBJECTIVE: CLEAR THE AREA   ENEMIES: {controller.EnemiesRemaining}"
                    : $"{PortalNightsLocalization.Text("hud.enemies")}: {controller.EnemiesRemaining}";
            }

            if (laneText != null)
            {
                if (controller.GameState == PortalNightsGameState.Planet1_Defense || controller.GameState == PortalNightsGameState.Planet1_RewardChoice || controller.GameState == PortalNightsGameState.Planet1_PortalReady)
                {
                    string portal = controller.PortalReady ? "PORTAL READY" : "PORTAL LOCKED";
                    laneText.text = $"TURRETS: {controller.RequiredTurretsBuilt}/{controller.RequiredTurretsTotal} BUILT   UPGRADES: {controller.RequiredTurretsMaxed}/{controller.RequiredTurretsTotal} MAX   {portal}";
                }
                else
                {
                    laneText.text = controller.GameState == PortalNightsGameState.Planet2_DefendSphere
                        ? "OBJECTIVE: DEFEND THE SPHERE"
                        : controller.GameState.ToString().Replace('_', ' ');
                }
            }

            if (coinsText != null)
            {
                coinsText.text = $"{PortalNightsLocalization.Text("hud.coins")}: {controller.Coins}";
            }

            if (coreHealth != null)
            {
                if (coreText != null)
                {
                    coreText.text = $"{PortalNightsLocalization.Text("hud.core")} {Mathf.CeilToInt(coreHealth.CurrentHealth)}/{Mathf.CeilToInt(coreHealth.MaxHealth)}";
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

            PortalNightsBuildPoint point = controller.GetClosestBuildPoint(player.transform.position, controller.BuildInteractionRange, false);
            if (point == null || controller.GameState != PortalNightsGameState.Planet1_Defense && controller.GameState != PortalNightsGameState.Planet1_PortalReady)
            {
                return "WASD MOVE   MOUSE AIM   LMB/SPACE FIRE   SHIFT SPRINT   E BUILD";
            }

            return point.GetInteractionPrompt();
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
    }
}
