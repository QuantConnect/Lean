# Skills — Teaching Claude to Contribute to LEAN

This folder contains **Skills** — structured instruction sets that teach Claude how to perform complex, multi-file tasks following project-specific conventions. Instead of explaining requirements from scratch every time, you zip a skill folder and upload it once. Claude then follows those instructions automatically whenever it detects a matching request.

## What Is a Skill?

A skill is a folder containing:

- **`SKILL.md`** — The main instruction file. It tells Claude *when* to activate (trigger phrases), *what* to generate (files, structure, conventions), and *how* to do it (templates, checklists, coding style rules).
- **Template files** (`.template`, `.py`, etc.) — Scaffolding that Claude uses to produce consistent, high-quality output. These contain placeholders that Claude fills in based on your request.

Think of a skill as a "recipe card" for Claude: it knows the ingredients, the steps, and the plating — you just tell it what dish to make.

## Available Skills

| Skill | Folder | Purpose |
|---|---|---|
| **LEAN Indicator** | `Indicators/` | Generate all files needed to contribute a new technical indicator to the [QuantConnect/Lean](https://github.com/QuantConnect/Lean) repo — C# class, helper method, unit tests, test data, and documentation entry. |

## How to Install a Skill

Skills must be zipped and uploaded to Claude via the settings panel. Here's how:

### Step 1: Zip the Skill Folder

Select the contents of the skill folder (not the parent folder itself) and compress them into a `.zip` file. For example, to install the **Indicators** skill:

```
Skills/
└── Indicators/          ← Zip this folder
    ├── SKILL.md
    ├── bar_indicator.cs.template
    ├── datapoint_indicator.cs.template
    ├── tradebar_indicator.cs.template
    ├── window_indicator.cs.template
    ├── helper_method.cs.template
    ├── unit_tests.cs.template
    ├── doc_entry.py.template
    ├── generate_test_data.py
    └── README.md.template
```

You can zip from the command line:

```bash
cd Skills
zip -r lean-indicator.zip Indicators/
```

Or simply right-click the `Indicators` folder and choose **Compress** / **Send to → Compressed (zipped) folder** on your OS.

### Step 2: Upload to Claude

1. Open **Claude** (claude.ai or the Claude app).
2. Go to **Settings** (gear icon or profile menu).
3. Navigate to **Capabilities** → **Skills**.
4. Click **Add Skill** and upload your `.zip` file.
5. The skill is now active for all future conversations.

### Step 3: Use It

Once installed, simply ask Claude to perform the task the skill covers. Claude will automatically detect the intent and follow the skill's instructions. For the Indicators skill, try prompts like:

- *"Create a LEAN indicator for Chande Momentum Oscillator"*
- *"Implement VWAP as a new indicator for QuantConnect"*
- *"Contribute an RSI indicator to the Lean engine"*

Claude will generate the full set of contribution files — indicator class, helper method, unit tests, test data CSV, generation script, and documentation entry — all following LEAN's coding conventions and contribution guidelines.

## How the Indicators Skill Works

When activated, the Indicators skill walks through a structured workflow:

1. **Gather requirements** — Indicator name, abbreviation, parameters, formula, and type.
2. **Classify the indicator** — Determines whether it's a DataPoint, Bar, TradeBar, or Window-based indicator based on what data the formula needs.
3. **Generate files** using the appropriate templates:
   - `Indicators/<Name>.cs` — The indicator class (C#)
   - `Algorithm/QCAlgorithm.Indicators.<ABBR>.cs` — Helper method snippet
   - `Tests/Indicators/<Name>Tests.cs` — Unit tests
   - `Tests/TestData/<name>.csv` — Reference test data
   - `Scripts/generate_test_data.py` — Reproducible data generation script
   - `Documentation/indicator_image_generator_entry.py` — Doc registration
   - `README.md` — Summary with integration steps and PR description
4. **Validate** against a built-in checklist (license headers, naming conventions, `decimal` types, XML docs, etc.).
