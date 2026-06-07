"""
Pydantic response models for component explanation API responses.
"""

from pydantic import BaseModel, Field
from typing import Optional


class ComponentExplanationResponse(BaseModel):
    """Full response from the POST /api/explain endpoint."""
    success: bool
    mock_mode: bool = Field(False, description="True if Gemini API was unavailable and mock data was used")
    object_id: str
    component_id: str
    component_name: str
    explanation: str = Field(..., description="Full AI-generated educational explanation text")
    error: Optional[str] = None


class SustainabilityResponse(BaseModel):
    """Sustainability analysis response."""
    success: bool
    mock_mode: bool = False
    object_id: str
    sustainability_score: int = Field(..., ge=1, le=10)
    carbon_footprint_summary: str
    recyclability_summary: str
    eco_tips: list[str]
    repairability_score: int = Field(..., ge=1, le=10)
    lifespan_years: int
    end_of_life_advice: str
    error: Optional[str] = None
