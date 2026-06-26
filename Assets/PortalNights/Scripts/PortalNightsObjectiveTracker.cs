using UnityEngine;
using UnityEngine.UI;

namespace PortalNights
{
    public enum PortalNightsObjectiveSeverity
    {
        Normal,
        Warning,
        Danger
    }

    public sealed class PortalNightsObjectiveTracker : MonoBehaviour
    {
        private Canvas canvas;
        private CanvasGroup panelGroup;
        private Image accent;
        private Image statusIcon;
        private Text objectiveText;
        private Text progressText;
        private Text toastText;
        private float toastTimer;

        public void EnsureUi(Canvas targetCanvas)
        {
            if (panelGroup != null)
            {
                return;
            }

            canvas = targetCanvas;
            if (canvas == null)
            {
                return;
            }

            Font font = PortalNightsUIFont.GetDefaultFont();

            GameObject panelObject = new GameObject("PN_ObjectivePanel", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            panelObject.transform.SetParent(canvas.transform, false);
            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(24f, -96f);
            panelRect.sizeDelta = new Vector2(462f, 84f);

            panelGroup = panelObject.GetComponent<CanvasGroup>();
            panelGroup.alpha = 0f;
            Image background = panelObject.GetComponent<Image>();
            background.color = new Color(0.02f, 0.06f, 0.1f, 0.84f);

            GameObject accentObject = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accentObject.transform.SetParent(panelObject.transform, false);
            RectTransform accentRect = accentObject.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 0f);
            accentRect.anchorMax = new Vector2(0f, 1f);
            accentRect.offsetMin = Vector2.zero;
            accentRect.offsetMax = new Vector2(5f, 0f);
            accent = accentObject.GetComponent<Image>();
            accent.color = new Color(0.38f, 0.96f, 1f, 1f);

            GameObject iconObject = new GameObject("ObjectiveIcon", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(panelObject.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(25f, 0f);
            iconRect.sizeDelta = new Vector2(14f, 14f);
            iconRect.localRotation = Quaternion.Euler(0f, 0f, 45f);
            statusIcon = iconObject.GetComponent<Image>();
            statusIcon.color = new Color(0.38f, 0.96f, 1f, 0.95f);

            objectiveText = CreateText("MainObjective", panelObject.transform, font, 24, FontStyle.Bold, new Color(0.92f, 1f, 1f, 1f));
            RectTransform objectiveRect = objectiveText.rectTransform;
            objectiveRect.anchorMin = new Vector2(0f, 0.5f);
            objectiveRect.anchorMax = new Vector2(1f, 1f);
            objectiveRect.offsetMin = new Vector2(44f, -2f);
            objectiveRect.offsetMax = new Vector2(-14f, -8f);

            progressText = CreateText("Progress", panelObject.transform, font, 17, FontStyle.Bold, new Color(0.62f, 0.96f, 1f, 0.95f));
            RectTransform progressRect = progressText.rectTransform;
            progressRect.anchorMin = new Vector2(0f, 0f);
            progressRect.anchorMax = new Vector2(1f, 0.5f);
            progressRect.offsetMin = new Vector2(44f, 8f);
            progressRect.offsetMax = new Vector2(-14f, 0f);

            toastText = CreateText("MissionToast", canvas.transform, font, 28, FontStyle.Bold, new Color(1f, 0.86f, 0.22f, 1f));
            RectTransform toastRect = toastText.rectTransform;
            toastRect.anchorMin = new Vector2(0.5f, 1f);
            toastRect.anchorMax = new Vector2(0.5f, 1f);
            toastRect.pivot = new Vector2(0.5f, 1f);
            toastRect.anchoredPosition = new Vector2(0f, -154f);
            toastRect.sizeDelta = new Vector2(760f, 48f);
            toastText.alignment = TextAnchor.MiddleCenter;
            toastText.text = string.Empty;
        }

        public void SetObjective(string mainText, string progressLine = "", PortalNightsObjectiveSeverity severity = PortalNightsObjectiveSeverity.Normal)
        {
            if (objectiveText == null || progressText == null)
            {
                return;
            }

            objectiveText.text = string.IsNullOrWhiteSpace(mainText) ? string.Empty : mainText.Trim();
            progressText.text = string.IsNullOrWhiteSpace(progressLine) ? string.Empty : progressLine.Trim();
            ApplySeverity(severity);
            panelGroup.alpha = string.IsNullOrWhiteSpace(objectiveText.text) ? 0f : 1f;
        }

        public void ClearObjective()
        {
            if (objectiveText != null)
            {
                objectiveText.text = string.Empty;
            }

            if (progressText != null)
            {
                progressText.text = string.Empty;
            }

            if (panelGroup != null)
            {
                panelGroup.alpha = 0f;
            }
        }

        public void ShowToast(string text)
        {
            if (toastText == null)
            {
                return;
            }

            toastText.text = string.IsNullOrWhiteSpace(text) ? string.Empty : text.Trim();
            toastTimer = string.IsNullOrWhiteSpace(toastText.text) ? 0f : 2.6f;
        }

        private void Update()
        {
            if (toastText == null || toastTimer <= 0f)
            {
                return;
            }

            toastTimer -= Time.unscaledDeltaTime;
            Color color = toastText.color;
            color.a = Mathf.Clamp01(toastTimer);
            toastText.color = color;
            if (toastTimer <= 0f)
            {
                toastText.text = string.Empty;
                color.a = 1f;
                toastText.color = color;
            }
        }

        private void ApplySeverity(PortalNightsObjectiveSeverity severity)
        {
            if (accent == null || objectiveText == null || progressText == null)
            {
                return;
            }

            Color accentColor = severity == PortalNightsObjectiveSeverity.Danger
                ? new Color(1f, 0.22f, 0.18f, 1f)
                : severity == PortalNightsObjectiveSeverity.Warning
                    ? new Color(1f, 0.76f, 0.22f, 1f)
                    : new Color(0.38f, 0.96f, 1f, 1f);
            accent.color = accentColor;
            if (statusIcon != null)
            {
                statusIcon.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.95f);
            }
            objectiveText.color = severity == PortalNightsObjectiveSeverity.Normal ? new Color(0.92f, 1f, 1f, 1f) : accentColor;
            progressText.color = new Color(accentColor.r, accentColor.g, accentColor.b, 0.92f);
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
            shadow.effectColor = new Color(0f, 0f, 0f, 0.85f);
            shadow.effectDistance = new Vector2(1.6f, -1.6f);
            return text;
        }
    }
}
