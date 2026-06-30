using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using Sandbox.ModAPI;

namespace GridSchematics
{
    public static partial class RenderEngine
    {
        static partial void DrawShipGridOverlay(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, RawRaycastScanData scanData, string fillMode, bool showBlocks, bool showBorder, bool showHullScan, bool showGrid, bool showReference, bool blurScan, bool showDebug, int rotationSteps, int raycastStep)
        {
            if (shipGrid == null || shipGrid.IsEmpty)
                return;

            var transform = GetOrBuildProjectionTransform(shipGrid, center, rotationSteps);
            if (!transform.IsValid || transform.CellSize <= 0f)
                return;

            var majorColor = UiDebugGridMajor;
            var minorColor = UiDebugGridMinor;
            var axisColor = UiReferenceAxis;
            var blockColor = UiBlockGhost;
            var fatBlockColor = UiFatBlockGhost;
            int subdivisions = shipGrid != null ? shipGrid.Subdivisions : 1;
            if (subdivisions < 1)
                subdivisions = 1;

            int gridStep = 1;
            if (transform.CellSize < 2f)
            {
                gridStep = (int)Math.Ceiling(2f / transform.CellSize);
                if (gridStep < 1)
                    gridStep = 1;
            }

            AddSprite(frame, new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                new Vector2(center.X + center.Width * 0.5f, center.Y + center.Height * 0.5f),
                new Vector2(center.Width, center.Height),
                new Color(0, 0, 0, 80)
            ));

            if (showHullScan)
                DrawRaycastScanImage(frame, center, transform, shipGrid, scanData, fillMode, blurScan, raycastStep);
            if (showBorder)
                DrawCachedShipBorder(frame, transform, shipGrid, scanData);

            if (showBlocks && shipGrid.Blocks.Count <= 20000)
                DrawCachedShipBlocks(frame, transform, shipGrid, blockColor, fatBlockColor);

            if (showGrid)
                DrawExtendedReferenceGrid(frame, center, shipGrid, transform, gridStep, subdivisions, minorColor, majorColor);
            if (showReference)
                DrawReferenceCenterLines(frame, center, shipGrid, transform, axisColor);

            if (showDebug && scanData != null && scanData.IsReady)
            {
                DrawShipLocalBoundsDebug(frame, transform);
                DrawScanSampleBoundsDebug(frame, transform, shipGrid, scanData);
            }
        }

        static partial void DrawShipReferenceMarkers(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, ScanCache cache, IMyCubeBlock panelBlock, bool showCenterOfMass, bool showPanelPosition, int rotationSteps)
        {
            if (shipGrid == null || shipGrid.IsEmpty || (!showCenterOfMass && !showPanelPosition))
                return;

            var transform = GetOrBuildProjectionTransform(shipGrid, center, rotationSteps);
            if (!transform.IsValid || transform.CellSize <= 0f)
                return;

            if (showCenterOfMass)
            {
                Vector3D centerOfMass;
                if (TryGetConstructCenterOfMass(cache, shipGrid, out centerOfMass))
                    DrawWorldReferenceMarker(frame, center, shipGrid, transform, centerOfMass, UiSelected);

                Vector3D centerOfThrust;
                if (TryGetConstructCenterOfThrust(cache, out centerOfThrust))
                    DrawWorldReferenceMarker(frame, center, shipGrid, transform, centerOfThrust, ResolveSecondarySchematicColor());
            }

            if (showPanelPosition && panelBlock != null)
                DrawWorldReferenceMarker(frame, center, shipGrid, transform, panelBlock.WorldMatrix.Translation, UiText);
        }

