using System;
using Sandbox.ModAPI;

namespace GridSchematics
{
    public partial class GridSchematicsLcdApp
    {
        void ToggleSettingsCategory(int category)
        {
            Ui.ActiveSettingsActionId = null;
            ClearStyleSliderDrag();
            if (UiLayout.IsSettingsCategoryExpanded(Ui.SettingsExpandedMask, category))
                Ui.SettingsExpandedMask = 0;
            else
                Ui.SettingsExpandedMask = category;
            _renderDirty = true;
        }

        void ClearStyleSliderDrag()
        {
            _activeStyleSliderRegionId = string.Empty;
        }

        bool TrySetStyleSliderFromCursor(string regionId)
        {
            var surface = Surface;
            if (surface == null || TouchInput == null)
                return false;

            var regions = UiLayout.BuildMenuPanelRegions((int)surface.SurfaceSize.X, (int)surface.SurfaceSize.Y, Ui.ActiveMenu, Ui.SettingsExpandedMask);
            if (regions == null)
                return false;

            for (int i = 0; i < regions.Length; i++)
            {
                var region = regions[i];
                if (!string.Equals(region.Id, regionId, StringComparison.Ordinal))
                    continue;

                float lineX;
                float lineWidth;
                UiLayout.GetSettingsHueSliderGeometry(region, out lineX, out lineWidth);
                if (lineWidth <= 0.001f)
                    return false;

                float ratio = (TouchInput.CursorPosition.X - lineX) / lineWidth;
                if (ratio < 0f)
                    ratio = 0f;
                if (ratio > 1f)
                    ratio = 1f;

                ApplyStyleSliderRatio(regionId, ratio);
                return true;
            }

            return false;
        }

        void ApplyStyleSliderRatio(string regionId, float ratio)
        {
            if (ratio < 0f)
                ratio = 0f;
            if (ratio > 1f)
                ratio = 1f;

            if (regionId == UiLayout.AdjustPaletteHueId)
                Config.UiHueShift = (int)Math.Round(ratio * 359f);
            else if (regionId == UiLayout.AdjustPaletteBrightnessId)
                Config.UiBrightness = Lerp(0.45f, 1.65f, ratio);
            else if (regionId == UiLayout.AdjustPaletteSaturationId)
                Config.UiSaturation = Lerp(0f, 2f, ratio);
            else if (regionId == UiLayout.AdjustAccentHueId)
                Config.UiAccentHueShift = (int)Math.Round(ratio * 359f);
            else if (regionId == UiLayout.AdjustAccentBrightnessId)
                Config.UiAccentBrightness = Lerp(0.45f, 1.65f, ratio);
            else if (regionId == UiLayout.AdjustAccentSaturationId)
                Config.UiAccentSaturation = Lerp(0f, 2f, ratio);
            else if (regionId == UiLayout.AdjustSchematicMainHueId)
                Config.SchematicMainHue = (int)Math.Round(ratio * 359f);
            else if (regionId == UiLayout.AdjustSchematicSecondaryHueId)
                Config.SchematicSecondaryHue = (int)Math.Round(ratio * 359f);
            else if (regionId == UiLayout.AdjustConveyorHueId)
                Config.ConveyorHue = (int)Math.Round(ratio * 359f);
        }

        static float Lerp(float min, float max, float ratio)
        {
            return min + (max - min) * ratio;
        }

        bool IsStyleSliderRegion(string id)
        {
            return id == UiLayout.AdjustPaletteHueId ||
                id == UiLayout.AdjustPaletteBrightnessId ||
                id == UiLayout.AdjustPaletteSaturationId ||
                id == UiLayout.AdjustAccentHueId ||
                id == UiLayout.AdjustAccentBrightnessId ||
                id == UiLayout.AdjustAccentSaturationId ||
                id == UiLayout.AdjustSchematicMainHueId ||
                id == UiLayout.AdjustSchematicSecondaryHueId ||
                id == UiLayout.AdjustConveyorHueId;
        }

