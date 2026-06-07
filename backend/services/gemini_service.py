"""
Gemini Service: Handles Gemini Text model interactions for component explanations
and sustainability analysis. Includes Mock Fallback Mode for missing API keys.
"""

import os
import logging
import re
import json
from pathlib import Path
from typing import Optional

logger = logging.getLogger(__name__)

# ─────────────────────────────────────────────
# Determine Gemini availability at import time
# ─────────────────────────────────────────────
_GEMINI_AVAILABLE = False
_genai = None

try:
    import google.generativeai as genai
    api_key = os.getenv("GEMINI_API_KEY", "").strip()
    if api_key and api_key != "your_gemini_api_key_here":
        genai.configure(api_key=api_key)
        _genai = genai
        _GEMINI_AVAILABLE = True
        logger.info("Gemini API configured successfully.")
    else:
        logger.warning("GEMINI_API_KEY not set or is placeholder. Using Mock Fallback Mode.")
except ImportError:
    logger.warning("google-generativeai package not available. Using Mock Fallback Mode.")
except Exception as e:
    logger.warning(f"Gemini configuration failed: {e}. Using Mock Fallback Mode.")


# ─────────────────────────────────────────────
# Prompt Loader
# ─────────────────────────────────────────────
_PROMPTS_DIR = Path(__file__).parent.parent / "prompts"


def _load_prompt(filename: str) -> str:
    """Load a prompt template from the prompts directory."""
    filepath = _PROMPTS_DIR / filename
    try:
        return filepath.read_text(encoding="utf-8")
    except FileNotFoundError:
        logger.error(f"Prompt file not found: {filepath}")
        return ""


# ─────────────────────────────────────────────
# Mock Response Data
# ─────────────────────────────────────────────
_MOCK_EXPLANATIONS = {
    ("laptop", "cpu"): (
        "What is it?: The CPU (Central Processing Unit) is the primary chip that carries out the instructions of a "
        "computer program. It acts as the brain of the laptop, performing all arithmetic, logic, and control operations.\n\n"
        "How does it work?: The CPU fetches instructions from RAM, decodes what each instruction means, executes the "
        "operation (such as adding numbers or moving data), and writes results back. Modern CPUs do this billions of "
        "times per second across multiple cores simultaneously.\n\n"
        "Why is it important?: Without a CPU, the laptop is simply an inert collection of metal and plastic. Every "
        "keystroke, web page load, and video playback depends entirely on the CPU. If it fails, the computer cannot "
        "boot at all.\n\n"
        "Fun Fact: A modern laptop CPU with 10 billion transistors switches between on and off states over 3 trillion "
        "times per second — faster than you blink by a factor of 30 million."
    ),
    ("laptop", "ram"): (
        "What is it?: RAM (Random Access Memory) is the laptop's short-term working memory. It stores data that the "
        "CPU is actively using, allowing rapid access without reading from the slower storage drive.\n\n"
        "How does it work?: RAM uses capacitors and transistors to store binary data as electrical charges. Unlike "
        "storage, it loses all data when power is removed, which is why it's called 'volatile' memory.\n\n"
        "Why is it important?: More RAM allows you to run more applications simultaneously. Without enough RAM, the "
        "system swaps data to the slower SSD, causing noticeable slowdowns.\n\n"
        "Fun Fact: If a laptop's RAM were a desk, the SSD would be a filing cabinet in another building, and the "
        "hard drive would be a warehouse across town."
    ),
    ("fan", "motor"): (
        "What is it?: The electric motor is the core of the fan, converting electrical energy into rotational "
        "mechanical energy through electromagnetic induction.\n\n"
        "How does it work?: When AC current flows through the stator windings, it creates a rotating magnetic field. "
        "This induces current in the rotor, which generates its own magnetic field that chases the stator field, "
        "causing the rotor shaft to spin.\n\n"
        "Why is it important?: The motor is the sole source of motion in the fan. Without it, the blades cannot spin "
        "and no airflow is created.\n\n"
        "Fun Fact: BLDC (Brushless DC) fan motors can last over 50,000 hours — that's nearly 6 years of "
        "continuous operation."
    ),
    ("phone", "processor"): (
        "What is it?: The SoC (System on Chip) is a single integrated circuit containing the CPU, GPU, NPU (neural "
        "processing unit), modem, and memory controller — essentially the entire computing brain of the smartphone.\n\n"
        "How does it work?: Billions of transistors etched at 3–5 nanometer process nodes switch states to perform "
        "operations. The NPU accelerates AI tasks like face recognition and photography enhancement without draining "
        "the battery.\n\n"
        "Why is it important?: Every touch, app, photo, and call is processed by the SoC. It determines the device's "
        "performance, battery life, and AI capabilities.\n\n"
        "Fun Fact: The transistors in a modern smartphone SoC are just 3 nanometers wide — about 20,000 times thinner "
        "than a human hair."
    ),
}

