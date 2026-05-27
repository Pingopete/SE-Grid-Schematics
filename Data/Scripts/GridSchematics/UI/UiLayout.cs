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
        public const string InfoPanelGroup2TabId = "info:tab:group2";
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
        public const string CargoInfoFocusAllId = "info:cargo:focus:all";
        public const string CargoInfoFocusReachableId = "info:cargo:focus:reachable";
        public const string CargoInfoFocusIsolatedId = "info:cargo:focus:isolated";
        public const string CargoInfoFocusFullId = "info:cargo:focus:full";
        public const string CargoInfoFocusOfflineId = "info:cargo:focus:offline";
        public const string CargoInfoFilterToggleId = "info:cargo:filter:toggle";
        public const string CargoInfoFilterPrefix = "info:cargo:filter:";
        public const string CargoInfoActionScrollId = "info:cargo:action:scroll";
        public const string CargoInfoBlockScrollId = "info:cargo:block:scroll";
        public const string CargoInfoBlockPrefix = "info:cargo:block:bar:";
        public const string CargoInfoMixScrollId = "info:cargo:mix:scroll";
        public const string CargoInfoMixRowPrefix = "info:cargo:mix:row:";
        public const string CargoInfoMixAddToQuotaId = "info:cargo:mix:addquota";
        public const string CargoInfoMixSortPrefix = "info:cargo:mix:sort:";
        public const string CargoInfoSendBlockToTransferId = "info:cargo:sendblock";
        public const string CargoInfoTransferModeId = "info:cargo:right:transfer";
        public const string CargoInfoActionsModeId = "info:cargo:right:actions";
        public const string CargoInfoTransferSourceSelectId = "info:cargo:transfer:source";
        public const string CargoInfoTransferSourceViewId = "info:cargo:transfer:source:view";
        public const string CargoInfoTransferDestSelectId = "info:cargo:transfer:dest";
        public const string CargoInfoTransferDestViewId = "info:cargo:transfer:dest:view";
        public const string CargoInfoTransferDirectionId = "info:cargo:transfer:direction";
        public const string CargoInfoTransferClearId = "info:cargo:transfer:clear";
        public const string CargoInfoTransferNowId = "info:cargo:transfer:now";
        public const string CargoInfoTransferQuotaPrefix = "info:cargo:transfer:quota:";
        public const string CargoInfoActionPrefix = "info:cargo:action:";

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
            int headerBarHeight = metrics.SI(16f);
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

        public static ScreenZone BuildCargoInfoPanelZone(ScreenZone center, bool fullPanel)
        {
            if (!fullPanel)
                return BuildInfoPanelZone(center, false);

            if (center.Width == 512 && center.Height == 480)
                return new ScreenZone(ScreenZoneType.CenterViewport, center.X, 312, center.Width, 184);

            var metrics = BuildMetrics(center.Width, center.Height);
            var headerMetrics = BuildChromeMetrics(center.Width, center.Height);
            int outerPad = metrics.SI(4f);
            int outerGap = metrics.SI(3f);
            int widgetWidth = Math.Max(16, (center.Width - outerPad * 2 - outerGap * 2) / 3);
            int desiredPanelHeight = headerMetrics.InfoHeaderHeight + outerPad * 2 + widgetWidth;
            int baselineHeight = (int)Math.Round(center.Height * 0.30f);
            if (baselineHeight < metrics.InfoPanelMinHeight)
                baselineHeight = Math.Min(center.Height, metrics.InfoPanelMinHeight);
            if (desiredPanelHeight < baselineHeight)
                desiredPanelHeight = baselineHeight;

            int maxPanelHeight = center.Height - metrics.InfoPanelReservedMapHeight;
            if (maxPanelHeight < 1)
                maxPanelHeight = 1;
            if (desiredPanelHeight > maxPanelHeight)
                desiredPanelHeight = maxPanelHeight;

            return new ScreenZone(
                ScreenZoneType.CenterViewport,
                center.X,
                center.Y + center.Height - desiredPanelHeight,
                center.Width,
                desiredPanelHeight);
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
            return new HitRegion[0];
        }

        public static HitRegion BuildInfoDrawerToggleRegion(int screenWidth, int screenHeight, bool showInfoPanel)
        {
            if (!BuildSurfaceProfile(screenWidth, screenHeight).AllowInfoPanel)
                return new HitRegion(0, 0, 0, 0, string.Empty, string.Empty);

            if (BuildSurfaceProfile(screenWidth, screenHeight).IsCanonical512Square)
                return new HitRegion(462, showInfoPanel ? 298 : 482, 50, 14, ToggleInfoPanelId, showInfoPanel ? "Hide systems info panel" : "Show systems info panel");

            var zones = BuildZones(screenWidth, screenHeight);
            var metrics = BuildChromeMetrics(screenWidth, screenHeight);
            int width = PanelButtonWidth(screenWidth, metrics, 50f);
            int height = Math.Max(12, metrics.InfoHeaderHeight - 2);
            int y = showInfoPanel ? Math.Max(0, BuildInfoPanelZone(zones.Center, true).Y - height / 2) : zones.Bottom.Y - height;
            return new HitRegion(zones.Center.X, y, width, height, ToggleInfoPanelId, showInfoPanel ? "Hide systems info panel" : "Show systems info panel");
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
            var items = new List<BlockStackItem>();
            for (int i = 0; i < itemCount; i++)
                items.Add(new BlockStackItem { Name = "BLOCK" });
            return BuildInfoPanelBlockTabRegions(screenWidth, screenHeight, items, scrollIndex, false);
        }

        public static HitRegion[] BuildInfoPanelBlockTabRegions(int screenWidth, int screenHeight, List<BlockStackItem> items, int scrollIndex, bool cargoPanel, bool manualSelectionAvailable = false, int manualGroupCount = 0)
        {
            int itemCount = items != null ? items.Count : 0;
            if (manualSelectionAvailable && manualGroupCount <= 0)
                manualGroupCount = 1;
            if (manualGroupCount > 2)
                manualGroupCount = 2;

            var zones = BuildZones(screenWidth, screenHeight);
            var profile = BuildSurfaceProfile(screenWidth, screenHeight);
            var zone = cargoPanel ? BuildCargoInfoPanelZone(zones.Center, profile.AllowFullInfoPanel) : BuildInfoPanelZone(zones.Center, profile.AllowFullInfoPanel);
            var metrics = BuildChromeMetrics(screenWidth, screenHeight);
            int headerHeight = metrics.InfoHeaderHeight;
            var regions = new List<HitRegion>();
            int y = zone.Y;

            if (cargoPanel && profile.IsCanonical512Square)
            {
                const int slot = 64;
                regions.Add(new HitRegion(0, y, slot, headerHeight, InfoPanelAllTabId, "Show all blocks"));
                int x = slot;
                if (manualGroupCount > 0)
                {
                    regions.Add(new HitRegion(x, y, slot, headerHeight, InfoPanelStackTabId, "Show group"));
                    x += slot;
                    if (manualGroupCount > 1)
                    {
                        regions.Add(new HitRegion(x, y, slot, headerHeight, InfoPanelGroup2TabId, "Show group 2"));
                        x += slot;
                    }
                }
                else if (itemCount > 1)
                {
                    regions.Add(new HitRegion(x, y, slot, headerHeight, InfoPanelStackTabId, "Show selected stack"));
                    x += slot;
                }

                int stripWidth = Math.Max(0, zone.X + zone.Width - x);
                if (stripWidth > 0)
                    regions.Add(new HitRegion(x, y, stripWidth, headerHeight, InfoPanelBlockTabScrollId, "Scroll stack blocks"));

                int visibleSlots = Math.Max(0, stripWidth / slot);
                int maxScroll = Math.Max(0, itemCount - visibleSlots);
                if (scrollIndex < 0)
                    scrollIndex = 0;
                if (scrollIndex > maxScroll)
                    scrollIndex = maxScroll;
                for (int slotIndex = 0; slotIndex < visibleSlots; slotIndex++)
                {
                    int itemIndex = scrollIndex + slotIndex;
                    if (itemIndex >= itemCount)
                        break;
                    regions.Add(new HitRegion(x + slotIndex * slot, y, slot, headerHeight, InfoPanelBlockTabPrefix + itemIndex.ToString(), "Select block tab"));
                }
                return regions.ToArray();
            }

            int pinnedWidth = InfoPanelPinnedTabWidth(screenWidth, metrics);
            int cursorX = zone.X;
            regions.Add(new HitRegion(cursorX, y, pinnedWidth, headerHeight, InfoPanelAllTabId, "Show all blocks"));
            cursorX += pinnedWidth;
            if (manualGroupCount > 0)
            {
                regions.Add(new HitRegion(cursorX, y, pinnedWidth, headerHeight, InfoPanelStackTabId, "Show group"));
                cursorX += pinnedWidth;
                if (manualGroupCount > 1)
                {
                    regions.Add(new HitRegion(cursorX, y, pinnedWidth, headerHeight, InfoPanelGroup2TabId, "Show group 2"));
                    cursorX += pinnedWidth;
                }
            }
            else if (itemCount > 1)
            {
                regions.Add(new HitRegion(cursorX, y, pinnedWidth, headerHeight, InfoPanelStackTabId, "Show selected stack"));
                cursorX += pinnedWidth;
            }

            int stripX = cursorX;
            int stripWidthDefault = zone.X + zone.Width - stripX;
            if (stripWidthDefault <= 0)
                return regions.ToArray();

            regions.Add(new HitRegion(stripX, y, stripWidthDefault, headerHeight, InfoPanelBlockTabScrollId, "Scroll stack blocks"));

            int maxDefaultScroll = Math.Max(0, itemCount - 1);
            if (scrollIndex < 0)
                scrollIndex = 0;
            if (scrollIndex > maxDefaultScroll)
                scrollIndex = maxDefaultScroll;

            int cursor = stripX;
            for (int i = scrollIndex; i < itemCount; i++)
            {
                string label = items != null && items[i] != null ? items[i].Name : "BLOCK";
                int width = cargoPanel ? pinnedWidth : InfoPanelBlockTabWidthForLabel(screenWidth, metrics, label);
                if (cursor >= zone.X + zone.Width)
                    break;
                if (cursor + width > zone.X + zone.Width)
                    width = zone.X + zone.Width - cursor;
                if (width <= 0)
                    break;
                regions.Add(new HitRegion(cursor, y, width, headerHeight, InfoPanelBlockTabPrefix + i.ToString(), "Select block tab"));
                cursor += width;
            }

            return regions.ToArray();
        }
        public static int InfoPanelBlockTabWidthForLabel(int screenWidth, UiMetrics metrics, string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                label = "BLOCK";
            int minWidth = InfoPanelBlockTabWidth(screenWidth, metrics);
            int desired = (int)(label.Length * metrics.S(6.8f) + metrics.S(18f) + 0.5f);
            int maxWidth = PanelButtonWidth(screenWidth, metrics, 128f);
            if (desired < minWidth)
                desired = minWidth;
            if (desired > maxWidth)
                desired = maxWidth;
            return desired;
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
            int y = screenHeight - metrics.BottomStripHeight;
            return new[]
            {
                new HitRegion(0, y, 64, buttonHeight, SchematicCargoId, "Cargo schematic layer"),
                new HitRegion(64, y, 64, buttonHeight, SchematicEnginesId, "Engines schematic layer"),
                new HitRegion(128, y, 64, buttonHeight, SchematicPowerId, "Power schematic layer"),
                new HitRegion(192, y, 64, buttonHeight, SchematicOxygenId, "Atmosphere schematic layer")
            };
        }
        public static HitRegion[] BuildCargoInfoPanelRegions(int screenWidth, int screenHeight, bool fullPanel, bool filterDropdownOpen, string rightPanelMode)
        {
            var profile = BuildSurfaceProfile(screenWidth, screenHeight);
            if (!profile.AllowInfoPanel)
                return new HitRegion[0];

            if (profile.IsCanonical512Square && fullPanel)
                return BuildCanonicalCargoInfoPanelRegions(filterDropdownOpen, !string.Equals(rightPanelMode, "ACTIONS", StringComparison.OrdinalIgnoreCase));

            bool transferMode = !string.Equals(rightPanelMode, "ACTIONS", StringComparison.OrdinalIgnoreCase);
            var zones = BuildZones(screenWidth, screenHeight);
            var panel = BuildCargoInfoPanelZone(zones.Center, fullPanel && profile.AllowFullInfoPanel);
            var baseMetrics = BuildMetrics(panel.Width, panel.Height);
            var headerMetrics = BuildChromeMetrics(screenWidth, screenHeight);
            int headerHeight = headerMetrics.InfoHeaderHeight;
            int contentH = panel.Height - headerHeight - baseMetrics.SI(6f);
            if (contentH <= baseMetrics.SI(24f))
                return new HitRegion[0];

            int outerPad = baseMetrics.SI(4f);
            int outerGap = baseMetrics.SI(3f);
            int panelW = (panel.Width - outerPad * 2 - outerGap * 2) / 3;
            if (panelW < 16)
                panelW = 16;
            int panelH = Math.Max(1, contentH);
            int x0 = panel.X + outerPad;
            int x1 = x0 + panelW + outerGap;
            int x2 = x1 + panelW + outerGap;
            int y0 = panel.Y + headerHeight + outerPad;

            int pad = ClampInt((int)(panelW * 0.035f + 0.5f), 6, 12);
            int gap = ClampInt((int)(Math.Min(panelW, panelH) * 0.020f + 0.5f), 2, 6);
            int widgetHeader = ClampInt((int)(panelH * 0.095f + 0.5f), 17, 28);
            int rowH = ClampInt((int)(panelH * 0.074f + 0.5f), 13, 17);
            int barH = ClampInt((int)(panelH * 0.060f + 0.5f), 15, 24);
            int microH = ClampInt((int)(panelH * 0.038f + 0.5f), 9, 15);
            var regions = new List<HitRegion>();

            int loadPrimaryY = y0 + widgetHeader;
            int loadPrimaryH = rowH + barH + gap + microH;
            regions.Add(new HitRegion(x0, loadPrimaryY, panelW, loadPrimaryH, CargoInfoFocusAllId, "Highlight all cargo blocks"));
            int reachY = loadPrimaryY + loadPrimaryH;
            regions.Add(new HitRegion(x0, reachY, panelW / 2, rowH + microH, CargoInfoFocusReachableId, "Highlight reachable cargo blocks"));
            regions.Add(new HitRegion(x0 + panelW / 2, reachY, panelW - panelW / 2, rowH + microH, CargoInfoFocusIsolatedId, "Highlight isolated cargo blocks"));
            int satY = y0 + (int)(panelH * 0.58f);
            regions.Add(new HitRegion(x0, satY, panelW, Math.Max(rowH, y0 + panelH - satY), CargoInfoFocusFullId, "Highlight saturated cargo blocks"));

            string[] categories = new[] { "ORE", "INGOT", "COMPONENTS", "TOOLS", "CONSUMABLE" };
            int filterWidth = Math.Max(panelW / 3, headerHeight * 5);
            if (filterWidth > panelW - pad * 2)
                filterWidth = panelW - pad * 2;
            int filterX = x1 + panelW - pad - filterWidth;
            regions.Add(new HitRegion(filterX, y0, filterWidth, widgetHeader, CargoInfoFilterToggleId, "Choose cargo filter"));
            // Add precise controls again at the end because touch hit testing gives priority to later regions.
            regions.Add(new HitRegion(96, 328, 76, 18, CargoInfoSendBlockToTransferId, "Send indicated cargo block to transfer"));
            regions.Add(new HitRegion(248, 328, 84, 18, CargoInfoFilterToggleId, "Choose cargo filter"));
            regions.Add(new HitRegion(178, 368, 40, 16, CargoInfoMixSortPrefix + "ITEM", "Sort cargo mix by item"));
            regions.Add(new HitRegion(222, 368, 32, 16, CargoInfoMixSortPrefix + "QUANT", "Sort cargo mix by quantity"));
            regions.Add(new HitRegion(258, 368, 34, 16, CargoInfoMixSortPrefix + "VOLUME", "Sort cargo mix by volume"));
            regions.Add(new HitRegion(296, 368, 30, 16, CargoInfoMixSortPrefix + "MASS", "Sort cargo mix by mass"));
            regions.Add(new HitRegion(250, 478, 92, 20, CargoInfoMixAddToQuotaId, "Add selected cargo mix rows to transfer quota"));
            regions.Add(new HitRegion(360, 328, 78, 18, CargoInfoTransferModeId, "Show cargo transfer quota"));
            regions.Add(new HitRegion(438, 328, 70, 18, CargoInfoActionsModeId, "Show selected block actions"));
            if (transferMode)
            {
                regions.Add(new HitRegion(348, 344, 50, 18, CargoInfoTransferSourceSelectId, "Set transfer source selection"));
                regions.Add(new HitRegion(398, 344, 108, 18, CargoInfoTransferSourceViewId, "Show transfer source cargo mix"));
                regions.Add(new HitRegion(348, 374, 50, 18, CargoInfoTransferDestSelectId, "Set transfer destination selection"));
                regions.Add(new HitRegion(398, 374, 108, 18, CargoInfoTransferDestViewId, "Show transfer destination cargo mix"));
                regions.Add(new HitRegion(456, 356, 50, 18, CargoInfoTransferDirectionId, "Reverse transfer direction"));
                for (int i = 0; i < 4; i++)
                {
                    regions.Add(new HitRegion(348, 416 + i * 16, 70, 16, CargoInfoTransferQuotaPrefix + i.ToString() + ":row", "Select transfer quota row"));
                    regions.Add(new HitRegion(454, 416 + i * 16, 13, 16, CargoInfoTransferQuotaPrefix + i.ToString() + ":up", "Increase transfer quota amount"));
                    regions.Add(new HitRegion(468, 416 + i * 16, 13, 16, CargoInfoTransferQuotaPrefix + i.ToString() + ":down", "Decrease transfer quota amount"));
                    regions.Add(new HitRegion(482, 416 + i * 16, 16, 16, CargoInfoTransferQuotaPrefix + i.ToString() + ":remove", "Remove transfer quota item"));
                }
                regions.Add(new HitRegion(344, 478, 84, 20, CargoInfoTransferClearId, "Clear transfer quota"));
                regions.Add(new HitRegion(420, 478, 92, 20, CargoInfoTransferNowId, "Transfer cargo quota now"));
            }

            if (filterDropdownOpen)
            {
                for (int i = 0; i < categories.Length; i++)
                    regions.Add(new HitRegion(filterX, y0 + widgetHeader + rowH * i, filterWidth, rowH, CargoInfoFilterPrefix + categories[i], "Filter cargo mix"));
            }

            int stripY = y0 + widgetHeader + gap + rowH;
            int stripH = Math.Max(barH + gap, rowH);
            int each = Math.Max(1, panelW / categories.Length);
            for (int i = 0; i < categories.Length; i++)
            {
                int w = i == categories.Length - 1 ? panelW - each * i : each;
                regions.Add(new HitRegion(x1 + each * i, stripY, w, stripH, CargoInfoFilterPrefix + categories[i], "Filter cargo mix"));
            }

            regions.Add(new HitRegion(x2 + pad, y0 + widgetHeader, Math.Max(1, panelW - pad * 2), Math.Max(1, panelH - widgetHeader - pad), CargoInfoActionScrollId, "Scroll selected cargo actions"));
            int rowY = y0 + widgetHeader + gap;
            int visibleRows = Math.Max(1, (y0 + panelH - rowY - pad) / rowH);
            if (visibleRows > 9)
                visibleRows = 9;
            for (int i = 0; i < visibleRows; i++)
                regions.Add(new HitRegion(x2 + pad, rowY + rowH * i, Math.Max(1, panelW - pad * 2), rowH, CargoInfoActionPrefix + i.ToString(), "Apply selected cargo block action"));

            return regions.ToArray();
        }
        static HitRegion[] BuildCanonicalCargoInfoPanelRegions(bool filterDropdownOpen, bool transferMode)
        {
            var regions = new List<HitRegion>();
            regions.Add(new HitRegion(0, 428, 168, 68, CargoInfoBlockScrollId, "Scroll cargo block bars"));
            regions.Add(new HitRegion(104, 328, 64, 17, CargoInfoSendBlockToTransferId, "Send indicated cargo block to transfer"));
            for (int i = 0; i < 13; i++)
                regions.Add(new HitRegion(6 + i * 12, 448, 10, 36, CargoInfoBlockPrefix + i.ToString(), "Select cargo block"));

            regions.Add(new HitRegion(168, 344, 176, 144, CargoInfoMixScrollId, "Scroll cargo mix rows"));
            regions.Add(new HitRegion(248, 328, 84, 16, CargoInfoFilterToggleId, "Choose cargo filter"));
            regions.Add(new HitRegion(178, 368, 40, 16, CargoInfoMixSortPrefix + "ITEM", "Sort cargo mix by item"));
            regions.Add(new HitRegion(222, 368, 32, 16, CargoInfoMixSortPrefix + "QUANT", "Sort cargo mix by quantity"));
            regions.Add(new HitRegion(258, 368, 34, 16, CargoInfoMixSortPrefix + "VOLUME", "Sort cargo mix by volume"));
            regions.Add(new HitRegion(296, 368, 30, 16, CargoInfoMixSortPrefix + "MASS", "Sort cargo mix by mass"));
            for (int i = 0; i < 6; i++)
                regions.Add(new HitRegion(176, 384 + i * 16, 152, 16, CargoInfoMixRowPrefix + i.ToString(), "Select cargo mix row"));
            regions.Add(new HitRegion(254, 480, 86, 16, CargoInfoMixAddToQuotaId, "Add selected cargo mix rows to transfer quota"));

            regions.Add(new HitRegion(360, 328, 78, 16, CargoInfoTransferModeId, "Show cargo transfer quota"));
            regions.Add(new HitRegion(438, 328, 70, 16, CargoInfoActionsModeId, "Show selected block actions"));
            regions.Add(new HitRegion(344, 344, 168, 144, CargoInfoActionScrollId, transferMode ? "Scroll transfer quota" : "Scroll selected cargo actions"));
            if (transferMode)
            {
                regions.Add(new HitRegion(352, 344, 46, 18, CargoInfoTransferSourceSelectId, "Set transfer source selection"));
                regions.Add(new HitRegion(398, 344, 108, 18, CargoInfoTransferSourceViewId, "Show transfer source cargo mix"));
                regions.Add(new HitRegion(352, 374, 46, 18, CargoInfoTransferDestSelectId, "Set transfer destination selection"));
                regions.Add(new HitRegion(398, 374, 108, 18, CargoInfoTransferDestViewId, "Show transfer destination cargo mix"));
                regions.Add(new HitRegion(456, 356, 50, 18, CargoInfoTransferDirectionId, "Reverse transfer direction"));
                regions.Add(new HitRegion(348, 480, 76, 16, CargoInfoTransferClearId, "Clear transfer quota"));
                regions.Add(new HitRegion(424, 480, 86, 16, CargoInfoTransferNowId, "Transfer cargo quota now"));
                for (int i = 0; i < 4; i++)
                {
                    regions.Add(new HitRegion(348, 416 + i * 16, 70, 16, CargoInfoTransferQuotaPrefix + i.ToString() + ":row", "Select transfer quota row"));
                    regions.Add(new HitRegion(454, 416 + i * 16, 13, 16, CargoInfoTransferQuotaPrefix + i.ToString() + ":up", "Increase transfer quota amount"));
                    regions.Add(new HitRegion(468, 416 + i * 16, 13, 16, CargoInfoTransferQuotaPrefix + i.ToString() + ":down", "Decrease transfer quota amount"));
                    regions.Add(new HitRegion(482, 416 + i * 16, 16, 16, CargoInfoTransferQuotaPrefix + i.ToString() + ":remove", "Remove transfer quota item"));
                }
            }
            else
            {
                for (int i = 0; i < 9; i++)
                    regions.Add(new HitRegion(352, 346 + i * 14, 140, 14, CargoInfoActionPrefix + i.ToString(), "Apply selected cargo block action"));
            }
            // Add precise controls again at the end because touch hit testing gives priority to later regions.
            regions.Add(new HitRegion(96, 328, 76, 18, CargoInfoSendBlockToTransferId, "Send indicated cargo block to transfer"));
            regions.Add(new HitRegion(248, 328, 84, 18, CargoInfoFilterToggleId, "Choose cargo filter"));
            regions.Add(new HitRegion(178, 368, 40, 16, CargoInfoMixSortPrefix + "ITEM", "Sort cargo mix by item"));
            regions.Add(new HitRegion(222, 368, 32, 16, CargoInfoMixSortPrefix + "QUANT", "Sort cargo mix by quantity"));
            regions.Add(new HitRegion(258, 368, 34, 16, CargoInfoMixSortPrefix + "VOLUME", "Sort cargo mix by volume"));
            regions.Add(new HitRegion(296, 368, 30, 16, CargoInfoMixSortPrefix + "MASS", "Sort cargo mix by mass"));
            regions.Add(new HitRegion(250, 478, 92, 20, CargoInfoMixAddToQuotaId, "Add selected cargo mix rows to transfer quota"));
            regions.Add(new HitRegion(360, 328, 78, 18, CargoInfoTransferModeId, "Show cargo transfer quota"));
            regions.Add(new HitRegion(438, 328, 70, 18, CargoInfoActionsModeId, "Show selected block actions"));
            if (transferMode)
            {
                regions.Add(new HitRegion(348, 344, 50, 18, CargoInfoTransferSourceSelectId, "Set transfer source selection"));
                regions.Add(new HitRegion(398, 344, 108, 18, CargoInfoTransferSourceViewId, "Show transfer source cargo mix"));
                regions.Add(new HitRegion(348, 374, 50, 18, CargoInfoTransferDestSelectId, "Set transfer destination selection"));
                regions.Add(new HitRegion(398, 374, 108, 18, CargoInfoTransferDestViewId, "Show transfer destination cargo mix"));
                regions.Add(new HitRegion(456, 356, 50, 18, CargoInfoTransferDirectionId, "Reverse transfer direction"));
                for (int i = 0; i < 4; i++)
                {
                    regions.Add(new HitRegion(348, 416 + i * 16, 70, 16, CargoInfoTransferQuotaPrefix + i.ToString() + ":row", "Select transfer quota row"));
                    regions.Add(new HitRegion(454, 416 + i * 16, 13, 16, CargoInfoTransferQuotaPrefix + i.ToString() + ":up", "Increase transfer quota amount"));
                    regions.Add(new HitRegion(468, 416 + i * 16, 13, 16, CargoInfoTransferQuotaPrefix + i.ToString() + ":down", "Decrease transfer quota amount"));
                    regions.Add(new HitRegion(482, 416 + i * 16, 16, 16, CargoInfoTransferQuotaPrefix + i.ToString() + ":remove", "Remove transfer quota item"));
                }
                regions.Add(new HitRegion(344, 478, 84, 20, CargoInfoTransferClearId, "Clear transfer quota"));
                regions.Add(new HitRegion(420, 478, 92, 20, CargoInfoTransferNowId, "Transfer cargo quota now"));
            }

            if (filterDropdownOpen)
            {
                string[] dropdownCategories = new[] { "ALL", "ORE", "INGOT", "COMPONENTS", "TOOLS", "CONSUMABLE" };
                for (int i = 0; i < dropdownCategories.Length; i++)
                    regions.Add(new HitRegion(248, 344 + i * 16, 84, 16, CargoInfoFilterPrefix + dropdownCategories[i], "Filter cargo mix"));
            }

            return regions.ToArray();
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


































