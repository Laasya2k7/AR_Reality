"""
POST /api/detect — Accepts an image upload, runs Gemini Vision detection,
enriches with database metadata, and returns full object information.
"""

import logging
from fastapi import APIRouter, UploadFile, File, HTTPException

from services import vision_service, object_service
from models.object_response import DetectResponse, ObjectMetadata, ComponentInfo, PositionOffset

logger = logging.getLogger(__name__)

router = APIRouter()


@router.post("/detect", response_model=DetectResponse, summary="Detect object from uploaded image")
async def detect_object(file: UploadFile = File(..., description="Camera image to analyze")):
    """
    Upload a camera image and receive full object detection results.

    - Sends the image to Gemini Vision (or returns mock data if unavailable).
    - Enriches the detection with component metadata from the JSON database.
    - Returns the complete object information for AR visualization.
    """
    # Validate file type
    allowed_types = {"image/jpeg", "image/jpg", "image/png", "image/webp"}
    content_type = file.content_type or "image/jpeg"
    if content_type not in allowed_types:
        raise HTTPException(
            status_code=415,
            detail=f"Unsupported image type '{content_type}'. Supported: {allowed_types}"
        )

    try:
        image_bytes = await file.read()
        if not image_bytes:
            raise HTTPException(status_code=400, detail="Uploaded file is empty.")

        logger.info(f"Received image: {file.filename} ({len(image_bytes)} bytes, type={content_type})")
    except Exception as e:
        logger.error(f"Failed to read uploaded file: {e}")
        raise HTTPException(status_code=400, detail="Could not read uploaded image.")

    # Run vision detection
    detection_result, is_mock = await vision_service.detect_object_from_image(image_bytes, content_type)

    detected_id = detection_result.get("object_id", "unknown")
    confidence = detection_result.get("confidence", 0.0)
    detection_notes = detection_result.get("detection_notes", "")

    if detected_id == "unknown" or confidence < 0.5:
        logger.warning(f"Low confidence detection: {detected_id} at {confidence}")
        return DetectResponse(
            success=False,
            mock_mode=is_mock,
            object_data=None,
            error=f"Could not confidently identify an object (confidence={confidence:.2f}). Please try a clearer image."
        )

    # Enrich with database metadata
    db_data = object_service.get_object_data(detected_id)
    if not db_data:
        logger.error(f"No database entry found for detected object: {detected_id}")
        return DetectResponse(
            success=False,
            mock_mode=is_mock,
            object_data=None,
            error=f"Object '{detected_id}' detected but not found in the knowledge database."
        )

    # Build components list
    components = []
    for comp in db_data.get("components", []):
        offset_raw = comp.get("position_offset", {})
        offset = PositionOffset(
            x=offset_raw.get("x", 0.0),
            y=offset_raw.get("y", 0.0),
            z=offset_raw.get("z", 0.0)
        )
        components.append(ComponentInfo(
            component_id=comp["component_id"],
            display_name=comp["display_name"],
            short_description=comp["short_description"],
            color_hex=comp["color_hex"],
            model_prefab=comp["model_prefab"],
            position_offset=offset,
            wiki_url=comp.get("wiki_url")
        ))

    object_metadata = ObjectMetadata(
        object_id=db_data["object_id"],
        display_name=db_data["display_name"],
        description=db_data["description"],
        functionality=db_data["functionality"],
        applications=db_data["applications"],
        interesting_facts=db_data["interesting_facts"],
        model_prefab=db_data["model_prefab"],
        sustainability_rating=db_data["sustainability_rating"],
        recyclability=db_data["recyclability"],
        co2_footprint_kg=db_data["co2_footprint_kg"],
        components=components,
        confidence=confidence,
        detection_notes=detection_notes
    )

    logger.info(f"Returning full metadata for: {detected_id} (mock={is_mock})")
    return DetectResponse(
        success=True,
        mock_mode=is_mock,
        object_data=object_metadata
    )
