using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using VRage.Game.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using System;
using System.Collections.Generic;

namespace GridSchematics
{
    public static partial class RenderEngine
    {
        static ScreenZone BuildInfoPanelZone(ScreenZone center)
        {
            return UiLayout.BuildInfoPanelZone(center, true);
        }

        static ScreenZone BuildInfoPanelZone(ScreenZone center, bool fullPanel)
        {
            return UiLayout.BuildInfoPanelZone(center, fullPanel);
        }

        static ScreenZone BuildInfoPanelZone(ScreenZone center, GridSchematicsLcdApp app, bool fullPanel)
        {
            if (app != null && app.Ui != null && app.Ui.InfoPanelMode == InfoPanelMode.Systems && app.Ui.ActiveOverlay == OverlayMode.Cargo)
                return UiLayout.BuildCargoInfoPanelZone(center, fullPanel);
            return UiLayout.BuildInfoPanelZone(center, fullPanel);
        }

        static ScreenZone BuildInfoMapZone(ScreenZone center, ScreenZone infoPanel)
        {
            int mapHeight = Math.Max(1, infoPanel.Y - center.Y);
            return new ScreenZone(
                ScreenZoneType.CenterViewport,
                center.X,
                center.Y,
                center.Width,
                mapHeight);
        }

        static UiMetrics BuildInfoPanelContentMetrics(ScreenZone zone)
        {
            return UiLayout.BuildMetrics(zone.Width, zone.Height);
        }

        static UiMetrics BuildInfoPanelHeaderMetrics(ScreenZone zone, GridSchematicsLcdApp app)
        {
            if (app != null && app.Surface != null)
            {
                int screenWidth = (int)app.Surface.SurfaceSize.X;
                int screenHeight = (int)app.Surface.SurfaceSize.Y;
                if (app.Ui != null && app.Ui.ActiveOverlay == OverlayMode.Cargo && screenWidth == 1024 && screenHeight == 1024)
                    return UiLayout.BuildMetrics(screenWidth, screenHeight);
                return UiLayout.BuildChromeMetrics(screenWidth, screenHeight);
            }
            return zone.Width == 1024 && zone.Height == 368 ? UiLayout.BuildMetrics(1024, 1024) : UiLayout.BuildChromeMetrics(zone.Width, 512);
        }

