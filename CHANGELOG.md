# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/).

## [v3] - 2026-01-27
### Added
*   "Grab Kinematic" toggle (default: Off) to prevent grabbing objects with physics disabled.

### Fixed
*   Improved grab sorting to use `ClosestPoint` instead of object pivot, resolving incorrect object selection.

## [v2] - 2026-01-27
### Added
*   Real-Time Physics. Stiffness, Mode, and Blend Speed now update instantly for active grabs.
*   "Grab Triggers" and "Grab MeshJoints" toggles for advanced control.

### Changed
*   Proper Spring Mechanics. "Soft" and "Firm" presets now use `ConfigurableJoint` Drives instead of hard locks.
*   Redesigned UI with 2-Column layout.

### Fixed
*   Resolved persistent crash on load by flattening architecture.
*   "Pink Blob" gizmo fixed by implementing robust Shader finding logic.
*   Correctly filtering out internal `PhysicsMeshJoint` objects.
*   Hardened `OnDestroy` to prevent memory leaks.

## [v1] - 2026-01-25
### Added
- Initial release of Proximity Grab.
- Proximity detection using `Physics.OverlapSphere`.
- Visualized Grab Radius (Green Wireframe).
- Attach/Detach UI Buttons & Triggers.
- Smart Parenting (ignores grabbing the hand's parent).
- Configurable Radius, Offsets, and Physics Presets (Soft/Firm/Lock).
