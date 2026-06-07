/// <summary>
/// ComponentUI.cs — Displays detailed information and AI explanation for a
/// selected X-Ray component. Shown as a slide-up card when a component is tapped.
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RealityOS.Models;

namespace RealityOS.UI
{
    public class ComponentUI : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // Inspector References
        // ─────────────────────────────────────────────

        [Header("Panel")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private RectTransform _panelRectTransform;

        [Header("Content")]
        [SerializeField] private TextMeshProUGUI _componentNameText;
        [SerializeField] private TextMeshProUGUI _shortDescriptionText;
        [SerializeField] private TextMeshProUGUI _explanationText;
        [SerializeField] private Image _componentColorDot;
        [SerializeField] private Button _wikiButton;
        [SerializeField] private Button _closeButton;

        [Header("Animation")]
        [SerializeField] private float _animationDuration = 0.3f;

        // ─────────────────────────────────────────────
        // State
        // ─────────────────────────────────────────────

        private ComponentInfo _currentComponent;
        private bool _isVisible = false;
        private System.Collections.IEnumerator _currentAnimation;

        // ─────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────

        private void Start()
        {
            Hide();
            _closeButton?.onClick.AddListener(Hide);
            _wikiButton?.onClick.AddListener(OpenWikiLink);
        }

        // ─────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────

        /// <summary>Display component info and the AI-generated explanation.</summary>
        public void DisplayExplanation(ComponentInfo component, string explanation)
        {
            _currentComponent = component;

            if (_componentNameText != null)
                _componentNameText.text = component.display_name;

            if (_shortDescriptionText != null)
                _shortDescriptionText.text = component.short_description;

            if (_explanationText != null)
                _explanationText.text = FormatExplanation(explanation);

            if (_componentColorDot != null)
                _componentColorDot.color = component.GetColor();

            bool hasWiki = !string.IsNullOrEmpty(component.wiki_url);
            if (_wikiButton != null)
                _wikiButton.gameObject.SetActive(hasWiki);

            Show();
        }

        /// <summary>Show the component panel with slide-up animation.</summary>
        public void Show()
        {
            if (_panelRoot != null) _panelRoot.SetActive(true);
            _isVisible = true;

            if (_currentAnimation != null) StopCoroutine(_currentAnimation);
            _currentAnimation = AnimatePanel(true);
            StartCoroutine(_currentAnimation);
        }

        /// <summary>Hide the component panel.</summary>
        public void Hide()
        {
            if (_currentAnimation != null) StopCoroutine(_currentAnimation);
            _currentAnimation = AnimatePanel(false);
            StartCoroutine(_currentAnimation);
            _isVisible = false;
        }

        public bool IsVisible => _isVisible;

        // ─────────────────────────────────────────────
        // Private Helpers
        // ─────────────────────────────────────────────

        private string FormatExplanation(string raw)
        {
            // Convert "What is it?:" pattern into bold headers for rich text display
            return raw
                .Replace("What is it?:", "<b>What is it?</b>")
                .Replace("How does it work?:", "<b>How does it work?</b>")
                .Replace("Why is it important?:", "<b>Why is it important?</b>")
                .Replace("Fun Fact:", "<b>Fun Fact</b> 💡");
        }

        private void OpenWikiLink()
        {
            if (_currentComponent != null && !string.IsNullOrEmpty(_currentComponent.wiki_url))
            {
                Application.OpenURL(_currentComponent.wiki_url);
                Debug.Log($"[ComponentUI] Opening wiki: {_currentComponent.wiki_url}");
            }
        }

        private System.Collections.IEnumerator AnimatePanel(bool show)
        {
            if (_canvasGroup == null || _panelRectTransform == null)
            {
                if (!show && _panelRoot != null) _panelRoot.SetActive(false);
                yield break;
            }

            float startAlpha = _canvasGroup.alpha;
            float targetAlpha = show ? 1f : 0f;

            float panelHeight = _panelRectTransform.rect.height;
            Vector2 startPos = _panelRectTransform.anchoredPosition;
            Vector2 targetPos = show
                ? new Vector2(startPos.x, 0)
                : new Vector2(startPos.x, -panelHeight);

            float elapsed = 0f;
            while (elapsed < _animationDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / _animationDuration);

                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                _panelRectTransform.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);

                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
            _panelRectTransform.anchoredPosition = targetPos;

            if (!show && _panelRoot != null)
                _panelRoot.SetActive(false);
        }
    }
}
