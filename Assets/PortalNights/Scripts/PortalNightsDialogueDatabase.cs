using System.Collections.Generic;
using UnityEngine;

namespace PortalNights
{
    public sealed class PortalNightsDialogueDatabase
    {
        private const string DefaultResourcePath = "Dialogues/PortalNights_Dialogue_RU";

        private readonly Dictionary<string, PortalNightsDialogueLine> linesById = new Dictionary<string, PortalNightsDialogueLine>();
        private readonly List<PortalNightsDialogueLine> lines = new List<PortalNightsDialogueLine>();

        public IReadOnlyList<PortalNightsDialogueLine> Lines => lines;

        public static PortalNightsDialogueDatabase LoadDefault()
        {
            PortalNightsDialogueDatabase database = new PortalNightsDialogueDatabase();
            TextAsset asset = Resources.Load<TextAsset>(DefaultResourcePath);
            if (asset == null)
            {
                Debug.LogWarning("[PortalNights] Dialogue database not found at Resources/" + DefaultResourcePath + ".");
                return database;
            }

            database.LoadFromJson(asset.text);
            return database;
        }

        public bool TryGetLine(string id, out PortalNightsDialogueLine line)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                line = null;
                return false;
            }

            return linesById.TryGetValue(id, out line);
        }

        public void LoadFromJson(string json)
        {
            lines.Clear();
            linesById.Clear();
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            PortalNightsDialogueLineCollection collection = JsonUtility.FromJson<PortalNightsDialogueLineCollection>(json);
            if (collection?.lines == null)
            {
                Debug.LogWarning("[PortalNights] Dialogue JSON parsed without a 'lines' array.");
                return;
            }

            foreach (PortalNightsDialogueLine line in collection.lines)
            {
                if (line == null || string.IsNullOrWhiteSpace(line.id))
                {
                    continue;
                }

                lines.Add(line);
                linesById[line.id] = line;
            }
        }
    }
}
