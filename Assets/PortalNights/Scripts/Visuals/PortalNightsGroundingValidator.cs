using UnityEngine;

namespace PortalNights.Visuals
{
    public sealed class PortalNightsGroundingValidator : MonoBehaviour
    {
        [SerializeField] private int planetIndex = 1;
        [SerializeField] private string characterLabel = "Character";
        [SerializeField] private Transform visualInstance;
        [SerializeField] private bool logPassedChecks = true;

        private bool logged;

        public static PortalNightsGroundingValidationResult AttachAndValidate(
            GameObject gameplayRoot,
            Transform visual,
            int planet,
            string label,
            bool forceLog = false)
        {
            if (gameplayRoot == null)
            {
                return default;
            }

            PortalNightsGroundingValidator validator = gameplayRoot.GetComponent<PortalNightsGroundingValidator>();
            if (validator == null)
            {
                validator = gameplayRoot.AddComponent<PortalNightsGroundingValidator>();
            }

            validator.Configure(visual, planet, label, forceLog);
            return validator.ValidateNow(forceLog);
        }

        public void Configure(Transform visual, int planet, string label, bool forceLog = false)
        {
            visualInstance = visual;
            planetIndex = Mathf.Max(1, planet);
            characterLabel = string.IsNullOrWhiteSpace(label) ? gameObject.name : label;
            if (forceLog)
            {
                logged = false;
            }
        }

        private void Start()
        {
            ValidateNow(false);
        }

        public PortalNightsGroundingValidationResult ValidateNow(bool forceLog)
        {
            PortalNightsGroundingValidationResult result =
                PortalNightsGroundingUtility.ValidateGrounding(gameObject, visualInstance, planetIndex, characterLabel);

            float difference = Mathf.Max(result.colliderFloorError, result.visualFloorError);
            bool shouldLog = forceLog || (!logged && (logPassedChecks || !result.success));
            if (shouldLog)
            {
                Debug.Log(
                    "[GROUND CHECK]\n"
                    + "Enemy: " + characterLabel + "\n"
                    + "Planet: " + planetIndex + "\n"
                    + "Collider bottom: " + result.colliderBottomY.ToString("F3") + "\n"
                    + "Visual bottom: " + result.visualBottomY.ToString("F3") + "\n"
                    + "Floor: " + result.floorY.ToString("F3") + "\n"
                    + "Difference: " + difference.ToString("F3"));
                logged = true;
            }

            if (!result.success)
            {
                Debug.LogWarning(
                    "[GROUNDING_FAIL] "
                    + "character=" + characterLabel
                    + " planet=" + planetIndex
                    + " floorY=" + result.floorY.ToString("F3")
                    + " visualBottom=" + result.visualBottomY.ToString("F3")
                    + " difference=" + result.visualFloorError.ToString("F3"));
            }

            return result;
        }
    }
}
