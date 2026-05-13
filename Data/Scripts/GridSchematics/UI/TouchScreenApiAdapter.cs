using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Models;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;
using IngameTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;
using IngameCubeBlock = VRage.Game.ModAPI.Ingame.IMyCubeBlock;

namespace GridSchematics
{
    public partial class TouchScreenApiAdapter
    {
        const long TouchApiChannel = 2668820525;
        static readonly bool UseAimCursorByDefault = true;
        const double AimDisplayFallbackPhysicalFillX = 0.86;
        const double AimDisplayFallbackPhysicalFillY = 0.82;
        const double AimCursorVisualInset = 0.006;
        const double AimCursorInteractionTolerance = 0.004;
        const int DetectorBoundsSampleSteps = 9;
        const int DetectorBoundsMinSamples = 6;
        const double DetectorBoundsSearchScaleX = 1.12;
        const double DetectorBoundsSearchScaleY = 1.34;
        const int MaxDetectorDebugSamples = 96;
        static readonly string[] ScreenSubpartProbeNames = new string[]
        {
            "ScreenArea",
            "ScreenArea90",
            "ScreenArea180",
            "ScreenArea270",
            "detector_textpanel",
            "detector_textpanel_1",
            "detector_textpanel_2",
            "detector_textpanel_3",
            "textpanel",
            "lcd",
            "screen"
        };

        struct DetectorDebugSample
        {
            public Vector3D Position;
            public double TargetX;
            public double TargetY;
        }

        static bool _apiRegistered;
        static bool _apiReady;
        static int _apiRequestCooldown;
        static Func<IngameCubeBlock, IngameTextSurface, object> _createTouchScreen;
        static Action<IngameCubeBlock, IngameTextSurface> _removeTouchScreen;
        static Func<object, bool> _touchScreenIsOnScreen;
        static Func<object, Vector2> _touchScreenGetCursorPosition;
        static Func<object, object> _touchScreenGetMouse1;
        static Func<object, object> _touchScreenGetMouse2;
        static Func<object, bool> _buttonStateJustReleased;
        static Func<object, bool> _buttonStateJustPressed;
        static Func<object, bool> _buttonStateIsPressed;

        readonly List<HitRegion> _hitRegions = new List<HitRegion>();
        IMyCubeBlock _panel;
        IngameTextSurface _surface;
        object _touchScreen;
        string _pressedRegionId;
        bool _reportedCurrentPress;
        int _createRetryCooldown;
        int _scrollVisualFrames;
        int _targetDummyCollisionLayer;
        int _detectorBoundsRefreshCooldown;
        int _detectorBoundsSampleCount;
        bool _hasDetectorBounds;
        double _detectorMinX;
        double _detectorMaxX;
        double _detectorMinY;
        double _detectorMaxY;
        double _detectorMinZ;
        double _detectorMaxZ;
        bool _hasDetectorPlaneBounds;
        Vector3D _detectorPlaneCenter;
        Vector3D _detectorPlaneRight;
        Vector3D _detectorPlaneUp;
        double _detectorPlaneMinX;
        double _detectorPlaneMaxX;
        double _detectorPlaneMinY;
        double _detectorPlaneMaxY;
        readonly List<DetectorDebugSample> _detectorDebugSamples = new List<DetectorDebugSample>();
        bool _lastAimDetectorHitInsideBounds;
        Vector3D _lastAimDetectorHitPosition;
        bool _wasPressed;
        bool _wasSecondaryPressed;
        bool _hasCapturedFrameButtonState;
        bool _capturedPrimaryPressed;
        bool _capturedSecondaryPressed;
        bool _capturedPrimaryJustPressed;
        bool _capturedPrimaryJustReleased;
        bool _capturedSecondaryJustPressed;
        bool _capturedSecondaryJustReleased;
        string _capturedPrimaryPressedRegionId;
        string _capturedPrimaryClickRegionId;
        bool _hasCursorCalibration;
        bool _mouseControlEnabled;
        bool _hasMouseControlPosition;
        Vector2 _mouseControlRawPosition;
        float _cursorCalibrationM11 = 1f;
        float _cursorCalibrationM12;
        float _cursorCalibrationM13;
        float _cursorCalibrationM21;
        float _cursorCalibrationM22 = 1f;
        float _cursorCalibrationM23;

        public bool IsAvailable { get; private set; }
        public string StatusText { get; private set; }
        public string LastHitRegionId { get; private set; }
        public string HoverRegionId { get; private set; }
        public Vector2 CursorPosition { get; private set; }
        public Vector2 RawCursorPosition { get; private set; }
        public Vector2 PreviousCursorPosition { get; private set; }
        public bool IsCursorOnScreen { get; private set; }
        public bool HasPreviousCursorPosition { get; private set; }
        public bool IsPressed { get; private set; }
        public bool JustPressed { get; private set; }
        public bool JustReleased { get; private set; }
        public bool HasSecondaryButton { get; private set; }
        public bool IsSecondaryPressed { get; private set; }
        public bool SecondaryJustPressed { get; private set; }
        public bool SecondaryJustReleased { get; private set; }
        public bool IsAimCursorActive { get; private set; }
        public bool IsVisualCursorOnScreen { get; private set; }
        public bool IsScrollVisualActive
        {
            get { return _scrollVisualFrames > 0; }
        }
        public IMyCubeBlock OwnerBlock
        {
            get { return _panel; }
        }

        public TouchScreenApiAdapter()
        {
            IsAvailable = false;
            StatusText = "Touch: waiting";
            LastHitRegionId = string.Empty;
            HoverRegionId = string.Empty;
            CursorPosition = Vector2.Zero;
            RawCursorPosition = Vector2.Zero;
            _createRetryCooldown = 0;
            _targetDummyCollisionLayer = -2;
            _detectorBoundsRefreshCooldown = 0;
            _detectorBoundsSampleCount = 0;
            _hasDetectorBounds = false;
            _hasDetectorPlaneBounds = false;
            _lastAimDetectorHitInsideBounds = false;
            _hasModelWalkDebugCorners = false;
            _modelWalkCaptureWasPressed = false;
            _wasPressed = false;
            _wasSecondaryPressed = false;
            _capturedPrimaryPressedRegionId = string.Empty;
            _capturedPrimaryClickRegionId = string.Empty;
        }

        public void Initialize(IMyCubeBlock panel, IMyTextSurface surface)
        {
            _panel = panel;
            _surface = surface as IngameTextSurface;
            if (!UseAimCursorByDefault)
            {
                EnsureApiLoaded();
                TryCreateTouchScreen();
            }
        }

        public void SetCursorCalibration(bool hasCalibration, float m11, float m12, float m13, float m21, float m22, float m23)
        {
            _hasCursorCalibration = hasCalibration;
            _cursorCalibrationM11 = m11;
            _cursorCalibrationM12 = m12;
            _cursorCalibrationM13 = m13;
            _cursorCalibrationM21 = m21;
            _cursorCalibrationM22 = m22;
            _cursorCalibrationM23 = m23;
        }

        public void SetMouseControlState(bool enabled, bool hasPosition, Vector2 rawPosition)
        {
            _mouseControlEnabled = enabled;
            _hasMouseControlPosition = hasPosition;
            _mouseControlRawPosition = rawPosition;
        }

