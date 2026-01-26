# AGENTS.md

This file contains rules and context for AI Agents working on this repository.

## Project Context
*   **Platform**: Virt-A-Mate (VaM). This is NOT standard Unity.
    *   **NO** `transform.parent` for hierarchy checks. Use `Atom` hierarchy or `linkToRB`.
    *   **NO** `Debug.DrawRay`. Use `LineRenderer` with `Hidden/Internal-Colored` shader (ZTest Always) for on-screen debug.
    *   **NO** Standard physics layers (mostly). Use `SuperController` APIs where possible.
*   **Repo Structure**: `Custom/Scripts/FivelSystems/ProximityGrab/ProximityGrab.cs` is the main file.
*   **License**: CC BY-SA 4.0 (Due to derivation from Kimowal's work).

## Documentation Rules
1.  **README.md**:
    *   Must be in standard Markdown.
    *   Must include a `<details>` section containing the **BBCode** version of the documentation (for VaM Hub).
    *   Must credit: **Kimowal** (PhysicsAttachmentEngine) and **Skynet** (Rigify/Visuals).
2.  **Versioning**: 
    *   **Tag Format**: MUST be `v` followed by a single integer (e.g., `v1`, `v2`, `v3`).
    *   **NO** SemVer (e.g., `v1.0.1` is forbidden).
    *   **Reason**: The workflow strips the `v` and uses the number directly compatibility with VaM's integer-based package system.
    *   Update `pluginLabelJSON.val` in code to match.

## VaM Specific API Notes
*   **Parenting**: Atoms are "linked" via `mainController.linkToRB`.
*   **Input**: Use `JSONStorable` classes (`JSONStorableFloat`, `JSONStorableAction`, etc.) for all UI and automation.
*   **Logging**: Use `SuperController.LogMessage()` and `SuperController.LogError()`.
