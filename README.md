# Siemens Train Maintenance VR - Proof of Concept

## Overview
This repository contains a Unity XR prototype for a virtual technical training scenario for Siemens train teams.

The goal is to demonstrate that maintenance training can be delivered remotely in VR, reducing the need to move technicians to physical training locations.

## Project Status
This is a **proof of concept (POC)**, not a final production training product.

Some parts of the project come from a previous template and may look disconnected. Cleanup and standardization are planned as development continues.

## Training Scenario (Current Concept)
- A train continuously moves through **3 stations** in a loop.
- The trainee must assemble required parts/components in the virtual environment.
- The train should only stop when the required assembly is completed correctly.

## Why This Is a “Dummy” Process
We currently do not have access to Siemens internal maintenance documentation.  
Because of that, the interaction flow and assembly logic are simplified to validate:
- technical feasibility,
- interaction quality,
- and training format potential.

## Primary Objective of This POC
Show Siemens stakeholders that:
1. VR-based technician training is feasible.
2. Core maintenance-like workflows can be represented interactively.
3. Progress/validation logic (assemble parts -> stop train) can be tracked in real time.

## Technical Context
- Engine: **Unity**
- Focus: **XR / OpenXR interaction**
- Current codebase includes reused template systems and custom scripts under `Assets/Script/`.

## Current Gameplay Logic Direction
- Use snap/placement validation for parts (example: `ChipSnapZone`-style behavior).
- Mark required assembly steps as complete.
- Trigger train stop once all required conditions are met.

## Team Notes
- Repository language standard: **English** (code, docs, comments, commit messages preferred in English).
- Internal discussion can happen in Portuguese, but project artifacts should remain in English for team consistency.

## AI Collaboration Notes
When assisting in this repository, AI agents should:
1. Preserve the core POC goal (remote VR maintenance training demonstration).
2. Prefer practical, incremental improvements over large rewrites.
3. Avoid over-engineering production systems unless explicitly requested.
4. Flag template leftovers but do not block delivery because of them.
5. Keep gameplay logic easy to explain in demos.

## Non-Goals (for now)
- Full Siemens-realistic maintenance procedures.
- Final UX polish.
- Production-grade architecture.
- Full analytics/LMS integration.

## Next Milestones (Suggested)
1. Finalize part list and completion criteria.
2. Stabilize snap/validation flow.
3. Integrate train-stop trigger with clear visual/audio feedback.
4. Prepare a guided demo path for stakeholders.
