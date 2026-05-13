using Sandbox.ModAPI;
using Sandbox.Game;
using VRage.Game.ModAPI;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using Sandbox.ModAPI.Interfaces;
using System;
using VRageMath;
using VRage.Utils;

namespace GridSchematics
{
    public partial class GridSchematicsLcdApp
    {
        const int InitialRaycastScanResolution = 256;
        const int MenuCloseGraceTicks = 120;
        const int StartupScanWarmupTicks = 120;
        const int StartupScanRetryTicks = 300;
        const int PanelRenderIntervalTicks = 6;
        const int SegmentTouchRenderIntervalTicks = 6;
        const int TopologyUpdateIntervalTicks = 12;
        const int SegmentProjectionRefreshTicks = 60;
        const int SharedCursorPublishIntervalTicks = 6;
        const int SegmentCursorOnlyRenderIntervalTicks = 10;
        const int LowLevelCalibrationCountdownTicks = 300;
        const int CursorCalibrationConfirmTicks = 300;
        const int CursorCalibrationClickCooldownTicks = 15;
        const int MinimumLowLevelPanelResolutionAxis = 256;
        const int IncrementalScanSamplesPerTick = 1152;
        const int IncrementalCompositionSamplesPerTick = 8192;
        const int ViewportMotionQualityHoldTicks = 10;
        static string CopiedPanelSettings = string.Empty;
        static string CopiedUiSettings = string.Empty;
        static string CopiedRenderingSettings = string.Empty;
        static string CopiedSchematicSettings = string.Empty;
        static string CopiedDebugSettings = string.Empty;

        public IMyTextPanel Panel { get; }
        public IMyTerminalBlock OwnerBlock { get; }
        public IMyTextSurface Surface { get; }
        public GridSchematicsSession Session { get; }
        public GridSchematicsConfig Config { get; }
        public UiState Ui { get; }
        public TouchScreenApiAdapter TouchInput { get; }
        public bool CursorOnlyRender { get; private set; }
        public bool SegmentCursorOnlyRender
        {
            get { return CursorOnlyRender && Ui.SegmentMode; }
        }
        public long ConstructId { get; private set; }
        public ScanCache ConstructCache { get; private set; }
        public bool ShouldDrawAimCursorHud
        {
            get { return Config.Enabled && TouchInput != null && TouchInput.IsAimCursorActive && TouchInput.IsVisualCursorOnScreen; }
        }
        public bool IsCursorCalibrationBlocking
        {
            get { return Config.Enabled && (!Config.HasCursorCalibration || Ui.CalibrationCompletedTick >= 0); }
        }
        public bool IsLowLevelCalibrationBlocking
        {
            get { return Config.Enabled && !_lowLevelCalibrationReleased; }
        }
        public bool HasStoredLowLevelCalibration
        {
            get { return TouchInput != null && TouchInput.HasStoredPanelCursorSurface; }
        }
        public bool IsLowLevelCalibrationProceeding
        {
            get { return _lowLevelCalibrationProceedRequested && HasStoredLowLevelCalibration && !_lowLevelCalibrationReleased; }
        }
        public bool IsLowLevelPanelMetricsCompatible
        {
            get
            {
                return Surface != null &&
                    Surface.SurfaceSize.X >= MinimumLowLevelPanelResolutionAxis &&
                    Surface.SurfaceSize.Y >= MinimumLowLevelPanelResolutionAxis;
            }
        }
        public bool SupportsInfoPanel
        {
            get
            {
                if (Surface == null)
                    return false;

                return UiLayout.BuildSurfaceProfile((int)Surface.SurfaceSize.X, (int)Surface.SurfaceSize.Y).AllowInfoPanel;
            }
        }
        public bool SupportsFullInfoPanel
        {
            get
            {
                if (Surface == null)
                    return false;

                return UiLayout.BuildSurfaceProfile((int)Surface.SurfaceSize.X, (int)Surface.SurfaceSize.Y).AllowFullInfoPanel;
            }
        }
        public bool IsOwnerFunctional
        {
            get
            {
                if (OwnerBlock == null)
                    return false;

                var functional = OwnerBlock as IMyFunctionalBlock;
                return functional == null || functional.IsFunctional;
            }
        }
        public int LowLevelCalibrationCountdownSeconds
        {
            get
            {
                if (!IsLowLevelCalibrationProceeding || _lowLevelCalibrationFoundTick < 0)
                    return 5;

                int remainingTicks = LowLevelCalibrationCountdownTicks - (_currentTick - _lowLevelCalibrationFoundTick);
                if (remainingTicks < 0)
                    remainingTicks = 0;
                return (remainingTicks + 59) / 60;
            }
        }
        public bool MouseControlEnabled
        {
            get { return Session != null ? Session.IsConstructMouseControlConfigured(ConstructId) : Config.MouseControl; }
        }
        public string MouseSensitivity
        {
            get { return Session != null ? Session.GetConstructMouseSensitivity(ConstructId, Config.MouseSensitivity) : GridSchematicsConfig.NormalizeMouseSensitivity(Config.MouseSensitivity); }
        }
        public bool IsViewportMotionActive
        {
            get { return _currentTick - _lastViewportMotionTick <= ViewportMotionQualityHoldTicks; }
        }
        public int LastPanelRenderTickDelta { get; private set; }
        public int LastPanelRenderIntervalTicks { get; private set; }

        string _lastCustomData = string.Empty;
        long _panelEntityId;
        int _lastProjectionRefreshTick = -600;
        int _lastTopologyUpdateTick = -600;
        int _lastRenderTick = -600;
        int _lastSharedCursorPublishTick = -600;
        bool _renderDirty = true;
        bool _touchRenderPending;
        bool _touchCursorRenderPending;
        bool _sharedCursorRenderPending;
        bool _lastCursorOnScreen;
        bool _lastTouchPressed;
        Vector2 _lastCursorPosition;
        string _lastHoverRegionId = string.Empty;
        string _lastTouchStatus = string.Empty;
        int _menuLeaveTick = -1;
        int _lastStartupScanAttemptTick = -StartupScanRetryTicks;
        int _lastSharedCursorRenderTick = -1;
        int _currentTick;
        int _lastCursorCalibrationClickTick = -CursorCalibrationClickCooldownTicks;
        int _lastMouseWheelValue;
        bool _hasLastMouseWheelValue;
        int _lowLevelCalibrationFoundTick = -1;
        int _lastLowLevelCalibrationCountdownSecond = -1;
        bool _lowLevelCalibrationReleased;
        bool _lowLevelCalibrationManualOverride;
        bool _lowLevelCalibrationProceedRequested;
        bool _isPanningRender;
        int _lastViewportMotionTick = -10000;
        Vector2 _lastPanCursorPosition;
        bool _pendingRenderClick;
        bool _renderClickMoved;
        Vector2 _renderClickStartPosition;
        string _renderClickStartRegionId = string.Empty;
        string _activeStyleSliderRegionId = string.Empty;
        bool _styleHueSliderDirty;
        bool _pendingClearSelectedOverlay;
        bool _selectedOverlayPanMoved;
        bool _activeScanIsStartup;
        ScanCache.IncrementalRaycastScanJob _activeScanJob;
        bool _hasScanCancelBackup;
        RawRaycastScanData _backupTopScan;
        RawRaycastScanData _backupSideScan;
        RawRaycastScanData _backupFrontScan;
        ShipGrid _backupTopGrid;
        ShipGrid _backupSideGrid;
        ShipGrid _backupFrontGrid;
        ConveyorTopology _backupConveyorNetwork;

        public ScanCache.IncrementalRaycastScanJob ActiveScanJob
        {
            get { return _activeScanJob; }
        }

        public GridSchematicsLcdApp(IMyTextPanel panel, GridSchematicsSession session, IMyTextSurface surface = null)
            : this(panel as IMyTerminalBlock, session, surface ?? panel as IMyTextSurface, panel)
        {
        }

        public GridSchematicsLcdApp(IMyTerminalBlock ownerBlock, GridSchematicsSession session, IMyTextSurface surface = null)
            : this(ownerBlock, session, surface, ownerBlock as IMyTextPanel)
        {
        }

        GridSchematicsLcdApp(IMyTerminalBlock ownerBlock, GridSchematicsSession session, IMyTextSurface surface, IMyTextPanel panel)
        {
            Panel = panel;
            OwnerBlock = ownerBlock;
            Surface = surface ?? ownerBlock as IMyTextSurface;
            Session = session;
            _panelEntityId = ownerBlock != null ? ownerBlock.EntityId : 0L;
            Config = new GridSchematicsConfig();
            Ui = new UiState();
            TouchInput = new TouchScreenApiAdapter();
            ApplyConfigToUi();
            UpdateConstructCache(session);
            RefreshConfig();
            EnsurePanelDefaults();
            InitializeTouchInput();
            TryRestorePersistedStartupScan();
        }

        public void Update(int tick)
        {
            _currentTick = tick;
            if (!IsOwnerFunctional)
                return;

            UpdateConstructCacheIfNeeded();
            if (RefreshConfig())
                _renderDirty = true;

            if (!Config.Enabled)
            {
                TouchInput.Dispose();
                return;
            }

            bool touchSceneChanged;
            bool touchCursorChanged;
            bool touchCursorMotionChanged;
            UpdateTouchInput(tick, out touchSceneChanged, out touchCursorChanged, out touchCursorMotionChanged);
            if (touchSceneChanged)
                _touchRenderPending = true;
            if (touchCursorChanged)
                _touchCursorRenderPending = true;
            bool cursorOnly = _touchCursorRenderPending && !_touchRenderPending && !_sharedCursorRenderPending && !_renderDirty;
            CursorOnlyRender = cursorOnly;
            if (UpdateLowLevelCalibrationState(tick))
                _touchRenderPending = true;
            bool startupScanChanged = EnsureStartupScan(tick);
            if (startupScanChanged)
                _renderDirty = true;

            bool scanJobChanged = UpdateActiveScanJob();
            if (scanJobChanged)
                _touchRenderPending = true;

            if (UpdateCalibrationState(tick))
                _touchRenderPending = true;

            bool projectionChanged = _activeScanJob == null && (cursorOnly ? false : UpdateTopologyIfNeeded(tick));
            if (projectionChanged)
                _renderDirty = true;

            bool sharedCursorChanged = false;
            if (sharedCursorChanged)
                _sharedCursorRenderPending = true;

            bool keepaliveRender = tick - _lastRenderTick >= 600;
            bool renderRequested = _renderDirty || _touchRenderPending || _sharedCursorRenderPending || keepaliveRender;
            bool cursorOnlyRequested = _touchCursorRenderPending && !_renderDirty && !_touchRenderPending && !_sharedCursorRenderPending;
            int renderInterval = cursorOnly
                ? (Ui.SegmentMode ? SegmentCursorOnlyRenderIntervalTicks : SegmentTouchRenderIntervalTicks)
                : PanelRenderIntervalTicks;
            bool renderIntervalElapsed = tick - _lastRenderTick >= renderInterval;
            bool shouldRender = renderRequested || cursorOnlyRequested;
            if (shouldRender && (renderIntervalElapsed || keepaliveRender))
            {
                LastPanelRenderTickDelta = _lastRenderTick < 0 ? 0 : tick - _lastRenderTick;
                LastPanelRenderIntervalTicks = renderInterval;
                RenderPanel(cursorOnly);
                _renderDirty = false;
                _touchRenderPending = false;
                _sharedCursorRenderPending = false;
                _touchCursorRenderPending = false;
                _lastRenderTick = tick;
                CaptureSharedCursorVisualState();
            }
        }

        bool EnsureStartupScan(int tick)
        {
            if (_activeScanJob != null)
                return false;

            if (ConstructCache == null || ConstructCache.StartupScanCompleted)
                return false;

            var grid = OwnerBlock.CubeGrid;
            if (grid == null)
                return false;

            if (tick < StartupScanWarmupTicks || tick - _lastStartupScanAttemptTick < StartupScanRetryTicks)
                return false;

            bool loaded = Session != null && Session.TryLoadPersistedScan(ConstructCache, grid);
            if (!loaded)
                ConstructCache.MarkStartupScanCompleted();

            _lastStartupScanAttemptTick = tick;
            _renderDirty = true;
            return loaded;
        }

        void TryRestorePersistedStartupScan()
        {
            if (Session == null || ConstructCache == null || ConstructCache.StartupScanCompleted || OwnerBlock == null)
                return;

            var grid = OwnerBlock.CubeGrid;
            if (grid == null)
                return;

            if (Session.TryLoadPersistedScan(ConstructCache, grid))
            {
                _lastStartupScanAttemptTick = 0;
                _renderDirty = true;
            }
        }

        bool HasReadyStartupScan()
        {
            if (ConstructCache == null)
                return false;

            return ConstructCache.HasReadyRaycastData(ScanView.Top) &&
                ConstructCache.HasReadyRaycastData(ScanView.Side) &&
                ConstructCache.HasReadyRaycastData(ScanView.Front);
        }

        bool HasReadyRaycastScan(ScanView view)
        {
            return ConstructCache != null && ConstructCache.HasReadyRaycastData(view);
        }

        void UpdateConstructCache(GridSchematicsSession session)
        {
            var grid = OwnerBlock.CubeGrid;
            if (grid == null)
                return;

            ConstructId = grid.EntityId;
            ConstructCache = session.GetConstructCache(ConstructId);
        }

        void UpdateConstructCacheIfNeeded()
        {
            var grid = OwnerBlock.CubeGrid;
            if (grid == null)
                return;

            if (grid.EntityId != ConstructId)
            {
                ConstructId = grid.EntityId;
            }
        }

        bool RefreshConfig()
        {
            var customData = OwnerBlock.CustomData ?? string.Empty;
            if (!string.Equals(customData, _lastCustomData, StringComparison.Ordinal))
            {
                _lastCustomData = customData;
                Config.Parse(customData);
                ApplyConfigToUi();
                return true;
            }

            return false;
        }