        bool UpdateStyleHueSliderDrag()
        {
            if (Ui.ActiveMenu != MenuPanel.Settings || TouchInput == null)
            {
                ClearStyleSliderDrag();
                return false;
            }

            if (!TouchInput.IsPressed)
            {
                ClearStyleSliderDrag();
                return false;
            }

            string hover = TouchInput.HoverRegionId ?? string.Empty;
            if (TouchInput.JustPressed)
                _activeStyleSliderRegionId = IsStyleSliderRegion(hover) ? hover : string.Empty;

            string activeSlider = _activeStyleSliderRegionId ?? string.Empty;
            bool updated = !string.IsNullOrEmpty(activeSlider) &&
                TouchInput.IsCursorOnScreen &&
                TrySetStyleSliderFromCursor(activeSlider);

            if (updated)
                _styleHueSliderDirty = true;
            return updated;
        }

        bool UpdateStyleSliderWheel()
        {
            if (Ui.ActiveMenu != MenuPanel.Settings || TouchInput == null || !TouchInput.IsAvailable || !TouchInput.IsCursorOnScreen || MyAPIGateway.Input == null)
                return false;

            string hover = TouchInput.HoverRegionId ?? string.Empty;
            if (!IsStyleSliderRegion(hover))
                return false;

            int wheelValue;
            try
            {
                wheelValue = MyAPIGateway.Input.MouseScrollWheelValue();
            }
            catch
            {
                return false;
            }

            if (!_hasLastMouseWheelValue)
            {
                _lastMouseWheelValue = wheelValue;
                _hasLastMouseWheelValue = true;
                return false;
            }

            int delta = wheelValue - _lastMouseWheelValue;
            _lastMouseWheelValue = wheelValue;
            if (delta == 0)
                return false;

            TouchInput.MarkScrollActive();
            AdjustStyleSliderByWheel(hover, delta > 0 ? 1 : -1);
            PersistPanelSettings();
            return true;
        }

        void AdjustStyleSliderByWheel(string regionId, int direction)
        {
            if (regionId == UiLayout.AdjustPaletteHueId)
                Config.UiHueShift = NormalizeHue(Config.UiHueShift + direction * 5);
            else if (regionId == UiLayout.AdjustPaletteBrightnessId)
                Config.UiBrightness = Clamp(Config.UiBrightness + direction * 0.05f, 0.45f, 1.65f);
            else if (regionId == UiLayout.AdjustPaletteSaturationId)
                Config.UiSaturation = Clamp(Config.UiSaturation + direction * 0.05f, 0f, 2f);
            else if (regionId == UiLayout.AdjustAccentHueId)
                Config.UiAccentHueShift = NormalizeHue(Config.UiAccentHueShift + direction * 5);
            else if (regionId == UiLayout.AdjustAccentBrightnessId)
                Config.UiAccentBrightness = Clamp(Config.UiAccentBrightness + direction * 0.05f, 0.45f, 1.65f);
            else if (regionId == UiLayout.AdjustAccentSaturationId)
                Config.UiAccentSaturation = Clamp(Config.UiAccentSaturation + direction * 0.05f, 0f, 2f);
            else if (regionId == UiLayout.AdjustSchematicMainHueId)
                Config.SchematicMainHue = NormalizeHue(Config.SchematicMainHue + direction * 5);
            else if (regionId == UiLayout.AdjustSchematicSecondaryHueId)
                Config.SchematicSecondaryHue = NormalizeHue(Config.SchematicSecondaryHue + direction * 5);
            else if (regionId == UiLayout.AdjustConveyorHueId)
                Config.ConveyorHue = NormalizeHue(Config.ConveyorHue + direction * 5);
        }

        bool TryResetSettingsItemFromSecondaryClick()
        {
            if (Ui.ActiveMenu != MenuPanel.Settings || TouchInput == null || !TouchInput.SecondaryJustPressed)
                return false;

            string hover = TouchInput.HoverRegionId ?? string.Empty;
            if (string.IsNullOrEmpty(hover) || !ResetSettingsItemToDefault(hover))
                return false;

            PersistPanelSettings();
            return true;
        }