        static partial void DrawDockedMobileGridBorders(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, ScanCache cache, bool showDockedMobileGrids, bool showBorder, int rotationSteps)
        {
            if (!showDockedMobileGrids || !showBorder || shipGrid == null || shipGrid.IsEmpty || cache == null || cache.HullScanTargetGrids == null)
                return;

            var transform = GetOrBuildProjectionTransform(shipGrid, center, rotationSteps);
            if (!transform.IsValid || transform.CellSize <= 0f)
                return;

            Color colorBase = ResolveSecondarySchematicColor();
            var color = new Color(colorBase.R, colorBase.G, colorBase.B, ClampByte(235f * CurrentShipBorderOpacity));
            var blocks = new List<IMySlimBlock>();
            var occupied = new HashSet<Vector2I>();
            for (int i = 0; i < cache.HullScanTargetGrids.Count; i++)
            {
                var grid = cache.HullScanTargetGrids[i];
                if (!IsDockedMobileOverlayGrid(cache, grid))
                    continue;

                blocks.Clear();
                occupied.Clear();
                try
                {
                    grid.GetBlocks(blocks);
                }
                catch
                {
                    continue;
                }

                for (int b = 0; b < blocks.Count; b++)
                {
                    var slim = blocks[b];
                    if (slim == null)
                        continue;

                    var projected = ProjectCargoBlockBounds(shipGrid, grid, slim);
                    for (int x = projected.Min.X; x <= projected.Max.X; x++)
                    {
                        for (int y = projected.Min.Y; y <= projected.Max.Y; y++)
                            occupied.Add(new Vector2I(x, y));
                    }
                }

                if (occupied.Count == 0)
                    continue;

                foreach (var cell in occupied)
                {
                    bool left = !occupied.Contains(new Vector2I(cell.X - 1, cell.Y));
                    bool right = !occupied.Contains(new Vector2I(cell.X + 1, cell.Y));
                    bool top = !occupied.Contains(new Vector2I(cell.X, cell.Y - 1));
                    bool bottom = !occupied.Contains(new Vector2I(cell.X, cell.Y + 1));
                    if (!left && !right && !top && !bottom)
                        continue;

                    float localX0 = cell.X - shipGrid.Min2D.X;
                    float localX1 = localX0 + 1f;
                    float localY0 = shipGrid.Max2D.Y - cell.Y;
                    float localY1 = localY0 + 1f;
                    float localCx = (localX0 + localX1) * 0.5f;
                    float localCy = (localY0 + localY1) * 0.5f;
                    float width = Math.Max(1f, transform.CellSize + 0.4f);
                    float height = Math.Max(1f, transform.CellSize + 0.4f);

                    if (left)
                        DrawTransformedRect(frame, transform, localX0, localCy, 1f, height, color);
                    if (right)
                        DrawTransformedRect(frame, transform, localX1, localCy, 1f, height, color);
                    if (top)
                        DrawTransformedRect(frame, transform, localCx, localY0, width, 1f, color);
                    if (bottom)
                        DrawTransformedRect(frame, transform, localCx, localY1, width, 1f, color);
                }
            }
        }

        static bool TryGetConstructCenterOfMass(ScanCache cache, ShipGrid shipGrid, out Vector3D centerOfMass)
        {
            centerOfMass = Vector3D.Zero;
            if (cache == null || cache.ConstructGrids == null || shipGrid == null || shipGrid.Blocks == null)
                return false;

            double totalWeight = 0.0;
            for (int i = 0; i < cache.ConstructGrids.Count; i++)
            {
                var grid = cache.ConstructGrids[i];
                if (grid == null || grid.MarkedForClose || grid.Physics == null)
                    continue;

                int weight = CountProjectedBlocksForGrid(shipGrid, grid.EntityId);
                if (weight <= 0)
                    weight = 1;

                centerOfMass += grid.Physics.CenterOfMassWorld * weight;
                totalWeight += weight;
            }

            if (totalWeight <= 0.0)
                return false;

            centerOfMass /= totalWeight;
            return true;
        }

        static bool TryGetConstructCenterOfThrust(ScanCache cache, out Vector3D centerOfThrust)
        {
            centerOfThrust = Vector3D.Zero;
            if (cache == null || cache.ConstructGrids == null)
                return false;

            double totalThrust = 0.0;
            var blocks = new List<IMySlimBlock>();
            for (int i = 0; i < cache.ConstructGrids.Count; i++)
            {
                var grid = cache.ConstructGrids[i];
                if (grid == null || grid.MarkedForClose)
                    continue;

                blocks.Clear();
                try
                {
                    grid.GetBlocks(blocks, block => block != null && block.FatBlock is IMyThrust);
                }
                catch
                {
                    continue;
                }

                for (int b = 0; b < blocks.Count; b++)
                {
                    var thrust = blocks[b].FatBlock as IMyThrust;
                    if (thrust == null || thrust.MarkedForClose)
                        continue;

                    double weight = 0.0;
                    try { weight = Math.Max(0f, thrust.MaxEffectiveThrust); } catch { weight = 0.0; }
                    if (weight <= 0.0)
                    {
                        try { weight = Math.Max(0f, thrust.MaxThrust); } catch { weight = 0.0; }
                    }
                    if (weight <= 0.0)
                        continue;

                    centerOfThrust += thrust.WorldMatrix.Translation * weight;
                    totalThrust += weight;
                }
            }

            if (totalThrust <= 0.0)
                return false;

            centerOfThrust /= totalThrust;
            return true;
        }
        static int CountProjectedBlocksForGrid(ShipGrid shipGrid, long gridId)
        {
            int count = 0;
            if (shipGrid == null || shipGrid.Blocks == null)
                return count;

            for (int i = 0; i < shipGrid.Blocks.Count; i++)
            {
                if (shipGrid.Blocks[i].GridEntityId == gridId)
                    count++;
            }

            return count;
        }

