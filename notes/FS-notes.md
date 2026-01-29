# FeatureScript Working Notes

A living guide for building clean, structured, standard‑pattern FeatureScript.

## Principles
- Prefer standard library patterns (`tool.fs`, `feature.fs`) over custom re‑implementations.
- Establish coordinate systems early; everything else depends on it.
- Keep UI, geometry, and manipulator logic separated in the structure.
- Normalize/guard definition values before using them; clamp sizes at the end.
- Make UI reflect the feature’s actual semantics (names, defaults, enums).

## Workflow (Suggested Order)
1) **Precondition UI**: standard predicates, naming, filters, defaults.
2) **Reference CSys**: resolve `definition.location` (mate connector vs sketch vertex).
3) **Pattern Transform**: apply `getRemainderPatternTransform` to the reference CSys.
4) **Local Geometry**: build in local coordinates only.
5) **Orientation**: transform to world using the reference CSys.
6) **Booleans**: use `processNewBodyIfNeeded` + `mergeScopeExclusion`.
7) **Manipulators**: add only after geometry is stable.
8) **Manipulator Change**: map manip results back into parameters.

## Standard Patterns to Reuse
### Boolean Operations
- Use `booleanStepTypePredicate(definition)` for New/Add/Remove/Intersect.
- Use `booleanStepScopePredicate(definition)` for merge scope UI.
- In the body:
  - `definition.mergeScopeExclusion = toolBodies;`
  - `processNewBodyIfNeeded(context, id, definition, reconstructOp);`

### Location / Orientation
- Location should accept **sketch points** and **mate connectors**.
- For a sketch vertex, orient to the **sketch plane normal**.
- Use `evOwnerSketchPlane` to retrieve the plane normal.

### Patterning
- Always apply `getRemainderPatternTransform` to your reference CSys.
- Compute derived axes from transformed origin to avoid stale vectors.

### Draft
- Use `opDraft` with **standard** angle bounds (0–89.9°).
- If a manipulator is used:
  - `axisDirection` is rotation axis; `rotationOrigin` defines the zero line.
  - Decide the plane of the dial **explicitly** (XY vs XZ).
- Treat draft “Start/End” relative to your **feature’s Z‑direction**.

## Manipulator Guidelines
- Keep manip math in local space, then transform to world with `toWorld(baseCsys, ...)`.
- One manip should affect one parameter (sizeX, sizeY, sizeZ, draftAngle).
- Clamp only after calculating new values, not before.
- If the manip feels “jumpy,” the base or offset is probably inconsistent.

## Naming & UI Conventions
- Match enum identifiers to UI labels (e.g., `START`, `MIDDLE`, `END`).
- Keep UI hints consistent with standard features:
  - `UIHint.HORIZONTAL_ENUM` for placements/operation type.
  - `UIHint.OPPOSITE_DIRECTION_CIRCULAR` for draft flip.
- Keep UI order from top to bottom: placement → location → sizes → offsets → draft → booleans.

## Debug/Regression Checklist
- Verify orientation with a **sketch vertex** and a **mate connector**.
- Test **feature patterns** with “Reapply features”.
- Test `flipZ` across placements and ensure draft direction is consistent.
- Check manipulator flip and direction on each placement mode.

## Notes on Consistency
- Tie “Top/Bottom” (or “Start/End”) to **local** Z direction (not world Z).
- “Start” should be the default if the feature grows from a reference plane.
- When in doubt: mirror the standard feature UX and logic.

