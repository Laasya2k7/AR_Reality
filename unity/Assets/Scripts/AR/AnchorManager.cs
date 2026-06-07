/// <summary>
/// AnchorManager.cs — Manages AR world anchors to pin 3D object models at detected
/// real-world positions. Supports AR Foundation on mobile and falls back to a
/// fixed position in front of the camera in Editor mode.
/// </summary>

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_ANDROID || UNITY_IOS
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
#endif

namespace RealityOS.AR
{
    public class AnchorManager : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // Inspector References
        // ─────────────────────────────────────────────

        [Header("Anchor Settings")]
        [Tooltip("Distance in front of the camera to place objects in Editor mode.")]
        [SerializeField] private float _editorPlacementDistance = 1.5f;

        [Tooltip("Height offset applied when placing objects above a detected plane.")]
        [SerializeField] private float _heightOffset = 0.0f;

        [Header("AR Foundation (Mobile Only)")]
#if UNITY_ANDROID || UNITY_IOS
        [SerializeField] private ARAnchorManager _arAnchorManager;
        [SerializeField] private ARRaycastManager _arRaycastManager;
        private List<ARRaycastHit> _raycastHits = new List<ARRaycastHit>();
#endif

        // ─────────────────────────────────────────────
        // State
        // ─────────────────────────────────────────────

        private GameObject _anchoredObject;
        private bool _isAnchored = false;

        // ─────────────────────────────────────────────
        // Public API
        // ─────────────────────────────────────────────

        /// <summary>
        /// Place and anchor a GameObject at the appropriate world position.
        /// On mobile, attempts raycast against detected AR planes.
        /// In Editor, places it in front of the main camera.
        /// </summary>
        public void AnchorObject(GameObject objectToAnchor)
        {
            _anchoredObject = objectToAnchor;

#if UNITY_EDITOR || UNITY_WEBGL
            PlaceInFrontOfCamera(objectToAnchor);
#else
            StartCoroutine(PlaceOnARPlane(objectToAnchor));
#endif
        }

        /// <summary>
        /// Remove the current anchor and hide the object.
        /// </summary>
        public void ReleaseAnchor()
        {
            if (_anchoredObject != null)
            {
                _anchoredObject.SetActive(false);
                _anchoredObject = null;
            }
            _isAnchored = false;
            Debug.Log("[AnchorManager] Anchor released.");
        }

        /// <summary>
        /// Update the anchored object's position to a new world-space position.
        /// </summary>
        public void MoveAnchor(Vector3 newPosition)
        {
            if (_anchoredObject != null)
            {
                _anchoredObject.transform.position = newPosition + Vector3.up * _heightOffset;
                Debug.Log($"[AnchorManager] Anchor moved to: {newPosition}");
            }
        }

        public bool IsAnchored => _isAnchored;

        // ─────────────────────────────────────────────
        // Placement Methods
        // ─────────────────────────────────────────────

        private void PlaceInFrontOfCamera(GameObject obj)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[AnchorManager] No main camera found for Editor placement.");
                return;
            }

            Vector3 position = cam.transform.position + cam.transform.forward * _editorPlacementDistance;
            position.y += _heightOffset;

            obj.transform.position = position;
            obj.transform.rotation = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
            obj.SetActive(true);

            _isAnchored = true;
            Debug.Log($"[AnchorManager] Editor mode: placed object at {position}");
        }

#if UNITY_ANDROID || UNITY_IOS
        private IEnumerator PlaceOnARPlane(GameObject obj)
        {
            if (_arRaycastManager == null)
            {
                Debug.LogWarning("[AnchorManager] ARRaycastManager not assigned. Falling back to camera placement.");
                PlaceInFrontOfCamera(obj);
                yield break;
            }

            // Wait until a valid AR plane hit is found by raycasting from screen center
            float timeout = 5.0f;
            float elapsed = 0f;
            bool placed = false;

            while (!placed && elapsed < timeout)
            {
                Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

                if (_arRaycastManager.Raycast(screenCenter, _raycastHits, TrackableType.PlaneWithinPolygon))
                {
                    Pose hitPose = _raycastHits[0].pose;
                    Vector3 position = hitPose.position + Vector3.up * _heightOffset;

                    obj.transform.position = position;
                    obj.transform.rotation = hitPose.rotation;
                    obj.SetActive(true);

                    _isAnchored = true;
                    placed = true;

                    Debug.Log($"[AnchorManager] Placed on AR plane at: {position}");
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (!placed)
            {
                Debug.LogWarning("[AnchorManager] Could not find AR plane. Falling back to camera placement.");
                PlaceInFrontOfCamera(obj);
            }
        }
#endif
    }
}