        static void DrawWorldReferenceMarker(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, ProjectionTransform transform, Vector3D worldPosition, Color color)
        {
            Vector2 screen;
            if (!TryProjectWorldReferencePoint(shipGrid, transform, worldPosition, out screen))
                return;

            if (screen.X < center.X || screen.X > center.X + center.Width || screen.Y < center.Y || screen.Y > center.Y + center.Height)
                return;

            float left = center.X;
            float right = center.X + center.Width;
            float top = center.Y;
            float bottom = center.Y + center.Height;
            float thickness = 1f;
            float half = 4f;

            DrawScreenLine(frame, new Vector2(screen.X, top), new Vector2(screen.X, screen.Y - half), thickness, color);
            DrawScreenLine(frame, new Vector2(screen.X, screen.Y + half), new Vector2(screen.X, bottom), thickness, color);
            DrawScreenLine(frame, new Vector2(left, screen.Y), new Vector2(screen.X - half, screen.Y), thickness, color);
            DrawScreenLine(frame, new Vector2(screen.X + half, screen.Y), new Vector2(right, screen.Y), thickness, color);
            DrawScreenLine(frame, screen + new Vector2(-half, -half), screen + new Vector2(half, -half), thickness, color);
            DrawScreenLine(frame, screen + new Vector2(half, -half), screen + new Vector2(half, half), thickness, color);
            DrawScreenLine(frame, screen + new Vector2(half, half), screen + new Vector2(-half, half), thickness, color);
            DrawScreenLine(frame, screen + new Vector2(-half, half), screen + new Vector2(-half, -half), thickness, color);
        }

        static bool TryProjectWorldReferencePoint(ShipGrid shipGrid, ProjectionTransform transform, Vector3D worldPosition, out Vector2 screen)
        {
            screen = Vector2.Zero;
            if (shipGrid == null || !transform.IsValid || shipGrid.BasisGridSizeMeters <= 0f)
                return false;

            var basis = shipGrid.ReferenceMatrix;
            var offset = worldPosition - basis.Translation;
            double x = Vector3D.Dot(offset, basis.Right) / shipGrid.BasisGridSizeMeters;
            double y = Vector3D.Dot(offset, basis.Up) / shipGrid.BasisGridSizeMeters;
            double z = Vector3D.Dot(offset, basis.Forward) / shipGrid.BasisGridSizeMeters;

            double projectedX;
            double projectedY;
            switch (shipGrid.ProjectionView)
            {
                case ScanView.Front:
                    projectedX = x;
                    projectedY = y;
                    break;
                case ScanView.Side:
                    projectedX = z;
                    projectedY = y;
                    break;
                default:
                    projectedX = x;
                    projectedY = z;
                    break;
            }

            float localX = (float)(projectedX - shipGrid.Min2D.X + 0.5);
            float localY = (float)(shipGrid.Max2D.Y - projectedY + 0.5);
            screen = transform.ProjectLocalPoint(localX, localY);
            return true;
        }
        static void DrawShipLocalBoundsDebug(MySpriteDrawFrame frame, ProjectionTransform transform)
        {
            float width = Math.Max(1f, transform.SourceWidth * transform.CellSize);
            float height = Math.Max(1f, transform.SourceHeight * transform.CellSize);
            float localCx = transform.SourceWidth * 0.5f;
            float localCy = transform.SourceHeight * 0.5f;
            var color = new Color(70, 210, 85, 210);

            DrawTransformedRect(frame, transform, localCx, 0f, width, 1.5f, color);
            DrawTransformedRect(frame, transform, localCx, transform.SourceHeight, width, 1.5f, color);
            DrawTransformedRect(frame, transform, 0f, localCy, 1.5f, height, color);
            DrawTransformedRect(frame, transform, transform.SourceWidth, localCy, 1.5f, height, color);
        }

