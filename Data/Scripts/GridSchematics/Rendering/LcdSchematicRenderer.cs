using System;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRageMath;

namespace PingoPete.GridSchematics
{
    public partial class GridSchematicsSession
    {
        // Draws the current schematic preview to the active LCD surface.
        void DrawPreview()
        {
            if (_panel == null)
                return;

            var surface = _panel as IMyTextSurface;
            if (surface == null)
            {
                TryWriteTextOnly(BuildAsciiReport());
                return;
            }

            try
            {
                surface.ContentType = ContentType.SCRIPT;
                surface.Script = "";
                surface.ScriptBackgroundColor = Color.Black;

                Vector2 size = surface.SurfaceSize;
                Vector2 origin;
                float scale;
                CalculateDrawLayout(size, out origin, out scale);

                using (var frame = surface.DrawFrame())
                {
                    DrawBackground(frame, size);
                    DrawOccupiedCells(frame, origin, scale);
                    DrawMarchingSquareLines(frame, origin, scale);
                    DrawOverlayReport(frame);
                }
            }
            catch (Exception e)
            {
                _lastStatus = "Draw failed: " + e.Message;
                TryWriteTextOnly(BuildAsciiReport());
            }
        }

        void CalculateDrawLayout(Vector2 size, out Vector2 origin, out float scale)
        {
            float usableW = size.X - LCD_MARGIN * 2f;
            float usableH = size.Y - LCD_MARGIN * 2f;

            float scaleX = usableW / _activeRes;
            float scaleY = usableH / _activeRes;
            scale = Math.Min(scaleX, scaleY);

            float drawW = _activeRes * scale;
            float drawH = _activeRes * scale;

            origin = new Vector2(
                LCD_MARGIN + (usableW - drawW) * 0.5f,
                LCD_MARGIN + (usableH - drawH) * 0.5f
            );
        }

        void DrawBackground(MySpriteDrawFrame frame, Vector2 size)
        {
            frame.Add(new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                size * 0.5f,
                size,
                Color.Black
            ));
        }

        void DrawOccupiedCells(MySpriteDrawFrame frame, Vector2 origin, float scale)
        {
            Vector2 cellSize = new Vector2(Math.Max(1f, scale * 1.02f), Math.Max(1f, scale * 1.02f));

            for (int y = 0; y < _activeRes; y++)
            {
                for (int x = 0; x < _activeRes; x++)
                {
                    if (!_occupied[x, y])
                        continue;

                    byte c = (byte)MathHelper.Clamp((int)(GetCellIntensity(x, y) * 255f), 0, 255);
                    Vector2 pos = origin + new Vector2(
                        (x + 0.5f) * scale,
                        (_activeRes - y - 0.5f) * scale
                    );

                    frame.Add(new MySprite(
                        SpriteType.TEXTURE,
                        "SquareSimple",
                        pos,
                        cellSize,
                        new Color(c, c, c, c)
                    ));
                }
            }
        }

        float GetCellIntensity(int x, int y)
        {
            switch (_fillMode)
            {
                case FillMode.Solid:
                    return 0.55f;

                case FillMode.Thickness:
                    return ScaleMetricIntensity(_thickness[x, y], _maxThickness);

                case FillMode.Density:
                    return ScaleMetricIntensity(_density[x, y], _maxDensity);

                default:
                    return 1f;
            }
        }

        float ScaleMetricIntensity(float value, float maxValue)
        {
            float intensity = maxValue > 0.0001f ? (value / maxValue) : 0f;
            return 0.06f + intensity * 0.94f;
        }

        void DrawMarchingSquareLines(MySpriteDrawFrame frame, Vector2 origin, float scale)
        {
            float lineThickness = Math.Max(0.5f, scale * 0.22f);
            for (int i = 0; i < _msLines.Count; i++)
            {
                Vector2 a = MsToScreen(_msLines[i].A, origin, scale);
                Vector2 b = MsToScreen(_msLines[i].B, origin, scale);
                AddLine(frame, a, b, Color.White, lineThickness);
            }
        }

        void DrawOverlayReport(MySpriteDrawFrame frame)
        {
            frame.Add(new MySprite(
                SpriteType.TEXT,
                BuildOverlayReport(),
                new Vector2(4f, 4f),
                null,
                Color.Lime,
                "Debug",
                TextAlignment.LEFT,
                0.42f
            ));
        }

        string BuildOverlayReport()
        {
            return
                "SCHEMA\n" +
                _activeView + "\n" +
                _fillMode + "\n" +
                _activeRes + "\n" +
                "G:" + _constructGridCount + "\n" +
                "R:" + _lastRays + "\n" +
                "H:" + _lastHits + "\n" +
                "L:" + _lastMarchLines + "\n" +
                "S:" + _lastScanMs.ToString("0") + "ms\n" +
                "M:" + _lastMarchMs.ToString("0") + "ms";
        }

        Vector2 MsToScreen(Vector2 p, Vector2 origin, float scale)
        {
            return origin + new Vector2(p.X * scale, (_activeRes - p.Y) * scale);
        }

        void AddLine(MySpriteDrawFrame frame, Vector2 a, Vector2 b, Color color, float thickness)
        {
            Vector2 d = b - a;
            float len = d.Length();
            if (len < 0.001f)
                return;

            float rot = (float)Math.Atan2(d.Y, d.X);

            frame.Add(new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                (a + b) * 0.5f,
                new Vector2(len, thickness),
                color,
                null,
                TextAlignment.CENTER,
                rot
            ));
        }

        string BuildAsciiReport()
        {
            _text.Clear();
            _text.AppendLine("GRID SCHEMATICS");
            _text.AppendLine("View: " + _activeView);
            _text.AppendLine("Fill: " + _fillMode);
            _text.AppendLine("Res: " + _activeRes + "x" + _activeRes);
            _text.AppendLine("Construct Grids: " + _constructGridCount);
            _text.AppendLine("Rays: " + _lastRays);
            _text.AppendLine("Hits: " + _lastHits);
            _text.AppendLine("MS Lines: " + _lastMarchLines);
            _text.AppendLine("Scan: " + _lastScanMs.ToString("0.00") + " ms");
            _text.AppendLine("March: " + _lastMarchMs.ToString("0.00") + " ms");
            _text.AppendLine("Max Thickness: " + _maxThickness.ToString("0.00"));
            _text.AppendLine("Max Density: " + _maxDensity.ToString("0.00"));
            _text.AppendLine(_lastStatus);
            return _text.ToString();
        }

        void TryWriteTextOnly(string s)
        {
            try
            {
                if (_panel != null)
                {
                    _panel.ContentType = ContentType.TEXT_AND_IMAGE;
                    _panel.WriteText(s ?? "");
                }
            }
            catch
            {
            }
        }
    }
}
