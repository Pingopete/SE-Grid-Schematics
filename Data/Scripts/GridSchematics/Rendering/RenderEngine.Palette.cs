using System;
using VRageMath;

namespace GridSchematics
{
    public static partial class RenderEngine
    {
        static Color UiAccent = new Color(44, 116, 124, 255);
        static Color UiAccentBright = new Color(90, 190, 205, 255);
        static Color UiAccentSoft = new Color(58, 138, 148, 205);
        static Color UiAccentDim = new Color(26, 58, 64, 190);
        static Color UiAccentFaint = new Color(16, 36, 42, 120);
        static Color UiAccentGhost = new Color(6, 18, 22, 145);
        static Color UiPanelFill = new Color(0, 3, 5, 255);
        static Color UiPanelFillSoft = new Color(1, 6, 8, 246);
        static Color UiMenuBarFill = new Color(4, 16, 20, 255);
        static Color UiMenuButtonFill = new Color(5, 20, 25, 248);
        static Color UiMenuButtonHover = new Color(14, 38, 45, 250);
        static Color UiMenuButtonActive = new Color(7, 24, 30, 252);
        static Color UiMenuDropdownFill = new Color(1, 7, 10, 252);
        static Color UiMenuSeparator = new Color(8, 16, 22, 150);
        static Color UiText = new Color(245, 248, 250, 255);
        static Color UiTextMuted = new Color(182, 195, 202, 225);
        static Color UiSelected = new Color(190, 160, 45, 230);
        static Color UiWarning = new Color(210, 80, 80, 220);
        static Color UiDebugGridMajor = new Color(58, 82, 96, 30);
        static Color UiDebugGridMinor = new Color(42, 62, 74, 16);
        static Color UiReferenceAxis = new Color(112, 210, 255, 35);
        static Color UiBlockGhost = new Color(210, 230, 240, 20);
        static Color UiFatBlockGhost = new Color(235, 248, 255, 28);

        public static void SetUiPalette(string palette)
        {
            SetUiPalette(palette, 0, 1f, 1f);
        }

        public static void SetUiPalette(string palette, int hueShift, float brightness, float alphaScale)
        {
            SetUiPalette(palette, hueShift, brightness, alphaScale, 0, 1f, 1f);
        }

        public static void SetUiPalette(string palette, int hueShift, float brightness, float alphaScale, int accentHueShift, float panelBrightness, float panelAlpha)
        {
            SetUiPalette(palette, hueShift, brightness, alphaScale, accentHueShift, panelBrightness, panelAlpha, 1f, 1f, 1f);
        }

        public static void SetUiPalette(string palette, int hueShift, float brightness, float alphaScale, int accentHueShift, float panelBrightness, float panelAlpha, float saturation, float accentBrightness, float accentSaturation)
        {
            if (string.IsNullOrEmpty(palette))
            {
                ApplyBluePalette();
            }
            else
            {
                string key = palette.ToUpperInvariant();
                if (key == "GREEN")
                    ApplyGreenPalette();
                else if (key == "AMBER")
                    ApplyAmberPalette();
                else if (key == "ICE")
                    ApplyIcePalette();
                else
                    ApplyBluePalette();
            }

            ApplyPaletteModifiers(hueShift, brightness, saturation, alphaScale);
            ApplyHighlightModifier(accentHueShift, accentBrightness, accentSaturation);
            ApplyPanelModifier(panelBrightness, panelAlpha);
        }

        static void ApplyBluePalette()
        {
            UiAccent = new Color(44, 116, 124, 255);
            UiAccentBright = new Color(90, 190, 205, 255);
            UiAccentSoft = new Color(58, 138, 148, 205);
            UiAccentDim = new Color(26, 58, 64, 190);
            UiAccentFaint = new Color(16, 36, 42, 120);
            UiAccentGhost = new Color(6, 18, 22, 145);
            UiPanelFill = new Color(0, 3, 5, 255);
            UiPanelFillSoft = new Color(1, 6, 8, 246);
            UiMenuBarFill = new Color(4, 16, 20, 255);
            UiMenuButtonFill = new Color(5, 20, 25, 248);
            UiMenuButtonHover = new Color(14, 38, 45, 250);
            UiMenuButtonActive = new Color(7, 24, 30, 252);
            UiMenuDropdownFill = new Color(1, 7, 10, 252);
            UiMenuSeparator = new Color(8, 16, 22, 150);
            UiText = new Color(245, 248, 250, 255);
            UiTextMuted = new Color(182, 195, 202, 225);
            UiSelected = new Color(190, 160, 45, 230);
            UiDebugGridMajor = new Color(58, 82, 96, 30);
            UiDebugGridMinor = new Color(42, 62, 74, 16);
            UiReferenceAxis = new Color(112, 210, 255, 35);
            UiBlockGhost = new Color(210, 230, 240, 20);
            UiFatBlockGhost = new Color(235, 248, 255, 28);
        }

