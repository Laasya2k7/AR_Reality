/// <summary>
/// ObjectData.cs — C# data model classes mapping JSON responses from the RealityOS backend.
/// These classes are used by JsonUtility.FromJson() for deserialization in Unity.
/// </summary>

using System;
using System.Collections.Generic;
using UnityEngine;

namespace RealityOS.Models
{
    // ─────────────────────────────────────────────────────────────
    // Component & Object Database Models
    // ─────────────────────────────────────────────────────────────

    [Serializable]
    public class PositionOffset
    {
        public float x;
        public float y;
        public float z;

        public Vector3 ToVector3() => new Vector3(x, y, z);
    }

    [Serializable]
    public class ComponentInfo
    {
        public string component_id;
        public string display_name;
        public string short_description;
        public string color_hex;
        public string model_prefab;
        public PositionOffset position_offset;
        public string wiki_url;

        /// <summary>Parse the hex color string into a Unity Color.</summary>
        public Color GetColor()
        {
            if (ColorUtility.TryParseHtmlString(color_hex, out Color c))
                return c;
            return Color.white;
        }
    }

    [Serializable]
    public class ObjectMetadata
    {
        public string object_id;
        public string display_name;
        public string description;
        public string functionality;
        public string[] applications;
        public string[] interesting_facts;
        public string model_prefab;
        public int sustainability_rating;
        public string recyclability;
        public float co2_footprint_kg;
        public ComponentInfo[] components;
        public float confidence;
        public string detection_notes;
    }

    // ─────────────────────────────────────────────────────────────
    // API Response Wrappers
    // ─────────────────────────────────────────────────────────────

    [Serializable]
    public class DetectResponse
    {
        public bool success;
        public bool mock_mode;
        public ObjectMetadata object_data;
        public string error;
    }

    [Serializable]
    public class ComponentExplanationResponse
    {
        public bool success;
        public bool mock_mode;
        public string object_id;
        public string component_id;
        public string component_name;
        public string explanation;
        public string error;
    }

    [Serializable]
    public class XRayResponse
    {
        public bool success;
        public string object_id;
        public string display_name;
        public ComponentInfo[] components;
        public string model_prefab;
    }

    [Serializable]
    public class SustainabilityResponse
    {
        public bool success;
        public bool mock_mode;
        public string object_id;
        public int sustainability_score;
        public string carbon_footprint_summary;
        public string recyclability_summary;
        public string[] eco_tips;
        public int repairability_score;
        public int lifespan_years;
        public string end_of_life_advice;
        public string error;
    }

    [Serializable]
    public class HealthResponse
    {
        public string status;
        public bool gemini_text_available;
        public bool gemini_vision_available;
        public bool mock_mode_active;
        public string[] supported_objects;
    }
}
