# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [2.0.0] - 2026-01-27
### Stabilized & Simplified
*   **Fix**: Resolved persistent crash on load by flattening architecture and removing race condition in initialization.
*   **Fix**: "Pink Blob" gizmo fixed by implementing robust Shader finding logic (Rigify style).
*   **Fix**: Correctly filtering out internal `PhysicsMeshJoint` objects to prevent unwanted grabs.
*   **Fix**: Hardened `OnDestroy` to prevent memory leaks and "ghost objects" upon plugin removal.
*   **Fix**: "Empty Origin List" bug fixed with delayed refresh on load.
*   **Refactor**: Reverted to Single-File Architecture for maximum stability and compatibility.
*   **UI**: Redesigned UI with 2-Column layout, Top Origin selector, and clearer settings.
*   **UI**: Added new "Grabbables" section to filter targets (Standard Rigidbodies, Triggers, MeshJoints).
*   **New**: Added "Grab Triggers" and "Grab MeshJoints" toggles for advanced control.
*   **New**: Visuals now change color (Green -> Blue) to indicate grab state.

## [1.0.0] - 2026-01-25
### Added
- Initial release of Proximity Grab.
- Proximity detection using `Physics.OverlapSphere`.
- Visualized Grab Radius (Green Wireframe).
- Attach/Detach UI Buttons & Triggers.
- Smart Parenting (ignores grabbing the hand's parent).
- Configurable Radius, Offsets, and Physics Presets (Soft/Firm/Lock).