        static void ApplyGreenPalette()
        {
            UiAccent = new Color(70, 210, 95, 255);
            UiAccentBright = new Color(120, 245, 130, 255);
            UiAccentSoft = new Color(70, 210, 95, 190);
            UiAccentDim = new Color(48, 72, 54, 180);
            UiAccentFaint = new Color(32, 72, 42, 115);
            UiAccentGhost = new Color(18, 38, 25, 130);
            UiPanelFill = new Color(12, 18, 15, 255);
            UiPanelFillSoft = new Color(18, 30, 22, 236);
            UiMenuBarFill = new Color(22, 38, 28, 255);
            UiMenuButtonFill = new Color(32, 54, 38, 242);
            UiMenuButtonHover = new Color(64, 98, 72, 250);
            UiMenuButtonActive = new Color(42, 72, 48, 252);
            UiMenuDropdownFill = new Color(24, 42, 30, 252);
            UiMenuSeparator = new Color(8, 18, 12, 150);
            UiText = new Color(245, 250, 246, 255);
            UiTextMuted = new Color(184, 202, 188, 225);
            UiSelected = new Color(190, 160, 45, 230);
            UiDebugGridMajor = new Color(58, 88, 66, 30);
            UiDebugGridMinor = new Color(42, 66, 48, 16);
            UiReferenceAxis = new Color(120, 245, 130, 35);
            UiBlockGhost = new Color(220, 240, 225, 20);
            UiFatBlockGhost = new Color(240, 255, 242, 28);
        }

        static void ApplyAmberPalette()
        {
            UiAccent = new Color(220, 160, 75, 255);
            UiAccentBright = new Color(255, 198, 105, 255);
            UiAccentSoft = new Color(220, 160, 75, 190);
            UiAccentDim = new Color(74, 60, 44, 180);
            UiAccentFaint = new Color(80, 54, 26, 115);
            UiAccentGhost = new Color(42, 30, 18, 130);
            UiPanelFill = new Color(18, 15, 12, 255);
            UiPanelFillSoft = new Color(32, 25, 18, 236);
            UiMenuBarFill = new Color(42, 32, 22, 255);
            UiMenuButtonFill = new Color(62, 45, 28, 242);
            UiMenuButtonHover = new Color(115, 82, 44, 250);
            UiMenuButtonActive = new Color(82, 58, 34, 252);
            UiMenuDropdownFill = new Color(48, 35, 22, 252);
            UiMenuSeparator = new Color(20, 12, 8, 150);
            UiText = new Color(252, 248, 240, 255);
            UiTextMuted = new Color(210, 196, 176, 225);
            UiSelected = new Color(190, 160, 45, 230);
            UiDebugGridMajor = new Color(96, 74, 48, 30);
            UiDebugGridMinor = new Color(72, 54, 36, 16);
            UiReferenceAxis = new Color(255, 198, 105, 35);
            UiBlockGhost = new Color(240, 230, 210, 20);
            UiFatBlockGhost = new Color(255, 248, 235, 28);
        }

        static void ApplyIcePalette()
        {
            UiAccent = new Color(130, 205, 235, 255);
            UiAccentBright = new Color(185, 235, 255, 255);
            UiAccentSoft = new Color(130, 205, 235, 190);
            UiAccentDim = new Color(58, 72, 82, 180);
            UiAccentFaint = new Color(42, 66, 78, 115);
            UiAccentGhost = new Color(22, 36, 44, 130);
            UiPanelFill = new Color(12, 18, 22, 255);
            UiPanelFillSoft = new Color(20, 32, 40, 236);
            UiMenuBarFill = new Color(26, 42, 52, 255);
            UiMenuButtonFill = new Color(38, 60, 72, 242);
            UiMenuButtonHover = new Color(82, 116, 132, 250);
            UiMenuButtonActive = new Color(52, 82, 96, 252);
            UiMenuDropdownFill = new Color(30, 48, 58, 252);
            UiMenuSeparator = new Color(10, 18, 24, 150);
            UiText = new Color(250, 252, 255, 255);
            UiTextMuted = new Color(196, 210, 218, 225);
            UiSelected = new Color(190, 160, 45, 230);
            UiDebugGridMajor = new Color(72, 96, 110, 30);
            UiDebugGridMinor = new Color(52, 72, 84, 16);
            UiReferenceAxis = new Color(185, 235, 255, 35);
            UiBlockGhost = new Color(225, 240, 248, 20);
            UiFatBlockGhost = new Color(248, 252, 255, 28);
        }

