using System;
using System.Collections.Generic;
using VRageMath;

namespace GridSchematics
{
    public enum ScreenZoneType
    {
        TopRow,
        LeftRail,
        RightRail,
        BottomStrip,
        CenterViewport
    }

    public enum UiSurfaceLayoutMode
    {
        Standard,
        Compact,
        Wide,
        Tall
    }

    public struct UiSurfaceProfile
    {
        public int Width;
        public int Height;
        public int MinSide;
        public float AspectRatio;
        public UiSurfaceLayoutMode Mode;
        public bool IsCanonical512Square;
        public bool UsesCompactScaling;
        public bool IsWide;
        public bool IsTall;
        public bool AllowInfoPanel;
        public bool AllowFullInfoPanel;
        public bool AllowHeaderInfoPanel;

        public bool IsStandard
        {
            get { return Mode == UiSurfaceLayoutMode.Standard; }
        }
    }

    public struct ScreenZone
    {
        public ScreenZoneType Type;
        public int X;
        public int Y;
        public int Width;
        public int Height;

        public ScreenZone(ScreenZoneType type, int x, int y, int width, int height)
        {
            Type = type;
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    public struct UiLayoutContext
    {
        public UiSurfaceProfile Profile;
        public UiMetrics Metrics;
        public ScreenZone Surface;
        public ScreenZone SafeContent;

        public UiLayoutContext(UiSurfaceProfile profile, UiMetrics metrics, ScreenZone surface, ScreenZone safeContent)
        {
            Profile = profile;
            Metrics = metrics;
            Surface = surface;
            SafeContent = safeContent;
        }
    }

    public struct UiMetrics
    {
        public float Scale;
        public int TopRowHeight;
        public int BottomStripHeight;
        public int InfoHeaderHeight;
        public int InfoPanelMinHeight;
        public int InfoPanelReservedMapHeight;
        public float TinyText;
        public float SmallText;
        public float MediumText;
        public float LargeText;
        public float Padding;
        public float Gap;
        public float Line;
        public bool IsCompact;
        public bool IsWide;
        public bool IsTall;

        public float S(float value)
        {
            return value * Scale;
        }

        public int SI(float value)
        {
            int scaled = (int)(value * Scale + 0.5f);
            return scaled < 1 ? 1 : scaled;
        }
    }

    public static class UiLayout
    {
        public static int TopRowHeight = 24;
        public static int BottomStripHeight = 24;
        public static int RailWidth = 0;
        public static int CenterMargin = 0;
        public const string MenuViewId = "menu:view";
        public const string MenuLayersId = "menu:layers";
        public const string MenuScanId = "menu:scan";
        public const string MenuSettingsId = "menu:settings";
        public const string MenuToolsId = "menu:tools";
        public const string ToggleChromeId = "menu:chrome";
        public const string ToggleInfoPanelId = "menu:info";
        public const string InfoPanelAllTabId = "info:tab:all";
        public const string InfoPanelStackTabId = "info:tab:stack";
        public const string InfoPanelBlockTabScrollId = "info:tab:scroll";
        public const string InfoPanelBlockTabPrefix = "info:tab:";
        public const string SegmentModeId = "mode:segment";
        public const string ViewTopId = "view:top";
        public const string ViewLeftId = "view:left";
        public const string ViewFrontId = "view:front";
        public const string RotateCcwId = "view:rotate:ccw";
        public const string RotateCwId = "view:rotate:cw";
        public const string ToggleBlocksId = "layer:blocks";
        public const string ToggleBorderId = "layer:border";
        public const string ToggleHullScanId = "layer:hullscan";
        public const string ToggleGridId = "layer:grid";
        public const string ToggleReferenceId = "layer:reference";
        public const string ToggleConveyorId = "layer:conveyor";
        public const string ToggleFillBarsId = "layer:fillbars";
        public const string ToggleAllConnectionsId = "layer:connections";
        public const string ToggleBlocksOccludeConveyorsId = "schematics:occlude_conveyors";
        public const string ToggleConnectedNetworksId = "schematics:connected_networks";
        public const string CycleScanColorScaleId = "scan:color";
        public const string UiBlockerId = "ui:blocker";
        public const string ToggleBlurId = "scan:blur";
        public const string TogglePerformanceModeId = "render:performance";
        public const string ToggleHighResScanningId = "scan:highres";
        public const string ToggleDebugModeId = "debug:enabled";
        public const string CyclePaletteId = "ui:palette";
        public const string AdjustPaletteHueId = "ui:palette:hue";
        public const string AdjustPaletteBrightnessId = "ui:palette:brightness";
        public const string AdjustPaletteSaturationId = "ui:palette:saturation";
        public const string AdjustPaletteAlphaId = "ui:palette:alpha";
        public const string CycleUiFontId = "ui:font";
        public const string AdjustAccentHueId = "ui:accent:hue";
        public const string AdjustAccentBrightnessId = "ui:accent:brightness";
        public const string AdjustAccentSaturationId = "ui:accent:saturation";
        public const string AdjustPanelBrightnessId = "ui:panel:brightness";
        public const string AdjustPanelAlphaId = "ui:panel:alpha";
        public const string ToggleMouseControlId = "ui:mousecontrol";
        public const string CycleMouseSensitivityId = "ui:mousecontrol:sensitivity";
        public const string ToggleGridRotationId = "ui:gridrotation";
        public const string AdjustHullScanAlphaId = "ui:hullscan:alpha";
        public const string AdjustSchematicAlphaId = "ui:schematic:alpha";
        public const string CycleStorageColorId = "ui:storage:color";
        public const string CycleEffectorColorId = "ui:effector:color";
        public const string AdjustSchematicMainHueId = "ui:schematic:main:hue";
        public const string AdjustSchematicSecondaryHueId = "ui:schematic:secondary:hue";
        public const string AdjustConveyorHueId = "ui:conveyor:hue";
        public const string ResetStyleSettingsId = "ui:style:reset";
        public const string ResetUiSettingsId = "ui:settings:reset";
        public const string SaveSettingsId = "ui:settings:save";
        public const string CopySettingsId = "ui:settings:copy";
        public const string PasteSettingsId = "ui:settings:paste";
        public const string ExportSettingsId = "ui:settings:export";
        public const string RecalibrateCursorId = "ui:cursor:recalibrate";
        public const string SettingsUiHeaderId = "settings:header:ui";
        public const string SettingsStyleHeaderId = "settings:header:style";
        public const string SettingsRenderingHeaderId = "settings:header:rendering";
        public const string SettingsSchematicsHeaderId = "settings:header:schematics";
        public const string SettingsDebugHeaderId = "settings:header:debug";
        public const string SettingsPanelDataHeaderId = "settings:header:paneldata";
        public const string CopyUiSettingsId = "settings:copy:ui";
        public const string PasteUiSettingsId = "settings:paste:ui";
        public const string CopyRenderingSettingsId = "settings:copy:rendering";
        public const string PasteRenderingSettingsId = "settings:paste:rendering";
        public const string CopySchematicSettingsId = "settings:copy:schematics";
        public const string PasteSchematicSettingsId = "settings:paste:schematics";
        public const string CopyDebugSettingsId = "settings:copy:debug";
        public const string PasteDebugSettingsId = "settings:paste:debug";
        public const string TogglePerfStatsId = "settings:debug:perfstats";
        public const string WipePanelCacheId = "settings:debug:wipecache";
        public const string ResetPanelSettingsId = "settings:debug:resetpanel";
        public const int SettingsCategoryUserInterface = 1;
        public const int SettingsCategoryRendering = 2;
        public const int SettingsCategorySchematics = 4;
        public const int SettingsCategoryDebug = 8;
        public const int SettingsCategoryPanelData = 16;
        public const int SettingsCategoryStyle = 32;
        public const string SetDensityId = "scan:set:density";
        public const string SetThicknessId = "scan:set:thickness";
        public const string SetVoidsId = "scan:set:voids";
        public const string SetHitsId = "scan:set:hits";
        public const string CycleScanModeId = "scan:mode";
        public const string RunScanId = "scan:run";
        public const string CancelScanId = "scan:cancel";
        public const string CalibrationStartId = "calibration:start";
        public const string CalibrationCloseId = "calibration:close";
        public const string CalibrationPointId = "calibration:point";
        public const string SchematicCargoId = "schematic:cargo";
        public const string SchematicEnginesId = "schematic:engines";
        public const string SchematicPowerId = "schematic:power";
        public const string SchematicOxygenId = "schematic:oxygen";
        public const string SchematicConveyorId = "schematic:conveyor";

        public static UiMetrics BuildMetrics(int width, int height)
        {
            var profile = BuildSurfaceProfile(width, height);
            if (profile.UsesCompactScaling)
                return BuildCompactMetrics(profile);

            float minSide = width < height ? width : height;
            float scale = minSide / 512f;
            if (scale < 0.68f)
                scale = 0.68f;
            if (scale > 1.35f)
                scale = 1.35f;

            var metrics = new UiMetrics();
            metrics.Scale = scale;
            int headerBarHeight = metrics.SI(19f);
            metrics.TopRowHeight = headerBarHeight;
            metrics.BottomStripHeight = headerBarHeight;
            metrics.InfoHeaderHeight = headerBarHeight;
            metrics.InfoPanelMinHeight = metrics.SI(88f);
            metrics.InfoPanelReservedMapHeight = metrics.SI(64f);
            metrics.TinyText = 0.24f * scale;
            if (metrics.TinyText < 0.34f)
                metrics.TinyText = 0.34f;
            metrics.SmallText = 0.34f * scale;
            if (metrics.SmallText < 0.34f)
                metrics.SmallText = 0.34f;
            metrics.MediumText = 0.42f * scale;
            if (metrics.MediumText < 0.42f)
                metrics.MediumText = 0.42f;
            metrics.LargeText = 0.55f * scale;
            if (metrics.LargeText < 0.55f)
                metrics.LargeText = 0.55f;
            metrics.Padding = 3f * scale;
            if (metrics.Padding < 2f)
                metrics.Padding = 2f;
            metrics.Gap = 3f * scale;
            if (metrics.Gap < 2f)
                metrics.Gap = 2f;
            metrics.Line = scale < 1f ? 1f : scale;
            metrics.IsCompact = minSide < 420f;
            metrics.IsWide = width > height * 1.35f;
            metrics.IsTall = height > width * 1.20f;
            return metrics;
        }

        public static UiSurfaceProfile BuildSurfaceProfile(int width, int height)
        {
            if (width < 1)
                width = 1;
            if (height < 1)
                height = 1;

            var profile = new UiSurfaceProfile();
            profile.Width = width;
            profile.Height = height;
            profile.MinSide = width < height ? width : height;
            profile.AspectRatio = height > 0 ? width / (float)height : 1f;
            profile.IsCanonical512Square = width == 512 && height == 512;
            profile.UsesCompactScaling = profile.MinSide < 512;
            profile.IsWide = width > height * 1.35f;
            profile.IsTall = height > width * 1.20f;

            if (profile.UsesCompactScaling)
                profile.Mode = UiSurfaceLayoutMode.Compact;
            else if (profile.IsWide)
                profile.Mode = UiSurfaceLayoutMode.Wide;
            else if (profile.IsTall)
                profile.Mode = UiSurfaceLayoutMode.Tall;
            else
                profile.Mode = UiSurfaceLayoutMode.Standard;

            profile.AllowFullInfoPanel = profile.MinSide >= 420 && width >= 360 && height >= 300;
            profile.AllowHeaderInfoPanel = width >= 220 && height >= 120;
            profile.AllowInfoPanel = profile.AllowFullInfoPanel || profile.AllowHeaderInfoPanel;
            return profile;
        }

        public static UiLayoutContext BuildLayoutContext(int width, int height)
        {
            return BuildLayoutContext(new ScreenZone(ScreenZoneType.CenterViewport, 0, 0, width, height));
        }

        public static UiLayoutContext BuildLayoutContext(ScreenZone surface)
        {
            if (surface.Width < 1)
                surface.Width = 1;
            if (surface.Height < 1)
                surface.Height = 1;

            var profile = BuildSurfaceProfile(surface.Width, surface.Height);
            var metrics = BuildMetrics(surface.Width, surface.Height);
            var safeContent = surface;

            if (!profile.IsStandard)
            {
                int marginX = ClampInt((int)(surface.Width * 0.04f + 0.5f), metrics.SI(6f), metrics.SI(24f));
                int marginY = ClampInt((int)(surface.Height * (profile.IsWide ? 0.055f : 0.045f) + 0.5f), metrics.SI(5f), metrics.SI(20f));
                safeContent = new ScreenZone(
                    ScreenZoneType.CenterViewport,
                    surface.X + marginX,
                    surface.Y + marginY,
                    Math.Max(1, surface.Width - marginX * 2),
                    Math.Max(1, surface.Height - marginY * 2));
            }

            return new UiLayoutContext(profile, metrics, surface, safeContent);
        }

        static UiMetrics BuildCompactMetrics(UiSurfaceProfile profile)
        {
            float scale = profile.MinSide / 512f;
            if (scale < 0.50f)
                scale = 0.50f;
            if (scale > 0.98f)
                scale = 0.98f;

            var metrics = new UiMetrics();
            metrics.Scale = scale;
            int headerBarHeight = ClampInt((int)(19f * scale + 0.5f), 14, 18);
            metrics.TopRowHeight = headerBarHeight;
            metrics.BottomStripHeight = headerBarHeight;
            metrics.InfoHeaderHeight = headerBarHeight;
            metrics.InfoPanelMinHeight = metrics.SI(82f);
            metrics.InfoPanelReservedMapHeight = metrics.SI(72f);
            metrics.TinyText = 0.28f * scale;
            if (metrics.TinyText < 0.26f)
                metrics.TinyText = 0.26f;
            metrics.SmallText = 0.34f * scale;
            if (metrics.SmallText < 0.30f)
                metrics.SmallText = 0.30f;
            metrics.MediumText = 0.42f * scale;
            if (metrics.MediumText < 0.34f)
                metrics.MediumText = 0.34f;
            metrics.LargeText = 0.55f * scale;
            if (metrics.LargeText < 0.42f)
                metrics.LargeText = 0.42f;
            metrics.Padding = 3f * scale;
            if (metrics.Padding < 1.5f)
                metrics.Padding = 1.5f;
            metrics.Gap = 3f * scale;
            if (metrics.Gap < 1f)
                metrics.Gap = 1f;
            metrics.Line = scale < 1f ? 1f : scale;
            metrics.IsCompact = true;
            metrics.IsWide = profile.IsWide;
            metrics.IsTall = profile.IsTall;
            return metrics;
        }

        static int ClampInt(int value, int min, int max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        public static UiMetrics BuildChromeMetrics(int screenWidth, int screenHeight)
        {
            if (BuildSurfaceProfile(screenWidth, screenHeight).UsesCompactScaling)
                return BuildMetrics(screenWidth, screenHeight);
            return BuildMetrics(screenWidth, 512);
        }

        public static int InfoPanelPinnedTabWidth(int screenWidth, UiMetrics metrics)
        {
            return PanelButtonWidth(screenWidth, metrics, 62f);
        }

        public static int InfoPanelBlockTabWidth(int screenWidth, UiMetrics metrics)
        {
            return Math.Max(metrics.InfoHeaderHeight, InfoPanelPinnedTabWidth(screenWidth, metrics) / 2);
        }

        public static int PanelButtonWidth(int screenWidth, UiMetrics metrics, float desiredWidth)
        {
            int unit = screenWidth / 16;
            if (unit < metrics.TopRowHeight)
                unit = metrics.TopRowHeight;
            int desired = metrics.SI(desiredWidth);
            int units = (desired + unit - 1) / unit;
            if (units < 1)
                units = 1;
            return units * unit;
        }

        public static ScreenZones BuildZones(int width, int height)
        {
            var metrics = BuildChromeMetrics(width, height);
            var top = new ScreenZone(ScreenZoneType.TopRow, 0, 0, width, metrics.TopRowHeight);
            var bottom = new ScreenZone(ScreenZoneType.BottomStrip, 0, height - metrics.BottomStripHeight, width, metrics.BottomStripHeight);
            var left = new ScreenZone(ScreenZoneType.LeftRail, 0, metrics.TopRowHeight, RailWidth, height - metrics.TopRowHeight - metrics.BottomStripHeight);
            var right = new ScreenZone(ScreenZoneType.RightRail, width - RailWidth, metrics.TopRowHeight, RailWidth, height - metrics.TopRowHeight - metrics.BottomStripHeight);
            var center = new ScreenZone(ScreenZoneType.CenterViewport, left.Width + CenterMargin, metrics.TopRowHeight + CenterMargin, width - left.Width - right.Width - CenterMargin * 2, height - metrics.TopRowHeight - metrics.BottomStripHeight - CenterMargin * 2);
            return new ScreenZones(top, left, right, bottom, center);
        }

        public static ScreenZone BuildInfoPanelZone(ScreenZone center, bool fullPanel)
        {
            if (fullPanel)
            {
                var metrics = BuildMetrics(center.Width, center.Height);
                int panelHeight = (int)Math.Round(center.Height * 0.30f);
                if (panelHeight < metrics.InfoPanelMinHeight)
                    panelHeight = Math.Min(center.Height, metrics.InfoPanelMinHeight);
                if (panelHeight > center.Height - metrics.InfoPanelReservedMapHeight)
                    panelHeight = Math.Max(1, center.Height - metrics.InfoPanelReservedMapHeight);

                return new ScreenZone(
                    ScreenZoneType.CenterViewport,
                    center.X,
                    center.Y + center.Height - panelHeight,
                    center.Width,
                    panelHeight);
            }

            var headerMetrics = BuildChromeMetrics(center.Width, center.Height);
            int headerHeight = headerMetrics.InfoHeaderHeight;
            if (headerHeight > center.Height)
                headerHeight = center.Height;

            return new ScreenZone(
                ScreenZoneType.CenterViewport,
                center.X,
                center.Y + center.Height - headerHeight,
                center.Width,
                headerHeight);
        }

        public static ScreenZone BuildTransformedCenterZone(ScreenZone center, int zoomLevel, int panX, int panY)
        {
            if (zoomLevel < 0)
                zoomLevel = 0;
            if (zoomLevel > 8)
                zoomLevel = 8;

            float zoom = 1f + zoomLevel * 0.25f;
            int width = (int)(center.Width * zoom);
            int height = (int)(center.Height * zoom);
            if (width < center.Width)
                width = center.Width;
            if (height < center.Height)
                height = center.Height;

            int x = center.X - (width - center.Width) / 2 + panX;
            int y = center.Y - (height - center.Height) / 2 + panY;
            return new ScreenZone(center.Type, x, y, width, height);
        }

        public static HitRegion[] BuildViewButtonRegions(int screenWidth)
        {
            const int buttonWidth = 48;
            const int buttonHeight = 18;
            const int gap = 4;
            int y = 3;
            int rightMargin = 8;
            int totalWidth = buttonWidth * 3 + gap * 2;
            int x = screenWidth - rightMargin - totalWidth;
            if (x < 150)
                x = 150;

            return new[]
            {
                new HitRegion(x, y, buttonWidth, buttonHeight, ViewTopId, "Top projection"),
                new HitRegion(x + buttonWidth + gap, y, buttonWidth, buttonHeight, ViewLeftId, "Left projection"),
                new HitRegion(x + (buttonWidth + gap) * 2, y, buttonWidth, buttonHeight, ViewFrontId, "Front projection")
            };
        }

        public static HitRegion[] BuildTopButtonRegions(int screenWidth)
        {
            return BuildTopMenuRegions(screenWidth, 512);
        }

        public static HitRegion[] BuildTopMenuRegions(int screenWidth)
        {
            return BuildTopMenuRegions(screenWidth, 512);
        }

        public static HitRegion[] BuildTopMenuRegions(int screenWidth, int screenHeight)
        {
            var metrics = BuildChromeMetrics(screenWidth, screenHeight);
            var profile = BuildSurfaceProfile(screenWidth, screenHeight);
            if (profile.IsCanonical512Square)
                return BuildCanonicalTopMenuRegions(screenWidth, screenHeight, metrics, profile);

            int settingsReserve = PanelButtonWidth(screenWidth, metrics, profile.UsesCompactScaling ? 56f : 88f) + metrics.TopRowHeight;
            int availableWidth = screenWidth - settingsReserve;
            if (availableWidth < metrics.TopRowHeight * 5)
                availableWidth = metrics.TopRowHeight * 5;

            int[] widths = BuildFlexibleChromeWidths(
                availableWidth,
                new int[]
                {
                    metrics.SI(profile.UsesCompactScaling ? 32f : 44f),
                    metrics.SI(profile.UsesCompactScaling ? 42f : 56f),
                    metrics.SI(profile.UsesCompactScaling ? 48f : 66f),
                    metrics.SI(profile.UsesCompactScaling ? 54f : 84f),
                    metrics.SI(profile.UsesCompactScaling ? 48f : 66f)
                },
                new int[]
                {
                    metrics.TopRowHeight,
                    metrics.TopRowHeight,
                    metrics.TopRowHeight,
                    metrics.TopRowHeight,
                    metrics.TopRowHeight
                },
                new float[] { 0.55f, 1.0f, 1.2f, 1.45f, 1.1f });

            int buttonHeight = metrics.TopRowHeight;
            int y = 0;
            int x = 0;

            return new[]
            {
                new HitRegion(x, y, widths[0], buttonHeight, ViewTopId, "Top projection"),
                new HitRegion(x + widths[0], y, widths[1], buttonHeight, ViewLeftId, "Side projection"),
                new HitRegion(x + widths[0] + widths[1], y, widths[2], buttonHeight, ViewFrontId, "Front projection"),
                new HitRegion(x + widths[0] + widths[1] + widths[2], y, widths[3], buttonHeight, SegmentModeId, "Open multiview layout"),
                new HitRegion(x + widths[0] + widths[1] + widths[2] + widths[3], y, widths[4], buttonHeight, MenuToolsId, "Open tools")
            };
        }

        static HitRegion[] BuildCanonicalTopMenuRegions(int screenWidth, int screenHeight, UiMetrics metrics, UiSurfaceProfile profile)
        {
            int viewButtonWidth = PanelButtonWidth(screenWidth, metrics, profile.UsesCompactScaling ? 36f : 52f);
            int sideButtonWidth = PanelButtonWidth(screenWidth, metrics, profile.UsesCompactScaling ? 38f : 54f);
            int frontButtonWidth = PanelButtonWidth(screenWidth, metrics, profile.UsesCompactScaling ? 42f : 62f);
            int multiviewWidth = PanelButtonWidth(screenWidth, metrics, profile.UsesCompactScaling ? 48f : 92f);
            int toolsWidth = PanelButtonWidth(screenWidth, metrics, profile.UsesCompactScaling ? 42f : 62f);
            int settingsReserve = PanelButtonWidth(screenWidth, metrics, profile.UsesCompactScaling ? 56f : 88f) + metrics.TopRowHeight;
            int availableTextWidth = screenWidth - settingsReserve - toolsWidth;
            int requestedTextWidth = viewButtonWidth + sideButtonWidth + frontButtonWidth + multiviewWidth;
            if (availableTextWidth > metrics.TopRowHeight * 4 && requestedTextWidth > availableTextWidth)
            {
                int compactWidth = availableTextWidth / 4;
                if (compactWidth < metrics.TopRowHeight)
                    compactWidth = metrics.TopRowHeight;
                viewButtonWidth = compactWidth;
                sideButtonWidth = compactWidth;
                frontButtonWidth = compactWidth;
                multiviewWidth = availableTextWidth - compactWidth * 3;
                if (multiviewWidth < metrics.TopRowHeight)
                    multiviewWidth = metrics.TopRowHeight;
            }
            int buttonHeight = metrics.TopRowHeight;
            int y = 0;
            int x = 0;
            int utilityX = x + viewButtonWidth + sideButtonWidth + frontButtonWidth + multiviewWidth;

            return new[]
            {
                new HitRegion(x, y, viewButtonWidth, buttonHeight, ViewTopId, "Top projection"),
                new HitRegion(x + viewButtonWidth, y, sideButtonWidth, buttonHeight, ViewLeftId, "Side projection"),
                new HitRegion(x + viewButtonWidth + sideButtonWidth, y, frontButtonWidth, buttonHeight, ViewFrontId, "Front projection"),
                new HitRegion(x + viewButtonWidth + sideButtonWidth + frontButtonWidth, y, multiviewWidth, buttonHeight, SegmentModeId, "Open multiview layout"),
                new HitRegion(utilityX, y, toolsWidth, buttonHeight, MenuToolsId, "Open tools")
            };
        }

        static int[] BuildFlexibleChromeWidths(int availableWidth, int[] desired, int[] minimum, float[] growWeights)
        {
            int count = desired.Length;
            var widths = new int[count];
            int desiredTotal = 0;
            int minimumTotal = 0;
            for (int i = 0; i < count; i++)
            {
                widths[i] = desired[i];
                desiredTotal += desired[i];
                minimumTotal += minimum[i];
            }

            if (availableWidth >= desiredTotal)
            {
                int extra = availableWidth - desiredTotal;
                float weightTotal = 0f;
                for (int i = 0; i < count; i++)
                    weightTotal += growWeights[i];

                int assigned = 0;
                for (int i = 0; i < count; i++)
                {
                    int add = i == count - 1 ? extra - assigned : (int)(extra * growWeights[i] / weightTotal + 0.5f);
                    widths[i] += add;
                    assigned += add;
                }

                return widths;
            }

            if (availableWidth <= minimumTotal)
            {
                int baseWidth = availableWidth / count;
                int remainder = availableWidth - baseWidth * count;
                for (int i = 0; i < count; i++)
                    widths[i] = Math.Max(1, baseWidth + (i == count - 1 ? remainder : 0));
                return widths;
            }

            int shrinkableTotal = desiredTotal - minimumTotal;
            int targetShrink = desiredTotal - availableWidth;
            int shrunk = 0;
            for (int i = 0; i < count; i++)
            {
                int shrinkable = desired[i] - minimum[i];
                int shrink = i == count - 1 ? targetShrink - shrunk : (int)(targetShrink * (shrinkable / (float)shrinkableTotal) + 0.5f);
                if (shrink > shrinkable)
                    shrink = shrinkable;
                widths[i] = desired[i] - shrink;
                shrunk += shrink;
            }

            return widths;
        }

        public static HitRegion[] BuildToolMenuRegions(int screenWidth, int screenHeight)
        {
            var metrics = BuildChromeMetrics(screenWidth, screenHeight);
            var topRegions = BuildTopMenuRegions(screenWidth, screenHeight);
            HitRegion tools = topRegions[topRegions.Length - 1];
            int size = metrics.TopRowHeight;
            int count = 7;
            int totalWidth = size * count;
            int x = tools.X + tools.Width / 2 - totalWidth / 2;
            if (x < 0)
                x = 0;
            if (x + totalWidth > screenWidth)
                x = Math.Max(0, screenWidth - totalWidth);
            int y = metrics.TopRowHeight + 1;

            return new[]
            {
                new HitRegion(x, y, size, size, ToggleConveyorId, "Toggle conveyor network"),
                new HitRegion(x + size, y, size, size, ToggleFillBarsId, "Cycle schematic fill bars"),
                new HitRegion(x + size * 2, y, size, size, ToggleBorderId, "Toggle hull outline"),
                new HitRegion(x + size * 3, y, size, size, ToggleHullScanId, "Toggle hull scan layer"),
                new HitRegion(x + size * 4, y, size, size, CycleScanModeId, "Cycle scan interpretation"),
                new HitRegion(x + size * 5, y, size, size, CycleScanColorScaleId, "Cycle scan color scale"),
                new HitRegion(x + size * 6, y, size, size, ToggleGridId, "Cycle block grid")
            };
        }

        public static HitRegion[] BuildTopRightModeRegions(int screenWidth)
        {
            return BuildTopRightModeRegions(screenWidth, 512);
        }

        public static HitRegion[] BuildTopRightModeRegions(int screenWidth, int screenHeight)
        {
            var metrics = BuildChromeMetrics(screenWidth, screenHeight);
            var profile = BuildSurfaceProfile(screenWidth, screenHeight);
            int settingsWidth = PanelButtonWidth(screenWidth, metrics, profile.UsesCompactScaling ? 56f : 88f);
            int chromeWidth = metrics.TopRowHeight;
            int buttonHeight = metrics.TopRowHeight;
            int y = 0;
            int x = screenWidth - settingsWidth - chromeWidth;
            if (x < 0)
                x = 0;

            return new[]
            {
                new HitRegion(x, y, settingsWidth, buttonHeight, MenuSettingsId, "Open display settings"),
                new HitRegion(x + settingsWidth, y, chromeWidth, chromeWidth, ToggleChromeId, "Hide menu chrome")
            };
        }

        public static HitRegion BuildChromeRestoreRegion(int screenWidth, int screenHeight)
        {
            var metrics = BuildChromeMetrics(screenWidth, screenHeight);
            int size = metrics.TopRowHeight;
            return new HitRegion(screenWidth - size, 0, size, size, ToggleChromeId, "Restore menu chrome");
        }

        public static HitRegion[] BuildBottomInfoRegions(int screenWidth, int screenHeight)
        {
            if (!BuildSurfaceProfile(screenWidth, screenHeight).AllowInfoPanel)
                return new HitRegion[0];

            var metrics = BuildChromeMetrics(screenWidth, screenHeight);
            int infoWidth = PanelButtonWidth(screenWidth, metrics, 74f);
            int buttonHeight = metrics.BottomStripHeight;
            int y = screenHeight - metrics.BottomStripHeight;
            int x = screenWidth - infoWidth;
            if (x < 0)
                x = 0;

            return new[]
            {
                new HitRegion(x, y, infoWidth, buttonHeight, ToggleInfoPanelId, "Toggle systems info panel")
            };
        }

        public static HitRegion[] BuildInfoPanelScanRegions(int screenWidth, int screenHeight)
        {
            if (!BuildSurfaceProfile(screenWidth, screenHeight).AllowFullInfoPanel)
                return new HitRegion[0];

            var metrics = BuildMetrics(screenWidth, screenHeight);
            var zones = BuildZones(screenWidth, screenHeight);
            int panelHeight = (int)(zones.Center.Height * 0.30f + 0.5f);
            if (panelHeight < metrics.InfoPanelMinHeight)
                panelHeight = zones.Center.Height < metrics.InfoPanelMinHeight ? zones.Center.Height : metrics.InfoPanelMinHeight;
            if (panelHeight > zones.Center.Height - metrics.InfoPanelReservedMapHeight)
                panelHeight = zones.Center.Height - metrics.InfoPanelReservedMapHeight > 1 ? zones.Center.Height - metrics.InfoPanelReservedMapHeight : 1;

            int panelY = zones.Center.Y + zones.Center.Height - panelHeight;
            int detailWidth = (int)(zones.Center.Width * 0.30f + 0.5f);
            if (detailWidth < 116)
                detailWidth = 116;
            if (detailWidth > zones.Center.Width - 120)
                detailWidth = zones.Center.Width - 120 > 80 ? zones.Center.Width - 120 : 80;

            int x = zones.Center.X + zones.Center.Width - detailWidth + 8;
            int width = detailWidth - 16;
            if (width < 32)
                width = 32;

            int buttonHeight = metrics.SI(18f);
            int gap = metrics.SI(3f);
            int y = panelY + metrics.InfoHeaderHeight + metrics.SI(6f);
            return new[]
            {
                new HitRegion(x, y, width, buttonHeight, SetDensityId, "Density scan interpretation"),
                new HitRegion(x, y + (buttonHeight + gap), width, buttonHeight, SetThicknessId, "Depth scan interpretation"),
                new HitRegion(x, y + (buttonHeight + gap) * 2, width, buttonHeight, SetVoidsId, "Void scan interpretation"),
                new HitRegion(x, y + (buttonHeight + gap) * 3, width, buttonHeight, RunScanId, "Run hull scan")
            };
        }

        public static HitRegion[] BuildInfoPanelBlockTabRegions(int screenWidth, int screenHeight, int itemCount, int scrollIndex)
        {
            if (itemCount <= 0)
                return new HitRegion[0];

            var zones = BuildZones(screenWidth, screenHeight);
            var profile = BuildSurfaceProfile(screenWidth, screenHeight);
            var zone = BuildInfoPanelZone(zones.Center, profile.AllowFullInfoPanel);
            var metrics = BuildChromeMetrics(screenWidth, screenHeight);
            int headerHeight = metrics.InfoHeaderHeight;
            var regions = new List<HitRegion>();
            int y = zone.Y;
            int pinnedWidth = InfoPanelPinnedTabWidth(screenWidth, metrics);
            int x = zone.X;
            regions.Add(new HitRegion(x, y, pinnedWidth, headerHeight, InfoPanelAllTabId, "Show all blocks"));
            x += pinnedWidth;
            if (itemCount > 1)
            {
                regions.Add(new HitRegion(x, y, pinnedWidth, headerHeight, InfoPanelStackTabId, "Show selected stack"));
                x += pinnedWidth;
            }

            int stripX = x;
            int stripWidth = zone.X + zone.Width - stripX;
            if (stripWidth <= 0)
                return regions.ToArray();

            regions.Add(new HitRegion(stripX, y, stripWidth, headerHeight, InfoPanelBlockTabScrollId, "Scroll stack blocks"));

            int tabWidth = InfoPanelBlockTabWidth(screenWidth, metrics);
            int visibleCount = stripWidth / tabWidth;
            if (visibleCount < 1)
                visibleCount = 1;
            int maxScroll = Math.Max(0, itemCount - visibleCount);
            if (scrollIndex < 0)
                scrollIndex = 0;
            if (scrollIndex > maxScroll)
                scrollIndex = maxScroll;

            for (int i = scrollIndex; i < itemCount && regions.Count < visibleCount + (itemCount > 1 ? 3 : 2); i++)
            {
                int tabX = stripX + (i - scrollIndex) * tabWidth;
                if (tabX >= zone.X + zone.Width)
                    break;
                int width = tabWidth;
                if (tabX + width > zone.X + zone.Width)
                    width = zone.X + zone.Width - tabX;
                if (width <= 0)
                    break;
                regions.Add(new HitRegion(tabX, y, width, headerHeight, InfoPanelBlockTabPrefix + i, "Select block tab"));
            }

            return regions.ToArray();
        }

        public static HitRegion[] BuildMenuPanelRegions(int screenWidth, int screenHeight, MenuPanel activeMenu)
        {
            return BuildMenuPanelRegions(screenWidth, screenHeight, activeMenu, SettingsCategoryStyle | SettingsCategoryUserInterface | SettingsCategoryRendering | SettingsCategorySchematics | SettingsCategoryPanelData);
        }

        public static HitRegion[] BuildMenuPanelRegions(int screenWidth, int screenHeight, MenuPanel activeMenu, int settingsExpandedMask)
        {
            var metrics = BuildChromeMetrics(screenWidth, screenHeight);
            int buttonHeight = metrics.BottomStripHeight;

            if (activeMenu == MenuPanel.Settings)
            {
                var profile = BuildSurfaceProfile(screenWidth, screenHeight);
                int panelWidth;
                if (profile.IsCanonical512Square)
                {
                    panelWidth = (int)(screenWidth * 0.45f + 0.5f);
                    if (panelWidth < metrics.SI(174f))
                        panelWidth = metrics.SI(174f);
                }
                else if (profile.UsesCompactScaling || screenWidth < metrics.SI(360f))
                {
                    panelWidth = screenWidth;
                }
                else
                {
                    panelWidth = (int)(screenWidth * (profile.IsWide ? 0.48f : 0.62f) + 0.5f);
                    if (panelWidth < metrics.SI(210f))
                        panelWidth = metrics.SI(210f);
                    if (panelWidth > screenWidth)
                        panelWidth = screenWidth;
                }

                int y = metrics.TopRowHeight + 1;
                int x = screenWidth - panelWidth;
                if (x < 0)
                    x = 0;

                var regions = new List<HitRegion>();
                int row = 0;

                AddSettingsHeader(regions, x, y, panelWidth, buttonHeight, ref row, SettingsSchematicsHeaderId, "SCHEMATICS");
                if (IsSettingsCategoryExpanded(settingsExpandedMask, SettingsCategorySchematics))
                {
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, ToggleReferenceId, "Toggle reference axes");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, ToggleAllConnectionsId, "Toggle all conveyor connections");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, ToggleBlocksOccludeConveyorsId, "Occlude conveyors behind schematic blocks");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, ToggleConnectedNetworksId, "Show docked/static connected conveyor networks");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, CopySchematicSettingsId, "Copy schematic settings");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, PasteSchematicSettingsId, "Paste schematic settings");
                }

