/// <summary>
/// APIClient.cs — Handles all HTTP communication with the RealityOS FastAPI backend.
/// Uses UnityWebRequest with coroutines. All public methods accept callbacks for results.
/// </summary>

using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using RealityOS.Models;

namespace RealityOS.Detection
{
    public class APIClient : MonoBehaviour
    {
        // ─────────────────────────────────────────────
        // Configuration
        // ─────────────────────────────────────────────

        [Header("Backend Configuration")]
        [Tooltip("Base URL of the RealityOS FastAPI backend. No trailing slash.")]
        [SerializeField] private string _baseUrl = "http://localhost:8000";

        [Tooltip("Timeout in seconds for API requests.")]
        [SerializeField] private float _timeoutSeconds = 15f;

        public static APIClient Instance { get; private set; }

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
            DontDestroyOnLoad(gameObject);
        }

        // ─────────────────────────────────────────────
        // Public API Methods
        // ─────────────────────────────────────────────

        /// <summary>
        /// Upload a camera image to POST /api/detect and receive full object metadata.
        /// </summary>
        public void DetectObject(byte[] imageBytes, string mimeType, Action<DetectResponse> onSuccess, Action<string> onError)
        {
            StartCoroutine(DetectCoroutine(imageBytes, mimeType, onSuccess, onError));
        }

        /// <summary>
        /// Request AI explanation for a component via POST /api/explain.
        /// </summary>
        public void ExplainComponent(string objectId, string componentId, Action<ComponentExplanationResponse> onSuccess, Action<string> onError)
        {
            StartCoroutine(ExplainCoroutine(objectId, componentId, onSuccess, onError));
        }

        /// <summary>
        /// Fetch X-Ray component list via GET /api/xray/{object_id}.
        /// </summary>
        public void GetXRayData(string objectId, Action<XRayResponse> onSuccess, Action<string> onError)
        {
            StartCoroutine(XRayCoroutine(objectId, onSuccess, onError));
        }

        /// <summary>
        /// Fetch sustainability analysis via POST /api/xray/sustainability.
        /// </summary>
        public void GetSustainability(string objectId, Action<SustainabilityResponse> onSuccess, Action<string> onError)
        {
            StartCoroutine(SustainabilityCoroutine(objectId, onSuccess, onError));
        }

        /// <summary>
        /// Check backend health via GET /health.
        /// </summary>
        public void CheckHealth(Action<HealthResponse> onSuccess, Action<string> onError)
        {
            StartCoroutine(GetJsonCoroutine<HealthResponse>($"{_baseUrl}/health", onSuccess, onError));
        }

        // ─────────────────────────────────────────────
        // Private Coroutines
        // ─────────────────────────────────────────────

        private IEnumerator DetectCoroutine(byte[] imageBytes, string mimeType, Action<DetectResponse> onSuccess, Action<string> onError)
        {
            string url = $"{_baseUrl}/api/detect";

            WWWForm form = new WWWForm();
            form.AddBinaryData("file", imageBytes, "capture.jpg", mimeType);

            using (UnityWebRequest request = UnityWebRequest.Post(url, form))
            {
                request.timeout = (int)_timeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"Detection failed: {request.error} (HTTP {request.responseCode})");
                    yield break;
                }

                try
                {
                    DetectResponse response = JsonUtility.FromJson<DetectResponse>(request.downloadHandler.text);
                    onSuccess?.Invoke(response);
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Failed to parse detection response: {ex.Message}");
                }
            }
        }

        private IEnumerator ExplainCoroutine(string objectId, string componentId, Action<ComponentExplanationResponse> onSuccess, Action<string> onError)
        {
            string url = $"{_baseUrl}/api/explain";
            string jsonBody = $"{{\"object_id\":\"{objectId}\",\"component_id\":\"{componentId}\"}}";
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = (int)_timeoutSeconds;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"Explanation failed: {request.error} (HTTP {request.responseCode})");
                    yield break;
                }

                try
                {
                    ComponentExplanationResponse response = JsonUtility.FromJson<ComponentExplanationResponse>(request.downloadHandler.text);
                    onSuccess?.Invoke(response);
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Failed to parse explanation response: {ex.Message}");
                }
            }
        }

        private IEnumerator XRayCoroutine(string objectId, Action<XRayResponse> onSuccess, Action<string> onError)
        {
            string url = $"{_baseUrl}/api/xray/{objectId}";
            yield return GetJsonCoroutine<XRayResponse>(url, onSuccess, onError);
        }

        private IEnumerator SustainabilityCoroutine(string objectId, Action<SustainabilityResponse> onSuccess, Action<string> onError)
        {
            string url = $"{_baseUrl}/api/xray/sustainability";
            string jsonBody = $"{{\"object_id\":\"{objectId}\"}}";
            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                request.uploadHandler = new UploadHandlerRaw(bodyBytes);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = (int)_timeoutSeconds;

                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"Sustainability request failed: {request.error}");
                    yield break;
                }

                try
                {
                    SustainabilityResponse response = JsonUtility.FromJson<SustainabilityResponse>(request.downloadHandler.text);
                    onSuccess?.Invoke(response);
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"Failed to parse sustainability response: {ex.Message}");
                }
            }
        }

        private IEnumerator GetJsonCoroutine<T>(string url, Action<T> onSuccess, Action<string> onError)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.timeout = (int)_timeoutSeconds;
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    onError?.Invoke($"GET {url} failed: {request.error}");
                    yield break;
                }

                try
                {
                    T response = JsonUtility.FromJson<T>(request.downloadHandler.text);
                    onSuccess?.Invoke(response);
                }
                catch (Exception ex)
                {
                    onError?.Invoke($"JSON parse error: {ex.Message}");
                }
            }
        }
    }
}
