using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PortalNights
{
    public sealed class PortalNightsMissionComms : MonoBehaviour
    {
        private readonly Dictionary<string, float> lastPlayedAt = new Dictionary<string, float>();
        private readonly HashSet<string> playedOnce = new HashSet<string>();

        private PortalNightsDialogueDatabase database;
        private PortalNightsRadioDialogueController radio;
        private PortalNightsObjectiveTracker objectiveTracker;
        private int debugObjectiveIndex;

        public static PortalNightsMissionComms Instance { get; private set; }

        public bool IsTransitionSuppressed
        {
            get => radio != null && radio.IsTransitionSuppressed;
            set
            {
                EnsureInitialized();
                if (radio != null)
                {
                    radio.IsTransitionSuppressed = value;
                }
            }
        }

        public static PortalNightsMissionComms EnsureInstance()
        {
            if (Instance != null)
            {
                Instance.EnsureInitialized();
                return Instance;
            }

            PortalNightsMissionComms existing = FindFirstObjectByType<PortalNightsMissionComms>();
            if (existing != null)
            {
                Instance = existing;
                Instance.EnsureInitialized();
                return Instance;
            }

            GameObject commsObject = new GameObject("PN_MissionComms");
            Instance = commsObject.AddComponent<PortalNightsMissionComms>();
            Instance.EnsureInitialized();
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureInitialized();
        }

        private void Update()
        {
            UpdateEditorDebugKeys();
        }

        public bool PlayDialogueById(string id)
        {
            EnsureInitialized();
            if (database == null || !database.TryGetLine(id, out PortalNightsDialogueLine line))
            {
                Debug.LogWarning("[PortalNights] Dialogue id not found: " + id, this);
                return false;
            }

            if (!CanPlay(line))
            {
                return false;
            }

            MarkPlayed(line);
            if (!string.IsNullOrWhiteSpace(line.objectiveText))
            {
                SetObjective(line.objectiveText, string.Empty);
            }

            radio?.Play(line);
            return true;
        }

        public void PlayDialogueSequence(params string[] ids)
        {
            EnsureInitialized();
            if (ids == null || ids.Length == 0)
            {
                return;
            }

            StartCoroutine(PlaySequenceRoutine(ids));
        }

        public void SetObjective(string mainText, string progressText)
        {
            EnsureInitialized();
            objectiveTracker?.SetObjective(mainText, progressText);
        }

        public void SetObjective(string mainText, string progressText, PortalNightsObjectiveSeverity severity)
        {
            EnsureInitialized();
            objectiveTracker?.SetObjective(mainText, progressText, severity);
        }

        public void ClearObjective()
        {
            EnsureInitialized();
            objectiveTracker?.ClearObjective();
        }

        public void ShowMissionToast(string text)
        {
            EnsureInitialized();
            string localizedText = PortalNightsLocalization.LocalizeRuntimeText(text);
            objectiveTracker?.ShowToast(localizedText);
            PortalNightsHud.Instance?.ShowToast(localizedText);
        }

        public void ResetRunPlaybackState()
        {
            playedOnce.Clear();
            lastPlayedAt.Clear();
        }

        private IEnumerator PlaySequenceRoutine(string[] ids)
        {
            foreach (string id in ids)
            {
                float wait = 0.25f;
                if (database != null && database.TryGetLine(id, out PortalNightsDialogueLine line))
                {
                    wait = Mathf.Max(0.25f, line.duration + 0.2f);
                }

                PlayDialogueById(id);
                yield return new WaitForSecondsRealtime(wait);
            }
        }

        private void EnsureInitialized()
        {
            database ??= PortalNightsDialogueDatabase.LoadDefault();
            radio = GetComponent<PortalNightsRadioDialogueController>();
            if (radio == null)
            {
                radio = gameObject.AddComponent<PortalNightsRadioDialogueController>();
            }

            radio.EnsureUi();

            objectiveTracker = GetComponent<PortalNightsObjectiveTracker>();
            if (objectiveTracker == null)
            {
                objectiveTracker = gameObject.AddComponent<PortalNightsObjectiveTracker>();
            }

            objectiveTracker.EnsureUi(radio.Canvas);
        }

        private bool CanPlay(PortalNightsDialogueLine line)
        {
            if (line == null || string.IsNullOrWhiteSpace(line.id))
            {
                return false;
            }

            if (!line.canRepeat && playedOnce.Contains(line.id))
            {
                return false;
            }

            if (line.repeatCooldown > 0f && lastPlayedAt.TryGetValue(line.id, out float lastTime))
            {
                float now = Time.unscaledTime;
                if (now - lastTime < line.repeatCooldown)
                {
                    return false;
                }
            }

            return true;
        }

        private void MarkPlayed(PortalNightsDialogueLine line)
        {
            if (line == null || string.IsNullOrWhiteSpace(line.id))
            {
                return;
            }

            playedOnce.Add(line.id);
            lastPlayedAt[line.id] = Time.unscaledTime;
        }

        private void UpdateEditorDebugKeys()
        {
#if UNITY_EDITOR && ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.f8Key.wasPressedThisFrame)
            {
                PlayDialogueById("p01_intro_001");
            }

            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                CycleDebugObjective();
            }
#endif
        }

        private void CycleDebugObjective()
        {
            string[] main =
            {
                PortalNightsLocalization.Text("objective.defendCore"),
                PortalNightsLocalization.Text("objective.defendSphere"),
                PortalNightsLocalization.Text("objective.rescueStaff"),
                PortalNightsLocalization.Text("objective.closeRifts"),
                PortalNightsLocalization.Text("objective.destroyCorruptedSphere")
            };
            string[] progress =
            {
                PortalNightsLocalization.Format("progress.wave", 2, 10),
                PortalNightsLocalization.Format("progress.wave", 2, 6),
                PortalNightsLocalization.Format("progress.staffAtSphere", 1),
                PortalNightsLocalization.Text("hud.riftsClosed") + ": 3/4",
                PortalNightsLocalization.Text("hud.sphereHp") + ": 62%"
            };

            PortalNightsObjectiveSeverity severity = debugObjectiveIndex >= 4
                ? PortalNightsObjectiveSeverity.Danger
                : debugObjectiveIndex >= 3
                    ? PortalNightsObjectiveSeverity.Warning
                    : PortalNightsObjectiveSeverity.Normal;
            SetObjective(main[debugObjectiveIndex], progress[debugObjectiveIndex], severity);
            debugObjectiveIndex = (debugObjectiveIndex + 1) % main.Length;
        }
    }
}