_MOCK_SUSTAINABILITY = {
    "laptop": {
        "sustainability_score": 6,
        "carbon_footprint_summary": (
            "Manufacturing a laptop produces approximately 300-400 kg of CO2 equivalent, with chip fabrication "
            "accounting for over 50% of that impact. The operational carbon depends heavily on your electricity "
            "source — renewable energy can reduce lifetime emissions by 80%."
        ),
        "recyclability_summary": (
            "Laptops contain valuable and recyclable materials including aluminum, copper, and gold. "
            "However, the complex assembly makes manual disassembly difficult. Certified e-waste recyclers "
            "can recover up to 80% of materials by weight."
        ),
        "eco_tips": [
            "Enable sleep/hibernate mode aggressively to reduce idle power consumption",
            "Extend your laptop's lifespan by replacing the battery rather than buying a new device",
            "Recycle through certified e-waste programs (e.g., manufacturer take-back or municipal e-waste centers)"
        ],
        "repairability_score": 5,
        "lifespan_years": 5,
        "end_of_life_advice": (
            "Never dispose of laptops in regular trash due to hazardous materials. "
            "Contact your manufacturer's take-back program or find a certified e-waste recycler."
        )
    },
    "fan": {
        "sustainability_score": 8,
        "carbon_footprint_summary": (
            "Electric fans have a very low manufacturing footprint of approximately 15-20 kg CO2 equivalent. "
            "Their operational impact is minimal at 30-75W consumption, making them far more efficient "
            "than air conditioning units."
        ),
        "recyclability_summary": (
            "Fans are primarily composed of steel, aluminum, and copper — all highly recyclable metals. "
            "The motor windings and blades can be separated and processed at most metal recycling facilities."
        ),
        "eco_tips": [
            "Use a ceiling fan instead of air conditioning to reduce cooling energy by up to 40%",
            "Clean fan blades regularly to maintain aerodynamic efficiency",
            "Choose ENERGY STAR certified fans with BLDC motors for 60-70% energy savings"
        ],
        "repairability_score": 9,
        "lifespan_years": 15,
        "end_of_life_advice": (
            "Electric fans can be donated if functional, or taken to a metal scrap recycler. "
            "Separate the plastic components from metal parts for best recyclability outcomes."
        )
    },
    "phone": {
        "sustainability_score": 4,
        "carbon_footprint_summary": (
            "A smartphone's carbon footprint is approximately 60-80 kg CO2 equivalent, with over 80% "
            "generated during manufacturing. The rare earth elements in displays and camera sensors "
            "require energy-intensive mining."
        ),
        "recyclability_summary": (
            "Smartphone recycling is challenging due to miniaturized, bonded components and rare earth elements. "
            "Only about 20% of smartphones are properly recycled globally, leaving enormous resource recovery "
            "potential untapped."
        ),
        "eco_tips": [
            "Keep your smartphone for at least 3-4 years to amortize the manufacturing carbon footprint",
            "Trade in or donate your old phone rather than discarding it",
            "Use a protective case to prevent accidental damage that shortens device lifespan"
        ],
        "repairability_score": 4,
        "lifespan_years": 4,
        "end_of_life_advice": (
            "Use manufacturer or carrier trade-in programs, or certified e-waste recyclers. "
            "Erase all personal data before recycling and remove the SIM card."
        )
    },
    "bottle": {
        "sustainability_score": 3,
        "carbon_footprint_summary": (
            "Single-use PET plastic bottles generate approximately 80-100g CO2 per bottle during production. "
            "At scale, the global production of 500 billion bottles per year releases hundreds of millions of "
            "tons of CO2 annually."
        ),
        "recyclability_summary": (
            "PET plastic (Type 1) is technically highly recyclable and accepted by most municipal programs. "
            "However, global effective recycling rates remain below 30% due to contamination and "
            "lack of collection infrastructure."
        ),
        "eco_tips": [
            "Switch to a reusable stainless steel or glass bottle to eliminate single-use plastic entirely",
            "Ensure bottle is clean and cap-free before placing in recycling bin",
            "Choose brands using recycled PET (rPET) content to close the loop"
        ],
        "repairability_score": 1,
        "lifespan_years": 1,
        "end_of_life_advice": (
            "Rinse the bottle, remove the cap (caps may need separate recycling), crush to save space, "
            "and place in your recycling bin. Check local guidelines for cap recycling."
        )
    }
}


