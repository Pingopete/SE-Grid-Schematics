using Sandbox.ModAPI;
using Sandbox.Game;
using Sandbox.Game.Entities;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PingoPete.GridSchematics
{
    public partial class GridSchematicsSession
    {
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

                float usableW = size.X - LCD_MARGIN * 2f;
                float usableH = size.Y - LCD_MARGIN * 2f;

                float scaleX = usableW / _activeRes;
                float scaleY = usableH / _activeRes;
                float scale = Math.Min(scaleX, scaleY);

                float drawW = _activeRes * scale;
                float drawH = _activeRes * scale;

                Vector2 origin = new Vector2(
                    LCD_MARGIN + (usableW - drawW) * 0.5f,
                    LCD_MARGIN + (usableH - drawH) * 0.5f
                );

                using (var frame = surface.DrawFrame())
                {
                    frame.Add(new MySprite(
                        SpriteType.TEXTURE,
                        "SquareSimple",
                        size * 0.5f,
                        size,
                        Color.Black
                    ));

                    for (int y = 0; y < _activeRes; y++)
                    {
                        for (int x = 0; x < _activeRes; x++)
                        {
                            if (!_occupied[x, y])
                                continue;

                            float intensity = 1f;

                            switch (_fillMode)
                            {
                                case FillMode.Solid:
                                    intensity = 0.55f;
                                    break;

                                case FillMode.Thickness:
                                    intensity = _maxThickness > 0.0001f ? (_thickness[x, y] / _maxThickness) : 0f;
                                    intensity = 0.06f + intensity * 0.94f;
                                    break;

                                case FillMode.Density:
                                    intensity = _maxDensity > 0.0001f ? (_density[x, y] / _maxDensity) : 0f;
                                    intensity = 0.06f + intensity * 0.94f;
                                    break;
                            }

                            byte c = (byte)MathHelper.Clamp((int)(intensity * 255f), 0, 255);

                            Vector2 pos = origin + new Vector2(
                                (x + 0.5f) * scale,
                                (_activeRes - y - 0.5f) * scale
                            );

                            frame.Add(new MySprite(
                                SpriteType.TEXTURE,
                                "SquareSimple",
                                pos,
                                new Vector2(Math.Max(1f, scale * 1.02f), Math.Max(1f, scale * 1.02f)),
                                new Color(c, c, c, c)
                            ));
                        }
                    }

                    float lineThickness = Math.Max(0.5f, scale * 0.22f);
                    for (int i = 0; i < _msLines.Count; i++)
                    {
                        Vector2 a = MsToScreen(_msLines[i].A, origin, scale);
                        Vector2 b = MsToScreen(_msLines[i].B, origin, scale);
                        AddLine(frame, a, b, Color.White, lineThickness);
                    }

                    string report =
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

                    frame.Add(new MySprite(
                        SpriteType.TEXT,
                        report,
                        new Vector2(4f, 4f),
                        null,
                        Color.Lime,
                        "Debug",
                        TextAlignment.LEFT,
                        0.42f
                    ));
                }
            }
            catch (Exception e)
            {
                _lastStatus = "Draw failed: " + e.Message;
                TryWriteTextOnly(BuildAsciiReport());
            }
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