        static void DrawSystemsInfoPanel(MySpriteDrawFrame frame, ScreenZone zone, GridSchematicsLcdApp app, bool isCursorOnlyRender)
        {
            if (zone.Width <= 0 || zone.Height <= 0)
                return;

            var metrics = BuildInfoPanelContentMetrics(zone);
            var headerMetrics = BuildInfoPanelHeaderMetrics(zone, app);
            bool headerOnly = app == null || !app.SupportsFullInfoPanel || zone.Height <= headerMetrics.InfoHeaderHeight + metrics.SI(2f);
            var panelCenter = new Vector2(zone.X + zone.Width * 0.5f, zone.Y + zone.Height * 0.5f);
            var panelSize = new Vector2(zone.Width, zone.Height);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", panelCenter, panelSize, UiPanelFillSoft));
            DrawScreenRectBorder(frame, panelCenter, panelSize, UiAccentDim);
            DrawInfoDrawerToggleBehindHeader(frame, app);
            if (app.Ui.InfoPanelMode == InfoPanelMode.Systems && (app.Ui.ActiveOverlay == OverlayMode.Cargo || (app.Ui.SelectedBlockStackItems != null && app.Ui.SelectedBlockStackItems.Count > 0) || GetManualBlockGroupCount(app.Ui) > 0))
                DrawInfoPanelHeaderTabs(frame, zone, app);
            else
                DrawInfoPanelHeader(frame, zone, GetInfoPanelHeading(app));
            if (headerOnly)
                return;

            if (app.Ui.ActiveOverlay == OverlayMode.Cargo)
            {
                var selectedCargoInfo = BuildSelectedBlockStackInfo(app.Ui);
                if (selectedCargoInfo == null)
                    selectedCargoInfo = FindSelectedOverlayInfo(app.Ui);
                DrawCargoInfoDrawerWidgets(frame, zone, app, selectedCargoInfo, headerMetrics.InfoHeaderHeight, isCursorOnlyRender);
                return;
            }

            int detailWidth = Math.Max(116, (int)Math.Round(zone.Width * 0.30f));
            int minGraphWidth = metrics.SI(120f);
            if (detailWidth < metrics.SI(116f))
                detailWidth = metrics.SI(116f);
            if (detailWidth > zone.Width - minGraphWidth)
                detailWidth = Math.Max(metrics.SI(80f), zone.Width - minGraphWidth);

            int graphWidth = Math.Max(1, zone.Width - detailWidth);
            int detailX = zone.X + graphWidth;
            int headerHeight = headerMetrics.InfoHeaderHeight;
            DrawInfoRailSeparator(frame, detailX, zone.Y + headerHeight + metrics.SI(4f), zone.Height - headerHeight - metrics.SI(8f));

            int inset = Math.Max(1, metrics.SI(1f));
            var graphZone = new ScreenZone(ScreenZoneType.CenterViewport, zone.X + inset, zone.Y + headerHeight, Math.Max(1, graphWidth - inset * 2), Math.Max(1, zone.Height - headerHeight - inset));
            if (app.Ui.InfoPanelMode == InfoPanelMode.Scan)
                DrawScanSymbolPanel(frame, graphZone, app);
            else
                DrawModeSymbolPanel(frame, graphZone, app, isCursorOnlyRender);

            var selectedInfo = FindSelectedOverlayInfo(app.Ui);
            if (selectedInfo == null)
                selectedInfo = BuildSelectedBlockStackInfo(app.Ui);
            var detailWidget = new CargoWidgetRect(detailX + metrics.Padding, zone.Y + headerHeight + metrics.Padding, Math.Max(1, detailWidth - metrics.Padding * 2f), Math.Max(1, zone.Height - headerHeight - metrics.Padding * 2f));
            DrawCargoWidgetFrame(frame, detailWidget, "BLOCK ACTIONS");
            var detailStart = new Vector2(detailWidget.X + metrics.S(7f), detailWidget.Y + metrics.S(17f));
            var detailSize = new Vector2(Math.Max(1, detailWidget.Width - metrics.S(14f)), Math.Max(1, detailWidget.Height - metrics.S(22f)));
            if (app.Ui.InfoPanelMode == InfoPanelMode.Scan)
            {
                DrawScanDrawerControls(frame, app);
            }
            else if (selectedInfo != null)
            {
                DrawSelectedOverlayInfoLines(frame, detailStart, detailSize, selectedInfo, app.Ui.SelectedOverlayLineIndex, UiText, UiSelected, UiAccentDim);
            }
            else
            {
                DrawSmallStatusGlyph(frame, new Vector2(detailWidget.X + detailWidget.Width * 0.5f, detailWidget.Y + detailWidget.Height * 0.48f), Math.Min(detailWidget.Width, detailWidget.Height) * 0.18f, UiAccentDim);
                AddSprite(frame, new MySprite(
                    SpriteType.TEXT,
                    "NO BLOCK",
                    new Vector2(detailWidget.X + detailWidget.Width * 0.5f, detailWidget.Y + detailWidget.Height * 0.68f),
                    null,
                    UiTextMuted,
                    InfoDrawerTextFontId,
                    TextAlignment.CENTER,
                    metrics.MediumText
                ));
            }
        }

        static void DrawInfoDrawerToggleBehindHeader(MySpriteDrawFrame frame, GridSchematicsLcdApp app)
        {
            if (app == null || app.Surface == null || app.Ui == null || app.Ui.InfoPanelMode != InfoPanelMode.Systems)
                return;

            var region = UiLayout.BuildInfoDrawerToggleRegion((int)app.Surface.SurfaceSize.X, (int)app.Surface.SurfaceSize.Y, true);
            if (string.IsNullOrEmpty(region.Id) || region.Width <= 0 || region.Height <= 0)
                return;

            string hoverId = app.TouchInput != null ? app.TouchInput.HoverRegionId ?? string.Empty : string.Empty;
            DrawViewButton(frame, region, "HIDE", false, string.Equals(region.Id, hoverId, StringComparison.Ordinal));
        }
        static string GetInfoPanelHeading(GridSchematicsLcdApp app)
        {
            if (app.Ui.InfoPanelMode == InfoPanelMode.Scan)
                return "HULL SCAN";
            if (app.Ui.ActiveOverlay == OverlayMode.Cargo)
            {
                var info = FindSelectedOverlayInfo(app.Ui);
                if (info != null && !string.IsNullOrEmpty(info.Name))
                    return ShortenInfoHeaderText(info.Name);
                return "TOTAL";
            }
            if (app.Ui.SelectedBlockStackItems != null && app.Ui.SelectedBlockStackItems.Count > 0)
            {
                var item = GetSelectedBlockStackItem(app.Ui);
                if (item != null && !string.IsNullOrEmpty(item.Name))
                    return ShortenInfoHeaderText(item.Name);
            }
            if (app.Ui.ShowConveyorOverlay && app.Ui.ActiveOverlay == OverlayMode.None)
                return "CONVEYOR";
            return FormatInfoModeLabel(app.Ui.ActiveOverlay);
        }

        static string ShortenInfoHeaderText(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "TOTAL";
            if (text.Length <= 22)
                return text.ToUpperInvariant();
            return text.Substring(0, 21).ToUpperInvariant() + ".";
        }

