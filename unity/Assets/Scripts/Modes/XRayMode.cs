/// <summary>
/// XRayMode.cs — Manages the X-Ray component separation/explosion view.
/// Highlights individual component models, handles click/tap selection events,
/// and triggers component explanation requests via ARManager.
/// </summary>

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RealityOS.Models;
using RealityOS.AR;

namespace RealityOS.Modes
{
    public class XRayMode : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // Inspector References
        // ─────────────────────────────────────────────

        [Header("Explosion Settings")]
        [Tooltip("Distance components are pushed out from center in exploded view.")]
        [SerializeField] private float _explosionRadius = 0.15f;

        [Tooltip("Speed of the explosion/collapse animation.")]
        [SerializeField] private float _animationSpeed = 2.0f;

        [Header("Highlight Settings")]
        [Tooltip("Emission intensity applied to the selected component.")]
        [SerializeField] private float _highlightEmissionIntensity = 1.5f;

        [Header("References")]
        [SerializeField] private ARManager _arManager;

        // ─────────────────────────────────────────────
        // State
        // ─────────────────────────────────────────────

        private ObjectMetadata _currentObject;
        private bool _isActive = false;
        private List<ComponentRenderer> _componentRenderers = new List<ComponentRenderer>();
        private ComponentRenderer _selectedComponent;

        // ─────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────

        /// <summary>Activate X-Ray mode: explode the object model into labeled components.</summary>
        public void Activate(ObjectMetadata objectData)
        {
            _currentObject = objectData;
            _isActive = true;
            Debug.Log($"[XRayMode] Activated for: {objectData.display_name}");
            BuildComponentRenderers(objectData);
            StartCoroutine(AnimateExplosion(true));
        }

        /// <summary>Deactivate X-Ray mode: collapse components back to original positions.</summary>
        public void Deactivate()
        {
            _isActive = false;
            Debug.Log("[XRayMode] Deactivated.");
            StartCoroutine(AnimateExplosion(false));
            ClearHighlight();
        }

        // ─────────────────────────────────────────────
        // Component Building
        // ─────────────────────────────────────────────

        private void BuildComponentRenderers(ObjectMetadata objectData)
        {
            _componentRenderers.Clear();

            GameObject objectInstance = _arManager?.GetCurrentObjectInstance();
            if (objectInstance == null)
            {
                Debug.LogWarning("[XRayMode] No object instance found.");
                return;
            }

            if (objectData.components == null) return;

            foreach (var comp in objectData.components)
            {
                // Try to find child GameObject named after the prefab
                Transform compTransform = objectInstance.transform.Find(comp.model_prefab);

                if (compTransform == null)
                {
                    // Create a placeholder if the child doesn't exist
                    GameObject placeholder = CreateComponentPlaceholder(comp);
                    placeholder.transform.SetParent(objectInstance.transform, false);
                    compTransform = placeholder.transform;
                }

                var cr = new ComponentRenderer
                {
                    ComponentInfo = comp,
                    Transform = compTransform,
                    OriginalLocalPosition = compTransform.localPosition,
                    TargetExplodedPosition = comp.position_offset?.ToVector3() ?? Vector3.zero
                };

                // Assign color
                Renderer renderer = compTransform.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color compColor = comp.GetColor();
                    renderer.material.color = compColor;
                    cr.Renderer = renderer;
                    cr.OriginalColor = compColor;
                }

                _componentRenderers.Add(cr);

                // Add click handler
                AddClickHandler(compTransform.gameObject, comp);
            }
        }

        private GameObject CreateComponentPlaceholder(ComponentInfo comp)
        {
            // Create a small cube as a visual placeholder for each component
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = comp.model_prefab;
            cube.transform.localScale = Vector3.one * 0.05f;

            Renderer r = cube.GetComponent<Renderer>();
            if (r != null)
                r.material.color = comp.GetColor();

            return cube;
        }

        private void AddClickHandler(GameObject obj, ComponentInfo comp)
        {
            XRayComponentHandler handler = obj.GetComponent<XRayComponentHandler>();
            if (handler == null)
                handler = obj.AddComponent<XRayComponentHandler>();

            handler.Initialize(comp, this);
        }

        // ─────────────────────────────────────────────
        // Selection & Highlighting
        // ─────────────────────────────────────────────

        /// <summary>Called when user taps/clicks a component in X-Ray view.</summary>
        public void SelectComponent(ComponentInfo component)
        {
            Debug.Log($"[XRayMode] Component tapped: {component.display_name}");

            ClearHighlight();

            // Find matching renderer
            foreach (var cr in _componentRenderers)
            {
                if (cr.ComponentInfo.component_id == component.component_id)
                {
                    _selectedComponent = cr;
                    HighlightComponent(cr);
                    break;
                }
            }

            // Notify ARManager to fetch explanation
            _arManager?.OnComponentSelected(component);
        }

        private void HighlightComponent(ComponentRenderer cr)
        {
            if (cr.Renderer == null) return;

            // Enable emission for highlight effect
            cr.Renderer.material.EnableKeyword("_EMISSION");
            cr.Renderer.material.SetColor("_EmissionColor", cr.OriginalColor * _highlightEmissionIntensity);
        }

        private void ClearHighlight()
        {
            if (_selectedComponent?.Renderer != null)
            {
                _selectedComponent.Renderer.material.DisableKeyword("_EMISSION");
                _selectedComponent.Renderer.material.color = _selectedComponent.OriginalColor;
                _selectedComponent = null;
            }
        }

        // ─────────────────────────────────────────────
        // Explosion Animation
        // ─────────────────────────────────────────────

        private IEnumerator AnimateExplosion(bool explode)
        {
            float elapsed = 0f;
            float duration = 1.0f / _animationSpeed;

            // Cache start and target positions
            Vector3[] startPositions = new Vector3[_componentRenderers.Count];
            Vector3[] targetPositions = new Vector3[_componentRenderers.Count];

            for (int i = 0; i < _componentRenderers.Count; i++)
            {
                startPositions[i] = _componentRenderers[i].Transform.localPosition;
                targetPositions[i] = explode
                    ? _componentRenderers[i].TargetExplodedPosition
                    : _componentRenderers[i].OriginalLocalPosition;
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                for (int i = 0; i < _componentRenderers.Count; i++)
                {
                    if (_componentRenderers[i].Transform != null)
                        _componentRenderers[i].Transform.localPosition = Vector3.Lerp(startPositions[i], targetPositions[i], t);
                }

                yield return null;
            }

            // Snap to final position
            for (int i = 0; i < _componentRenderers.Count; i++)
            {
                if (_componentRenderers[i].Transform != null)
                    _componentRenderers[i].Transform.localPosition = targetPositions[i];
            }
        }

        // ─────────────────────────────────────────────
        // Helper Data Class
        // ─────────────────────────────────────────────

        private class ComponentRenderer
        {
            public ComponentInfo ComponentInfo;
            public Transform Transform;
            public Renderer Renderer;
            public Vector3 OriginalLocalPosition;
            public Vector3 TargetExplodedPosition;
            public Color OriginalColor;
        }
    }

    /// <summary>
    /// Auto-attached component that handles click/tap events on component GameObjects.
    /// </summary>
    public class XRayComponentHandler : MonoBehaviour
    {
        private ComponentInfo _componentInfo;
        private XRayMode _xRayMode;

        public void Initialize(ComponentInfo info, XRayMode mode)
        {
            _componentInfo = info;
            _xRayMode = mode;
        }

        private void OnMouseDown()
        {
            _xRayMode?.SelectComponent(_componentInfo);
        }
    }
}