# ─────────────────────────────────────────────
# Public API
# ─────────────────────────────────────────────

def is_mock_mode() -> bool:
    """Return True if the system is running in Mock Fallback Mode."""
    return not _GEMINI_AVAILABLE


async def explain_component(
    object_name: str,
    component_name: str,
    object_id: str,
    component_id: str,
    additional_context: str = ""
) -> tuple[str, bool]:
    """
    Generate an AI explanation for a component.

    Returns:
        A tuple of (explanation_text, is_mock_mode).
    """
    if not _GEMINI_AVAILABLE:
        # Return mock data if available, otherwise return a generic explanation
        mock_key = (object_id, component_id)
        mock_text = _MOCK_EXPLANATIONS.get(mock_key)
        if not mock_text:
            mock_text = (
                f"What is it?: The {component_name} is a key component of the {object_name}.\n\n"
                f"How does it work?: It performs its specific function as part of the overall system.\n\n"
                f"Why is it important?: Without the {component_name}, the {object_name} would not function correctly.\n\n"
                f"Fun Fact: The engineering behind the {component_name} has evolved significantly over the past decade."
            )
        return mock_text, True

    try:
        prompt_template = _load_prompt("explain_prompt.txt")
        prompt = prompt_template.format(
            object_name=object_name,
            component_name=component_name,
            additional_context=additional_context or f"Standard {component_name} in a {object_name}"
        )

        model = _genai.GenerativeModel("gemini-1.5-flash")
        response = model.generate_content(prompt)
        explanation = response.text.strip()
        return explanation, False

    except Exception as e:
        logger.error(f"Gemini explain_component failed: {e}. Falling back to mock.")
        mock_key = (object_id, component_id)
        mock_text = _MOCK_EXPLANATIONS.get(
            mock_key,
            f"The {component_name} is an essential component of the {object_name} that enables its core functionality."
        )
        return mock_text, True


async def get_sustainability_analysis(object_id: str, object_name: str) -> tuple[dict, bool]:
    """
    Generate sustainability analysis for an object.

    Returns:
        A tuple of (sustainability_dict, is_mock_mode).
    """
    if not _GEMINI_AVAILABLE:
        mock_data = _MOCK_SUSTAINABILITY.get(object_id, {
            "sustainability_score": 5,
            "carbon_footprint_summary": f"The {object_name} has a moderate environmental impact.",
            "recyclability_summary": f"The {object_name} contains materials that can be partially recycled.",
            "eco_tips": [
                "Extend product lifespan by maintaining it regularly",
                "Recycle through certified facilities",
                "Consider buying refurbished products"
            ],
            "repairability_score": 5,
            "lifespan_years": 5,
            "end_of_life_advice": "Consult local e-waste or recycling regulations for proper disposal."
        })
        return mock_data, True

    try:
        prompt_template = _load_prompt("sustainability_prompt.txt")
        prompt = prompt_template.format(object_name=object_name)

        model = _genai.GenerativeModel("gemini-1.5-flash")
        response = model.generate_content(prompt)
        text = response.text.strip()

        # Strip markdown code fences if present
        text = re.sub(r"```(?:json)?\s*", "", text).strip().rstrip("```").strip()
        result = json.loads(text)
        return result, False

    except Exception as e:
        logger.error(f"Gemini sustainability analysis failed: {e}. Falling back to mock.")
        mock_data = _MOCK_SUSTAINABILITY.get(object_id, {
            "sustainability_score": 5,
            "carbon_footprint_summary": f"The {object_name} has a moderate environmental impact.",
            "recyclability_summary": "Contains partially recyclable materials.",
            "eco_tips": ["Extend product lifespan", "Recycle through certified facilities"],
            "repairability_score": 5,
            "lifespan_years": 5,
            "end_of_life_advice": "Consult local regulations for disposal."
        })
        return mock_data, True
