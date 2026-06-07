"""
Object Service: Loads object and component definitions from JSON database files.
"""

import json
import os
import logging
from typing import Optional, Dict, Any

logger = logging.getLogger(__name__)

# Path to database directory
_DB_DIR = os.path.join(os.path.dirname(__file__), "..", "database")

# Supported object IDs mapped to their JSON file names
SUPPORTED_OBJECTS = {
    "laptop": "laptop.json",
    "fan": "fan.json",
    "phone": "phone.json",
    "bottle": "bottle.json",
}

# In-memory cache to avoid repeated disk reads
_cache: Dict[str, Dict[str, Any]] = {}


def _load_object_json(object_id: str) -> Optional[Dict[str, Any]]:
    """Load and cache an object's JSON database entry."""
    if object_id in _cache:
        return _cache[object_id]

    filename = SUPPORTED_OBJECTS.get(object_id)
    if not filename:
        logger.warning(f"No database file registered for object_id: {object_id}")
        return None

    filepath = os.path.join(_DB_DIR, filename)
    try:
        with open(filepath, "r", encoding="utf-8") as f:
            data = json.load(f)
        _cache[object_id] = data
        logger.info(f"Loaded database for object: {object_id}")
        return data
    except FileNotFoundError:
        logger.error(f"Database file not found: {filepath}")
        return None
    except json.JSONDecodeError as e:
        logger.error(f"JSON decode error in {filepath}: {e}")
        return None


def get_object_data(object_id: str) -> Optional[Dict[str, Any]]:
    """
    Retrieve full object data from the JSON database.

    Args:
        object_id: The object identifier (e.g., 'laptop', 'fan').

    Returns:
        A dictionary with all object metadata, or None if not found.
    """
    return _load_object_json(object_id)


def get_component_data(object_id: str, component_id: str) -> Optional[Dict[str, Any]]:
    """
    Retrieve a specific component's metadata from an object.

    Args:
        object_id: The object identifier.
        component_id: The component identifier (e.g., 'cpu', 'motor').

    Returns:
        A dictionary with component metadata, or None if not found.
    """
    obj_data = _load_object_json(object_id)
    if not obj_data:
        return None

    components = obj_data.get("components", [])
    for component in components:
        if component.get("component_id") == component_id:
            return component

    logger.warning(f"Component '{component_id}' not found in object '{object_id}'")
    return None


def list_supported_objects() -> list[str]:
    """Return the list of all supported object IDs."""
    return list(SUPPORTED_OBJECTS.keys())