        static void DrawInfoPanelHeader(MySpriteDrawFrame frame, ScreenZone zone, string heading)
        {
            var metrics = zone.Width == 1024 && zone.Height == 368 ? UiLayout.BuildMetrics(1024, 1024) : UiLayout.BuildChromeMetrics(zone.Width, 512);
            int headerHeight = metrics.InfoHeaderHeight;
            var headerCenter = new Vector2(zone.X + zone.Width * 0.5f, zone.Y + headerHeight * 0.5f);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", headerCenter, new Vector2(zone.Width, headerHeight), UiMenuButtonFill));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(zone.X + zone.Width * 0.5f, zone.Y + headerHeight - metrics.Line * 0.5f), new Vector2(zone.Width, metrics.Line), UiAccentDim));

            float boxWidth = Math.Max(metrics.S(70f), heading.Length * metrics.S(8f) + metrics.S(18f));
            var labelCenter = new Vector2(zone.X + metrics.S(8f) + boxWidth * 0.5f, zone.Y + headerHeight * 0.5f);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", labelCenter, new Vector2(boxWidth, headerHeight), UiPanelFill));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(zone.X + metrics.S(8f), zone.Y + headerHeight * 0.5f), new Vector2(metrics.Line, headerHeight), UiAccentSoft));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(zone.X + metrics.S(8f) + boxWidth, zone.Y + headerHeight * 0.5f), new Vector2(metrics.Line, headerHeight), UiAccentSoft));
            AddSprite(frame, new MySprite(
                SpriteType.TEXT,
                heading,
                new Vector2(labelCenter.X, labelCenter.Y - metrics.S(6f)),
                null,
                UiText,
                InfoDrawerTextFontId,
                TextAlignment.CENTER,
                metrics.SmallText
            ));
        }

