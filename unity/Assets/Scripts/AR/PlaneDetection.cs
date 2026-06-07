/// <summary>
/// PlaneDetection.cs — Enables and disables AR Foundation plane visualizers.
/// Provides utility methods for toggling plane visualization during different app modes.
/// Compiles gracefully in Unity Editor without AR Foundation.
/// </summary>

using System.Collections.Generic;
using UnityEngine;

#if UNITY_ANDROID || UNITY_IOS
using UnityEngine.XR.ARFoundation;
#endif

namespace RealityOS.AR
{
    public class PlaneDetection : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // Inspector References
        // ─────────────────────────────────────────────

#if UNITY_ANDROID || UNITY_IOS
        [Header("AR Foundation")]
        [SerializeField] private ARPlaneManager _arPlaneManager;
#endif

        [Header("Visualization")]
        [Tooltip("Material applied to detected planes when visualization is enabled.")]
        [SerializeField] private Material _planeMaterialEnabled;

        [Tooltip("Material applied to detected planes when visualization is disabled (transparent).")]
        [SerializeField] private Material _planeMaterialDisabled;

        // ─────────────────────────────────────────────
        // State
        // ─────────────────────────────────────────────

        private bool _visualizationEnabled = true;

        // ─────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────

        private void Start()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (_arPlaneManager != null)
                _arPlaneManager.planesChanged += OnPlanesChanged;
#endif
            EnablePlaneDetection(true);
        }

        private void OnDestroy()
        {
#if UNITY_ANDROID || UNITY_IOS
            if (_arPlaneManager != null)
                _arPlaneManager.planesChanged -= OnPlanesChanged;
#endif
        }

        // ─────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────

        /// <summary>Enable or disable AR plane detection and visualization.</summary>
        public void EnablePlaneDetection(bool enable)
        {
            _visualizationEnabled = enable;

#if UNITY_ANDROID || UNITY_IOS
            if (_arPlaneManager != null)
            {
                _arPlaneManager.enabled = enable;

                // Update all existing plane visualizers
                foreach (var plane in _arPlaneManager.trackables)
                {
                    plane.gameObject.SetActive(enable);
                }
            }
#endif

            Debug.Log($"[PlaneDetection] Plane detection {(enable ? "enabled" : "disabled")}.");
        }

        /// <summary>Toggle plane visualization without stopping detection.</summary>
        public void TogglePlaneVisualization(bool showVisuals)
        {
#if UNITY_ANDROID || UNITY_IOS
            if (_arPlaneManager == null) return;

            foreach (var plane in _arPlaneManager.trackables)
            {
                var renderer = plane.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.material = showVisuals ? _planeMaterialEnabled : _planeMaterialDisabled;
            }
#endif

            Debug.Log($"[PlaneDetection] Plane visuals {(showVisuals ? "shown" : "hidden")}.");
        }

        // ─────────────────────────────────────────────
        // Plane Event Handlers
        // ─────────────────────────────────────────────

#if UNITY_ANDROID || UNITY_IOS
        private void OnPlanesChanged(ARPlanesChangedEventArgs args)
        {
            foreach (var plane in args.added)
            {
                Debug.Log($"[PlaneDetection] New plane detected: {plane.trackableId} (size: {plane.size})");
                var renderer = plane.GetComponent<Renderer>();
                if (renderer != null && _planeMaterialEnabled != null)
                    renderer.material = _planeMaterialEnabled;
            }

            foreach (var plane in args.removed)
            {
                Debug.Log($"[PlaneDetection] Plane removed: {plane.trackableId}");
            }
        }
#endif
    }
}