        void ApplyConfigToUi()
        {
            Ui.ShowDiscoveredBlocks = Config.ShowBlocks;
            Ui.ShowShipBorder = Config.ShowBorder;
            Ui.ShipBorderOpacity = Config.ShowBorder ? 0.5f : 0f;
            Ui.ShowDebugGrid = Config.ShowGrid;
            Ui.GridVisibilityLevel = Config.ShowGrid ? 1 : 0;
            Ui.ShowReferenceLines = Config.ShowReference;
            Ui.ShowHullScan = Config.ShowHullScan;
            Ui.HullScanBrightness = Config.ShowHullScan ? 1f : 0f;
            Ui.ShowAllConnections = Config.ShowAllConnections;
            Ui.ShowConveyorOverlay = Config.ShowConveyor || string.Equals(Config.OverlayMode, "CONVEYOR", StringComparison.OrdinalIgnoreCase) || string.Equals(Config.OverlayMode, "CONV", StringComparison.OrdinalIgnoreCase);
            Ui.FillBarsVisibilityLevel = GridSchematicsConfig.NormalizeFillBarsVisibilityLevel(Config.ShowFillBars ? Config.FillBarsVisibilityLevel : 0);
            Ui.BlurScanRender = Config.BlurScan;
            Ui.ShowInfoPanel = Config.ShowInfoPanel && SupportsInfoPanel;
            Ui.ActiveOverlay = ParseOverlayMode(Config.OverlayMode);
            Config.UiFont = GridSchematicsConfig.NormalizeUiFont(Config.UiFont);
            TouchInput.SetCursorCalibration(
                Config.HasCursorCalibration,
                Config.CursorCalibrationM11,
                Config.CursorCalibrationM12,
                Config.CursorCalibrationM13,
                Config.CursorCalibrationM21,
                Config.CursorCalibrationM22,
                Config.CursorCalibrationM23);
            Config.MouseSensitivity = GridSchematicsConfig.NormalizeMouseSensitivity(Config.MouseSensitivity);
            if (Session != null)
            {
                Session.NotePanelMouseSensitivitySetting(ConstructId, Config.MouseSensitivity);
                Session.NotePanelMouseControlSetting(ConstructId, Config.MouseControl);
            }

            Ui.CursorCalibrationRequired = !Config.HasCursorCalibration;
            if (Ui.CursorCalibrationRequired)
            {
                Ui.ShowCalibrationPrompt = true;
                Ui.CalibrationActive = true;
                Ui.CalibrationPromptDismissed = false;
                if (Ui.CalibrationCompletedTick >= 0)
                    Ui.CalibrationCompletedTick = -1;
            }
            else
            {
                Ui.CursorCalibrationRequired = false;
                Ui.ShowCalibrationPrompt = false;
                Ui.CalibrationActive = false;
                Ui.CalibrationPromptDismissed = true;
                Ui.CalibrationStep = 0;
                Ui.CalibrationCompletedTick = -1;
            }
        }

        void PersistPanelSettings()
        {
            Config.ShowBlocks = Ui.ShowDiscoveredBlocks;
            Config.ShowBorder = Ui.ShowShipBorder;
            Config.ShowGrid = Ui.ShowDebugGrid;
            Config.ShowReference = Ui.ShowReferenceLines;
            Config.ShowHullScan = Ui.ShowHullScan;
            Config.ShowAllConnections = Ui.ShowAllConnections;
            Config.ShowConveyor = Ui.ShowConveyorOverlay;
            Config.ShowFillBars = Ui.ShowFillBars;
            Config.FillBarsVisibilityLevel = GridSchematicsConfig.NormalizeFillBarsVisibilityLevel(Ui.FillBarsVisibilityLevel);
            Config.BlurScan = Ui.BlurScanRender;
            if (SupportsInfoPanel)
                Config.ShowInfoPanel = Ui.ShowInfoPanel;
            Config.OverlayMode = FormatOverlayMode(Ui.ActiveOverlay);

            var text = Config.ToIniText();
            _lastCustomData = text;
            OwnerBlock.CustomData = text;
        }

        public void SetMouseControlEnabledFromConstruct(bool enabled, bool persist)
        {
            Config.MouseControl = enabled;
            if (persist)
                PersistPanelSettings();
            _renderDirty = true;
        }

        public void SetMouseSensitivityFromConstruct(string sensitivity, bool persist)
        {
            Config.MouseSensitivity = GridSchematicsConfig.NormalizeMouseSensitivity(sensitivity);
            if (persist)
                PersistPanelSettings();
            _renderDirty = true;
        }

        string BuildSettingsGroupText(string group)
        {
            if (group == "UI")
            {
                return "UI_PALETTE=" + Config.UiPalette + "\n" +
                    "UI_COLOR_HUE=" + Config.UiHueShift + "\n" +
                    "UI_BRIGHTNESS=" + Config.UiBrightness.ToString("0.00") + "\n" +
                    "UI_SATURATION=" + Config.UiSaturation.ToString("0.00") + "\n" +
                    "UI_ALPHA=" + Config.UiAlpha.ToString("0.00") + "\n" +
                    "UI_FONT=" + GridSchematicsConfig.NormalizeUiFont(Config.UiFont) + "\n" +
                    "UI_HIGHLIGHT_COLOR_HUE=" + Config.UiAccentHueShift + "\n" +
                    "UI_ACCENT_BRIGHTNESS=" + Config.UiAccentBrightness.ToString("0.00") + "\n" +
                    "UI_ACCENT_SATURATION=" + Config.UiAccentSaturation.ToString("0.00") + "\n" +
                    "UI_PANEL_BRIGHTNESS=" + Config.UiPanelBrightness.ToString("0.00") + "\n" +
                    "UI_PANEL_ALPHA=" + Config.UiPanelAlpha.ToString("0.00") + "\n" +
                    "SCHEMATIC_MAIN_COLOR_HUE=" + Config.SchematicMainHue + "\n" +
                    "SCHEMATIC_SECONDARY_COLOR_HUE=" + Config.SchematicSecondaryHue + "\n" +
                    "CONVEYOR_COLOR_HUE=" + Config.ConveyorHue + "\n";
            }

            if (group == "RENDERING")
            {
                return "BLUR=" + Ui.BlurScanRender + "\n" +
                    "PERFORMANCE_MODE=" + Config.PerformanceMode + "\n" +
                    "HIGH_RES_SCANNING=" + Config.HighResScanning + "\n" +
                    "HULL_SCAN_COLOR=" + GridSchematicsConfig.NormalizeHullScanColorScale(Config.HullScanColorScale) + "\n";
            }

            if (group == "SCHEMATICS")
            {
                return "REFERENCE=" + Ui.ShowReferenceLines + "\n" +
                    "CONNECTIONS=" + Ui.ShowAllConnections + "\n" +
                    "BLOCKS_OCCLUDE_CONVEYORS=" + Config.BlocksOccludeConveyors + "\n" +
                    "SHOW_CONNECTED_NETWORKS=" + Config.ShowConnectedNetworks + "\n";
            }

            if (group == "DEBUG")
            {
                return "DEBUG=" + Config.ShowDebug + "\n" +
                    "PERF_STATS=" + Config.ShowPerfStats + "\n" +
                    "BLOCKS=" + Ui.ShowDiscoveredBlocks + "\n";
            }

            return string.Empty;
        }

        void PasteSettingsGroup(string group, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (group == "UI")
            {
                Config.UiPalette = GridSchematicsConfig.NormalizeUiPalette(ReadStringSetting(text, "UI_PALETTE", Config.UiPalette));
                Config.UiHueShift = ReadAbsoluteOrLegacyHueSetting(text, "UI_COLOR_HUE", "UI_HUE", GridSchematicsConfig.DefaultUiMainColorHue, Config.UiHueShift);
                Config.UiBrightness = ReadFloatSetting(text, "UI_BRIGHTNESS", Config.UiBrightness);
                Config.UiSaturation = ReadFloatSetting(text, "UI_SATURATION", Config.UiSaturation);
                Config.UiAlpha = ReadFloatSetting(text, "UI_ALPHA", Config.UiAlpha);
                Config.UiFont = GridSchematicsConfig.NormalizeUiFont(ReadStringSetting(text, "UI_FONT", Config.UiFont));
                Config.UiAccentHueShift = ReadAbsoluteOrLegacyHueSetting(text, "UI_HIGHLIGHT_COLOR_HUE", "UI_ACCENT_HUE", GridSchematicsConfig.DefaultUiHighlightColorHue, Config.UiAccentHueShift);
                Config.UiAccentBrightness = ReadFloatSetting(text, "UI_ACCENT_BRIGHTNESS", Config.UiAccentBrightness);
                Config.UiAccentSaturation = ReadFloatSetting(text, "UI_ACCENT_SATURATION", Config.UiAccentSaturation);
                Config.UiPanelBrightness = ReadFloatSetting(text, "UI_PANEL_BRIGHTNESS", Config.UiPanelBrightness);
                Config.UiPanelAlpha = ReadFloatSetting(text, "UI_PANEL_ALPHA", Config.UiPanelAlpha);
                Config.SchematicMainHue = ReadAbsoluteOrLegacyHueSetting(text, "SCHEMATIC_MAIN_COLOR_HUE", "SCHEMATIC_MAIN_HUE", GridSchematicsConfig.DefaultSchematicMainColorHue, Config.SchematicMainHue);
                Config.SchematicSecondaryHue = ReadAbsoluteOrLegacyHueSetting(text, "SCHEMATIC_SECONDARY_COLOR_HUE", "SCHEMATIC_SECONDARY_HUE", GridSchematicsConfig.DefaultSchematicSecondaryColorHue, Config.SchematicSecondaryHue);
                Config.ConveyorHue = ReadAbsoluteOrLegacyHueSetting(text, "CONVEYOR_COLOR_HUE", "CONVEYOR_HUE", GridSchematicsConfig.DefaultConveyorColorHue, Config.ConveyorHue);
            }
            else if (group == "RENDERING")
            {
                Ui.BlurScanRender = ReadBoolSetting(text, "BLUR", Ui.BlurScanRender);
                Config.PerformanceMode = ReadBoolSetting(text, "PERFORMANCE_MODE", Config.PerformanceMode);
                Config.HighResScanning = ReadBoolSetting(text, "HIGH_RES_SCANNING", Config.HighResScanning);
                Config.HullScanColorScale = GridSchematicsConfig.NormalizeHullScanColorScale(ReadStringSetting(text, "HULL_SCAN_COLOR", Config.HullScanColorScale));
            }
            else if (group == "SCHEMATICS")
            {
                Ui.ShowReferenceLines = ReadBoolSetting(text, "REFERENCE", Ui.ShowReferenceLines);
                Ui.ShowAllConnections = ReadBoolSetting(text, "CONNECTIONS", Ui.ShowAllConnections);
                Config.BlocksOccludeConveyors = ReadBoolSetting(text, "BLOCKS_OCCLUDE_CONVEYORS", Config.BlocksOccludeConveyors);
                Config.ShowConnectedNetworks = ReadBoolSetting(text, "SHOW_CONNECTED_NETWORKS", Config.ShowConnectedNetworks);
            }
            else if (group == "DEBUG")
            {
                Config.ShowDebug = ReadBoolSetting(text, "DEBUG", Config.ShowDebug);
                Config.ShowPerfStats = ReadBoolSetting(text, "PERF_STATS", Config.ShowPerfStats);
                Ui.ShowDiscoveredBlocks = ReadBoolSetting(text, "BLOCKS", Ui.ShowDiscoveredBlocks);
            }

            PersistPanelSettings();
            _renderDirty = true;
        }

        string ReadStringSetting(string text, string key, string fallback)
        {
            string value;
            return TryReadSetting(text, key, out value) ? value : fallback;
        }

        int ReadIntSetting(string text, string key, int fallback)
        {
            string value;
            int parsed;
            if (TryReadSetting(text, key, out value) && int.TryParse(value, out parsed))
                return parsed;
            return fallback;
        }

        int ReadAbsoluteOrLegacyHueSetting(string text, string absoluteKey, string legacyKey, int defaultHue, int fallback)
        {
            string value;
            int parsed;
            if (TryReadSetting(text, absoluteKey, out value) && int.TryParse(value, out parsed))
                return NormalizeHue(parsed);
            if (TryReadSetting(text, legacyKey, out value) && int.TryParse(value, out parsed))
                return NormalizeHue(defaultHue + parsed);
            return fallback;
        }

        static int NormalizeHue(int hue)
        {
            int normalized = hue % 360;
            if (normalized < 0)
                normalized += 360;
            return normalized;
        }

        float ReadFloatSetting(string text, string key, float fallback)
        {
            string value;
            float parsed;
            if (TryReadSetting(text, key, out value) && float.TryParse(value, out parsed))
                return parsed;
            return fallback;
        }

        bool ReadBoolSetting(string text, string key, bool fallback)
        {
            string value;
            bool parsed;
            if (TryReadSetting(text, key, out value) && bool.TryParse(value, out parsed))
                return parsed;
            return fallback;
        }

        bool TryReadSetting(string text, string key, out string value)
        {
            value = string.Empty;
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(key))
                return false;

            var lines = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                int separator = line.IndexOf('=');
                if (separator <= 0)
                    continue;

                var lineKey = line.Substring(0, separator).Trim();
                if (!string.Equals(lineKey, key, StringComparison.OrdinalIgnoreCase))
                    continue;

                value = line.Substring(separator + 1).Trim();
                return true;
            }