        static void DrawInfoPanelHeaderTabs(MySpriteDrawFrame frame, ScreenZone zone, GridSchematicsLcdApp app)
        {
            var ui = app != null ? app.Ui : null;
            string hoverId = app != null && app.TouchInput != null ? app.TouchInput.HoverRegionId ?? string.Empty : string.Empty;
            var metrics = BuildInfoPanelHeaderMetrics(zone, app);
            int headerHeight = metrics.InfoHeaderHeight;
            var headerCenter = new Vector2(zone.X + zone.Width * 0.5f, zone.Y + headerHeight * 0.5f);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", headerCenter, new Vector2(zone.Width, headerHeight), UiMenuButtonFill));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(zone.X + zone.Width * 0.5f, zone.Y + headerHeight - metrics.Line * 0.5f), new Vector2(zone.Width, metrics.Line), UiAccentDim));

            int count = ui != null && ui.SelectedBlockStackItems != null ? ui.SelectedBlockStackItems.Count : 0;
            int manualGroupCount = GetManualBlockGroupCount(ui);
            bool manualSelectionAvailable = manualGroupCount > 0;
            bool manualSelected = IsManualSelectedBlockStack(ui);
            bool auxAvailable = app != null && app.Ui != null && app.Ui.ActiveOverlay == OverlayMode.Cargo && app.HasAuxiliaryStaticCargoSource();
            bool auxActive = auxAvailable && string.Equals(app.Ui.CargoInfoSource ?? "LOCAL", "AUX", StringComparison.Ordinal);
            int metricScreenWidth = app != null && app.Surface != null ? (int)app.Surface.SurfaceSize.X : zone.Width;
            int metricScreenHeight = app != null && app.Surface != null ? (int)app.Surface.SurfaceSize.Y : 512;
            if (app != null && app.Ui.ActiveOverlay == OverlayMode.Cargo && ((metricScreenWidth == 512 && metricScreenHeight == 512) || (metricScreenWidth == 1024 && metricScreenHeight == 1024)))
            {
                int canonicalScale = metricScreenWidth == 1024 ? 2 : 1;
                int slot = 64 * canonicalScale;
                int fixedX = 0;
                if (auxAvailable)
                {
                    var auxRegion = new HitRegion(fixedX, zone.Y, slot, headerHeight, UiLayout.InfoPanelAuxTabId, "Show auxiliary connected static grid");
                    DrawInfoHeaderButton(frame, auxRegion, "AUX", auxActive, string.Equals(hoverId, auxRegion.Id, StringComparison.Ordinal));
                    fixedX += slot;
                }
                var fixedAllRegion = new HitRegion(fixedX, zone.Y, slot, headerHeight, UiLayout.InfoPanelAllTabId, auxAvailable ? "Show local cargo blocks" : "Show all blocks");
                DrawInfoHeaderButton(frame, fixedAllRegion, auxAvailable ? "LOCAL" : "ALL", !auxActive && ui.SelectedBlockStackIndex == UiState.SelectedBlockStackAllIndex, string.Equals(hoverId, fixedAllRegion.Id, StringComparison.Ordinal));
                fixedX += slot;
                if (manualGroupCount > 0)
                {
                    var group1Region = new HitRegion(fixedX, zone.Y, slot, headerHeight, UiLayout.InfoPanelStackTabId, "Show group");
                    DrawInfoHeaderButton(frame, group1Region, ManualGroupHeaderLabel(ui, 1, manualGroupCount), IsManualGroupSelected(ui, 1), string.Equals(hoverId, group1Region.Id, StringComparison.Ordinal));
                    fixedX += slot;
                    if (manualGroupCount > 1)
                    {
                        var group2Region = new HitRegion(fixedX, zone.Y, slot, headerHeight, UiLayout.InfoPanelGroup2TabId, "Show group 2");
                        DrawInfoHeaderButton(frame, group2Region, ManualGroupHeaderLabel(ui, 2, manualGroupCount), IsManualGroupSelected(ui, 2), string.Equals(hoverId, group2Region.Id, StringComparison.Ordinal));
                        fixedX += slot;
                    }
                }
                else if (count > 1)
                {
                    var stackRegion = new HitRegion(fixedX, zone.Y, slot, headerHeight, UiLayout.InfoPanelStackTabId, "Show selected stack");
                    DrawInfoHeaderButton(frame, stackRegion, StackHeaderLabel(ui), ui.SelectedBlockStackIndex == UiState.SelectedBlockStackAggregateIndex, string.Equals(hoverId, stackRegion.Id, StringComparison.Ordinal));
                    fixedX += slot;
                }

                int visibleSlots = Math.Max(0, (zone.X + zone.Width - fixedX) / slot);
                int fixedMaxScroll = Math.Max(0, count - visibleSlots);
                if (ui.SelectedBlockStackScrollIndex < 0)
                    ui.SelectedBlockStackScrollIndex = 0;
                if (ui.SelectedBlockStackScrollIndex > fixedMaxScroll)
                    ui.SelectedBlockStackScrollIndex = fixedMaxScroll;
                for (int slotIndex = 0; slotIndex < visibleSlots; slotIndex++)
                {
                    int itemIndex = ui.SelectedBlockStackScrollIndex + slotIndex;
                    if (itemIndex >= count)
                        break;
                    var item = ui.SelectedBlockStackItems[itemIndex];
                    if (item == null)
                        continue;
                    bool selected = itemIndex == ui.SelectedBlockStackIndex || ui.SelectedBlockStackIndex == UiState.SelectedBlockStackAggregateIndex;
                    var region = new HitRegion(fixedX + slotIndex * slot, zone.Y, slot, headerHeight, UiLayout.InfoPanelBlockTabPrefix + itemIndex.ToString(), "Select block tab");
                    DrawInfoHeaderButton(frame, region, ShortenInfoTabText(item.Name), selected, string.Equals(hoverId, region.Id, StringComparison.Ordinal));
                }
                DrawInfoHeaderScrollHints(frame, fixedX, zone.X + zone.Width, zone.Y, headerHeight, ui.SelectedBlockStackScrollIndex, Math.Max(0, count - (ui.SelectedBlockStackScrollIndex + visibleSlots)));
                return;
            }

            int pinnedWidth = UiLayout.InfoPanelPinnedTabWidth(metricScreenWidth, metrics);
            int x = zone.X;
            if (auxAvailable)
            {
                var auxRegion = new HitRegion(x, zone.Y, pinnedWidth, headerHeight, UiLayout.InfoPanelAuxTabId, "Show auxiliary connected static grid");
                DrawInfoHeaderButton(frame, auxRegion, "AUX", auxActive, string.Equals(hoverId, auxRegion.Id, StringComparison.Ordinal));
                x += pinnedWidth;
            }
            var allRegion = new HitRegion(x, zone.Y, pinnedWidth, headerHeight, UiLayout.InfoPanelAllTabId, auxAvailable ? "Show local cargo blocks" : "Show all blocks");
            DrawInfoHeaderButton(frame, allRegion, auxAvailable ? "LOCAL" : "ALL", !auxActive && ui.SelectedBlockStackIndex == UiState.SelectedBlockStackAllIndex, string.Equals(hoverId, allRegion.Id, StringComparison.Ordinal));
            x += pinnedWidth;
            if (manualGroupCount > 0)
            {
                var group1Region = new HitRegion(x, zone.Y, pinnedWidth, headerHeight, UiLayout.InfoPanelStackTabId, "Show group");
                DrawInfoHeaderButton(frame, group1Region, ManualGroupHeaderLabel(ui, 1, manualGroupCount), IsManualGroupSelected(ui, 1), string.Equals(hoverId, group1Region.Id, StringComparison.Ordinal));
                x += pinnedWidth;
                if (manualGroupCount > 1)
                {
                    var group2Region = new HitRegion(x, zone.Y, pinnedWidth, headerHeight, UiLayout.InfoPanelGroup2TabId, "Show group 2");
                    DrawInfoHeaderButton(frame, group2Region, ManualGroupHeaderLabel(ui, 2, manualGroupCount), IsManualGroupSelected(ui, 2), string.Equals(hoverId, group2Region.Id, StringComparison.Ordinal));
                    x += pinnedWidth;
                }
            }
            else if (count > 1)
            {
                var stackRegion = new HitRegion(x, zone.Y, pinnedWidth, headerHeight, UiLayout.InfoPanelStackTabId, "Show selected stack");
                DrawInfoHeaderButton(frame, stackRegion, StackHeaderLabel(ui), ui.SelectedBlockStackIndex == UiState.SelectedBlockStackAggregateIndex, string.Equals(hoverId, stackRegion.Id, StringComparison.Ordinal));
                x += pinnedWidth;
            }

            int stripX = x;
            int maxScroll = Math.Max(0, count - 1);
            if (ui.SelectedBlockStackScrollIndex < 0)
                ui.SelectedBlockStackScrollIndex = 0;
            if (ui.SelectedBlockStackScrollIndex > maxScroll)
                ui.SelectedBlockStackScrollIndex = maxScroll;

            int cursor = stripX;
            int lastVisibleIndex = ui.SelectedBlockStackScrollIndex - 1;
            for (int i = ui.SelectedBlockStackScrollIndex; i < count; i++)
            {
                if (cursor >= zone.X + zone.Width)
                    break;

                var item = ui.SelectedBlockStackItems[i];
                if (item == null)
                    continue;

                int width = UiLayout.InfoPanelBlockTabWidthForLabel(metricScreenWidth, metrics, item.Name);
                if (cursor + width > zone.X + zone.Width)
                    width = zone.X + zone.Width - cursor;
                if (width <= 0)
                    break;

                bool selected = i == ui.SelectedBlockStackIndex || ui.SelectedBlockStackIndex == UiState.SelectedBlockStackAggregateIndex;
                var region = new HitRegion(cursor, zone.Y, width, headerHeight, UiLayout.InfoPanelBlockTabPrefix + i, "Select block tab");
                DrawInfoHeaderButton(frame, region, ShortenInfoTabText(item.Name), selected, string.Equals(hoverId, region.Id, StringComparison.Ordinal));
                cursor += width;
                lastVisibleIndex = i;
            }
            DrawInfoHeaderScrollHints(frame, stripX, zone.X + zone.Width, zone.Y, headerHeight, ui.SelectedBlockStackScrollIndex, Math.Max(0, count - lastVisibleIndex - 1));
        }

