# TODO

## Bugs
- [x] **CRITICAL**: The script no longer works: `Error in Grab: Object reference not set to an instance of an object`.

- [x] **FIX**: SphereCast's origin is being mistakenly set to the linkedRB (parent) Atom's object instead of the current Atom's selected object. The behaviour can be replicated by changing the "Link to Atom" and "Link To" fields, and then triggering "Refresh Origin List": If there are no linked atoms, the origin is set to the proper (desired) object (Grab Origin Body's dropdown field), if there is a linked atom, the origin is set to the linked atom's object (linkedRB), which is what we want to avoid. The origin must always be the current Atom's selected object.

- [x] **FIX**: If an object is already grabbed, we should not be able to grab it again (must prevent adding a rigidbody if it already exists in the list of attachments).
- [x] **CRITICAL**: Fixed VaM crash caused by `ClosestPoint` on non-convex MeshColliders.
- [x] **FIX**: Resolved C# version compatibility issues (pattern matching, expression-bodied members).
- [x] **CRITICAL**: Fixed persistent load crash on Person atoms by targeting `PhysicsModel` hierarchy and implementing whitelist.
- [x] **REFACTOR**: Improved error handling (removed loop try-catch) and decoupled Visualizer dependencies.

## Features
- [x] **OPTIMIZATION**: Replaced `Update()` loop polling with event-based visuals to save performance.
- [ ] **NEW**: We already have 'Detach Last', why not have 'Attach Last'? (Should also be exposed for other plugins to use that trigger)
- [ ] **NEW**: Consequence trigger fields: Add field where we can add 'OnGrab' and 'OnRelease' triggers.