        public void ClearHitRegions()
        {
            _hitRegions.Clear();
        }

        public void AddHitRegion(HitRegion region)
        {
            _hitRegions.Add(region);
        }

        public void ProcessInput()
        {
            if (_scrollVisualFrames > 0)
                _scrollVisualFrames--;

            LastHitRegionId = string.Empty;
            HoverRegionId = string.Empty;
            bool previousCursorOnScreen = IsCursorOnScreen;
            Vector2 previousCursorPosition = CursorPosition;
            IsCursorOnScreen = false;
            HasPreviousCursorPosition = false;
            IsPressed = false;
            JustPressed = false;
            JustReleased = false;
            HasSecondaryButton = false;
            IsSecondaryPressed = false;
            SecondaryJustPressed = false;
            SecondaryJustReleased = false;
            IsAimCursorActive = false;
            IsVisualCursorOnScreen = false;
            UpdateModelWalkDebugCapture();

            if (UseAimCursorByDefault && ProcessAimCursorInput(previousCursorOnScreen, previousCursorPosition))
                return;

            EnsureApiLoaded();
            TryCreateTouchScreen();

            IsAvailable = _apiReady && _touchScreen != null;
            if (!IsAvailable)
            {
                if (!_apiReady)
                    StatusText = "Touch: API wait";
            else if (_surface == null)
                    StatusText = "Touch: no surface";
                else if (_touchScreen == null)
                    StatusText = "Touch: no screen";
                return;
            }

            try
            {
                IsCursorOnScreen = _touchScreenIsOnScreen != null && _touchScreenIsOnScreen(_touchScreen);
                RawCursorPosition = _touchScreenGetCursorPosition != null ? _touchScreenGetCursorPosition(_touchScreen) : Vector2.Zero;
                CursorPosition = ApplyCursorCalibration(RawCursorPosition);
                if (previousCursorOnScreen && IsCursorOnScreen)
                {
                    PreviousCursorPosition = previousCursorPosition;
                    HasPreviousCursorPosition = true;
                }
                StatusText = IsCursorOnScreen ? "Touch: active" : "Touch: calibrate/aim";

                var mouse1 = _touchScreenGetMouse1 != null ? _touchScreenGetMouse1(_touchScreen) : null;
                if (mouse1 != null)
                {
                    IsPressed = _buttonStateIsPressed != null && _buttonStateIsPressed(mouse1);
                    JustPressed = _buttonStateJustPressed != null && _buttonStateJustPressed(mouse1);
                    JustReleased = _buttonStateJustReleased != null && _buttonStateJustReleased(mouse1);
                }

                var mouse2 = _touchScreenGetMouse2 != null ? _touchScreenGetMouse2(_touchScreen) : null;
                HasSecondaryButton = mouse2 != null;
                if (mouse2 != null)
                {
                    IsSecondaryPressed = _buttonStateIsPressed != null && _buttonStateIsPressed(mouse2);
                    SecondaryJustPressed = _buttonStateJustPressed != null && _buttonStateJustPressed(mouse2);
                    SecondaryJustReleased = _buttonStateJustReleased != null && _buttonStateJustReleased(mouse2);
                    if (IsSecondaryPressed && !_wasSecondaryPressed)
                        SecondaryJustPressed = true;
                    if (!IsSecondaryPressed && _wasSecondaryPressed)
                        SecondaryJustReleased = true;
                }

                if (IsCursorOnScreen)
                {
                    HoverRegionId = FindHitRegion(CursorPosition);

                    bool pressStarted = JustPressed || (IsPressed && !_wasPressed);
                    bool pressEnded = JustReleased || (!IsPressed && _wasPressed);

                    if (pressStarted)
                    {
                        _pressedRegionId = HoverRegionId;
                        _reportedCurrentPress = false;
                    }

                    if (pressEnded)
                    {
                        if (!_reportedCurrentPress && !string.IsNullOrEmpty(_pressedRegionId) &&
                            string.Equals(_pressedRegionId, HoverRegionId, StringComparison.Ordinal))
                        {
                            LastHitRegionId = _pressedRegionId;
                        }

                        _pressedRegionId = string.Empty;
                        _reportedCurrentPress = false;
                    }
                }
                else if (!IsPressed)
                {
                    _pressedRegionId = string.Empty;
                    _reportedCurrentPress = false;
                }

                _wasPressed = IsPressed;
                _wasSecondaryPressed = IsSecondaryPressed;
            }
            catch
            {
                IsAvailable = false;
                StatusText = "Touch: input err";
                _wasPressed = false;
                _wasSecondaryPressed = false;
                _pressedRegionId = string.Empty;
                _reportedCurrentPress = false;
                ResetCapturedFrameButtonState();
            }
        }

        bool ProcessAimCursorInput(bool previousCursorOnScreen, Vector2 previousCursorPosition)
        {
            IsAvailable = true;
            HasSecondaryButton = true;
            StatusText = "Aim: searching";

            if (_mouseControlEnabled)
                return ProcessMouseControlledCursor(previousCursorOnScreen, previousCursorPosition);

            Vector2 surfacePoint;
            bool visualCursorOnScreen;
            if (!TryGetAimSurfacePoint(out surfacePoint, out visualCursorOnScreen))
            {
                IsCursorOnScreen = false;
                StatusText = "Aim: off screen";
                if (!_wasPressed)
                {
                    _pressedRegionId = string.Empty;
                    _reportedCurrentPress = false;
                }

                CaptureAimButtonState(false);
                ResetCapturedFrameButtonState();
                _wasPressed = IsPressed;
                _wasSecondaryPressed = IsSecondaryPressed;
                return true;
            }

            IsAimCursorActive = true;
            IsCursorOnScreen = true;
            IsVisualCursorOnScreen = visualCursorOnScreen;
            RawCursorPosition = surfacePoint;
            CursorPosition = ApplyCursorCalibration(surfacePoint);
            if (previousCursorOnScreen)
            {
                PreviousCursorPosition = previousCursorPosition;
                HasPreviousCursorPosition = true;
            }

            StatusText = BuildAimSurfaceStatusText();
            CaptureAimButtonState(true);

            HoverRegionId = FindHitRegion(CursorPosition);
            ApplyCapturedFrameButtonState();
            bool pressStarted = JustPressed || (IsPressed && !_wasPressed);
            bool pressEnded = JustReleased || (!IsPressed && _wasPressed);

            if (pressStarted)
            {
                _pressedRegionId = HoverRegionId;
                _reportedCurrentPress = false;
            }

            if (pressEnded)
            {
                if (!_reportedCurrentPress && !string.IsNullOrEmpty(_pressedRegionId) &&
                    string.Equals(_pressedRegionId, HoverRegionId, StringComparison.Ordinal))
                {
                    LastHitRegionId = _pressedRegionId;
                }

                _pressedRegionId = string.Empty;
                _reportedCurrentPress = false;
            }

            ConsumeCapturedFrameButtonState();
            _wasPressed = IsPressed;
            _wasSecondaryPressed = IsSecondaryPressed;
            return true;
        }

