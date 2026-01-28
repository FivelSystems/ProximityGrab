# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [v2] - 2026-01-27
### Physics Overhaul & Stabilization
*   **Feature**: Real-Time Physics. Stiffness, Mode, and Blend Speed now update instantly for active grabs.
*   **Feature**: Proper Spring Mechanics. "Soft" and "Firm" presets now use `ConfigurableJoint` Drives instead of hard locks.
*   **Feature**: "Grab Triggers" and "Grab MeshJoints" toggles for advanced control.
*   **Fix**: Resolved persistent crash on load by flattening architecture.
*   **Fix**: "Pink Blob" gizmo fixed by implementing robust Shader finding logic.
*   **Fix**: Correctly filtering out internal `PhysicsMeshJoint` objects.
*   **Fix**: Hardened `OnDestroy` to prevent memory leaks.
*   **UI**: Redesigned UI with 2-Column layout.

## [v1] - 2026-01-25
### Added
- Initial release of Proximity Grab.
- Proximity detection using `Physics.OverlapSphere`.
- Visualized Grab Radius (Green Wireframe).
- Attach/Detach UI Buttons & Triggers.
- Smart Parenting (ignores grabbing the hand's parent).
- Configurable Radius, Offsets, and Physics Presets (Soft/Firm/Lock).
