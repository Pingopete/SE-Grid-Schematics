using VRage.Game.ModAPI;
using VRage.Game;
using VRage.Utils;
using System;

namespace GridSchematics
{
    public class GridSchematicsConfig
    {
        public const string FillNone = "NONE";
        public const string FillDensity = "DENSITY";
        public const string FillThickness = "THICKNESS";
        public const string FillVoids = "VOIDS";
        public const string FillHits = "HITS";
        public const string HullColorGreyscale = "GREYSCALE";
        public const string HullColorThermal = "THERMAL";
        public const string HullColorUi = "UI";
        public const int DefaultUiMainColorHue = 192;
        public const int DefaultUiHighlightColorHue = 48;
        public const int DefaultSchematicMainColorHue = 132;
        public const int DefaultSchematicSecondaryColorHue = 313;
        public const int DefaultConveyorColorHue = 187;
        public const string DefaultUiFont = "MOZARTGLOW";
        static readonly UiFontOption[] UiFontOptions = new[]
        {
            new UiFontOption("DEBUG", "DEBUG", "Debug", new[] { "DEBUG" }),
            new UiFontOption("BAHNSCHRIFT", "BAHN", "GridSchematics_Bahnschrift", new[] { "BAHNSCHRIFT", "BAHN", "GRIDSCHEMATICS_BAHNSCHRIFT" }),
            new UiFontOption("CRYSRG", "CRYS", "GridSchematics_Crysrg", new[] { "CRYSRG", "CRYS", "GRIDSCHEMATICS_CRYSRG" }),
            new UiFontOption("MONOM", "MONO", "GridSchematics_MonoM", new[] { "MONOM", "MONO_M", "MONO", "MONOGLOW", "MONO_GLOW", "GRIDSCHEMATICS_MONOM" }),
            new UiFontOption("MONOMHARD", "MONOH", "GridSchematics_MonoMHard", new[] { "MONOMHARD", "MONO_M_HARD", "MONOHARD", "MONO HARD", "GRIDSCHEMATICS_MONOMHARD" }),
            new UiFontOption("MOZART", "MOZ", "GridSchematics_Mozart", new[] { "MOZART", "GRIDSCHEMATICS_MOZART" }),
            new UiFontOption("MOZARTGLOW", "MOZG", "GridSchematics_MozartGlow", new[] { "MOZARTGLOW", "MOZART_GLOW", "MOZGLOW", "GRIDSCHEMATICS_MOZARTGLOW" }),
            new UiFontOption("TELEGRAMA", "TELE", "GridSchematics_Telegrama", new[] { "TELEGRAMA", "TELEGRAM", "GRIDSCHEMATICS_TELEGRAMA" }),
            new UiFontOption("TELEGRAMAGLOW", "TELEG", "GridSchematics_TelegramaGlow", new[] { "TELEGRAMAGLOW", "TELEGRAMA_GLOW", "TELEGRAM_GLOW", "TELEGLOW", "GRIDSCHEMATICS_TELEGRAMAGLOW" })
        };

        struct UiFontOption
        {
            public readonly string Id;
            public readonly string Label;
            public readonly string Subtype;
            public readonly string[] Aliases;

            public UiFontOption(string id, string label, string subtype, string[] aliases)
            {
                Id = id;
                Label = label;
                Subtype = subtype;
                Aliases = aliases;
            }
        }

        public string View = "TOP";
        public int Resolution = 256;
        public string FillMode = FillThickness;
        public string HullScanColorScale = HullColorGreyscale;
        public bool Enabled = true;
        public bool ShowDebug = false;
        public bool ShowPerfStats = false;
        public bool ShowBlocks = false;
        public bool ShowBorder = true;
        public bool ShowGrid = true;
        public bool ShowReference = true;
        public bool ShowCenterOfMass = false;
        public bool ShowPanelPosition = false;
        public bool ShowDockedMobileGrids = false;
        public bool ShowHullScan = true;
        public bool ShowAllConnections = false;
        public bool BlocksOccludeConveyors = true;
        public bool ShowConnectedNetworks = false;
        public bool ShowConveyor = true;
        public bool ShowFillBars = true;
        public int FillBarsVisibilityLevel = 2;
        public bool BlurScan = true;
        public bool PerformanceMode = false;
        public bool HighResScanning = false;
        // Scan-time 1+4 ray refinement. ON (default) = full fidelity; OFF = recovery-only
        // refinement (extra rays spent only where the center ray missed) for faster scans.
        public bool SuperSampling = true;
        public bool ShowInfoPanel = false;
        public string UiPalette = "BLUE";
        public int UiHueShift = DefaultUiMainColorHue;
        public float UiBrightness = 1f;
        public float UiSaturation = 1f;
        public float UiAlpha = 1f;
        public string UiFont = DefaultUiFont;
        public int UiAccentHueShift = DefaultUiHighlightColorHue;
        public float UiAccentBrightness = 1f;
        public float UiAccentSaturation = 1f;
        public float UiPanelBrightness = 1f;
        public float UiPanelAlpha = 1f;
        public int SchematicMainHue = DefaultSchematicMainColorHue;
        public int SchematicSecondaryHue = DefaultSchematicSecondaryColorHue;
        public int ConveyorHue = DefaultConveyorColorHue;
        public float HullScanAlpha = 1f;
        public float SchematicAlpha = 1f;
        public string StorageColor = "GREEN";
        public string EffectorColor = "MAGENTA";
        public string OverlayMode = "NONE";
        public int RotationTop = 0;
        public int RotationLeft = 0;
        public int RotationFront = 0;
        public bool MouseControl = true;
        public string MouseSensitivity = "MID";
        public bool AllowGridRotation = true;
        public bool HasCursorCalibration = false;
        public float CursorCalibrationM11 = 1f;
        public float CursorCalibrationM12 = 0f;
        public float CursorCalibrationM13 = 0f;
        public float CursorCalibrationM21 = 0f;
        public float CursorCalibrationM22 = 1f;
        public float CursorCalibrationM23 = 0f;