        bool ProcessMouseControlledCursor(bool previousCursorOnScreen, Vector2 previousCursorPosition)
        {
            IsAimCursorActive = true;
            IsCursorOnScreen = _hasMouseControlPosition;
            IsVisualCursorOnScreen = _hasMouseControlPosition;
            StatusText = _hasMouseControlPosition ? "Mouse: active" : "Mouse: aim panel";

            if (!_hasMouseControlPosition)
            {
                if (!_wasPressed)
                {
                    _pressedRegionId = string.Empty;
                    _reportedCurrentPress = false;
                }

                CaptureAimButtonState(false);
                ResetCapturedFrameButtonState();
                _wasPressed = IsPressed;
                _wasSecondaryPressed = IsSecondaryPressed;
                return true;
            }

            RawCursorPosition = _mouseControlRawPosition;
            CursorPosition = ApplyCursorCalibration(RawCursorPosition);
            if (previousCursorOnScreen)
            {
                PreviousCursorPosition = previousCursorPosition;
                HasPreviousCursorPosition = true;
            }

            CaptureAimButtonState(true);
            HoverRegionId = FindHitRegion(CursorPosition);
            ApplyCapturedFrameButtonState();
            bool pressStarted = JustPressed || (IsPressed && !_wasPressed);
            bool pressEnded = JustReleased || (!IsPressed && _wasPressed);

            if (pressStarted)
            {
                _pressedRegionId = HoverRegionId;
                _reportedCurrentPress = false;
            }

            if (pressEnded)
            {
                if (!_reportedCurrentPress && !string.IsNullOrEmpty(_pressedRegionId) &&
                    string.Equals(_pressedRegionId, HoverRegionId, StringComparison.Ordinal))
                    LastHitRegionId = _pressedRegionId;

                _pressedRegionId = string.Empty;
                _reportedCurrentPress = false;
            }

            ConsumeCapturedFrameButtonState();
            _wasPressed = IsPressed;
            _wasSecondaryPressed = IsSecondaryPressed;
            return true;
        }

        public void CaptureFrameButtonState(Vector2 rawSurfacePoint, bool primaryPressed, bool secondaryPressed)
        {
            if (_surface == null)
                return;

            var cursorPosition = ApplyCursorCalibration(rawSurfacePoint);
            string hoverRegionId = FindHitRegion(cursorPosition);

            if (!_hasCapturedFrameButtonState)
            {
                _hasCapturedFrameButtonState = true;
                _capturedPrimaryPressed = primaryPressed;
                _capturedSecondaryPressed = secondaryPressed;
                _capturedPrimaryPressedRegionId = primaryPressed ? hoverRegionId : string.Empty;
                return;
            }

            if (primaryPressed && !_capturedPrimaryPressed)
            {
                _capturedPrimaryJustPressed = true;
                _capturedPrimaryPressedRegionId = hoverRegionId;
            }
            else if (!primaryPressed && _capturedPrimaryPressed)
            {
                _capturedPrimaryJustReleased = true;
                if (!string.IsNullOrEmpty(_capturedPrimaryPressedRegionId) &&
                    string.Equals(_capturedPrimaryPressedRegionId, hoverRegionId, StringComparison.Ordinal))
                {
                    _capturedPrimaryClickRegionId = _capturedPrimaryPressedRegionId;
                }

                _capturedPrimaryPressedRegionId = string.Empty;
            }

            if (secondaryPressed && !_capturedSecondaryPressed)
                _capturedSecondaryJustPressed = true;
            else if (!secondaryPressed && _capturedSecondaryPressed)
                _capturedSecondaryJustReleased = true;

            _capturedPrimaryPressed = primaryPressed;
            _capturedSecondaryPressed = secondaryPressed;
        }

        void ApplyCapturedFrameButtonState()
        {
            if (!_hasCapturedFrameButtonState)
                return;

            if (_capturedPrimaryPressed)
                IsPressed = true;
            if (_capturedPrimaryJustPressed)
                JustPressed = true;
            if (_capturedPrimaryJustReleased)
                JustReleased = true;

            HasSecondaryButton = true;
            if (_capturedSecondaryPressed)
                IsSecondaryPressed = true;
            if (_capturedSecondaryJustPressed)
                SecondaryJustPressed = true;
            if (_capturedSecondaryJustReleased)
                SecondaryJustReleased = true;
        }

        void ConsumeCapturedFrameButtonState()
        {
            if (!string.IsNullOrEmpty(_capturedPrimaryClickRegionId) && string.IsNullOrEmpty(LastHitRegionId))
                LastHitRegionId = _capturedPrimaryClickRegionId;

            _capturedPrimaryJustPressed = false;
            _capturedPrimaryJustReleased = false;
            _capturedSecondaryJustPressed = false;
            _capturedSecondaryJustReleased = false;
            _capturedPrimaryClickRegionId = string.Empty;
        }

        void ResetCapturedFrameButtonState()
        {
            _hasCapturedFrameButtonState = false;
            _capturedPrimaryPressed = false;
            _capturedSecondaryPressed = false;
            _capturedPrimaryJustPressed = false;
            _capturedPrimaryJustReleased = false;
            _capturedSecondaryJustPressed = false;
            _capturedSecondaryJustReleased = false;
            _capturedPrimaryPressedRegionId = string.Empty;
            _capturedPrimaryClickRegionId = string.Empty;
        }

        void CaptureAimButtonState(bool cursorOnScreen)
        {
            if (MyAPIGateway.Input == null)
                return;

            try
            {
                bool guiCursorVisible = MyAPIGateway.Gui != null && MyAPIGateway.Gui.IsCursorVisible;
                bool acceptInput = cursorOnScreen && !guiCursorVisible;
                IsPressed = acceptInput && MyAPIGateway.Input.IsLeftMousePressed();
                JustPressed = IsPressed && !_wasPressed;
                JustReleased = !IsPressed && _wasPressed;
                IsSecondaryPressed = acceptInput && MyAPIGateway.Input.IsRightMousePressed();
                SecondaryJustPressed = IsSecondaryPressed && !_wasSecondaryPressed;
                SecondaryJustReleased = !IsSecondaryPressed && _wasSecondaryPressed;
            }
            catch
            {
                IsPressed = false;
                JustPressed = false;
                JustReleased = false;
                IsSecondaryPressed = false;
                SecondaryJustPressed = false;
                SecondaryJustReleased = false;
            }
        }

        Vector2 ApplyCursorCalibration(Vector2 point)
        {
            if (!_hasCursorCalibration)
                return point;

            return new Vector2(
                _cursorCalibrationM11 * point.X + _cursorCalibrationM12 * point.Y + _cursorCalibrationM13,
                _cursorCalibrationM21 * point.X + _cursorCalibrationM22 * point.Y + _cursorCalibrationM23);
        }

