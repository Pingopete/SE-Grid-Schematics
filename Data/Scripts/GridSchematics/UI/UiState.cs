using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.ModAPI;
using VRageMath;

namespace GridSchematics
{
    public enum PageType
    {
        Structure,
        Cargo,
        Power,
        Engines,
        Oxygen,
        Info,
        Settings
    }

    public enum DisplayMode
    {
        Overview,
        Network,
        Diagnostics
    }

    public enum OverlayMode
    {
        None,
        Cargo,
        Power,
        Engines,
        Oxygen,
        Conveyor,
        Systems,
        Custom
    }

    public enum MenuPanel
    {
        None,
        View,
        Layers,
        Tools,
        Scan,
        Settings
    }

    public enum InfoPanelMode
    {
        Systems,
        Scan
    }

    public class UiState
    {
        public const int SelectedBlockStackAllIndex = -2;
        public const int SelectedBlockStackAggregateIndex = -1;

        public PageType ActivePage { get; set; } = PageType.Structure;
        public DisplayMode ActiveDisplayMode { get; set; } = DisplayMode.Overview;
        public OverlayMode ActiveOverlay { get; set; } = OverlayMode.None;
        public MenuPanel ActiveMenu { get; set; } = MenuPanel.None;
        public int SettingsExpandedMask { get; set; }
        public string ActiveSettingsActionId { get; set; }
        public bool IsModalOpen { get; set; }
        public bool ShowDiscoveredBlocks { get; set; } = false;
        public bool ShowShipBorder { get; set; } = true;
        public float ShipBorderOpacity { get; set; } = 1f;
        public bool ShowHullScan { get; set; } = true;
        public float HullScanBrightness { get; set; } = 1f;
        public bool ShowDebugGrid { get; set; } = true;
        public int GridVisibilityLevel { get; set; } = 1;
        public bool ShowReferenceLines { get; set; } = true;
        public bool ShowCenterOfMassMarker { get; set; }
        public bool ShowPanelPositionMarker { get; set; }
        public bool ShowDockedMobileGrids { get; set; }
        public bool ShowAllConnections { get; set; }
        public bool ShowConveyorOverlay { get; set; } = true;
        public int FillBarsVisibilityLevel { get; set; } = 2;
        public bool ShowFillBars
        {
            get { return FillBarsVisibilityLevel > 0; }
            set { FillBarsVisibilityLevel = value ? 2 : 0; }
        }
        public float FillBarsAlphaScale
        {
            get { return FillBarsVisibilityLevel == 1 ? 0.7f : FillBarsVisibilityLevel > 1 ? 1f : 0f; }
        }
        public bool BlurScanRender { get; set; } = true;
        public bool ShowInfoPanel { get; set; }
        public InfoPanelMode InfoPanelMode { get; set; } = InfoPanelMode.Systems;
        public bool SegmentMode { get; set; }
        public bool ChromeHidden { get; set; }
        public ShipGrid SegmentFrontGrid { get; set; }
        public ShipGrid SegmentLeftGrid { get; set; }
        public ShipGrid SegmentTopGrid { get; set; }
        public int LastSegmentProjectionRefreshTick { get; set; } = -600;
        public int SegmentProjectionRefreshStep { get; set; }
        public string ModalMessage { get; set; }
        public bool CalibrationPromptDismissed { get; set; }
        public bool ShowCalibrationPrompt { get; set; }
        public PanelPerfStats LastPerfStats { get; } = new PanelPerfStats();
        public bool CalibrationActive { get; set; }
        public bool CursorCalibrationRequired { get; set; }
        public int CalibrationStep { get; set; }
        public int CalibrationCompletedTick { get; set; } = -1;
        public int CalibrationRestartCountdownSeconds { get; set; } = 5;
        public Vector2[] CalibrationRawPoints { get; } = new Vector2[3];
        public int SelectedNetworkIndex { get; set; }
        public int ZoomLevel { get; set; }
        public int PanX { get; set; }
        public int PanY { get; set; }
        public ScreenZone RenderViewportZone { get; set; }
        public string SelectedOverlayBlockId { get; set; }
        public int SelectedOverlayLineIndex { get; set; }
        public string PreviewBlockStackSignature { get; set; }
        public string SelectedBlockStackSignature { get; set; }
        public int PreviewBlockStackIndex { get; set; }
        public int SelectedBlockStackIndex { get; set; } = SelectedBlockStackAllIndex;
        public int SelectedBlockStackScrollIndex { get; set; }
        public string CargoInfoFilter { get; set; } = "ALL";
        public string CargoInfoFocus { get; set; } = "ALL";
        public string CargoInfoSource { get; set; } = "LOCAL";
        public long CargoAuxiliaryGridId { get; set; }
        public int CargoActionScrollIndex { get; set; }
        public int CargoBlockScrollIndex { get; set; }
        public int CargoBlockCursorIndex { get; set; } = 6;
        public int CargoBlockCursorActiveUntilTick { get; set; }
        public int CargoMixScrollIndex { get; set; }
        public string CargoMixSortKey { get; set; } = "QUANT";
        public int CargoMixSortDirection { get; set; } = 1;
        public bool CargoFilterDropdownOpen { get; set; }
        public string CargoRightPanelMode { get; set; } = "TRANSFER";
        public string CargoTransferCaptureTarget { get; set; } = string.Empty;
        public int CargoTransferCaptureUntilTick { get; set; }
        public string CargoTransferMixViewTarget { get; set; } = "SOURCE";
        public bool CargoTransferSelectionActive { get; set; }
        public bool CargoTransferDirectionReversed { get; set; }
        public int CargoTransferQuotaScrollIndex { get; set; }
        public List<string> CargoMixSelectedItemKeys { get; } = new List<string>();
        public string CargoTransferSourceLabel { get; set; } = string.Empty;
        public string CargoTransferDestLabel { get; set; } = string.Empty;
        public List<BlockStackItem> CargoTransferSourceItems { get; } = new List<BlockStackItem>();
        public List<BlockStackItem> CargoTransferDestItems { get; } = new List<BlockStackItem>();
        public List<CargoTransferQuotaItem> CargoTransferQuotaItems { get; } = new List<CargoTransferQuotaItem>();
        public string CachedCargoSummaryKey { get; set; }
        public RenderEngine.CargoPanelSummary CachedCargoSummary { get; set; }
        public string CachedCargoLoadSummaryKey { get; set; }
        public RenderEngine.CargoPanelSummary CachedCargoLoadSummary { get; set; }
        public int CachedCargoSummaryTick { get; set; } = -1000;
        public int CachedCargoLoadSummaryTick { get; set; } = -1000;
        public List<HitRegion> HitRegions { get; } = new List<HitRegion>();
        public List<OverlayBlockInfo> OverlayBlockRegions { get; } = new List<OverlayBlockInfo>();
        public List<BlockStackItem> PreviewBlockStackItems { get; } = new List<BlockStackItem>();
        public List<BlockStackItem> SelectedBlockStackItems { get; } = new List<BlockStackItem>();
        public List<BlockStackItem> ManualSelectedBlockItems { get; } = new List<BlockStackItem>();
        public List<BlockStackItem> ManualSelectedBlockItems2 { get; } = new List<BlockStackItem>();
        public bool ManualGroup2Enabled { get; set; }
        public int ActiveManualGroupIndex { get; set; }
    }

