using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GridSchematics
{
    public static partial class RenderEngine
    {
        const int PipelineAlpha = 128;
        const int SharedCursorAlpha = 128;
        const int MaxScanRunCacheEntries = 24;
        const int MaxTopologyKeyCacheEntries = 12;
        const int MaxOverlaySourceCacheEntries = 18;
        const int MaxProjectionTransformCacheEntries = 24;
        const int MaxOverlayInfoCacheEntries = 24;
        const int MaxBorderCacheEntries = 12;
        const int MaxBlockGhostCacheEntries = 18;
        const int MaxConveyorProjectionCacheEntries = 18;
        const int MinimumLowLevelPanelResolutionAxis = 256;
        const int SegmentRaycastRenderStep = 2;
        const string ConnectorSideIconTexture = "GridSchematics_Icon_ConnectorSide";
        const string SorterIconTexture = "GridSchematics_Icon_Sorter";
        const string CalibrationTextFontId = "GridSchematics_Bahnschrift";
        static string InfoDrawerTextFontId = "GridSchematics_Mozart";
        static float CurrentHullScanAlpha = 1f;
        static float CurrentHullScanBrightness = 1f;
        static float CurrentShipBorderOpacity = 1f;
        static float CurrentSchematicAlpha = 1f;
        static string CurrentHullScanColorScale = GridSchematicsConfig.HullColorGreyscale;
        static string CurrentStorageColor = "GREEN";
        static string CurrentEffectorColor = "MAGENTA";
        static int CurrentSchematicMainHue;
        static int CurrentSchematicSecondaryHue;
        static int CurrentConveyorHue;
        static bool CurrentBlocksOccludeConveyors = true;
        static bool CurrentShowConnectedNetworks;
        static string CurrentUiFont = "MOZARTGLOW";
        static string CurrentTextFontId = "GridSchematics_MozartGlow";
        static float CurrentButtonTextScaleMultiplier = 1f;
        static float CurrentButtonTextBaselineNudge = 0f;
        static int CurrentPerfSpriteCount;
        static int CurrentPerfTextCount;
        static int CurrentPerfCachedSpriteCount;
        static int CurrentPerfCacheHits;
        static int CurrentPerfCacheMisses;
        static readonly Dictionary<string, CachedScanRuns> ScanRunCache = new Dictionary<string, CachedScanRuns>();
        static readonly Dictionary<string, CachedConveyorLineKeys> ActiveInventoryLineKeyCache = new Dictionary<string, CachedConveyorLineKeys>();
        static readonly Dictionary<string, CachedOverlaySources> OverlaySourceCache = new Dictionary<string, CachedOverlaySources>();
        static readonly Dictionary<string, CachedProjectionTransform> ProjectionTransformCache = new Dictionary<string, CachedProjectionTransform>();
        static readonly Dictionary<string, CachedOverlayInfo> OverlayInfoCache = new Dictionary<string, CachedOverlayInfo>();
        static readonly Dictionary<string, CachedBorderRects> ShipBorderCache = new Dictionary<string, CachedBorderRects>();
        static readonly Dictionary<string, CachedScreenRects> BlockGhostCache = new Dictionary<string, CachedScreenRects>();
        static readonly Dictionary<string, CachedConveyorProjection> ConveyorProjectionCache = new Dictionary<string, CachedConveyorProjection>();
        static int CacheUseCounter;
        static Vector2 CurrentSpriteViewportOrigin;
        static bool CurrentDynamicTextFitEnabled;

        static void BeginPerfFrame()
        {
            CurrentPerfSpriteCount = 0;
            CurrentPerfTextCount = 0;
            CurrentPerfCachedSpriteCount = 0;
            CurrentPerfCacheHits = 0;
            CurrentPerfCacheMisses = 0;
        }

        static void AddSprite(MySpriteDrawFrame frame, MySprite sprite)
        {
            CurrentPerfSpriteCount++;
            if (sprite.Type == SpriteType.TEXT)
                CurrentPerfTextCount++;
            if (CurrentSpriteViewportOrigin.X != 0f || CurrentSpriteViewportOrigin.Y != 0f)
                OffsetSpritePosition(ref sprite, CurrentSpriteViewportOrigin);
            CaptureLayoutSnapshotSprite(sprite);
            frame.Add(sprite);
        }

        static void AddCachedSprite(MySpriteDrawFrame frame, MySprite sprite)
        {
            CurrentPerfCachedSpriteCount++;
            AddSprite(frame, sprite);
        }

        static void TrackCacheHit()
        {
            CurrentPerfCacheHits++;
        }

        static void TrackCacheMiss()
        {
            CurrentPerfCacheMisses++;
        }

        static void OffsetSpritePosition(ref MySprite sprite, Vector2 offset)
        {
            if (!sprite.Position.HasValue)
                return;

            sprite.Position = sprite.Position.Value + offset;
        }

        static Vector2 GetSurfaceViewportOrigin(IMyTextSurface surface, Vector2 surfaceSize)
        {
            Vector2 textureSize = surfaceSize;
            try
            {
                if (surface != null)
                    textureSize = surface.TextureSize;
            }
            catch
            {
                textureSize = surfaceSize;
            }

            float x = (textureSize.X - surfaceSize.X) * 0.5f;
            float y = (textureSize.Y - surfaceSize.Y) * 0.5f;
            if (x < 0f)
                x = 0f;
            if (y < 0f)
                y = 0f;

            return new Vector2(x, y);
        }

        static void CommitPerfFrame(GridSchematicsLcdApp app, bool cursorOnly, int scanRaycastStep)
        {
            if (app == null || app.Ui == null || app.Ui.LastPerfStats == null)
                return;

            var stats = app.Ui.LastPerfStats;
            stats.SpriteCount = CurrentPerfSpriteCount;
            stats.TextCount = CurrentPerfTextCount;
            stats.CachedSpriteCount = CurrentPerfCachedSpriteCount;
            stats.CacheHits = CurrentPerfCacheHits;
            stats.CacheMisses = CurrentPerfCacheMisses;
            stats.RenderTickDelta = app.LastPanelRenderTickDelta;
            stats.RenderIntervalTicks = app.LastPanelRenderIntervalTicks;
            stats.ScanStep = scanRaycastStep;
            stats.ScanResolution = app.Config != null && app.Config.HighResScanning ? 512 : 256;
            stats.CursorOnly = cursorOnly;
            stats.MotionActive = app.IsViewportMotionActive;
        }

        public static void ClearRenderCaches()
        {
            ScanRunCache.Clear();
            ActiveInventoryLineKeyCache.Clear();
            OverlaySourceCache.Clear();
            ProjectionTransformCache.Clear();
            OverlayInfoCache.Clear();
            ShipBorderCache.Clear();
            BlockGhostCache.Clear();
            ConveyorProjectionCache.Clear();
            UiButtonVisualCache.Clear();
            CacheUseCounter = 0;
        }

        struct CachedScanRun
        {
            public int Y;
            public int StartX;
            public int EndX;
            public int Shade;
        }

        class CachedScanRuns
        {
            public List<CachedScanRun> Runs = new List<CachedScanRun>();
            public int LastUsed;
        }

        class CachedConveyorLineKeys
        {
            public HashSet<int> Keys = new HashSet<int>();
            public int LastUsed;
        }

        class CachedOverlaySources
        {
            public List<OverlayBlockSource> Sources = new List<OverlayBlockSource>();
            public int LastUsed;
        }

        class CachedProjectionTransform
        {
            public ProjectionTransform Transform;
            public int LastUsed;
        }

        class CachedOverlayInfo
        {
            public OverlayBlockInfo Info;
            public int Signature;
            public int LastUsed;
        }

        struct CachedLocalRect
        {
            public float X;
            public float Y;
            public float Width;
            public float Height;
        }

        class CachedBorderRects
        {
            public List<CachedLocalRect> Rects = new List<CachedLocalRect>();
            public int LastUsed;
        }

        struct CachedScreenRect
        {
            public Vector2 Center;
            public Vector2 Size;
            public Color Color;
            public string Texture;
        }

        class CachedScreenRects
        {
            public List<CachedScreenRect> Rects = new List<CachedScreenRect>();
            public int LastUsed;
        }

        struct CachedScreenLine
        {
            public Vector2 Start;
            public Vector2 End;
            public float Thickness;
            public float DashLength;
            public float GapLength;
            public Color Color;
            public bool Dashed;
        }

        class CachedConveyorProjection
        {
            public List<CachedScreenLine> Lines = new List<CachedScreenLine>();
            public List<CachedScreenRect> Points = new List<CachedScreenRect>();
            public int LastUsed;
        }

        public static void RenderPanel(GridSchematicsLcdApp app)
        {
            RenderPanel(app, false);
        }

        public static void RenderPanel(GridSchematicsLcdApp app, bool isCursorOnlyRender)
        {
            if (app.OwnerBlock == null)
                return;

            var surface = app.Surface;
            if (surface == null)
                return;

            try
            {
                surface.ContentType = ContentType.SCRIPT;
                surface.ScriptForegroundColor = new Color(255, 255, 255);
                surface.ScriptBackgroundColor = new Color(0, 0, 0);
            }
            catch
            {
                // Some text surfaces may not expose all script panel properties.
            }

            if (!RenderSpriteSurface(app, surface, isCursorOnlyRender))
            {
                RenderTextSurface(app, surface);
            }
        }

        static bool RenderSpriteSurface(GridSchematicsLcdApp app, IMyTextSurface surface, bool isCursorOnlyRender)
        {
            try
            {
                SetUiPalette(app.Config.UiPalette, app.Config.UiHueShift, app.Config.UiBrightness, app.Config.UiAlpha, app.Config.UiAccentHueShift, app.Config.UiPanelBrightness, app.Config.UiPanelAlpha, app.Config.UiSaturation, app.Config.UiAccentBrightness, app.Config.UiAccentSaturation);
                ApplyGridVisibilityLevel(app.Ui.GridVisibilityLevel);
                CurrentHullScanAlpha = app.Config.HullScanAlpha;
                CurrentHullScanBrightness = app.Ui.HullScanBrightness;
                CurrentShipBorderOpacity = app.Ui.ShipBorderOpacity;
                CurrentSchematicAlpha = app.Config.SchematicAlpha;
                CurrentHullScanColorScale = GridSchematicsConfig.NormalizeHullScanColorScale(app.Config.HullScanColorScale);
                CurrentStorageColor = app.Config.StorageColor;
                CurrentEffectorColor = app.Config.EffectorColor;
                CurrentSchematicMainHue = app.Config.SchematicMainHue;
                CurrentSchematicSecondaryHue = app.Config.SchematicSecondaryHue;
                CurrentConveyorHue = app.Config.ConveyorHue;
                CurrentBlocksOccludeConveyors = app.Config.BlocksOccludeConveyors;
                CurrentShowConnectedNetworks = app.Config.ShowConnectedNetworks;
                CurrentUiFont = GridSchematicsConfig.NormalizeUiFont(app.Config.UiFont);
                CurrentTextFontId = GridSchematicsConfig.GetUiFontSubtype(CurrentUiFont);
                InfoDrawerTextFontId = GetInfoDrawerFontSubtype(CurrentUiFont);
                ApplyUiFontRenderProfile(CurrentUiFont);
                var size = surface.SurfaceSize;
                CurrentDynamicTextFitEnabled = !UiLayout.BuildSurfaceProfile((int)size.X, (int)size.Y).IsCanonical512Square;
                CurrentSpriteViewportOrigin = GetSurfaceViewportOrigin(surface, size);
                var zones = app.Ui.ChromeHidden
                    ? new ScreenZones(
                        new ScreenZone(ScreenZoneType.TopRow, 0, 0, (int)size.X, 0),
                        new ScreenZone(ScreenZoneType.LeftRail, 0, 0, 0, (int)size.Y),
                        new ScreenZone(ScreenZoneType.RightRail, (int)size.X, 0, 0, (int)size.Y),
                        new ScreenZone(ScreenZoneType.BottomStrip, 0, (int)size.Y, (int)size.X, 0),
                        new ScreenZone(ScreenZoneType.CenterViewport, 0, 0, (int)size.X, (int)size.Y))
                    : UiLayout.BuildZones((int)size.X, (int)size.Y);
                bool allowInfoPanel = app.SupportsInfoPanel;
                bool fullInfoPanel = app.SupportsFullInfoPanel;
                var infoPanelZone = allowInfoPanel && app.Ui.ShowInfoPanel && !app.Ui.SegmentMode && !app.Ui.ChromeHidden
                    ? BuildInfoPanelZone(zones.Center, app, fullInfoPanel)
                    : new ScreenZone(ScreenZoneType.CenterViewport, 0, 0, 0, 0);
                var mapZone = allowInfoPanel && app.Ui.ShowInfoPanel && !app.Ui.SegmentMode && !app.Ui.ChromeHidden
                    ? BuildInfoMapZone(zones.Center, infoPanelZone)
                    : zones.Center;
                app.Ui.RenderViewportZone = mapZone;
                var renderCenter = UiLayout.BuildTransformedCenterZone(mapZone, app.Ui.ZoomLevel, app.Ui.PanX, app.Ui.PanY);
                BeginPerfFrame();
                BeginLayoutSnapshotFrame(app, surface, size);

                using (var frame = surface.DrawFrame())
                {
                    AddSprite(frame, new MySprite(
                        SpriteType.TEXTURE,
                        "SquareSimple",
                        size * 0.5f,
                        size,
                        Color.Black
                    ));

                    if (app.Session != null && app.Session.IsPanelCursorDepthOffsetCalibrationActive)
                    {
                        DrawPanelCursorDepthOffsetCalibrationPanel(frame, new ScreenZone(ScreenZoneType.CenterViewport, 0, 0, (int)size.X, (int)size.Y), app, surface);
                        CommitPerfFrame(app, isCursorOnlyRender, 1);
                        return true;
                    }

                    if (app.IsLowLevelCalibrationBlocking)
                    {
                        DrawLowLevelCalibrationPanel(frame, new ScreenZone(ScreenZoneType.CenterViewport, 0, 0, (int)size.X, (int)size.Y), app, surface);
                        DrawManualCalibrationDebugPopup(frame, new ScreenZone(ScreenZoneType.CenterViewport, 0, 0, (int)size.X, (int)size.Y), app);
                        CommitPerfFrame(app, isCursorOnlyRender, 1);
                        return true;
                    }

                    if (app.IsCursorCalibrationBlocking)
                    {
                        DrawCalibrationModal(frame, new ScreenZone(ScreenZoneType.CenterViewport, 0, 0, (int)size.X, (int)size.Y), app.Ui, app.TouchInput.HoverRegionId);
                        DrawManualCalibrationDebugPopup(frame, new ScreenZone(ScreenZoneType.CenterViewport, 0, 0, (int)size.X, (int)size.Y), app);
                        if (!app.TouchInput.IsAimCursorActive)
                            DrawCursor(frame, app.TouchInput, new ScreenZone(ScreenZoneType.CenterViewport, 0, 0, (int)size.X, (int)size.Y));
                        CommitPerfFrame(app, isCursorOnlyRender, 1);
                        return true;
                    }

                    AddSprite(frame, new MySprite(
                        SpriteType.TEXTURE,
                        "SquareSimple",
                        new Vector2(zones.Top.Width / 2f, zones.Top.Height / 2f),
                        new Vector2(zones.Top.Width, zones.Top.Height),
                        Color.Black
                    ));

                    var activeScanView = app.Config.GetScanView();
                    int rotationSteps = app.Config.GetRotationSteps();
                    var rootGrid = app.OwnerBlock != null ? app.OwnerBlock.CubeGrid : null;
                    ShipGrid activeShipGrid = null;
                    if (!app.Ui.SegmentMode)
                    {
                        activeShipGrid = app.ConstructCache != null && rootGrid != null
                        ? app.ConstructCache.UpdateConstructProjection(rootGrid, activeScanView, false)
                        : app.ConstructCache?.ShipGrid;
                    }
                    app.Ui.OverlayBlockRegions.Clear();

                    bool cursorOnly = isCursorOnlyRender;
                    bool drawModeOverlays = app.Ui.ActiveOverlay != OverlayMode.None || app.TouchInput == null || 
                        app.TouchInput.IsPressed || app.TouchInput.JustPressed || app.Ui.SelectedOverlayBlockId != null;
                    bool drawDebugGrid = app.Ui.ShowDebugGrid;
                    bool drawReference = app.Ui.ShowReferenceLines;
                    int scanRaycastStep = app.Config.PerformanceMode && app.IsViewportMotionActive ? SegmentRaycastRenderStep : 1;

                    if (app.Ui.SegmentMode)
                    {
                        DrawSegmentModePanel(frame, zones.Center, app, rootGrid, cursorOnly);
                    }
                    else
                    {
                        bool drawDebugBlocks = app.Ui.ShowDiscoveredBlocks;
                        DrawShipGridOverlay(frame, renderCenter, activeShipGrid, app.ConstructCache?.GetRaycastData(activeScanView), app.Config.FillMode, drawDebugBlocks, app.Ui.ShowShipBorder, app.Ui.ShowHullScan, drawDebugGrid, drawReference, app.Ui.BlurScanRender, app.Config.ShowDebug, rotationSteps, scanRaycastStep);
                        DrawShipReferenceMarkers(frame, renderCenter, activeShipGrid, app.ConstructCache, app.OwnerBlock, app.Ui.ShowCenterOfMassMarker, app.Ui.ShowPanelPositionMarker, rotationSteps);
                        DrawDockedMobileGridBorders(frame, renderCenter, activeShipGrid, app.ConstructCache, true, app.Ui.ShowShipBorder, rotationSteps);
                        if (app.Ui.ShowConveyorOverlay)
                            DrawConveyorOverlay(frame, renderCenter, activeShipGrid, app.ConstructCache, rotationSteps, app.Ui.ShowAllConnections);
                        bool occludeConveyorUnderFillBars = app.Ui.ShowConveyorOverlay && app.Config.BlocksOccludeConveyors;
                        if (app.Ui.ActiveOverlay == OverlayMode.Cargo && drawModeOverlays)
                            DrawCargoOverlay(frame, renderCenter, activeShipGrid, app.ConstructCache, rotationSteps, app.Ui.ShowAllConnections, app.TouchInput, app.Ui, occludeConveyorUnderFillBars);
                        if (app.Ui.ActiveOverlay == OverlayMode.Engines && app.IsThrustOverlayAvailable() && drawModeOverlays)
                            DrawEnginesOverlay(frame, renderCenter, activeShipGrid, app.ConstructCache, rotationSteps, app.Ui.ShowAllConnections, app.TouchInput, app.Ui, occludeConveyorUnderFillBars);
                        if (app.Ui.ActiveOverlay == OverlayMode.Oxygen)
                            DrawOxygenOverlay(frame, renderCenter, activeShipGrid, app.ConstructCache, rotationSteps, app.Ui.ShowAllConnections);
                        if (app.Ui.ActiveOverlay == OverlayMode.Power && drawModeOverlays)
                            DrawPowerOverlay(frame, renderCenter, activeShipGrid, app.ConstructCache, rotationSteps, app.Ui.ShowAllConnections, app.TouchInput, app.Ui, occludeConveyorUnderFillBars);
                        DrawGridNameLabel(frame, mapZone, rootGrid, app.ConstructCache);
                        UpdateAndDrawBlockStackPreview(frame, mapZone, renderCenter, activeShipGrid, app.TouchInput, app.Ui, rotationSteps);
                    bool drawInfoPanel = true;
                    if (allowInfoPanel && app.Ui.ShowInfoPanel && !app.Ui.ChromeHidden && drawInfoPanel)
                        DrawSystemsInfoPanel(frame, infoPanelZone, app, cursorOnly);
                    }

                    if (zones.Left.Width > 0)
                    {
                        AddSprite(frame, new MySprite(
                            SpriteType.TEXTURE,
                            "SquareSimple",
                            new Vector2(zones.Left.Width / 2f, zones.Left.Y + zones.Left.Height / 2f),
                            new Vector2(zones.Left.Width, zones.Left.Height),
                            Color.Black
                        ));
                    }

                    if (zones.Right.Width > 0)
                    {
                        AddSprite(frame, new MySprite(
                            SpriteType.TEXTURE,
                            "SquareSimple",
                            new Vector2(zones.Right.X + zones.Right.Width / 2f, zones.Right.Y + zones.Right.Height / 2f),
                            new Vector2(zones.Right.Width, zones.Right.Height),
                            Color.Black
                        ));
                    }

                    AddSprite(frame, new MySprite(
                        SpriteType.TEXTURE,
                        "SquareSimple",
                        new Vector2(size.X / 2f, zones.Bottom.Y + zones.Bottom.Height / 2f),
                        new Vector2(zones.Bottom.Width, zones.Bottom.Height),
                        Color.Black
                    ));

                    var conveyorSummary = app.ConstructCache?.ConveyorNetwork != null
                        ? $"Conveyor nodes: {app.ConstructCache.ConveyorNetwork.Nodes.Count}, ports: {app.ConstructCache.ConveyorNetwork.Ports.Count}, lines: {app.ConstructCache.ConveyorNetwork.Lines.Count}, edges: {app.ConstructCache.ConveyorNetwork.Edges.Count}"
                        : "Conveyor topology: not scanned yet";

                    var scanData = app.ConstructCache?.GetRaycastData(activeScanView);
                    var scanSummary = "Scan: unavailable";
                    if (scanData != null)
                    {
                        scanSummary = scanData.IsReady
                            ? $"Scan ready: {scanData.HitSampleCount} samples, max {scanData.MaxHitCount}/{scanData.MaxThickness}"
                            : "Scan pending";
                    }

                    if (app.Config.ShowDebug)
                        DrawRenderDebugPanel(frame, surface, size, mapZone, zones.Center, renderCenter, app, activeShipGrid, scanData, rotationSteps, scanRaycastStep, conveyorSummary, scanSummary);
                    if (app.Config.ShowPerfStats)
                        DrawPerformanceStatsLine(frame, mapZone, app.Ui.LastPerfStats);

                    DrawBootLoadingPanel(frame, zones.Center, app);
                    DrawScanProgressPanel(frame, zones.Center, app.ActiveScanJob);
                    if (app.Ui.ChromeHidden)
                    {
                        DrawChromeRestoreButton(frame, (int)size.X, (int)size.Y, true, app.TouchInput.HoverRegionId);
                    }
                    else
                    {
                        DrawBottomSchematicButtons(frame, (int)size.X, (int)size.Y, app.Ui.ActiveOverlay, app.Ui.ShowConveyorOverlay, app.Ui.ShowInfoPanel, app.Ui.InfoPanelMode, app.Ui.ActiveMenu, app.Config.FillMode, app.TouchInput.HoverRegionId, app.IsThrustOverlayAvailable());
                        DrawTopMenu(frame, (int)size.X, (int)size.Y, app.Ui.ActiveMenu, app.Config.View, app.Ui.ShowDiscoveredBlocks, app.Ui.ShowShipBorder, app.Ui.ShowHullScan, app.Ui.ShowDebugGrid, app.Config.ShowDebug, app.Config.ShowPerfStats, app.Ui.ShowReferenceLines, app.Ui.ShowCenterOfMassMarker, app.Ui.ShowPanelPositionMarker, app.Ui.ShowDockedMobileGrids, app.Ui.ShowConveyorOverlay, app.Ui.ShowFillBars, app.Ui.ShowAllConnections, app.Ui.BlurScanRender, app.Config.FillMode, app.Config.UiPalette, app.Config.UiHueShift, app.Config.UiBrightness, app.Config.UiSaturation, app.Config.UiAlpha, app.Config.UiAccentHueShift, app.Config.UiAccentBrightness, app.Config.UiAccentSaturation, app.Config.UiPanelBrightness, app.Config.UiPanelAlpha, app.Config.SchematicMainHue, app.Config.SchematicSecondaryHue, app.Config.ConveyorHue, app.Config.HullScanAlpha, app.Config.SchematicAlpha, app.Config.StorageColor, app.Config.EffectorColor, app.Ui.SegmentMode, app.MouseControlEnabled, app.MouseSensitivity, app.Config.AllowGridRotation, app.Config.PerformanceMode, app.Config.HighResScanning, app.Ui.SettingsExpandedMask, app.Ui.ActiveSettingsActionId, app.TouchInput.HoverRegionId);
                    }
                    DrawCalibrationModal(frame, zones.Center, app.Ui, app.TouchInput.HoverRegionId);
                    DrawManualCalibrationDebugPopup(frame, zones.Center, app);
                    if (!app.TouchInput.IsAimCursorActive)
                        DrawCursor(frame, app.TouchInput, new ScreenZone(ScreenZoneType.CenterViewport, 0, 0, (int)size.X, (int)size.Y));
                    CommitPerfFrame(app, cursorOnly, scanRaycastStep);
                }

                return true;
            }
            catch
            {
                CancelLayoutSnapshotFrame();
                return false;
            }
            finally
            {
                CompleteLayoutSnapshotFrame();
                CurrentSpriteViewportOrigin = Vector2.Zero;
                CurrentDynamicTextFitEnabled = false;
            }
        }

        static string GetInfoDrawerFontSubtype(string uiFont)
        {
            uiFont = GridSchematicsConfig.NormalizeUiFont(uiFont);
            if (uiFont == "MOZARTGLOW")
                return GridSchematicsConfig.GetUiFontSubtype("MOZART");
            if (uiFont == "TELEGRAMAGLOW")
                return GridSchematicsConfig.GetUiFontSubtype("TELEGRAMA");
            return GridSchematicsConfig.GetUiFontSubtype(uiFont);
        }

        static void ApplyUiFontRenderProfile(string uiFont)
        {
            CurrentButtonTextScaleMultiplier = 1f;
            CurrentButtonTextBaselineNudge = 0f;

            if (uiFont == "BAHNSCHRIFT")
            {
                CurrentButtonTextScaleMultiplier = 0.78f;
                CurrentButtonTextBaselineNudge = -3f;
            }
            else if (uiFont == "CRYSRG")
            {
                CurrentButtonTextBaselineNudge = -4f;
            }
            else if (uiFont == "MONOM" || uiFont == "MONOMHARD")
            {
                CurrentButtonTextBaselineNudge = -3f;
            }
            else if (uiFont == "MOZART" || uiFont == "MOZARTGLOW")
            {
                CurrentButtonTextBaselineNudge = -4f;
            }
            else if (uiFont == "TELEGRAMA" || uiFont == "TELEGRAMAGLOW")
            {
                CurrentButtonTextBaselineNudge = -3f;
            }
        }

        static void DrawRenderDebugPanel(MySpriteDrawFrame frame, IMyTextSurface surface, Vector2 surfaceSize, ScreenZone mapZone, ScreenZone centerZone, ScreenZone renderCenter, GridSchematicsLcdApp app, ShipGrid activeShipGrid, RawRaycastScanData scanData, int rotationSteps, int scanRaycastStep, string conveyorSummary, string scanSummary)
        {
            float panelHeight = Math.Min(mapZone.Height * 0.34f, 150f);
            if (panelHeight < 104f)
                panelHeight = 104f;
            float panelWidth = mapZone.Width;
            var panelCenter = new Vector2(mapZone.X + panelWidth * 0.5f, mapZone.Y + panelHeight * 0.5f);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", panelCenter, new Vector2(panelWidth, panelHeight), new Color(0, 0, 0, 224)));
            DrawScreenRectBorder(frame, panelCenter, new Vector2(panelWidth, panelHeight), UiAccentDim);

            float x0 = mapZone.X + 8f;
            float x1 = mapZone.X + panelWidth * 0.50f + 6f;
            float y0 = mapZone.Y + 9f;
            float line = 10.5f;
            float small = 0.38f;
            float head = 0.44f;
            var text = Color.White;
            var muted = new Color(190, 205, 212, 235);
            var cyan = UiAccentBright;

            DrawDebugText(frame, "RENDER DEBUG", x0, y0, cyan, head);
            DrawDebugText(frame, "SCAN / DRAW", x1, y0, cyan, head);

            DrawDebugText(frame, "MODE " + app.Ui.ActiveDisplayMode + "  VIEW " + app.Config.View + "  ROT " + rotationSteps, x0, y0 + line * 1f, text, small);
            DrawDebugText(frame, "OVERLAY " + app.Ui.ActiveOverlay + "  FILL " + app.Config.FillMode, x0, y0 + line * 2f, muted, small);
            DrawDebugText(frame, "SURF " + FormatVector(surfaceSize) + "  TEX " + FormatVector(surface != null ? surface.TextureSize : Vector2.Zero), x0, y0 + line * 3f, muted, small);
            DrawDebugText(frame, "VIEWPORT " + centerZone.Width + "x" + centerZone.Height + "  RENDER " + renderCenter.Width + "x" + renderCenter.Height, x0, y0 + line * 4f, muted, small);
            DrawDebugText(frame, "ZOOM " + app.Ui.ZoomLevel + "  PAN " + app.Ui.PanX + "," + app.Ui.PanY + "  MOTION " + OnOff(app.IsViewportMotionActive), x0, y0 + line * 5f, muted, small);
            int previewCount = app.Ui.PreviewBlockStackItems != null ? app.Ui.PreviewBlockStackItems.Count : 0;
            DrawDebugText(frame, "GRID " + OnOff(app.Ui.ShowDebugGrid) + "  REF " + OnOff(app.Ui.ShowReferenceLines) + "  FILLBARS " + app.Ui.FillBarsVisibilityLevel + "  PREVIEW " + previewCount, x0, y0 + line * 6f, muted, small);
            DrawDebugText(frame, ShortenDebugText(conveyorSummary, 44), x0, y0 + line * 7f, muted, small);
            DrawDebugText(frame, FormatPanelSurfaceCandidate(app.TouchInput != null ? app.TouchInput.LastPanelSurfaceCandidate : new PanelSurfaceCandidate()), x0, y0 + line * 8f, muted, small);

            string scanReady = scanData != null && scanData.IsReady ? "YES" : "NO";
            int scanRes = scanData != null ? scanData.Resolution : 0;
            DrawDebugText(frame, "SCAN res " + scanRes + "  ready " + scanReady + "  " + ShortenDebugText(scanSummary, 34), x1, y0 + line * 1f, text, small);
            DrawDebugText(frame, "DRAW step " + scanRaycastStep + "  smooth " + OnOff(app.Ui.BlurScanRender) + "  perf " + OnOff(app.Config.PerformanceMode) + "  high " + OnOff(app.Config.HighResScanning), x1, y0 + line * 2f, text, small);

            if (scanData != null && activeShipGrid != null && !activeShipGrid.IsEmpty)
            {
                var debugTransform = GetOrBuildProjectionTransform(activeShipGrid, renderCenter, rotationSteps);
                if (debugTransform.IsValid)
                {
                    float spanX = Math.Max(0.001f, scanData.SampleMaxX - scanData.SampleMinX);
                    float spanY = Math.Max(0.001f, scanData.SampleMaxY - scanData.SampleMinY);
                    float samplePxX = spanX / Math.Max(1f, scanData.Resolution) * debugTransform.CellSize;
                    float samplePxY = spanY / Math.Max(1f, scanData.Resolution) * scanRaycastStep * debugTransform.CellSize;
                    DrawDebugText(frame, "CELL " + debugTransform.CellSize.ToString("0.00") + "px  SAMPLE " + samplePxX.ToString("0.00") + " x " + samplePxY.ToString("0.00") + "px", x1, y0 + line * 3f, muted, small);
                }

                float localScanMinX = scanData.SampleMinX - activeShipGrid.Min2D.X + 0.5f;
                float localScanMaxX = scanData.SampleMaxX - activeShipGrid.Min2D.X + 0.5f;
                float localScanMinY = activeShipGrid.Max2D.Y - scanData.SampleMaxY + 0.5f;
                float localScanMaxY = activeShipGrid.Max2D.Y - scanData.SampleMinY + 0.5f;
                DrawDebugText(frame, "LOCAL X " + localScanMinX.ToString("0.0") + ".." + localScanMaxX.ToString("0.0") + " / " + activeShipGrid.Size2D.X + "  Y " + localScanMinY.ToString("0.0") + ".." + localScanMaxY.ToString("0.0") + " / " + activeShipGrid.Size2D.Y, x1, y0 + line * 4f, muted, small);
                DrawDebugText(frame, "BASIS " + ShortenDebugText(activeShipGrid.ReferenceName, 18) + "  " + (activeShipGrid.UsesRemoteReference ? "RC" : "GRID") + "  id " + activeShipGrid.BasisGridEntityId, x1, y0 + line * 5f, muted, small);
            }

            var ray = app.ConstructCache != null ? app.ConstructCache.LastRaycastDiagnostics : null;
            if (ray != null)
            {
                DrawDebugText(frame, "RAY " + ray.View + " targets g" + ray.HullTargetGridCount + " c" + ray.ConnectorTargetGridCount + " rays " + ray.RaysCast, x1, y0 + line * 6f, muted, small);
                DrawDebugText(frame, "HITS phys " + ray.RaysWithPhysicsHit + "/" + ray.PhysicsHitCount + " accepted " + ray.RaysWithAcceptedHit + "/" + ray.AcceptedHitCount + " conn " + ray.AcceptedConnectorHitCount, x1, y0 + line * 7f, muted, small);
            }
        }

        static void DrawDebugText(MySpriteDrawFrame frame, string text, float x, float y, Color color, float scale)
        {
            AddSprite(frame, new MySprite(SpriteType.TEXT, text ?? string.Empty, new Vector2(x, y), null, color, CurrentTextFontId, TextAlignment.LEFT, scale));
        }

        static string FormatPanelSurfaceCandidate(PanelSurfaceCandidate candidate)
        {
            if (!candidate.IsValid)
                return "SURFACE CAND none";

            string quality = candidate.Quality.ToString().ToUpperInvariant();
            string status = ShortenDebugText(candidate.StatusText ?? string.Empty, 22);
            return "SURFACE CAND " + quality +
                " " + candidate.Score.ToString("0") +
                " " + candidate.Width.ToString("0.00") + "x" + candidate.Height.ToString("0.00") +
                " " + status;
        }

        static void DrawPerformanceStatsLine(MySpriteDrawFrame frame, ScreenZone mapZone, PanelPerfStats stats)
        {
            if (stats == null || mapZone.Width <= 0 || mapZone.Height <= 0)
                return;

            string mode = stats.CursorOnly ? "cur" : "full";
            string motion = stats.MotionActive ? "mot" : "idle";
            string text = "PERF " + mode + " " + stats.RenderTickDelta + "t/" + stats.RenderIntervalTicks + "t  " +
                "SPR " + stats.SpriteCount + " TXT " + stats.TextCount + " CSPR " + stats.CachedSpriteCount + "  " +
                "CACHE " + stats.CacheHits + "/" + stats.CacheMisses + "  " +
                "SCAN " + stats.ScanResolution + " s" + stats.ScanStep + " " + motion;

            float x = mapZone.X + 6f;
            float y = mapZone.Y + mapZone.Height - 14f;
            if (y < mapZone.Y + 4f)
                y = mapZone.Y + 4f;

            AddSprite(frame, new MySprite(
                SpriteType.TEXT,
                text,
                SnapPoint(new Vector2(x, y)),
                null,
                UiAccentBright,
                CurrentTextFontId,
                TextAlignment.LEFT,
                0.36f));
        }

        static string FormatVector(Vector2 value)
        {
            return ((int)value.X).ToString() + "x" + ((int)value.Y).ToString();
        }

        static string OnOff(bool value)
        {
            return value ? "ON" : "OFF";
        }

        static string ShortenDebugText(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text) || text.Length <= maxChars)
                return text ?? string.Empty;
            if (maxChars <= 3)
                return text.Substring(0, maxChars);
            return text.Substring(0, maxChars - 3) + "...";
        }

        static void DrawSegmentModePanel(MySpriteDrawFrame frame, ScreenZone center, GridSchematicsLcdApp app, IMyCubeGrid rootGrid, bool cursorOnlyRender)
        {
            AddSprite(frame, new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                new Vector2(center.X + center.Width * 0.5f, center.Y + center.Height * 0.5f),
                new Vector2(center.Width, center.Height),
                Color.Black
            ));

            int gap = 1;
            int leftWidth = center.Width / 2;
            int rightWidth = center.Width - leftWidth - gap;
            int topHeight = center.Height / 2;
            int bottomHeight = center.Height - topHeight - gap;

            var frontZone = new ScreenZone(ScreenZoneType.CenterViewport, center.X, center.Y, Math.Max(1, leftWidth), Math.Max(1, topHeight));
            var leftZone = new ScreenZone(ScreenZoneType.CenterViewport, center.X + leftWidth + gap, center.Y, Math.Max(1, rightWidth), Math.Max(1, topHeight));
            var topZone = new ScreenZone(ScreenZoneType.CenterViewport, center.X, center.Y + topHeight + gap, Math.Max(1, leftWidth), Math.Max(1, bottomHeight));
            var infoZone = new ScreenZone(ScreenZoneType.CenterViewport, center.X + leftWidth + gap, center.Y + topHeight + gap, Math.Max(1, rightWidth), Math.Max(1, bottomHeight));

            DrawSegmentProjection(frame, frontZone, app, app.Ui.SegmentFrontGrid, ScanView.Front, app.Config.RotationFront, "FRONT", cursorOnlyRender);
            DrawSegmentProjection(frame, leftZone, app, app.Ui.SegmentLeftGrid, ScanView.Side, app.Config.RotationLeft, "SIDE", cursorOnlyRender);
            DrawSegmentProjection(frame, topZone, app, app.Ui.SegmentTopGrid, ScanView.Top, app.Config.RotationTop, "TOP", cursorOnlyRender);
            DrawSegmentInfoPanel(frame, infoZone, app);
        }

        static void DrawSegmentProjection(MySpriteDrawFrame frame, ScreenZone zone, GridSchematicsLcdApp app, ShipGrid shipGrid, ScanView view, int rotationSteps, string label, bool cursorOnlyRender)
        {
            int raycastStep = app.Config.PerformanceMode ? SegmentRaycastRenderStep : 1;
            DrawShipGridOverlay(frame, zone, shipGrid, app.ConstructCache?.GetRaycastData(view), app.Config.FillMode, false, app.Ui.ShowShipBorder, app.Ui.ShowHullScan, false, false, app.Ui.BlurScanRender, false, rotationSteps, raycastStep);
            if (app.Ui.ShowDebugGrid)
                DrawSegmentBlockAlignedGrid(frame, zone, shipGrid, rotationSteps);
            if (app.Ui.ShowReferenceLines)
                DrawSegmentReferenceLines(frame, zone, shipGrid, rotationSteps);
            if (app.Ui.ShowConveyorOverlay)
                DrawConveyorOverlay(frame, zone, shipGrid, app.ConstructCache, rotationSteps, app.Ui.ShowAllConnections);
            DrawSegmentVisionOverlay(frame, zone, app, shipGrid, rotationSteps);

            var panelCenter = new Vector2(zone.X + zone.Width * 0.5f, zone.Y + zone.Height * 0.5f);
            DrawScreenRectBorder(frame, panelCenter, new Vector2(zone.Width, zone.Height), UiAccentDim);

            AddSprite(frame, new MySprite(
                SpriteType.TEXT,
                label,
                new Vector2(zone.X + 8f, zone.Y + 8f),
                null,
                UiTextMuted,
                CurrentTextFontId,
                TextAlignment.LEFT,
                0.55f
            ));
        }

        static void DrawSegmentBlockAlignedGrid(MySpriteDrawFrame frame, ScreenZone zone, ShipGrid shipGrid, int rotationSteps)
        {
            if (shipGrid == null || shipGrid.IsEmpty)
                return;

            var transform = GetOrBuildProjectionTransform(shipGrid, zone, rotationSteps);
            if (!transform.IsValid || transform.CellSize <= 0f)
                return;

            float spacing = transform.CellSize;
            if (spacing < 3f)
            {
                int multiplier = (int)Math.Ceiling(3f / spacing);
                spacing *= Math.Max(1, multiplier);
            }

            float left = zone.X;
            float right = zone.X + zone.Width;
            float top = zone.Y;
            float bottom = zone.Y + zone.Height;
            float originX = transform.Origin.X;
            float originY = transform.Origin.Y;

            int firstVertical = (int)Math.Floor((left - originX) / spacing) - 1;
            int lastVertical = (int)Math.Ceiling((right - originX) / spacing) + 1;
            for (int i = firstVertical; i <= lastVertical; i++)
            {
                float x = SnapPixel(originX + i * spacing);
                bool major = i % 5 == 0;
                DrawScreenLine(frame, new Vector2(x, top), new Vector2(x, bottom), 1f, major ? UiDebugGridMajor : UiDebugGridMinor);
            }

            int firstHorizontal = (int)Math.Floor((top - originY) / spacing) - 1;
            int lastHorizontal = (int)Math.Ceiling((bottom - originY) / spacing) + 1;
            for (int i = firstHorizontal; i <= lastHorizontal; i++)
            {
                float y = SnapPixel(originY + i * spacing);
                bool major = i % 5 == 0;
                DrawScreenLine(frame, new Vector2(left, y), new Vector2(right, y), 1f, major ? UiDebugGridMajor : UiDebugGridMinor);
            }
        }

        static void DrawSegmentReferenceLines(MySpriteDrawFrame frame, ScreenZone zone, ShipGrid shipGrid, int rotationSteps)
        {
            if (shipGrid == null || shipGrid.IsEmpty)
                return;

            var transform = GetOrBuildProjectionTransform(shipGrid, zone, rotationSteps);
            if (!transform.IsValid || transform.CellSize <= 0f)
                return;

            float localX = 0f - shipGrid.Min2D.X + 0.5f;
            float localY = shipGrid.Max2D.Y - 0f + 0.5f;

            if (localX >= 0f && localX <= transform.SourceWidth)
            {
                var start = transform.ProjectLocalPoint(localX, 0f);
                var end = transform.ProjectLocalPoint(localX, transform.SourceHeight);
                DrawClippedScreenLine(frame, zone, start, end, 2f, UiReferenceAxis);
            }

            if (localY >= 0f && localY <= transform.SourceHeight)
            {
                var start = transform.ProjectLocalPoint(0f, localY);
                var end = transform.ProjectLocalPoint(transform.SourceWidth, localY);
                DrawClippedScreenLine(frame, zone, start, end, 2f, UiReferenceAxis);
            }
        }

        static void DrawSegmentVisionOverlay(MySpriteDrawFrame frame, ScreenZone zone, GridSchematicsLcdApp app, ShipGrid shipGrid, int rotationSteps)
        {
            if (app == null || app.ConstructCache == null || shipGrid == null || shipGrid.IsEmpty)
                return;

            if (app.Ui.ActiveOverlay == OverlayMode.Cargo)
                DrawCargoOverlay(frame, zone, shipGrid, app.ConstructCache, rotationSteps, app.Ui.ShowAllConnections, null, app.Ui, app.Ui.ShowConveyorOverlay && app.Config.BlocksOccludeConveyors);
            else if (app.Ui.ActiveOverlay == OverlayMode.Engines)
                DrawEnginesOverlay(frame, zone, shipGrid, app.ConstructCache, rotationSteps, app.Ui.ShowAllConnections, null, app.Ui, app.Ui.ShowConveyorOverlay && app.Config.BlocksOccludeConveyors);
            else if (app.Ui.ActiveOverlay == OverlayMode.Oxygen)
                DrawOxygenOverlay(frame, zone, shipGrid, app.ConstructCache, rotationSteps, app.Ui.ShowAllConnections);
            else if (app.Ui.ActiveOverlay == OverlayMode.Power)
                DrawPowerOverlay(frame, zone, shipGrid, app.ConstructCache, rotationSteps, app.Ui.ShowAllConnections, null, app.Ui, app.Ui.ShowConveyorOverlay && app.Config.BlocksOccludeConveyors);
        }

        static void DrawSegmentInfoPanel(MySpriteDrawFrame frame, ScreenZone zone, GridSchematicsLcdApp app)
        {
            var metrics = UiLayout.BuildMetrics(zone.Width, zone.Height);
            var center = new Vector2(zone.X + zone.Width * 0.5f, zone.Y + zone.Height * 0.5f);
            var size = new Vector2(zone.Width, zone.Height);
            AddSprite(frame, new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                center,
                size,
                UiPanelFillSoft
            ));
            DrawScreenRectBorder(frame, center, size, UiAccentDim);

            AddSprite(frame, new MySprite(
                SpriteType.TEXT,
                "SEGMENT",
                new Vector2(zone.X + 12f, zone.Y + 16f),
                null,
                UiAccentBright,
                CurrentTextFontId,
                TextAlignment.LEFT,
                metrics.LargeText
            ));

            var controls = UiLayout.BuildSegmentScanControlRegions(zone);
            for (int i = 0; i < controls.Length; i++)
            {
                var region = controls[i];
                bool active = false;
                if (region.Id == UiLayout.SetDensityId)
                    active = string.Equals(app.Config.FillMode, GridSchematicsConfig.FillDensity, StringComparison.OrdinalIgnoreCase);
                else if (region.Id == UiLayout.SetThicknessId)
                    active = string.Equals(app.Config.FillMode, GridSchematicsConfig.FillThickness, StringComparison.OrdinalIgnoreCase);
                else if (region.Id == UiLayout.SetVoidsId)
                    active = string.Equals(app.Config.FillMode, GridSchematicsConfig.FillVoids, StringComparison.OrdinalIgnoreCase);
                else if (region.Id == UiLayout.ToggleBlurId)
                    active = app.Ui.BlurScanRender;

                string label = region.Id == UiLayout.SetDensityId ? "DENSITY" :
                    region.Id == UiLayout.SetThicknessId ? "DEPTH" :
                    region.Id == UiLayout.SetVoidsId ? "VOIDS" :
                    region.Id == UiLayout.ToggleBlurId ? "SMOOTH" :
                    region.Id == UiLayout.RunScanId ? "RUN SCAN" : region.Hint;
                DrawViewButton(frame, region, label, active, string.Equals(app.TouchInput.HoverRegionId, region.Id, StringComparison.Ordinal));
            }
        }

        static void DrawScanProgressPanel(MySpriteDrawFrame frame, ScreenZone zone, ScanCache.IncrementalRaycastScanJob job)
        {
            if (job == null || job.IsComplete)
                return;

            float panelWidth = Math.Min(320f, Math.Max(180f, zone.Width * 0.72f));
            float panelHeight = 124f;
            var center = new Vector2(zone.X + zone.Width * 0.5f, zone.Y + zone.Height * 0.5f);
            var panelSize = new Vector2(panelWidth, panelHeight);
            var border = UiAccentSoft;
            var text = UiText;
            var muted = UiTextMuted;
            var accent = UiAccentBright;

            DrawModalScrim(frame, zone, 64);
            DrawModalPanel(frame, center, panelSize, border);

            AddSprite(frame, new MySprite(
                SpriteType.TEXT,
                "HULL SCAN",
                center + new Vector2(0f, -36f),
                null,
                text,
                CurrentTextFontId,
                TextAlignment.CENTER,
                0.72f
            ));

            AddSprite(frame, new MySprite(
                SpriteType.TEXT,
                "STAGE: " + job.StageLabel,
                center + new Vector2(0f, -20f),
                null,
                muted,
                CurrentTextFontId,
                TextAlignment.CENTER,
                0.55f
            ));

            DrawProgressBar(frame, center + new Vector2(0f, 4f), new Vector2(panelWidth - 46f, 9f), job.AxisProgress, accent, UiAccentGhost, border);
            DrawProgressBar(frame, center + new Vector2(0f, 24f), new Vector2(panelWidth - 46f, 9f), job.OverallProgress, accent, UiAccentGhost, border);
            DrawViewButton(frame, UiLayout.BuildScanProgressCancelRegion(zone), "CANCEL", false, false);
        }

        static void DrawBootLoadingPanel(MySpriteDrawFrame frame, ScreenZone zone, GridSchematicsLcdApp app)
        {
            if (app == null || app.ActiveScanJob != null || app.ConstructCache == null || app.ConstructCache.StartupScanCompleted)
                return;

            float panelWidth = Math.Min(300f, Math.Max(176f, zone.Width * 0.62f));
            float panelHeight = 86f;
            var center = new Vector2(zone.X + zone.Width * 0.5f, zone.Y + zone.Height * 0.5f);
            var panelSize = new Vector2(panelWidth, panelHeight);
            DrawModalScrim(frame, zone, 64);
            DrawModalPanel(frame, center, panelSize, UiAccentSoft);

            AddSprite(frame, new MySprite(SpriteType.TEXT, "BOOST UP", center + new Vector2(0f, -28f), null, UiText, CurrentTextFontId, TextAlignment.CENTER, 0.68f));
            AddSprite(frame, new MySprite(SpriteType.TEXT, "LOADING SAVED STRUCTURE DATA", center + new Vector2(0f, -12f), null, UiTextMuted, CurrentTextFontId, TextAlignment.CENTER, 0.46f));
            DrawProgressBar(frame, center + new Vector2(0f, 14f), new Vector2(panelWidth - 46f, 8f), 0.45f, UiAccentBright, UiAccentGhost, UiAccentSoft);
        }

        static void DrawLowLevelCalibrationPanel(MySpriteDrawFrame frame, ScreenZone zone, GridSchematicsLcdApp app, IMyTextSurface surface)
        {
            if (app == null || surface == null)
                return;

            Vector2 size = surface.SurfaceSize;
            bool metricsCompatible = size.X >= MinimumLowLevelPanelResolutionAxis && size.Y >= MinimumLowLevelPanelResolutionAxis;
            int surfaceIndex = app.TouchInput != null ? app.TouchInput.GetSurfaceIndex() : -1;
            bool found = app.Session != null && app.TouchInput != null
                ? app.Session.IsPanelSurfaceLowLevelCalibrated(app.TouchInput.OwnerBlock, surfaceIndex)
                : app.HasStoredLowLevelCalibration;
            bool activeManual = app.Session != null && app.Session.IsManualPanelCalibrationActive;
            bool hasManualSurface = app.Session != null && app.TouchInput != null && app.Session.IsManualPanelCalibrationSurface(app.TouchInput.OwnerBlock, surfaceIndex);
            bool selectedManualSurface = app.Session != null && app.TouchInput != null && app.Session.IsManualPanelCalibrationSelectedSurface(app.TouchInput.OwnerBlock, surfaceIndex);
            bool peerManualSurface = app.Session != null && app.TouchInput != null && app.Session.IsManualPanelCalibrationPeerSurface(app.TouchInput.OwnerBlock);
            bool fallbackSurface = app.Session != null && app.TouchInput != null && app.Session.IsManualPanelCalibrationFallbackSurface(app.TouchInput.OwnerBlock, surfaceIndex);
            if (activeManual && app.TouchInput != null)
                app.Session.RegisterManualCalibrationRenderedInput(app.TouchInput);
            float left = zone.X + Math.Max(10f, zone.Width * 0.06f);
            float right = zone.X + zone.Width * 0.965f;
            float contentWidth = right - left;
            float top = zone.Y + Math.Max(6f, zone.Height * 0.045f);
            float resolutionScale = Math.Min(1f, Math.Min(zone.Width / 900f, zone.Height / 500f));
            float compactScale = 0.65f + resolutionScale * 0.35f;
            float line = Math.Max(8f, zone.Height * 0.046f * (0.80f + resolutionScale * 0.20f));
            float scale = Math.Max(0.16f, Math.Min(0.41f, zone.Height / 885f * compactScale));
            float titleScale = Math.Max(0.24f, Math.Min(0.55f, zone.Height / 715f * (0.62f + resolutionScale * 0.38f)));
            float metricScale = Math.Max(0.14f, scale * 0.90f);
            Color text = UiText;
            Color muted = UiTextMuted;
            Color accent = found ? new Color(80, 255, 145, 255) : new Color(255, 185, 45, 255);
            Color background = selectedManualSurface ? new Color(0, 18, 10, 255) : (peerManualSurface ? new Color(18, 0, 4, 255) : Color.Black);

            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(zone.X + zone.Width * 0.5f, zone.Y + zone.Height * 0.5f), new Vector2(zone.Width, zone.Height), background));

            AddCalibrationText(frame, "GRID SCHEMATICS", new Vector2(left, top), text, titleScale);
            float metricsY = top + line * 1.28f;
            AddCalibrationText(frame, "CALIBRATION:", new Vector2(left, metricsY), text, metricScale);
            AddCalibrationText(frame, found ? "FOUND" : "MISSING", new Vector2(left + contentWidth * 0.34f, metricsY), found ? new Color(80, 255, 145, 255) : new Color(255, 65, 65, 255), metricScale);
            AddCalibrationText(frame, "RES:", new Vector2(left + contentWidth * 0.50f, metricsY), accent, metricScale);
            AddCalibrationText(frame, ((int)size.X).ToString() + " X " + ((int)size.Y).ToString(), new Vector2(left + contentWidth * 0.58f, metricsY), muted, metricScale);
            AddCalibrationText(frame, "INDEX:", new Vector2(left + contentWidth * 0.78f, metricsY), accent, metricScale);
            AddCalibrationText(frame, surfaceIndex >= 0 ? surfaceIndex.ToString() : "?", new Vector2(left + contentWidth * 0.91f, metricsY), muted, metricScale);

            if (found && metricsCompatible && !activeManual)
                return;

            if (activeManual)
            {
                float legendBottom = DrawLowLevelCalibrationCommandLegend(frame, left, right, top + line * 2.55f, line, scale, hasManualSurface, fallbackSurface);
                DrawCalibrationExportNotice(frame, zone, app, left, legendBottom + line * 0.45f, line * 0.62f, Math.Max(0.13f, Math.Min(scale * 0.72f, 0.22f)), new Color(80, 255, 145, 255));
                return;
            }

            if (!metricsCompatible)
                AddCalibrationText(frame, "INCOMPATIBLE PANEL METRICS.", new Vector2(left, top + line * 2.70f), new Color(255, 65, 65, 255), scale);
            else if (!found)
                AddCalibrationText(frame, "CHAT /GSDISPLAYCAL TO BEGIN", new Vector2(left, top + line * 2.70f), text, scale);
        }

        static void DrawPanelCursorDepthOffsetCalibrationPanel(MySpriteDrawFrame frame, ScreenZone zone, GridSchematicsLcdApp app, IMyTextSurface surface)
        {
            if (app == null || app.Session == null || surface == null)
                return;

            Vector2 size = surface.SurfaceSize;
            int surfaceIndex = app.TouchInput != null ? app.TouchInput.GetSurfaceIndex() : -1;
            bool selected = app.TouchInput != null && app.Session.IsPanelCursorDepthOffsetCalibrationSelectedSurface(app.TouchInput.OwnerBlock, surfaceIndex);
            bool peer = app.TouchInput != null && app.Session.IsPanelCursorDepthOffsetCalibrationPeerSurface(app.TouchInput.OwnerBlock);
            int displayIndex = app.TouchInput != null
                ? app.Session.GetPanelCursorDepthOffsetCalibrationScreenIndex(app.TouchInput.OwnerBlock, surfaceIndex)
                : surfaceIndex;

            Color background = selected ? new Color(0, 18, 10, 255) : (peer ? new Color(18, 0, 4, 255) : Color.Black);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(zone.X + zone.Width * 0.5f, zone.Y + zone.Height * 0.5f), new Vector2(zone.Width, zone.Height), background));

            float scale = Math.Max(0.18f, Math.Min(0.38f, size.Y / 900f));
            AddCalibrationText(frame, "INDEX: " + (displayIndex >= 0 ? displayIndex.ToString(CultureInfo.InvariantCulture) : "?"), new Vector2(zone.X + Math.Max(10f, zone.Width * 0.055f), zone.Y + Math.Max(8f, zone.Height * 0.045f)), new Color(255, 245, 70, 245), scale);
        }

        static float DrawLowLevelCalibrationCommandLegend(MySpriteDrawFrame frame, float x, float right, float top, float line, float baseScale, bool hasManualSurface, bool fallbackSurface)
        {
            float y = top;
            float scale = Math.Max(0.19f, Math.Min(baseScale * 0.92f, 0.29f));
            float smallLine = Math.Max(6f, line * 0.72f);
            Color head = new Color(80, 255, 145, 255);
            Color text = UiTextMuted;

            AddCalibrationText(frame, "POSITION CONTROLS:", new Vector2(x, y), head, scale);
            y += smallLine * 1.15f;
            AddCalibrationCommandRow(frame, x, right, y, "SCROLL:", "SCALE", text, scale);
            y += smallLine;
            AddCalibrationCommandRow(frame, x, right, y, "SHIFT+SCROLL:", "SHIFT CENTER X", text, scale);
            y += smallLine;
            AddCalibrationCommandRow(frame, x, right, y, "CTRL+SCROLL:", "SHIFT CENTER Y", text, scale);
            y += smallLine;
            AddCalibrationCommandRow(frame, x, right, y, "CTRL+SHIFT+SCROLL:", "RAISE/LOWER SURFACE", text, scale);
            y += smallLine;
            AddCalibrationCommandRow(frame, x, right, y, "ALT+SHIFT+SCROLL:", "SCALE X", text, scale);
            y += smallLine;
            AddCalibrationCommandRow(frame, x, right, y, "ALT+CTRL+SCROLL:", "SCALE Y", text, scale);
            y += smallLine;
            AddCalibrationCommandRow(frame, x, right, y, "CTRL+SHIFT+ALT+SCROLL:", "ROTATE", text, scale);
            y += smallLine * 1.35f;

            AddCalibrationText(frame, "COMMANDS:", new Vector2(x, y), head, scale);
            y += smallLine * 1.15f;
            AddCalibrationCommandRow(frame, x, right, y, "[ / ]:", "SELECT SCREEN INDEX", text, scale);
            y += smallLine;
            AddCalibrationCommandRow(frame, x, right, y, "H:", "TOGGLE FULL DEBUG VISUALS", text, scale);
            y += smallLine;
            AddCalibrationCommandRow(frame, x, right, y, "LMB:", "TARGET SCREEN CENTER", text, scale);
            y += smallLine;
            AddCalibrationCommandRow(frame, x, right, y, "MMB:", "FOCUS SCREEN DRAFT", text, scale);
            y += smallLine;
            AddCalibrationCommandRow(frame, x, right, y, "SHIFT+LMB:", "SAVE BLOCK CALIBRATIONS", text, scale);
            y += smallLine;
            AddCalibrationCommandRow(frame, x, right, y, "CTRL+LMB:", "EXPORT TO CLIPBOARD", text, scale);
            y += smallLine;
            AddCalibrationCommandRow(frame, x, right, y, "CTRL+ALT+LMB:", "EXPORT ALL TO CLIPBOARD", text, scale);
            y += smallLine;
            AddCalibrationCommandRow(frame, x, right, y, "CTRL+SHIFT+LMB:", "PROCEED TO CURSOR CALIBRATION", text, scale);
            y += smallLine;
            AddCalibrationCommandRow(frame, x, right, y, "RMB:", "RELOAD SAVED CALIBRATION", text, scale);
            y += smallLine;
            AddCalibrationCommandRow(frame, x, right, y, "CTRL+RMB:", "DELETE SAVED CALIBRATION", text, scale);
            return y + smallLine;
        }

        static void AddCalibrationCommandRow(MySpriteDrawFrame frame, float left, float right, float y, string control, string action, Color color, float scale)
        {
            AddSprite(frame, new MySprite(SpriteType.TEXT, control ?? string.Empty, new Vector2(left, y), null, color, CalibrationTextFontId, TextAlignment.LEFT, scale));
            AddSprite(frame, new MySprite(SpriteType.TEXT, action ?? string.Empty, new Vector2(right, y), null, color, CalibrationTextFontId, TextAlignment.RIGHT, scale));
        }

        static void AddCalibrationText(MySpriteDrawFrame frame, string text, Vector2 position, Color color, float scale)
        {
            AddSprite(frame, new MySprite(SpriteType.TEXT, text ?? string.Empty, position, null, color, CalibrationTextFontId, TextAlignment.LEFT, scale));
        }

        static void DrawManualCalibrationDebugPopup(MySpriteDrawFrame frame, ScreenZone zone, GridSchematicsLcdApp app)
        {
            if (app == null ||
                app.Config == null ||
                !app.Config.ShowDebug ||
                app.TouchInput == null ||
                !app.TouchInput.HasStoredPanelCursorSurface ||
                !app.TouchInput.IsAimCursorActive ||
                !app.TouchInput.IsVisualCursorOnScreen)
            {
                return;
            }

            float panelWidth = Math.Min(zone.Width * 0.86f, 520f);
            if (panelWidth < 260f)
                panelWidth = Math.Min(zone.Width - 12f, 260f);
            float panelHeight = Math.Min(zone.Height * 0.40f, 132f);
            if (panelHeight < 96f)
                panelHeight = Math.Min(zone.Height - 12f, 96f);

            var center = new Vector2(zone.X + zone.Width * 0.5f, zone.Y + zone.Height * 0.5f);
            var size = new Vector2(panelWidth, panelHeight);
            Color red = new Color(255, 30, 30, 255);
            Color dark = new Color(0, 0, 0, 226);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, size, dark));
            DrawScreenRectBorder(frame, center, size, red);
            DrawScreenRectBorder(frame, center, new Vector2(Math.Max(1f, size.X - 4f), Math.Max(1f, size.Y - 4f)), red);

            float line = Math.Max(13f, panelHeight * 0.22f);
            float titleScale = Math.Max(0.34f, Math.Min(0.52f, panelHeight / 210f));
            float bodyScale = Math.Max(0.24f, Math.Min(0.36f, panelHeight / 300f));
            AddSprite(frame, new MySprite(SpriteType.TEXT, "MANUAL CALIBRATION", center + new Vector2(0f, -line * 1.45f), null, red, CalibrationTextFontId, TextAlignment.CENTER, titleScale));
            AddSprite(frame, new MySprite(SpriteType.TEXT, "CTRL+LMB: EXPORT EXISTING CALIBRATION TO CLIPBOARD", center + new Vector2(0f, -line * 0.35f), null, red, CalibrationTextFontId, TextAlignment.CENTER, bodyScale));
            AddSprite(frame, new MySprite(SpriteType.TEXT, "CHAT /GSUICAL: PERFORM CURSOR CALIBRATION", center + new Vector2(0f, line * 0.60f), null, red, CalibrationTextFontId, TextAlignment.CENTER, bodyScale));
            AddSprite(frame, new MySprite(SpriteType.TEXT, "CHAT /GSDISPLAYCAL: PERFORM LOW LEVEL CALIBRATION", center + new Vector2(0f, line * 1.55f), null, red, CalibrationTextFontId, TextAlignment.CENTER, bodyScale));
            DrawCalibrationExportNotice(frame, zone, app, center.X - panelWidth * 0.42f, center.Y + panelHeight * 0.70f, Math.Max(10f, line * 0.72f), bodyScale, red);
        }

        static void DrawCalibrationExportNotice(MySpriteDrawFrame frame, ScreenZone zone, GridSchematicsLcdApp app, float x, float y, float line, float scale, Color color)
        {
            if (app == null || app.Session == null || app.TouchInput == null || app.TouchInput.OwnerBlock == null)
                return;

            string blockId;
            string[] indexLines;
            if (!app.Session.TryGetManualCalibrationExportNotice(app.TouchInput.OwnerBlock, out blockId, out indexLines))
                return;

            float maxY = zone.Y + zone.Height - line;
            AddCalibrationText(frame, "EXPORTED:", new Vector2(x, y), color, scale);
            y += line;
            if (y > maxY)
                return;

            AddCalibrationText(frame, "BLOCK ID: " + ShortenCalibrationText(blockId, 48), new Vector2(x, y), color, scale);
            y += line;
            if (indexLines == null)
                return;

            for (int i = 0; i < indexLines.Length && y <= maxY; i++)
            {
                AddCalibrationText(frame, indexLines[i], new Vector2(x, y), color, scale);
                y += line;
            }
        }

        static string ShortenCalibrationText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || maxLength <= 0)
                return string.Empty;
            if (text.Length <= maxLength)
                return text;
            if (maxLength <= 3)
                return text.Substring(0, maxLength);
            return text.Substring(0, maxLength - 3) + "...";
        }

        static void DrawCalibrationModal(MySpriteDrawFrame frame, ScreenZone zone, UiState ui, string hoverRegionId)
        {
            if (ui == null || (!ui.ShowCalibrationPrompt && !ui.CalibrationActive))
                return;

            bool active = ui.CalibrationActive;
            int dim = active ? 204 : 64;
            DrawModalScrim(frame, zone, dim);

            var context = UiLayout.BuildLayoutContext(zone);
            var metrics = context.Metrics;
            var profile = context.Profile;
            bool compactPopup = !profile.IsStandard;
            var modalZone = UiLayout.BuildCalibrationPromptZone(zone, active);
            float panelWidth = modalZone.Width;
            float panelHeight = modalZone.Height;
            var center = new Vector2(modalZone.X + modalZone.Width * 0.5f, modalZone.Y + modalZone.Height * 0.5f);
            var panelSize = new Vector2(panelWidth, panelHeight);
            DrawModalPanel(frame, center, panelSize, UiAccentSoft);

            string title = ui.CursorCalibrationRequired ? "CURSOR CALIBRATION" : active ? "CALIBRATION" : "CALIBRATION NEEDED";
            string status = BuildCalibrationStatus(ui);
            float headerHeight = Math.Max(18f, metrics.InfoHeaderHeight);
            float left = center.X - panelWidth * 0.5f;
            float top = center.Y - panelHeight * 0.5f;
            float right = center.X + panelWidth * 0.5f;
            float contentX = left + metrics.S(12f);
            float contentWidth = Math.Max(1f, panelWidth - metrics.S(24f));
            var headerCenter = new Vector2(center.X, top + headerHeight * 0.5f);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", headerCenter, new Vector2(panelWidth, headerHeight), UiMenuButtonFill));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(center.X, top + headerHeight - metrics.Line * 0.5f), new Vector2(panelWidth, metrics.Line), UiAccentDim));
            float labelLeft = left + metrics.S(8f);
            float labelWidth = panelWidth - metrics.S(16f);
            float labelMinWidth = metrics.S(compactPopup ? 86f : 120f);
            float measuredTitleWidth = EstimateCalibrationTextWidth(title, metrics.SmallText) + metrics.S(18f);
            if (!compactPopup)
                labelWidth = Math.Min(labelWidth, Math.Max(labelMinWidth, measuredTitleWidth));
            if (labelWidth < labelMinWidth)
                labelWidth = Math.Min(panelWidth - metrics.S(16f), labelMinWidth);
            var labelCenter = new Vector2(labelLeft + labelWidth * 0.5f, top + headerHeight * 0.5f);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", labelCenter, new Vector2(labelWidth, headerHeight), UiPanelFill));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(labelLeft, labelCenter.Y), new Vector2(metrics.Line, headerHeight), UiAccentSoft));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(labelLeft + labelWidth, labelCenter.Y), new Vector2(metrics.Line, headerHeight), UiAccentSoft));
            DrawFittedCalibrationText(frame, title, labelCenter + new Vector2(0f, -metrics.S(6f)), UiText, metrics.SmallText, labelWidth - metrics.S(12f), TextAlignment.CENTER);

            float statusY = compactPopup ? top + headerHeight + Math.Max(17f, panelHeight * 0.20f) : top + headerHeight + metrics.S(22f);
            float instructionY = compactPopup ? statusY + Math.Max(15f, panelHeight * 0.16f) : top + headerHeight + metrics.S(43f);
            float footerY = compactPopup ? top + panelHeight - Math.Max(14f, panelHeight * 0.12f) : top + panelHeight - metrics.S(18f);
            DrawFittedCalibrationText(frame, status, new Vector2(contentX, statusY), UiTextMuted, metrics.MediumText, contentWidth, TextAlignment.LEFT);
            if (ui.CalibrationActive && ui.CalibrationCompletedTick >= 0)
            {
                string restart = "RESTARTING CALIBRATION IN: " + ui.CalibrationRestartCountdownSeconds.ToString();
                DrawFittedCalibrationText(frame, restart, new Vector2(contentX, instructionY), UiAccentBright, metrics.SmallText, contentWidth, TextAlignment.LEFT);
            }
            else
            {
                string instruction = "CLICK THE ACTIVE MARKER";
                DrawFittedCalibrationText(frame, instruction, new Vector2(contentX, instructionY), UiAccentBright, metrics.SmallText, contentWidth, TextAlignment.LEFT);
            }

            string lowLevelText = SelectCalibrationTextVariant(contentWidth, metrics.SmallText, compactPopup,
                profile.UsesCompactScaling ? "CHAT /GSDISPLAYCAL: LOW LEVEL CAL" : "CHAT /GSDISPLAYCAL TO ENTER LOW LEVEL CALIBRATION",
                "/GSDISPLAYCAL: LOW LEVEL CAL",
                "/GSDISPLAYCAL: LOW CAL");
            if (compactPopup)
                DrawFittedCalibrationText(frame, lowLevelText, new Vector2(center.X, footerY), UiTextMuted, metrics.SmallText, contentWidth, TextAlignment.CENTER);
            else
                DrawFittedCalibrationText(frame, lowLevelText, new Vector2(right - metrics.S(12f), footerY), UiTextMuted, metrics.SmallText, contentWidth, TextAlignment.RIGHT);

            var calibrationTargetZone = UiLayout.BuildCalibrationTargetZone(zone);
            var interactionZone = active ? calibrationTargetZone : modalZone;

            if (active)
                DrawCalibrationPoints(frame, calibrationTargetZone, ui);
            else
            {
                var startRegion = UiLayout.BuildCalibrationStartRegion(interactionZone);
                DrawCalibrationButton(frame, startRegion, "START", false, IsHoverRegion(startRegion, hoverRegionId));
            }

            if (!ui.CursorCalibrationRequired)
            {
                var closeRegion = UiLayout.BuildCalibrationCloseRegion(interactionZone);
                bool completed = ui.CalibrationCompletedTick >= 0;
                DrawCalibrationButton(frame, closeRegion, completed ? "CONTINUE" : active ? "CLOSE" : "LATER", completed, IsHoverRegion(closeRegion, hoverRegionId));
            }
        }

        static void DrawCalibrationButton(MySpriteDrawFrame frame, HitRegion region, string label, bool active, bool hover)
        {
            DrawCachedButtonBase(frame, region, label, active, hover, true, 1f, true, true, CalibrationTextFontId);
        }

        static bool IsHoverRegion(HitRegion region, string hoverRegionId)
        {
            return string.Equals(region.Id, hoverRegionId ?? string.Empty, StringComparison.Ordinal);
        }

        static void DrawFittedCalibrationText(MySpriteDrawFrame frame, string text, Vector2 position, Color color, float desiredScale, float availableWidth, TextAlignment alignment)
        {
            AddSprite(frame, new MySprite(SpriteType.TEXT, text ?? string.Empty, position, null, color, CalibrationTextFontId, alignment, FitCalibrationTextScale(text, desiredScale, availableWidth)));
        }

        static string SelectCalibrationTextVariant(float availableWidth, float desiredScale, bool preferShort, string longText, string mediumText, string shortText)
        {
            if (!preferShort && EstimateCalibrationTextWidth(longText, desiredScale) <= availableWidth)
                return longText;
            if (EstimateCalibrationTextWidth(mediumText, desiredScale) <= availableWidth)
                return mediumText;
            return shortText;
        }

        static float EstimateCalibrationTextWidth(string text, float scale)
        {
            if (string.IsNullOrEmpty(text))
                return 0f;
            return text.Length * 18f * scale;
        }

        static float FitCalibrationTextScale(string text, float desiredScale, float availableWidth)
        {
            if (string.IsNullOrEmpty(text) || availableWidth <= 1f)
                return desiredScale;

            float approximateWidth = EstimateCalibrationTextWidth(text, desiredScale);
            if (approximateWidth <= availableWidth)
                return desiredScale;

            float scale = desiredScale * availableWidth / approximateWidth;
            if (scale < 0.16f)
                scale = 0.16f;
            return scale;
        }

        static string BuildCalibrationStatus(UiState ui)
        {
            if (ui.CalibrationActive && ui.CalibrationCompletedTick >= 0)
                return "CALIBRATION COMPLETED";
            if (ui.CursorCalibrationRequired && ui.CalibrationActive)
                return "AIM AT MARKER AND CLICK  " + Math.Max(0, Math.Min(3, ui.CalibrationStep)).ToString() + "/3";
            if (!ui.CalibrationActive)
                return "TOUCH CURSOR IS NOT ALIGNED TO THIS PANEL";

            int done = ui.CalibrationStep;
            if (done < 0)
                done = 0;
            if (done > 3)
                done = 3;
            return "POINTS REGISTERED " + done.ToString() + "/3";
        }

        static void DrawCalibrationPoints(MySpriteDrawFrame frame, ScreenZone zone, UiState ui)
        {
            if (ui.CalibrationCompletedTick >= 0)
                return;

            int step = Math.Max(0, Math.Min(2, ui.CalibrationStep));
            var point = UiLayout.GetCalibrationPoint(zone, step);
            float size = Math.Max(9f, Math.Min(14f, Math.Min(zone.Width, zone.Height) * 0.045f));
            DrawCalibrationCross(frame, point, size, new Color(255, 60, 60, 245));
        }

        static void DrawCalibrationCross(MySpriteDrawFrame frame, Vector2 center, float size, Color color)
        {
            DrawScreenLine(frame, center + new Vector2(-size, 0f), center + new Vector2(size, 0f), 2f, color);
            DrawScreenLine(frame, center + new Vector2(0f, -size), center + new Vector2(0f, size), 2f, color);
        }

        static void DrawModalScrim(MySpriteDrawFrame frame, ScreenZone zone, int alpha)
        {
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(zone.X + zone.Width * 0.5f, zone.Y + zone.Height * 0.5f), new Vector2(zone.Width, zone.Height), new Color(0, 0, 0, ClampByte(alpha))));
        }

        static void DrawModalPanel(MySpriteDrawFrame frame, Vector2 center, Vector2 size, Color border)
        {
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, size, UiPanelFillSoft));
            DrawScreenRectBorder(frame, center, size, border);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(center.X, center.Y - size.Y * 0.5f + 1f), new Vector2(size.X, 1f), UiAccentDim));
        }

        static void DrawProgressBar(MySpriteDrawFrame frame, Vector2 center, Vector2 size, float progress, Color fill, Color background, Color border)
        {
            progress = Clamp(progress, 0f, 1f);
            AddSprite(frame, new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                center,
                size,
                background
            ));
            DrawScreenRectBorder(frame, center, size, border);

            float innerWidth = Math.Max(1f, (size.X - 2f) * progress);
            var fillCenter = new Vector2(center.X - (size.X - 2f) * 0.5f + innerWidth * 0.5f, center.Y);
            AddSprite(frame, new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                fillCenter,
                new Vector2(innerWidth, Math.Max(1f, size.Y - 2f)),
                fill
            ));
        }

        static void DrawGridNameLabel(MySpriteDrawFrame frame, ScreenZone zone, IMyCubeGrid grid, ScanCache cache)
        {
            if (grid == null || zone.Width <= 0 || zone.Height <= 0)
                return;

            var displayGrid = ResolveDisplayNameGrid(grid, cache);
            string name = displayGrid != null ? displayGrid.DisplayName : grid.DisplayName;
            if (string.IsNullOrWhiteSpace(name))
                name = "GRID";

            name = name.Trim().ToUpperInvariant();
            if (name.Length > 28)
                name = name.Substring(0, 25) + "...";

            AddSprite(frame, new MySprite(
                SpriteType.TEXT,
                name,
                new Vector2(zone.X + 6f, zone.Y + 5f),
                null,
                new Color(20, 220, 255, 210),
                CurrentTextFontId,
                TextAlignment.LEFT,
                0.38f
            ));
        }

        static IMyCubeGrid ResolveDisplayNameGrid(IMyCubeGrid fallback, ScanCache cache)
        {
            if (cache == null || cache.ConstructGrids == null || cache.ConstructGrids.Count == 0)
                return fallback;

            IMyCubeGrid best = null;
            double bestVolume = -1.0;
            for (int i = 0; i < cache.ConstructGrids.Count; i++)
            {
                var grid = cache.ConstructGrids[i];
                if (grid == null || grid.MarkedForClose || grid.GridSize < 2f)
                    continue;

                var size = grid.WorldAABB.Size;
                double volume = size.X * size.Y * size.Z;
                if (volume > bestVolume)
                {
                    bestVolume = volume;
                    best = grid;
                }
            }

            return best ?? fallback;
        }

        static void UpdateAndDrawBlockStackPreview(MySpriteDrawFrame frame, ScreenZone mapZone, ScreenZone renderZone, ShipGrid shipGrid, TouchScreenApiAdapter input, UiState ui, int rotationSteps)
        {
            if (ui == null)
                return;

            ui.PreviewBlockStackItems.Clear();

            if (input == null || !input.IsAvailable || !input.IsCursorOnScreen)
                return;

            if (shipGrid != null && !shipGrid.IsEmpty && IsPointInZone(input.CursorPosition, mapZone))
            {
                var transform = GetOrBuildProjectionTransform(shipGrid, renderZone, rotationSteps);
                if (transform.IsValid && transform.CellSize > 0f)
                {
                    if (ui.ActiveOverlay == OverlayMode.None)
                    {
                        float localX;
                        float localY;
                        if (transform.TryScreenToLocal(input.CursorPosition, out localX, out localY))
                        {
                            int projectedX = shipGrid.Min2D.X + (int)Math.Floor(localX);
                            int projectedY = shipGrid.Max2D.Y - (int)Math.Floor(localY);
                            BuildBlockStackItems(shipGrid, projectedX, projectedY, ui.PreviewBlockStackItems);
                        }
                    }
                    else
                    {
                        BuildOverlayBlockStackItems(ui, input.CursorPosition, ui.PreviewBlockStackItems);
                    }
                }
            }
            else
            {
                BuildUiBlockStackPreviewItems(ui, input.HoverRegionId ?? string.Empty, ui.PreviewBlockStackItems);
            }

            if (ui.PreviewBlockStackItems.Count == 0)
                return;

            string signature = BuildBlockStackSignature(ui.PreviewBlockStackItems);
            if (!string.Equals(signature, ui.PreviewBlockStackSignature, StringComparison.Ordinal))
            {
                ui.PreviewBlockStackSignature = signature;
                ui.PreviewBlockStackIndex = 0;
            }

            if (ui.PreviewBlockStackIndex >= ui.PreviewBlockStackItems.Count)
                ui.PreviewBlockStackIndex = ui.PreviewBlockStackItems.Count - 1;
            if (ui.PreviewBlockStackIndex < 0)
                ui.PreviewBlockStackIndex = 0;

            DrawBlockStackPreview(frame, renderZone, ui.PreviewBlockStackItems, -1);
        }

        static void BuildUiBlockStackPreviewItems(UiState ui, string hoverId, List<BlockStackItem> items)
        {
            if (ui == null || items == null || string.IsNullOrEmpty(hoverId))
                return;

            if (string.Equals(hoverId, UiLayout.InfoPanelAllTabId, StringComparison.Ordinal))
                return;

            if (string.Equals(hoverId, UiLayout.InfoPanelStackTabId, StringComparison.Ordinal))
            {
                var source = ui.ManualSelectedBlockItems != null && ui.ManualSelectedBlockItems.Count > 0 ? ui.ManualSelectedBlockItems : ui.SelectedBlockStackItems;
                AddBlockStackPreviewItems(source, items, "ui:group:1");
                return;
            }

            if (string.Equals(hoverId, UiLayout.InfoPanelGroup2TabId, StringComparison.Ordinal))
            {
                AddBlockStackPreviewItems(ui.ManualSelectedBlockItems2, items, "ui:group:2");
                return;
            }

            if (hoverId.StartsWith(UiLayout.InfoPanelBlockTabPrefix, StringComparison.Ordinal))
            {
                int index;
                if (int.TryParse(hoverId.Substring(UiLayout.InfoPanelBlockTabPrefix.Length), out index) && ui.SelectedBlockStackItems != null && index >= 0 && index < ui.SelectedBlockStackItems.Count)
                    AddBlockStackPreviewItem(ui.SelectedBlockStackItems[index], items, "ui:tab:" + index.ToString());
                return;
            }

            if (string.Equals(hoverId, UiLayout.CargoInfoTransferSourceSelectId, StringComparison.Ordinal) ||
                string.Equals(hoverId, UiLayout.CargoInfoTransferSourceViewId, StringComparison.Ordinal))
            {
                AddBlockStackPreviewItems(ui.CargoTransferSourceItems, items, "ui:transfer:source");
                return;
            }

            if (string.Equals(hoverId, UiLayout.CargoInfoTransferDestSelectId, StringComparison.Ordinal) ||
                string.Equals(hoverId, UiLayout.CargoInfoTransferDestViewId, StringComparison.Ordinal))
            {
                AddBlockStackPreviewItems(ui.CargoTransferDestItems, items, "ui:transfer:dest");
                return;
            }

            if (hoverId.StartsWith(UiLayout.CargoInfoMixRowPrefix, StringComparison.Ordinal))
            {
                int row;
                if (!int.TryParse(hoverId.Substring(UiLayout.CargoInfoMixRowPrefix.Length), out row))
                    return;
                var summary = ui.CachedCargoSummary;
                var rows = BuildCargoMixRowsForRender(summary, summary != null ? summary.Filter : ui.CargoInfoFilter);
                string sortKey = string.IsNullOrEmpty(ui.CargoMixSortKey) ? "QUANT" : ui.CargoMixSortKey;
                int sortDirection = ui.CargoMixSortDirection == 2 ? 2 : 1;
                rows.Sort(delegate(CargoPanelItem a, CargoPanelItem b)
                {
                    int result = CompareCargoMixItems(a, b, sortKey);
                    return sortDirection == 2 ? -result : result;
                });
                int index = ui.CargoMixScrollIndex + row;
                if (index >= 0 && index < rows.Count && rows[index] != null)
                {
                    items.Add(new BlockStackItem
                    {
                        Id = "ui:cargo:item:" + CargoPanelItemKey(rows[index]),
                        Name = rows[index].Name,
                        Depth = 0
                    });
                }
            }
        }

        static void AddBlockStackPreviewItems(List<BlockStackItem> source, List<BlockStackItem> items, string idPrefix)
        {
            if (source == null || items == null)
                return;
            for (int i = 0; i < source.Count; i++)
                AddBlockStackPreviewItem(source[i], items, idPrefix + ":" + i.ToString());
        }

        static void AddBlockStackPreviewItem(BlockStackItem source, List<BlockStackItem> items, string id)
        {
            if (source == null || items == null)
                return;
            items.Add(new BlockStackItem
            {
                Id = id,
                Name = source.Name,
                Block = source.Block,
                Projected = source.Projected,
                Depth = source.Depth
            });
        }
        static void BuildOverlayBlockStackItems(UiState ui, Vector2 cursorPosition, List<BlockStackItem> items)
        {
            if (ui == null || ui.OverlayBlockRegions == null || items == null)
                return;

            for (int i = 0; i < ui.OverlayBlockRegions.Count; i++)
            {
                var info = ui.OverlayBlockRegions[i];
                if (info == null || !IsPointInRegion(cursorPosition, info.Region))
                    continue;

                if (info.Blocks != null && info.Blocks.Count > 0)
                {
                    for (int blockIndex = 0; blockIndex < info.Blocks.Count; blockIndex++)
                    {
                        var block = info.Blocks[blockIndex];
                        string name = GetReadableBlockName(block);
                        if (string.IsNullOrEmpty(name))
                            name = string.IsNullOrEmpty(info.Name) ? "BLOCK" : info.Name;

                        items.Add(new BlockStackItem
                        {
                            Id = "stack:overlay:" + info.Id + ":" + blockIndex,
                            Name = name,
                            Block = block,
                            Depth = i
                        });
                    }
                }
                else
                {
                    items.Add(new BlockStackItem
                    {
                        Id = "stack:overlay:" + info.Id,
                        Name = string.IsNullOrEmpty(info.Name) ? "BLOCK" : info.Name,
                        Depth = i
                    });
                }
            }

            items.Sort(CompareBlockStackItems);
        }

        static void BuildBlockStackItems(ShipGrid shipGrid, int projectedX, int projectedY, List<BlockStackItem> items)
        {
            if (shipGrid == null || shipGrid.Blocks == null || items == null)
                return;

            for (int i = 0; i < shipGrid.Blocks.Count; i++)
            {
                var projected = shipGrid.Blocks[i];
                if (projected.Projected.X != projectedX || projected.Projected.Y != projectedY)
                    continue;

                var block = projected.FatBlock;
                string name = GetReadableBlockName(block);
                if (string.IsNullOrEmpty(name))
                    name = projected.HasFatBlock ? "BLOCK" : "ARMOR BLOCK";

                items.Add(new BlockStackItem
                {
                    Id = "stack:block:" + projected.GridEntityId + ":" + projected.BasisPosition.X + ":" + projected.BasisPosition.Y + ":" + projected.BasisPosition.Z,
                    Name = name,
                    Block = block,
                    Projected = projected.Projected,
                    Depth = projected.Depth
                });
            }

            items.Sort(CompareBlockStackItems);
        }

        static string BuildBlockStackSignature(List<BlockStackItem> items)
        {
            if (items == null || items.Count == 0)
                return string.Empty;

            var builder = new StringBuilder();
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null)
                    continue;
                builder.Append(item.Id);
                builder.Append('|');
            }
            return builder.ToString();
        }

        static bool IsPointInZone(Vector2 point, ScreenZone zone)
        {
            return point.X >= zone.X && point.X <= zone.X + zone.Width &&
                point.Y >= zone.Y && point.Y <= zone.Y + zone.Height;
        }

        static bool IsPointInRegion(Vector2 point, HitRegion region)
        {
            return point.X >= region.X && point.X <= region.X + region.Width &&
                point.Y >= region.Y && point.Y <= region.Y + region.Height;
        }

        static int CompareBlockStackItems(BlockStackItem a, BlockStackItem b)
        {
            if (a == null && b == null)
                return 0;
            if (a == null)
                return 1;
            if (b == null)
                return -1;
            return a.Depth.CompareTo(b.Depth);
        }

        static void DrawBlockStackPreview(MySpriteDrawFrame frame, ScreenZone zone, List<BlockStackItem> items, int selectedIndex)
        {
            var metrics = UiLayout.BuildMetrics(zone.Width, zone.Height);
            int maxItems = Math.Min(8, items.Count);
            float x = zone.X + metrics.S(8f);
            float y = zone.Y + zone.Height - metrics.S(24f);
            float step = metrics.S(10f);
            if (step < 9f)
                step = 9f;

            for (int i = 0; i < maxItems; i++)
            {
                var item = items[i];
                if (item == null)
                    continue;

                bool selected = i == selectedIndex;
                Color color = selected ? new Color(80, 130, 255, 235) : new Color(20, 220, 255, 215);
                string label = ShortenBlockStackLabel(item.Name, 24);
                float textY = y - i * step;
                AddSprite(frame, new MySprite(SpriteType.TEXT, label, new Vector2(x, textY), null, color, CurrentTextFontId, TextAlignment.LEFT, metrics.SmallText));
                if (selected)
                {
                    float markerX = x + metrics.S(116f);
                    float markerY = textY - metrics.S(3f);
                    DrawScreenLine(frame, new Vector2(x - metrics.S(3f), textY + metrics.S(1f)), new Vector2(x - metrics.S(3f), textY - metrics.S(6f)), 1f, color);
                    DrawScreenLine(frame, new Vector2(markerX + metrics.S(5f), markerY - metrics.S(5f)), new Vector2(markerX, markerY), 1f, color);
                    DrawScreenLine(frame, new Vector2(markerX + metrics.S(5f), markerY + metrics.S(5f)), new Vector2(markerX, markerY), 1f, color);
                }
            }
        }

        static string GetReadableBlockName(IMyCubeBlock block)
        {
            if (block == null)
                return string.Empty;

            string name = block.DisplayNameText;
            if (string.IsNullOrWhiteSpace(name))
                name = block.DefinitionDisplayNameText;
            if (string.IsNullOrWhiteSpace(name))
                name = "BLOCK";
            return name.Trim().ToUpperInvariant();
        }

        static string ShortenBlockStackLabel(string text, int maxChars)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "BLOCK";
            text = text.Trim().ToUpperInvariant();
            if (text.Length <= maxChars)
                return text;
            if (maxChars <= 3)
                return text.Substring(0, maxChars);
            return text.Substring(0, maxChars - 3) + "...";
        }

        static string BuildTouchStatus(TouchScreenApiAdapter input)
        {
            if (input == null || string.IsNullOrEmpty(input.HoverRegionId))
                return input != null ? input.StatusText : "Touch: unavailable";

            string label = input.HoverRegionId == UiLayout.ViewTopId ? "Top" :
                input.HoverRegionId == UiLayout.MenuViewId ? "View" :
                input.HoverRegionId == UiLayout.MenuLayersId ? "Layers" :
                input.HoverRegionId == UiLayout.MenuScanId ? "Scan" :
                input.HoverRegionId == UiLayout.MenuSettingsId ? "Settings" :
                input.HoverRegionId == UiLayout.ViewLeftId ? "Side" :
                input.HoverRegionId == UiLayout.ViewFrontId ? "Front" :
                input.HoverRegionId == UiLayout.RotateCcwId ? "Rotate Left" :
                input.HoverRegionId == UiLayout.RotateCwId ? "Rotate Right" :
                input.HoverRegionId == UiLayout.ToggleBlocksId ? "All Blocks" :
                input.HoverRegionId == UiLayout.ToggleBorderId ? "Border" :
                input.HoverRegionId == UiLayout.ToggleGridId ? "Grid" :
                input.HoverRegionId == UiLayout.ToggleReferenceId ? "Reference" :
                input.HoverRegionId == UiLayout.ToggleAllConnectionsId ? "All Connections" :
                input.HoverRegionId == UiLayout.ToggleBlocksOccludeConveyorsId ? "Block Occlusion" :
                input.HoverRegionId == UiLayout.ToggleConnectedNetworksId ? "Connected Networks" :
                input.HoverRegionId == UiLayout.ToggleBlurId ? "Blur" :
                input.HoverRegionId == UiLayout.CycleUiFontId ? "Font" :
                input.HoverRegionId == UiLayout.ToggleMouseControlId ? "Mouse Control" :
                input.HoverRegionId == UiLayout.CycleMouseSensitivityId ? "Mouse Sensitivity" :
                input.HoverRegionId == UiLayout.SetDensityId ? "Density" :
                input.HoverRegionId == UiLayout.SetThicknessId ? "Depth" :
                input.HoverRegionId == UiLayout.SetVoidsId ? "Voids" :
                input.HoverRegionId == UiLayout.SetHitsId ? "Fill" :
                input.HoverRegionId == UiLayout.SegmentModeId ? "Multiview" :
                input.HoverRegionId == UiLayout.SchematicCargoId ? "Cargo" :
                input.HoverRegionId == UiLayout.SchematicEnginesId ? "Thrust" :
                input.HoverRegionId == UiLayout.SchematicPowerId ? "Power" :
                input.HoverRegionId == UiLayout.SchematicOxygenId ? "Oxygen" :
                input.HoverRegionId == UiLayout.SchematicConveyorId ? "Conveyor" :
                input.HoverRegionId == UiLayout.CycleScanModeId ? "Scan Mode" :
                input.HoverRegionId == UiLayout.CycleScanColorScaleId ? "Scan Color" :
                input.HoverRegionId == UiLayout.RunScanId ? "Run Scan" :
                input.HoverRegionId == UiLayout.RecalibrateCursorId ? "Recalibrate" : "UI";

            return input.IsPressed ? "Touch: press " + label : "Touch: hover " + label;
        }

        static partial void DrawTopMenu(MySpriteDrawFrame frame, int screenWidth, int screenHeight, MenuPanel activeMenu, string activeView, bool showBlocks, bool showBorder, bool showHullScan, bool showGrid, bool showDebug, bool showPerfStats, bool showReference, bool showCenterOfMass, bool showPanelPosition, bool showDockedMobileGrids, bool showConveyorOverlay, bool showFillBars, bool showAllConnections, bool blurScan, string fillMode, string uiPalette, int uiHue, float uiBrightness, float uiSaturation, float uiAlpha, int uiAccentHue, float uiAccentBrightness, float uiAccentSaturation, float uiPanelBrightness, float uiPanelAlpha, int schematicMainHue, int schematicSecondaryHue, int conveyorHue, float hullScanAlpha, float schematicAlpha, string storageColor, string effectorColor, bool segmentMode, bool mouseControl, string mouseSensitivity, bool allowGridRotation, bool performanceMode, bool highResScanning, int settingsExpandedMask, string activeSettingsActionId, string hoverRegionId);

        static partial void DrawBottomSchematicButtons(MySpriteDrawFrame frame, int screenWidth, int screenHeight, OverlayMode activeOverlay, bool showConveyorOverlay, bool showInfoPanel, InfoPanelMode infoPanelMode, MenuPanel activeMenu, string fillMode, string hoverRegionId, bool includeThrust);

        static partial void DrawChromeRestoreButton(MySpriteDrawFrame frame, int screenWidth, int screenHeight, bool minimized, string hoverRegionId);

        static partial void DrawMenuPanelBackground(MySpriteDrawFrame frame, HitRegion[] regions);

        static partial void DrawViewButton(MySpriteDrawFrame frame, HitRegion region, string label, bool active, bool hover);

        static partial void DrawCursor(MySpriteDrawFrame frame, TouchScreenApiAdapter input, ScreenZone clipZone);

        static partial void DrawSharedGridCursor(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, SharedGridCursor? sharedCursor, long localPanelId, ScanView view, int rotationSteps);

        static partial void DrawShipGridOverlay(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, RawRaycastScanData scanData, string fillMode, bool showBlocks, bool showBorder, bool showHullScan, bool showGrid, bool showReference, bool blurScan, bool showDebug, int rotationSteps, int raycastStep);

        static partial void DrawShipReferenceMarkers(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, ScanCache cache, IMyCubeBlock panelBlock, bool showCenterOfMass, bool showPanelPosition, int rotationSteps);

                static partial void DrawDockedMobileGridBorders(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, ScanCache cache, bool showDockedMobileGrids, bool showBorder, int rotationSteps);

        static partial void DrawCargoOverlay(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, ScanCache cache, int rotationSteps, bool showAllConnections, TouchScreenApiAdapter input, UiState ui, bool occludeConveyorUnderFillBars);

        static partial void DrawEnginesOverlay(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, ScanCache cache, int rotationSteps, bool showAllConnections, TouchScreenApiAdapter input, UiState ui, bool occludeConveyorUnderFillBars);

        static partial void DrawOxygenOverlay(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, ScanCache cache, int rotationSteps, bool showAllConnections);

        static partial void DrawConveyorOverlay(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, ScanCache cache, int rotationSteps, bool showAllConnections);

        static partial void DrawPowerOverlay(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, ScanCache cache, int rotationSteps, bool showAllConnections, TouchScreenApiAdapter input, UiState ui, bool occludeConveyorUnderFillBars);

        static bool TryGetVisibleAxisLocalX(ShipGrid shipGrid, SharedGridCursor cursor, bool isSourcePanel, ScanView view, out float localX)
        {
            localX = 0f;
            if (view == ScanView.Side)
            {
                if (!cursor.HasZ)
                    return false;
                if (!isSourcePanel && !cursor.DirectZ)
                    return false;

                localX = (float)(cursor.Z - shipGrid.Min2D.X + 0.5);
                return true;
            }

            if (!cursor.HasX)
                return false;
            if (!isSourcePanel && !cursor.DirectX)
                return false;

            localX = (float)(cursor.X - shipGrid.Min2D.X + 0.5);
            return true;
        }

        static bool TryGetVisibleAxisLocalY(ShipGrid shipGrid, SharedGridCursor cursor, bool isSourcePanel, ScanView view, out float localY)
        {
            localY = 0f;
            if (view == ScanView.Top)
            {
                if (!cursor.HasZ)
                    return false;
                if (!isSourcePanel && !cursor.DirectZ)
                    return false;

                localY = (float)(shipGrid.Max2D.Y - cursor.Z + 0.5);
                return true;
            }

            if (!cursor.HasY)
                return false;
            if (!isSourcePanel && !cursor.DirectY)
                return false;

            localY = (float)(shipGrid.Max2D.Y - cursor.Y + 0.5);
            return true;
        }

        static void DrawCargoConveyorTopology(MySpriteDrawFrame frame, ProjectionTransform transform, ShipGrid shipGrid, ConveyorTopology topology, CargoConveyorFilter filter, bool showAllConnections)
        {
            if (topology == null || shipGrid == null || shipGrid.IsEmpty || !transform.IsValid)
                return;

            if (filter == null)
            {
                DrawCachedConveyorProjection(frame, transform, shipGrid, topology, showAllConnections);
                return;
            }

            const int maxLineSegments = 5000;
            int drawnSegments = 0;
            var activeInventoryLineKeys = GetActiveInventoryConveyorLineKeys(topology);

            if (topology.Lines != null)
            {
                for (int i = 0; i < topology.Lines.Count && drawnSegments < maxLineSegments; i++)
                {
                    var line = topology.Lines[i];
                    if (line == null || line.BasisPositions == null || line.BasisPositions.Count == 0)
                        continue;
                    if (filter != null && !filter.IsLineVisible(line.LineKey))
                        continue;

                    bool active = line.IsFunctional && line.IsWorking && !line.IsDisconnected && activeInventoryLineKeys.Contains(line.LineKey);
                    var color = active
                        ? ApplyConveyorHue(new Color(20, 220, 255, PipelineAlpha))
                        : new Color(255, 95, 55, PipelineAlpha);
                    float thickness = 1f;

                    if (line.BasisPositions.Count == 1)
                    {
                        DrawConveyorPoint(frame, transform, shipGrid, line.BasisPositions[0], color, 3f);
                        continue;
                    }

                    for (int p = 1; p < line.BasisPositions.Count && drawnSegments < maxLineSegments; p++)
                    {
                        var start = ProjectBasisCellCenter(transform, shipGrid, line.BasisPositions[p - 1]);
                        var end = ProjectBasisCellCenter(transform, shipGrid, line.BasisPositions[p]);
                        if ((end - start).LengthSquared() <= 0.25f)
                        {
                            DrawConveyorPoint(frame, transform, shipGrid, line.BasisPositions[p], color, 2.25f);
                            continue;
                        }

                        DrawScreenLine(frame, start, end, thickness, color);
                        drawnSegments++;
                    }
                }
            }

            DrawCargoConveyorBlockPassThroughs(frame, transform, shipGrid, topology, filter);
            if (showAllConnections)
                DrawCargoConveyorEndpointExtensions(frame, transform, shipGrid, topology, filter, true);
            DrawCargoConveyorPorts(frame, transform, shipGrid, topology, filter, drawnSegments == 0);
        }

        static void DrawCachedConveyorProjection(MySpriteDrawFrame frame, ProjectionTransform transform, ShipGrid shipGrid, ConveyorTopology topology, bool showAllConnections)
        {
            var cached = GetOrBuildConveyorProjection(transform, shipGrid, topology, showAllConnections);
            if (cached == null)
                return;

            for (int i = 0; i < cached.Lines.Count; i++)
            {
                var line = cached.Lines[i];
                CurrentPerfCachedSpriteCount++;
                if (line.Dashed)
                    DrawDashedScreenLine(frame, line.Start, line.End, line.Thickness, line.DashLength, line.GapLength, line.Color);
                else
                    DrawScreenLine(frame, line.Start, line.End, line.Thickness, line.Color);
            }

            for (int i = 0; i < cached.Points.Count; i++)
            {
                var point = cached.Points[i];
                AddCachedSprite(frame, new MySprite(SpriteType.TEXTURE, string.IsNullOrEmpty(point.Texture) ? "SquareSimple" : point.Texture, point.Center, point.Size, point.Color));
            }
        }

        static CachedConveyorProjection GetOrBuildConveyorProjection(ProjectionTransform transform, ShipGrid shipGrid, ConveyorTopology topology, bool showAllConnections)
        {
            string key = BuildConveyorProjectionCacheKey(transform, shipGrid, topology, showAllConnections);
            CachedConveyorProjection cached;
            if (ConveyorProjectionCache.TryGetValue(key, out cached) && cached != null)
            {
                TrackCacheHit();
                cached.LastUsed = ++CacheUseCounter;
                return cached;
            }
            TrackCacheMiss();

            cached = BuildConveyorProjection(transform, shipGrid, topology, showAllConnections);
            cached.LastUsed = ++CacheUseCounter;
            ConveyorProjectionCache[key] = cached;
            TrimConveyorProjectionCache();
            return cached;
        }

        static CachedConveyorProjection BuildConveyorProjection(ProjectionTransform transform, ShipGrid shipGrid, ConveyorTopology topology, bool showAllConnections)
        {
            var cached = new CachedConveyorProjection();
            const int maxLineSegments = 5000;
            int drawnSegments = 0;
            var activeInventoryLineKeys = GetActiveInventoryConveyorLineKeys(topology);

            if (topology.Lines != null)
            {
                for (int i = 0; i < topology.Lines.Count && drawnSegments < maxLineSegments; i++)
                {
                    var line = topology.Lines[i];
                    if (line == null || line.BasisPositions == null || line.BasisPositions.Count == 0)
                        continue;

                    bool active = line.IsFunctional && line.IsWorking && !line.IsDisconnected && activeInventoryLineKeys.Contains(line.LineKey);
                    var color = active
                        ? ApplyConveyorHue(new Color(20, 220, 255, PipelineAlpha))
                        : new Color(255, 95, 55, PipelineAlpha);

                    if (line.BasisPositions.Count == 1)
                    {
                        AddCachedConveyorPoint(cached, transform, shipGrid, line.BasisPositions[0], color, 3f);
                        continue;
                    }

                    for (int p = 1; p < line.BasisPositions.Count && drawnSegments < maxLineSegments; p++)
                    {
                        var start = ProjectBasisCellCenter(transform, shipGrid, line.BasisPositions[p - 1]);
                        var end = ProjectBasisCellCenter(transform, shipGrid, line.BasisPositions[p]);
                        if ((end - start).LengthSquared() <= 0.25f)
                        {
                            AddCachedConveyorPoint(cached, transform, shipGrid, line.BasisPositions[p], color, 2.25f);
                            continue;
                        }

                        cached.Lines.Add(new CachedScreenLine { Start = start, End = end, Thickness = 1f, Color = color });
                        drawnSegments++;
                    }
                }
            }

            AddCachedConveyorBlockPassThroughs(cached, transform, shipGrid, topology);
            if (showAllConnections)
                AddCachedConveyorEndpointExtensions(cached, transform, shipGrid, topology, true);
            AddCachedConveyorPorts(cached, transform, shipGrid, topology, drawnSegments == 0);
            return cached;
        }

        static string BuildConveyorProjectionCacheKey(ProjectionTransform transform, ShipGrid shipGrid, ConveyorTopology topology, bool showAllConnections)
        {
            return BuildTopologyCacheKey(topology, "conveyor-projection") + ":" +
                shipGrid.GetHashCode() + ":" +
                shipGrid.ProjectionView + ":" +
                transform.RotationSteps + ":" +
                (int)(transform.Origin.X * 10f) + ":" +
                (int)(transform.Origin.Y * 10f) + ":" +
                (int)(transform.CellSize * 100f) + ":" +
                transform.SourceWidth + ":" +
                transform.SourceHeight + ":" +
                showAllConnections + ":" +
                CurrentConveyorHue;
        }

        static void TrimConveyorProjectionCache()
        {
            if (ConveyorProjectionCache.Count <= MaxConveyorProjectionCacheEntries)
                return;

            string oldestKey = null;
            int oldestUse = int.MaxValue;
            foreach (var entry in ConveyorProjectionCache)
            {
                if (entry.Value != null && entry.Value.LastUsed < oldestUse)
                {
                    oldestUse = entry.Value.LastUsed;
                    oldestKey = entry.Key;
                }
            }

            if (oldestKey != null)
                ConveyorProjectionCache.Remove(oldestKey);
        }

        class CargoConveyorFilter
        {
            public HashSet<int> VisibleLineKeys = new HashSet<int>();

            public bool IsLineVisible(int lineKey)
            {
                return VisibleLineKeys.Contains(lineKey);
            }

            public bool IsPortVisible(ConveyorPort port)
            {
                return port != null && VisibleLineKeys.Contains(port.LineKey);
            }
        }

        static HashSet<int> BuildActiveInventoryConveyorLineKeys(ConveyorTopology topology)
        {
            var active = new HashSet<int>();
            if (topology == null || topology.Lines == null || topology.Ports == null || topology.Nodes == null)
                return active;

            var graph = new Dictionary<int, List<int>>();
            for (int i = 0; i < topology.Lines.Count; i++)
            {
                var line = topology.Lines[i];
                if (line != null && line.IsFunctional && line.IsWorking && !line.IsDisconnected)
                    EnsureGraphNode(graph, line.LineKey);
            }

            var seedLines = new HashSet<int>();
            var linesByNode = new Dictionary<long, List<int>>();
            int maxPorts = Math.Min(topology.Ports.Count, 1200);
            for (int i = 0; i < maxPorts; i++)
            {
                var port = topology.Ports[i];
                if (port == null || !port.HasLine || !port.IsConnected || !graph.ContainsKey(port.LineKey))
                    continue;

                ConveyorNode node;
                if (!TryFindNearestConveyorNode(topology.Nodes, port, out node))
                    continue;

                AddLineToNode(linesByNode, node.BlockEntityId, port.LineKey);
                if (IsActiveInventoryNetworkNode(node))
                    seedLines.Add(port.LineKey);
            }

            foreach (var lineKeys in linesByNode.Values)
                ConnectLineKeys(graph, lineKeys);

            var queue = new Queue<int>();
            foreach (var lineKey in seedLines)
            {
                if (!graph.ContainsKey(lineKey) || active.Contains(lineKey))
                    continue;

                active.Add(lineKey);
                queue.Enqueue(lineKey);
            }

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                List<int> neighbors;
                if (!graph.TryGetValue(current, out neighbors))
                    continue;

                for (int i = 0; i < neighbors.Count; i++)
                {
                    int next = neighbors[i];
                    if (active.Contains(next))
                        continue;

                    active.Add(next);
                    queue.Enqueue(next);
                }
            }

            return active;
        }

        static HashSet<int> GetActiveInventoryConveyorLineKeys(ConveyorTopology topology)
        {
            if (topology == null)
                return new HashSet<int>();

            string key = BuildTopologyCacheKey(topology, "active-inventory");
            CachedConveyorLineKeys cached;
            if (ActiveInventoryLineKeyCache.TryGetValue(key, out cached) && cached != null)
            {
                TrackCacheHit();
                cached.LastUsed = ++CacheUseCounter;
                return cached.Keys;
            }
            TrackCacheMiss();

            cached = new CachedConveyorLineKeys();
            cached.Keys = BuildActiveInventoryConveyorLineKeys(topology);
            cached.LastUsed = ++CacheUseCounter;
            ActiveInventoryLineKeyCache[key] = cached;
            TrimConveyorLineKeyCache();
            return cached.Keys;
        }

        static string BuildTopologyCacheKey(ConveyorTopology topology, string purpose)
        {
            int lineCount = topology.Lines != null ? topology.Lines.Count : 0;
            int portCount = topology.Ports != null ? topology.Ports.Count : 0;
            int nodeCount = topology.Nodes != null ? topology.Nodes.Count : 0;
            int edgeCount = topology.Edges != null ? topology.Edges.Count : 0;
            int identity = topology.GetHashCode();
            return purpose + ":" + identity + ":" + lineCount + ":" + portCount + ":" + nodeCount + ":" + edgeCount;
        }

        static void TrimConveyorLineKeyCache()
        {
            if (ActiveInventoryLineKeyCache.Count <= MaxTopologyKeyCacheEntries)
                return;

            string oldestKey = null;
            int oldestUse = int.MaxValue;
            foreach (var entry in ActiveInventoryLineKeyCache)
            {
                if (entry.Value != null && entry.Value.LastUsed < oldestUse)
                {
                    oldestUse = entry.Value.LastUsed;
                    oldestKey = entry.Key;
                }
            }

            if (oldestKey != null)
                ActiveInventoryLineKeyCache.Remove(oldestKey);
        }

        static bool IsActiveInventoryNetworkNode(ConveyorNode node)
        {
            if (node == null || !node.IsFunctional || !node.IsWorking || string.IsNullOrEmpty(node.BlockType))
                return false;

            string id = node.BlockType.ToLowerInvariant();
            return id.Contains("cargo") ||
                id.Contains("container") ||
                id.Contains("connector") ||
                id.Contains("collector") ||
                id.Contains("ejector") ||
                id.Contains("refinery") ||
                id.Contains("assembler") ||
                id.Contains("survival") ||
                id.Contains("oxygen") ||
                id.Contains("hydrogen") ||
                id.Contains("tank");
        }

        static CargoConveyorFilter BuildCargoConveyorFilter(ConveyorTopology topology)
        {
            var filter = new CargoConveyorFilter();
            if (topology == null || topology.Lines == null || topology.Ports == null || topology.Nodes == null)
                return filter;

            var graph = new Dictionary<int, List<int>>();
            for (int i = 0; i < topology.Lines.Count; i++)
            {
                var line = topology.Lines[i];
                if (line != null)
                    EnsureGraphNode(graph, line.LineKey);
            }

            var storageSeeds = new HashSet<int>();
            var effectorSeeds = new HashSet<int>();
            var linesByNode = new Dictionary<long, List<int>>();
            int maxPorts = Math.Min(topology.Ports.Count, 1200);
            for (int i = 0; i < maxPorts; i++)
            {
                var port = topology.Ports[i];
                if (port == null || !port.HasLine)
                    continue;

                EnsureGraphNode(graph, port.LineKey);
                AddCargoFilterPortMatches(topology.Nodes, port, linesByNode, storageSeeds, effectorSeeds);
            }

            foreach (var lineKeys in linesByNode.Values)
                ConnectLineKeys(graph, lineKeys);

            foreach (var lineKey in storageSeeds)
                filter.VisibleLineKeys.Add(lineKey);

            MarkCargoPaths(graph, storageSeeds, effectorSeeds, filter.VisibleLineKeys);
            return filter;
        }

        static CargoConveyorFilter BuildEnginesConveyorFilter(ConveyorTopology topology)
        {
            var filter = new CargoConveyorFilter();
            if (topology == null || topology.Lines == null || topology.Ports == null || topology.Nodes == null)
                return filter;

            var graph = new Dictionary<int, List<int>>();
            for (int i = 0; i < topology.Lines.Count; i++)
            {
                var line = topology.Lines[i];
                if (line != null)
                    EnsureGraphNode(graph, line.LineKey);
            }

            var storageSeeds = new HashSet<int>();
            var effectorSeeds = new HashSet<int>();
            var linesByNode = new Dictionary<long, List<int>>();
            int maxPorts = Math.Min(topology.Ports.Count, 1200);
            for (int i = 0; i < maxPorts; i++)
            {
                var port = topology.Ports[i];
                if (port == null || !port.HasLine)
                    continue;

                EnsureGraphNode(graph, port.LineKey);
                AddEnginesFilterPortMatches(topology.Nodes, port, linesByNode, storageSeeds, effectorSeeds);
            }

            foreach (var lineKeys in linesByNode.Values)
                ConnectLineKeys(graph, lineKeys);

            MarkCargoPaths(graph, storageSeeds, effectorSeeds, filter.VisibleLineKeys);
            return filter;
        }

        static void EnsureGraphNode(Dictionary<int, List<int>> graph, int lineKey)
        {
            if (!graph.ContainsKey(lineKey))
                graph[lineKey] = new List<int>();
        }

        static void AddLineToNode(Dictionary<long, List<int>> linesByNode, long nodeId, int lineKey)
        {
            List<int> lineKeys;
            if (!linesByNode.TryGetValue(nodeId, out lineKeys))
            {
                lineKeys = new List<int>();
                linesByNode[nodeId] = lineKeys;
            }

            if (!lineKeys.Contains(lineKey))
                lineKeys.Add(lineKey);
        }

        static void AddCargoFilterPortMatches(List<ConveyorNode> nodes, ConveyorPort port, Dictionary<long, List<int>> linesByNode, HashSet<int> storageSeeds, HashSet<int> effectorSeeds)
        {
            if (nodes == null || port == null)
                return;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node == null || node.GridEntityId != port.GridEntityId)
                    continue;

                if (!IsNearNodeBounds(port.LocalGridPosition, node, 1))
                    continue;

                if (DistanceSquaredToNodeBounds(port.LocalGridPosition, node) >= 18.0)
                    continue;

                AddLineToNode(linesByNode, node.BlockEntityId, port.LineKey);

                CargoOverlayRole role;
                if (TryGetCargoOverlayRoleId(node.BlockType, out role))
                {
                    if (role == CargoOverlayRole.Storage)
                        storageSeeds.Add(port.LineKey);
                    else
                        effectorSeeds.Add(port.LineKey);
                }
            }
        }

        static void AddEnginesFilterPortMatches(List<ConveyorNode> nodes, ConveyorPort port, Dictionary<long, List<int>> linesByNode, HashSet<int> storageSeeds, HashSet<int> effectorSeeds)
        {
            if (nodes == null || port == null)
                return;

            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node == null || node.GridEntityId != port.GridEntityId)
                    continue;

                if (!IsNearNodeBounds(port.LocalGridPosition, node, 1))
                    continue;

                if (DistanceSquaredToNodeBounds(port.LocalGridPosition, node) >= 18.0)
                    continue;

                AddLineToNode(linesByNode, node.BlockEntityId, port.LineKey);

                CargoOverlayRole role;
                if (TryGetEnginesOverlayRoleId(node.BlockType, out role))
                {
                    if (role == CargoOverlayRole.Storage)
                        storageSeeds.Add(port.LineKey);
                    else
                        effectorSeeds.Add(port.LineKey);
                }
            }
        }

        static void ConnectLineKeys(Dictionary<int, List<int>> graph, List<int> lineKeys)
        {
            if (lineKeys == null || lineKeys.Count < 2)
                return;

            for (int i = 0; i < lineKeys.Count; i++)
            {
                for (int j = i + 1; j < lineKeys.Count; j++)
                    AddGraphEdge(graph, lineKeys[i], lineKeys[j]);
            }
        }

        static void AddGraphEdge(Dictionary<int, List<int>> graph, int a, int b)
        {
            EnsureGraphNode(graph, a);
            EnsureGraphNode(graph, b);
            if (!graph[a].Contains(b))
                graph[a].Add(b);
            if (!graph[b].Contains(a))
                graph[b].Add(a);
        }

        static void MarkCargoPaths(Dictionary<int, List<int>> graph, HashSet<int> storageSeeds, HashSet<int> effectorSeeds, HashSet<int> visibleLineKeys)
        {
            if (graph == null || storageSeeds == null || effectorSeeds == null || visibleLineKeys == null ||
                storageSeeds.Count == 0 || effectorSeeds.Count == 0)
                return;

            var parent = new Dictionary<int, int>();
            var queue = new Queue<int>();
            foreach (var lineKey in storageSeeds)
            {
                if (!graph.ContainsKey(lineKey) || parent.ContainsKey(lineKey))
                    continue;

                parent[lineKey] = int.MinValue;
                queue.Enqueue(lineKey);
            }

            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                List<int> neighbors;
                if (!graph.TryGetValue(current, out neighbors))
                    continue;

                for (int i = 0; i < neighbors.Count; i++)
                {
                    int next = neighbors[i];
                    if (parent.ContainsKey(next))
                        continue;

                    parent[next] = current;
                    queue.Enqueue(next);
                }
            }

            foreach (var lineKey in effectorSeeds)
                MarkPathToStorage(parent, lineKey, visibleLineKeys);
        }

        static void MarkPathToStorage(Dictionary<int, int> parent, int lineKey, HashSet<int> visibleLineKeys)
        {
            if (parent == null || visibleLineKeys == null || !parent.ContainsKey(lineKey))
                return;

            int current = lineKey;
            int guard = 0;
            while (current != int.MinValue && guard++ < 5000)
            {
                visibleLineKeys.Add(current);
                current = parent[current];
            }
        }

        static void AddCachedConveyorEndpointExtensions(CachedConveyorProjection cached, ProjectionTransform transform, ShipGrid shipGrid, ConveyorTopology topology, bool diagnosticLinks)
        {
            if (cached == null || topology == null || topology.Ports == null || topology.Nodes == null)
                return;

            int maxPorts = Math.Min(topology.Ports.Count, 1200);
            for (int i = 0; i < maxPorts; i++)
            {
                var port = topology.Ports[i];
                if (port == null || !port.HasLine)
                    continue;

                ConveyorNode node;
                if (!TryFindNearestConveyorNode(topology.Nodes, port, out node))
                    continue;

                var start = ProjectBasisPoint(transform, shipGrid, port.BasisPosition);
                var end = ProjectBasisPoint(transform, shipGrid, node.BasisCenter);
                float minExtensionLength = Math.Max(1.5f, transform.CellSize * 0.65f);
                float minLengthSq = minExtensionLength * minExtensionLength * 0.4f;
                if ((end - start).LengthSquared() <= minLengthSq)
                    continue;

                bool axisAligned = IsMostlyAxisAligned(end - start);
                if (!diagnosticLinks && !axisAligned)
                    continue;
                if (diagnosticLinks && axisAligned)
                    continue;

                var color = port.IsConnected
                    ? ApplyConveyorHue(new Color(20, 220, 255, PipelineAlpha))
                    : ApplyConveyorHue(new Color(40, 105, 255, PipelineAlpha));
                cached.Lines.Add(new CachedScreenLine
                {
                    Start = start,
                    End = end,
                    Thickness = diagnosticLinks ? 1f : 1.25f,
                    DashLength = 5f,
                    GapLength = 4f,
                    Color = color,
                    Dashed = diagnosticLinks
                });
            }
        }

        static void AddCachedConveyorBlockPassThroughs(CachedConveyorProjection cached, ProjectionTransform transform, ShipGrid shipGrid, ConveyorTopology topology)
        {
            if (cached == null || topology == null || topology.Ports == null || topology.Nodes == null)
                return;

            var portsByNode = new Dictionary<long, ConveyorBlockPortSet>();
            int maxPorts = Math.Min(topology.Ports.Count, 1200);
            for (int i = 0; i < maxPorts; i++)
            {
                var port = topology.Ports[i];
                if (port == null || !port.HasLine || !port.IsConnected)
                    continue;

                ConveyorNode node;
                if (!TryFindNearestConveyorNode(topology.Nodes, port, out node))
                    continue;
                if (!IsLargeConveyorNode(node))
                    continue;
                if (!IsPortOnNodeFace(port, node))
                    continue;

                ConveyorBlockPortSet set;
                if (!portsByNode.TryGetValue(node.BlockEntityId, out set))
                {
                    set = new ConveyorBlockPortSet();
                    set.Node = node;
                    portsByNode[node.BlockEntityId] = set;
                }

                if (!ContainsConveyorPort(set.Ports, port))
                    set.Ports.Add(port);
            }

            foreach (var set in portsByNode.Values)
            {
                if (set == null || set.Node == null || set.Ports == null || set.Ports.Count < 2)
                    continue;
                if (CountDistinctLineKeys(set.Ports) < 2)
                    continue;

                AddCachedConveyorBlockPortJunction(cached, transform, shipGrid, set.Node, set.Ports, ApplyConveyorHue(new Color(20, 220, 255, PipelineAlpha)));
            }
        }

        static void AddCachedConveyorBlockPortJunction(CachedConveyorProjection cached, ProjectionTransform transform, ShipGrid shipGrid, ConveyorNode node, List<ConveyorPort> ports, Color color)
        {
            if (cached == null || node == null || ports == null || ports.Count < 2)
                return;

            var junction = SnapPoint(ProjectBasisPoint(transform, shipGrid, node.BasisCenter));
            const float bridgeThickness = 1f;
            float bridgeDotLength = bridgeThickness;
            float bridgeGapLength = bridgeDotLength * 2f;
            for (int i = 0; i < ports.Count; i++)
            {
                var port = ports[i];
                if (port == null)
                    continue;

                var portPoint = SnapPoint(ProjectBasisPoint(transform, shipGrid, port.BasisPosition));
                Vector2 elbow;
                if (TryGetAxisAlignedJunctionElbow(portPoint, junction, out elbow))
                {
                    cached.Lines.Add(new CachedScreenLine { Start = portPoint, End = elbow, Thickness = bridgeThickness, DashLength = bridgeDotLength, GapLength = bridgeGapLength, Color = color, Dashed = true });
                    cached.Lines.Add(new CachedScreenLine { Start = elbow, End = junction, Thickness = bridgeThickness, DashLength = bridgeDotLength, GapLength = bridgeGapLength, Color = color, Dashed = true });
                }
                else
                {
                    cached.Lines.Add(new CachedScreenLine { Start = portPoint, End = junction, Thickness = bridgeThickness, DashLength = bridgeDotLength, GapLength = bridgeGapLength, Color = color, Dashed = true });
                }
            }
        }

        static void AddCachedConveyorPorts(CachedConveyorProjection cached, ProjectionTransform transform, ShipGrid shipGrid, ConveyorTopology topology, bool prominent)
        {
            if (cached == null || topology == null || topology.Ports == null || topology.Nodes == null)
                return;

            var nodesById = new Dictionary<long, ConveyorNode>();
            for (int i = 0; i < topology.Nodes.Count; i++)
            {
                var node = topology.Nodes[i];
                if (node != null)
                    nodesById[node.BlockEntityId] = node;
            }

            int maxPorts = Math.Min(topology.Ports.Count, 1200);
            float size = prominent ? 6.5f : 4.75f;
            var drawnSorterNodes = new HashSet<long>();
            for (int i = 0; i < maxPorts; i++)
            {
                var port = topology.Ports[i];
                if (port == null)
                    continue;

                ConveyorNode node;
                var basisPosition = nodesById.TryGetValue(port.NodeId, out node)
                    ? node.BasisPosition + port.DirectionVector
                    : port.BasisPosition;
                if (IsConveyorSorterNode(node))
                {
                    if (drawnSorterNodes.Add(node.BlockEntityId))
                        AddCachedConveyorIcon(cached, transform, shipGrid, node.BasisCenter, SorterIconTexture, ApplyConveyorHue(new Color(20, 220, 255, PipelineAlpha)), prominent ? 11f : 8.5f);
                    continue;
                }

                var color = port.IsConnected
                    ? ApplyConveyorHue(new Color(20, 220, 255, PipelineAlpha))
                    : ApplyConveyorHue(new Color(40, 105, 255, PipelineAlpha));
                AddCachedConveyorPoint(cached, transform, shipGrid, basisPosition, color, size);
            }
        }

        static void AddCachedConveyorPoint(CachedConveyorProjection cached, ProjectionTransform transform, ShipGrid shipGrid, Vector3I basisPosition, Color color, float size)
        {
            if (cached == null)
                return;

            var point = SnapPoint(ProjectBasisCellCenter(transform, shipGrid, basisPosition));
            float pixelSize = SnapPixelSize(size);
            cached.Points.Add(new CachedScreenRect
            {
                Center = point,
                Size = new Vector2(pixelSize, pixelSize),
                Color = color
            });
        }

        static void AddCachedConveyorIcon(CachedConveyorProjection cached, ProjectionTransform transform, ShipGrid shipGrid, Vector3D basisPosition, string texture, Color color, float size)
        {
            if (cached == null)
                return;

            var point = SnapPoint(ProjectBasisPoint(transform, shipGrid, basisPosition));
            float pixelSize = SnapPixelSize(size);
            cached.Points.Add(new CachedScreenRect
            {
                Center = point,
                Size = new Vector2(pixelSize, pixelSize),
                Color = color,
                Texture = texture
            });
        }

        static void DrawCargoConveyorEndpointExtensions(MySpriteDrawFrame frame, ProjectionTransform transform, ShipGrid shipGrid, ConveyorTopology topology, CargoConveyorFilter filter, bool diagnosticLinks)
        {
            if (topology == null || topology.Ports == null || topology.Nodes == null)
                return;

            int maxPorts = Math.Min(topology.Ports.Count, 1200);
            for (int i = 0; i < maxPorts; i++)
            {
                var port = topology.Ports[i];
                if (port == null || !port.HasLine)
                    continue;
                if (filter != null && !filter.IsPortVisible(port))
                    continue;

                ConveyorNode node;
                if (!TryFindNearestConveyorNode(topology.Nodes, port, out node))
                    continue;

                var start = ProjectBasisPoint(transform, shipGrid, port.BasisPosition);
                var end = ProjectBasisPoint(transform, shipGrid, node.BasisCenter);
                float minExtensionLength = Math.Max(1.5f, transform.CellSize * 0.65f);
                float minLengthSq = minExtensionLength * minExtensionLength * 0.4f;
                if ((end - start).LengthSquared() <= minLengthSq)
                    continue;

                bool axisAligned = IsMostlyAxisAligned(end - start);
                if (!diagnosticLinks && !axisAligned)
                    continue;
                if (diagnosticLinks && axisAligned)
                    continue;

                var color = port.IsConnected
                    ? ApplyConveyorHue(new Color(20, 220, 255, PipelineAlpha))
                    : ApplyConveyorHue(new Color(40, 105, 255, PipelineAlpha));
                if (diagnosticLinks)
                    DrawDashedScreenLine(frame, start, end, 1f, 5f, 4f, color);
                else
                    DrawScreenLine(frame, start, end, 1.25f, color);
            }
        }

        static void DrawCargoConveyorBlockPassThroughs(MySpriteDrawFrame frame, ProjectionTransform transform, ShipGrid shipGrid, ConveyorTopology topology, CargoConveyorFilter filter)
        {
            if (topology == null || topology.Ports == null || topology.Nodes == null)
                return;

            var portsByNode = new Dictionary<long, ConveyorBlockPortSet>();
            int maxPorts = Math.Min(topology.Ports.Count, 1200);
            for (int i = 0; i < maxPorts; i++)
            {
                var port = topology.Ports[i];
                if (port == null || !port.HasLine || !port.IsConnected)
                    continue;
                if (filter != null && !filter.IsPortVisible(port))
                    continue;

                ConveyorNode node;
                if (!TryFindNearestConveyorNode(topology.Nodes, port, out node))
                    continue;
                if (!IsLargeConveyorNode(node))
                    continue;
                if (!IsPortOnNodeFace(port, node))
                    continue;

                ConveyorBlockPortSet set;
                if (!portsByNode.TryGetValue(node.BlockEntityId, out set))
                {
                    set = new ConveyorBlockPortSet();
                    set.Node = node;
                    portsByNode[node.BlockEntityId] = set;
                }

                if (!ContainsConveyorPort(set.Ports, port))
                    set.Ports.Add(port);
            }

            foreach (var set in portsByNode.Values)
            {
                if (set == null || set.Node == null || set.Ports == null || set.Ports.Count < 2)
                    continue;
                if (CountDistinctLineKeys(set.Ports) < 2)
                    continue;

                var color = ApplyConveyorHue(new Color(20, 220, 255, PipelineAlpha));
                DrawConveyorBlockPortJunction(frame, transform, shipGrid, set.Node, set.Ports, color);
            }
        }

        class ConveyorBlockPortSet
        {
            public ConveyorNode Node;
            public List<ConveyorPort> Ports = new List<ConveyorPort>();
        }

        static bool IsLargeConveyorNode(ConveyorNode node)
        {
            if (node == null)
                return false;

            int sx = Math.Abs(node.GridMax.X - node.GridMin.X) + 1;
            int sy = Math.Abs(node.GridMax.Y - node.GridMin.Y) + 1;
            int sz = Math.Abs(node.GridMax.Z - node.GridMin.Z) + 1;
            return sx * sy * sz > 1;
        }

        static bool IsPortOnNodeFace(ConveyorPort port, ConveyorNode node)
        {
            if (port == null || node == null)
                return false;

            return IsInsideNodeBounds(port.LocalGridPosition, node) ||
                IsInsideNodeBounds(port.NeighborGridPosition, node);
        }

        static bool IsInsideNodeBounds(Vector3I position, ConveyorNode node)
        {
            return position.X >= node.GridMin.X && position.X <= node.GridMax.X &&
                position.Y >= node.GridMin.Y && position.Y <= node.GridMax.Y &&
                position.Z >= node.GridMin.Z && position.Z <= node.GridMax.Z;
        }

        static bool ContainsConveyorPort(List<ConveyorPort> ports, ConveyorPort port)
        {
            if (ports == null || port == null)
                return false;

            for (int i = 0; i < ports.Count; i++)
            {
                if (ports[i] != null && ports[i].Id == port.Id)
                    return true;
            }

            return false;
        }

        static int CountDistinctLineKeys(List<ConveyorPort> ports)
        {
            if (ports == null)
                return 0;

            var lineKeys = new HashSet<int>();
            for (int i = 0; i < ports.Count; i++)
            {
                var port = ports[i];
                if (port != null)
                    lineKeys.Add(port.LineKey);
            }

            return lineKeys.Count;
        }

        static void DrawConveyorBlockPortJunction(MySpriteDrawFrame frame, ProjectionTransform transform, ShipGrid shipGrid, ConveyorNode node, List<ConveyorPort> ports, Color color)
        {
            if (node == null || ports == null || ports.Count < 2)
                return;

            var junction = SnapPoint(ProjectBasisPoint(transform, shipGrid, node.BasisCenter));
            const float bridgeThickness = 1f;
            float bridgeDotLength = bridgeThickness;
            float bridgeGapLength = bridgeDotLength * 2f;
            for (int i = 0; i < ports.Count; i++)
            {
                var port = ports[i];
                if (port == null)
                    continue;

                var portPoint = SnapPoint(ProjectBasisPoint(transform, shipGrid, port.BasisPosition));
                Vector2 elbow;
                if (TryGetAxisAlignedJunctionElbow(portPoint, junction, out elbow))
                {
                    DrawDashedScreenLine(frame, portPoint, elbow, bridgeThickness, bridgeDotLength, bridgeGapLength, color);
                    DrawDashedScreenLine(frame, elbow, junction, bridgeThickness, bridgeDotLength, bridgeGapLength, color);
                }
                else
                {
                    DrawDashedScreenLine(frame, portPoint, junction, bridgeThickness, bridgeDotLength, bridgeGapLength, color);
                }
            }
        }

        static bool TryGetAxisAlignedJunctionElbow(Vector2 portPoint, Vector2 junction, out Vector2 elbow)
        {
            elbow = Vector2.Zero;
            float dx = Math.Abs(portPoint.X - junction.X);
            float dy = Math.Abs(portPoint.Y - junction.Y);
            if (dx <= 0.5f || dy <= 0.5f)
                return false;

            elbow = dx >= dy
                ? new Vector2(junction.X, portPoint.Y)
                : new Vector2(portPoint.X, junction.Y);
            elbow = SnapPoint(elbow);
            return true;
        }

        static bool IsMostlyAxisAligned(Vector2 delta)
        {
            float ax = Math.Abs(delta.X);
            float ay = Math.Abs(delta.Y);
            float major = Math.Max(ax, ay);
            float minor = Math.Min(ax, ay);
            if (major <= 0.001f)
                return false;

            return minor / major <= 0.18f;
        }

        static bool TryFindNearestConveyorNode(List<ConveyorNode> nodes, ConveyorPort port, out ConveyorNode nearest)
        {
            nearest = null;
            if (nodes == null || port == null)
                return false;

            double bestDistance = 18.0;
            for (int i = 0; i < nodes.Count; i++)
            {
                var node = nodes[i];
                if (node == null || node.GridEntityId != port.GridEntityId)
                    continue;

                if (!IsNearNodeBounds(port.LocalGridPosition, node, 1))
                    continue;

                double distance = DistanceSquaredToNodeBounds(port.LocalGridPosition, node);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                nearest = node;
            }

            return nearest != null;
        }

        static bool IsNearNodeBounds(Vector3I position, ConveyorNode node, int padding)
        {
            return position.X >= node.GridMin.X - padding && position.X <= node.GridMax.X + padding &&
                position.Y >= node.GridMin.Y - padding && position.Y <= node.GridMax.Y + padding &&
                position.Z >= node.GridMin.Z - padding && position.Z <= node.GridMax.Z + padding;
        }

        static double DistanceSquaredToNodeBounds(Vector3I position, ConveyorNode node)
        {
            int dx = DistanceToRange(position.X, node.GridMin.X, node.GridMax.X);
            int dy = DistanceToRange(position.Y, node.GridMin.Y, node.GridMax.Y);
            int dz = DistanceToRange(position.Z, node.GridMin.Z, node.GridMax.Z);
            return dx * dx + dy * dy + dz * dz;
        }

        static int DistanceToRange(int value, int min, int max)
        {
            if (value < min)
                return min - value;
            if (value > max)
                return value - max;
            return 0;
        }

        static void DrawCargoConveyorPorts(MySpriteDrawFrame frame, ProjectionTransform transform, ShipGrid shipGrid, ConveyorTopology topology, CargoConveyorFilter filter, bool prominent)
        {
            if (topology == null || topology.Ports == null || topology.Nodes == null)
                return;

            var nodesById = new Dictionary<long, ConveyorNode>();
            for (int i = 0; i < topology.Nodes.Count; i++)
            {
                var node = topology.Nodes[i];
                if (node != null)
                    nodesById[node.BlockEntityId] = node;
            }

            int maxPorts = Math.Min(topology.Ports.Count, 1200);
            float size = prominent ? 6.5f : 4.75f;
            var drawnSorterNodes = new HashSet<long>();
            for (int i = 0; i < maxPorts; i++)
            {
                var port = topology.Ports[i];
                if (port == null)
                    continue;
                if (filter != null && !filter.IsPortVisible(port))
                    continue;

                ConveyorNode node;
                var basisPosition = nodesById.TryGetValue(port.NodeId, out node)
                    ? node.BasisPosition + port.DirectionVector
                    : port.BasisPosition;
                if (IsConveyorSorterNode(node))
                {
                    if (drawnSorterNodes.Add(node.BlockEntityId))
                        DrawConveyorIcon(frame, transform, shipGrid, node.BasisCenter, SorterIconTexture, ApplyConveyorHue(new Color(20, 220, 255, PipelineAlpha)), prominent ? 11f : 8.5f);
                    continue;
                }

                var color = port.IsConnected
                    ? ApplyConveyorHue(new Color(20, 220, 255, PipelineAlpha))
                    : ApplyConveyorHue(new Color(40, 105, 255, PipelineAlpha));
                DrawConveyorPoint(frame, transform, shipGrid, basisPosition, color, size);
            }
        }

        static bool IsConveyorSorterNode(ConveyorNode node)
        {
            if (node == null || string.IsNullOrEmpty(node.BlockType))
                return false;

            return node.BlockType.IndexOf("Sorter", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static void DrawConveyorPoint(MySpriteDrawFrame frame, ProjectionTransform transform, ShipGrid shipGrid, Vector3I basisPosition, Color color, float size)
        {
            var point = SnapPoint(ProjectBasisCellCenter(transform, shipGrid, basisPosition));
            float pixelSize = SnapPixelSize(size);
            AddSprite(frame, new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                point,
                new Vector2(pixelSize, pixelSize),
                color));
        }

        static void DrawConveyorIcon(MySpriteDrawFrame frame, ProjectionTransform transform, ShipGrid shipGrid, Vector3D basisPosition, string texture, Color color, float size)
        {
            var point = SnapPoint(ProjectBasisPoint(transform, shipGrid, basisPosition));
            float pixelSize = SnapPixelSize(size);
            AddSprite(frame, new MySprite(
                SpriteType.TEXTURE,
                texture,
                point,
                new Vector2(pixelSize, pixelSize),
                color));
        }

        static Vector2 ProjectBasisCellCenter(ProjectionTransform transform, ShipGrid shipGrid, Vector3I basisPosition)
        {
            return ProjectBasisPoint(transform, shipGrid, new Vector3D(basisPosition.X, basisPosition.Y, basisPosition.Z));
        }

        static Vector2 ProjectBasisPoint(ProjectionTransform transform, ShipGrid shipGrid, Vector3D basisPosition)
        {
            var projected = Project(shipGrid, basisPosition);
            float localX = projected.X - shipGrid.Min2D.X + 0.5f;
            float localY = shipGrid.Max2D.Y - projected.Y + 0.5f;
            return transform.ProjectLocalPoint(localX, localY);
        }

        static Vector2 Project(ShipGrid shipGrid, Vector3D position)
        {
            switch (shipGrid.ProjectionView)
            {
                case ScanView.Front:
                    return new Vector2((float)position.X, (float)position.Y);
                case ScanView.Side:
                    return new Vector2((float)position.Z, (float)position.Y);
                default:
                    return new Vector2((float)position.X, (float)position.Z);
            }
        }

        enum CargoOverlayRole
        {
            Storage,
            Effector
        }

        struct ProjectedBounds2D
        {
            public Vector2I Min;
            public Vector2I Max;
        }

        class CargoOverlayGroup
        {
            public Vector2I Min;
            public Vector2I Max;
            public CargoOverlayRole Role;
            public List<float> FillRatios = new List<float>();
            public List<bool> EnabledStates = new List<bool>();
            public List<CargoConnectorIndicator> ConnectorIndicators = new List<CargoConnectorIndicator>();
            public List<IMyCubeBlock> Blocks = new List<IMyCubeBlock>();
        }

        class OverlayBlockSource
        {
            public Vector2I Min;
            public Vector2I Max;
            public CargoOverlayRole Role;
            public IMyCubeBlock Block;
        }

        struct CargoConnectorIndicator
        {
            public Vector2 Direction;
            public bool Connected;
            public bool FacingView;
        }

        static List<OverlayBlockSource> GetOverlaySources(ScanCache cache, ShipGrid shipGrid, string modeName)
        {
            return GetOverlaySources(cache, shipGrid, modeName, false);
        }

        static List<OverlayBlockSource> GetOverlaySources(ScanCache cache, ShipGrid shipGrid, string modeName, bool includeDockedMobileGrids)
        {
            if (cache == null || shipGrid == null || cache.ConstructGrids == null)
                return new List<OverlayBlockSource>();

            string key = BuildOverlaySourceCacheKey(cache, shipGrid, modeName, includeDockedMobileGrids);
            CachedOverlaySources cached;
            if (OverlaySourceCache.TryGetValue(key, out cached) && cached != null)
            {
                TrackCacheHit();
                cached.LastUsed = ++CacheUseCounter;
                return cached.Sources;
            }
            TrackCacheMiss();

            cached = new CachedOverlaySources();
            BuildOverlaySources(cache, shipGrid, modeName, cached.Sources, includeDockedMobileGrids);
            cached.LastUsed = ++CacheUseCounter;
            OverlaySourceCache[key] = cached;
            TrimOverlaySourceCache();
            return cached.Sources;
        }

        static void BuildOverlaySources(ScanCache cache, ShipGrid shipGrid, string modeName, List<OverlayBlockSource> sources, bool includeDockedMobileGrids)
        {
            for (int i = 0; i < cache.ConstructGrids.Count; i++)
                AppendOverlaySourcesForGrid(cache.ConstructGrids[i], shipGrid, modeName, sources);

            if (!includeDockedMobileGrids || cache.HullScanTargetGrids == null)
                return;

            for (int i = 0; i < cache.HullScanTargetGrids.Count; i++)
            {
                var grid = cache.HullScanTargetGrids[i];
                if (!IsDockedMobileOverlayGrid(cache, grid))
                    continue;

                AppendOverlaySourcesForGrid(grid, shipGrid, modeName, sources);
            }
        }

        static void AppendOverlaySourcesForGrid(IMyCubeGrid grid, ShipGrid shipGrid, string modeName, List<OverlayBlockSource> sources)
        {
            if (grid == null || grid.MarkedForClose)
                return;

            var blocks = new System.Collections.Generic.List<IMySlimBlock>();
            grid.GetBlocks(blocks, block => block != null && block.FatBlock != null);
            for (int b = 0; b < blocks.Count; b++)
            {
                var slim = blocks[b];
                var fat = slim.FatBlock;
                CargoOverlayRole role;
                if (!TryGetOverlayRoleForMode(modeName, fat, out role))
                    continue;

                var projected = ProjectCargoBlockBounds(shipGrid, grid, slim);
                sources.Add(new OverlayBlockSource
                {
                    Min = projected.Min,
                    Max = projected.Max,
                    Role = role,
                    Block = fat
                });
            }
        }

        static bool IsDockedMobileOverlayGrid(ScanCache cache, IMyCubeGrid grid)
        {
            if (cache == null || grid == null || grid.MarkedForClose)
                return false;
            if (!cache.IsConnectorHullScanGrid(grid.EntityId))
                return false;

            try
            {
                return !grid.IsStatic;
            }
            catch
            {
                return true;
            }
        }

        static bool TryGetOverlayRoleForMode(string modeName, IMyCubeBlock block, out CargoOverlayRole role)
        {
            if (string.Equals(modeName, "Cargo", StringComparison.OrdinalIgnoreCase))
                return TryGetCargoOverlayRole(block, out role);
            if (string.Equals(modeName, "Engines", StringComparison.OrdinalIgnoreCase))
                return TryGetEnginesOverlayRole(block, out role);
            if (string.Equals(modeName, "Power", StringComparison.OrdinalIgnoreCase))
                return TryGetPowerOverlayRole(block, out role);

            role = CargoOverlayRole.Storage;
            return false;
        }

        static float GetOverlayFillRatioForMode(string modeName, IMyCubeBlock block, CargoOverlayRole role)
        {
            if (string.Equals(modeName, "Cargo", StringComparison.OrdinalIgnoreCase))
                return GetCargoOverlayFillRatio(block, role);
            if (string.Equals(modeName, "Engines", StringComparison.OrdinalIgnoreCase))
                return GetEnginesOverlayFillRatio(block, role);
            if (string.Equals(modeName, "Power", StringComparison.OrdinalIgnoreCase))
                return GetPowerOverlayFillRatio(block, role);

            return 0f;
        }

        static string BuildOverlaySourceCacheKey(ScanCache cache, ShipGrid shipGrid, string modeName)
        {
            return BuildOverlaySourceCacheKey(cache, shipGrid, modeName, false);
        }

        static string BuildOverlaySourceCacheKey(ScanCache cache, ShipGrid shipGrid, string modeName, bool includeDockedMobileGrids)
        {
            int gridCount = cache.ConstructGrids != null ? cache.ConstructGrids.Count : 0;
            int dockedGridCount = includeDockedMobileGrids && cache != null ? cache.ConnectorHullScanGridCount : 0;
            return modeName + ":" +
                cache.ConstructId + ":" +
                cache.LastUpdatedUtc.Ticks + ":" +
                shipGrid.GetHashCode() + ":" +
                shipGrid.ProjectionView + ":" +
                shipGrid.Min2D.X + ":" +
                shipGrid.Min2D.Y + ":" +
                shipGrid.Max2D.X + ":" +
                shipGrid.Max2D.Y + ":" +
                gridCount + ":" +
                (includeDockedMobileGrids ? "D" : "L") + ":" +
                dockedGridCount;
        }

        static void TrimOverlaySourceCache()
        {
            if (OverlaySourceCache.Count <= MaxOverlaySourceCacheEntries)
                return;

            string oldestKey = null;
            int oldestUse = int.MaxValue;
            foreach (var entry in OverlaySourceCache)
            {
                if (entry.Value != null && entry.Value.LastUsed < oldestUse)
                {
                    oldestUse = entry.Value.LastUsed;
                    oldestKey = entry.Key;
                }
            }

            if (oldestKey != null)
                OverlaySourceCache.Remove(oldestKey);
        }

        static void AddCargoOverlayBlock(List<CargoOverlayGroup> groups, Vector2I min, Vector2I max, float fillRatio, CargoOverlayRole role, bool isEnabled, bool hasConnectorIndicator, CargoConnectorIndicator connectorIndicator, IMyCubeBlock block = null)
        {
            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                if (group.Role == role && group.Min == min && group.Max == max)
                {
                    group.FillRatios.Add(fillRatio);
                    group.EnabledStates.Add(isEnabled);
                    if (block != null)
                        group.Blocks.Add(block);
                    if (hasConnectorIndicator)
                        group.ConnectorIndicators.Add(connectorIndicator);
                    return;
                }
            }

            var newGroup = new CargoOverlayGroup
            {
                Min = min,
                Max = max,
                Role = role
            };
            newGroup.FillRatios.Add(fillRatio);
            newGroup.EnabledStates.Add(isEnabled);
            if (block != null)
                newGroup.Blocks.Add(block);
            if (hasConnectorIndicator)
                newGroup.ConnectorIndicators.Add(connectorIndicator);
            groups.Add(newGroup);
        }

        static bool TryGetCargoOverlayRole(IMyCubeBlock block, out CargoOverlayRole role)
        {
            role = CargoOverlayRole.Storage;
            if (block == null || block.BlockDefinition.SubtypeName == null)
                return false;

            string id = block.BlockDefinition.SubtypeName.ToLowerInvariant();
            if (IsCargoGasTank(id))
                return false;

            if (TryGetCargoOverlayRoleId(id, out role))
                return true;

            return false;
        }

        static bool TryGetEnginesOverlayRole(IMyCubeBlock block, out CargoOverlayRole role)
        {
            role = CargoOverlayRole.Storage;
            if (block == null || block.BlockDefinition.SubtypeName == null)
                return false;

            return TryGetEnginesOverlayRoleId(block.BlockDefinition.SubtypeName, out role);
        }

        static bool TryGetCargoOverlayRoleId(string id, out CargoOverlayRole role)
        {
            role = CargoOverlayRole.Storage;
            if (string.IsNullOrEmpty(id))
                return false;

            id = id.ToLowerInvariant();
            if (IsCargoGasTank(id))
                return false;

            if (IsCargoStorageBlock(id))
            {
                role = CargoOverlayRole.Storage;
                return true;
            }

            if (id.Contains("connector") || id.Contains("collector"))
            {
                role = CargoOverlayRole.Effector;
                return true;
            }

            if (IsCargoAccessBlock(id))
            {
                role = CargoOverlayRole.Effector;
                return true;
            }

            if (id.Contains("assembler") || id.Contains("refinery") || id.Contains("survival") ||
                id.Contains("drill") || id.Contains("sorter") || id.Contains("ejector"))
            {
                role = CargoOverlayRole.Effector;
                return true;
            }

            return false;
        }

        static bool TryGetEnginesOverlayRoleId(string id, out CargoOverlayRole role)
        {
            role = CargoOverlayRole.Storage;
            if (string.IsNullOrEmpty(id))
                return false;

            id = id.ToLowerInvariant();
            if (IsHydrogenTank(id))
            {
                role = CargoOverlayRole.Storage;
                return true;
            }

            if (IsEngineEffector(id))
            {
                role = CargoOverlayRole.Effector;
                return true;
            }

            return false;
        }

        static bool IsCargoStorageBlock(string subtype)
        {
            if (string.IsNullOrEmpty(subtype))
                return false;

            return subtype.Contains("cargo") || subtype.Contains("container");
        }

        static bool IsCargoAccessBlock(string subtype)
        {
            if (string.IsNullOrEmpty(subtype))
                return false;

            subtype = subtype.ToLowerInvariant();
            if (!(subtype.Contains("access") || subtype.Contains("terminal")))
                return false;

            return subtype.Contains("cargo") ||
                subtype.Contains("inventory") ||
                subtype.Contains("conveyor");
        }

        static bool IsCargoGasTank(string subtype)
        {
            if (string.IsNullOrEmpty(subtype))
                return false;

            if (!subtype.Contains("tank"))
                return false;

            return subtype.Contains("hydrogen") || subtype.Contains("oxygen") ||
                subtype.Contains("o2") || subtype.Contains("h2") ||
                subtype.Contains("gas");
        }

        static bool IsHydrogenTank(string subtype)
        {
            if (string.IsNullOrEmpty(subtype))
                return false;

            subtype = subtype.ToLowerInvariant();
            if (!subtype.Contains("tank"))
                return false;

            return subtype.Contains("hydrogen") || subtype.Contains("h2");
        }

        static bool IsEngineEffector(string subtype)
        {
            if (string.IsNullOrEmpty(subtype))
                return false;

            subtype = subtype.ToLowerInvariant();
            return subtype.Contains("engine") ||
                subtype.Contains("thruster") ||
                subtype.Contains("thrust");
        }

        static bool HasTerminalInventory(IMyCubeBlock block)
        {
            if (block as IMyTerminalBlock == null)
                return false;

            try
            {
                return block.InventoryCount > 0;
            }
            catch
            {
                return false;
            }
        }

        static float GetCargoOverlayFillRatio(IMyCubeBlock block, CargoOverlayRole role)
        {
            return GetInventoryFillRatio(block);
        }

        static float GetEnginesOverlayFillRatio(IMyCubeBlock block, CargoOverlayRole role)
        {
            if (block == null)
                return 0f;

            if (role == CargoOverlayRole.Storage)
                return GetGasTankFillRatio(block);

            var thrust = block as IMyThrust;
            if (thrust != null)
            {
                try
                {
                    float max = thrust.MaxThrust;
                    if (max > 0f)
                        return Clamp01(thrust.CurrentThrust / max);
                }
                catch
                {
                }
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

        static bool IsOverlayBlockOn(IMyCubeBlock block)
        {
            var functional = block as IMyFunctionalBlock;
            if (functional == null)
                return true;

            try
            {
                return functional.Enabled && functional.IsFunctional;
            }
            catch
            {
                return true;
            }
        }

        static bool TryGetConnectorIndicator(IMyCubeBlock block, ShipGrid shipGrid, ProjectionTransform transform, out CargoConnectorIndicator indicator)
        {
            indicator = new CargoConnectorIndicator();
            var connector = block as IMyShipConnector;
            if (connector == null || shipGrid == null || !transform.IsValid)
                return false;

            var basis = shipGrid.ReferenceMatrix;
            var forward = connector.WorldMatrix.Forward;
            var basisDirection = new Vector3D(
                Vector3D.Dot(forward, basis.Right),
                Vector3D.Dot(forward, basis.Up),
                Vector3D.Dot(forward, basis.Forward));

            Vector2 projectedDirection;
            switch (shipGrid.ProjectionView)
            {
                case ScanView.Front:
                    projectedDirection = new Vector2((float)basisDirection.X, (float)-basisDirection.Y);
                    break;
                case ScanView.Side:
                    projectedDirection = new Vector2((float)basisDirection.Z, (float)-basisDirection.Y);
                    break;
                default:
                    projectedDirection = new Vector2((float)basisDirection.X, (float)-basisDirection.Z);
                    break;
            }

            indicator.Connected = connector.Status.ToString() == "Connected";
            if (projectedDirection.LengthSquared() <= 0.0001f)
            {
                indicator.Direction = Vector2.Zero;
                indicator.FacingView = true;
                return true;
            }

            var origin = transform.ProjectLocalPoint(transform.SourceWidth * 0.5f, transform.SourceHeight * 0.5f);
            var target = transform.ProjectLocalPoint(transform.SourceWidth * 0.5f + projectedDirection.X, transform.SourceHeight * 0.5f + projectedDirection.Y);
            var screenDirection = target - origin;
            float length = screenDirection.Length();
            if (length <= 0.001f)
            {
                indicator.Direction = Vector2.Zero;
                indicator.FacingView = true;
                return true;
            }

            indicator.Direction = screenDirection / length;
            indicator.FacingView = false;
            return true;
        }

        static ProjectedBounds2D ProjectCargoBlockBounds(ShipGrid shipGrid, IMyCubeGrid grid, IMySlimBlock block)
        {
            var projected = new ProjectedBounds2D
            {
                Min = new Vector2I(int.MaxValue, int.MaxValue),
                Max = new Vector2I(int.MinValue, int.MinValue)
            };

            var min = block.Min;
            var max = block.Max;
            for (int x = min.X; x <= max.X; x++)
            {
                for (int y = min.Y; y <= max.Y; y++)
                {
                    for (int z = min.Z; z <= max.Z; z++)
                    {
                        var basis = WorldToShipBasis(GridPositionToWorld(grid, new Vector3I(x, y, z)), shipGrid);
                        var p = ShipGrid.Project(basis, shipGrid.ProjectionView);
                        if (p.X < projected.Min.X) projected.Min.X = p.X;
                        if (p.Y < projected.Min.Y) projected.Min.Y = p.Y;
                        if (p.X > projected.Max.X) projected.Max.X = p.X;
                        if (p.Y > projected.Max.Y) projected.Max.Y = p.Y;
                    }
                }
            }

            if (projected.Min.X == int.MaxValue)
            {
                projected.Min = Vector2I.Zero;
                projected.Max = Vector2I.Zero;
            }

            return projected;
        }

        static Vector3D GridPositionToWorld(IMyCubeGrid grid, Vector3I position)
        {
            var local = new Vector3D(position.X * grid.GridSize, position.Y * grid.GridSize, position.Z * grid.GridSize);
            return Vector3D.Transform(local, grid.WorldMatrix);
        }

        static Vector3I WorldToShipBasis(Vector3D worldPosition, ShipGrid shipGrid)
        {
            float basisGridSize = shipGrid.BasisGridSizeMeters > 0f ? shipGrid.BasisGridSizeMeters : 2.5f;
            var basis = shipGrid.ReferenceMatrix;
            var offset = worldPosition - basis.Translation;
            return new Vector3I(
                (int)Math.Round(Vector3D.Dot(offset, basis.Right) / basisGridSize),
                (int)Math.Round(Vector3D.Dot(offset, basis.Up) / basisGridSize),
                (int)Math.Round(Vector3D.Dot(offset, basis.Forward) / basisGridSize));
        }

        static float GetInventoryFillRatio(IMyCubeBlock block)
        {
            if (block == null)
                return 0f;

            float current = 0f;
            float max = 0f;
            try
            {
                int count = block.InventoryCount;
                for (int i = 0; i < count; i++)
                {
                    var inventory = block.GetInventory(i);
                    if (inventory == null)
                        continue;

                    current += (float)inventory.CurrentVolume;
                    max += (float)inventory.MaxVolume;
                }
            }
            catch
            {
                return 0f;
            }

            if (max <= 0f)
                return 0f;

            return Math.Max(0f, Math.Min(1f, current / max));
        }

        static float GetGasTankFillRatio(IMyCubeBlock block)
        {
            var tank = block as IMyGasTank;
            if (tank != null)
            {
                try
                {
                    return Clamp01((float)tank.FilledRatio);
                }
                catch
                {
                }
            }

            return GetInventoryFillRatio(block);
        }

        static float Clamp01(float value)
        {
            if (value < 0f)
                return 0f;
            if (value > 1f)
                return 1f;
            return value;
        }

        static void DrawCargoOverlayGroup(MySpriteDrawFrame frame, ProjectionTransform transform, ShipGrid shipGrid, CargoOverlayGroup group, bool showFillBars, float fillAlphaScale, bool occludeConveyorUnderFillBars, string modeName, UiState ui)
        {
            float localX0 = group.Min.X - shipGrid.Min2D.X;
            float localX1 = group.Max.X - shipGrid.Min2D.X + 1f;
            float localY0 = shipGrid.Max2D.Y - group.Max.Y;
            float localY1 = shipGrid.Max2D.Y - group.Min.Y + 1f;
            float localCx = (localX0 + localX1) * 0.5f;
            float localCy = (localY0 + localY1) * 0.5f;
            float width = Math.Max(1.5f, (localX1 - localX0) * transform.CellSize);
            float height = Math.Max(1.5f, (localY1 - localY0) * transform.CellSize);
            var center = transform.ProjectLocalPoint(localCx, localCy);
            var size = transform.GetRotatedSize(width, height);
            SnapOverlayScreenRect(ref center, ref size);
            bool isConnectorGroup = group.ConnectorIndicators != null && group.ConnectorIndicators.Count > 0;
            bool showConnectorSymbol = isConnectorGroup && ShouldDrawConnectorSymbology(size, group.ConnectorIndicators.Count);
            bool showConnectorFallbackBox = isConnectorGroup && !showConnectorSymbol;

            Color baseSchematic = group.Role == CargoOverlayRole.Storage
                ? ResolveStorageSchematicColor()
                : ResolveSecondarySchematicColor();
            Color border = showConnectorFallbackBox
                ? new Color(255, 216, 70, 255)
                : baseSchematic;
            Color empty = group.Role == CargoOverlayRole.Storage
                ? ScaleColor(baseSchematic, 0.18f, 85)
                : ScaleColor(baseSchematic, 0.20f, 85);
            Color fill = group.Role == CargoOverlayRole.Storage
                ? ScaleColor(baseSchematic, 1f, 112)
                : ScaleColor(baseSchematic, 0.75f, 112);
            Color occlusion = group.Role == CargoOverlayRole.Storage
                ? ScaleColor(baseSchematic, 0.14f, 210)
                : ScaleColor(baseSchematic, 0.16f, 210);
            empty = ScaleAlpha(empty, CurrentSchematicAlpha);
            fill = ScaleAlpha(fill, CurrentSchematicAlpha);
            border = ScaleAlpha(border, Math.Max(0.35f, CurrentSchematicAlpha));
            occlusion = ScaleAlpha(occlusion, Math.Max(0.35f, CurrentSchematicAlpha));
            if (HasActiveOverlaySelectionFocus(ui) && !OverlayGroupMatchesActiveSelectionFocus(group, ui))
            {
                empty = ScaleAlpha(empty, 0.30f);
                fill = ScaleAlpha(fill, 0.30f);
                border = ScaleAlpha(border, 0.30f);
                occlusion = ScaleAlpha(occlusion, 0.30f);
            }
            fillAlphaScale = Clamp01(fillAlphaScale);
            empty = ScaleAlpha(empty, fillAlphaScale);
            fill = ScaleAlpha(fill, fillAlphaScale);

            if (showFillBars && occludeConveyorUnderFillBars)
                DrawSchematicOcclusionMask(frame, center, size, occlusion);

            if (showFillBars)
            {
                if (!isConnectorGroup)
                    DrawCargoFillLanes(frame, center, size, group.FillRatios, empty, fill);
            }

            if (showConnectorSymbol)
                DrawConnectorIndicators(frame, center, size, group.ConnectorIndicators);

            if (!isConnectorGroup || showConnectorFallbackBox)
            {
                DrawScreenRectBorder(frame, center, size, border);
                DrawOverlayBlockStateMark(frame, center, size, group.EnabledStates, border);
            }
        }

        static bool HasActiveOverlaySelectionFocus(UiState ui)
        {
            if (ui == null)
                return false;
            if (ui.CargoTransferSelectionActive)
                return (ui.CargoTransferSourceItems != null && ui.CargoTransferSourceItems.Count > 0) || (ui.CargoTransferDestItems != null && ui.CargoTransferDestItems.Count > 0);
            return ui.SelectedBlockStackItems != null && ui.SelectedBlockStackItems.Count > 0 && ui.SelectedBlockStackIndex != UiState.SelectedBlockStackAllIndex;
        }

        static bool OverlayGroupMatchesActiveSelectionFocus(CargoOverlayGroup group, UiState ui)
        {
            if (group == null || ui == null)
                return false;
            if (ui.CargoTransferSelectionActive)
            {
                if (OverlayGroupMatchesBlockStackItems(group, ui.CargoTransferSourceItems))
                    return true;
                if (OverlayGroupMatchesBlockStackItems(group, ui.CargoTransferDestItems))
                    return true;
                return false;
            }
            if (ui.SelectedBlockStackItems != null && ui.SelectedBlockStackItems.Count > 0 && ui.SelectedBlockStackIndex != UiState.SelectedBlockStackAllIndex)
                return OverlayGroupMatchesSelectedBlockStack(group, ui);
            return false;
        }

        static bool OverlayGroupMatchesBlockStackItems(CargoOverlayGroup group, List<BlockStackItem> items)
        {
            if (group == null || items == null || items.Count == 0)
                return false;
            for (int i = 0; i < items.Count; i++)
            {
                if (OverlayGroupContainsBlock(group, items[i] != null ? items[i].Block : null))
                    return true;
            }
            return false;
        }
        static bool TryResolveOverlayGroupHighlightColor(CargoOverlayGroup group, string modeName, UiState ui, out Color color)
        {
            color = Color.Transparent;
            return false;
        }

        static bool OverlayGroupMatchesSelectedBlockStack(CargoOverlayGroup group, UiState ui)
        {
            if (group == null || group.Blocks == null || ui == null || ui.SelectedBlockStackItems == null || ui.SelectedBlockStackItems.Count == 0)
                return false;
            if (ui.SelectedBlockStackIndex == UiState.SelectedBlockStackAllIndex)
                return false;

            if (ui.SelectedBlockStackIndex == UiState.SelectedBlockStackAggregateIndex)
            {
                for (int i = 0; i < ui.SelectedBlockStackItems.Count; i++)
                {
                    if (OverlayGroupContainsBlock(group, ui.SelectedBlockStackItems[i] != null ? ui.SelectedBlockStackItems[i].Block : null))
                        return true;
                }
                return false;
            }

            if (ui.SelectedBlockStackIndex < 0 || ui.SelectedBlockStackIndex >= ui.SelectedBlockStackItems.Count)
                return false;
            return OverlayGroupContainsBlock(group, ui.SelectedBlockStackItems[ui.SelectedBlockStackIndex] != null ? ui.SelectedBlockStackItems[ui.SelectedBlockStackIndex].Block : null);
        }

        static bool OverlayGroupContainsBlock(CargoOverlayGroup group, IMyCubeBlock block)
        {
            if (group == null || group.Blocks == null || block == null)
                return false;
            long entityId = block.EntityId;
            for (int i = 0; i < group.Blocks.Count; i++)
            {
                var candidate = group.Blocks[i];
                if (candidate != null && candidate.EntityId == entityId)
                    return true;
            }
            return false;
        }
        static void DrawSchematicOcclusionMask(MySpriteDrawFrame frame, Vector2 center, Vector2 size, Color color)
        {
            var mask = new Color(color.R, color.G, color.B, color.A);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, size, mask));
        }

        static void SnapOverlayScreenRect(ref Vector2 center, ref Vector2 size)
        {
            int left = (int)Math.Round(center.X - size.X * 0.5f);
            int right = (int)Math.Round(center.X + size.X * 0.5f);
            int top = (int)Math.Round(center.Y - size.Y * 0.5f);
            int bottom = (int)Math.Round(center.Y + size.Y * 0.5f);

            if (right <= left)
                right = left + 1;
            if (bottom <= top)
                bottom = top + 1;

            center = new Vector2((left + right) * 0.5f, (top + bottom) * 0.5f);
            size = new Vector2(right - left, bottom - top);
        }

        static void DrawOverlayBlockStateMark(MySpriteDrawFrame frame, Vector2 center, Vector2 size, List<bool> enabledStates, Color color)
        {
            int mark = GetOverlayBlockStateMark(enabledStates);
            if (mark <= 0)
                return;

            float left = center.X - size.X * 0.5f;
            float right = center.X + size.X * 0.5f;
            float top = center.Y - size.Y * 0.5f;
            float bottom = center.Y + size.Y * 0.5f;
            const float thickness = 1f;

            DrawScreenLine(frame, new Vector2(left, top), new Vector2(right, bottom), thickness, color);
            if (mark >= 2)
                DrawScreenLine(frame, new Vector2(left, bottom), new Vector2(right, top), thickness, color);
        }

        static int GetOverlayBlockStateMark(List<bool> enabledStates)
        {
            if (enabledStates == null || enabledStates.Count == 0)
                return 0;

            int enabledCount = 0;
            for (int i = 0; i < enabledStates.Count; i++)
            {
                if (enabledStates[i])
                    enabledCount++;
            }

            if (enabledCount == enabledStates.Count)
                return 0;
            if (enabledCount == 0)
                return 2;
            return 1;
        }

        static void DrawOverlayInteractionUi(MySpriteDrawFrame frame, ScreenZone center, ProjectionTransform transform, ShipGrid shipGrid, List<CargoOverlayGroup> groups, TouchScreenApiAdapter input, string modeName, UiState ui)
        {
            if (groups == null || ui == null)
                return;

            string transformKey = shipGrid == null ? string.Empty : BuildProjectionTransformCacheKey(shipGrid, center, transform.RotationSteps);
            var uiCenter = ui.RenderViewportZone.Width > 0 && ui.RenderViewportZone.Height > 0
                ? ui.RenderViewportZone
                : center;
            for (int i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                if (group == null)
                    continue;

                Vector2 groupCenter;
                Vector2 groupSize;
                GetOverlayGroupScreenRect(transform, shipGrid, group, out groupCenter, out groupSize);
                string id = BuildOverlayBlockId(modeName, group);
                int signature = BuildOverlayBlockInfoSignature(modeName, group);
                var info = GetOrBuildOverlayBlockInfo(id, groupCenter, groupSize, group, modeName, signature, transformKey);
                ui.OverlayBlockRegions.Add(info);

                if (input != null && string.Equals(input.HoverRegionId, id, StringComparison.Ordinal))
                {
                    DrawInsetOverlaySelectionBorder(frame, groupCenter, groupSize, UiAccentSoft);
                }

            }

            // Selected overlay details now live in the unified systems info drawer.
        }

        static void GetOverlayGroupScreenRect(ProjectionTransform transform, ShipGrid shipGrid, CargoOverlayGroup group, out Vector2 center, out Vector2 size)
        {
            float localX0 = group.Min.X - shipGrid.Min2D.X;
            float localX1 = group.Max.X - shipGrid.Min2D.X + 1f;
            float localY0 = shipGrid.Max2D.Y - group.Max.Y;
            float localY1 = shipGrid.Max2D.Y - group.Min.Y + 1f;
            float localCx = (localX0 + localX1) * 0.5f;
            float localCy = (localY0 + localY1) * 0.5f;
            float width = Math.Max(1.5f, (localX1 - localX0) * transform.CellSize);
            float height = Math.Max(1.5f, (localY1 - localY0) * transform.CellSize);
            center = transform.ProjectLocalPoint(localCx, localCy);
            size = transform.GetRotatedSize(width, height);
            SnapOverlayScreenRect(ref center, ref size);
        }

        static void DrawInsetOverlaySelectionBorder(MySpriteDrawFrame frame, Vector2 center, Vector2 size, Color color)
        {
            if (size.X <= 3f || size.Y <= 3f)
                return;

            DrawScreenRectBorder(frame, center, new Vector2(Math.Max(1f, size.X - 2f), Math.Max(1f, size.Y - 2f)), color);
        }

        static string BuildOverlayBlockId(string modeName, CargoOverlayGroup group)
        {
            return "overlay:block:" + modeName + ":" + group.Role + ":" + group.Min.X + ":" + group.Min.Y + ":" + group.Max.X + ":" + group.Max.Y;
        }

        static string BuildProjectionTransformCacheKey(ShipGrid shipGrid, ScreenZone center, int rotationSteps)
        {
            return "tx:" +
                shipGrid.GetHashCode() + ":" +
                shipGrid.ProjectionView + ":" +
                shipGrid.Min2D.X + ":" +
                shipGrid.Min2D.Y + ":" +
                shipGrid.Max2D.X + ":" +
                shipGrid.Max2D.Y + ":" +
                shipGrid.BlockCount + ":" +
                center.X + ":" +
                center.Y + ":" +
                center.Width + ":" +
                center.Height + ":" +
                rotationSteps;
        }

        static ProjectionTransform GetOrBuildProjectionTransform(ShipGrid shipGrid, ScreenZone center, int rotationSteps)
        {
            if (shipGrid == null || shipGrid.IsEmpty)
                return default(ProjectionTransform);

            string key = BuildProjectionTransformCacheKey(shipGrid, center, rotationSteps);
            CachedProjectionTransform cached;
            if (ProjectionTransformCache.TryGetValue(key, out cached) && cached != null)
            {
                TrackCacheHit();
                cached.LastUsed = ++CacheUseCounter;
                return cached.Transform;
            }
            TrackCacheMiss();

            var transform = shipGrid.CreateTransform(center, rotationSteps);
            cached = new CachedProjectionTransform
            {
                Transform = transform,
                LastUsed = ++CacheUseCounter
            };
            ProjectionTransformCache[key] = cached;
            TrimProjectionTransformCache();
            return transform;
        }

        static ProjectionTransform GetOrBuildProjectionTransform(ShipGrid shipGrid, ScreenZone center)
        {
            return GetOrBuildProjectionTransform(shipGrid, center, 0);
        }

        static OverlayBlockInfo GetOrBuildOverlayBlockInfo(string id, Vector2 center, Vector2 size, CargoOverlayGroup group, string modeName, int signature, string transformKey)
        {
            string key = "overlayinfo:" + id + ":" + transformKey + ":" + signature;
            CachedOverlayInfo cached;
            if (OverlayInfoCache.TryGetValue(key, out cached) && cached != null &&
                cached.Info != null && cached.Signature == signature)
            {
                TrackCacheHit();
                cached.LastUsed = ++CacheUseCounter;
                RefreshOverlayBlockInfo(cached.Info, id, center, size, group, modeName);
                return cached.Info;
            }
            TrackCacheMiss();

            var info = BuildOverlayBlockInfo(id, center, size, group, modeName);
            cached = new CachedOverlayInfo
            {
                Info = info,
                Signature = signature,
                LastUsed = ++CacheUseCounter
            };
            OverlayInfoCache[key] = cached;
            TrimOverlayInfoCache();
            return info;
        }

        static int BuildOverlayBlockInfoSignature(string modeName, CargoOverlayGroup group)
        {
            int signature = 17;
            signature = signature * 31 + (modeName == null ? 0 : modeName.GetHashCode());
            if (group == null)
                return signature;

            signature = signature * 31 + (int)group.Role;
            signature = signature * 31 + group.Min.X;
            signature = signature * 31 + group.Min.Y;
            signature = signature * 31 + group.Max.X;
            signature = signature * 31 + group.Max.Y;

            int fillCount = group.FillRatios != null ? group.FillRatios.Count : 0;
            signature = signature * 31 + fillCount;
            for (int i = 0; i < fillCount; i++)
            {
                signature = signature * 31 + (int)(Clamp01(group.FillRatios[i]) * 100f); // QW2: 1% buckets (was 1/10000) to stop sub-perceptible fill changes thrashing OverlayInfoCache
            }

            int stateCount = group.EnabledStates != null ? group.EnabledStates.Count : 0;
            signature = signature * 31 + stateCount;
            for (int i = 0; i < stateCount; i++)
            {
                signature = signature * 31 + (group.EnabledStates[i] ? 1 : 0);
            }

            int blockCount = group.Blocks != null ? group.Blocks.Count : 0;
            signature = signature * 31 + blockCount;
            for (int i = 0; i < blockCount; i++)
            {
                var block = group.Blocks[i];
                long blockId = 0L;
                if (block != null)
                {
                    try
                    {
                        blockId = block.EntityId;
                    }
                    catch
                    {
                    }
                }
                signature = signature * 31 + (int)(blockId ^ (blockId >> 32));
            }

            int connectorCount = group.ConnectorIndicators != null ? group.ConnectorIndicators.Count : 0;
            signature = signature * 31 + connectorCount;
            for (int i = 0; i < connectorCount; i++)
            {
                var connector = group.ConnectorIndicators[i];
                signature = signature * 31 + (connector.Connected ? 1 : 0);
                signature = signature * 31 + (connector.FacingView ? 1 : 0);
                signature = signature * 31 + (int)Math.Floor(connector.Direction.X * 10f);
                signature = signature * 31 + (int)Math.Floor(connector.Direction.Y * 10f);
            }

            return signature;
        }

        static void TrimOverlayInfoCache()
        {
            if (OverlayInfoCache.Count <= MaxOverlayInfoCacheEntries)
                return;

            string oldestKey = null;
            int oldestUse = int.MaxValue;
            foreach (var entry in OverlayInfoCache)
            {
                if (entry.Value != null && entry.Value.LastUsed < oldestUse)
                {
                    oldestUse = entry.Value.LastUsed;
                    oldestKey = entry.Key;
                }
            }

            if (oldestKey != null)
                OverlayInfoCache.Remove(oldestKey);
        }

        static void TrimProjectionTransformCache()
        {
            if (ProjectionTransformCache.Count <= MaxProjectionTransformCacheEntries)
                return;

            string oldestKey = null;
            int oldestUse = int.MaxValue;
            foreach (var entry in ProjectionTransformCache)
            {
                if (entry.Value != null && entry.Value.LastUsed < oldestUse)
                {
                    oldestUse = entry.Value.LastUsed;
                    oldestKey = entry.Key;
                }
            }

            if (oldestKey != null)
                ProjectionTransformCache.Remove(oldestKey);
        }

        static OverlayBlockInfo BuildOverlayBlockInfo(string id, Vector2 center, Vector2 size, CargoOverlayGroup group, string modeName)
        {
            var info = new OverlayBlockInfo();
            RefreshOverlayBlockInfo(info, id, center, size, group, modeName);
            return info;
        }

        static void RefreshOverlayBlockInfo(OverlayBlockInfo info, string id, Vector2 center, Vector2 size, CargoOverlayGroup group, string modeName)
        {
            if (info == null)
                return;

            int left = (int)Math.Floor(center.X - size.X * 0.5f);
            int top = (int)Math.Floor(center.Y - size.Y * 0.5f);
            int width = (int)Math.Ceiling(size.X);
            int height = (int)Math.Ceiling(size.Y);
            if (width < 1)
                width = 1;
            if (height < 1)
                height = 1;

            info.Id = id;
            info.Region = new HitRegion(left, top, width, height, id, "Overlay block");
            info.Name = GetOverlayBlockName(group);
            info.Role = group.Role == CargoOverlayRole.Storage ? "Storage" : "Effector";
            info.State = GetOverlayStateText(group.EnabledStates);
            info.Metric = GetOverlayMetricText(group, modeName);
            info.Damage = GetOverlayDamageText(group);
            info.Count = group.FillRatios != null ? group.FillRatios.Count : 0;

            if (info.Blocks == null)
                info.Blocks = new List<IMyCubeBlock>();
            else
                info.Blocks.Clear();

            if (info.ToggleBlocks == null)
                info.ToggleBlocks = new List<IMyFunctionalBlock>();
            else
                info.ToggleBlocks.Clear();

            if (info.Lines == null)
                info.Lines = new List<OverlayBlockInfoLine>();
            else
                info.Lines.Clear();

            if (group.Blocks != null)
            {
                for (int i = 0; i < group.Blocks.Count; i++)
                {
                    if (group.Blocks[i] != null)
                        info.Blocks.Add(group.Blocks[i]);
                    var functional = group.Blocks[i] as IMyFunctionalBlock;
                    if (functional != null)
                        info.ToggleBlocks.Add(functional);
                }
            }

            PopulateOverlayInfoLines(info, group, modeName);
        }

        static void PopulateOverlayInfoLines(OverlayBlockInfo info, CargoOverlayGroup group, string modeName)
        {
            if (info == null || group == null)
                return;

            int count = group.Blocks != null && group.Blocks.Count > 0 ? group.Blocks.Count : Math.Max(1, info.Count);
            for (int i = 0; i < count; i++)
            {
                var block = group.Blocks != null && i < group.Blocks.Count ? group.Blocks[i] : null;
                var functional = block as IMyFunctionalBlock;
                float ratio = group.FillRatios != null && i < group.FillRatios.Count ? Clamp01(group.FillRatios[i]) : 0f;

                info.Lines.Add(new OverlayBlockInfoLine
                {
                    Text = info.Role + ":",
                    IsFillBar = true,
                    FillRatio = ratio
                });
                info.Lines.Add(new OverlayBlockInfoLine { Text = "Name: " + GetSingleOverlayBlockName(block) });
                info.Lines.Add(new OverlayBlockInfoLine
                {
                    Text = "State: " + GetSingleOverlayState(functional),
                    ToggleBlock = functional
                });
                AddBatteryChargeModeLine(info, block, modeName);
                AddTerminalActionLines(info, block);
                info.Lines.Add(new OverlayBlockInfoLine { Text = GetSingleOverlayDamageText(block) });

                if (i < count - 1)
                    info.Lines.Add(new OverlayBlockInfoLine { IsSeparator = true });
            }
        }

        static string GetSingleOverlayBlockName(IMyCubeBlock block)
        {
            if (block == null)
                return "Unknown block";

            string name = block.DisplayNameText;
            if (string.IsNullOrEmpty(name))
                name = block.DefinitionDisplayNameText;
            if (string.IsNullOrEmpty(name))
                name = "Block";
            return name;
        }

        static string GetSingleOverlayState(IMyFunctionalBlock block)
        {
            if (block == null)
                return "n/a";
            return block.Enabled ? "On" : "Off";
        }

        static void AddBatteryChargeModeLine(OverlayBlockInfo info, IMyCubeBlock block, string modeName)
        {
            if (info == null || modeName != "Power")
                return;

            var battery = block as IMyBatteryBlock;
            if (battery == null)
                return;

            string chargeMode = "Unknown";
            try
            {
                chargeMode = battery.ChargeMode.ToString();
            }
            catch
            {
            }

            info.Lines.Add(new OverlayBlockInfoLine
            {
                Text = "Charge: " + chargeMode,
                BatteryBlock = battery
            });
        }

        static void AddTerminalActionLines(OverlayBlockInfo info, IMyCubeBlock block)
        {
            if (info == null || block == null)
                return;

            var terminal = block as IMyTerminalBlock;
            if (terminal == null)
                return;

            var actions = new List<ITerminalAction>();
            try
            {
                terminal.GetActions(actions);
            }
            catch
            {
                return;
            }

            for (int i = 0; i < actions.Count; i++)
            {
                var action = actions[i];
                if (action == null)
                    continue;

                string name = GetTerminalActionName(action);
                if (string.IsNullOrEmpty(name))
                    name = action.Id;
                if (string.IsNullOrEmpty(name))
                    name = "Action";

                string value = GetTerminalActionValue(action, terminal);
                string text = string.IsNullOrEmpty(value)
                    ? "Action: " + name
                    : "Action: " + name + " " + value;

                info.Lines.Add(new OverlayBlockInfoLine
                {
                    Text = text,
                    TerminalBlock = terminal,
                    TerminalAction = action
                });
            }
        }

        static string GetTerminalActionName(ITerminalAction action)
        {
            if (action == null)
                return string.Empty;

            try
            {
                if (action.Name != null)
                    return action.Name.ToString();
            }
            catch
            {
            }

            return action.Id;
        }

        static string GetTerminalActionValue(ITerminalAction action, IMyTerminalBlock block)
        {
            if (action == null || block == null)
                return string.Empty;

            try
            {
                var builder = new StringBuilder();
                action.WriteValue(block, builder);
                return builder.ToString();
            }
            catch
            {
                return string.Empty;
            }
        }

        static string GetSingleOverlayDamageText(IMyCubeBlock block)
        {
            if (block == null || block.SlimBlock == null)
                return "Damage: n/a";

            float max = block.SlimBlock.MaxIntegrity;
            if (max <= 0f)
                return "Damage: n/a";

            int damagePercent = 100 - (int)Math.Round(Clamp01(block.SlimBlock.BuildIntegrity / max) * 100f);
            return "Damage: " + damagePercent.ToString() + "%";
        }

        static string GetOverlayBlockName(CargoOverlayGroup group)
        {
            if (group == null || group.Blocks == null || group.Blocks.Count == 0 || group.Blocks[0] == null)
                return "Unknown block";

            string name = group.Blocks[0].DisplayNameText;
            if (string.IsNullOrEmpty(name))
                name = group.Blocks[0].DefinitionDisplayNameText;
            if (string.IsNullOrEmpty(name))
                name = "Block";

            if (group.Blocks.Count > 1)
                name += " +" + (group.Blocks.Count - 1).ToString();

            return name;
        }

        static string GetOverlayDamageText(CargoOverlayGroup group)
        {
            if (group == null || group.Blocks == null || group.Blocks.Count == 0)
                return "Damage: n/a";

            float totalRatio = 0f;
            int counted = 0;
            for (int i = 0; i < group.Blocks.Count; i++)
            {
                var block = group.Blocks[i];
                if (block == null || block.SlimBlock == null)
                    continue;

                float max = block.SlimBlock.MaxIntegrity;
                if (max <= 0f)
                    continue;

                totalRatio += Clamp01(block.SlimBlock.BuildIntegrity / max);
                counted++;
            }

            if (counted == 0)
                return "Damage: n/a";

            int damagePercent = 100 - (int)Math.Round((totalRatio / counted) * 100f);
            return "Damage: " + damagePercent.ToString() + "%";
        }

        static void DrawSelectedOverlayInfoPanel(MySpriteDrawFrame frame, ScreenZone center, OverlayBlockInfo info, int selectedLineIndex)
        {
            if (info == null)
                return;

            float panelWidth = center.Width * 0.5f;
            float panelHeight = center.Height * 0.33f;
            if (panelWidth < 180f)
                panelWidth = 180f;
            if (panelHeight < 92f)
                panelHeight = 92f;

            var panelPos = new Vector2(center.X + center.Width - panelWidth, center.Y);
            var panelSize = new Vector2(panelWidth, panelHeight);
            var panelCenter = panelPos + panelSize * 0.5f;
            var border = UiAccentSoft;
            var text = UiText;
            var selected = UiSelected;

            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", panelCenter, panelSize, UiPanelFillSoft));
            DrawScreenRectBorder(frame, panelCenter, panelSize, border);

            DrawSelectedOverlayInfoLines(frame, panelPos + new Vector2(8f, 8f), panelSize, info, selectedLineIndex, text, selected, border);
        }

        static void DrawSelectedOverlayInfoLines(MySpriteDrawFrame frame, Vector2 start, Vector2 panelSize, OverlayBlockInfo info, int selectedLineIndex, Color textColor, Color selectedColor, Color borderColor)
        {
            if (info == null || info.Lines == null || info.Lines.Count == 0)
                return;

            int visibleSlots = (int)Math.Floor((panelSize.Y - 16f) / 15f);
            if (visibleSlots < 1)
                visibleSlots = 1;

            int first = selectedLineIndex - visibleSlots / 2;
            if (first < 0)
                first = 0;
            if (first + visibleSlots > info.Lines.Count)
                first = Math.Max(0, info.Lines.Count - visibleSlots);

            float y = start.Y;
            for (int i = first; i < info.Lines.Count && i < first + visibleSlots; i++)
            {
                var line = info.Lines[i];
                if (line == null)
                    continue;

                bool isSelected = i == selectedLineIndex;
                var color = isSelected ? selectedColor : textColor;
                if (line.IsSeparator)
                {
                    DrawScreenLine(frame, new Vector2(start.X, y + 6f), new Vector2(start.X + panelSize.X - 16f, y + 6f), 1f, borderColor);
                }
                else if (line.IsFillBar)
                {
                    DrawReadoutText(frame, line.Text, new Vector2(start.X, y), color, 0.5f);
                    DrawInfoFillBar(frame, new Vector2(start.X + 58f, y + 6f), new Vector2(panelSize.X - 78f, 7f), line.FillRatio, borderColor, isSelected ? selectedColor : UiAccentSoft);
                }
                else if (!string.IsNullOrEmpty(line.Text))
                {
                    string suffix = line.CanToggle ? "  [RMB]" : string.Empty;
                    DrawReadoutText(frame, line.Text + suffix, new Vector2(start.X, y), color, 0.5f);
                }

                y += 15f;
            }
        }

        static void DrawInfoFillBar(MySpriteDrawFrame frame, Vector2 leftCenter, Vector2 size, float ratio, Color borderColor, Color fillColor)
        {
            ratio = Clamp01(ratio);
            var center = new Vector2(leftCenter.X + size.X * 0.5f, leftCenter.Y);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, size, UiAccentGhost));
            DrawScreenRectBorder(frame, center, size, borderColor, 2f);
            float inset = 2f;
            float fillWidth = Math.Max(1f, (size.X - inset * 2f) * ratio);
            var fillCenter = new Vector2(center.X - (size.X - inset * 2f) * 0.5f + fillWidth * 0.5f, center.Y);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", fillCenter, new Vector2(fillWidth, Math.Max(1f, size.Y - inset * 2f)), fillColor));
        }

        static void DrawOverlayReadoutPanel(MySpriteDrawFrame frame, ScreenZone center, CargoOverlayGroup group, string modeName)
        {
            int count = group.FillRatios != null ? group.FillRatios.Count : 0;
            string role = group.Role == CargoOverlayRole.Storage ? "Storage" : "Effector";
            string state = GetOverlayStateText(group.EnabledStates);
            string metric = GetOverlayMetricText(group, modeName);

            var panelPos = new Vector2(center.X + 8f, center.Y + 8f);
            var panelSize = new Vector2(168f, string.IsNullOrEmpty(metric) ? 58f : 72f);
            var panelCenter = panelPos + panelSize * 0.5f;
            var border = group.Role == CargoOverlayRole.Storage
                ? new Color(50, 230, 85, 235)
                : new Color(255, 90, 220, 235);

            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", panelCenter, panelSize, new Color(0, 0, 0, 190)));
            DrawScreenRectBorder(frame, panelCenter, panelSize, border);
            DrawReadoutText(frame, modeName + " " + role, panelPos + new Vector2(8f, 7f), border, 0.58f);
            DrawReadoutText(frame, "Count: " + count, panelPos + new Vector2(8f, 23f), Color.LightGray, 0.54f);
            DrawReadoutText(frame, "State: " + state, panelPos + new Vector2(8f, 38f), Color.LightGray, 0.54f);
            if (!string.IsNullOrEmpty(metric))
                DrawReadoutText(frame, metric, panelPos + new Vector2(8f, 53f), Color.LightGray, 0.54f);
        }

        static void DrawReadoutText(MySpriteDrawFrame frame, string text, Vector2 position, Color color, float scale)
        {
            AddSprite(frame, new MySprite(
                SpriteType.TEXT,
                text,
                position,
                null,
                color,
                CurrentTextFontId,
                TextAlignment.LEFT,
                scale));
        }

        static string GetOverlayStateText(List<bool> enabledStates)
        {
            if (enabledStates == null || enabledStates.Count == 0)
                return "Unknown";

            int enabledCount = 0;
            for (int i = 0; i < enabledStates.Count; i++)
            {
                if (enabledStates[i])
                    enabledCount++;
            }

            if (enabledCount == enabledStates.Count)
                return "On";
            if (enabledCount == 0)
                return "Off";
            return "Partial";
        }

        static string GetOverlayMetricText(CargoOverlayGroup group, string modeName)
        {
            float average = GetAverageFillRatio(group.FillRatios);
            int percent = (int)Math.Round(Clamp01(average) * 100f);
            if (modeName == "Cargo")
                return "Fill: " + percent + "%";
            if (modeName == "Engines")
                return group.Role == CargoOverlayRole.Storage ? "Fuel: " + percent + "%" : "Throttle: " + percent + "%";
            if (modeName == "Power")
                return group.Role == CargoOverlayRole.Storage ? "Stored: " + percent + "%" : "Output: " + percent + "%";
            return string.Empty;
        }

        static void DrawCargoFillLanes(MySpriteDrawFrame frame, Vector2 center, Vector2 size, List<float> fillRatios, Color emptyColor, Color fillColor)
        {
            if (fillRatios == null || fillRatios.Count == 0)
                return;

            const int padding = 3;
            const int laneGap = 3;
            int outerLeft = (int)Math.Round(center.X - size.X * 0.5f);
            int outerRight = outerLeft + (int)Math.Round(size.X);
            int outerTop = (int)Math.Round(center.Y - size.Y * 0.5f);
            int outerBottom = outerTop + (int)Math.Round(size.Y);
            int x0 = outerLeft + padding;
            int x1 = outerRight - padding;
            int y0 = outerTop + padding;
            int y1 = outerBottom - padding;
            int innerWidth = x1 - x0;
            int innerHeight = y1 - y0;
            if (innerWidth <= 0 || innerHeight <= 0)
                return;

            emptyColor = new Color(emptyColor.R, emptyColor.G, emptyColor.B, fillColor.A);
            AddSprite(frame, new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                new Vector2(x0 + innerWidth * 0.5f, y0 + innerHeight * 0.5f),
                new Vector2(Math.Max(1, innerWidth), Math.Max(1, innerHeight)),
                emptyColor));

            const float minLaneScreenWidth = 11f;
            int totalLaneCount = fillRatios.Count;
            int maxScreenLanes = (int)Math.Floor(innerWidth / minLaneScreenWidth);
            if (maxScreenLanes < 1)
                maxScreenLanes = 1;

            int laneCount = Math.Min(totalLaneCount, maxScreenLanes);
            bool collapsed = laneCount < totalLaneCount;
            int availableLaneWidth = innerWidth - laneGap * (laneCount - 1);
            if (availableLaneWidth <= 0)
                return;

            for (int i = 0; i < laneCount; i++)
            {
                int sourceIndex = ResolveFillLaneSourceIndex(i, laneCount, totalLaneCount);
                float ratio = Math.Max(0f, Math.Min(1f, fillRatios[sourceIndex]));
                int laneX0 = x0 + (availableLaneWidth * i) / laneCount + laneGap * i;
                int laneX1 = x0 + (availableLaneWidth * (i + 1)) / laneCount + laneGap * i;
                if (laneX1 <= laneX0)
                    continue;

                int laneWidth = laneX1 - laneX0;
                int fillHeight = (int)Math.Round(innerHeight * ratio);
                if (fillHeight > innerHeight)
                    fillHeight = innerHeight;
                if (fillHeight < 0)
                    fillHeight = 0;

                if (fillHeight > 0)
                {
                    var laneCenter = new Vector2(laneX0 + laneWidth * 0.5f, y0 + innerHeight * 0.5f);
                    var fillCenter = new Vector2(laneCenter.X, y1 - fillHeight * 0.5f);
                    var fillSize = new Vector2(Math.Max(1, laneWidth), fillHeight);
                    AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", fillCenter, fillSize, fillColor));
                }
            }

            // Collapsed lane counts are intentionally hidden; the selected block panel carries stack detail.
        }

        static Color ResolveSchematicColor(string colorName, Color fallback)
        {
            if (string.IsNullOrEmpty(colorName))
                return fallback;

            switch (colorName.ToUpperInvariant())
            {
                case "GREEN":
                    return new Color(0, 255, 64, 255);
                case "MAGENTA":
                    return new Color(255, 90, 220, 255);
                case "YELLOW":
                    return new Color(255, 216, 70, 255);
                case "CYAN":
                    return new Color(20, 220, 255, 255);
                case "BLUE":
                    return new Color(80, 130, 255, 255);
                case "WHITE":
                    return new Color(235, 245, 255, 255);
                default:
                    return fallback;
            }
        }

        static Color ScaleColor(Color color, float rgbScale, int alpha)
        {
            return new Color(
                ClampByte(color.R * rgbScale),
                ClampByte(color.G * rgbScale),
                ClampByte(color.B * rgbScale),
                ClampByte(alpha));
        }

        static Color ScaleAlpha(Color color, float alphaScale)
        {
            return new Color(color.R, color.G, color.B, ClampByte(color.A * alphaScale));
        }

        static byte ClampByte(float value)
        {
            if (value < 0f)
                return 0;
            if (value > 255f)
                return 255;
            return (byte)Math.Round(value);
        }

        static int ResolveFillLaneSourceIndex(int laneIndex, int laneCount, int totalLaneCount)
        {
            if (totalLaneCount <= 1 || laneCount <= 1)
                return 0;

            int index = (int)Math.Round(laneIndex * (totalLaneCount - 1) / (float)(laneCount - 1));
            if (index < 0)
                return 0;
            if (index >= totalLaneCount)
                return totalLaneCount - 1;
            return index;
        }

        static float GetAverageFillRatio(List<float> fillRatios)
        {
            if (fillRatios == null || fillRatios.Count == 0)
                return 0f;

            float total = 0f;
            for (int i = 0; i < fillRatios.Count; i++)
            {
                total += Math.Max(0f, Math.Min(1f, fillRatios[i]));
            }

            return total / fillRatios.Count;
        }

        static void DrawConnectorIndicators(MySpriteDrawFrame frame, Vector2 center, Vector2 size, List<CargoConnectorIndicator> indicators)
        {
            if (indicators == null || indicators.Count == 0)
                return;
            if (!ShouldDrawConnectorSymbology(size, indicators.Count))
                return;

            int count = Math.Min(indicators.Count, 4);
            float radius = Math.Max(7f, Math.Min(size.X, size.Y) * 0.62f);
            var symbolColor = new Color(255, 216, 70, 255);
            var connectedSymbolColor = new Color(255, 238, 145, 255);
            float iconSize = Math.Max(10f, radius * 1.35f);
            for (int i = 0; i < count; i++)
            {
                var indicator = indicators[i];
                var dir = indicator.Direction;
                float sideOffset = (i - (count - 1) * 0.5f) * 3f;
                var color = indicator.Connected ? connectedSymbolColor : symbolColor;

                if (indicator.FacingView || dir.LengthSquared() <= 0.0001f)
                {
                    DrawConnectorFrontRing(frame, center + new Vector2(sideOffset, 0f), iconSize * 0.34f, color);
                    continue;
                }

                var perp = new Vector2(-dir.Y, dir.X);
                float angle = (float)Math.Atan2(dir.Y, dir.X);
                AddSprite(frame, new MySprite(
                    SpriteType.TEXTURE,
                    ConnectorSideIconTexture,
                    center + perp * sideOffset,
                    new Vector2(iconSize, iconSize),
                    color,
                    null,
                    TextAlignment.CENTER,
                    angle));
            }
        }

        static Color ResolveStorageSchematicColor()
        {
            return SetPaletteColorHue(ResolveSchematicColor(CurrentStorageColor, new Color(0, 255, 64, 255)), CurrentSchematicMainHue, 1f, 1f, 1f);
        }

        static Color ResolveSecondarySchematicColor()
        {
            return SetPaletteColorHue(ResolveSchematicColor(CurrentEffectorColor, new Color(255, 90, 220, 255)), CurrentSchematicSecondaryHue, 1f, 1f, 1f);
        }

        static Color ApplyConveyorHue(Color color)
        {
            return SetPaletteColorHue(color, CurrentConveyorHue, 1f, 1f, 1f);
        }

        static void DrawConnectorFrontRing(MySpriteDrawFrame frame, Vector2 center, float radius, Color color)
        {
            center = SnapPoint(center);
            float snappedRadius = SnapPixelSize(Math.Max(4f, radius));
            float thickness = SnapPixelSize(Math.Max(1f, snappedRadius * 0.14f));
            const int segments = 24;
            for (int i = 0; i < segments; i++)
            {
                float a0 = MathHelper.TwoPi * i / segments;
                float a1 = MathHelper.TwoPi * (i + 1) / segments;
                var p0 = center + new Vector2((float)Math.Cos(a0), (float)Math.Sin(a0)) * snappedRadius;
                var p1 = center + new Vector2((float)Math.Cos(a1), (float)Math.Sin(a1)) * snappedRadius;
                DrawScreenLine(frame, p0, p1, thickness, color);
            }
        }

        static bool ShouldDrawConnectorSymbology(Vector2 size, int connectorCount)
        {
            float minDimension = Math.Min(size.X, size.Y);
            float maxDimension = Math.Max(size.X, size.Y);
            if (minDimension < 18f)
                return false;
            if (connectorCount > 1 && maxDimension < 24f)
                return false;
            return true;
        }


        static void DrawScreenRectBorder(MySpriteDrawFrame frame, Vector2 center, Vector2 size, Color color)
        {
            DrawScreenRectBorder(frame, center, size, color, 1f);
        }

        static void DrawScreenRectBorder(MySpriteDrawFrame frame, Vector2 center, Vector2 size, Color color, float thickness)
        {
            float left = SnapPixel(center.X - size.X * 0.5f);
            float right = SnapPixel(center.X + size.X * 0.5f);
            float top = SnapPixel(center.Y - size.Y * 0.5f);
            float bottom = SnapPixel(center.Y + size.Y * 0.5f);
            float snappedCenterX = SnapPixel((left + right) * 0.5f);
            float snappedCenterY = SnapPixel((top + bottom) * 0.5f);
            float width = Math.Max(1f, SnapPixelSize(right - left));
            float height = Math.Max(1f, SnapPixelSize(bottom - top));
            thickness = Math.Max(1f, SnapPixelSize(thickness));

            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(snappedCenterX, top), new Vector2(width, thickness), color));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(snappedCenterX, bottom), new Vector2(width, thickness), color));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(left, snappedCenterY), new Vector2(thickness, height), color));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(right, snappedCenterY), new Vector2(thickness, height), color));
        }

        static void DrawScreenLine(MySpriteDrawFrame frame, Vector2 start, Vector2 end, float thickness, Color color)
        {
            if (TryDrawAxisAlignedScreenLine(frame, start, end, thickness, color))
                return;

            var delta = end - start;
            float length = delta.Length();
            if (length <= 0.1f)
                return;

            float pixelThickness = SnapPixelSize(thickness);
            AddSprite(frame, new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                (start + end) * 0.5f,
                new Vector2(length, pixelThickness),
                color,
                null,
                TextAlignment.CENTER,
                (float)Math.Atan2(delta.Y, delta.X)));
        }

        static bool TryDrawAxisAlignedScreenLine(MySpriteDrawFrame frame, Vector2 start, Vector2 end, float thickness, Color color)
        {
            var delta = end - start;
            if (!IsMostlyAxisAligned(delta))
                return false;

            float pixelThickness = SnapPixelSize(thickness);
            float ax = Math.Abs(delta.X);
            float ay = Math.Abs(delta.Y);
            if (ax >= ay)
            {
                float x0 = SnapPixel(Math.Min(start.X, end.X));
                float x1 = SnapPixel(Math.Max(start.X, end.X));
                float y = SnapPixel((start.Y + end.Y) * 0.5f);
                float width = Math.Max(1f, SnapPixelSize(x1 - x0));
                AddSprite(frame, new MySprite(
                    SpriteType.TEXTURE,
                    "SquareSimple",
                    new Vector2((x0 + x1) * 0.5f, y),
                    new Vector2(width, pixelThickness),
                    color));
                return true;
            }

            float y0 = SnapPixel(Math.Min(start.Y, end.Y));
            float y1 = SnapPixel(Math.Max(start.Y, end.Y));
            float x = SnapPixel((start.X + end.X) * 0.5f);
            float height = Math.Max(1f, SnapPixelSize(y1 - y0));
            AddSprite(frame, new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                new Vector2(x, (y0 + y1) * 0.5f),
                new Vector2(pixelThickness, height),
                color));
            return true;
        }

        static void DrawDashedScreenLine(MySpriteDrawFrame frame, Vector2 start, Vector2 end, float thickness, float dashLength, float gapLength, Color color)
        {
            var delta = end - start;
            float length = delta.Length();
            if (length <= 0.1f)
                return;

            var direction = delta / length;
            float cursor = 0f;
            float dash = Math.Max(1f, dashLength);
            float gap = Math.Max(0.5f, gapLength);
            while (cursor < length)
            {
                float next = Math.Min(length, cursor + dash);
                DrawScreenLine(frame, start + direction * cursor, start + direction * next, thickness, color);
                cursor = next + gap;
            }
        }

        static Vector2 SnapPoint(Vector2 value)
        {
            return new Vector2(SnapPixel(value.X), SnapPixel(value.Y));
        }

        static float SnapPixel(float value)
        {
            return (float)Math.Round(value);
        }

        static float SnapPixelSize(float value)
        {
            return Math.Max(1f, (float)Math.Round(value));
        }

        static void DrawCargoNodeBorder(MySpriteDrawFrame frame, ProjectionTransform transform, float localX0, float localX1, float localY0, float localY1, Color color)
        {
            float localCx = (localX0 + localX1) * 0.5f;
            float localCy = (localY0 + localY1) * 0.5f;
            float width = Math.Max(1f, (localX1 - localX0) * transform.CellSize);
            float height = Math.Max(1f, (localY1 - localY0) * transform.CellSize);

            DrawTransformedRect(frame, transform, localCx, localY0, width, 1f, color);
            DrawTransformedRect(frame, transform, localCx, localY1, width, 1f, color);
            DrawTransformedRect(frame, transform, localX0, localCy, 1f, height, color);
            DrawTransformedRect(frame, transform, localX1, localCy, 1f, height, color);
        }

        static void DrawRaycastScanImage(MySpriteDrawFrame frame, ScreenZone zone, ProjectionTransform transform, ShipGrid shipGrid, RawRaycastScanData scanData, string fillMode, bool blurScan, int renderStep)
        {
            if (fillMode == GridSchematicsConfig.FillNone)
                return;

            if (shipGrid == null || scanData == null || !scanData.IsReady || scanData.Samples == null || scanData.Resolution <= 0)
                return;

            int resolution = scanData.Resolution;
            int step = Math.Max(1, renderStep);
            float spanX = Math.Max(0.001f, scanData.SampleMaxX - scanData.SampleMinX);
            float spanY = Math.Max(0.001f, scanData.SampleMaxY - scanData.SampleMinY);
            float baseSampleWidth = spanX / (float)resolution * transform.CellSize;
            float baseSampleHeight = spanY / (float)resolution * transform.CellSize;
            step = Math.Max(step, GetScreenResolvedScanStep(baseSampleWidth, baseSampleHeight));
            float tileOverlap = step > 1 ? 0.05f : 0.75f;
            float sampleWidth = baseSampleWidth;
            float sampleHeight = baseSampleHeight * step;
            var cachedRuns = GetOrBuildScanRuns(scanData, fillMode, blurScan, step);
            if (cachedRuns == null || cachedRuns.Runs == null || cachedRuns.Runs.Count == 0)
                return;

            for (int i = 0; i < cachedRuns.Runs.Count; i++)
            {
                CurrentPerfCachedSpriteCount++;
                var run = cachedRuns.Runs[i];
                DrawScanRun(frame, zone, transform, shipGrid, scanData, resolution, step, run.Y, run.StartX, run.EndX, run.Shade, sampleWidth, sampleHeight, tileOverlap, fillMode);
            }
        }

        static CachedScanRuns GetOrBuildScanRuns(RawRaycastScanData scanData, string fillMode, bool blurScan, int step)
        {
            string key = BuildScanRunCacheKey(scanData, fillMode, blurScan, step);
            CachedScanRuns cached;
            if (ScanRunCache.TryGetValue(key, out cached) && cached != null)
            {
                TrackCacheHit();
                cached.LastUsed = ++CacheUseCounter;
                return cached;
            }
            TrackCacheMiss();

            cached = BuildScanRuns(scanData, fillMode, blurScan, step);
            cached.LastUsed = ++CacheUseCounter;
            ScanRunCache[key] = cached;
            TrimScanRunCache();
            return cached;
        }

        static CachedScanRuns BuildScanRuns(RawRaycastScanData scanData, string fillMode, bool blurScan, int step)
        {
            var cached = new CachedScanRuns();
            int resolution = scanData.Resolution;
            for (int y = 0; y < resolution; y += step)
            {
                int runStart = -1;
                int runShade = 0;

                for (int x = 0; x <= resolution; x += step)
                {
                    int shade = 0;
                    if (x < resolution)
                    {
                        float value = GetScanRunValue(scanData, x, y, step, fillMode, blurScan);
                        if (value > 0.01f)
                        {
                            value = Math.Min(1f, value);
                            shade = Math.Max(0, Math.Min(255, (int)(value * 220f)));
                        }
                    }

                    if (shade <= 0)
                    {
                        if (runStart >= 0)
                        {
                            cached.Runs.Add(new CachedScanRun { Y = y, StartX = runStart, EndX = x, Shade = runShade });
                            runStart = -1;
                        }
                        continue;
                    }

                    if (runStart < 0)
                    {
                        runStart = x;
                        runShade = shade;
                        continue;
                    }

                    if (shade != runShade)
                    {
                        cached.Runs.Add(new CachedScanRun { Y = y, StartX = runStart, EndX = x, Shade = runShade });
                        runStart = x;
                        runShade = shade;
                        continue;
                    }
                }

                if (runStart >= 0)
                    cached.Runs.Add(new CachedScanRun { Y = y, StartX = runStart, EndX = resolution, Shade = runShade });
            }

            return cached;
        }

        static int GetScreenResolvedScanStep(float sampleWidth, float sampleHeight)
        {
            float minSample = Math.Min(sampleWidth, sampleHeight);
            if (minSample >= 1f)
                return 1;

            int step = (int)Math.Ceiling(1f / Math.Max(0.05f, minSample));
            if (step < 1)
                step = 1;
            if (step > 4)
                step = 4;
            return step;
        }

        static float GetScanRunValue(RawRaycastScanData scanData, int x, int y, int step, string fillMode, bool blurScan)
        {
            if (step <= 1)
                return blurScan ? GetBlurredScanValue(scanData, x, y, fillMode) : GetDisplayScanValue(scanData, x, y, fillMode);

            int resolution = scanData.Resolution;
            float total = 0f;
            int count = 0;
            int yEnd = Math.Min(resolution, y + step);
            int xEnd = Math.Min(resolution, x + step);
            for (int sy = y; sy < yEnd; sy++)
            {
                for (int sx = x; sx < xEnd; sx++)
                {
                    total += blurScan ? GetBlurredScanValue(scanData, sx, sy, fillMode) : GetDisplayScanValue(scanData, sx, sy, fillMode);
                    count++;
                }
            }

            return count > 0 ? total / count : 0f;
        }

        static string BuildScanRunCacheKey(RawRaycastScanData scanData, string fillMode, bool blurScan, int step)
        {
            int maxThicknessBucket = (int)(scanData.MaxThickness * 1000f);
            return scanData.GetHashCode() + ":" +
                scanData.View + ":" +
                scanData.ScannedUtc.Ticks + ":" +
                scanData.Resolution + ":" +
                (int)(scanData.SampleMinX * 100f) + ":" +
                (int)(scanData.SampleMaxX * 100f) + ":" +
                (int)(scanData.SampleMinY * 100f) + ":" +
                (int)(scanData.SampleMaxY * 100f) + ":" +
                scanData.HitSampleCount + ":" +
                scanData.MaxHitCount + ":" +
                maxThicknessBucket + ":" +
                fillMode + ":" +
                blurScan + ":" +
                step;
        }

        static void TrimScanRunCache()
        {
            if (ScanRunCache.Count <= MaxScanRunCacheEntries)
                return;

            string oldestKey = null;
            int oldestUse = int.MaxValue;
            foreach (var entry in ScanRunCache)
            {
                if (entry.Value != null && entry.Value.LastUsed < oldestUse)
                {
                    oldestUse = entry.Value.LastUsed;
                    oldestKey = entry.Key;
                }
            }

            if (oldestKey != null)
                ScanRunCache.Remove(oldestKey);
        }

        static void DrawScanRun(MySpriteDrawFrame frame, ScreenZone zone, ProjectionTransform transform, ShipGrid shipGrid, RawRaycastScanData scanData, int resolution, int step, int y, int startX, int endX, int shade, float sampleWidth, float sampleHeight, float tileOverlap, string fillMode)
        {
            int runWidthCells = Math.Max(1, endX - startX);
            float spanX = Math.Max(0.001f, scanData.SampleMaxX - scanData.SampleMinX);
            float spanY = Math.Max(0.001f, scanData.SampleMaxY - scanData.SampleMinY);
            float stepOffset = Math.Max(1, step) - 1;
            float sampleIndexX = startX - stepOffset * 0.5f + runWidthCells * 0.5f;
            float sampleIndexY = y + (step * 0.5f - stepOffset * 0.5f);
            float sampleX = scanData.SampleMinX + sampleIndexX / Math.Max(1f, resolution) * spanX;
            float sampleY = scanData.SampleMinY + sampleIndexY / Math.Max(1f, resolution) * spanY;
            float localX = sampleX - shipGrid.Min2D.X + 0.5f;
            float localY = shipGrid.Max2D.Y - sampleY + 0.5f;
            var p = transform.ProjectLocalPoint(localX, localY);
            if (p.X < zone.X - sampleWidth || p.X > zone.X + zone.Width + sampleWidth ||
                p.Y < zone.Y - sampleHeight || p.Y > zone.Y + zone.Height + sampleHeight)
                return;

            var size = transform.GetRotatedSize(Math.Max(1f, sampleWidth * runWidthCells + tileOverlap), Math.Max(1f, sampleHeight + tileOverlap));
            int alpha = fillMode == GridSchematicsConfig.FillHits ? 26 : 255;
            alpha = Math.Max(0, Math.Min(255, (int)(alpha * CurrentHullScanAlpha)));
            int displayShade = Math.Max(shade, Math.Min(255, shade + 34));
            displayShade = ClampByte(displayShade * CurrentHullScanBrightness);
            if (!TryClipRectToZone(ref p, ref size, zone))
                return;

            AddSprite(frame, new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                p,
                size,
                ResolveHullScanDisplayColor(displayShade, alpha)
            ));
        }

        static Color ResolveHullScanDisplayColor(int shade, int alpha)
        {
            float t = Math.Max(0f, Math.Min(1f, shade / 255f));
            if (CurrentHullScanColorScale == GridSchematicsConfig.HullColorThermal)
                return WithAlpha(GetThermalScanColor(t), alpha);
            if (CurrentHullScanColorScale == GridSchematicsConfig.HullColorUi)
                return WithAlpha(LerpColor(UiMenuButtonFill, UiSelected, t), alpha);
            return new Color(shade, shade, shade, alpha);
        }

        static Color GetThermalScanColor(float t)
        {
            if (t < 0.25f)
                return LerpColor(new Color(10, 0, 70), new Color(36, 42, 190), t / 0.25f);
            if (t < 0.50f)
                return LerpColor(new Color(36, 42, 190), new Color(165, 0, 185), (t - 0.25f) / 0.25f);
            if (t < 0.76f)
                return LerpColor(new Color(165, 0, 185), new Color(255, 72, 25), (t - 0.50f) / 0.26f);
            if (t < 0.92f)
                return LerpColor(new Color(255, 72, 25), new Color(255, 230, 45), (t - 0.76f) / 0.16f);
            return LerpColor(new Color(255, 230, 45), Color.White, (t - 0.92f) / 0.08f);
        }

        static Color LerpColor(Color from, Color to, float amount)
        {
            if (amount < 0f)
                amount = 0f;
            if (amount > 1f)
                amount = 1f;

            return new Color(
                (byte)Math.Round(from.R + (to.R - from.R) * amount),
                (byte)Math.Round(from.G + (to.G - from.G) * amount),
                (byte)Math.Round(from.B + (to.B - from.B) * amount),
                (byte)Math.Round(from.A + (to.A - from.A) * amount));
        }

        static Color WithAlpha(Color color, int alpha)
        {
            return new Color(color.R, color.G, color.B, ClampByte(alpha));
        }

        static bool TryClipRectToZone(ref Vector2 center, ref Vector2 size, ScreenZone zone)
        {
            float left = center.X - size.X * 0.5f;
            float right = center.X + size.X * 0.5f;
            float top = center.Y - size.Y * 0.5f;
            float bottom = center.Y + size.Y * 0.5f;

            float zoneLeft = zone.X;
            float zoneRight = zone.X + zone.Width;
            float zoneTop = zone.Y;
            float zoneBottom = zone.Y + zone.Height;

            if (right <= zoneLeft || left >= zoneRight || bottom <= zoneTop || top >= zoneBottom)
                return false;

            if (left < zoneLeft) left = zoneLeft;
            if (right > zoneRight) right = zoneRight;
            if (top < zoneTop) top = zoneTop;
            if (bottom > zoneBottom) bottom = zoneBottom;

            float width = right - left;
            float height = bottom - top;
            if (width <= 0.1f || height <= 0.1f)
                return false;

            center = new Vector2((left + right) * 0.5f, (top + bottom) * 0.5f);
            size = new Vector2(width, height);
            return true;
        }

        static float GetScanValue(RawRaycastScanData scanData, int x, int y, string fillMode)
        {
            int index = y * scanData.Resolution + x;
            if (index < 0 || index >= scanData.Samples.Length)
                return 0f;

            var sample = scanData.Samples[index];
            if (fillMode == GridSchematicsConfig.FillNone)
            {
                return 0f;
            }

            if (fillMode == GridSchematicsConfig.FillHits)
            {
                return sample.HasHit ? 1f : 0f;
            }

            if (fillMode == GridSchematicsConfig.FillDensity)
            {
                return sample.HitCount > 0 ? sample.HitCount / (float)Math.Max(1, scanData.MaxHitCount) : 0f;
            }

            if (fillMode == GridSchematicsConfig.FillVoids)
            {
                return GetVoidDeficitValue(scanData, x, y);
            }

            return sample.Thickness > 0 ? sample.Thickness / (float)Math.Max(1, scanData.MaxThickness) : 0f;
        }

        static float GetDisplayScanValue(RawRaycastScanData scanData, int x, int y, string fillMode)
        {
            return GetScanValue(scanData, x, y, fillMode);
        }

        static float GetLocallySmoothedScanValue(RawRaycastScanData scanData, int x, int y, string fillMode)
        {
            int resolution = scanData.Resolution;
            int index = y * resolution + x;
            if (index < 0 || index >= scanData.Samples.Length)
                return 0f;

            bool requireHit = fillMode == GridSchematicsConfig.FillDensity || fillMode == GridSchematicsConfig.FillThickness;
            if (requireHit && !scanData.Samples[index].HasHit)
                return 0f;

            float total = 0f;
            float weightTotal = 0f;

            for (int dy = -1; dy <= 1; dy++)
            {
                int sy = y + dy;
                if (sy < 0 || sy >= resolution)
                    continue;

                for (int dx = -1; dx <= 1; dx++)
                {
                    int sx = x + dx;
                    if (sx < 0 || sx >= resolution)
                        continue;

                    int neighborIndex = sy * resolution + sx;
                    if (requireHit && !scanData.Samples[neighborIndex].HasHit)
                        continue;

                    float weight = dx == 0 && dy == 0 ? 6f : (dx == 0 || dy == 0 ? 2f : 1f);
                    total += GetScanValue(scanData, sx, sy, fillMode) * weight;
                    weightTotal += weight;
                }
            }

            return weightTotal > 0f ? total / weightTotal : 0f;
        }

        static float GetVoidDeficitValue(RawRaycastScanData scanData, int x, int y)
        {
            int resolution = scanData.Resolution;
            int index = y * resolution + x;
            if (index < 0 || index >= scanData.Samples.Length)
                return 0f;

            var sample = scanData.Samples[index];
            bool inSilhouette = sample.HasHit || IsLocallyEnclosedByHits(scanData, x, y, 5);
            if (!inSilhouette)
                return 0f;

            float density = GetLocalRawDensity(scanData, x, y);
            float voidValue = 1f - density;
            if (!sample.HasHit)
                voidValue *= 0.65f;

            voidValue = Math.Max(0f, Math.Min(1f, voidValue));
            return (float)Math.Pow(voidValue, 1.15);
        }

        static float GetLocalRawDensity(RawRaycastScanData scanData, int x, int y)
        {
            int resolution = scanData.Resolution;
            float total = 0f;
            float weightTotal = 0f;

            for (int dy = -1; dy <= 1; dy++)
            {
                int sy = y + dy;
                if (sy < 0 || sy >= resolution)
                    continue;

                for (int dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dy == 0)
                        continue;

                    int sx = x + dx;
                    if (sx < 0 || sx >= resolution)
                        continue;

                    float weight = dx == 0 && dy == 0 ? 4f : (dx == 0 || dy == 0 ? 2f : 1f);
                    total += GetRawDensity(scanData.Samples[sy * resolution + sx], scanData.MaxHitCount) * weight;
                    weightTotal += weight;
                }
            }

            return weightTotal > 0f ? total / weightTotal : 0f;
        }

        static bool IsLocallyEnclosedByHits(RawRaycastScanData scanData, int x, int y, int radius)
        {
            bool left = HasHitInDirection(scanData, x, y, -1, 0, radius);
            bool right = HasHitInDirection(scanData, x, y, 1, 0, radius);
            bool down = HasHitInDirection(scanData, x, y, 0, -1, radius);
            bool up = HasHitInDirection(scanData, x, y, 0, 1, radius);

            return (left && right) || (down && up);
        }

        static bool HasHitInDirection(RawRaycastScanData scanData, int x, int y, int dx, int dy, int radius)
        {
            int resolution = scanData.Resolution;
            for (int i = 1; i <= radius; i++)
            {
                int sx = x + dx * i;
                int sy = y + dy * i;
                if (sx < 0 || sy < 0 || sx >= resolution || sy >= resolution)
                    return false;

                if (scanData.Samples[sy * resolution + sx].HasHit)
                    return true;
            }

            return false;
        }

        static float GetRawDensity(RawRaycastSample sample, int maxHitCount)
        {
            return sample.HitCount > 0 ? sample.HitCount / (float)Math.Max(1, maxHitCount) : 0f;
        }

        static float GetBlurredScanValue(RawRaycastScanData scanData, int x, int y, string fillMode)
        {
            int resolution = scanData.Resolution;
            float total = GetDisplayScanValue(scanData, x, y, fillMode) * 4f;
            float weightTotal = 4f;

            total += GetBlurNeighborValue(scanData, x - 1, y, fillMode, ref weightTotal);
            total += GetBlurNeighborValue(scanData, x + 1, y, fillMode, ref weightTotal);
            total += GetBlurNeighborValue(scanData, x, y - 1, fillMode, ref weightTotal);
            total += GetBlurNeighborValue(scanData, x, y + 1, fillMode, ref weightTotal);

            return weightTotal > 0f ? total / weightTotal : 0f;
        }

        static float GetBlurNeighborValue(RawRaycastScanData scanData, int x, int y, string fillMode, ref float weightTotal)
        {
            int resolution = scanData.Resolution;
            if (x < 0 || y < 0 || x >= resolution || y >= resolution)
                return 0f;

            weightTotal += 1f;
            return GetDisplayScanValue(scanData, x, y, fillMode);
        }

        static void DrawCachedShipBlocks(MySpriteDrawFrame frame, ProjectionTransform transform, ShipGrid shipGrid, Color blockColor, Color fatBlockColor)
        {
            var cached = GetOrBuildBlockGhostRects(transform, shipGrid, blockColor, fatBlockColor);
            if (cached == null || cached.Rects == null)
                return;

            for (int i = 0; i < cached.Rects.Count; i++)
            {
                var rect = cached.Rects[i];
                AddCachedSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", rect.Center, rect.Size, rect.Color));
            }
        }

        static CachedScreenRects GetOrBuildBlockGhostRects(ProjectionTransform transform, ShipGrid shipGrid, Color blockColor, Color fatBlockColor)
        {
            if (shipGrid == null || shipGrid.IsEmpty || shipGrid.Blocks == null)
                return null;

            string key = BuildBlockGhostCacheKey(transform, shipGrid);
            CachedScreenRects cached;
            if (BlockGhostCache.TryGetValue(key, out cached) && cached != null)
            {
                TrackCacheHit();
                cached.LastUsed = ++CacheUseCounter;
                return cached;
            }
            TrackCacheMiss();

            cached = new CachedScreenRects();
            var size = new Vector2(Math.Max(1f, transform.CellSize - 1.5f), Math.Max(1f, transform.CellSize - 1.5f));
            for (int i = 0; i < shipGrid.Blocks.Count; i++)
            {
                var block = shipGrid.Blocks[i];
                cached.Rects.Add(new CachedScreenRect
                {
                    Center = transform.ProjectCellCenter(shipGrid, block.Projected),
                    Size = size,
                    Color = block.HasFatBlock ? fatBlockColor : blockColor
                });
            }

            cached.LastUsed = ++CacheUseCounter;
            BlockGhostCache[key] = cached;
            TrimBlockGhostCache();
            return cached;
        }

        static string BuildBlockGhostCacheKey(ProjectionTransform transform, ShipGrid shipGrid)
        {
            return "blocks:" +
                shipGrid.GetHashCode() + ":" +
                shipGrid.ProjectionView + ":" +
                shipGrid.BlockCount + ":" +
                transform.RotationSteps + ":" +
                (int)(transform.Origin.X * 10f) + ":" +
                (int)(transform.Origin.Y * 10f) + ":" +
                (int)(transform.CellSize * 100f) + ":" +
                transform.SourceWidth + ":" +
                transform.SourceHeight;
        }

        static void TrimBlockGhostCache()
        {
            if (BlockGhostCache.Count <= MaxBlockGhostCacheEntries)
                return;

            string oldestKey = null;
            int oldestUse = int.MaxValue;
            foreach (var entry in BlockGhostCache)
            {
                if (entry.Value != null && entry.Value.LastUsed < oldestUse)
                {
                    oldestUse = entry.Value.LastUsed;
                    oldestKey = entry.Key;
                }
            }

            if (oldestKey != null)
                BlockGhostCache.Remove(oldestKey);
        }

        static void DrawCachedShipBorder(MySpriteDrawFrame frame, ProjectionTransform transform, ShipGrid shipGrid, RawRaycastScanData scanData)
        {
            var cached = GetOrBuildShipBorderRects(scanData, shipGrid, transform.SourceWidth, transform.SourceHeight, transform.CellSize);
            if (cached == null || cached.Rects == null)
                return;

            var borderColor = new Color(255, 255, 255, ClampByte(255f * CurrentShipBorderOpacity));
            for (int i = 0; i < cached.Rects.Count; i++)
            {
                CurrentPerfCachedSpriteCount++;
                var rect = cached.Rects[i];
                DrawTransformedRect(frame, transform, rect.X, rect.Y, rect.Width, rect.Height, borderColor);
            }
        }

        static CachedBorderRects GetOrBuildShipBorderRects(RawRaycastScanData scanData, ShipGrid shipGrid, int sourceWidth, int sourceHeight, float cellSize)
        {
            if (scanData == null || shipGrid == null || !scanData.IsReady || scanData.Samples == null || scanData.Resolution <= 0)
                return null;

            string key = BuildShipBorderCacheKey(scanData, shipGrid, sourceWidth, sourceHeight, cellSize);
            CachedBorderRects cached;
            if (ShipBorderCache.TryGetValue(key, out cached) && cached != null)
            {
                TrackCacheHit();
                cached.LastUsed = ++CacheUseCounter;
                return cached;
            }
            TrackCacheMiss();

            cached = BuildShipBorderRects(scanData, shipGrid, sourceWidth, sourceHeight, cellSize);
            cached.LastUsed = ++CacheUseCounter;
            ShipBorderCache[key] = cached;
            TrimShipBorderCache();
            return cached;
        }

        static CachedBorderRects BuildShipBorderRects(RawRaycastScanData scanData, ShipGrid shipGrid, int sourceWidth, int sourceHeight, float cellSize)
        {
            var cached = new CachedBorderRects();

            int resolution = scanData.Resolution;
            var mask = BuildCleanOccupancyMask(scanData);
            var exterior = BuildExteriorEmptyMask(mask, resolution);
            float spanX = Math.Max(0.001f, scanData.SampleMaxX - scanData.SampleMinX);
            float spanY = Math.Max(0.001f, scanData.SampleMaxY - scanData.SampleMinY);
            float cellWidth = spanX / (float)resolution * cellSize;
            float cellHeight = spanY / (float)resolution * cellSize;
            float thickness = 1f;

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int index = y * resolution + x;
                    if (!mask[index])
                        continue;

                    bool left = x <= 0 || exterior[index - 1];
                    bool right = x >= resolution - 1 || exterior[index + 1];
                    bool top = y <= 0 || exterior[index - resolution];
                    bool bottom = y >= resolution - 1 || exterior[index + resolution];

                    float sampleX0 = scanData.SampleMinX + x / (float)resolution * spanX;
                    float sampleX1 = scanData.SampleMinX + (x + 1) / (float)resolution * spanX;
                    float sampleY0 = scanData.SampleMinY + (y + 1) / (float)resolution * spanY;
                    float sampleY1 = scanData.SampleMinY + y / (float)resolution * spanY;
                    float localX0 = sampleX0 - shipGrid.Min2D.X + 0.5f;
                    float localX1 = sampleX1 - shipGrid.Min2D.X + 0.5f;
                    float localY0 = shipGrid.Max2D.Y - sampleY0 + 0.5f;
                    float localY1 = shipGrid.Max2D.Y - sampleY1 + 0.5f;
                    float localCx = (localX0 + localX1) * 0.5f;
                    float localCy = (localY0 + localY1) * 0.5f;

                    if (left)
                        cached.Rects.Add(new CachedLocalRect { X = localX0, Y = localCy, Width = thickness, Height = cellHeight + 0.6f });
                    if (right)
                        cached.Rects.Add(new CachedLocalRect { X = localX1, Y = localCy, Width = thickness, Height = cellHeight + 0.6f });
                    if (top)
                        cached.Rects.Add(new CachedLocalRect { X = localCx, Y = localY1, Width = cellWidth + 0.6f, Height = thickness });
                    if (bottom)
                        cached.Rects.Add(new CachedLocalRect { X = localCx, Y = localY0, Width = cellWidth + 0.6f, Height = thickness });
                }
            }

            return cached;
        }

        static string BuildShipBorderCacheKey(RawRaycastScanData scanData, ShipGrid shipGrid, int sourceWidth, int sourceHeight, float cellSize)
        {
            return "border:" +
                scanData.GetHashCode() + ":" +
                scanData.View + ":" +
                shipGrid.GetHashCode() + ":" +
                sourceWidth + ":" +
                sourceHeight + ":" +
                (int)(cellSize * 100f) + ":" +
                (int)(scanData.SampleMinX * 100f) + ":" +
                (int)(scanData.SampleMaxX * 100f) + ":" +
                (int)(scanData.SampleMinY * 100f) + ":" +
                (int)(scanData.SampleMaxY * 100f) + ":" +
                scanData.ScannedUtc.Ticks + ":" +
                scanData.Resolution + ":" +
                scanData.HitSampleCount + ":" +
                scanData.MaxHitCount + ":" +
                (int)(scanData.MaxThickness * 1000f);
        }

        static void TrimShipBorderCache()
        {
            if (ShipBorderCache.Count <= MaxBorderCacheEntries)
                return;

            string oldestKey = null;
            int oldestUse = int.MaxValue;
            foreach (var entry in ShipBorderCache)
            {
                if (entry.Value != null && entry.Value.LastUsed < oldestUse)
                {
                    oldestUse = entry.Value.LastUsed;
                    oldestKey = entry.Key;
                }
            }

            if (oldestKey != null)
                ShipBorderCache.Remove(oldestKey);
        }

        static void DrawTransformedRect(MySpriteDrawFrame frame, ProjectionTransform transform, float localX, float localY, float widthPx, float heightPx, Color color)
        {
            AddSprite(frame, new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                transform.ProjectLocalPoint(localX, localY),
                transform.GetRotatedSize(widthPx, heightPx),
                color
            ));
        }

        static bool[] BuildExteriorEmptyMask(bool[] occupancyMask, int resolution)
        {
            var exterior = new bool[resolution * resolution];
            var queue = new int[resolution * resolution];
            int head = 0;
            int tail = 0;

            for (int x = 0; x < resolution; x++)
            {
                TryQueueExterior(occupancyMask, exterior, queue, ref tail, x);
                TryQueueExterior(occupancyMask, exterior, queue, ref tail, (resolution - 1) * resolution + x);
            }

            for (int y = 1; y < resolution - 1; y++)
            {
                TryQueueExterior(occupancyMask, exterior, queue, ref tail, y * resolution);
                TryQueueExterior(occupancyMask, exterior, queue, ref tail, y * resolution + resolution - 1);
            }

            while (head < tail)
            {
                int index = queue[head++];
                int x = index % resolution;
                int y = index / resolution;

                if (x > 0)
                    TryQueueExterior(occupancyMask, exterior, queue, ref tail, index - 1);
                if (x < resolution - 1)
                    TryQueueExterior(occupancyMask, exterior, queue, ref tail, index + 1);
                if (y > 0)
                    TryQueueExterior(occupancyMask, exterior, queue, ref tail, index - resolution);
                if (y < resolution - 1)
                    TryQueueExterior(occupancyMask, exterior, queue, ref tail, index + resolution);
            }

            return exterior;
        }

        static void TryQueueExterior(bool[] occupancyMask, bool[] exterior, int[] queue, ref int tail, int index)
        {
            if (index < 0 || index >= occupancyMask.Length || occupancyMask[index] || exterior[index])
                return;

            exterior[index] = true;
            queue[tail++] = index;
        }

        static bool[] BuildCleanOccupancyMask(RawRaycastScanData scanData)
        {
            int resolution = scanData.Resolution;
            var mask = new bool[resolution * resolution];

            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    int index = y * resolution + x;
                    if (scanData.Samples[index].HasHit)
                    {
                        mask[index] = true;
                        continue;
                    }

                    int hits = CountHitNeighbors(scanData, x, y, 1);
                    mask[index] = hits >= 5;
                }
            }

            return mask;
        }

        static int CountHitNeighbors(RawRaycastScanData scanData, int x, int y, int radius)
        {
            int count = 0;
            int resolution = scanData.Resolution;
            for (int dy = -radius; dy <= radius; dy++)
            {
                int sy = y + dy;
                if (sy < 0 || sy >= resolution)
                    continue;

                for (int dx = -radius; dx <= radius; dx++)
                {
                    int sx = x + dx;
                    if (sx < 0 || sx >= resolution)
                        continue;

                    if (scanData.Samples[sy * resolution + sx].HasHit)
                        count++;
                }
            }

            return count;
        }

        static void DrawExtendedReferenceGrid(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, ProjectionTransform transform, int gridStep, int subdivisions, Color minorColor, Color majorColor)
        {
            int extraCells = (int)Math.Ceiling(Math.Max(center.Width, center.Height) / Math.Max(1f, transform.CellSize));
            int firstX = FloorToStep(-extraCells, gridStep);
            int lastX = transform.SourceWidth + extraCells;
            float extendedHeight = (transform.SourceHeight + extraCells * 2) * transform.CellSize;
            float extendedWidth = (transform.SourceWidth + extraCells * 2) * transform.CellSize;
            for (int x = firstX; x <= lastX; x += gridStep)
            {
                int worldX = shipGrid.Min2D.X + x;
                bool isMajor = worldX % subdivisions == 0;
                DrawTransformedRect(frame, transform, x, transform.SourceHeight * 0.5f, 1f, extendedHeight, isMajor ? majorColor : minorColor);
            }

            int firstY = FloorToStep(-extraCells, gridStep);
            int lastY = transform.SourceHeight + extraCells;
            for (int y = firstY; y <= lastY; y += gridStep)
            {
                int worldY = shipGrid.Min2D.Y + y;
                bool isMajor = worldY % subdivisions == 0;
                DrawTransformedRect(frame, transform, transform.SourceWidth * 0.5f, transform.SourceHeight - y, extendedWidth, 1f, isMajor ? majorColor : minorColor);
            }
        }

        static void DrawReferenceCenterLines(MySpriteDrawFrame frame, ScreenZone center, ShipGrid shipGrid, ProjectionTransform transform, Color axisColor)
        {
            float localX = 0f - shipGrid.Min2D.X + 0.5f;
            float localY = shipGrid.Max2D.Y - 0f + 0.5f;
            float extendedLength = Math.Max(center.Width, center.Height) * 2f;

            if (localX >= 0f && localX <= transform.SourceWidth)
                DrawTransformedRect(frame, transform, localX, transform.SourceHeight * 0.5f, 2f, extendedLength, axisColor);

            if (localY >= 0f && localY <= transform.SourceHeight)
                DrawTransformedRect(frame, transform, transform.SourceWidth * 0.5f, localY, extendedLength, 2f, axisColor);
        }

        static int FloorToStep(int value, int step)
        {
            if (step <= 1)
                return value;

            int remainder = value % step;
            if (remainder < 0)
                remainder += step;
            return value - remainder;
        }

        static void RenderTextSurface(GridSchematicsLcdApp app, IMyTextSurface surface)
        {
            var builder = new StringBuilder();
            builder.AppendLine("Grid Schematics");
            builder.AppendLine($"Page: {app.Ui.ActivePage}");
            builder.AppendLine($"View: {app.Config.View}");
            builder.AppendLine($"Mode: {app.Ui.ActiveDisplayMode}");
            builder.AppendLine($"Overlay: {app.Ui.ActiveOverlay}");
            builder.AppendLine($"Resolution: {app.Config.Resolution}");
            builder.AppendLine($"Fill: {app.Config.FillMode}");
            builder.AppendLine();

            var zones = UiLayout.BuildZones(512, 512);
            builder.AppendLine($"Top zone: {zones.Top.Width}x{zones.Top.Height}");
            builder.AppendLine($"Left rail: {zones.Left.Width}x{zones.Left.Height}");
            builder.AppendLine($"Center viewport: {zones.Center.Width}x{zones.Center.Height}");
            builder.AppendLine($"Right rail: {zones.Right.Width}x{zones.Right.Height}");
            builder.AppendLine($"Bottom strip: {zones.Bottom.Width}x{zones.Bottom.Height}");
            builder.AppendLine();

            if (app.ConstructCache?.ShipGrid != null && !app.ConstructCache.ShipGrid.IsEmpty)
            {
                var shipGrid = app.ConstructCache.ShipGrid;
                var transform = GetOrBuildProjectionTransform(shipGrid, zones.Center);
                builder.AppendLine($"Projected grid: {shipGrid.Size2D.X}x{shipGrid.Size2D.Y}");
                builder.AppendLine($"Depth: {shipGrid.DepthSize}");
                builder.AppendLine($"Construct grids: {shipGrid.GridCount}");
                builder.AppendLine($"Projected blocks: {shipGrid.BlockCount}");
                builder.AppendLine($"Scale: {transform.CellSize:0.0}px/block");
                builder.AppendLine();
            }
            else
            {
                builder.AppendLine("Projected grid: unavailable");
                builder.AppendLine();
            }

            if (app.ConstructCache?.ConveyorNetwork != null)
            {
                builder.AppendLine($"Conveyor nodes: {app.ConstructCache.ConveyorNetwork.Nodes.Count}");
                builder.AppendLine($"Conveyor ports: {app.ConstructCache.ConveyorNetwork.Ports.Count}");
                builder.AppendLine($"Conveyor lines: {app.ConstructCache.ConveyorNetwork.Lines.Count}");
                builder.AppendLine($"Conveyor edges: {app.ConstructCache.ConveyorNetwork.Edges.Count}");
            }
            else
            {
                builder.AppendLine("Conveyor topology: not scanned yet");
            }

            if (app.TouchInput.IsAvailable)
            {
                builder.AppendLine("Touch input: available");
                if (!string.IsNullOrEmpty(app.TouchInput.LastHitRegionId))
                {
                    builder.AppendLine($"Touched: {app.TouchInput.LastHitRegionId}");
                }
            }
            else
            {
                builder.AppendLine("TouchScreenAPI: unavailable");
            }

            builder.AppendLine();
            builder.AppendLine("[Render scaffold active]");
            builder.AppendLine("Use GridSchematics/Rendering to add custom sprite output.");

            surface.WriteText(builder.ToString(), false);
        }
    }
}