        public void Parse(string customData)
        {
            if (string.IsNullOrEmpty(customData))
                return;

            var lines = customData.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();
                if (line.Length == 0)
                    continue;

                if (line.StartsWith("#") || line.StartsWith("//"))
                    continue;

                var separatorIndex = line.IndexOf('=');
                if (separatorIndex < 0)
                    continue;

                var key = line.Substring(0, separatorIndex).Trim().ToUpperInvariant();
                var value = line.Substring(separatorIndex + 1).Trim();
                switch (key)
                {
                    case "ENABLED":
                        bool enabled;
                        if (bool.TryParse(value, out enabled))
                            Enabled = enabled;
                        break;
                    case "VIEW":
                        if (!string.IsNullOrEmpty(value))
                            View = value.ToUpperInvariant();
                        break;
                    case "RES":
                        int res;
                        if (int.TryParse(value, out res) && res > 0)
                            Resolution = res;
                        break;
                    case "FILLMODE":
                        if (!string.IsNullOrEmpty(value))
                            FillMode = NormalizeFillMode(value);
                        break;
                    case "HULL_SCAN_COLOR":
                    case "HULL_COLOR":
                    case "SCAN_COLOR":
                    case "COLOR_SCALE":
                        HullScanColorScale = NormalizeHullScanColorScale(value);
                        break;
                    case "DEBUG":
                        bool debug;
                        if (bool.TryParse(value, out debug))
                            ShowDebug = debug;
                        break;
                    case "PERF_STATS":
                    case "PERFORMANCE_STATS":
                    case "SHOW_PERF_STATS":
                        bool perfStats;
                        if (bool.TryParse(value, out perfStats))
                            ShowPerfStats = perfStats;
                        break;
                    case "BLOCKS":
                        bool blocks;
                        if (bool.TryParse(value, out blocks))
                            ShowBlocks = blocks;
                        break;
                    case "BORDER":
                        bool border;
                        if (bool.TryParse(value, out border))
                            ShowBorder = border;
                        break;
                    case "GRID":
                        bool grid;
                        if (bool.TryParse(value, out grid))
                            ShowGrid = grid;
                        break;
                    case "REFERENCE":
                    case "REF":
                        bool reference;
                        if (bool.TryParse(value, out reference))
                            ShowReference = reference;
                        break;
                    case "CENTER_OF_MASS":
                    case "CENTER_OF_MASS_MARKER":
                    case "COM":
                        bool centerOfMass;
                        if (bool.TryParse(value, out centerOfMass))
                            ShowCenterOfMass = centerOfMass;
                        break;
                    case "PANEL_POSITION":
                    case "PANEL_POSITION_MARKER":
                    case "PANEL_MARKER":
                        bool panelPosition;
                        if (bool.TryParse(value, out panelPosition))
                            ShowPanelPosition = panelPosition;
                        break;
                    case "DOCKED_MOBILE_GRIDS":
                    case "DOCKED_SHIPS":
                    case "DOCKED_GRID_OVERLAYS":
                        bool dockedMobileGrids;
                        if (bool.TryParse(value, out dockedMobileGrids))
                            ShowDockedMobileGrids = dockedMobileGrids;
                        break;
                    case "HULL_SCAN":
                    case "HULLSCAN":
                    case "SCAN_LAYER":
                        bool hullScan;
                        if (bool.TryParse(value, out hullScan))
                            ShowHullScan = hullScan;
                        break;
                    case "CONNECTIONS":
                    case "ALL_CONNECTIONS":
                    case "ALLCONNECTIONS":
                        bool connections;
                        if (bool.TryParse(value, out connections))
                            ShowAllConnections = connections;
                        break;
                    case "BLOCKS_OCCLUDE_CONVEYORS":
                    case "OCCLUDE_CONVEYORS":
                    case "CONVEYOR_OCCLUSION":
                        bool blocksOccludeConveyors;
                        if (bool.TryParse(value, out blocksOccludeConveyors))
                            BlocksOccludeConveyors = blocksOccludeConveyors;
                        break;
                    case "SHOW_CONNECTED_NETWORKS":
                    case "CONNECTED_NETWORKS":
                        bool showConnectedNetworks;
                        if (bool.TryParse(value, out showConnectedNetworks))
                            ShowConnectedNetworks = showConnectedNetworks;
                        break;
                    case "BLUR":
                        bool blur;
                        if (bool.TryParse(value, out blur))
                            BlurScan = blur;
                        break;
                    case "PERFORMANCE_MODE":
                    case "PERFORMANCEMODE":
                    case "RENDER_PERFORMANCE":
                        bool performanceMode;
                        if (bool.TryParse(value, out performanceMode))
                            PerformanceMode = performanceMode;
                        break;
                    case "HIGH_RES_SCANNING":
                    case "HIGHRESSCANNING":
                    case "HIGH_RES_SCAN":
                        bool highResScanning;
                        if (bool.TryParse(value, out highResScanning))
                            HighResScanning = highResScanning;
                        break;
                    case "SUPER_SAMPLING":
                    case "SUPERSAMPLING":
                        bool superSampling;
                        if (bool.TryParse(value, out superSampling))
                            SuperSampling = superSampling;
                        break;
                    case "INFO_PANEL":
                    case "INFO":
                        bool infoPanel;
                        if (bool.TryParse(value, out infoPanel))
                            ShowInfoPanel = infoPanel;
                        break;
                    case "CONVEYOR":
                    case "CONVEYORS":
                        bool conveyor;
                        if (bool.TryParse(value, out conveyor))
                            ShowConveyor = conveyor;
                        break;
                    case "FILL_BARS":
                    case "FILLBARS":
                    case "SHOW_FILL_BARS":
                        bool fillBars;
                        if (bool.TryParse(value, out fillBars))
                        {
                            ShowFillBars = fillBars;
                            FillBarsVisibilityLevel = fillBars ? 2 : 0;
                        }
                        else
                        {
                            int fillBarsLevel;
                            if (int.TryParse(value, out fillBarsLevel))
                            {
                                FillBarsVisibilityLevel = NormalizeFillBarsVisibilityLevel(fillBarsLevel);
                                ShowFillBars = FillBarsVisibilityLevel > 0;
                            }
                        }
                        break;
                    case "FILL_BARS_LEVEL":
                    case "FILLBARS_LEVEL":
                    case "SHOW_FILL_BARS_LEVEL":
                        int parsedFillBarsLevel;
                        if (int.TryParse(value, out parsedFillBarsLevel))
                        {
                            FillBarsVisibilityLevel = NormalizeFillBarsVisibilityLevel(parsedFillBarsLevel);
                            ShowFillBars = FillBarsVisibilityLevel > 0;
                        }
                        break;
                    case "OVERLAY":
                    case "SCHEMATIC":
                        if (!string.IsNullOrEmpty(value))
                            OverlayMode = value.ToUpperInvariant();
                        break;
                    case "UI_PALETTE":
                    case "PALETTE":
                    case "THEME":
                        if (!string.IsNullOrEmpty(value))
                            UiPalette = NormalizeUiPalette(value);
                        break;
                    case "UI_COLOR_HUE":
                    case "UI_MAIN_COLOR_HUE":
                    case "MAIN_COLOR_HUE":
                        int absoluteUiHue;
                        if (int.TryParse(value, out absoluteUiHue))
                            UiHueShift = NormalizeHue(absoluteUiHue);
                        break;
                    case "UI_HUE":
                    case "HUE":
                        int hue;
                        if (int.TryParse(value, out hue))
                            UiHueShift = NormalizeHue(DefaultUiMainColorHue + hue);
                        break;
                    case "UI_BRIGHTNESS":
                    case "BRIGHTNESS":
                        float brightness;
                        if (float.TryParse(value, out brightness))
                            UiBrightness = ClampFloat(brightness, 0.45f, 1.65f);
                        break;
                    case "UI_SATURATION":
                    case "SATURATION":
                        float saturation;
                        if (float.TryParse(value, out saturation))
                            UiSaturation = ClampFloat(saturation, 0f, 2f);
                        break;
                    case "UI_ALPHA":
                    case "ALPHA":
                        float alpha;
                        if (float.TryParse(value, out alpha))
                            UiAlpha = ClampFloat(alpha, 0.25f, 1f);
                        break;
                    case "UI_FONT":
                    case "FONT":
                        UiFont = NormalizeUiFont(value);
                        break;
                    case "UI_HIGHLIGHT_COLOR_HUE":
                    case "UI_ACCENT_COLOR_HUE":
                    case "HIGHLIGHT_COLOR_HUE":
                        int absoluteAccentHue;
                        if (int.TryParse(value, out absoluteAccentHue))
                            UiAccentHueShift = NormalizeHue(absoluteAccentHue);
                        break;
                    case "UI_ACCENT_HUE":
                    case "ACCENT_HUE":
                        int accentHue;
                        if (int.TryParse(value, out accentHue))
                            UiAccentHueShift = NormalizeHue(DefaultUiHighlightColorHue + accentHue);
                        break;
                    case "UI_ACCENT_BRIGHTNESS":
                    case "ACCENT_BRIGHTNESS":
                    case "UI_HIGHLIGHT_BRIGHTNESS":
                    case "HIGHLIGHT_BRIGHTNESS":
                        float accentBrightness;
                        if (float.TryParse(value, out accentBrightness))
                            UiAccentBrightness = ClampFloat(accentBrightness, 0.45f, 1.65f);
                        break;
                    case "UI_ACCENT_SATURATION":
                    case "ACCENT_SATURATION":
                    case "UI_HIGHLIGHT_SATURATION":
                    case "HIGHLIGHT_SATURATION":
                        float accentSaturation;
                        if (float.TryParse(value, out accentSaturation))
                            UiAccentSaturation = ClampFloat(accentSaturation, 0f, 2f);
                        break;
                    case "UI_PANEL_BRIGHTNESS":
                    case "PANEL_BRIGHTNESS":
                        float panelBrightness;
                        if (float.TryParse(value, out panelBrightness))
                            UiPanelBrightness = ClampFloat(panelBrightness, 0.45f, 1.65f);
                        break;
                    case "UI_PANEL_ALPHA":
                    case "PANEL_ALPHA":
                        float panelAlpha;
                        if (float.TryParse(value, out panelAlpha))
                            UiPanelAlpha = ClampFloat(panelAlpha, 0.25f, 1f);
                        break;
                    case "SCHEMATIC_MAIN_COLOR_HUE":
                    case "SCH_MAIN_COLOR_HUE":
                    case "SCH_MAIN_COLOR":
                        int absoluteSchematicMainHue;
                        if (int.TryParse(value, out absoluteSchematicMainHue))
                            SchematicMainHue = NormalizeHue(absoluteSchematicMainHue);
                        break;
                    case "SCHEMATIC_MAIN_HUE":
                    case "SCH_MAIN_HUE":
                        int schematicMainHue;
                        if (int.TryParse(value, out schematicMainHue))
                            SchematicMainHue = NormalizeHue(DefaultSchematicMainColorHue + schematicMainHue);
                        break;
                    case "SCHEMATIC_SECONDARY_COLOR_HUE":
                    case "SCH_SECONDARY_COLOR_HUE":
                    case "SCH_SECONDARY_COLOR":
                        int absoluteSchematicSecondaryHue;
                        if (int.TryParse(value, out absoluteSchematicSecondaryHue))
                            SchematicSecondaryHue = NormalizeHue(absoluteSchematicSecondaryHue);
                        break;
                    case "SCHEMATIC_SECONDARY_HUE":
                    case "SCH_SECONDARY_HUE":
                        int schematicSecondaryHue;
                        if (int.TryParse(value, out schematicSecondaryHue))
                            SchematicSecondaryHue = NormalizeHue(DefaultSchematicSecondaryColorHue + schematicSecondaryHue);
                        break;
                    case "CONVEYOR_COLOR_HUE":
                    case "CONVEYOR_COLOR":
                        int absoluteConveyorHue;
                        if (int.TryParse(value, out absoluteConveyorHue))
                            ConveyorHue = NormalizeHue(absoluteConveyorHue);
                        break;
                    case "CONVEYOR_HUE":
                        int conveyorHue;
                        if (int.TryParse(value, out conveyorHue))
                            ConveyorHue = NormalizeHue(DefaultConveyorColorHue + conveyorHue);
                        break;
                    case "HULL_SCAN_ALPHA":
                    case "HULL_ALPHA":
                        float hullAlpha;
                        if (float.TryParse(value, out hullAlpha))
                            HullScanAlpha = ClampFloat(hullAlpha, 0f, 1f);
                        break;
                    case "SCHEMATIC_ALPHA":
                    case "OVERLAY_ALPHA":
                        float schematicAlpha;
                        if (float.TryParse(value, out schematicAlpha))
                            SchematicAlpha = ClampFloat(schematicAlpha, 0f, 1f);
                        break;
                    case "STORAGE_COLOR":
                        if (!string.IsNullOrEmpty(value))
                            StorageColor = NormalizeSchematicColor(value);
                        break;
                    case "EFFECTOR_COLOR":
                        if (!string.IsNullOrEmpty(value))
                            EffectorColor = NormalizeSchematicColor(value);
                        break;
                    case "ROT_TOP":
                        int rotTop;
                        if (int.TryParse(value, out rotTop))
                            RotationTop = NormalizeRotation(rotTop);
                        break;
                    case "ROT_LEFT":
                        int rotLeft;
                        if (int.TryParse(value, out rotLeft))
                            RotationLeft = NormalizeRotation(rotLeft);
                        break;
                    case "ROT_FRONT":
                        int rotFront;
                        if (int.TryParse(value, out rotFront))
                            RotationFront = NormalizeRotation(rotFront);
                        break;
                    case "MOUSE_CONTROL":
                    case "CURSOR_MOUSE_CONTROL":
                        bool mouseControl;
                        if (bool.TryParse(value, out mouseControl))
                            MouseControl = mouseControl;
                        break;
                    case "MOUSE_SENSITIVITY":
                    case "CURSOR_MOUSE_SENSITIVITY":
                        MouseSensitivity = NormalizeMouseSensitivity(value);
                        break;
                    case "ALLOW_GRID_ROTATION":
                    case "GRID_ROTATION":
                        bool allowGridRotation;
                        if (bool.TryParse(value, out allowGridRotation))
                            AllowGridRotation = allowGridRotation;
                        break;
                    case "CURSOR_CALIBRATED":
                    case "CURSOR_CALIBRATION":
                        bool calibrated;
                        if (bool.TryParse(value, out calibrated))
                            HasCursorCalibration = calibrated;
                        break;
                    case "CURSOR_CALIB_M11":
                        TryParseCalibrationFloat(value, ref CursorCalibrationM11);
                        break;
                    case "CURSOR_CALIB_M12":
                        TryParseCalibrationFloat(value, ref CursorCalibrationM12);
                        break;
                    case "CURSOR_CALIB_M13":
                        TryParseCalibrationFloat(value, ref CursorCalibrationM13);
                        break;
                    case "CURSOR_CALIB_M21":
                        TryParseCalibrationFloat(value, ref CursorCalibrationM21);
                        break;
                    case "CURSOR_CALIB_M22":
                        TryParseCalibrationFloat(value, ref CursorCalibrationM22);
                        break;
                    case "CURSOR_CALIB_M23":
                        TryParseCalibrationFloat(value, ref CursorCalibrationM23);
                        break;
                }
            }
        }

