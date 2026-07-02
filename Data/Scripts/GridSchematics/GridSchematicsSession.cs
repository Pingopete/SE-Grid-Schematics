using Sandbox.ModAPI;
using Sandbox.Game;
using Sandbox.Game.Entities;
using VRage.Game.Components;
using VRage.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using VRage.Game;
using VRage.Utils;
using VRageRender;

namespace GridSchematics
{
    public enum PersistedScanLoadStatus
    {
        Loaded,
        Missing,
        Invalid,
        Obsolete
    }
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public partial class GridSchematicsSession : MySessionComponentBase
    {
        const string PANEL_TAG = "[GRID_SCHEMATIC]";
        const int PANEL_SCAN_TICK_INTERVAL = 600;
        // v12: sample lines carry depth intervals (index|hitCount|s:e,s:e scaled 1/10000) instead of
        // the collapsed float summary; the summary fields are derived from the intervals on load.
        const int PERSISTED_SCAN_VERSION = 12;
        const float PersistedIntervalScale = 10000f;
        const bool PANEL_CURSOR_LIMIT_SAMPLE_RATE = false;
        const long PANEL_CURSOR_SAMPLE_INTERVAL_TICKS = 10000000L / 30L;
        const ushort PANEL_INTERACTION_INPUT_CHANNEL = 38473;
        static readonly MyStringId CursorMaterial = MyStringId.GetOrCompute("Square");
        static readonly MyStringId PanelCursorMaterial = MyStringId.GetOrCompute("GridSchematics_Cursor");
        static readonly MyStringId CalibrationDebugSquareMaterial = MyStringId.GetOrCompute("GridSchematics_DepthDebugSquare");
        public const string TEXT_SURFACE_SCRIPT_ID = "Grid_Schematics";

        public static GridSchematicsSession Instance { get; private set; }

        bool _initialized;
        int _tickCounter;
        int _aimDebugNotificationCooldown;
        bool _hasPanelCursorDrawData;
        bool _panelInteractionInputBlocked;
        long _lastPanelCursorSampleUtcTicks;
        GridSchematicsLcdApp _panelCursorDrawApp;
        PanelCursorWorldDrawData _panelCursorDrawData;
        bool _panelCursorDrawPressed;
        bool _panelCursorDrawSecondaryPressed;
        PanelInteractionInputBlocker _panelInteractionInputBlocker;
        readonly List<string> _panelInteractionBlockedControls = new List<string>(2);
        readonly List<string> _panelInteractionBlockedShipControls = new List<string>(16);
        readonly List<string> _panelInteractionTransitionControls = new List<string>(16);
        bool _panelInteractionBlockedWithShipControls;
        readonly Dictionary<long, ConstructMouseCursorState> _constructMouseCursors = new Dictionary<long, ConstructMouseCursorState>();

        readonly List<GridSchematicsLcdApp> _apps = new List<GridSchematicsLcdApp>();
        readonly List<GridSchematicsLcdApp> _surfaceScriptApps = new List<GridSchematicsLcdApp>();
        readonly List<TouchScreenApiAdapter> _unsupportedSurfaceProbes = new List<TouchScreenApiAdapter>();
        readonly List<TouchScreenApiAdapter> _manualCalibrationRenderedInputs = new List<TouchScreenApiAdapter>();
        readonly Dictionary<long, ScanCache> _constructCache = new Dictionary<long, ScanCache>();
        readonly Dictionary<long, SharedGridCursor> _sharedCursors = new Dictionary<long, SharedGridCursor>();

        class ConstructMouseCursorState
        {
            public bool Enabled;
            public bool HasPosition;
            public bool HasPanelFocus;
            public long ActivePanelId;
            public int ActiveSurfaceIndex = -1;
            public Vector2 RawPosition;
            public string Sensitivity = "MID";
        }

        struct PanelSurfaceFrame
        {
            public GridSchematicsLcdApp App;
            public Vector3D TopLeft;
            public Vector3D TopRight;
            public Vector3D BottomRight;
            public Vector3D BottomLeft;
            public Vector3D Normal;
            public Vector3D AxisX;
            public Vector3D AxisY;
            public Vector2 Size;

            public Vector3D Center { get { return (TopLeft + TopRight + BottomRight + BottomLeft) * 0.25; } }
            public Vector3D LeftEdgeCenter { get { return (TopLeft + BottomLeft) * 0.5; } }
            public Vector3D RightEdgeCenter { get { return (TopRight + BottomRight) * 0.5; } }
            public Vector3D TopEdgeCenter { get { return (TopLeft + TopRight) * 0.5; } }
            public Vector3D BottomEdgeCenter { get { return (BottomLeft + BottomRight) * 0.5; } }
        }

        public override void LoadData()
        {
            base.LoadData();
            Instance = this;
            _panelInteractionInputBlocker = new PanelInteractionInputBlocker(PANEL_INTERACTION_INPUT_CHANNEL);
            _panelInteractionInputBlocker.Init();
            RegisterPanelDiscoveryDebugCommand();
            RegisterPanelDiscoveryPolygonDebugCommand();
            RegisterPanelCalibrationDebugCommand();
            RegisterPanelCursorDepthOffsetDebugCommand();
            RegisterPanelLayoutSnapshotCommand();
            RegisterPanelLiveMockupCommand();
            RegisterPanelResolutionDebugCommand();
        }

        public override void UpdateAfterSimulation()
        {
            base.UpdateAfterSimulation();

            if (MyAPIGateway.Session == null)
                return;

            if (!_initialized)
            {
                Initialize();
                _initialized = true;
            }

            _tickCounter++;
            if (_tickCounter % PANEL_SCAN_TICK_INTERVAL == 0)
            {
                RefreshApps();
            }

            for (int i = 0; i < _apps.Count; i++)
            {
                _apps[i].Update(_tickCounter);
            }

            UpdatePanelLiveMockup(_tickCounter);
            UpdatePanelResolutionDebug(_tickCounter);
            UpdatePanelInteractionInputBlocker();
        }

        public override void Draw()
        {
            base.Draw();
            DrawPanelSurfaceCursor();
            UpdateManualCalibrationInputFocusGate();
            UpdateCalibratedDebugPopupInput();
            UpdatePolygonDebugGlobalReset();
            UpdatePanelCursorDepthOffsetDebug();
            DrawPanelCursorDepthOffsetDebug();
            DrawPanelDiscoveryDebug();
            DrawPanelDiscoveryPolygonDebug();
            DrawPanelCalibrationDebug();
            DrawPanelResolutionDebug();
        }

        protected override void UnloadData()
        {
            for (int i = 0; i < _apps.Count; i++)
            {
                if (_apps[i] != null && _apps[i].ConstructCache != null && _apps[i].OwnerBlock != null)
                    SavePersistedScan(_apps[i].ConstructCache, _apps[i].OwnerBlock.CubeGrid);
                _apps[i].Dispose();
            }

            _apps.Clear();
            _surfaceScriptApps.Clear();
            _unsupportedSurfaceProbes.Clear();
            UnregisterPanelDiscoveryDebugCommand();
            UnregisterPanelDiscoveryPolygonDebugCommand();
            UnregisterPanelCalibrationDebugCommand();
            UnregisterPanelCursorDepthOffsetDebugCommand();
            UnregisterPanelLayoutSnapshotCommand();
            UnregisterPanelLiveMockupCommand();
            UnregisterPanelResolutionDebugCommand();
            ClearPanelLiveMockup();
            ClearPanelResolutionDebug();
            SetPanelInteractionInputBlocked(false, false);
            if (_panelInteractionInputBlocker != null)
            {
                _panelInteractionInputBlocker.Dispose();
                _panelInteractionInputBlocker = null;
            }
            TouchScreenApiAdapter.UnloadSharedApi();
            Instance = null;
            base.UnloadData();
        }

        void Initialize()
        {
            RegisterPanelDiscoveryDebugCommand();
            RegisterPanelDiscoveryPolygonDebugCommand();
            RegisterPanelCalibrationDebugCommand();
            RegisterPanelCursorDepthOffsetDebugCommand();
            RegisterPanelLayoutSnapshotCommand();
            RegisterPanelLiveMockupCommand();
            RegisterPanelResolutionDebugCommand();
            RefreshApps();
        }

        void RefreshApps()
        {
            var updatedPanels = new List<IMyTextPanel>();
            var entitySet = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entitySet, entity => entity is IMyTextPanel);

            foreach (var entity in entitySet)
            {
                var panel = entity as IMyTextPanel;
                if (panel != null)
                {
                    var surface = panel as IMyTextSurface;
                    if (surface != null && surface.Script == TEXT_SURFACE_SCRIPT_ID)
                    {
                        continue;
                    }

                    if (IsTaggedPanelAppSurface(panel))
                    {
                        updatedPanels.Add(panel);
                    }
                }
            }

            for (int i = _apps.Count - 1; i >= 0; i--)
            {
                if (_apps[i].Panel == null || !_apps[i].Panel.IsFunctional || !IsTaggedPanelAppSurface(_apps[i].Panel))
                {
                    ClearCursorStateForApp(_apps[i]);
                    _apps[i].Dispose();
                    _apps.RemoveAt(i);
                }
            }

            foreach (var panel in updatedPanels)
            {
                bool found = false;
                for (int i = 0; i < _apps.Count; i++)
                {
                    if (_apps[i].Panel.EntityId == panel.EntityId)
                    {
                        found = true;
                        break;
                    }
                }
                if (!found)
                {
                    _apps.Add(new GridSchematicsLcdApp(panel, this));
                }
            }
        }

        void DrawAimCursorHud()
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return;

            GridSchematicsLcdApp activeApp = FindAimCursorApp();
            if (activeApp == null)
                return;

            var input = activeApp.TouchInput;
            var camera = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D center = camera.Translation + camera.Forward * 0.18;
            Vector3D right = camera.Right;
            Vector3D up = camera.Up;
            Vector3D diagA = right + up;
            Vector3D diagB = right - up;
            right.Normalize();
            up.Normalize();
            diagA.Normalize();
            diagB.Normalize();

            float half = 0.00060f;
            float thickness = 0.00011f;
            var color = input.IsPressed ? new Color(255, 216, 70, 255) : new Color(20, 220, 255, 255);

            if (input.IsSecondaryPressed)
            {
            DrawAimCursorSegment(center, diagA, half * 2f, thickness, color);
            DrawAimCursorSegment(center, diagB, half * 2f, thickness, color);
                return;
            }

