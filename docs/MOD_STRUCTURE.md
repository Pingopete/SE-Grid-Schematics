# Mod Structure

Space Engineers requires runtime assets to stay under `Data/`, so organization should happen inside that shape instead of moving files out of it.

## Current Layout

```text
Data/
  Scripts/
    GridSchematics/
      GridSchematicsSession.cs
      Core/
        ConstructDiscovery.cs
        GridSchematicsTypes.cs
        PanelSettings.cs
      Rendering/
        LcdSchematicRenderer.cs
        MarchingSquares.cs
      Scanning/
        OrthographicScanner.cs
```

## Responsibility Map

- `GridSchematicsSession.cs`: Space Engineers session lifecycle and top-level scan orchestration.
- `Core/ConstructDiscovery.cs`: finding the active panel, construct grids, scan basis, and bounds.
- `Core/PanelSettings.cs`: parsing LCD `CustomData` settings and selecting view axes.
- `Core/GridSchematicsTypes.cs`: shared enums and small data structs.
- `Scanning/OrthographicScanner.cs`: raycast sampling and occupancy/thickness/density metrics.
- `Rendering/MarchingSquares.cs`: outline generation from occupied cells.
- `Rendering/LcdSchematicRenderer.cs`: LCD sprite drawing and text fallback output.

## Suggested Future Split

As features grow, add focused folders instead of rebuilding the large single-file shape:

```text
Data/
  Scripts/
    GridSchematics/
      GridSchematicsSession.cs
      Core/
      Panels/
      Scanning/
      Overlays/
      Rendering/
      Utilities/
```

Suggested namespaces:

```text
PingoPete.GridSchematics
PingoPete.GridSchematics.Scanning
PingoPete.GridSchematics.Rendering
PingoPete.GridSchematics.Utilities
```

As overlays and drawers are added, keep domain-specific logic in focused folders:

- `Overlays/`: cargo, engine, oxygen, power, conveyor, and weapons schematic layers.
- `Panels/`: multipanel routing, info drawers, setting menu, panel roles, and display mode selection.
- `Scanning/`: reusable scan passes and block/system classification.
- `Core/`: shared state, settings, alerts, and coordination models.

## Refactor Order

1. Keep `main` playable.
2. Make behavior-preserving file moves on `refactor/*` branches.
3. Build new behavior on `feature/*` branches after the structural branch is merged.
4. Keep each branch focused on one subsystem so regressions are easy to isolate.
