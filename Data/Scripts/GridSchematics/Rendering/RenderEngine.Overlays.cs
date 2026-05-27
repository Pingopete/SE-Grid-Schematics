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
            var sources = GetOverlaySources(cache, shipGrid, "Cargo");
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
            var sources = GetOverlaySources(cache, shipGrid, "Engines");
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
            var sources = GetOverlaySources(cache, shipGrid, "Power");
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