        public ScanView GetScanView()
        {
            if (View == "FRONT")
                return ScanView.Front;
            if (View == "LEFT")
                return ScanView.Side;
            if (View == "SIDE")
                return ScanView.Side;
            return ScanView.Top;
        }

        public void CycleFillMode()
        {
            FillMode = NormalizeFillMode(FillMode);
            if (FillMode == FillDensity)
                FillMode = FillThickness;
            else if (FillMode == FillThickness)
                FillMode = FillVoids;
            else
                FillMode = FillDensity;
        }

        public static string NormalizeFillMode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return FillThickness;

            value = value.ToUpperInvariant();
            if (value == FillNone || value == FillDensity || value == FillThickness || value == FillVoids)
                return value;
            if (value == FillHits || value == "HIT" || value == "FILL")
                return FillThickness;
            return FillThickness;
        }

        public void CycleHullScanColorScale()
        {
            HullScanColorScale = NormalizeHullScanColorScale(HullScanColorScale);
            if (HullScanColorScale == HullColorGreyscale)
                HullScanColorScale = HullColorThermal;
            else if (HullScanColorScale == HullColorThermal)
                HullScanColorScale = HullColorUi;
            else
                HullScanColorScale = HullColorGreyscale;
        }

        public static string NormalizeHullScanColorScale(string value)
        {
            if (string.IsNullOrEmpty(value))
                return HullColorGreyscale;

            value = value.ToUpperInvariant();
            if (value == "GRAY" || value == "GREY" || value == "GRAYSCALE")
                return HullColorGreyscale;
            if (value == HullColorThermal || value == "HEAT" || value == "THERM")
                return HullColorThermal;
            if (value == HullColorUi || value == "STYLE" || value == "THEME")
                return HullColorUi;
            return HullColorGreyscale;
        }

        public string GetHullScanColorScaleLabel()
        {
            return GetHullScanColorScaleLabelStatic(HullScanColorScale);
        }

        public static string GetHullScanColorScaleLabelStatic(string colorScale)
        {
            string color = NormalizeHullScanColorScale(colorScale);
            if (color == HullColorThermal)
                return "THERMAL";
            if (color == HullColorUi)
                return "STYLE";
            return "GREY";
        }

        public string GetFillModeLabel()
        {
            return GetFillModeLabelStatic(FillMode);
        }

        public static string GetFillModeLabelStatic(string fillMode)
        {
            if (fillMode == FillNone)
                return "NONE";
            if (fillMode == FillDensity)
                return "DEN";
            if (fillMode == FillVoids)
                return "VOID";
            return "THK";
        }

        public void ToggleFillMode(string fillMode)
        {
            fillMode = NormalizeFillMode(fillMode);
            FillMode = FillMode == fillMode ? FillNone : fillMode;
        }

        public int GetRotationSteps()
        {
            if (View == "FRONT")
                return RotationFront;
            if (View == "LEFT" || View == "SIDE")
                return RotationLeft;
            return RotationTop;
        }

        public void RotateCurrentView(int deltaSteps)
        {
            if (View == "FRONT")
                RotationFront = NormalizeRotation(RotationFront + deltaSteps);
            else if (View == "LEFT" || View == "SIDE")
                RotationLeft = NormalizeRotation(RotationLeft + deltaSteps);
            else
                RotationTop = NormalizeRotation(RotationTop + deltaSteps);
        }

        public void CycleUiPalette()
        {
            if (UiPalette == "BLUE")
                UiPalette = "GREEN";
            else if (UiPalette == "GREEN")
                UiPalette = "AMBER";
            else if (UiPalette == "AMBER")
                UiPalette = "ICE";
            else
                UiPalette = "BLUE";
        }

        public void AdjustUiHue(int delta)
        {
            UiHueShift = NormalizeHue(UiHueShift + delta);
        }

        public void AdjustUiBrightness(float delta)
        {
            UiBrightness = ClampFloat(UiBrightness + delta, 0.45f, 1.65f);
        }

        public void AdjustUiSaturation(float delta)
        {
            UiSaturation = ClampFloat(UiSaturation + delta, 0f, 2f);
        }

        public void AdjustUiAlpha(float delta)
        {
            UiAlpha = ClampFloat(UiAlpha + delta, 0.25f, 1f);
        }

        public void AdjustUiAccentHue(int delta)
        {
            UiAccentHueShift = NormalizeHue(UiAccentHueShift + delta);
        }

        public void AdjustUiAccentBrightness(float delta)
        {
            UiAccentBrightness = ClampFloat(UiAccentBrightness + delta, 0.45f, 1.65f);
        }

        public void AdjustUiAccentSaturation(float delta)
        {
            UiAccentSaturation = ClampFloat(UiAccentSaturation + delta, 0f, 2f);
        }

        public void AdjustUiPanelBrightness(float delta)
        {
            UiPanelBrightness = ClampFloat(UiPanelBrightness + delta, 0.45f, 1.65f);
        }

        public void AdjustUiPanelAlpha(float delta)
        {
            UiPanelAlpha = ClampFloat(UiPanelAlpha + delta, 0.25f, 1f);
        }

        public void AdjustHullScanAlpha(float delta)
        {
            HullScanAlpha = ClampFloat(HullScanAlpha + delta, 0f, 1f);
        }

        public void AdjustSchematicAlpha(float delta)
        {
            SchematicAlpha = ClampFloat(SchematicAlpha + delta, 0f, 1f);
        }

        public void CycleStorageColor()
        {
            StorageColor = CycleSchematicColor(StorageColor);
        }

        public void CycleEffectorColor()
        {
            EffectorColor = CycleSchematicColor(EffectorColor);
        }

        public void ResetUiSettings()
        {
            ResetUiStyleSettings();
            HullScanAlpha = 1f;
            SchematicAlpha = 1f;
            PerformanceMode = false;
            HighResScanning = false;
            StorageColor = "GREEN";
            EffectorColor = "MAGENTA";
            ShowHullScan = true;
            ShowFillBars = true;
            FillBarsVisibilityLevel = 2;
        }

        public void ResetPanelSettingsPreservingCalibration()
        {
            bool hasCursorCalibration = HasCursorCalibration;
            float m11 = CursorCalibrationM11;
            float m12 = CursorCalibrationM12;
            float m13 = CursorCalibrationM13;
            float m21 = CursorCalibrationM21;
            float m22 = CursorCalibrationM22;
            float m23 = CursorCalibrationM23;

            CopyFrom(new GridSchematicsConfig());

            HasCursorCalibration = hasCursorCalibration;
            CursorCalibrationM11 = m11;
            CursorCalibrationM12 = m12;
            CursorCalibrationM13 = m13;
            CursorCalibrationM21 = m21;
            CursorCalibrationM22 = m22;
            CursorCalibrationM23 = m23;
        }

        public void CopyFrom(GridSchematicsConfig other)
        {
            if (other == null)
                return;

            View = other.View;
            Resolution = other.Resolution;
            FillMode = other.FillMode;
            HullScanColorScale = other.HullScanColorScale;
            Enabled = other.Enabled;
            ShowDebug = other.ShowDebug;
            ShowPerfStats = other.ShowPerfStats;
            ShowBlocks = other.ShowBlocks;
            ShowBorder = other.ShowBorder;
            ShowGrid = other.ShowGrid;
            ShowReference = other.ShowReference;
            ShowCenterOfMass = other.ShowCenterOfMass;
            ShowPanelPosition = other.ShowPanelPosition;
            ShowDockedMobileGrids = other.ShowDockedMobileGrids;
            ShowHullScan = other.ShowHullScan;
            ShowAllConnections = other.ShowAllConnections;
            BlocksOccludeConveyors = other.BlocksOccludeConveyors;
            ShowConnectedNetworks = other.ShowConnectedNetworks;
            ShowConveyor = other.ShowConveyor;
            ShowFillBars = other.ShowFillBars;
            FillBarsVisibilityLevel = other.FillBarsVisibilityLevel;
            BlurScan = other.BlurScan;
            PerformanceMode = other.PerformanceMode;
            HighResScanning = other.HighResScanning;
            ShowInfoPanel = other.ShowInfoPanel;
            UiPalette = other.UiPalette;
            UiHueShift = other.UiHueShift;
            UiBrightness = other.UiBrightness;
            UiSaturation = other.UiSaturation;
            UiAlpha = other.UiAlpha;
            UiFont = other.UiFont;
            UiAccentHueShift = other.UiAccentHueShift;
            UiAccentBrightness = other.UiAccentBrightness;
            UiAccentSaturation = other.UiAccentSaturation;
            UiPanelBrightness = other.UiPanelBrightness;
            UiPanelAlpha = other.UiPanelAlpha;
            SchematicMainHue = other.SchematicMainHue;
            SchematicSecondaryHue = other.SchematicSecondaryHue;
            ConveyorHue = other.ConveyorHue;
            HullScanAlpha = other.HullScanAlpha;
            SchematicAlpha = other.SchematicAlpha;
            StorageColor = other.StorageColor;
            EffectorColor = other.EffectorColor;
            OverlayMode = other.OverlayMode;
            RotationTop = other.RotationTop;
            RotationLeft = other.RotationLeft;
            RotationFront = other.RotationFront;
            MouseControl = other.MouseControl;
            MouseSensitivity = other.MouseSensitivity;
            AllowGridRotation = other.AllowGridRotation;
            HasCursorCalibration = other.HasCursorCalibration;
            CursorCalibrationM11 = other.CursorCalibrationM11;
            CursorCalibrationM12 = other.CursorCalibrationM12;
            CursorCalibrationM13 = other.CursorCalibrationM13;
            CursorCalibrationM21 = other.CursorCalibrationM21;
            CursorCalibrationM22 = other.CursorCalibrationM22;
            CursorCalibrationM23 = other.CursorCalibrationM23;
        }

        public void ResetUiStyleSettings()
        {
            UiPalette = "BLUE";
            UiHueShift = DefaultUiMainColorHue;
            UiBrightness = 1f;
            UiSaturation = 1f;
            UiAlpha = 1f;
            UiFont = DefaultUiFont;
            UiAccentHueShift = DefaultUiHighlightColorHue;
            UiAccentBrightness = 1f;
            UiAccentSaturation = 1f;
            UiPanelBrightness = 1f;
            UiPanelAlpha = 1f;
            SchematicMainHue = DefaultSchematicMainColorHue;
            SchematicSecondaryHue = DefaultSchematicSecondaryColorHue;
            ConveyorHue = DefaultConveyorColorHue;
        }

        public static int NormalizeFillBarsVisibilityLevel(int value)
        {
            if (value < 0)
                return 0;
            if (value > 2)
                return 2;
            return value;
        }

        public void CycleMouseSensitivity()
        {
            MouseSensitivity = NormalizeMouseSensitivity(MouseSensitivity);
            if (MouseSensitivity == "LOW")
                MouseSensitivity = "MID";
            else if (MouseSensitivity == "MID")
                MouseSensitivity = "HIGH";
            else
                MouseSensitivity = "LOW";
        }

        public void CycleUiFont()
        {
            UiFont = NormalizeUiFont(UiFont);
            int index = GetUiFontOptionIndex(UiFont);
            index++;
            if (index >= UiFontOptions.Length)
                index = 0;
            UiFont = UiFontOptions[index].Id;
        }

        public static string NormalizeUiPalette(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "BLUE";

            value = value.ToUpperInvariant();
            if (value == "GREEN" || value == "AMBER" || value == "ICE")
                return value;

            return "BLUE";
        }

        public static string NormalizeSchematicColor(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "GREEN";

            value = value.ToUpperInvariant();
            if (value == "GREEN" || value == "MAGENTA" || value == "YELLOW" || value == "CYAN" || value == "BLUE" || value == "WHITE")
                return value;

            return "GREEN";
        }

        public static string NormalizeMouseSensitivity(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "MID";

            value = value.ToUpperInvariant();
            if (value == "LOW" || value == "MID" || value == "HIGH")
                return value;

            if (value == "MED" || value == "MEDIUM" || value == "NORMAL")
                return "MID";

            return "MID";
        }

        public static string NormalizeUiFont(string value)
        {
            if (string.IsNullOrEmpty(value))
                return DefaultUiFont;

            value = value.ToUpperInvariant();
            for (int i = 0; i < UiFontOptions.Length; i++)
            {
                var option = UiFontOptions[i];
                if (value == option.Id || value == option.Label || value == option.Subtype.ToUpperInvariant())
                    return option.Id;
                if (option.Aliases != null)
                {
                    for (int j = 0; j < option.Aliases.Length; j++)
                    {
                        if (value == option.Aliases[j])
                            return option.Id;
                    }
                }
            }

            return DefaultUiFont;
        }

        public static string GetUiFontSubtype(string value)
        {
            return GetUiFontOption(NormalizeUiFont(value)).Subtype;
        }

        public static string GetUiFontLabel(string value)
        {
            return GetUiFontOption(NormalizeUiFont(value)).Label;
        }

        static UiFontOption GetUiFontOption(string value)
        {
            int index = GetUiFontOptionIndex(value);
            if (index < 0)
                index = GetUiFontOptionIndex(DefaultUiFont);
            return UiFontOptions[index];
        }

        static int GetUiFontOptionIndex(string value)
        {
            if (string.IsNullOrEmpty(value))
                return GetUiFontOptionIndex(DefaultUiFont);

            for (int i = 0; i < UiFontOptions.Length; i++)
            {
                if (UiFontOptions[i].Id == value)
                    return i;
            }

            return 0;
        }

        static string CycleSchematicColor(string value)
        {
            value = NormalizeSchematicColor(value);
            if (value == "GREEN")
                return "MAGENTA";
            if (value == "MAGENTA")
                return "YELLOW";
            if (value == "YELLOW")
                return "CYAN";
            if (value == "CYAN")
                return "BLUE";
            if (value == "BLUE")
                return "WHITE";
            return "GREEN";
        }

        static int NormalizeRotation(int value)
        {
            int normalized = value % 4;
            if (normalized < 0)
                normalized += 4;
            return normalized;
        }

        static int NormalizeHue(int value)
        {
            int normalized = value % 360;
            if (normalized < 0)
                normalized += 360;
            return normalized;
        }

        static float ClampFloat(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        static void TryParseCalibrationFloat(string value, ref float target)
        {
            float parsed;
            if (float.TryParse(value, out parsed) && !float.IsNaN(parsed) && !float.IsInfinity(parsed))
                target = parsed;
        }

        public void SetCursorCalibration(float m11, float m12, float m13, float m21, float m22, float m23)
        {
            HasCursorCalibration = true;
            CursorCalibrationM11 = m11;
            CursorCalibrationM12 = m12;
            CursorCalibrationM13 = m13;
            CursorCalibrationM21 = m21;
            CursorCalibrationM22 = m22;
            CursorCalibrationM23 = m23;
        }

        public string ToIniText()
        {
            return string.Format("ENABLED={0}\nVIEW={1}\nRES={2}\nFILLMODE={3}\nHULL_SCAN_COLOR={4}\nDEBUG={5}\nPERF_STATS={6}\nBLOCKS={7}\nBORDER={8}\nGRID={9}\nREFERENCE={10}\nHULL_SCAN={11}\nCONNECTIONS={12}\nBLOCKS_OCCLUDE_CONVEYORS={13}\nSHOW_CONNECTED_NETWORKS={14}\nCONVEYOR={15}\nFILL_BARS={16}\nFILL_BARS_LEVEL={17}\nBLUR={18}\nPERFORMANCE_MODE={19}\nHIGH_RES_SCANNING={20}\nSUPER_SAMPLING={57}\nINFO_PANEL={21}\nOVERLAY={22}\nUI_PALETTE={23}\nUI_COLOR_HUE={24}\nUI_BRIGHTNESS={25:0.00}\nUI_SATURATION={26:0.00}\nUI_ALPHA={27:0.00}\nUI_FONT={28}\nUI_HIGHLIGHT_COLOR_HUE={29}\nUI_ACCENT_BRIGHTNESS={30:0.00}\nUI_ACCENT_SATURATION={31:0.00}\nUI_PANEL_BRIGHTNESS={32:0.00}\nUI_PANEL_ALPHA={33:0.00}\nSCHEMATIC_MAIN_COLOR_HUE={34}\nSCHEMATIC_SECONDARY_COLOR_HUE={35}\nCONVEYOR_COLOR_HUE={36}\nHULL_SCAN_ALPHA={37:0.00}\nSCHEMATIC_ALPHA={38:0.00}\nSTORAGE_COLOR={39}\nEFFECTOR_COLOR={40}\nROT_TOP={41}\nROT_LEFT={42}\nROT_FRONT={43}\nMOUSE_CONTROL={44}\nMOUSE_SENSITIVITY={45}\nALLOW_GRID_ROTATION={46}\nCURSOR_CALIBRATED={47}\nCURSOR_CALIB_M11={48:R}\nCURSOR_CALIB_M12={49:R}\nCURSOR_CALIB_M13={50:R}\nCURSOR_CALIB_M21={51:R}\nCURSOR_CALIB_M22={52:R}\nCURSOR_CALIB_M23={53:R}\nCENTER_OF_MASS={54}\nPANEL_POSITION={55}\nDOCKED_MOBILE_GRIDS={56}\n", Enabled, View, Resolution, NormalizeFillMode(FillMode), NormalizeHullScanColorScale(HullScanColorScale), ShowDebug, ShowPerfStats, ShowBlocks, ShowBorder, ShowGrid, ShowReference, ShowHullScan, ShowAllConnections, BlocksOccludeConveyors, ShowConnectedNetworks, ShowConveyor, ShowFillBars, NormalizeFillBarsVisibilityLevel(FillBarsVisibilityLevel), BlurScan, PerformanceMode, HighResScanning, ShowInfoPanel, OverlayMode, UiPalette, UiHueShift, UiBrightness, UiSaturation, UiAlpha, NormalizeUiFont(UiFont), UiAccentHueShift, UiAccentBrightness, UiAccentSaturation, UiPanelBrightness, UiPanelAlpha, SchematicMainHue, SchematicSecondaryHue, ConveyorHue, HullScanAlpha, SchematicAlpha, StorageColor, EffectorColor, RotationTop, RotationLeft, RotationFront, MouseControl, NormalizeMouseSensitivity(MouseSensitivity), AllowGridRotation, HasCursorCalibration, CursorCalibrationM11, CursorCalibrationM12, CursorCalibrationM13, CursorCalibrationM21, CursorCalibrationM22, CursorCalibrationM23, ShowCenterOfMass, ShowPanelPosition, ShowDockedMobileGrids, SuperSampling);
        }
    }
}