        static void ApplyPaletteModifiers(int hueShift, float brightness, float saturation, float alphaScale)
        {
            if (brightness < 0.1f)
                brightness = 0.1f;
            if (saturation < 0f)
                saturation = 0f;
            if (alphaScale < 0.05f)
                alphaScale = 0.05f;
            if (alphaScale > 1f)
                alphaScale = 1f;

            UiAccent = SetPaletteColorHue(UiAccent, hueShift, brightness, saturation, alphaScale);
            UiAccentBright = SetPaletteColorHue(UiAccentBright, hueShift, brightness, saturation, alphaScale);
            UiAccentSoft = SetPaletteColorHue(UiAccentSoft, hueShift, brightness, saturation, alphaScale);
            UiAccentDim = SetPaletteColorHue(UiAccentDim, hueShift, brightness, saturation, alphaScale);
            UiAccentFaint = SetPaletteColorHue(UiAccentFaint, hueShift, brightness, saturation, alphaScale);
            UiAccentGhost = SetPaletteColorHue(UiAccentGhost, hueShift, brightness, saturation, alphaScale);
            UiPanelFill = SetPaletteColorHue(UiPanelFill, hueShift, brightness, saturation, alphaScale);
            UiPanelFillSoft = SetPaletteColorHue(UiPanelFillSoft, hueShift, brightness, saturation, alphaScale);
            UiMenuBarFill = SetPaletteColorHue(UiMenuBarFill, hueShift, brightness, saturation, alphaScale);
            UiMenuButtonFill = SetPaletteColorHue(UiMenuButtonFill, hueShift, brightness, saturation, alphaScale);
            UiMenuButtonHover = SetPaletteColorHue(UiMenuButtonHover, hueShift, brightness, saturation, alphaScale);
            UiMenuButtonActive = SetPaletteColorHue(UiMenuButtonActive, hueShift, brightness, saturation, alphaScale);
            UiMenuDropdownFill = SetPaletteColorHue(UiMenuDropdownFill, hueShift, brightness, saturation, alphaScale);
            UiMenuSeparator = SetPaletteColorHue(UiMenuSeparator, hueShift, brightness, saturation, alphaScale);
            UiDebugGridMajor = SetPaletteColorHue(UiDebugGridMajor, hueShift, brightness, saturation, alphaScale);
            UiDebugGridMinor = SetPaletteColorHue(UiDebugGridMinor, hueShift, brightness, saturation, alphaScale);
            UiReferenceAxis = SetPaletteColorHue(UiReferenceAxis, hueShift, brightness, saturation, alphaScale);
            UiBlockGhost = SetPaletteColorHue(UiBlockGhost, hueShift, brightness, saturation, alphaScale);
            UiFatBlockGhost = SetPaletteColorHue(UiFatBlockGhost, hueShift, brightness, saturation, alphaScale);
        }

        static void ApplyHighlightModifier(int accentHueShift, float brightness, float saturation)
        {
            UiSelected = SetPaletteColorHue(UiSelected, accentHueShift, brightness, saturation, 1f);
        }

        static void ApplyPanelModifier(float panelBrightness, float panelAlpha)
        {
            UiPanelFill = AdjustPaletteColor(UiPanelFill, 0, panelBrightness, panelAlpha);
            UiPanelFillSoft = AdjustPaletteColor(UiPanelFillSoft, 0, panelBrightness, panelAlpha);
            UiMenuBarFill = AdjustPaletteColor(UiMenuBarFill, 0, panelBrightness, panelAlpha);
            UiMenuButtonFill = AdjustPaletteColor(UiMenuButtonFill, 0, panelBrightness, panelAlpha);
            UiMenuButtonHover = AdjustPaletteColor(UiMenuButtonHover, 0, panelBrightness, panelAlpha);
            UiMenuButtonActive = AdjustPaletteColor(UiMenuButtonActive, 0, panelBrightness, panelAlpha);
            UiMenuDropdownFill = AdjustPaletteColor(UiMenuDropdownFill, 0, panelBrightness, panelAlpha);
        }

