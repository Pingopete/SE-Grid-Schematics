# Developer Notes

## Start Here

Grid Schematics is currently organized as one Space Engineers session component split across C# `partial` files. The entry point is:

```text
Data/Scripts/GridSchematics/GridSchematicsSession.cs
```

All runtime scripts stay under `Data/Scripts/GridSchematics/` so Space Engineers can load them.

## Current Behavior

- The mod looks for one LCD/text panel whose name contains `[GRID_SCHEMATICS]`.
- Settings are read from that panel's `CustomData`.
- Scans run on startup and when panel settings change.
- Output is currently a schematic preview drawn to the active LCD surface.
- Multipanel routing, overlays, info drawers, setting menu, and alert system are planned but not implemented yet.

## Code Map

- `GridSchematicsSession.cs`: session lifecycle and top-level scan orchestration.
- `Core/GridSchematicsTypes.cs`: shared enums and simple structs.
- `Core/PanelSettings.cs`: panel `CustomData` parsing and view axis selection.
- `Core/ConstructDiscovery.cs`: same-construct grid collection, bounds, and scan basis selection.
- `Panels/PanelDiscovery.cs`: current single tagged panel discovery.
- `Scanning/OrthographicScanner.cs`: physics raycast sampling and scan metrics.
- `Rendering/MarchingSquares.cs`: outline generation from occupied scan cells.
- `Rendering/LcdSchematicRenderer.cs`: LCD sprite drawing and text fallback output.

## Branch Notes

Keep `main` playable. Structural cleanup should happen on `refactor/*` branches, and feature work should happen on focused `feature/*` branches.

Before starting feature branches like overlays or info drawers, merge the current structure refactor into `main` after an in-game load test.
