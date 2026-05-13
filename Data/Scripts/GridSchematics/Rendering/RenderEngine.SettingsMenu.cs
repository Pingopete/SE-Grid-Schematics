using System;
using VRage.Game.GUI.TextPanel;
using VRageMath;

namespace GridSchematics
{
    public static partial class RenderEngine
    {
        static bool IsSettingsHeaderRegion(string id)
        {
            return id == UiLayout.SettingsUiHeaderId ||
                id == UiLayout.SettingsStyleHeaderId ||
                id == UiLayout.SettingsRenderingHeaderId ||
                id == UiLayout.SettingsSchematicsHeaderId ||
                id == UiLayout.SettingsDebugHeaderId ||
                id == UiLayout.SettingsPanelDataHeaderId ||
                id == UiLayout.UiBlockerId;
        }

        static string FormatSettingsHeaderLabel(string label, int settingsExpandedMask, int category)
        {
            return label;
        }

        static void DrawSettingsHeader(MySpriteDrawFrame frame, HitRegion region, string label, bool hover, int settingsExpandedMask)
        {
            var center = SnapPoint(new Vector2(region.X + region.Width * 0.5f, region.Y + region.Height * 0.5f));
            var size = new Vector2(SnapPixelSize(region.Width), SnapPixelSize(region.Height));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, size, hover ? UiMenuButtonHover : new Color(UiMenuButtonHover.R, UiMenuButtonHover.G, UiMenuButtonHover.B, 220)));
            DrawScreenRectBorder(frame, center, size, hover ? UiAccentSoft : UiAccentDim);
            string icon = UiLayout.IsSettingsCategoryExpanded(settingsExpandedMask, GetSettingsCategoryForHeader(region.Id)) ? UiIconMinusTexture : UiIconPlusTexture;
            float iconSize = SnapPixelSize(Math.Min(region.Height * 0.56f, region.Width * 0.16f));
            float textX = region.X + region.Height * 0.86f;
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, icon, SnapPoint(new Vector2(region.X + region.Height * 0.46f, center.Y)), new Vector2(iconSize, iconSize), hover ? UiText : UiTextMuted));
            AddSprite(frame, new MySprite(
                SpriteType.TEXT,
                label,
                SnapPoint(new Vector2(textX, center.Y + ButtonTextBaselineOffset(region))),
                null,
                hover ? UiText : UiTextMuted,
                CurrentTextFontId,
                TextAlignment.LEFT,
                FitUiTextScale(label, ButtonTextScale(region), region.X + region.Width - textX - region.Height * 0.35f, CurrentTextFontId)
            ));
        }

        static void DrawSettingsRowButton(MySpriteDrawFrame frame, HitRegion region, string label, bool active, bool hover)
        {
            if (region.Id == UiLayout.ToggleHighResScanningId && active)
            {
                DrawHighRiskSettingsRowButton(frame, region, label, hover);
                return;
            }

            DrawCachedButtonBase(frame, region, label, active, hover, false);

            string left;
            string right;
            SplitSettingsRowLabel(label, out left, out right);

            var textColor = hover ? UiText : UiTextMuted;
            float padding = SnapPixelSize(Math.Max(5f, region.Height * 0.36f));
            float scale = ButtonTextScale(region) * 0.82f;
            float y = region.Y + region.Height * 0.5f + ButtonTextBaselineOffset(region);
            float leftAvailable = region.Width - padding * 2f;
            float rightAvailable = 0f;
            if (!string.IsNullOrEmpty(right))
            {
                rightAvailable = Math.Max(region.Width * 0.18f, Math.Min(region.Width * 0.40f, EstimateUiTextWidth(right, scale, CurrentTextFontId)));
                leftAvailable = region.Width - padding * 3f - rightAvailable;
            }

            AddSprite(frame, new MySprite(
                SpriteType.TEXT,
                left,
                SnapPoint(new Vector2(region.X + padding, y)),
                null,
                textColor,
                CurrentTextFontId,
                TextAlignment.LEFT,
                FitUiTextScale(left, scale, leftAvailable, CurrentTextFontId)
            ));

            if (!string.IsNullOrEmpty(right))
            {
                AddSprite(frame, new MySprite(
                    SpriteType.TEXT,
                    right,
                    SnapPoint(new Vector2(region.X + region.Width - padding, y)),
                    null,
                    textColor,
                    CurrentTextFontId,
                    TextAlignment.RIGHT,
                    FitUiTextScale(right, scale, rightAvailable, CurrentTextFontId)
                ));
            }
        }

        static void SplitSettingsRowLabel(string label, out string left, out string right)
        {
            left = label ?? string.Empty;
            right = null;
            SplitSettingsRowLabel(label, "BACKGROUND", "BACKGROUND:", out left, out right);
            if (right != null)
                return;
            SplitSettingsRowLabel(label, "THEME", "THEME:", out left, out right);
            if (right != null)
                return;
            SplitSettingsRowLabel(label, "FONT", "FONT:", out left, out right);
            if (right != null)
                return;
            SplitSettingsRowLabel(label, "UI BRIGHT", "UI BRIGHT:", out left, out right);
            if (right != null)
                return;
            SplitSettingsRowLabel(label, "PANEL", "PANEL:", out left, out right);
            if (right != null)
                return;
            SplitSettingsRowLabel(label, "MOUSE", "MOUSE:", out left, out right);
            if (right != null)
                return;
            SplitSettingsRowLabel(label, "ALLOW GRID ROTATION", "ALLOW GRID ROTATION:", out left, out right);
            if (right != null)
                return;
            SplitSettingsRowLabel(label, "RENDER SMOOTHING", "RENDER SMOOTHING:", out left, out right);
            if (right != null)
                return;
            SplitSettingsRowLabel(label, "PERFORMANCE MODE", "PERFORMANCE MODE:", out left, out right);
            if (right != null)
                return;
            SplitSettingsRowLabel(label, "HIGH RES SCANNING", "HIGH RES SCANNING:", out left, out right);
            if (right != null)
                return;
            SplitSettingsRowLabel(label, "ENABLE DEBUG", "ENABLE DEBUG:", out left, out right);
            if (right != null)
                return;
            SplitSettingsRowLabel(label, "HULL", "HULL:", out left, out right);
            if (right != null)
                return;
            SplitSettingsRowLabel(label, "SCHEMATIC", "SCHEMATIC:", out left, out right);
            if (right != null)
                return;
            SplitSettingsRowLabel(label, "STOR", "STORAGE:", out left, out right);
            if (right != null)
                return;
            SplitSettingsRowLabel(label, "EFF", "EFFECTOR:", out left, out right);
        }

        static void SplitSettingsRowLabel(string label, string prefix, string displayPrefix, out string left, out string right)
        {
            left = label ?? string.Empty;
            right = null;
            if (string.IsNullOrEmpty(label) || !label.StartsWith(prefix + " ", StringComparison.Ordinal))
                return;

            left = displayPrefix;
            right = label.Substring(prefix.Length + 1);
        }

        static int GetSettingsCategoryForHeader(string id)
        {
            if (id == UiLayout.SettingsStyleHeaderId)
                return UiLayout.SettingsCategoryStyle;
            if (id == UiLayout.SettingsUiHeaderId)
                return UiLayout.SettingsCategoryUserInterface;
            if (id == UiLayout.SettingsRenderingHeaderId)
                return UiLayout.SettingsCategoryRendering;
            if (id == UiLayout.SettingsSchematicsHeaderId)
                return UiLayout.SettingsCategorySchematics;
            if (id == UiLayout.SettingsDebugHeaderId)
                return UiLayout.SettingsCategoryDebug;
            if (id == UiLayout.SettingsPanelDataHeaderId)
                return UiLayout.SettingsCategoryPanelData;
            return 0;
        }

        static void DrawHighRiskSettingsRowButton(MySpriteDrawFrame frame, HitRegion region, string label, bool hover)
        {
            var center = SnapPoint(new Vector2(region.X + region.Width * 0.5f, region.Y + region.Height * 0.5f));
            var size = new Vector2(SnapPixelSize(region.Width), SnapPixelSize(region.Height));
            var fill = hover ? new Color(90, 18, 18, 220) : new Color(56, 10, 10, 190);
            var edge = hover ? new Color(255, 72, 72, 220) : new Color(190, 42, 42, 190);
            var text = hover ? new Color(255, 118, 118, 255) : new Color(235, 76, 76, 245);

            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, size, fill));
            DrawScreenRectBorder(frame, center, size, edge);

            string left;
            string right;
            SplitSettingsRowLabel(label, out left, out right);

            float padding = SnapPixelSize(Math.Max(5f, region.Height * 0.36f));
            float scale = ButtonTextScale(region) * 0.82f;
            float y = region.Y + region.Height * 0.5f + ButtonTextBaselineOffset(region);
            float leftAvailable = region.Width - padding * 2f;
            float rightAvailable = 0f;
            if (!string.IsNullOrEmpty(right))
            {
                rightAvailable = Math.Max(region.Width * 0.18f, Math.Min(region.Width * 0.40f, EstimateUiTextWidth(right, scale, CurrentTextFontId)));
                leftAvailable = region.Width - padding * 3f - rightAvailable;
            }

            AddSprite(frame, new MySprite(
                SpriteType.TEXT,
                left,
                SnapPoint(new Vector2(region.X + padding, y)),
                null,
                text,
                CurrentTextFontId,
                TextAlignment.LEFT,
                FitUiTextScale(left, scale, leftAvailable, CurrentTextFontId)
            ));

            if (!string.IsNullOrEmpty(right))
            {
                AddSprite(frame, new MySprite(
                    SpriteType.TEXT,
                    right,
                    SnapPoint(new Vector2(region.X + region.Width - padding, y)),
                    null,
                    text,
                    CurrentTextFontId,
                    TextAlignment.RIGHT,
                    FitUiTextScale(right, scale, rightAvailable, CurrentTextFontId)
                ));
            }
        }

        static string FormatUiSliderValue(float value)
        {
            return ((int)Math.Round(value * 100f)).ToString();
        }

        static float NormalizeRange(float value, float min, float max)
        {
            if (max <= min)
                return 0f;
            float ratio = (value - min) / (max - min);
            if (ratio < 0f)
                return 0f;
            if (ratio > 1f)
                return 1f;
            return ratio;
        }

        static void DrawStyleSlider(MySpriteDrawFrame frame, HitRegion region, string label, float ratio, bool hover)
        {
            if (ratio < 0f)
                ratio = 0f;
            if (ratio > 1f)
                ratio = 1f;

            DrawCachedButtonBase(frame, region, label, false, false, false);

            var textColor = hover ? UiText : UiTextMuted;
            float padding = SnapPixelSize(Math.Max(5f, region.Height * 0.36f));
            float scale = ButtonTextScale(region) * 0.82f;
            float y = region.Y + region.Height * 0.5f + ButtonTextBaselineOffset(region);
            float lineX;
            float lineWidth;
            UiLayout.GetSettingsHueSliderGeometry(region, out lineX, out lineWidth);
            float labelAvailable = lineX - (region.X + padding) - padding;
            AddSprite(frame, new MySprite(
                SpriteType.TEXT,
                label,
                SnapPoint(new Vector2(region.X + padding, y)),
                null,
                textColor,
                CurrentTextFontId,
                TextAlignment.LEFT,
                FitUiTextScale(label, scale, labelAvailable, CurrentTextFontId)
            ));
            float lineY = region.Y + region.Height * 0.5f;
            float lineThickness = SnapPixelSize(Math.Max(1f, region.Height * 0.08f));
            var lineCenter = SnapPoint(new Vector2(lineX + lineWidth * 0.5f, lineY));
            var lineColor = hover ? new Color(150, 150, 150, 220) : new Color(90, 90, 90, 150);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", lineCenter, new Vector2(SnapPixelSize(lineWidth), lineThickness), lineColor));

            float knobSize = SnapPixelSize(Math.Max(5f, region.Height * 0.42f));
            var knobCenter = SnapPoint(new Vector2(lineX + lineWidth * ratio, lineY));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", knobCenter, new Vector2(knobSize, knobSize), hover ? UiSelected : DimSliderKnobColor(UiSelected)));
        }

        static Color DimSliderKnobColor(Color color)
        {
            return new Color(
                (byte)Math.Round(color.R * 0.62f),
                (byte)Math.Round(color.G * 0.62f),
                (byte)Math.Round(color.B * 0.62f),
                (byte)Math.Round(color.A * 0.78f));
        }
    }
}
