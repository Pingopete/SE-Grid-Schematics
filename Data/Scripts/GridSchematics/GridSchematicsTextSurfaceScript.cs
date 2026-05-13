using Sandbox.Game.GameSystems.TextSurfaceScripts;
using Sandbox.ModAPI;
using System;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace GridSchematics
{
    [MyTextSurfaceScript(GridSchematicsSession.TEXT_SURFACE_SCRIPT_ID, "Grid Schematics")]
    public class GridSchematicsTextSurfaceScript : MyTSSCommon
    {
        const int MinimumSupportedResolutionAxis = 256;
        const string CalibrationTextFontId = "GridSchematics_Bahnschrift";

        public override ScriptUpdate NeedsUpdate
        {
            get { return ScriptUpdate.Update10; }
        }

        readonly IMyTextSurface _surface;
        readonly IMyCubeBlock _block;
        readonly IMyTerminalBlock _terminalBlock;

        GridSchematicsLcdApp _app;
        TouchScreenApiAdapter _unsupportedTouchInput;
        int _tick;
        bool _unsupportedMessageShown;
        int _unsupportedLowLevelFoundTick = -1;

        public GridSchematicsTextSurfaceScript(IMyTextSurface surface, IMyCubeBlock block, Vector2 size)
            : base(surface, block, size)
        {
            _surface = surface;
            _block = block;
            _terminalBlock = block as IMyTerminalBlock;

            if (_terminalBlock != null)
                _terminalBlock.OnMarkForClose += BlockMarkedForClose;

            _surface.ScriptBackgroundColor = Color.Black;
            _surface.ScriptForegroundColor = Color.White;
        }

        public override void Dispose()
        {
            base.Dispose();

            if (_terminalBlock != null)
                _terminalBlock.OnMarkForClose -= BlockMarkedForClose;

            if (_app != null)
            {
                var session = GridSchematicsSession.Instance;
                if (session != null)
                    session.UnregisterSurfaceScriptApp(_app);
                _app.Dispose();
                _app = null;
            }

            if (_unsupportedTouchInput != null)
            {
                var session = GridSchematicsSession.Instance;
                if (session != null)
                    session.UnregisterUnsupportedSurfaceProbe(_unsupportedTouchInput);
                _unsupportedTouchInput.Dispose();
                _unsupportedTouchInput = null;
            }
        }

        void BlockMarkedForClose(IMyEntity entity)
        {
            Dispose();
        }

        public override void Run()
        {
            try
            {
                base.Run();

                if (_app == null)
                {
                    TryInitializeApp();
                }

                _tick += 10;
                if (_app == null)
                {
                    DrawUnsupportedOrLoading();
                    return;
                }

                _app.Update(_tick);
            }
            catch (Exception e)
            {
                if (_app != null)
                {
                    _app.Dispose();
                    _app = null;
                }

                MyLog.Default.WriteLineAndConsole("[GridSchematics] TSS error: " + e);
                DrawMessage("Grid Schematics\nScript error");
            }
        }

        void TryInitializeApp()
        {
            var session = GridSchematicsSession.Instance;
            if (session == null)
                return;

            if (!IsSurfaceResolutionSupported(_surface))
            {
                TryInitializeUnsupportedCalibrationProbe(session);
                return;
            }

            if (_terminalBlock == null)
            {
                TryInitializeUnsupportedCalibrationProbe(session);
                return;
            }

            if (_unsupportedTouchInput != null)
            {
                session.UnregisterUnsupportedSurfaceProbe(_unsupportedTouchInput);
                _unsupportedTouchInput.Dispose();
                _unsupportedTouchInput = null;
            }

            _app = new GridSchematicsLcdApp(_terminalBlock, session, _surface);
            session.RegisterSurfaceScriptApp(_app);
        }

        void TryInitializeUnsupportedCalibrationProbe(GridSchematicsSession session)
        {
            if (_unsupportedTouchInput != null || _block == null || _surface == null)
                return;

            _unsupportedTouchInput = new TouchScreenApiAdapter();
            _unsupportedTouchInput.Initialize(_block, _surface);
            session.TryApplyStoredPanelCursorCalibration(_unsupportedTouchInput);
            session.RegisterUnsupportedSurfaceProbe(_unsupportedTouchInput);
        }

        void DrawUnsupportedOrLoading()
        {
            if (GridSchematicsSession.Instance == null)
            {
                DrawMessage("Grid Schematics\nWaiting for session");
                return;
            }

            if (!_unsupportedMessageShown)
            {
                _unsupportedMessageShown = true;
                MyLog.Default.WriteLineAndConsole("[GridSchematics] Grid Schematics LCD script is running low-level calibration preview on a non-IMyTextPanel surface.");
            }

            if (_unsupportedTouchInput != null)
            {
                _unsupportedTouchInput.ProcessInput();
                DrawUnsupportedCalibrationStatus();
                return;
            }

            DrawMessage("Grid Schematics\nCalibration preview unavailable");
        }

        void DrawUnsupportedCalibrationStatus()
        {
            Vector2 size = _surface.SurfaceSize;
            if (size.X <= 1f || size.Y <= 1f)
                return;

            Vector2 textureSize = GetSurfaceTextureSize();
            Vector2 origin = GetSurfaceViewportOrigin(size, textureSize);
            var session = GridSchematicsSession.Instance;
            int surfaceIndex = _unsupportedTouchInput != null ? _unsupportedTouchInput.GetSurfaceIndex() : -1;
            bool found = session != null && _unsupportedTouchInput != null
                ? session.IsPanelSurfaceLowLevelCalibrated(_unsupportedTouchInput.OwnerBlock, surfaceIndex)
                : _unsupportedTouchInput != null && _unsupportedTouchInput.HasStoredPanelCursorSurface;
            bool metricsCompatible = size.X >= MinimumSupportedResolutionAxis && size.Y >= MinimumSupportedResolutionAxis;
            bool activeManual = session != null && session.IsManualPanelCalibrationActive;
            bool hasManualSurface = session != null && _unsupportedTouchInput != null && session.IsManualPanelCalibrationSurface(_unsupportedTouchInput.OwnerBlock, surfaceIndex);
            bool selectedManualSurface = session != null && _unsupportedTouchInput != null && session.IsManualPanelCalibrationSelectedSurface(_unsupportedTouchInput.OwnerBlock, surfaceIndex);
            bool peerManualSurface = session != null && _unsupportedTouchInput != null && session.IsManualPanelCalibrationPeerSurface(_unsupportedTouchInput.OwnerBlock);
            bool fallbackSurface = session != null && _unsupportedTouchInput != null && session.IsManualPanelCalibrationFallbackSurface(_unsupportedTouchInput.OwnerBlock, surfaceIndex);
            if (activeManual && session != null && _unsupportedTouchInput != null)
                session.RegisterManualCalibrationRenderedInput(_unsupportedTouchInput);
            if (session != null && session.IsPanelCursorDepthOffsetCalibrationActive)
            {
                DrawUnsupportedDepthOffsetCalibrationStatus(frameOrigin: origin, size: size, surfaceIndex: surfaceIndex, session: session);
                return;
            }

            if (found && _unsupportedLowLevelFoundTick < 0)
                _unsupportedLowLevelFoundTick = _tick;
            if (!found)
                _unsupportedLowLevelFoundTick = -1;

            int countdown = 5;
            if (found && _unsupportedLowLevelFoundTick >= 0)
            {
                int remainingTicks = 300 - (_tick - _unsupportedLowLevelFoundTick);
                if (remainingTicks < 0)
                    remainingTicks = 0;
                countdown = (remainingTicks + 59) / 60;
            }

            float resolutionScale = Math.Min(1f, Math.Min(size.X / 900f, size.Y / 500f));
            float compactScale = 0.65f + resolutionScale * 0.35f;
            float scale = Math.Max(0.16f, Math.Min(0.41f, size.Y / 885f * compactScale));
            float titleScale = Math.Max(0.24f, Math.Min(0.55f, size.Y / 715f * (0.62f + resolutionScale * 0.38f)));
            float line = Math.Max(8f, size.Y * 0.046f * (0.80f + resolutionScale * 0.20f));
            float left = size.X * 0.06f;
            float right = size.X * 0.965f;
            float contentWidth = right - left;
            float top = size.Y * 0.045f;
            float metricScale = Math.Max(0.14f, scale * 0.90f);
            Color accent = found ? new Color(80, 255, 145) : new Color(255, 185, 45);
            Color background = selectedManualSurface ? new Color(0, 18, 10) : (peerManualSurface ? new Color(18, 0, 4) : Color.Black);

            using (var frame = _surface.DrawFrame())
            {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", origin + size * 0.5f, size, background));

                AddText(frame, "GRID SCHEMATICS", origin + new Vector2(left, top), Color.White, titleScale, TextAlignment.LEFT);
                float metricsY = top + line * 1.28f;
                AddText(frame, "CALIBRATION:", origin + new Vector2(left, metricsY), Color.White, metricScale, TextAlignment.LEFT);
                AddText(frame, found ? "FOUND" : "MISSING", origin + new Vector2(left + contentWidth * 0.34f, metricsY), found ? new Color(80, 255, 145) : new Color(255, 65, 65), metricScale, TextAlignment.LEFT);
                AddText(frame, "RES:", origin + new Vector2(left + contentWidth * 0.50f, metricsY), accent, metricScale, TextAlignment.LEFT);
                AddText(frame, ((int)size.X) + " X " + ((int)size.Y), origin + new Vector2(left + contentWidth * 0.58f, metricsY), new Color(190, 205, 212), metricScale, TextAlignment.LEFT);
                AddText(frame, "INDEX:", origin + new Vector2(left + contentWidth * 0.78f, metricsY), accent, metricScale, TextAlignment.LEFT);
                AddText(frame, surfaceIndex >= 0 ? surfaceIndex.ToString() : "?", origin + new Vector2(left + contentWidth * 0.91f, metricsY), new Color(190, 205, 212), metricScale, TextAlignment.LEFT);
                if (found && metricsCompatible && !activeManual)
                    return;

                if (activeManual)
                {
                    float legendBottom = DrawLowLevelCalibrationCommandLegend(frame, origin, left, right, top + line * 2.55f, line, scale, hasManualSurface, fallbackSurface);
                    DrawCalibrationExportNotice(frame, session, origin, _block, left, legendBottom + line * 0.45f, line * 0.62f, Math.Max(0.13f, Math.Min(scale * 0.72f, 0.22f)), new Color(80, 255, 145));
                }
                else
                {
                    if (!metricsCompatible)
                        AddText(frame, "INCOMPATIBLE PANEL METRICS.", origin + new Vector2(left, top + line * 2.70f), new Color(255, 65, 65), scale, TextAlignment.LEFT);
                    else if (!found)
                        AddText(frame, "CHAT /GSDISPLAYCAL TO BEGIN", origin + new Vector2(left, top + line * 2.70f), Color.White, scale, TextAlignment.LEFT);
                }
            }
        }

        void DrawUnsupportedDepthOffsetCalibrationStatus(Vector2 frameOrigin, Vector2 size, int surfaceIndex, GridSchematicsSession session)
        {
            bool selected = session != null && _unsupportedTouchInput != null && session.IsPanelCursorDepthOffsetCalibrationSelectedSurface(_unsupportedTouchInput.OwnerBlock, surfaceIndex);
            bool peer = session != null && _unsupportedTouchInput != null && session.IsPanelCursorDepthOffsetCalibrationPeerSurface(_unsupportedTouchInput.OwnerBlock);
            int displayIndex = session != null && _unsupportedTouchInput != null
                ? session.GetPanelCursorDepthOffsetCalibrationScreenIndex(_unsupportedTouchInput.OwnerBlock, surfaceIndex)
                : surfaceIndex;
            Color background = selected ? new Color(0, 18, 10) : (peer ? new Color(18, 0, 4) : Color.Black);

            using (var frame = _surface.DrawFrame())
            {
                frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", frameOrigin + size * 0.5f, size, background));
                AddText(frame, "INDEX: " + (displayIndex >= 0 ? displayIndex.ToString() : "?"), frameOrigin + new Vector2(Math.Max(10f, size.X * 0.055f), Math.Max(8f, size.Y * 0.045f)), new Color(255, 245, 70), Math.Max(0.18f, Math.Min(0.38f, size.Y / 900f)), TextAlignment.LEFT);
            }
        }

        static float DrawLowLevelCalibrationCommandLegend(MySpriteDrawFrame frame, Vector2 origin, float x, float right, float top, float line, float baseScale, bool hasManualSurface, bool fallbackSurface)
        {
            float y = top;
            float scale = Math.Max(0.19f, Math.Min(baseScale * 0.92f, 0.29f));
            float smallLine = Math.Max(6f, line * 0.72f);
            Color head = new Color(80, 255, 145);
            Color text = new Color(190, 205, 212);

            AddText(frame, "POSITION CONTROLS:", origin + new Vector2(x, y), head, scale, TextAlignment.LEFT);
            y += smallLine * 1.15f;
            AddCommandRow(frame, origin, x, right, y, "SCROLL:", "SCALE", text, scale);
            y += smallLine;
            AddCommandRow(frame, origin, x, right, y, "SHIFT+SCROLL:", "SHIFT CENTER X", text, scale);
            y += smallLine;
            AddCommandRow(frame, origin, x, right, y, "CTRL+SCROLL:", "SHIFT CENTER Y", text, scale);
            y += smallLine;
            AddCommandRow(frame, origin, x, right, y, "CTRL+SHIFT+SCROLL:", "RAISE/LOWER SURFACE", text, scale);
            y += smallLine;
            AddCommandRow(frame, origin, x, right, y, "ALT+SHIFT+SCROLL:", "SCALE X", text, scale);
            y += smallLine;
            AddCommandRow(frame, origin, x, right, y, "ALT+CTRL+SCROLL:", "SCALE Y", text, scale);
            y += smallLine;
            AddCommandRow(frame, origin, x, right, y, "CTRL+SHIFT+ALT+SCROLL:", "ROTATE", text, scale);
            y += smallLine * 1.35f;

            AddText(frame, "COMMANDS:", origin + new Vector2(x, y), head, scale, TextAlignment.LEFT);
            y += smallLine * 1.15f;
            AddCommandRow(frame, origin, x, right, y, "[ / ]:", "SELECT SCREEN INDEX", text, scale);
            y += smallLine;
            AddCommandRow(frame, origin, x, right, y, "H:", "TOGGLE FULL DEBUG VISUALS", text, scale);
            y += smallLine;
            AddCommandRow(frame, origin, x, right, y, "LMB:", "TARGET SCREEN CENTER", text, scale);
            y += smallLine;
            AddCommandRow(frame, origin, x, right, y, "MMB:", "FOCUS SCREEN DRAFT", text, scale);
            y += smallLine;
            AddCommandRow(frame, origin, x, right, y, "SHIFT+LMB:", "SAVE BLOCK CALIBRATIONS", text, scale);
            y += smallLine;
            AddCommandRow(frame, origin, x, right, y, "CTRL+LMB:", "EXPORT TO CLIPBOARD", text, scale);
            y += smallLine;
            AddCommandRow(frame, origin, x, right, y, "CTRL+ALT+LMB:", "EXPORT ALL TO CLIPBOARD", text, scale);
            y += smallLine;
            AddCommandRow(frame, origin, x, right, y, "CTRL+SHIFT+LMB:", "PROCEED TO CURSOR CALIBRATION", text, scale);
            y += smallLine;
            AddCommandRow(frame, origin, x, right, y, "RMB:", "RELOAD SAVED CALIBRATION", text, scale);
            y += smallLine;
            AddCommandRow(frame, origin, x, right, y, "CTRL+RMB:", "DELETE SAVED CALIBRATION", text, scale);
            return y + smallLine;
        }

        static void AddCommandRow(MySpriteDrawFrame frame, Vector2 origin, float left, float right, float y, string control, string action, Color color, float scale)
        {
            AddText(frame, control, origin + new Vector2(left, y), color, scale, TextAlignment.LEFT);
            AddText(frame, action, origin + new Vector2(right, y), color, scale, TextAlignment.RIGHT);
        }

        static void DrawCalibrationExportNotice(MySpriteDrawFrame frame, GridSchematicsSession session, Vector2 origin, IMyCubeBlock block, float x, float y, float line, float scale, Color color)
        {
            if (session == null || block == null)
                return;

            string blockId;
            string[] indexLines;
            if (!session.TryGetManualCalibrationExportNotice(block, out blockId, out indexLines))
                return;

            AddText(frame, "EXPORTED:", origin + new Vector2(x, y), color, scale, TextAlignment.LEFT);
            y += line;
            AddText(frame, "BLOCK ID: " + Shorten(blockId, 48), origin + new Vector2(x, y), color, scale, TextAlignment.LEFT);
            y += line;
            if (indexLines == null)
                return;

            for (int i = 0; i < indexLines.Length; i++)
            {
                AddText(frame, indexLines[i], origin + new Vector2(x, y), color, scale, TextAlignment.LEFT);
                y += line;
            }
        }

        static bool IsSurfaceResolutionSupported(IMyTextSurface surface)
        {
            if (surface == null)
                return false;

            try
            {
                Vector2 size = surface.SurfaceSize;
                return size.X >= MinimumSupportedResolutionAxis && size.Y >= MinimumSupportedResolutionAxis;
            }
            catch
            {
                return false;
            }
        }

        Vector2 GetSurfaceTextureSize()
        {
            try
            {
                return _surface.TextureSize;
            }
            catch
            {
                return _surface != null ? _surface.SurfaceSize : Vector2.Zero;
            }
        }

        static Vector2 GetSurfaceViewportOrigin(Vector2 surfaceSize, Vector2 textureSize)
        {
            Vector2 origin = (textureSize - surfaceSize) * 0.5f;
            if (origin.X < 0f) origin.X = 0f;
            if (origin.Y < 0f) origin.Y = 0f;
            return origin;
        }

        static void DrawScreenBorder(MySpriteDrawFrame frame, Vector2 origin, Vector2 size, Color color)
        {
            float thickness = Math.Max(2f, Math.Min(6f, size.Y * 0.01f));
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", origin + new Vector2(size.X * 0.5f, thickness * 0.5f), new Vector2(size.X, thickness), color));
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", origin + new Vector2(size.X * 0.5f, size.Y - thickness * 0.5f), new Vector2(size.X, thickness), color));
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", origin + new Vector2(thickness * 0.5f, size.Y * 0.5f), new Vector2(thickness, size.Y), color));
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", origin + new Vector2(size.X - thickness * 0.5f, size.Y * 0.5f), new Vector2(thickness, size.Y), color));
        }

        static void DrawRectBorder(MySpriteDrawFrame frame, Vector2 origin, Vector2 center, Vector2 size, Color color, float thickness)
        {
            thickness = Math.Max(1f, thickness);
            float left = center.X - size.X * 0.5f;
            float right = center.X + size.X * 0.5f;
            float top = center.Y - size.Y * 0.5f;
            float bottom = center.Y + size.Y * 0.5f;
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", origin + new Vector2(center.X, top), new Vector2(size.X, thickness), color));
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", origin + new Vector2(center.X, bottom), new Vector2(size.X, thickness), color));
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", origin + new Vector2(left, center.Y), new Vector2(thickness, size.Y), color));
            frame.Add(new MySprite(SpriteType.TEXTURE, "SquareSimple", origin + new Vector2(right, center.Y), new Vector2(thickness, size.Y), color));
        }

        static void AddText(MySpriteDrawFrame frame, string text, Vector2 position, Color color, float scale, TextAlignment alignment)
        {
            frame.Add(new MySprite(SpriteType.TEXT, text ?? string.Empty, position, null, color, CalibrationTextFontId, alignment, scale));
        }

        static Color GetCandidateColor(PanelSurfaceCandidateQuality quality)
        {
            if (quality == PanelSurfaceCandidateQuality.Good)
                return new Color(80, 255, 145);
            if (quality == PanelSurfaceCandidateQuality.Questionable)
                return new Color(255, 185, 45);
            if (quality == PanelSurfaceCandidateQuality.Bad)
                return new Color(255, 65, 65);
            return new Color(120, 190, 255);
        }

        static string Shorten(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text) || maxLength <= 0)
                return string.Empty;
            if (text.Length <= maxLength)
                return text;
            if (maxLength <= 3)
                return text.Substring(0, maxLength);
            return text.Substring(0, maxLength - 3) + "...";
        }

        void DrawMessage(string message)
        {
            Vector2 size = _surface.SurfaceSize;
            Vector2 textureSize = GetSurfaceTextureSize();
            Vector2 origin = GetSurfaceViewportOrigin(size, textureSize);

            using (var frame = _surface.DrawFrame())
            {
                frame.Add(new MySprite(
                    SpriteType.TEXTURE,
                    "SquareSimple",
                    origin + size * 0.5f,
                    size,
                    Color.Black
                ));

                frame.Add(new MySprite(
                    SpriteType.TEXT,
                    message,
                    origin + size * 0.5f,
                    null,
                    Color.White,
                    "Debug",
                    TextAlignment.CENTER,
                    0.9f
                ));
            }
        }
    }
}