            DrawAimCursorSegment(center, right, half * 2f, thickness, color);
            DrawAimCursorSegment(center, up, half * 2f, thickness, color);

        }

        void DrawPanelSurfaceCursor()
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return;

            if (_panelCursorDepthOffsetEnabled)
            {
                ClearPanelCursorDrawCache();
                ClearConstructMouseCursorFocus();
                return;
            }

            PanelCursorWorldDrawData cursor;
            GridSchematicsLcdApp activeApp;
            bool allowPanelInput = true;
            bool hasAimedPanel = TryFindPanelSurfaceCursorApp(out activeApp, out cursor) && activeApp != null && activeApp.TouchInput != null;
            bool forceAimCursor = hasAimedPanel && activeApp.IsCursorCalibrationBlocking;
            if (forceAimCursor)
            {
                ClearConstructMouseCursorFocus();
            }
            else if (hasAimedPanel && IsConstructMouseControlEnabled(activeApp.ConstructId))
            {
                if (!TryUpdateMouseControlledPanelCursor(activeApp, cursor, out activeApp, out cursor))
                    return;
            }
            else if (!hasAimedPanel)
            {
                ClearConstructMouseCursorFocus();
                if (!TryFindVisibleMouseControlledPanelCursorForRender(out activeApp, out cursor))
                    return;
                allowPanelInput = false;
            }

            _panelCursorDrawPressed = allowPanelInput && IsCurrentLeftMousePressed();
            _panelCursorDrawSecondaryPressed = allowPanelInput && IsCurrentRightMousePressed();
            if (allowPanelInput && activeApp != null && activeApp.TouchInput != null && IsPanelCursorPointInsideBounds(cursor))
                activeApp.TouchInput.CaptureFrameButtonState(cursor.RawSurfacePoint, _panelCursorDrawPressed, _panelCursorDrawSecondaryPressed);

            Vector3D normal = cursor.Normal;
            if (normal.LengthSquared() <= 0.000001)
                return;
            normal.Normalize();
            if (Vector3D.Dot(MyAPIGateway.Session.Camera.WorldMatrix.Translation - cursor.PlaneHit, normal) < 0.0)
                normal = -normal;

            bool isSecondaryPressed = _panelCursorDrawSecondaryPressed;
            var color = new Color(255, 255, 255, 255);

            if (isSecondaryPressed)
            {
                DrawPanelCursorTexture(cursor, normal, color, false);
                return;
            }

            DrawPanelCursorTexture(cursor, normal, color, false);
        }

        static bool IsPanelCursorPointInsideBounds(PanelCursorWorldDrawData cursor)
        {
            return cursor.LocalA >= cursor.MinA &&
                cursor.LocalA <= cursor.MaxA &&
                cursor.LocalB >= cursor.MinB &&
                cursor.LocalB <= cursor.MaxB;
        }

        void UpdatePanelInteractionInputBlocker()
        {
            bool blockInput = ShouldBlockVanillaPanelInteraction();
            bool blockShipControls = blockInput && IsPlayerInShipControlSeat();
            SetPanelInteractionInputBlocked(blockInput, blockShipControls);
        }

        bool ShouldBlockVanillaPanelInteraction()
        {
            try
            {
                if (MyAPIGateway.Session == null || MyAPIGateway.Session.Player == null || MyAPIGateway.Gui == null)
                    return false;

                if (MyAPIGateway.Gui.IsCursorVisible)
                    return false;

                if (MyAPIGateway.Session.Player.Character != null &&
                    MyAPIGateway.Session.Player.Character.EquippedTool != null)
                    return false;

                PanelCursorWorldDrawData cursor;
                GridSchematicsLcdApp activeApp;
                if (TryFindPanelSurfaceCursorApp(out activeApp, out cursor) &&
                    activeApp != null &&
                    activeApp.TouchInput != null)
                {
                    return true;
                }

                if (_panelDiscoveryPolygonDebugEnabled)
                {
                    TouchScreenApiAdapter aimedInput;
                    if (TryFindAimedCalibrationInput(null, out aimedInput))
                        return true;
                }

                GridSchematicsLcdApp aimedStoredApp;
                return TryFindAimedStoredCalibrationApp(out aimedStoredApp);
            }
            catch
            {
                return false;
            }
        }

        void SetPanelInteractionInputBlocked(bool blocked, bool blockShipControls)
        {
            try
            {
                if (MyAPIGateway.Session == null || MyAPIGateway.Session.Player == null)
                    return;

                if (_panelInteractionInputBlocker == null)
                    _panelInteractionInputBlocker = new PanelInteractionInputBlocker(PANEL_INTERACTION_INPUT_CHANNEL);

                EnsurePanelInteractionControlLists();

                var playerId = MyAPIGateway.Session.Player.IdentityId;
                var previousControls = _panelInteractionInputBlocked
                    ? (_panelInteractionBlockedWithShipControls ? _panelInteractionBlockedShipControls : _panelInteractionBlockedControls)
                    : null;
                var targetControls = blocked
                    ? (blockShipControls ? _panelInteractionBlockedShipControls : _panelInteractionBlockedControls)
                    : null;

                if (previousControls == null && targetControls == null)
                    return;

                if (previousControls != null && targetControls != null && ReferenceEquals(previousControls, targetControls))
                    return;

                SetPanelInteractionControlsEnabledExcept(previousControls, targetControls, playerId, true);
                SetPanelInteractionControlsEnabledExcept(targetControls, previousControls, playerId, false);
                _panelInteractionInputBlocked = blocked;
                _panelInteractionBlockedWithShipControls = blocked && blockShipControls;
            }
            catch
            {
                _panelInteractionInputBlocked = false;
                _panelInteractionBlockedWithShipControls = false;
            }
        }

        void SetPanelInteractionControlsEnabledExcept(List<string> controls, List<string> except, long playerId, bool enabled)
        {
            if (controls == null || controls.Count == 0)
                return;

            _panelInteractionTransitionControls.Clear();
            for (int i = 0; i < controls.Count; i++)
            {
                string control = controls[i];
                if (!string.IsNullOrEmpty(control) && !ContainsPanelInteractionControl(except, control))
                    _panelInteractionTransitionControls.Add(control);
            }

            if (_panelInteractionTransitionControls.Count > 0)
                _panelInteractionInputBlocker.SetControlsEnabled(_panelInteractionTransitionControls, playerId, enabled);
        }

        static bool ContainsPanelInteractionControl(List<string> controls, string control)
        {
            if (controls == null || string.IsNullOrEmpty(control))
                return false;

            for (int i = 0; i < controls.Count; i++)
            {
                if (string.Equals(controls[i], control, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        void EnsurePanelInteractionControlLists()
        {
            if (_panelInteractionBlockedControls.Count == 0)
            {
                _panelInteractionBlockedControls.Add(MyControlsSpace.PRIMARY_TOOL_ACTION.String);
                _panelInteractionBlockedControls.Add(MyControlsSpace.SECONDARY_TOOL_ACTION.String);
            }

            if (_panelInteractionBlockedShipControls.Count == 0)
            {
                _panelInteractionBlockedShipControls.Add(MyControlsSpace.PRIMARY_TOOL_ACTION.String);
                _panelInteractionBlockedShipControls.Add(MyControlsSpace.SECONDARY_TOOL_ACTION.String);
                _panelInteractionBlockedShipControls.Add(MyControlsSpace.ROLL_LEFT.String);
                _panelInteractionBlockedShipControls.Add(MyControlsSpace.ROLL_RIGHT.String);
                _panelInteractionBlockedShipControls.Add(MyControlsSpace.ROTATION_LEFT.String);
                _panelInteractionBlockedShipControls.Add(MyControlsSpace.ROTATION_RIGHT.String);
                _panelInteractionBlockedShipControls.Add(MyControlsSpace.ROTATION_UP.String);
                _panelInteractionBlockedShipControls.Add(MyControlsSpace.ROTATION_DOWN.String);
                _panelInteractionBlockedShipControls.Add(MyControlsSpace.LOOK_LEFT.String);
                _panelInteractionBlockedShipControls.Add(MyControlsSpace.LOOK_RIGHT.String);
                _panelInteractionBlockedShipControls.Add(MyControlsSpace.LOOK_UP.String);
                _panelInteractionBlockedShipControls.Add(MyControlsSpace.LOOK_DOWN.String);
            }
        }

        static void DrawPanelCursorTexture(PanelCursorWorldDrawData cursor, Vector3D normal, Color color, bool enlarged)
        {
            const double cursorSize = PanelDiscoveryCursorHalfSize * 1.96;
            const double cursorSurfaceOffset = 0.0008;
            const double hotspotX = 0.16;
            const double hotspotY = 0.18;
            const double textureRotation = -Math.PI * 0.25;

            double width = enlarged ? cursorSize * 1.22 : cursorSize;
            double height = width;
            double cos = Math.Cos(textureRotation);
            double sin = Math.Sin(textureRotation);
            Vector3D baseAxisA;
            Vector3D baseAxisB;
            if (!TryBuildPanelCursorTextureBaseAxes(cursor, normal, out baseAxisA, out baseAxisB))
                return;

            Vector3D axisA = baseAxisA * cos - baseAxisB * sin;
            Vector3D axisB = baseAxisA * sin + baseAxisB * cos;
            Vector3D center = cursor.PlaneHit +
                axisA * ((0.5 - hotspotX) * width) +
                axisB * ((hotspotY - 0.5) * height) +
                normal * cursorSurfaceOffset;

            MyTransparentGeometry.AddBillboardOriented(
                PanelCursorMaterial,
                color.ToVector4(),
                center,
                axisA,
                axisB,
                (float)width,
                (float)height);
        }

        static bool TryBuildPanelCursorTextureBaseAxes(PanelCursorWorldDrawData cursor, Vector3D normal, out Vector3D axisA, out Vector3D axisB)
        {
            axisA = Vector3D.Zero;
            axisB = Vector3D.Zero;

            if (normal.LengthSquared() <= 0.000001)
                return false;

            Vector3D surfaceNormal = normal;
            surfaceNormal.Normalize();

            if (MyAPIGateway.Session != null && MyAPIGateway.Session.Camera != null)
            {
                MatrixD camera = MyAPIGateway.Session.Camera.WorldMatrix;
                Vector3D projectedUp = camera.Up - surfaceNormal * Vector3D.Dot(camera.Up, surfaceNormal);
                if (projectedUp.LengthSquared() <= 0.000001)
                    projectedUp = cursor.AxisB - surfaceNormal * Vector3D.Dot(cursor.AxisB, surfaceNormal);

                if (projectedUp.LengthSquared() > 0.000001)
                {
                    projectedUp.Normalize();
                    if (Vector3D.Dot(projectedUp, camera.Up) < 0.0)
                        projectedUp = -projectedUp;

                    Vector3D projectedRight = Vector3D.Cross(projectedUp, surfaceNormal);
                    if (projectedRight.LengthSquared() > 0.000001)
                    {
                        projectedRight.Normalize();
                        axisA = projectedRight;
                        axisB = projectedUp;
                        return true;
                    }
                }
            }

            axisA = cursor.AxisA;
            axisB = cursor.AxisB;
            if (axisA.LengthSquared() <= 0.000001 || axisB.LengthSquared() <= 0.000001)
                return false;

            axisA.Normalize();
            axisB = axisB - surfaceNormal * Vector3D.Dot(axisB, surfaceNormal);
            if (axisB.LengthSquared() <= 0.000001)
                axisB = Vector3D.Cross(surfaceNormal, axisA);
            if (axisB.LengthSquared() <= 0.000001)
                return false;

            axisB.Normalize();
            return true;
        }

        bool TryUpdateMouseControlledPanelCursor(GridSchematicsLcdApp aimedApp, PanelCursorWorldDrawData aimCursor, out GridSchematicsLcdApp cursorApp, out PanelCursorWorldDrawData cursor)
        {
            cursor = aimCursor;
            cursorApp = aimedApp;
            if (!IsAnyCursorAppSelectable(aimedApp))
                return false;

            ConstructMouseCursorState state = GetOrCreateConstructMouseCursor(aimedApp.ConstructId);
            state.Enabled = true;
            state.HasPanelFocus = true;
            state.Sensitivity = GridSchematicsConfig.NormalizeMouseSensitivity(state.Sensitivity);

            bool altLook = IsCurrentAltPressed();
            int aimedSurfaceIndex = GetAppSurfaceIndex(aimedApp);
            if (!state.HasPosition)
            {
                if (altLook)
                    return false;

                SetConstructMouseCursorTarget(state, aimedApp, aimedSurfaceIndex, aimCursor.RawSurfacePoint);
            }

            cursorApp = FindMouseCursorApp(aimedApp.ConstructId, state.ActivePanelId, state.ActiveSurfaceIndex);
            if (cursorApp == null)
            {
                cursorApp = aimedApp;
                SetConstructMouseCursorTarget(state, aimedApp, aimedSurfaceIndex, aimCursor.RawSurfacePoint);
            }

            if (!altLook)
            {
                int mouseX = GetCurrentMouseXForGameplay();
                int mouseY = GetCurrentMouseYForGameplay();
                if (mouseX != 0 || mouseY != 0)
                {
                    float sensitivity = GetMouseSensitivityMultiplier(state.Sensitivity);
                    state.RawPosition += new Vector2(mouseX * sensitivity, mouseY * sensitivity);
                    ConstrainMouseCursorToPanelNetwork(aimedApp, ref cursorApp, ref state.RawPosition);
                    if (cursorApp == null)
                        return false;
                    state.ActivePanelId = cursorApp.OwnerBlock.EntityId;
                    state.ActiveSurfaceIndex = GetAppSurfaceIndex(cursorApp);
                }
                else
                {
                    state.RawPosition = ClampRawCursorToSurface(cursorApp.Surface, state.RawPosition);
                }
            }
            else
            {
                state.RawPosition = ClampRawCursorToSurface(cursorApp.Surface, state.RawPosition);
            }

            if (cursorApp == null || cursorApp.TouchInput == null || !cursorApp.TouchInput.TryBuildPanelCursorWorldDrawData(state.RawPosition, out cursor))
            {
                cursorApp = aimedApp;
                SetConstructMouseCursorTarget(state, aimedApp, aimedSurfaceIndex, aimCursor.RawSurfacePoint);
                if (cursorApp == null || cursorApp.TouchInput == null || !cursorApp.TouchInput.TryBuildPanelCursorWorldDrawData(state.RawPosition, out cursor))
                    return false;
            }

            cursor.RawSurfacePoint = state.RawPosition;
            return true;
        }

        bool TryFindVisibleMouseControlledPanelCursor(out GridSchematicsLcdApp activeApp, out PanelCursorWorldDrawData cursor)
        {
            activeApp = null;
            cursor = new PanelCursorWorldDrawData();

            foreach (var entry in _constructMouseCursors)
            {
                var state = entry.Value;
                if (state == null || !state.Enabled || !state.HasPosition || state.ActivePanelId == 0)
                    continue;

                var app = FindMouseCursorApp(entry.Key, state.ActivePanelId, state.ActiveSurfaceIndex);
                if (app == null || app.TouchInput == null)
                {
                    ClearConstructMouseCursorTarget(state);
                    continue;
                }

                GridSchematicsLcdApp cursorApp = app;
                if (!IsCurrentAltPressed())
                {
                    int mouseX = GetCurrentMouseXForGameplay();
                    int mouseY = GetCurrentMouseYForGameplay();
                    if (mouseX != 0 || mouseY != 0)
                    {
                        state.Sensitivity = GridSchematicsConfig.NormalizeMouseSensitivity(state.Sensitivity);
                        float sensitivity = GetMouseSensitivityMultiplier(state.Sensitivity);
                        state.RawPosition += new Vector2(mouseX * sensitivity, mouseY * sensitivity);
                        ConstrainMouseCursorToPanelNetwork(app, ref cursorApp, ref state.RawPosition);
                        if (cursorApp == null)
                            continue;

                        state.ActivePanelId = cursorApp.OwnerBlock.EntityId;
                        state.ActiveSurfaceIndex = GetAppSurfaceIndex(cursorApp);
                        app = cursorApp;
                    }
                }
                else
                {
                    state.RawPosition = ClampRawCursorToSurface(app.Surface, state.RawPosition);
                }

                PanelCursorWorldDrawData candidate;
                if (!app.TouchInput.TryBuildPanelCursorWorldDrawData(state.RawPosition, out candidate))
                {
                    ClearConstructMouseCursorTarget(state);
                    continue;
                }

                if (!IsWorldCursorInFrontOfCamera(candidate.PlaneHit))
                    continue;

                candidate.RawSurfacePoint = state.RawPosition;
                activeApp = app;
                cursor = candidate;
                state.HasPanelFocus = true;
                _panelCursorDrawPressed = IsCurrentLeftMousePressed();
                _panelCursorDrawSecondaryPressed = IsCurrentRightMousePressed();
                return true;
            }

            return false;
        }

        bool TryFindVisibleMouseControlledPanelCursorForRender(out GridSchematicsLcdApp activeApp, out PanelCursorWorldDrawData cursor)
        {
            activeApp = null;
            cursor = new PanelCursorWorldDrawData();

            foreach (var entry in _constructMouseCursors)
            {
                var state = entry.Value;
                if (state == null || !state.Enabled || !state.HasPosition || state.ActivePanelId == 0)
                    continue;

                var app = FindMouseCursorApp(entry.Key, state.ActivePanelId, state.ActiveSurfaceIndex);
                if (app == null || app.TouchInput == null)
                {
                    ClearConstructMouseCursorTarget(state);
                    continue;
                }

                PanelCursorWorldDrawData candidate;
                if (!app.TouchInput.TryBuildPanelCursorWorldDrawData(state.RawPosition, out candidate))
                {
                    ClearConstructMouseCursorTarget(state);
                    continue;
                }

                if (!IsWorldCursorInFrontOfCamera(candidate.PlaneHit))
                    continue;

                candidate.RawSurfacePoint = state.RawPosition;
                activeApp = app;
                cursor = candidate;
                _panelCursorDrawPressed = false;
                _panelCursorDrawSecondaryPressed = false;
                return true;
            }

            return false;
        }

        static bool IsWorldCursorInFrontOfCamera(Vector3D point)
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return false;

            var camera = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D toPoint = point - camera.Translation;
            if (toPoint.LengthSquared() <= 0.000001)
                return false;
            toPoint.Normalize();

            return Vector3D.Dot(camera.Forward, toPoint) > 0.0;
        }

        static void SetConstructMouseCursorTarget(ConstructMouseCursorState state, GridSchematicsLcdApp app, int surfaceIndex, Vector2 rawPosition)
        {
            if (state == null || app == null || app.OwnerBlock == null)
                return;

            state.RawPosition = ClampRawCursorToSurface(app.Surface, rawPosition);
            state.ActivePanelId = app.OwnerBlock.EntityId;
            state.ActiveSurfaceIndex = surfaceIndex;
            state.HasPosition = true;
            state.HasPanelFocus = true;
        }

        static void ClearConstructMouseCursorTarget(ConstructMouseCursorState state)
        {
            if (state == null)
                return;

            state.HasPosition = false;
            state.HasPanelFocus = false;
            state.ActivePanelId = 0;
            state.ActiveSurfaceIndex = -1;
            state.RawPosition = Vector2.Zero;
        }

        void ConstrainMouseCursorToPanelNetwork(GridSchematicsLcdApp aimedApp, ref GridSchematicsLcdApp cursorApp, ref Vector2 rawPosition)
        {
            if (cursorApp == null || cursorApp.Surface == null)
                return;

            Vector2 size = cursorApp.Surface.SurfaceSize;
            if (rawPosition.X >= 0f && rawPosition.Y >= 0f &&
                (size.X <= 0f || rawPosition.X <= size.X) &&
                (size.Y <= 0f || rawPosition.Y <= size.Y))
                return;

            GridSchematicsLcdApp nextApp;
            Vector2 nextRaw;
            if (TryMoveMouseCursorAcrossPanelEdge(aimedApp, cursorApp, rawPosition, out nextApp, out nextRaw))
            {
                cursorApp = nextApp;
                rawPosition = ClampRawCursorToSurface(cursorApp.Surface, nextRaw);
                return;
            }

            rawPosition = ClampRawCursorToSurface(cursorApp.Surface, rawPosition);
        }

        bool TryMoveMouseCursorAcrossPanelEdge(GridSchematicsLcdApp aimedApp, GridSchematicsLcdApp currentApp, Vector2 rawPosition, out GridSchematicsLcdApp nextApp, out Vector2 nextRaw)
        {
            nextApp = null;
            nextRaw = rawPosition;
            if (currentApp == null || currentApp.Surface == null)
                return false;

            Vector2 currentSize = currentApp.Surface.SurfaceSize;
            if (currentSize.X <= 0f || currentSize.Y <= 0f)
                return false;

            int edge = -1;
            float overflow = 0f;
            if (rawPosition.X < 0f)
            {
                edge = 0;
                overflow = rawPosition.X;
            }
            else if (rawPosition.X > currentSize.X)
            {
                edge = 1;
                overflow = rawPosition.X - currentSize.X;
            }
            else if (rawPosition.Y < 0f)
            {
                edge = 2;
                overflow = rawPosition.Y;
            }
            else if (rawPosition.Y > currentSize.Y)
            {
                edge = 3;
                overflow = rawPosition.Y - currentSize.Y;
            }

            if (edge < 0)
                return false;

            if (!TryFindAdjacentMouseCursorPanel(aimedApp, currentApp, edge, out nextApp))
                return false;

            Vector2 nextSize = nextApp.Surface.SurfaceSize;
            if (nextSize.X <= 0f || nextSize.Y <= 0f)
                return false;

            float normX = currentSize.X > 0f ? rawPosition.X / currentSize.X : 0.5f;
            float normY = currentSize.Y > 0f ? rawPosition.Y / currentSize.Y : 0.5f;
            if (normX < 0f) normX = 0f;
            if (normX > 1f) normX = 1f;
            if (normY < 0f) normY = 0f;
            if (normY > 1f) normY = 1f;

            if (edge == 0)
                nextRaw = new Vector2(nextSize.X + overflow, normY * nextSize.Y);
            else if (edge == 1)
                nextRaw = new Vector2(overflow, normY * nextSize.Y);
            else if (edge == 2)
                nextRaw = new Vector2(normX * nextSize.X, nextSize.Y + overflow);
            else
                nextRaw = new Vector2(normX * nextSize.X, overflow);

            if (nextApp.TouchInput == null || !nextApp.TouchInput.TryRefinePanelCursorSurfaceForRawPoint(nextRaw))
                return false;

            return true;
        }

        bool TryFindAdjacentMouseCursorPanel(GridSchematicsLcdApp aimedApp, GridSchematicsLcdApp currentApp, int edge, out GridSchematicsLcdApp nextApp)
        {
            nextApp = null;
            PanelSurfaceFrame current;
            if (!TryGetPanelSurfaceFrame(currentApp, out current))
                return false;

            double bestScore = double.MaxValue;
            TryScoreAdjacentMouseCursorPanel(aimedApp, currentApp, current, edge, ref nextApp, ref bestScore);
            ScoreAdjacentMouseCursorPanels(_apps, currentApp, current, edge, ref nextApp, ref bestScore);
            ScoreAdjacentMouseCursorPanels(_surfaceScriptApps, currentApp, current, edge, ref nextApp, ref bestScore);
            return nextApp != null;
        }

        void TryScoreAdjacentMouseCursorPanel(GridSchematicsLcdApp candidate, GridSchematicsLcdApp currentApp, PanelSurfaceFrame current, int edge, ref GridSchematicsLcdApp bestApp, ref double bestScore)
        {
            if (candidate == null || candidate == currentApp)
                return;

            PanelSurfaceFrame frame;
            if (!TryGetPanelSurfaceFrame(candidate, out frame))
                return;

            if (!IsUsableAdjacentSurface(current, frame))
                return;

            double score = ScoreAdjacentEdgeDistance(current, frame, edge);
            if (score < bestScore)
            {
                bestScore = score;
                bestApp = candidate;
            }
        }

        void ScoreAdjacentMouseCursorPanels(List<GridSchematicsLcdApp> apps, GridSchematicsLcdApp currentApp, PanelSurfaceFrame current, int edge, ref GridSchematicsLcdApp bestApp, ref double bestScore)
        {
            if (apps == null)
                return;

            for (int i = apps.Count - 1; i >= 0; i--)
            {
                var candidate = apps[i];
                bool requireSurfaceScript = object.ReferenceEquals(apps, _surfaceScriptApps);
                bool requirePanelTag = object.ReferenceEquals(apps, _apps);
                if (!IsCursorAppSelectable(candidate, requireSurfaceScript, requirePanelTag))
                {
                    ClearCursorStateForApp(candidate);
                    apps.RemoveAt(i);
                    continue;
                }

                TryScoreAdjacentMouseCursorPanel(candidate, currentApp, current, edge, ref bestApp, ref bestScore);
            }
        }

        static bool IsUsableAdjacentSurface(PanelSurfaceFrame current, PanelSurfaceFrame candidate)
        {
            if (current.App == null || candidate.App == null || current.App.ConstructId != candidate.App.ConstructId)
                return false;

            double normalDot = Math.Abs(Vector3D.Dot(current.Normal, candidate.Normal));
            double axisXDot = Math.Abs(Vector3D.Dot(current.AxisX, candidate.AxisX));
            double axisYDot = Math.Abs(Vector3D.Dot(current.AxisY, candidate.AxisY));
            return normalDot > 0.35 && axisXDot > 0.35 && axisYDot > 0.35;
        }

        static double ScoreAdjacentEdgeDistance(PanelSurfaceFrame current, PanelSurfaceFrame candidate, int edge)
        {
            Vector3D direction;
            Vector3D perpendicular;
            double currentPrimary;
            double candidatePrimary;
            double currentSecondary;
            double candidateSecondary;
            if (edge == 0)
            {
                direction = -current.AxisX;
                perpendicular = current.AxisY;
                currentPrimary = GetFrameWidth(current);
                candidatePrimary = GetFrameWidth(candidate);
                currentSecondary = GetFrameHeight(current);
                candidateSecondary = GetFrameHeight(candidate);
            }
            else if (edge == 1)
            {
                direction = current.AxisX;
                perpendicular = current.AxisY;
                currentPrimary = GetFrameWidth(current);
                candidatePrimary = GetFrameWidth(candidate);
                currentSecondary = GetFrameHeight(current);
                candidateSecondary = GetFrameHeight(candidate);
            }
            else if (edge == 2)
            {
                direction = -current.AxisY;
                perpendicular = current.AxisX;
                currentPrimary = GetFrameHeight(current);
                candidatePrimary = GetFrameHeight(candidate);
                currentSecondary = GetFrameWidth(current);
                candidateSecondary = GetFrameWidth(candidate);
            }
            else
            {
                direction = current.AxisY;
                perpendicular = current.AxisX;
                currentPrimary = GetFrameHeight(current);
                candidatePrimary = GetFrameHeight(candidate);
                currentSecondary = GetFrameWidth(current);
                candidateSecondary = GetFrameWidth(candidate);
            }

            Vector3D centerDelta = candidate.Center - current.Center;
            double centerDistanceAlongExit = Vector3D.Dot(centerDelta, direction);
            if (centerDistanceAlongExit <= 0.0)
                return ScoreAdjacentEdgeCenterDistance(current, candidate, edge, direction);

            double edgeGap = centerDistanceAlongExit - currentPrimary * 0.5 - candidatePrimary * 0.5;
            const double MaxAdjacentDisplayGap = 0.75;
            if (edgeGap < -0.08 || edgeGap > MaxAdjacentDisplayGap)
                return ScoreAdjacentEdgeCenterDistance(current, candidate, edge, direction);

            double perpendicularOffset = Math.Abs(Vector3D.Dot(centerDelta, perpendicular));
            double maxPerpendicularOffset = (currentSecondary + candidateSecondary) * 0.5;
            if (perpendicularOffset > maxPerpendicularOffset)
                return ScoreAdjacentEdgeCenterDistance(current, candidate, edge, direction);

            double normalizedOffset = maxPerpendicularOffset > 0.0001 ? perpendicularOffset / maxPerpendicularOffset : perpendicularOffset;
            double normalizedGap = edgeGap > 0.0 ? edgeGap / MaxAdjacentDisplayGap : 0.0;
            double centerScore = normalizedGap * normalizedGap + normalizedOffset * normalizedOffset;
            return Math.Min(centerScore, ScoreAdjacentEdgeCenterDistance(current, candidate, edge, direction));
        }

        static double ScoreAdjacentEdgeCenterDistance(PanelSurfaceFrame current, PanelSurfaceFrame candidate, int edge, Vector3D direction)
        {
            Vector3D currentEdgeCenter;
            Vector3D candidateEdgeCenter;
            Vector3D edgeAxis;
            if (edge == 0)
            {
                currentEdgeCenter = current.LeftEdgeCenter;
                candidateEdgeCenter = candidate.RightEdgeCenter;
                edgeAxis = current.AxisY;
            }
            else if (edge == 1)
            {
                currentEdgeCenter = current.RightEdgeCenter;
                candidateEdgeCenter = candidate.LeftEdgeCenter;
                edgeAxis = current.AxisY;
            }
            else if (edge == 2)
            {
                currentEdgeCenter = current.TopEdgeCenter;
                candidateEdgeCenter = candidate.BottomEdgeCenter;
                edgeAxis = current.AxisX;
            }
            else
            {
                currentEdgeCenter = current.BottomEdgeCenter;
                candidateEdgeCenter = candidate.TopEdgeCenter;
                edgeAxis = current.AxisX;
            }

            double edgeCenterDistance = Vector3D.Distance(currentEdgeCenter, candidateEdgeCenter);
            const double MaxAngledAdjacentEdgeDistance = 1.1;
            if (edgeCenterDistance > MaxAngledAdjacentEdgeDistance)
                return double.MaxValue;

            Vector3D edgeDelta = candidateEdgeCenter - currentEdgeCenter;
            double exitDot = edgeDelta.LengthSquared() > 0.000001 ? Vector3D.Dot(edgeDelta / edgeDelta.Length(), direction) : 1.0;
            if (exitDot < -0.25)
                return double.MaxValue;

            Vector3D candidateEdgeAxis = edge <= 1 ? candidate.AxisY : candidate.AxisX;
            double edgeParallel = Math.Abs(Vector3D.Dot(edgeAxis, candidateEdgeAxis));
            double edgeScore = (edgeCenterDistance / MaxAngledAdjacentEdgeDistance);
            edgeScore = edgeScore * edgeScore + (1.0 - edgeParallel) * (1.0 - edgeParallel);
            return edgeScore;
        }

        static double GetFrameWidth(PanelSurfaceFrame frame)
        {
            return (frame.TopRight - frame.TopLeft).Length();
        }

        static double GetFrameHeight(PanelSurfaceFrame frame)
        {
            return (frame.BottomLeft - frame.TopLeft).Length();
        }

        static bool TryGetPanelSurfaceFrame(GridSchematicsLcdApp app, out PanelSurfaceFrame frame)
        {
            frame = new PanelSurfaceFrame();
            if (app == null || app.TouchInput == null || app.Surface == null || !app.IsOwnerFunctional || app.Config == null || !app.Config.Enabled)
                return false;

            Vector3D topLeft;
            Vector3D topRight;
            Vector3D bottomRight;
            Vector3D bottomLeft;
            if (!app.TouchInput.TryGetPanelCursorSurfaceCorners(out topLeft, out topRight, out bottomRight, out bottomLeft))
                return false;

            Vector3D axisX = topRight - topLeft;
            Vector3D axisY = bottomLeft - topLeft;
            if (axisX.LengthSquared() <= 0.000001 || axisY.LengthSquared() <= 0.000001)
                return false;
            axisX.Normalize();
            axisY.Normalize();

            Vector3D normal = Vector3D.Cross(axisX, axisY);
            if (normal.LengthSquared() <= 0.000001)
                return false;
            normal.Normalize();

            frame.App = app;
            frame.TopLeft = topLeft;
            frame.TopRight = topRight;
            frame.BottomRight = bottomRight;
            frame.BottomLeft = bottomLeft;
            frame.AxisX = axisX;
            frame.AxisY = axisY;
            frame.Normal = normal;
            frame.Size = app.Surface.SurfaceSize;
            return true;
        }

        GridSchematicsLcdApp FindMouseCursorApp(long constructId, long panelId, int surfaceIndex)
        {
            GridSchematicsLcdApp app = FindMouseCursorAppInList(_apps, constructId, panelId, surfaceIndex, false, true);
            if (app != null)
                return app;

            return FindMouseCursorAppInList(_surfaceScriptApps, constructId, panelId, surfaceIndex, true, false);
        }

        static GridSchematicsLcdApp FindMouseCursorAppInList(List<GridSchematicsLcdApp> apps, long constructId, long panelId, int surfaceIndex, bool requireSurfaceScript, bool requirePanelTag)
        {
            if (apps == null)
                return null;

            for (int i = apps.Count - 1; i >= 0; i--)
            {
                var app = apps[i];
                if (IsCursorAppSelectable(app, requireSurfaceScript, requirePanelTag) && app.OwnerBlock.EntityId == panelId && app.ConstructId == constructId)
                {
                    if (surfaceIndex < 0 || GetAppSurfaceIndex(app) == surfaceIndex)
                        return app;
                }
            }

            return null;
        }

        static bool IsTaggedPanelAppSurface(IMyTextPanel panel)
        {
            if (panel == null)
                return false;

            var name = panel.CustomName ?? string.Empty;
            return name.ToUpperInvariant().Contains(PANEL_TAG);
        }

        static bool IsSurfaceScriptAppSurface(GridSchematicsLcdApp app)
        {
            try
            {
                return app != null && app.Surface != null && app.Surface.Script == TEXT_SURFACE_SCRIPT_ID;
            }
            catch
            {
                return false;
            }
        }

        static bool IsCursorAppSelectable(GridSchematicsLcdApp app, bool requireSurfaceScript, bool requirePanelTag)
        {
            if (app == null || app.OwnerBlock == null || app.Surface == null || app.TouchInput == null || !app.IsOwnerFunctional || app.Config == null || !app.Config.Enabled)
                return false;

            if (requireSurfaceScript && !IsSurfaceScriptAppSurface(app))
                return false;

            if (requirePanelTag && !IsTaggedPanelAppSurface(app.Panel))
                return false;

            return true;
        }

        static bool IsAnyCursorAppSelectable(GridSchematicsLcdApp app)
        {
            if (!IsCursorAppSelectable(app, false, false))
                return false;

            return IsSurfaceScriptAppSurface(app) || IsTaggedPanelAppSurface(app.Panel);
        }

        static int GetAppSurfaceIndex(GridSchematicsLcdApp app)
        {
            if (app == null || app.TouchInput == null)
                return -1;

            return app.TouchInput.GetSurfaceIndex();
        }

        static GridSchematicsLcdApp FindFirstMouseCursorAppInList(List<GridSchematicsLcdApp> apps, long constructId, long panelId)
        {
            if (apps == null)
                return null;

            for (int i = apps.Count - 1; i >= 0; i--)
            {
                var app = apps[i];
                if (IsAnyCursorAppSelectable(app) && app.OwnerBlock.EntityId == panelId && app.ConstructId == constructId)
                    return app;
            }

            return null;
        }

        static float GetMouseSensitivityMultiplier(string sensitivity)
        {
            sensitivity = GridSchematicsConfig.NormalizeMouseSensitivity(sensitivity);
            if (sensitivity == "LOW")
                return 0.55f;
            if (sensitivity == "HIGH")
                return 1.65f;
            return 1f;
        }

        static Vector2 ClampRawCursorToSurface(IMyTextSurface surface, Vector2 rawPosition)
        {
            if (surface == null)
                return rawPosition;

            Vector2 size = surface.SurfaceSize;
            if (rawPosition.X < 0f) rawPosition.X = 0f;
            if (rawPosition.Y < 0f) rawPosition.Y = 0f;
            if (size.X > 0f && rawPosition.X > size.X) rawPosition.X = size.X;
            if (size.Y > 0f && rawPosition.Y > size.Y) rawPosition.Y = size.Y;
            return rawPosition;
        }

        bool TryFindPanelSurfaceCursorApp(out GridSchematicsLcdApp app, out PanelCursorWorldDrawData cursor)
        {
            return TryFindPanelSurfaceCursorApp(null, out app, out cursor);
        }

        bool TryFindPanelSurfaceCursorApp(IMyCubeBlock ownerBlock, out GridSchematicsLcdApp app, out PanelCursorWorldDrawData cursor)
        {
            app = null;
            cursor = new PanelCursorWorldDrawData();

            long nowTicks = DateTime.UtcNow.Ticks;
            if (PANEL_CURSOR_LIMIT_SAMPLE_RATE && _hasPanelCursorDrawData && nowTicks - _lastPanelCursorSampleUtcTicks < PANEL_CURSOR_SAMPLE_INTERVAL_TICKS)
            {
                app = _panelCursorDrawApp;
                cursor = _panelCursorDrawData;
                return app != null &&
                    app.IsOwnerFunctional &&
                    app.Config != null &&
                    app.Config.Enabled &&
                    (ownerBlock == null || app.OwnerBlock != null && app.OwnerBlock.EntityId == ownerBlock.EntityId) &&
                    IsPanelCursorPointInsideBounds(cursor);
            }

            Vector3D physicsHit;
            bool hasPhysicsHit = TryGetCameraPhysicsHit(out physicsHit);
            double bestScore = double.MaxValue;
            PanelCursorWorldDrawData bestCursor = new PanelCursorWorldDrawData();
            GridSchematicsLcdApp bestApp = null;

            ScorePanelSurfaceCursorApps(_apps, ownerBlock, hasPhysicsHit, physicsHit, ref bestApp, ref bestCursor, ref bestScore, false, true);
            for (int i = _surfaceScriptApps.Count - 1; i >= 0; i--)
            {
                var candidate = _surfaceScriptApps[i];
                if (!IsCursorAppSelectable(candidate, true, false))
                {
                    ClearCursorStateForApp(candidate);
                    _surfaceScriptApps.RemoveAt(i);
                    continue;
                }

                ScorePanelSurfaceCursorCandidate(candidate, ownerBlock, hasPhysicsHit, physicsHit, ref bestApp, ref bestCursor, ref bestScore, true, false);
            }

            if (bestApp != null)
            {
                app = bestApp;
                cursor = bestCursor;
                CapturePanelCursorDrawData(app, cursor, nowTicks);
                return true;
            }

            _hasPanelCursorDrawData = false;
            _panelCursorDrawApp = null;
            return false;
        }

        static bool TryGetCameraPhysicsHit(out Vector3D hitPosition)
        {
            hitPosition = Vector3D.Zero;
            try
            {
                if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null || MyAPIGateway.Physics == null)
                    return false;

                var camera = MyAPIGateway.Session.Camera.WorldMatrix;
                Vector3D rayOrigin = camera.Translation;
                Vector3D rayEnd = rayOrigin + camera.Forward * 12.0;
                IHitInfo physicsHit;
                if (!MyAPIGateway.Physics.CastRay(rayOrigin, rayEnd, out physicsHit) || physicsHit == null)
                    return false;

                hitPosition = physicsHit.Position;
                return true;
            }
            catch
            {
                return false;
            }
        }

        static void ScorePanelSurfaceCursorApps(List<GridSchematicsLcdApp> apps, IMyCubeBlock ownerBlock, bool hasPhysicsHit, Vector3D physicsHit, ref GridSchematicsLcdApp bestApp, ref PanelCursorWorldDrawData bestCursor, ref double bestScore, bool requireSurfaceScript, bool requirePanelTag)
        {
            if (apps == null)
                return;

            for (int i = apps.Count - 1; i >= 0; i--)
            {
                ScorePanelSurfaceCursorCandidate(apps[i], ownerBlock, hasPhysicsHit, physicsHit, ref bestApp, ref bestCursor, ref bestScore, requireSurfaceScript, requirePanelTag);
            }
        }

        static void ScorePanelSurfaceCursorCandidate(GridSchematicsLcdApp app, IMyCubeBlock ownerBlock, bool hasPhysicsHit, Vector3D physicsHit, ref GridSchematicsLcdApp bestApp, ref PanelCursorWorldDrawData bestCursor, ref double bestScore, bool requireSurfaceScript, bool requirePanelTag)
        {
            if (!IsCursorAppSelectable(app, requireSurfaceScript, requirePanelTag))
                return;

            if (ownerBlock != null && app.OwnerBlock.EntityId != ownerBlock.EntityId)
                return;

            PanelCursorWorldDrawData candidate;
            if (!TryRefreshPanelSurfaceCursorApp(app, out candidate))
                return;

            if (!IsPanelCursorPointInsideBounds(candidate))
                return;

            double score = ScorePanelSurfaceCursorHit(candidate, hasPhysicsHit, physicsHit);
            if (score < bestScore)
            {
                bestScore = score;
                bestApp = app;
                bestCursor = candidate;
            }
        }

        static double ScorePanelSurfaceCursorHit(PanelCursorWorldDrawData cursor, bool hasPhysicsHit, Vector3D physicsHit)
        {
            double score = 0.0;
            if (hasPhysicsHit)
            {
                double physicsScore = Vector3D.DistanceSquared(cursor.PlaneHit, physicsHit) * 1000.0;
                score += Math.Min(physicsScore, 1.0);
            }
            else if (MyAPIGateway.Session != null && MyAPIGateway.Session.Camera != null)
            {
                score += Vector3D.DistanceSquared(MyAPIGateway.Session.Camera.WorldMatrix.Translation, cursor.PlaneHit) * 0.001;
            }

            double width = cursor.MaxA - cursor.MinA;
            double height = cursor.MaxB - cursor.MinB;
            if (width > 0.0001 && height > 0.0001)
            {
                double normalizedA = (cursor.LocalA - cursor.MinA) / width;
                double normalizedB = (cursor.LocalB - cursor.MinB) / height;
                double edgeClearance = Math.Min(Math.Min(normalizedA, 1.0 - normalizedA), Math.Min(normalizedB, 1.0 - normalizedB));
                score += MathHelper.Clamp(0.5 - edgeClearance, 0.0, 0.5) * 8.0;
            }

            return score;
        }

        void CapturePanelCursorDrawData(GridSchematicsLcdApp app, PanelCursorWorldDrawData cursor, long nowTicks)
        {
            _hasPanelCursorDrawData = true;
            _lastPanelCursorSampleUtcTicks = nowTicks;
            _panelCursorDrawApp = app;
            _panelCursorDrawData = cursor;
            _panelCursorDrawPressed = IsCurrentLeftMousePressed();
            _panelCursorDrawSecondaryPressed = IsCurrentRightMousePressed();
        }

        void ClearPanelCursorDrawCache()
        {
            _hasPanelCursorDrawData = false;
            _panelCursorDrawApp = null;
            _panelCursorDrawData = new PanelCursorWorldDrawData();
            _panelCursorDrawPressed = false;
            _panelCursorDrawSecondaryPressed = false;
        }

        static bool TryRefreshPanelSurfaceCursorApp(GridSchematicsLcdApp app, out PanelCursorWorldDrawData cursor)
        {
            cursor = new PanelCursorWorldDrawData();
            if (!IsAnyCursorAppSelectable(app))
                return false;

            return app.TouchInput.TryRefreshPanelCursorWorldDrawData(out cursor);
        }

        static bool IsCurrentLeftMousePressed()
        {
            try
            {
                return !IsGameGuiCursorVisible() && MyAPIGateway.Input != null && MyAPIGateway.Input.IsLeftMousePressed();
            }
            catch
            {
                return false;
            }
        }

        static bool IsCurrentRightMousePressed()
        {
            try
            {
                return !IsGameGuiCursorVisible() && MyAPIGateway.Input != null && MyAPIGateway.Input.IsRightMousePressed();
            }
            catch
            {
                return false;
            }
        }

        static bool IsCurrentAltPressed()
        {
            try
            {
                return MyAPIGateway.Input != null && MyAPIGateway.Input.IsAnyAltKeyPressed();
            }
            catch
            {
                return false;
            }
        }

        static bool IsCurrentShiftPressed()
        {
            try
            {
                return MyAPIGateway.Input != null && MyAPIGateway.Input.IsAnyShiftKeyPressed();
            }
            catch
            {
                return false;
            }
        }

        static bool IsCurrentControlPressed()
        {
            try
            {
                return MyAPIGateway.Input != null && MyAPIGateway.Input.IsAnyCtrlKeyPressed();
            }
            catch
            {
                return false;
            }
        }

        static int GetCurrentMouseXForGameplay()
        {
            try
            {
                if (IsGameGuiCursorVisible())
                    return 0;
                return MyAPIGateway.Input != null ? MyAPIGateway.Input.GetMouseXForGamePlay() : 0;
            }
            catch
            {
                return 0;
            }
        }

        static int GetCurrentMouseYForGameplay()
        {
            try
            {
                if (IsGameGuiCursorVisible())
                    return 0;
                return MyAPIGateway.Input != null ? MyAPIGateway.Input.GetMouseYForGamePlay() : 0;
            }
            catch
            {
                return 0;
            }
        }

        static bool IsGameGuiCursorVisible()
        {
            try
            {
                return MyAPIGateway.Gui != null && MyAPIGateway.Gui.IsCursorVisible;
            }
            catch
            {
                return false;
            }
        }

        void ShowAimDebugNotification(TouchScreenApiAdapter input)
        {
            if (input == null || MyAPIGateway.Utilities == null)
                return;

            if (_aimDebugNotificationCooldown > 0)
            {
                _aimDebugNotificationCooldown--;
                return;
            }

            _aimDebugNotificationCooldown = 45;
            try
            {
                MyAPIGateway.Utilities.ShowNotification(input.StatusText, 900, "White");
            }
            catch
            {
            }
        }

        void DrawDiscoveredDisplayDebugEdge(TouchScreenApiAdapter input)
        {
            if (input == null)
                return;

            Vector3D topLeft;
            Vector3D topRight;
            Vector3D bottomRight;
            Vector3D bottomLeft;
            if (!input.TryGetDiscoveredDisplayCorners(out topLeft, out topRight, out bottomRight, out bottomLeft))
                return;

            var color = new Color(255, 216, 70, 210);
            float thickness = 0.006f;
            DrawWorldDebugLine(topLeft, topRight, thickness, color);
            DrawWorldDebugLine(topRight, bottomRight, thickness, color);
            DrawWorldDebugLine(bottomRight, bottomLeft, thickness, color);
            DrawWorldDebugLine(bottomLeft, topLeft, thickness, color);
        }
        static void DrawAimCursorSegment(Vector3D center, Vector3D direction, float length, float thickness, Color color)
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return;

            Vector3D axis = direction;
            if (axis.LengthSquared() <= 0.000001)
                return;
            axis.Normalize();

            var camera = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D normal = camera.Forward;
            Vector3D up = Vector3D.Cross(normal, axis);
            if (up.LengthSquared() <= 0.000001)
                up = camera.Up;
            up.Normalize();

            var drawColor = color.ToVector4();
            MyTransparentGeometry.AddBillboardOriented(CursorMaterial, drawColor, center, axis, up, length, thickness);
        }
        GridSchematicsLcdApp FindAimCursorApp()
        {
            for (int i = _apps.Count - 1; i >= 0; i--)
            {
                var app = _apps[i];
                if (IsCursorAppSelectable(app, false, true) && app.ShouldDrawAimCursorHud)
                    return app;
            }

            for (int i = _surfaceScriptApps.Count - 1; i >= 0; i--)
            {
                var app = _surfaceScriptApps[i];
                if (!IsCursorAppSelectable(app, true, false))
                {
                    ClearCursorStateForApp(app);
                    _surfaceScriptApps.RemoveAt(i);
                    continue;
                }

                if (app.ShouldDrawAimCursorHud)
                    return app;
            }

            for (int i = _apps.Count - 1; i >= 0; i--)
            {
                var app = _apps[i];
                if (IsCursorAppSelectable(app, false, true))
                    return app;
            }

            for (int i = _surfaceScriptApps.Count - 1; i >= 0; i--)
            {
                var app = _surfaceScriptApps[i];
                if (!IsCursorAppSelectable(app, true, false))
                {
                    ClearCursorStateForApp(app);
                    _surfaceScriptApps.RemoveAt(i);
                    continue;
                }

                return app;
            }

            return null;
        }

        void ClearCursorStateForApp(GridSchematicsLcdApp app)
        {
            if (app == null || app.OwnerBlock == null)
                return;

            long constructId = app.ConstructId;
            long panelId = app.OwnerBlock.EntityId;
            int surfaceIndex = GetAppSurfaceIndex(app);

            ConstructMouseCursorState state;
            if (_constructMouseCursors.TryGetValue(constructId, out state) && state.ActivePanelId == panelId && (surfaceIndex < 0 || state.ActiveSurfaceIndex == surfaceIndex))
                ClearConstructMouseCursorTarget(state);

            SharedGridCursor shared;
            if (_sharedCursors.TryGetValue(constructId, out shared) && shared.IsFromPanel(panelId))
                ClearSharedCursor(constructId, _tickCounter);

            if (_panelCursorDrawApp == app)
                ClearPanelCursorDrawCache();
        }
        public void RegisterSurfaceScriptApp(GridSchematicsLcdApp app)
        {
            if (app == null)
                return;

            for (int i = 0; i < _surfaceScriptApps.Count; i++)
            {
                if (_surfaceScriptApps[i] == app)
                    return;
            }

            _surfaceScriptApps.Add(app);
        }

        public void UnregisterSurfaceScriptApp(GridSchematicsLcdApp app)
        {
            if (app == null)
                return;

            for (int i = _surfaceScriptApps.Count - 1; i >= 0; i--)
            {
                if (_surfaceScriptApps[i] == app)
                {
                    ClearCursorStateForApp(app);
                    _surfaceScriptApps.RemoveAt(i);
                }
            }
        }

        public void RegisterUnsupportedSurfaceProbe(TouchScreenApiAdapter probe)
        {
            if (probe == null)
                return;

            for (int i = 0; i < _unsupportedSurfaceProbes.Count; i++)
            {
                if (_unsupportedSurfaceProbes[i] == probe)
                    return;
            }

            _unsupportedSurfaceProbes.Add(probe);
        }

        public void UnregisterUnsupportedSurfaceProbe(TouchScreenApiAdapter probe)
        {
            if (probe == null)
                return;

            for (int i = _unsupportedSurfaceProbes.Count - 1; i >= 0; i--)
            {
                if (_unsupportedSurfaceProbes[i] == probe)
                    _unsupportedSurfaceProbes.RemoveAt(i);
            }
        }

        public ScanCache GetConstructCache(long constructId)
        {
            ScanCache cache;
            if (!_constructCache.TryGetValue(constructId, out cache))
            {
                cache = new ScanCache(constructId);
                _constructCache[constructId] = cache;
            }
            return cache;
        }

        ConstructMouseCursorState GetOrCreateConstructMouseCursor(long constructId)
        {
            ConstructMouseCursorState state;
            if (!_constructMouseCursors.TryGetValue(constructId, out state))
            {
                state = new ConstructMouseCursorState();
                _constructMouseCursors[constructId] = state;
            }

            return state;
        }

        void ClearConstructMouseCursorFocus()
        {
            foreach (var entry in _constructMouseCursors)
                entry.Value.HasPanelFocus = false;
        }

        public bool IsConstructMouseControlEnabled(long constructId)
        {
            return IsConstructMouseControlConfigured(constructId) && IsConstructMouseControlAvailable(constructId);
        }

        public bool IsConstructMouseControlConfigured(long constructId)
        {
            ConstructMouseCursorState state;
            return _constructMouseCursors.TryGetValue(constructId, out state) && state.Enabled;
        }

        public bool IsConstructMouseControlAvailable(long constructId)
        {
            try
            {
                if (MyAPIGateway.Session == null || MyAPIGateway.Session.ControlledObject == null)
                    return false;

                var controlled = MyAPIGateway.Session.ControlledObject;
                var shipController = controlled as Sandbox.ModAPI.Ingame.IMyShipController;
                if (shipController == null && controlled.Entity != null)
                    shipController = controlled.Entity as Sandbox.ModAPI.Ingame.IMyShipController;
                if (shipController == null || !shipController.IsUnderControl || !shipController.CanControlShip)
                    return false;

                IMyCubeBlock block = controlled.Entity as IMyCubeBlock;
                return block != null && IsGridInConstructMouseGroup(block.CubeGrid, constructId);
            }
            catch
            {
                return false;
            }
        }

        static bool IsPlayerInShipControlSeat()
        {
            try
            {
                if (MyAPIGateway.Session == null || MyAPIGateway.Session.ControlledObject == null)
                    return false;

                var controlled = MyAPIGateway.Session.ControlledObject;
                var shipController = controlled as Sandbox.ModAPI.Ingame.IMyShipController;
                if (shipController == null && controlled.Entity != null)
                    shipController = controlled.Entity as Sandbox.ModAPI.Ingame.IMyShipController;

                return shipController != null && shipController.IsUnderControl && shipController.CanControlShip;
            }
            catch
            {
                return false;
            }
        }

        static bool IsGridInConstructMouseGroup(IMyCubeGrid controlledGrid, long constructId)
        {
            if (controlledGrid == null)
                return false;

            if (controlledGrid.EntityId == constructId)
                return true;

            if (MyAPIGateway.GridGroups == null)
                return false;

            var grids = new List<IMyCubeGrid>();
            try
            {
                MyAPIGateway.GridGroups.GetGroup(controlledGrid, GridLinkTypeEnum.Mechanical, grids);
                for (int i = 0; i < grids.Count; i++)
                {
                    var grid = grids[i];
                    if (grid != null && !grid.MarkedForClose && grid.EntityId == constructId)
                        return true;
                }
            }
            catch
            {
            }

            return false;
        }

        public string GetConstructMouseSensitivity(long constructId, string fallback)
        {
            ConstructMouseCursorState state;
            if (_constructMouseCursors.TryGetValue(constructId, out state))
                return GridSchematicsConfig.NormalizeMouseSensitivity(state.Sensitivity);

            return GridSchematicsConfig.NormalizeMouseSensitivity(fallback);
        }

        public void NotePanelMouseControlSetting(long constructId, bool enabled)
        {
            ConstructMouseCursorState state = GetOrCreateConstructMouseCursor(constructId);
            state.Enabled = enabled;
            if (!enabled)
            {
                ClearConstructMouseCursorTarget(state);
            }
        }

        public void NotePanelMouseSensitivitySetting(long constructId, string sensitivity)
        {
            GetOrCreateConstructMouseCursor(constructId).Sensitivity = GridSchematicsConfig.NormalizeMouseSensitivity(sensitivity);
        }

        public void SetConstructMouseControl(long constructId, bool enabled)
        {
            ConstructMouseCursorState state = GetOrCreateConstructMouseCursor(constructId);
            state.Enabled = enabled;
            if (!enabled)
            {
                ClearConstructMouseCursorTarget(state);
            }

            ApplyMouseControlToApps(_apps, constructId, enabled);
            ApplyMouseControlToApps(_surfaceScriptApps, constructId, enabled);
        }

        public void SetConstructMouseSensitivity(long constructId, string sensitivity)
        {
            string normalized = GridSchematicsConfig.NormalizeMouseSensitivity(sensitivity);
            ConstructMouseCursorState state = GetOrCreateConstructMouseCursor(constructId);
            state.Sensitivity = normalized;
            ApplyMouseSensitivityToApps(_apps, constructId, normalized);
            ApplyMouseSensitivityToApps(_surfaceScriptApps, constructId, normalized);
        }

        void ApplyMouseControlToApps(List<GridSchematicsLcdApp> apps, long constructId, bool enabled)
        {
            if (apps == null)
                return;

            for (int i = apps.Count - 1; i >= 0; i--)
            {
                var app = apps[i];
                if (!IsAnyCursorAppSelectable(app))
                {
                    ClearCursorStateForApp(app);
                    apps.RemoveAt(i);
                    continue;
                }

                if (app.ConstructId == constructId)
                    app.SetMouseControlEnabledFromConstruct(enabled, true);
            }
        }

        void ApplyMouseSensitivityToApps(List<GridSchematicsLcdApp> apps, long constructId, string sensitivity)
        {
            if (apps == null)
                return;

            for (int i = apps.Count - 1; i >= 0; i--)
            {
                var app = apps[i];
                if (!IsAnyCursorAppSelectable(app))
                {
                    ClearCursorStateForApp(app);
                    apps.RemoveAt(i);
                    continue;
                }

                if (app.ConstructId == constructId)
                    app.SetMouseSensitivityFromConstruct(sensitivity, true);
            }
        }

        public bool TryGetMouseControlCursorForPanel(long constructId, long panelId, int surfaceIndex, out Vector2 rawPosition)
        {
            rawPosition = Vector2.Zero;
            ConstructMouseCursorState state;
            if (!_constructMouseCursors.TryGetValue(constructId, out state) ||
                !state.Enabled ||
                !IsConstructMouseControlAvailable(constructId) ||
                !state.HasPosition ||
                !state.HasPanelFocus ||
                state.ActivePanelId != panelId ||
                state.ActiveSurfaceIndex != surfaceIndex)
            {
                return false;
            }

            rawPosition = state.RawPosition;
            return true;
        }

        public SharedGridCursor GetSharedCursor(long constructId)
        {
            SharedGridCursor cursor;
            if (_sharedCursors.TryGetValue(constructId, out cursor))
                return cursor;

            return new SharedGridCursor { Active = false, ConstructId = constructId };
        }

        public bool IsSharedCursorSource(long constructId, long panelId)
        {
            SharedGridCursor cursor;
            return _sharedCursors.TryGetValue(constructId, out cursor) && cursor.IsFromPanel(panelId);
        }

        public void SetSharedCursor(SharedGridCursor cursor)
        {
            _sharedCursors[cursor.ConstructId] = cursor;
        }

        public void ClearSharedCursor(long constructId, int tick)
        {
            _sharedCursors[constructId] = new SharedGridCursor
            {
                Active = false,
                ConstructId = constructId,
                LastUpdatedTick = tick
            };
        }

        public bool TryLoadPersistedScan(ScanCache cache, IMyCubeGrid rootGrid)
        {
            return LoadPersistedScan(cache, rootGrid) == PersistedScanLoadStatus.Loaded;
        }

        public PersistedScanLoadStatus LoadPersistedScan(ScanCache cache, IMyCubeGrid rootGrid)
        {
            if (cache == null || rootGrid == null || MyAPIGateway.Utilities == null)
                return PersistedScanLoadStatus.Invalid;

            TextReader reader = null;
            try
            {
                reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(GetScanStorageFileName(cache.ConstructId), typeof(GridSchematicsSession));
                string versionLine = reader.ReadLine();
                string constructLine = reader.ReadLine();
                string signatureLine = reader.ReadLine();
                if (versionLine == null && constructLine == null && signatureLine == null)
                    return PersistedScanLoadStatus.Missing;

                int persistedVersion;
                if (!TryParsePersistedInt(versionLine, "VERSION=", out persistedVersion))
                    return PersistedScanLoadStatus.Obsolete;
                if (persistedVersion != PERSISTED_SCAN_VERSION)
                    return PersistedScanLoadStatus.Obsolete;
                if (constructLine != "CONSTRUCT=" + cache.ConstructId)
                    return PersistedScanLoadStatus.Obsolete;

                string signature = cache.BuildScanSignature(rootGrid);
                if (signatureLine != "SIGNATURE=" + signature)
                    return PersistedScanLoadStatus.Invalid;

                var loadedData = new Dictionary<ScanView, RawRaycastScanData>();
                var intervalScratch = new List<float>(8);
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!line.StartsWith("VIEW=", StringComparison.Ordinal))
                        continue;

                    ScanView view;
                    if (!TryParseScanView(line.Substring(5), out view))
                        return PersistedScanLoadStatus.Obsolete;

                    int resolution = ParseRequiredInt(reader.ReadLine(), "RESOLUTION=");
                    int hitSampleCount = ParseRequiredInt(reader.ReadLine(), "HIT_SAMPLES=");
                    int maxHitCount = ParseRequiredInt(reader.ReadLine(), "MAX_HIT=");
                    float maxThickness = ParseRequiredFloat(reader.ReadLine(), "MAX_THICKNESS=");
                    float sampleMinX = ParseRequiredFloat(reader.ReadLine(), "SAMPLE_MIN_X=");
                    float sampleMaxX = ParseRequiredFloat(reader.ReadLine(), "SAMPLE_MAX_X=");
                    float sampleMinY = ParseRequiredFloat(reader.ReadLine(), "SAMPLE_MIN_Y=");
                    float sampleMaxY = ParseRequiredFloat(reader.ReadLine(), "SAMPLE_MAX_Y=");
                    float depthStart = ParseRequiredFloat(reader.ReadLine(), "DEPTH_START=");
                    float depthEnd = ParseRequiredFloat(reader.ReadLine(), "DEPTH_END=");
                    long scannedTicks = ParseRequiredLong(reader.ReadLine(), "SCANNED_TICKS=");
                    int sampleCount = ParseRequiredInt(reader.ReadLine(), "SAMPLES=");

                    var data = new RawRaycastScanData(view, resolution);
                    data.HitSampleCount = hitSampleCount;
                    data.MaxHitCount = maxHitCount;
                    data.MaxThickness = maxThickness;
                    data.SetSampleBounds(sampleMinX, sampleMaxX, sampleMinY, sampleMaxY, depthStart, depthEnd);
                    data.ScannedUtc = scannedTicks > 0 ? new DateTime(scannedTicks) : DateTime.UtcNow;
                    data.IsReady = true;

                    var profileBuilder = new ScanDepthProfile.Builder(resolution * resolution);
                    for (int i = 0; i < sampleCount; i++)
                    {
                        var sampleLine = reader.ReadLine();
                        if (sampleLine == null)
                            return PersistedScanLoadStatus.Obsolete;

                        if (!LoadPersistedSampleV12(data, sampleLine, profileBuilder, intervalScratch))
                            return PersistedScanLoadStatus.Obsolete;
                    }

                    if (reader.ReadLine() != "ENDVIEW")
                        return PersistedScanLoadStatus.Obsolete;

                    data.DepthProfile = profileBuilder.Finish();
                    loadedData[view] = data;
                }

                RawRaycastScanData top = null;
                RawRaycastScanData side = null;
                RawRaycastScanData front = null;
                bool complete = loadedData.TryGetValue(ScanView.Top, out top) &&
                    loadedData.TryGetValue(ScanView.Side, out side) &&
                    loadedData.TryGetValue(ScanView.Front, out front);
                if (!complete)
                    return PersistedScanLoadStatus.Obsolete;

                cache.RaycastData[ScanView.Top] = top;
                cache.RaycastData[ScanView.Side] = side;
                cache.RaycastData[ScanView.Front] = front;
                // R8: no forced projection/conveyor rebuilds here — they pile ~5 world sweeps plus a
                // per-grid GetObjectBuilder onto the world-join tick. Projections build lazily on the
                // first render and the conveyor network refreshes on its own cadence.
                cache.MarkUpdated();
                cache.MarkStartupScanCompleted();
                return PersistedScanLoadStatus.Loaded;
            }
            catch (FileNotFoundException)
            {
                return PersistedScanLoadStatus.Missing;
            }
            catch
            {
                return PersistedScanLoadStatus.Invalid;
            }
            finally
            {
                if (reader != null)
                    reader.Dispose();
            }
        }
        public void SavePersistedScan(ScanCache cache, IMyCubeGrid rootGrid)
        {
            if (cache == null || rootGrid == null || MyAPIGateway.Utilities == null)
                return;

            if (cache.GetRaycastData(ScanView.Top) == null ||
                cache.GetRaycastData(ScanView.Side) == null ||
                cache.GetRaycastData(ScanView.Front) == null)
                return;

            TextWriter writer = null;
            try
            {
                string signature = cache.BuildScanSignature(rootGrid);
                writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(GetScanStorageFileName(cache.ConstructId), typeof(GridSchematicsSession));
                writer.WriteLine("VERSION=" + PERSISTED_SCAN_VERSION);
                writer.WriteLine("CONSTRUCT=" + cache.ConstructId);
                writer.WriteLine("SIGNATURE=" + signature);
                WritePersistedScanView(writer, cache.GetRaycastData(ScanView.Top));
                WritePersistedScanView(writer, cache.GetRaycastData(ScanView.Side));
                WritePersistedScanView(writer, cache.GetRaycastData(ScanView.Front));
            }
            catch
            {
            }
            finally
            {
                if (writer != null)
                    writer.Dispose();
            }
        }

        static string GetScanStorageFileName(long constructId)
        {
            return "GridSchematics_Scan_" + constructId + ".txt";
        }

        static void WritePersistedScanView(TextWriter writer, RawRaycastScanData data)
        {
            if (writer == null || data == null || !data.IsReady || data.Samples == null)
                return;

            int nonEmpty = 0;
            for (int i = 0; i < data.Samples.Length; i++)
            {
                if (data.Samples[i].HasHit)
                    nonEmpty++;
            }

            writer.WriteLine("VIEW=" + data.View);
            writer.WriteLine("RESOLUTION=" + data.Resolution);
            writer.WriteLine("HIT_SAMPLES=" + data.HitSampleCount);
            writer.WriteLine("MAX_HIT=" + data.MaxHitCount);
            writer.WriteLine("MAX_THICKNESS=" + data.MaxThickness.ToString("R", CultureInfo.InvariantCulture));
            writer.WriteLine("SAMPLE_MIN_X=" + data.SampleMinX.ToString("R", CultureInfo.InvariantCulture));
            writer.WriteLine("SAMPLE_MAX_X=" + data.SampleMaxX.ToString("R", CultureInfo.InvariantCulture));
            writer.WriteLine("SAMPLE_MIN_Y=" + data.SampleMinY.ToString("R", CultureInfo.InvariantCulture));
            writer.WriteLine("SAMPLE_MAX_Y=" + data.SampleMaxY.ToString("R", CultureInfo.InvariantCulture));
            writer.WriteLine("DEPTH_START=" + data.DepthStart.ToString("R", CultureInfo.InvariantCulture));
            writer.WriteLine("DEPTH_END=" + data.DepthEnd.ToString("R", CultureInfo.InvariantCulture));
            writer.WriteLine("SCANNED_TICKS=" + data.ScannedUtc.Ticks);
            writer.WriteLine("SAMPLES=" + nonEmpty);

            var lineBuilder = new StringBuilder(96);
            var profile = data.DepthProfile;
            for (int i = 0; i < data.Samples.Length; i++)
            {
                var sample = data.Samples[i];
                if (!sample.HasHit)
                    continue;

                lineBuilder.Length = 0;
                lineBuilder.Append(i).Append('|').Append(sample.HitCount).Append('|');

                int pairStart = 0;
                int pairCount = 0;
                if (profile != null && i < profile.CellCount)
                {
                    pairStart = profile.GetPairStart(i);
                    pairCount = profile.GetPairCount(i);
                }

                if (pairCount > 0)
                {
                    for (int p = 0; p < pairCount; p++)
                    {
                        if (p > 0)
                            lineBuilder.Append(',');
                        int s = ScaleIntervalBound(profile.Bounds[(pairStart + p) * 2]);
                        int e = ScaleIntervalBound(profile.Bounds[(pairStart + p) * 2 + 1]);
                        lineBuilder.Append(s).Append(':').Append(e);
                    }
                }
                else
                {
                    // Legacy data without a depth profile: persist the collapsed first..last span.
                    lineBuilder.Append(ScaleIntervalBound(sample.FirstDistance)).Append(':').Append(ScaleIntervalBound(sample.LastDistance));
                }

                writer.WriteLine(lineBuilder.ToString());
            }

            writer.WriteLine("ENDVIEW");
        }

        static int ScaleIntervalBound(float value)
        {
            if (value < 0f)
                value = 0f;
            if (value > 1.5f)
                value = 1.5f;
            return (int)Math.Round(value * PersistedIntervalScale);
        }

        // Parses "index|hitCount|s0:e0,s1:e1,..." without Split allocations, reconstructing both the
        // depth profile and the derived summary sample. Returns false on malformed input.
        static bool LoadPersistedSampleV12(RawRaycastScanData data, string line, ScanDepthProfile.Builder profileBuilder, List<float> intervalScratch)
        {
            int cursor = 0;
            int index;
            if (!TryParseIntUntil(line, ref cursor, '|', out index))
                return false;
            if (index < 0 || index >= data.Samples.Length)
                return false;

            int hitCount;
            if (!TryParseIntUntil(line, ref cursor, '|', out hitCount))
                return false;

            intervalScratch.Clear();
            float first = float.MaxValue;
            float last = float.MinValue;
            float largestSolid = 0f;
            float largestVoid = 0f;
            float previousEnd = -1f;
            int pairCount = 0;
            while (cursor < line.Length)
            {
                int s;
                int e;
                if (!TryParseIntUntil(line, ref cursor, ':', out s))
                    return false;
                if (!TryParseIntUntil(line, ref cursor, ',', out e))
                    return false;

                float start = s / PersistedIntervalScale;
                float end = e / PersistedIntervalScale;
                if (end < start)
                    return false;

                intervalScratch.Add(start);
                intervalScratch.Add(end);
                pairCount++;

                if (start < first)
                    first = start;
                if (end > last)
                    last = end;
                float solid = end - start;
                if (solid > largestSolid)
                    largestSolid = solid;
                if (previousEnd < 0f)
                {
                    if (start > largestVoid)
                        largestVoid = start;
                }
                else if (start - previousEnd > largestVoid)
                {
                    largestVoid = start - previousEnd;
                }
                previousEnd = end;
            }

            if (pairCount == 0 || hitCount <= 0)
                return false;

            float trailingVoid = 1f - last;
            if (trailingVoid > largestVoid)
                largestVoid = trailingVoid;

            data.Samples[index] = new RawRaycastSample
            {
                HitCount = hitCount,
                FirstDistance = first,
                LastDistance = last,
                SegmentCount = pairCount,
                TransitionComplexity = Math.Max(0, pairCount - 1),
                LargestSolidSegment = largestSolid,
                LargestVoidSegment = Math.Max(0f, largestVoid)
            };

            return profileBuilder.AppendCell(index, intervalScratch);
        }

        // Parses a non-negative integer starting at cursor, consuming through the terminator (or end
        // of string). '|' and end-of-string are accepted in place of the expected terminator so the
        // last field of a group parses cleanly.
        static bool TryParseIntUntil(string line, ref int cursor, char terminator, out int value)
        {
            value = 0;
            if (line == null || cursor >= line.Length)
                return false;

            bool any = false;
            while (cursor < line.Length)
            {
                char c = line[cursor];
                if (c == terminator || c == '|' || c == ',')
                {
                    cursor++;
                    return any;
                }

                if (c < '0' || c > '9')
                    return false;

                value = value * 10 + (c - '0');
                cursor++;
                any = true;
            }

            return any;
        }

        static bool TryParseScanView(string value, out ScanView view)
        {
            view = ScanView.Top;
            if (string.Equals(value, "Top", StringComparison.OrdinalIgnoreCase))
                return true;
            if (string.Equals(value, "Side", StringComparison.OrdinalIgnoreCase))
            {
                view = ScanView.Side;
                return true;
            }
            if (string.Equals(value, "Front", StringComparison.OrdinalIgnoreCase))
            {
                view = ScanView.Front;
                return true;
            }

            return false;
        }

        static bool TryParsePersistedInt(string line, string prefix, out int value)
        {
            value = 0;
            if (line == null || !line.StartsWith(prefix, StringComparison.Ordinal))
                return false;

            return int.TryParse(line.Substring(prefix.Length), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }
        static int ParseRequiredInt(string line, string prefix)
        {
            return int.Parse(StripPrefix(line, prefix), CultureInfo.InvariantCulture);
        }

        static long ParseRequiredLong(string line, string prefix)
        {
            return long.Parse(StripPrefix(line, prefix), CultureInfo.InvariantCulture);
        }

        static float ParseRequiredFloat(string line, string prefix)
        {
            return float.Parse(StripPrefix(line, prefix), CultureInfo.InvariantCulture);
        }

        static string StripPrefix(string line, string prefix)
        {
            if (line == null || !line.StartsWith(prefix, StringComparison.Ordinal))
                throw new Exception("Invalid persisted scan data");
            return line.Substring(prefix.Length);
        }
    }
}