            return false;
        }

        OverlayMode ParseOverlayMode(string value)
        {
            if (string.IsNullOrEmpty(value))
                return OverlayMode.None;

            switch (value.ToUpperInvariant())
            {
                case "CARGO":
                    return OverlayMode.Cargo;
                case "ENGINES":
                    return OverlayMode.Engines;
                case "POWER":
                    return OverlayMode.Power;
                case "OXYGEN":
                    return OverlayMode.Oxygen;
                default:
                    return OverlayMode.None;
            }
        }

        string FormatOverlayMode(OverlayMode overlay)
        {
            switch (overlay)
            {
                case OverlayMode.Cargo:
                    return "CARGO";
                case OverlayMode.Engines:
                    return "ENGINES";
                case OverlayMode.Power:
                    return "POWER";
                case OverlayMode.Oxygen:
                    return "OXYGEN";
                default:
                    return "NONE";
            }
        }

        void EnsurePanelDefaults()
        {
            if (string.IsNullOrWhiteSpace(OwnerBlock.CustomData))
            {
                OwnerBlock.CustomData = Config.ToIniText();
            }

            try
            {
                var surface = Surface;
                if (surface != null)
                {
                    surface.ContentType = ContentType.SCRIPT;
                    surface.ScriptForegroundColor = new Color(255, 255, 255);
                    surface.ScriptBackgroundColor = new Color(0, 0, 0);
                }
            }
            catch
            {
                // Some game environments may not expose these panel settings safely.
            }
        }

        void InitializeTouchInput()
        {
            TouchInput.Initialize(OwnerBlock, Surface);
            if (Session != null)
                Session.TryApplyStoredPanelCursorCalibration(TouchInput);
        }

        bool UpdateActiveScanJob()
        {
            if (_activeScanJob == null)
                return false;

            bool changed = _activeScanJob.Advance(IncrementalScanSamplesPerTick, IncrementalCompositionSamplesPerTick);
            if (_activeScanJob.IsComplete)
            {
                if (_activeScanIsStartup && HasReadyStartupScan())
                    ConstructCache.MarkStartupScanCompleted();
                if (HasReadyStartupScan() && Session != null)
                    Session.SavePersistedScan(ConstructCache, OwnerBlock.CubeGrid);

                _activeScanJob = null;
                _activeScanIsStartup = false;
                _hasScanCancelBackup = false;
                _renderDirty = true;
                return true;
            }

            return changed;
        }

        void StartIncrementalRaycastScan(IMyCubeGrid grid, int resolution, bool startup, int tick)
        {
            if (grid == null || ConstructCache == null)
                return;

            if (_activeScanJob != null && !_activeScanJob.IsComplete)
                return;

            CaptureScanCancelBackup();
            _activeScanJob = ConstructCache.BeginIncrementalRaycastScans(grid, resolution);
            _activeScanIsStartup = startup;
            ResetViewportCamera();
            _lastStartupScanAttemptTick = tick;
            _renderDirty = true;
        }

        void CancelActiveScan()
        {
            if (_activeScanJob == null)
                return;

            _activeScanJob = null;
            _activeScanIsStartup = false;
            RestoreScanCancelBackup();
            _touchRenderPending = true;
            _renderDirty = true;
        }

        void CaptureScanCancelBackup()
        {
            if (ConstructCache == null)
                return;

            _backupTopScan = ConstructCache.GetRaycastData(ScanView.Top);
            _backupSideScan = ConstructCache.GetRaycastData(ScanView.Side);
            _backupFrontScan = ConstructCache.GetRaycastData(ScanView.Front);
            ConstructCache.ProjectedGrids.TryGetValue(ScanView.Top, out _backupTopGrid);
            ConstructCache.ProjectedGrids.TryGetValue(ScanView.Side, out _backupSideGrid);
            ConstructCache.ProjectedGrids.TryGetValue(ScanView.Front, out _backupFrontGrid);
            _backupConveyorNetwork = ConstructCache.ConveyorNetwork;
            _hasScanCancelBackup = true;
        }

        void RestoreScanCancelBackup()
        {
            if (!_hasScanCancelBackup || ConstructCache == null)
                return;

            RestoreRaycastData(ScanView.Top, _backupTopScan);
            RestoreRaycastData(ScanView.Side, _backupSideScan);
            RestoreRaycastData(ScanView.Front, _backupFrontScan);
            RestoreProjectedGrid(ScanView.Top, _backupTopGrid);
            RestoreProjectedGrid(ScanView.Side, _backupSideGrid);
            RestoreProjectedGrid(ScanView.Front, _backupFrontGrid);
            ConstructCache.ConveyorNetwork = _backupConveyorNetwork;
            _hasScanCancelBackup = false;
        }

        void RestoreRaycastData(ScanView view, RawRaycastScanData data)
        {
            if (data == null)
                ConstructCache.RaycastData.Remove(view);
            else
                ConstructCache.RaycastData[view] = data;
        }

        void RestoreProjectedGrid(ScanView view, ShipGrid grid)
        {
            if (grid == null)
                ConstructCache.ProjectedGrids.Remove(view);
            else
                ConstructCache.ProjectedGrids[view] = grid;
        }

        bool UpdateCalibrationState(int tick)
        {
            bool changed = false;
            if (Ui.CalibrationActive && Ui.CalibrationCompletedTick >= 0)
            {
                int remainingTicks = CursorCalibrationConfirmTicks - (tick - Ui.CalibrationCompletedTick);
                if (remainingTicks <= 0)
                {
                    StartCursorRecalibration();
                    return true;
                }

                int countdown = (remainingTicks + 59) / 60;
                if (countdown != Ui.CalibrationRestartCountdownSeconds)
                {
                    Ui.CalibrationRestartCountdownSeconds = countdown;
                    changed = true;
                }

                if (changed)
                    _renderDirty = true;
                return changed;
            }

            if (!Config.HasCursorCalibration)
            {
                Ui.CursorCalibrationRequired = true;
                if (!Ui.ShowCalibrationPrompt || !Ui.CalibrationActive)
                {
                    Ui.ShowCalibrationPrompt = true;
                    Ui.CalibrationActive = true;
                    Ui.CalibrationPromptDismissed = false;
                    Ui.CalibrationCompletedTick = -1;
                    Ui.CalibrationRestartCountdownSeconds = 5;
                    changed = true;
                }

                if (changed)
                    _renderDirty = true;
                return changed;
            }

            if (!Ui.CalibrationPromptDismissed && !Ui.CalibrationActive && !Ui.ShowCalibrationPrompt &&
                tick > StartupScanWarmupTicks + 120 && TouchInput != null && TouchInput.IsAvailable &&
                !TouchInput.IsCursorOnScreen && string.Equals(TouchInput.StatusText, "Touch: calibrate/aim", StringComparison.Ordinal))
            {
                Ui.ShowCalibrationPrompt = true;
                changed = true;
            }

            if (changed)
                _renderDirty = true;
            return changed;
        }

        void StartCalibration()
        {
            Ui.ShowCalibrationPrompt = true;
            Ui.CalibrationActive = true;
            Ui.CalibrationPromptDismissed = false;
            Ui.CalibrationStep = 0;
            Ui.CalibrationCompletedTick = -1;
            Ui.CalibrationRestartCountdownSeconds = 5;
            Ui.CursorCalibrationRequired = !Config.HasCursorCalibration;
            _lastCursorCalibrationClickTick = _currentTick;
            _renderDirty = true;
        }

        void CloseCalibrationPrompt()
        {
            if (Ui.CursorCalibrationRequired)
            {
                StartCalibration();
                return;
            }

            if (Ui.CalibrationCompletedTick >= 0)
                PersistPanelSettings();

            Ui.ShowCalibrationPrompt = false;
            Ui.CalibrationActive = false;
            Ui.CalibrationPromptDismissed = true;
            Ui.CalibrationStep = 0;
            Ui.CalibrationCompletedTick = -1;
            Ui.CalibrationRestartCountdownSeconds = 5;
            _renderDirty = true;
        }

        public void StartCursorRecalibration()
        {
            Config.HasCursorCalibration = false;
            Config.CursorCalibrationM11 = 1f;
            Config.CursorCalibrationM12 = 0f;
            Config.CursorCalibrationM13 = 0f;
            Config.CursorCalibrationM21 = 0f;
            Config.CursorCalibrationM22 = 1f;
            Config.CursorCalibrationM23 = 0f;
            TouchInput.SetCursorCalibration(false, 1f, 0f, 0f, 0f, 1f, 0f);
            Ui.CursorCalibrationRequired = true;
            Ui.ShowCalibrationPrompt = true;
            Ui.CalibrationActive = true;
            Ui.CalibrationPromptDismissed = false;
            Ui.CalibrationStep = 0;
            Ui.CalibrationCompletedTick = -1;
            Ui.CalibrationRestartCountdownSeconds = 5;
            Ui.ActiveMenu = MenuPanel.None;
            _lastCursorCalibrationClickTick = _currentTick;
            PersistPanelSettings();
            _renderDirty = true;
        }

        void ToggleConstructMouseControl()
        {
            bool enabled = !MouseControlEnabled;
            if (Session != null)
                Session.SetConstructMouseControl(ConstructId, enabled);
            else
                SetMouseControlEnabledFromConstruct(enabled, true);

            _renderDirty = true;
        }

        void CycleConstructMouseSensitivity()
        {
            Config.MouseSensitivity = MouseSensitivity;
            Config.CycleMouseSensitivity();
            if (Session != null)
                Session.SetConstructMouseSensitivity(ConstructId, Config.MouseSensitivity);
            else
                SetMouseSensitivityFromConstruct(Config.MouseSensitivity, true);

            _renderDirty = true;
        }

        void AdvanceCalibrationPoint()
        {
            if (!Ui.CalibrationActive || Ui.CalibrationCompletedTick >= 0)
                return;

            CaptureCalibrationPoint();
            _renderDirty = true;
        }

        bool CaptureCalibrationPoint()
        {
            if (!Ui.CalibrationActive || Ui.CalibrationCompletedTick >= 0 || TouchInput == null || !TouchInput.IsCursorOnScreen || Surface == null)
                return false;

            if (_currentTick - _lastCursorCalibrationClickTick < CursorCalibrationClickCooldownTicks)
                return false;

            int step = Ui.CalibrationStep;
            if (step < 0 || step >= 3)
                return false;

            var calibrationZone = UiLayout.BuildCalibrationTargetZone((int)Surface.SurfaceSize.X, (int)Surface.SurfaceSize.Y);
            Ui.CalibrationRawPoints[step] = TouchInput.RawCursorPosition;
            _lastCursorCalibrationClickTick = _currentTick;
            Ui.CalibrationStep++;
            if (Ui.CalibrationStep >= 3)
            {
                if (TryCompleteCursorCalibration(calibrationZone))
                    return true;

                Ui.CalibrationStep = 0;
            }

            return true;
        }

        bool UpdateLowLevelCalibrationState(int tick)
        {
            if (_lowLevelCalibrationReleased)
                return false;

            bool changed = false;
            bool metricsCompatible = IsLowLevelPanelMetricsCompatible;
            if (TouchInput != null && !TouchInput.HasStoredPanelCursorSurface && Session != null && !_lowLevelCalibrationManualOverride)
            {
                if (Session.TryApplyStoredPanelCursorCalibration(TouchInput))
                    changed = true;
            }

            if (!metricsCompatible)
            {
                if (_lowLevelCalibrationProceedRequested || _lowLevelCalibrationFoundTick >= 0 || _lastLowLevelCalibrationCountdownSecond >= 0)
                {
                    _lowLevelCalibrationProceedRequested = false;
                    _lowLevelCalibrationFoundTick = -1;
                    _lastLowLevelCalibrationCountdownSecond = -1;
                    changed = true;
                }

                if (changed)
                    _renderDirty = true;
                return changed;
            }

            if (HasStoredLowLevelCalibration && !_lowLevelCalibrationManualOverride && !_lowLevelCalibrationProceedRequested)
            {
                _lowLevelCalibrationProceedRequested = true;
                changed = true;
            }

            if (HasStoredLowLevelCalibration && _lowLevelCalibrationProceedRequested)
            {
                if (_lowLevelCalibrationFoundTick < 0)
                {
                    _lowLevelCalibrationFoundTick = tick;
                    _lastLowLevelCalibrationCountdownSecond = -1;
                    changed = true;
                }

                int countdown = LowLevelCalibrationCountdownSeconds;
                if (countdown != _lastLowLevelCalibrationCountdownSecond)
                {
                    _lastLowLevelCalibrationCountdownSecond = countdown;
                    changed = true;
                }

                if (tick - _lowLevelCalibrationFoundTick >= LowLevelCalibrationCountdownTicks)
                {
                    _lowLevelCalibrationReleased = true;
                    changed = true;
                }
            }
            else
            {
                if (_lowLevelCalibrationFoundTick >= 0 || _lastLowLevelCalibrationCountdownSecond >= 0)
                {
                    _lowLevelCalibrationFoundTick = -1;
                    _lastLowLevelCalibrationCountdownSecond = -1;
                    changed = true;
                }
            }

            if (changed)
                _renderDirty = true;
            return changed;
        }

        public void ResetLowLevelCalibrationForManual()
        {
            if (TouchInput != null)
                TouchInput.ResetPanelCursorSurfaceCalibration();
            _lowLevelCalibrationReleased = false;
            _lowLevelCalibrationManualOverride = true;
            _lowLevelCalibrationProceedRequested = false;
            _lowLevelCalibrationFoundTick = -1;
            _lastLowLevelCalibrationCountdownSecond = -1;
            _renderDirty = true;
            _touchRenderPending = true;
        }

        public void NoteManualLowLevelCalibrationApplied()
        {
            _lowLevelCalibrationManualOverride = true;
            _lowLevelCalibrationReleased = false;
            _lowLevelCalibrationProceedRequested = false;
            _lowLevelCalibrationFoundTick = -1;
            _lastLowLevelCalibrationCountdownSecond = -1;
            _renderDirty = true;
            _touchRenderPending = true;
        }

        public void ProceedToCursorCalibrationAfterLowLevel()
        {
            if (!HasStoredLowLevelCalibration || !IsLowLevelPanelMetricsCompatible)
                return;

            _lowLevelCalibrationManualOverride = false;
            _lowLevelCalibrationProceedRequested = true;
            _lowLevelCalibrationReleased = false;
            _lowLevelCalibrationFoundTick = -1;
            _lastLowLevelCalibrationCountdownSecond = -1;
            _renderDirty = true;
            _touchRenderPending = true;
        }

        bool TryCompleteCursorCalibration(ScreenZone calibrationZone)
        {
            Vector2 raw0 = Ui.CalibrationRawPoints[0];
            Vector2 raw1 = Ui.CalibrationRawPoints[1];
            Vector2 raw2 = Ui.CalibrationRawPoints[2];
            Vector2 target0 = UiLayout.GetCalibrationPoint(calibrationZone, 0);
            Vector2 target1 = UiLayout.GetCalibrationPoint(calibrationZone, 1);
            Vector2 target2 = UiLayout.GetCalibrationPoint(calibrationZone, 2);

            float m11;
            float m12;
            float m13;
            float m21;
            float m22;
            float m23;
            if (!TrySolveCursorCalibration(raw0, raw1, raw2, target0, target1, target2, out m11, out m12, out m13, out m21, out m22, out m23))
                return false;

            Config.SetCursorCalibration(m11, m12, m13, m21, m22, m23);
            TouchInput.SetCursorCalibration(true, m11, m12, m13, m21, m22, m23);
            Ui.CursorCalibrationRequired = false;
            Ui.ShowCalibrationPrompt = true;
            Ui.CalibrationActive = true;
            Ui.CalibrationPromptDismissed = false;
            Ui.CalibrationStep = 3;
            Ui.CalibrationCompletedTick = _currentTick;
            Ui.CalibrationRestartCountdownSeconds = 5;
            return true;
        }

        static bool TrySolveCursorCalibration(Vector2 raw0, Vector2 raw1, Vector2 raw2, Vector2 target0, Vector2 target1, Vector2 target2, out float m11, out float m12, out float m13, out float m21, out float m22, out float m23)
        {
            m11 = 1f;
            m12 = 0f;
            m13 = 0f;
            m21 = 0f;
            m22 = 1f;
            m23 = 0f;

            double x0 = raw0.X;
            double y0 = raw0.Y;
            double x1 = raw1.X;
            double y1 = raw1.Y;
            double x2 = raw2.X;
            double y2 = raw2.Y;
            double determinant = x0 * (y1 - y2) + x1 * (y2 - y0) + x2 * (y0 - y1);
            if (Math.Abs(determinant) < 8.0)
                return false;

            m11 = (float)((target0.X * (y1 - y2) + target1.X * (y2 - y0) + target2.X * (y0 - y1)) / determinant);
            m12 = (float)((target0.X * (x2 - x1) + target1.X * (x0 - x2) + target2.X * (x1 - x0)) / determinant);
            m13 = (float)((target0.X * (x1 * y2 - x2 * y1) + target1.X * (x2 * y0 - x0 * y2) + target2.X * (x0 * y1 - x1 * y0)) / determinant);
            m21 = (float)((target0.Y * (y1 - y2) + target1.Y * (y2 - y0) + target2.Y * (y0 - y1)) / determinant);
            m22 = (float)((target0.Y * (x2 - x1) + target1.Y * (x0 - x2) + target2.Y * (x1 - x0)) / determinant);
            m23 = (float)((target0.Y * (x1 * y2 - x2 * y1) + target1.Y * (x2 * y0 - x0 * y2) + target2.Y * (x0 * y1 - x1 * y0)) / determinant);

            return !float.IsNaN(m11) && !float.IsInfinity(m11) &&
                !float.IsNaN(m12) && !float.IsInfinity(m12) &&
                !float.IsNaN(m13) && !float.IsInfinity(m13) &&
                !float.IsNaN(m21) && !float.IsInfinity(m21) &&
                !float.IsNaN(m22) && !float.IsInfinity(m22) &&
                !float.IsNaN(m23) && !float.IsInfinity(m23);
        }

        void UpdateTouchInput(int tick, out bool sceneChanged, out bool cursorChanged, out bool cursorMotionChanged)
        {
            sceneChanged = false;
            cursorChanged = false;
            cursorMotionChanged = false;
            RegisterViewButtonHitRegions();
            UpdateMouseControlInputState();
            TouchInput.ProcessInput();
            bool changed = HasTouchVisualChanged(out sceneChanged, out cursorChanged, out cursorMotionChanged);
            bool actionChanged = false;
            if (Ui.CursorCalibrationRequired && Ui.CalibrationActive && TouchInput.JustPressed)
            {
                if (CaptureCalibrationPoint())
                {
                    changed = true;
                    actionChanged = true;
                    sceneChanged = true;
                    _renderDirty = true;
                }

                CaptureTouchVisualState();
                return;
            }

            if (!string.IsNullOrEmpty(TouchInput.LastHitRegionId) && !IsOverlayBlockRegion(TouchInput.LastHitRegionId))
            {
                changed = true;
                actionChanged = true;
                _renderDirty = true;
                _isPanningRender = false;
                _pendingClearSelectedOverlay = false;
                _selectedOverlayPanMoved = false;
                Ui.HitRegions.Clear();
                Ui.HitRegions.Add(new HitRegion(0, 0, 1, 1, TouchInput.LastHitRegionId, "Touched region"));

                if (IsUiBlockerRegion(TouchInput.LastHitRegionId))
                {
                }
                else
                {
                    PlayMenuPressSound();
                    HandleMenuPress(TouchInput.LastHitRegionId);
                }
            }
            else if (TouchInput.JustPressed && Ui.SelectedOverlayBlockId != null)
            {
                if (IsCursorInRenderArea() && string.IsNullOrEmpty(TouchInput.HoverRegionId))
                {
                    _pendingClearSelectedOverlay = true;
                    _selectedOverlayPanMoved = false;
                }
            }

            if (UpdateStyleHueSliderDrag())
            {
                changed = true;
                actionChanged = true;
                _renderDirty = true;
            }
            else if (UpdateStyleSliderWheel())
            {
                changed = true;
                actionChanged = true;
                _renderDirty = true;
            }
            else if (TouchInput.JustReleased && _styleHueSliderDirty)
            {
                _styleHueSliderDirty = false;
                PersistPanelSettings();
            }

            if (TryResetSettingsItemFromSecondaryClick())
            {
                changed = true;
                actionChanged = true;
                _renderDirty = true;
            }

            if (UpdateRenderClickSelection())
            {
                changed = true;
                actionChanged = true;
                _renderDirty = true;
            }

            if (UpdateInfoPanelStackHeaderScroll())
            {
                changed = true;
                actionChanged = true;
                _renderDirty = true;
            }

            if (TouchInput.SecondaryJustPressed && Ui.SelectedOverlayBlockId != null && !IsCursorInActiveMenuField())
            {
                if (TryToggleSelectedOverlayBlock())
                {
                    PlayMenuPressSound();
                    changed = true;
                    actionChanged = true;
                    _renderDirty = true;
                }
            }

            if (Ui.SelectedOverlayBlockId != null)
            {
                if (UpdateViewportPan())
                {
                    if (_pendingClearSelectedOverlay)
                        _selectedOverlayPanMoved = true;
                    changed = true;
                    actionChanged = true;
                    _renderDirty = true;
                }

                if (_pendingClearSelectedOverlay && !TouchInput.IsPressed)
                {
                    if (!_selectedOverlayPanMoved)
                    {
                        Ui.SelectedOverlayBlockId = null;
                        Ui.SelectedOverlayLineIndex = 0;
                        changed = true;
                        actionChanged = true;
                        _renderDirty = true;
                    }

                    _pendingClearSelectedOverlay = false;
                    _selectedOverlayPanMoved = false;
                }
            }
            else
            {
                if (UpdateViewportWheel())
                {
                    changed = true;
                    actionChanged = true;
                    _renderDirty = true;
                }

                if (UpdateViewportPan())
                {
                    changed = true;
                    actionChanged = true;
                    _renderDirty = true;
                }
            }

            if (ShouldCloseActiveMenu(tick))
            {
                Ui.ActiveMenu = MenuPanel.None;
                ClearStyleSliderDrag();
                _menuLeaveTick = -1;
                changed = true;
                actionChanged = true;
            }

            if (actionChanged)
                sceneChanged = true;

            CaptureTouchVisualState();
        }

        void UpdateMouseControlInputState()
        {
            if (TouchInput == null)
                return;

            if (IsCursorCalibrationBlocking)
            {
                TouchInput.SetMouseControlState(false, false, Vector2.Zero);
                return;
            }

            if (Session == null || !MouseControlEnabled || !Session.IsConstructMouseControlAvailable(ConstructId))
            {
                TouchInput.SetMouseControlState(false, false, Vector2.Zero);
                return;
            }

            Vector2 rawPosition;
            int surfaceIndex = TouchInput != null ? TouchInput.GetSurfaceIndex() : -1;
            bool hasPosition = Session.TryGetMouseControlCursorForPanel(ConstructId, _panelEntityId, surfaceIndex, out rawPosition);
            TouchInput.SetMouseControlState(true, hasPosition, rawPosition);
        }

        void HandleMenuPress(string regionId)
        {
            if (HandleInfoPanelBlockTabPress(regionId))
                return;

            switch (regionId)
            {
                case UiLayout.ToggleChromeId:
                    Ui.ChromeHidden = !Ui.ChromeHidden;
                    Ui.ActiveMenu = MenuPanel.None;
                    _renderDirty = true;
                    break;
                case UiLayout.MenuViewId:
                    ToggleMenu(MenuPanel.View);
                    _renderDirty = true;
                    break;
                case UiLayout.MenuLayersId:
                    ToggleMenu(MenuPanel.Layers);
                    _renderDirty = true;
                    break;
                case UiLayout.MenuScanId:
                    if (!SupportsInfoPanel)
                    {
                        Ui.ShowInfoPanel = false;
                        Ui.InfoPanelMode = InfoPanelMode.Scan;
                        Ui.ActiveMenu = MenuPanel.None;
                        _renderDirty = true;
                        break;
                    }

                    if (Ui.ShowInfoPanel && Ui.InfoPanelMode == InfoPanelMode.Scan)
                    {
                        Ui.ShowInfoPanel = false;
                    }
                    else
                    {
                        Ui.ShowInfoPanel = true;
                        Ui.InfoPanelMode = InfoPanelMode.Scan;
                    }
                    Ui.ActiveMenu = MenuPanel.None;
                    ResetViewportCamera();
                    PersistPanelSettings();
                    break;
                case UiLayout.ToggleInfoPanelId:
                    if (!SupportsInfoPanel)
                    {
                        Ui.ShowInfoPanel = false;
                        Ui.ActiveMenu = MenuPanel.None;
                        _renderDirty = true;
                        break;
                    }

                    if (Ui.ShowInfoPanel && Ui.InfoPanelMode == InfoPanelMode.Systems)
                    {
                        Ui.ShowInfoPanel = false;
                    }
                    else
                    {
                        Ui.ShowInfoPanel = true;
                        Ui.InfoPanelMode = InfoPanelMode.Systems;
                    }
                    Ui.ActiveMenu = MenuPanel.None;
                    ResetViewportCamera();
                    PersistPanelSettings();
                    break;
                case UiLayout.MenuSettingsId:
                    ToggleMenu(MenuPanel.Settings);
                    _renderDirty = true;
                    break;
                case UiLayout.MenuToolsId:
                    ToggleMenu(MenuPanel.Tools);
                    _renderDirty = true;
                    break;
                case UiLayout.SettingsStyleHeaderId:
                    ToggleSettingsCategory(UiLayout.SettingsCategoryStyle);
                    break;
                case UiLayout.SettingsUiHeaderId:
                    ToggleSettingsCategory(UiLayout.SettingsCategoryUserInterface);
                    break;
                case UiLayout.SettingsRenderingHeaderId:
                    ToggleSettingsCategory(UiLayout.SettingsCategoryRendering);
                    break;
                case UiLayout.SettingsSchematicsHeaderId:
                    ToggleSettingsCategory(UiLayout.SettingsCategorySchematics);
                    break;
                case UiLayout.SettingsDebugHeaderId:
                    ToggleSettingsCategory(UiLayout.SettingsCategoryDebug);
                    break;
                case UiLayout.SettingsPanelDataHeaderId:
                    ToggleSettingsCategory(UiLayout.SettingsCategoryPanelData);
                    break;
                case UiLayout.SegmentModeId:
                    Ui.SegmentMode = !Ui.SegmentMode;
                    Ui.ActiveMenu = MenuPanel.None;
                    Ui.SelectedOverlayBlockId = null;
                    Ui.SelectedOverlayLineIndex = 0;
                    ResetViewportCamera();
                    if (Ui.SegmentMode)
                    {
                    Ui.SegmentFrontGrid = null;
                    Ui.SegmentLeftGrid = null;
                    Ui.SegmentTopGrid = null;
                        Ui.LastSegmentProjectionRefreshTick = -SegmentProjectionRefreshTicks;
                        Ui.SegmentProjectionRefreshStep = 0;
                        UpdateSegmentProjectionCache(_currentTick, true);
                    }
                    _renderDirty = true;
                    break;
                case UiLayout.ViewTopId:
                    Config.View = "TOP";
                    Ui.SegmentMode = false;
                    Ui.ActiveMenu = MenuPanel.None;
                    ResetViewportCamera();
                    PersistPanelSettings();
                    break;
                case UiLayout.ViewLeftId:
                    Config.View = "LEFT";
                    Ui.SegmentMode = false;
                    Ui.ActiveMenu = MenuPanel.None;
                    ResetViewportCamera();
                    PersistPanelSettings();
                    break;
                case UiLayout.ViewFrontId:
                    Config.View = "FRONT";
                    Ui.SegmentMode = false;
                    Ui.ActiveMenu = MenuPanel.None;
                    ResetViewportCamera();
                    PersistPanelSettings();
                    break;
                case UiLayout.RotateCcwId:
                    if (Config.AllowGridRotation)
                    {
                        Config.RotateCurrentView(-1);
                        PersistPanelSettings();
                    }
                    break;
                case UiLayout.RotateCwId:
                    if (Config.AllowGridRotation)
                    {
                        Config.RotateCurrentView(1);
                        PersistPanelSettings();
                    }
                    break;
                case UiLayout.ToggleBlocksId:
                    Ui.ShowDiscoveredBlocks = !Ui.ShowDiscoveredBlocks;
                    PersistPanelSettings();
                    break;
                case UiLayout.ToggleBorderId:
                    CycleShipBorderVisibility();
                    PersistPanelSettings();
                    break;
                case UiLayout.ToggleHullScanId:
                    CycleHullScanVisibility();
                    PersistPanelSettings();
                    break;
                case UiLayout.ToggleGridId:
                    CycleGridVisibility();
                    PersistPanelSettings();
                    break;
                case UiLayout.ToggleReferenceId:
                    Ui.ShowReferenceLines = !Ui.ShowReferenceLines;
                    PersistPanelSettings();
                    break;
                case UiLayout.ToggleAllConnectionsId:
                    Ui.ShowAllConnections = !Ui.ShowAllConnections;
                    PersistPanelSettings();
                    break;
                case UiLayout.ToggleBlocksOccludeConveyorsId:
                    Config.BlocksOccludeConveyors = !Config.BlocksOccludeConveyors;
                    PersistPanelSettings();
                    break;
                case UiLayout.ToggleConnectedNetworksId:
                    Config.ShowConnectedNetworks = !Config.ShowConnectedNetworks;
                    PersistPanelSettings();
                    break;
                case UiLayout.ToggleBlurId:
                    Ui.BlurScanRender = !Ui.BlurScanRender;
                    PersistPanelSettings();
                    break;
                case UiLayout.ToggleConveyorId:
                    Ui.ShowConveyorOverlay = !Ui.ShowConveyorOverlay;
                    PersistPanelSettings();
                    break;
                case UiLayout.ToggleFillBarsId:
                    CycleFillBarsVisibility();
                    if (Ui.ActiveMenu != MenuPanel.Tools && Ui.ActiveMenu != MenuPanel.Settings)
                        Ui.ActiveMenu = MenuPanel.None;
                    PersistPanelSettings();
                    break;
                case UiLayout.TogglePerformanceModeId:
                    Config.PerformanceMode = !Config.PerformanceMode;
                    if (Config.PerformanceMode)
                        Config.HighResScanning = false;
                    PersistPanelSettings();
                    break;
                case UiLayout.ToggleHighResScanningId:
                    Config.HighResScanning = !Config.HighResScanning;
                    if (Config.HighResScanning)
                        Config.PerformanceMode = false;
                    PersistPanelSettings();
                    break;
                case UiLayout.ToggleDebugModeId:
                    Config.ShowDebug = !Config.ShowDebug;
                    PersistPanelSettings();
                    break;
                case UiLayout.TogglePerfStatsId:
                    Config.ShowPerfStats = !Config.ShowPerfStats;
                    PersistPanelSettings();
                    break;
                case UiLayout.CyclePaletteId:
                    Config.CycleUiPalette();
                    PersistPanelSettings();
                    break;
                case UiLayout.AdjustPaletteHueId:
                    if (!TrySetStyleSliderFromCursor(regionId))
                        Config.AdjustUiHue(15);
                    PersistPanelSettings();
                    break;
                case UiLayout.AdjustPaletteBrightnessId:
                    if (!TrySetStyleSliderFromCursor(regionId))
                        Config.UiBrightness = NextBrightnessPreset(Config.UiBrightness);
                    PersistPanelSettings();
                    break;
                case UiLayout.AdjustPaletteSaturationId:
                    if (!TrySetStyleSliderFromCursor(regionId))
                        Config.AdjustUiSaturation(0.1f);
                    PersistPanelSettings();
                    break;
                case UiLayout.AdjustPaletteAlphaId:
                    Config.AdjustUiAlpha(-0.1f);
                    if (Config.UiAlpha <= 0.3f)
                        Config.UiAlpha = 1f;
                    PersistPanelSettings();
                    break;
                case UiLayout.CycleUiFontId:
                    Config.CycleUiFont();
                    PersistPanelSettings();
                    break;
                case UiLayout.AdjustAccentHueId:
                    if (!TrySetStyleSliderFromCursor(regionId))
                        Config.AdjustUiAccentHue(15);
                    PersistPanelSettings();
                    break;
                case UiLayout.AdjustAccentBrightnessId:
                    if (!TrySetStyleSliderFromCursor(regionId))
                        Config.AdjustUiAccentBrightness(0.1f);
                    PersistPanelSettings();
                    break;
                case UiLayout.AdjustAccentSaturationId:
                    if (!TrySetStyleSliderFromCursor(regionId))
                        Config.AdjustUiAccentSaturation(0.1f);
                    PersistPanelSettings();
                    break;
                case UiLayout.AdjustSchematicMainHueId:
                case UiLayout.AdjustSchematicSecondaryHueId:
                case UiLayout.AdjustConveyorHueId:
                    TrySetStyleSliderFromCursor(regionId);
                    PersistPanelSettings();
                    break;
                case UiLayout.AdjustPanelBrightnessId:
                    Config.UiPanelBrightness = NextBrightnessPreset(Config.UiPanelBrightness);
                    PersistPanelSettings();
                    break;
                case UiLayout.ToggleMouseControlId:
                    ToggleConstructMouseControl();
                    break;
                case UiLayout.CycleMouseSensitivityId:
                    CycleConstructMouseSensitivity();
                    break;
                case UiLayout.ToggleGridRotationId:
                    Config.AllowGridRotation = !Config.AllowGridRotation;
                    PersistPanelSettings();
                    break;
                case UiLayout.AdjustPanelAlphaId:
                    Config.AdjustUiPanelAlpha(-0.1f);
                    if (Config.UiPanelAlpha <= 0.3f)
                        Config.UiPanelAlpha = 1f;
                    PersistPanelSettings();
                    break;
                case UiLayout.AdjustHullScanAlphaId:
                    Config.HullScanAlpha = NextAlphaPreset(Config.HullScanAlpha);
                    PersistPanelSettings();
                    break;
                case UiLayout.AdjustSchematicAlphaId:
                    Config.SchematicAlpha = NextAlphaPreset(Config.SchematicAlpha);
                    PersistPanelSettings();
                    break;
                case UiLayout.CycleStorageColorId:
                    Config.CycleStorageColor();
                    PersistPanelSettings();
                    break;
                case UiLayout.CycleEffectorColorId:
                    Config.CycleEffectorColor();
                    PersistPanelSettings();
                    break;
                case UiLayout.SaveSettingsId:
                    PersistPanelSettings();
                    break;
                case UiLayout.CopySettingsId:
                    LatchSettingsAction(regionId);
                    PersistPanelSettings();
                    CopiedPanelSettings = Config.ToIniText();
                    break;
                case UiLayout.PasteSettingsId:
                    LatchSettingsAction(regionId);
                    PasteCopiedPanelSettings();
                    break;
                case UiLayout.CopyUiSettingsId:
                    LatchSettingsAction(regionId);
                    CopiedUiSettings = BuildSettingsGroupText("UI");
                    break;
                case UiLayout.PasteUiSettingsId:
                    LatchSettingsAction(regionId);
                    PasteSettingsGroup("UI", CopiedUiSettings);
                    break;
                case UiLayout.ResetStyleSettingsId:
                    Config.ResetUiStyleSettings();
                    ApplyConfigToUi();
                    PersistPanelSettings();
                    break;
                case UiLayout.CopyRenderingSettingsId:
                    LatchSettingsAction(regionId);
                    CopiedRenderingSettings = BuildSettingsGroupText("RENDERING");
                    break;
                case UiLayout.PasteRenderingSettingsId:
                    LatchSettingsAction(regionId);
                    PasteSettingsGroup("RENDERING", CopiedRenderingSettings);
                    break;
                case UiLayout.CopySchematicSettingsId:
                    LatchSettingsAction(regionId);
                    CopiedSchematicSettings = BuildSettingsGroupText("SCHEMATICS");
                    break;
                case UiLayout.PasteSchematicSettingsId:
                    LatchSettingsAction(regionId);
                    PasteSettingsGroup("SCHEMATICS", CopiedSchematicSettings);
                    break;
                case UiLayout.CopyDebugSettingsId:
                    LatchSettingsAction(regionId);
                    CopiedDebugSettings = BuildSettingsGroupText("DEBUG");
                    break;
                case UiLayout.PasteDebugSettingsId:
                    LatchSettingsAction(regionId);
                    PasteSettingsGroup("DEBUG", CopiedDebugSettings);
                    break;
                case UiLayout.WipePanelCacheId:
                    LatchSettingsAction(regionId);
                    WipePanelRenderCache();
                    break;
                case UiLayout.ResetPanelSettingsId:
                    LatchSettingsAction(regionId);
                    ResetPanelSettingsToDefaults();
                    break;
                case UiLayout.ExportSettingsId:
                    PersistPanelSettings();
                    break;
                case UiLayout.RecalibrateCursorId:
                    StartCursorRecalibration();
                    break;
                case UiLayout.ResetUiSettingsId:
                    Config.ResetUiSettings();
                    ApplyConfigToUi();
                    PersistPanelSettings();
                    break;
                case UiLayout.CycleScanModeId:
                    Config.CycleFillMode();
                    PersistPanelSettings();
                    break;
                case UiLayout.CycleScanColorScaleId:
                    Config.CycleHullScanColorScale();
                    PersistPanelSettings();
                    break;
                case UiLayout.SetDensityId:
                    Config.ToggleFillMode(GridSchematicsConfig.FillDensity);
                    PersistPanelSettings();
                    break;
                case UiLayout.SetThicknessId:
                    Config.ToggleFillMode(GridSchematicsConfig.FillThickness);
                    PersistPanelSettings();
                    break;
                case UiLayout.SetVoidsId:
                    Config.ToggleFillMode(GridSchematicsConfig.FillVoids);
                    PersistPanelSettings();
                    break;
                case UiLayout.SetHitsId:
                    Config.ToggleFillMode(GridSchematicsConfig.FillHits);
                    PersistPanelSettings();
                    break;
                case UiLayout.RunScanId:
                    RunCachedRaycastScan();
                    break;
                case UiLayout.CancelScanId:
                    CancelActiveScan();
                    break;
                case UiLayout.CalibrationStartId:
                    StartCalibration();
                    break;
                case UiLayout.CalibrationCloseId:
                    CloseCalibrationPrompt();
                    break;
                case UiLayout.CalibrationPointId:
                    AdvanceCalibrationPoint();
                    break;
                case UiLayout.SchematicCargoId:
                    Ui.ActiveOverlay = Ui.ActiveOverlay == OverlayMode.Cargo ? OverlayMode.None : OverlayMode.Cargo;
                    PersistPanelSettings();
                    break;
                case UiLayout.SchematicEnginesId:
                    Ui.ActiveOverlay = Ui.ActiveOverlay == OverlayMode.Engines ? OverlayMode.None : OverlayMode.Engines;
                    PersistPanelSettings();
                    break;
                case UiLayout.SchematicPowerId:
                    Ui.ActiveOverlay = Ui.ActiveOverlay == OverlayMode.Power ? OverlayMode.None : OverlayMode.Power;
                    PersistPanelSettings();
                    break;
                case UiLayout.SchematicOxygenId:
                    Ui.ActiveOverlay = Ui.ActiveOverlay == OverlayMode.Oxygen ? OverlayMode.None : OverlayMode.Oxygen;
                    PersistPanelSettings();
                    break;
                case UiLayout.SchematicConveyorId:
                    Ui.ShowConveyorOverlay = !Ui.ShowConveyorOverlay;
                    PersistPanelSettings();
                    break;
            }
        }

        bool HandleInfoPanelBlockTabPress(string regionId)
        {
            if (string.IsNullOrEmpty(regionId))
                return false;

            int index;
            if (string.Equals(regionId, UiLayout.InfoPanelAllTabId, StringComparison.Ordinal))
            {
                index = UiState.SelectedBlockStackAllIndex;
            }
            else if (string.Equals(regionId, UiLayout.InfoPanelStackTabId, StringComparison.Ordinal))
            {
                index = UiState.SelectedBlockStackAggregateIndex;
            }
            else
            {
                if (!regionId.StartsWith(UiLayout.InfoPanelBlockTabPrefix, StringComparison.Ordinal))
                    return false;
                if (!int.TryParse(regionId.Substring(UiLayout.InfoPanelBlockTabPrefix.Length), out index))
                    return true;
            }

            if (Ui.SelectedBlockStackItems == null || Ui.SelectedBlockStackItems.Count == 0)
                return true;
            if (index >= Ui.SelectedBlockStackItems.Count)
                return true;
            if (index == UiState.SelectedBlockStackAggregateIndex && Ui.SelectedBlockStackItems.Count <= 1)
                return true;

            Ui.SelectedBlockStackIndex = index;
            Ui.SelectedOverlayBlockId = null;
            Ui.SelectedOverlayLineIndex = 0;
            Ui.ShowInfoPanel = SupportsInfoPanel;
            Ui.InfoPanelMode = InfoPanelMode.Systems;
            Ui.ActiveMenu = MenuPanel.None;
            _hasLastMouseWheelValue = false;
            _renderDirty = true;
            return true;
        }

        void CycleGridVisibility()
        {
            if (Ui.GridVisibilityLevel <= 0)
                Ui.GridVisibilityLevel = 1;
            else if (Ui.GridVisibilityLevel == 1)
                Ui.GridVisibilityLevel = 2;
            else
                Ui.GridVisibilityLevel = 0;

            Ui.ShowDebugGrid = Ui.GridVisibilityLevel != 0;
        }

        void CycleShipBorderVisibility()
        {
            if (!Ui.ShowShipBorder)
            {
                Ui.ShowShipBorder = true;
                Ui.ShipBorderOpacity = 0.25f;
            }
            else if (Ui.ShipBorderOpacity < 0.375f)
            {
                Ui.ShipBorderOpacity = 0.5f;
            }
            else
            {
                Ui.ShowShipBorder = false;
                Ui.ShipBorderOpacity = 0f;
            }
        }

        void CycleFillBarsVisibility()
        {
            Ui.FillBarsVisibilityLevel--;
            if (Ui.FillBarsVisibilityLevel < 0)
                Ui.FillBarsVisibilityLevel = 2;
        }

        void CycleHullScanVisibility()
        {
            if (!Ui.ShowHullScan)
            {
                Ui.ShowHullScan = true;
                Ui.HullScanBrightness = 0.5f;
            }
            else if (Ui.HullScanBrightness < 0.75f)
            {
                Ui.HullScanBrightness = 1f;
            }
            else
            {
                Ui.ShowHullScan = false;
                Ui.HullScanBrightness = 0f;
            }
        }

        float NextBrightnessPreset(float current)
        {
            if (current < 0.85f)
                return 1.0f;
            if (current < 1.15f)
                return 1.25f;
            return 0.75f;
        }

        float NextAlphaPreset(float current)
        {
            if (current > 0.85f)
                return 0.65f;
            if (current > 0.45f)
                return 0.35f;
            return 1f;
        }

        void PasteCopiedPanelSettings()
        {
            if (string.IsNullOrEmpty(CopiedPanelSettings))
                return;

            Config.Parse(CopiedPanelSettings);
            ApplyConfigToUi();
            PersistPanelSettings();
            _renderDirty = true;
        }

        bool HandleOverlayBlockPress(string regionId)
        {
            if (!IsOverlayBlockRegion(regionId))
                return false;

            if (string.Equals(Ui.SelectedOverlayBlockId, regionId, StringComparison.Ordinal))
            {
                Ui.SelectedOverlayBlockId = null;
                Ui.SelectedOverlayLineIndex = 0;
                _hasLastMouseWheelValue = false;
            }
            else
            {
                Ui.SelectedOverlayBlockId = regionId;
                Ui.SelectedOverlayLineIndex = 0;
                if (!PopulateSelectedBlockStackFromPreview())
                    PopulateSelectedBlockStackFromOverlayInfo(FindOverlayInfo(regionId));
                Ui.ShowInfoPanel = SupportsInfoPanel;
                Ui.InfoPanelMode = InfoPanelMode.Systems;
                Ui.ActiveMenu = MenuPanel.None;
                if (SupportsFullInfoPanel)
                    ResetViewportCameraForInfoDrawerOpen();
                PersistPanelSettings();
                _hasLastMouseWheelValue = false;
            }

            return true;
        }

        void PopulateSelectedBlockStackFromOverlayInfo(OverlayBlockInfo info)
        {
            ClearSelectedBlockStack();
            if (info == null || info.Blocks == null || info.Blocks.Count == 0)
                return;

            for (int i = 0; i < info.Blocks.Count; i++)
            {
                var block = info.Blocks[i];
                if (block == null)
                    continue;

                string name = block.DisplayNameText;
                if (string.IsNullOrEmpty(name))
                    name = block.DefinitionDisplayNameText;
                if (string.IsNullOrEmpty(name))
                    name = "Block";

                Ui.SelectedBlockStackItems.Add(new BlockStackItem
                {
                    Id = info.Id + ":block:" + i.ToString(),
                    Name = name,
                    Block = block,
                    Projected = new Vector2I(info.Region.X, info.Region.Y),
                    Depth = i
                });
            }

            Ui.SelectedBlockStackSignature = info.Id;
            Ui.SelectedBlockStackIndex = Ui.SelectedBlockStackItems.Count > 1
                ? UiState.SelectedBlockStackAggregateIndex
                : 0;
            Ui.SelectedBlockStackScrollIndex = 0;
        }

        bool PopulateSelectedBlockStackFromPreview()
        {
            if (Ui.PreviewBlockStackItems == null || Ui.PreviewBlockStackItems.Count == 0)
                return false;

            ClearSelectedBlockStack();
            for (int i = 0; i < Ui.PreviewBlockStackItems.Count; i++)
                Ui.SelectedBlockStackItems.Add(Ui.PreviewBlockStackItems[i]);

            Ui.SelectedBlockStackSignature = Ui.PreviewBlockStackSignature;
            Ui.SelectedBlockStackIndex = Ui.SelectedBlockStackItems.Count > 1
                ? UiState.SelectedBlockStackAggregateIndex
                : 0;
            Ui.SelectedBlockStackScrollIndex = 0;
            return true;
        }

        bool IsOverlayBlockRegion(string regionId)
        {
            return !string.IsNullOrEmpty(regionId) && regionId.StartsWith("overlay:block:", StringComparison.Ordinal);
        }

        bool IsUiBlockerRegion(string regionId)
        {
            return string.Equals(regionId, UiLayout.UiBlockerId, StringComparison.Ordinal) ||
                string.Equals(regionId, UiLayout.InfoPanelBlockTabScrollId, StringComparison.Ordinal);
        }

        void ResetViewportCamera()
        {
            Ui.ZoomLevel = 0;
            Ui.PanX = 0;
            Ui.PanY = 0;
            _isPanningRender = false;
        }

        void ResetViewportCameraForInfoDrawerOpen()
        {
            if (Ui.ZoomLevel == 0 && Ui.PanX == 0 && Ui.PanY == 0)
                ResetViewportCamera();
            else
                _isPanningRender = false;
        }

        bool UpdateViewportWheel()
        {
            if (!TouchInput.IsAvailable || !TouchInput.IsCursorOnScreen || MyAPIGateway.Input == null)
            {
                _hasLastMouseWheelValue = false;
                return false;
            }

            bool cursorInRenderArea = IsCursorInRenderArea();
            if (!cursorInRenderArea)
            {
                _hasLastMouseWheelValue = false;
                return false;
            }
            int wheelValue;
            try
            {
                wheelValue = MyAPIGateway.Input.MouseScrollWheelValue();
            }
            catch
            {
                return false;
            }

            if (!_hasLastMouseWheelValue)
            {
                _lastMouseWheelValue = wheelValue;
                _hasLastMouseWheelValue = true;
                return false;
            }

            int delta = wheelValue - _lastMouseWheelValue;
            _lastMouseWheelValue = wheelValue;
            if (delta == 0)
                return false;
            TouchInput.MarkScrollActive();

            if (IsCapsLockHeld())
                return false;

            if (IsControlHeld())
            {
                if (Config.AllowGridRotation)
                {
                    Config.RotateCurrentView(delta > 0 ? 1 : -1);
                    PersistPanelSettings();
                    return true;
                }

                return false;
            }

            if (!cursorInRenderArea)
                return false;

            int nextZoom = Ui.ZoomLevel + (delta > 0 ? 1 : -1);
            if (nextZoom < 0)
                nextZoom = 0;
            if (nextZoom > 8)
                nextZoom = 8;
            if (nextZoom == Ui.ZoomLevel)
                return false;

            Ui.ZoomLevel = nextZoom;
            _lastViewportMotionTick = _currentTick;
            if (Ui.ZoomLevel == 0)
            {
                Ui.PanX = 0;
                Ui.PanY = 0;
            }

            return true;
        }

        bool UpdateInfoPanelStackHeaderScroll()
        {
            if (!TouchInput.IsAvailable || !TouchInput.IsCursorOnScreen || MyAPIGateway.Input == null)
                return false;
            if (!SupportsInfoPanel || !Ui.ShowInfoPanel || Ui.InfoPanelMode != InfoPanelMode.Systems || Ui.SelectedBlockStackItems == null || Ui.SelectedBlockStackItems.Count <= 0)
                return false;

            string hover = TouchInput.HoverRegionId ?? string.Empty;
            if (!string.Equals(hover, UiLayout.InfoPanelBlockTabScrollId, StringComparison.Ordinal) &&
                !string.Equals(hover, UiLayout.InfoPanelAllTabId, StringComparison.Ordinal) &&
                !string.Equals(hover, UiLayout.InfoPanelStackTabId, StringComparison.Ordinal) &&
                !hover.StartsWith(UiLayout.InfoPanelBlockTabPrefix, StringComparison.Ordinal))
                return false;

            int wheelValue;
            try
            {
                wheelValue = MyAPIGateway.Input.MouseScrollWheelValue();
            }
            catch
            {
                return false;
            }

            if (!_hasLastMouseWheelValue)
            {
                _lastMouseWheelValue = wheelValue;
                _hasLastMouseWheelValue = true;
                return false;
            }

            int delta = wheelValue - _lastMouseWheelValue;
            _lastMouseWheelValue = wheelValue;
            if (delta == 0)
                return false;

            TouchInput.MarkScrollActive();
            int direction = delta > 0 ? -1 : 1;
            int maxScroll = GetInfoPanelStackHeaderMaxScroll();
            int next = Ui.SelectedBlockStackScrollIndex + direction;
            if (next < 0)
                next = 0;
            if (next > maxScroll)
                next = maxScroll;
            if (next == Ui.SelectedBlockStackScrollIndex)
                return false;

            Ui.SelectedBlockStackScrollIndex = next;
            return true;
        }

        int GetInfoPanelStackHeaderMaxScroll()
        {
            if (Surface == null || Ui.SelectedBlockStackItems == null || Ui.SelectedBlockStackItems.Count <= 0)
                return 0;

            var zones = UiLayout.BuildZones((int)Surface.SurfaceSize.X, (int)Surface.SurfaceSize.Y);
            var metrics = UiLayout.BuildChromeMetrics((int)Surface.SurfaceSize.X, (int)Surface.SurfaceSize.Y);
            int pinnedWidth = UiLayout.InfoPanelPinnedTabWidth((int)Surface.SurfaceSize.X, metrics);
            int pinnedCount = Ui.SelectedBlockStackItems.Count > 1 ? 2 : 1;
            int stripWidth = zones.Center.Width - pinnedWidth * pinnedCount;
            int tabWidth = UiLayout.InfoPanelBlockTabWidth((int)Surface.SurfaceSize.X, metrics);
            int visibleCount = stripWidth > 0 ? stripWidth / tabWidth : 1;
            if (visibleCount < 1)
                visibleCount = 1;
            return Math.Max(0, Ui.SelectedBlockStackItems.Count - visibleCount);
        }

        bool TrySelectPreviewBlockStack()
        {
            if (!TouchInput.IsAvailable || !TouchInput.IsCursorOnScreen)
                return false;
            if (!IsCursorInRenderArea() || !string.IsNullOrEmpty(TouchInput.HoverRegionId))
                return false;
            if (Ui.PreviewBlockStackItems == null || Ui.PreviewBlockStackItems.Count == 0)
                return false;

            Ui.SelectedBlockStackItems.Clear();
            for (int i = 0; i < Ui.PreviewBlockStackItems.Count; i++)
                Ui.SelectedBlockStackItems.Add(Ui.PreviewBlockStackItems[i]);

            Ui.SelectedBlockStackSignature = Ui.PreviewBlockStackSignature;
            Ui.SelectedBlockStackIndex = Ui.PreviewBlockStackIndex;
            Ui.SelectedBlockStackScrollIndex = 0;
            if (Ui.SelectedBlockStackIndex < 0)
                Ui.SelectedBlockStackIndex = 0;
            if (Ui.SelectedBlockStackIndex >= Ui.SelectedBlockStackItems.Count)
                Ui.SelectedBlockStackIndex = Ui.SelectedBlockStackItems.Count - 1;

            Ui.SelectedOverlayBlockId = null;
            Ui.SelectedOverlayLineIndex = 0;
            Ui.ShowInfoPanel = SupportsInfoPanel;
            Ui.InfoPanelMode = InfoPanelMode.Systems;
            Ui.ActiveMenu = MenuPanel.None;
            if (SupportsFullInfoPanel)
                ResetViewportCameraForInfoDrawerOpen();
            PersistPanelSettings();
            return true;
        }

        bool UpdateRenderClickSelection()
        {
            if (!TouchInput.IsAvailable || !TouchInput.IsCursorOnScreen)
            {
                ResetRenderClickSelection();
                return false;
            }

            if (TouchInput.JustPressed && IsCursorInRenderArea() &&
                (string.IsNullOrEmpty(TouchInput.HoverRegionId) || IsOverlayBlockRegion(TouchInput.HoverRegionId)))
            {
                _pendingRenderClick = true;
                _renderClickMoved = false;
                _renderClickStartPosition = TouchInput.CursorPosition;
                _renderClickStartRegionId = TouchInput.HoverRegionId ?? string.Empty;
            }

            if (!_pendingRenderClick)
                return false;

            if (TouchInput.IsPressed)
            {
                if (Vector2.DistanceSquared(TouchInput.CursorPosition, _renderClickStartPosition) > 4f)
                    _renderClickMoved = true;
                return false;
            }

            if (!TouchInput.JustReleased)
            {
                ResetRenderClickSelection();
                return false;
            }

            bool handled = false;
            if (!_renderClickMoved && IsCursorInRenderArea())
            {
                string hover = TouchInput.HoverRegionId ?? string.Empty;
                if (IsOverlayBlockRegion(_renderClickStartRegionId) &&
                    string.Equals(_renderClickStartRegionId, hover, StringComparison.Ordinal))
                {
                    handled = HandleOverlayBlockPress(_renderClickStartRegionId);
                }
                else if (string.IsNullOrEmpty(_renderClickStartRegionId) && string.IsNullOrEmpty(hover))
                {
                    handled = TrySelectPreviewBlockStack();
                }
            }

            ResetRenderClickSelection();
            return handled;
        }

        void ResetRenderClickSelection()
        {
            _pendingRenderClick = false;
            _renderClickMoved = false;
            _renderClickStartRegionId = string.Empty;
        }

        void ClearSelectedBlockStack()
        {
            Ui.SelectedBlockStackItems.Clear();
            Ui.SelectedBlockStackIndex = 0;
            Ui.SelectedBlockStackScrollIndex = 0;
            Ui.SelectedBlockStackSignature = string.Empty;
        }

        bool UpdateViewportPan()
        {
            if (!TouchInput.IsAvailable || !TouchInput.IsCursorOnScreen)
            {
                _isPanningRender = false;
                return false;
            }

            if (!TouchInput.IsPressed)
            {
                _isPanningRender = false;
                return false;
            }

            if (!IsCursorInRenderArea())
            {
                _isPanningRender = false;
                return false;
            }

            if (!_isPanningRender && !string.IsNullOrEmpty(TouchInput.HoverRegionId) && !IsOverlayBlockRegion(TouchInput.HoverRegionId))
            {
                _isPanningRender = false;
                return false;
            }

            if (!_isPanningRender)
            {
                _isPanningRender = true;
                _lastPanCursorPosition = TouchInput.CursorPosition;
                return false;
            }

            var delta = TouchInput.CursorPosition - _lastPanCursorPosition;
            _lastPanCursorPosition = TouchInput.CursorPosition;
            if (Math.Abs(delta.X) < 0.5f && Math.Abs(delta.Y) < 0.5f)
                return false;

            Ui.PanX += (int)Math.Round(delta.X);
            Ui.PanY += (int)Math.Round(delta.Y);
            _lastViewportMotionTick = _currentTick;
            return true;
        }

        bool IsControlHeld()
        {
            if (MyAPIGateway.Input == null)
                return false;

            try
            {
                return MyAPIGateway.Input.IsAnyCtrlKeyPressed();
            }
            catch
            {
                return false;
            }
        }

        bool IsCapsLockHeld()
        {
            if (MyAPIGateway.Input == null)
                return false;

            try
            {
                return MyAPIGateway.Input.IsKeyPress(VRage.Input.MyKeys.CapsLock);
            }
            catch
            {
                return false;
            }
        }

        bool TryToggleSelectedOverlayBlock()
        {
            var info = FindOverlayInfo(Ui.SelectedOverlayBlockId);
            if (info == null || info.Lines == null || Ui.SelectedOverlayLineIndex < 0 || Ui.SelectedOverlayLineIndex >= info.Lines.Count)
                return false;

            var line = info.Lines[Ui.SelectedOverlayLineIndex];
            if (line == null)
                return false;

            if (line.BatteryBlock != null)
            {
                CycleBatteryChargeMode(line.BatteryBlock);
                return true;
            }

            if (line.TerminalAction != null && line.TerminalBlock != null)
            {
                try
                {
                    if (!line.TerminalAction.IsEnabled(line.TerminalBlock))
                        return false;
                }
                catch
                {
                }

                try
                {
                    line.TerminalAction.Apply(line.TerminalBlock);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            if (line.ToggleBlock == null)
                return false;

            line.ToggleBlock.Enabled = !line.ToggleBlock.Enabled;
            return true;
        }

        void CycleBatteryChargeMode(IMyBatteryBlock battery)
        {
            if (battery == null)
                return;

            try
            {
                if (battery.ChargeMode == Sandbox.ModAPI.Ingame.ChargeMode.Auto)
                    battery.ChargeMode = Sandbox.ModAPI.Ingame.ChargeMode.Recharge;
                else if (battery.ChargeMode == Sandbox.ModAPI.Ingame.ChargeMode.Recharge)
                    battery.ChargeMode = Sandbox.ModAPI.Ingame.ChargeMode.Discharge;
                else
                    battery.ChargeMode = Sandbox.ModAPI.Ingame.ChargeMode.Auto;
            }
            catch
            {
            }
        }

        bool IsCursorInRenderArea()
        {
            if (!TouchInput.IsAvailable || !TouchInput.IsCursorOnScreen || Surface == null)
                return false;

            var zones = UiLayout.BuildZones((int)Surface.SurfaceSize.X, (int)Surface.SurfaceSize.Y);
            var renderZone = BuildInteractiveRenderZone(zones.Center);
            var p = TouchInput.CursorPosition;
            return p.X >= renderZone.X && p.X <= renderZone.X + renderZone.Width &&
                p.Y >= renderZone.Y && p.Y <= renderZone.Y + renderZone.Height;
        }

        ScreenZone BuildInteractiveRenderZone(ScreenZone center)
        {
            if (Ui.ChromeHidden || !SupportsInfoPanel || !Ui.ShowInfoPanel || Ui.SegmentMode)
                return center;

            var infoPanel = UiLayout.BuildInfoPanelZone(center, SupportsFullInfoPanel);

            return new ScreenZone(
                ScreenZoneType.CenterViewport,
                center.X,
                center.Y,
                center.Width,
                Math.Max(1, infoPanel.Y - center.Y));
        }

        OverlayBlockInfo FindOverlayInfo(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            for (int i = 0; i < Ui.OverlayBlockRegions.Count; i++)
            {
                var info = Ui.OverlayBlockRegions[i];
                if (info != null && string.Equals(info.Id, id, StringComparison.Ordinal))
                    return info;
            }

            return null;
        }

        void PlayMenuPressSound()
        {
            try
            {
                MyVisualScriptLogicProvider.PlaySoundAmbientLocal("HudBleep");
            }
            catch
            {
            }
        }

        void PlaySharedCursorModeSound()
        {
            try
            {
                MyVisualScriptLogicProvider.PlaySoundAmbientLocal("HudClick");
            }
            catch
            {
            }
        }

        void PlayBlockSelectSound()
        {
            try
            {
                MyVisualScriptLogicProvider.PlaySoundAmbientLocal("HudMouseClick");
            }
            catch
            {
            }
        }

        bool UpdateSharedGridCursor(int tick)
        {
            if (Ui.SelectedOverlayBlockId != null)
                return false;

            if (Session == null || ConstructCache == null || ConstructCache.ShipGrid == null || ConstructCache.ShipGrid.IsEmpty)
                return false;

            if (!TouchInput.IsAvailable || !TouchInput.IsCursorOnScreen)
                return false;

            var activeCursor = Session.GetSharedCursor(ConstructId);
            bool isActiveSource = activeCursor.IsFromPanel(_panelEntityId);
            bool hasSecondaryInput = TouchInput.HasSecondaryButton;
            bool pressStarted = hasSecondaryInput ? TouchInput.SecondaryJustPressed : TouchInput.JustPressed;
            bool pressHeld = hasSecondaryInput ? TouchInput.IsSecondaryPressed : TouchInput.IsPressed;
            bool wantsPublish = pressStarted || isActiveSource || pressHeld;
            if (!wantsPublish)
                return false;

            if (!pressStarted && tick - _lastSharedCursorPublishTick < SharedCursorPublishIntervalTicks)
                return false;

            var grid = OwnerBlock.CubeGrid;
            var sharedSourceGrid = grid != null ? ConstructCache.UpdateConstructProjection(grid, Config.GetScanView(), false) : null;
            if (sharedSourceGrid == null || sharedSourceGrid.IsEmpty)
                return false;

            var surface = Surface;
            if (surface == null)
                return false;

            var zones = UiLayout.BuildZones((int)surface.SurfaceSize.X, (int)surface.SurfaceSize.Y);
            var renderZone = BuildInteractiveRenderZone(zones.Center);
            if (!IsPointInZone(TouchInput.CursorPosition, renderZone))
                return false;
            var renderCenter = UiLayout.BuildTransformedCenterZone(renderZone, Ui.ZoomLevel, Ui.PanX, Ui.PanY);

            if (pressStarted)
            {
                if (activeCursor.IsFromPanel(_panelEntityId))
                {
                    Session.ClearSharedCursor(ConstructId, tick);
                    _lastSharedCursorPublishTick = tick;
                    return true;
                }

                return TryPublishSharedGridCursor(tick, renderCenter, sharedSourceGrid, null);
            }

            if (isActiveSource)
                return TryPublishSharedGridCursor(tick, renderCenter, sharedSourceGrid, activeCursor);

            if (pressHeld)
                return TryPublishSharedGridCursor(tick, renderCenter, sharedSourceGrid, null);

            return false;
        }

        bool TryPublishSharedGridCursor(int tick, ScreenZone center, ShipGrid shipGrid, SharedGridCursor? previousCursor)
        {
            var transform = shipGrid.CreateTransform(center, Config.GetRotationSteps());
            float localX;
            float localY;
            if (!transform.TryScreenToLocal(TouchInput.CursorPosition, out localX, out localY))
                return false;

            double projectedX = shipGrid.Min2D.X + localX - 0.5;
            double projectedY = shipGrid.Max2D.Y - localY + 0.5;
            double inferredAxis;
            var cursor = new SharedGridCursor
            {
                Active = true,
                ConstructId = ConstructId,
                SourcePanelId = _panelEntityId,
                LastUpdatedTick = tick,
                DirectX = false,
                DirectY = false,
                DirectZ = false
            };

            var view = Config.GetScanView();

            switch (view)
            {
                case ScanView.Front:
                    cursor.X = projectedX;
                    cursor.Y = projectedY;
                    cursor.HasX = true;
                    cursor.HasY = true;
                    cursor.DirectX = true;
                    cursor.DirectY = true;
                    if (!TryEstimateMissingAxisDepth(view, shipGrid, projectedX, projectedY, out inferredAxis))
                    {
                        if (previousCursor.HasValue && previousCursor.Value.HasZ)
                            inferredAxis = previousCursor.Value.Z;
                        else
                            inferredAxis = (shipGrid.DepthMin + shipGrid.DepthMax) * 0.5;
                    }

                    cursor.Z = inferredAxis;
                    cursor.HasZ = true;
                    break;
                case ScanView.Side:
                    cursor.Z = projectedX;
                    cursor.Y = projectedY;
                    cursor.HasZ = true;
                    cursor.HasY = true;
                    cursor.DirectZ = true;
                    cursor.DirectY = true;
                    if (!TryEstimateMissingAxisDepth(view, shipGrid, projectedX, projectedY, out inferredAxis))
                    {
                        if (previousCursor.HasValue && previousCursor.Value.HasX)
                            inferredAxis = previousCursor.Value.X;
                        else
                            inferredAxis = (shipGrid.DepthMin + shipGrid.DepthMax) * 0.5;
                    }

                    cursor.X = inferredAxis;
                    cursor.HasX = true;
                    break;
                default:
                    cursor.X = projectedX;
                    cursor.Z = projectedY;
                    cursor.HasX = true;
                    cursor.HasZ = true;
                    cursor.DirectX = true;
                    cursor.DirectZ = true;
                    if (!TryEstimateMissingAxisDepth(view, shipGrid, projectedX, projectedY, out inferredAxis))
                    {
                        if (previousCursor.HasValue && previousCursor.Value.HasY)
                            inferredAxis = previousCursor.Value.Y;
                        else
                            inferredAxis = (shipGrid.Min.Y + shipGrid.Max.Y) * 0.5;
                    }

                    cursor.Y = inferredAxis;
                    cursor.HasY = true;
                    break;
            }

            Session.SetSharedCursor(cursor);
            _lastSharedCursorPublishTick = tick;
            return true;
        }

        bool TryEstimateMissingAxisDepth(ScanView view, ShipGrid shipGrid, double primaryProjected, double secondaryProjected, out double inferredAxis)
        {
            inferredAxis = 0;
            if (ConstructCache == null || shipGrid == null)
                return false;

            var scanData = ConstructCache.GetRaycastData(view);
            if (scanData == null || !scanData.IsReady || scanData.Resolution <= 0 || scanData.Samples == null)
                return false;

            double minPrimary;
            double maxPrimary;
            double minSecondary;
            double maxSecondary;
            switch (view)
            {
                case ScanView.Front:
                    minPrimary = shipGrid.Min2D.X - 0.5;
                    maxPrimary = shipGrid.Max2D.X + 0.5;
                    minSecondary = shipGrid.Min2D.Y - 0.5;
                    maxSecondary = shipGrid.Max2D.Y + 0.5;
                    break;
                case ScanView.Side:
                    minPrimary = shipGrid.Min2D.X - 0.5;
                    maxPrimary = shipGrid.Max2D.X + 0.5;
                    minSecondary = shipGrid.Min2D.Y - 0.5;
                    maxSecondary = shipGrid.Max2D.Y + 0.5;
                    break;
                default:
                    minPrimary = shipGrid.Min2D.X - 0.5;
                    maxPrimary = shipGrid.Max2D.X + 0.5;
                    minSecondary = shipGrid.Min2D.Y - 0.5;
                    maxSecondary = shipGrid.Max2D.Y + 0.5;
                    break;
            }

            int resolution = Math.Max(1, scanData.Resolution);
            int indexX = ResolveProjectedAxisIndex(primaryProjected, minPrimary, maxPrimary, Math.Max(1, resolution - 1));
            int indexY = ResolveProjectedAxisIndex(secondaryProjected, minSecondary, maxSecondary, Math.Max(1, resolution - 1));
            if (indexX < 0 || indexX >= resolution || indexY < 0 || indexY >= resolution)
                return false;

            int sampleIndex = indexY * resolution + indexX;
            if (sampleIndex < 0 || sampleIndex >= scanData.Samples.Length)
                return false;

            var sample = scanData.Samples[sampleIndex];
            if (!sample.HasHit)
                return false;

            double normalizedDepth = (sample.FirstDistance + sample.LastDistance) * 0.5;
            double depthStart = shipGrid.DepthMin - 1.0;
            double depthEnd = shipGrid.DepthMax + 1.0;
            if (depthEnd <= depthStart)
                return false;

            inferredAxis = depthStart + (depthEnd - depthStart) * normalizedDepth;
            return true;
        }

        static int ResolveProjectedAxisIndex(double projected, double min, double max, int resolutionMax)
        {
            if (resolutionMax <= 0)
                return 0;

            float step = (float)((max - min) / resolutionMax);
            if (Math.Abs(step) < 0.0001f)
                return 0;

            int index = (int)Math.Round((projected - min) / step);
            if (index < 0)
                return 0;

            if (index > resolutionMax)
                return resolutionMax;

            return index;
        }

        static bool IsPointInZone(Vector2 point, ScreenZone zone)
        {
            return point.X >= zone.X && point.X <= zone.X + zone.Width &&
                point.Y >= zone.Y && point.Y <= zone.Y + zone.Height;
        }

        bool HasSharedCursorVisualChanged()
        {
            if (Session == null)
                return false;

            var cursor = Session.GetSharedCursor(ConstructId);
            return cursor.LastUpdatedTick != _lastSharedCursorRenderTick;
        }

        void CaptureSharedCursorVisualState()
        {
            if (Session == null)
                return;

            _lastSharedCursorRenderTick = Session.GetSharedCursor(ConstructId).LastUpdatedTick;
        }

        bool ShouldCloseActiveMenu(int tick)
        {
            if (Ui.ActiveMenu == MenuPanel.None)
            {
                _menuLeaveTick = -1;
                return false;
            }

            if (Ui.ActiveMenu == MenuPanel.Settings &&
                TouchInput != null &&
                TouchInput.IsPressed &&
                !string.IsNullOrEmpty(_activeStyleSliderRegionId))
            {
                _menuLeaveTick = -1;
                return false;
            }

            if (IsCursorInActiveMenuField())
            {
                _menuLeaveTick = -1;
                return false;
            }

            if (_menuLeaveTick < 0)
            {
                _menuLeaveTick = tick;
                return false;
            }

            return tick - _menuLeaveTick >= MenuCloseGraceTicks;
        }

        bool IsCursorInActiveMenuField()
        {
            if (!TouchInput.IsCursorOnScreen)
                return false;

            var hover = TouchInput.HoverRegionId ?? string.Empty;
            if (hover == UiLayout.MenuViewId || hover == UiLayout.MenuLayersId || hover == UiLayout.MenuScanId || hover == UiLayout.MenuSettingsId)
                return true;

            var surface = Surface;
            if (surface == null)
                return false;

            var regions = UiLayout.BuildMenuPanelRegions((int)surface.SurfaceSize.X, (int)surface.SurfaceSize.Y, Ui.ActiveMenu, Ui.SettingsExpandedMask);
            if (regions == null || regions.Length == 0)
                return false;

            var p = TouchInput.CursorPosition;
            const float padding = 14f;
            int minX = regions[0].X;
            int minY = regions[0].Y;
            int maxX = regions[0].X + regions[0].Width;
            int maxY = regions[0].Y + regions[0].Height;
            for (int i = 1; i < regions.Length; i++)
            {
                if (regions[i].X < minX) minX = regions[i].X;
                if (regions[i].Y < minY) minY = regions[i].Y;
                if (regions[i].X + regions[i].Width > maxX) maxX = regions[i].X + regions[i].Width;
                if (regions[i].Y + regions[i].Height > maxY) maxY = regions[i].Y + regions[i].Height;
            }

            return p.X >= minX - padding && p.X <= maxX + padding && p.Y >= minY - padding && p.Y <= maxY + padding;
        }

        bool HasTouchVisualChanged(out bool sceneChanged, out bool cursorChanged, out bool cursorMotionChanged)
        {
            sceneChanged = false;
            cursorChanged = false;
            cursorMotionChanged = false;

            if (TouchInput.IsCursorOnScreen != _lastCursorOnScreen)
            {
                cursorChanged = true;
                return true;
            }

            if (TouchInput.IsPressed != _lastTouchPressed)
            {
                cursorChanged = true;
                return true;
            }

            if (!string.Equals(TouchInput.HoverRegionId ?? string.Empty, _lastHoverRegionId, StringComparison.Ordinal))
            {
                cursorChanged = true;
                return true;
            }

            if (!string.Equals(TouchInput.StatusText ?? string.Empty, _lastTouchStatus, StringComparison.Ordinal))
            {
                cursorChanged = true;
                return true;
            }

            float cursorMovementThresholdSq = Ui.SegmentMode ? 1f : 0.16f;
            if (TouchInput.IsCursorOnScreen && Vector2.DistanceSquared(TouchInput.CursorPosition, _lastCursorPosition) > cursorMovementThresholdSq)
            {
                cursorChanged = true;
                cursorMotionChanged = true;
                return true;
            }

            return false;
        }

        bool HasTouchVisualChanged()
        {
            bool sceneChanged;
            bool cursorChanged;
            bool cursorMotionChanged;
            return HasTouchVisualChanged(out sceneChanged, out cursorChanged, out cursorMotionChanged);
        }

        void CaptureTouchVisualState()
        {
            _lastCursorOnScreen = TouchInput.IsCursorOnScreen;
            _lastTouchPressed = TouchInput.IsPressed;
            _lastCursorPosition = TouchInput.CursorPosition;
            _lastHoverRegionId = TouchInput.HoverRegionId ?? string.Empty;
            _lastTouchStatus = TouchInput.StatusText ?? string.Empty;
        }

        void ToggleMenu(MenuPanel menu)
        {
            bool closingOrChangingSettings = Ui.ActiveMenu == MenuPanel.Settings || menu == MenuPanel.Settings;
            Ui.ActiveMenu = Ui.ActiveMenu == menu ? MenuPanel.None : menu;
            if (closingOrChangingSettings)
            {
                Ui.ActiveSettingsActionId = null;
                ClearStyleSliderDrag();
            }
            _menuLeaveTick = -1;
        }

        void LatchSettingsAction(string regionId)
        {
            if (Ui.ActiveMenu == MenuPanel.Settings)
                Ui.ActiveSettingsActionId = regionId;
        }

        void WipePanelRenderCache()
        {
            RenderEngine.ClearRenderCaches();
            Ui.CachedCargoSummaryKey = null;
            Ui.CachedCargoSummary = null;
            Ui.OverlayBlockRegions.Clear();
            Ui.PreviewBlockStackItems.Clear();
            Ui.SelectedBlockStackItems.Clear();
            Ui.SelectedOverlayBlockId = null;
            Ui.SelectedOverlayLineIndex = 0;
            Ui.SelectedBlockStackSignature = null;
            Ui.PreviewBlockStackSignature = null;
            Ui.LastSegmentProjectionRefreshTick = -SegmentProjectionRefreshTicks;
            Ui.SegmentProjectionRefreshStep = 0;
            Ui.SegmentFrontGrid = null;
            Ui.SegmentLeftGrid = null;
            Ui.SegmentTopGrid = null;
            if (ConstructCache != null)
            {
                ConstructCache.ProjectedGrids.Clear();
                ConstructCache.ConveyorNetwork = null;
            }
            _lastProjectionRefreshTick = -600;
            _lastTopologyUpdateTick = -600;
            _renderDirty = true;
        }

        void ResetPanelSettingsToDefaults()
        {
            Config.ResetPanelSettingsPreservingCalibration();
            if (Session != null)
            {
                Session.SetConstructMouseControl(ConstructId, Config.MouseControl);
                Session.SetConstructMouseSensitivity(ConstructId, Config.MouseSensitivity);
            }
            ApplyConfigToUi();
            Ui.ActiveSettingsActionId = UiLayout.ResetPanelSettingsId;
            Ui.SettingsExpandedMask = UiLayout.SettingsCategoryDebug;
            ResetViewportCamera();
            PersistPanelSettings();
            WipePanelRenderCache();
        }

        void RegisterViewButtonHitRegions()
        {
            TouchInput.ClearHitRegions();

            var surface = Surface;
            if (surface == null)
                return;

            if (Ui.ChromeHidden)
            {
                TouchInput.AddHitRegion(UiLayout.BuildChromeRestoreRegion((int)surface.SurfaceSize.X, (int)surface.SurfaceSize.Y));
                return;
            }

            if (!Ui.SegmentMode)
            {
                for (int i = 0; i < Ui.OverlayBlockRegions.Count; i++)
                {
                    var info = Ui.OverlayBlockRegions[i];
                    if (info != null)
                        TouchInput.AddHitRegion(info.Region);
                }
            }

            var zones = UiLayout.BuildZones((int)surface.SurfaceSize.X, (int)surface.SurfaceSize.Y);
            if (Ui.ShowCalibrationPrompt || Ui.CalibrationActive)
            {
                var surfaceZone = new ScreenZone(ScreenZoneType.CenterViewport, 0, 0, (int)surface.SurfaceSize.X, (int)surface.SurfaceSize.Y);
                var calibrationZone = Ui.CalibrationActive
                    ? UiLayout.BuildCalibrationTargetZone(surfaceZone)
                    : UiLayout.BuildCalibrationPromptZone(surfaceZone, false);
                TouchInput.AddHitRegion(new HitRegion(0, 0, (int)surface.SurfaceSize.X, (int)surface.SurfaceSize.Y, UiLayout.UiBlockerId, "Calibration modal"));
                if (Ui.CalibrationActive && Ui.CalibrationCompletedTick < 0)
                    TouchInput.AddHitRegion(UiLayout.BuildCalibrationPointRegion(calibrationZone, Ui.CalibrationStep));
                if (!Ui.CalibrationActive && !Ui.CursorCalibrationRequired)
                    TouchInput.AddHitRegion(UiLayout.BuildCalibrationStartRegion(calibrationZone));
                if (!Ui.CursorCalibrationRequired)
                    TouchInput.AddHitRegion(UiLayout.BuildCalibrationCloseRegion(calibrationZone));
                return;
            }

            if (_activeScanJob != null && !_activeScanJob.IsComplete)
                TouchInput.AddHitRegion(UiLayout.BuildScanProgressCancelRegion(zones.Center));
            TouchInput.AddHitRegion(new HitRegion(zones.Top.X, zones.Top.Y, zones.Top.Width, zones.Top.Height, UiLayout.UiBlockerId, "UI chrome"));
            TouchInput.AddHitRegion(new HitRegion(zones.Bottom.X, zones.Bottom.Y, zones.Bottom.Width, zones.Bottom.Height, UiLayout.UiBlockerId, "UI chrome"));
            if (SupportsInfoPanel && Ui.ShowInfoPanel && !Ui.SegmentMode)
            {
                var infoPanelZone = UiLayout.BuildInfoPanelZone(zones.Center, SupportsFullInfoPanel);
                TouchInput.AddHitRegion(new HitRegion(infoPanelZone.X, infoPanelZone.Y, infoPanelZone.Width, infoPanelZone.Height, UiLayout.UiBlockerId, "Info panel"));
            }

            var panelBlockerRegions = UiLayout.BuildMenuPanelRegions((int)surface.SurfaceSize.X, (int)surface.SurfaceSize.Y, Ui.ActiveMenu, Ui.SettingsExpandedMask);
            if (panelBlockerRegions != null && panelBlockerRegions.Length > 0)
            {
                int minX = panelBlockerRegions[0].X;
                int minY = panelBlockerRegions[0].Y;
                int maxX = panelBlockerRegions[0].X + panelBlockerRegions[0].Width;
                int maxY = panelBlockerRegions[0].Y + panelBlockerRegions[0].Height;
                for (int i = 1; i < panelBlockerRegions.Length; i++)
                {
                    var region = panelBlockerRegions[i];
                    if (region.X < minX) minX = region.X;
                    if (region.Y < minY) minY = region.Y;
                    if (region.X + region.Width > maxX) maxX = region.X + region.Width;
                    if (region.Y + region.Height > maxY) maxY = region.Y + region.Height;
                }
                TouchInput.AddHitRegion(new HitRegion(minX, minY, maxX - minX, maxY - minY, UiLayout.UiBlockerId, "Menu panel"));
            }

            var regions = UiLayout.BuildTopMenuRegions((int)surface.SurfaceSize.X, (int)surface.SurfaceSize.Y);
            for (int i = 0; i < regions.Length; i++)
            {
                TouchInput.AddHitRegion(regions[i]);
            }

            var topRightRegions = UiLayout.BuildTopRightModeRegions((int)surface.SurfaceSize.X, (int)surface.SurfaceSize.Y);
            for (int i = 0; i < topRightRegions.Length; i++)
            {
                TouchInput.AddHitRegion(topRightRegions[i]);
            }

            var bottomInfoRegions = UiLayout.BuildBottomInfoRegions((int)surface.SurfaceSize.X, (int)surface.SurfaceSize.Y);
            for (int i = 0; i < bottomInfoRegions.Length; i++)
            {
                TouchInput.AddHitRegion(bottomInfoRegions[i]);
            }

            var panelRegions = UiLayout.BuildMenuPanelRegions((int)surface.SurfaceSize.X, (int)surface.SurfaceSize.Y, Ui.ActiveMenu, Ui.SettingsExpandedMask);
            for (int i = 0; i < panelRegions.Length; i++)
            {
                TouchInput.AddHitRegion(panelRegions[i]);
            }

            if (SupportsFullInfoPanel && Ui.ShowInfoPanel && Ui.InfoPanelMode == InfoPanelMode.Scan)
            {
                var scanDrawerRegions = UiLayout.BuildInfoPanelScanRegions((int)surface.SurfaceSize.X, (int)surface.SurfaceSize.Y);
                for (int i = 0; i < scanDrawerRegions.Length; i++)
                {
                    TouchInput.AddHitRegion(scanDrawerRegions[i]);
                }
            }
            else if (SupportsInfoPanel && Ui.ShowInfoPanel && Ui.InfoPanelMode == InfoPanelMode.Systems &&
                Ui.SelectedBlockStackItems != null && Ui.SelectedBlockStackItems.Count > 0)
            {
                var tabRegions = UiLayout.BuildInfoPanelBlockTabRegions((int)surface.SurfaceSize.X, (int)surface.SurfaceSize.Y, Ui.SelectedBlockStackItems.Count, Ui.SelectedBlockStackScrollIndex);
                for (int i = 0; i < tabRegions.Length; i++)
                {
                    TouchInput.AddHitRegion(tabRegions[i]);
                }
            }

            if (Ui.SegmentMode)
            {
            zones = UiLayout.BuildZones((int)surface.SurfaceSize.X, (int)surface.SurfaceSize.Y);
                int gap = 1;
                int leftWidth = zones.Center.Width / 2;
                int topHeight = zones.Center.Height / 2;
                var infoZone = new ScreenZone(ScreenZoneType.CenterViewport, zones.Center.X + leftWidth + gap, zones.Center.Y + topHeight + gap, Math.Max(1, zones.Center.Width - leftWidth - gap), Math.Max(1, zones.Center.Height - topHeight - gap));
                var segmentControls = UiLayout.BuildSegmentScanControlRegions(infoZone);
                for (int i = 0; i < segmentControls.Length; i++)
                    TouchInput.AddHitRegion(segmentControls[i]);
            }

            var bottomRegions = UiLayout.BuildBottomSchematicRegions((int)surface.SurfaceSize.X, (int)surface.SurfaceSize.Y);
            for (int i = 0; i < bottomRegions.Length; i++)
            {
                TouchInput.AddHitRegion(bottomRegions[i]);
            }

            if (Ui.SegmentMode)
                return;
        }

        public void Dispose()
        {
            TouchInput.Dispose();
        }

        bool UpdateTopologyIfNeeded(int tick)
        {
            var grid = OwnerBlock.CubeGrid;
            if (grid == null || ConstructCache == null)
                return false;

            var view = Config.GetScanView();
            bool forceTopologyRefresh = tick - _lastProjectionRefreshTick >= 600;
            if (forceTopologyRefresh)
                _lastProjectionRefreshTick = tick;

            bool updateIntervalElapsed = tick - _lastTopologyUpdateTick >= TopologyUpdateIntervalTicks;
            if (!forceTopologyRefresh && !updateIntervalElapsed)
                return UpdateSegmentProjectionCache(tick, false);

            _lastTopologyUpdateTick = tick;
            bool forceProjectionRefresh = forceTopologyRefresh && !HasReadyRaycastScan(view);
            ConstructCache.UpdateConstructProjection(grid, view, forceProjectionRefresh);
            ConstructCache.GetOrCreateConveyorNetwork(grid, forceTopologyRefresh);
            bool segmentChanged = UpdateSegmentProjectionCache(tick, forceTopologyRefresh);
            return forceTopologyRefresh || segmentChanged;
        }

        bool UpdateSegmentProjectionCache(int tick, bool force)
        {
            if (!Ui.SegmentMode || ConstructCache == null || OwnerBlock == null)
                return false;

            var grid = OwnerBlock.CubeGrid;
            if (grid == null)
                return false;

            bool missing = Ui.SegmentFrontGrid == null || Ui.SegmentLeftGrid == null || Ui.SegmentTopGrid == null;
            bool intervalElapsed = tick - Ui.LastSegmentProjectionRefreshTick >= SegmentProjectionRefreshTicks;
            if (!force && !missing && !intervalElapsed)
                return false;

            ScanView view;
            if (force || missing)
            {
                view = Ui.SegmentProjectionRefreshStep == 0 ? ScanView.Front :
                    Ui.SegmentProjectionRefreshStep == 1 ? ScanView.Side : ScanView.Top;
                Ui.SegmentProjectionRefreshStep = (Ui.SegmentProjectionRefreshStep + 1) % 3;
            }
            else
            {
                view = Ui.SegmentProjectionRefreshStep == 0 ? ScanView.Front :
                    Ui.SegmentProjectionRefreshStep == 1 ? ScanView.Side : ScanView.Top;
                Ui.SegmentProjectionRefreshStep = (Ui.SegmentProjectionRefreshStep + 1) % 3;
                Ui.LastSegmentProjectionRefreshTick = tick;
            }

            bool forceProjectionRefresh = force && !HasReadyRaycastScan(view);
            var shipGrid = ConstructCache.UpdateConstructProjection(grid, view, forceProjectionRefresh);
            if (view == ScanView.Front)
                Ui.SegmentFrontGrid = shipGrid;
            else if (view == ScanView.Side)
                Ui.SegmentLeftGrid = shipGrid;
            else
                Ui.SegmentTopGrid = shipGrid;

            if (force && !missing)
                Ui.LastSegmentProjectionRefreshTick = tick;
            if (Ui.SegmentFrontGrid != null && Ui.SegmentLeftGrid != null && Ui.SegmentTopGrid != null && missing)
                Ui.LastSegmentProjectionRefreshTick = tick;

            return true;
        }

        void RenderPanel()
        {
            RenderPanel(false);
        }

        void RenderPanel(bool forceCursorOnly)
        {
            if (ConstructCache == null)
                return;

            RenderEngine.RenderPanel(this, forceCursorOnly);
        }

        void RunCachedRaycastScan()
        {
            var grid = OwnerBlock.CubeGrid;
            if (grid == null || ConstructCache == null)
                return;

            StartIncrementalRaycastScan(grid, GetConfiguredRaycastScanResolution(), false, _currentTick);
        }

        int GetConfiguredRaycastScanResolution()
        {
            return Config != null && Config.HighResScanning ? 512 : InitialRaycastScanResolution;
        }
    }
}