        static void DrawScanSampleBoundsDebug(MySpriteDrawFrame frame, ProjectionTransform transform, ShipGrid shipGrid, RawRaycastScanData scanData)
        {
            if (shipGrid == null || scanData == null)
                return;

            float localX0 = scanData.SampleMinX - shipGrid.Min2D.X + 0.5f;
            float localX1 = scanData.SampleMaxX - shipGrid.Min2D.X + 0.5f;
            float localY0 = shipGrid.Max2D.Y - scanData.SampleMaxY + 0.5f;
            float localY1 = shipGrid.Max2D.Y - scanData.SampleMinY + 0.5f;
            float localCx = (localX0 + localX1) * 0.5f;
            float localCy = (localY0 + localY1) * 0.5f;
            float width = Math.Max(1f, (localX1 - localX0) * transform.CellSize);
            float height = Math.Max(1f, (localY1 - localY0) * transform.CellSize);
            var color = new Color(255, 220, 65, 180);

            DrawTransformedRect(frame, transform, localCx, localY0, width, 1.5f, color);
            DrawTransformedRect(frame, transform, localCx, localY1, width, 1.5f, color);
            DrawTransformedRect(frame, transform, localX0, localCy, 1.5f, height, color);
            DrawTransformedRect(frame, transform, localX1, localCy, 1.5f, height, color);
        }

        static partial void DrawCargoOverlay(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, ScanCache cache, int rotationSteps, bool showAllConnections, TouchScreenApiAdapter input, UiState ui, bool occludeConveyorUnderFillBars)
        {
            if (shipGrid == null || shipGrid.IsEmpty || cache == null || cache.ConstructGrids == null)
                return;

            var transform = GetOrBuildProjectionTransform(shipGrid, center, rotationSteps);
            if (!transform.IsValid || transform.CellSize <= 0f)
                return;

            var groups = new List<CargoOverlayGroup>();
            var sources = GetOverlaySources(cache, shipGrid, "Cargo", ui != null && ui.ShowDockedMobileGrids);
            for (int i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                if (source == null || source.Block == null)
                    continue;

                CargoConnectorIndicator connectorIndicator;
                bool hasConnectorIndicator = TryGetConnectorIndicator(source.Block, shipGrid, transform, out connectorIndicator);
                AddCargoOverlayBlock(groups, source.Min, source.Max, GetOverlayFillRatioForMode("Cargo", source.Block, source.Role), source.Role, IsOverlayBlockOn(source.Block), hasConnectorIndicator, connectorIndicator, source.Block);
            }

            for (int i = 0; i < groups.Count; i++)
            {
                bool showFillBars = ui == null || ui.ShowFillBars;
                float fillAlphaScale = ui == null ? 1f : ui.FillBarsAlphaScale;
                DrawCargoOverlayGroup(frame, transform, shipGrid, groups[i], showFillBars, fillAlphaScale, occludeConveyorUnderFillBars && showFillBars, "Cargo", ui);
            }

            DrawOverlayInteractionUi(frame, center, transform, shipGrid, groups, input, "Cargo", ui);
        }

        static partial void DrawEnginesOverlay(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, ScanCache cache, int rotationSteps, bool showAllConnections, TouchScreenApiAdapter input, UiState ui, bool occludeConveyorUnderFillBars)
        {
            if (shipGrid == null || shipGrid.IsEmpty || cache == null || cache.ConstructGrids == null)
                return;

            var transform = GetOrBuildProjectionTransform(shipGrid, center, rotationSteps);
            if (!transform.IsValid || transform.CellSize <= 0f)
                return;

            var groups = new List<CargoOverlayGroup>();
            var sources = GetOverlaySources(cache, shipGrid, "Engines", ui != null && ui.ShowDockedMobileGrids);
            for (int i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                if (source == null || source.Block == null)
                    continue;

                AddCargoOverlayBlock(groups, source.Min, source.Max, GetOverlayFillRatioForMode("Engines", source.Block, source.Role), source.Role, IsOverlayBlockOn(source.Block), false, new CargoConnectorIndicator(), source.Block);
            }

            for (int i = 0; i < groups.Count; i++)
            {
                bool showFillBars = ui == null || ui.ShowFillBars;
                float fillAlphaScale = ui == null ? 1f : ui.FillBarsAlphaScale;
                DrawCargoOverlayGroup(frame, transform, shipGrid, groups[i], showFillBars, fillAlphaScale, occludeConveyorUnderFillBars && showFillBars, "Engines", ui);
            }

            DrawOverlayInteractionUi(frame, center, transform, shipGrid, groups, input, "Engines", ui);
        }

        static partial void DrawOxygenOverlay(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, ScanCache cache, int rotationSteps, bool showAllConnections)
        {
            if (shipGrid == null || shipGrid.IsEmpty || cache == null)
                return;

            var transform = GetOrBuildProjectionTransform(shipGrid, center, rotationSteps);
            if (!transform.IsValid || transform.CellSize <= 0f)
                return;
        }

