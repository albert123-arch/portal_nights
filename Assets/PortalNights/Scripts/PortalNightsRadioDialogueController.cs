using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace PortalNights
{
    public sealed class PortalNightsRadioDialogueController : MonoBehaviour
    {
        private readonly Queue<PortalNightsDialogueLine> queuedLines = new Queue<PortalNightsDialogueLine>();

        [SerializeField] private bool showDebugPanelBackgrounds;

        private Canvas canvas;
        private CanvasGroup panelGroup;
        private Text speakerText;
        private Text bodyText;
        private Image portraitAccent;
        private PortalNightsDialogueLine currentLine;
        private float currentTimer;
        private float targetAlpha;

        public Canvas Canvas => canvas;
        public bool IsTransitionSuppressed { get; set; }
        public bool IsShowing => currentLine != null || (panelGroup != null && panelGroup.alpha > 0.02f);

        public void EnsureUi()
        {
            if (canvas != null)
            {
                return;
            }

            Font font = PortalNightsUIFont.GetDefaultFont();
            GameObject canvasObject = new GameObject("PN_MissionCommsCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 320;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            GameObject panelObject = new GameObject("PN_RadioPanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            panelObject.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.2f, 0f);
            panelRect.anchorMax = new Vector2(0.8f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 122f);
            panelRect.sizeDelta = new Vector2(0f, 86f);

            panelGroup = panelObject.GetComponent<CanvasGroup>();
            panelGroup.alpha = 0f;
            panelGroup.blocksRaycasts = false;
            panelGroup.interactable = false;

            Image background = panelObject.GetComponent<Image>();
            background.color = new Color(0.015f, 0.035f, 0.07f, 0.92f);
            background.raycastTarget = false;
            background.enabled = showDebugPanelBackgrounds;

            GameObject cyanLine = new GameObject("CyanSignalLine", typeof(RectTransform), typeof(Image));
            cyanLine.transform.SetParent(panelObject.transform, false);
            RectTransform cyanRect = cyanLine.GetComponent<RectTransform>();
            cyanRect.anchorMin = new Vector2(0f, 1f);
            cyanRect.anchorMax = new Vector2(1f, 1f);
            cyanRect.pivot = new Vector2(0.5f, 1f);
            cyanRect.sizeDelta = new Vector2(0f, 3f);
            cyanRect.anchoredPosition = Vector2.zero;
            Image cyanImage = cyanLine.GetComponent<Image>();
            cyanImage.color = new Color(0.26f, 0.96f, 1f, 0.95f);
            cyanImage.raycastTarget = false;
            cyanImage.enabled = showDebugPanelBackgrounds;

            GameObject portraitObject = new GameObject("PortraitPlaceholder", typeof(RectTransform), typeof(Image));
            portraitObject.transform.SetParent(panelObject.transform, false);
            RectTransform portraitRect = portraitObject.GetComponent<RectTransform>();
            portraitRect.anchorMin = new Vector2(0f, 0.5f);
            portraitRect.anchorMax = new Vector2(0f, 0.5f);
            portraitRect.pivot = new Vector2(0f, 0.5f);
            portraitRect.anchoredPosition = new Vector2(18f, 0f);
            portraitRect.sizeDelta = new Vector2(56f, 56f);
            portraitAccent = portraitObject.GetComponent<Image>();
            portraitAccent.color = new Color(0.12f, 0.66f, 0.92f, 0.38f);
            portraitAccent.raycastTarget = false;
            portraitAccent.enabled = showDebugPanelBackgrounds;

            speakerText = CreateText("Speaker", panelObject.transform, font, 18, FontStyle.Bold, new Color(0.42f, 0.98f, 1f, 1f));
            RectTransform speakerRect = speakerText.rectTransform;
            speakerRect.anchorMin = new Vector2(0f, 0.5f);
            speakerRect.anchorMax = new Vector2(1f, 1f);
            speakerRect.offsetMin = new Vector2(92f, -6f);
            speakerRect.offsetMax = new Vector2(-22f, -10f);

            bodyText = CreateText("Dialogue", panelObject.transform, font, 22, FontStyle.Bold, new Color(0.94f, 0.99f, 1f, 1f));
            RectTransform bodyRect = bodyText.rectTransform;
            bodyRect.anchorMin = new Vector2(0f, 0f);
            bodyRect.anchorMax = new Vector2(1f, 0.58f);
            bodyRect.offsetMin = new Vector2(92f, 8f);
            bodyRect.offsetMax = new Vector2(-22f, 2f);
        }

        public void Play(PortalNightsDialogueLine line)
        {
            if (line == null || string.IsNullOrWhiteSpace(line.text))
            {
                return;
            }

            EnsureUi();
            if (IsTransitionSuppressed)
            {
                return;
            }

            if (currentLine != null && line.priority > currentLine.priority)
            {
                StartLine(line);
                return;
            }

            queuedLines.Enqueue(line);
            if (currentLine == null)
            {
                StartNextLine();
            }
        }

        public void Clear()
        {
            queuedLines.Clear();
            currentLine = null;
            currentTimer = 0f;
            targetAlpha = 0f;
            if (speakerText != null)
            {
                SetTextIfChanged(speakerText, string.Empty);
            }

            if (bodyText != null)
            {
                SetTextIfChanged(bodyText, string.Empty);
            }
        }

        private void Update()
        {
            if (panelGroup == null)
            {
                return;
            }

            panelGroup.alpha = Mathf.MoveTowards(panelGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 5.5f);
            if (currentLine != null)
            {
                currentTimer -= Time.unscaledDeltaTime;
                if (currentTimer <= 0f)
                {
                    currentLine = null;
                    targetAlpha = 0f;
                }
            }
            else if (queuedLines.Count > 0 && panelGroup.alpha <= 0.04f)
            {
                StartNextLine();
            }
        }

        private void StartNextLine()
        {
            if (queuedLines.Count == 0)
            {
                return;
            }

            StartLine(queuedLines.Dequeue());
        }

        private void StartLine(PortalNightsDialogueLine line)
        {
            currentLine = line;
            currentTimer = Mathf.Max(1.2f, line.duration);
            targetAlpha = 1f;
            if (speakerText != null)
            {
                SetTextIfChanged(speakerText, string.IsNullOrWhiteSpace(line.speaker) ? PortalNightsLocalization.Text("toast.system") : line.speaker.ToUpperInvariant());
            }

            if (bodyText != null)
            {
                SetTextIfChanged(bodyText, line.text);
            }

            if (portraitAccent != null)
            {
                portraitAccent.color = GetSpeakerColor(line.speaker);
            }
        }

        private static Color GetSpeakerColor(string speaker)
        {
            if (!string.IsNullOrWhiteSpace(speaker) && speaker.Contains("1"))
            {
                return new Color(0.36f, 0.78f, 1f, 0.48f);
            }

            if (!string.IsNullOrWhiteSpace(speaker) && speaker.Contains("2"))
            {
                return new Color(1f, 0.58f, 0.2f, 0.48f);
            }

            return new Color(0.18f, 0.86f, 1f, 0.44f);
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
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;

            Shadow shadow = textObject.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.92f);
            shadow.effectDistance = new Vector2(1.7f, -1.7f);
            return text;
        }
    }
}
