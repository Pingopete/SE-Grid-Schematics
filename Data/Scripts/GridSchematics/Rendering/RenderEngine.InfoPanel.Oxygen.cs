using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace GridSchematics
{
    public static partial class RenderEngine
    {
        static void DrawOxygenInfoGlyph(MySpriteDrawFrame frame, ScreenZone zone, Color oxygen, Color muted)
        {
            var metrics = UiLayout.BuildMetrics(zone.Width, zone.Height);
            for (int i = 0; i < 3; i++)
            {
                float x = zone.X + zone.Width * (0.30f + i * 0.20f);
                float h = zone.Height * 0.42f;
                float w = zone.Width * 0.10f;
                var center = new Vector2(x, zone.Y + zone.Height * 0.56f);
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, new Vector2(w, h), muted));
                DrawInfoBarGlyph(frame, center + new Vector2(0f, h * 0.16f), w * 0.70f, h * 0.55f, 0.35f + i * 0.18f, oxygen, new Color(oxygen.R, oxygen.G, oxygen.B, 42));
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center + new Vector2(0f, -h * 0.58f), new Vector2(w * 0.62f, metrics.S(3f)), oxygen));
            }
        }
    }
}
