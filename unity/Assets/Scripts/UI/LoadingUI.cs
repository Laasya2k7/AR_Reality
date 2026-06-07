/// <summary>
/// LoadingUI.cs — Manages the loading overlay shown during network requests.
/// Supports a spinner animation, customizable message, and fade transitions.
/// </summary>

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RealityOS.UI
{
    public class LoadingUI : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // Inspector References
        // ─────────────────────────────────────────────

        [Header("Panel")]
        [SerializeField] private GameObject _panelRoot;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Content")]
        [SerializeField] private TextMeshProUGUI _messageText;
        [SerializeField] private Image _spinnerImage;

        [Header("Spinner Settings")]
        [Tooltip("Degrees per second for the spinner rotation.")]
        [SerializeField] private float _spinnerSpeed = 200f;

        [Header("Fade Settings")]
        [SerializeField] private float _fadeDuration = 0.2f;

        // ─────────────────────────────────────────────
        // State
        // ─────────────────────────────────────────────

        private bool _isVisible = false;
        private Coroutine _spinnerCoroutine;
        private Coroutine _fadeCoroutine;

        // ─────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────

        private void Awake()
        {
            if (_panelRoot != null) _panelRoot.SetActive(false);
            if (_canvasGroup != null) _canvasGroup.alpha = 0f;
        }

        // ─────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────

        /// <summary>Show the loading panel with a custom message.</summary>
        public void Show(string message = "Loading...")
        {
            if (_isVisible) return;

            _isVisible = true;

            if (_panelRoot != null) _panelRoot.SetActive(true);

            if (_messageText != null)
                _messageText.text = message;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeCanvasGroup(1f));

            if (_spinnerImage != null)
            {
                if (_spinnerCoroutine != null) StopCoroutine(_spinnerCoroutine);
                _spinnerCoroutine = StartCoroutine(SpinnerCoroutine());
            }

            Debug.Log($"[LoadingUI] Showing: {message}");
        }

        /// <summary>Hide the loading panel with a fade-out.</summary>
        public void Hide()
        {
            if (!_isVisible) return;

            _isVisible = false;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeAndHide());

            if (_spinnerCoroutine != null)
            {
                StopCoroutine(_spinnerCoroutine);
                _spinnerCoroutine = null;
            }

            Debug.Log("[LoadingUI] Hiding.");
        }

        /// <summary>Update the displayed message while the loading panel is visible.</summary>
        public void UpdateMessage(string newMessage)
        {
            if (_messageText != null)
                _messageText.text = newMessage;
        }

        public bool IsVisible => _isVisible;

        // ─────────────────────────────────────────────
        // Coroutines
        // ─────────────────────────────────────────────

        private IEnumerator SpinnerCoroutine()
        {
            while (true)
            {
                if (_spinnerImage != null)
                    _spinnerImage.transform.Rotate(0f, 0f, -_spinnerSpeed * Time.deltaTime);
                yield return null;
            }
        }

        private IEnumerator FadeCanvasGroup(float targetAlpha)
        {
            if (_canvasGroup == null) yield break;

            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < _fadeDuration)
            {
                elapsed += Time.deltaTime;
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / _fadeDuration);
                yield return null;
            }

            _canvasGroup.alpha = targetAlpha;
        }

        private IEnumerator FadeAndHide()
        {
            yield return StartCoroutine(FadeCanvasGroup(0f));
            if (_panelRoot != null) _panelRoot.SetActive(false);
        }
    }
}
