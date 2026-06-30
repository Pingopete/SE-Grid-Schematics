using Sandbox.ModAPI;
using System;
using System.Globalization;
using System.Text;
using VRage.Game.ModAPI;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace GridSchematics
{
    public partial class GridSchematicsSession
    {
        const string PanelResolutionDebugCommand = "/GSRES";
        const int PanelResolutionDebugWatchIntervalTicks = 15;

        bool _panelResolutionDebugCommandRegistered;
        bool _panelResolutionDebugWatchEnabled;
        IMyCubeBlock _panelResolutionDebugBlock;
        IMyTextSurface _panelResolutionDebugSurface;
        int _panelResolutionDebugSurfaceIndex = -1;
        int _panelResolutionDebugLastWatchTick = -10000;

        void RegisterPanelResolutionDebugCommand()
        {
            if (_panelResolutionDebugCommandRegistered || MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.MessageEntered += OnPanelResolutionDebugMessageEntered;
            _panelResolutionDebugCommandRegistered = true;
        }

        void UnregisterPanelResolutionDebugCommand()
        {
            if (!_panelResolutionDebugCommandRegistered || MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.MessageEntered -= OnPanelResolutionDebugMessageEntered;
            _panelResolutionDebugCommandRegistered = false;
        }

        void OnPanelResolutionDebugMessageEntered(string messageText, ref bool sendToOthers)
        {
            string text = messageText == null ? string.Empty : messageText.Trim();
            string[] parts = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || !IsPanelResolutionDebugCommand(parts[0]))
                return;

            sendToOthers = false;
            string action = parts.Length > 1 ? parts[1].ToUpperInvariant() : "READ";

            if (action == "HELP")
            {
                ShowPanelResolutionDebugMessage("/GSRES READ, /GSRES WATCH, /GSRES OFF, /GSRES SET 1024 [width height], /GSRES BUMP [factor]. Aim at an LCD surface first.");
                return;
            }

            if (action == "OFF" || action == "STOP")
            {
                ClearPanelResolutionDebug();
                ShowPanelResolutionDebugMessage("Resolution watch stopped.");
                return;
            }

            if (action == "WATCH" || action == "ON")
            {
                if (!CaptureAimedPanelResolutionDebugTarget())
                {
                    ShowPanelResolutionDebugMessage("Aim at an LCD surface, then run /GSRES WATCH.");
                    return;
                }

                _panelResolutionDebugWatchEnabled = true;
                _panelResolutionDebugLastWatchTick = -10000;
                ShowPanelResolutionDebugMessage("Resolution watch: " + BuildPanelResolutionDebugReport(_panelResolutionDebugBlock, _panelResolutionDebugSurface, _panelResolutionDebugSurfaceIndex));
                return;
            }

            if (action == "READ" || action == "INFO")
            {
                IMyCubeBlock block;
                IMyTextSurface surface;
                int surfaceIndex;
                if (!TryFindAimedPanelResolutionDebugSurface(out block, out surface, out surfaceIndex))
                {
                    ShowPanelResolutionDebugMessage("Aim at an LCD surface, then run /GSRES READ.");
                    return;
                }

                ShowPanelResolutionDebugMessage(BuildPanelResolutionDebugReport(block, surface, surfaceIndex));
                return;
            }

            if (action == "BUMP")
            {
                HandlePanelResolutionDebugBump(parts);
                return;
            }

            int resolution;
            int resolutionPartIndex = action == "SET" ? 2 : 1;
            if (!TryParsePanelResolutionDebugInt(parts, resolutionPartIndex, out resolution))
            {
                ShowPanelResolutionDebugMessage("Bad resolution. Example: /GSRES SET 1024 2 1");
                return;
            }

            float screenWidth = 0f;
            float screenHeight = 0f;
            TryParsePanelResolutionDebugFloat(parts, resolutionPartIndex + 1, out screenWidth);
            TryParsePanelResolutionDebugFloat(parts, resolutionPartIndex + 2, out screenHeight);
            ApplyPanelResolutionDebugSet(resolution, screenWidth, screenHeight);
        }

        static bool IsPanelResolutionDebugCommand(string command)
        {
            return command.Equals(PanelResolutionDebugCommand, StringComparison.OrdinalIgnoreCase) ||
                command.Equals("\\GSRES", StringComparison.OrdinalIgnoreCase) ||
                command.Equals("/GSLCDRES", StringComparison.OrdinalIgnoreCase) ||
                command.Equals("\\GSLCDRES", StringComparison.OrdinalIgnoreCase);
        }

        void UpdatePanelResolutionDebug(int tick)
        {
            if (!_panelResolutionDebugWatchEnabled)
                return;

            if (tick - _panelResolutionDebugLastWatchTick < PanelResolutionDebugWatchIntervalTicks)
                return;

            _panelResolutionDebugLastWatchTick = tick;
            if (_panelResolutionDebugBlock == null ||
                _panelResolutionDebugBlock.MarkedForClose ||
                !_panelResolutionDebugBlock.IsFunctional ||
                _panelResolutionDebugSurface == null)
            {
                ClearPanelResolutionDebug();
                return;
            }

            ShowPanelResolutionDebugNotification(BuildPanelResolutionDebugReport(_panelResolutionDebugBlock, _panelResolutionDebugSurface, _panelResolutionDebugSurfaceIndex));
        }

        void DrawPanelResolutionDebug()
        {
            if (!_panelResolutionDebugWatchEnabled || _panelResolutionDebugBlock == null)
                return;

            GridSchematicsLcdApp app;
            PanelCursorWorldDrawData cursor;
            if (TryFindPanelSurfaceCursorApp(_panelResolutionDebugBlock, out app, out cursor) && app != null)
            {
                Vector3D topLeft = cursor.PlaneHit + cursor.AxisA * (cursor.MinA - cursor.LocalA) + cursor.AxisB * (cursor.MaxB - cursor.LocalB);
                Vector3D topRight = cursor.PlaneHit + cursor.AxisA * (cursor.MaxA - cursor.LocalA) + cursor.AxisB * (cursor.MaxB - cursor.LocalB);
                Vector3D bottomRight = cursor.PlaneHit + cursor.AxisA * (cursor.MaxA - cursor.LocalA) + cursor.AxisB * (cursor.MinB - cursor.LocalB);
                Vector3D bottomLeft = cursor.PlaneHit + cursor.AxisA * (cursor.MinA - cursor.LocalA) + cursor.AxisB * (cursor.MinB - cursor.LocalB);
                DrawPanelCursorSurfaceDebugEdge(topLeft, topRight, bottomRight, bottomLeft, new Color(75, 215, 255, 245), 0.008f);
            }
        }

        void ClearPanelResolutionDebug()
        {
            _panelResolutionDebugWatchEnabled = false;
            _panelResolutionDebugBlock = null;
            _panelResolutionDebugSurface = null;
            _panelResolutionDebugSurfaceIndex = -1;
        }

        bool CaptureAimedPanelResolutionDebugTarget()
        {
            IMyCubeBlock block;
            IMyTextSurface surface;
            int surfaceIndex;
            if (!TryFindAimedPanelResolutionDebugSurface(out block, out surface, out surfaceIndex))
                return false;

            _panelResolutionDebugBlock = block;
            _panelResolutionDebugSurface = surface;
            _panelResolutionDebugSurfaceIndex = surfaceIndex;
            return true;
        }

        bool TryFindAimedPanelResolutionDebugSurface(out IMyCubeBlock block, out IMyTextSurface surface, out int surfaceIndex)
        {
            block = null;
            surface = null;
            surfaceIndex = -1;

            if (TryFindAimedPanelLiveMockupSurface(out block, out surface, out surfaceIndex))
                return block != null && surface != null;

            return false;
        }

        void HandlePanelResolutionDebugBump(string[] parts)
        {
            IMyCubeBlock block;
            IMyTextSurface surface;
            int surfaceIndex;
            if (!TryFindAimedPanelResolutionDebugSurface(out block, out surface, out surfaceIndex))
            {
                ShowPanelResolutionDebugMessage("Aim at an LCD surface, then run /GSRES BUMP.");
                return;
            }

            float factor = 2f;
            TryParsePanelResolutionDebugFloat(parts, 2, out factor);
            if (factor <= 0f)
                factor = 2f;

            Vector2 textureSize = TryReadPanelResolutionDebugTextureSize(surface);
            int current = (int)Math.Max(textureSize.X, textureSize.Y);
            if (current <= 0)
            {
                Vector2 surfaceSize = surface != null ? surface.SurfaceSize : Vector2.Zero;
                current = (int)Math.Max(surfaceSize.X, surfaceSize.Y);
            }

            if (current <= 0)
                current = 512;

            ApplyPanelResolutionDebugSet((int)Math.Round(current * factor), 0f, 0f);
        }

        void ApplyPanelResolutionDebugSet(int resolution, float screenWidth, float screenHeight)
        {
            if (resolution < 64)
                resolution = 64;
            if (resolution > 8192)
                resolution = 8192;

            IMyCubeBlock block;
            IMyTextSurface surface;
            int surfaceIndex;
            if (!TryFindAimedPanelResolutionDebugSurface(out block, out surface, out surfaceIndex))
            {
                ShowPanelResolutionDebugMessage("Aim at an LCD surface, then run /GSRES SET.");
                return;
            }

            Vector2 beforeSurface = surface != null ? surface.SurfaceSize : Vector2.Zero;
            Vector2 beforeTexture = TryReadPanelResolutionDebugTextureSize(surface);

            string definitionResult;
            bool definitionChanged = TryMutatePanelResolutionDefinition(block, surfaceIndex, resolution, screenWidth, screenHeight, out definitionResult);

            string renderResult;
            bool renderChanged = TryInvokePanelResolutionRenderResize(block, surfaceIndex, resolution, out renderResult);

            Vector2 afterSurface = surface != null ? surface.SurfaceSize : Vector2.Zero;
            Vector2 afterTexture = TryReadPanelResolutionDebugTextureSize(surface);

            _panelResolutionDebugBlock = block;
            _panelResolutionDebugSurface = surface;
            _panelResolutionDebugSurfaceIndex = surfaceIndex;
            _panelResolutionDebugWatchEnabled = true;

            var sb = new StringBuilder();
            sb.Append("SET ");
            sb.Append(resolution.ToString(CultureInfo.InvariantCulture));
            sb.Append(" def=");
            sb.Append(definitionChanged ? "yes" : "no");
            sb.Append(" render=");
            sb.Append(renderChanged ? "yes" : "no");
            sb.Append(" | ");
            sb.Append(FormatPanelResolutionVector(beforeSurface));
            sb.Append("/");
            sb.Append(FormatPanelResolutionVector(beforeTexture));
            sb.Append(" -> ");
            sb.Append(FormatPanelResolutionVector(afterSurface));
            sb.Append("/");
            sb.Append(FormatPanelResolutionVector(afterTexture));
            sb.Append(" | ");
            sb.Append(definitionResult);
            sb.Append(" | ");
            sb.Append(renderResult);
            ShowPanelResolutionDebugMessage(sb.ToString());
        }

        static string BuildPanelResolutionDebugReport(IMyCubeBlock block, IMyTextSurface surface, int surfaceIndex)
        {
            Vector2 surfaceSize = surface != null ? surface.SurfaceSize : Vector2.Zero;
            Vector2 textureSize = TryReadPanelResolutionDebugTextureSize(surface);
            int rediscovered = (int)Math.Max(textureSize.X, textureSize.Y);
            if (rediscovered <= 0)
                rediscovered = (int)Math.Max(surfaceSize.X, surfaceSize.Y);

            string name = "LCD";
            var terminal = block as Sandbox.ModAPI.IMyTerminalBlock;
            if (terminal != null && !string.IsNullOrEmpty(terminal.CustomName))
                name = terminal.CustomName;

            string definition = GetBlockDefinitionId(block);
            return name +
                " s" + surfaceIndex.ToString(CultureInfo.InvariantCulture) +
                " surf=" + FormatPanelResolutionVector(surfaceSize) +
                " tex=" + FormatPanelResolutionVector(textureSize) +
                " minRediscovered=" + rediscovered.ToString(CultureInfo.InvariantCulture) +
                " def=" + definition;
        }

        static Vector2 TryReadPanelResolutionDebugTextureSize(IMyTextSurface surface)
        {
            if (surface == null)
                return Vector2.Zero;

            try
            {
                return surface.TextureSize;
            }
            catch
            {
                return Vector2.Zero;
            }
        }

        static string FormatPanelResolutionVector(Vector2 value)
        {
            return ((int)Math.Round(value.X)).ToString(CultureInfo.InvariantCulture) +
                "x" +
                ((int)Math.Round(value.Y)).ToString(CultureInfo.InvariantCulture);
        }

        static bool TryParsePanelResolutionDebugInt(string[] parts, int index, out int value)
        {
            value = 0;
            if (parts == null || index < 0 || index >= parts.Length)
                return false;

            return int.TryParse(parts[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
        }

        static bool TryParsePanelResolutionDebugFloat(string[] parts, int index, out float value)
        {
            value = 0f;
            if (parts == null || index < 0 || index >= parts.Length)
                return false;

            return float.TryParse(parts[index], NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        static bool TryMutatePanelResolutionDefinition(IMyCubeBlock block, int surfaceIndex, int resolution, float screenWidth, float screenHeight, out string detail)
        {
            detail = "runtime definition mutation is not exposed through whitelisted ModAPI";
            return false;
        }

        static bool TryInvokePanelResolutionRenderResize(IMyCubeBlock block, int surfaceIndex, int resolution, out string detail)
        {
            detail = "internal render resize requires reflection/plugin access; skipped for whitelist safety";
            return false;
        }

        static void ShowPanelResolutionDebugMessage(string text)
        {
            try
            {
                if (MyAPIGateway.Utilities == null)
                    return;

                MyAPIGateway.Utilities.ShowMessage("Grid Schematics", text);
                MyAPIGateway.Utilities.ShowNotification(text, 2200, "White");
            }
            catch
            {
            }
        }

        static void ShowPanelResolutionDebugNotification(string text)
        {
            try
            {
                if (MyAPIGateway.Utilities != null)
                    MyAPIGateway.Utilities.ShowNotification(text, 350, "White");
            }
            catch
            {
            }
        }
    }
}