        string BuildAimSurfaceStatusText()
        {
            if (_panel == null || _surface == null)
                return "Aim: active";

            string surfaceName = string.Empty;
            string displayName = string.Empty;
            string subtypeName = string.Empty;
            int surfaceIndex = -1;
            bool hasModSurface = false;
            bool hasModProvider = false;
            Vector3D blockPosition = Vector3D.Zero;
            try
            {
                surfaceName = _surface.Name ?? string.Empty;
                displayName = _surface.DisplayName ?? string.Empty;
                subtypeName = _panel.BlockDefinition.SubtypeName ?? string.Empty;
                hasModSurface = (_surface as Sandbox.ModAPI.IMyTextSurface) != null;
                hasModProvider = (_panel as Sandbox.ModAPI.IMyTextSurfaceProvider) != null;
                blockPosition = _panel.GetPosition();
                var provider = _panel as Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;
                if (provider != null)
                {
                    for (int i = 0; i < provider.SurfaceCount; i++)
                    {
                        if (object.ReferenceEquals(provider.GetSurface(i), _surface))
                        {
                            surfaceIndex = i;
                            break;
                        }
                    }
                }
            }
            catch
            {
            }

            Vector2 surfaceSize = Vector2.Zero;
            Vector2 textureSize = Vector2.Zero;
            try
            {
                surfaceSize = _surface.SurfaceSize;
                textureSize = GetSurfaceTextureSize();
            }
            catch
            {
            }

            string area = !string.IsNullOrEmpty(displayName) ? displayName : surfaceName;
            if (string.IsNullOrEmpty(area))
                area = "surface";

            string indexText = surfaceIndex >= 0 ? "#" + surfaceIndex : "#?";
            return "Aim:" + indexText +
                " " + ShortenDebugText(area, 14) +
                " " + ShortenDebugText(subtypeName, 24) +
                " sz:" + ((int)surfaceSize.X) + "x" + ((int)surfaceSize.Y) +
                " sub:" + ShortenDebugText(BuildSubpartProbeStatus(surfaceName, displayName), 16) +
                " ms:" + (hasModSurface ? "Y" : "N") +
                " mp:" + (hasModProvider ? "Y" : "N") +
                " b:" + blockPosition.X.ToString("0.0") + "," +
                blockPosition.Y.ToString("0.0") + "," +
                blockPosition.Z.ToString("0.0");
        }

        static string ShortenDebugText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || maxLength <= 0)
                return string.Empty;

            if (text.Length <= maxLength)
                return text;

            if (maxLength <= 3)
                return text.Substring(0, maxLength);

