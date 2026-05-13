using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using System;
using System.Collections.Generic;

namespace GridSchematics
{
    public static partial class RenderEngine
    {
        static void DrawScanSymbolPanel(MySpriteDrawFrame frame, ScreenZone zone, GridSchematicsLcdApp app)
        {
            var metrics = UiLayout.BuildMetrics(zone.Width, zone.Height);
            float pad = Math.Max(metrics.S(8f), zone.Height * 0.10f);
            var top = app.ConstructCache != null ? app.ConstructCache.GetRaycastData(ScanView.Top) : null;
            var side = app.ConstructCache != null ? app.ConstructCache.GetRaycastData(ScanView.Side) : null;
            var front = app.ConstructCache != null ? app.ConstructCache.GetRaycastData(ScanView.Front) : null;
            float barWidth = Math.Max(metrics.S(24f), zone.Width * 0.22f);
            float barHeight = Math.Max(metrics.S(10f), zone.Height * 0.12f);
            float y = zone.Y + zone.Height * 0.30f;
            DrawScanAxisBar(frame, new Vector2(zone.X + zone.Width * 0.22f, y), barWidth, barHeight, "TOP", top);
            DrawScanAxisBar(frame, new Vector2(zone.X + zone.Width * 0.50f, y), barWidth, barHeight, "SIDE", side);
            DrawScanAxisBar(frame, new Vector2(zone.X + zone.Width * 0.78f, y), barWidth, barHeight, "FRONT", front);

            float sweepY = zone.Y + zone.Height * 0.58f;
            float left = zone.X + pad;
            float right = zone.X + zone.Width - pad;
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2((left + right) * 0.5f, sweepY), new Vector2(right - left, metrics.S(1.5f)), UiAccentDim));
            float sweepX = left + (right - left) * (((CacheUseCounter % 80) + 1) / 80f);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(sweepX, sweepY), new Vector2(metrics.S(5f), zone.Height * 0.22f), UiAccentBright));

            DrawInfoBarGlyph(frame, new Vector2(zone.X + zone.Width * 0.28f, zone.Y + zone.Height * 0.78f), zone.Width * 0.20f, zone.Height * 0.10f, ScanReadyRatio(top), UiAccentSoft, UiAccentGhost);
            DrawInfoBarGlyph(frame, new Vector2(zone.X + zone.Width * 0.50f, zone.Y + zone.Height * 0.78f), zone.Width * 0.20f, zone.Height * 0.10f, ScanReadyRatio(side), UiAccentSoft, UiAccentGhost);
            DrawInfoBarGlyph(frame, new Vector2(zone.X + zone.Width * 0.72f, zone.Y + zone.Height * 0.78f), zone.Width * 0.20f, zone.Height * 0.10f, ScanReadyRatio(front), UiAccentSoft, UiAccentGhost);
        }

        static void DrawScanAxisBar(MySpriteDrawFrame frame, Vector2 center, float width, float height, string label, RawRaycastScanData data)
        {
            var metrics = UiLayout.BuildMetrics((int)Math.Max(1f, width * 3f), (int)Math.Max(1f, height * 6f));
            DrawInfoBarGlyph(frame, center, width, height, ScanReadyRatio(data), UiAccentSoft, UiAccentGhost);
            AddSprite(frame, new MySprite(
                SpriteType.TEXT,
                label,
                new Vector2(center.X, center.Y - height * 0.5f - metrics.S(10f)),
                null,
                data != null && data.IsReady ? UiText : UiTextMuted,
                CurrentTextFontId,
                TextAlignment.CENTER,
                metrics.SmallText
            ));
        }

        static float ScanReadyRatio(RawRaycastScanData data)
        {
            if (data == null)
                return 0f;
            return data.IsReady ? 1f : 0.15f;
        }

        static void DrawScanDrawerControls(MySpriteDrawFrame frame, GridSchematicsLcdApp app)
        {
            if (app.Surface == null)
                return;

            var size = app.Surface.SurfaceSize;
            var regions = UiLayout.BuildInfoPanelScanRegions((int)size.X, (int)size.Y);
            for (int i = 0; i < regions.Length; i++)
            {
                var region = regions[i];
                bool active = false;
                if (region.Id == UiLayout.SetDensityId)
                    active = string.Equals(app.Config.FillMode, GridSchematicsConfig.FillDensity, StringComparison.OrdinalIgnoreCase);
                else if (region.Id == UiLayout.SetThicknessId)
                    active = string.Equals(app.Config.FillMode, GridSchematicsConfig.FillThickness, StringComparison.OrdinalIgnoreCase);
                else if (region.Id == UiLayout.SetVoidsId)
                    active = string.Equals(app.Config.FillMode, GridSchematicsConfig.FillVoids, StringComparison.OrdinalIgnoreCase);

                bool hover = string.Equals(app.TouchInput.HoverRegionId, region.Id, StringComparison.Ordinal);
                string label = region.Id == UiLayout.SetDensityId ? "DENSITY" :
                    region.Id == UiLayout.SetThicknessId ? "DEPTH" :
                    region.Id == UiLayout.SetVoidsId ? "VOIDS" :
                    region.Id == UiLayout.RunScanId ? "RUN SCAN" : region.Hint;
                DrawViewButton(frame, region, label, active, hover);
            }
        }

    }
}
