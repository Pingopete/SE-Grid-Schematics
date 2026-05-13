# Project Roadmap

This roadmap keeps larger work split into focused branches and pull requests. `main` should remain the stable playable branch.

## Foundation

These features unlock or stabilize several later systems.

| Goal | Branch | Notes |
| --- | --- | --- |
| Multipanel support | `feature/multipanel-support` | Discover and manage multiple schematic panels without duplicating scan work. |
| Setting menu | `feature/setting-menu` | In-game configuration surface for panels, view modes, overlays, scan options, and alerts. |
| World-space cursor | `feature/worldspace-cursor` | Shared cursor/pointer model for inspecting schematic elements. |
| Structure scanning | `feature/structure-scanning` | Improve construct discovery, scan passes, and reusable block/system classification. |
| Alert system | `feature/alert-system` | Shared status/warning pipeline for panels, overlays, and info drawers. |

## Schematic Overlays

Each overlay should be isolated so display rules and block classification do not bleed into unrelated systems.

| Goal | Branch | Notes |
| --- | --- | --- |
| Cargo schematic overlay | `feature/cargo-overlay` | Cargo blocks, inventories, fullness, and routing-relevant cargo state. |
| Engine schematic overlay | `feature/engine-overlay` | Thrusters, propulsion grouping, orientation, and status. |
| Oxygen schematic overlay | `feature/oxygen-overlay` | Oxygen tanks, vents, generators, farms, and pressure-related display. |
| Power schematic overlay | `feature/power-overlay` | Reactors, batteries, solar, wind, and power state display. |
| Conveyor schematic overlay | `feature/conveyor-overlay` | Conveyor network, sorters, connectors, junctions, and broken routes. |
| Weapons schematic overlay | `feature/weapons-overlay` | Weapon blocks, arcs/ranges where practical, ammo or readiness state. |

## Info Drawers

Info drawers should use the same scan/classification data as overlays, but render detailed text or compact diagnostics.

| Goal | Branch | Notes |
| --- | --- | --- |
| Cargo info drawer | `feature/cargo-info-drawer` | Detailed cargo inventory and capacity readout. |
| Engine info drawer | `feature/engine-info-drawer` | Detailed thrust, grouping, and damage/status readout. |
| Oxygen info drawer | `feature/oxygen-info-drawer` | Detailed oxygen production, storage, and pressurization readout. |
| Power info drawer | `feature/power-info-drawer` | Detailed power production, storage, load, and warnings. |
| Weapons info drawer | `feature/weapons-info-drawer` | Detailed weapon status, ammo, targeting/readiness, and alerts. |

## Suggested Build Order

1. Merge `refactor/mod-structure` into `main` once tested in game.
2. Build `feature/multipanel-support`.
3. Build `feature/setting-menu`.
4. Build `feature/structure-scanning`.
5. Build `feature/worldspace-cursor`.
6. Build one vertical slice: one overlay plus its matching info drawer.
7. Generalize any shared overlay/drawer patterns before adding the remaining systems.
8. Add `feature/alert-system` once enough systems exist to reveal common alert needs.

## Vertical Slice Recommendation

Start with `feature/conveyor-overlay` plus `feature/cargo-info-drawer` or `feature/cargo-overlay`. Conveyors and cargo touch many ship systems, so this will expose useful architecture questions early.

Keep vertical slices small:

- scan/classify the system
- render a minimal overlay
- inspect it with the cursor or drawer
- test in game
- merge before expanding visuals

## Definition Of Done

- Branch is based on latest `main`.
- Feature is isolated to the relevant folder where possible.
- In-game panel named `[GRID_SCHEMATICS]` still updates.
- Existing scan/render behavior still works.
- Branch is pushed and merged through a pull request.
