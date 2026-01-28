# AGENTS.md

This file contains rules and context for AI Agents working on this repository.

## Project Context
*   **Platform**: Virt-A-Mate (VaM). This is NOT standard Unity.
    *   **NO** `transform.parent` for hierarchy checks. Use `Atom` hierarchy or `linkToRB`.
    *   **NO** `Debug.DrawRay`. Use `LineRenderer` with `Hidden/Internal-Colored` shader (ZTest Always) for on-screen debug.
    *   **NO** Standard physics layers (mostly). Use `SuperController` APIs where possible.
*   **Repo Structure**: `Custom/Scripts/FivelSystems/ProximityGrab/ProximityGrab.cs` is the main file.
*   **License**: CC BY-SA 4.0 (Due to derivation from Kimowal's work).

## Coding Standards
1.  **Architecture**:
    *   **Single-File**: VaM plugins must compile from a single `.cs` file. Use **Nested Classes** (or multiple classes in the same file) to separate logic.
    *   **SOLID**: Separate Visuals, Logic, and UI into distinct classes.
    *   **DRY**: Extract repeated logic (especially UI creation and math) into helper methods or classes.
2.  **Unity/Physics**:
    *   **Update Loop**: `Update()` is generally safe for `ConfigurableJoint` parameter blending (as seen in references). Use `FixedUpdate()` only if strictly manipulating RB physics steps.
    *   **Safety**: Always null-check `Atom`, `FreeControllerV3`, and `Rigidbody` before access.
    *   **Naming**: Follow **JetBrains Rider C#** conventions. Variable names must be clean, self-explanatory, and human-readable (e.g., `isBlending` instead of `bBl`).
    *   **Encapsulation**: Ensure proper hiding of internal state. Expose only necessary behavior via public methods. Fields should be private.

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
3.  **CHANGELOG.md**:
    *   Must follow [Keep a Changelog](https://keepachangelog.com/en/1.0.0/) standards.
    *   Sections: `Added`, `Changed`, `Deprecated`, `Removed`, `Fixed`, `Security`.
    *   Update this file with every notable change.

## VaM Specific API Notes
*   **Parenting**: Atoms are "linked" via `mainController.linkToRB`.
*   **Input**: Use `JSONStorable` classes (`JSONStorableFloat`, `JSONStorableAction`, etc.) for all UI and automation.
*   **Logging**: Use `SuperController.LogMessage()` and `SuperController.LogError()`.
