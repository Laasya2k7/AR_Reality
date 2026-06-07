"""
RealityOS FastAPI Backend
Entry point: sets up the server, CORS middleware, and registers all API routers.
"""

import os
import logging
from fastapi import FastAPI
from fastapi.middleware.cors import CORSMiddleware
from dotenv import load_dotenv

# Load environment variables from .env file BEFORE any service imports
load_dotenv()

from routes import detect, explain, xray

# ─────────────────────────────────────────────
# Logging Configuration
# ─────────────────────────────────────────────
logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(name)s: %(message)s",
    datefmt="%Y-%m-%d %H:%M:%S"
)
logger = logging.getLogger(__name__)

# ─────────────────────────────────────────────
# FastAPI App Initialization
# ─────────────────────────────────────────────
app = FastAPI(
    title="RealityOS API",
    description=(
        "AI-powered Augmented Reality backend for RealityOS. "
        "Provides object detection via Gemini Vision, component explanations via Gemini AI, "
        "and X-Ray component metadata for Unity AR visualization."
    ),
    version="1.0.0",
    contact={
        "name": "RealityOS Team",
        "url": "https://github.com/realityos"
    },
    license_info={
        "name": "MIT"
    }
)

# ─────────────────────────────────────────────
# CORS Middleware — Required for Unity WebRequest
# ─────────────────────────────────────────────
app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],          # In production, restrict to your Unity app's domain
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)

# ─────────────────────────────────────────────
# Router Registration
# ─────────────────────────────────────────────
app.include_router(detect.router, prefix="/api", tags=["Detection"])
app.include_router(explain.router, prefix="/api", tags=["Explanation"])
app.include_router(xray.router, prefix="/api", tags=["X-Ray"])


# ─────────────────────────────────────────────
# Health & Root Endpoints
# ─────────────────────────────────────────────
@app.get("/", tags=["Health"])
async def root():
    """Root endpoint — confirms the API is running."""
    return {
        "status": "ok",
        "service": "RealityOS API",
        "version": "1.0.0",
        "docs": "/docs"
    }


@app.get("/health", tags=["Health"])
async def health_check():
    """Health check endpoint for monitoring and deployment verification."""
    from services import gemini_service, vision_service
    return {
        "status": "healthy",
        "gemini_text_available": not gemini_service.is_mock_mode(),
        "gemini_vision_available": not vision_service.is_mock_mode(),
        "mock_mode_active": gemini_service.is_mock_mode(),
        "supported_objects": ["laptop", "fan", "phone", "bottle"]
    }


# ─────────────────────────────────────────────
# Entry Point
# ─────────────────────────────────────────────
if __name__ == "__main__":
    import uvicorn
    port = int(os.getenv("PORT", 8000))
    logger.info(f"Starting RealityOS API on port {port}")
    uvicorn.run("app:app", host="0.0.0.0", port=port, reload=True, log_level="info")