        static partial void DrawConveyorOverlay(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, ScanCache cache, int rotationSteps, bool showAllConnections)
        {
            if (shipGrid == null || shipGrid.IsEmpty || cache == null)
                return;

            var transform = GetOrBuildProjectionTransform(shipGrid, center, rotationSteps);
            if (!transform.IsValid || transform.CellSize <= 0f)
                return;

            DrawCargoConveyorTopology(frame, transform, shipGrid, cache.ConveyorNetwork, null, showAllConnections);
        }

        static partial void DrawPowerOverlay(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, ScanCache cache, int rotationSteps, bool showAllConnections, TouchScreenApiAdapter input, UiState ui, bool occludeConveyorUnderFillBars)
        {
            if (shipGrid == null || shipGrid.IsEmpty || cache == null || cache.ConstructGrids == null)
                return;

            var transform = GetOrBuildProjectionTransform(shipGrid, center, rotationSteps);
            if (!transform.IsValid || transform.CellSize <= 0f)
                return;

            var groups = new List<CargoOverlayGroup>();
            var sources = GetOverlaySources(cache, shipGrid, "Power", ui != null && ui.ShowDockedMobileGrids);
            for (int i = 0; i < sources.Count; i++)
            {
                var source = sources[i];
                if (source == null || source.Block == null)
                    continue;

                AddCargoOverlayBlock(
                    groups,
                    source.Min,
                    source.Max,
                    GetOverlayFillRatioForMode("Power", source.Block, source.Role),
                    source.Role,
                    IsOverlayBlockOn(source.Block),
                    false,
                    new CargoConnectorIndicator(),
                    source.Block);
            }

            for (int i = 0; i < groups.Count; i++)
            {
                bool showFillBars = ui == null || ui.ShowFillBars;
                float fillAlphaScale = ui == null ? 1f : ui.FillBarsAlphaScale;
                DrawCargoOverlayGroup(frame, transform, shipGrid, groups[i], showFillBars, fillAlphaScale, occludeConveyorUnderFillBars && showFillBars, "Power", ui);
            }

            DrawOverlayInteractionUi(frame, center, transform, shipGrid, groups, input, "Power", ui);
        }

        static bool TryGetPowerOverlayRole(IMyCubeBlock block, out CargoOverlayRole role)
        {
            role = CargoOverlayRole.Storage;
            if (block == null || string.IsNullOrEmpty(block.BlockDefinition.SubtypeName))
                return false;

            string id = block.BlockDefinition.SubtypeName.ToLowerInvariant();
            var battery = block as IMyBatteryBlock;
            if (battery != null || IsPowerBatteryBlock(id))
            {
                role = CargoOverlayRole.Storage;
                return true;
            }

            var producer = block as IMyPowerProducer;
            if (producer != null)
            {
                role = CargoOverlayRole.Effector;
                return true;
            }

            if (IsPowerEffector(id))
            {
                role = CargoOverlayRole.Effector;
                return true;
            }

            return false;
        }

        static bool IsPowerBatteryBlock(string subtype)
        {
            if (string.IsNullOrEmpty(subtype))
                return false;

            return subtype.Contains("battery") || subtype.Contains("capacitor");
        }

        static bool IsPowerEffector(string subtype)
        {
            if (string.IsNullOrEmpty(subtype))
                return false;

            return subtype.Contains("reactor") ||
                subtype.Contains("generator") ||
                subtype.Contains("solar") ||
                subtype.Contains("wind");
        }

        static float GetPowerOverlayFillRatio(IMyCubeBlock block, CargoOverlayRole role)
        {
            if (block == null)
                return 0f;

            if (role == CargoOverlayRole.Storage)
            {
                var battery = block as IMyBatteryBlock;
                if (battery != null)
                {
                    try
                    {
                        float max = (float)battery.MaxStoredPower;
                        if (max > 0f)
                            return Clamp01((float)(battery.CurrentStoredPower / max));
                    }
                    catch
                    {
                    }
                }
                return 0f;
            }

            var producer = block as IMyPowerProducer;
            if (producer != null)
            {
                try
                {
                    float max = producer.MaxOutput;
                    if (max > 0f)
                        return Clamp01(producer.CurrentOutput / max);
                }
                catch
                {
                }
            }

            var functional = block as IMyFunctionalBlock;
            return functional != null && functional.IsWorking ? 1f : 0f;
        }
    }
}



