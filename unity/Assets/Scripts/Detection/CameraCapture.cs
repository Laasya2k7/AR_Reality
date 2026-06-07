/// <summary>
/// CameraCapture.cs — Captures frames from the device camera or WebCamTexture (Unity Editor),
/// encodes them as JPEG, and triggers the object detection pipeline.
/// Supports both AR Foundation (mobile) and WebCamTexture (Editor/WebGL) modes.
/// </summary>

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using RealityOS.Models;

namespace RealityOS.Detection
{
    public class CameraCapture : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // Inspector References
        // ─────────────────────────────────────────────

        [Header("Camera Settings")]
        [Tooltip("RawImage UI element used to preview the camera feed in Editor mode.")]
        [SerializeField] private RawImage _cameraPreviewImage;

        [Tooltip("JPEG quality for uploaded images (0-100). Lower = faster upload.")]
        [SerializeField][Range(10, 100)] private int _jpegQuality = 75;

        [Tooltip("Cooldown between automatic capture attempts (seconds).")]
        [SerializeField] private float _captureInterval = 3.0f;

        [Header("Events")]
        [SerializeField] private ARManager _arManager;

        // ─────────────────────────────────────────────
        // Private State
        // ─────────────────────────────────────────────

        private WebCamTexture _webCamTexture;
        private bool _isCapturing = false;
        private bool _isProcessing = false;
        private Coroutine _autoCaptureCoroutine;

        // ─────────────────────────────────────────────
        // Unity Lifecycle
        // ─────────────────────────────────────────────

        private void Start()
        {
            InitializeCamera();
        }

