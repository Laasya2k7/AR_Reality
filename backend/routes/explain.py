"""
POST /api/explain — Returns AI-generated educational explanation for a specific
object component using Gemini Text or Mock Fallback Mode.
"""

import logging
from fastapi import APIRouter, HTTPException
from pydantic import BaseModel

from services import gemini_service, object_service
from models.component_response import ComponentExplanationResponse

logger = logging.getLogger(__name__)

router = APIRouter()


class ExplainRequest(BaseModel):
    object_id: str
    component_id: str


@router.post("/explain", response_model=ComponentExplanationResponse, summary="Get AI explanation for a component")
async def explain_component(request: ExplainRequest):
    """
    Get an AI-generated educational explanation for a specific component.

    - Validates the object_id and component_id against the database.
    - Calls Gemini Text API to generate a rich, structured explanation.
    - Falls back to pre-written mock explanations if the API is unavailable.
    """
    object_id = request.object_id.strip().lower()
    component_id = request.component_id.strip().lower()

    # Validate object exists
    obj_data = object_service.get_object_data(object_id)
    if not obj_data:
        raise HTTPException(
            status_code=404,
            detail=f"Object '{object_id}' not found in the knowledge database."
        )

    # Validate component exists
    comp_data = object_service.get_component_data(object_id, component_id)
    if not comp_data:
        raise HTTPException(
            status_code=404,
            detail=f"Component '{component_id}' not found for object '{object_id}'."
        )

    object_name = obj_data["display_name"]
    component_name = comp_data["display_name"]
    additional_context = comp_data.get("short_description", "")

    logger.info(f"Explaining: {object_name} > {component_name}")

    explanation_text, is_mock = await gemini_service.explain_component(
        object_name=object_name,
        component_name=component_name,
        object_id=object_id,
        component_id=component_id,
        additional_context=additional_context
    )

    return ComponentExplanationResponse(
        success=True,
        mock_mode=is_mock,
        object_id=object_id,
        component_id=component_id,
        component_name=component_name,
        explanation=explanation_text
    )