                AddSettingsHeader(regions, x, y, panelWidth, buttonHeight, ref row, SettingsRenderingHeaderId, "RENDERING");
                if (IsSettingsCategoryExpanded(settingsExpandedMask, SettingsCategoryRendering))
                {
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, ToggleBlurId, "Toggle scan smoothing");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, TogglePerformanceModeId, "Toggle performance mode");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, ToggleHighResScanningId, "Toggle high resolution scanning");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, CopyRenderingSettingsId, "Copy rendering settings");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, PasteRenderingSettingsId, "Paste rendering settings");
                }

                AddSettingsHeader(regions, x, y, panelWidth, buttonHeight, ref row, SettingsUiHeaderId, "CONTROLS");
                if (IsSettingsCategoryExpanded(settingsExpandedMask, SettingsCategoryUserInterface))
                {
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, ToggleMouseControlId, "Toggle mouse-controlled panel cursor");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, CycleMouseSensitivityId, "Cycle mouse cursor sensitivity");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, ToggleGridRotationId, "Allow grid rotation");
                }

                AddSettingsHeader(regions, x, y, panelWidth, buttonHeight, ref row, SettingsStyleHeaderId, "STYLE");
                if (IsSettingsCategoryExpanded(settingsExpandedMask, SettingsCategoryStyle))
                {
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, AdjustPaletteBrightnessId, "Adjust main UI brightness");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, AdjustPaletteHueId, "Adjust main UI color");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, AdjustPaletteSaturationId, "Adjust main UI saturation");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, AdjustAccentBrightnessId, "Adjust highlight UI brightness");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, AdjustAccentHueId, "Adjust highlight UI color");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, AdjustAccentSaturationId, "Adjust highlight UI saturation");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, AdjustSchematicMainHueId, "Adjust schematic main color");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, AdjustSchematicSecondaryHueId, "Adjust schematic secondary color");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, AdjustConveyorHueId, "Adjust conveyor color");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, CycleUiFontId, "Cycle UI font");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, ResetStyleSettingsId, "Reset style settings");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, CopyUiSettingsId, "Copy interface settings");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, PasteUiSettingsId, "Paste interface settings");
                }

                AddSettingsHeader(regions, x, y, panelWidth, buttonHeight, ref row, SettingsPanelDataHeaderId, "PANEL DATA");
                if (IsSettingsCategoryExpanded(settingsExpandedMask, SettingsCategoryPanelData))
                {
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, SaveSettingsId, "Save settings to panel");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, CopySettingsId, "Copy all settings to session clipboard");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, PasteSettingsId, "Paste all settings from session clipboard");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, ExportSettingsId, "Export settings to custom data");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, RecalibrateCursorId, "Start cursor calibration");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, ResetUiSettingsId, "Reset UI settings");
                }

                AddSettingsHeader(regions, x, y, panelWidth, buttonHeight, ref row, SettingsDebugHeaderId, "DEBUG");
                if (IsSettingsCategoryExpanded(settingsExpandedMask, SettingsCategoryDebug))
                {
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, ToggleDebugModeId, "Toggle panel debug overlay");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, TogglePerfStatsId, "Toggle panel performance stats");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, ToggleBlocksId, "Show all blocks");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, WipePanelCacheId, "Rebuild panel render caches");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, ResetPanelSettingsId, "Reset panel settings");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, CopyDebugSettingsId, "Copy debug settings");
                    AddSettingsRow(regions, x, y, panelWidth, buttonHeight, ref row, PasteDebugSettingsId, "Paste debug settings");
                }

                return regions.ToArray();
            }

            if (activeMenu == MenuPanel.Tools)
                return BuildToolMenuRegions(screenWidth, screenHeight);

            if (activeMenu == MenuPanel.Scan)
            {
                int panelWidth = metrics.SI(104f);
                int x = screenWidth - panelWidth - metrics.SI(88f);
                if (x < 0)
                    x = 0;
                int y = metrics.TopRowHeight + 1;
                return new[]
                {
                    new HitRegion(x, y, panelWidth, buttonHeight, SetDensityId, "Density scan interpretation"),
                    new HitRegion(x, y + buttonHeight, panelWidth, buttonHeight, SetThicknessId, "Depth scan interpretation"),
                    new HitRegion(x, y + buttonHeight * 2, panelWidth, buttonHeight, SetVoidsId, "Void scan interpretation"),
                    new HitRegion(x, y + buttonHeight * 3, panelWidth, buttonHeight, RunScanId, "Run cached raycast scan")
                };
            }

            return new HitRegion[0];
        }

        public static HitRegion[] BuildMenuPanelRegions(int screenWidth, MenuPanel activeMenu)
        {
            return BuildMenuPanelRegions(screenWidth, 512, activeMenu);
        }

        public static HitRegion[] BuildMenuPanelRegions(int screenWidth, MenuPanel activeMenu, int settingsExpandedMask)
        {
            return BuildMenuPanelRegions(screenWidth, 512, activeMenu, settingsExpandedMask);
        }

        static void AddSettingsHeader(List<HitRegion> regions, int x, int y, int width, int height, ref int row, string id, string label)
        {
            regions.Add(new HitRegion(x, y + height * row, width, height, id, label));
            row++;
        }

        static void AddSettingsRow(List<HitRegion> regions, int x, int y, int width, int height, ref int row, string id, string label)
        {
            regions.Add(new HitRegion(x, y + height * row, width, height, id, label));
            row++;
        }

        public static bool IsSettingsCategoryExpanded(int mask, int category)
        {
            return (mask & category) != 0;
        }

        public static void GetSettingsHueSliderGeometry(HitRegion region, out float lineX, out float lineWidth)
        {
            float padding = Math.Max(5f, region.Height * 0.36f);
            float labelWidth = Math.Max(region.Width * 0.48f, region.Height * 7.8f);
            lineX = region.X + labelWidth;
            lineWidth = region.Width - labelWidth - padding;
            float minWidth = Math.Max(16f, region.Height * 1.6f);
            if (lineWidth < minWidth)
            {
                lineWidth = minWidth;
                lineX = region.X + region.Width - padding - lineWidth;
            }
        }

        public static HitRegion[] BuildBottomSchematicRegions(int screenWidth, int screenHeight)
        {
            var metrics = BuildChromeMetrics(screenWidth, screenHeight);
            var profile = BuildSurfaceProfile(screenWidth, screenHeight);
            if (!profile.IsCanonical512Square)
            {
                int compactButtonHeight = metrics.BottomStripHeight;
                int compactY = screenHeight - metrics.BottomStripHeight;
                int reservedInfoWidth = profile.AllowInfoPanel ? PanelButtonWidth(screenWidth, metrics, 74f) : 0;
                int availableWidth = screenWidth - reservedInfoWidth;
                if (availableWidth < 4)
                    availableWidth = screenWidth;
                int baseWidth = availableWidth / 4;
                int remainder = availableWidth - baseWidth * 4;
                int compactX = 0;

                var cargo = new HitRegion(compactX, compactY, baseWidth, compactButtonHeight, SchematicCargoId, "Cargo schematic layer");
                compactX += cargo.Width;
                var engines = new HitRegion(compactX, compactY, baseWidth, compactButtonHeight, SchematicEnginesId, "Engines schematic layer");
                compactX += engines.Width;
                var power = new HitRegion(compactX, compactY, baseWidth, compactButtonHeight, SchematicPowerId, "Power schematic layer");
                compactX += power.Width;
                var oxygen = new HitRegion(compactX, compactY, baseWidth + remainder, compactButtonHeight, SchematicOxygenId, "Oxygen schematic layer");

                return new[] { cargo, engines, power, oxygen };
            }

            int buttonHeight = metrics.BottomStripHeight;
            int cargoWidth = PanelButtonWidth(screenWidth, metrics, 62f);
            int enginesWidth = PanelButtonWidth(screenWidth, metrics, 72f);
            int powerWidth = PanelButtonWidth(screenWidth, metrics, 62f);
            int oxygenWidth = PanelButtonWidth(screenWidth, metrics, 72f);
            const int gap = 0;
            int x = 0;

            int y = screenHeight - metrics.BottomStripHeight;
            int enginesX = x + cargoWidth + gap;
            int powerX = enginesX + enginesWidth + gap;
            int oxygenX = powerX + powerWidth + gap;

            return new[]
            {
                new HitRegion(x, y, cargoWidth, buttonHeight, SchematicCargoId, "Cargo schematic layer"),
                new HitRegion(enginesX, y, enginesWidth, buttonHeight, SchematicEnginesId, "Engines schematic layer"),
                new HitRegion(powerX, y, powerWidth, buttonHeight, SchematicPowerId, "Power schematic layer"),
                new HitRegion(oxygenX, y, oxygenWidth, buttonHeight, SchematicOxygenId, "Oxygen schematic layer")
            };
        }

        public static HitRegion[] BuildSegmentScanControlRegions(ScreenZone zone)
        {
            var metrics = BuildMetrics(zone.Width, zone.Height);
            int buttonHeight = metrics.SI(18f);
            int gap = metrics.SI(3f);
            int x = zone.X + metrics.SI(8f);
            int y = zone.Y + metrics.SI(28f);
            int width = zone.Width - metrics.SI(16f);
            if (width < metrics.SI(40f))
                width = metrics.SI(40f);

            return new[]
            {
                new HitRegion(x, y, width, buttonHeight, SetDensityId, "Density scan interpretation"),
                new HitRegion(x, y + (buttonHeight + gap), width, buttonHeight, SetThicknessId, "Depth scan interpretation"),
                new HitRegion(x, y + (buttonHeight + gap) * 2, width, buttonHeight, SetVoidsId, "Void scan interpretation"),
                new HitRegion(x, y + (buttonHeight + gap) * 3, width, buttonHeight, ToggleBlurId, "Toggle scan smoothing"),
                new HitRegion(x, y + (buttonHeight + gap) * 4, width, buttonHeight, RunScanId, "Run hull scan")
            };
        }

        public static HitRegion BuildScanProgressCancelRegion(ScreenZone zone)
        {
            var metrics = BuildMetrics(zone.Width, zone.Height);
            int width = PanelButtonWidth(zone.Width, metrics, 72f);
            if (width > zone.Width - metrics.SI(20f))
                width = zone.Width - metrics.SI(20f);
            if (width < metrics.SI(42f))
                width = metrics.SI(42f);
            int height = metrics.TopRowHeight;
            int x = zone.X + (zone.Width - width) / 2;
            int y = zone.Y + zone.Height / 2 + metrics.SI(35f);
            return new HitRegion(x, y, width, height, CancelScanId, "Cancel scan");
        }

        public static HitRegion BuildCalibrationStartRegion(ScreenZone zone)
        {
            var metrics = BuildMetrics(zone.Width, zone.Height);
            int width = PanelButtonWidth(zone.Width, metrics, 92f);
            int height = metrics.TopRowHeight;
            int x = zone.X + zone.Width / 2 - width - metrics.SI(3f);
            int y = zone.Y + zone.Height / 2 + metrics.SI(36f);
            return new HitRegion(x, y, width, height, CalibrationStartId, "Start calibration");
        }

        public static HitRegion BuildCalibrationCloseRegion(ScreenZone zone)
        {
            var metrics = BuildMetrics(zone.Width, zone.Height);
            int width = PanelButtonWidth(zone.Width, metrics, 72f);
            int height = metrics.TopRowHeight;
            int x = zone.X + zone.Width / 2 + metrics.SI(3f);
            int y = zone.Y + zone.Height / 2 + metrics.SI(36f);
            return new HitRegion(x, y, width, height, CalibrationCloseId, "Close calibration prompt");
        }

        public static HitRegion BuildCalibrationPointRegion(ScreenZone zone, int step)
        {
            var point = GetCalibrationPoint(zone, step);
            int size = BuildMetrics(zone.Width, zone.Height).SI(34f);
            return new HitRegion((int)(point.X - size * 0.5f), (int)(point.Y - size * 0.5f), size, size, CalibrationPointId, "Calibration point");
        }

        public static ScreenZone BuildCalibrationTargetZone(int screenWidth, int screenHeight)
        {
            return BuildCalibrationTargetZone(new ScreenZone(ScreenZoneType.CenterViewport, 0, 0, screenWidth, screenHeight));
        }

        public static ScreenZone BuildCalibrationTargetZone(ScreenZone outerZone)
        {
            int screenWidth = outerZone.Width;
            int screenHeight = outerZone.Height;
            var profile = BuildSurfaceProfile(screenWidth, screenHeight);
            if (profile.IsStandard)
                return outerZone;

            return BuildCalibrationPromptZone(outerZone, true);
        }

        public static ScreenZone BuildCalibrationPromptZone(ScreenZone outerZone, bool active)
        {
            var context = BuildLayoutContext(outerZone);
            var surface = context.Surface;
            var safe = context.SafeContent;
            int screenWidth = safe.Width;
            int screenHeight = safe.Height;
            var metrics = context.Metrics;
            var profile = context.Profile;
            float margin = Math.Max(6f, metrics.S(profile.UsesCompactScaling ? 14f : 20f));
            float maxPanelWidth = Math.Max(1f, screenWidth - margin * 2f);
            float maxPanelHeight = Math.Max(1f, screenHeight - margin * 2f);
            float panelWidth;
            float panelHeight;

            if (!profile.IsStandard)
            {
                bool veryWideCompact = screenWidth > screenHeight * 1.55f;
                panelWidth = Math.Max(210f, screenWidth * 0.70f);
                if (panelWidth > maxPanelWidth)
                    panelWidth = maxPanelWidth;
                panelHeight = Math.Max(active ? 104f : 88f, screenHeight * (active ? (veryWideCompact ? 0.36f : 0.43f) : (veryWideCompact ? 0.30f : 0.36f)));
                if (panelHeight > maxPanelHeight)
                    panelHeight = maxPanelHeight;
            }
            else
            {
                float desiredWidth = screenWidth * 0.72f;
                panelWidth = Math.Min(370f, Math.Max(metrics.S(210f), desiredWidth));
                if (panelWidth > maxPanelWidth)
                    panelWidth = maxPanelWidth;
                panelHeight = active ? 140f : 112f;
                if (panelHeight > maxPanelHeight)
                    panelHeight = maxPanelHeight;
            }

            float centerX = safe.X + screenWidth * 0.5f;
            float centerY = safe.Y + screenHeight * 0.5f;
            if (!profile.IsStandard && screenWidth > screenHeight * 1.55f)
            {
                centerY += Math.Min(screenHeight * 0.07f, 18f);
                float minCenterY = surface.Y + margin + panelHeight * 0.5f;
                float maxCenterY = surface.Y + surface.Height - margin - panelHeight * 0.5f;
                if (centerY < minCenterY)
                    centerY = minCenterY;
                if (centerY > maxCenterY)
                    centerY = maxCenterY;
            }

            return new ScreenZone(
                ScreenZoneType.CenterViewport,
                (int)(centerX - panelWidth * 0.5f + 0.5f),
                (int)(centerY - panelHeight * 0.5f + 0.5f),
                Math.Max(1, (int)(panelWidth + 0.5f)),
                Math.Max(1, (int)(panelHeight + 0.5f)));
        }

        public static Vector2 GetCalibrationPoint(ScreenZone zone, int step)
        {
            var metrics = BuildMetrics(zone.Width, zone.Height);
            float inset = metrics.S(34f);
            float maxInsetX = Math.Max(0f, zone.Width * 0.24f);
            float maxInsetY = Math.Max(0f, zone.Height * 0.24f);
            if (inset > maxInsetX)
                inset = maxInsetX;
            if (inset > maxInsetY)
                inset = maxInsetY;

            float left = zone.X + inset;
            float right = zone.X + zone.Width - inset;
            float top = zone.Y + inset;
            float bottom = zone.Y + zone.Height - inset;
            if (step <= 0)
                return new Vector2(left, top);
            if (step == 1)
                return new Vector2(left, bottom);
            return new Vector2(right, bottom);
        }
    }

    public struct ScreenZones
    {
        public ScreenZone Top;
        public ScreenZone Left;
        public ScreenZone Right;
        public ScreenZone Bottom;
        public ScreenZone Center;

        public ScreenZones(ScreenZone top, ScreenZone left, ScreenZone right, ScreenZone bottom, ScreenZone center)
        {
            Top = top;
            Left = left;
            Right = right;
            Bottom = bottom;
            Center = center;
        }
    }
}
