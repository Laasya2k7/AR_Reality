"""
test_backend.py — Integration tests for the RealityOS FastAPI backend.

Validates:
- Health endpoint returns correct structure
- Mock mode detection (no API key)
- POST /api/detect with a test image
- POST /api/explain for known and unknown components
- GET /api/xray/{object_id}
- POST /api/xray/sustainability
- 404 handling for unknown objects

Run with:
    python test_backend.py

Or against a running server:
    python test_backend.py --base-url http://localhost:8000
"""

import sys
import os
import json
import base64

# Force UTF-8 output on Windows
if sys.platform == "win32":
    import io
    sys.stdout = io.TextIOWrapper(sys.stdout.buffer, encoding='utf-8', errors='replace')
import argparse
import urllib.request
import urllib.error
import urllib.parse
import io


# ─────────────────────────────────────────────
# Configuration
# ─────────────────────────────────────────────

DEFAULT_BASE_URL = "http://localhost:8000"


def make_request(url, method="GET", data=None, content_type="application/json"):
    """Simple HTTP request helper without external dependencies."""
    req = urllib.request.Request(url, method=method)
    if data:
        if isinstance(data, bytes):
            req.add_header("Content-Type", content_type)
            req.data = data
        else:
            req.add_header("Content-Type", "application/json")
            req.data = json.dumps(data).encode("utf-8")

    try:
        with urllib.request.urlopen(req, timeout=15) as response:
            body = response.read().decode("utf-8")
            return response.status, json.loads(body)
    except urllib.error.HTTPError as e:
        body = e.read().decode("utf-8")
        try:
            return e.code, json.loads(body)
        except Exception:
            return e.code, {"error": body}
    except Exception as e:
        return 0, {"error": str(e)}


def create_test_jpeg() -> bytes:
    """Create a minimal 1x1 pixel JPEG for upload testing (no PIL required)."""
    # Minimal valid JPEG bytes (1x1 white pixel)
    jpeg_bytes = bytes([
        0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01,
        0x01, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0x00, 0xFF, 0xDB, 0x00, 0x43,
        0x00, 0x08, 0x06, 0x06, 0x07, 0x06, 0x05, 0x08, 0x07, 0x07, 0x07, 0x09,
        0x09, 0x08, 0x0A, 0x0C, 0x14, 0x0D, 0x0C, 0x0B, 0x0B, 0x0C, 0x19, 0x12,
        0x13, 0x0F, 0x14, 0x1D, 0x1A, 0x1F, 0x1E, 0x1D, 0x1A, 0x1C, 0x1C, 0x20,
        0x24, 0x2E, 0x27, 0x20, 0x22, 0x2C, 0x23, 0x1C, 0x1C, 0x28, 0x37, 0x29,
        0x2C, 0x30, 0x31, 0x34, 0x34, 0x34, 0x1F, 0x27, 0x39, 0x3D, 0x38, 0x32,
        0x3C, 0x2E, 0x33, 0x34, 0x32, 0xFF, 0xC0, 0x00, 0x0B, 0x08, 0x00, 0x01,
        0x00, 0x01, 0x01, 0x01, 0x11, 0x00, 0xFF, 0xC4, 0x00, 0x1F, 0x00, 0x00,
        0x01, 0x05, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08,
        0x09, 0x0A, 0x0B, 0xFF, 0xC4, 0x00, 0xB5, 0x10, 0x00, 0x02, 0x01, 0x03,
        0x03, 0x02, 0x04, 0x03, 0x05, 0x05, 0x04, 0x04, 0x00, 0x00, 0x01, 0x7D,
        0x01, 0x02, 0x03, 0x00, 0x04, 0x11, 0x05, 0x12, 0x21, 0x31, 0x41, 0x06,
        0x13, 0x51, 0x61, 0x07, 0x22, 0x71, 0x14, 0x32, 0x81, 0x91, 0xA1, 0x08,
        0x23, 0x42, 0xB1, 0xC1, 0x15, 0x52, 0xD1, 0xF0, 0x24, 0x33, 0x62, 0x72,
        0x82, 0x09, 0x0A, 0x16, 0x17, 0x18, 0x19, 0x1A, 0x25, 0x26, 0x27, 0x28,
        0xFF, 0xDA, 0x00, 0x08, 0x01, 0x01, 0x00, 0x00, 0x3F, 0x00, 0xFB, 0xD2,
        0x8A, 0x28, 0x03, 0xFF, 0xD9
    ])
    return jpeg_bytes


# ─────────────────────────────────────────────
# Test Cases
# ─────────────────────────────────────────────

PASS = "[PASS]"
FAIL = "[FAIL]"
results = []


def test(name, condition, detail=""):
    status = PASS if condition else FAIL
    results.append((name, status, detail))
    print(f"  {status} {name}" + (f"\n       {detail}" if detail else ""))


