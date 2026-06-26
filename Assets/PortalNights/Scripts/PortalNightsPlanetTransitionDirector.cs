using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PortalNights
{
    public sealed class PortalNightsPlanetTransitionDirector : MonoBehaviour
    {
        private Canvas canvas;
        private Image fadeImage;
        private Text transitText;
        private CanvasGroup titleGroup;
        private Text titleText;
        private Text subtitleText;
        private Coroutine transitionRoutine;
        private Coroutine titleRoutine;
        private float fadeAlpha;
        private float suppressUntil;

        public static PortalNightsPlanetTransitionDirector Instance { get; private set; }
        public bool IsTransitionActive => transitionRoutine != null || Time.unscaledTime < suppressUntil || fadeAlpha > 0.04f;

        public static PortalNightsPlanetTransitionDirector EnsureInstance()
        {
            if (Instance != null)
            {
                Instance.EnsureUi();
                return Instance;
            }

            PortalNightsPlanetTransitionDirector existing = FindFirstObjectByType<PortalNightsPlanetTransitionDirector>();
            if (existing != null)
            {
                Instance = existing;
                Instance.EnsureUi();
                return Instance;
            }

            GameObject directorObject = new GameObject("PN_PlanetTransitionDirector");
            Instance = directorObject.AddComponent<PortalNightsPlanetTransitionDirector>();
            Instance.EnsureUi();
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
            EnsureUi();
        }

        public void BeginPlanetTransition(int fromPlanet, int toPlanet)
        {
            EnsureUi();
            PortalNightsMissionComms comms = PortalNightsMissionComms.EnsureInstance();
            string departureDialogueId = GetDepartureDialogueId(fromPlanet, toPlanet);
            if (!string.IsNullOrWhiteSpace(departureDialogueId))
            {
                comms.PlayDialogueById(departureDialogueId);
            }

            comms.IsTransitionSuppressed = true;
            suppressUntil = Time.unscaledTime + 1.5f;
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            transitionRoutine = StartCoroutine(BeginTransitionRoutine());
        }

        public void CompletePlanetTransition(int toPlanet)
        {
            EnsureUi();
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            transitionRoutine = StartCoroutine(CompleteTransitionRoutine(toPlanet));
        }

        public void ShowPlanetTitle(int planetIndex, string planetName)
        {
            EnsureUi();
            if (titleRoutine != null)
            {
                StopCoroutine(titleRoutine);
            }

            titleRoutine = StartCoroutine(ShowPlanetTitleRoutine(planetIndex, planetName));
        }

        public void PlayArrivalBriefing(int planetIndex)
        {
            PortalNightsMissionComms comms = PortalNightsMissionComms.EnsureInstance();
            comms.IsTransitionSuppressed = false;
            switch (planetIndex)
            {
                case 2:
                    comms.PlayDialogueById("p02_arrival_001");
                    comms.SetObjective("CLEAR THE AREA", string.Empty);
                    break;
                case 3:
                    comms.PlayDialogueById("p03_arrival_001");
                    comms.SetObjective("RESCUE STAFF", "STAFF RESCUED: 0/2");
                    break;
                case 4:
                    comms.PlayDialogueById("p04_arrival_001");
                    comms.SetObjective("CLOSE RIFTS", "KILL THE SWARM: 0/140");
                    break;
                case 5:
                    comms.PlayDialogueById("p05_arrival_001");
                    comms.SetObjective("DESTROY THE CORRUPTED SPHERE", string.Empty, PortalNightsObjectiveSeverity.Danger);
                    break;
                default:
                    comms.PlayDialogueById("universe_next_001");
                    comms.SetObjective("PLANET 1 - ENEMIES EMPOWERED", string.Empty, PortalNightsObjectiveSeverity.Warning);
                    break;
            }
        }

        private IEnumerator BeginTransitionRoutine()
        {
            SetTransitVisible(true);
            yield return FadeTo(0.88f, 0.42f);
        }

        private IEnumerator CompleteTransitionRoutine(int toPlanet)
        {
            SetTransitVisible(true);
            if (fadeAlpha < 0.76f)
            {
                yield return FadeTo(0.88f, 0.34f);
            }

            yield return new WaitForSecondsRealtime(0.16f);
            SetTransitVisible(false);
            yield return FadeTo(0f, 0.56f);
            suppressUntil = Time.unscaledTime + 0.1f;
            transitionRoutine = null;
            ShowPlanetTitle(toPlanet, GetPlanetName(toPlanet));
            PlayArrivalBriefing(toPlanet);
        }

        private IEnumerator FadeTo(float target, float duration)
        {
            float start = fadeAlpha;
            float elapsed = 0f;
            duration = Mathf.Max(0.01f, duration);
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                SetFadeAlpha(Mathf.Lerp(start, target, Smooth01(elapsed / duration)));
                yield return null;
            }

            SetFadeAlpha(target);
        }

        private IEnumerator ShowPlanetTitleRoutine(int planetIndex, string planetName)
        {
            if (titleGroup == null || titleText == null || subtitleText == null)
            {
                yield break;
            }

            titleText.text = planetIndex <= 1 ? "UNIVERSE SHIFT" : "PLANET " + planetIndex;
            subtitleText.text = planetName;
            float elapsed = 0f;
            while (elapsed < 0.22f)
            {
                elapsed += Time.unscaledDeltaTime;
                titleGroup.alpha = Mathf.Clamp01(elapsed / 0.22f);
                yield return null;
            }

            titleGroup.alpha = 1f;
            yield return new WaitForSecondsRealtime(1.25f);
            elapsed = 0f;
            while (elapsed < 0.36f)
            {
                elapsed += Time.unscaledDeltaTime;
                titleGroup.alpha = 1f - Mathf.Clamp01(elapsed / 0.36f);
                yield return null;
            }

            titleGroup.alpha = 0f;
            titleRoutine = null;
        }

        private void EnsureUi()
        {
            if (canvas != null)
            {
                return;
            }

            Font font = PortalNightsUIFont.GetDefaultFont();
            GameObject canvasObject = new GameObject("PN_PlanetTransitionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 360;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject fadeObject = new GameObject("Fade", typeof(RectTransform), typeof(Image));
            fadeObject.transform.SetParent(canvas.transform, false);
            RectTransform fadeRect = fadeObject.GetComponent<RectTransform>();
            fadeRect.anchorMin = Vector2.zero;
            fadeRect.anchorMax = Vector2.one;
            fadeRect.offsetMin = Vector2.zero;
            fadeRect.offsetMax = Vector2.zero;
            fadeImage = fadeObject.GetComponent<Image>();
            fadeImage.color = new Color(0f, 0f, 0f, 0f);
            fadeImage.raycastTarget = false;

            transitText = CreateText("TransitText", canvas.transform, font, 26, FontStyle.Bold, new Color(0.72f, 0.98f, 1f, 0f));
            RectTransform transitRect = transitText.rectTransform;
            transitRect.anchorMin = new Vector2(0.5f, 0.5f);
            transitRect.anchorMax = new Vector2(0.5f, 0.5f);
            transitRect.pivot = new Vector2(0.5f, 0.5f);
            transitRect.anchoredPosition = new Vector2(0f, -58f);
            transitRect.sizeDelta = new Vector2(560f, 80f);
            transitText.alignment = TextAnchor.MiddleCenter;
            transitText.text = "SIGNAL SHIFTING...\nPORTAL TRANSIT...";

            GameObject titleObject = new GameObject("PlanetTitleCard", typeof(RectTransform), typeof(CanvasGroup));
            titleObject.transform.SetParent(canvas.transform, false);
            RectTransform titleRect = titleObject.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 0.5f);
            titleRect.anchorMax = new Vector2(0.5f, 0.5f);
            titleRect.pivot = new Vector2(0.5f, 0.5f);
            titleRect.anchoredPosition = new Vector2(0f, 78f);
            titleRect.sizeDelta = new Vector2(900f, 138f);
            titleGroup = titleObject.GetComponent<CanvasGroup>();
            titleGroup.alpha = 0f;
            titleGroup.blocksRaycasts = false;
            titleGroup.interactable = false;

            titleText = CreateText("Title", titleObject.transform, font, 42, FontStyle.BoldAndItalic, Color.white);
            RectTransform titleTextRect = titleText.rectTransform;
            titleTextRect.anchorMin = new Vector2(0f, 0.48f);
            titleTextRect.anchorMax = new Vector2(1f, 1f);
            titleTextRect.offsetMin = Vector2.zero;
            titleTextRect.offsetMax = Vector2.zero;
            titleText.alignment = TextAnchor.MiddleCenter;

            subtitleText = CreateText("Subtitle", titleObject.transform, font, 28, FontStyle.Bold, new Color(0.42f, 0.98f, 1f, 1f));
            RectTransform subtitleRect = subtitleText.rectTransform;
            subtitleRect.anchorMin = new Vector2(0f, 0f);
            subtitleRect.anchorMax = new Vector2(1f, 0.55f);
            subtitleRect.offsetMin = Vector2.zero;
            subtitleRect.offsetMax = Vector2.zero;
            subtitleText.alignment = TextAnchor.MiddleCenter;
        }

        private void SetFadeAlpha(float alpha)
        {
            fadeAlpha = Mathf.Clamp01(alpha);
            if (fadeImage != null)
            {
                fadeImage.color = new Color(0f, 0f, 0f, fadeAlpha);
            }

            if (transitText != null && !string.IsNullOrEmpty(transitText.text))
            {
                Color color = transitText.color;
                color.a = fadeAlpha > 0.2f ? Mathf.Clamp01((fadeAlpha - 0.2f) / 0.4f) : 0f;
                transitText.color = color;
            }
        }

        private void SetTransitVisible(bool visible)
        {
            if (transitText != null)
            {
                transitText.text = visible ? "SIGNAL SHIFTING...\nPORTAL TRANSIT..." : string.Empty;
            }
        }

        private static string GetDepartureDialogueId(int fromPlanet, int toPlanet)
        {
            if (fromPlanet == 1 && toPlanet == 2)
            {
                return "p01_entering_portal_001";
            }

            if (fromPlanet == 4 && toPlanet == 5)
            {
                return "p04_portal_to_planet5_ready_001";
            }

            if (fromPlanet == 5 && toPlanet == 1)
            {
                return "p05_enter_next_universe_001";
            }

            return string.Empty;
        }

        private static string GetPlanetName(int planetIndex)
        {
            switch (planetIndex)
            {
                case 2:
                    return "CRYSTAL MOON";
                case 3:
                    return "ASH RELAY STATION";
                case 4:
                    return "SWARM EXPANSE";
                case 5:
                    return "CRIMSON SINGULARITY";
                default:
                    return "PLANET 1 - ENEMIES EMPOWERED";
            }
        }

        private static float Smooth01(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        private static Text CreateText(string name, Transform parent, Font font, int fontSize, FontStyle style, Color color)
        {
            GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Shadow));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.GetComponent<Text>();
            if (font != null)
            {
                text.font = font;
            }
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            Shadow shadow = textObject.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.9f);
            shadow.effectDistance = new Vector2(2f, -2f);
            return text;
        }
    }
}
