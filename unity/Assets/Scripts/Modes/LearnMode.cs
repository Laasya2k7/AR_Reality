/// <summary>
/// LearnMode.cs — Handles the Learn Mode display logic.
/// Shows object description, functionality, applications, and interesting facts
/// pulled from the detection response. No additional API calls needed.
/// </summary>

using System.Text;
using UnityEngine;
using RealityOS.Models;
using RealityOS.UI;

namespace RealityOS.Modes
{
    public class LearnMode : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // Inspector References
        // ─────────────────────────────────────────────

        [Header("UI References")]
        [SerializeField] private ObjectInfoUI _objectInfoUI;

        // ─────────────────────────────────────────────
        // State
        // ─────────────────────────────────────────────

        private ObjectMetadata _currentObject;
        private bool _isActive = false;

        // ─────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────

        /// <summary>Activate Learn Mode for the given object data.</summary>
        public void Activate(ObjectMetadata objectData)
        {
            _currentObject = objectData;
            _isActive = true;
            Debug.Log($"[LearnMode] Activated for: {objectData.display_name}");
            DisplayLearnContent();
        }

        /// <summary>Deactivate Learn Mode and hide learn-specific UI.</summary>
        public void Deactivate()
        {
            _isActive = false;
            Debug.Log("[LearnMode] Deactivated.");
        }

        // ─────────────────────────────────────────────
        // Content Building
        // ─────────────────────────────────────────────

        private void DisplayLearnContent()
        {
            if (_currentObject == null) return;

            // Build a rich text description for the UI
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"<b>About the {_currentObject.display_name}</b>");
            sb.AppendLine();
            sb.AppendLine(_currentObject.description);
            sb.AppendLine();

            sb.AppendLine("<b>How It Works</b>");
            sb.AppendLine(_currentObject.functionality);
            sb.AppendLine();

            if (_currentObject.applications != null && _currentObject.applications.Length > 0)
            {
                sb.AppendLine("<b>Key Applications</b>");
                foreach (var app in _currentObject.applications)
                    sb.AppendLine($"• {app}");
                sb.AppendLine();
            }

            if (_currentObject.interesting_facts != null && _currentObject.interesting_facts.Length > 0)
            {
                sb.AppendLine("<b>Did You Know?</b>");
                foreach (var fact in _currentObject.interesting_facts)
                    sb.AppendLine($"💡 {fact}");
            }

            _objectInfoUI?.SetLearnContent(sb.ToString());
        }
    }
}
