/// <summary>
/// ARManager.cs — Central state manager for the RealityOS AR application.
/// Coordinates object spawning, active mode switching, UI updates, and component selection.
/// This is the main hub that connects detection, 3D visualization, and UI systems.
/// </summary>

using System.Collections.Generic;
using UnityEngine;
using RealityOS.Models;
using RealityOS.UI;
using RealityOS.Modes;

namespace RealityOS.AR
{
    public enum AppMode
    {
        Idle,
        Scanning,
        Learn,
        XRay,
        Sustainability
    }

    public class ARManager : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // Inspector References
        // ─────────────────────────────────────────────

        [Header("Prefabs & Spawning")]
        [Tooltip("Parent transform where AR object models are instantiated.")]
        [SerializeField] private Transform _arObjectParent;

        [Tooltip("Dictionary of object_id -> Prefab. Assign in Inspector.")]
        [SerializeField] private ObjectPrefabMapping[] _objectPrefabMappings;

        [Header("Mode Controllers")]
        [SerializeField] private LearnMode _learnMode;
        [SerializeField] private XRayMode _xRayMode;
        [SerializeField] private SustainabilityMode _sustainabilityMode;

        [Header("UI Controllers")]
        [SerializeField] private ObjectInfoUI _objectInfoUI;
        [SerializeField] private LoadingUI _loadingUI;

        // ─────────────────────────────────────────────
        // State
        // ─────────────────────────────────────────────

        public static ARManager Instance { get; private set; }

        private ObjectMetadata _currentObject;
        private GameObject _currentObjectInstance;
        private AppMode _currentMode = AppMode.Idle;

        public ObjectMetadata CurrentObject => _currentObject;
        public AppMode CurrentMode => _currentMode;

        // ─────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            SetMode(AppMode.Scanning);
            _loadingUI?.Hide();
        }

        // ─────────────────────────────────────────────
        // Detection Callbacks (called by CameraCapture)
        // ─────────────────────────────────────────────

        /// <summary>Called by CameraCapture when detection begins (shows loading state).</summary>
        public void OnDetectionStarted()
        {
            Debug.Log("[ARManager] Detection started.");
            _loadingUI?.Show("Analyzing...");
        }

        /// <summary>Called when detection returns a successful result.</summary>
        public void OnObjectDetected(ObjectMetadata objectData)
        {
            _loadingUI?.Hide();

            if (_currentObject != null && _currentObject.object_id == objectData.object_id)
            {
                Debug.Log($"[ARManager] Same object detected again: {objectData.object_id}");
                return; // Already displaying this object
            }

            _currentObject = objectData;
            Debug.Log($"[ARManager] Object detected: {objectData.display_name}");

            SpawnObjectModel(objectData);
            _objectInfoUI?.DisplayObject(objectData);
            SetMode(AppMode.Learn); // Default to Learn Mode on detection
        }

        /// <summary>Called when detection fails or returns an error.</summary>
        public void OnDetectionFailed(string errorMessage)
        {
            _loadingUI?.Hide();
            Debug.LogWarning($"[ARManager] Detection failed: {errorMessage}");
            _objectInfoUI?.ShowError(errorMessage);
        }

        // ─────────────────────────────────────────────
        // Mode Switching (called by UI buttons)
        // ─────────────────────────────────────────────

        public void SetMode(AppMode mode)
        {
            _currentMode = mode;
            Debug.Log($"[ARManager] Switching to mode: {mode}");

            // Deactivate all modes
            _learnMode?.Deactivate();
            _xRayMode?.Deactivate();
            _sustainabilityMode?.Deactivate();

            // Activate the requested mode
            switch (mode)
            {
                case AppMode.Learn:
                    if (_currentObject != null)
                        _learnMode?.Activate(_currentObject);
                    break;

                case AppMode.XRay:
                    if (_currentObject != null)
                        _xRayMode?.Activate(_currentObject);
                    break;

                case AppMode.Sustainability:
                    if (_currentObject != null)
                        _sustainabilityMode?.Activate(_currentObject);
                    break;

                case AppMode.Scanning:
                case AppMode.Idle:
                    // No active mode
                    break;
            }

            _objectInfoUI?.UpdateModeButtons(mode);
        }

        // Convenience mode switch methods for UI buttons
        public void SwitchToLearnMode() => SetMode(AppMode.Learn);
        public void SwitchToXRayMode() => SetMode(AppMode.XRay);
        public void SwitchToSustainabilityMode() => SetMode(AppMode.Sustainability);
        public void SwitchToScanning() => SetMode(AppMode.Scanning);

        // ─────────────────────────────────────────────
        // Component Selection (called by XRayMode)
        // ─────────────────────────────────────────────

        public void OnComponentSelected(ComponentInfo component)
        {
            Debug.Log($"[ARManager] Component selected: {component.display_name}");
            _loadingUI?.Show("Loading explanation...");

            APIClient.Instance?.ExplainComponent(
                _currentObject.object_id,
                component.component_id,
                (response) =>
                {
                    _loadingUI?.Hide();
                    if (response.success)
                        _objectInfoUI?.ShowComponentExplanation(component, response.explanation);
                    else
                        _objectInfoUI?.ShowError(response.error ?? "Explanation unavailable.");
                },
                (error) =>
                {
                    _loadingUI?.Hide();
                    _objectInfoUI?.ShowError($"Network error: {error}");
                }
            );
        }

        // ─────────────────────────────────────────────
        // Object Model Spawning
        // ─────────────────────────────────────────────

        private void SpawnObjectModel(ObjectMetadata objectData)
        {
            // Destroy existing model
            if (_currentObjectInstance != null)
            {
                Destroy(_currentObjectInstance);
                _currentObjectInstance = null;
            }

            GameObject prefab = FindPrefabForObject(objectData.object_id);
            if (prefab == null)
            {
                Debug.LogWarning($"[ARManager] No prefab found for object_id: {objectData.object_id}. Using placeholder.");
                prefab = GameObject.CreatePrimitive(PrimitiveType.Cube); // Fallback placeholder
                prefab.name = $"{objectData.display_name}_Placeholder";
            }

            Vector3 spawnPosition = _arObjectParent != null
                ? _arObjectParent.position
                : Vector3.zero + Vector3.up * 0.5f;

            _currentObjectInstance = Instantiate(prefab, spawnPosition, Quaternion.identity, _arObjectParent);
            _currentObjectInstance.name = $"{objectData.object_id}_model";

            Debug.Log($"[ARManager] Spawned model: {_currentObjectInstance.name}");
        }

        private GameObject FindPrefabForObject(string objectId)
        {
            if (_objectPrefabMappings == null) return null;

            foreach (var mapping in _objectPrefabMappings)
            {
                if (mapping.objectId == objectId)
                    return mapping.prefab;
            }
            return null;
        }

        public GameObject GetCurrentObjectInstance() => _currentObjectInstance;
    }

    /// <summary>
    /// Simple serializable mapping between an object_id string and a Unity prefab.
    /// Assign in Inspector.
    /// </summary>
    [System.Serializable]
    public class ObjectPrefabMapping
    {
        public string objectId;
        public GameObject prefab;
    }
}
