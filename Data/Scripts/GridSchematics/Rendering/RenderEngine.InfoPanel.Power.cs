using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace GridSchematics
{
    public static partial class RenderEngine
    {
        static void DrawPowerInfoGlyph(MySpriteDrawFrame frame, ScreenZone zone, Color storage, Color effector, Color muted)
        {
            var metrics = UiLayout.BuildMetrics(zone.Width, zone.Height);
            float busY = zone.Y + zone.Height * 0.50f;
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(zone.X + zone.Width * 0.5f, busY), new Vector2(zone.Width * 0.70f, metrics.S(2f)), UiTextMuted));
            for (int i = 0; i < 3; i++)
            {
                float x = zone.X + zone.Width * (0.18f + i * 0.12f);
                DrawInfoBarGlyph(frame, new Vector2(x, busY - zone.Height * 0.20f), zone.Width * 0.08f, zone.Height * 0.22f, 0.42f + i * 0.18f, storage, muted);
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(x, busY - zone.Height * 0.06f), new Vector2(metrics.S(2f), zone.Height * 0.12f), storage));
            }
            for (int i = 0; i < 3; i++)
            {
                float x = zone.X + zone.Width * (0.60f + i * 0.12f);
                DrawInfoBarGlyph(frame, new Vector2(x, busY + zone.Height * 0.20f), zone.Width * 0.08f, zone.Height * 0.22f, 0.72f - i * 0.12f, effector, muted);
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(x, busY + zone.Height * 0.06f), new Vector2(metrics.S(2f), zone.Height * 0.12f), effector));
            }
        }
    }
}
