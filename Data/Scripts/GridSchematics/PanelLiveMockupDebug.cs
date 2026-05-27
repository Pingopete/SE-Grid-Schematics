using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;
using IMyTextSurfaceProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace GridSchematics
{
    public partial class GridSchematicsSession
    {
        const string PanelLiveMockupCommand = "/GSLIVE";
        const int PanelLiveMockupDrawInterval = 3;
        static Vector2 PanelLiveMockupViewportOrigin;

        bool _panelLiveMockupCommandRegistered;
        bool _panelLiveMockupEnabled;
        IMyCubeBlock _panelLiveMockupBlock;
        IMyTextSurface _panelLiveMockupSurface;
        int _panelLiveMockupSurfaceIndex = -1;
        string _panelLiveMockupFont = "BAHNSCHRIFT";
        string _panelLiveMockupMode = "BRIDGE";
        string _panelLiveBridgeForceFont;
        int _panelLiveBridgeLastHash;
        int _panelLiveBridgeLastSpriteCount;
        string _panelLiveBridgeLastStatus;

        void RegisterPanelLiveMockupCommand()
        {
            if (_panelLiveMockupCommandRegistered || MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.MessageEntered += OnPanelLiveMockupMessageEntered;
            _panelLiveMockupCommandRegistered = true;
        }

        void UnregisterPanelLiveMockupCommand()
        {
            if (!_panelLiveMockupCommandRegistered || MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.MessageEntered -= OnPanelLiveMockupMessageEntered;
            _panelLiveMockupCommandRegistered = false;
        }

        void OnPanelLiveMockupMessageEntered(string messageText, ref bool sendToOthers)
        {
            string text = messageText == null ? string.Empty : messageText.Trim();
            string[] parts = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || !IsPanelLiveMockupCommand(parts[0]))
                return;

            sendToOthers = false;
            string action = parts.Length > 1 ? parts[1].ToUpperInvariant() : "ON";

            if (action == "OFF" || action == "STOP")
            {
                ClearPanelLiveMockup();
                ShowPanelLiveMockupMessage("Live mockup stopped.");
                return;
            }

            if (action == "FONT" || action == "FORCEFONT")
            {
                if (parts.Length > 2)
                {
                    string requestedFont = parts[2];
                    if (requestedFont.Equals("OFF", StringComparison.OrdinalIgnoreCase) ||
                        requestedFont.Equals("CLEAR", StringComparison.OrdinalIgnoreCase) ||
                        requestedFont.Equals("NONE", StringComparison.OrdinalIgnoreCase) ||
                        requestedFont.Equals("TOOL", StringComparison.OrdinalIgnoreCase))
                    {
                        _panelLiveBridgeForceFont = null;
                        ShowPanelLiveMockupMessage("Live bridge font override cleared. Using tool FontId values.");
                        return;
                    }

                    _panelLiveMockupFont = GridSchematicsConfig.NormalizeUiFont(requestedFont);
                    _panelLiveBridgeForceFont = _panelLiveMockupFont;
                }

                if (!string.IsNullOrEmpty(_panelLiveBridgeForceFont))
                    ShowPanelLiveMockupMessage("Live bridge force font: " + GridSchematicsConfig.GetUiFontLabel(_panelLiveBridgeForceFont));
                else
                    ShowPanelLiveMockupMessage("Live mockup font: " + GridSchematicsConfig.GetUiFontLabel(_panelLiveMockupFont));
                return;
            }

            if (action == "MODE")
            {
                if (parts.Length > 2)
                    _panelLiveMockupMode = NormalizePanelLiveMockupMode(parts[2]);
                ShowPanelLiveMockupMessage("Live mockup mode: " + _panelLiveMockupMode);
                return;
            }

            if (action == "HELP")
            {
                ShowPanelLiveMockupMessage("/GSLIVE ON while aiming at an LCD. Tool bridge is default. /GSLIVE MODE CARGO for fallback. /GSLIVE OFF.");
                return;
            }

            if (parts.Length > 1 && action != "ON" && action != "START")
                _panelLiveMockupFont = GridSchematicsConfig.NormalizeUiFont(parts[1]);

            IMyCubeBlock block;
            IMyTextSurface surface;
            int surfaceIndex;
            if (!TryFindAimedPanelLiveMockupSurface(out block, out surface, out surfaceIndex))
            {
                ShowPanelLiveMockupMessage("Aim at an LCD surface, then run /GSLIVE ON.");
                return;
            }

            _panelLiveMockupBlock = block;
            _panelLiveMockupSurface = surface;
            _panelLiveMockupSurfaceIndex = surfaceIndex;
            _panelLiveMockupEnabled = true;
            PreparePanelLiveMockupSurface(surface);
            DrawPanelLiveMockup(surface, 0);

            var terminalBlock = block as Sandbox.ModAPI.IMyTerminalBlock;
            string name = terminalBlock != null ? terminalBlock.CustomName : "LCD";
            ShowPanelLiveMockupMessage("Live mockup started on " + name + " surface " + surfaceIndex + ".");
        }

        static bool IsPanelLiveMockupCommand(string command)
        {
            return command.Equals(PanelLiveMockupCommand, StringComparison.OrdinalIgnoreCase) ||
                command.Equals("\\GSLIVE", StringComparison.OrdinalIgnoreCase) ||
                command.Equals("/GSMOCK", StringComparison.OrdinalIgnoreCase) ||
                command.Equals("\\GSMOCK", StringComparison.OrdinalIgnoreCase);
        }

        static string NormalizePanelLiveMockupMode(string mode)
        {
            if (string.IsNullOrWhiteSpace(mode))
                return "BRIDGE";

            mode = mode.Trim().ToUpperInvariant();
            if (mode == "BRIDGE" || mode == "TOOL" || mode == "LIVE")
                return "BRIDGE";
            if (mode == "CARGO" || mode == "INFO" || mode == "BARE" || mode == "MOCK")
                return mode == "MOCK" ? "CARGO" : mode;
            return "BRIDGE";
        }

        void UpdatePanelLiveMockup(int tick)
        {
            if (!_panelLiveMockupEnabled)
                return;

            if (tick % PanelLiveMockupDrawInterval != 0)
                return;

            if (_panelLiveMockupBlock == null ||
                _panelLiveMockupBlock.MarkedForClose ||
                !_panelLiveMockupBlock.IsFunctional ||
                _panelLiveMockupSurface == null)
            {
                ClearPanelLiveMockup();
                return;
            }

            PreparePanelLiveMockupSurface(_panelLiveMockupSurface);
            DrawPanelLiveMockup(_panelLiveMockupSurface, tick);
        }

        void ClearPanelLiveMockup()
        {
            _panelLiveMockupEnabled = false;
            _panelLiveMockupBlock = null;
            _panelLiveMockupSurface = null;
            _panelLiveMockupSurfaceIndex = -1;
        }

        bool TryFindAimedPanelLiveMockupSurface(out IMyCubeBlock block, out IMyTextSurface surface, out int surfaceIndex)
        {
            block = null;
            surface = null;
            surfaceIndex = -1;

            GridSchematicsLcdApp app;
            PanelCursorWorldDrawData cursor;
            if (TryFindPanelSurfaceCursorApp(out app, out cursor) && app != null && app.OwnerBlock != null && app.Surface != null)
            {
                block = app.OwnerBlock as IMyCubeBlock;
                surface = app.Surface;
                surfaceIndex = GetAppSurfaceIndex(app);
                return block != null;
            }

            IMyEntity entity;
            if (!TryGetAimedEntity(out entity))
                return false;

            block = entity as IMyCubeBlock;
            if (block == null)
                return false;

            var directSurface = block as IMyTextSurface;
            if (directSurface != null)
            {
                surface = directSurface;
                surfaceIndex = 0;
                return true;
            }

            var provider = block as IMyTextSurfaceProvider;
            if (provider != null && provider.SurfaceCount > 0)
            {
                surface = provider.GetSurface(0);
                surfaceIndex = 0;
                return surface != null;
            }

            return false;
        }

        static bool TryGetAimedEntity(out IMyEntity entity)
        {
            entity = null;
            try
            {
                if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null || MyAPIGateway.Physics == null)
                    return false;

                MatrixD camera = MyAPIGateway.Session.Camera.WorldMatrix;
                Vector3D rayOrigin = camera.Translation;
                Vector3D rayDirection = camera.Forward;
                if (rayDirection.LengthSquared() <= 0.000001)
                    return false;
                rayDirection.Normalize();

                IHitInfo physicsHit;
                if (!MyAPIGateway.Physics.CastRay(rayOrigin, rayOrigin + rayDirection * 12.0, out physicsHit) || physicsHit == null)
                    return false;

                return TryResolveModelEntityFromPhysicsHit(physicsHit, rayDirection, out entity);
            }
            catch
            {
                return false;
            }
        }

        static void PreparePanelLiveMockupSurface(IMyTextSurface surface)
        {
            if (surface == null)
                return;

            surface.ContentType = ContentType.SCRIPT;
            surface.ScriptForegroundColor = Color.White;
            surface.ScriptBackgroundColor = Color.Black;
        }

        void DrawPanelLiveMockup(IMyTextSurface surface, int tick)
        {
            Vector2 size = surface.SurfaceSize;
            if (size.X <= 0f || size.Y <= 0f)
                return;

            string font = GridSchematicsConfig.GetUiFontSubtype(_panelLiveMockupFont);
            LiveBridgeFrame bridgeFrame = null;
            bool hasBridge = _panelLiveMockupMode == "BRIDGE" && TryReadLiveBridgeFrame(out bridgeFrame);
            Vector2 previousViewportOrigin = PanelLiveMockupViewportOrigin;
            PanelLiveMockupViewportOrigin = GetPanelLiveMockupViewportOrigin(surface, size);
            try
            {
                using (var frame = surface.DrawFrame())
                {
                    if (hasBridge)
                    {
                        DrawLiveBridgeFrame(frame, size, bridgeFrame);
                        return;
                    }

                    DrawLiveMockupBackground(frame, size);

                    if (_panelLiveMockupMode == "BRIDGE")
                        DrawLiveBridgeMissing(frame, size, font);
                    else if (_panelLiveMockupMode == "BARE")
                        DrawLiveMockupBare(frame, size, font, tick);
                    else
                        DrawLiveMockupCargo(frame, size, font, tick);
                }
            }
            finally
            {
                PanelLiveMockupViewportOrigin = previousViewportOrigin;
            }
        }

        class LiveBridgeFrame
        {
            public float Width = 512f;
            public float Height = 512f;
            public readonly List<LiveBridgeSprite> Sprites = new List<LiveBridgeSprite>();
        }

        struct LiveBridgeSprite
        {
            public bool IsText;
            public string Data;
            public float X;
            public float Y;
            public float Width;
            public float Height;
            public byte R;
            public byte G;
            public byte B;
            public byte A;
            public float RotationOrScale;
            public string FontId;
            public TextAlignment Alignment;
        }

        bool TryReadLiveBridgeFrame(out LiveBridgeFrame frame)
        {
            frame = null;
            if (MyAPIGateway.Utilities == null)
                return false;

            TextReader reader = null;
            try
            {
                reader = MyAPIGateway.Utilities.ReadFileInLocalStorage("GridSchematics_LiveBridge.txt", typeof(GridSchematicsSession));
                string content = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(content))
                    return false;

                int hash = content.GetHashCode();
                frame = ParseLiveBridgeFrame(content);
                if (frame == null || frame.Sprites.Count == 0)
                    return false;

                _panelLiveBridgeLastHash = hash;
                _panelLiveBridgeLastSpriteCount = frame.Sprites.Count;
                _panelLiveBridgeLastStatus = null;
                return true;
            }
            catch (Exception ex)
            {
                _panelLiveBridgeLastStatus = ex.Message;
                return false;
            }
            finally
            {
                if (reader != null)
                    reader.Dispose();
            }
        }

        static LiveBridgeFrame ParseLiveBridgeFrame(string content)
        {
            var frame = new LiveBridgeFrame();
            string[] lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0 || !lines[0].StartsWith("GSLIVEBRIDGE|", StringComparison.Ordinal))
                return null;

            for (int i = 1; i < lines.Length; i++)
            {
                string[] parts = lines[i].Split('|');
                if (parts.Length == 0)
                    continue;

                if (parts[0] == "SURFACE" && parts.Length >= 3)
                {
                    frame.Width = ParseBridgeFloat(parts[1], 512f);
                    frame.Height = ParseBridgeFloat(parts[2], 512f);
                    if (frame.Width <= 0f) frame.Width = 512f;
                    if (frame.Height <= 0f) frame.Height = 512f;
                    continue;
                }

                if (parts[0] != "S" || parts.Length < 14)
                    continue;

                LiveBridgeSprite sprite = new LiveBridgeSprite();
                sprite.IsText = parts[1] == "T";
                sprite.Data = DecodeBridgeString(parts[2]);
                sprite.X = ParseBridgeFloat(parts[3], 0f);
                sprite.Y = ParseBridgeFloat(parts[4], 0f);
                sprite.Width = ParseBridgeFloat(parts[5], 0f);
                sprite.Height = ParseBridgeFloat(parts[6], 0f);
                sprite.R = ParseBridgeByte(parts[7], 255);
                sprite.G = ParseBridgeByte(parts[8], 255);
                sprite.B = ParseBridgeByte(parts[9], 255);
                sprite.A = ParseBridgeByte(parts[10], 255);
                sprite.RotationOrScale = ParseBridgeFloat(parts[11], sprite.IsText ? 1f : 0f);
                sprite.FontId = DecodeBridgeString(parts[12]);
                sprite.Alignment = ParseBridgeAlignment(parts[13]);
                frame.Sprites.Add(sprite);
            }

            return frame;
        }

        void DrawLiveBridgeFrame(MySpriteDrawFrame frame, Vector2 targetSize, LiveBridgeFrame bridgeFrame)
        {
            float scaleX = targetSize.X / Math.Max(1f, bridgeFrame.Width);
            float scaleY = targetSize.Y / Math.Max(1f, bridgeFrame.Height);
            float textScale = Math.Min(scaleX, scaleY);

            for (int i = 0; i < bridgeFrame.Sprites.Count; i++)
            {
                LiveBridgeSprite sprite = bridgeFrame.Sprites[i];
                Color color = new Color(sprite.R, sprite.G, sprite.B, sprite.A);
                Vector2 position = new Vector2(sprite.X * scaleX, sprite.Y * scaleY);

                if (sprite.IsText)
                {
                    string fontId = !string.IsNullOrEmpty(_panelLiveBridgeForceFont) ? GridSchematicsConfig.GetUiFontSubtype(_panelLiveBridgeForceFont) : (string.IsNullOrEmpty(sprite.FontId) ? "White" : sprite.FontId);
                    AddLiveSprite(frame, new MySprite(SpriteType.TEXT, sprite.Data ?? string.Empty, position, null, color, fontId, sprite.Alignment, sprite.RotationOrScale * textScale), true, false);
                }
                else
                {
                    AddLiveSprite(frame, new MySprite(SpriteType.TEXTURE, string.IsNullOrEmpty(sprite.Data) ? "SquareSimple" : sprite.Data, position, new Vector2(sprite.Width * scaleX, sprite.Height * scaleY), color, null, TextAlignment.CENTER, sprite.RotationOrScale), true, true);
                }
            }
        }

        void DrawLiveBridgeMissing(MySpriteDrawFrame frame, Vector2 size, string font)
        {
            AddLiveText(frame, "WAITING FOR TOOL BRIDGE", new Vector2(size.X * 0.5f, size.Y * 0.42f), new Color(0, 220, 255, 230), font, GetLiveScale(size, 0.72f), TextAlignment.CENTER);
            string detail = string.IsNullOrEmpty(_panelLiveBridgeLastStatus) ? "enable Export To Game in the layout tool" : _panelLiveBridgeLastStatus;
            AddLiveText(frame, detail, new Vector2(size.X * 0.5f, size.Y * 0.52f), new Color(160, 230, 240, 210), font, GetLiveScale(size, 0.42f), TextAlignment.CENTER);
            if (_panelLiveBridgeLastSpriteCount > 0)
                AddLiveText(frame, "last frame " + _panelLiveBridgeLastSpriteCount + " sprites", new Vector2(size.X * 0.5f, size.Y * 0.60f), new Color(90, 150, 160, 190), font, GetLiveScale(size, 0.34f), TextAlignment.CENTER);
        }

        static string DecodeBridgeString(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            try
            {
                return Encoding.UTF8.GetString(Convert.FromBase64String(value));
            }
            catch
            {
                return string.Empty;
            }
        }

        static float ParseBridgeFloat(string value, float fallback)
        {
            float parsed;
            return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) ? parsed : fallback;
        }

        static byte ParseBridgeByte(string value, int fallback)
        {
            int parsed;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                parsed = fallback;
            if (parsed < 0) parsed = 0;
            if (parsed > 255) parsed = 255;
            return (byte)parsed;
        }

        static TextAlignment ParseBridgeAlignment(string value)
        {
            if (value == "L") return TextAlignment.LEFT;
            if (value == "R") return TextAlignment.RIGHT;
            return TextAlignment.CENTER;
        }
        static void DrawLiveMockupBackground(MySpriteDrawFrame frame, Vector2 size)
        {
            AddLiveRect(frame, size * 0.5f, size, new Color(2, 7, 9, 255));
            AddLiveRect(frame, new Vector2(size.X * 0.5f, 1.5f), new Vector2(size.X, 3f), new Color(0, 230, 255, 160));
            AddLiveRect(frame, new Vector2(size.X * 0.5f, size.Y - 1.5f), new Vector2(size.X, 3f), new Color(0, 230, 255, 110));
            AddLiveRect(frame, new Vector2(1.5f, size.Y * 0.5f), new Vector2(3f, size.Y), new Color(0, 230, 255, 110));
            AddLiveRect(frame, new Vector2(size.X - 1.5f, size.Y * 0.5f), new Vector2(3f, size.Y), new Color(0, 230, 255, 110));
        }

        static void DrawLiveMockupBare(MySpriteDrawFrame frame, Vector2 size, string font, int tick)
        {
            float pulse = GetLivePulse(tick);
            Color accent = new Color(0, (int)(180 + 60 * pulse), 255, 230);
            AddLiveText(frame, "GRID SCHEMATICS LIVE", new Vector2(size.X * 0.5f, size.Y * 0.16f), accent, font, GetLiveScale(size, 0.92f), TextAlignment.CENTER);
            AddLiveText(frame, "font " + font, new Vector2(size.X * 0.5f, size.Y * 0.24f), new Color(160, 230, 240, 210), font, GetLiveScale(size, 0.55f), TextAlignment.CENTER);
            DrawLiveMeter(frame, new Vector2(size.X * 0.14f, size.Y * 0.58f), new Vector2(size.X * 0.72f, size.Y * 0.055f), 0.74f, accent);
            DrawLiveMeter(frame, new Vector2(size.X * 0.14f, size.Y * 0.68f), new Vector2(size.X * 0.72f, size.Y * 0.055f), 0.42f + pulse * 0.18f, new Color(60, 255, 150, 225));
            DrawLiveMeter(frame, new Vector2(size.X * 0.14f, size.Y * 0.78f), new Vector2(size.X * 0.72f, size.Y * 0.055f), 0.18f, new Color(255, 190, 70, 225));
        }

        static void DrawLiveMockupCargo(MySpriteDrawFrame frame, Vector2 size, string font, int tick)
        {
            float scale = GetLiveScale(size, 1f);
            float pulse = GetLivePulse(tick);
            Color text = new Color(190, 240, 245, 235);
            Color muted = new Color(80, 135, 145, 200);
            Color cyan = new Color(0, 220, 255, 225);
            Color green = new Color(70, 255, 130, 230);
            Color amber = new Color(255, 180, 70, 230);

            float topHeight = Math.Max(26f, size.Y * 0.08f);
            AddLiveRect(frame, new Vector2(size.X * 0.5f, topHeight * 0.5f), new Vector2(size.X - 8f, topHeight), new Color(22, 31, 33, 230));
            AddLiveText(frame, "CARGO", new Vector2(size.X * 0.08f, topHeight * 0.58f), cyan, font, scale * 0.45f, TextAlignment.LEFT);
            AddLiveText(frame, "LIVE MOCKUP", new Vector2(size.X * 0.5f, topHeight * 0.58f), text, font, scale * 0.42f, TextAlignment.CENTER);
            AddLiveText(frame, "FONT " + font, new Vector2(size.X - 10f, topHeight * 0.58f), muted, font, scale * 0.34f, TextAlignment.RIGHT);

            float drawerTop = size.Y * 0.57f;
            AddLiveText(frame, "LOAD STATE", new Vector2(12f, drawerTop + 16f), text, font, scale * 0.34f, TextAlignment.LEFT);
            AddLiveText(frame, "CARGO MIX", new Vector2(size.X * 0.43f, drawerTop + 16f), text, font, scale * 0.34f, TextAlignment.LEFT);
            AddLiveText(frame, "BLOCK ACTIONS", new Vector2(size.X * 0.72f, drawerTop + 16f), text, font, scale * 0.34f, TextAlignment.LEFT);

            DrawLivePanel(frame, new Vector2(size.X * 0.21f, drawerTop + size.Y * 0.20f), new Vector2(size.X * 0.39f, size.Y * 0.32f), cyan);
            DrawLivePanel(frame, new Vector2(size.X * 0.54f, drawerTop + size.Y * 0.20f), new Vector2(size.X * 0.27f, size.Y * 0.32f), amber);
            DrawLivePanel(frame, new Vector2(size.X * 0.84f, drawerTop + size.Y * 0.20f), new Vector2(size.X * 0.24f, size.Y * 0.32f), muted);

            DrawLiveMeter(frame, new Vector2(18f, drawerTop + size.Y * 0.12f), new Vector2(size.X * 0.34f, size.Y * 0.035f), 0.77f, green);
            DrawLiveMeter(frame, new Vector2(18f, drawerTop + size.Y * 0.20f), new Vector2(size.X * 0.34f, size.Y * 0.035f), 0.38f + pulse * 0.08f, amber);

            float barX = size.X * 0.43f;
            DrawLiveMeter(frame, new Vector2(barX, drawerTop + size.Y * 0.11f), new Vector2(size.X * 0.23f, size.Y * 0.04f), 0.62f, amber);
            DrawLiveMeter(frame, new Vector2(barX, drawerTop + size.Y * 0.18f), new Vector2(size.X * 0.23f, size.Y * 0.04f), 0.24f, cyan);
            DrawLiveMeter(frame, new Vector2(barX, drawerTop + size.Y * 0.25f), new Vector2(size.X * 0.23f, size.Y * 0.04f), 0.08f, green);

            AddLiveRect(frame, new Vector2(size.X * 0.84f, drawerTop + size.Y * 0.18f), new Vector2(size.X * 0.08f, size.X * 0.08f), new Color(18, 22, 23, 255));
            AddLiveRect(frame, new Vector2(size.X * 0.84f, drawerTop + size.Y * 0.18f), new Vector2(size.X * 0.022f, size.X * 0.022f), new Color(170, 180, 180, 210));
            AddLiveText(frame, "NO ACTIONS", new Vector2(size.X * 0.84f, drawerTop + size.Y * 0.31f), muted, font, scale * 0.30f, TextAlignment.CENTER);
        }

        static void DrawLivePanel(MySpriteDrawFrame frame, Vector2 center, Vector2 size, Color color)
        {
            AddLiveRect(frame, center, size, new Color(0, 0, 0, 120));
            DrawLiveBorder(frame, center, size, color, 1f);
        }

        static void DrawLiveMeter(MySpriteDrawFrame frame, Vector2 topLeft, Vector2 size, float ratio, Color color)
        {
            ratio = MathHelper.Clamp(ratio, 0f, 1f);
            AddLiveRect(frame, topLeft + size * 0.5f, size, new Color(10, 18, 19, 230));
            AddLiveRect(frame, topLeft + new Vector2(size.X * ratio * 0.5f, size.Y * 0.5f), new Vector2(size.X * ratio, size.Y), color);
            DrawLiveBorder(frame, topLeft + size * 0.5f, size, new Color(color.R, color.G, color.B, 150), 1f);
        }

        static void DrawLiveBorder(MySpriteDrawFrame frame, Vector2 center, Vector2 size, Color color, float thickness)
        {
            float left = center.X - size.X * 0.5f;
            float right = center.X + size.X * 0.5f;
            float top = center.Y - size.Y * 0.5f;
            float bottom = center.Y + size.Y * 0.5f;
            AddLiveRect(frame, new Vector2(center.X, top), new Vector2(size.X, thickness), color);
            AddLiveRect(frame, new Vector2(center.X, bottom), new Vector2(size.X, thickness), color);
            AddLiveRect(frame, new Vector2(left, center.Y), new Vector2(thickness, size.Y), color);
            AddLiveRect(frame, new Vector2(right, center.Y), new Vector2(thickness, size.Y), color);
        }

        static void AddLiveRect(MySpriteDrawFrame frame, Vector2 center, Vector2 size, Color color)
        {
            AddLiveSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, size, color), true, true);
        }

        static void AddLiveText(MySpriteDrawFrame frame, string text, Vector2 position, Color color, string font, float scale, TextAlignment alignment)
        {
            AddLiveSprite(frame, new MySprite(SpriteType.TEXT, text ?? string.Empty, position, null, color, font, alignment, scale), true, false);
        }

        static void AddLiveSprite(MySpriteDrawFrame frame, MySprite sprite, bool snapPosition, bool snapSize)
        {
            bool rightAngleRotated;
            if (snapPosition && snapSize &&
                sprite.Type == SpriteType.TEXTURE &&
                sprite.Position.HasValue &&
                sprite.Size.HasValue &&
                TryGetLiveAxisAlignedTextureRotation(sprite, out rightAngleRotated))
            {
                Vector2 position = sprite.Position.Value;
                Vector2 size = sprite.Size.Value;
                float visualWidth = rightAngleRotated ? size.Y : size.X;
                float visualHeight = rightAngleRotated ? size.X : size.Y;
                float left = SnapLivePixel(position.X - visualWidth * 0.5f);
                float right = SnapLivePixel(position.X + visualWidth * 0.5f);
                float top = SnapLivePixel(position.Y - visualHeight * 0.5f);
                float bottom = SnapLivePixel(position.Y + visualHeight * 0.5f);
                float width = Math.Max(1f, SnapLivePixelSize(right - left));
                float height = Math.Max(1f, SnapLivePixelSize(bottom - top));

                sprite.Position = new Vector2((left + right) * 0.5f, (top + bottom) * 0.5f) + PanelLiveMockupViewportOrigin;
                sprite.Size = rightAngleRotated ? new Vector2(height, width) : new Vector2(width, height);
                frame.Add(sprite);
                return;
            }

            if (sprite.Position.HasValue)
            {
                Vector2 position = sprite.Position.Value;
                if (snapPosition)
                    position = SnapLivePoint(position);
                sprite.Position = position + PanelLiveMockupViewportOrigin;
            }

            if (snapSize && sprite.Size.HasValue)
            {
                Vector2 size = sprite.Size.Value;
                sprite.Size = new Vector2(SnapLivePixelSize(size.X), SnapLivePixelSize(size.Y));
            }

            frame.Add(sprite);
        }

        static bool TryGetLiveAxisAlignedTextureRotation(MySprite sprite, out bool rightAngleRotated)
        {
            rightAngleRotated = false;
            float rotation = sprite.RotationOrScale;
            while (rotation > Math.PI)
                rotation -= (float)(Math.PI * 2.0);
            while (rotation < -Math.PI)
                rotation += (float)(Math.PI * 2.0);

            if (Math.Abs(rotation) <= 0.0001f || Math.Abs(Math.Abs(rotation) - (float)Math.PI) <= 0.0001f)
                return true;

            if (Math.Abs(Math.Abs(rotation) - (float)(Math.PI * 0.5)) <= 0.0001f)
            {
                rightAngleRotated = true;
                return true;
            }

            return false;
        }

        static Vector2 GetPanelLiveMockupViewportOrigin(IMyTextSurface surface, Vector2 surfaceSize)
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
            if (x < 0f) x = 0f;
            if (y < 0f) y = 0f;
            return new Vector2(x, y);
        }

        static Vector2 SnapLivePoint(Vector2 value)
        {
            return new Vector2(SnapLivePixel(value.X), SnapLivePixel(value.Y));
        }

        static float SnapLivePixel(float value)
        {
            return (float)Math.Round(value);
        }

        static float SnapLivePixelSize(float value)
        {
            return Math.Max(1f, (float)Math.Round(value));
        }

        static float GetLiveScale(Vector2 surfaceSize, float multiplier)
        {
            float basis = Math.Min(surfaceSize.X, surfaceSize.Y);
            return MathHelper.Clamp(basis / 512f, 0.45f, 1.35f) * multiplier;
        }

        static float GetLivePulse(int tick)
        {
            return (float)((Math.Sin(tick * 0.055) + 1.0) * 0.5);
        }

        static void ShowPanelLiveMockupMessage(string text)
        {
            try
            {
                if (MyAPIGateway.Utilities == null)
                    return;

                MyAPIGateway.Utilities.ShowMessage("Grid Schematics", text);
                MyAPIGateway.Utilities.ShowNotification(text, 1800, "White");
            }
            catch
            {
            }
        }
    }
}










