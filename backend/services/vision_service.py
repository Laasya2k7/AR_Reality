"""
Vision Service: Uses Gemini Vision API to analyze camera images and detect objects.
Includes Mock Fallback Mode for development without a valid API key.
"""

import os
import re
import json
import logging
import base64
import random
from typing import Optional

logger = logging.getLogger(__name__)

# ─────────────────────────────────────────────
# Gemini Vision availability check
# ─────────────────────────────────────────────
_VISION_AVAILABLE = False
_genai = None

try:
    import google.generativeai as genai
    api_key = os.getenv("GEMINI_API_KEY", "").strip()
    if api_key and api_key != "your_gemini_api_key_here":
        genai.configure(api_key=api_key)
        _genai = genai
        _VISION_AVAILABLE = True
        logger.info("Gemini Vision API configured successfully.")
    else:
        logger.warning("No valid GEMINI_API_KEY found. Vision Service running in Mock Fallback Mode.")
except ImportError:
    logger.warning("google-generativeai not installed. Vision running in Mock Fallback Mode.")
except Exception as e:
    logger.warning(f"Vision API configuration failed: {e}. Using Mock Fallback Mode.")


# ─────────────────────────────────────────────
# Mock detection data (cycling for demo)
# ─────────────────────────────────────────────
_MOCK_DETECTIONS = [
    {
        "object_id": "laptop",
        "confidence": 0.95,
        "display_name": "Laptop",
        "detection_notes": "A laptop computer is clearly visible with the lid open showing the keyboard and display."
    },
    {
        "object_id": "phone",
        "confidence": 0.92,
        "display_name": "Smartphone",
        "detection_notes": "A modern smartphone is visible in the frame with a dark glass display face up."
    },
    {
        "object_id": "fan",
        "confidence": 0.88,
        "display_name": "Electric Fan",
        "detection_notes": "An electric desk fan with visible rotating blades and a cylindrical motor housing."
    },
    {
        "object_id": "bottle",
        "confidence": 0.90,
        "display_name": "Plastic Water Bottle",
        "detection_notes": "A clear plastic water bottle with a white screw cap is visible in the frame."
    }
]

_mock_index = 0


def is_mock_mode() -> bool:
    """Return True if Vision Service is in Mock Fallback Mode."""
    return not _VISION_AVAILABLE


async def detect_object_from_image(image_bytes: bytes, mime_type: str = "image/jpeg") -> tuple[dict, bool]:
    """
    Analyze an image and return the detected object classification.

    Args:
        image_bytes: Raw image data as bytes.
        mime_type: MIME type of the image (e.g., 'image/jpeg', 'image/png').

    Returns:
        A tuple of (detection_result_dict, is_mock_mode).
        detection_result_dict has keys: object_id, confidence, display_name, detection_notes
    """
    global _mock_index

    if not _VISION_AVAILABLE:
        # Return cycling mock detections for demo purposes
        result = _MOCK_DETECTIONS[_mock_index % len(_MOCK_DETECTIONS)]
        _mock_index += 1
        logger.info(f"Mock Vision returning: {result['object_id']} (confidence={result['confidence']})")
        return result, True

    try:
        from pathlib import Path
        prompts_dir = Path(__file__).parent.parent / "prompts"
        xray_prompt_path = prompts_dir / "xray_prompt.txt"
        vision_prompt = xray_prompt_path.read_text(encoding="utf-8")

        # Prepare image for Gemini Vision
        image_part = {
            "mime_type": mime_type,
            "data": image_bytes
        }

        model = _genai.GenerativeModel("gemini-1.5-flash")
        response = model.generate_content([vision_prompt, image_part])
        raw_text = response.text.strip()

        # Strip markdown code fences
        raw_text = re.sub(r"```(?:json)?\s*", "", raw_text).strip().rstrip("```").strip()
        result = json.loads(raw_text)

        # Validate required fields
        required_keys = {"object_id", "confidence", "display_name", "detection_notes"}
        if not required_keys.issubset(result.keys()):
            raise ValueError(f"Gemini response missing required fields: {result}")

        logger.info(f"Gemini Vision detected: {result['object_id']} (confidence={result['confidence']})")
        return result, False

    except json.JSONDecodeError as e:
        logger.error(f"Failed to parse Gemini Vision JSON response: {e}")
        # Fallback to mock on parse error
        result = _MOCK_DETECTIONS[0]
        return result, True
    except Exception as e:
        logger.error(f"Gemini Vision detection failed: {e}. Falling back to mock.")
        result = _MOCK_DETECTIONS[_mock_index % len(_MOCK_DETECTIONS)]
        _mock_index += 1
        return result, True
