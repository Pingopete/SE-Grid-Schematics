using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VRage.Game.Entity;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace GridSchematics
{
    public partial class GridSchematicsSession
    {
        const string PanelDiscoveryPolygonDebugCommand = "\\PanelPolyTest";
        const string PanelDiscoveryPolygonAltDebugCommandName = "DISPLAYCALALT";
        const string PanelCursorCalibrationCommandName = "GSUICAL";
        const int PolygonDebugMaxSteps = 140;
        const int PolygonDebugRefineSteps = 8;
        const double PolygonDebugStep = 0.015;
        const double PolygonDebugCastDepth = 0.07;
        const double PolygonDebugPlaneTolerance = 0.006;
        const double PolygonDebugNormalDotThreshold = 0.9975;
        const double PolygonDebugMinimumSize = 0.06;
        const float PolygonDebugTriangleProbeRadius = 0.28f;
        const int PolygonDebugMaxProbeTriangles = 512;
        const float PolygonDebugOcclusionProbeRadius = 0.06f;
        const double PolygonDebugOcclusionDepth = 0.16;
        const double PolygonDebugOcclusionSurfaceClearance = 0.004;
        const double PolygonDebugOcclusionNormalDotThreshold = 0.985;
        const double PolygonDebugConnectionTolerance = 0.012;
        const double PolygonDebugOcclusionProjectedTolerance = 0.018;
        const float PolygonDebugSubpartTrimProbeRadius = 0.7f;
        const double PolygonDebugSubpartTrimDepth = 0.22;
        const double PolygonDebugSubpartTrimProjectedTolerance = 0.012;
        const double PolygonDebugNearbyOccluderSearchRadius = 1.6;
        const double PolygonDebugNeighborProbeOutsideOffset = 0.055;
        const double PolygonDebugNeighborProbeDepth = 0.24;
        const double PolygonDebugSpatialOccluderSearchPadding = 0.45;
        const double PolygonDebugSpatialOccluderDepth = 0.28;
        const float PolygonDebugSpatialOccluderSampleRadius = 0.24f;
        const double PolygonDebugCameraRayHarvestOutsideOffset = 0.08;
        const int PolygonDebugTriangleFillStrips = 10;
        const int PolygonDebugMaskLongAxisCells = 72;
        const int PolygonDebugMaskMinAxisCells = 18;
        const double PolygonDebugAltAimMaxDistance = 12.0;
        const double PolygonDebugAltAimPlanePadding = 0.35;
        const double PolygonDebugAltAimCenterPadding = 0.65;
        const double PolygonDebugAltAimRenderedCenterPadding = 2.2;
        const double PolygonDebugAltModelHitDefaultHalfA = 0.26;
        const double PolygonDebugAltModelHitDefaultHalfB = 0.145;
        const double PolygonDebugAltModelFitSampleStep = 0.055;
        const double PolygonDebugAltModelFitMaxSampleDistance = 0.24;
        const string PanelCalibrationCatalogSeparator = "----- Grid Schematics panel calibration -----";
        const string PanelCalibrationBlockExportFileName = "PanelCursorCalibrationBlockExport.txt";
        const string PanelCalibrationCatalogFileName = "PanelCursorCalibrationCatalog.txt";
        const double PanelCursorCalibrationBorderInnerEdgeInset = 0.001;
        const long ManualCalibrationInputFocusGapTicks = 500L * TimeSpan.TicksPerMillisecond;

        readonly List<MyTriangle_Vertex_Normals> _polygonDebugTriangles = new List<MyTriangle_Vertex_Normals>(PolygonDebugMaxProbeTriangles);
        readonly List<MyTriangle_Vertex_Normals> _polygonDebugOcclusionTriangles = new List<MyTriangle_Vertex_Normals>(96);
        readonly List<MyTriangle_Vertex_Normals> _polygonDebugSubpartTrimTriangles = new List<MyTriangle_Vertex_Normals>(256);
        readonly List<MyTriangle_Vertex_Normals> _polygonDebugSpatialOccluderTriangles = new List<MyTriangle_Vertex_Normals>(512);
        readonly List<MyTriangle_Vertex_Normals> _polygonDebugConnectedTriangles = new List<MyTriangle_Vertex_Normals>(PolygonDebugMaxProbeTriangles);
        readonly List<IMyEntity> _polygonDebugOcclusionEntities = new List<IMyEntity>();
        readonly HashSet<IMyEntity> _polygonDebugNearbyEntities = new HashSet<IMyEntity>();
        readonly List<PolygonDebugWorldTriangle> _polygonDebugWorldOccluderTriangles = new List<PolygonDebugWorldTriangle>(512);
        readonly List<int> _polygonDebugTriangleQueue = new List<int>(PolygonDebugMaxProbeTriangles);
        readonly List<bool> _polygonDebugTriangleVisited = new List<bool>(PolygonDebugMaxProbeTriangles);
        bool _panelDiscoveryPolygonDebugEnabled;
        bool _panelDiscoveryPolygonDebugCommandRegistered;
        bool _polygonDebugCaptureWasPressed;
        bool _polygonDebugVisualsVisible;
        bool _polygonDebugVisualToggleWasPressed;
        bool _polygonDebugCalibrationCopyWasPressed;
        bool _polygonDebugFocusWasPressed;
        bool _polygonDebugIndexDecreaseWasPressed;
        bool _polygonDebugIndexIncreaseWasPressed;
        bool _polygonDebugResetWasPressed;
        bool _calibratedDebugExportWasPressed;
        bool _manualCalibrationMouseInputArmed;
        bool _polygonDebugAlternateAimMode;
        bool _hasPolygonDebugAltAimSeed;
        TouchScreenApiAdapter _polygonDebugAltAimInput;
        double _polygonDebugAltAimLocalA;
        double _polygonDebugAltAimLocalB;
        bool _hasPolygonDebugAltAimFrame;
        Vector3D _polygonDebugAltAimAxisA;
        Vector3D _polygonDebugAltAimAxisB;
        double _polygonDebugAltAimMinA;
        double _polygonDebugAltAimMaxA;
        double _polygonDebugAltAimMinB;
        double _polygonDebugAltAimMaxB;
        bool _polygonDebugScrollInitialized;
        int _polygonDebugLastScrollValue;
        long _manualCalibrationInputLastDrawTicks;
        readonly Dictionary<string, PolygonDebugCalibrationDraft> _polygonDebugCalibrationDrafts = new Dictionary<string, PolygonDebugCalibrationDraft>();
        ManualCalibrationExportNotice _manualCalibrationExportNotice;
        ManualCalibrationStatusNotice _manualCalibrationStatusNotice;
        bool _hasPolygonDebugSurface;
        IMyEntity _polygonDebugEntity;
        Vector3D _polygonDebugSeed;
        Vector3D _polygonDebugNormal;
        Vector3D _polygonDebugAxisA;
        Vector3D _polygonDebugAxisB;
        Vector3D _polygonDebugTopLeft;
        Vector3D _polygonDebugTopRight;
        Vector3D _polygonDebugBottomRight;
        Vector3D _polygonDebugBottomLeft;
        IMyEntity _polygonDebugRootBlockEntity;
        IMyCubeBlock _polygonDebugOwnerBlock;
        double _polygonDebugMinA;
        double _polygonDebugMaxA;
        double _polygonDebugMinB;
        double _polygonDebugMaxB;
        double _polygonDebugCalibrationOffsetA;
        double _polygonDebugCalibrationOffsetB;
        double _polygonDebugCalibrationScaleA = 1.0;
        double _polygonDebugCalibrationScaleB = 1.0;
        double _polygonDebugCalibrationDepthOffset = PanelDiscoveryCursorSurfaceOffset;
        bool _polygonDebugManualBoundsMode;
        bool _polygonDebugFallbackBoundsMode;
        double _polygonDebugManualHalfA = 0.25;
        double _polygonDebugManualHalfB = 0.15;
        int _polygonDebugCalibrationScreenIndex = -1;

        public bool IsManualPanelCalibrationActive
        {
            get { return _panelDiscoveryPolygonDebugEnabled; }
        }

        public bool HasManualPanelCalibrationSurface
        {
            get { return _panelDiscoveryPolygonDebugEnabled && _hasPolygonDebugSurface; }
        }

        public bool IsManualPanelCalibrationSurface(IMyCubeBlock block, int surfaceIndex)
        {
            if (!_panelDiscoveryPolygonDebugEnabled || !_hasPolygonDebugSurface || block == null || _polygonDebugOwnerBlock == null)
                return false;

            if (block.EntityId != _polygonDebugOwnerBlock.EntityId)
                return false;

            return _polygonDebugCalibrationScreenIndex < 0 || surfaceIndex < 0 || _polygonDebugCalibrationScreenIndex == surfaceIndex;
        }

        public bool IsManualPanelCalibrationSelectedSurface(IMyCubeBlock block, int surfaceIndex)
        {
            if (!_panelDiscoveryPolygonDebugEnabled || !_hasPolygonDebugSurface || block == null || _polygonDebugOwnerBlock == null)
                return false;

            if (block.EntityId != _polygonDebugOwnerBlock.EntityId)
                return false;

            return _polygonDebugCalibrationScreenIndex >= 0 &&
                surfaceIndex >= 0 &&
                _polygonDebugCalibrationScreenIndex == surfaceIndex;
        }

        public bool IsManualPanelCalibrationPeerSurface(IMyCubeBlock block)
        {
            if (!_panelDiscoveryPolygonDebugEnabled || !_hasPolygonDebugSurface || block == null || _polygonDebugOwnerBlock == null)
                return false;

            return _polygonDebugCalibrationScreenIndex >= 0 && block.EntityId == _polygonDebugOwnerBlock.EntityId;
        }

        public bool IsManualPanelCalibrationFallbackSurface(IMyCubeBlock block, int surfaceIndex)
        {
            return _polygonDebugFallbackBoundsMode && IsManualPanelCalibrationSurface(block, surfaceIndex);
        }

        public class ManualCalibrationExportNotice
        {
            public long BlockEntityId;
            public string BlockId;
            public string[] IndexLines;
        }

        public class ManualCalibrationStatusNotice
        {
            public long BlockEntityId;
            public int SurfaceIndex;
            public string Status;
        }

        class PolygonDebugCalibrationDraft
        {
            public long BlockEntityId;
            public int SurfaceIndex;
            public Vector3D SeedLocal;
            public Vector3D NormalLocal;
            public Vector3D AxisALocal;
            public Vector3D AxisBLocal;
            public double MinA;
            public double MaxA;
            public double MinB;
            public double MaxB;
            public double OffsetA;
            public double OffsetB;
            public double ScaleA;
            public double ScaleB;
            public double DepthOffset;
            public bool ManualBoundsMode;
            public bool FallbackBoundsMode;
            public double ManualHalfA;
            public double ManualHalfB;
        }

        struct PolygonDebugWorldTriangle
        {
            public Vector3D V0;
            public Vector3D V1;
            public Vector3D V2;
        }

        struct PolygonDebugAltAimCandidate
        {
            public TouchScreenApiAdapter Input;
            public IMyEntity Entity;
            public IMyCubeBlock OwnerBlock;
            public Vector3D HitPosition;
            public Vector3D HitNormal;
            public int SurfaceIndex;
            public double LocalA;
            public double LocalB;
            public Vector3D AxisA;
            public Vector3D AxisB;
            public double MinA;
            public double MaxA;
            public double MinB;
            public double MaxB;
            public double Score;
        }

        void RegisterPanelDiscoveryPolygonDebugCommand()
        {
            if (_panelDiscoveryPolygonDebugCommandRegistered || MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.MessageEntered += OnPanelDiscoveryPolygonDebugMessageEntered;
            _panelDiscoveryPolygonDebugCommandRegistered = true;
        }

        void UnregisterPanelDiscoveryPolygonDebugCommand()
        {
            if (!_panelDiscoveryPolygonDebugCommandRegistered || MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.MessageEntered -= OnPanelDiscoveryPolygonDebugMessageEntered;
            _panelDiscoveryPolygonDebugCommandRegistered = false;
        }

        void OnPanelDiscoveryPolygonDebugMessageEntered(string messageText, ref bool sendToOthers)
        {
            string command = messageText == null ? string.Empty : messageText.Trim();
            string[] parts = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 0 &&
                (parts[0].Equals(PanelCursorCalibrationCommandName, StringComparison.OrdinalIgnoreCase) ||
                parts[0].Equals("/" + PanelCursorCalibrationCommandName, StringComparison.OrdinalIgnoreCase) ||
                parts[0].Equals("\\" + PanelCursorCalibrationCommandName, StringComparison.OrdinalIgnoreCase)))
            {
                sendToOthers = false;
                if (!RecalibrateAimedPanelCursor())
                    ShowPolygonDebugMessage("Aim at a Grid Schematics panel and use /GSUICAL again");
                return;
            }

            if (parts.Length == 0)
                return;

            bool isAltCommand = IsPanelDiscoveryPolygonAltDebugCommand(parts[0]);
            if (!isAltCommand &&
                !parts[0].Equals(PanelDiscoveryPolygonDebugCommand, StringComparison.OrdinalIgnoreCase) &&
                !parts[0].Equals("/PanelPolyTest", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            sendToOthers = false;
            HandlePanelCalibrationPrototypeCommand(parts, isAltCommand ? "/" + PanelDiscoveryPolygonAltDebugCommandName : parts[0]);
        }

        static bool IsPanelDiscoveryPolygonAltDebugCommand(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                return false;

            string trimmed = command.Trim();
            if (trimmed.StartsWith("/", StringComparison.Ordinal) || trimmed.StartsWith("\\", StringComparison.Ordinal))
                trimmed = trimmed.Substring(1);

            return trimmed.Equals(PanelDiscoveryPolygonAltDebugCommandName, StringComparison.OrdinalIgnoreCase);
        }

        void HandlePanelCalibrationPrototypeCommand(string[] parts, string commandName)
        {
            bool requestedAltMode = IsPanelDiscoveryPolygonAltDebugCommand(commandName);
            if (parts.Length > 1)
            {
                if (!_panelDiscoveryPolygonDebugEnabled)
                    EnablePanelCalibrationPrototype(commandName);
                else
                {
                    _polygonDebugAlternateAimMode = requestedAltMode;
                    if (requestedAltMode)
                        TryPrimePolygonDebugAltAimSeed();
                    else
                        ClearPolygonDebugAltAimSeed();
                }

                HandlePanelDiscoveryPolygonDebugSubCommand(parts, commandName);
                return;
            }

            if (_panelDiscoveryPolygonDebugEnabled && _polygonDebugAlternateAimMode != requestedAltMode)
            {
                _polygonDebugAlternateAimMode = requestedAltMode;
                if (requestedAltMode)
                    TryPrimePolygonDebugAltAimSeed();
                else
                    ClearPolygonDebugAltAimSeed();
                ShowPolygonDebugMessage(requestedAltMode ? "Manual panel calibration ALT seed mode ON" : "Manual panel calibration standard seed mode ON");
                return;
            }

            _panelDiscoveryPolygonDebugEnabled = !_panelDiscoveryPolygonDebugEnabled;
            if (_panelDiscoveryPolygonDebugEnabled)
                EnablePanelCalibrationPrototype(commandName);
            else
                DisablePanelCalibrationPrototype(commandName);
        }

        void EnablePanelCalibrationPrototype(string commandName)
        {
            _panelDiscoveryPolygonDebugEnabled = true;
            _polygonDebugAlternateAimMode = IsPanelDiscoveryPolygonAltDebugCommand(commandName);
            if (_polygonDebugAlternateAimMode)
                TryPrimePolygonDebugAltAimSeed();
            else
                ClearPolygonDebugAltAimSeed();
            _panelCalibrationDebugEnabled = false;
            _panelDiscoveryDebugEnabled = false;
            _hasGlobalModelWalkDebugCorners = false;
            _globalModelWalkCaptureWasPressed = false;
            _polygonDebugCaptureWasPressed = false;
            _polygonDebugVisualToggleWasPressed = false;
            _polygonDebugCalibrationCopyWasPressed = false;
            _polygonDebugFocusWasPressed = false;
            _polygonDebugIndexDecreaseWasPressed = false;
            _polygonDebugIndexIncreaseWasPressed = false;
            _polygonDebugResetWasPressed = false;
            _calibratedDebugExportWasPressed = false;
            _polygonDebugScrollInitialized = false;
            _polygonDebugVisualsVisible = false;
            if (!_polygonDebugAlternateAimMode && TrySeedManualCalibrationFromPhysicalAimedBlock())
            {
                ShowPolygonDebugMessage("Manual panel calibration ON - physical block focused");
            }
            else if (!_polygonDebugAlternateAimMode && TrySeedPolygonDebugFromAimedStoredCalibration())
            {
                ApplyManualLowLevelCalibrationToOwnerBlock(_polygonDebugOwnerBlock);
                ShowPolygonDebugMessage("Manual panel calibration ON - loaded existing bounds for adjustment");
            }
            else if (_polygonDebugAlternateAimMode && TryBuildPolygonDebugSurface())
            {
                _hasPolygonDebugSurface = true;
                NoteManualCalibrationStatus(_polygonDebugOwnerBlock, _polygonDebugCalibrationScreenIndex, "ALT TARGET SELECTED");
                ApplyManualLowLevelCalibrationToOwnerBlock(_polygonDebugOwnerBlock);
                ShowPolygonDebugMessage(BuildPolygonDebugAltSelectionMessage("Manual panel calibration ALT ON"));
            }
            else
            {
                TouchScreenApiAdapter aimedInput;
                if (TryFindAimedCalibrationInput(null, out aimedInput) && aimedInput != null)
                    ApplyManualLowLevelCalibrationToOwnerBlock(aimedInput.OwnerBlock);
                ShowPolygonDebugMessage((_polygonDebugAlternateAimMode ? "Manual panel calibration ALT ON - aim near the LCD, then click. " : "Manual panel calibration ON - aim and click the LCD, then adjust or save. ") + commandName + " help");
            }
        }

        void DisablePanelCalibrationPrototype(string commandName)
        {
            ClearPolygonDebugSurface();
            _polygonDebugCaptureWasPressed = false;
            _polygonDebugVisualToggleWasPressed = false;
            _polygonDebugCalibrationCopyWasPressed = false;
            _polygonDebugFocusWasPressed = false;
            _polygonDebugResetWasPressed = false;
            _calibratedDebugExportWasPressed = false;
            _polygonDebugScrollInitialized = false;
            _polygonDebugIndexDecreaseWasPressed = false;
            _polygonDebugIndexIncreaseWasPressed = false;
            _polygonDebugAlternateAimMode = false;
            ClearPolygonDebugAltAimSeed();
            ShowPolygonDebugMessage("Manual panel calibration OFF");
        }

        bool TrySeedManualCalibrationFromPhysicalAimedBlock()
        {
            IMyEntity entity;
            IMyCubeBlock ownerBlock;
            Vector3D hitPosition;
            Vector3D hitNormal;
            if (!TryGetPolygonDebugAimHit(out entity, out ownerBlock, out hitPosition, out hitNormal) || ownerBlock == null)
                return false;

            bool applied = ApplyManualLowLevelCalibrationToOwnerBlock(ownerBlock);
            TouchScreenApiAdapter input;
            if (TryFindStoredCalibrationInputForOwner(ownerBlock, out input))
            {
                TrySeedPolygonDebugFromStoredCalibration(input);
                return true;
            }

            return applied;
        }

        bool ApplyManualLowLevelCalibrationToOwnerBlock(IMyCubeBlock ownerBlock)
        {
            if (ownerBlock == null)
                return false;

            int applied = ResetManualLowLevelCalibrationAppsForOwner(_apps, ownerBlock) + ResetManualLowLevelCalibrationAppsForOwner(_surfaceScriptApps, ownerBlock);
            if (applied <= 0)
                return false;

            ClearPanelCursorDrawCache();
            NoteManualCalibrationStatus(ownerBlock, -1, "LOW LEVEL CALIBRATION");
            return true;
        }

        bool TryFindStoredCalibrationInputForOwner(IMyCubeBlock ownerBlock, out TouchScreenApiAdapter input)
        {
            input = null;
            if (ownerBlock == null)
                return false;

            if (TryFindStoredCalibrationInputForOwnerInApps(_apps, ownerBlock, out input))
                return true;

            if (TryFindStoredCalibrationInputForOwnerInApps(_surfaceScriptApps, ownerBlock, out input))
                return true;

            for (int i = _unsupportedSurfaceProbes.Count - 1; i >= 0; i--)
            {
                TouchScreenApiAdapter candidate = _unsupportedSurfaceProbes[i];
                if (candidate == null || candidate.OwnerBlock == null || candidate.OwnerBlock.MarkedForClose)
                {
                    _unsupportedSurfaceProbes.RemoveAt(i);
                    continue;
                }

                if (candidate.OwnerBlock.EntityId == ownerBlock.EntityId)
                {
                    if (!candidate.HasStoredPanelCursorSurface)
                        TryApplyStoredPanelCursorCalibration(candidate);

                    if (candidate.HasStoredPanelCursorSurface)
                    {
                        input = candidate;
                        return true;
                    }
                }
            }

            return false;
        }

        bool TryFindStoredCalibrationInputForOwnerInApps(List<GridSchematicsLcdApp> apps, IMyCubeBlock ownerBlock, out TouchScreenApiAdapter input)
        {
            input = null;
            if (apps == null || ownerBlock == null)
                return false;

            for (int i = apps.Count - 1; i >= 0; i--)
            {
                GridSchematicsLcdApp app = apps[i];
                if (app == null || !app.IsOwnerFunctional)
                {
                    apps.RemoveAt(i);
                    continue;
                }

                if (IsCalibrationAppForOwner(app, ownerBlock))
                {
                    if (app.TouchInput != null && !app.TouchInput.HasStoredPanelCursorSurface)
                        TryApplyStoredPanelCursorCalibration(app.TouchInput);

                    if (app.TouchInput != null && app.TouchInput.HasStoredPanelCursorSurface)
                    {
                        input = app.TouchInput;
                        return true;
                    }
                }
            }

            return false;
        }

        static int ResetManualLowLevelCalibrationAppsForOwner(List<GridSchematicsLcdApp> apps, IMyCubeBlock ownerBlock)
        {
            if (apps == null || ownerBlock == null)
                return 0;

            int applied = 0;
            for (int i = apps.Count - 1; i >= 0; i--)
            {
                GridSchematicsLcdApp app = apps[i];
                if (app == null || !app.IsOwnerFunctional)
                {
                    apps.RemoveAt(i);
                    continue;
                }

                if (IsCalibrationAppForOwner(app, ownerBlock))
                {
                    app.ResetLowLevelCalibrationForManual();
                    applied++;
                }
            }

            return applied;
        }

        static bool IsCalibrationAppForOwner(GridSchematicsLcdApp app, IMyCubeBlock ownerBlock)
        {
            if (app == null ||
                !app.IsOwnerFunctional ||
                app.OwnerBlock == null ||
                app.Config == null ||
                !app.Config.Enabled ||
                ownerBlock == null)
            {
                return false;
            }

            if (app.OwnerBlock.EntityId == ownerBlock.EntityId)
                return true;

            return false;
        }

        void ClearPolygonDebugAltAimSeed()
        {
            _hasPolygonDebugAltAimSeed = false;
            _polygonDebugAltAimInput = null;
            _polygonDebugAltAimLocalA = 0.0;
            _polygonDebugAltAimLocalB = 0.0;
            _hasPolygonDebugAltAimFrame = false;
            _polygonDebugAltAimAxisA = Vector3D.Zero;
            _polygonDebugAltAimAxisB = Vector3D.Zero;
            _polygonDebugAltAimMinA = 0.0;
            _polygonDebugAltAimMaxA = 0.0;
            _polygonDebugAltAimMinB = 0.0;
            _polygonDebugAltAimMaxB = 0.0;
        }

        public void RegisterManualCalibrationRenderedInput(TouchScreenApiAdapter input)
        {
            if (!_panelDiscoveryPolygonDebugEnabled || !_polygonDebugAlternateAimMode || input == null || input.OwnerBlock == null || input.OwnerBlock.MarkedForClose)
                return;

            for (int i = 0; i < _manualCalibrationRenderedInputs.Count; i++)
            {
                if (_manualCalibrationRenderedInputs[i] == input)
                    return;
            }

            _manualCalibrationRenderedInputs.Add(input);
        }

        void ClearPolygonDebugSurface()
        {
            _hasPolygonDebugSurface = false;
            _polygonDebugEntity = null;
            _polygonDebugSeed = Vector3D.Zero;
            _polygonDebugNormal = Vector3D.Zero;
            _polygonDebugAxisA = Vector3D.Zero;
            _polygonDebugAxisB = Vector3D.Zero;
            _polygonDebugRootBlockEntity = null;
            _polygonDebugOwnerBlock = null;
            _polygonDebugTopLeft = Vector3D.Zero;
            _polygonDebugTopRight = Vector3D.Zero;
            _polygonDebugBottomRight = Vector3D.Zero;
            _polygonDebugBottomLeft = Vector3D.Zero;
            _polygonDebugMinA = 0.0;
            _polygonDebugMaxA = 0.0;
            _polygonDebugMinB = 0.0;
            _polygonDebugMaxB = 0.0;
            _polygonDebugCalibrationScreenIndex = -1;
            _polygonDebugManualBoundsMode = false;
            _polygonDebugFallbackBoundsMode = false;
            _polygonDebugManualHalfA = 0.25;
            _polygonDebugManualHalfB = 0.15;
            ResetPolygonDebugCalibrationAdjustments();
            _polygonDebugOcclusionEntities.Clear();
            _polygonDebugNearbyEntities.Clear();
            _polygonDebugWorldOccluderTriangles.Clear();
            _polygonDebugTriangles.Clear();
            _polygonDebugOcclusionTriangles.Clear();
            _polygonDebugSubpartTrimTriangles.Clear();
            _polygonDebugSpatialOccluderTriangles.Clear();
            _polygonDebugConnectedTriangles.Clear();
            _polygonDebugTriangleQueue.Clear();
            _polygonDebugTriangleVisited.Clear();
        }

        void HandlePanelDiscoveryPolygonDebugSubCommand(string[] parts, string commandName)
        {
            string subCommand = parts[1].Trim();
            if (subCommand.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                ShowPolygonDebugMessage(commandName + " commands: click screen, manual, size <w> <h>, move <a> <b>, scale <a> <b>, depth <m>, tilt <aDeg> <bDeg>, screen <i>, reset, save");
                return;
            }

            if (subCommand.Equals("copy", StringComparison.OrdinalIgnoreCase))
            {
                CopyPolygonDebugCalibrationToClipboard();
                return;
            }

            if (subCommand.Equals("copyall", StringComparison.OrdinalIgnoreCase) ||
                subCommand.Equals("exportall", StringComparison.OrdinalIgnoreCase))
            {
                CopyWorldPanelCalibrationCatalogToClipboard();
                return;
            }

            if (subCommand.Equals("save", StringComparison.OrdinalIgnoreCase) ||
                subCommand.Equals("confirm", StringComparison.OrdinalIgnoreCase))
            {
                SavePolygonDebugCalibrationToCatalog();
                return;
            }

            if (subCommand.Equals("visuals", StringComparison.OrdinalIgnoreCase) ||
                subCommand.Equals("hide", StringComparison.OrdinalIgnoreCase))
            {
                TogglePolygonDebugVisuals();
                return;
            }

            if (subCommand.Equals("reset", StringComparison.OrdinalIgnoreCase))
            {
                ResetPolygonDebugCalibrationAdjustments();
                ShowPolygonDebugMessage("Polygon panel calibration adjustments reset");
                return;
            }

            if (subCommand.Equals("screen", StringComparison.OrdinalIgnoreCase))
            {
                int screenIndex;
                if (parts.Length < 3 || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out screenIndex))
                {
                    ShowPolygonDebugMessage("Usage: " + commandName + " screen <lcdSurfaceIndex>");
                    return;
                }

                _polygonDebugCalibrationScreenIndex = screenIndex;
                ShowPolygonDebugMessage("Polygon panel calibration screen index set to " + screenIndex.ToString(CultureInfo.InvariantCulture));
                return;
            }

            if (subCommand.Equals("depth", StringComparison.OrdinalIgnoreCase))
            {
                double depthOffset;
                if (parts.Length < 3 || !TryParsePolygonDebugDouble(parts[2], out depthOffset))
                {
                    ShowPolygonDebugMessage("Usage: " + commandName + " depth <normalOffsetMeters>");
                    return;
                }

                _polygonDebugCalibrationDepthOffset = depthOffset;
                ShowPolygonDebugMessage("Polygon panel cursor depth set to " + FormatPolygonDebugDouble(depthOffset));
                return;
            }

            if (subCommand.Equals("manual", StringComparison.OrdinalIgnoreCase))
            {
                if (parts.Length >= 3)
                    _polygonDebugManualBoundsMode = parts[2].Equals("on", StringComparison.OrdinalIgnoreCase) || parts[2].Equals("true", StringComparison.OrdinalIgnoreCase) || parts[2].Equals("1", StringComparison.OrdinalIgnoreCase);
                else
                    _polygonDebugManualBoundsMode = !_polygonDebugManualBoundsMode;

                if (_hasPolygonDebugSurface && _polygonDebugManualBoundsMode)
                    ApplyPolygonDebugManualBoundsToCurrentSurface();

                _polygonDebugFallbackBoundsMode = _polygonDebugManualBoundsMode;

                ShowPolygonDebugMessage("Polygon panel manual rectangle mode " + (_polygonDebugManualBoundsMode ? "ON" : "OFF"));
                return;
            }

            if (subCommand.Equals("size", StringComparison.OrdinalIgnoreCase))
            {
                double widthA;
                double heightB;
                if (parts.Length < 4 ||
                    !TryParsePolygonDebugDouble(parts[2], out widthA) ||
                    !TryParsePolygonDebugDouble(parts[3], out heightB) ||
                    widthA <= 0.001 ||
                    heightB <= 0.001)
                {
                    ShowPolygonDebugMessage("Usage: " + commandName + " size <widthA> <heightB>");
                    return;
                }

                _polygonDebugManualHalfA = widthA * 0.5;
                _polygonDebugManualHalfB = heightB * 0.5;
                if (_hasPolygonDebugSurface && _polygonDebugManualBoundsMode)
                    ApplyPolygonDebugManualBoundsToCurrentSurface();

                ShowPolygonDebugMessage("Polygon panel manual rectangle size set");
                return;
            }

            if (subCommand.Equals("move", StringComparison.OrdinalIgnoreCase) ||
                subCommand.Equals("nudge", StringComparison.OrdinalIgnoreCase))
            {
                double deltaA;
                double deltaB;
                if (parts.Length < 4 ||
                    !TryParsePolygonDebugDouble(parts[2], out deltaA) ||
                    !TryParsePolygonDebugDouble(parts[3], out deltaB))
                {
                    ShowPolygonDebugMessage("Usage: " + commandName + " move <deltaA> <deltaB>");
                    return;
                }

                _polygonDebugCalibrationOffsetA += deltaA;
                _polygonDebugCalibrationOffsetB += deltaB;
                ShowPolygonDebugMessage("Polygon panel bounds moved");
                return;
            }

            if (subCommand.Equals("scale", StringComparison.OrdinalIgnoreCase))
            {
                double scaleA;
                double scaleB;
                if (parts.Length < 4 ||
                    !TryParsePolygonDebugDouble(parts[2], out scaleA) ||
                    !TryParsePolygonDebugDouble(parts[3], out scaleB))
                {
                    ShowPolygonDebugMessage("Usage: " + commandName + " scale <scaleA> <scaleB>");
                    return;
                }

                _polygonDebugCalibrationScaleA *= scaleA;
                _polygonDebugCalibrationScaleB *= scaleB;
                ClampPolygonDebugCalibrationScales();
                ShowPolygonDebugMessage("Polygon panel bounds scaled");
                return;
            }

            if (subCommand.Equals("adjust", StringComparison.OrdinalIgnoreCase))
            {
                if (parts.Length < 6 ||
                    !TryParsePolygonDebugDouble(parts[2], out _polygonDebugCalibrationOffsetA) ||
                    !TryParsePolygonDebugDouble(parts[3], out _polygonDebugCalibrationOffsetB) ||
                    !TryParsePolygonDebugDouble(parts[4], out _polygonDebugCalibrationScaleA) ||
                    !TryParsePolygonDebugDouble(parts[5], out _polygonDebugCalibrationScaleB))
                {
                    ShowPolygonDebugMessage("Usage: " + commandName + " adjust <offsetA> <offsetB> <scaleA> <scaleB>");
                    return;
                }

                ClampPolygonDebugCalibrationScales();
                if (parts.Length >= 7)
                    TryParsePolygonDebugDouble(parts[6], out _polygonDebugCalibrationDepthOffset);
                ShowPolygonDebugMessage("Polygon panel calibration adjustments applied");
                return;
            }

            if (subCommand.Equals("tilt", StringComparison.OrdinalIgnoreCase) ||
                subCommand.Equals("plane", StringComparison.OrdinalIgnoreCase))
            {
                double tiltA;
                double tiltB;
                if (!_polygonDebugAlternateAimMode)
                {
                    ShowPolygonDebugMessage("Plane tilt is only available in /DISPLAYCALALT");
                    return;
                }

                if (parts.Length < 4 ||
                    !TryParsePolygonDebugDouble(parts[2], out tiltA) ||
                    !TryParsePolygonDebugDouble(parts[3], out tiltB))
                {
                    ShowPolygonDebugMessage("Usage: " + commandName + " tilt <aroundA-deg> <aroundB-deg>");
                    return;
                }

                TiltPolygonDebugPlane(tiltA * Math.PI / 180.0, tiltB * Math.PI / 180.0);
                ShowPolygonDebugMessage("ALT panel plane tilted");
                return;
            }

            ShowPolygonDebugMessage(commandName + " commands: help, save, visuals, screen, depth, tilt, manual, size, move, scale, reset, adjust");
        }

        void DrawPanelDiscoveryPolygonDebug()
        {
            if (!_panelDiscoveryPolygonDebugEnabled)
                return;

            UpdatePolygonDebugHotkeys();
            UpdatePolygonDebugCapture();
            UpdatePolygonDebugFocusSelection();
            UpdatePolygonDebugScrollAdjustments();
            DrawPolygonDebugSurface();
        }

        void UpdatePolygonDebugHotkeys()
        {
            if (MyAPIGateway.Input == null || MyAPIGateway.Gui == null)
                return;

            bool allowHotkeys = false;
            try
            {
                allowHotkeys = !MyAPIGateway.Gui.IsCursorVisible;
            }
            catch
            {
                allowHotkeys = false;
            }

            bool visualPressed = allowHotkeys && IsPolygonDebugKeyPressed(VRage.Input.MyKeys.H);
            bool copyPressed = allowHotkeys && IsPolygonDebugKeyPressed(VRage.Input.MyKeys.K);
            bool indexDecreasePressed = allowHotkeys && IsPolygonDebugKeyPressed(VRage.Input.MyKeys.OemOpenBrackets);
            bool indexIncreasePressed = allowHotkeys && IsPolygonDebugKeyPressed(VRage.Input.MyKeys.OemCloseBrackets);

            if (visualPressed && !_polygonDebugVisualToggleWasPressed)
                TogglePolygonDebugVisuals();

            if (copyPressed && !_polygonDebugCalibrationCopyWasPressed)
                CopyPolygonDebugCalibrationToClipboard();

            if (indexDecreasePressed && !_polygonDebugIndexDecreaseWasPressed)
                CyclePolygonDebugScreenIndex(-1);

            if (indexIncreasePressed && !_polygonDebugIndexIncreaseWasPressed)
                CyclePolygonDebugScreenIndex(1);

            _polygonDebugVisualToggleWasPressed = visualPressed;
            _polygonDebugCalibrationCopyWasPressed = copyPressed;
            _polygonDebugIndexDecreaseWasPressed = indexDecreasePressed;
            _polygonDebugIndexIncreaseWasPressed = indexIncreasePressed;
        }

        void CyclePolygonDebugScreenIndex(int direction)
        {
            if (!_hasPolygonDebugSurface || _polygonDebugOwnerBlock == null || direction == 0)
            {
                ShowPolygonDebugMessage("Target a screen before cycling calibration index");
                return;
            }

            int surfaceCount = GetBlockSurfaceCount(_polygonDebugOwnerBlock);
            if (surfaceCount <= 0)
            {
                ShowPolygonDebugMessage("No LCD surfaces found on this block");
                return;
            }

            int current = _polygonDebugCalibrationScreenIndex;
            if (current < 0 || current >= surfaceCount)
                current = 0;

            int next = (current + direction) % surfaceCount;
            if (next < 0)
                next += surfaceCount;

            _polygonDebugCalibrationScreenIndex = next;
            NoteManualCalibrationStatus(_polygonDebugOwnerBlock, _polygonDebugCalibrationScreenIndex, "INDEX " + next.ToString(CultureInfo.InvariantCulture) + " SELECTED");
            ShowPolygonDebugMessage("Manual calibration screen index " + next.ToString(CultureInfo.InvariantCulture) + " selected");
        }

        bool IsPolygonDebugKeyPressed(VRage.Input.MyKeys key)
        {
            try
            {
                return MyAPIGateway.Input != null && MyAPIGateway.Input.IsKeyPress(key);
            }
            catch
            {
                return false;
            }
        }

        void UpdateManualCalibrationInputFocusGate()
        {
            long now = DateTime.UtcNow.Ticks;
            bool hadInputGap = _manualCalibrationInputLastDrawTicks != 0 &&
                now - _manualCalibrationInputLastDrawTicks > ManualCalibrationInputFocusGapTicks;
            _manualCalibrationInputLastDrawTicks = now;

            bool cursorVisible = IsGameGuiCursorVisible();
            bool mouseDown = IsCurrentLeftMousePressed() || IsCurrentRightMousePressed() || IsCurrentMiddleMousePressed();

            if (hadInputGap || cursorVisible)
            {
                _manualCalibrationMouseInputArmed = false;
                _polygonDebugCaptureWasPressed = mouseDown;
                _polygonDebugFocusWasPressed = mouseDown;
                _polygonDebugResetWasPressed = mouseDown;
                _calibratedDebugExportWasPressed = mouseDown;
                _panelCursorDepthOffsetCaptureWasPressed = mouseDown;
            }

            if (!_manualCalibrationMouseInputArmed && !mouseDown && !cursorVisible)
            {
                _manualCalibrationMouseInputArmed = true;
                _polygonDebugCaptureWasPressed = false;
                _polygonDebugFocusWasPressed = false;
                _polygonDebugResetWasPressed = false;
                _calibratedDebugExportWasPressed = false;
                _panelCursorDepthOffsetCaptureWasPressed = false;
            }
        }

        bool IsManualCalibrationMouseInputArmed()
        {
            return _manualCalibrationMouseInputArmed && !IsGameGuiCursorVisible();
        }

        static bool IsCurrentMiddleMousePressed()
        {
            try
            {
                return !IsGameGuiCursorVisible() && MyAPIGateway.Input != null && MyAPIGateway.Input.IsMiddleMousePressed();
            }
            catch
            {
                return false;
            }
        }

        void TogglePolygonDebugVisuals()
        {
            _polygonDebugVisualsVisible = !_polygonDebugVisualsVisible;
            NoteManualCalibrationStatus(_polygonDebugOwnerBlock, _polygonDebugCalibrationScreenIndex, _polygonDebugVisualsVisible ? "DEBUG VISUALS ON" : "DEBUG VISUALS OFF");
            ShowPolygonDebugMessage("Polygon panel debug visuals " + (_polygonDebugVisualsVisible ? "ON" : "OFF") + " - cursor remains visible");
        }

        void UpdatePolygonDebugCapture()
        {
            if (MyAPIGateway.Input == null || MyAPIGateway.Gui == null)
                return;

            if (!IsManualCalibrationMouseInputArmed())
                return;

            bool pressed = false;
            try
            {
                pressed = !MyAPIGateway.Gui.IsCursorVisible && MyAPIGateway.Input.IsLeftMousePressed();
            }
            catch
            {
                pressed = false;
            }

            bool shift = IsCurrentShiftPressed();
            bool control = IsCurrentControlPressed();
            bool alt = IsCurrentAltPressed();

            if (pressed && !_polygonDebugCaptureWasPressed)
            {
                if (shift && control)
                {
                    bool canProceed = true;
                    if (_hasPolygonDebugSurface)
                        canProceed = SavePolygonDebugCalibrationToCatalog();

                    if (canProceed)
                    {
                        ProceedAimedPanelToCursorCalibration();
                        if (_hasPolygonDebugSurface)
                            ClearPolygonDebugSurface();
                    }
                }
                else if (shift)
                {
                    SavePolygonDebugCalibrationToCatalog();
                }
                else if (control)
                {
                    if (alt)
                        CopyWorldPanelCalibrationCatalogToClipboard();
                    else
                        CopyPolygonDebugCalibrationToClipboard();
                }
                else
                {
                    RetargetPolygonDebugSurface(true);
                }
            }

            _polygonDebugCaptureWasPressed = pressed;
        }

        void UpdatePolygonDebugFocusSelection()
        {
            if (MyAPIGateway.Input == null || MyAPIGateway.Gui == null)
                return;

            if (!IsManualCalibrationMouseInputArmed())
                return;

            bool pressed = IsCurrentMiddleMousePressed();
            if (pressed && !_polygonDebugFocusWasPressed)
                FocusPolygonDebugAimedSurface();

            _polygonDebugFocusWasPressed = pressed;
        }

        bool RetargetPolygonDebugSurface(bool forceFreshDiscovery)
        {
            StoreCurrentPolygonDebugDraft();
            _polygonDebugManualBoundsMode = false;
            _polygonDebugFallbackBoundsMode = false;
            _polygonDebugManualHalfA = 0.25;
            _polygonDebugManualHalfB = 0.15;
            ResetPolygonDebugCalibrationAdjustments();
            _hasPolygonDebugSurface = TryBuildPolygonDebugSurface();
            if (_hasPolygonDebugSurface)
            {
                NoteManualCalibrationStatus(_polygonDebugOwnerBlock, _polygonDebugCalibrationScreenIndex, forceFreshDiscovery ? "CALIBRATION IN PROGRESS" : "SCREEN FOCUSED");
                if (_polygonDebugAlternateAimMode)
                    ShowPolygonDebugMessage(BuildPolygonDebugAltSelectionMessage("ALT calibration target"));
                return true;
            }

            ShowPolygonDebugMessage("No screen calibration target under aim");
            return false;
        }

        string BuildPolygonDebugAltSelectionMessage(string prefix)
        {
            string blockId = _polygonDebugOwnerBlock == null ? "no block" : GetBlockDefinitionId(_polygonDebugOwnerBlock);
            string screen = _polygonDebugCalibrationScreenIndex >= 0
                ? _polygonDebugCalibrationScreenIndex.ToString(CultureInfo.InvariantCulture)
                : "?";
            return prefix + " - " + blockId + " screen " + screen;
        }

        void FocusPolygonDebugAimedSurface()
        {
            StoreCurrentPolygonDebugDraft();

            TouchScreenApiAdapter input;
            if (TryFindAimedCalibrationInput(null, out input) && input != null)
            {
                IMyCubeBlock ownerBlock = input.OwnerBlock;
                int selectedIndex = _polygonDebugCalibrationScreenIndex >= 0 ? _polygonDebugCalibrationScreenIndex : 0;
                int surfaceCount = GetBlockSurfaceCount(ownerBlock);
                if (surfaceCount > 0 && selectedIndex >= surfaceCount)
                    selectedIndex = 0;

                if (TryRestorePolygonDebugDraft(ownerBlock, selectedIndex))
                {
                    NoteManualCalibrationStatus(ownerBlock, selectedIndex, "DRAFT FOCUSED");
                    ShowPolygonDebugMessage("Screen calibration draft focused");
                    return;
                }

                TouchScreenApiAdapter selectedInput;
                GridSchematicsLcdApp selectedApp;
                if ((TryFindCalibrationInputForOwner(_apps, ownerBlock, selectedIndex, out selectedInput, out selectedApp) ||
                    TryFindCalibrationInputForOwner(_surfaceScriptApps, ownerBlock, selectedIndex, out selectedInput, out selectedApp) ||
                    TryFindCalibrationProbeForOwner(ownerBlock, selectedIndex, out selectedInput)) &&
                    selectedInput != null)
                {
                    if (!selectedInput.HasStoredPanelCursorSurface)
                        TryApplyStoredPanelCursorCalibration(selectedInput);

                    if (selectedInput.HasStoredPanelCursorSurface && TrySeedPolygonDebugFromStoredCalibration(selectedInput))
                    {
                        NoteManualCalibrationStatus(ownerBlock, selectedIndex, "SAVED CALIBRATION FOCUSED");
                        StoreCurrentPolygonDebugDraft();
                        ShowPolygonDebugMessage("Saved screen calibration focused");
                        return;
                    }
                }
            }

            RetargetPolygonDebugSurface(false);
        }

        static string GetPolygonDebugDraftKey(IMyCubeBlock block, int surfaceIndex)
        {
            if (block == null || surfaceIndex < 0)
                return string.Empty;

            return block.EntityId.ToString(CultureInfo.InvariantCulture) + ":" + surfaceIndex.ToString(CultureInfo.InvariantCulture);
        }

        void ClampPolygonDebugSelectedScreenIndexToOwner()
        {
            int surfaceCount = GetBlockSurfaceCount(_polygonDebugOwnerBlock);
            if (surfaceCount <= 0)
            {
                if (_polygonDebugCalibrationScreenIndex < 0)
                    _polygonDebugCalibrationScreenIndex = 0;
                return;
            }

            if (_polygonDebugCalibrationScreenIndex < 0 || _polygonDebugCalibrationScreenIndex >= surfaceCount)
                _polygonDebugCalibrationScreenIndex = 0;
        }

        void StoreCurrentPolygonDebugDraft()
        {
            if (!_hasPolygonDebugSurface || _polygonDebugOwnerBlock == null || _polygonDebugCalibrationScreenIndex < 0)
                return;

            IMyEntity ownerEntity = _polygonDebugOwnerBlock as IMyEntity;
            if (ownerEntity == null)
                return;

            string key = GetPolygonDebugDraftKey(_polygonDebugOwnerBlock, _polygonDebugCalibrationScreenIndex);
            if (string.IsNullOrEmpty(key))
                return;

            MatrixD inverseBasis = MatrixD.Invert(ownerEntity.WorldMatrix);
            MatrixD inverseBasisNormal = MatrixD.Transpose(ownerEntity.WorldMatrix);
            _polygonDebugCalibrationDrafts[key] = new PolygonDebugCalibrationDraft
            {
                BlockEntityId = _polygonDebugOwnerBlock.EntityId,
                SurfaceIndex = _polygonDebugCalibrationScreenIndex,
                SeedLocal = Vector3D.Transform(_polygonDebugSeed, inverseBasis),
                NormalLocal = Vector3D.TransformNormal(_polygonDebugNormal, inverseBasisNormal),
                AxisALocal = Vector3D.TransformNormal(_polygonDebugAxisA, inverseBasisNormal),
                AxisBLocal = Vector3D.TransformNormal(_polygonDebugAxisB, inverseBasisNormal),
                MinA = _polygonDebugMinA,
                MaxA = _polygonDebugMaxA,
                MinB = _polygonDebugMinB,
                MaxB = _polygonDebugMaxB,
                OffsetA = _polygonDebugCalibrationOffsetA,
                OffsetB = _polygonDebugCalibrationOffsetB,
                ScaleA = _polygonDebugCalibrationScaleA,
                ScaleB = _polygonDebugCalibrationScaleB,
                DepthOffset = _polygonDebugCalibrationDepthOffset,
                ManualBoundsMode = _polygonDebugManualBoundsMode,
                FallbackBoundsMode = _polygonDebugFallbackBoundsMode,
                ManualHalfA = _polygonDebugManualHalfA,
                ManualHalfB = _polygonDebugManualHalfB
            };
        }

        bool TryRestorePolygonDebugDraft(TouchScreenApiAdapter input)
        {
            if (input == null || input.OwnerBlock == null)
                return false;

            return TryRestorePolygonDebugDraft(input.OwnerBlock, input.GetSurfaceIndex());
        }

        bool TryRestorePolygonDebugDraft(IMyCubeBlock ownerBlock, int surfaceIndex)
        {
            if (ownerBlock == null || surfaceIndex < 0)
                return false;

            string key = GetPolygonDebugDraftKey(ownerBlock, surfaceIndex);
            PolygonDebugCalibrationDraft draft;
            if (string.IsNullOrEmpty(key) || !_polygonDebugCalibrationDrafts.TryGetValue(key, out draft))
                return false;

            IMyEntity ownerEntity = ownerBlock as IMyEntity;
            if (ownerEntity == null)
                return false;

            Vector3D seed = Vector3D.Transform(draft.SeedLocal, ownerEntity.WorldMatrix);
            Vector3D normal = Vector3D.TransformNormal(draft.NormalLocal, ownerEntity.WorldMatrix);
            Vector3D axisA = Vector3D.TransformNormal(draft.AxisALocal, ownerEntity.WorldMatrix);
            Vector3D axisB = Vector3D.TransformNormal(draft.AxisBLocal, ownerEntity.WorldMatrix);
            if (normal.LengthSquared() <= 0.000001 || axisA.LengthSquared() <= 0.000001 || axisB.LengthSquared() <= 0.000001)
                return false;

            normal.Normalize();
            axisA.Normalize();
            axisB.Normalize();

            ClearPolygonDebugSurface();
            _polygonDebugEntity = ownerEntity;
            _polygonDebugRootBlockEntity = ownerEntity;
            _polygonDebugOwnerBlock = ownerBlock;
            _polygonDebugSeed = seed;
            _polygonDebugNormal = normal;
            _polygonDebugAxisA = axisA;
            _polygonDebugAxisB = axisB;
            _polygonDebugMinA = draft.MinA;
            _polygonDebugMaxA = draft.MaxA;
            _polygonDebugMinB = draft.MinB;
            _polygonDebugMaxB = draft.MaxB;
            _polygonDebugCalibrationOffsetA = draft.OffsetA;
            _polygonDebugCalibrationOffsetB = draft.OffsetB;
            _polygonDebugCalibrationScaleA = draft.ScaleA;
            _polygonDebugCalibrationScaleB = draft.ScaleB;
            _polygonDebugCalibrationDepthOffset = draft.DepthOffset;
            _polygonDebugManualBoundsMode = draft.ManualBoundsMode;
            _polygonDebugFallbackBoundsMode = draft.FallbackBoundsMode;
            _polygonDebugManualHalfA = draft.ManualHalfA;
            _polygonDebugManualHalfB = draft.ManualHalfB;
            _polygonDebugCalibrationScreenIndex = surfaceIndex;
            SetPolygonDebugLocalBounds(_polygonDebugMinA, _polygonDebugMaxA, _polygonDebugMinB, _polygonDebugMaxB);
            _hasPolygonDebugSurface = true;
            return true;
        }

        void ProceedAimedPanelToCursorCalibration()
        {
            GridSchematicsLcdApp app;
            if (TryFindAimedStoredCalibrationApp(out app))
            {
                app.ProceedToCursorCalibrationAfterLowLevel();
                NoteManualCalibrationStatus(app.TouchInput == null ? null : app.TouchInput.OwnerBlock, app.TouchInput == null ? -1 : app.TouchInput.GetSurfaceIndex(), "PROCEEDING TO CURSOR CALIBRATION");
                ShowPolygonDebugMessage("Proceeding to cursor calibration");
            }
            else if (TryProceedPolygonDebugOwnerToCursorCalibration())
            {
                ShowPolygonDebugMessage("Proceeding to cursor calibration");
            }
            else
            {
                ShowPolygonDebugMessage("No calibrated screen under aim");
            }
        }

        bool TryProceedPolygonDebugOwnerToCursorCalibration()
        {
            if (_polygonDebugOwnerBlock == null || _polygonDebugCalibrationScreenIndex < 0)
                return false;

            GridSchematicsLcdApp app;
            if (TryFindCalibrationAppForOwner(_apps, out app) || TryFindCalibrationAppForOwner(_surfaceScriptApps, out app))
            {
                app.ProceedToCursorCalibrationAfterLowLevel();
                NoteManualCalibrationStatus(_polygonDebugOwnerBlock, _polygonDebugCalibrationScreenIndex, "PROCEEDING TO CURSOR CALIBRATION");
                return true;
            }

            return false;
        }

        bool TryFindCalibrationAppForOwner(List<GridSchematicsLcdApp> apps, out GridSchematicsLcdApp foundApp)
        {
            foundApp = null;
            if (apps == null)
                return false;

            for (int i = apps.Count - 1; i >= 0; i--)
            {
                GridSchematicsLcdApp app = apps[i];
                if (app == null || !app.IsOwnerFunctional)
                {
                    apps.RemoveAt(i);
                    continue;
                }

                if (DoesPolygonDebugInputMatchOwner(app.TouchInput) && app.HasStoredLowLevelCalibration)
                {
                    foundApp = app;
                    return true;
                }
            }

            return false;
        }

        void UpdatePolygonDebugGlobalReset()
        {
            if (!IsManualCalibrationMouseInputArmed())
                return;

            bool pressed = IsCurrentRightMousePressed();
            if (pressed && !_polygonDebugResetWasPressed)
            {
                bool control = IsCurrentControlPressed();
                bool shift = IsCurrentShiftPressed();
                bool alt = IsCurrentAltPressed();
                bool handled = false;
                if (_panelDiscoveryPolygonDebugEnabled)
                    handled = control && shift && alt ? ResetAimedPanelLowLevelCalibration() : control ? DeleteAimedPanelLowLevelCalibration() : ResetAimedPanelLowLevelCalibration();

                if (handled)
                {
                }
            }

            _polygonDebugResetWasPressed = pressed;
        }

        bool RecalibrateAimedPanelCursor()
        {
            TouchScreenApiAdapter input;
            GridSchematicsLcdApp app;
            if (!TryFindResetTargetInput(out input, out app) || app == null)
                return false;

            app.StartCursorRecalibration();
            if (input != null)
                NoteManualCalibrationStatus(input.OwnerBlock, input.GetSurfaceIndex(), "CURSOR CALIBRATION RESET");
            ShowPolygonDebugMessage("Cursor calibration restarted");
            return true;
        }

        bool ResetAimedPanelLowLevelCalibration()
        {
            TouchScreenApiAdapter input;
            GridSchematicsLcdApp app;
            if (!TryFindResetTargetInput(out input, out app))
                return false;

            if (input != null && !input.HasStoredPanelCursorSurface)
                TryApplyStoredPanelCursorCalibration(input);

            if (input != null && input.HasStoredPanelCursorSurface && TrySeedPolygonDebugFromStoredCalibration(input))
            {
                if (app != null)
                    app.NoteManualLowLevelCalibrationApplied();

                NoteManualCalibrationStatus(input.OwnerBlock, input.GetSurfaceIndex(), "CALIBRATION RELOADED");
                ShowPolygonDebugMessage("Screen calibration reloaded from saved surface");
                return true;
            }

            if (app != null)
                app.ResetLowLevelCalibrationForManual();
            else if (input != null)
                input.ResetPanelCursorSurfaceCalibration();

            if (_panelDiscoveryPolygonDebugEnabled)
                ClearPolygonDebugSurface();

            if (input != null)
                NoteManualCalibrationStatus(input.OwnerBlock, input.GetSurfaceIndex(), "CALIBRATION RESET");
            ShowPolygonDebugMessage("Screen calibration reset");
            return true;
        }

        bool DeleteAimedPanelLowLevelCalibration()
        {
            TouchScreenApiAdapter input;
            GridSchematicsLcdApp app;
            if (!TryFindResetTargetInput(out input, out app))
                return false;

            if (input != null)
                RemoveStoredPanelCursorCalibration(input);

            if (app != null)
                app.ResetLowLevelCalibrationForManual();
            else if (input != null)
                input.ResetPanelCursorSurfaceCalibration();

            if (_panelDiscoveryPolygonDebugEnabled)
                ClearPolygonDebugSurface();

            if (input != null)
            {
                string draftKey = GetPolygonDebugDraftKey(input.OwnerBlock, input.GetSurfaceIndex());
                if (!string.IsNullOrEmpty(draftKey))
                    _polygonDebugCalibrationDrafts.Remove(draftKey);
                NoteManualCalibrationStatus(input.OwnerBlock, input.GetSurfaceIndex(), "CALIBRATION DELETED");
                TryWritePanelCalibrationLocalStorage(PanelCalibrationBlockExportFileName, BuildBlockPanelCalibrationExportText(input.OwnerBlock, -1, null));
            }
            ShowPolygonDebugMessage("Saved screen calibration deleted");
            return true;
        }

        bool TryFindResetTargetInput(out TouchScreenApiAdapter input, out GridSchematicsLcdApp app)
        {
            input = null;
            app = null;

            if (_hasPolygonDebugSurface && _polygonDebugOwnerBlock != null && _polygonDebugCalibrationScreenIndex >= 0)
            {
                if (TryFindCalibrationInputForOwner(_apps, _polygonDebugOwnerBlock, _polygonDebugCalibrationScreenIndex, out input, out app) ||
                    TryFindCalibrationInputForOwner(_surfaceScriptApps, _polygonDebugOwnerBlock, _polygonDebugCalibrationScreenIndex, out input, out app) ||
                    TryFindCalibrationProbeForOwner(_polygonDebugOwnerBlock, _polygonDebugCalibrationScreenIndex, out input))
                {
                    return true;
                }
            }

            GridSchematicsLcdApp aimedApp;
            if (TryFindAimedStoredCalibrationApp(out aimedApp))
            {
                app = aimedApp;
                input = aimedApp.TouchInput;
                return true;
            }

            TouchScreenApiAdapter aimedInput;
            if (TryFindAimedCalibrationInput(null, out aimedInput))
            {
                input = aimedInput;
                app = FindAppForInput(input);
                return true;
            }

            return false;
        }

        bool TryFindCalibrationInputForOwner(List<GridSchematicsLcdApp> apps, IMyCubeBlock ownerBlock, int surfaceIndex, out TouchScreenApiAdapter input, out GridSchematicsLcdApp foundApp)
        {
            input = null;
            foundApp = null;
            if (apps == null || ownerBlock == null || surfaceIndex < 0)
                return false;

            for (int i = apps.Count - 1; i >= 0; i--)
            {
                GridSchematicsLcdApp candidate = apps[i];
                if (candidate == null || !candidate.IsOwnerFunctional)
                {
                    apps.RemoveAt(i);
                    continue;
                }

                TouchScreenApiAdapter candidateInput = candidate.TouchInput;
                if (candidateInput != null &&
                    candidateInput.OwnerBlock != null &&
                    candidateInput.OwnerBlock.EntityId == ownerBlock.EntityId &&
                    candidateInput.GetSurfaceIndex() == surfaceIndex)
                {
                    input = candidateInput;
                    foundApp = candidate;
                    return true;
                }
            }

            return false;
        }

        bool TryFindCalibrationProbeForOwner(IMyCubeBlock ownerBlock, int surfaceIndex, out TouchScreenApiAdapter input)
        {
            input = null;
            if (ownerBlock == null || surfaceIndex < 0)
                return false;

            for (int i = _unsupportedSurfaceProbes.Count - 1; i >= 0; i--)
            {
                TouchScreenApiAdapter candidate = _unsupportedSurfaceProbes[i];
                if (candidate == null || candidate.OwnerBlock == null || candidate.OwnerBlock.MarkedForClose)
                {
                    _unsupportedSurfaceProbes.RemoveAt(i);
                    continue;
                }

                if (candidate.OwnerBlock.EntityId == ownerBlock.EntityId && candidate.GetSurfaceIndex() == surfaceIndex)
                {
                    input = candidate;
                    return true;
                }
            }

            return false;
        }

        GridSchematicsLcdApp FindAppForInput(TouchScreenApiAdapter input)
        {
            GridSchematicsLcdApp app;
            if (TryFindAppForInput(_apps, input, out app) || TryFindAppForInput(_surfaceScriptApps, input, out app))
                return app;

            return null;
        }

        static bool TryFindAppForInput(List<GridSchematicsLcdApp> apps, TouchScreenApiAdapter input, out GridSchematicsLcdApp foundApp)
        {
            foundApp = null;
            if (apps == null || input == null)
                return false;

            for (int i = 0; i < apps.Count; i++)
            {
                GridSchematicsLcdApp app = apps[i];
                if (app != null && object.ReferenceEquals(app.TouchInput, input))
                {
                    foundApp = app;
                    return true;
                }
            }

            return false;
        }

        bool ResetAimedPanelLowLevelCalibrationOld()
        {
            for (int i = _apps.Count - 1; i >= 0; i--)
            {
                GridSchematicsLcdApp app = _apps[i];
                if (app == null || !app.IsOwnerFunctional)
                {
                    _apps.RemoveAt(i);
                    continue;
                }

                if (DoesCurrentAimHitInput(app.TouchInput))
                {
                    RemoveStoredPanelCursorCalibration(app.TouchInput);
                    NoteManualCalibrationStatus(app.TouchInput == null ? null : app.TouchInput.OwnerBlock, app.TouchInput == null ? -1 : app.TouchInput.GetSurfaceIndex(), "CALIBRATION RESET");
                    app.ResetLowLevelCalibrationForManual();
                    if (_panelDiscoveryPolygonDebugEnabled)
                        ClearPolygonDebugSurface();
                    return true;
                }
            }

            for (int i = _surfaceScriptApps.Count - 1; i >= 0; i--)
            {
                GridSchematicsLcdApp app = _surfaceScriptApps[i];
                if (app == null || !app.IsOwnerFunctional)
                {
                    _surfaceScriptApps.RemoveAt(i);
                    continue;
                }

                if (DoesCurrentAimHitInput(app.TouchInput))
                {
                    RemoveStoredPanelCursorCalibration(app.TouchInput);
                    NoteManualCalibrationStatus(app.TouchInput == null ? null : app.TouchInput.OwnerBlock, app.TouchInput == null ? -1 : app.TouchInput.GetSurfaceIndex(), "CALIBRATION RESET");
                    app.ResetLowLevelCalibrationForManual();
                    if (_panelDiscoveryPolygonDebugEnabled)
                        ClearPolygonDebugSurface();
                    return true;
                }
            }

            for (int i = _unsupportedSurfaceProbes.Count - 1; i >= 0; i--)
            {
                TouchScreenApiAdapter input = _unsupportedSurfaceProbes[i];
                if (input == null || input.OwnerBlock == null || input.OwnerBlock.MarkedForClose)
                {
                    _unsupportedSurfaceProbes.RemoveAt(i);
                    continue;
                }

                if (DoesCurrentAimHitInput(input))
                {
                    RemoveStoredPanelCursorCalibration(input);
                    NoteManualCalibrationStatus(input.OwnerBlock, input.GetSurfaceIndex(), "CALIBRATION RESET");
                    input.ResetPanelCursorSurfaceCalibration();
                    if (_panelDiscoveryPolygonDebugEnabled)
                        ClearPolygonDebugSurface();
                    return true;
                }
            }

            return false;
        }

        bool TrySeedPolygonDebugFromAimedStoredCalibration()
        {
            TouchScreenApiAdapter input;
            if (!TryFindAimedStoredCalibrationInput(out input))
                return false;

            return TrySeedPolygonDebugFromStoredCalibration(input);
        }

        bool TryFindAimedStoredCalibrationInput(out TouchScreenApiAdapter aimedInput)
        {
            aimedInput = null;

            GridSchematicsLcdApp aimedApp;
            PanelCursorWorldDrawData cursor;
            if (TryFindPanelSurfaceCursorApp(null, out aimedApp, out cursor) &&
                aimedApp != null &&
                aimedApp.TouchInput != null &&
                aimedApp.TouchInput.HasStoredPanelCursorSurface)
            {
                aimedInput = aimedApp.TouchInput;
                return true;
            }

            for (int i = _apps.Count - 1; i >= 0; i--)
            {
                GridSchematicsLcdApp app = _apps[i];
                if (app == null || !app.IsOwnerFunctional)
                {
                    _apps.RemoveAt(i);
                    continue;
                }

                if (IsStoredCalibrationAimedInput(app.TouchInput))
                {
                    aimedInput = app.TouchInput;
                    return true;
                }
            }

            for (int i = _surfaceScriptApps.Count - 1; i >= 0; i--)
            {
                GridSchematicsLcdApp app = _surfaceScriptApps[i];
                if (app == null || !app.IsOwnerFunctional)
                {
                    _surfaceScriptApps.RemoveAt(i);
                    continue;
                }

                if (IsStoredCalibrationAimedInput(app.TouchInput))
                {
                    aimedInput = app.TouchInput;
                    return true;
                }
            }

            for (int i = _unsupportedSurfaceProbes.Count - 1; i >= 0; i--)
            {
                TouchScreenApiAdapter input = _unsupportedSurfaceProbes[i];
                if (input == null || input.OwnerBlock == null || input.OwnerBlock.MarkedForClose)
                {
                    _unsupportedSurfaceProbes.RemoveAt(i);
                    continue;
                }

                if (IsStoredCalibrationAimedInput(input))
                {
                    aimedInput = input;
                    return true;
                }
            }

            return false;
        }

        bool TryFindAimedCalibrationInput(IMyCubeBlock ownerBlock, out TouchScreenApiAdapter aimedInput)
        {
            aimedInput = null;

            GridSchematicsLcdApp aimedApp;
            PanelCursorWorldDrawData cursor;
            if (TryFindPanelSurfaceCursorApp(ownerBlock, out aimedApp, out cursor) &&
                aimedApp != null &&
                aimedApp.TouchInput != null)
            {
                aimedInput = aimedApp.TouchInput;
                return true;
            }

            if (TryFindAimedCalibrationInputInApps(_apps, ownerBlock, out aimedInput))
                return true;

            if (TryFindAimedCalibrationInputInApps(_surfaceScriptApps, ownerBlock, out aimedInput))
                return true;

            for (int i = _unsupportedSurfaceProbes.Count - 1; i >= 0; i--)
            {
                TouchScreenApiAdapter input = _unsupportedSurfaceProbes[i];
                if (input == null || input.OwnerBlock == null || input.OwnerBlock.MarkedForClose)
                {
                    _unsupportedSurfaceProbes.RemoveAt(i);
                    continue;
                }

                if (DoesCalibrationInputMatchOwner(input, ownerBlock) && DoesCurrentAimHitInput(input))
                {
                    aimedInput = input;
                    return true;
                }
            }

            return false;
        }

        bool TryFindAimedCalibrationInputInApps(List<GridSchematicsLcdApp> apps, IMyCubeBlock ownerBlock, out TouchScreenApiAdapter aimedInput)
        {
            aimedInput = null;
            if (apps == null)
                return false;

            for (int i = apps.Count - 1; i >= 0; i--)
            {
                GridSchematicsLcdApp app = apps[i];
                if (app == null || !app.IsOwnerFunctional)
                {
                    apps.RemoveAt(i);
                    continue;
                }

                if (DoesCalibrationInputMatchOwner(app.TouchInput, ownerBlock) && DoesCurrentAimHitInput(app.TouchInput))
                {
                    aimedInput = app.TouchInput;
                    return true;
                }
            }

            return false;
        }

        static bool DoesCalibrationInputMatchOwner(TouchScreenApiAdapter input, IMyCubeBlock ownerBlock)
        {
            if (input == null || input.OwnerBlock == null)
                return false;

            if (ownerBlock == null)
                return true;

            return input.OwnerBlock.EntityId == ownerBlock.EntityId;
        }

        static bool IsStoredCalibrationAimedInput(TouchScreenApiAdapter input)
        {
            return input != null &&
                input.OwnerBlock != null &&
                !input.OwnerBlock.MarkedForClose &&
                input.HasStoredPanelCursorSurface &&
                input.IsAimCursorActive &&
                input.IsVisualCursorOnScreen;
        }

        bool TrySeedPolygonDebugFromStoredCalibration(TouchScreenApiAdapter input)
        {
            if (input == null || input.OwnerBlock == null)
                return false;

            Vector3D seed;
            Vector3D normal;
            Vector3D axisA;
            Vector3D axisB;
            double minA;
            double maxA;
            double minB;
            double maxB;
            if (!input.TryGetStoredPanelCursorSurfaceCalibration(out seed, out normal, out axisA, out axisB, out minA, out maxA, out minB, out maxB))
                return false;

            if (normal.LengthSquared() <= 0.000001 || axisA.LengthSquared() <= 0.000001 || axisB.LengthSquared() <= 0.000001)
                return false;

            normal.Normalize();
            axisA.Normalize();
            axisB.Normalize();

            IMyCubeBlock ownerBlock = input.OwnerBlock;
            IMyEntity ownerEntity = ownerBlock as IMyEntity;
            if (ownerEntity == null)
                return false;

            ClearPolygonDebugSurface();
            ResetPolygonDebugCalibrationAdjustments();
            _polygonDebugCalibrationDepthOffset = 0.0;

            _polygonDebugEntity = ownerEntity;
            _polygonDebugRootBlockEntity = ownerEntity;
            _polygonDebugOwnerBlock = ownerBlock;
            _polygonDebugSeed = seed;
            _polygonDebugNormal = normal;
            _polygonDebugAxisA = axisA;
            _polygonDebugAxisB = axisB;
            _polygonDebugManualBoundsMode = true;
            _polygonDebugFallbackBoundsMode = false;
            _polygonDebugCalibrationScreenIndex = input.GetSurfaceIndex();
            _polygonDebugManualHalfA = Math.Max((maxA - minA) * 0.5, 0.001);
            _polygonDebugManualHalfB = Math.Max((maxB - minB) * 0.5, 0.001);
            SetPolygonDebugLocalBounds(minA, maxA, minB, maxB);
            _hasPolygonDebugSurface = true;
            return true;
        }

        void UpdateCalibratedDebugPopupInput()
        {
            if (!IsManualCalibrationMouseInputArmed())
                return;

            bool pressed = IsCurrentLeftMousePressed() && IsCurrentControlPressed();
            if (pressed && !_calibratedDebugExportWasPressed)
            {
                if (IsCurrentShiftPressed())
                {
                    ProceedAimedPanelToCursorCalibration();
                }
                else
                {
                    GridSchematicsLcdApp app;
                    if (TryFindAimedStoredCalibrationDebugApp(out app))
                        ExportExistingPanelCalibration(app);
                }
            }

            _calibratedDebugExportWasPressed = pressed;
        }

        bool TryFindAimedStoredCalibrationDebugApp(out GridSchematicsLcdApp aimedApp)
        {
            aimedApp = null;
            PanelCursorWorldDrawData cursor;
            if (TryFindPanelSurfaceCursorApp(null, out aimedApp, out cursor) &&
                aimedApp != null &&
                IsStoredCalibrationDebugCandidateApp(aimedApp))
            {
                return true;
            }

            for (int i = _apps.Count - 1; i >= 0; i--)
            {
                GridSchematicsLcdApp app = _apps[i];
                if (IsStoredCalibrationDebugAimedApp(app))
                {
                    aimedApp = app;
                    return true;
                }
            }

            for (int i = _surfaceScriptApps.Count - 1; i >= 0; i--)
            {
                GridSchematicsLcdApp app = _surfaceScriptApps[i];
                if (IsStoredCalibrationDebugAimedApp(app))
                {
                    aimedApp = app;
                    return true;
                }
            }

            return false;
        }

        bool TryFindAimedStoredCalibrationApp(out GridSchematicsLcdApp aimedApp)
        {
            aimedApp = null;
            PanelCursorWorldDrawData cursor;
            if (TryFindPanelSurfaceCursorApp(null, out aimedApp, out cursor) &&
                aimedApp != null &&
                IsStoredCalibrationCandidateApp(aimedApp))
            {
                return true;
            }

            for (int i = _apps.Count - 1; i >= 0; i--)
            {
                GridSchematicsLcdApp app = _apps[i];
                if (IsStoredCalibrationAimedApp(app))
                {
                    aimedApp = app;
                    return true;
                }
            }

            for (int i = _surfaceScriptApps.Count - 1; i >= 0; i--)
            {
                GridSchematicsLcdApp app = _surfaceScriptApps[i];
                if (IsStoredCalibrationAimedApp(app))
                {
                    aimedApp = app;
                    return true;
                }
            }

            return false;
        }

        static bool IsStoredCalibrationAimedApp(GridSchematicsLcdApp app)
        {
            return IsStoredCalibrationCandidateApp(app) &&
                app.TouchInput.IsAimCursorActive &&
                app.TouchInput.IsVisualCursorOnScreen;
        }

        static bool IsStoredCalibrationDebugAimedApp(GridSchematicsLcdApp app)
        {
            return IsStoredCalibrationDebugCandidateApp(app) &&
                app.TouchInput.IsAimCursorActive &&
                app.TouchInput.IsVisualCursorOnScreen;
        }

        static bool IsStoredCalibrationCandidateApp(GridSchematicsLcdApp app)
        {
            return app != null &&
                app.IsOwnerFunctional &&
                app.TouchInput != null &&
                app.TouchInput.HasStoredPanelCursorSurface;
        }

        static bool IsStoredCalibrationDebugCandidateApp(GridSchematicsLcdApp app)
        {
            return IsStoredCalibrationCandidateApp(app) &&
                app.Config != null &&
                app.Config.ShowDebug;
        }

        void ExportExistingPanelCalibration(GridSchematicsLcdApp app)
        {
            if (app == null || app.TouchInput == null || app.TouchInput.OwnerBlock == null)
                return;

            Vector3D seed;
            Vector3D normal;
            Vector3D axisA;
            Vector3D axisB;
            double minA;
            double maxA;
            double minB;
            double maxB;
            if (!app.TouchInput.TryGetStoredPanelCursorSurfaceCalibration(out seed, out normal, out axisA, out axisB, out minA, out maxA, out minB, out maxB))
                return;

            string text = BuildBlockPanelCalibrationExportText(app.TouchInput.OwnerBlock, app.TouchInput.GetSurfaceIndex(), BuildPanelCalibrationEntry(app.TouchInput, "ExistingStoredSurface", seed, normal, axisA, axisB, minA, maxA, minB, maxB));
            try
            {
                if (MyAPIGateway.Utilities == null)
                    return;

                TryWritePanelCalibrationLocalStorage(PanelCalibrationBlockExportFileName, text);

                bool copiedToClipboard = TryCopyTextToSystemClipboard(text);
                NoteManualCalibrationExport(app.TouchInput.OwnerBlock, app.TouchInput.GetSurfaceIndex());
                NoteManualCalibrationStatus(app.TouchInput.OwnerBlock, app.TouchInput.GetSurfaceIndex(), "CALIBRATION EXPORTED");
                ShowPolygonDebugMessage(copiedToClipboard ? "Existing panel calibration copied to clipboard" : "Existing panel calibration exported to local storage file");
            }
            catch
            {
                ShowPolygonDebugMessage("Could not export existing calibration");
            }
        }

        static bool TryCopyTextToSystemClipboard(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            try
            {
                MyClipboardHelper.SetClipboard(text);
                return true;
            }
            catch
            {
            }

            return false;
        }

        static bool TryWritePanelCalibrationLocalStorage(string fileName, string text)
        {
            if (string.IsNullOrEmpty(fileName) || MyAPIGateway.Utilities == null)
                return false;

            try
            {
                using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(fileName, typeof(GridSchematicsSession)))
                {
                    writer.Write(text ?? string.Empty);
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        string BuildPanelCalibrationEntry(TouchScreenApiAdapter input, string calibrationMode, Vector3D seed, Vector3D normal, Vector3D axisA, Vector3D axisB, double minA, double maxA, double minB, double maxB)
        {
            IMyCubeBlock ownerBlock = input.OwnerBlock;
            IMyEntity basisEntity = ownerBlock as IMyEntity;
            MatrixD inverseBasis = basisEntity == null ? MatrixD.Identity : MatrixD.Invert(basisEntity.WorldMatrix);
            MatrixD inverseBasisNormal = basisEntity == null ? MatrixD.Identity : MatrixD.Transpose(basisEntity.WorldMatrix);
            Vector3D localSeed = Vector3D.Transform(seed, inverseBasis);
            Vector3D localNormal = Vector3D.TransformNormal(normal, inverseBasisNormal);
            Vector3D localAxisA = Vector3D.TransformNormal(axisA, inverseBasisNormal);
            Vector3D localAxisB = Vector3D.TransformNormal(axisB, inverseBasisNormal);

            int screenIndex = input.GetSurfaceIndex();
            return
                "ScreenCalibration[" + screenIndex.ToString(CultureInfo.InvariantCulture) + "]:\n" +
                "ScreenIndex=" + screenIndex.ToString(CultureInfo.InvariantCulture) + "\n" +
                "CalibrationMode=" + calibrationMode + "\n" +
                "SeedLocal=" + FormatPolygonDebugVector(localSeed) + "\n" +
                "NormalLocal=" + FormatPolygonDebugVector(localNormal) + "\n" +
                "AxisALocal=" + FormatPolygonDebugVector(localAxisA) + "\n" +
                "AxisBLocal=" + FormatPolygonDebugVector(localAxisB) + "\n" +
                "FinalMinA=" + FormatPolygonDebugDouble(minA) + "\n" +
                "FinalMaxA=" + FormatPolygonDebugDouble(maxA) + "\n" +
                "FinalMinB=" + FormatPolygonDebugDouble(minB) + "\n" +
                "FinalMaxB=" + FormatPolygonDebugDouble(maxB) + "\n";
        }

        string BuildBlockPanelCalibrationExportText(IMyCubeBlock block, int extraScreenIndex, string extraEntry)
        {
            string blockDefinition = GetBlockDefinitionId(block);
            string blockDisplayName = "Unknown";
            long blockEntityId = 0L;
            int surfaceCount = GetBlockSurfaceCount(block);

            if (block != null)
            {
                blockEntityId = block.EntityId;
                var blockEntity = block as IMyEntity;
                if (blockEntity != null && !string.IsNullOrEmpty(blockEntity.DisplayName))
                    blockDisplayName = blockEntity.DisplayName;
            }

            var entries = new Dictionary<int, string>();
            AddCatalogCalibrationEntriesForBlock(blockDefinition, entries);
            AddLiveCalibrationEntriesForBlock(block, entries);
            if (!string.IsNullOrEmpty(extraEntry) && extraScreenIndex >= 0)
                entries[extraScreenIndex] = extraEntry;

            if (surfaceCount <= 0)
            {
                foreach (var pair in entries)
                {
                    if (pair.Key + 1 > surfaceCount)
                        surfaceCount = pair.Key + 1;
                }
            }

            return BuildBlockPanelCalibrationExportText(blockDefinition, blockDisplayName, blockEntityId, surfaceCount, entries);
        }

        static string BuildBlockPanelCalibrationExportText(string blockDefinition, string blockDisplayName, long blockEntityId, int surfaceCount, Dictionary<int, string> entries)
        {
            if (entries == null)
                entries = new Dictionary<int, string>();

            if (surfaceCount <= 0)
            {
                foreach (var pair in entries)
                {
                    if (pair.Key + 1 > surfaceCount)
                        surfaceCount = pair.Key + 1;
                }
            }

            var builder = new System.Text.StringBuilder();
            AppendBlockPanelCalibrationExportText(builder, blockDefinition, blockDisplayName, blockEntityId, surfaceCount, entries);
            return builder.ToString();
        }

        static void AppendBlockPanelCalibrationExportText(System.Text.StringBuilder builder, string blockDefinition, string blockDisplayName, long blockEntityId, int surfaceCount, Dictionary<int, string> entries)
        {
            if (builder == null)
                return;

            builder.AppendLine("GridSchematicsPanelCursorCalibrationBlock");
            builder.AppendLine("BlockDefinitionId=" + (blockDefinition ?? string.Empty));
            builder.AppendLine("BlockDisplayName=" + (string.IsNullOrEmpty(blockDisplayName) ? "Unknown" : blockDisplayName));
            builder.AppendLine("DebugBlockEntityId=" + blockEntityId.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine("DetectedSurfaceCount=" + surfaceCount.ToString(CultureInfo.InvariantCulture));
            builder.AppendLine();

            if (entries == null)
                return;

            for (int i = 0; i < surfaceCount; i++)
            {
                string entry;
                if (entries.TryGetValue(i, out entry))
                {
                    builder.Append(entry);
                    if (!entry.EndsWith("\n", StringComparison.Ordinal))
                        builder.AppendLine();
                    builder.AppendLine();
                }
            }

            foreach (var pair in entries)
            {
                if (pair.Key >= 0 && pair.Key < surfaceCount)
                    continue;

                builder.Append(pair.Value);
                if (!pair.Value.EndsWith("\n", StringComparison.Ordinal))
                    builder.AppendLine();
                builder.AppendLine();
            }
        }

        void NoteManualCalibrationExport(IMyCubeBlock block, int exportedScreenIndex)
        {
            if (block == null)
                return;

            _manualCalibrationExportNotice = new ManualCalibrationExportNotice
            {
                BlockEntityId = block.EntityId,
                BlockId = GetBlockDefinitionId(block),
                IndexLines = BuildManualCalibrationExportIndexLines(block, exportedScreenIndex)
            };
        }

        void NoteManualCalibrationStatus(IMyCubeBlock block, int surfaceIndex, string status)
        {
            if (block == null || string.IsNullOrEmpty(status))
                return;

            _manualCalibrationStatusNotice = new ManualCalibrationStatusNotice
            {
                BlockEntityId = block.EntityId,
                SurfaceIndex = surfaceIndex,
                Status = status
            };
        }

        public bool TryGetManualCalibrationStatus(IMyCubeBlock block, int surfaceIndex, out string status)
        {
            status = string.Empty;
            if (block == null || _manualCalibrationStatusNotice == null || _manualCalibrationStatusNotice.BlockEntityId != block.EntityId)
                return false;

            if (_manualCalibrationStatusNotice.SurfaceIndex >= 0 && surfaceIndex >= 0 && _manualCalibrationStatusNotice.SurfaceIndex != surfaceIndex)
                return false;

            status = _manualCalibrationStatusNotice.Status ?? string.Empty;
            return !string.IsNullOrEmpty(status);
        }

        string[] BuildManualCalibrationExportIndexLines(IMyCubeBlock block, int exportedScreenIndex)
        {
            int surfaceCount = GetBlockSurfaceCount(block);
            if (surfaceCount <= 0)
                surfaceCount = exportedScreenIndex >= 0 ? exportedScreenIndex + 1 : 1;

            string[] lines = new string[surfaceCount];
            string blockDefinition = GetBlockDefinitionId(block);
            for (int i = 0; i < surfaceCount; i++)
            {
                bool calibrated = i == exportedScreenIndex ||
                    IsLiveSurfaceCalibrated(block, i) ||
                    IsCatalogSurfaceCalibrated(blockDefinition, i);

                lines[i] = "INDEX " + i.ToString(CultureInfo.InvariantCulture) + ": " + (calibrated ? "CALIBRATED" : "MISSING");
            }

            return lines;
        }

        int GetBlockSurfaceCount(IMyCubeBlock block)
        {
            if (block == null)
                return 0;

            try
            {
                var provider = block as Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;
                if (provider != null)
                    return provider.SurfaceCount;
            }
            catch
            {
            }

            return 0;
        }

        bool IsLiveSurfaceCalibrated(IMyCubeBlock block, int surfaceIndex)
        {
            return IsLiveSurfaceCalibratedInApps(_apps, block, surfaceIndex) ||
                IsLiveSurfaceCalibratedInApps(_surfaceScriptApps, block, surfaceIndex) ||
                IsLiveSurfaceCalibratedInProbes(block, surfaceIndex);
        }

        static bool IsLiveSurfaceCalibratedInApps(List<GridSchematicsLcdApp> apps, IMyCubeBlock block, int surfaceIndex)
        {
            if (apps == null || block == null)
                return false;

            for (int i = 0; i < apps.Count; i++)
            {
                GridSchematicsLcdApp app = apps[i];
                if (app == null || app.TouchInput == null || app.TouchInput.OwnerBlock == null)
                    continue;

                if (app.TouchInput.OwnerBlock.EntityId == block.EntityId &&
                    app.TouchInput.GetSurfaceIndex() == surfaceIndex &&
                    app.TouchInput.HasStoredPanelCursorSurface)
                {
                    return true;
                }
            }

            return false;
        }

        bool IsLiveSurfaceCalibratedInProbes(IMyCubeBlock block, int surfaceIndex)
        {
            if (block == null)
                return false;

            for (int i = 0; i < _unsupportedSurfaceProbes.Count; i++)
            {
                TouchScreenApiAdapter input = _unsupportedSurfaceProbes[i];
                if (input == null || input.OwnerBlock == null)
                    continue;

                if (input.OwnerBlock.EntityId == block.EntityId &&
                    input.GetSurfaceIndex() == surfaceIndex &&
                    input.HasStoredPanelCursorSurface)
                {
                    return true;
                }
            }

            return false;
        }

        bool IsCatalogSurfaceCalibrated(string blockDefinition, int surfaceIndex)
        {
            if (string.IsNullOrEmpty(blockDefinition) || MyAPIGateway.Utilities == null)
                return false;

            string catalog;
            if (!TryReadPanelCalibrationCatalog(out catalog) || string.IsNullOrEmpty(catalog))
                return false;

            string[] entries = catalog.Split(new string[] { PanelCalibrationCatalogSeparator }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                string entryDefinition = GetCalibrationCatalogValue(entries[i], "BlockDefinitionId");
                if (!string.Equals(entryDefinition, blockDefinition, StringComparison.Ordinal))
                    continue;

                int entryScreenIndex;
                if (int.TryParse(GetCalibrationCatalogValue(entries[i], "ScreenIndex"), NumberStyles.Integer, CultureInfo.InvariantCulture, out entryScreenIndex) &&
                    entryScreenIndex == surfaceIndex)
                {
                    return true;
                }
            }

            return false;
        }

        bool RemoveStoredPanelCursorCalibration(TouchScreenApiAdapter input)
        {
            if (input == null || input.OwnerBlock == null)
                return false;

            string blockDefinition = GetBlockDefinitionId(input.OwnerBlock);
            int surfaceIndex = input.GetSurfaceIndex();
            if (string.IsNullOrEmpty(blockDefinition) || surfaceIndex < 0 || MyAPIGateway.Utilities == null)
                return false;

            string catalog;
            if (!TryReadPanelCalibrationLocalStorage(out catalog) || string.IsNullOrEmpty(catalog))
                return false;

            string updated;
            bool removed = RemovePanelCursorCalibrationCatalogEntry(catalog, blockDefinition, surfaceIndex, out updated);
            if (!removed)
                return false;

            try
            {
                TryWritePanelCalibrationLocalStorage(PanelCalibrationCatalogFileName, updated ?? string.Empty);

                return true;
            }
            catch
            {
                return false;
            }
        }

        static bool RemovePanelCursorCalibrationCatalogEntry(string catalog, string blockDefinition, int surfaceIndex, out string updatedCatalog)
        {
            updatedCatalog = catalog ?? string.Empty;
            if (string.IsNullOrEmpty(catalog) || string.IsNullOrEmpty(blockDefinition) || surfaceIndex < 0)
                return false;

            bool removed = false;
            var builder = new System.Text.StringBuilder();
            string[] entries = catalog.Split(new string[] { PanelCalibrationCatalogSeparator }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                string entry = entries[i];
                if (string.IsNullOrWhiteSpace(entry))
                    continue;

                string trimmed = entry.Trim();
                if (!string.Equals(GetCalibrationCatalogValue(trimmed, "BlockDefinitionId"), blockDefinition, StringComparison.Ordinal))
                {
                    AppendPanelCalibrationCatalogEntry(builder, trimmed);
                    continue;
                }

                string withoutScreen = RemoveScreenCalibrationSection(trimmed, surfaceIndex);
                if (!string.Equals(withoutScreen, trimmed, StringComparison.Ordinal))
                    removed = true;

                if (!string.IsNullOrWhiteSpace(withoutScreen) &&
                    withoutScreen.IndexOf("ScreenCalibration[", StringComparison.Ordinal) >= 0)
                {
                    AppendPanelCalibrationCatalogEntry(builder, withoutScreen.Trim());
                }
            }

            if (!removed)
                return false;

            updatedCatalog = builder.ToString();
            return true;
        }

        static void AppendPanelCalibrationCatalogEntry(System.Text.StringBuilder builder, string entry)
        {
            if (builder == null || string.IsNullOrWhiteSpace(entry))
                return;

            if (builder.Length > 0 && builder[builder.Length - 1] != '\n')
                builder.AppendLine();

            builder.AppendLine(PanelCalibrationCatalogSeparator);
            builder.Append(entry.Trim());
            builder.AppendLine();
        }

        static string RemoveScreenCalibrationSection(string entry, int surfaceIndex)
        {
            if (string.IsNullOrEmpty(entry) || surfaceIndex < 0)
                return entry ?? string.Empty;

            string result = entry.Replace("\r", string.Empty);
            string marker = "ScreenCalibration[" + surfaceIndex.ToString(CultureInfo.InvariantCulture) + "]:";
            int start = result.IndexOf(marker, StringComparison.Ordinal);
            while (start >= 0)
            {
                int sectionStart = start;
                if (sectionStart > 0 && result[sectionStart - 1] == '\n')
                    sectionStart--;

                int nextScreen = result.IndexOf("\nScreenCalibration[", start + marker.Length, StringComparison.Ordinal);
                int suggested = result.IndexOf("\nSuggested permanent lookup shape:", start + marker.Length, StringComparison.Ordinal);
                int sectionEnd = result.Length;
                if (nextScreen >= 0 && (suggested < 0 || nextScreen < suggested))
                    sectionEnd = nextScreen;
                else if (suggested >= 0)
                    sectionEnd = suggested;

                result = result.Remove(sectionStart, sectionEnd - sectionStart);
                start = result.IndexOf(marker, StringComparison.Ordinal);
            }

            return result.Trim();
        }

        public bool IsPanelSurfaceLowLevelCalibrated(IMyCubeBlock block, int surfaceIndex)
        {
            if (block == null)
                return false;

            if (IsLiveSurfaceCalibrated(block, surfaceIndex))
                return true;

            return IsCatalogSurfaceCalibrated(GetBlockDefinitionId(block), surfaceIndex);
        }

        void AddCatalogCalibrationEntriesForBlock(string blockDefinition, Dictionary<int, string> entries)
        {
            if (entries == null || string.IsNullOrEmpty(blockDefinition))
                return;

            string catalog;
            if (!TryReadPanelCalibrationCatalog(out catalog) || string.IsNullOrEmpty(catalog))
                return;

            string[] rawEntries = catalog.Split(new string[] { PanelCalibrationCatalogSeparator }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < rawEntries.Length; i++)
            {
                string entry = rawEntries[i];
                string entryDefinition = GetCalibrationCatalogValue(entry, "BlockDefinitionId");
                if (!string.Equals(entryDefinition, blockDefinition, StringComparison.Ordinal))
                    continue;

                int screenIndex;
                if (!int.TryParse(GetCalibrationCatalogValue(entry, "ScreenIndex"), NumberStyles.Integer, CultureInfo.InvariantCulture, out screenIndex))
                    continue;

                string screenEntry = ExtractScreenCalibrationEntry(entry, screenIndex);
                if (!string.IsNullOrEmpty(screenEntry))
                    entries[screenIndex] = screenEntry;
            }
        }

        static string ExtractScreenCalibrationEntry(string entry, int screenIndex)
        {
            if (string.IsNullOrEmpty(entry))
                return string.Empty;

            string marker = "ScreenCalibration[" + screenIndex.ToString(CultureInfo.InvariantCulture) + "]:";
            int start = entry.IndexOf(marker, StringComparison.Ordinal);
            if (start < 0)
                return string.Empty;

            int next = entry.IndexOf("\nScreenCalibration[", start + marker.Length, StringComparison.Ordinal);
            string screenEntry = next >= 0 ? entry.Substring(start, next - start) : entry.Substring(start);
            int suggested = screenEntry.IndexOf("\nSuggested permanent lookup shape:", StringComparison.Ordinal);
            if (suggested >= 0)
                screenEntry = screenEntry.Substring(0, suggested);

            return screenEntry.Trim() + "\n";
        }

        void AddLiveCalibrationEntriesForBlock(IMyCubeBlock block, Dictionary<int, string> entries)
        {
            if (block == null || entries == null)
                return;

            AddLiveCalibrationEntriesForBlockFromApps(_apps, block, entries);
            AddLiveCalibrationEntriesForBlockFromApps(_surfaceScriptApps, block, entries);

            for (int i = 0; i < _unsupportedSurfaceProbes.Count; i++)
            {
                TouchScreenApiAdapter input = _unsupportedSurfaceProbes[i];
                AddLiveCalibrationEntryForInput(input, block, entries);
            }
        }

        void AddLiveCalibrationEntriesForBlockFromApps(List<GridSchematicsLcdApp> apps, IMyCubeBlock block, Dictionary<int, string> entries)
        {
            if (apps == null)
                return;

            for (int i = 0; i < apps.Count; i++)
            {
                GridSchematicsLcdApp app = apps[i];
                if (app == null)
                    continue;

                AddLiveCalibrationEntryForInput(app.TouchInput, block, entries);
            }
        }

        void AddLiveCalibrationEntryForInput(TouchScreenApiAdapter input, IMyCubeBlock block, Dictionary<int, string> entries)
        {
            if (input == null || input.OwnerBlock == null || input.OwnerBlock.EntityId != block.EntityId || !input.HasStoredPanelCursorSurface)
                return;

            Vector3D seed;
            Vector3D normal;
            Vector3D axisA;
            Vector3D axisB;
            double minA;
            double maxA;
            double minB;
            double maxB;
            if (!input.TryGetStoredPanelCursorSurfaceCalibration(out seed, out normal, out axisA, out axisB, out minA, out maxA, out minB, out maxB))
                return;

            int surfaceIndex = input.GetSurfaceIndex();
            if (surfaceIndex < 0)
                return;

            entries[surfaceIndex] = BuildPanelCalibrationEntry(input, "ExistingStoredSurface", seed, normal, axisA, axisB, minA, maxA, minB, maxB);
        }

        public bool TryGetManualCalibrationExportNotice(IMyCubeBlock block, out string blockId, out string[] indexLines)
        {
            blockId = string.Empty;
            indexLines = null;
            if (block == null || _manualCalibrationExportNotice == null || _manualCalibrationExportNotice.BlockEntityId != block.EntityId)
                return false;

            blockId = _manualCalibrationExportNotice.BlockId ?? string.Empty;
            indexLines = _manualCalibrationExportNotice.IndexLines;
            return true;
        }

        static bool DoesCurrentAimHitInput(TouchScreenApiAdapter input)
        {
            if (input == null)
                return false;

            Vector3D hit;
            Vector3D normal;
            return input.TryGetCurrentModelAimDebugHit(out hit, out normal);
        }

        void UpdatePolygonDebugScrollAdjustments()
        {
            if (!_hasPolygonDebugSurface || MyAPIGateway.Input == null || IsGameGuiCursorVisible())
            {
                _polygonDebugScrollInitialized = false;
                return;
            }

            int wheelValue;
            try
            {
                wheelValue = MyAPIGateway.Input.MouseScrollWheelValue();
            }
            catch
            {
                _polygonDebugScrollInitialized = false;
                return;
            }

            if (!_polygonDebugScrollInitialized)
            {
                _polygonDebugLastScrollValue = wheelValue;
                _polygonDebugScrollInitialized = true;
                return;
            }

            int delta = wheelValue - _polygonDebugLastScrollValue;
            _polygonDebugLastScrollValue = wheelValue;
            if (delta == 0)
                return;

            int direction = delta > 0 ? 1 : -1;
            bool shift = IsCurrentShiftPressed();
            bool control = IsCurrentControlPressed();
            bool alt = IsCurrentAltPressed();
            const double MoveStep = 0.0015;
            const double DepthStep = 0.001;
            const double ScaleStep = 0.00875;
            const double RotateStepRadians = Math.PI / 360.0;
            const double TiltStepRadians = Math.PI / 720.0;

            if (_polygonDebugAlternateAimMode && alt && shift && control)
            {
                RotatePolygonDebugBounds(direction * RotateStepRadians);
            }
            else if (_polygonDebugAlternateAimMode && alt && shift)
            {
                TiltPolygonDebugPlane(0.0, direction * TiltStepRadians);
            }
            else if (_polygonDebugAlternateAimMode && alt && control)
            {
                RotatePolygonDebugBounds(direction * RotateStepRadians);
            }
            else if (_polygonDebugAlternateAimMode && alt)
            {
                TiltPolygonDebugPlane(direction * TiltStepRadians, 0.0);
            }
            else if (alt && shift && control)
            {
                RotatePolygonDebugBounds(direction * RotateStepRadians);
            }
            else if (alt && shift)
            {
                _polygonDebugCalibrationScaleA *= 1.0 + direction * ScaleStep;
            }
            else if (alt && control)
            {
                _polygonDebugCalibrationScaleB *= 1.0 + direction * ScaleStep;
            }
            else if (shift && control)
            {
                _polygonDebugCalibrationDepthOffset += direction * DepthStep;
            }
            else if (shift)
            {
                _polygonDebugCalibrationOffsetA += direction * MoveStep;
            }
            else if (control)
            {
                _polygonDebugCalibrationOffsetB += direction * MoveStep;
            }
            else
            {
                double factor = 1.0 + direction * ScaleStep;
                _polygonDebugCalibrationScaleA *= factor;
                _polygonDebugCalibrationScaleB *= factor;
            }

            ClampPolygonDebugCalibrationScales();
        }

        void RotatePolygonDebugBounds(double radians)
        {
            if (radians == 0.0 ||
                _polygonDebugNormal.LengthSquared() <= 0.000001 ||
                _polygonDebugAxisA.LengthSquared() <= 0.000001 ||
                _polygonDebugAxisB.LengthSquared() <= 0.000001)
            {
                return;
            }

            Vector3D normal = _polygonDebugNormal;
            normal.Normalize();

            double finalMinA;
            double finalMaxA;
            double finalMinB;
            double finalMaxB;
            GetPolygonDebugEffectiveCullingBounds(out finalMinA, out finalMaxA, out finalMinB, out finalMaxB);
            double centerA = (finalMinA + finalMaxA) * 0.5;
            double centerB = (finalMinB + finalMaxB) * 0.5;
            Vector3D center = _polygonDebugSeed + _polygonDebugAxisA * centerA + _polygonDebugAxisB * centerB;

            Vector3D axisA = RotatePolygonDebugVectorAroundAxis(_polygonDebugAxisA, normal, radians);
            Vector3D axisB = RotatePolygonDebugVectorAroundAxis(_polygonDebugAxisB, normal, radians);
            if (axisA.LengthSquared() <= 0.000001 || axisB.LengthSquared() <= 0.000001)
                return;

            axisA.Normalize();
            axisB.Normalize();
            _polygonDebugAxisA = axisA;
            _polygonDebugAxisB = axisB;
            _polygonDebugSeed = center - axisA * centerA - axisB * centerB;
            SetPolygonDebugLocalBounds(_polygonDebugMinA, _polygonDebugMaxA, _polygonDebugMinB, _polygonDebugMaxB);
            NoteManualCalibrationStatus(_polygonDebugOwnerBlock, _polygonDebugCalibrationScreenIndex, "ROTATION ADJUSTED");
        }

        void TiltPolygonDebugPlane(double radiansAroundA, double radiansAroundB)
        {
            if (!_polygonDebugAlternateAimMode ||
                !_hasPolygonDebugSurface ||
                _polygonDebugNormal.LengthSquared() <= 0.000001 ||
                _polygonDebugAxisA.LengthSquared() <= 0.000001 ||
                _polygonDebugAxisB.LengthSquared() <= 0.000001)
            {
                return;
            }

            if (radiansAroundA == 0.0 && radiansAroundB == 0.0)
                return;

            Vector3D axisA = _polygonDebugAxisA;
            Vector3D axisB = _polygonDebugAxisB;
            Vector3D normal = _polygonDebugNormal;
            axisA.Normalize();
            axisB.Normalize();
            normal.Normalize();

            double centerA = (_polygonDebugMinA + _polygonDebugMaxA) * 0.5;
            double centerB = (_polygonDebugMinB + _polygonDebugMaxB) * 0.5;
            Vector3D center = _polygonDebugSeed + axisA * centerA + axisB * centerB;

            if (radiansAroundA != 0.0)
            {
                axisB = RotatePolygonDebugVectorAroundAxis(axisB, axisA, radiansAroundA);
                normal = RotatePolygonDebugVectorAroundAxis(normal, axisA, radiansAroundA);
            }

            if (radiansAroundB != 0.0)
            {
                axisA = RotatePolygonDebugVectorAroundAxis(axisA, axisB, radiansAroundB);
                normal = RotatePolygonDebugVectorAroundAxis(normal, axisB, radiansAroundB);
            }

            if (normal.LengthSquared() <= 0.000001 || axisA.LengthSquared() <= 0.000001)
                return;

            normal.Normalize();
            axisA = axisA - normal * Vector3D.Dot(axisA, normal);
            if (axisA.LengthSquared() <= 0.000001)
                return;
            axisA.Normalize();

            Vector3D rebuiltB = Vector3D.Cross(normal, axisA);
            if (rebuiltB.LengthSquared() <= 0.000001)
                return;
            rebuiltB.Normalize();

            _polygonDebugNormal = normal;
            _polygonDebugAxisA = axisA;
            _polygonDebugAxisB = rebuiltB;
            _polygonDebugSeed = center - axisA * centerA - rebuiltB * centerB;
            SetPolygonDebugLocalBounds(_polygonDebugMinA, _polygonDebugMaxA, _polygonDebugMinB, _polygonDebugMaxB);
            NoteManualCalibrationStatus(_polygonDebugOwnerBlock, _polygonDebugCalibrationScreenIndex, "PLANE TILTED");
        }

        static Vector3D RotatePolygonDebugVectorAroundAxis(Vector3D vector, Vector3D axis, double radians)
        {
            if (axis.LengthSquared() <= 0.000001)
                return vector;

            axis.Normalize();
            double cos = Math.Cos(radians);
            double sin = Math.Sin(radians);
            return vector * cos + Vector3D.Cross(axis, vector) * sin + axis * Vector3D.Dot(axis, vector) * (1.0 - cos);
        }

        void DrawPolygonDebugSurface()
        {
            if (!_hasPolygonDebugSurface)
                return;

            var borderColor = new Color(80, 255, 145, 255);
            float thickness = 0.003f;

            Vector3D topLeft;
            Vector3D topRight;
            Vector3D bottomRight;
            Vector3D bottomLeft;
            GetPolygonDebugFinalCorners(out topLeft, out topRight, out bottomRight, out bottomLeft);

            Vector3D calibrationOffset = GetPolygonDebugCalibrationPlaneOffset();
            DrawPolygonDebugCornerMarkers(topLeft + calibrationOffset, topRight + calibrationOffset, bottomRight + calibrationOffset, bottomLeft + calibrationOffset, borderColor);
            DrawPolygonDebugSurfaceIndexLabel(topLeft + calibrationOffset, topRight + calibrationOffset, bottomRight + calibrationOffset, bottomLeft + calibrationOffset, borderColor);

            if (_polygonDebugVisualsVisible)
            {
                DrawPolygonDebugCalibrationAxes(topLeft + calibrationOffset, topRight + calibrationOffset, bottomRight + calibrationOffset, bottomLeft + calibrationOffset);
                DrawWorldDebugLine(topLeft + calibrationOffset, topRight + calibrationOffset, thickness, borderColor);
                DrawWorldDebugLine(topRight + calibrationOffset, bottomRight + calibrationOffset, thickness, borderColor);
                DrawWorldDebugLine(bottomRight + calibrationOffset, bottomLeft + calibrationOffset, thickness, borderColor);
                DrawWorldDebugLine(bottomLeft + calibrationOffset, topLeft + calibrationOffset, thickness, borderColor);
                DrawPolygonDebugMaskOverlay();
                DrawPolygonDebugTriangleCloud();
                DrawPolygonDebugSubpartWiremesh();
            }
            DrawAimLockedPolygonCursor();
        }

        void DrawPolygonDebugSurfaceIndexLabel(Vector3D topLeft, Vector3D topRight, Vector3D bottomRight, Vector3D bottomLeft, Color color)
        {
            if (_polygonDebugCalibrationScreenIndex < 0)
                return;

            Vector3D axisA = _polygonDebugAxisA;
            Vector3D axisB = _polygonDebugAxisB;
            Vector3D normal = GetPolygonDebugNormalTowardCamera();
            if (axisA.LengthSquared() <= 0.000001 || axisB.LengthSquared() <= 0.000001 || normal.LengthSquared() <= 0.000001)
                return;

            axisA.Normalize();
            axisB.Normalize();
            normal.Normalize();

            double width = Vector3D.Distance(topLeft, topRight);
            double height = Vector3D.Distance(topLeft, bottomLeft);
            double digitHeight = Math.Min(Math.Max(Math.Min(width, height) * 0.10, 0.024), 0.070);
            double digitWidth = digitHeight * 0.56;
            double spacing = digitWidth * 0.28;
            float thickness = (float)Math.Min(Math.Max(digitHeight * 0.070, 0.0016), 0.0032);

            string label = _polygonDebugCalibrationScreenIndex.ToString(CultureInfo.InvariantCulture);
            double totalWidth = label.Length * digitWidth + Math.Max(0, label.Length - 1) * spacing;
            Vector3D topCenter = (topLeft + topRight) * 0.5;
            Vector3D origin = topCenter - axisA * (totalWidth * 0.5) + axisB * (digitHeight * 0.58) + normal * 0.012;

            for (int i = 0; i < label.Length; i++)
            {
                int digit = label[i] - '0';
                if (digit >= 0 && digit <= 9)
                    DrawPolygonDebugSevenSegmentDigit(origin + axisA * (i * (digitWidth + spacing)), axisA, -axisB, digitWidth, digitHeight, thickness, color, digit);
            }
        }

        static void DrawPolygonDebugSevenSegmentDigit(Vector3D topLeft, Vector3D right, Vector3D down, double width, double height, float thickness, Color color, int digit)
        {
            for (int segment = 0; segment < 7; segment++)
            {
                if (!IsPolygonDebugDigitSegmentLit(digit, segment))
                    continue;

                switch (segment)
                {
                    case 0:
                        DrawPolygonDebugDigitSegment(topLeft, right, down, 0.15, 0.00, 0.85, 0.00, width, height, thickness, color);
                        break;
                    case 1:
                        DrawPolygonDebugDigitSegment(topLeft, right, down, 0.92, 0.08, 0.92, 0.45, width, height, thickness, color);
                        break;
                    case 2:
                        DrawPolygonDebugDigitSegment(topLeft, right, down, 0.92, 0.55, 0.92, 0.92, width, height, thickness, color);
                        break;
                    case 3:
                        DrawPolygonDebugDigitSegment(topLeft, right, down, 0.15, 1.00, 0.85, 1.00, width, height, thickness, color);
                        break;
                    case 4:
                        DrawPolygonDebugDigitSegment(topLeft, right, down, 0.08, 0.55, 0.08, 0.92, width, height, thickness, color);
                        break;
                    case 5:
                        DrawPolygonDebugDigitSegment(topLeft, right, down, 0.08, 0.08, 0.08, 0.45, width, height, thickness, color);
                        break;
                    case 6:
                        DrawPolygonDebugDigitSegment(topLeft, right, down, 0.15, 0.50, 0.85, 0.50, width, height, thickness, color);
                        break;
                }
            }
        }

        static void DrawPolygonDebugDigitSegment(Vector3D topLeft, Vector3D right, Vector3D down, double x0, double y0, double x1, double y1, double width, double height, float thickness, Color color)
        {
            Vector3D start = topLeft + right * (x0 * width) + down * (y0 * height);
            Vector3D end = topLeft + right * (x1 * width) + down * (y1 * height);
            DrawWorldDebugLine(start, end, thickness, color);
        }

        static bool IsPolygonDebugDigitSegmentLit(int digit, int segment)
        {
            switch (digit)
            {
                case 0:
                    return segment != 6;
                case 1:
                    return segment == 1 || segment == 2;
                case 2:
                    return segment == 0 || segment == 1 || segment == 6 || segment == 4 || segment == 3;
                case 3:
                    return segment == 0 || segment == 1 || segment == 6 || segment == 2 || segment == 3;
                case 4:
                    return segment == 5 || segment == 6 || segment == 1 || segment == 2;
                case 5:
                    return segment == 0 || segment == 5 || segment == 6 || segment == 2 || segment == 3;
                case 6:
                    return segment == 0 || segment == 5 || segment == 6 || segment == 4 || segment == 2 || segment == 3;
                case 7:
                    return segment == 0 || segment == 1 || segment == 2;
                case 8:
                    return true;
                case 9:
                    return segment == 0 || segment == 1 || segment == 2 || segment == 3 || segment == 5 || segment == 6;
            }

            return false;
        }

        Vector3D GetPolygonDebugCalibrationPlaneOffset()
        {
            Vector3D normal = _polygonDebugNormal;
            if (normal.LengthSquared() <= 0.000001)
                return Vector3D.Zero;

            normal.Normalize();
            return normal * _polygonDebugCalibrationDepthOffset;
        }

        Vector3D GetPolygonDebugCalibrationPlaneSeed()
        {
            return _polygonDebugSeed + GetPolygonDebugCalibrationPlaneOffset();
        }

        void DrawPolygonDebugCornerMarkers(Vector3D topLeft, Vector3D topRight, Vector3D bottomRight, Vector3D bottomLeft, Color color)
        {
            Vector3D axisA = _polygonDebugAxisA;
            Vector3D axisB = _polygonDebugAxisB;
            if (axisA.LengthSquared() <= 0.000001 || axisB.LengthSquared() <= 0.000001)
                return;

            axisA.Normalize();
            axisB.Normalize();

            double width = Vector3D.Distance(topLeft, topRight);
            double height = Vector3D.Distance(topLeft, bottomLeft);
            double markLength = Math.Min(Math.Min(width, height) * 0.04, 0.016);
            if (markLength < 0.005)
                markLength = 0.005;

            const float thickness = 0.002f;
            DrawPolygonDebugCornerMarkerLeg(topLeft, axisA, axisB, markLength, thickness, color);
            DrawPolygonDebugCornerMarkerLeg(topLeft, -axisB, -axisA, markLength, thickness, color);
            DrawPolygonDebugCornerMarkerLeg(topRight, -axisA, axisB, markLength, thickness, color);
            DrawPolygonDebugCornerMarkerLeg(topRight, -axisB, axisA, markLength, thickness, color);
            DrawPolygonDebugCornerMarkerLeg(bottomRight, -axisA, -axisB, markLength, thickness, color);
            DrawPolygonDebugCornerMarkerLeg(bottomRight, axisB, axisA, markLength, thickness, color);
            DrawPolygonDebugCornerMarkerLeg(bottomLeft, axisA, -axisB, markLength, thickness, color);
            DrawPolygonDebugCornerMarkerLeg(bottomLeft, axisB, -axisA, markLength, thickness, color);
        }

        void DrawPolygonDebugCornerMarkerLeg(Vector3D corner, Vector3D inwardAlong, Vector3D outwardAcross, double length, float thickness, Color color)
        {
            if (length <= 0.0001 || thickness <= 0.0001f)
                return;

            Vector3D center = corner + inwardAlong * (length * 0.5) + outwardAcross * (thickness * 0.5);
            MyTransparentGeometry.AddBillboardOriented(
                CalibrationDebugSquareMaterial,
                color.ToVector4(),
                center,
                inwardAlong,
                outwardAcross,
                (float)length,
                thickness);
        }

        Vector3D GetPolygonDebugNormalTowardCamera()
        {
            Vector3D normal = _polygonDebugNormal;
            if (normal.LengthSquared() <= 0.000001)
                return Vector3D.Zero;

            normal.Normalize();
            if (MyAPIGateway.Session != null && MyAPIGateway.Session.Camera != null &&
                Vector3D.Dot(MyAPIGateway.Session.Camera.WorldMatrix.Translation - _polygonDebugSeed, normal) < 0.0)
            {
                normal = -normal;
            }

            return normal;
        }

        void DrawPolygonDebugCalibrationAxes(Vector3D topLeft, Vector3D topRight, Vector3D bottomRight, Vector3D bottomLeft)
        {
            Vector3D center = (topLeft + topRight + bottomRight + bottomLeft) * 0.25;
            Vector3D axisA = _polygonDebugAxisA;
            Vector3D axisB = _polygonDebugAxisB;
            Vector3D normal = _polygonDebugNormal;
            if (axisA.LengthSquared() <= 0.000001 || axisB.LengthSquared() <= 0.000001 || normal.LengthSquared() <= 0.000001)
                return;

            axisA.Normalize();
            axisB.Normalize();
            normal.Normalize();

            double width = Vector3D.Distance(topLeft, topRight);
            double height = Vector3D.Distance(topLeft, bottomLeft);
            double markerScale = Math.Min(Math.Max(width, height) * 0.18, 0.16);
            if (markerScale < 0.035)
                markerScale = 0.035;

            DrawWorldDebugCross(center + normal * 0.006, 0.020f, 0.004f, new Color(255, 255, 255, 245));
            DrawWorldDebugArrow(center, axisA, markerScale, new Color(255, 80, 80, 245));
            DrawWorldDebugArrow(center, axisB, markerScale, new Color(80, 255, 120, 245));
            DrawWorldDebugArrow(center, normal, Math.Min(markerScale * 0.75, 0.10), new Color(80, 160, 255, 245));
            DrawSurfaceIndexTicks(center - axisA * markerScale * 0.70 - axisB * markerScale * 0.70, axisA, axisB, normal, _polygonDebugCalibrationScreenIndex);
        }

        bool TryBuildPolygonDebugSurface()
        {
            _hasPolygonDebugSurface = false;
            _polygonDebugEntity = null;
            _polygonDebugSeed = Vector3D.Zero;
            _polygonDebugNormal = Vector3D.Zero;
            _polygonDebugAxisA = Vector3D.Zero;
            _polygonDebugAxisB = Vector3D.Zero;
            _polygonDebugRootBlockEntity = null;
            _polygonDebugOwnerBlock = null;
            _polygonDebugTopLeft = Vector3D.Zero;
            _polygonDebugTopRight = Vector3D.Zero;
            _polygonDebugBottomRight = Vector3D.Zero;
            _polygonDebugBottomLeft = Vector3D.Zero;
            _polygonDebugMinA = 0.0;
            _polygonDebugMaxA = 0.0;
            _polygonDebugMinB = 0.0;
            _polygonDebugMaxB = 0.0;
            _polygonDebugOcclusionEntities.Clear();
            _polygonDebugWorldOccluderTriangles.Clear();
            _polygonDebugTriangles.Clear();

            IMyEntity entity;
            IMyCubeBlock ownerBlock;
            Vector3D seed;
            Vector3D normal;
            if (_polygonDebugAlternateAimMode)
            {
                if (!TryGetPolygonDebugAltAimHit(out entity, out ownerBlock, out seed, out normal))
                {
                    return false;
                }
            }
            else if (!TryGetPolygonDebugAimHit(out entity, out ownerBlock, out seed, out normal))
            {
                return false;
            }

            if (normal.LengthSquared() <= 0.000001)
                return false;
            normal.Normalize();
            if (MyAPIGateway.Session != null && MyAPIGateway.Session.Camera != null &&
                Vector3D.Dot(MyAPIGateway.Session.Camera.WorldMatrix.Translation - seed, normal) < 0.0)
            {
                normal = -normal;
            }

            Vector3D axisA;
            Vector3D axisB;
            bool useAltFrame = _polygonDebugAlternateAimMode &&
                _hasPolygonDebugAltAimFrame &&
                _polygonDebugAltAimAxisA.LengthSquared() > 0.000001 &&
                _polygonDebugAltAimAxisB.LengthSquared() > 0.000001;
            if (useAltFrame)
            {
                axisA = _polygonDebugAltAimAxisA;
                axisB = _polygonDebugAltAimAxisB;
                axisA.Normalize();
                axisB.Normalize();
            }
            else if (!TryBuildGlobalModelWalkAxes(entity, normal, out axisA, out axisB))
                return false;

            _polygonDebugEntity = entity;
            _polygonDebugRootBlockEntity = ResolvePolygonDebugRootBlockEntity(entity);
            _polygonDebugOwnerBlock = ownerBlock ?? ResolvePolygonDebugOwnerBlock(entity);
            if (_polygonDebugOwnerBlock != null)
                _polygonDebugRootBlockEntity = _polygonDebugOwnerBlock as IMyEntity;
            ClampPolygonDebugSelectedScreenIndexToOwner();
            _polygonDebugSeed = seed;
            _polygonDebugNormal = normal;
            _polygonDebugAxisA = axisA;
            _polygonDebugAxisB = axisB;

            if (useAltFrame)
            {
                _polygonDebugManualBoundsMode = true;
                _polygonDebugFallbackBoundsMode = false;
                SetPolygonDebugLocalBounds(_polygonDebugAltAimMinA, _polygonDebugAltAimMaxA, _polygonDebugAltAimMinB, _polygonDebugAltAimMaxB);
                CollectPolygonDebugOcclusionEntities();
                CollectPolygonDebugSpatialOccluders();
                return true;
            }

            if (_polygonDebugManualBoundsMode)
            {
                return ApplyPolygonDebugFallbackBoundsToCurrentSurface();
            }

            double minA = -WalkRealPolygonSurfaceDistance(seed, normal, -axisA);
            double maxA = WalkRealPolygonSurfaceDistance(seed, normal, axisA);
            double minB = -WalkRealPolygonSurfaceDistance(seed, normal, -axisB);
            double maxB = WalkRealPolygonSurfaceDistance(seed, normal, axisB);

            if (maxA - minA < PolygonDebugMinimumSize || maxB - minB < PolygonDebugMinimumSize)
                return ApplyPolygonDebugFallbackBoundsToCurrentSurface();

            _polygonDebugFallbackBoundsMode = false;

            _polygonDebugTopLeft = seed + axisA * minA + axisB * maxB;
            _polygonDebugTopRight = seed + axisA * maxA + axisB * maxB;
            _polygonDebugBottomRight = seed + axisA * maxA + axisB * minB;
            _polygonDebugBottomLeft = seed + axisA * minA + axisB * minB;
            _polygonDebugMinA = minA;
            _polygonDebugMaxA = maxA;
            _polygonDebugMinB = minB;
            _polygonDebugMaxB = maxB;
            CollectPolygonDebugTriangles();
            CollectPolygonDebugOcclusionEntities();
            CollectPolygonDebugNeighborProbeEntities();
            CollectPolygonDebugCameraRayHarvestEntities();
            CollectPolygonDebugSpatialOccluders();
            RefinePolygonDebugBoundsWithOcclusionEntities();
            return true;
        }

        bool ApplyPolygonDebugFallbackBoundsToCurrentSurface()
        {
            _polygonDebugManualBoundsMode = true;
            _polygonDebugFallbackBoundsMode = true;
            ApplyPolygonDebugManualBoundsToCurrentSurface();
            CollectPolygonDebugOcclusionEntities();
            CollectPolygonDebugNeighborProbeEntities();
            CollectPolygonDebugCameraRayHarvestEntities();
            CollectPolygonDebugSpatialOccluders();
            NoteManualCalibrationStatus(_polygonDebugOwnerBlock, _polygonDebugCalibrationScreenIndex, "FALLBACK MODE");
            return true;
        }

        void CollectPolygonDebugTriangles()
        {
            _polygonDebugTriangles.Clear();
            if (_polygonDebugEntity == null)
                return;

            try
            {
                MatrixD world = _polygonDebugEntity.WorldMatrix;
                MatrixD inverseWorld = MatrixD.Invert(_polygonDebugEntity.WorldMatrix);
                Vector3D localSeedD = Vector3D.Transform(_polygonDebugSeed, inverseWorld);
                Vector3D localNormalD = Vector3D.TransformNormal(_polygonDebugNormal, MatrixD.Transpose(_polygonDebugEntity.WorldMatrix));
                if (localNormalD.LengthSquared() <= 0.000001)
                    return;
                localNormalD.Normalize();

                var localSeed = new Vector3((float)localSeedD.X, (float)localSeedD.Y, (float)localSeedD.Z);
                var localNormal = new Vector3((float)localNormalD.X, (float)localNormalD.Y, (float)localNormalD.Z);
                var sphere = new BoundingSphere(localSeed, PolygonDebugTriangleProbeRadius);
                Vector3? normalFilter = localNormal;
                float? maxAngle = null;
                _polygonDebugEntity.GetTrianglesIntersectingSphere(ref sphere, normalFilter, maxAngle, _polygonDebugTriangles, PolygonDebugMaxProbeTriangles);

                for (int i = _polygonDebugTriangles.Count - 1; i >= 0; i--)
                {
                    if (!IsPolygonDebugTriangleOnSeedSurface(_polygonDebugTriangles[i], localSeed, localNormal))
                        _polygonDebugTriangles.RemoveAt(i);
                }

                KeepConnectedPolygonDebugTriangles(world);
                RebuildPolygonDebugBoundsFromTriangles();
            }
            catch
            {
                _polygonDebugTriangles.Clear();
            }
        }

        bool TryRebuildPolygonDebugAxesFromTriangleCloud(MatrixD world)
        {
            if (_polygonDebugTriangles.Count == 0 || _polygonDebugNormal.LengthSquared() <= 0.000001)
                return false;

            Vector3D normal = _polygonDebugNormal;
            normal.Normalize();

            Vector3D tempAxisA = _polygonDebugAxisA;
            tempAxisA -= normal * Vector3D.Dot(tempAxisA, normal);
            if (tempAxisA.LengthSquared() <= 0.000001)
            {
                tempAxisA = Vector3D.CalculatePerpendicularVector(normal);
            }

            tempAxisA.Normalize();

            Vector3D tempAxisB = Vector3D.Cross(normal, tempAxisA);
            if (tempAxisB.LengthSquared() <= 0.000001)
                return false;
            tempAxisB.Normalize();

            int vertexCount = 0;
            double meanA = 0.0;
            double meanB = 0.0;
            for (int i = 0; i < _polygonDebugTriangles.Count; i++)
            {
                MyTriangle_Vertex_Normals triangle = _polygonDebugTriangles[i];
                AccumulatePolygonDebugAxisPoint(TransformPolygonDebugVertex(triangle.Vertices.Vertex0, world), tempAxisA, tempAxisB, ref vertexCount, ref meanA, ref meanB);
                AccumulatePolygonDebugAxisPoint(TransformPolygonDebugVertex(triangle.Vertices.Vertex1, world), tempAxisA, tempAxisB, ref vertexCount, ref meanA, ref meanB);
                AccumulatePolygonDebugAxisPoint(TransformPolygonDebugVertex(triangle.Vertices.Vertex2, world), tempAxisA, tempAxisB, ref vertexCount, ref meanA, ref meanB);
            }

            if (vertexCount < 3)
                return false;

            meanA /= vertexCount;
            meanB /= vertexCount;

            double covarianceAA = 0.0;
            double covarianceBB = 0.0;
            double covarianceAB = 0.0;
            for (int i = 0; i < _polygonDebugTriangles.Count; i++)
            {
                MyTriangle_Vertex_Normals triangle = _polygonDebugTriangles[i];
                AccumulatePolygonDebugAxisCovariance(TransformPolygonDebugVertex(triangle.Vertices.Vertex0, world), tempAxisA, tempAxisB, meanA, meanB, ref covarianceAA, ref covarianceBB, ref covarianceAB);
                AccumulatePolygonDebugAxisCovariance(TransformPolygonDebugVertex(triangle.Vertices.Vertex1, world), tempAxisA, tempAxisB, meanA, meanB, ref covarianceAA, ref covarianceBB, ref covarianceAB);
                AccumulatePolygonDebugAxisCovariance(TransformPolygonDebugVertex(triangle.Vertices.Vertex2, world), tempAxisA, tempAxisB, meanA, meanB, ref covarianceAA, ref covarianceBB, ref covarianceAB);
            }

            if (covarianceAA + covarianceBB <= 0.00000001)
                return false;

            double angle = 0.5 * Math.Atan2(2.0 * covarianceAB, covarianceAA - covarianceBB);
            Vector3D axisA = tempAxisA * Math.Cos(angle) + tempAxisB * Math.Sin(angle);
            axisA -= normal * Vector3D.Dot(axisA, normal);
            if (axisA.LengthSquared() <= 0.000001)
                return false;
            axisA.Normalize();

            if (_polygonDebugAxisA.LengthSquared() > 0.000001 && Vector3D.Dot(axisA, _polygonDebugAxisA) < 0.0)
                axisA = -axisA;

            Vector3D axisB = Vector3D.Cross(normal, axisA);
            if (axisB.LengthSquared() <= 0.000001)
                return false;
            axisB.Normalize();

            _polygonDebugAxisA = axisA;
            _polygonDebugAxisB = axisB;
            return true;
        }

        void AccumulatePolygonDebugAxisPoint(Vector3D point, Vector3D axisA, Vector3D axisB, ref int count, ref double sumA, ref double sumB)
        {
            Vector3D delta = point - _polygonDebugSeed;
            sumA += Vector3D.Dot(delta, axisA);
            sumB += Vector3D.Dot(delta, axisB);
            count++;
        }

        void AccumulatePolygonDebugAxisCovariance(Vector3D point, Vector3D axisA, Vector3D axisB, double meanA, double meanB, ref double covarianceAA, ref double covarianceBB, ref double covarianceAB)
        {
            Vector3D delta = point - _polygonDebugSeed;
            double a = Vector3D.Dot(delta, axisA) - meanA;
            double b = Vector3D.Dot(delta, axisB) - meanB;
            covarianceAA += a * a;
            covarianceBB += b * b;
            covarianceAB += a * b;
        }

        void RebuildPolygonDebugBoundsFromTriangles()
        {
            if (_polygonDebugEntity == null || _polygonDebugTriangles.Count == 0)
                return;

            MatrixD world = _polygonDebugEntity.WorldMatrix;
            bool found = false;
            double minA = 0.0;
            double maxA = 0.0;
            double minB = 0.0;
            double maxB = 0.0;

            for (int i = 0; i < _polygonDebugTriangles.Count; i++)
            {
                MyTriangle_Vertex_Normals triangle = _polygonDebugTriangles[i];
                IncludePolygonDebugBoundsVertex(TransformPolygonDebugVertex(triangle.Vertices.Vertex0, world), ref found, ref minA, ref maxA, ref minB, ref maxB);
                IncludePolygonDebugBoundsVertex(TransformPolygonDebugVertex(triangle.Vertices.Vertex1, world), ref found, ref minA, ref maxA, ref minB, ref maxB);
                IncludePolygonDebugBoundsVertex(TransformPolygonDebugVertex(triangle.Vertices.Vertex2, world), ref found, ref minA, ref maxA, ref minB, ref maxB);
            }

            if (!found || maxA - minA < PolygonDebugMinimumSize || maxB - minB < PolygonDebugMinimumSize)
                return;

            _polygonDebugTopLeft = _polygonDebugSeed + _polygonDebugAxisA * minA + _polygonDebugAxisB * maxB;
            _polygonDebugTopRight = _polygonDebugSeed + _polygonDebugAxisA * maxA + _polygonDebugAxisB * maxB;
            _polygonDebugBottomRight = _polygonDebugSeed + _polygonDebugAxisA * maxA + _polygonDebugAxisB * minB;
            _polygonDebugBottomLeft = _polygonDebugSeed + _polygonDebugAxisA * minA + _polygonDebugAxisB * minB;
            _polygonDebugMinA = minA;
            _polygonDebugMaxA = maxA;
            _polygonDebugMinB = minB;
            _polygonDebugMaxB = maxB;
        }

        void ResetPolygonDebugCalibrationAdjustments()
        {
            _polygonDebugCalibrationOffsetA = 0.0;
            _polygonDebugCalibrationOffsetB = 0.0;
            _polygonDebugCalibrationScaleA = 1.0;
            _polygonDebugCalibrationScaleB = 1.0;
            _polygonDebugCalibrationDepthOffset = PanelDiscoveryCursorSurfaceOffset;
        }

        void ApplyPolygonDebugManualBoundsToCurrentSurface()
        {
            SetPolygonDebugLocalBounds(-_polygonDebugManualHalfA, _polygonDebugManualHalfA, -_polygonDebugManualHalfB, _polygonDebugManualHalfB);
        }

        void SetPolygonDebugLocalBounds(double minA, double maxA, double minB, double maxB)
        {
            _polygonDebugTopLeft = _polygonDebugSeed + _polygonDebugAxisA * minA + _polygonDebugAxisB * maxB;
            _polygonDebugTopRight = _polygonDebugSeed + _polygonDebugAxisA * maxA + _polygonDebugAxisB * maxB;
            _polygonDebugBottomRight = _polygonDebugSeed + _polygonDebugAxisA * maxA + _polygonDebugAxisB * minB;
            _polygonDebugBottomLeft = _polygonDebugSeed + _polygonDebugAxisA * minA + _polygonDebugAxisB * minB;
            _polygonDebugMinA = minA;
            _polygonDebugMaxA = maxA;
            _polygonDebugMinB = minB;
            _polygonDebugMaxB = maxB;
        }

        void ClampPolygonDebugCalibrationScales()
        {
            if (_polygonDebugCalibrationScaleA < 0.05)
                _polygonDebugCalibrationScaleA = 0.05;
            if (_polygonDebugCalibrationScaleB < 0.05)
                _polygonDebugCalibrationScaleB = 0.05;
        }

        void GetPolygonDebugFinalBounds(out double minA, out double maxA, out double minB, out double maxB)
        {
            ClampPolygonDebugCalibrationScales();

            double centerA = (_polygonDebugMinA + _polygonDebugMaxA) * 0.5 + _polygonDebugCalibrationOffsetA;
            double centerB = (_polygonDebugMinB + _polygonDebugMaxB) * 0.5 + _polygonDebugCalibrationOffsetB;
            double halfA = (_polygonDebugMaxA - _polygonDebugMinA) * 0.5 * _polygonDebugCalibrationScaleA;
            double halfB = (_polygonDebugMaxB - _polygonDebugMinB) * 0.5 * _polygonDebugCalibrationScaleB;

            minA = centerA - halfA;
            maxA = centerA + halfA;
            minB = centerB - halfB;
            maxB = centerB + halfB;
        }

        void GetPolygonDebugEffectiveCullingBounds(out double minA, out double maxA, out double minB, out double maxB)
        {
            GetPolygonDebugFinalBounds(out minA, out maxA, out minB, out maxB);
            ApplyPanelCursorCalibrationBorderInset(ref minA, ref maxA, ref minB, ref maxB);
        }

        void GetPolygonDebugFinalCorners(out Vector3D topLeft, out Vector3D topRight, out Vector3D bottomRight, out Vector3D bottomLeft)
        {
            double minA;
            double maxA;
            double minB;
            double maxB;
            GetPolygonDebugEffectiveCullingBounds(out minA, out maxA, out minB, out maxB);

            topLeft = _polygonDebugSeed + _polygonDebugAxisA * minA + _polygonDebugAxisB * maxB;
            topRight = _polygonDebugSeed + _polygonDebugAxisA * maxA + _polygonDebugAxisB * maxB;
            bottomRight = _polygonDebugSeed + _polygonDebugAxisA * maxA + _polygonDebugAxisB * minB;
            bottomLeft = _polygonDebugSeed + _polygonDebugAxisA * minA + _polygonDebugAxisB * minB;
        }

        static bool TryParsePolygonDebugDouble(string text, out double value)
        {
            return double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        static string FormatPolygonDebugDouble(double value)
        {
            return value.ToString("R", CultureInfo.InvariantCulture);
        }

        static string FormatPolygonDebugVector(Vector3D value)
        {
            return "new Vector3D(" +
                FormatPolygonDebugDouble(value.X) + ", " +
                FormatPolygonDebugDouble(value.Y) + ", " +
                FormatPolygonDebugDouble(value.Z) + ")";
        }

        void CopyPolygonDebugCalibrationToClipboard()
        {
            if (!_hasPolygonDebugSurface)
            {
                ShowPolygonDebugMessage("No polygon panel debug surface to export");
                return;
            }

            string singleEntry = BuildPolygonDebugCalibrationText();
            string text = BuildBlockPanelCalibrationExportText(_polygonDebugOwnerBlock, _polygonDebugCalibrationScreenIndex, ExtractScreenCalibrationEntry(singleEntry, _polygonDebugCalibrationScreenIndex));
            try
            {
                if (MyAPIGateway.Utilities == null)
                    return;

                TryWritePanelCalibrationLocalStorage(PanelCalibrationBlockExportFileName, text);

                bool copiedToClipboard = TryCopyTextToSystemClipboard(text);
                NoteManualCalibrationExport(_polygonDebugOwnerBlock, _polygonDebugCalibrationScreenIndex);
                NoteManualCalibrationStatus(_polygonDebugOwnerBlock, _polygonDebugCalibrationScreenIndex, "CALIBRATION EXPORTED");
                ShowPolygonDebugMessage(copiedToClipboard ? "Polygon panel calibration copied to clipboard" : "Polygon panel calibration exported to local storage file");
            }
            catch
            {
                ShowPolygonDebugMessage("Could not export calibration");
            }
        }

        void CopyWorldPanelCalibrationCatalogToClipboard()
        {
            string text;
            if (!TryReadPanelCalibrationCatalog(out text))
                text = string.Empty;

            if (string.IsNullOrWhiteSpace(text))
            {
                ShowPolygonDebugMessage("No saved panel calibrations in catalog");
                return;
            }

            try
            {
                if (MyAPIGateway.Utilities == null)
                    return;

                bool copiedToClipboard = TryCopyTextToSystemClipboard(text);
                NoteManualCalibrationStatus(_polygonDebugOwnerBlock, _polygonDebugCalibrationScreenIndex, "CATALOG COPY REQUESTED");
                ShowPolygonDebugMessage(copiedToClipboard ? "Panel calibration catalog copied to clipboard" : "Panel calibration catalog is the local storage source of truth");
            }
            catch
            {
                ShowPolygonDebugMessage("Could not copy panel calibration catalog");
            }
        }

        bool SavePolygonDebugCalibrationToCatalog()
        {
            if (!_hasPolygonDebugSurface)
            {
                ShowPolygonDebugMessage("No polygon panel debug surface to save");
                return false;
            }

            string text = BuildPolygonDebugCalibrationText();
            try
            {
                if (MyAPIGateway.Utilities == null)
                    return false;

                if (_polygonDebugCalibrationScreenIndex < 0)
                {
                    ShowPolygonDebugMessage("No screen index selected for calibration save");
                    return false;
                }

                StoreCurrentPolygonDebugDraft();
                var draftIndexes = new List<int>();
                foreach (var pair in _polygonDebugCalibrationDrafts)
                {
                    PolygonDebugCalibrationDraft draft = pair.Value;
                    if (draft != null &&
                        _polygonDebugOwnerBlock != null &&
                        draft.BlockEntityId == _polygonDebugOwnerBlock.EntityId &&
                        draft.SurfaceIndex >= 0 &&
                        !draftIndexes.Contains(draft.SurfaceIndex))
                    {
                        draftIndexes.Add(draft.SurfaceIndex);
                    }
                }

                draftIndexes.Sort();
                if (draftIndexes.Count == 0)
                    draftIndexes.Add(_polygonDebugCalibrationScreenIndex);

                string existing = string.Empty;
                try
                {
                    using (var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(PanelCalibrationCatalogFileName, typeof(GridSchematicsSession)))
                    {
                        existing = reader.ReadToEnd();
                    }
                }
                catch
                {
                    existing = string.Empty;
                }

                string compacted = existing;
                string blockDefinition = GetBlockDefinitionId(_polygonDebugOwnerBlock);
                for (int i = 0; i < draftIndexes.Count; i++)
                {
                    if (!string.IsNullOrEmpty(compacted))
                        RemovePanelCursorCalibrationCatalogEntry(compacted, blockDefinition, draftIndexes[i], out compacted);
                }

                var entriesToSave = new List<string>();
                for (int i = 0; i < draftIndexes.Count; i++)
                {
                    PolygonDebugCalibrationDraft draft;
                    string key = GetPolygonDebugDraftKey(_polygonDebugOwnerBlock, draftIndexes[i]);
                    if (!string.IsNullOrEmpty(key) && _polygonDebugCalibrationDrafts.TryGetValue(key, out draft) && draft != null)
                    {
                        entriesToSave.Add(BuildPolygonDebugCalibrationTextFromDraft(_polygonDebugOwnerBlock, draft));
                    }
                }

                if (entriesToSave.Count == 0)
                    entriesToSave.Add(text);

                using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(PanelCalibrationCatalogFileName, typeof(GridSchematicsSession)))
                {
                    if (!string.IsNullOrEmpty(compacted))
                    {
                        writer.Write(compacted);
                        if (!compacted.EndsWith("\n", StringComparison.Ordinal))
                            writer.WriteLine();
                        writer.WriteLine();
                    }

                    for (int i = 0; i < entriesToSave.Count; i++)
                    {
                        writer.WriteLine(PanelCalibrationCatalogSeparator);
                        writer.Write(entriesToSave[i]);
                        writer.WriteLine();
                    }
                }

                string exportText = BuildBlockPanelCalibrationExportText(_polygonDebugOwnerBlock, -1, null);
                TryWritePanelCalibrationLocalStorage(PanelCalibrationBlockExportFileName, exportText);

                int appliedCount = ApplyPolygonDebugDraftsToMatchingInputs(_polygonDebugOwnerBlock, draftIndexes);
                NoteManualCalibrationStatus(_polygonDebugOwnerBlock, _polygonDebugCalibrationScreenIndex, "BLOCK CALIBRATIONS SAVED");
                ShowPolygonDebugMessage("Block calibration drafts saved to local catalog (" + entriesToSave.Count.ToString(CultureInfo.InvariantCulture) + " screen" + (entriesToSave.Count == 1 ? "" : "s") + ")" + (appliedCount > 0 ? " and applied" : ""));
                return true;
            }
            catch
            {
                ShowPolygonDebugMessage("Could not save calibration");
                return false;
            }
        }

        int ApplyPolygonDebugCalibrationToMatchingInputs()
        {
            if (!_hasPolygonDebugSurface || _polygonDebugOwnerBlock == null)
                return 0;

            double finalMinA;
            double finalMaxA;
            double finalMinB;
            double finalMaxB;
            GetPolygonDebugEffectiveCullingBounds(out finalMinA, out finalMaxA, out finalMinB, out finalMaxB);

            int applied = 0;
            applied += ApplyPolygonDebugCalibrationToMatchingApps(_apps, finalMinA, finalMaxA, finalMinB, finalMaxB);
            applied += ApplyPolygonDebugCalibrationToMatchingApps(_surfaceScriptApps, finalMinA, finalMaxA, finalMinB, finalMaxB);

            for (int i = _unsupportedSurfaceProbes.Count - 1; i >= 0; i--)
            {
                TouchScreenApiAdapter input = _unsupportedSurfaceProbes[i];
                if (input == null || input.OwnerBlock == null || input.OwnerBlock.MarkedForClose)
                {
                    _unsupportedSurfaceProbes.RemoveAt(i);
                    continue;
                }

                if (DoesPolygonDebugInputMatchOwner(input))
                {
                    input.SetManualPanelCursorSurface(GetPolygonDebugCalibrationPlaneSeed(), _polygonDebugNormal, _polygonDebugAxisA, _polygonDebugAxisB, finalMinA, finalMaxA, finalMinB, finalMaxB);
                    applied++;
                }
            }

            return applied;
        }

        int ApplyPolygonDebugDraftsToMatchingInputs(IMyCubeBlock ownerBlock, List<int> draftIndexes)
        {
            if (ownerBlock == null || draftIndexes == null)
                return 0;

            int applied = 0;
            for (int i = 0; i < draftIndexes.Count; i++)
            {
                string key = GetPolygonDebugDraftKey(ownerBlock, draftIndexes[i]);
                PolygonDebugCalibrationDraft draft;
                if (string.IsNullOrEmpty(key) || !_polygonDebugCalibrationDrafts.TryGetValue(key, out draft) || draft == null)
                    continue;

                applied += ApplyPolygonDebugDraftToMatchingInputs(ownerBlock, draft);
            }

            return applied;
        }

        int ApplyPolygonDebugDraftToMatchingInputs(IMyCubeBlock ownerBlock, PolygonDebugCalibrationDraft draft)
        {
            if (ownerBlock == null || draft == null || draft.SurfaceIndex < 0)
                return 0;

            Vector3D seed;
            Vector3D normal;
            Vector3D axisA;
            Vector3D axisB;
            double finalMinA;
            double finalMaxA;
            double finalMinB;
            double finalMaxB;
            if (!TryGetPolygonDebugDraftWorldValues(ownerBlock, draft, out seed, out normal, out axisA, out axisB, out finalMinA, out finalMaxA, out finalMinB, out finalMaxB))
                return 0;

            int applied = 0;
            applied += ApplyPolygonDebugDraftToMatchingApps(_apps, ownerBlock, draft.SurfaceIndex, seed, normal, axisA, axisB, finalMinA, finalMaxA, finalMinB, finalMaxB);
            applied += ApplyPolygonDebugDraftToMatchingApps(_surfaceScriptApps, ownerBlock, draft.SurfaceIndex, seed, normal, axisA, axisB, finalMinA, finalMaxA, finalMinB, finalMaxB);

            for (int i = _unsupportedSurfaceProbes.Count - 1; i >= 0; i--)
            {
                TouchScreenApiAdapter input = _unsupportedSurfaceProbes[i];
                if (input == null || input.OwnerBlock == null || input.OwnerBlock.MarkedForClose)
                {
                    _unsupportedSurfaceProbes.RemoveAt(i);
                    continue;
                }

                if (input.OwnerBlock.EntityId == ownerBlock.EntityId && input.GetSurfaceIndex() == draft.SurfaceIndex)
                {
                    input.SetManualPanelCursorSurface(seed, normal, axisA, axisB, finalMinA, finalMaxA, finalMinB, finalMaxB);
                    applied++;
                }
            }

            return applied;
        }

        int ApplyPolygonDebugDraftToMatchingApps(List<GridSchematicsLcdApp> apps, IMyCubeBlock ownerBlock, int surfaceIndex, Vector3D seed, Vector3D normal, Vector3D axisA, Vector3D axisB, double finalMinA, double finalMaxA, double finalMinB, double finalMaxB)
        {
            if (apps == null || ownerBlock == null || surfaceIndex < 0)
                return 0;

            int applied = 0;
            for (int i = apps.Count - 1; i >= 0; i--)
            {
                GridSchematicsLcdApp app = apps[i];
                if (app == null || !app.IsOwnerFunctional)
                {
                    apps.RemoveAt(i);
                    continue;
                }

                TouchScreenApiAdapter input = app.TouchInput;
                if (input == null || input.OwnerBlock == null)
                    continue;

                if (input.OwnerBlock.EntityId == ownerBlock.EntityId && input.GetSurfaceIndex() == surfaceIndex)
                {
                    input.SetManualPanelCursorSurface(seed, normal, axisA, axisB, finalMinA, finalMaxA, finalMinB, finalMaxB);
                    app.NoteManualLowLevelCalibrationApplied();
                    applied++;
                }
            }

            return applied;
        }

        int ApplyPolygonDebugCalibrationToMatchingApps(List<GridSchematicsLcdApp> apps, double finalMinA, double finalMaxA, double finalMinB, double finalMaxB)
        {
            if (apps == null)
                return 0;

            int applied = 0;
            for (int i = apps.Count - 1; i >= 0; i--)
            {
                GridSchematicsLcdApp app = apps[i];
                if (app == null || !app.IsOwnerFunctional)
                {
                    apps.RemoveAt(i);
                    continue;
                }

                TouchScreenApiAdapter input = app.TouchInput;
                if (DoesPolygonDebugInputMatchOwner(input))
                {
                    input.SetManualPanelCursorSurface(GetPolygonDebugCalibrationPlaneSeed(), _polygonDebugNormal, _polygonDebugAxisA, _polygonDebugAxisB, finalMinA, finalMaxA, finalMinB, finalMaxB);
                    app.NoteManualLowLevelCalibrationApplied();
                    applied++;
                }
            }

            return applied;
        }

        bool DoesPolygonDebugInputMatchOwner(TouchScreenApiAdapter input)
        {
            if (input == null || input.OwnerBlock == null || _polygonDebugOwnerBlock == null)
                return false;

            if (input.OwnerBlock.EntityId != _polygonDebugOwnerBlock.EntityId)
                return false;

            if (_polygonDebugCalibrationScreenIndex < 0)
                return false;

            return input.GetSurfaceIndex() == _polygonDebugCalibrationScreenIndex;
        }

        string BuildPolygonDebugCalibrationText()
        {
            string blockDefinition = "Unknown";
            string blockDisplayName = "Unknown";
            long blockEntityId = 0L;
            int surfaceCount = -1;
            if (_polygonDebugOwnerBlock != null)
            {
                blockEntityId = _polygonDebugOwnerBlock.EntityId;
                var blockEntity = _polygonDebugOwnerBlock as IMyEntity;
                if (blockEntity != null && !string.IsNullOrEmpty(blockEntity.DisplayName))
                    blockDisplayName = blockEntity.DisplayName;

                if (_polygonDebugOwnerBlock.SlimBlock != null && _polygonDebugOwnerBlock.SlimBlock.BlockDefinition != null)
                    blockDefinition = _polygonDebugOwnerBlock.SlimBlock.BlockDefinition.Id.ToString();

                var surfaceProvider = _polygonDebugOwnerBlock as Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;
                if (surfaceProvider != null)
                    surfaceCount = surfaceProvider.SurfaceCount;
            }

            IMyEntity basisEntity = _polygonDebugRootBlockEntity ?? _polygonDebugEntity;
            MatrixD inverseBasis = basisEntity == null ? MatrixD.Identity : MatrixD.Invert(basisEntity.WorldMatrix);
            MatrixD inverseBasisNormal = basisEntity == null ? MatrixD.Identity : MatrixD.Transpose(basisEntity.WorldMatrix);

            Vector3D localSeed = Vector3D.Transform(_polygonDebugSeed, inverseBasis);
            Vector3D localNormal = Vector3D.TransformNormal(_polygonDebugNormal, inverseBasisNormal);
            Vector3D localAxisA = Vector3D.TransformNormal(_polygonDebugAxisA, inverseBasisNormal);
            Vector3D localAxisB = Vector3D.TransformNormal(_polygonDebugAxisB, inverseBasisNormal);

            double finalMinA;
            double finalMaxA;
            double finalMinB;
            double finalMaxB;
            GetPolygonDebugFinalBounds(out finalMinA, out finalMaxA, out finalMinB, out finalMaxB);

            return
                "GridSchematicsPanelCursorCalibrationBlock\n" +
                "BlockDefinitionId=" + blockDefinition + "\n" +
                "BlockDisplayName=" + blockDisplayName + "\n" +
                "DebugBlockEntityId=" + blockEntityId.ToString(CultureInfo.InvariantCulture) + "\n" +
                "ModelEntityName=" + (_polygonDebugEntity == null ? "Unknown" : _polygonDebugEntity.DisplayName) + "\n" +
                "DetectedSurfaceCount=" + surfaceCount.ToString(CultureInfo.InvariantCulture) + "\n" +
                "\n" +
                "ScreenCalibration[" + _polygonDebugCalibrationScreenIndex.ToString(CultureInfo.InvariantCulture) + "]:\n" +
                "ScreenIndex=" + _polygonDebugCalibrationScreenIndex.ToString(CultureInfo.InvariantCulture) + "\n" +
                "CalibrationMode=" + (_polygonDebugManualBoundsMode ? "ManualRectangle" : "PolygonDiscovery") + "\n" +
                "SeedLocal=" + FormatPolygonDebugVector(localSeed) + "\n" +
                "NormalLocal=" + FormatPolygonDebugVector(localNormal) + "\n" +
                "AxisALocal=" + FormatPolygonDebugVector(localAxisA) + "\n" +
                "AxisBLocal=" + FormatPolygonDebugVector(localAxisB) + "\n" +
                "RawMinA=" + FormatPolygonDebugDouble(_polygonDebugMinA) + "\n" +
                "RawMaxA=" + FormatPolygonDebugDouble(_polygonDebugMaxA) + "\n" +
                "RawMinB=" + FormatPolygonDebugDouble(_polygonDebugMinB) + "\n" +
                "RawMaxB=" + FormatPolygonDebugDouble(_polygonDebugMaxB) + "\n" +
                "OffsetA=" + FormatPolygonDebugDouble(_polygonDebugCalibrationOffsetA) + "\n" +
                "OffsetB=" + FormatPolygonDebugDouble(_polygonDebugCalibrationOffsetB) + "\n" +
                "ScaleA=" + FormatPolygonDebugDouble(_polygonDebugCalibrationScaleA) + "\n" +
                "ScaleB=" + FormatPolygonDebugDouble(_polygonDebugCalibrationScaleB) + "\n" +
                "CursorDepthOffset=" + FormatPolygonDebugDouble(_polygonDebugCalibrationDepthOffset) + "\n" +
                "ManualWidthA=" + FormatPolygonDebugDouble(_polygonDebugManualHalfA * 2.0) + "\n" +
                "ManualHeightB=" + FormatPolygonDebugDouble(_polygonDebugManualHalfB * 2.0) + "\n" +
                "FinalMinA=" + FormatPolygonDebugDouble(finalMinA) + "\n" +
                "FinalMaxA=" + FormatPolygonDebugDouble(finalMaxA) + "\n" +
                "FinalMinB=" + FormatPolygonDebugDouble(finalMinB) + "\n" +
                "FinalMaxB=" + FormatPolygonDebugDouble(finalMaxB) + "\n";
        }

        string BuildPolygonDebugCalibrationTextFromDraft(IMyCubeBlock ownerBlock, PolygonDebugCalibrationDraft draft)
        {
            if (ownerBlock == null || draft == null)
                return string.Empty;

            string blockDefinition = GetBlockDefinitionId(ownerBlock);
            string blockDisplayName = "Unknown";
            long blockEntityId = ownerBlock.EntityId;
            int surfaceCount = GetBlockSurfaceCount(ownerBlock);
            var blockEntity = ownerBlock as IMyEntity;
            if (blockEntity != null && !string.IsNullOrEmpty(blockEntity.DisplayName))
                blockDisplayName = blockEntity.DisplayName;

            Vector3D localNormal = draft.NormalLocal;
            if (localNormal.LengthSquared() > 0.000001)
                localNormal.Normalize();

            Vector3D localAxisA = draft.AxisALocal;
            Vector3D localAxisB = draft.AxisBLocal;
            if (localAxisA.LengthSquared() > 0.000001)
                localAxisA.Normalize();
            if (localAxisB.LengthSquared() > 0.000001)
                localAxisB.Normalize();

            double finalMinA;
            double finalMaxA;
            double finalMinB;
            double finalMaxB;
            GetPolygonDebugDraftFinalBounds(draft, out finalMinA, out finalMaxA, out finalMinB, out finalMaxB);

            return
                "GridSchematicsPanelCursorCalibrationBlock\n" +
                "BlockDefinitionId=" + blockDefinition + "\n" +
                "BlockDisplayName=" + blockDisplayName + "\n" +
                "DebugBlockEntityId=" + blockEntityId.ToString(CultureInfo.InvariantCulture) + "\n" +
                "ModelEntityName=" + (blockEntity == null ? "Unknown" : blockEntity.DisplayName) + "\n" +
                "DetectedSurfaceCount=" + surfaceCount.ToString(CultureInfo.InvariantCulture) + "\n" +
                "\n" +
                "ScreenCalibration[" + draft.SurfaceIndex.ToString(CultureInfo.InvariantCulture) + "]:\n" +
                "ScreenIndex=" + draft.SurfaceIndex.ToString(CultureInfo.InvariantCulture) + "\n" +
                "CalibrationMode=" + (draft.ManualBoundsMode ? "ManualRectangle" : "PolygonDiscovery") + "\n" +
                "SeedLocal=" + FormatPolygonDebugVector(draft.SeedLocal) + "\n" +
                "NormalLocal=" + FormatPolygonDebugVector(localNormal) + "\n" +
                "AxisALocal=" + FormatPolygonDebugVector(localAxisA) + "\n" +
                "AxisBLocal=" + FormatPolygonDebugVector(localAxisB) + "\n" +
                "RawMinA=" + FormatPolygonDebugDouble(draft.MinA) + "\n" +
                "RawMaxA=" + FormatPolygonDebugDouble(draft.MaxA) + "\n" +
                "RawMinB=" + FormatPolygonDebugDouble(draft.MinB) + "\n" +
                "RawMaxB=" + FormatPolygonDebugDouble(draft.MaxB) + "\n" +
                "OffsetA=" + FormatPolygonDebugDouble(draft.OffsetA) + "\n" +
                "OffsetB=" + FormatPolygonDebugDouble(draft.OffsetB) + "\n" +
                "ScaleA=" + FormatPolygonDebugDouble(draft.ScaleA) + "\n" +
                "ScaleB=" + FormatPolygonDebugDouble(draft.ScaleB) + "\n" +
                "CursorDepthOffset=" + FormatPolygonDebugDouble(draft.DepthOffset) + "\n" +
                "ManualWidthA=" + FormatPolygonDebugDouble(draft.ManualHalfA * 2.0) + "\n" +
                "ManualHeightB=" + FormatPolygonDebugDouble(draft.ManualHalfB * 2.0) + "\n" +
                "FinalMinA=" + FormatPolygonDebugDouble(finalMinA) + "\n" +
                "FinalMaxA=" + FormatPolygonDebugDouble(finalMaxA) + "\n" +
                "FinalMinB=" + FormatPolygonDebugDouble(finalMinB) + "\n" +
                "FinalMaxB=" + FormatPolygonDebugDouble(finalMaxB) + "\n";
        }

        static void GetPolygonDebugDraftFinalBounds(PolygonDebugCalibrationDraft draft, out double minA, out double maxA, out double minB, out double maxB)
        {
            double scaleA = draft == null ? 1.0 : draft.ScaleA;
            double scaleB = draft == null ? 1.0 : draft.ScaleB;
            if (scaleA < 0.05)
                scaleA = 0.05;
            if (scaleB < 0.05)
                scaleB = 0.05;

            double rawMinA = draft == null ? 0.0 : draft.MinA;
            double rawMaxA = draft == null ? 0.0 : draft.MaxA;
            double rawMinB = draft == null ? 0.0 : draft.MinB;
            double rawMaxB = draft == null ? 0.0 : draft.MaxB;
            double centerA = (rawMinA + rawMaxA) * 0.5 + (draft == null ? 0.0 : draft.OffsetA);
            double centerB = (rawMinB + rawMaxB) * 0.5 + (draft == null ? 0.0 : draft.OffsetB);
            double halfA = (rawMaxA - rawMinA) * 0.5 * scaleA;
            double halfB = (rawMaxB - rawMinB) * 0.5 * scaleB;

            minA = centerA - halfA;
            maxA = centerA + halfA;
            minB = centerB - halfB;
            maxB = centerB + halfB;
        }

        static bool TryGetPolygonDebugDraftWorldValues(IMyCubeBlock ownerBlock, PolygonDebugCalibrationDraft draft, out Vector3D seed, out Vector3D normal, out Vector3D axisA, out Vector3D axisB, out double finalMinA, out double finalMaxA, out double finalMinB, out double finalMaxB)
        {
            seed = Vector3D.Zero;
            normal = Vector3D.Zero;
            axisA = Vector3D.Zero;
            axisB = Vector3D.Zero;
            finalMinA = 0.0;
            finalMaxA = 0.0;
            finalMinB = 0.0;
            finalMaxB = 0.0;

            if (ownerBlock == null || draft == null)
                return false;

            IMyEntity ownerEntity = ownerBlock as IMyEntity;
            if (ownerEntity == null)
                return false;

            Vector3D localSeed = draft.SeedLocal;
            Vector3D localNormal = draft.NormalLocal;
            if (localNormal.LengthSquared() > 0.000001)
            {
                localNormal.Normalize();
                localSeed += localNormal * draft.DepthOffset;
            }

            seed = Vector3D.Transform(localSeed, ownerEntity.WorldMatrix);
            normal = Vector3D.TransformNormal(localNormal, ownerEntity.WorldMatrix);
            axisA = Vector3D.TransformNormal(draft.AxisALocal, ownerEntity.WorldMatrix);
            axisB = Vector3D.TransformNormal(draft.AxisBLocal, ownerEntity.WorldMatrix);
            if (normal.LengthSquared() <= 0.000001 || axisA.LengthSquared() <= 0.000001 || axisB.LengthSquared() <= 0.000001)
                return false;

            normal.Normalize();
            axisA.Normalize();
            axisB.Normalize();
            GetPolygonDebugDraftFinalBounds(draft, out finalMinA, out finalMaxA, out finalMinB, out finalMaxB);
            return true;
        }

        public bool TryApplyStoredPanelCursorCalibration(TouchScreenApiAdapter input)
        {
            if (input == null || input.OwnerBlock == null || MyAPIGateway.Utilities == null)
                return false;

            string blockDefinition = GetBlockDefinitionId(input.OwnerBlock);
            if (string.IsNullOrEmpty(blockDefinition))
                return false;

            int surfaceIndex = input.GetSurfaceIndex();
            string catalog = string.Empty;
            if (!TryReadPanelCalibrationCatalog(out catalog))
                return false;

            if (string.IsNullOrEmpty(catalog))
                return false;

            string[] entries = catalog.Split(new string[] { PanelCalibrationCatalogSeparator }, StringSplitOptions.RemoveEmptyEntries);
            string best = null;
            for (int i = 0; i < entries.Length; i++)
            {
                string entry = entries[i];
                string entryDefinition = GetCalibrationCatalogValue(entry, "BlockDefinitionId");
                if (!string.Equals(entryDefinition, blockDefinition, StringComparison.Ordinal))
                    continue;

                int entryScreenIndex;
                if (!int.TryParse(GetCalibrationCatalogValue(entry, "ScreenIndex"), NumberStyles.Integer, CultureInfo.InvariantCulture, out entryScreenIndex))
                    entryScreenIndex = -1;

                if (entryScreenIndex >= 0 && surfaceIndex >= 0 && entryScreenIndex != surfaceIndex)
                    continue;

                best = entry;
            }

            if (best == null)
                return false;

            Vector3D seedLocal;
            Vector3D normalLocal;
            Vector3D axisALocal;
            Vector3D axisBLocal;
            double minA;
            double maxA;
            double minB;
            double maxB;
            if (!TryParseCalibrationCatalogVector(GetCalibrationCatalogValue(best, "SeedLocal"), out seedLocal) ||
                !TryParseCalibrationCatalogVector(GetCalibrationCatalogValue(best, "NormalLocal"), out normalLocal) ||
                !TryParseCalibrationCatalogVector(GetCalibrationCatalogValue(best, "AxisALocal"), out axisALocal) ||
                !TryParseCalibrationCatalogVector(GetCalibrationCatalogValue(best, "AxisBLocal"), out axisBLocal) ||
                !TryParsePolygonDebugDouble(GetCalibrationCatalogValue(best, "FinalMinA"), out minA) ||
                !TryParsePolygonDebugDouble(GetCalibrationCatalogValue(best, "FinalMaxA"), out maxA) ||
                !TryParsePolygonDebugDouble(GetCalibrationCatalogValue(best, "FinalMinB"), out minB) ||
                !TryParsePolygonDebugDouble(GetCalibrationCatalogValue(best, "FinalMaxB"), out maxB))
            {
                return false;
            }

            double depthOffset;
            if (!TryParsePolygonDebugDouble(GetCalibrationCatalogValue(best, "CursorDepthOffset"), out depthOffset))
                depthOffset = 0.0;

            double localDepthOffset;
            if (surfaceIndex >= 0 && TryReadPanelCursorDepthOffset(blockDefinition, surfaceIndex, out localDepthOffset))
            {
                depthOffset = localDepthOffset;
            }
            else if (Math.Abs(depthOffset) <= 0.000001)
            {
                double builtInDepthOffset;
                if (TryGetBuiltInPanelCursorDepthOffset(blockDefinition, surfaceIndex, out builtInDepthOffset) &&
                    Math.Abs(builtInDepthOffset) > 0.000001)
                {
                    depthOffset = builtInDepthOffset;
                }
            }

            if (Math.Abs(depthOffset) > 0.000001 && normalLocal.LengthSquared() > 0.000001)
            {
                Vector3D depthNormal = normalLocal;
                depthNormal.Normalize();
                seedLocal += depthNormal * depthOffset;
            }

            ApplyPanelCursorCalibrationBorderInset(ref minA, ref maxA, ref minB, ref maxB);
            input.SetManualPanelCursorSurfaceLocal(seedLocal, normalLocal, axisALocal, axisBLocal, minA, maxA, minB, maxB);
            return true;
        }

        static void ApplyPanelCursorCalibrationBorderInset(ref double minA, ref double maxA, ref double minB, ref double maxB)
        {
            double inset = PanelCursorCalibrationBorderInnerEdgeInset;
            if (maxA - minA > inset * 2.0)
            {
                minA += inset;
                maxA -= inset;
            }

            if (maxB - minB > inset * 2.0)
            {
                minB += inset;
                maxB -= inset;
            }
        }

        static bool TryGetBuiltInPanelCursorDepthOffset(string blockDefinition, int surfaceIndex, out double depthOffset)
        {
            depthOffset = 0.0;
            if (string.IsNullOrEmpty(blockDefinition) || string.IsNullOrWhiteSpace(BuiltInPanelCursorCalibrationCatalog))
                return false;

            bool found = false;
            string[] entries = BuiltInPanelCursorCalibrationCatalog.Split(new string[] { PanelCalibrationCatalogSeparator }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                string entry = entries[i];
                string entryDefinition = GetCalibrationCatalogValue(entry, "BlockDefinitionId");
                if (!string.Equals(entryDefinition, blockDefinition, StringComparison.Ordinal))
                    continue;

                int entryScreenIndex;
                if (!int.TryParse(GetCalibrationCatalogValue(entry, "ScreenIndex"), NumberStyles.Integer, CultureInfo.InvariantCulture, out entryScreenIndex))
                    entryScreenIndex = -1;

                if (entryScreenIndex >= 0 && surfaceIndex >= 0 && entryScreenIndex != surfaceIndex)
                    continue;

                double parsed;
                if (!TryParsePolygonDebugDouble(GetCalibrationCatalogValue(entry, "CursorDepthOffset"), out parsed))
                    continue;

                depthOffset = parsed;
                found = true;
            }

            return found;
        }

        static bool TryReadPanelCalibrationCatalog(out string catalog)
        {
            catalog = string.Empty;

            string localCatalog;
            bool readLocal = TryReadPanelCalibrationLocalStorage(out localCatalog);
            string builtInCatalog = BuiltInPanelCursorCalibrationCatalog ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(builtInCatalog))
            {
                catalog = builtInCatalog.Trim();
                if (!string.IsNullOrWhiteSpace(localCatalog))
                {
                    if (!catalog.EndsWith("\n", StringComparison.Ordinal))
                        catalog += "\n";
                    catalog += localCatalog.Trim();
                }

                return true;
            }

            catalog = localCatalog ?? string.Empty;
            return readLocal;
        }

        static bool TryReadPanelCalibrationLocalStorage(out string catalog)
        {
            catalog = string.Empty;
            if (MyAPIGateway.Utilities == null)
                return false;

            try
            {
                using (TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(PanelCalibrationCatalogFileName, typeof(GridSchematicsSession)))
                {
                    catalog = reader.ReadToEnd();
                }

                return true;
            }
            catch
            {
                catalog = string.Empty;
                return false;
            }
        }

        static string GetBlockDefinitionId(IMyCubeBlock block)
        {
            if (block == null || block.SlimBlock == null || block.SlimBlock.BlockDefinition == null)
                return string.Empty;

            return block.SlimBlock.BlockDefinition.Id.ToString();
        }

        static string GetCalibrationCatalogValue(string entry, string key)
        {
            if (entry == null || key == null)
                return string.Empty;

            string[] lines = entry.Replace("\r", string.Empty).Split('\n');
            string prefix = key + "=";
            for (int i = lines.Length - 1; i >= 0; i--)
            {
                string line = lines[i];
                if (line != null && line.StartsWith(prefix, StringComparison.Ordinal))
                    return line.Substring(prefix.Length).Trim();
            }

            return string.Empty;
        }

        static bool TryParseCalibrationCatalogVector(string text, out Vector3D value)
        {
            value = Vector3D.Zero;
            if (string.IsNullOrEmpty(text))
                return false;

            int start = text.IndexOf('(');
            int end = text.LastIndexOf(')');
            if (start < 0 || end <= start)
                return false;

            string[] parts = text.Substring(start + 1, end - start - 1).Split(',');
            if (parts.Length < 3)
                return false;

            double x;
            double y;
            double z;
            if (!TryParsePolygonDebugDouble(parts[0].Trim(), out x) ||
                !TryParsePolygonDebugDouble(parts[1].Trim(), out y) ||
                !TryParsePolygonDebugDouble(parts[2].Trim(), out z))
            {
                return false;
            }

            value = new Vector3D(x, y, z);
            return true;
        }

        void ShowPolygonDebugMessage(string text)
        {
            try
            {
                if (MyAPIGateway.Utilities != null)
                {
                    MyAPIGateway.Utilities.ShowMessage("Grid Schematics", text);
                    MyAPIGateway.Utilities.ShowNotification(text, 1600, "White");
                }
            }
            catch
            {
            }
        }

        void CollectPolygonDebugOcclusionEntities()
        {
            _polygonDebugOcclusionEntities.Clear();
            AddPolygonDebugEntityTree(_polygonDebugRootBlockEntity);
            if (_polygonDebugEntity != null && (_polygonDebugRootBlockEntity == null || _polygonDebugEntity.EntityId != _polygonDebugRootBlockEntity.EntityId))
                AddPolygonDebugEntityTree(_polygonDebugEntity);
        }

        IMyEntity ResolvePolygonDebugRootBlockEntity(IMyEntity entity)
        {
            IMyEntity fallback = entity;
            MyEntity current = entity as MyEntity;
            for (int i = 0; i < 16 && current != null; i++)
            {
                if (current is IMyCubeBlock)
                    return current;

                fallback = current;
                current = current.Parent;
            }

            return fallback;
        }

        bool TryGetPolygonDebugAltAimHit(out IMyEntity modelEntity, out IMyCubeBlock ownerBlock, out Vector3D hitPosition, out Vector3D hitNormal)
        {
            modelEntity = null;
            ownerBlock = null;
            hitPosition = Vector3D.Zero;
            hitNormal = Vector3D.Zero;
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return false;

            var camera = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D rayOrigin = camera.Translation;
            Vector3D rayDirection = camera.Forward;
            if (rayDirection.LengthSquared() <= 0.000001)
                return false;
            rayDirection.Normalize();

            PolygonDebugAltAimCandidate best = new PolygonDebugAltAimCandidate();
            bool found = false;
            TryScorePolygonDebugAltAimCandidates(_apps, rayOrigin, rayDirection, ref best, ref found);
            TryScorePolygonDebugAltAimCandidates(_surfaceScriptApps, rayOrigin, rayDirection, ref best, ref found);
            TryScorePolygonDebugAltAimProbeCandidates(rayOrigin, rayDirection, ref best, ref found);
            TryScorePolygonDebugAltRenderedCandidates(rayOrigin, rayDirection, ref best, ref found);
            if (!found && !TryGetPolygonDebugCachedAltAimCandidate(out best))
                return false;

            RememberPolygonDebugAltAimCandidate(best);
            modelEntity = best.Entity;
            ownerBlock = best.OwnerBlock;
            hitPosition = best.HitPosition;
            hitNormal = best.HitNormal;
            if (best.SurfaceIndex >= 0)
                _polygonDebugCalibrationScreenIndex = best.SurfaceIndex;
            return modelEntity != null && ownerBlock != null;
        }

        bool TryPrimePolygonDebugAltAimSeed()
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return false;

            var camera = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D rayOrigin = camera.Translation;
            Vector3D rayDirection = camera.Forward;
            if (rayDirection.LengthSquared() <= 0.000001)
                return false;
            rayDirection.Normalize();

            PolygonDebugAltAimCandidate best = new PolygonDebugAltAimCandidate();
            bool found = false;
            TryScorePolygonDebugAltAimCandidates(_apps, rayOrigin, rayDirection, ref best, ref found);
            TryScorePolygonDebugAltAimCandidates(_surfaceScriptApps, rayOrigin, rayDirection, ref best, ref found);
            TryScorePolygonDebugAltAimProbeCandidates(rayOrigin, rayDirection, ref best, ref found);
            TryScorePolygonDebugAltRenderedCandidates(rayOrigin, rayDirection, ref best, ref found);
            if (!found)
                return false;

            RememberPolygonDebugAltAimCandidate(best);
            if (best.SurfaceIndex >= 0)
                _polygonDebugCalibrationScreenIndex = best.SurfaceIndex;
            return true;
        }

        void RememberPolygonDebugAltAimCandidate(PolygonDebugAltAimCandidate candidate)
        {
            if (candidate.Input == null || candidate.OwnerBlock == null || candidate.OwnerBlock.MarkedForClose)
                return;

            _polygonDebugAltAimInput = candidate.Input;
            _polygonDebugAltAimLocalA = candidate.LocalA;
            _polygonDebugAltAimLocalB = candidate.LocalB;
            _hasPolygonDebugAltAimSeed = true;

            if (candidate.AxisA.LengthSquared() > 0.000001 && candidate.AxisB.LengthSquared() > 0.000001)
            {
                _polygonDebugAltAimAxisA = candidate.AxisA;
                _polygonDebugAltAimAxisB = candidate.AxisB;
                _polygonDebugAltAimAxisA.Normalize();
                _polygonDebugAltAimAxisB.Normalize();
                _polygonDebugAltAimMinA = candidate.MinA;
                _polygonDebugAltAimMaxA = candidate.MaxA;
                _polygonDebugAltAimMinB = candidate.MinB;
                _polygonDebugAltAimMaxB = candidate.MaxB;
                _hasPolygonDebugAltAimFrame = true;
            }
        }

        bool TryGetPolygonDebugCachedAltAimCandidate(out PolygonDebugAltAimCandidate candidate)
        {
            candidate = new PolygonDebugAltAimCandidate();
            if (!_hasPolygonDebugAltAimSeed || _polygonDebugAltAimInput == null)
                return false;

            TouchScreenApiAdapter input = _polygonDebugAltAimInput;
            if (input.OwnerBlock == null || input.OwnerBlock.MarkedForClose)
            {
                ClearPolygonDebugAltAimSeed();
                return false;
            }

            IMyEntity entity = input.OwnerBlock as IMyEntity;
            if (entity == null || entity.Model == null)
                return false;

            Vector3D topLeft;
            Vector3D topRight;
            Vector3D bottomRight;
            Vector3D bottomLeft;
            if (!TryGetPolygonDebugAltInputCorners(input, out topLeft, out topRight, out bottomRight, out bottomLeft))
                return false;

            Vector3D axisA = topRight - topLeft;
            Vector3D axisB = bottomLeft - topLeft;
            double width = axisA.Length();
            double height = axisB.Length();
            if (width <= 0.0001 || height <= 0.0001)
                return false;

            axisA /= width;
            axisB /= height;
            Vector3D normal = Vector3D.Cross(axisA, axisB);
            if (normal.LengthSquared() <= 0.000001)
                return false;
            normal.Normalize();

            if (MyAPIGateway.Session != null && MyAPIGateway.Session.Camera != null)
            {
                Vector3D center = (topLeft + topRight + bottomRight + bottomLeft) * 0.25;
                if (Vector3D.Dot(MyAPIGateway.Session.Camera.WorldMatrix.Translation - center, normal) < 0.0)
                    normal = -normal;
            }

            double seedA = ClampDouble(_polygonDebugAltAimLocalA, 0.0, width);
            double seedB = ClampDouble(_polygonDebugAltAimLocalB, 0.0, height);
            Vector3D axisBUp = -axisB;
            candidate.Input = input;
            candidate.Entity = entity;
            candidate.OwnerBlock = input.OwnerBlock;
            candidate.HitPosition = topLeft + axisA * seedA + axisB * seedB;
            candidate.HitNormal = normal;
            candidate.SurfaceIndex = input.GetSurfaceIndex();
            candidate.LocalA = seedA;
            candidate.LocalB = seedB;
            candidate.AxisA = axisA;
            candidate.AxisB = axisBUp;
            candidate.MinA = -seedA;
            candidate.MaxA = width - seedA;
            candidate.MinB = seedB - height;
            candidate.MaxB = seedB;
            candidate.Score = 0.0;
            return true;
        }

        void TryScorePolygonDebugAltAimCandidates(List<GridSchematicsLcdApp> apps, Vector3D rayOrigin, Vector3D rayDirection, ref PolygonDebugAltAimCandidate best, ref bool found)
        {
            if (apps == null)
                return;

            for (int i = apps.Count - 1; i >= 0; i--)
            {
                GridSchematicsLcdApp app = apps[i];
                if (app == null || !app.IsOwnerFunctional)
                {
                    apps.RemoveAt(i);
                    continue;
                }

                TryScorePolygonDebugAltAimCandidate(app.TouchInput, rayOrigin, rayDirection, PolygonDebugAltAimCenterPadding, 100.0, ref best, ref found);
            }
        }

        void TryScorePolygonDebugAltAimProbeCandidates(Vector3D rayOrigin, Vector3D rayDirection, ref PolygonDebugAltAimCandidate best, ref bool found)
        {
            for (int i = _unsupportedSurfaceProbes.Count - 1; i >= 0; i--)
            {
                TouchScreenApiAdapter input = _unsupportedSurfaceProbes[i];
                if (input == null || input.OwnerBlock == null || input.OwnerBlock.MarkedForClose)
                {
                    _unsupportedSurfaceProbes.RemoveAt(i);
                    continue;
                }

                TryScorePolygonDebugAltAimCandidate(input, rayOrigin, rayDirection, PolygonDebugAltAimCenterPadding, 100.0, ref best, ref found);
            }
        }

        void TryScorePolygonDebugAltRenderedCandidates(Vector3D rayOrigin, Vector3D rayDirection, ref PolygonDebugAltAimCandidate best, ref bool found)
        {
            for (int i = _manualCalibrationRenderedInputs.Count - 1; i >= 0; i--)
            {
                TouchScreenApiAdapter input = _manualCalibrationRenderedInputs[i];
                if (input == null || input.OwnerBlock == null || input.OwnerBlock.MarkedForClose)
                {
                    _manualCalibrationRenderedInputs.RemoveAt(i);
                    continue;
                }

                TryScorePolygonDebugAltAimCandidate(input, rayOrigin, rayDirection, PolygonDebugAltAimRenderedCenterPadding, 260.0, ref best, ref found);
            }
        }

        void TryScorePolygonDebugAltAimCandidate(TouchScreenApiAdapter input, Vector3D rayOrigin, Vector3D rayDirection, double centerPadding, double centerBaseScore, ref PolygonDebugAltAimCandidate best, ref bool found)
        {
            if (input == null || input.OwnerBlock == null || input.OwnerBlock.MarkedForClose)
                return;

            IMyEntity entity = input.OwnerBlock as IMyEntity;
            if (entity == null || entity.Model == null)
                return;

            Vector3D topLeft;
            Vector3D topRight;
            Vector3D bottomRight;
            Vector3D bottomLeft;
            if (!TryGetPolygonDebugAltInputCorners(input, out topLeft, out topRight, out bottomRight, out bottomLeft))
                return;

            Vector3D axisA = topRight - topLeft;
            Vector3D axisB = bottomLeft - topLeft;
            double width = axisA.Length();
            double height = axisB.Length();
            if (width <= 0.0001 || height <= 0.0001)
                return;

            axisA /= width;
            axisB /= height;
            Vector3D normal = Vector3D.Cross(axisA, axisB);
            if (normal.LengthSquared() <= 0.000001)
                return;
            normal.Normalize();

            Vector3D center = (topLeft + topRight + bottomRight + bottomLeft) * 0.25;
            if (Vector3D.Dot(rayOrigin - center, normal) < 0.0)
                normal = -normal;

            double denom = Vector3D.Dot(rayDirection, normal);
            if (Math.Abs(denom) <= 0.000001)
                return;

            double distance = Vector3D.Dot(center - rayOrigin, normal) / denom;
            if (distance <= 0.05 || distance > PolygonDebugAltAimMaxDistance)
            {
                TryScorePolygonDebugAltAimCenterCandidate(input, entity, topLeft, axisA, axisB, width, height, normal, center, rayOrigin, rayDirection, centerPadding, centerBaseScore, ref best, ref found);
                return;
            }

            Vector3D planeHit = rayOrigin + rayDirection * distance;
            Vector3D delta = planeHit - topLeft;
            double localA = Vector3D.Dot(delta, axisA);
            double localB = Vector3D.Dot(delta, axisB);
            double outsideA = GetPolygonDebugOutsideDistance(localA, 0.0, width);
            double outsideB = GetPolygonDebugOutsideDistance(localB, 0.0, height);
            if (outsideA > PolygonDebugAltAimPlanePadding || outsideB > PolygonDebugAltAimPlanePadding)
            {
                TryScorePolygonDebugAltAimCenterCandidate(input, entity, topLeft, axisA, axisB, width, height, normal, center, rayOrigin, rayDirection, centerPadding, centerBaseScore, ref best, ref found);
                return;
            }

            double seedA = ClampDouble(localA, 0.0, width);
            double seedB = ClampDouble(localB, 0.0, height);
            Vector3D seed = topLeft + axisA * seedA + axisB * seedB;
            bool refinedFromPhysicalHit = false;
            Vector3D detectorSeed;
            Vector3D detectorNormal;
            Vector3D detectorAxisA;
            Vector3D detectorAxisB;
            bool trustModelHit = centerBaseScore >= 200.0;
            if (trustModelHit && TryRefinePolygonDebugAltCandidateFromDetector(input, rayOrigin, rayDirection, axisA, axisB, out detectorSeed, out detectorNormal, out detectorAxisA, out detectorAxisB))
            {
                seed = detectorSeed;
                normal = detectorNormal;
                axisA = detectorAxisA;
                axisB = detectorAxisB;
                refinedFromPhysicalHit = true;
            }
            else
            {
                Vector3D modelSeed;
                Vector3D modelNormal;
                bool refinedFromModelHit = TryRefinePolygonDebugAltCandidateFromModelHit(entity, rayOrigin, rayDirection, center, width, height, axisA, axisB, trustModelHit, out modelSeed, out modelNormal);
                if (refinedFromModelHit)
                {
                    seed = modelSeed;
                    normal = modelNormal;
                    RebuildPolygonDebugAltAxesForNormal(ref axisA, ref axisB, normal);
                    refinedFromPhysicalHit = true;
                }
            }
            double score = distance + (outsideA + outsideB) * 8.0;
            if (found && score >= best.Score)
                return;

            Vector3D axisBUp = -axisB;
            best.Input = input;
            best.Entity = entity;
            best.OwnerBlock = input.OwnerBlock;
            best.HitPosition = seed;
            best.HitNormal = normal;
            best.SurfaceIndex = input.GetSurfaceIndex();
            best.LocalA = seedA;
            best.LocalB = seedB;
            best.AxisA = axisA;
            best.AxisB = axisBUp;
            if (refinedFromPhysicalHit)
            {
                GetPolygonDebugAltRefinedModelBounds(width, height, out best.MinA, out best.MaxA, out best.MinB, out best.MaxB);
            }
            else
            {
                best.MinA = -seedA;
                best.MaxA = width - seedA;
                best.MinB = seedB - height;
                best.MaxB = seedB;
            }
            best.Score = score;
            found = true;
        }

        void TryScorePolygonDebugAltAimCenterCandidate(TouchScreenApiAdapter input, IMyEntity entity, Vector3D topLeft, Vector3D axisA, Vector3D axisB, double width, double height, Vector3D normal, Vector3D center, Vector3D rayOrigin, Vector3D rayDirection, double centerPadding, double centerBaseScore, ref PolygonDebugAltAimCandidate best, ref bool found)
        {
            double forwardDistance = Vector3D.Dot(center - rayOrigin, rayDirection);
            if (forwardDistance <= 0.05 || forwardDistance > PolygonDebugAltAimMaxDistance)
                return;

            Vector3D closestPoint = rayOrigin + rayDirection * forwardDistance;
            double lateralDistance = Vector3D.Distance(center, closestPoint);
            double allowedDistance = Math.Max(width, height) * 0.75 + centerPadding;
            if (lateralDistance > allowedDistance)
                return;

            double seedA = width * 0.5;
            double seedB = height * 0.5;
            Vector3D seed = topLeft + axisA * seedA + axisB * seedB;
            bool refinedFromPhysicalHit = false;
            Vector3D detectorSeed;
            Vector3D detectorNormal;
            Vector3D detectorAxisA;
            Vector3D detectorAxisB;
            bool trustModelHit = centerBaseScore >= 200.0;
            if (trustModelHit && TryRefinePolygonDebugAltCandidateFromDetector(input, rayOrigin, rayDirection, axisA, axisB, out detectorSeed, out detectorNormal, out detectorAxisA, out detectorAxisB))
            {
                seed = detectorSeed;
                normal = detectorNormal;
                axisA = detectorAxisA;
                axisB = detectorAxisB;
                refinedFromPhysicalHit = true;
            }
            else
            {
                Vector3D modelSeed;
                Vector3D modelNormal;
                bool refinedFromModelHit = TryRefinePolygonDebugAltCandidateFromModelHit(entity, rayOrigin, rayDirection, center, width, height, axisA, axisB, trustModelHit, out modelSeed, out modelNormal);
                if (refinedFromModelHit)
                {
                    seed = modelSeed;
                    normal = modelNormal;
                    RebuildPolygonDebugAltAxesForNormal(ref axisA, ref axisB, normal);
                    refinedFromPhysicalHit = true;
                }
            }
            double score = centerBaseScore + forwardDistance + lateralDistance * 10.0;
            if (found && score >= best.Score)
                return;

            Vector3D axisBUp = -axisB;
            best.Input = input;
            best.Entity = entity;
            best.OwnerBlock = input.OwnerBlock;
            best.HitPosition = seed;
            best.HitNormal = normal;
            best.SurfaceIndex = input.GetSurfaceIndex();
            best.LocalA = seedA;
            best.LocalB = seedB;
            best.AxisA = axisA;
            best.AxisB = axisBUp;
            if (refinedFromPhysicalHit)
            {
                GetPolygonDebugAltRefinedModelBounds(width, height, out best.MinA, out best.MaxA, out best.MinB, out best.MaxB);
            }
            else
            {
                best.MinA = -seedA;
                best.MaxA = width - seedA;
                best.MinB = seedB - height;
                best.MaxB = seedB;
            }
            best.Score = score;
            found = true;
        }

        static void GetPolygonDebugAltRefinedModelBounds(double sourceWidth, double sourceHeight, out double minA, out double maxA, out double minB, out double maxB)
        {
            double halfA = Math.Min(Math.Max(sourceWidth * 0.18, 0.08), PolygonDebugAltModelHitDefaultHalfA);
            double halfB = Math.Min(Math.Max(sourceHeight * 0.18, 0.055), PolygonDebugAltModelHitDefaultHalfB);
            minA = -halfA;
            maxA = halfA;
            minB = -halfB;
            maxB = halfB;
        }

        bool TryRefinePolygonDebugAltCandidateFromDetector(TouchScreenApiAdapter input, Vector3D rayOrigin, Vector3D rayDirection, Vector3D axisA, Vector3D axisB, out Vector3D seed, out Vector3D normal, out Vector3D fittedAxisA, out Vector3D fittedAxisB)
        {
            seed = Vector3D.Zero;
            normal = Vector3D.Zero;
            fittedAxisA = Vector3D.Zero;
            fittedAxisB = Vector3D.Zero;
            if (input == null)
                return false;

            return input.TryGetAimDetectorPlaneFit(
                rayOrigin,
                rayDirection,
                axisA,
                axisB,
                PolygonDebugAltModelFitSampleStep,
                PolygonDebugAltModelFitMaxSampleDistance,
                out seed,
                out normal,
                out fittedAxisA,
                out fittedAxisB);
        }

        bool TryRefinePolygonDebugAltCandidateFromModelHit(IMyEntity entity, Vector3D rayOrigin, Vector3D rayDirection, Vector3D candidateCenter, double width, double height, Vector3D axisA, Vector3D axisB, bool trustModelHit, out Vector3D seed, out Vector3D normal)
        {
            seed = Vector3D.Zero;
            normal = Vector3D.Zero;
            if (entity == null || rayDirection.LengthSquared() <= 0.000001)
                return false;

            Vector3D rayEnd = rayOrigin + rayDirection * PolygonDebugAltAimMaxDistance;
            Vector3D hitPosition;
            Vector3D hitNormal;
            if (!TryGetGlobalModelSegmentHit(entity, rayOrigin, rayEnd, out hitPosition, out hitNormal))
                return false;

            if (hitNormal.LengthSquared() <= 0.000001)
                return false;
            hitNormal.Normalize();

            if (!trustModelHit)
            {
                double maxDistance = Math.Max(width, height) * 1.25 + 0.35;
                if (Vector3D.DistanceSquared(hitPosition, candidateCenter) > maxDistance * maxDistance)
                    return false;

                Vector3D lateral = hitPosition - candidateCenter;
                double localA = Vector3D.Dot(lateral, axisA);
                double localB = Vector3D.Dot(lateral, axisB);
                double halfA = width * 0.5 + 0.20;
                double halfB = height * 0.5 + 0.20;
                if (Math.Abs(localA) > halfA || Math.Abs(localB) > halfB)
                    return false;
            }

            if (Vector3D.Dot(rayOrigin - hitPosition, hitNormal) < 0.0)
                hitNormal = -hitNormal;

            Vector3D fittedNormal;
            if (TryFitPolygonDebugAltModelHitPlane(entity, rayOrigin, rayDirection, hitPosition, hitNormal, axisA, axisB, out fittedNormal))
                hitNormal = fittedNormal;

            seed = hitPosition;
            normal = hitNormal;
            return true;
        }

        bool TryFitPolygonDebugAltModelHitPlane(IMyEntity entity, Vector3D rayOrigin, Vector3D rayDirection, Vector3D centerHit, Vector3D initialNormal, Vector3D axisA, Vector3D axisB, out Vector3D fittedNormal)
        {
            fittedNormal = Vector3D.Zero;
            if (entity == null || rayDirection.LengthSquared() <= 0.000001 || axisA.LengthSquared() <= 0.000001 || axisB.LengthSquared() <= 0.000001)
                return false;

            axisA.Normalize();
            axisB.Normalize();

            Vector3D rightHit;
            Vector3D rightNormal;
            bool hasRight = TryGetPolygonDebugAltOffsetModelHit(entity, rayOrigin, rayDirection, axisA * PolygonDebugAltModelFitSampleStep, centerHit, out rightHit, out rightNormal);
            Vector3D leftHit;
            Vector3D leftNormal;
            bool hasLeft = TryGetPolygonDebugAltOffsetModelHit(entity, rayOrigin, rayDirection, -axisA * PolygonDebugAltModelFitSampleStep, centerHit, out leftHit, out leftNormal);
            Vector3D upHit;
            Vector3D upNormal;
            bool hasUp = TryGetPolygonDebugAltOffsetModelHit(entity, rayOrigin, rayDirection, -axisB * PolygonDebugAltModelFitSampleStep, centerHit, out upHit, out upNormal);
            Vector3D downHit;
            Vector3D downNormal;
            bool hasDown = TryGetPolygonDebugAltOffsetModelHit(entity, rayOrigin, rayDirection, axisB * PolygonDebugAltModelFitSampleStep, centerHit, out downHit, out downNormal);

            Vector3D tangentA = Vector3D.Zero;
            if (hasRight && hasLeft)
                tangentA = rightHit - leftHit;
            else if (hasRight)
                tangentA = rightHit - centerHit;
            else if (hasLeft)
                tangentA = centerHit - leftHit;

            Vector3D tangentB = Vector3D.Zero;
            if (hasUp && hasDown)
                tangentB = upHit - downHit;
            else if (hasUp)
                tangentB = upHit - centerHit;
            else if (hasDown)
                tangentB = centerHit - downHit;

            if (tangentA.LengthSquared() > 0.000001 && tangentB.LengthSquared() > 0.000001)
            {
                fittedNormal = Vector3D.Cross(tangentA, tangentB);
                if (fittedNormal.LengthSquared() > 0.000001)
                {
                    fittedNormal.Normalize();
                    if (Vector3D.Dot(rayOrigin - centerHit, fittedNormal) < 0.0)
                        fittedNormal = -fittedNormal;

                    if (initialNormal.LengthSquared() <= 0.000001 || Math.Abs(Vector3D.Dot(fittedNormal, initialNormal)) >= 0.45)
                        return true;
                }
            }

            Vector3D normalSum = Vector3D.Zero;
            int normalCount = 0;
            AddPolygonDebugAltSampleNormal(initialNormal, centerHit, rayOrigin, ref normalSum, ref normalCount);
            if (hasRight) AddPolygonDebugAltSampleNormal(rightNormal, rightHit, rayOrigin, ref normalSum, ref normalCount);
            if (hasLeft) AddPolygonDebugAltSampleNormal(leftNormal, leftHit, rayOrigin, ref normalSum, ref normalCount);
            if (hasUp) AddPolygonDebugAltSampleNormal(upNormal, upHit, rayOrigin, ref normalSum, ref normalCount);
            if (hasDown) AddPolygonDebugAltSampleNormal(downNormal, downHit, rayOrigin, ref normalSum, ref normalCount);

            if (normalCount < 2 || normalSum.LengthSquared() <= 0.000001)
                return false;

            fittedNormal = normalSum;
            fittedNormal.Normalize();
            if (Vector3D.Dot(rayOrigin - centerHit, fittedNormal) < 0.0)
                fittedNormal = -fittedNormal;
            return true;
        }

        bool TryGetPolygonDebugAltOffsetModelHit(IMyEntity entity, Vector3D rayOrigin, Vector3D rayDirection, Vector3D offset, Vector3D centerHit, out Vector3D hitPosition, out Vector3D hitNormal)
        {
            hitPosition = Vector3D.Zero;
            hitNormal = Vector3D.Zero;
            Vector3D start = rayOrigin + offset;
            Vector3D end = start + rayDirection * PolygonDebugAltAimMaxDistance;
            if (!TryGetGlobalModelSegmentHit(entity, start, end, out hitPosition, out hitNormal))
                return false;

            if (Vector3D.DistanceSquared(hitPosition, centerHit) > PolygonDebugAltModelFitMaxSampleDistance * PolygonDebugAltModelFitMaxSampleDistance)
                return false;

            if (hitNormal.LengthSquared() <= 0.000001)
                return false;
            hitNormal.Normalize();
            if (Vector3D.Dot(rayOrigin - hitPosition, hitNormal) < 0.0)
                hitNormal = -hitNormal;
            return true;
        }

        static void AddPolygonDebugAltSampleNormal(Vector3D normal, Vector3D hitPosition, Vector3D cameraPosition, ref Vector3D normalSum, ref int normalCount)
        {
            if (normal.LengthSquared() <= 0.000001)
                return;

            normal.Normalize();
            if (Vector3D.Dot(cameraPosition - hitPosition, normal) < 0.0)
                normal = -normal;
            normalSum += normal;
            normalCount++;
        }

        static void RebuildPolygonDebugAltAxesForNormal(ref Vector3D axisA, ref Vector3D axisB, Vector3D normal)
        {
            if (normal.LengthSquared() <= 0.000001 || axisA.LengthSquared() <= 0.000001)
                return;

            normal.Normalize();
            axisA = axisA - normal * Vector3D.Dot(axisA, normal);
            if (axisA.LengthSquared() <= 0.000001)
            {
                axisA = Vector3D.Cross(axisB, normal);
                if (axisA.LengthSquared() <= 0.000001)
                    return;
            }

            axisA.Normalize();
            axisB = Vector3D.Cross(normal, axisA);
            if (axisB.LengthSquared() <= 0.000001)
                return;
            axisB.Normalize();
        }

        bool TryGetPolygonDebugAltInputCorners(TouchScreenApiAdapter input, out Vector3D topLeft, out Vector3D topRight, out Vector3D bottomRight, out Vector3D bottomLeft)
        {
            topLeft = Vector3D.Zero;
            topRight = Vector3D.Zero;
            bottomRight = Vector3D.Zero;
            bottomLeft = Vector3D.Zero;
            if (input == null)
                return false;

            Vector3D seed;
            Vector3D normal;
            Vector3D axisA;
            Vector3D axisB;
            double minA;
            double maxA;
            double minB;
            double maxB;
            if (input.TryGetStoredPanelCursorSurfaceCalibration(out seed, out normal, out axisA, out axisB, out minA, out maxA, out minB, out maxB) &&
                axisA.LengthSquared() > 0.000001 &&
                axisB.LengthSquared() > 0.000001)
            {
                axisA.Normalize();
                axisB.Normalize();
                topLeft = seed + axisA * minA + axisB * maxB;
                topRight = seed + axisA * maxA + axisB * maxB;
                bottomRight = seed + axisA * maxA + axisB * minB;
                bottomLeft = seed + axisA * minA + axisB * minB;
                return true;
            }

            if (input.TryGetPanelCursorSurfaceCorners(out topLeft, out topRight, out bottomRight, out bottomLeft))
                return true;

            if (input.TryGetLastPanelSurfaceCandidateCorners(out topLeft, out topRight, out bottomRight, out bottomLeft))
                return true;

            if (input.TryGetDetectorBoundsDebugCorners(out topLeft, out topRight, out bottomRight, out bottomLeft))
                return true;

            if (input.TryGetDiscoveredDisplayCorners(out topLeft, out topRight, out bottomRight, out bottomLeft))
                return true;

            return input.TryGetStablePhysicalDisplayCorners(out topLeft, out topRight, out bottomRight, out bottomLeft);
        }

        static double GetPolygonDebugOutsideDistance(double value, double min, double max)
        {
            if (value < min)
                return min - value;
            if (value > max)
                return value - max;
            return 0.0;
        }

        static double ClampDouble(double value, double min, double max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }

        bool TryGetPolygonDebugAimHit(out IMyEntity modelEntity, out IMyCubeBlock ownerBlock, out Vector3D hitPosition, out Vector3D hitNormal)
        {
            modelEntity = null;
            ownerBlock = null;
            hitPosition = Vector3D.Zero;
            hitNormal = Vector3D.Zero;
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null || MyAPIGateway.Physics == null)
                return false;

            var camera = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D rayOrigin = camera.Translation;
            Vector3D rayDirection = camera.Forward;
            if (rayDirection.LengthSquared() <= 0.000001)
                return false;
            rayDirection.Normalize();

            Vector3D rayEnd = rayOrigin + rayDirection * 12.0;
            IHitInfo physicsHit;
            if (!MyAPIGateway.Physics.CastRay(rayOrigin, rayEnd, out physicsHit) || physicsHit == null || physicsHit.HitEntity == null)
                return false;

            ownerBlock = TryResolveOwnerBlockFromPhysicsHit(physicsHit, rayDirection);
            if (!TryResolveModelEntityFromPhysicsHit(physicsHit, rayDirection, out modelEntity))
                return false;

            if (ownerBlock == null)
                ownerBlock = ResolvePolygonDebugOwnerBlock(modelEntity);

            return TryGetGlobalModelSegmentHit(modelEntity, rayOrigin, rayEnd, out hitPosition, out hitNormal);
        }

        IMyCubeBlock TryResolveOwnerBlockFromPhysicsHit(IHitInfo physicsHit, Vector3D rayDirection)
        {
            if (physicsHit == null || physicsHit.HitEntity == null)
                return null;

            var directBlock = physicsHit.HitEntity as IMyCubeBlock;
            if (directBlock != null)
                return directBlock;

            var entity = physicsHit.HitEntity as MyEntity;
            for (int i = 0; i < 16 && entity != null; i++)
            {
                var block = entity as IMyCubeBlock;
                if (block != null)
                    return block;
                entity = entity.Parent;
            }

            var grid = physicsHit.HitEntity as IMyCubeGrid;
            if (grid != null)
                return FindOwnerBlockNearGridHit(grid, physicsHit.Position, rayDirection);

            return null;
        }

        IMyCubeBlock FindOwnerBlockNearGridHit(IMyCubeGrid grid, Vector3D hitPosition, Vector3D rayDirection)
        {
            if (grid == null)
                return null;

            try
            {
                Vector3D sample = hitPosition + rayDirection * 0.06;
                Vector3I cell = grid.WorldToGridInteger(sample);
                IMyCubeBlock bestBlock = null;
                double bestDistanceSq = double.MaxValue;

                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            var slim = grid.GetCubeBlock(cell + new Vector3I(x, y, z));
                            if (slim == null || slim.FatBlock == null)
                                continue;

                            var blockEntity = slim.FatBlock as IMyEntity;
                            if (blockEntity == null)
                                continue;

                            double distanceSq = Vector3D.DistanceSquared(blockEntity.WorldMatrix.Translation, hitPosition);
                            if (distanceSq < bestDistanceSq)
                            {
                                bestDistanceSq = distanceSq;
                                bestBlock = slim.FatBlock;
                            }
                        }
                    }
                }

                return bestBlock;
            }
            catch
            {
                return null;
            }
        }

        IMyCubeBlock ResolvePolygonDebugOwnerBlock(IMyEntity entity)
        {
            MyEntity current = entity as MyEntity;
            for (int i = 0; i < 16 && current != null; i++)
            {
                var block = current as IMyCubeBlock;
                if (block != null)
                    return block;

                current = current.Parent;
            }

            return _polygonDebugRootBlockEntity as IMyCubeBlock;
        }

        bool IsPolygonDebugSameOwnerBlock(IMyEntity entity)
        {
            if (_polygonDebugOwnerBlock == null || entity == null)
                return false;

            IMyCubeBlock block = ResolvePolygonDebugOwnerBlock(entity);
            return block != null && block.EntityId == _polygonDebugOwnerBlock.EntityId;
        }

        void AddPolygonDebugEntityTree(IMyEntity entity)
        {
            if (entity == null)
                return;

            AddPolygonDebugOcclusionEntity(entity);

            var myEntity = entity as MyEntity;
            if (myEntity == null || myEntity.Subparts == null || myEntity.Subparts.Count == 0)
                return;

            foreach (var pair in myEntity.Subparts)
                AddPolygonDebugEntityTree(pair.Value);
        }

        void AddPolygonDebugOcclusionEntity(IMyEntity entity)
        {
            if (entity == null || entity.Model == null)
                return;

            for (int i = 0; i < _polygonDebugOcclusionEntities.Count; i++)
            {
                if (_polygonDebugOcclusionEntities[i] != null && _polygonDebugOcclusionEntities[i].EntityId == entity.EntityId)
                    return;
            }

            _polygonDebugOcclusionEntities.Add(entity);
        }

        void CollectPolygonDebugNeighborProbeEntities()
        {
            if (_polygonDebugAxisA.LengthSquared() <= 0.000001 || _polygonDebugAxisB.LengthSquared() <= 0.000001 || _polygonDebugNormal.LengthSquared() <= 0.000001)
                return;

            double centerA = (_polygonDebugMinA + _polygonDebugMaxA) * 0.5;
            double centerB = (_polygonDebugMinB + _polygonDebugMaxB) * 0.5;
            double leftA = _polygonDebugMinA - PolygonDebugNeighborProbeOutsideOffset;
            double rightA = _polygonDebugMaxA + PolygonDebugNeighborProbeOutsideOffset;
            double bottomB = _polygonDebugMinB - PolygonDebugNeighborProbeOutsideOffset;
            double topB = _polygonDebugMaxB + PolygonDebugNeighborProbeOutsideOffset;

            ProbePolygonDebugNeighborAt(leftA, centerB);
            ProbePolygonDebugNeighborAt(rightA, centerB);
            ProbePolygonDebugNeighborAt(centerA, bottomB);
            ProbePolygonDebugNeighborAt(centerA, topB);
            ProbePolygonDebugNeighborAt(leftA, bottomB);
            ProbePolygonDebugNeighborAt(leftA, topB);
            ProbePolygonDebugNeighborAt(rightA, bottomB);
            ProbePolygonDebugNeighborAt(rightA, topB);
        }

        void CollectPolygonDebugCameraRayHarvestEntities()
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null || MyAPIGateway.Physics == null)
                return;

            double minA = _polygonDebugMinA - PolygonDebugCameraRayHarvestOutsideOffset;
            double maxA = _polygonDebugMaxA + PolygonDebugCameraRayHarvestOutsideOffset;
            double minB = _polygonDebugMinB - PolygonDebugCameraRayHarvestOutsideOffset;
            double maxB = _polygonDebugMaxB + PolygonDebugCameraRayHarvestOutsideOffset;
            double centerA = (minA + maxA) * 0.5;
            double centerB = (minB + maxB) * 0.5;
            double quarterA0 = minA + (maxA - minA) * 0.25;
            double quarterA1 = minA + (maxA - minA) * 0.75;
            double quarterB0 = minB + (maxB - minB) * 0.25;
            double quarterB1 = minB + (maxB - minB) * 0.75;

            HarvestPolygonDebugCameraRayAt(centerA, maxB);
            HarvestPolygonDebugCameraRayAt(centerA, minB);
            HarvestPolygonDebugCameraRayAt(minA, centerB);
            HarvestPolygonDebugCameraRayAt(maxA, centerB);
            HarvestPolygonDebugCameraRayAt(minA, minB);
            HarvestPolygonDebugCameraRayAt(minA, maxB);
            HarvestPolygonDebugCameraRayAt(maxA, minB);
            HarvestPolygonDebugCameraRayAt(maxA, maxB);
            HarvestPolygonDebugCameraRayAt(quarterA0, maxB);
            HarvestPolygonDebugCameraRayAt(quarterA1, maxB);
            HarvestPolygonDebugCameraRayAt(quarterA0, minB);
            HarvestPolygonDebugCameraRayAt(quarterA1, minB);
            HarvestPolygonDebugCameraRayAt(minA, quarterB0);
            HarvestPolygonDebugCameraRayAt(minA, quarterB1);
            HarvestPolygonDebugCameraRayAt(maxA, quarterB0);
            HarvestPolygonDebugCameraRayAt(maxA, quarterB1);
        }

        void HarvestPolygonDebugCameraRayAt(double localA, double localB)
        {
            var camera = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D origin = camera.Translation;
            Vector3D target = PolygonDebugPointFromLocal(localA, localB);
            Vector3D direction = target - origin;
            double distance = direction.Length();
            if (distance <= 0.0001)
                return;
            direction /= distance;

            Vector3D end = origin + direction * Math.Min(12.0, distance + 0.9);
            try
            {
                IHitInfo hit;
                if (!MyAPIGateway.Physics.CastRay(origin, end, out hit) || hit == null || hit.HitEntity == null)
                    return;

                IMyEntity entity;
                if (!TryResolveModelEntityFromPhysicsHit(hit, direction, out entity))
                    return;

                AddPolygonDebugEntityTree(entity);
            }
            catch
            {
            }
        }

        void ProbePolygonDebugNeighborAt(double localA, double localB)
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return;

            Vector3D target = _polygonDebugSeed + _polygonDebugAxisA * localA + _polygonDebugAxisB * localB;
            Vector3D toCamera = MyAPIGateway.Session.Camera.WorldMatrix.Translation - target;
            Vector3D probeNormal = Vector3D.Dot(toCamera, _polygonDebugNormal) >= 0.0 ? _polygonDebugNormal : -_polygonDebugNormal;
            Vector3D start = target + probeNormal * PolygonDebugNeighborProbeDepth;
            Vector3D end = target - probeNormal * PolygonDebugNeighborProbeDepth;

            IMyEntity entity;
            if (TryResolvePolygonDebugNeighborEntity(start, end, probeNormal, out entity))
            {
                if (IsPolygonDebugSameOwnerBlock(entity))
                    AddPolygonDebugEntityTree(entity);
            }
        }

        bool TryResolvePolygonDebugNeighborEntity(Vector3D start, Vector3D end, Vector3D rayDirection, out IMyEntity entity)
        {
            entity = null;
            if (MyAPIGateway.Physics == null)
                return false;

            try
            {
                IHitInfo hit;
                if (MyAPIGateway.Physics.CastRay(start, end, out hit) && hit != null && hit.HitEntity != null)
                {
                    if (TryResolveModelEntityFromPhysicsHit(hit, rayDirection, out entity))
                        return true;
                }

                if (MyAPIGateway.Physics.CastRay(end, start, out hit) && hit != null && hit.HitEntity != null)
                {
                    if (TryResolveModelEntityFromPhysicsHit(hit, -rayDirection, out entity))
                        return true;
                }
            }
            catch
            {
            }

            return false;
        }

        void RefinePolygonDebugBoundsWithOcclusionEntities()
        {
            if (_polygonDebugEntity == null || _polygonDebugAxisA.LengthSquared() <= 0.000001 || _polygonDebugAxisB.LengthSquared() <= 0.000001)
                return;

            if (_polygonDebugOcclusionEntities.Count == 0)
                return;

            double minA = _polygonDebugMinA;
            double maxA = _polygonDebugMaxA;
            double minB = _polygonDebugMinB;
            double maxB = _polygonDebugMaxB;

            for (int i = 0; i < _polygonDebugOcclusionEntities.Count; i++)
            {
                IMyEntity occluder = _polygonDebugOcclusionEntities[i];
                if (occluder == null || occluder.Model == null)
                    continue;

                RefinePolygonDebugBoundsWithOccluderEntity(occluder, ref minA, ref maxA, ref minB, ref maxB);
            }

            if (maxA - minA < PolygonDebugMinimumSize || maxB - minB < PolygonDebugMinimumSize)
                return;

            _polygonDebugMinA = minA;
            _polygonDebugMaxA = maxA;
            _polygonDebugMinB = minB;
            _polygonDebugMaxB = maxB;
            _polygonDebugTopLeft = _polygonDebugSeed + _polygonDebugAxisA * minA + _polygonDebugAxisB * maxB;
            _polygonDebugTopRight = _polygonDebugSeed + _polygonDebugAxisA * maxA + _polygonDebugAxisB * maxB;
            _polygonDebugBottomRight = _polygonDebugSeed + _polygonDebugAxisA * maxA + _polygonDebugAxisB * minB;
            _polygonDebugBottomLeft = _polygonDebugSeed + _polygonDebugAxisA * minA + _polygonDebugAxisB * minB;
        }

        void CollectPolygonDebugSpatialOccluders()
        {
            _polygonDebugWorldOccluderTriangles.Clear();
            if (MyAPIGateway.Entities == null || _polygonDebugAxisA.LengthSquared() <= 0.000001 || _polygonDebugAxisB.LengthSquared() <= 0.000001)
                return;

            try
            {
                _polygonDebugNearbyEntities.Clear();
                MyAPIGateway.Entities.GetEntities(_polygonDebugNearbyEntities, IsSpatialPolygonDebugOccluderCandidate);
                foreach (IMyEntity entity in _polygonDebugNearbyEntities)
                    CollectPolygonDebugSpatialOccluderTrianglesForBounds(entity);
            }
            catch
            {
                _polygonDebugWorldOccluderTriangles.Clear();
            }
            finally
            {
                _polygonDebugNearbyEntities.Clear();
            }
        }

        bool IsSpatialPolygonDebugOccluderCandidate(IMyEntity entity)
        {
            if (entity == null || entity.MarkedForClose || entity.Model == null)
                return false;

            try
            {
                double radius = GetPolygonDebugSpatialSearchRadius();
                return DistanceSquaredToBox(_polygonDebugSeed, entity.WorldAABB) <= radius * radius;
            }
            catch
            {
                return false;
            }
        }

        double GetPolygonDebugSpatialSearchRadius()
        {
            double width = Math.Max(0.0, _polygonDebugMaxA - _polygonDebugMinA);
            double height = Math.Max(0.0, _polygonDebugMaxB - _polygonDebugMinB);
            return Math.Sqrt(width * width + height * height) * 0.5 + PolygonDebugSpatialOccluderSearchPadding;
        }

        static double DistanceSquaredToBox(Vector3D point, BoundingBoxD box)
        {
            double dx = 0.0;
            if (point.X < box.Min.X) dx = box.Min.X - point.X;
            else if (point.X > box.Max.X) dx = point.X - box.Max.X;

            double dy = 0.0;
            if (point.Y < box.Min.Y) dy = box.Min.Y - point.Y;
            else if (point.Y > box.Max.Y) dy = point.Y - box.Max.Y;

            double dz = 0.0;
            if (point.Z < box.Min.Z) dz = box.Min.Z - point.Z;
            else if (point.Z > box.Max.Z) dz = point.Z - box.Max.Z;

            return dx * dx + dy * dy + dz * dz;
        }

        void CollectPolygonDebugSpatialOccluderTrianglesForBounds(IMyEntity entity)
        {
            CollectPolygonDebugSpatialOccluderTriangles(entity, _polygonDebugSeed);

            double minA = _polygonDebugMinA - PolygonDebugNeighborProbeOutsideOffset;
            double maxA = _polygonDebugMaxA + PolygonDebugNeighborProbeOutsideOffset;
            double minB = _polygonDebugMinB - PolygonDebugNeighborProbeOutsideOffset;
            double maxB = _polygonDebugMaxB + PolygonDebugNeighborProbeOutsideOffset;
            double centerA = (minA + maxA) * 0.5;
            double centerB = (minB + maxB) * 0.5;
            double quarterA0 = minA + (maxA - minA) * 0.25;
            double quarterA1 = minA + (maxA - minA) * 0.75;
            double quarterB0 = minB + (maxB - minB) * 0.25;
            double quarterB1 = minB + (maxB - minB) * 0.75;

            CollectPolygonDebugSpatialOccluderTriangles(entity, PolygonDebugPointFromLocal(centerA, maxB));
            CollectPolygonDebugSpatialOccluderTriangles(entity, PolygonDebugPointFromLocal(centerA, minB));
            CollectPolygonDebugSpatialOccluderTriangles(entity, PolygonDebugPointFromLocal(minA, centerB));
            CollectPolygonDebugSpatialOccluderTriangles(entity, PolygonDebugPointFromLocal(maxA, centerB));
            CollectPolygonDebugSpatialOccluderTriangles(entity, PolygonDebugPointFromLocal(minA, minB));
            CollectPolygonDebugSpatialOccluderTriangles(entity, PolygonDebugPointFromLocal(minA, maxB));
            CollectPolygonDebugSpatialOccluderTriangles(entity, PolygonDebugPointFromLocal(maxA, minB));
            CollectPolygonDebugSpatialOccluderTriangles(entity, PolygonDebugPointFromLocal(maxA, maxB));
            CollectPolygonDebugSpatialOccluderTriangles(entity, PolygonDebugPointFromLocal(quarterA0, maxB));
            CollectPolygonDebugSpatialOccluderTriangles(entity, PolygonDebugPointFromLocal(quarterA1, maxB));
            CollectPolygonDebugSpatialOccluderTriangles(entity, PolygonDebugPointFromLocal(quarterA0, minB));
            CollectPolygonDebugSpatialOccluderTriangles(entity, PolygonDebugPointFromLocal(quarterA1, minB));
            CollectPolygonDebugSpatialOccluderTriangles(entity, PolygonDebugPointFromLocal(minA, quarterB0));
            CollectPolygonDebugSpatialOccluderTriangles(entity, PolygonDebugPointFromLocal(minA, quarterB1));
            CollectPolygonDebugSpatialOccluderTriangles(entity, PolygonDebugPointFromLocal(maxA, quarterB0));
            CollectPolygonDebugSpatialOccluderTriangles(entity, PolygonDebugPointFromLocal(maxA, quarterB1));
        }

        Vector3D PolygonDebugPointFromLocal(double localA, double localB)
        {
            return _polygonDebugSeed + _polygonDebugAxisA * localA + _polygonDebugAxisB * localB;
        }

        void CollectPolygonDebugSpatialOccluderTriangles(IMyEntity entity, Vector3D sampleWorld)
        {
            if (entity == null || entity.Model == null)
                return;

            try
            {
                _polygonDebugSpatialOccluderTriangles.Clear();
                MatrixD inverseWorld = MatrixD.Invert(entity.WorldMatrix);
                Vector3D localSeedD = Vector3D.Transform(sampleWorld, inverseWorld);
                var localSeed = new Vector3((float)localSeedD.X, (float)localSeedD.Y, (float)localSeedD.Z);
                var sphere = new BoundingSphere(localSeed, PolygonDebugSpatialOccluderSampleRadius);
                Vector3? normalFilter = null;
                float? maxAngle = null;
                entity.GetTrianglesIntersectingSphere(ref sphere, normalFilter, maxAngle, _polygonDebugSpatialOccluderTriangles, 512);

                MatrixD world = entity.WorldMatrix;
                for (int i = 0; i < _polygonDebugSpatialOccluderTriangles.Count; i++)
                {
                    MyTriangle_Vertex_Normals triangle = _polygonDebugSpatialOccluderTriangles[i];
                    Vector3D v0 = TransformPolygonDebugVertex(triangle.Vertices.Vertex0, world);
                    Vector3D v1 = TransformPolygonDebugVertex(triangle.Vertices.Vertex1, world);
                    Vector3D v2 = TransformPolygonDebugVertex(triangle.Vertices.Vertex2, world);
                    if (!IsPolygonDebugSpatialOccluderTriangle(v0, v1, v2))
                        continue;

                    _polygonDebugWorldOccluderTriangles.Add(new PolygonDebugWorldTriangle
                    {
                        V0 = v0,
                        V1 = v1,
                        V2 = v2
                    });
                }
            }
            catch
            {
                _polygonDebugSpatialOccluderTriangles.Clear();
            }
        }

        bool IsPolygonDebugSpatialOccluderTriangle(Vector3D v0, Vector3D v1, Vector3D v2)
        {
            Vector3D faceNormal = Vector3D.Cross(v1 - v0, v2 - v0);
            if (faceNormal.LengthSquared() <= 0.000001)
                return false;
            faceNormal.Normalize();

            double screenNormalDot = Math.Abs(Vector3D.Dot(faceNormal, _polygonDebugNormal));
            if (screenNormalDot >= PolygonDebugOcclusionNormalDotThreshold)
                return false;

            return IsPolygonDebugSpatialOccluderVertex(v0) ||
                IsPolygonDebugSpatialOccluderVertex(v1) ||
                IsPolygonDebugSpatialOccluderVertex(v2) ||
                IsPolygonDebugSpatialOccluderVertex((v0 + v1 + v2) / 3.0);
        }

        bool IsPolygonDebugSpatialOccluderVertex(Vector3D vertex)
        {
            Vector3D delta = vertex - _polygonDebugSeed;
            double height = Vector3D.Dot(delta, _polygonDebugNormal);
            if (height < -PolygonDebugOcclusionSurfaceClearance || height > PolygonDebugSpatialOccluderDepth)
                return false;

            double a = Vector3D.Dot(delta, _polygonDebugAxisA);
            double b = Vector3D.Dot(delta, _polygonDebugAxisB);
            return a >= _polygonDebugMinA - PolygonDebugSpatialOccluderSearchPadding &&
                a <= _polygonDebugMaxA + PolygonDebugSpatialOccluderSearchPadding &&
                b >= _polygonDebugMinB - PolygonDebugSpatialOccluderSearchPadding &&
                b <= _polygonDebugMaxB + PolygonDebugSpatialOccluderSearchPadding;
        }

        void RefinePolygonDebugBoundsWithOccluderEntity(IMyEntity occluder, ref double minA, ref double maxA, ref double minB, ref double maxB)
        {
            try
            {
                _polygonDebugSubpartTrimTriangles.Clear();
                MatrixD inverseWorld = MatrixD.Invert(occluder.WorldMatrix);
                Vector3D localSeedD = Vector3D.Transform(_polygonDebugSeed, inverseWorld);
                var localSeed = new Vector3((float)localSeedD.X, (float)localSeedD.Y, (float)localSeedD.Z);
                var sphere = new BoundingSphere(localSeed, PolygonDebugSubpartTrimProbeRadius);
                Vector3? normalFilter = null;
                float? maxAngle = null;
                occluder.GetTrianglesIntersectingSphere(ref sphere, normalFilter, maxAngle, _polygonDebugSubpartTrimTriangles, 256);

                MatrixD world = occluder.WorldMatrix;
                for (int i = 0; i < _polygonDebugSubpartTrimTriangles.Count; i++)
                    RefinePolygonDebugBoundsWithOccluderTriangle(_polygonDebugSubpartTrimTriangles[i], world, ref minA, ref maxA, ref minB, ref maxB);
            }
            catch
            {
                _polygonDebugSubpartTrimTriangles.Clear();
            }
        }

        void RefinePolygonDebugBoundsWithOccluderTriangle(MyTriangle_Vertex_Normals triangle, MatrixD world, ref double minA, ref double maxA, ref double minB, ref double maxB)
        {
            Vector3D v0 = TransformPolygonDebugVertex(triangle.Vertices.Vertex0, world);
            Vector3D v1 = TransformPolygonDebugVertex(triangle.Vertices.Vertex1, world);
            Vector3D v2 = TransformPolygonDebugVertex(triangle.Vertices.Vertex2, world);
            Vector3D faceNormal = Vector3D.Cross(v1 - v0, v2 - v0);
            if (faceNormal.LengthSquared() <= 0.000001)
                return;
            faceNormal.Normalize();

            if (Math.Abs(Vector3D.Dot(faceNormal, _polygonDebugNormal)) >= PolygonDebugOcclusionNormalDotThreshold)
                return;

            TryTrimPolygonDebugBoundsWithProjectedOccluderVertex(v0, ref minA, ref maxA, ref minB, ref maxB);
            TryTrimPolygonDebugBoundsWithProjectedOccluderVertex(v1, ref minA, ref maxA, ref minB, ref maxB);
            TryTrimPolygonDebugBoundsWithProjectedOccluderVertex(v2, ref minA, ref maxA, ref minB, ref maxB);
        }

        void TryTrimPolygonDebugBoundsWithProjectedOccluderVertex(Vector3D vertex, ref double minA, ref double maxA, ref double minB, ref double maxB)
        {
            Vector3D delta = vertex - _polygonDebugSeed;
            double height = Vector3D.Dot(delta, _polygonDebugNormal);
            if (height < -PolygonDebugOcclusionSurfaceClearance || height > PolygonDebugSubpartTrimDepth)
                return;

            double a = Vector3D.Dot(delta, _polygonDebugAxisA);
            double b = Vector3D.Dot(delta, _polygonDebugAxisB);
            bool inA = a >= minA - PolygonDebugSubpartTrimProjectedTolerance && a <= maxA + PolygonDebugSubpartTrimProjectedTolerance;
            bool inB = b >= minB - PolygonDebugSubpartTrimProjectedTolerance && b <= maxB + PolygonDebugSubpartTrimProjectedTolerance;

            if (!inA && !inB)
                return;

            if (inB)
            {
                double distMinA = Math.Abs(a - minA);
                double distMaxA = Math.Abs(a - maxA);
                if (distMinA <= PolygonDebugSubpartTrimProjectedTolerance || (a > minA && a < (minA + maxA) * 0.5 && distMinA < distMaxA))
                    minA = Math.Max(minA, a + PolygonDebugSubpartTrimProjectedTolerance);
                else if (distMaxA <= PolygonDebugSubpartTrimProjectedTolerance || (a < maxA && a > (minA + maxA) * 0.5 && distMaxA < distMinA))
                    maxA = Math.Min(maxA, a - PolygonDebugSubpartTrimProjectedTolerance);
            }

            if (inA)
            {
                double distMinB = Math.Abs(b - minB);
                double distMaxB = Math.Abs(b - maxB);
                if (distMinB <= PolygonDebugSubpartTrimProjectedTolerance || (b > minB && b < (minB + maxB) * 0.5 && distMinB < distMaxB))
                    minB = Math.Max(minB, b + PolygonDebugSubpartTrimProjectedTolerance);
                else if (distMaxB <= PolygonDebugSubpartTrimProjectedTolerance || (b < maxB && b > (minB + maxB) * 0.5 && distMaxB < distMinB))
                    maxB = Math.Min(maxB, b - PolygonDebugSubpartTrimProjectedTolerance);
            }
        }

        void IncludePolygonDebugBoundsVertex(Vector3D vertex, ref bool found, ref double minA, ref double maxA, ref double minB, ref double maxB)
        {
            Vector3D delta = vertex - _polygonDebugSeed;
            double planeDistance = Math.Abs(Vector3D.Dot(delta, _polygonDebugNormal));
            if (planeDistance > PolygonDebugPlaneTolerance * 2.0)
                return;

            double a = Vector3D.Dot(delta, _polygonDebugAxisA);
            double b = Vector3D.Dot(delta, _polygonDebugAxisB);
            if (!found)
            {
                minA = maxA = a;
                minB = maxB = b;
                found = true;
                return;
            }

            if (a < minA) minA = a;
            if (a > maxA) maxA = a;
            if (b < minB) minB = b;
            if (b > maxB) maxB = b;
        }

        static bool IsPolygonDebugTriangleOnSeedSurface(MyTriangle_Vertex_Normals triangle, Vector3 localSeed, Vector3 localNormal)
        {
            Vector3 v0 = triangle.Vertices.Vertex0;
            Vector3 v1 = triangle.Vertices.Vertex1;
            Vector3 v2 = triangle.Vertices.Vertex2;
            Vector3 faceNormal = Vector3.Cross(v1 - v0, v2 - v0);
            if (faceNormal.LengthSquared() <= 0.000001f)
                return false;
            faceNormal.Normalize();

            double normalDot = Math.Abs(Vector3.Dot(faceNormal, localNormal));
            if (normalDot < PolygonDebugNormalDotThreshold)
                return false;

            Vector3 center = (v0 + v1 + v2) / 3f;
            double planeDistance = Math.Abs(Vector3.Dot(center - localSeed, localNormal));
            return planeDistance <= PolygonDebugPlaneTolerance;
        }

        void KeepConnectedPolygonDebugTriangles(MatrixD world)
        {
            _polygonDebugConnectedTriangles.Clear();
            _polygonDebugTriangleQueue.Clear();
            _polygonDebugTriangleVisited.Clear();

            int count = _polygonDebugTriangles.Count;
            if (count <= 1)
                return;

            for (int i = 0; i < count; i++)
                _polygonDebugTriangleVisited.Add(false);

            int seedIndex = FindSeedPolygonDebugTriangle(world);
            if (seedIndex < 0)
                return;

            _polygonDebugTriangleQueue.Add(seedIndex);
            _polygonDebugTriangleVisited[seedIndex] = true;

            for (int queueIndex = 0; queueIndex < _polygonDebugTriangleQueue.Count; queueIndex++)
            {
                int currentIndex = _polygonDebugTriangleQueue[queueIndex];
                MyTriangle_Vertex_Normals current = _polygonDebugTriangles[currentIndex];
                _polygonDebugConnectedTriangles.Add(current);

                for (int candidateIndex = 0; candidateIndex < count; candidateIndex++)
                {
                    if (_polygonDebugTriangleVisited[candidateIndex])
                        continue;

                    if (!ArePolygonDebugTrianglesConnected(current, _polygonDebugTriangles[candidateIndex], world))
                        continue;

                    _polygonDebugTriangleVisited[candidateIndex] = true;
                    _polygonDebugTriangleQueue.Add(candidateIndex);
                }
            }

            if (_polygonDebugConnectedTriangles.Count == 0)
                return;

            _polygonDebugTriangles.Clear();
            for (int i = 0; i < _polygonDebugConnectedTriangles.Count; i++)
                _polygonDebugTriangles.Add(_polygonDebugConnectedTriangles[i]);
        }

        int FindSeedPolygonDebugTriangle(MatrixD world)
        {
            Vector2D seedPoint = Vector2D.Zero;
            int bestIndex = -1;
            double bestDistanceSq = double.MaxValue;

            for (int i = 0; i < _polygonDebugTriangles.Count; i++)
            {
                MyTriangle_Vertex_Normals triangle = _polygonDebugTriangles[i];
                Vector2D a = ProjectPolygonDebugPoint(TransformPolygonDebugVertex(triangle.Vertices.Vertex0, world));
                Vector2D b = ProjectPolygonDebugPoint(TransformPolygonDebugVertex(triangle.Vertices.Vertex1, world));
                Vector2D c = ProjectPolygonDebugPoint(TransformPolygonDebugVertex(triangle.Vertices.Vertex2, world));

                if (IsPointInProjectedTriangle(seedPoint, a, b, c))
                    return i;

                double distanceSq = DistanceSquaredToProjectedTriangle(seedPoint, a, b, c);
                if (distanceSq < bestDistanceSq)
                {
                    bestDistanceSq = distanceSq;
                    bestIndex = i;
                }
            }

            return bestDistanceSq <= PolygonDebugConnectionTolerance * PolygonDebugConnectionTolerance ? bestIndex : -1;
        }

        bool ArePolygonDebugTrianglesConnected(MyTriangle_Vertex_Normals a, MyTriangle_Vertex_Normals b, MatrixD world)
        {
            Vector2D a0 = ProjectPolygonDebugPoint(TransformPolygonDebugVertex(a.Vertices.Vertex0, world));
            Vector2D a1 = ProjectPolygonDebugPoint(TransformPolygonDebugVertex(a.Vertices.Vertex1, world));
            Vector2D a2 = ProjectPolygonDebugPoint(TransformPolygonDebugVertex(a.Vertices.Vertex2, world));
            Vector2D b0 = ProjectPolygonDebugPoint(TransformPolygonDebugVertex(b.Vertices.Vertex0, world));
            Vector2D b1 = ProjectPolygonDebugPoint(TransformPolygonDebugVertex(b.Vertices.Vertex1, world));
            Vector2D b2 = ProjectPolygonDebugPoint(TransformPolygonDebugVertex(b.Vertices.Vertex2, world));

            return AreProjectedTriangleEdgesClose(a0, a1, b0, b1) ||
                AreProjectedTriangleEdgesClose(a0, a1, b1, b2) ||
                AreProjectedTriangleEdgesClose(a0, a1, b2, b0) ||
                AreProjectedTriangleEdgesClose(a1, a2, b0, b1) ||
                AreProjectedTriangleEdgesClose(a1, a2, b1, b2) ||
                AreProjectedTriangleEdgesClose(a1, a2, b2, b0) ||
                AreProjectedTriangleEdgesClose(a2, a0, b0, b1) ||
                AreProjectedTriangleEdgesClose(a2, a0, b1, b2) ||
                AreProjectedTriangleEdgesClose(a2, a0, b2, b0);
        }

        static bool AreProjectedTriangleEdgesClose(Vector2D a0, Vector2D a1, Vector2D b0, Vector2D b1)
        {
            double toleranceSq = PolygonDebugConnectionTolerance * PolygonDebugConnectionTolerance;
            return Vector2D.DistanceSquared(a0, b0) <= toleranceSq && Vector2D.DistanceSquared(a1, b1) <= toleranceSq ||
                Vector2D.DistanceSquared(a0, b1) <= toleranceSq && Vector2D.DistanceSquared(a1, b0) <= toleranceSq;
        }

        void DrawPolygonDebugTriangleCloud()
        {
            if (_polygonDebugEntity == null || _polygonDebugTriangles.Count == 0)
                return;

            MatrixD world = _polygonDebugEntity.WorldMatrix;
            var color = new Color(75, 210, 255, 190);
            const float thickness = 0.0025f;

            for (int i = 0; i < _polygonDebugTriangles.Count; i++)
            {
                MyTriangle_Vertex_Normals triangle = _polygonDebugTriangles[i];
                Vector3D v0 = TransformPolygonDebugVertex(triangle.Vertices.Vertex0, world);
                Vector3D v1 = TransformPolygonDebugVertex(triangle.Vertices.Vertex1, world);
                Vector3D v2 = TransformPolygonDebugVertex(triangle.Vertices.Vertex2, world);
                DrawWorldDebugLine(v0, v1, thickness, color);
                DrawWorldDebugLine(v1, v2, thickness, color);
                DrawWorldDebugLine(v2, v0, thickness, color);
            }
        }

        static Vector3D TransformPolygonDebugVertex(Vector3 vertex, MatrixD world)
        {
            return Vector3D.Transform(new Vector3D(vertex.X, vertex.Y, vertex.Z), world);
        }

        void DrawPolygonDebugAcceptedScreenFill()
        {
            if (_polygonDebugEntity == null || _polygonDebugTriangles.Count == 0)
                return;

            MatrixD world = _polygonDebugEntity.WorldMatrix;
            var color = new Color(45, 185, 255, 34);
            for (int i = 0; i < _polygonDebugTriangles.Count; i++)
            {
                MyTriangle_Vertex_Normals triangle = _polygonDebugTriangles[i];
                DrawFilledPolygonDebugTriangle(
                    TransformPolygonDebugVertex(triangle.Vertices.Vertex0, world),
                    TransformPolygonDebugVertex(triangle.Vertices.Vertex1, world),
                    TransformPolygonDebugVertex(triangle.Vertices.Vertex2, world),
                    color,
                    false);
            }
        }

        void DrawPolygonDebugFinalCursorRegionFill()
        {
            if (_polygonDebugEntity == null || _polygonDebugTriangles.Count == 0)
                return;

            MatrixD world = _polygonDebugEntity.WorldMatrix;
            var color = new Color(80, 255, 150, 58);
            for (int i = 0; i < _polygonDebugTriangles.Count; i++)
            {
                MyTriangle_Vertex_Normals triangle = _polygonDebugTriangles[i];
                DrawFilledPolygonDebugTriangle(
                    TransformPolygonDebugVertex(triangle.Vertices.Vertex0, world),
                    TransformPolygonDebugVertex(triangle.Vertices.Vertex1, world),
                    TransformPolygonDebugVertex(triangle.Vertices.Vertex2, world),
                    color,
                    true);
            }
        }

        void DrawFilledPolygonDebugTriangle(Vector3D v0, Vector3D v1, Vector3D v2, Color color, bool applyOccluderCull)
        {
            Vector2D p0 = ProjectPolygonDebugPoint(v0);
            Vector2D p1 = ProjectPolygonDebugPoint(v1);
            Vector2D p2 = ProjectPolygonDebugPoint(v2);
            double minY = Math.Min(p0.Y, Math.Min(p1.Y, p2.Y));
            double maxY = Math.Max(p0.Y, Math.Max(p1.Y, p2.Y));
            double height = maxY - minY;
            if (height <= 0.0001)
                return;

            double stripHeight = height / PolygonDebugTriangleFillStrips;
            float billboardHeight = (float)Math.Max(stripHeight * 0.92, 0.001);
            for (int i = 0; i < PolygonDebugTriangleFillStrips; i++)
            {
                double y = minY + (i + 0.5) * stripHeight;
                double x0;
                double x1;
                if (!TryGetPolygonDebugTriangleScanline(p0, p1, p2, y, out x0, out x1))
                    continue;

                double width = x1 - x0;
                if (width <= 0.0001)
                    continue;

                Vector2D sample = new Vector2D((x0 + x1) * 0.5, y);
                if (applyOccluderCull && IsProjectedPolygonDebugPointOccluded(sample))
                    continue;

                Vector3D center = _polygonDebugSeed + _polygonDebugAxisA * sample.X + _polygonDebugAxisB * sample.Y;
                MyTransparentGeometry.AddBillboardOriented(
                    CalibrationDebugSquareMaterial,
                    color.ToVector4(),
                    center + _polygonDebugNormal * 0.004,
                    _polygonDebugAxisA,
                    _polygonDebugAxisB,
                    (float)width,
                    billboardHeight);
            }
        }

        static bool TryGetPolygonDebugTriangleScanline(Vector2D p0, Vector2D p1, Vector2D p2, double y, out double minX, out double maxX)
        {
            minX = 0.0;
            maxX = 0.0;
            double a = 0.0;
            double b = 0.0;
            int count = 0;
            AddPolygonDebugScanlineIntersection(p0, p1, y, ref a, ref b, ref count);
            AddPolygonDebugScanlineIntersection(p1, p2, y, ref a, ref b, ref count);
            AddPolygonDebugScanlineIntersection(p2, p0, y, ref a, ref b, ref count);
            if (count < 2)
                return false;

            minX = Math.Min(a, b);
            maxX = Math.Max(a, b);
            return true;
        }

        static void AddPolygonDebugScanlineIntersection(Vector2D a, Vector2D b, double y, ref double x0, ref double x1, ref int count)
        {
            if (Math.Abs(a.Y - b.Y) <= 0.0000001)
                return;

            double minY = Math.Min(a.Y, b.Y);
            double maxY = Math.Max(a.Y, b.Y);
            if (y < minY || y > maxY)
                return;

            double t = (y - a.Y) / (b.Y - a.Y);
            if (t < -0.00001 || t > 1.00001)
                return;

            double x = a.X + (b.X - a.X) * t;
            if (count == 0)
                x0 = x;
            else if (count == 1)
                x1 = x;
            else
            {
                if (x < x0) x0 = x;
                if (x > x1) x1 = x;
            }
            count++;
        }

        bool IsProjectedPolygonDebugPointOccluded(Vector2D point)
        {
            for (int i = 0; i < _polygonDebugWorldOccluderTriangles.Count; i++)
            {
                PolygonDebugWorldTriangle triangle = _polygonDebugWorldOccluderTriangles[i];
                Vector2D a = ProjectPolygonDebugPoint(triangle.V0);
                Vector2D b = ProjectPolygonDebugPoint(triangle.V1);
                Vector2D c = ProjectPolygonDebugPoint(triangle.V2);
                if (IsPointNearProjectedTriangle(point, a, b, c, PolygonDebugOcclusionProjectedTolerance))
                    return true;
            }

            return false;
        }

        void DrawPolygonDebugMaskOverlay()
        {
            double minA;
            double maxA;
            double minB;
            double maxB;
            GetPolygonDebugEffectiveCullingBounds(out minA, out maxA, out minB, out maxB);

            double width = maxA - minA;
            double height = maxB - minB;
            if (width <= 0.0001 || height <= 0.0001 || _polygonDebugAxisA.LengthSquared() <= 0.000001 || _polygonDebugAxisB.LengthSquared() <= 0.000001)
                return;

            int cellsA;
            int cellsB;
            if (width >= height)
            {
                cellsA = PolygonDebugMaskLongAxisCells;
                cellsB = Math.Max(PolygonDebugMaskMinAxisCells, (int)Math.Round(PolygonDebugMaskLongAxisCells * height / width));
            }
            else
            {
                cellsB = PolygonDebugMaskLongAxisCells;
                cellsA = Math.Max(PolygonDebugMaskMinAxisCells, (int)Math.Round(PolygonDebugMaskLongAxisCells * width / height));
            }

            double stepA = width / cellsA;
            double stepB = height / cellsB;
            float drawWidth = (float)(stepA * 0.92);
            float drawHeight = (float)(stepB * 0.92);
            var finalColor = new Color(80, 255, 145, 72);
            var cutColor = new Color(255, 65, 110, 88);

            for (int b = 0; b < cellsB; b++)
            {
                double localB = minB + (b + 0.5) * stepB;
                for (int a = 0; a < cellsA; a++)
                {
                    double localA = minA + (a + 0.5) * stepA;
                    Vector2D point = new Vector2D(localA, localB);
                    Vector3D worldPoint = PolygonDebugPointFromLocal(localA, localB);
                    bool raw = _polygonDebugManualBoundsMode || IsPointInsidePolygonDebugTriangleCloud(worldPoint);
                    if (!raw)
                        continue;

                    bool cut = IsProjectedPolygonDebugPointOccluded(point);
                    Color color = cut ? cutColor : finalColor;
                    DrawPolygonDebugMaskCell(worldPoint, drawWidth * 0.74f, drawHeight * 0.74f, color);
                }
            }
        }

        void DrawPolygonDebugMaskCell(Vector3D center, float width, float height, Color color)
        {
            MyTransparentGeometry.AddBillboardOriented(
                CalibrationDebugSquareMaterial,
                color.ToVector4(),
                center + _polygonDebugNormal * 0.007,
                _polygonDebugAxisA,
                _polygonDebugAxisB,
                width,
                height);
        }

        void DrawAimLockedPolygonCursor()
        {
            if (_polygonDebugEntity == null || !_polygonDebugManualBoundsMode && _polygonDebugTriangles.Count == 0 || MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return;

            var camera = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D rayOrigin = camera.Translation;
            Vector3D rayDirection = camera.Forward;
            if (rayDirection.LengthSquared() <= 0.000001 || _polygonDebugNormal.LengthSquared() <= 0.000001)
                return;
            rayDirection.Normalize();

            Vector3D normal = _polygonDebugNormal;
            normal.Normalize();
            Vector3D planeSeed = _polygonDebugSeed + normal * _polygonDebugCalibrationDepthOffset;

            double denom = Vector3D.Dot(rayDirection, normal);
            if (Math.Abs(denom) <= 0.000001)
                return;

            double rayDistance = Vector3D.Dot(planeSeed - rayOrigin, normal) / denom;
            if (rayDistance <= 0.0)
                return;

            Vector3D planeHit = rayOrigin + rayDirection * rayDistance;
            if (!DoesPolygonDebugCursorOverlapBounds(planeHit))
                return;

            var cursorColor = new Color(255, 250, 90, 255);
            DrawClippedPolygonDebugCursorBillboard(planeHit, cursorColor);
        }

        bool DoesPolygonDebugCursorOverlapBounds(Vector3D center)
        {
            Vector2D local = ProjectPolygonDebugPoint(center);
            double halfSize = PanelDiscoveryCursorHalfSize;
            double halfThickness = PanelDiscoveryCursorThickness * 0.5;
            return DoesPolygonDebugLocalRectOverlapBounds(local.X - halfSize, local.X + halfSize, local.Y - halfThickness, local.Y + halfThickness) ||
                DoesPolygonDebugLocalRectOverlapBounds(local.X - halfThickness, local.X + halfThickness, local.Y - halfSize, local.Y + halfSize);
        }

        bool DoesPolygonDebugLocalRectOverlapBounds(double minA, double maxA, double minB, double maxB)
        {
            double finalMinA;
            double finalMaxA;
            double finalMinB;
            double finalMaxB;
            GetPolygonDebugEffectiveCullingBounds(out finalMinA, out finalMaxA, out finalMinB, out finalMaxB);

            return maxA >= finalMinA && minA <= finalMaxA &&
                maxB >= finalMinB && minB <= finalMaxB;
        }

        void DrawClippedPolygonDebugCursorBillboard(Vector3D center, Color color)
        {
            Vector2D local = ProjectPolygonDebugPoint(center);
            double halfSize = PanelDiscoveryCursorHalfSize;
            double halfThickness = PanelDiscoveryCursorThickness * 0.5;
            DrawClippedPolygonDebugCursorBar(local.X - halfSize, local.X + halfSize, local.Y - halfThickness, local.Y + halfThickness, color);
            DrawClippedPolygonDebugCursorBar(local.X - halfThickness, local.X + halfThickness, local.Y - halfSize, local.Y + halfSize, color);
        }

        void DrawClippedPolygonDebugCursorBar(double minA, double maxA, double minB, double maxB, Color color)
        {
            double finalMinA;
            double finalMaxA;
            double finalMinB;
            double finalMaxB;
            GetPolygonDebugEffectiveCullingBounds(out finalMinA, out finalMaxA, out finalMinB, out finalMaxB);

            if (minA < finalMinA) minA = finalMinA;
            if (maxA > finalMaxA) maxA = finalMaxA;
            if (minB < finalMinB) minB = finalMinB;
            if (maxB > finalMaxB) maxB = finalMaxB;

            double width = maxA - minA;
            double height = maxB - minB;
            if (width <= 0.0001 || height <= 0.0001)
                return;

            Vector3D center = _polygonDebugSeed +
                _polygonDebugAxisA * ((minA + maxA) * 0.5) +
                _polygonDebugAxisB * ((minB + maxB) * 0.5) +
                _polygonDebugNormal * _polygonDebugCalibrationDepthOffset;

            MyTransparentGeometry.AddBillboardOriented(
                CalibrationDebugSquareMaterial,
                color.ToVector4(),
                center,
                _polygonDebugAxisA,
                _polygonDebugAxisB,
                (float)width,
                (float)height);
        }

        bool IsPointInsidePolygonDebugTriangleCloud(Vector3D point)
        {
            if (_polygonDebugEntity == null || _polygonDebugTriangles.Count == 0)
                return false;

            MatrixD world = _polygonDebugEntity.WorldMatrix;
            Vector3D projectedPoint = point - _polygonDebugNormal * Vector3D.Dot(point - _polygonDebugSeed, _polygonDebugNormal);
            Vector2D p = ProjectPolygonDebugPoint(projectedPoint);

            for (int i = 0; i < _polygonDebugTriangles.Count; i++)
            {
                MyTriangle_Vertex_Normals triangle = _polygonDebugTriangles[i];
                Vector2D a = ProjectPolygonDebugPoint(TransformPolygonDebugVertex(triangle.Vertices.Vertex0, world));
                Vector2D b = ProjectPolygonDebugPoint(TransformPolygonDebugVertex(triangle.Vertices.Vertex1, world));
                Vector2D c = ProjectPolygonDebugPoint(TransformPolygonDebugVertex(triangle.Vertices.Vertex2, world));
                if (IsPointInProjectedTriangle(p, a, b, c))
                    return true;
            }

            return false;
        }

        Vector2D ProjectPolygonDebugPoint(Vector3D point)
        {
            Vector3D delta = point - _polygonDebugSeed;
            return new Vector2D(Vector3D.Dot(delta, _polygonDebugAxisA), Vector3D.Dot(delta, _polygonDebugAxisB));
        }

        static bool IsPointInProjectedTriangle(Vector2D p, Vector2D a, Vector2D b, Vector2D c)
        {
            double d1 = ProjectedTriangleSign(p, a, b);
            double d2 = ProjectedTriangleSign(p, b, c);
            double d3 = ProjectedTriangleSign(p, c, a);
            const double tolerance = 0.00008;
            bool hasNegative = d1 < -tolerance || d2 < -tolerance || d3 < -tolerance;
            bool hasPositive = d1 > tolerance || d2 > tolerance || d3 > tolerance;
            return !(hasNegative && hasPositive);
        }

        static double ProjectedTriangleSign(Vector2D p1, Vector2D p2, Vector2D p3)
        {
            return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
        }

        bool IsPolygonDebugCursorCoveredByRaisedGeometry(Vector3D planeHit, Vector3D towardCameraNormal)
        {
            if (_polygonDebugEntity == null)
                return false;

            try
            {
                _polygonDebugOcclusionTriangles.Clear();
                MatrixD inverseWorld = MatrixD.Invert(_polygonDebugEntity.WorldMatrix);
                Vector3D probeCenterWorld = planeHit + towardCameraNormal * (PolygonDebugOcclusionDepth * 0.5);
                Vector3D probeCenterLocalD = Vector3D.Transform(probeCenterWorld, inverseWorld);
                var probeCenterLocal = new Vector3((float)probeCenterLocalD.X, (float)probeCenterLocalD.Y, (float)probeCenterLocalD.Z);
                var sphere = new BoundingSphere(probeCenterLocal, PolygonDebugOcclusionProbeRadius);
                Vector3? normalFilter = null;
                float? maxAngle = null;
                _polygonDebugEntity.GetTrianglesIntersectingSphere(ref sphere, normalFilter, maxAngle, _polygonDebugOcclusionTriangles, 96);

                MatrixD world = _polygonDebugEntity.WorldMatrix;
                for (int i = 0; i < _polygonDebugOcclusionTriangles.Count; i++)
                {
                if (IsOccludingRaisedTriangle(_polygonDebugOcclusionTriangles[i], world, planeHit, towardCameraNormal))
                    return true;
            }

            for (int i = 0; i < _polygonDebugWorldOccluderTriangles.Count; i++)
            {
                if (IsOccludingRaisedWorldTriangle(_polygonDebugWorldOccluderTriangles[i], planeHit, towardCameraNormal))
                    return true;
            }
        }
            catch
            {
                return false;
            }

            return false;
        }

        bool IsOccludingRaisedTriangle(MyTriangle_Vertex_Normals triangle, MatrixD world, Vector3D planeHit, Vector3D towardCameraNormal)
        {
            Vector3D v0 = TransformPolygonDebugVertex(triangle.Vertices.Vertex0, world);
            Vector3D v1 = TransformPolygonDebugVertex(triangle.Vertices.Vertex1, world);
            Vector3D v2 = TransformPolygonDebugVertex(triangle.Vertices.Vertex2, world);
            Vector3D center = (v0 + v1 + v2) / 3.0;
            Vector3D delta = center - planeHit;
            double height = Vector3D.Dot(delta, towardCameraNormal);
            if (height <= PolygonDebugOcclusionSurfaceClearance || height > PolygonDebugOcclusionDepth)
                return false;

            double localA = Vector3D.Dot(delta, _polygonDebugAxisA);
            double localB = Vector3D.Dot(delta, _polygonDebugAxisB);
            double lateralLimit = PolygonDebugOcclusionProbeRadius;
            if (localA * localA + localB * localB > lateralLimit * lateralLimit)
                return false;

            Vector3D faceNormal = Vector3D.Cross(v1 - v0, v2 - v0);
            if (faceNormal.LengthSquared() <= 0.000001)
                return false;
            faceNormal.Normalize();

            double screenNormalDot = Math.Abs(Vector3D.Dot(faceNormal, _polygonDebugNormal));
            if (screenNormalDot >= PolygonDebugOcclusionNormalDotThreshold)
                return false;

            Vector2D p = ProjectPolygonDebugPoint(planeHit);
            Vector2D a = ProjectPolygonDebugPoint(v0);
            Vector2D b = ProjectPolygonDebugPoint(v1);
            Vector2D c = ProjectPolygonDebugPoint(v2);
            return IsPointNearProjectedTriangle(p, a, b, c, PolygonDebugOcclusionProjectedTolerance);
        }

        bool IsOccludingRaisedWorldTriangle(PolygonDebugWorldTriangle triangle, Vector3D planeHit, Vector3D towardCameraNormal)
        {
            Vector3D center = (triangle.V0 + triangle.V1 + triangle.V2) / 3.0;
            Vector3D delta = center - planeHit;
            double height = Vector3D.Dot(delta, towardCameraNormal);
            if (height <= PolygonDebugOcclusionSurfaceClearance || height > PolygonDebugSpatialOccluderDepth)
                return false;

            Vector2D p = ProjectPolygonDebugPoint(planeHit);
            Vector2D a = ProjectPolygonDebugPoint(triangle.V0);
            Vector2D b = ProjectPolygonDebugPoint(triangle.V1);
            Vector2D c = ProjectPolygonDebugPoint(triangle.V2);
            return IsPointNearProjectedTriangle(p, a, b, c, PolygonDebugOcclusionProjectedTolerance);
        }

        static bool IsPointNearProjectedTriangle(Vector2D p, Vector2D a, Vector2D b, Vector2D c, double tolerance)
        {
            if (IsPointInProjectedTriangle(p, a, b, c))
                return true;

            double toleranceSq = tolerance * tolerance;
            return DistanceSquaredToProjectedSegment(p, a, b) <= toleranceSq ||
                DistanceSquaredToProjectedSegment(p, b, c) <= toleranceSq ||
                DistanceSquaredToProjectedSegment(p, c, a) <= toleranceSq;
        }

        static double DistanceSquaredToProjectedTriangle(Vector2D p, Vector2D a, Vector2D b, Vector2D c)
        {
            if (IsPointInProjectedTriangle(p, a, b, c))
                return 0.0;

            return Math.Min(
                DistanceSquaredToProjectedSegment(p, a, b),
                Math.Min(
                    DistanceSquaredToProjectedSegment(p, b, c),
                    DistanceSquaredToProjectedSegment(p, c, a)));
        }

        static double DistanceSquaredToProjectedSegment(Vector2D p, Vector2D a, Vector2D b)
        {
            Vector2D ab = b - a;
            double lengthSq = ab.LengthSquared();
            if (lengthSq <= 0.00000001)
                return Vector2D.DistanceSquared(p, a);

            double t = Vector2D.Dot(p - a, ab) / lengthSq;
            if (t < 0.0) t = 0.0;
            if (t > 1.0) t = 1.0;
            Vector2D closest = a + ab * t;
            return Vector2D.DistanceSquared(p, closest);
        }

        void DrawPolygonDebugSubpartWiremesh()
        {
            var color = new Color(255, 80, 210, 210);
            const float thickness = 0.002f;

            for (int i = 0; i < _polygonDebugOcclusionEntities.Count; i++)
            {
                IMyEntity entity = _polygonDebugOcclusionEntities[i];
                if (entity == null || entity.Model == null)
                    continue;

                DrawPolygonDebugSubpartTriangles(entity, color, thickness);
            }

            var spatialColor = new Color(255, 40, 80, 235);
            for (int i = 0; i < _polygonDebugWorldOccluderTriangles.Count; i++)
            {
                PolygonDebugWorldTriangle triangle = _polygonDebugWorldOccluderTriangles[i];
                DrawWorldDebugLine(triangle.V0, triangle.V1, thickness, spatialColor);
                DrawWorldDebugLine(triangle.V1, triangle.V2, thickness, spatialColor);
                DrawWorldDebugLine(triangle.V2, triangle.V0, thickness, spatialColor);
            }
        }

        void DrawPolygonDebugSubpartTriangles(IMyEntity subpart, Color color, float thickness)
        {
            try
            {
                _polygonDebugSubpartTrimTriangles.Clear();
                MatrixD inverseWorld = MatrixD.Invert(subpart.WorldMatrix);
                Vector3D localSeedD = Vector3D.Transform(_polygonDebugSeed, inverseWorld);
                var localSeed = new Vector3((float)localSeedD.X, (float)localSeedD.Y, (float)localSeedD.Z);
                var sphere = new BoundingSphere(localSeed, PolygonDebugSubpartTrimProbeRadius);
                Vector3? normalFilter = null;
                float? maxAngle = null;
                subpart.GetTrianglesIntersectingSphere(ref sphere, normalFilter, maxAngle, _polygonDebugSubpartTrimTriangles, 256);

                MatrixD world = subpart.WorldMatrix;
                for (int i = 0; i < _polygonDebugSubpartTrimTriangles.Count; i++)
                {
                    MyTriangle_Vertex_Normals triangle = _polygonDebugSubpartTrimTriangles[i];
                    Vector3D v0 = TransformPolygonDebugVertex(triangle.Vertices.Vertex0, world);
                    Vector3D v1 = TransformPolygonDebugVertex(triangle.Vertices.Vertex1, world);
                    Vector3D v2 = TransformPolygonDebugVertex(triangle.Vertices.Vertex2, world);
                    DrawWorldDebugLine(v0, v1, thickness, color);
                    DrawWorldDebugLine(v1, v2, thickness, color);
                    DrawWorldDebugLine(v2, v0, thickness, color);
                }
            }
            catch
            {
                _polygonDebugSubpartTrimTriangles.Clear();
            }
        }

        double WalkRealPolygonSurfaceDistance(Vector3D seed, Vector3D normal, Vector3D direction)
        {
            if (direction.LengthSquared() <= 0.000001 || _polygonDebugEntity == null)
                return 0.0;
            direction.Normalize();

            double acceptedDistance = 0.0;
            double rejectedDistance = 0.0;
            for (int step = 1; step <= PolygonDebugMaxSteps; step++)
            {
                double nextDistance = step * PolygonDebugStep;
                if (!IsSameRealPolygonSurfaceAt(seed + direction * nextDistance, seed, normal))
                {
                    rejectedDistance = nextDistance;
                    break;
                }

                acceptedDistance = nextDistance;
            }

            if (rejectedDistance > acceptedDistance)
                acceptedDistance = RefineRealPolygonSurfaceEdge(seed, normal, direction, acceptedDistance, rejectedDistance);

            return acceptedDistance;
        }

        double RefineRealPolygonSurfaceEdge(Vector3D seed, Vector3D normal, Vector3D direction, double acceptedDistance, double rejectedDistance)
        {
            double low = acceptedDistance;
            double high = rejectedDistance;

            for (int i = 0; i < PolygonDebugRefineSteps; i++)
            {
                double mid = (low + high) * 0.5;
                if (IsSameRealPolygonSurfaceAt(seed + direction * mid, seed, normal))
                    low = mid;
                else
                    high = mid;
            }

            return low;
        }

        bool IsSameRealPolygonSurfaceAt(Vector3D target, Vector3D seed, Vector3D normal)
        {
            Vector3D hitPosition;
            Vector3D hitNormal;
            if (!TryGetRealPolygonSurfaceProbeHit(target, normal, out hitPosition, out hitNormal))
                return false;

            if (hitNormal.LengthSquared() <= 0.000001)
                return false;
            hitNormal.Normalize();

            double normalDot = Math.Abs(Vector3D.Dot(hitNormal, normal));
            if (normalDot < PolygonDebugNormalDotThreshold)
                return false;

            double planeDistance = Math.Abs(Vector3D.Dot(hitPosition - seed, normal));
            if (planeDistance > PolygonDebugPlaneTolerance)
                return false;

            Vector3D lateral = hitPosition - target - normal * Vector3D.Dot(hitPosition - target, normal);
            return lateral.LengthSquared() <= PolygonDebugStep * PolygonDebugStep * 0.56;
        }

        bool TryGetRealPolygonSurfaceProbeHit(Vector3D target, Vector3D normal, out Vector3D hitPosition, out Vector3D hitNormal)
        {
            hitPosition = Vector3D.Zero;
            hitNormal = Vector3D.Zero;
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null || _polygonDebugEntity == null)
                return false;

            Vector3D toCamera = MyAPIGateway.Session.Camera.WorldMatrix.Translation - target;
            Vector3D probeNormal = Vector3D.Dot(toCamera, normal) >= 0.0 ? normal : -normal;
            return TryGetGlobalModelSegmentHit(_polygonDebugEntity, target + probeNormal * PolygonDebugCastDepth, target - probeNormal * PolygonDebugCastDepth, out hitPosition, out hitNormal);
        }
    }
}