        bool ResetSettingsItemToDefault(string id)
        {
            if (id == UiLayout.AdjustPaletteHueId)
                Config.UiHueShift = GridSchematicsConfig.DefaultUiMainColorHue;
            else if (id == UiLayout.AdjustPaletteBrightnessId)
                Config.UiBrightness = 1f;
            else if (id == UiLayout.AdjustPaletteSaturationId)
                Config.UiSaturation = 1f;
            else if (id == UiLayout.AdjustAccentHueId)
                Config.UiAccentHueShift = GridSchematicsConfig.DefaultUiHighlightColorHue;
            else if (id == UiLayout.AdjustAccentBrightnessId)
                Config.UiAccentBrightness = 1f;
            else if (id == UiLayout.AdjustAccentSaturationId)
                Config.UiAccentSaturation = 1f;
            else if (id == UiLayout.AdjustSchematicMainHueId)
                Config.SchematicMainHue = GridSchematicsConfig.DefaultSchematicMainColorHue;
            else if (id == UiLayout.AdjustSchematicSecondaryHueId)
                Config.SchematicSecondaryHue = GridSchematicsConfig.DefaultSchematicSecondaryColorHue;
            else if (id == UiLayout.AdjustConveyorHueId)
                Config.ConveyorHue = GridSchematicsConfig.DefaultConveyorColorHue;
            else if (id == UiLayout.CycleUiFontId)
                Config.UiFont = GridSchematicsConfig.DefaultUiFont;
            else if (id == UiLayout.ToggleMouseControlId)
            {
                Config.MouseControl = false;
                if (Session != null)
                    Session.SetConstructMouseControl(ConstructId, false);
            }
            else if (id == UiLayout.CycleMouseSensitivityId)
            {
                Config.MouseSensitivity = "MID";
                if (Session != null)
                    Session.SetConstructMouseSensitivity(ConstructId, Config.MouseSensitivity);
            }
            else if (id == UiLayout.ToggleGridRotationId)
                Config.AllowGridRotation = true;
            else if (id == UiLayout.ToggleDebugModeId)
                Config.ShowDebug = false;
            else if (id == UiLayout.TogglePerfStatsId)
                Config.ShowPerfStats = false;
            else if (id == UiLayout.ToggleHullScanId)
                Ui.ShowHullScan = true;
            else if (id == UiLayout.AdjustHullScanAlphaId)
                Config.HullScanAlpha = 1f;
            else if (id == UiLayout.AdjustSchematicAlphaId)
                Config.SchematicAlpha = 1f;
            else if (id == UiLayout.ToggleBlurId)
                Ui.BlurScanRender = true;
            else if (id == UiLayout.TogglePerformanceModeId)
                Config.PerformanceMode = false;
            else if (id == UiLayout.ToggleHighResScanningId)
                Config.HighResScanning = false;
            else if (id == UiLayout.ToggleFillBarsId)
                Ui.FillBarsVisibilityLevel = 2;
            else if (id == UiLayout.CycleStorageColorId)
                Config.StorageColor = "GREEN";
            else if (id == UiLayout.CycleEffectorColorId)
                Config.EffectorColor = "MAGENTA";
            else if (id == UiLayout.ToggleBlocksId)
                Ui.ShowDiscoveredBlocks = false;
            else if (id == UiLayout.ToggleGridId)
                Ui.GridVisibilityLevel = 1;
            else if (id == UiLayout.ToggleReferenceId)
                Ui.ShowReferenceLines = true;
            else if (id == UiLayout.ToggleAllConnectionsId)
                Ui.ShowAllConnections = false;
            else if (id == UiLayout.ToggleBlocksOccludeConveyorsId)
                Config.BlocksOccludeConveyors = true;
            else if (id == UiLayout.ToggleConnectedNetworksId)
                Config.ShowConnectedNetworks = false;
            else
                return false;

            Ui.ShowDebugGrid = Ui.GridVisibilityLevel != 0;
            return true;
        }

        static float Clamp(float value, float min, float max)
        {
            if (value < min)
                return min;
            if (value > max)
                return max;
            return value;
        }
    }
}