    public class PanelPerfStats
    {
        public int SpriteCount;
        public int TextCount;
        public int CachedSpriteCount;
        public int CacheHits;
        public int CacheMisses;
        public int RenderTickDelta;
        public int RenderIntervalTicks;
        public int ScanStep;
        public int ScanResolution;
        public bool CursorOnly;
        public bool MotionActive;
    }

    public class BlockStackItem
    {
        public string Id;
        public string Name;
        public IMyCubeBlock Block;
        public Vector2I Projected;
        public int Depth;
    }

    public class CargoTransferQuotaItem
    {
        public string Key;
        public string Name;
        public string Category;
        public string TypeId;
        public string SubtypeId;
        public float Amount;
        public float Volume;
        public float MaxAmount;
    }

    public class OverlayBlockInfo
    {
        public HitRegion Region;
        public string Id;
        public string Name;
        public string Role;
        public string State;
        public string Metric;
        public string Damage;
        public int Count;
        public List<IMyCubeBlock> Blocks = new List<IMyCubeBlock>();
        public List<IMyFunctionalBlock> ToggleBlocks = new List<IMyFunctionalBlock>();
        public List<OverlayBlockInfoLine> Lines = new List<OverlayBlockInfoLine>();

        public bool CanToggle
        {
            get { return ToggleBlocks != null && ToggleBlocks.Count > 0; }
        }
    }

    public class OverlayBlockInfoLine
    {
        public string Text;
        public bool IsFillBar;
        public bool IsSeparator;
        public float FillRatio;
        public IMyFunctionalBlock ToggleBlock;
        public IMyBatteryBlock BatteryBlock;
        public IMyTerminalBlock TerminalBlock;
        public ITerminalAction TerminalAction;
        public string TerminalActionId;
        public List<IMyTerminalBlock> TerminalBlocks = new List<IMyTerminalBlock>();
        public List<ITerminalAction> TerminalActions = new List<ITerminalAction>();

        public bool CanToggle
        {
            get { return ToggleBlock != null || BatteryBlock != null || (TerminalBlock != null && TerminalAction != null); }
        }
    }

    public struct HitRegion
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public string Id;
        public string Hint;

        public HitRegion(int x, int y, int width, int height, string id, string hint)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Id = id;
            Hint = hint;
        }
    }
}