        private void OnDestroy()
        {
            StopCamera();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) StopCamera();
            else if (_isCapturing) StartCamera();
        }

        // ─────────────────────────────────────────────
        // Camera Initialization
        // ─────────────────────────────────────────────

        private void InitializeCamera()
        {
#if UNITY_EDITOR || UNITY_WEBGL
            // Use WebCamTexture in Editor and WebGL builds
            InitializeWebCamTexture();
#else
            // On mobile with AR Foundation, the camera is managed by ARManager
            Debug.Log("[CameraCapture] Running on mobile — using AR Foundation camera.");
#endif
        }

        private void InitializeWebCamTexture()
        {
            WebCamDevice[] devices = WebCamTexture.devices;
            if (devices.Length == 0)
            {
                Debug.LogWarning("[CameraCapture] No camera devices found. Capture disabled.");
                return;
            }

            // Prefer rear camera on mobile; use default camera in Editor
            string deviceName = devices[0].name;
            foreach (var device in devices)
            {
                if (!device.isFrontFacing)
                {
                    deviceName = device.name;
                    break;
                }
            }

            _webCamTexture = new WebCamTexture(deviceName, 1280, 720, 30);

            if (_cameraPreviewImage != null)
                _cameraPreviewImage.texture = _webCamTexture;

            StartCamera();
            Debug.Log($"[CameraCapture] WebCamTexture started: {deviceName}");
        }

        // ─────────────────────────────────────────────
        // Public Controls
        // ─────────────────────────────────────────────

        public void StartCamera()
        {
            if (_webCamTexture != null && !_webCamTexture.isPlaying)
            {
                _webCamTexture.Play();
                _isCapturing = true;
                Debug.Log("[CameraCapture] Camera started.");
            }
        }

        public void StopCamera()
        {
            if (_webCamTexture != null && _webCamTexture.isPlaying)
            {
                _webCamTexture.Stop();
                _isCapturing = false;
                Debug.Log("[CameraCapture] Camera stopped.");
            }

            if (_autoCaptureCoroutine != null)
            {
                StopCoroutine(_autoCaptureCoroutine);
                _autoCaptureCoroutine = null;
            }
        }

        /// <summary>
        /// Start auto-capture mode: continuously captures and detects objects at intervals.
        /// </summary>
        public void StartAutoCapture()
        {
            if (_autoCaptureCoroutine != null)
                StopCoroutine(_autoCaptureCoroutine);

            _autoCaptureCoroutine = StartCoroutine(AutoCaptureCoroutine());
            Debug.Log("[CameraCapture] Auto-capture started.");
        }

        public void StopAutoCapture()
        {
            if (_autoCaptureCoroutine != null)
            {
                StopCoroutine(_autoCaptureCoroutine);
                _autoCaptureCoroutine = null;
                Debug.Log("[CameraCapture] Auto-capture stopped.");
            }
        }

        /// <summary>
        /// Manually trigger a single capture and detection pass.
        /// </summary>
        public void CaptureNow()
        {
            if (!_isProcessing)
                StartCoroutine(CaptureAndDetectCoroutine());
        }

        // ─────────────────────────────────────────────
        // Capture & Detection Coroutines
        // ─────────────────────────────────────────────

        private IEnumerator AutoCaptureCoroutine()
        {
            while (true)
            {
                if (!_isProcessing)
                    yield return StartCoroutine(CaptureAndDetectCoroutine());

                yield return new WaitForSeconds(_captureInterval);
            }
        }

        private IEnumerator CaptureAndDetectCoroutine()
        {
            _isProcessing = true;

            byte[] imageBytes = null;

#if UNITY_EDITOR || UNITY_WEBGL
            imageBytes = CaptureFromWebCam();
#else
            // On AR Foundation builds, capture from AR Camera
            imageBytes = CaptureFromARCamera();
#endif

            if (imageBytes == null || imageBytes.Length == 0)
            {
                Debug.LogWarning("[CameraCapture] Image capture returned null or empty data.");
                _isProcessing = false;
                yield break;
            }

            Debug.Log($"[CameraCapture] Captured {imageBytes.Length} bytes. Sending to detection...");

            // Notify ARManager of detection start
            _arManager?.OnDetectionStarted();

            bool requestComplete = false;

            APIClient.Instance.DetectObject(
                imageBytes,
                "image/jpeg",
                (response) =>
                {
                    OnDetectionSuccess(response);
                    requestComplete = true;
                },
                (error) =>
                {
                    OnDetectionError(error);
                    requestComplete = true;
                }
            );

            // Wait for the API call to complete
            yield return new WaitUntil(() => requestComplete);

            _isProcessing = false;
        }

        private byte[] CaptureFromWebCam()
        {
            if (_webCamTexture == null || !_webCamTexture.isPlaying)
            {
                Debug.LogWarning("[CameraCapture] WebCamTexture is not running.");
                return null;
            }

            // Copy WebCamTexture to a Texture2D for encoding
            Texture2D snapshot = new Texture2D(_webCamTexture.width, _webCamTexture.height, TextureFormat.RGB24, false);
            snapshot.SetPixels(_webCamTexture.GetPixels());
            snapshot.Apply();

            byte[] bytes = snapshot.EncodeToJPG(_jpegQuality);
            Destroy(snapshot);
            return bytes;
        }

        private byte[] CaptureFromARCamera()
        {
            Camera arCamera = Camera.main;
            if (arCamera == null) return null;

            RenderTexture rt = new RenderTexture(1280, 720, 24);
            arCamera.targetTexture = rt;
            arCamera.Render();

            RenderTexture.active = rt;
            Texture2D snapshot = new Texture2D(rt.width, rt.height, TextureFormat.RGB24, false);
            snapshot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            snapshot.Apply();

            arCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);

            byte[] bytes = snapshot.EncodeToJPG(_jpegQuality);
            Destroy(snapshot);
            return bytes;
        }

        // ─────────────────────────────────────────────
        // Callbacks
        // ─────────────────────────────────────────────

        private void OnDetectionSuccess(DetectResponse response)
        {
            if (response.success && response.object_data != null)
            {
                Debug.Log($"[CameraCapture] Detected: {response.object_data.display_name} " +
                          $"(confidence={response.object_data.confidence:P0}, mock={response.mock_mode})");
                _arManager?.OnObjectDetected(response.object_data);
            }
            else
            {
                string msg = response.error ?? "Unknown detection error.";
                Debug.LogWarning($"[CameraCapture] Detection unsuccessful: {msg}");
                _arManager?.OnDetectionFailed(msg);
            }
        }

        private void OnDetectionError(string errorMessage)
        {
            Debug.LogError($"[CameraCapture] API error: {errorMessage}");
            _arManager?.OnDetectionFailed(errorMessage);
        }
    }
}
