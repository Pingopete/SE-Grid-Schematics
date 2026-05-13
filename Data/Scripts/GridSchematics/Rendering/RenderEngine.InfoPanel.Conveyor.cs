using System;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace GridSchematics
{
    public static partial class RenderEngine
    {
        static void DrawConveyorInfoGlyph(MySpriteDrawFrame frame, ScreenZone zone, Color conveyor, Color muted)
        {
            var metrics = UiLayout.BuildMetrics(zone.Width, zone.Height);
            Vector2 a = new Vector2(zone.X + zone.Width * 0.18f, zone.Y + zone.Height * 0.55f);
            Vector2 b = new Vector2(zone.X + zone.Width * 0.40f, zone.Y + zone.Height * 0.38f);
            Vector2 c = new Vector2(zone.X + zone.Width * 0.62f, zone.Y + zone.Height * 0.58f);
            Vector2 d = new Vector2(zone.X + zone.Width * 0.82f, zone.Y + zone.Height * 0.42f);
            DrawInfoConnector(frame, a, b, conveyor);
            DrawInfoConnector(frame, b, c, conveyor);
            DrawInfoConnector(frame, c, d, conveyor);
            DrawSmallStatusGlyph(frame, a, metrics.S(7f), conveyor);
            DrawSmallStatusGlyph(frame, b, metrics.S(7f), conveyor);
            DrawSmallStatusGlyph(frame, c, metrics.S(7f), conveyor);
            DrawSmallStatusGlyph(frame, d, metrics.S(7f), muted);
        }

        static void DrawInfoConnector(MySpriteDrawFrame frame, Vector2 start, Vector2 end, Color color)
        {
            var delta = end - start;
            float length = delta.Length();
            if (length <= 0.01f)
                return;

            float angle = (float)Math.Atan2(delta.Y, delta.X);
            var metrics = UiLayout.BuildMetrics((int)Math.Max(1f, length * 3f), (int)Math.Max(1f, length * 3f));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", (start + end) * 0.5f, new Vector2(length, metrics.S(2f)), color, null, TextAlignment.CENTER, angle));
        }
    }
}
