# Mod Structure

Space Engineers requires runtime assets to stay under `Data/`, so organization should happen inside that shape instead of moving files out of it.

## Current Layout

```text
Data/
  Scripts/
    SchemaRayTest/
      SchemaRayTest.cs
```

## Suggested Next Split

When the single session component grows too large, split by responsibility:

```text
Data/
  Scripts/
    RaycastGridDiscovery/
      RaycastGridDiscoverySession.cs
      Scanning/
        ConstructScanner.cs
        RayMetrics.cs
        ScanSettings.cs
      Rendering/
        LcdSchematicRenderer.cs
        MarchingSquares.cs
      Utilities/
        GridDiscovery.cs
```

Suggested namespaces:

```text
PingoPete.RaycastGridDiscovery
PingoPete.RaycastGridDiscovery.Scanning
PingoPete.RaycastGridDiscovery.Rendering
PingoPete.RaycastGridDiscovery.Utilities
```

## Refactor Order

1. Rename `SchemaRayTest` to `RaycastGridDiscovery` after Git history is established.
2. Extract settings parsing into `ScanSettings`.
3. Extract construct/grid discovery into `GridDiscovery`.
4. Extract ray scanning into `ConstructScanner`.
5. Extract LCD drawing into `LcdSchematicRenderer`.
6. Keep each extraction on its own branch so regressions are easy to isolate.
