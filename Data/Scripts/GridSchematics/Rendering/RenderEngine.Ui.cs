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
        const float UiButtonTextScale = 0.55f;
        const float UiButtonTextBaselineOffset = -7f;
        const float TouchCursorSize = 9f;
        const float TouchCursorThickness = 1f;
        const float SharedCursorLineThickness = 1.15f;
        const float SharedCursorCrossRadius = 6f;
        const int MaxUiButtonVisualCacheEntries = 384;
        const string UiIconConveyorTexture = "GridSchematics_Icon_Conveyor";
        const string UiIconFillBarsTexture = "GridSchematics_Icon_FillBars";
        const string UiIconBorderTexture = "GridSchematics_Icon_Border";
        const string UiIconHullScanTexture = "GridSchematics_Icon_HullScan";
        const string UiIconGridTexture = "GridSchematics_Icon_Grid";
        const string UiIconReferenceTexture = "GridSchematics_Icon_Reference";
        const string UiIconCloseTexture = "GridSchematics_Icon_Close";
        const string UiIconMinimizeTexture = "GridSchematics_Icon_Minimize";
        const string UiIconPlusTexture = "GridSchematics_Icon_Reference";
        const string UiIconMinusTexture = "GridSchematics_Icon_Minimize";
        static readonly Dictionary<string, CachedUiButtonVisual> UiButtonVisualCache = new Dictionary<string, CachedUiButtonVisual>();

        class CachedUiButtonVisual
        {
            public List<MySprite> Sprites = new List<MySprite>();
            public int LastUsed;
        }

        static partial void DrawTopMenu(MySpriteDrawFrame frame, int screenWidth, int screenHeight, MenuPanel activeMenu, string activeView, bool showBlocks, bool showBorder, bool showHullScan, bool showGrid, bool showDebug, bool showPerfStats, bool showReference, bool showCenterOfMass, bool showPanelPosition, bool showDockedMobileGrids, bool showConveyorOverlay, bool showFillBars, bool showAllConnections, bool blurScan, string fillMode, string uiPalette, int uiHue, float uiBrightness, float uiSaturation, float uiAlpha, int uiAccentHue, float uiAccentBrightness, float uiAccentSaturation, float uiPanelBrightness, float uiPanelAlpha, int schematicMainHue, int schematicSecondaryHue, int conveyorHue, float hullScanAlpha, float schematicAlpha, string storageColor, string effectorColor, bool segmentMode, bool mouseControl, string mouseSensitivity, bool allowGridRotation, bool performanceMode, bool highResScanning, int settingsExpandedMask, string activeSettingsActionId, string hoverRegionId)
        {
            var profile = UiLayout.BuildSurfaceProfile(screenWidth, screenHeight);
            var topRegions = UiLayout.BuildTopMenuRegions(screenWidth, screenHeight);
            var topRightRegions = UiLayout.BuildTopRightModeRegions(screenWidth, screenHeight);
            DrawStaticPanelBarForRegions(frame, screenWidth, topRegions, topRightRegions);
            DrawTopPanelFillerButtons(frame, screenWidth, topRegions, topRightRegions);

            for (int i = 0; i < topRegions.Length; i++)
            {
                var region = topRegions[i];
                bool active = false;
                if (region.Id == UiLayout.ViewTopId)
                    active = !segmentMode && string.Equals(activeView, "TOP", StringComparison.OrdinalIgnoreCase);
                else if (region.Id == UiLayout.ViewLeftId)
                    active = !segmentMode && (string.Equals(activeView, "LEFT", StringComparison.OrdinalIgnoreCase) || string.Equals(activeView, "SIDE", StringComparison.OrdinalIgnoreCase));
                else if (region.Id == UiLayout.ViewFrontId)
                    active = !segmentMode && string.Equals(activeView, "FRONT", StringComparison.OrdinalIgnoreCase);
                else if (region.Id == UiLayout.SegmentModeId)
                    active = segmentMode;
                else if (region.Id == UiLayout.MenuToolsId)
                    active = activeMenu == MenuPanel.Tools;

                bool hover = string.Equals(region.Id, hoverRegionId, StringComparison.Ordinal);
                string label = region.Id == UiLayout.ViewTopId ? "TOP" :
                    region.Id == UiLayout.ViewLeftId ? "SIDE" :
                    region.Id == UiLayout.ViewFrontId ? "FRONT" :
                    region.Id == UiLayout.SegmentModeId ? (profile.UsesCompactScaling ? "MULTI" : "MULTIVIEW") :
                    region.Id == UiLayout.MenuToolsId ? "TOOLS" : region.Hint;

                DrawViewButton(frame, region, label, active, hover);
            }

            for (int i = 0; i < topRightRegions.Length; i++)
            {
                var region = topRightRegions[i];
                bool hover = string.Equals(region.Id, hoverRegionId, StringComparison.Ordinal);
                if (region.Id == UiLayout.ToggleChromeId)
                    DrawChromeToggleButton(frame, region, false, hover);
                else
                    DrawViewButton(frame, region, profile.UsesCompactScaling ? "SET" : "SETTINGS", activeMenu == MenuPanel.Settings, hover);
            }

            var panelRegions = (activeMenu == MenuPanel.Settings || activeMenu == MenuPanel.Scan || activeMenu == MenuPanel.Tools) ? UiLayout.BuildMenuPanelRegions(screenWidth, screenHeight, activeMenu, settingsExpandedMask) : new HitRegion[0];
            if (panelRegions.Length > 0)
            {
                DrawMenuPanelBackground(frame, panelRegions);
                for (int i = 0; i < panelRegions.Length; i++)
                {
                    var region = panelRegions[i];
                    bool active = false;
                    bool hover = string.Equals(region.Id, hoverRegionId, StringComparison.Ordinal);

                    if (activeMenu == MenuPanel.View)
                    {
                        if (region.Id == UiLayout.ViewTopId)
                            active = string.Equals(activeView, "TOP", StringComparison.OrdinalIgnoreCase);
                        else if (region.Id == UiLayout.ViewLeftId)
                            active = string.Equals(activeView, "LEFT", StringComparison.OrdinalIgnoreCase) || string.Equals(activeView, "SIDE", StringComparison.OrdinalIgnoreCase);
                        else if (region.Id == UiLayout.ViewFrontId)
                            active = string.Equals(activeView, "FRONT", StringComparison.OrdinalIgnoreCase);
                        else if (region.Id == UiLayout.ToggleReferenceId)
                            active = showReference;
                        else if (region.Id == UiLayout.ToggleMouseControlId)
                            active = mouseControl;
                    }

                    if (activeMenu == MenuPanel.Layers)
                    {
                        if (region.Id == UiLayout.ToggleBlocksId)
                            active = showBlocks;
                        else if (region.Id == UiLayout.ToggleBorderId)
                            active = showBorder;
                        else if (region.Id == UiLayout.ToggleGridId)
                            active = showGrid;
                        else if (region.Id == UiLayout.ToggleReferenceId)
                            active = showReference;
                        else if (region.Id == UiLayout.ToggleCenterOfMassId)
                            active = showCenterOfMass;
                        else if (region.Id == UiLayout.TogglePanelPositionId)
                            active = showPanelPosition;
                        else if (region.Id == UiLayout.ToggleDockedMobileGridsId)
                            active = showDockedMobileGrids;
                        else if (region.Id == UiLayout.ToggleAllConnectionsId)
                            active = showAllConnections;
                    }

                    if (activeMenu == MenuPanel.Settings)
                    {
                        if (region.Id == UiLayout.ToggleBlurId)
                            active = blurScan;
                        else if (region.Id == UiLayout.TogglePerformanceModeId)
                            active = performanceMode;
                        else if (region.Id == UiLayout.ToggleHighResScanningId)
                            active = highResScanning;
                        else if (region.Id == UiLayout.ToggleDebugModeId)
                            active = showDebug;
                        else if (region.Id == UiLayout.TogglePerfStatsId)
                            active = showPerfStats;
                        else if (region.Id == UiLayout.ToggleAllConnectionsId)
                            active = showAllConnections;
                        else if (region.Id == UiLayout.ToggleHullScanId)
                            active = showHullScan;
                        else if (region.Id == UiLayout.ToggleFillBarsId)
                            active = showFillBars;
                        else if (region.Id == UiLayout.ToggleBlocksId)
                            active = showBlocks;
                        else if (region.Id == UiLayout.ToggleGridId)
                            active = showGrid;
                        else if (region.Id == UiLayout.ToggleReferenceId)
                            active = showReference;
                        else if (region.Id == UiLayout.ToggleCenterOfMassId)
                            active = showCenterOfMass;
                        else if (region.Id == UiLayout.TogglePanelPositionId)
                            active = showPanelPosition;
                        else if (region.Id == UiLayout.ToggleDockedMobileGridsId)
                            active = showDockedMobileGrids;
                        else if (region.Id == UiLayout.ToggleMouseControlId)
                            active = mouseControl;
                    }

                    if (activeMenu == MenuPanel.Scan)
                    {
                        if (region.Id == UiLayout.SetDensityId)
                            active = string.Equals(fillMode, GridSchematicsConfig.FillDensity, StringComparison.OrdinalIgnoreCase);
                        else if (region.Id == UiLayout.SetThicknessId)
                            active = string.Equals(fillMode, GridSchematicsConfig.FillThickness, StringComparison.OrdinalIgnoreCase);
                        else if (region.Id == UiLayout.SetVoidsId)
                            active = string.Equals(fillMode, GridSchematicsConfig.FillVoids, StringComparison.OrdinalIgnoreCase);
                    }

                    if (activeMenu == MenuPanel.Tools)
                    {
                        if (region.Id == UiLayout.ToggleConveyorId)
                            active = showConveyorOverlay;
                        else if (region.Id == UiLayout.ToggleFillBarsId)
                            active = showFillBars;
                        else if (region.Id == UiLayout.ToggleBorderId)
                            active = showBorder;
                        else if (region.Id == UiLayout.ToggleHullScanId)
                            active = showHullScan;
                        else if (region.Id == UiLayout.CycleScanModeId)
                            active = !string.Equals(fillMode, GridSchematicsConfig.FillNone, StringComparison.OrdinalIgnoreCase);
                        else if (region.Id == UiLayout.CycleScanColorScaleId)
                            active = !string.Equals(GridSchematicsConfig.NormalizeHullScanColorScale(CurrentHullScanColorScale), GridSchematicsConfig.HullColorGreyscale, StringComparison.OrdinalIgnoreCase);
                        else if (region.Id == UiLayout.ToggleGridId)
                            active = showGrid;
                        else if (region.Id == UiLayout.ToggleReferenceId)
                            active = showReference;
                        else if (region.Id == UiLayout.ToggleCenterOfMassId)
                            active = showCenterOfMass;
                        else if (region.Id == UiLayout.TogglePanelPositionId)
                            active = showPanelPosition;
                        else if (region.Id == UiLayout.ToggleDockedMobileGridsId)
                            active = showDockedMobileGrids;
                    }

                string label = region.Id == UiLayout.ViewTopId ? "TOP" :
                    region.Id == UiLayout.ViewLeftId ? "LEFT" :
                    region.Id == UiLayout.ViewFrontId ? "FRONT" :
                        region.Id == UiLayout.RotateCcwId ? "ROT -" :
                        region.Id == UiLayout.RotateCwId ? "ROT +" :
                        region.Id == UiLayout.ToggleBorderId ? "OUTLINE" :
                        region.Id == UiLayout.ToggleGridId ? "GRID" :
                        region.Id == UiLayout.ToggleReferenceId ? "REFERENCE" :
                        region.Id == UiLayout.SetDensityId ? "DENSITY" :
                        region.Id == UiLayout.SetThicknessId ? "DEPTH" :
                        region.Id == UiLayout.SetVoidsId ? "VOIDS" :
                        region.Id == UiLayout.ToggleHullScanId ? "HULL SCAN" :
                        region.Id == UiLayout.CycleScanModeId ? "SCAN " + GridSchematicsConfig.GetFillModeLabelStatic(fillMode) :
                        region.Id == UiLayout.CycleScanColorScaleId ? "COLOR " + GridSchematicsConfig.GetHullScanColorScaleLabelStatic(CurrentHullScanColorScale) :
                        region.Id == UiLayout.ToggleFillBarsId ? "FILL BARS" :
                        region.Id == UiLayout.ToggleCenterOfMassId ? "CENTER OF MASS" :
                        region.Id == UiLayout.TogglePanelPositionId ? "PANEL POSITION" :
                        region.Id == UiLayout.ToggleDockedMobileGridsId ? "DOCKED SHIPS" :
                        region.Id == UiLayout.ToggleBlocksId ? "SHOW ALL BLOCKS" :
                        region.Id == UiLayout.ToggleAllConnectionsId ? "SHOW ALL CONNECTIONS" :
                        region.Id == UiLayout.ToggleBlocksOccludeConveyorsId ? "BLOCKS OCCLUDE CONVEYORS " + (CurrentBlocksOccludeConveyors ? "ON" : "OFF") :
                        region.Id == UiLayout.ToggleConnectedNetworksId ? "SHOW CONNECTED NETWORKS " + (CurrentShowConnectedNetworks ? "ON" : "OFF") :
                        region.Id == UiLayout.ToggleBlurId ? "RENDER SMOOTHING " + (blurScan ? "ON" : "OFF") :
                        region.Id == UiLayout.TogglePerformanceModeId ? "PERFORMANCE MODE " + (performanceMode ? "ON" : "OFF") :
                        region.Id == UiLayout.ToggleHighResScanningId ? "HIGH RES SCANNING " + (highResScanning ? "ON" : "OFF") :
                        region.Id == UiLayout.ToggleDebugModeId ? "ENABLE DEBUG " + (showDebug ? "ON" : "OFF") :
                        region.Id == UiLayout.TogglePerfStatsId ? "PERF STATS " + (showPerfStats ? "ON" : "OFF") :
                        region.Id == UiLayout.SettingsStyleHeaderId ? FormatSettingsHeaderLabel("STYLE", settingsExpandedMask, UiLayout.SettingsCategoryStyle) :
                        region.Id == UiLayout.SettingsUiHeaderId ? FormatSettingsHeaderLabel("CONTROLS", settingsExpandedMask, UiLayout.SettingsCategoryUserInterface) :
                        region.Id == UiLayout.SettingsRenderingHeaderId ? FormatSettingsHeaderLabel("RENDERING", settingsExpandedMask, UiLayout.SettingsCategoryRendering) :
                        region.Id == UiLayout.SettingsSchematicsHeaderId ? FormatSettingsHeaderLabel("SCHEMATICS", settingsExpandedMask, UiLayout.SettingsCategorySchematics) :
                        region.Id == UiLayout.SettingsDebugHeaderId ? FormatSettingsHeaderLabel("DEBUG", settingsExpandedMask, UiLayout.SettingsCategoryDebug) :
                        region.Id == UiLayout.SettingsPanelDataHeaderId ? FormatSettingsHeaderLabel("PANEL DATA", settingsExpandedMask, UiLayout.SettingsCategoryPanelData) :
                        region.Id == UiLayout.CyclePaletteId ? "BACKGROUND " + GridSchematicsConfig.NormalizeUiPalette(uiPalette) :
                        region.Id == UiLayout.AdjustPaletteBrightnessId ? "UI 1 - BRIGHTNESS" :
                        region.Id == UiLayout.AdjustPaletteHueId ? "UI 1 - COLOR" :
                        region.Id == UiLayout.AdjustPaletteSaturationId ? "UI 1 - SATURATION" :
                        region.Id == UiLayout.AdjustAccentBrightnessId ? "UI 2 - BRIGHTNESS" :
                        region.Id == UiLayout.AdjustAccentHueId ? "UI 2 - COLOR" :
                        region.Id == UiLayout.AdjustAccentSaturationId ? "UI 2 - SATURATION" :
                        region.Id == UiLayout.AdjustSchematicMainHueId ? "SCH 1 - COLOR" :
                        region.Id == UiLayout.AdjustSchematicSecondaryHueId ? "SCH 2 - COLOR" :
                        region.Id == UiLayout.AdjustConveyorHueId ? "CONVEYOR - COLOR" :
                        region.Id == UiLayout.CycleUiFontId ? "FONT " + GridSchematicsConfig.GetUiFontLabel(CurrentUiFont) :
                        region.Id == UiLayout.AdjustPanelBrightnessId ? "PANEL " + FormatUiSliderValue(uiPanelBrightness) :
                        region.Id == UiLayout.ToggleMouseControlId ? "MOUSE CONTROL" :
                        region.Id == UiLayout.CycleMouseSensitivityId ? "MOUSE " + GridSchematicsConfig.NormalizeMouseSensitivity(mouseSensitivity) :
                        region.Id == UiLayout.ToggleGridRotationId ? "ALLOW GRID ROTATION " + (allowGridRotation ? "ON" : "OFF") :
                        region.Id == UiLayout.AdjustHullScanAlphaId ? "HULL " + FormatUiSliderValue(hullScanAlpha) :
                        region.Id == UiLayout.AdjustSchematicAlphaId ? "SCHEMATIC " + FormatUiSliderValue(schematicAlpha) :
                        region.Id == UiLayout.CycleStorageColorId ? "STOR " + GridSchematicsConfig.NormalizeSchematicColor(storageColor) :
                        region.Id == UiLayout.CycleEffectorColorId ? "EFF " + GridSchematicsConfig.NormalizeSchematicColor(effectorColor) :
                        region.Id == UiLayout.CopyUiSettingsId ? "COPY UI" :
                        region.Id == UiLayout.PasteUiSettingsId ? "PASTE UI" :
                        region.Id == UiLayout.CopyRenderingSettingsId ? "COPY RENDER" :
                        region.Id == UiLayout.PasteRenderingSettingsId ? "PASTE RENDER" :
                        region.Id == UiLayout.CopySchematicSettingsId ? "COPY SCHEM" :
                        region.Id == UiLayout.PasteSchematicSettingsId ? "PASTE SCHEM" :
                        region.Id == UiLayout.CopyDebugSettingsId ? "COPY DEBUG" :
                        region.Id == UiLayout.PasteDebugSettingsId ? "PASTE DEBUG" :
                        region.Id == UiLayout.WipePanelCacheId ? "REBUILD CACHE" :
                        region.Id == UiLayout.ResetPanelSettingsId ? "RESET PANEL SETTINGS" :
                        region.Id == UiLayout.SaveSettingsId ? "SAVE" :
                        region.Id == UiLayout.CopySettingsId ? "COPY" :
                        region.Id == UiLayout.PasteSettingsId ? "PASTE" :
                        region.Id == UiLayout.ExportSettingsId ? "EXPORT" :
                        region.Id == UiLayout.RecalibrateCursorId ? "RECALIBRATE" :
                        region.Id == UiLayout.ResetStyleSettingsId ? "RESET" :
                        region.Id == UiLayout.ResetUiSettingsId ? "RESET UI" :
                        region.Id == UiLayout.RunScanId ? "RUN SCAN" : region.Hint;

                    if (activeMenu == MenuPanel.Tools)
                        DrawToolPanelIconButton(frame, region, active, hover);
                    else if (IsSettingsHeaderRegion(region.Id))
                        DrawSettingsHeader(frame, region, label, hover, settingsExpandedMask);
                    else if (activeMenu == MenuPanel.Settings && region.Id == UiLayout.AdjustPaletteBrightnessId)
                        DrawStyleSlider(frame, region, label, NormalizeRange(uiBrightness, 0.45f, 1.65f), hover);
                    else if (activeMenu == MenuPanel.Settings && region.Id == UiLayout.AdjustPaletteHueId)
                        DrawStyleSlider(frame, region, label, uiHue / 359f, hover);
                    else if (activeMenu == MenuPanel.Settings && region.Id == UiLayout.AdjustPaletteSaturationId)
                        DrawStyleSlider(frame, region, label, NormalizeRange(uiSaturation, 0f, 2f), hover);
                    else if (activeMenu == MenuPanel.Settings && region.Id == UiLayout.AdjustAccentBrightnessId)
                        DrawStyleSlider(frame, region, label, NormalizeRange(uiAccentBrightness, 0.45f, 1.65f), hover);
                    else if (activeMenu == MenuPanel.Settings && region.Id == UiLayout.AdjustAccentHueId)
                        DrawStyleSlider(frame, region, label, uiAccentHue / 359f, hover);
                    else if (activeMenu == MenuPanel.Settings && region.Id == UiLayout.AdjustAccentSaturationId)
                        DrawStyleSlider(frame, region, label, NormalizeRange(uiAccentSaturation, 0f, 2f), hover);
                    else if (activeMenu == MenuPanel.Settings && region.Id == UiLayout.AdjustSchematicMainHueId)
                        DrawStyleSlider(frame, region, label, schematicMainHue / 359f, hover);
                    else if (activeMenu == MenuPanel.Settings && region.Id == UiLayout.AdjustSchematicSecondaryHueId)
                        DrawStyleSlider(frame, region, label, schematicSecondaryHue / 359f, hover);
                    else if (activeMenu == MenuPanel.Settings && region.Id == UiLayout.AdjustConveyorHueId)
                        DrawStyleSlider(frame, region, label, conveyorHue / 359f, hover);
                    else if (activeMenu == MenuPanel.Settings)
                    {
                        bool rowActive = string.Equals(region.Id, activeSettingsActionId, StringComparison.Ordinal) ||
                            (region.Id == UiLayout.ToggleHighResScanningId && highResScanning);
                        DrawSettingsRowButton(frame, region, label, rowActive, hover);
                    }
                    else
                        DrawViewButton(frame, region, label, active, hover);
                }
            }
        }

        static void DrawToolPanelIconButton(MySpriteDrawFrame frame, HitRegion region, bool active, bool hover)
        {
            bool drawLeftBorder = region.Id == UiLayout.ToggleConveyorId;
            bool drawRightBorder = true;
            if (region.Id == UiLayout.ToggleConveyorId)
                DrawUtilityIconButton(frame, region, UtilityIcon.Network, active, hover, drawLeftBorder, drawRightBorder);
            else if (region.Id == UiLayout.ToggleFillBarsId)
                DrawUtilityIconButton(frame, region, UtilityIcon.FillBars, active, hover, drawLeftBorder, drawRightBorder);
            else if (region.Id == UiLayout.ToggleBorderId)
                DrawUtilityIconButton(frame, region, UtilityIcon.Outline, active, hover, drawLeftBorder, drawRightBorder);
            else if (region.Id == UiLayout.ToggleHullScanId)
                DrawUtilityIconButton(frame, region, UtilityIcon.FilledOutline, active, hover, drawLeftBorder, drawRightBorder);
            else if (region.Id == UiLayout.CycleScanModeId)
                DrawUtilityIconButton(frame, region, UtilityIcon.ScanMode, active, hover, drawLeftBorder, drawRightBorder);
            else if (region.Id == UiLayout.CycleScanColorScaleId)
                DrawUtilityIconButton(frame, region, UtilityIcon.ColorScale, active, hover, drawLeftBorder, drawRightBorder);
            else if (region.Id == UiLayout.ToggleGridId)
                DrawUtilityIconButton(frame, region, UtilityIcon.Grid, active, hover, drawLeftBorder, drawRightBorder);
            else if (region.Id == UiLayout.ToggleReferenceId)
                DrawUtilityIconButton(frame, region, UtilityIcon.Reference, active, hover, drawLeftBorder, drawRightBorder);
            else if (region.Id == UiLayout.ToggleCenterOfMassId)
                DrawUtilityIconButton(frame, region, UtilityIcon.CenterOfMass, active, hover, drawLeftBorder, drawRightBorder);
            else if (region.Id == UiLayout.TogglePanelPositionId)
                DrawUtilityIconButton(frame, region, UtilityIcon.PanelPosition, active, hover, drawLeftBorder, drawRightBorder);
            else if (region.Id == UiLayout.ToggleDockedMobileGridsId)
                DrawUtilityIconButton(frame, region, UtilityIcon.DockedMobileGrids, active, hover, drawLeftBorder, drawRightBorder);
            else
                DrawViewButton(frame, region, string.Empty, active, hover);
        }

        static void DrawSliderButton(MySpriteDrawFrame frame, HitRegion region, string label, float ratio, bool hover)
        {
            if (ratio < 0f)
                ratio = 0f;
            if (ratio > 1f)
                ratio = 1f;

            var center = new Vector2(region.X + region.Width * 0.5f, region.Y + region.Height * 0.5f);
            var size = new Vector2(region.Width, region.Height);
            var fill = hover ? UiMenuButtonHover : UiMenuButtonFill;

            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, size, fill));
            AddSprite(frame, new MySprite(
                SpriteType.TEXT,
                label,
                new Vector2(region.X + region.Height * 0.20f, center.Y + ButtonTextBaselineOffset(region)),
                null,
                hover ? UiText : UiTextMuted,
                CurrentTextFontId,
                TextAlignment.LEFT,
                ButtonTextScale(region) * 0.76f
            ));

            float scale = region.Height / 24f;
            float barX = region.X + region.Height * 2.08f;
            float barW = Math.Max(region.Height * 0.42f, region.Width - region.Height * 2.42f);
            var barCenter = new Vector2(barX + barW * 0.5f, center.Y + 2f * scale);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", barCenter, new Vector2(barW, Math.Max(1f, 3f * scale)), UiAccentGhost));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(barX + barW * ratio, center.Y + 2f * scale), new Vector2(Math.Max(2f, 4f * scale), Math.Max(5f, 9f * scale)), UiSelected));
        }

        static partial void DrawBottomSchematicButtons(MySpriteDrawFrame frame, int screenWidth, int screenHeight, OverlayMode activeOverlay, bool showConveyorOverlay, bool showInfoPanel, InfoPanelMode infoPanelMode, MenuPanel activeMenu, string fillMode, string hoverRegionId, bool includeThrust)
        {
            var regions = UiLayout.BuildBottomSchematicRegions(screenWidth, screenHeight, includeThrust);
            var infoRegions = UiLayout.BuildBottomInfoRegions(screenWidth, screenHeight);
            DrawBottomPanelBar(frame, screenWidth, screenHeight);
            DrawBottomPanelFillerButtons(frame, screenWidth, screenHeight, regions, infoRegions);
            if (regions == null || regions.Length == 0)
                return;

            for (int i = 0; i < regions.Length; i++)
            {
                var region = regions[i];
                bool hover = string.Equals(region.Id, hoverRegionId, StringComparison.Ordinal);
                bool active = false;
                if (region.Id == UiLayout.SchematicCargoId)
                    active = activeOverlay == OverlayMode.Cargo;
                else if (region.Id == UiLayout.SchematicEnginesId)
                    active = activeOverlay == OverlayMode.Engines;
                else if (region.Id == UiLayout.SchematicPowerId)
                    active = activeOverlay == OverlayMode.Power;
                else if (region.Id == UiLayout.SchematicOxygenId)
                    active = activeOverlay == OverlayMode.Oxygen;

                string label = region.Id == UiLayout.SchematicCargoId ? "CARGO" :
                    region.Id == UiLayout.SchematicEnginesId ? (UiLayout.BuildSurfaceProfile(screenWidth, screenHeight).UsesCompactScaling ? "THR" : "THRUST") :
                    region.Id == UiLayout.SchematicPowerId ? "POWER" :
                    region.Id == UiLayout.SchematicOxygenId ? (UiLayout.BuildSurfaceProfile(screenWidth, screenHeight).UsesCompactScaling ? "O2" : "OXYGEN") : region.Hint;
                DrawViewButton(frame, region, label, active, hover);
            }

            for (int i = 0; i < infoRegions.Length; i++)
            {
                var region = infoRegions[i];
                bool hover = string.Equals(region.Id, hoverRegionId, StringComparison.Ordinal);
                bool active = showInfoPanel && infoPanelMode == InfoPanelMode.Systems;
                DrawViewButton(frame, region, "INFO", active, hover);
            }
               var drawerToggle = UiLayout.BuildInfoDrawerToggleRegion(screenWidth, screenHeight, showInfoPanel && infoPanelMode == InfoPanelMode.Systems);
            if (!(showInfoPanel && infoPanelMode == InfoPanelMode.Systems) && !string.IsNullOrEmpty(drawerToggle.Id) && drawerToggle.Width > 0 && drawerToggle.Height > 0)
            {
                bool hover = string.Equals(drawerToggle.Id, hoverRegionId, StringComparison.Ordinal);
                DrawViewButton(frame, drawerToggle, showInfoPanel && infoPanelMode == InfoPanelMode.Systems ? "HIDE" : "SHOW", false, hover);
            }
     }

        static void DrawBottomPanelFillerButtons(MySpriteDrawFrame frame, int screenWidth, int screenHeight, HitRegion[] primaryRegions, HitRegion[] secondaryRegions)
        {
            var zones = UiLayout.BuildZones(screenWidth, screenHeight);
            int y = zones.Bottom.Y;
            int height = zones.Bottom.Height;
            int x = 0;
            DrawBottomPanelFillerBeforeRegions(frame, ref x, y, height, primaryRegions);
            DrawBottomPanelFillerBeforeRegions(frame, ref x, y, height, secondaryRegions);
            if (x < screenWidth)
                DrawCachedButtonBase(frame, new HitRegion(x, y, screenWidth - x, height, "bottom:filler:end", string.Empty), string.Empty, false, false, false);
        }

        static void DrawBottomPanelFillerBeforeRegions(MySpriteDrawFrame frame, ref int x, int y, int height, HitRegion[] regions)
        {
            if (regions == null)
                return;

            for (int i = 0; i < regions.Length; i++)
            {
                var region = regions[i];
                if (region.Y != y || region.Height != height)
                    continue;
                if (region.X > x)
                    DrawCachedButtonBase(frame, new HitRegion(x, y, region.X - x, height, "bottom:filler:" + x.ToString(), string.Empty), string.Empty, false, false, false);
                if (region.X + region.Width > x)
                    x = region.X + region.Width;
            }
        }

        static void DrawBottomPanelBar(MySpriteDrawFrame frame, int screenWidth, int screenHeight)
        {
            var zones = UiLayout.BuildZones(screenWidth, screenHeight);
            DrawStaticPanelBar(frame, screenWidth, zones.Bottom.Y, zones.Bottom.Height);
        }

        static void DrawTopPanelFillerButtons(MySpriteDrawFrame frame, int screenWidth, HitRegion[] primaryRegions, HitRegion[] secondaryRegions)
        {
            int y;
            int height;
            if (!TryGetRegionBand(primaryRegions, out y, out height) && !TryGetRegionBand(secondaryRegions, out y, out height))
                return;

            int x = 0;
            DrawTopPanelFillerBeforeRegions(frame, ref x, y, height, primaryRegions);
            DrawTopPanelFillerBeforeRegions(frame, ref x, y, height, secondaryRegions);
            if (x < screenWidth)
                DrawCachedButtonBase(frame, new HitRegion(x, y, screenWidth - x, height, "top:filler:end", string.Empty), string.Empty, false, false, false);
        }

        static void DrawTopPanelFillerBeforeRegions(MySpriteDrawFrame frame, ref int x, int y, int height, HitRegion[] regions)
        {
            if (regions == null)
                return;

            for (int i = 0; i < regions.Length; i++)
            {
                var region = regions[i];
                if (region.Y != y || region.Height != height)
                    continue;
                if (region.X > x)
                    DrawCachedButtonBase(frame, new HitRegion(x, y, region.X - x, height, "top:filler:" + x.ToString(), string.Empty), string.Empty, false, false, false);
                if (region.X + region.Width > x)
                    x = region.X + region.Width;
            }
        }

        static void DrawStaticPanelBarForRegions(MySpriteDrawFrame frame, int screenWidth, HitRegion[] primaryRegions, HitRegion[] secondaryRegions)
        {
            int y;
            int height;
            if (!TryGetRegionBand(primaryRegions, out y, out height) && !TryGetRegionBand(secondaryRegions, out y, out height))
                return;

            int secondaryY;
            int secondaryHeight;
            if (TryGetRegionBand(secondaryRegions, out secondaryY, out secondaryHeight))
            {
                int top = Math.Min(y, secondaryY);
                int bottom = Math.Max(y + height, secondaryY + secondaryHeight);
                y = top;
                height = Math.Max(1, bottom - top);
            }

            DrawStaticPanelBar(frame, screenWidth, y, height);
        }

        static bool TryGetRegionBand(HitRegion[] regions, out int y, out int height)
        {
            y = 0;
            height = 0;
            if (regions == null || regions.Length == 0)
                return false;

            int top = regions[0].Y;
            int bottom = regions[0].Y + regions[0].Height;
            for (int i = 1; i < regions.Length; i++)
            {
                if (regions[i].Y < top)
                    top = regions[i].Y;
                if (regions[i].Y + regions[i].Height > bottom)
                    bottom = regions[i].Y + regions[i].Height;
            }

            y = top;
            height = Math.Max(1, bottom - top);
            return true;
        }

        static void DrawStaticPanelBar(MySpriteDrawFrame frame, int screenWidth, int y, int height)
        {
            var center = SnapPoint(new Vector2(screenWidth * 0.5f, y + height * 0.5f));
            var size = new Vector2(SnapPixelSize(screenWidth), SnapPixelSize(height));
            float line = Math.Max(1f, height / 24f);
            AddSprite(frame, new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                center,
                size,
                UiMenuBarFill
            ));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(center.X, SnapPixel(y)), new Vector2(size.X, line), UiAccentDim));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(center.X, SnapPixel(y + height)), new Vector2(size.X, line), UiAccentDim));
        }

        static partial void DrawMenuPanelBackground(MySpriteDrawFrame frame, HitRegion[] regions)
        {
            if (regions == null || regions.Length == 0)
                return;

            int left = regions[0].X;
            int top = regions[0].Y;
            int right = regions[0].X + regions[0].Width;
            int bottom = regions[0].Y + regions[0].Height;
            for (int i = 1; i < regions.Length; i++)
            {
                var region = regions[i];
                if (region.X < left)
                    left = region.X;
                if (region.Y < top)
                    top = region.Y;
                if (region.X + region.Width > right)
                    right = region.X + region.Width;
                if (region.Y + region.Height > bottom)
                    bottom = region.Y + region.Height;
            }

            var center = new Vector2((left + right) * 0.5f, (top + bottom) * 0.5f);
            var size = new Vector2(right - left, bottom - top);
            AddSprite(frame, new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                center,
                size,
                new Color(UiMenuDropdownFill.R, UiMenuDropdownFill.G, UiMenuDropdownFill.B, 168)
            ));
            DrawScreenRectBorder(frame, center, size, UiAccentDim);
        }

        static partial void DrawViewButton(MySpriteDrawFrame frame, HitRegion region, string label, bool active, bool hover)
        {
            DrawCachedButtonBase(frame, region, label, active, hover, true);
        }

        static void DrawCachedButtonBase(MySpriteDrawFrame frame, HitRegion region, string label, bool active, bool hover, bool includeText)
        {
            DrawCachedButtonBase(frame, region, label, active, hover, includeText, 1f);
        }

        static void DrawCachedButtonBase(MySpriteDrawFrame frame, HitRegion region, string label, bool active, bool hover, bool includeText, float textScaleMultiplier)
        {
            DrawCachedButtonBase(frame, region, label, active, hover, includeText, textScaleMultiplier, true, true);
        }

        static void DrawCachedButtonBase(MySpriteDrawFrame frame, HitRegion region, string label, bool active, bool hover, bool includeText, float textScaleMultiplier, bool drawLeftBorder, bool drawRightBorder)
        {
            DrawCachedButtonBase(frame, region, label, active, hover, includeText, textScaleMultiplier, drawLeftBorder, drawRightBorder, CurrentTextFontId);
        }

        static void DrawCachedButtonBase(MySpriteDrawFrame frame, HitRegion region, string label, bool active, bool hover, bool includeText, float textScaleMultiplier, bool drawLeftBorder, bool drawRightBorder, string fontId)
        {
            var center = SnapPoint(new Vector2(region.X + region.Width * 0.5f, region.Y + region.Height * 0.5f));
            var size = new Vector2(SnapPixelSize(region.Width), SnapPixelSize(region.Height));
            var selected = DimSelectedUiColor(UiSelected);
            var selectedHover = UiSelected;
            var activeFill = DimSelectedUiColor(UiMenuButtonActive);
            var activeHoverFill = BlendUiColor(activeFill, UiSelected, 0.30f, activeFill.A);
            var fill = active ? hover ? activeHoverFill : activeFill : hover ? UiMenuButtonHover : UiMenuButtonFill;
            var textColor = active ? Color.White : hover ? UiText : UiTextMuted;
            var borderColor = active ? hover ? selectedHover : selected : hover ? UiAccentSoft : UiAccentDim;
            float textScale = includeText ? FitUiTextScale(label, ButtonTextScale(region) * textScaleMultiplier, GetButtonTextAvailableWidth(region), fontId) : 0f;
            string cacheKey = BuildUiButtonVisualCacheKey(region, label, active, hover, includeText, fill, textColor, borderColor, drawLeftBorder, drawRightBorder, fontId, textScale);
            CachedUiButtonVisual cached;
            if (UiButtonVisualCache.TryGetValue(cacheKey, out cached) && cached != null)
            {
                TrackCacheHit();
                cached.LastUsed = ++CacheUseCounter;
                AddCachedSprites(frame, cached.Sprites);
                return;
            }
            TrackCacheMiss();

            cached = new CachedUiButtonVisual();
            cached.LastUsed = ++CacheUseCounter;
            var sprites = cached.Sprites;

            sprites.Add(new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                center,
                size,
                fill
            ));
            if (active)
            {
                int selectedWashAlpha = hover ? 44 : 24;
                var selectedWash = hover ? selectedHover : selected;
                sprites.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", center, new Vector2(Math.Max(1f, size.X - 2f), Math.Max(1f, size.Y - 2f)), new Color(selectedWash.R, selectedWash.G, selectedWash.B, selectedWashAlpha)));
            }
            AddScreenRectBorderSpriteSet(sprites, center, size, borderColor, drawLeftBorder, drawRightBorder);
            if (includeText)
            {
                sprites.Add(new MySprite(
                    SpriteType.TEXT,
                    label,
                    SnapPoint(new Vector2(center.X, center.Y + ButtonTextBaselineOffset(region))),
                    null,
                    textColor,
                    fontId,
                    TextAlignment.CENTER,
                    textScale
                ));
            }

            if (active)
                AddScreenRectBorderSpriteSet(sprites, center, size, selected, drawLeftBorder, drawRightBorder);

            UiButtonVisualCache[cacheKey] = cached;
            TrimUiButtonVisualCache();
            AddCachedSprites(frame, sprites);
        }

        enum UtilityIcon
        {
            Network,
            FillBars,
            Outline,
            FilledOutline,
            ScanMode,
            ColorScale,
            Grid,
            Reference,
            CenterOfMass,
            PanelPosition,
            DockedMobileGrids
        }

        static void DrawUtilityIconButton(MySpriteDrawFrame frame, HitRegion region, UtilityIcon icon, bool active, bool hover)
        {
            DrawUtilityIconButton(frame, region, icon, active, hover, true, true);
        }

        static void DrawUtilityIconButton(MySpriteDrawFrame frame, HitRegion region, UtilityIcon icon, bool active, bool hover, bool drawLeftBorder, bool drawRightBorder)
        {
            var center = SnapPoint(new Vector2(region.X + region.Width * 0.5f, region.Y + region.Height * 0.5f));
            var iconColor = active ? Color.White : hover ? UiText : UiTextMuted;
            float scale = region.Height / 24f;
            if (scale < 0.70f)
                scale = 0.70f;
            if (scale > 1.45f)
                scale = 1.45f;

            DrawCachedButtonBase(frame, region, string.Empty, active, hover, false, 1f, drawLeftBorder, drawRightBorder);

            string texture = GetUtilityIconTexture(icon);
            if (!string.IsNullOrEmpty(texture))
            {
                float iconSize = SnapPixelSize(Math.Min(region.Width, region.Height) * 0.68f);
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, texture, center, new Vector2(iconSize, iconSize), iconColor));
                return;
            }

            if (icon == UtilityIcon.Network)
                DrawNetworkGlyph(frame, center, iconColor, scale);
            else if (icon == UtilityIcon.FillBars)
                DrawFillBarsGlyph(frame, center, iconColor, scale);
            else if (icon == UtilityIcon.Outline)
                DrawOutlineGlyph(frame, center, iconColor, scale);
            else if (icon == UtilityIcon.FilledOutline)
                DrawFilledOutlineGlyph(frame, center, iconColor, scale);
            else if (icon == UtilityIcon.ScanMode)
                DrawScanModeGlyph(frame, center, iconColor, scale);
            else if (icon == UtilityIcon.ColorScale)
                DrawColorScaleGlyph(frame, center, scale);
            else if (icon == UtilityIcon.Grid)
                DrawGridGlyph(frame, center, iconColor, scale);
            else if (icon == UtilityIcon.CenterOfMass)
                DrawCenterOfMassGlyph(frame, center, iconColor, scale);
            else if (icon == UtilityIcon.PanelPosition)
                DrawPanelPositionGlyph(frame, center, iconColor, scale);
            else if (icon == UtilityIcon.DockedMobileGrids)
                DrawDockedMobileGridsGlyph(frame, center, iconColor, scale);
            else
                DrawReferenceGlyph(frame, center, iconColor, scale);
        }

        static string GetUtilityIconTexture(UtilityIcon icon)
        {
            if (icon == UtilityIcon.Network)
                return UiIconConveyorTexture;
            if (icon == UtilityIcon.FillBars)
                return UiIconFillBarsTexture;
            if (icon == UtilityIcon.Outline)
                return UiIconBorderTexture;
            if (icon == UtilityIcon.FilledOutline)
                return UiIconHullScanTexture;
            if (icon == UtilityIcon.Grid)
                return UiIconGridTexture;
            if (icon == UtilityIcon.Reference)
                return UiIconReferenceTexture;

            return null;
        }

        static string BuildUiButtonVisualCacheKey(HitRegion region, string label, bool active, bool hover, bool includeText, Color fill, Color textColor, Color borderColor, bool drawLeftBorder, bool drawRightBorder, string fontId, float textScale)
        {
            return region.X + ":" + region.Y + ":" + region.Width + ":" + region.Height + ":" +
                (label ?? string.Empty) + ":" +
                (active ? "S" : hover ? "H" : "I") + ":" +
                (includeText ? "T" : "N") + ":" +
                (drawLeftBorder ? "L" : "l") + (drawRightBorder ? "R" : "r") + ":" +
                ColorKey(fill) + ":" + ColorKey(textColor) + ":" + ColorKey(borderColor) + ":" + ColorKey(UiSelected) + ":" +
                (fontId ?? string.Empty) + ":" + ((int)(textScale * 1000f)).ToString();
        }

        static Color DimSelectedUiColor(Color color)
        {
            return new Color(
                (byte)Math.Round(color.R * 0.85f),
                (byte)Math.Round(color.G * 0.85f),
                (byte)Math.Round(color.B * 0.85f),
                color.A);
        }

        static Color BlendUiColor(Color from, Color to, float amount, byte alpha)
        {
            if (amount < 0f)
                amount = 0f;
            if (amount > 1f)
                amount = 1f;

            return new Color(
                (byte)Math.Round(from.R + (to.R - from.R) * amount),
                (byte)Math.Round(from.G + (to.G - from.G) * amount),
                (byte)Math.Round(from.B + (to.B - from.B) * amount),
                alpha);
        }

        static string ColorKey(Color color)
        {
            return color.R + "," + color.G + "," + color.B + "," + color.A;
        }

        static void AddCachedSprites(MySpriteDrawFrame frame, List<MySprite> sprites)
        {
            if (sprites == null)
                return;

            for (int i = 0; i < sprites.Count; i++)
                AddCachedSprite(frame, sprites[i]);
        }

        static void AddScreenRectBorderSpriteSet(List<MySprite> sprites, Vector2 center, Vector2 size, Color color)
        {
            AddScreenRectBorderSpriteSet(sprites, center, size, color, true, true);
        }

        static void AddScreenRectBorderSpriteSet(List<MySprite> sprites, Vector2 center, Vector2 size, Color color, bool drawLeftBorder, bool drawRightBorder)
        {
            float left = SnapPixel(center.X - size.X * 0.5f);
            float right = SnapPixel(center.X + size.X * 0.5f);
            float top = SnapPixel(center.Y - size.Y * 0.5f);
            float bottom = SnapPixel(center.Y + size.Y * 0.5f);
            float snappedCenterX = SnapPixel((left + right) * 0.5f);
            float snappedCenterY = SnapPixel((top + bottom) * 0.5f);
            float width = Math.Max(1f, SnapPixelSize(right - left));
            float height = Math.Max(1f, SnapPixelSize(bottom - top));

            sprites.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(snappedCenterX, top), new Vector2(width, 1f), color));
            sprites.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(snappedCenterX, bottom), new Vector2(width, 1f), color));
            if (drawLeftBorder)
                sprites.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(left, snappedCenterY), new Vector2(1f, height), color));
            if (drawRightBorder)
                sprites.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(right, snappedCenterY), new Vector2(1f, height), color));
        }

        static void TrimUiButtonVisualCache()
        {
            while (UiButtonVisualCache.Count > MaxUiButtonVisualCacheEntries)
            {
                string oldestKey = null;
                int oldestUse = int.MaxValue;
                foreach (var pair in UiButtonVisualCache)
                {
                    if (pair.Value == null || pair.Value.LastUsed < oldestUse)
                    {
                        oldestKey = pair.Key;
                        oldestUse = pair.Value == null ? int.MinValue : pair.Value.LastUsed;
                    }
                }

                if (oldestKey == null)
                    return;
                UiButtonVisualCache.Remove(oldestKey);
            }
        }

        static void DrawButtonSelectionBorder(MySpriteDrawFrame frame, Vector2 center, Vector2 size)
        {
            DrawScreenRectBorder(frame, center, size, DimSelectedUiColor(UiSelected));
        }

        static void DrawChromeToggleButton(MySpriteDrawFrame frame, HitRegion region, bool minimized, bool hover)
        {
            var center = SnapPoint(new Vector2(region.X + region.Width * 0.5f, region.Y + region.Height * 0.5f));
            var size = new Vector2(SnapPixelSize(region.Width), SnapPixelSize(region.Height));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, size, hover ? UiMenuButtonHover : UiMenuButtonFill));
            DrawScreenRectBorder(frame, center, size, hover ? UiAccentSoft : UiAccentDim);
            Color color = hover ? UiText : UiTextMuted;
            string texture = minimized ? UiIconMinimizeTexture : UiIconCloseTexture;
            float iconSize = SnapPixelSize(Math.Min(region.Width, region.Height) * 0.66f);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, texture, center, new Vector2(iconSize, iconSize), color));
        }

        static void DrawChromeGlyph(MySpriteDrawFrame frame, Vector2 center, Color color, bool minimized, float iconSize)
        {
            float thickness = Math.Max(1f, iconSize * 0.12f);
            if (minimized)
            {
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, new Vector2(iconSize * 0.70f, thickness), color));
                return;
            }

            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, new Vector2(iconSize * 0.78f, thickness), color, null, TextAlignment.CENTER, 0.7853982f));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, new Vector2(iconSize * 0.78f, thickness), color, null, TextAlignment.CENTER, -0.7853982f));
        }

        static partial void DrawChromeRestoreButton(MySpriteDrawFrame frame, int screenWidth, int screenHeight, bool minimized, string hoverRegionId)
        {
            var region = minimized ? UiLayout.BuildChromeRestoreRegion(screenWidth, screenHeight) : new HitRegion(screenWidth - UiLayout.BuildMetrics(screenWidth, screenHeight).TopRowHeight, 0, UiLayout.BuildMetrics(screenWidth, screenHeight).TopRowHeight, UiLayout.BuildMetrics(screenWidth, screenHeight).TopRowHeight, UiLayout.ToggleChromeId, "Hide menu chrome");
            DrawChromeToggleButton(frame, region, minimized, string.Equals(region.Id, hoverRegionId, StringComparison.Ordinal));
        }

        static float ButtonTextScale(HitRegion region)
        {
            float scale = UiButtonTextScale * region.Height / 24f * CurrentButtonTextScaleMultiplier;
            if (scale < 0.30f)
                return 0.30f;
            if (scale > 0.78f)
                return 0.78f;
            return scale;
        }

        static float GetButtonTextAvailableWidth(HitRegion region)
        {
            float inset = Math.Max(3f, region.Height * 0.22f);
            float width = region.Width - inset * 2f;
            return width < 1f ? 1f : width;
        }

        static float EstimateUiTextWidth(string text, float scale, string fontId)
        {
            if (string.IsNullOrEmpty(text))
                return 0f;

            float units = 0f;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == ' ' || c == ':' || c == '.' || c == '-' || c == '/')
                    units += 0.55f;
                else if (c == 'I' || c == '1' || c == '!')
                    units += 0.62f;
                else if (c == 'M' || c == 'W')
                    units += 1.22f;
                else
                    units += 1f;
            }

            float fontWidth = 17.0f;
            return units * fontWidth * scale;
        }

        static float FitUiTextScale(string text, float desiredScale, float availableWidth, string fontId)
        {
            if (!CurrentDynamicTextFitEnabled)
                return desiredScale;

            if (string.IsNullOrEmpty(text) || availableWidth <= 1f)
                return desiredScale;

            float width = EstimateUiTextWidth(text, desiredScale, fontId);
            if (width <= availableWidth)
                return desiredScale;

            float scale = desiredScale * availableWidth / width;
            float minScale = 0.22f;
            if (scale < minScale)
                scale = minScale;
            return scale;
        }

        static float ButtonTextBaselineOffset(HitRegion region)
        {
            return UiButtonTextBaselineOffset * region.Height / 24f + CurrentButtonTextBaselineNudge;
        }

        static void DrawOutlineGlyph(MySpriteDrawFrame frame, Vector2 center, Color color, float scale)
        {
            float r = 5.5f * scale;
            float s = 3.8f * scale;
            float line = Math.Max(1f, 1.4f * scale);
            DrawScreenLine(frame, center + new Vector2(-s, -r), center + new Vector2(s, -r), line, color);
            DrawScreenLine(frame, center + new Vector2(s, -r), center + new Vector2(r, -s), line, color);
            DrawScreenLine(frame, center + new Vector2(r, -s), center + new Vector2(r, s), line, color);
            DrawScreenLine(frame, center + new Vector2(r, s), center + new Vector2(s, r), line, color);
            DrawScreenLine(frame, center + new Vector2(s, r), center + new Vector2(-s, r), line, color);
            DrawScreenLine(frame, center + new Vector2(-s, r), center + new Vector2(-r, s), line, color);
            DrawScreenLine(frame, center + new Vector2(-r, s), center + new Vector2(-r, -s), line, color);
            DrawScreenLine(frame, center + new Vector2(-r, -s), center + new Vector2(-s, -r), line, color);
        }

        static void DrawNetworkGlyph(MySpriteDrawFrame frame, Vector2 center, Color color, float scale)
        {
            float line = Math.Max(1f, 1.35f * scale);
            Vector2 hub = center + new Vector2(-4f * scale, 0f);
            Vector2 upper = center + new Vector2(3f * scale, -5f * scale);
            Vector2 mid = center + new Vector2(5f * scale, 0f);
            Vector2 lower = center + new Vector2(3f * scale, 5f * scale);
            DrawScreenLine(frame, hub, upper, line, color);
            DrawScreenLine(frame, hub, mid, line, color);
            DrawScreenLine(frame, hub, lower, line, color);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "Circle", hub, new Vector2(3.2f * scale, 3.2f * scale), color));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", upper, new Vector2(3.4f * scale, 3.4f * scale), color));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", mid, new Vector2(3.4f * scale, 3.4f * scale), color));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", lower, new Vector2(3.4f * scale, 3.4f * scale), color));
        }

        static void DrawFillBarsGlyph(MySpriteDrawFrame frame, Vector2 center, Color color, float scale)
        {
            float width = Math.Max(2f, 3f * scale);
            float height = Math.Max(6f, 12f * scale);
            float bottom = center.Y + 6f * scale;
            var leftCenter = new Vector2(center.X - 4f * scale, bottom - height * 0.34f);
            var rightCenter = new Vector2(center.X + 4f * scale, bottom - height * 0.58f);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", leftCenter, new Vector2(width, height * 0.68f), color));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", rightCenter, new Vector2(width, height), color));
        }

        static void DrawFilledOutlineGlyph(MySpriteDrawFrame frame, Vector2 center, Color color, float scale)
        {
            DrawOutlineGlyph(frame, center, color, scale);
            AddSprite(frame, new MySprite(
                SpriteType.TEXTURE,
                "Circle",
                center,
                new Vector2(7f * scale, 7f * scale),
                color
            ));
        }

        static void DrawScanModeGlyph(MySpriteDrawFrame frame, Vector2 center, Color color, float scale)
        {
            float line = Math.Max(1f, 1.25f * scale);
            float left = center.X - 6f * scale;
            float right = center.X + 6f * scale;
            float mid = center.Y;
            DrawScreenLine(frame, new Vector2(left, mid), new Vector2(right, mid), line, color);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(left + 1f * scale, mid - 4f * scale), new Vector2(3f * scale, 5f * scale), color));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(center.X, mid), new Vector2(3f * scale, 9f * scale), color));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(right - 1f * scale, mid + 3f * scale), new Vector2(3f * scale, 4f * scale), color));
        }

        static void DrawColorScaleGlyph(MySpriteDrawFrame frame, Vector2 center, float scale)
        {
            float width = 3.3f * scale;
            float height = 11f * scale;
            Color cold = CurrentHullScanColorScale == GridSchematicsConfig.HullColorThermal ? new Color(55, 36, 190) : UiMenuButtonFill;
            Color mid = CurrentHullScanColorScale == GridSchematicsConfig.HullColorThermal ? new Color(210, 20, 170) : BlendUiColor(UiMenuButtonFill, UiSelected, 0.5f, 255);
            Color hot = CurrentHullScanColorScale == GridSchematicsConfig.HullColorThermal ? new Color(255, 235, 60) : UiSelected;
            if (CurrentHullScanColorScale == GridSchematicsConfig.HullColorGreyscale)
            {
                cold = new Color(70, 70, 70);
                mid = new Color(150, 150, 150);
                hot = new Color(235, 235, 235);
            }

            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center + new Vector2(-4f * scale, 0f), new Vector2(width, height), cold));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, new Vector2(width, height), mid));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center + new Vector2(4f * scale, 0f), new Vector2(width, height), hot));
        }

        static void DrawGridGlyph(MySpriteDrawFrame frame, Vector2 center, Color color, float scale)
        {
            float half = 6f * scale;
            float offset = 2.5f * scale;
            float line = Math.Max(1f, 1.25f * scale);
            DrawScreenLine(frame, center + new Vector2(-offset, -half), center + new Vector2(-offset, half), line, color);
            DrawScreenLine(frame, center + new Vector2(offset, -half), center + new Vector2(offset, half), line, color);
            DrawScreenLine(frame, center + new Vector2(-half, -offset), center + new Vector2(half, -offset), line, color);
            DrawScreenLine(frame, center + new Vector2(-half, offset), center + new Vector2(half, offset), line, color);
        }

        static void DrawCenterOfMassGlyph(MySpriteDrawFrame frame, Vector2 center, Color color, float scale)
        {
            float arm = 6f * scale;
            float half = 3.4f * scale;
            float line = Math.Max(1f, 1.1f * scale);
            DrawScreenLine(frame, center + new Vector2(0f, -arm), center + new Vector2(0f, -half), line, color);
            DrawScreenLine(frame, center + new Vector2(0f, half), center + new Vector2(0f, arm), line, color);
            DrawScreenLine(frame, center + new Vector2(-arm, 0f), center + new Vector2(-half, 0f), line, color);
            DrawScreenLine(frame, center + new Vector2(half, 0f), center + new Vector2(arm, 0f), line, color);
            DrawScreenLine(frame, center + new Vector2(-half, -half), center + new Vector2(half, -half), line, color);
            DrawScreenLine(frame, center + new Vector2(half, -half), center + new Vector2(half, half), line, color);
            DrawScreenLine(frame, center + new Vector2(half, half), center + new Vector2(-half, half), line, color);
            DrawScreenLine(frame, center + new Vector2(-half, half), center + new Vector2(-half, -half), line, color);
        }

        static void DrawPanelPositionGlyph(MySpriteDrawFrame frame, Vector2 center, Color color, float scale)
        {
            float half = 5.5f * scale;
            float line = Math.Max(1f, 1.15f * scale);
            DrawScreenLine(frame, center + new Vector2(-half, -half), center + new Vector2(half, -half), line, color);
            DrawScreenLine(frame, center + new Vector2(half, -half), center + new Vector2(half, half), line, color);
            DrawScreenLine(frame, center + new Vector2(half, half), center + new Vector2(-half, half), line, color);
            DrawScreenLine(frame, center + new Vector2(-half, half), center + new Vector2(-half, -half), line, color);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, new Vector2(3.2f * scale, 3.2f * scale), color));
        }
        static void DrawDockedMobileGridsGlyph(MySpriteDrawFrame frame, Vector2 center, Color color, float scale)
        {
            float line = Math.Max(1f, 1.1f * scale);
            float small = 4.1f * scale;
            float large = 6.2f * scale;
            Vector2 a = center + new Vector2(-3.8f * scale, -2.2f * scale);
            Vector2 b = center + new Vector2(3.5f * scale, 2.6f * scale);
            DrawScreenRectBorder(frame, a, new Vector2(large, small), color);
            DrawScreenRectBorder(frame, b, new Vector2(small, large), color);
            DrawScreenLine(frame, a + new Vector2(large * 0.5f, 0f), b + new Vector2(-small * 0.5f, 0f), line, color);
        }
        static void DrawReferenceGlyph(MySpriteDrawFrame frame, Vector2 center, Color color, float scale)
        {
            float arm = 6f * scale;
            float line = Math.Max(1f, 1.45f * scale);
            DrawScreenLine(frame, center + new Vector2(0f, -arm), center + new Vector2(0f, arm), line, color);
            DrawScreenLine(frame, center + new Vector2(-arm, 0f), center + new Vector2(arm, 0f), line, color);
            AddSprite(frame, new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                center,
                new Vector2(2.6f * scale, 2.6f * scale),
                color
            ));
        }

        static void DrawFlatButtonSeparator(MySpriteDrawFrame frame, Vector2 center, Vector2 size)
        {
            float right = center.X + size.X * 0.5f;
            float height = size.Y;
            AddSprite(frame, new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                new Vector2(right, center.Y),
                new Vector2(1f, height),
                UiMenuSeparator
            ));
        }

        static partial void DrawCursor(MySpriteDrawFrame frame, TouchScreenApiAdapter input, ScreenZone clipZone)
        {
            if (input == null)
                return;

            var color = input.IsAvailable
                ? (input.IsPressed ? UiSelected : UiAccentBright)
                : UiWarning;

            if (!input.IsAvailable)
                return;

            if (input.IsCursorOnScreen)
            {
                var cursor = SnapPoint(input.CursorPosition);
                float size = TouchCursorSize;
                float thickness = TouchCursorThickness;
                float half = size * 0.5f;

                if (input.IsSecondaryPressed)
                {
                    DrawClippedScreenLine(frame, clipZone, cursor + new Vector2(-half, -half), cursor + new Vector2(half, half), thickness, color);
                    DrawClippedScreenLine(frame, clipZone, cursor + new Vector2(-half, half), cursor + new Vector2(half, -half), thickness, color);
                    return;
                }

                DrawClippedScreenLine(frame, clipZone, cursor + new Vector2(-half, 0f), cursor + new Vector2(half, 0f), thickness, color);
                DrawClippedScreenLine(frame, clipZone, cursor + new Vector2(0f, -half), cursor + new Vector2(0f, half), thickness, color);

                if (input.IsPressed)
                {
                    Vector2 boxSize = new Vector2(size, size);
                    DrawScreenRectBorder(frame, cursor, boxSize, color);
                }
            }
        }

        static partial void DrawSharedGridCursor(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, SharedGridCursor? sharedCursor, long localPanelId, ScanView view, int rotationSteps)
        {
            if (shipGrid == null || shipGrid.IsEmpty || !sharedCursor.HasValue || !sharedCursor.Value.Active)
                return;

            var cursor = sharedCursor.Value;
            var transform = GetOrBuildProjectionTransform(shipGrid, center, rotationSteps);
            if (!transform.IsValid || transform.CellSize <= 0f)
                return;

            bool isSourcePanel = cursor.IsFromPanel(localPanelId);
            float localX;
            float localY;
            bool hasX = TryGetVisibleAxisLocalX(shipGrid, cursor, isSourcePanel, view, out localX);
            bool hasY = TryGetVisibleAxisLocalY(shipGrid, cursor, isSourcePanel, view, out localY);
            if (!hasX && !hasY)
                return;

            if (!hasX)
                localX = transform.SourceWidth * 0.5f;
            if (!hasY)
                localY = transform.SourceHeight * 0.5f;

            var localPoint = new Vector2(localX, localY);
            var centerPoint = transform.ProjectLocalPoint(localPoint.X, localPoint.Y);
            var activeColor = UiAccentBright;
            var centerColor = isSourcePanel ? UiAccentBright : UiSelected;
            float viewportExtension = (float)Math.Ceiling(Math.Max(center.Width, center.Height) / Math.Max(1f, transform.CellSize)) + 4f;

            if (hasX)
                DrawClippedScreenLine(frame, center, transform.ProjectLocalPoint(localPoint.X, -viewportExtension), transform.ProjectLocalPoint(localPoint.X, transform.SourceHeight + viewportExtension), SharedCursorLineThickness, activeColor);
            if (hasY)
                DrawClippedScreenLine(frame, center, transform.ProjectLocalPoint(-viewportExtension, localPoint.Y), transform.ProjectLocalPoint(transform.SourceWidth + viewportExtension, localPoint.Y), SharedCursorLineThickness, activeColor);
            if (hasX && hasY)
            {
                DrawClippedDashedScreenLine(frame, center, centerPoint + new Vector2(-SharedCursorCrossRadius, 0f), centerPoint + new Vector2(SharedCursorCrossRadius, 0f), 1.65f, 2f, 1f, centerColor);
                DrawClippedDashedScreenLine(frame, center, centerPoint + new Vector2(0f, -SharedCursorCrossRadius), centerPoint + new Vector2(0f, SharedCursorCrossRadius), 1.65f, 2f, 1f, centerColor);
            }
            else
            {
                AddSprite(frame, new MySprite(
                    SpriteType.TEXTURE,
                    "SquareSimple",
                    centerPoint,
                    new Vector2(2.5f, 2.5f),
                    centerColor
                ));
            }
        }

        static void DrawClippedScreenLine(MySpriteDrawFrame frame, ScreenZone zone, Vector2 start, Vector2 end, float thickness, Color color)
        {
            if (TryClipLineToZone(zone, ref start, ref end))
                DrawScreenLine(frame, start, end, thickness, color);
        }

        static void DrawClippedDashedScreenLine(MySpriteDrawFrame frame, ScreenZone zone, Vector2 start, Vector2 end, float thickness, float dashLength, float gapLength, Color color)
        {
            if (TryClipLineToZone(zone, ref start, ref end))
                DrawDashedScreenLine(frame, start, end, thickness, dashLength, gapLength, color);
        }

        static Vector2 ClampLabelPositionToZone(Vector2 position, ScreenZone zone, float approximateWidth, float approximateHeight)
        {
            float minX = zone.X + 2f;
            float maxX = zone.X + zone.Width - approximateWidth - 2f;
            float minY = zone.Y + 2f;
            float maxY = zone.Y + zone.Height - approximateHeight - 2f;
            if (maxX < minX)
                maxX = minX;
            if (maxY < minY)
                maxY = minY;

            return new Vector2(
                Clamp(position.X, minX, maxX),
                Clamp(position.Y, minY, maxY));
        }

        static bool TryClipLineToZone(ScreenZone zone, ref Vector2 start, ref Vector2 end)
        {
            float left = zone.X;
            float right = zone.X + zone.Width;
            float top = zone.Y;
            float bottom = zone.Y + zone.Height;
            float dx = end.X - start.X;
            float dy = end.Y - start.Y;
            float t0 = 0f;
            float t1 = 1f;

            if (!ClipTest(-dx, start.X - left, ref t0, ref t1))
                return false;
            if (!ClipTest(dx, right - start.X, ref t0, ref t1))
                return false;
            if (!ClipTest(-dy, start.Y - top, ref t0, ref t1))
                return false;
            if (!ClipTest(dy, bottom - start.Y, ref t0, ref t1))
                return false;

            var originalStart = start;
            if (t1 < 1f)
                end = originalStart + new Vector2(dx * t1, dy * t1);
            if (t0 > 0f)
                start = originalStart + new Vector2(dx * t0, dy * t0);

            return true;
        }

        static bool ClipTest(float p, float q, ref float t0, ref float t1)
        {
            if (Math.Abs(p) <= 0.0001f)
                return q >= 0f;

            float r = q / p;
            if (p < 0f)
            {
                if (r > t1)
                    return false;
                if (r > t0)
                    t0 = r;
            }
            else
            {
                if (r < t0)
                    return false;
                if (r < t1)
                    t1 = r;
            }

            return true;
        }

        static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}



