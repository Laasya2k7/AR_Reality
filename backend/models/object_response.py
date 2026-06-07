"""
Pydantic response models for object detection and metadata API responses.
"""

from pydantic import BaseModel, Field
from typing import List, Optional


class PositionOffset(BaseModel):
    """3D offset for AR component placement."""
    x: float = 0.0
    y: float = 0.0
    z: float = 0.0


class ComponentInfo(BaseModel):
    """Metadata for a single component within a detected object."""
    component_id: str = Field(..., description="Unique identifier for the component")
    display_name: str = Field(..., description="Human-readable component name")
    short_description: str = Field(..., description="Brief one-line description")
    color_hex: str = Field(..., description="Hex color code for AR highlighting")
    model_prefab: str = Field(..., description="Unity prefab name for 3D model")
    position_offset: PositionOffset = Field(
        default_factory=PositionOffset,
        description="World-space offset for AR positioning"
    )
    wiki_url: Optional[str] = Field(None, description="Wikipedia link for further reading")


class ObjectDetectionResult(BaseModel):
    """Raw detection result from Gemini Vision."""
    object_id: str = Field(..., description="Detected object identifier")
    confidence: float = Field(..., ge=0.0, le=1.0, description="Detection confidence 0.0-1.0")
    display_name: str = Field(..., description="Human-readable object name")
    detection_notes: str = Field(..., description="What Gemini observed in the image")


class ObjectMetadata(BaseModel):
    """Full metadata response for a detected object, combining DB data and detection info."""
    object_id: str
    display_name: str
    description: str
    functionality: str
    applications: List[str]
    interesting_facts: List[str]
    model_prefab: str
    sustainability_rating: int = Field(..., ge=1, le=10)
    recyclability: str
    co2_footprint_kg: float
    components: List[ComponentInfo]
    confidence: float = Field(0.0, ge=0.0, le=1.0)
    detection_notes: str = ""


class DetectResponse(BaseModel):
    """Full response from the POST /api/detect endpoint."""
    success: bool
    mock_mode: bool = Field(False, description="True if Gemini API was unavailable and mock data was used")
    object_data: Optional[ObjectMetadata] = None
    error: Optional[str] = None