def run_all_tests(base_url: str):
    print(f"\n{'='*60}")
    print(f"  RealityOS Backend Tests")
    print(f"  Target: {base_url}")
    print(f"{'='*60}\n")

    # ── Test 1: Root endpoint
    print("[ 1 ] Root Endpoint")
    status, body = make_request(f"{base_url}/")
    test("GET / returns 200", status == 200, f"Got: {status}")
    test("Root returns service name", body.get("service") == "RealityOS API", f"Body: {body}")

    # ── Test 2: Health endpoint
    print("\n[ 2 ] Health Endpoint")
    status, body = make_request(f"{base_url}/health")
    test("GET /health returns 200", status == 200)
    test("Health has 'status' field", "status" in body)
    test("Health has 'mock_mode_active' field", "mock_mode_active" in body)
    test("Health has 'supported_objects' list", "supported_objects" in body)
    print(f"       Mock mode: {body.get('mock_mode_active')} | Gemini Vision: {body.get('gemini_vision_available')}")

    # ── Test 3: Object Detection
    print("\n[ 3 ] Object Detection (POST /api/detect)")
    jpeg_bytes = create_test_jpeg()

    # Build multipart/form-data manually
    boundary = "----TestBoundary7MA4YWxkTrZu0gW"
    body_parts = (
        f"--{boundary}\r\n"
        f'Content-Disposition: form-data; name="file"; filename="test.jpg"\r\n'
        f"Content-Type: image/jpeg\r\n\r\n"
    ).encode("utf-8") + jpeg_bytes + f"\r\n--{boundary}--\r\n".encode("utf-8")

    status, body = make_request(
        f"{base_url}/api/detect",
        method="POST",
        data=body_parts,
        content_type=f"multipart/form-data; boundary={boundary}"
    )
    test("POST /api/detect returns 200", status == 200, f"Got: {status}, body snippet: {str(body)[:150]}")
    test("Response has 'success' field", "success" in body)
    test("Response has 'mock_mode' field", "mock_mode" in body)
    if body.get("success") and body.get("object_data"):
        obj = body["object_data"]
        test("Object has 'object_id'", "object_id" in obj)
        test("Object has 'components' list", "components" in obj and len(obj["components"]) > 0)
        print(f"       Detected: {obj.get('display_name')} (confidence={obj.get('confidence')})")

    # ── Test 4: Component Explanation
    print("\n[ 4 ] Component Explanation (POST /api/explain)")
    status, body = make_request(f"{base_url}/api/explain", method="POST",
                                data={"object_id": "laptop", "component_id": "cpu"})
    test("POST /api/explain (laptop/cpu) returns 200", status == 200, f"Got: {status}")
    test("Explanation has 'success' field", body.get("success") is True)
    test("Explanation has non-empty text", len(body.get("explanation", "")) > 50)
    if body.get("explanation"):
        print(f"       Preview: {body['explanation'][:100]}...")

    # ── Test 5: Unknown component (404)
    print("\n[ 5 ] 404 Handling")
    status, body = make_request(f"{base_url}/api/explain", method="POST",
                                data={"object_id": "laptop", "component_id": "unknown_part"})
    test("Unknown component returns 404", status == 404, f"Got: {status}")

    status, body = make_request(f"{base_url}/api/xray/nonexistent_object")
    test("Unknown object returns 404", status == 404, f"Got: {status}")

    # ── Test 6: X-Ray Endpoint
    print("\n[ 6 ] X-Ray Data (GET /api/xray/{object_id})")
    for obj_id in ["laptop", "fan", "phone", "bottle"]:
        status, body = make_request(f"{base_url}/api/xray/{obj_id}")
        test(f"GET /api/xray/{obj_id} returns 200", status == 200, f"Got: {status}")
        if status == 200:
            comp_count = len(body.get("components", []))
            test(f"  {obj_id} has components", comp_count > 0, f"{comp_count} components found")

    # ── Test 7: Sustainability
    print("\n[ 7 ] Sustainability Analysis (POST /api/xray/sustainability)")
    for obj_id in ["laptop", "fan"]:
        status, body = make_request(f"{base_url}/api/xray/sustainability", method="POST",
                                    data={"object_id": obj_id})
        test(f"Sustainability for {obj_id} returns 200", status == 200, f"Got: {status}")
        if status == 200:
            test(f"  {obj_id} has sustainability_score", "sustainability_score" in body)
            test(f"  {obj_id} has eco_tips", len(body.get("eco_tips", [])) > 0)

    # ── Test 8: Fan explanation
    print("\n[ 8 ] Fan Motor Explanation")
    status, body = make_request(f"{base_url}/api/explain", method="POST",
                                data={"object_id": "fan", "component_id": "motor"})
    test("Fan motor explanation returns 200", status == 200)
    test("Fan motor has explanation text", len(body.get("explanation", "")) > 50)

    # ─────────────────────────────────────────────
    # Summary
    # ─────────────────────────────────────────────
    print(f"\n{'='*60}")
    passed = sum(1 for _, s, _ in results if s == PASS)
    total = len(results)
    print(f"  Results: {passed}/{total} tests passed")
    if passed == total:
        print("  *** All tests passed! Backend is running correctly. ***")
    else:
        failed = [(n, d) for n, s, d in results if s == FAIL]
        print(f"  WARNING: {total - passed} test(s) failed:")
        for name, detail in failed:
            print(f"     - {name}: {detail}")
    print(f"{'='*60}\n")
    return passed == total


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="RealityOS Backend Tests")
    parser.add_argument("--base-url", default=DEFAULT_BASE_URL, help="Backend URL")
    args = parser.parse_args()

    success = run_all_tests(args.base_url)
    sys.exit(0 if success else 1)