            return text.Substring(0, maxLength - 3) + "...";
        }

        string BuildRaycastDetectorStatusText()
        {
            if (_panel == null || MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return "det:N/A";

            try
            {
                var block = _panel as VRage.Game.ModAPI.IMyCubeBlock;
                if (block == null)
                    return "det:NOBLOCK";

                var camera = MyAPIGateway.Session.Camera.WorldMatrix;
                Vector3D rayOrigin = camera.Translation;
                Vector3D rayDirection = camera.Forward;
                if (rayDirection.LengthSquared() <= 0.000001)
                    return "det:NORAY";

                rayDirection.Normalize();
                string detector = block.RaycastDetectors(rayOrigin, rayOrigin + rayDirection * 12.0);
                return string.IsNullOrEmpty(detector) ? "det:<none>" : "det:" + detector;
            }
            catch
            {
                return "det:ERR";
            }
        }

        public string GetAimDetectorDebugText()
        {
            return BuildRaycastDetectorStatusText();
        }

        public bool TryGetLastAimDetectorDebugHit(out Vector3D hitPosition, out bool insideBounds)
        {
            hitPosition = _lastAimDetectorHitPosition;
            insideBounds = _lastAimDetectorHitInsideBounds;
            return hitPosition.LengthSquared() > 0.000001;
        }

        public bool TryGetCurrentDetectorAimDebugHit(out Vector3D hitPosition)
        {
            hitPosition = Vector3D.Zero;

            if (_panel == null || MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return false;

            var camera = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D rayOrigin = camera.Translation;
            Vector3D rayDirection = camera.Forward;
            if (rayDirection.LengthSquared() <= 0.000001)
                return false;
            rayDirection.Normalize();

            return TryGetPanelDetectorAimHit(rayOrigin, rayDirection, out hitPosition);
        }

        string BuildModelIntersectionStatusText()
        {
            if (_panel == null || MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return "mdl:N/A";

            try
            {
                var entity = _panel as VRage.ModAPI.IMyEntity;
                if (entity == null || entity.Model == null)
                    return "mdl:NOMODEL";

                var camera = MyAPIGateway.Session.Camera.WorldMatrix;
                Vector3D rayOrigin = camera.Translation;
                Vector3D rayDirection = camera.Forward;
                if (rayDirection.LengthSquared() <= 0.000001)
                    return "mdl:NORAY";

                rayDirection.Normalize();
                var line = new LineD(rayOrigin, rayOrigin + rayDirection * 12.0);
                MyIntersectionResultLineTriangleEx? tri;
                if (!entity.GetIntersectionWithLine(ref line, out tri, IntersectionFlags.ALL_TRIANGLES) || !tri.HasValue)
                    return "mdl:<none>";

                var hit = tri.Value;
                Vector3D worldHit = hit.IntersectionPointInWorldSpace;
                Vector3D localHit = Vector3D.Transform(worldHit, MatrixD.Invert(_panel.WorldMatrix));
                Vector3D localNormal = Vector3D.TransformNormal(hit.NormalInWorldSpace, MatrixD.Transpose(_panel.WorldMatrix));
                if (localNormal.LengthSquared() > 0.000001)
                    localNormal.Normalize();

                return "mdl:hit" +
                    " lh=" + localHit.X.ToString("0.00") + "," +
                    localHit.Y.ToString("0.00") + "," +
                    localHit.Z.ToString("0.00") +
                    " ln=" + localNormal.X.ToString("0.00") + "," +
                    localNormal.Y.ToString("0.00") + "," +
                    localNormal.Z.ToString("0.00");
            }
            catch
            {
                return "mdl:ERR";
            }
        }

        string BuildSubpartProbeStatus(string surfaceName, string displayName)
        {
            if (_panel == null)
                return "NOPANEL";

            string matchedName = string.Empty;
            Vector3D matchedPosition = Vector3D.Zero;
            if (TryFindNamedSubpart(surfaceName, out matchedName, out matchedPosition) ||
                TryFindNamedSubpart(displayName, out matchedName, out matchedPosition))
            {
                return matchedName + "@" + matchedPosition.X.ToString("0.0") + "," +
                    matchedPosition.Y.ToString("0.0") + "," +
                    matchedPosition.Z.ToString("0.0");
            }

            for (int i = 0; i < ScreenSubpartProbeNames.Length; i++)
            {
                if (TryFindNamedSubpart(ScreenSubpartProbeNames[i], out matchedName, out matchedPosition))
                {
                    return matchedName + "@" + matchedPosition.X.ToString("0.0") + "," +
                        matchedPosition.Y.ToString("0.0") + "," +
                        matchedPosition.Z.ToString("0.0");
                }
            }

            return "NONE";
        }

        bool TryFindNamedSubpart(string name, out string matchedName, out Vector3D matchedPosition)
        {
            matchedName = string.Empty;
            matchedPosition = Vector3D.Zero;
            if (_panel == null || string.IsNullOrEmpty(name))
                return false;

            try
            {
                MyEntitySubpart subpart;
                if (_panel.TryGetSubpart(name, out subpart) && subpart != null)
                {
                    matchedName = name;
                    matchedPosition = subpart.WorldMatrix.Translation;
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        bool TryGetAimSurfacePoint(out Vector2 surfacePoint, out bool visualCursorOnScreen)
        {
            surfacePoint = Vector2.Zero;
            visualCursorOnScreen = false;
            if (_panel == null || _surface == null || MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
            {
                return false;
            }

            var camera = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D rayOrigin = camera.Translation;
            Vector3D rayDirection = camera.Forward;
            if (rayDirection.LengthSquared() <= 0.000001)
            {
                return false;
            }
            rayDirection.Normalize();

            if (TryGetPolygonDiscoveredAimSurfacePoint(rayOrigin, rayDirection, out surfacePoint, out visualCursorOnScreen))
            {
                _lastAimDetectorHitPosition = GetPolygonDiscoveredLastPlaneHit();
                _lastAimDetectorHitInsideBounds = visualCursorOnScreen;
                return true;
            }

            return false;
        }

        bool TryGetPanelDetectorAimHit(Vector3D rayOrigin, Vector3D rayDirection, out Vector3D hitPosition)
        {
            hitPosition = Vector3D.Zero;
            if (_panel == null || MyAPIGateway.Physics == null)
                return false;

            try
            {
                if (_targetDummyCollisionLayer == -2)
                    _targetDummyCollisionLayer = MyAPIGateway.Physics.GetCollisionLayer("TargetDummyLayer");

                Vector3D rayEnd = rayOrigin + rayDirection * 12.0;
                IHitInfo hit;
                if (_targetDummyCollisionLayer >= 0 && MyAPIGateway.Physics.CastRay(rayOrigin, rayEnd, out hit, _targetDummyCollisionLayer))
                {
                    if (IsHitOnThisPanel(hit))
                    {
                        hitPosition = hit.Position;
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        bool TryGetPanelDetectorSegmentHit(Vector3D rayStart, Vector3D rayEnd, out Vector3D hitPosition)
        {
            hitPosition = Vector3D.Zero;
            if (_panel == null || MyAPIGateway.Physics == null)
                return false;

            try
            {
                if (_targetDummyCollisionLayer == -2)
                    _targetDummyCollisionLayer = MyAPIGateway.Physics.GetCollisionLayer("TargetDummyLayer");

                IHitInfo hit;
                if (_targetDummyCollisionLayer >= 0 && MyAPIGateway.Physics.CastRay(rayStart, rayEnd, out hit, _targetDummyCollisionLayer))
                {
                    if (IsHitOnThisPanel(hit))
                    {
                        hitPosition = hit.Position;
                        return true;
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }

        public bool TryGetAimDetectorPlaneFit(Vector3D rayOrigin, Vector3D rayDirection, Vector3D axisAHint, Vector3D axisBHint, double sampleStep, double maxSampleDistance, out Vector3D seed, out Vector3D normal, out Vector3D axisA, out Vector3D axisB)
        {
            seed = Vector3D.Zero;
            normal = Vector3D.Zero;
            axisA = Vector3D.Zero;
            axisB = Vector3D.Zero;
            if (_panel == null || rayDirection.LengthSquared() <= 0.000001 || axisAHint.LengthSquared() <= 0.000001 || axisBHint.LengthSquared() <= 0.000001)
                return false;

            rayDirection.Normalize();
            axisAHint.Normalize();
            axisBHint.Normalize();

            if (!TryGetPanelDetectorAimHit(rayOrigin, rayDirection, out seed))
                return false;

            Vector3D rightHit;
            bool hasRight = TryGetDetectorOffsetHit(rayOrigin, rayDirection, axisAHint * sampleStep, seed, maxSampleDistance, out rightHit);
            Vector3D leftHit;
            bool hasLeft = TryGetDetectorOffsetHit(rayOrigin, rayDirection, -axisAHint * sampleStep, seed, maxSampleDistance, out leftHit);
            Vector3D upHit;
            bool hasUp = TryGetDetectorOffsetHit(rayOrigin, rayDirection, -axisBHint * sampleStep, seed, maxSampleDistance, out upHit);
            Vector3D downHit;
            bool hasDown = TryGetDetectorOffsetHit(rayOrigin, rayDirection, axisBHint * sampleStep, seed, maxSampleDistance, out downHit);

            Vector3D tangentA = Vector3D.Zero;
            if (hasRight && hasLeft)
                tangentA = rightHit - leftHit;
            else if (hasRight)
                tangentA = rightHit - seed;
            else if (hasLeft)
                tangentA = seed - leftHit;

            Vector3D tangentB = Vector3D.Zero;
            if (hasUp && hasDown)
                tangentB = upHit - downHit;
            else if (hasUp)
                tangentB = upHit - seed;
            else if (hasDown)
                tangentB = seed - downHit;

            if (tangentA.LengthSquared() <= 0.000001 || tangentB.LengthSquared() <= 0.000001)
                return false;

            axisA = tangentA;
            axisB = tangentB;
            axisA.Normalize();
            axisB.Normalize();

            normal = Vector3D.Cross(axisA, axisB);
            if (normal.LengthSquared() <= 0.000001)
                return false;

            normal.Normalize();
            if (Vector3D.Dot(rayOrigin - seed, normal) < 0.0)
            {
                normal = -normal;
                axisB = -axisB;
            }

            axisA = axisA - normal * Vector3D.Dot(axisA, normal);
            if (axisA.LengthSquared() <= 0.000001)
                return false;
            axisA.Normalize();
            axisB = Vector3D.Cross(normal, axisA);
            if (axisB.LengthSquared() <= 0.000001)
                return false;
            axisB.Normalize();
            return true;
        }

        bool TryGetDetectorOffsetHit(Vector3D rayOrigin, Vector3D rayDirection, Vector3D offset, Vector3D centerHit, double maxSampleDistance, out Vector3D hitPosition)
        {
            hitPosition = Vector3D.Zero;
            Vector3D start = rayOrigin + offset;
            Vector3D end = start + rayDirection * 12.0;
            if (!TryGetPanelDetectorSegmentHit(start, end, out hitPosition))
                return false;

            return Vector3D.DistanceSquared(hitPosition, centerHit) <= maxSampleDistance * maxSampleDistance;
        }

        bool IsHitOnThisPanel(IHitInfo hit)
        {
            if (hit == null || hit.HitEntity == null || _panel == null)
                return false;

            if (hit.HitEntity.EntityId == _panel.EntityId)
                return true;

            if (_panel.CubeGrid != null && hit.HitEntity.EntityId == _panel.CubeGrid.EntityId)
            {
                Vector3D local = Vector3D.Transform(hit.Position, MatrixD.Invert(_panel.WorldMatrix));
                BoundingBoxD box = _panel.LocalAABB;
                const double tolerance = 0.04;
                return local.X >= box.Min.X - tolerance && local.X <= box.Max.X + tolerance &&
                    local.Y >= box.Min.Y - tolerance && local.Y <= box.Max.Y + tolerance &&
                    local.Z >= box.Min.Z - tolerance && local.Z <= box.Max.Z + tolerance;
            }

            return false;
        }

        void TryRefreshDetectorBounds(Vector3D rayOrigin, Vector3D displayCenter, Vector3D normal, Vector3D right, Vector3D up, double faceWidth, double faceHeight, Vector3D detectorHitPosition)
        {
            if (_hasDetectorBounds && _detectorBoundsSampleCount >= DetectorBoundsMinSamples)
                return;

            if (_detectorBoundsRefreshCooldown > 0)
            {
                _detectorBoundsRefreshCooldown--;
                return;
            }

            _detectorBoundsRefreshCooldown = 30;
            _detectorBoundsSampleCount = 0;
            _hasDetectorBounds = false;
            _hasDetectorPlaneBounds = false;
            _detectorDebugSamples.Clear();

            MatrixD panelMatrix = _panel.WorldMatrix;
            BoundingBoxD box = _panel.LocalAABB;
            double localZ = (box.Min.Z + box.Max.Z) * 0.5;
            double castDepth = Math.Max(0.12, Math.Abs(box.Max.Z - box.Min.Z) + 0.12);
            Vector3D panelNormal = panelMatrix.Forward;
            if (panelNormal.LengthSquared() <= 0.000001)
                return;
            panelNormal.Normalize();

            for (int y = 0; y < DetectorBoundsSampleSteps; y++)
            {
                double fy = DetectorBoundsSampleSteps <= 1 ? 0.5 : (double)y / (DetectorBoundsSampleSteps - 1);
                double localY = (0.5 - fy) * faceHeight * DetectorBoundsSearchScaleY;
                for (int x = 0; x < DetectorBoundsSampleSteps; x++)
                {
                    double fx = DetectorBoundsSampleSteps <= 1 ? 0.5 : (double)x / (DetectorBoundsSampleSteps - 1);
                    double localX = (fx - 0.5) * faceWidth * DetectorBoundsSearchScaleX;
                    Vector3D localTarget = new Vector3D(localX, localY, localZ);
                    Vector3D target = Vector3D.Transform(localTarget, panelMatrix);
                    Vector3D rayStart = target + panelNormal * castDepth;
                    Vector3D rayEnd = target - panelNormal * castDepth;
                    Vector3D hitPosition;
                    if (TryGetPanelDetectorSegmentHit(rayStart, rayEnd, out hitPosition) ||
                        TryGetPanelDetectorSegmentHit(rayEnd, rayStart, out hitPosition))
                    {
                        AddDetectorBoundsSample(hitPosition, localX, localY);
                    }
                }
            }

            RebuildDetectorPlaneBounds();
        }

        static Vector3D GetDetectorAnchoredSampleCenter(Vector3D displayCenter, Vector3D normal, Vector3D detectorHitPosition)
        {
            if (normal.LengthSquared() <= 0.000001)
                return displayCenter;

            normal.Normalize();
            return displayCenter + normal * Vector3D.Dot(detectorHitPosition - displayCenter, normal);
        }

        void AddDetectorBoundsSample(Vector3D hitPosition, double targetX, double targetY)
        {
            if (_detectorDebugSamples.Count < MaxDetectorDebugSamples)
            {
                _detectorDebugSamples.Add(new DetectorDebugSample
                {
                    Position = hitPosition,
                    TargetX = targetX,
                    TargetY = targetY
                });
            }

            Vector3D local = Vector3D.Transform(hitPosition, MatrixD.Invert(_panel.WorldMatrix));
            if (!_hasDetectorBounds)
            {
                _detectorMinX = local.X;
            _detectorMaxX = local.X;
            _detectorMinY = local.Y;
            _detectorMaxY = local.Y;
                _detectorMinZ = local.Z;
                _detectorMaxZ = local.Z;
                _hasDetectorBounds = true;
            }
            else
            {
                if (local.X < _detectorMinX)
                    _detectorMinX = local.X;
                if (local.X > _detectorMaxX)
                    _detectorMaxX = local.X;
                if (local.Y < _detectorMinY)
                    _detectorMinY = local.Y;
                if (local.Y > _detectorMaxY)
                    _detectorMaxY = local.Y;
                if (local.Z < _detectorMinZ)
                    _detectorMinZ = local.Z;
                if (local.Z > _detectorMaxZ)
                    _detectorMaxZ = local.Z;
            }

            _detectorBoundsSampleCount++;
        }

        void RebuildDetectorPlaneBounds()
        {
            if (_detectorDebugSamples.Count < DetectorBoundsMinSamples)
                return;

            MatrixD panelMatrix = _panel.WorldMatrix;
            Vector3D right = panelMatrix.Right;
            Vector3D up = panelMatrix.Up;
            if (right.LengthSquared() <= 0.000001 || up.LengthSquared() <= 0.000001)
                return;

            right.Normalize();
            up.Normalize();

            MatrixD inversePanelMatrix = MatrixD.Invert(panelMatrix);
            Vector3D firstLocalHit = Vector3D.Transform(_detectorDebugSamples[0].Position, inversePanelMatrix);
            double minLocalX = firstLocalHit.X;
            double maxLocalX = firstLocalHit.X;
            double minLocalY = firstLocalHit.Y;
            double maxLocalY = firstLocalHit.Y;
            double averageLocalZ = firstLocalHit.Z;

            for (int i = 0; i < _detectorDebugSamples.Count; i++)
            {
                DetectorDebugSample sample = _detectorDebugSamples[i];
                Vector3D localHit = Vector3D.Transform(sample.Position, inversePanelMatrix);
                if (localHit.X < minLocalX)
                    minLocalX = localHit.X;
                if (localHit.X > maxLocalX)
                    maxLocalX = localHit.X;
                if (localHit.Y < minLocalY)
                    minLocalY = localHit.Y;
                if (localHit.Y > maxLocalY)
                    maxLocalY = localHit.Y;

                averageLocalZ += localHit.Z;
            }

            averageLocalZ /= _detectorDebugSamples.Count + 1;
            double centerX = (minLocalX + maxLocalX) * 0.5;
            double centerY = (minLocalY + maxLocalY) * 0.5;

            _detectorPlaneCenter = Vector3D.Transform(new Vector3D(centerX, centerY, averageLocalZ), panelMatrix);
            _detectorPlaneRight = right;
            _detectorPlaneUp = up;
            _detectorPlaneMinX = minLocalX - centerX;
            _detectorPlaneMaxX = maxLocalX - centerX;
            _detectorPlaneMinY = minLocalY - centerY;
            _detectorPlaneMaxY = maxLocalY - centerY;

            _hasDetectorPlaneBounds = true;
        }

        bool IsDetectorHitInsideDiscoveredBounds(Vector3D hitPosition)
        {
            if (!_hasDetectorBounds || _detectorBoundsSampleCount < DetectorBoundsMinSamples)
                return true;

            if (_hasDetectorPlaneBounds)
            {
                Vector3D delta = hitPosition - _detectorPlaneCenter;
                double x = Vector3D.Dot(delta, _detectorPlaneRight);
                double y = Vector3D.Dot(delta, _detectorPlaneUp);
                return x >= _detectorPlaneMinX && x <= _detectorPlaneMaxX &&
                    y >= _detectorPlaneMinY && y <= _detectorPlaneMaxY;
            }

            Vector3D local = Vector3D.Transform(hitPosition, MatrixD.Invert(_panel.WorldMatrix));
            return local.X >= _detectorMinX && local.X <= _detectorMaxX &&
                local.Y >= _detectorMinY && local.Y <= _detectorMaxY;
        }
        public bool TryGetDiscoveredDisplayCorners(out Vector3D topLeft, out Vector3D topRight, out Vector3D bottomRight, out Vector3D bottomLeft)
        {
            topLeft = Vector3D.Zero;
            topRight = Vector3D.Zero;
            bottomRight = Vector3D.Zero;
            bottomLeft = Vector3D.Zero;

            if (_panel == null || _surface == null || MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return false;

            var camera = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D rayOrigin = camera.Translation;
            Vector3D rayDirection = camera.Forward;
            if (rayDirection.LengthSquared() <= 0.000001)
                return false;
            rayDirection.Normalize();

            var panelMatrix = _panel.WorldMatrix;
            Vector3D right = panelMatrix.Right;
            Vector3D up = panelMatrix.Up;
            Vector3D forward = panelMatrix.Forward;
            Vector3D backward = panelMatrix.Backward;
            BoundingBoxD box = _panel.LocalAABB;
            Vector3D localCenter = (box.Min + box.Max) * 0.5;
            double faceWidth = Math.Max(0.01, Math.Abs(box.Max.X - box.Min.X));
            double faceHeight = Math.Max(0.01, Math.Abs(box.Max.Y - box.Min.Y));
            Vector3D forwardFaceCenter = panelMatrix.Translation + right * localCenter.X + up * localCenter.Y + backward * box.Min.Z;
            Vector3D backwardFaceCenter = panelMatrix.Translation + right * localCenter.X + up * localCenter.Y + backward * box.Max.Z;

            double forwardDistance;
            bool forwardHit = TryGetFaceIntersectionDistance(rayOrigin, rayDirection, forwardFaceCenter, forward, out forwardDistance);
            double backwardDistance;
            bool backwardHit = TryGetFaceIntersectionDistance(rayOrigin, rayDirection, backwardFaceCenter, backward, out backwardDistance);

            if (forwardHit && (!backwardHit || forwardDistance <= backwardDistance))
                return TryBuildDiscoveredDisplayCorners(forwardFaceCenter, forward, right, up, faceWidth, faceHeight, out topLeft, out topRight, out bottomRight, out bottomLeft);

            if (backwardHit)
                return TryBuildDiscoveredDisplayCorners(backwardFaceCenter, backward, -right, up, faceWidth, faceHeight, out topLeft, out topRight, out bottomRight, out bottomLeft);

            return false;
        }

        static bool TryGetFaceIntersectionDistance(Vector3D rayOrigin, Vector3D rayDirection, Vector3D displayCenter, Vector3D normal, out double distance)
        {
            distance = 0.0;
            double denom = Vector3D.Dot(rayDirection, normal);
            if (Math.Abs(denom) < 0.0001)
                return false;

            distance = Vector3D.Dot(displayCenter - rayOrigin, normal) / denom;
            return distance >= 0.05 && distance <= 12.0;
        }

        bool TryBuildDiscoveredDisplayCorners(Vector3D displayCenter, Vector3D normal, Vector3D right, Vector3D up, double faceWidth, double faceHeight, out Vector3D topLeft, out Vector3D topRight, out Vector3D bottomRight, out Vector3D bottomLeft)
        {
            topLeft = Vector3D.Zero;
            topRight = Vector3D.Zero;
            bottomRight = Vector3D.Zero;
            bottomLeft = Vector3D.Zero;

            Vector2 surfaceSize = _surface.SurfaceSize;
            Vector2 textureSize = GetSurfaceTextureSize();
            double physicalAspect = textureSize.Y <= 0f ? (surfaceSize.Y <= 0f ? 1.0 : surfaceSize.X / surfaceSize.Y) : textureSize.X / textureSize.Y;
            double physicalFillX;
            double physicalFillY;
            GetSurfacePhysicalFill(surfaceSize, textureSize, out physicalFillX, out physicalFillY);
            double displayWidth;
            double displayHeight;
            GetPhysicalDisplayRect(faceWidth, faceHeight, physicalAspect, physicalFillX, physicalFillY, out displayWidth, out displayHeight);
            if (displayWidth <= 0.001 || displayHeight <= 0.001)
                return false;

            Vector3D normalOffset = normal;
            if (normalOffset.LengthSquared() > 0.000001)
                normalOffset.Normalize();
            Vector3D center = displayCenter + normalOffset * 0.006;
            Vector3D horizontal = right * (displayWidth * 0.5);
            Vector3D vertical = up * (displayHeight * 0.5);
            topLeft = center - horizontal + vertical;
            topRight = center + horizontal + vertical;
            bottomRight = center + horizontal - vertical;
            bottomLeft = center - horizontal - vertical;
            return true;
        }

        bool TryProjectAimToPanelFace(Vector3D rayOrigin, Vector3D rayDirection, Vector3D displayCenter, Vector3D normal, Vector3D right, Vector3D up, double faceWidth, double faceHeight, bool allowClampedOutside, out Vector2 surfacePoint, out Vector2 rawUv, out double distance)
        {
            surfacePoint = Vector2.Zero;
            rawUv = Vector2.Zero;
            distance = 0.0;

            double denom = Vector3D.Dot(rayDirection, normal);
            if (Math.Abs(denom) < 0.0001)
                return false;

            distance = Vector3D.Dot(displayCenter - rayOrigin, normal) / denom;
            if (distance < 0.05 || distance > 12.0)
                return false;

            Vector3D hit = rayOrigin + rayDirection * distance;
            Vector3D local = hit - displayCenter;

            double localX = Vector3D.Dot(local, right);
            double localY = Vector3D.Dot(local, up);
            Vector2 surfaceSize = _surface.SurfaceSize;
            Vector2 textureSize = GetSurfaceTextureSize();
            double physicalAspect = textureSize.Y <= 0f ? (surfaceSize.Y <= 0f ? 1.0 : surfaceSize.X / surfaceSize.Y) : textureSize.X / textureSize.Y;
            double physicalFillX;
            double physicalFillY;
            GetSurfacePhysicalFill(surfaceSize, textureSize, out physicalFillX, out physicalFillY);
            double displayWidth;
            double displayHeight;
            GetPhysicalDisplayRect(faceWidth, faceHeight, physicalAspect, physicalFillX, physicalFillY, out displayWidth, out displayHeight);

            if (displayWidth <= 0.001 || displayHeight <= 0.001)
                return false;

            double u = localX / displayWidth + 0.5;
            double v = 0.5 - localY / displayHeight;
            rawUv = new Vector2((float)u, (float)v);

            if (!allowClampedOutside &&
                (u < -AimCursorInteractionTolerance || u > 1.0 + AimCursorInteractionTolerance ||
                v < -AimCursorInteractionTolerance || v > 1.0 + AimCursorInteractionTolerance))
                return false;

            double clampedU = Clamp01(u);
            double clampedV = Clamp01(v);
            surfacePoint = new Vector2((float)(clampedU * _surface.SurfaceSize.X), (float)(clampedV * _surface.SurfaceSize.Y));
            return true;
        }

        Vector2 GetSurfaceTextureSize()
        {
            try
            {
                return _surface.TextureSize;
            }
            catch
            {
                return _surface.SurfaceSize;
            }
        }

        static void GetSurfacePhysicalFill(Vector2 surfaceSize, Vector2 textureSize, out double fillX, out double fillY)
        {
            fillX = AimDisplayFallbackPhysicalFillX;
            fillY = AimDisplayFallbackPhysicalFillY;

            if (surfaceSize.X <= 0f || surfaceSize.Y <= 0f || textureSize.X <= 0f || textureSize.Y <= 0f)
                return;

            double ratioX = surfaceSize.X / textureSize.X;
            double ratioY = surfaceSize.Y / textureSize.Y;
            if (ratioX > 0.2 && ratioX < 0.98)
                fillX = ratioX;
            if (ratioY > 0.2 && ratioY < 0.98)
                fillY = ratioY;
        }

        static void GetPhysicalDisplayRect(double faceWidth, double faceHeight, double physicalAspect, double physicalFillX, double physicalFillY, out double displayWidth, out double displayHeight)
        {
            displayWidth = faceWidth;
            displayHeight = displayWidth / physicalAspect;
            if (displayHeight > faceHeight)
            {
                displayHeight = faceHeight;
                displayWidth = displayHeight * physicalAspect;
            }

            displayWidth *= physicalFillX;
            displayHeight *= physicalFillY;
        }

        bool IsProjectedSurfacePointVisuallyInside(Vector2 surfacePoint)
        {
            if (_surface == null || _surface.SurfaceSize.X <= 0f || _surface.SurfaceSize.Y <= 0f)
                return false;

            double u = surfacePoint.X / _surface.SurfaceSize.X;
            double v = surfacePoint.Y / _surface.SurfaceSize.Y;
            return u >= AimCursorVisualInset && u <= 1.0 - AimCursorVisualInset &&
                v >= AimCursorVisualInset && v <= 1.0 - AimCursorVisualInset;
        }

        static double Clamp01(double value)
        {
            if (value < 0.0)
                return 0.0;
            if (value > 1.0)
                return 1.0;
            return value;
        }

        public void MarkScrollActive()
        {
            _scrollVisualFrames = 8;
        }

        public void Dispose()
        {
            var block = _panel as IngameCubeBlock;
            if (_touchScreen == null || block == null || _surface == null || _removeTouchScreen == null)
                return;

            try
            {
                _removeTouchScreen(block, _surface);
            }
            catch
            {
            }

            _touchScreen = null;
            _createRetryCooldown = 0;
        }

        public static void UnloadSharedApi()
        {
            if (_apiRegistered && MyAPIGateway.Utilities != null)
            {
                try
                {
                    MyAPIGateway.Utilities.UnregisterMessageHandler(TouchApiChannel, HandleApiMessage);
                }
                catch
                {
                }
            }

            _apiRegistered = false;
            _apiReady = false;
            _apiRequestCooldown = 0;
            _createTouchScreen = null;
            _removeTouchScreen = null;
            _touchScreenIsOnScreen = null;
            _touchScreenGetCursorPosition = null;
            _touchScreenGetMouse1 = null;
            _touchScreenGetMouse2 = null;
            _buttonStateJustReleased = null;
            _buttonStateJustPressed = null;
            _buttonStateIsPressed = null;
        }

        string FindHitRegion(Vector2 point)
        {
            const float hitPadding = 3f;
            for (int i = _hitRegions.Count - 1; i >= 0; i--)
            {
                var region = _hitRegions[i];
                if (point.X >= region.X - hitPadding && point.X <= region.X + region.Width + hitPadding &&
                    point.Y >= region.Y - hitPadding && point.Y <= region.Y + region.Height + hitPadding)
                {
                    return region.Id;
                }
            }

            return string.Empty;
        }

        void TryCreateTouchScreen()
        {
            if (_touchScreen != null || !_apiReady || _createTouchScreen == null || _panel == null || _surface == null)
                return;

            if (_createRetryCooldown > 0)
            {
                _createRetryCooldown--;
                return;
            }

            var block = _panel as IngameCubeBlock;
            if (block == null)
            {
                StatusText = "Touch: bad block";
                return;
            }

            try
            {
                _touchScreen = _createTouchScreen(block, _surface);
                IsAvailable = _touchScreen != null;
                StatusText = IsAvailable ? "Touch: ready" : "Touch: create fail";
                if (!IsAvailable)
                    _createRetryCooldown = 120;
            }
            catch
            {
                IsAvailable = false;
                StatusText = "Touch: create err";
                _createRetryCooldown = 120;
            }
        }

        static void EnsureApiLoaded()
        {
            if (MyAPIGateway.Utilities == null)
                return;

            if (!_apiRegistered)
            {
                _apiRegistered = true;
                MyAPIGateway.Utilities.RegisterMessageHandler(TouchApiChannel, HandleApiMessage);
            }

            if (!_apiReady)
            {
                if (_apiRequestCooldown > 0)
                {
                    _apiRequestCooldown--;
                }
                else
                {
                    _apiRequestCooldown = 120;
                    MyAPIGateway.Utilities.SendModMessage(TouchApiChannel, "ApiRequestTouch");
                }
            }
        }

        static void HandleApiMessage(object message)
        {
            if (_apiReady || message is string)
                return;

            var delegates = message as IReadOnlyDictionary<string, Delegate>;
            if (delegates == null)
                return;

            try
            {
                Assign(delegates, "CreateTouchScreen", ref _createTouchScreen);
                Assign(delegates, "RemoveTouchScreen", ref _removeTouchScreen);
                Assign(delegates, "TouchScreen_IsOnScreen", ref _touchScreenIsOnScreen);
                Assign(delegates, "TouchScreen_GetCursorPosition", ref _touchScreenGetCursorPosition);
                Assign(delegates, "TouchScreen_GetMouse1", ref _touchScreenGetMouse1);
                TryAssign(delegates, "TouchScreen_GetMouse2", ref _touchScreenGetMouse2);
                Assign(delegates, "ButtonState_JustReleased", ref _buttonStateJustReleased);
                Assign(delegates, "ButtonState_JustPressed", ref _buttonStateJustPressed);
                Assign(delegates, "ButtonState_IsPressed", ref _buttonStateIsPressed);
                _apiReady = true;
            }
            catch
            {
                _apiReady = false;
            }
        }

        static void Assign<T>(IReadOnlyDictionary<string, Delegate> delegates, string name, ref T field) where T : class
        {
            Delegate value;
            if (!delegates.TryGetValue(name, out value))
                throw new Exception("Missing TouchScreenAPI delegate: " + name);

            field = value as T;
            if (field == null)
                throw new Exception("Wrong TouchScreenAPI delegate type: " + name);
        }

        static void TryAssign<T>(IReadOnlyDictionary<string, Delegate> delegates, string name, ref T field) where T : class
        {
            Delegate value;
            if (!delegates.TryGetValue(name, out value))
                return;

            field = value as T;
        }
    }
}
