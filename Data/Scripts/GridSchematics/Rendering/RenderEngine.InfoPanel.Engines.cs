using System;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace GridSchematics
{
    public static partial class RenderEngine
    {
        static void DrawEngineInfoGlyph(MySpriteDrawFrame frame, ScreenZone zone, Color effector, Color muted)
        {
            var center = new Vector2(zone.X + zone.Width * 0.5f, zone.Y + zone.Height * 0.55f);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, new Vector2(zone.Width * 0.22f, zone.Height * 0.16f), muted));
            DrawThrustBarGlyph(frame, center + new Vector2(0f, -zone.Height * 0.24f), new Vector2(0f, -1f), zone.Height * 0.20f, effector);
            DrawThrustBarGlyph(frame, center + new Vector2(0f, zone.Height * 0.24f), new Vector2(0f, 1f), zone.Height * 0.20f, effector);
            DrawThrustBarGlyph(frame, center + new Vector2(-zone.Width * 0.26f, 0f), new Vector2(-1f, 0f), zone.Width * 0.18f, effector);
            DrawThrustBarGlyph(frame, center + new Vector2(zone.Width * 0.26f, 0f), new Vector2(1f, 0f), zone.Width * 0.18f, effector);
        }

        static void DrawThrustBarGlyph(MySpriteDrawFrame frame, Vector2 start, Vector2 dir, float length, Color color)
        {
            var metrics = UiLayout.BuildMetrics((int)Math.Max(1f, length * 4f), (int)Math.Max(1f, length * 4f));
            bool vertical = Math.Abs(dir.Y) > Math.Abs(dir.X);
            var size = vertical ? new Vector2(metrics.S(3f), length) : new Vector2(length, metrics.S(3f));
            var center = start + dir * (length * 0.5f);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, size, color));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", start + dir * length, new Vector2(metrics.S(8f), metrics.S(8f)), color));
        }
    }
}