        static void ApplyGridVisibilityLevel(int gridVisibilityLevel)
        {
            if (gridVisibilityLevel <= 1)
                return;

            UiDebugGridMajor = new Color(UiDebugGridMajor.R, UiDebugGridMajor.G, UiDebugGridMajor.B, ToByte(UiDebugGridMajor.A * 1.5f));
            UiDebugGridMinor = new Color(UiDebugGridMinor.R, UiDebugGridMinor.G, UiDebugGridMinor.B, ToByte(UiDebugGridMinor.A * 1.5f));
        }

        static Color AdjustPaletteColor(Color color, int hueShift, float brightness, float alphaScale)
        {
            return AdjustPaletteColor(color, hueShift, brightness, 1f, alphaScale);
        }

        static Color AdjustPaletteColor(Color color, int hueShift, float brightness, float saturationScale, float alphaScale)
        {
            float h;
            float s;
            float v;
            RgbToHsv(color.R / 255f, color.G / 255f, color.B / 255f, out h, out s, out v);
            h = (h + hueShift) % 360f;
            if (h < 0f)
                h += 360f;
            s *= saturationScale;
            if (s > 1f)
                s = 1f;
            if (s < 0f)
                s = 0f;
            v *= brightness;
            if (v > 1f)
                v = 1f;

            float r;
            float g;
            float b;
            HsvToRgb(h, s, v, out r, out g, out b);
            return new Color(ToByte(r * 255f), ToByte(g * 255f), ToByte(b * 255f), ToByte(color.A * alphaScale));
        }

        static Color SetPaletteColorHue(Color color, int hue, float brightness, float saturationScale, float alphaScale)
        {
            float h;
            float s;
            float v;
            RgbToHsv(color.R / 255f, color.G / 255f, color.B / 255f, out h, out s, out v);
            h = hue % 360f;
            if (h < 0f)
                h += 360f;
            s *= saturationScale;
            if (s > 1f)
                s = 1f;
            if (s < 0f)
                s = 0f;
            v *= brightness;
            if (v > 1f)
                v = 1f;
            if (v < 0f)
                v = 0f;

            float r;
            float g;
            float b;
            HsvToRgb(h, s, v, out r, out g, out b);
            return new Color(ToByte(r * 255f), ToByte(g * 255f), ToByte(b * 255f), ToByte(color.A * alphaScale));
        }

        static void RgbToHsv(float r, float g, float b, out float h, out float s, out float v)
        {
            float max = Math.Max(r, Math.Max(g, b));
            float min = Math.Min(r, Math.Min(g, b));
            float delta = max - min;
            v = max;
            s = max <= 0f ? 0f : delta / max;

            if (delta <= 0.0001f)
            {
                h = 0f;
                return;
            }

            if (max == r)
                h = 60f * (((g - b) / delta) % 6f);
            else if (max == g)
                h = 60f * (((b - r) / delta) + 2f);
            else
                h = 60f * (((r - g) / delta) + 4f);

            if (h < 0f)
                h += 360f;
        }

        static void HsvToRgb(float h, float s, float v, out float r, out float g, out float b)
        {
            float c = v * s;
            float x = c * (1f - Math.Abs((h / 60f) % 2f - 1f));
            float m = v - c;

            if (h < 60f)
            {
                r = c; g = x; b = 0f;
            }
            else if (h < 120f)
            {
                r = x; g = c; b = 0f;
            }
            else if (h < 180f)
            {
                r = 0f; g = c; b = x;
            }
            else if (h < 240f)
            {
                r = 0f; g = x; b = c;
            }
            else if (h < 300f)
            {
                r = x; g = 0f; b = c;
            }
            else
            {
                r = c; g = 0f; b = x;
            }

            r += m;
            g += m;
            b += m;
        }

        static byte ToByte(float value)
        {
            if (value < 0f)
                return 0;
            if (value > 255f)
                return 255;
            return (byte)Math.Round(value);
        }
    }
}
