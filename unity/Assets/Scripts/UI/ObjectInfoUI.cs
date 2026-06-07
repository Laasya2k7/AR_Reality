/// <summary>
/// ObjectInfoUI.cs — Main UI panel that displays the detected object's information.
/// Handles mode switching tabs, Learn Mode content, component explanation panels,
/// and error messages. Attach to the root UI Canvas GameObject.
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RealityOS.Models;
using RealityOS.AR;

namespace RealityOS.UI
{
    public class ObjectInfoUI : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // Inspector References — Header
        // ─────────────────────────────────────────────

        [Header("Header Panel")]
        [SerializeField] private TextMeshProUGUI _objectNameText;
        [SerializeField] private TextMeshProUGUI _confidenceText;
        [SerializeField] private TextMeshProUGUI _mockModeIndicator;
        [SerializeField] private Image _sustainabilityDotImage;

        [Header("Mode Buttons")]
        [SerializeField] private Button _learnModeButton;
        [SerializeField] private Button _xrayModeButton;
        [SerializeField] private Button _sustainabilityModeButton;
        [SerializeField] private Color _activeModeColor = new Color(0.2f, 0.7f, 1.0f);
        [SerializeField] private Color _inactiveModeColor = new Color(0.3f, 0.3f, 0.3f);

        [Header("Content Panels")]
        [SerializeField] private GameObject _learnPanel;
        [SerializeField] private TextMeshProUGUI _learnContentText;

        [SerializeField] private GameObject _xrayPanel;
        [SerializeField] private ComponentUI _componentUI;

        [SerializeField] private GameObject _sustainabilityPanel;
        [SerializeField] private TextMeshProUGUI _sustainabilityContentText;

        [Header("Error Panel")]
        [SerializeField] private GameObject _errorPanel;
        [SerializeField] private TextMeshProUGUI _errorText;

        [Header("Idle Panel")]
        [SerializeField] private GameObject _idlePanel;
        [SerializeField] private TextMeshProUGUI _scanInstructionText;

        [Header("References")]
        [SerializeField] private ARManager _arManager;

        // ─────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────

        private void Start()
        {
            ShowIdleState();
            SetupButtonListeners();
        }

        // ─────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────

        /// <summary>Display detected object name, confidence, and mode bar.</summary>
        public void DisplayObject(ObjectMetadata obj)
        {
            HideAllPanels();

            if (_objectNameText != null)
                _objectNameText.text = obj.display_name;

            if (_confidenceText != null)
                _confidenceText.text = $"Confidence: {obj.confidence:P0}";

            if (_mockModeIndicator != null)
            {
                bool isMock = obj.confidence == 0.0f; // Heuristic — refine based on response
                _mockModeIndicator.gameObject.SetActive(false);
            }

            // Set sustainability dot color based on rating
            if (_sustainabilityDotImage != null)
            {
                float rating = obj.sustainability_rating / 10f;
                _sustainabilityDotImage.color = Color.Lerp(Color.red, Color.green, rating);
            }

            if (_learnPanel != null)
                _learnPanel.SetActive(true);
        }

        /// <summary>Set the Learn Mode text content.</summary>
        public void SetLearnContent(string content)
        {
            if (_learnContentText != null)
                _learnContentText.text = content;

            ShowPanel(_learnPanel);
        }

        /// <summary>Set the Sustainability Mode text content.</summary>
        public void SetSustainabilityContent(string content)
        {
            if (_sustainabilityContentText != null)
                _sustainabilityContentText.text = content;

            ShowPanel(_sustainabilityPanel);
        }

        /// <summary>Show component explanation in the component detail panel.</summary>
        public void ShowComponentExplanation(ComponentInfo component, string explanation)
        {
            _componentUI?.DisplayExplanation(component, explanation);
        }

        /// <summary>Update mode button colors based on the active mode.</summary>
        public void UpdateModeButtons(AppMode activeMode)
        {
            SetButtonColor(_learnModeButton, activeMode == AppMode.Learn);
            SetButtonColor(_xrayModeButton, activeMode == AppMode.XRay);
            SetButtonColor(_sustainabilityModeButton, activeMode == AppMode.Sustainability);

            // Show appropriate content panel
            if (_learnPanel != null) _learnPanel.SetActive(activeMode == AppMode.Learn);
            if (_xrayPanel != null) _xrayPanel.SetActive(activeMode == AppMode.XRay);
            if (_sustainabilityPanel != null) _sustainabilityPanel.SetActive(activeMode == AppMode.Sustainability);
        }

        /// <summary>Display an error message.</summary>
        public void ShowError(string message)
        {
            if (_errorPanel != null) _errorPanel.SetActive(true);
            if (_errorText != null) _errorText.text = $"⚠️ {message}";

            Debug.LogWarning($"[ObjectInfoUI] Error: {message}");

            // Auto-hide error after 4 seconds
            CancelInvoke(nameof(HideError));
            Invoke(nameof(HideError), 4.0f);
        }

        /// <summary>Show the idle/scanning state with scanning instructions.</summary>
        public void ShowIdleState()
        {
            HideAllPanels();
            if (_idlePanel != null) _idlePanel.SetActive(true);
            if (_scanInstructionText != null)
                _scanInstructionText.text = "Point your camera at an object to begin.";
        }

        // ─────────────────────────────────────────────
        // Private Helpers
        // ─────────────────────────────────────────────

        private void SetupButtonListeners()
        {
            _learnModeButton?.onClick.AddListener(() => _arManager?.SwitchToLearnMode());
            _xrayModeButton?.onClick.AddListener(() => _arManager?.SwitchToXRayMode());
            _sustainabilityModeButton?.onClick.AddListener(() => _arManager?.SwitchToSustainabilityMode());
        }

        private void HideAllPanels()
        {
            if (_learnPanel != null) _learnPanel.SetActive(false);
            if (_xrayPanel != null) _xrayPanel.SetActive(false);
            if (_sustainabilityPanel != null) _sustainabilityPanel.SetActive(false);
            if (_errorPanel != null) _errorPanel.SetActive(false);
            if (_idlePanel != null) _idlePanel.SetActive(false);
        }

        private void ShowPanel(GameObject panel)
        {
            HideAllPanels();
            if (panel != null) panel.SetActive(true);
        }

        private void SetButtonColor(Button button, bool isActive)
        {
            if (button == null) return;
            var image = button.GetComponent<Image>();
            if (image != null)
                image.color = isActive ? _activeModeColor : _inactiveModeColor;
        }

        private void HideError()
        {
            if (_errorPanel != null) _errorPanel.SetActive(false);
        }
    }
}
