/// <summary>
/// SustainabilityMode.cs — Handles the Sustainability Mode display.
/// Fetches environmental impact data from the backend and presents
/// eco-scores, carbon footprint, recyclability info, and eco tips.
/// </summary>

using System.Text;
using UnityEngine;
using RealityOS.Models;
using RealityOS.AR;
using RealityOS.Detection;
using RealityOS.UI;

namespace RealityOS.Modes
{
    public class SustainabilityMode : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // Inspector References
        // ─────────────────────────────────────────────

        [Header("UI References")]
        [SerializeField] private ObjectInfoUI _objectInfoUI;
        [SerializeField] private LoadingUI _loadingUI;

        // ─────────────────────────────────────────────
        // State
        // ─────────────────────────────────────────────

        private ObjectMetadata _currentObject;
        private bool _isActive = false;
        private SustainabilityResponse _cachedData;

        // ─────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────

        /// <summary>Activate Sustainability Mode and fetch analysis data.</summary>
        public void Activate(ObjectMetadata objectData)
        {
            _currentObject = objectData;
            _isActive = true;
            Debug.Log($"[SustainabilityMode] Activated for: {objectData.display_name}");

            // Use cached data if available for the same object
            if (_cachedData != null && _cachedData.object_id == objectData.object_id)
            {
                DisplaySustainabilityData(_cachedData);
                return;
            }

            FetchSustainabilityData(objectData.object_id);
        }

        /// <summary>Deactivate Sustainability Mode.</summary>
        public void Deactivate()
        {
            _isActive = false;
            Debug.Log("[SustainabilityMode] Deactivated.");
        }

        // ─────────────────────────────────────────────
        // Data Fetching
        // ─────────────────────────────────────────────

        private void FetchSustainabilityData(string objectId)
        {
            _loadingUI?.Show("Analyzing eco-impact...");

            APIClient.Instance.GetSustainability(
                objectId,
                (response) =>
                {
                    _loadingUI?.Hide();
                    if (!_isActive) return; // Mode was switched before data arrived

                    if (response.success)
                    {
                        _cachedData = response;
                        DisplaySustainabilityData(response);
                    }
                    else
                    {
                        string error = response.error ?? "Failed to retrieve sustainability data.";
                        Debug.LogWarning($"[SustainabilityMode] Error: {error}");
                        _objectInfoUI?.ShowError(error);
                    }
                },
                (error) =>
                {
                    _loadingUI?.Hide();
                    Debug.LogError($"[SustainabilityMode] Network error: {error}");
                    _objectInfoUI?.ShowError($"Network error: {error}");
                }
            );
        }

        // ─────────────────────────────────────────────
        // Display
        // ─────────────────────────────────────────────

        private void DisplaySustainabilityData(SustainabilityResponse data)
        {
            StringBuilder sb = new StringBuilder();

            // Eco Score Visualization
            sb.AppendLine($"<b>🌿 Eco Score: {data.sustainability_score}/10</b>");
            sb.AppendLine(BuildScoreBar(data.sustainability_score, 10, "🟩", "⬜"));
            sb.AppendLine();

            sb.AppendLine($"<b>🔧 Repairability: {data.repairability_score}/10</b>");
            sb.AppendLine(BuildScoreBar(data.repairability_score, 10, "🟦", "⬜"));
            sb.AppendLine();

            sb.AppendLine($"<b>⏳ Expected Lifespan:</b> {data.lifespan_years} years");
            sb.AppendLine();

            sb.AppendLine("<b>🌍 Carbon Footprint</b>");
            sb.AppendLine(data.carbon_footprint_summary);
            sb.AppendLine();

            sb.AppendLine("<b>♻️ Recyclability</b>");
            sb.AppendLine(data.recyclability_summary);
            sb.AppendLine();

            if (data.eco_tips != null && data.eco_tips.Length > 0)
            {
                sb.AppendLine("<b>💚 Eco Tips</b>");
                foreach (var tip in data.eco_tips)
                    sb.AppendLine($"• {tip}");
                sb.AppendLine();
            }

            sb.AppendLine("<b>🗑️ End of Life</b>");
            sb.AppendLine(data.end_of_life_advice);

            if (data.mock_mode)
                sb.AppendLine("\n<i><color=#888888>[Mock data — add Gemini API key for live analysis]</color></i>");

            _objectInfoUI?.SetSustainabilityContent(sb.ToString());
        }

        /// <summary>Builds a simple text-based progress bar for scores.</summary>
        private string BuildScoreBar(int value, int max, string filledChar, string emptyChar)
        {
            var bar = new StringBuilder();
            for (int i = 0; i < max; i++)
                bar.Append(i < value ? filledChar : emptyChar);
            return bar.ToString();
        }
    }
}
