using System;

namespace PortalNights
{
    [Serializable]
    public sealed class PortalNightsDialogueLine
    {
        public string id;
        public int planet;
        public string gameState;
        public string trigger;
        public string speaker;
        public string text;
        public string objectiveText;
        public float duration = 3.5f;
        public int priority;
        public float repeatCooldown;
        public string voiceClipId;
        public bool canRepeat;
    }

    [Serializable]
    public sealed class PortalNightsDialogueLineCollection
    {
        public PortalNightsDialogueLine[] lines;
    }
}
