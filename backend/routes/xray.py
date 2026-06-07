"""
GET /api/xray/{object_id} — Returns X-Ray component details for a specific object.
POST /api/xray/sustainability — Returns sustainability analysis for an object.
"""

import logging
from fastapi import APIRouter, HTTPException
from pydantic import BaseModel

from services import object_service, gemini_service
from models.object_response import ComponentInfo, PositionOffset
from models.component_response import SustainabilityResponse

logger = logging.getLogger(__name__)

router = APIRouter()


class XRayResponse(BaseModel):
    success: bool
    object_id: str
    display_name: str
    components: list[ComponentInfo]
    model_prefab: str


class SustainabilityRequest(BaseModel):
    object_id: str


@router.get("/xray/{object_id}", response_model=XRayResponse, summary="Get X-Ray component list for an object")
async def get_xray(object_id: str):
    """
    Retrieve X-Ray component details for a known object.

    Returns a list of components with their 3D model prefab names, colors,
    and AR positioning offsets for Unity to render the exploded X-Ray view.
    """
    object_id = object_id.strip().lower()

    db_data = object_service.get_object_data(object_id)
    if not db_data:
        raise HTTPException(
            status_code=404,
            detail=f"Object '{object_id}' not found. Supported objects: {object_service.list_supported_objects()}"
        )

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

    logger.info(f"X-Ray request: {object_id} ({len(components)} components)")
    return XRayResponse(
        success=True,
        object_id=object_id,
        display_name=db_data["display_name"],
        components=components,
        model_prefab=db_data["model_prefab"]
    )


@router.post("/xray/sustainability", response_model=SustainabilityResponse, summary="Get sustainability analysis")
async def get_sustainability(request: SustainabilityRequest):
    """
    Generate a sustainability and environmental impact analysis for an object.
    Uses Gemini AI or falls back to pre-computed mock data.
    """
    object_id = request.object_id.strip().lower()

    db_data = object_service.get_object_data(object_id)
    if not db_data:
        raise HTTPException(
            status_code=404,
            detail=f"Object '{object_id}' not found."
        )

    object_name = db_data["display_name"]
    analysis, is_mock = await gemini_service.get_sustainability_analysis(object_id, object_name)

    return SustainabilityResponse(
        success=True,
        mock_mode=is_mock,
        object_id=object_id,
        sustainability_score=analysis.get("sustainability_score", 5),
        carbon_footprint_summary=analysis.get("carbon_footprint_summary", ""),
        recyclability_summary=analysis.get("recyclability_summary", ""),
        eco_tips=analysis.get("eco_tips", []),
        repairability_score=analysis.get("repairability_score", 5),
        lifespan_years=analysis.get("lifespan_years", 5),
        end_of_life_advice=analysis.get("end_of_life_advice", "")
    )