        static void DrawInfoHeaderScrollHints(MySpriteDrawFrame frame, int stripX, int stripRight, int y, int headerHeight, int hiddenLeft, int hiddenRight)
        {
            float centerY = y + headerHeight * 0.5f;
            if (hiddenLeft > 0)
            {
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(stripX + 12f, centerY), new Vector2(24f, headerHeight), UiMenuButtonFill));
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(stripX + 7f, centerY), new Vector2(6f, 6f), UiSelected, null, TextAlignment.CENTER, -1.5708f));
                AddSprite(frame, new MySprite(SpriteType.TEXT, hiddenLeft.ToString(), new Vector2(stripX + 14f, y + headerHeight * 0.5f - 5f), null, UiSelected, InfoDrawerTextFontId, TextAlignment.LEFT, 0.22f));
            }
            if (hiddenRight > 0)
            {
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(stripRight - 12f, centerY), new Vector2(24f, headerHeight), UiMenuButtonFill));
                AddSprite(frame, new MySprite(SpriteType.TEXT, hiddenRight.ToString(), new Vector2(stripRight - 18f, y + headerHeight * 0.5f - 5f), null, UiSelected, InfoDrawerTextFontId, TextAlignment.RIGHT, 0.22f));
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(stripRight - 7f, centerY), new Vector2(6f, 6f), UiSelected, null, TextAlignment.CENTER, 1.5708f));
            }
        }

        static int GetManualBlockGroupCount(UiState ui)
        {
            if (ui == null)
                return 0;
            if (ui.ManualGroup2Enabled)
                return 2;
            return ui.ManualSelectedBlockItems != null && ui.ManualSelectedBlockItems.Count > 0 ? 1 : 0;
        }

        static int GetSelectedManualGroupIndex(UiState ui)
        {
            if (ui == null || string.IsNullOrEmpty(ui.SelectedBlockStackSignature))
                return 0;
            if (string.Equals(ui.SelectedBlockStackSignature, "manual:selection", StringComparison.Ordinal) ||
                string.Equals(ui.SelectedBlockStackSignature, "manual:group:1", StringComparison.Ordinal))
                return 1;
            if (string.Equals(ui.SelectedBlockStackSignature, "manual:group:2", StringComparison.Ordinal))
                return 2;
            return 0;
        }

        static bool IsManualSelectedBlockStack(UiState ui)
        {
            return GetSelectedManualGroupIndex(ui) > 0;
        }

        static bool IsManualGroupSelected(UiState ui, int groupIndex)
        {
            return GetSelectedManualGroupIndex(ui) == groupIndex;
        }

        static int GetManualGroupItemCount(UiState ui, int groupIndex)
        {
            if (ui == null)
                return 0;
            var items = groupIndex == 2 ? ui.ManualSelectedBlockItems2 : ui.ManualSelectedBlockItems;
            return items != null ? items.Count : 0;
        }

        static string ManualGroupHeaderLabel(UiState ui, int groupIndex, int groupCount)
        {
            string name = groupCount > 1 ? "GROUP" + (groupIndex == 2 ? "B" : "A") : "GROUP";
            return name + "-" + GetManualGroupItemCount(ui, groupIndex).ToString();
        }

        static string StackHeaderLabel(UiState ui)
        {
            int count = ui != null && ui.SelectedBlockStackItems != null ? ui.SelectedBlockStackItems.Count : 0;
            return count > 0 ? "STACK-" + count.ToString() : "STACK";
        }

        static void DrawInfoHeaderButton(MySpriteDrawFrame frame, HitRegion region, string label, bool active, bool hover)
        {
            DrawViewButton(frame, region, label, active, hover);
        }

        static string ShortenInfoTabText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "BLOCK";

            string acronym = string.Empty;
            bool emittedLetterForToken = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsLetter(c))
                {
                    if (!emittedLetterForToken)
                    {
                        acronym += char.ToUpperInvariant(c);
                        emittedLetterForToken = true;
                    }
                }
                else if (char.IsDigit(c))
                {
                    acronym += c;
                }
                else
                {
                    emittedLetterForToken = false;
                }
            }

            if (string.IsNullOrEmpty(acronym))
                return "BLOCK";
            return acronym;
        }

        static OverlayBlockInfo FindSelectedOverlayInfo(UiState ui)
        {
            if (ui == null || string.IsNullOrEmpty(ui.SelectedOverlayBlockId) || ui.OverlayBlockRegions == null)
                return null;

            for (int i = 0; i < ui.OverlayBlockRegions.Count; i++)
            {
                var info = ui.OverlayBlockRegions[i];
                if (info != null && string.Equals(info.Id, ui.SelectedOverlayBlockId, StringComparison.Ordinal))
                    return info;
            }

            return null;
        }

        static BlockStackItem GetSelectedBlockStackItem(UiState ui)
        {
            if (ui == null || ui.SelectedBlockStackItems == null || ui.SelectedBlockStackItems.Count == 0)
                return null;
            if (ui.SelectedBlockStackIndex < 0)
                return null;
            if (ui.SelectedBlockStackIndex >= ui.SelectedBlockStackItems.Count)
                ui.SelectedBlockStackIndex = ui.SelectedBlockStackItems.Count - 1;
            return ui.SelectedBlockStackItems[ui.SelectedBlockStackIndex];
        }

        static OverlayBlockInfo BuildSelectedBlockStackInfo(UiState ui)
        {
            if (ui != null && ui.SelectedBlockStackIndex == UiState.SelectedBlockStackAggregateIndex)
                return BuildSelectedBlockStackAggregateInfo(ui);
            if (ui != null && ui.SelectedBlockStackIndex == UiState.SelectedBlockStackAllIndex)
                return null;

            var item = GetSelectedBlockStackItem(ui);
            if (item == null)
                return null;

            var block = item.Block;
            var info = new OverlayBlockInfo
            {
                Id = item.Id,
                Name = item.Name,
                Role = "Block",
                State = GetSingleOverlayState(block as IMyFunctionalBlock),
                Metric = block != null ? "Fill: " + ((int)Math.Round(GetInventoryFillRatio(block) * 100f)).ToString() + "%" : string.Empty,
                Damage = GetSingleOverlayDamageText(block),
                Count = 1
            };

            if (block != null)
                info.Blocks.Add(block);
            var functional = block as IMyFunctionalBlock;
            if (functional != null)
                info.ToggleBlocks.Add(functional);

            info.Lines.Add(new OverlayBlockInfoLine { Text = "Name: " + item.Name });
            info.Lines.Add(new OverlayBlockInfoLine
            {
                Text = "State: " + GetSingleOverlayState(functional),
                ToggleBlock = functional
            });
            AddBatteryChargeModeLine(info, block, "Power");
            AddTerminalActionLines(info, block);
            info.Lines.Add(new OverlayBlockInfoLine { Text = GetSingleOverlayDamageText(block) });
            return info;
        }

        static OverlayBlockInfo BuildSelectedBlockStackAggregateInfo(UiState ui)
        {
            if (ui == null || ui.SelectedBlockStackItems == null || ui.SelectedBlockStackItems.Count == 0)
                return null;

            var info = new OverlayBlockInfo
            {
                Id = ui.SelectedBlockStackSignature,
                Name = IsManualSelectedBlockStack(ui) ? "Group" : "Selected Stack",
                Role = IsManualSelectedBlockStack(ui) ? "Group" : "Stack",
                Count = ui.SelectedBlockStackItems.Count
            };

            float fillTotal = 0f;
            int fillCount = 0;
            for (int i = 0; i < ui.SelectedBlockStackItems.Count; i++)
            {
                var item = ui.SelectedBlockStackItems[i];
                if (item == null || item.Block == null)
                    continue;

                info.Blocks.Add(item.Block);
                var functional = item.Block as IMyFunctionalBlock;
                if (functional != null)
                    info.ToggleBlocks.Add(functional);
                fillTotal += GetInventoryFillRatio(item.Block);
                fillCount++;
            }

            float averageFill = fillCount > 0 ? fillTotal / fillCount : 0f;
            info.Metric = "Fill: " + ((int)Math.Round(averageFill * 100f)).ToString() + "%";
            info.Lines.Add(new OverlayBlockInfoLine { Text = (IsManualSelectedBlockStack(ui) ? "Group: " : "Stack: ") + ui.SelectedBlockStackItems.Count.ToString() + " blocks" });
            info.Lines.Add(new OverlayBlockInfoLine
            {
                Text = info.Metric,
                IsFillBar = true,
                FillRatio = averageFill
            });
            AddSharedTerminalActionLines(info);

            return info;
        }


        static void AddSharedTerminalActionLines(OverlayBlockInfo info)
        {
            if (info == null || info.Blocks == null || info.Blocks.Count == 0)
                return;

            var map = new Dictionary<string, OverlayBlockInfoLine>();
            for (int blockIndex = 0; blockIndex < info.Blocks.Count; blockIndex++)
            {
                var terminal = info.Blocks[blockIndex] as IMyTerminalBlock;
                if (terminal == null)
                    continue;

                var actions = new List<ITerminalAction>();
                try
                {
                    terminal.GetActions(actions);
                }
                catch
                {
                    continue;
                }

                for (int i = 0; i < actions.Count; i++)
                {
                    var action = actions[i];
                    if (action == null)
                        continue;

                    string key = action.Id;
                    if (string.IsNullOrEmpty(key))
                        key = GetTerminalActionName(action);
                    if (string.IsNullOrEmpty(key))
                        continue;

                    OverlayBlockInfoLine line;
                    if (!map.TryGetValue(key, out line))
                    {
                        string name = GetTerminalActionName(action);
                        if (string.IsNullOrEmpty(name))
                            name = key;
                        line = new OverlayBlockInfoLine
                        {
                            Text = "Action: " + name,
                            TerminalActionId = key
                        };
                        map[key] = line;
                        info.Lines.Add(line);
                    }

                    line.TerminalBlocks.Add(terminal);
                    line.TerminalActions.Add(action);
                }
            }
        }
        static void DrawModeSymbolPanel(MySpriteDrawFrame frame, ScreenZone zone, GridSchematicsLcdApp app, bool isCursorOnlyRender)
        {
            var metrics = UiLayout.BuildMetrics(zone.Width, zone.Height);
            var center = new Vector2(zone.X + zone.Width * 0.5f, zone.Y + zone.Height * 0.5f);
            float pad = Math.Max(metrics.S(8f), zone.Height * 0.12f);
            float railY = center.Y;
            float left = zone.X + pad;
            float right = zone.X + zone.Width - pad;
            float width = Math.Max(20f, right - left);
            Color storage = ResolveStorageSchematicColor();
            Color effector = ResolveSecondarySchematicColor();
            Color conveyor = ApplyConveyorHue(new Color(0, 190, 210, 220));
            Color accent = app.Ui.ActiveOverlay == OverlayMode.Cargo ? storage :
                app.Ui.ActiveOverlay == OverlayMode.Power ? effector :
                app.Ui.ActiveOverlay == OverlayMode.Engines ? effector :
                app.Ui.ActiveOverlay == OverlayMode.Oxygen ? new Color(80, 180, 255, 230) :
                app.Ui.ShowConveyorOverlay ? conveyor : UiAccentSoft;
            Color hot = app.Ui.ActiveOverlay == OverlayMode.Cargo ? storage :
                app.Ui.ActiveOverlay == OverlayMode.Power ? storage :
                app.Ui.ActiveOverlay == OverlayMode.Engines ? effector :
                app.Ui.ActiveOverlay == OverlayMode.Oxygen ? new Color(130, 215, 255, 255) :
                app.Ui.ShowConveyorOverlay ? conveyor : UiAccentBright;
            Color muted = UiAccentDim;

            if (app.Ui.ActiveOverlay == OverlayMode.Cargo)
            {
                DrawCargoInfoGlyph(frame, zone, storage, conveyor, muted, app, isCursorOnlyRender);
                return;
            }
            if (app.Ui.ActiveOverlay == OverlayMode.Power)
            {
                DrawPowerInfoGlyph(frame, zone, storage, effector, muted);
                return;
            }
            if (app.Ui.ActiveOverlay == OverlayMode.Engines)
            {
                DrawEngineInfoGlyph(frame, zone, effector, muted);
                return;
            }
            if (app.Ui.ActiveOverlay == OverlayMode.Oxygen)
            {
                DrawOxygenInfoGlyph(frame, zone, new Color(120, 210, 255, 235), muted);
                return;
            }
            if (app.Ui.ShowConveyorOverlay)
            {
                DrawConveyorInfoGlyph(frame, zone, conveyor, muted);
                return;
            }

            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(left + width * 0.5f, railY), new Vector2(width, metrics.S(2f)), app.Ui.ShowConveyorOverlay ? conveyor : muted));

            int nodeCount = app.Ui.ActiveOverlay == OverlayMode.Power ? 6 :
                app.Ui.ActiveOverlay == OverlayMode.Engines ? 5 :
                app.Ui.ActiveOverlay == OverlayMode.Oxygen ? 4 :
                app.Ui.ActiveOverlay == OverlayMode.Cargo ? 7 : 6;

            for (int i = 0; i < nodeCount; i++)
            {
                float ratio = nodeCount <= 1 ? 0.5f : i / (float)(nodeCount - 1);
                float x = left + width * ratio;
                float height = (i % 2 == 0 ? 0.34f : 0.22f) * zone.Height;
                Color nodeColor = app.Ui.ActiveOverlay == OverlayMode.Power && i % 2 == 0 ? storage : hot;
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(x, railY - height * 0.25f), new Vector2(metrics.S(2f), height), accent));
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(x, railY), new Vector2(metrics.S(7f), metrics.S(7f)), nodeColor));
            }

            DrawSmallStatusGlyph(frame, new Vector2(zone.X + zone.Width * 0.18f, zone.Y + zone.Height * 0.72f), zone.Height * 0.12f, accent);
            DrawInfoBarGlyph(frame, new Vector2(zone.X + zone.Width * 0.42f, zone.Y + zone.Height * 0.72f), zone.Width * 0.22f, zone.Height * 0.12f, 0.72f, accent, muted);
            DrawInfoBarGlyph(frame, new Vector2(zone.X + zone.Width * 0.72f, zone.Y + zone.Height * 0.72f), zone.Width * 0.22f, zone.Height * 0.12f, 0.38f, hot, muted);
        }

        static string FormatInfoModeLabel(OverlayMode mode)
        {
            switch (mode)
            {
                case OverlayMode.Cargo:
                    return "CARGO";
                case OverlayMode.Engines:
                    return "THRUST";
                case OverlayMode.Power:
                    return "POWER";
                case OverlayMode.Oxygen:
                    return "OXYGEN";
                default:
                    return "SYSTEM";
            }
        }

        static void DrawInfoRailSeparator(MySpriteDrawFrame frame, int x, int y, int height)
        {
            var metrics = UiLayout.BuildMetrics(Math.Max(1, x), Math.Max(1, height));
            AddSprite(frame, new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                new Vector2(x + metrics.Line * 0.5f, y + height * 0.5f),
                new Vector2(metrics.Line, Math.Max(1, height)),
                UiAccentDim
            ));
        }

        static void DrawSmallStatusGlyph(MySpriteDrawFrame frame, Vector2 center, float radius, Color color)
        {
            if (radius < 4f)
                radius = 4f;
            DrawScreenRectBorder(frame, center, new Vector2(radius * 1.8f, radius * 1.8f), color);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, new Vector2(radius * 0.48f, radius * 0.48f), color));
        }

        static void DrawInfoBarGlyph(MySpriteDrawFrame frame, Vector2 center, float width, float height, float ratio, Color fill, Color back)
        {
            if (ratio < 0f)
                ratio = 0f;
            if (ratio > 1f)
                ratio = 1f;

            width = Math.Max(12f, width);
            height = Math.Max(8f, height);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, new Vector2(width, height), back));
            float fillWidth = width * ratio;
            if (fillWidth > 1f)
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(center.X - width * 0.5f + fillWidth * 0.5f, center.Y), new Vector2(fillWidth, height), fill));
        }

    }
}

















