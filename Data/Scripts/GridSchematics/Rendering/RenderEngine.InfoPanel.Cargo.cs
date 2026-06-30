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
        // QW1: TTL (in ticks) for the cargo summary/load-summary caches; ~0.5s keeps the drawer
        // visually current while removing the per-render physical-group inventory walk under power churn.
        const int CargoSummaryRefreshTicks = 30;

        // QW7: reused scratch list for per-inventory item reads (single-threaded render path),
        // instead of allocating a new List per inventory per block per summary build.
        static readonly List<VRage.Game.ModAPI.Ingame.MyInventoryItem> CargoInventoryItemScratch = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();

        static void DrawCargoInfoGlyph(MySpriteDrawFrame frame, ScreenZone zone, Color storage, Color conveyor, Color muted, GridSchematicsLcdApp app, bool isCursorOnlyRender)
        {
            var metrics = UiLayout.BuildMetrics(zone.Width, zone.Height);
            var summary = GetOrBuildCargoSummaryForPanel(app, FindSelectedOverlayInfo(app.Ui), isCursorOnlyRender);
            if (summary == null || summary.MaxVolume <= 0f)
            {
                summary = new CargoPanelSummary();
            }

            float pad = metrics.Padding;
            float left = zone.X + pad;
            float right = zone.X + zone.Width - pad;
            float width = Math.Max(24f, right - left);
            float gap = metrics.Gap;
            float summaryWidgetWidth = (width - gap) * 0.62f;
            float itemsWidgetWidth = Math.Max(36f, width - gap - summaryWidgetWidth);

            float widgetTop = zone.Y + metrics.Padding;
            float widgetHeight = Math.Max(metrics.S(24f), zone.Height - metrics.Padding * 2f);

            var summaryWidget = new CargoWidgetRect(left, widgetTop, summaryWidgetWidth, widgetHeight);
            var itemsWidget = new CargoWidgetRect(left + summaryWidgetWidth + gap, widgetTop, itemsWidgetWidth, widgetHeight);

            DrawCargoWidgetFrame(frame, summaryWidget, "CAPACITY");
            DrawCargoWidgetFrame(frame, itemsWidget, "TOP ITEMS");
            DrawCargoCapacityWidget(frame, summaryWidget, summary, storage);
            DrawCargoTopItemsWidget(frame, itemsWidget, summary, storage);
        }



        const float CargoReduxCanonicalPanelY = 312f;
        static bool CurrentCargoReduxUsesPanelTransform;
        static float CurrentCargoReduxScale = 1f;
        static float CurrentCargoReduxPanelX;
        static float CurrentCargoReduxPanelY;

        struct CargoDrawerMetrics
        {
            public float Pad;
            public float Gap;
            public float HeaderHeight;
            public float TitleText;
            public float TinyText;
            public float SmallText;
            public float ValueText;
            public float RowHeight;
            public float BarHeight;
            public float MicroBarHeight;
            public float Line;
        }

        static CargoDrawerMetrics BuildCargoDrawerMetrics(CargoWidgetRect widget)
        {
            float minSide = Math.Max(1f, Math.Min(widget.Width, widget.Height));
            float widthScale = widget.Width / 170f;
            float heightScale = widget.Height / 170f;
            float scale = Math.Min(widthScale, heightScale);
            if (scale < 0.86f) scale = 0.86f;
            if (scale > 1.18f) scale = 1.18f;

            var metrics = new CargoDrawerMetrics();
            metrics.Pad = ClampFloat(widget.Width * 0.035f, 6f, 12f);
            metrics.Gap = ClampFloat(minSide * 0.020f, 2f, 6f);
            metrics.HeaderHeight = ClampFloat(widget.Height * 0.095f, 17f, 28f);
            metrics.TitleText = ClampFloat(0.255f * scale, 0.23f, 0.30f);
            metrics.TinyText = ClampFloat(0.235f * scale, 0.22f, 0.27f);
            metrics.SmallText = ClampFloat(0.255f * scale, 0.23f, 0.30f);
            metrics.ValueText = ClampFloat(0.290f * scale, 0.26f, 0.34f);
            metrics.RowHeight = ClampFloat(widget.Height * 0.074f, 13f, 17f);
            metrics.BarHeight = ClampFloat(widget.Height * 0.060f, 15f, 24f);
            metrics.MicroBarHeight = ClampFloat(widget.Height * 0.038f, 9f, 15f);
            metrics.Line = scale < 0.96f ? 1f : scale;
            return metrics;
        }

        static float ClampFloat(float value, float min, float max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
        static void DrawCargoInfoDrawerWidgets(MySpriteDrawFrame frame, ScreenZone zone, GridSchematicsLcdApp app, OverlayBlockInfo selectedInfo, int headerHeight, bool isCursorOnlyRender)
        {
            var summary = GetOrBuildCargoSummaryForPanel(app, selectedInfo, isCursorOnlyRender);
            if (summary == null)
                summary = new CargoPanelSummary();
            var loadSummary = GetOrBuildCargoLoadSummaryForPanel(app, isCursorOnlyRender);
            if (loadSummary == null)
                loadSummary = new CargoPanelSummary();

            bool previousUsesPanelTransform = CurrentCargoReduxUsesPanelTransform;
            float previousScale = CurrentCargoReduxScale;
            float previousPanelX = CurrentCargoReduxPanelX;
            float previousPanelY = CurrentCargoReduxPanelY;
            ConfigureCargoReduxLayout(zone, app);
            try
            {
                DrawCargoInfoDrawerReduxChrome(frame, app, summary);
                DrawCargoInfoDrawerReduxLoad(frame, loadSummary, null, app);
                DrawCargoInfoDrawerReduxMix(frame, summary, app);
                DrawCargoInfoDrawerReduxActions(frame, selectedInfo, app);
            }
            finally
            {
                CurrentCargoReduxUsesPanelTransform = previousUsesPanelTransform;
                CurrentCargoReduxScale = previousScale;
                CurrentCargoReduxPanelX = previousPanelX;
                CurrentCargoReduxPanelY = previousPanelY;
            }
        }

        static void DrawCargoInfoDrawerReduxChrome(MySpriteDrawFrame frame, GridSchematicsLcdApp app, CargoPanelSummary mixSummary)
        {
            Color panel = new Color(7, 7, 7, 255);
            Color major = new Color(91, 91, 91, 190);
            Color soft = new Color(91, 91, 91, 75);
            Color faint = new Color(192, 192, 192, 13);
            string hoverId = app != null && app.TouchInput != null ? app.TouchInput.HoverRegionId ?? string.Empty : string.Empty;
            string mode = app != null && app.Ui != null ? app.Ui.CargoRightPanelMode ?? "TRANSFER" : "TRANSFER";
            bool transferMode = !string.Equals(mode, "ACTIONS", StringComparison.OrdinalIgnoreCase);
            bool transferSelectionActive = app != null && app.Ui != null && app.Ui.CargoTransferSelectionActive;

            AddCargoReduxRect(frame, 256f, 412f, 512f, 168f, panel);
            AddCargoReduxRect(frame, 256f, 328.5f, 512f, 1f, major);
            AddCargoReduxRect(frame, 256f, 496f, 512f, 1f, major);
            AddCargoReduxRect(frame, 0f, 412f, 1f, 200f, major);
            AddCargoReduxRect(frame, 512f, 412f, 1f, 200f, major);
            AddCargoReduxRect(frame, 168f, 412f, 1f, 168f, soft);
            AddCargoReduxRect(frame, 344f, 412f, 1f, 168f, soft);

            AddCargoReduxText(frame, CargoLoadHeaderLabel(app), 7f, 331f, ResolveStorageSchematicColor(), 0.30f, TextAlignment.LEFT);
            AddCargoReduxText(frame, CargoMixHeaderLabel(app, mixSummary), 176f, 331f, CargoMixHeaderColor(app), 0.30f, TextAlignment.LEFT);
            AddCargoReduxText(frame, "FILTER:", 248f, 331f, CargoSelectableTextColor(app, UiLayout.CargoInfoFilterToggleId, true), 0.30f, TextAlignment.LEFT);
            DrawCargoRightHeaderText(frame, "TRANSFER", 365f, UiLayout.CargoInfoTransferModeId, transferSelectionActive, transferMode, hoverId);
            DrawCargoRightHeaderText(frame, "ACTIONS", 444f, UiLayout.CargoInfoActionsModeId, !transferMode, false, hoverId);
            AddCargoReduxTexture(frame, "Triangle", 331f, 337f, 6f, 6f, CargoSelectableTextColor(app, UiLayout.CargoInfoFilterToggleId, true), 3.1416f);

            AddCargoReduxRect(frame, 84f, 345.5f, 156f, 1f, new Color(192, 192, 192, 17));
            AddCargoReduxRect(frame, 256f, 345.5f, 164f, 1f, faint);
            AddCargoReduxRect(frame, 428f, 345.5f, 156f, 1f, new Color(91, 91, 91, 48));

            AddCargoReduxRect(frame, 250f, 384.5f, 152f, 1f, faint);
            AddCargoReduxRect(frame, 222f, 426.5f, 108f, 1f, faint, 1.57079637f);
            AddCargoReduxRect(frame, 254f, 426.5f, 108f, 1f, faint, 1.57079637f);
            AddCargoReduxRect(frame, 292f, 426.5f, 108f, 1f, faint, 1.57079637f);
            for (int i = 0; i < 7; i++)
                AddCargoReduxRect(frame, 250f, 400.5f + i * 16f, 152f, 1f, faint);
        }

        static string CargoMixHeaderLabel(GridSchematicsLcdApp app, CargoPanelSummary summary)
        {
            if (summary != null && !string.IsNullOrWhiteSpace(summary.DisplayLabel))
                return AbbreviateCargoBlockLabel(summary.DisplayLabel, 8);
            return "CARGO MIX";
        }

        static Color CargoMixHeaderColor(GridSchematicsLcdApp app)
        {
            if (app != null && app.Ui != null && string.Equals(app.Ui.CargoRightPanelMode ?? string.Empty, "TRANSFER", StringComparison.OrdinalIgnoreCase))
                return string.Equals(app.Ui.CargoTransferMixViewTarget ?? "SOURCE", "DEST", StringComparison.Ordinal) ? ResolveSecondarySchematicColor() : ResolveStorageSchematicColor();
            return UiTextMuted;
        }
        static void DrawCargoInfoDrawerReduxLoad(MySpriteDrawFrame frame, CargoPanelSummary summary, OverlayBlockInfo selectedInfo, GridSchematicsLcdApp app)
        {
            Color storage = ResolveStorageSchematicColor();
            Color fill = GetCargoFillColor(storage, summary.FillRatio);
            string used = FormatCargoCompactVolume(summary.CurrentVolume);
            string max = FormatCargoCompactVolume(summary.MaxVolume);
            string free = FormatCargoCompactVolume(Math.Max(0f, summary.MaxVolume - summary.CurrentVolume));
            string hoverId = app != null && app.TouchInput != null ? app.TouchInput.HoverRegionId ?? string.Empty : string.Empty;
            Color sendBase = ResolveStorageSchematicColor();
            Color sendColor = string.Equals(hoverId, UiLayout.CargoInfoSendBlockToTransferId, StringComparison.Ordinal)
                ? sendBase
                : new Color(sendBase.R, sendBase.G, sendBase.B, 118);

            string sendLabel = CargoLoadSendLabel(app);
            if (!string.IsNullOrEmpty(sendLabel))
                AddCargoReduxText(frame, sendLabel, 160f, 331f, sendColor, 0.30f, TextAlignment.RIGHT);
            AddCargoReduxText(frame, used, 6f, 348f, fill, 0.25f, TextAlignment.LEFT);
            AddCargoReduxText(frame, "/ " + max, 76f, 348f, UiTextMuted, 0.25f, TextAlignment.RIGHT);
            AddCargoReduxText(frame, FormatCargoCompactMass(summary.CurrentVolume), 127.8f, 348f, storage, 0.25f, TextAlignment.RIGHT);
            AddCargoReduxText(frame, "/ " + FormatCargoCompactMass(summary.MaxVolume), 162f, 348f, UiTextMuted, 0.25f, TextAlignment.RIGHT);

            float barLeft = 6f;
            float barRight = 162f;
            float barWidth = barRight - barLeft;
            float ratio = Clamp01(summary.FillRatio);
            float freeRatio = summary.MaxVolume > 0f ? Clamp01((summary.MaxVolume - summary.CurrentVolume) / summary.MaxVolume) : 0f;
            AddCargoReduxRect(frame, 84f, 368f, 156f, 14f, new Color(storage.R, storage.G, storage.B, 15));
            AddCargoReduxRect(frame, barLeft + barWidth * freeRatio * 0.5f, 368f, Math.Max(1f, barWidth * freeRatio), 8f, new Color(storage.R, storage.G, storage.B, 32));
            float twrRatio = EstimateCargoTwrLimitRatio(app, summary);
            if (twrRatio >= 0f && twrRatio <= 1f)
            {
                float twrWidth = Math.Max(1f, barWidth * Math.Max(0f, 1f - twrRatio));
                AddCargoReduxRect(frame, barRight - twrWidth * 0.5f, 368f, twrWidth, 14f, new Color(UiWarning.R, UiWarning.G, UiWarning.B, 25));
            }
            AddCargoReduxRect(frame, barLeft + barWidth * ratio * 0.5f, 368f, Math.Max(1f, barWidth * ratio), 8f, fill);
            AddCargoReduxRect(frame, 84f, 361.5f, 156f, 1f, storage);
            AddCargoReduxRect(frame, 84f, 374.5f, 156f, 1f, storage);
            AddCargoReduxRect(frame, 6.5f, 368f, 1f, 14f, storage);
            AddCargoReduxRect(frame, 161.5f, 368f, 1f, 14f, storage);

            AddCargoReduxText(frame, "FILL: " + ((int)Math.Round(ratio * 100f)).ToString() + "%", 6f, 377f, fill, 0.25f, TextAlignment.LEFT);
            AddCargoReduxText(frame, "RESERVE: " + free + " / " + FormatCargoCompactMass(Math.Max(0f, summary.MaxVolume - summary.CurrentVolume)), 162f, 377f, storage, 0.25f, TextAlignment.RIGHT);

            bool selectionScoped = IsCargoLoadSelectionScoped(app);
            bool cursorActive = !selectionScoped || IsCargoLoadCursorActive(app);
            CargoPanelBlock selectedBlock = cursorActive ? GetCurrentCargoCursorBlock(summary, app) : null;
            Color cursorReadout = storage;
            if (selectedBlock != null)
                cursorReadout = IsCargoPanelBlockDamaged(selectedBlock) ? UiWarning : (!selectedBlock.Reachable ? new Color(255, 128, 0, 255) : storage);
            string nameValue = selectedBlock != null ? CurrentCargoCursorBlockLabel(summary, app) : selectionScoped ? CargoLoadSelectionLabel(app) : CurrentCargoCursorBlockLabel(summary, app);
            string fillValue = selectedBlock != null ? ((int)Math.Round(Clamp01(selectedBlock.FillRatio) * 100f)).ToString() + "%" : selectionScoped ? ((int)Math.Round(ratio * 100f)).ToString() + "%" : "--";
            string volumeValue = selectedBlock != null ? FormatCargoCompactVolume(GetCargoBlockMaxVolume(selectedBlock.Block)) : selectionScoped ? FormatCargoCompactVolume(summary.MaxVolume) : "--";
            AddCargoReduxText(frame, "NAME:", 7f, 396f, cursorReadout, 0.25f, TextAlignment.LEFT);
            AddCargoReduxText(frame, nameValue, 41f, 396f, cursorReadout, 0.25f, TextAlignment.LEFT);
            AddCargoReduxText(frame, "FILL:", 7f, 406f, cursorReadout, 0.25f, TextAlignment.LEFT);
            AddCargoReduxText(frame, fillValue, 41f, 406f, cursorReadout, 0.25f, TextAlignment.LEFT);
            AddCargoReduxText(frame, "VOLUME:", 7f, 416f, cursorReadout, 0.25f, TextAlignment.LEFT);
            AddCargoReduxText(frame, volumeValue, 41f, 416f, cursorReadout, 0.25f, TextAlignment.LEFT);

            AddCargoReduxText(frame, "TOTAL:", 80f, 396f, storage, 0.25f, TextAlignment.LEFT);
            AddCargoReduxText(frame, summary.Blocks.Count.ToString(), 125f, 396f, storage, 0.25f, TextAlignment.LEFT);
            AddCargoReduxText(frame, "ISOLATED:", 80f, 406f, new Color(255, 128, 0, 255), 0.25f, TextAlignment.LEFT);
            AddCargoReduxText(frame, summary.IsolatedCount.ToString(), 125f, 406f, new Color(255, 128, 0, 255), 0.25f, TextAlignment.LEFT);
            AddCargoReduxText(frame, FormatCargoCompactVolume(GetIsolatedCargoVolume(summary)), 134f, 406f, new Color(255, 128, 0, 255), 0.25f, TextAlignment.LEFT);
            AddCargoReduxText(frame, "DAMAGED:", 80f, 416f, UiWarning, 0.25f, TextAlignment.LEFT);
            AddCargoReduxText(frame, summary.OfflineCount.ToString(), 125f, 416f, UiWarning, 0.25f, TextAlignment.LEFT);
            AddCargoReduxText(frame, FormatCargoCompactVolume(GetOfflineCargoVolume(summary)), 134f, 416f, UiWarning, 0.25f, TextAlignment.LEFT);

            DrawCargoReduxBlockBars(frame, summary, storage, app, selectedInfo);
        }

        static void DrawCargoReduxBlockBars(MySpriteDrawFrame frame, CargoPanelSummary summary, Color storage, GridSchematicsLcdApp app, OverlayBlockInfo selectedInfo)
        {
            int visible = 13;
            int count = summary != null && summary.Blocks != null ? summary.Blocks.Count : 0;
            int first = app != null && app.Ui != null ? app.Ui.CargoBlockScrollIndex : 0;
            if (first < 0) first = 0;
            if (first + visible > count)
                first = Math.Max(0, count - visible);
            if (app != null && app.Ui != null && app.Ui.CargoBlockScrollIndex != first)
                app.Ui.CargoBlockScrollIndex = first;
            int visibleCount = Math.Min(visible, Math.Max(0, count - first));
            int cursorLane = app != null && app.Ui != null ? app.Ui.CargoBlockCursorIndex : 0;
            if (cursorLane < 0) cursorLane = 0;
            if (visibleCount > 0 && cursorLane >= visibleCount) cursorLane = visibleCount - 1;
            if (visibleCount <= 0) cursorLane = 0;
            if (app != null && app.Ui != null && app.Ui.CargoBlockCursorIndex != cursorLane)
                app.Ui.CargoBlockCursorIndex = cursorLane;

            float baseY = 482f;
            for (int lane = 0; lane < visibleCount; lane++)
            {
                float x = 11f + lane * 12f;
                int index = first + lane;
                var block = summary.Blocks[index];
                Color baseColor = block == null ? storage : (IsCargoPanelBlockDamaged(block) ? UiWarning : (!block.Reachable ? new Color(255, 128, 0, 255) : storage));
                AddCargoReduxRect(frame, x, 465f, 10f, 34f, new Color(baseColor.R, baseColor.G, baseColor.B, 15));
                float fillRatio = block != null ? Clamp01(block.FillRatio) : 0f;
                float h = Math.Max(1f, 34f * fillRatio);
                AddCargoReduxRect(frame, x, baseY - h * 0.5f, 10f, h, baseColor);
            }

            if (visibleCount > 0)
            {
                float leftX = 11f;
                float rightX = 11f + (visibleCount - 1) * 12f;
                float railCenter = (leftX + rightX) * 0.5f;
                float railWidth = Math.Max(2f, rightX - leftX);
                AddCargoReduxRect(frame, railCenter, 441f, railWidth, 2f, storage);
                AddCargoReduxRect(frame, leftX, 443.5f, 2f, 5f, storage);
                AddCargoReduxRect(frame, rightX, 443.5f, 2f, 5f, storage);
                AddCargoReduxText(frame, count > 0 ? (first + 1).ToString("00") : "00", leftX + 0.5f, 427f, storage, 0.25f, TextAlignment.CENTER);
                AddCargoReduxText(frame, count > 0 ? Math.Min(count, first + visibleCount).ToString("00") : "00", rightX, 427f, storage, 0.25f, TextAlignment.CENTER);
                if (!IsCargoLoadSelectionScoped(app) || IsCargoLoadCursorActive(app))
                {
                    int cursorIndex = Math.Min(count - 1, first + cursorLane);
                    float selectedX = 11f + cursorLane * 12f;
                    if (cursorLane != 0 && cursorLane != visibleCount - 1)
                        AddCargoReduxText(frame, (cursorIndex + 1).ToString("00"), selectedX, 427f, storage, 0.25f, TextAlignment.CENTER);
                    AddCargoReduxRect(frame, selectedX, 441.5f, 2f, 9f, storage);
                }
            }

            AddCargoReduxRect(frame, 83f, 488f, 154f, 3f, new Color(storage.R, storage.G, storage.B, 8));
            float thumbWidth = count <= visible ? 154f : Math.Max(12f, 154f * visible / (float)Math.Max(visible, count));
            float thumbRatio = count <= visible ? 0f : first / (float)Math.Max(1, count - visible);
            AddCargoReduxRect(frame, 6f + thumbWidth * 0.5f + thumbRatio * (154f - thumbWidth), 488f, thumbWidth, 5f, new Color(storage.R, storage.G, storage.B, 95));
        }

        static bool IsCargoPanelBlockDamaged(CargoPanelBlock block)
        {
            if (block == null || block.Block == null)
                return false;
            var functional = block.Block as IMyFunctionalBlock;
            if (functional == null)
                return false;
            try
            {
                return !functional.IsFunctional;
            }
            catch
            {
                return false;
            }
        }
        static int GetSelectedCargoBlockIndex(CargoPanelSummary summary, GridSchematicsLcdApp app, OverlayBlockInfo selectedInfo)
        {
            if (summary == null || summary.Blocks == null)
                return -1;

            long selectedId = 0L;
            if (selectedInfo != null && selectedInfo.Blocks != null && selectedInfo.Blocks.Count > 0 && selectedInfo.Blocks[0] != null)
                selectedId = selectedInfo.Blocks[0].EntityId;
            if (selectedId == 0L && app != null && app.Ui != null && app.Ui.SelectedBlockStackItems != null && app.Ui.SelectedBlockStackIndex >= 0 && app.Ui.SelectedBlockStackIndex < app.Ui.SelectedBlockStackItems.Count)
            {
                var selected = app.Ui.SelectedBlockStackItems[app.Ui.SelectedBlockStackIndex];
                if (selected != null && selected.Block != null)
                    selectedId = selected.Block.EntityId;
            }
            if (selectedId == 0L)
                return -1;
            for (int i = 0; i < summary.Blocks.Count; i++)
            {
                var block = summary.Blocks[i] != null ? summary.Blocks[i].Block : null;
                if (block != null && block.EntityId == selectedId)
                    return i;
            }
            return -1;
        }
        static void DrawCargoInfoDrawerReduxMix(MySpriteDrawFrame frame, CargoPanelSummary summary, GridSchematicsLcdApp app)
        {
            Color storage = ResolveStorageSchematicColor();
            string filter = summary != null ? NormalizeCargoInfoSelector(summary.Filter) : "ALL";
            string hoverId = app != null && app.TouchInput != null ? app.TouchInput.HoverRegionId ?? string.Empty : string.Empty;
            AddCargoReduxText(frame, CargoFilterHeaderLabel(filter), 289f, 331f, CargoSelectableTextColor(app, UiLayout.CargoInfoFilterToggleId, true), 0.30f, TextAlignment.LEFT);
            DrawCargoReduxStackedBar(frame, summary, filter, storage);

            var rows = BuildCargoMixRowsForRender(summary, filter);
            ApplyCargoMixSort(rows, app);
            DrawCargoMixSortHeaders(frame, app);

            float total = GetCargoMixTotal(summary, filter);
            int visible = 6;
            int first = app != null && app.Ui != null ? app.Ui.CargoMixScrollIndex : 0;
            if (first < 0) first = 0;
            if (first + visible > rows.Count)
                first = Math.Max(0, rows.Count - visible);
            if (app != null && app.Ui != null && app.Ui.CargoMixScrollIndex != first)
                app.Ui.CargoMixScrollIndex = first;
            int shown = Math.Min(visible, Math.Max(0, rows.Count - first));
            for (int i = 0; i < shown; i++)
            {
                int rowIndex = first + i;
                var item = rows[rowIndex];
                float y = 388f + i * 16f;
                bool hover = string.Equals(hoverId, UiLayout.CargoInfoMixRowPrefix + i.ToString(), StringComparison.Ordinal);
                bool selected = app != null && app.Ui != null && app.Ui.CargoMixSelectedItemKeys.IndexOf(CargoPanelItemKey(item)) >= 0;
                if (selected)
                    AddCargoReduxRect(frame, 250f, y + 5f, 152f, 16f, new Color(UiSelected.R, UiSelected.G, UiSelected.B, hover ? 72 : 36));
                Color color = GetCargoMixItemColor(item, rowIndex, filter, storage);
                Color textColor = hover ? UiText : UiTextMuted;
                AddCargoReduxRect(frame, 181f, y + 5f, 10f, 10f, color);
                AddCargoReduxText(frame, ShortenCargoMixItemName(item.Name), 190f, y, hover ? UiText : color, 0.25f, TextAlignment.LEFT);
                string quant = filter == "COMP" ? FormatCargoCompactAmount(item.Amount > 0f ? item.Amount : item.Volume) : ((int)Math.Round((total > 0f ? item.Volume / total : 0f) * 100f)).ToString() + "%";
                AddCargoReduxText(frame, quant, 230f, y, textColor, 0.25f, TextAlignment.LEFT);
                AddCargoReduxText(frame, FormatCargoCompactVolume(item.Volume), 258f, y, textColor, 0.25f, TextAlignment.LEFT);
                AddCargoReduxText(frame, FormatCargoCompactMass(item.Volume), 296f, y, textColor, 0.25f, TextAlignment.LEFT);
            }

            int hidden = Math.Max(0, rows.Count - first - shown);
            if (hidden > 0)
            {
                AddCargoReduxText(frame, hidden.ToString(), 181f, 483f, UiTextMuted, 0.25f, TextAlignment.CENTER);
                AddCargoReduxTexture(frame, "Triangle", 192f, 489f, 6f, 6f, UiTextMuted, 3.1416f);
            }

            bool hasDestSelection = app == null || app.Ui == null || (app.Ui.CargoTransferDestItems != null && app.Ui.CargoTransferDestItems.Count > 0);
            bool canAddQuota = hasDestSelection && (app == null || app.Ui == null || !string.Equals(app.Ui.CargoRightPanelMode ?? string.Empty, "TRANSFER", StringComparison.OrdinalIgnoreCase) || string.Equals(app.Ui.CargoTransferMixViewTarget ?? "SOURCE", "SOURCE", StringComparison.Ordinal));
            string addLabel = canAddQuota ? (app != null && app.Ui != null && app.Ui.CargoMixSelectedItemKeys.Count > 0 ? "ADD TO QUOTA >>" : "ADD ALL >>") : (!hasDestSelection ? "SELECT DEST" : "SOURCE REQUIRED");
            AddCargoReduxText(frame, addLabel, 324f, 483f, canAddQuota ? CargoSelectableTextColor(app, UiLayout.CargoInfoMixAddToQuotaId, false) : UiTextMuted, 0.25f, TextAlignment.RIGHT);

            AddCargoReduxRect(frame, 334f, 432.5f, 4f, 97f, new Color(128, 128, 128, 7));
            float visibleRatio = rows.Count <= 0 ? 1f : Math.Min(1f, visible / (float)rows.Count);
            float thumbH = Math.Max(8f, 108f * visibleRatio);
            float thumbRatio = rows.Count <= visible ? 0f : first / (float)Math.Max(1, rows.Count - visible);
            AddCargoReduxRect(frame, 334f, 377f + thumbH * 0.5f + thumbRatio * Math.Max(1f, 108f - thumbH), 6f, thumbH, new Color(192, 192, 192, 115));
            DrawCargoReduxFilterDropdown(frame, app, filter);
        }

        static void DrawCargoReduxFilterDropdown(MySpriteDrawFrame frame, GridSchematicsLcdApp app, string activeFilter)
        {
            if (app == null || app.Ui == null || !app.Ui.CargoFilterDropdownOpen)
                return;
            string hoverId = app.TouchInput != null ? app.TouchInput.HoverRegionId ?? string.Empty : string.Empty;
            string hoverFilter = hoverId.StartsWith(UiLayout.CargoInfoFilterPrefix, StringComparison.Ordinal)
                ? NormalizeCargoInfoSelector(hoverId.Substring(UiLayout.CargoInfoFilterPrefix.Length))
                : string.Empty;
            string[] options = new[] { "ALL", "ORE", "INGOT", "COMP", "TOOLS", "CONSUMABLE" };
            AddCargoReduxRect(frame, 288f, 400f, 84f, 96f, new Color(6, 8, 8, 255));
            DrawCargoReduxBorder(frame, 288f, 400f, 84f, 96f, UiAccentDim);
            for (int i = 0; i < options.Length; i++)
            {
                string option = options[i];
                string normalizedOption = NormalizeCargoInfoSelector(option);
                float y = 352f + i * 16f;
                bool active = normalizedOption == NormalizeCargoInfoSelector(activeFilter);
                bool hover = normalizedOption == hoverFilter;
                Color fill = new Color(6, 8, 8, 255);
                Color border = active ? UiSelected : UiAccentDim;
                Color text = active ? UiSelected : UiText;
                if (hover)
                {
                    fill = new Color(UiSelected.R, UiSelected.G, UiSelected.B, 18);
                    border = UiSelected;
                    text = UiSelected;
                }
                else if (active)
                {
                    fill = new Color(UiSelected.R, UiSelected.G, UiSelected.B, 24);
                }

                AddCargoReduxRect(frame, 288f, y + 8f, 82f, 16f, fill);
                DrawCargoReduxBorder(frame, 288f, y + 8f, 82f, 16f, border);
                AddCargoReduxText(frame, CargoFilterDisplayLabel(option), 250f, y + 3f, text, 0.25f, TextAlignment.LEFT);
            }
        }
        static string ShortenCargoMixItemName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;
            name = name.Trim();
            if (name.Length <= 6)
                return name;
            return name.Substring(0, 6) + ".";
        }
        static void DrawCargoMixSortHeaders(MySpriteDrawFrame frame, GridSchematicsLcdApp app)
        {
            string key = app != null && app.Ui != null && !string.IsNullOrEmpty(app.Ui.CargoMixSortKey) ? app.Ui.CargoMixSortKey : "QUANT";
            int direction = app != null && app.Ui != null && app.Ui.CargoMixSortDirection == 2 ? 2 : 1;
            string hoverId = app != null && app.TouchInput != null ? app.TouchInput.HoverRegionId ?? string.Empty : string.Empty;
            DrawCargoMixSortHeader(frame, "ITEM", 178f, key, direction, hoverId);
            DrawCargoMixSortHeader(frame, "QUANT", 237f, key, direction, hoverId);
            DrawCargoMixSortHeader(frame, "VOLUME", 258f, key, direction, hoverId);
            DrawCargoMixSortHeader(frame, "MASS", 296f, key, direction, hoverId);
        }
        static void DrawCargoMixSortHeader(MySpriteDrawFrame frame, string label, float x, string activeKey, int direction, string hoverId)
        {
            bool active = direction != 0 && string.Equals(activeKey, label, StringComparison.Ordinal);
            bool hover = string.Equals(hoverId, UiLayout.CargoInfoMixSortPrefix + label, StringComparison.Ordinal);
            Color color = active ? (hover ? UiSelected : new Color(UiSelected.R, UiSelected.G, UiSelected.B, 155)) : (hover ? UiText : UiTextMuted);
            TextAlignment align = label == "QUANT" ? TextAlignment.CENTER : TextAlignment.LEFT;
            AddCargoReduxText(frame, label == "QUANT" ? "QTY" : label, x, 372f, color, 0.25f, align);
        }
        static void ApplyCargoMixSort(List<CargoPanelItem> rows, GridSchematicsLcdApp app)
        {
            if (rows == null)
                return;
            string key = app != null && app.Ui != null && !string.IsNullOrEmpty(app.Ui.CargoMixSortKey) ? app.Ui.CargoMixSortKey : "QUANT";
            int direction = app != null && app.Ui != null && app.Ui.CargoMixSortDirection == 2 ? 2 : 1;
            rows.Sort(delegate(CargoPanelItem a, CargoPanelItem b)
            {
                int result = CompareCargoMixItems(a, b, key);
                return direction == 2 ? -result : result;
            });
        }

        static int CompareCargoMixItems(CargoPanelItem a, CargoPanelItem b, string key)
        {
            if (a == null && b == null) return 0;
            if (a == null) return 1;
            if (b == null) return -1;
            key = key ?? string.Empty;
            if (key == "ITEM")
                return string.Compare(a.Name ?? string.Empty, b.Name ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            float aValue = key == "MASS" ? a.Mass : key == "QUANT" ? a.Amount : a.Volume;
            float bValue = key == "MASS" ? b.Mass : key == "QUANT" ? b.Amount : b.Volume;
            int valueCompare = bValue.CompareTo(aValue);
            if (valueCompare != 0)
                return valueCompare;
            return string.Compare(a.Name ?? string.Empty, b.Name ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }
        static void DrawCargoReduxStackedBar(MySpriteDrawFrame frame, CargoPanelSummary summary, string filter, Color storage)
        {
            AddCargoReduxRect(frame, 250f, 360f, 152f, 16f, UiAccentGhost);
            float total = GetCargoMixTotal(summary, filter);
            if (summary == null || total <= 0f)
                return;

            filter = NormalizeCargoInfoSelector(filter);
            if (filter == "ALL")
            {
                string[] categories = new[] { "ORE", "INGOT", "COMP", "TOOLS", "CONSUMABLE", "OTHER" };
                float x = 174f;
                for (int i = 0; i < categories.Length; i++)
                {
                    string category = categories[i];
                    float amount = GetCargoCategoryVolume(summary, category);
                    if (amount <= 0f)
                        continue;
                    float w = Math.Max(1f, 152f * Clamp01(amount / total));
                    if (x + w > 326f)
                        w = 326f - x;
                    if (w <= 0f)
                        break;
                    AddCargoReduxRect(frame, x + w * 0.5f, 360f, w, 16f, GetCargoCategoryColor(category, storage));
                    x += w;
                }
                return;
            }

            var rows = BuildCargoMixRowsForRender(summary, filter);
            float cursor = 174f;
            for (int i = 0; i < rows.Count; i++)
            {
                var item = rows[i];
                if (item == null || item.Volume <= 0f)
                    continue;
                float w = Math.Max(1f, 152f * Clamp01(item.Volume / total));
                if (cursor + w > 326f)
                    w = 326f - cursor;
                if (w <= 0f)
                    break;
                AddCargoReduxRect(frame, cursor + w * 0.5f, 360f, w, 16f, GetCargoMixItemColor(item, i, filter, storage));
                cursor += w;
            }
        }

        static void DrawCargoInfoDrawerReduxActions(MySpriteDrawFrame frame, OverlayBlockInfo selectedInfo, GridSchematicsLcdApp app)
        {
            string mode = app != null && app.Ui != null ? app.Ui.CargoRightPanelMode ?? "TRANSFER" : "TRANSFER";
            if (!string.Equals(mode, "ACTIONS", StringComparison.OrdinalIgnoreCase))
            {
                DrawCargoTransferWidget(frame, app);
                return;
            }

            var actions = BuildCargoActionRows(selectedInfo);
            int first = app != null && app.Ui != null ? app.Ui.CargoActionScrollIndex : 0;
            int visible = 9;
            if (first < 0) first = 0;
            if (first + visible > actions.Count)
                first = Math.Max(0, actions.Count - visible);
            if (app != null && app.Ui != null && app.Ui.CargoActionScrollIndex != first)
                app.Ui.CargoActionScrollIndex = first;

            string hoverId = app != null && app.TouchInput != null ? app.TouchInput.HoverRegionId ?? string.Empty : string.Empty;
            for (int row = 0; row < visible; row++)
            {
                float y = 352.5f + row * 14f;
                int index = first + row;
                if (index >= actions.Count)
                    continue;
                var line = actions[index];
                bool hover = string.Equals(hoverId, UiLayout.CargoInfoActionPrefix + row.ToString(), StringComparison.Ordinal);
                DrawCargoReduxActionRow(frame, line, row, y, hover);
            }

            AddCargoReduxRect(frame, 502f, 418f, 4f, 134f, new Color(128, 128, 128, 7));
            if (actions.Count > visible)
            {
                float ratio = first / (float)Math.Max(1, actions.Count - visible);
                float thumbH = Math.Max(12f, 126f * visible / (float)actions.Count);
                AddCargoReduxRect(frame, 502f, 352f + thumbH * 0.5f + ratio * Math.Max(1f, 126f - thumbH), 6f, thumbH, new Color(192, 192, 192, 95));
                AddCargoReduxTexture(frame, "Triangle", 424.5f, 487.5f, 7f, 7f, UiTextMuted, 3.14159274f);
            }
        }

        static void DrawCargoTransferWidget(MySpriteDrawFrame frame, GridSchematicsLcdApp app)
        {
            Color storage = ResolveStorageSchematicColor();
            string hoverId = app != null && app.TouchInput != null ? app.TouchInput.HoverRegionId ?? string.Empty : string.Empty;
            var source = app != null && app.Ui != null ? app.Ui.CargoTransferSourceItems : null;
            var dest = app != null && app.Ui != null ? app.Ui.CargoTransferDestItems : null;
            var sourceSummary = BuildCargoTransferSelectionSummary(source, app != null && app.Ui != null ? app.Ui.CargoTransferSourceLabel : string.Empty);
            var destSummary = BuildCargoTransferSelectionSummary(dest, app != null && app.Ui != null ? app.Ui.CargoTransferDestLabel : string.Empty);
            float quotaAmount = GetCargoTransferQuotaAmount(app);
            float destFree = Math.Max(0f, destSummary.MaxVolume - destSummary.CurrentVolume);
            float destQuotaRatio = destFree > 0f ? quotaAmount / destFree : (quotaAmount > 0f ? 2f : 0f);
            bool overflow = destQuotaRatio > 1f;

            DrawCargoTransferSelectionBar(frame, app, true, sourceSummary, storage);
            DrawCargoTransferSelectionBar(frame, app, false, destSummary, ResolveSecondarySchematicColor());
            if (destSummary.MaxVolume > 0f && quotaAmount > 0f)
            {
                float projectedRatio = Clamp01((destSummary.CurrentVolume + quotaAmount) / destSummary.MaxVolume);
                Color projectedColor = overflow ? new Color(UiWarning.R, UiWarning.G, UiWarning.B, 35) : new Color(UiSelected.R, UiSelected.G, UiSelected.B, 56);
                AddCargoReduxRect(frame, 398.344879f + 64f * projectedRatio * 0.5f, 382.0388f, Math.Max(1f, 64f * projectedRatio), 8f, projectedColor);
            }

            DrawCargoTransferQuotaChrome(frame, storage, overflow ? UiWarning : UiSelected);
            AddCargoReduxText(frame, "VOL: " + FormatCargoCompactVolume(quotaAmount), 375f, 363f, storage, 0.25f, TextAlignment.LEFT);
            AddCargoReduxText(frame, "M: " + FormatCargoCompactMass(quotaAmount), 429f, 363f, storage, 0.25f, TextAlignment.LEFT);
            Color dirColor = storage;
            AddCargoReduxTexture(frame, "Triangle", 472f, 369f, 8f, 8f, dirColor, 3.1416f);
            AddCargoReduxText(frame, "DIR", 485f, 363f, dirColor, 0.25f, TextAlignment.LEFT);

            AddCargoReduxText(frame, overflow ? FormatCargoCompactVolume(quotaAmount) : ((int)Math.Round(destQuotaRatio * 100f)).ToString() + "%", 403f, 390f, overflow ? UiWarning : UiSelected, 0.25f, TextAlignment.LEFT);
            AddCargoReduxText(frame, "/ " + FormatCargoCompactVolume(destFree), 429f, 390f, overflow || destFree <= 0f ? UiWarning : UiSelected, 0.25f, TextAlignment.LEFT);

            AddCargoReduxText(frame, "ITEM", 351f, 404f, UiTextMuted, 0.25f, TextAlignment.LEFT);
            AddCargoReduxText(frame, "VOLUME", 403f, 404f, UiTextMuted, 0.25f, TextAlignment.CENTER);
            AddCargoReduxText(frame, "MASS", 436f, 404f, UiTextMuted, 0.25f, TextAlignment.CENTER);
            AddCargoReduxText(frame, "ADJUST", 474f, 404f, UiTextMuted, 0.25f, TextAlignment.CENTER);

            DrawCargoTransferTableChrome(frame);
            DrawCargoTransferQuotaRows(frame, app);
            AddCargoReduxText(frame, "<< CLEAR ALL", 352f, 483f, CargoSelectableTextColor(app, UiLayout.CargoInfoTransferClearId, false), 0.25f, TextAlignment.LEFT);
            Color transferNow = ResolveSecondarySchematicColor();
            if (app == null || app.TouchInput == null || !string.Equals(app.TouchInput.HoverRegionId ?? string.Empty, UiLayout.CargoInfoTransferNowId, StringComparison.Ordinal))
                transferNow = new Color(transferNow.R, transferNow.G, transferNow.B, 150);
            AddCargoReduxText(frame, "TRANSFER NOW >>", 460f, 483f, transferNow, 0.25f, TextAlignment.CENTER);
        }

        static void DrawCargoReduxActionRow(MySpriteDrawFrame frame, OverlayBlockInfoLine line, int row, float y, bool hover)
        {
            float centerY = y + 7f;
            Color baseFill = new Color(192, 192, 192, 3);
            Color baseBorder = new Color(255, 255, 255, 5);
            if (hover)
            {
                baseFill = new Color(UiSelected.R, UiSelected.G, UiSelected.B, 34);
                baseBorder = UiSelected;
            }

            AddCargoReduxRect(frame, 421.5f, centerY, 141f, 15f, baseFill);
            DrawCargoReduxBorder(frame, 421.5f, centerY, 141f, 15f, baseBorder);

            string label;
            string state;
            SplitCargoActionDisplay(line, out label, out state);
            bool on = IsCargoActionStateOn(state);
            bool command = string.Equals(state, "DO", StringComparison.Ordinal);
            Color labelColor = hover ? UiSelected : UiTextMuted;
            Color stateColor = on ? UiSelected : (hover ? UiText : UiTextMuted);

            AddCargoReduxText(frame, ShortenCargoActionDescription(label), 355f, y, labelColor, 0.30f, TextAlignment.LEFT);
            AddCargoReduxText(frame, state, 464f, y, stateColor, 0.30f, TextAlignment.RIGHT);
            DrawCargoReduxActionSwitch(frame, 479f, centerY, on, command, hover);
        }

        static void DrawCargoReduxActionSwitch(MySpriteDrawFrame frame, float x, float y, bool on, bool command, bool hover)
        {
            Color rail = hover ? new Color(UiSelected.R, UiSelected.G, UiSelected.B, 55) : new Color(192, 192, 192, 28);
            Color knob = on ? new Color(UiSelected.R, UiSelected.G, UiSelected.B, 145) : new Color(192, 192, 192, command ? 58 : 95);
            AddCargoReduxRect(frame, x, y, 16f, 7f, rail);
            if (command)
            {
                AddCargoReduxTexture(frame, "Triangle", x + 4f, y, 6f, 6f, hover ? UiSelected : UiTextMuted, 1.57079637f);
                return;
            }

            AddCargoReduxRect(frame, on ? x + 4f : x - 4f, y, 8f, 7f, knob);
        }

        static bool IsCargoActionStateOn(string state)
        {
            if (string.IsNullOrWhiteSpace(state))
                return false;
            state = state.Trim().ToUpperInvariant();
            return state == "ON" || state == "YES" || state == "TRUE" || (state != "OFF" && state != "DO" && state != "NO" && state != "FALSE");
        }

        static void SplitCargoActionDisplay(OverlayBlockInfoLine line, out string label, out string state)
        {
            label = CargoActionLineLabel(line);
            state = CargoActionStateLabel(line);
            if (string.IsNullOrWhiteSpace(label))
                label = "ACTION";
            label = label.Trim();

            string parsedState;
            string parsedLabel;
            if (TrySplitCargoActionTrailingValue(label, out parsedLabel, out parsedState))
            {
                label = parsedLabel;
                state = parsedState;
            }

            if (label.StartsWith("STATE:", StringComparison.OrdinalIgnoreCase))
                label = "ENABLED";
            if (label.StartsWith("ACTION:", StringComparison.OrdinalIgnoreCase))
                label = label.Substring(7).Trim();
            if (label.EndsWith(" ON/OFF", StringComparison.OrdinalIgnoreCase))
                label = label.Substring(0, label.Length - 7).Trim();
            if (label.EndsWith(" ONOFF", StringComparison.OrdinalIgnoreCase))
                label = label.Substring(0, label.Length - 6).Trim();
            if (string.IsNullOrWhiteSpace(state))
                state = "DO";
            label = label.ToUpperInvariant();
            state = state.ToUpperInvariant();
        }

        static bool TrySplitCargoActionTrailingValue(string text, out string label, out string state)
        {
            label = text;
            state = string.Empty;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            int split = text.LastIndexOf(' ');
            if (split <= 0 || split >= text.Length - 1)
                return false;

            string candidate = text.Substring(split + 1).Trim();
            string upper = candidate.ToUpperInvariant();
            if (upper == "ON" || upper == "OFF" || upper == "DO" || upper == "YES" || upper == "NO" || upper == "TRUE" || upper == "FALSE" || upper == "ORE" || upper == "INGOT" || upper == "COMP" || upper == "ALL")
            {
                label = text.Substring(0, split).Trim();
                state = upper;
                return true;
            }

            return false;
        }

        static string ShortenCargoActionDescription(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "ACTION";
            text = text.Trim().ToUpperInvariant();
            return text.Length <= 12 ? text : text.Substring(0, 11) + ".";
        }
        static void ConfigureCargoReduxLayout(ScreenZone zone, GridSchematicsLcdApp app)
        {
            CurrentCargoReduxUsesPanelTransform = false;
            CurrentCargoReduxScale = 1f;
            CurrentCargoReduxPanelX = 0f;
            CurrentCargoReduxPanelY = 0f;

            if (app == null || app.Surface == null)
                return;

            var surfaceSize = app.Surface.SurfaceSize;
            var profile = UiLayout.BuildSurfaceProfile((int)surfaceSize.X, (int)surfaceSize.Y);
            if (!profile.IsCanonical1024Square)
                return;

            CurrentCargoReduxUsesPanelTransform = true;
            CurrentCargoReduxScale = 2f;
            CurrentCargoReduxPanelX = zone.X;
            CurrentCargoReduxPanelY = zone.Y;
        }

        static Vector2 CargoReduxPosition(float x, float y)
        {
            if (!CurrentCargoReduxUsesPanelTransform)
                return new Vector2(x, y);
            return new Vector2(CurrentCargoReduxPanelX + x * CurrentCargoReduxScale, CurrentCargoReduxPanelY + (y - CargoReduxCanonicalPanelY) * CurrentCargoReduxScale);
        }

        static Vector2 CargoReduxSize(float w, float h)
        {
            if (!CurrentCargoReduxUsesPanelTransform)
                return new Vector2(Math.Max(1f, w), Math.Max(1f, h));
            return new Vector2(Math.Max(1f, w * CurrentCargoReduxScale), Math.Max(1f, h * CurrentCargoReduxScale));
        }

        static float CargoReduxTextScale(float scale)
        {
            return CurrentCargoReduxUsesPanelTransform ? scale * CurrentCargoReduxScale : scale;
        }

        static void AddCargoReduxRect(MySpriteDrawFrame frame, float x, float y, float w, float h, Color color)
        {
            AddCargoReduxRect(frame, x, y, w, h, color, 0f);
        }

        static void AddCargoReduxRect(MySpriteDrawFrame frame, float x, float y, float w, float h, Color color, float rotation)
        {
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", CargoReduxPosition(x, y), CargoReduxSize(w, h), color, null, TextAlignment.CENTER, rotation));
        }

        static void AddCargoReduxTexture(MySpriteDrawFrame frame, string texture, float x, float y, float w, float h, Color color, float rotation)
        {
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, texture, CargoReduxPosition(x, y), CargoReduxSize(w, h), color, null, TextAlignment.CENTER, rotation));
        }

        static void DrawCargoReduxBorder(MySpriteDrawFrame frame, float x, float y, float w, float h, Color color)
        {
            DrawScreenRectBorder(frame, CargoReduxPosition(x, y), CargoReduxSize(w, h), color, CurrentCargoReduxUsesPanelTransform ? CurrentCargoReduxScale : 1f);
        }

        static void AddCargoReduxText(MySpriteDrawFrame frame, string text, float x, float y, Color color, float scale, TextAlignment alignment)
        {
            AddSprite(frame, new MySprite(SpriteType.TEXT, text ?? string.Empty, CargoReduxPosition(x, y), null, color, InfoDrawerTextFontId, alignment, CargoReduxTextScale(scale)));
        }

        static Color CargoSelectableTextColor(GridSchematicsLcdApp app, string regionId, bool selected)
        {
            string hoverId = app != null && app.TouchInput != null ? app.TouchInput.HoverRegionId ?? string.Empty : string.Empty;
            bool hover = string.Equals(hoverId, regionId, StringComparison.Ordinal);
            if (selected)
                return hover ? UiSelected : new Color(UiSelected.R, UiSelected.G, UiSelected.B, 150);
            return hover ? UiText : UiTextMuted;
        }

        static void DrawCargoRightHeaderText(MySpriteDrawFrame frame, string text, float x, string regionId, bool active, bool brightWhenInactive, string hoverId)
        {
            bool hover = string.Equals(hoverId, regionId, StringComparison.Ordinal);
            Color color = active ? (hover ? UiSelected : new Color(UiSelected.R, UiSelected.G, UiSelected.B, 155)) : brightWhenInactive ? UiText : (hover ? UiText : UiTextMuted);
            AddCargoReduxText(frame, text, x, 331f, color, 0.30f, TextAlignment.LEFT);
        }

        static List<CargoPanelItem> BuildCargoMixRowsForRender(CargoPanelSummary summary, string filter)
        {
            var rows = new List<CargoPanelItem>();
            if (summary == null || summary.TopItems == null)
                return rows;
            filter = NormalizeCargoInfoSelector(filter);
            if (filter == "ALL")
            {
                AddCargoCategoryRows(summary, rows);
                return rows;
            }
            for (int i = 0; i < summary.TopItems.Count; i++)
            {
                var item = summary.TopItems[i];
                if (item == null)
                    continue;
                if (CategoryMatchesCargoFilter(item.Category, filter))
                    rows.Add(item);
            }
            return rows;
        }

        static void AddCargoCategoryRows(CargoPanelSummary summary, List<CargoPanelItem> rows)
        {
            if (summary == null || rows == null)
                return;
            string[] categories = new[] { "ORE", "INGOT", "COMP", "TOOLS", "CONSUMABLE", "OTHER" };
            for (int i = 0; i < categories.Length; i++)
            {
                string category = categories[i];
                float volume = GetCargoCategoryVolume(summary, category);
                if (volume <= 0f)
                    continue;
                float amount = 0f;
                float mass = 0f;
                if (summary.TopItems != null)
                {
                    for (int j = 0; j < summary.TopItems.Count; j++)
                    {
                        var item = summary.TopItems[j];
                        if (item == null || !string.Equals(NormalizeCargoInfoSelector(item.Category), category, StringComparison.Ordinal))
                            continue;
                        amount += item.Amount;
                        mass += item.Mass;
                    }
                }
                rows.Add(new CargoPanelItem
                {
                    Key = "category/" + category,
                    Name = CargoCategoryRowLabel(category),
                    Category = category,
                    TypeId = "Category",
                    SubtypeId = category,
                    Amount = amount,
                    Volume = volume,
                    Mass = mass
                });
            }
        }

        static string CargoCategoryRowLabel(string category)
        {
            category = NormalizeCargoInfoSelector(category);
            if (category == "ORE") return "ORES";
            if (category == "INGOT") return "INGOTS";
            if (category == "COMP") return "COMP.";
            if (category == "CONSUMABLE") return "CONSUM.";
            if (category == "TOOLS") return "TOOLS";
            return "OTHER";
        }
        static string CargoPanelItemKey(CargoPanelItem item)
        {
            if (item == null)
                return string.Empty;
            if (!string.IsNullOrEmpty(item.Key))
                return item.Key;
            return (item.TypeId ?? string.Empty) + "/" + (item.SubtypeId ?? item.Name ?? string.Empty);
        }

        static CargoPanelBlock GetCurrentCargoCursorBlock(CargoPanelSummary summary, GridSchematicsLcdApp app)
        {
            if (summary == null || summary.Blocks == null || summary.Blocks.Count == 0 || app == null || app.Ui == null)
                return null;
            int index = app.Ui.CargoBlockScrollIndex + app.Ui.CargoBlockCursorIndex;
            if (index < 0 || index >= summary.Blocks.Count)
                return null;
            return summary.Blocks[index];
        }

        static string CurrentCargoCursorBlockLabel(CargoPanelSummary summary, GridSchematicsLcdApp app)
        {
            var block = GetCurrentCargoCursorBlock(summary, app);
            if (block == null || block.Block == null)
                return "SELECT";
            return ShortenTransferSelectionLabel(block.Block.DisplayNameText, block.Block.DefinitionDisplayNameText, 4);
        }

        static string ShortenTransferSelectionLabel(string name, string fallback, int max)
        {
            if (string.IsNullOrWhiteSpace(name))
                name = fallback;
            return AbbreviateCargoBlockLabel(name, max);
        }

        static string AbbreviateCargoBlockLabel(string text, int max)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "BLOCK";
            text = text.Trim().ToUpperInvariant();
            var acronym = new System.Text.StringBuilder(max > 0 ? max : 8);
            bool emittedLetterForToken = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (char.IsLetter(c))
                {
                    if (!emittedLetterForToken)
                    {
                        acronym.Append(char.ToUpperInvariant(c));
                        emittedLetterForToken = true;
                    }
                }
                else if (char.IsDigit(c))
                {
                    acronym.Append(c);
                }
                else
                {
                    emittedLetterForToken = false;
                }
                if (max > 0 && acronym.Length >= max)
                    break;
            }
            if (acronym.Length == 0)
                return "BLOCK";
            return acronym.ToString();
        }

        static float GetCargoBlockCurrentVolume(IMyCubeBlock block)
        {
            if (block == null)
                return 0f;
            float total = 0f;
            try
            {
                for (int i = 0; i < block.InventoryCount; i++)
                {
                    var inv = block.GetInventory(i);
                    if (inv != null)
                        total += (float)inv.CurrentVolume;
                }
            }
            catch
            {
            }
            return total;
        }

        static float GetCargoBlockMaxVolume(IMyCubeBlock block)
        {
            if (block == null)
                return 0f;
            float total = 0f;
            try
            {
                for (int i = 0; i < block.InventoryCount; i++)
                {
                    var inv = block.GetInventory(i);
                    if (inv != null)
                        total += (float)inv.MaxVolume;
                }
            }
            catch
            {
            }
            return total;
        }

        class CargoTransferSelectionSummary
        {
            public string Label = "SELECT >";
            public float CurrentVolume;
            public float MaxVolume;
        }

        static CargoTransferSelectionSummary BuildCargoTransferSelectionSummary(List<BlockStackItem> items, string labelOverride)
        {
            var summary = new CargoTransferSelectionSummary();
            if (items == null || items.Count == 0)
                return summary;
            summary.Label = !string.IsNullOrEmpty(labelOverride)
                ? labelOverride
                : items.Count == 1 && items[0] != null && items[0].Block != null
                    ? ShortenTransferSelectionLabel(items[0].Name, items[0].Block.DefinitionDisplayNameText, 5)
                    : (items.Count > 1 ? "GROUP" : "SELECT >");
            for (int i = 0; i < items.Count; i++)
            {
                var block = items[i] != null ? items[i].Block : null;
                summary.CurrentVolume += GetCargoBlockCurrentVolume(block);
                summary.MaxVolume += GetCargoBlockMaxVolume(block);
            }
            return summary;
        }

        static void DrawCargoTransferSelectionBar(MySpriteDrawFrame frame, GridSchematicsLcdApp app, bool source, CargoTransferSelectionSummary summary, Color color)
        {
            string target = source ? "SOURCE" : "DEST";
            string region = source ? UiLayout.CargoInfoTransferSourceSelectId : UiLayout.CargoInfoTransferDestSelectId;
            float y = source ? 354.0388f : 382.0388f;
            float textY = source ? 349f : 377f;
            bool capture = app != null && app.Ui != null && string.Equals(app.Ui.CargoTransferCaptureTarget, target, StringComparison.Ordinal);
            bool activeView = app != null && app.Ui != null && string.Equals(app.Ui.CargoTransferMixViewTarget ?? "SOURCE", target, StringComparison.Ordinal);
            Color labelColor = capture && ((CacheUseCounter / 20) % 2 == 0) ? UiText : color;
            Color hoverColor = CargoSelectableTextColor(app, region, capture);
            if (!capture)
                labelColor = hoverColor;
            string label = capture ? "SELECT" : summary.Label;
            if (activeView)
                AddCargoReduxRect(frame, 430.344879f, y, 68f, 12f, new Color(color.R, color.G, color.B, 22));
            AddCargoReduxText(frame, label, 394f, textY, labelColor, 0.275f, TextAlignment.RIGHT);
            AddCargoReduxText(frame, FormatCargoCompactVolume(summary.CurrentVolume), 476.8f, textY, capture ? UiText : color, 0.25f, TextAlignment.LEFT);
            float ratio = summary.MaxVolume > 0f ? Clamp01(summary.CurrentVolume / summary.MaxVolume) : 0f;
            AddCargoReduxRect(frame, 430.344879f, y, 64f, 9f, new Color(color.R, color.G, color.B, 15));
            AddCargoReduxRect(frame, 398.344879f, y, 1f, 9f, color);
            AddCargoReduxRect(frame, 462.344879f, y, 1f, 9f, color);
            AddCargoReduxRect(frame, 430.344879f, y - 4.5f, 64f, 1f, color);
            AddCargoReduxRect(frame, 430.344879f, y + 4.5f, 64f, 1f, color);
            if (ratio > 0f)
                AddCargoReduxRect(frame, 398.344879f + 64f * ratio * 0.5f, y, Math.Max(1f, 64f * ratio), 8f, color);
        }

        static float GetCargoTransferQuotaAmount(GridSchematicsLcdApp app)
        {
            if (app == null || app.Ui == null || app.Ui.CargoTransferQuotaItems == null)
                return 0f;
            float total = 0f;
            for (int i = 0; i < app.Ui.CargoTransferQuotaItems.Count; i++)
            {
                var item = app.Ui.CargoTransferQuotaItems[i];
                if (item != null)
                    total += Math.Max(0f, item.Amount);
            }
            return total;
        }

        static void DrawCargoTransferQuotaChrome(MySpriteDrawFrame frame, Color sourceColor, Color destColor)
        {
            Color sourceDim = new Color(sourceColor.R, sourceColor.G, sourceColor.B, 55);
            Color destDim = new Color(destColor.R, destColor.G, destColor.B, 56);
            AddCargoReduxRect(frame, 437.344879f, 362.5388f, 133f, 1f, sourceDim);
            AddCargoReduxRect(frame, 437.5f, 374.5f, 133f, 1f, sourceDim);
            AddCargoReduxRect(frame, 371.5f, 368f, 1f, 12f, sourceDim);
            AddCargoReduxRect(frame, 503.5f, 368f, 1f, 12f, sourceDim);
            AddCargoReduxRect(frame, 467.5f, 382f, 10f, 1f, sourceDim);
            AddCargoReduxRect(frame, 430.5f, 390f, 65f, 1f, destDim);
            AddCargoReduxRect(frame, 430.344879f, 400.5388f, 65f, 1f, destDim);
            AddCargoReduxRect(frame, 398.344879f, 395.5388f, 1f, 11f, destDim);
            AddCargoReduxRect(frame, 462.344879f, 395.0388f, 1f, 10f, destDim);
        }

        static void DrawCargoTransferTableChrome(MySpriteDrawFrame frame)
        {
            Color line = new Color(192, 192, 192, 18);
            AddCargoReduxRect(frame, 422.5f, 416.5f, 145f, 1f, line);
            AddCargoReduxRect(frame, 422.5f, 432.5f, 145f, 1f, line);
            AddCargoReduxRect(frame, 422.5f, 448.5f, 145f, 1f, line);
            AddCargoReduxRect(frame, 422.5f, 464.5f, 145f, 1f, line);
            AddCargoReduxRect(frame, 422.5f, 480.5f, 145f, 1f, line);
            AddCargoReduxRect(frame, 384f, 448.5f, 64f, 1f, line, 1.57079637f);
            AddCargoReduxRect(frame, 422f, 448.5f, 64f, 1f, line, 1.57079637f);
            AddCargoReduxRect(frame, 451.5f, 448.5f, 63f, 1f, line, 1.57079637f);
            AddCargoReduxRect(frame, 466.5f, 448.5f, 63f, 1f, line, 1.57079637f);
            AddCargoReduxRect(frame, 480.5f, 448.5f, 63f, 1f, line, 1.57079637f);
            AddCargoReduxRect(frame, 494.5f, 448.5f, 63f, 1f, line, 1.57079637f);
        }
        static void DrawCargoTransferQuotaRows(MySpriteDrawFrame frame, GridSchematicsLcdApp app)
        {
            if (app == null || app.Ui == null || app.Ui.CargoTransferQuotaItems == null)
                return;
            int first = app.Ui.CargoTransferQuotaScrollIndex;
            if (first < 0) first = 0;
            int visible = 4;
            if (first + visible > app.Ui.CargoTransferQuotaItems.Count)
                first = Math.Max(0, app.Ui.CargoTransferQuotaItems.Count - visible);
            app.Ui.CargoTransferQuotaScrollIndex = first;
            string hoverId = app.TouchInput != null ? app.TouchInput.HoverRegionId ?? string.Empty : string.Empty;
            for (int row = 0; row < visible; row++)
            {
                int index = first + row;
                if (index >= app.Ui.CargoTransferQuotaItems.Count)
                    continue;
                var item = app.Ui.CargoTransferQuotaItems[index];
                float y = 420f + row * 16f;
                bool hover = hoverId.StartsWith(UiLayout.CargoInfoTransferQuotaPrefix + row.ToString() + ":", StringComparison.Ordinal);
                Color text = hover ? UiText : UiTextMuted;
                AddCargoReduxText(frame, ShortenCargoMixItemName(item.Name), 351f, y, text, 0.25f, TextAlignment.LEFT);
                AddCargoReduxText(frame, FormatCargoCompactVolume(item.Amount), 403f, y, text, 0.25f, TextAlignment.CENTER);
                AddCargoReduxText(frame, FormatCargoCompactMass(item.Amount), 437f, y, text, 0.25f, TextAlignment.CENTER);
                AddCargoReduxTexture(frame, "Triangle", 459f, y + 4f, 8f, 8f, text, 6.28318548f);
                AddCargoReduxTexture(frame, "Triangle", 473f, y + 5f, 8f, 8f, text, 3.14159274f);
                DrawCargoReduxX(frame, 487.5f, y + 5f, text);
            }
            AddCargoReduxRect(frame, 503f, 448.5f, 4f, 63f, new Color(128, 128, 128, 7));
            if (app.Ui.CargoTransferQuotaItems.Count > visible)
            {
                float thumbH = Math.Max(12f, 63f * visible / (float)app.Ui.CargoTransferQuotaItems.Count);
                float ratio = first / (float)Math.Max(1, app.Ui.CargoTransferQuotaItems.Count - visible);
                AddCargoReduxRect(frame, 503f, 416f + thumbH * 0.5f + ratio * Math.Max(1f, 63f - thumbH), 6f, thumbH, new Color(192, 192, 192, 95));
            }
        }

        static void DrawCargoReduxX(MySpriteDrawFrame frame, float x, float y, Color color)
        {
            AddCargoReduxRect(frame, x, y, 1f, 10f, color, 0.7853982f);
            AddCargoReduxRect(frame, x, y, 1f, 10f, color, 2.3561945f);
        }
        static string FormatCargoCompactAmount(float amount)
        {
            return FormatCargoCompactNumber(amount, string.Empty, "k", "M", "G");
        }

        static string FormatCargoCompactVolume(float volume)
        {
            return FormatCargoCompactNumber(volume * 1000f, "L", "kL", "ML", "GL");
        }

        static string FormatCargoCompactMass(float amount)
        {
            return FormatCargoCompactNumber(amount, "t", "kt", "Mt", "Gt");
        }

        static string FormatCargoCompactNumber(float value, string unit0, string unit1, string unit2, string unit3)
        {
            float abs = Math.Abs(value);
            string unit = unit0;
            if (abs >= 1000000000f)
            {
                value /= 1000000000f;
                unit = unit3;
            }
            else if (abs >= 1000000f)
            {
                value /= 1000000f;
                unit = unit2;
            }
            else if (abs >= 1000f)
            {
                value /= 1000f;
                unit = unit1;
            }

            return ((int)Math.Round(value)).ToString() + unit;
        }
        static string FormatCargoMass(float amount)
        {
            if (amount >= 1000f)
                return (amount / 1000f).ToString("0.#") + " kt";
            return ((int)Math.Round(amount)).ToString() + " t";
        }

        static float GetIsolatedCargoVolume(CargoPanelSummary summary)
        {
            if (summary == null || summary.Blocks == null || summary.Blocks.Count == 0 || summary.MaxVolume <= 0f)
                return 0f;
            return summary.CurrentVolume * Clamp01(summary.IsolatedCount / (float)Math.Max(1, summary.Blocks.Count));
        }

        static float GetOfflineCargoVolume(CargoPanelSummary summary)
        {
            if (summary == null || summary.Blocks == null || summary.Blocks.Count == 0 || summary.MaxVolume <= 0f)
                return 0f;
            return summary.CurrentVolume * Clamp01(summary.OfflineCount / (float)Math.Max(1, summary.Blocks.Count));
        }

        static string CargoActionStateLabel(OverlayBlockInfoLine line)
        {
            if (line == null)
                return string.Empty;
            try
            {
                if (line.ToggleBlock != null)
                    return line.ToggleBlock.Enabled ? "ON" : "OFF";
                if (line.BatteryBlock != null)
                    return line.BatteryBlock.Enabled ? "ON" : "OFF";
                if (line.TerminalBlock != null && line.TerminalAction != null)
                    return "DO";
                if (line.TerminalBlocks != null && line.TerminalBlocks.Count > 0)
                    return "DO";
            }
            catch
            {
            }
            return string.Empty;
        }

        static float EstimateCargoTwrLimitRatio(GridSchematicsLcdApp app, CargoPanelSummary summary)
        {
            float fallback = 0.80f;
            if (summary == null || summary.MaxVolume <= 0f || app == null || app.OwnerBlock == null)
                return fallback;

            try
            {
                var root = app.OwnerBlock.CubeGrid;
                if (root == null || MyAPIGateway.GridGroups == null)
                    return fallback;

                var grids = new List<IMyCubeGrid>();
                MyAPIGateway.GridGroups.GetGroup(root, GridLinkTypeEnum.Physical, grids);
                if (grids.Count == 0)
                    grids.Add(root);

                float upwardThrust = 0f;
                float physicalMass = 0f;
                var slimBlocks = new List<IMySlimBlock>();
                for (int g = 0; g < grids.Count; g++)
                {
                    var grid = grids[g];
                    if (grid == null)
                        continue;
                    slimBlocks.Clear();
                    grid.GetBlocks(slimBlocks);
                    for (int i = 0; i < slimBlocks.Count; i++)
                    {
                        var fat = slimBlocks[i] != null ? slimBlocks[i].FatBlock : null;
                        if (fat == null)
                            continue;
                        physicalMass += Math.Max(0f, slimBlocks[i].Mass);
                        var thrust = fat as Sandbox.ModAPI.Ingame.IMyThrust;
                        if (thrust != null && thrust.IsFunctional && thrust.Enabled)
                            upwardThrust += Math.Max(0f, thrust.MaxEffectiveThrust);
                    }
                }

                if (upwardThrust <= 0f || physicalMass <= 0f)
                    return fallback;

                float targetMass = upwardThrust / (1.1f * 9.81f);
                float cargoMassCapacity = Math.Max(1f, summary.MaxVolume * 1000f);
                return (targetMass - physicalMass) / cargoMassCapacity;
            }
            catch
            {
                return fallback;
            }
        }
        static void DrawCargoLoadStateWidget(MySpriteDrawFrame frame, CargoWidgetRect widget, CargoPanelSummary summary, Color storage)
        {
            var dm = BuildCargoDrawerMetrics(widget);
            float x = widget.X + dm.Pad;
            float w = Math.Max(16f, widget.Width - dm.Pad * 2f);
            float y = widget.Y + dm.HeaderHeight + dm.Gap;
            Color loadColor = GetCargoFillColor(storage, summary.FillRatio);
            string used = FormatCargoCompactVolume(summary.CurrentVolume);
            string max = FormatCargoCompactVolume(summary.MaxVolume);
            string free = FormatCargoCompactVolume(Math.Max(0f, summary.MaxVolume - summary.CurrentVolume));

            AddSprite(frame, new MySprite(SpriteType.TEXT, used + "/" + max, new Vector2(x, y), null, loadColor, InfoDrawerTextFontId, TextAlignment.LEFT, dm.TinyText));
            AddSprite(frame, new MySprite(SpriteType.TEXT, "FREE " + free, new Vector2(widget.X + widget.Width - dm.Pad, y), null, UiTextMuted, InfoDrawerTextFontId, TextAlignment.RIGHT, dm.TinyText));

            y += dm.RowHeight + dm.BarHeight * 0.55f;
            DrawInfoBarGlyph(frame, new Vector2(x + w * 0.5f, y), w, dm.BarHeight, summary.FillRatio, loadColor, UiAccentGhost);
            DrawCargoThresholdMarker(frame, x + w * 0.80f, y, dm.BarHeight, "80%", CargoCautionColor());
            DrawCargoThresholdMarker(frame, x + w * 0.95f, y, dm.BarHeight, "95%", UiWarning);

            y += dm.BarHeight * 0.5f + dm.Gap + dm.MicroBarHeight * 0.5f;
            float freeRatio = summary.MaxVolume > 0f ? Clamp01((summary.MaxVolume - summary.CurrentVolume) / summary.MaxVolume) : 0f;
            DrawInfoBarGlyph(frame, new Vector2(x + w * 0.5f, y), w, dm.MicroBarHeight, freeRatio, ScaleColor(storage, 0.72f, 210), UiAccentGhost);

            y += dm.MicroBarHeight * 0.5f + dm.Gap + dm.RowHeight * 0.30f;
            DrawCargoReachabilityMini(frame, new Vector2(x, y), w, summary, dm);

            float stripTop = widget.Y + widget.Height * 0.58f;
            float stripBottom = widget.Y + widget.Height - dm.Pad;
            if (stripTop < y + dm.RowHeight + dm.Gap)
                stripTop = y + dm.RowHeight + dm.Gap;
            float stripHeight = stripBottom - stripTop;
            if (stripHeight >= 16f)
            {
                AddSprite(frame, new MySprite(SpriteType.TEXT, "SATURATION", new Vector2(x, stripTop), null, UiTextMuted, InfoDrawerTextFontId, TextAlignment.LEFT, dm.TinyText));
                DrawCargoSaturationStrip(frame, new Vector2(x, stripTop + dm.RowHeight * 0.72f), w, stripHeight - dm.RowHeight * 0.72f, summary);
            }
        }

        static void DrawCargoMixBarWidget(MySpriteDrawFrame frame, CargoWidgetRect widget, CargoPanelSummary summary, Color storage, GridSchematicsLcdApp app)
        {
            var dm = BuildCargoDrawerMetrics(widget);
            float x = widget.X + dm.Pad;
            float w = Math.Max(16f, widget.Width - dm.Pad * 2f);
            float y = widget.Y + dm.HeaderHeight + dm.Gap;
            string filter = NormalizeCargoInfoSelector(summary.Filter);
            string focus = NormalizeCargoInfoSelector(summary.Focus);
            AddSprite(frame, new MySprite(SpriteType.TEXT, "FOCUS: " + focus, new Vector2(x, y), null, UiSelected, InfoDrawerTextFontId, TextAlignment.LEFT, dm.TinyText));

            float barTop = y + dm.RowHeight;
            DrawCargoStackedBarFiltered(frame, new Vector2(x, barTop), new Vector2(w, dm.BarHeight), summary, filter, storage);
            DrawCargoFilterDropdown(frame, widget, dm, filter, app != null && app.Ui != null && app.Ui.CargoFilterDropdownOpen);
            float total = GetCargoMixTotal(summary, filter);
            y = barTop + dm.BarHeight + dm.Gap;

            int shown = 0;
            int hidden = 0;
            int maxRows = Math.Max(2, (int)((widget.Y + widget.Height - y - dm.Gap) / dm.RowHeight));
            if (maxRows > 6)
                maxRows = 6;
            for (int i = 0; i < summary.TopItems.Count; i++)
            {
                var item = summary.TopItems[i];
                if (item == null || !CategoryMatchesCargoFilter(item.Category, filter))
                    continue;
                if (shown >= maxRows)
                {
                    hidden++;
                    continue;
                }

                float rowY = y + shown * dm.RowHeight;
                float pct = total > 0f ? Clamp01(item.Volume / total) : 0f;
                Color color = GetCargoMixItemColor(item, shown, filter, storage);
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(x + dm.Gap, rowY + dm.RowHeight * 0.38f), new Vector2(Math.Max(3f, dm.Gap * 1.4f), Math.Max(3f, dm.Gap * 1.4f)), color));
                AddSprite(frame, new MySprite(SpriteType.TEXT, ShortenCargoName(item.Name, widget.Width * 0.38f), new Vector2(x + dm.Gap * 3.1f, rowY), null, color, InfoDrawerTextFontId, TextAlignment.LEFT, dm.SmallText));
                AddSprite(frame, new MySprite(SpriteType.TEXT, ((int)Math.Round(pct * 100f)).ToString() + "%", new Vector2(widget.X + widget.Width - dm.Pad - w * 0.17f, rowY), null, color, InfoDrawerTextFontId, TextAlignment.RIGHT, dm.SmallText));
                AddSprite(frame, new MySprite(SpriteType.TEXT, FormatCargoAmount(item.Volume), new Vector2(widget.X + widget.Width - dm.Pad, rowY), null, UiTextMuted, InfoDrawerTextFontId, TextAlignment.RIGHT, dm.TinyText));
                shown++;
            }

            if (hidden > 0)
                AddSprite(frame, new MySprite(SpriteType.TEXT, "+" + hidden.ToString() + " MORE", new Vector2(widget.X + widget.Width - dm.Pad, widget.Y + widget.Height - dm.RowHeight), null, UiSelected, InfoDrawerTextFontId, TextAlignment.RIGHT, dm.SmallText));
        }


        static void DrawCargoFilterDropdown(MySpriteDrawFrame frame, CargoWidgetRect widget, CargoDrawerMetrics dm, string activeFilter, bool open)
        {
            if (!open)
                return;

            string[] options = new[] { "ORE", "INGOT", "COMPONENTS", "TOOLS", "CONSUMABLE" };
            float width = Math.Max(widget.Width * 0.38f, dm.HeaderHeight * 5f);
            if (width > widget.Width - dm.Pad * 2f)
                width = widget.Width - dm.Pad * 2f;
            float x = widget.X + widget.Width - dm.Pad - width;
            float y = widget.Y + dm.HeaderHeight;
            for (int i = 0; i < options.Length; i++)
            {
                string option = options[i];
                bool active = NormalizeCargoInfoSelector(option) == activeFilter;
                float rowY = y + dm.RowHeight * i;
                var center = new Vector2(x + width * 0.5f, rowY + dm.RowHeight * 0.5f);
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, new Vector2(width, dm.RowHeight), active ? UiMenuButtonActive : UiPanelFill));
                DrawScreenRectBorder(frame, center, new Vector2(width, dm.RowHeight), active ? UiSelected : UiAccentDim, dm.Line);
                AddSprite(frame, new MySprite(SpriteType.TEXT, option, new Vector2(x + dm.Gap * 1.5f, rowY + dm.RowHeight * 0.10f), null, active ? UiSelected : UiTextMuted, InfoDrawerTextFontId, TextAlignment.LEFT, dm.TinyText));
            }
        }
        static void DrawCargoBlockActionsWidget(MySpriteDrawFrame frame, CargoWidgetRect widget, OverlayBlockInfo selectedInfo, GridSchematicsLcdApp app)
        {
            var dm = BuildCargoDrawerMetrics(widget);
            float x = widget.X + dm.Pad;
            float y = widget.Y + dm.HeaderHeight + dm.Gap;
            float w = Math.Max(12f, widget.Width - dm.Pad * 2f);
            var actions = BuildCargoActionRows(selectedInfo);
            if (actions.Count == 0)
            {
                DrawSmallStatusGlyph(frame, new Vector2(widget.X + widget.Width * 0.5f, widget.Y + widget.Height * 0.44f), Math.Min(widget.Width, widget.Height) * 0.10f, UiAccentDim);
                AddSprite(frame, new MySprite(SpriteType.TEXT, "NO ACTIONS", new Vector2(widget.X + widget.Width * 0.5f, widget.Y + widget.Height * 0.64f), null, UiTextMuted, InfoDrawerTextFontId, TextAlignment.CENTER, dm.ValueText));
                return;
            }

            int visible = Math.Max(1, (int)((widget.Y + widget.Height - y - dm.Pad) / dm.RowHeight));
            if (visible > 9)
                visible = 9;
            int first = app != null && app.Ui != null ? app.Ui.CargoActionScrollIndex : 0;
            if (first < 0) first = 0;
            if (first + visible > actions.Count)
                first = Math.Max(0, actions.Count - visible);
            if (app != null && app.Ui != null && app.Ui.CargoActionScrollIndex != first)
                app.Ui.CargoActionScrollIndex = first;

            for (int row = 0; row < visible; row++)
            {
                int index = first + row;
                if (index >= actions.Count)
                    break;
                var line = actions[index];
                if (line == null)
                    continue;
                bool selected = app != null && app.Ui != null && app.Ui.SelectedOverlayLineIndex == index;
                DrawCargoActionRow(frame, x, y + row * dm.RowHeight, w, line, selected, dm);
            }

            if (actions.Count > visible)
            {
                float railX = widget.X + widget.Width - dm.Pad * 0.55f;
                float railTop = widget.Y + dm.HeaderHeight + dm.Gap;
                float railH = widget.Height - dm.HeaderHeight - dm.Pad - dm.Gap;
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(railX, railTop + railH * 0.5f), new Vector2(dm.Line, railH), UiAccentGhost));
                float ratio = actions.Count <= visible ? 0f : first / (float)(actions.Count - visible);
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(railX, railTop + dm.RowHeight + ratio * Math.Max(1f, railH - dm.RowHeight * 2f)), new Vector2(dm.Line, dm.RowHeight * 1.25f), UiSelected));
            }
        }

        static List<OverlayBlockInfoLine> BuildCargoActionRows(OverlayBlockInfo info)
        {
            var rows = new List<OverlayBlockInfoLine>();
            if (info == null || info.Lines == null)
                return rows;
            for (int i = 0; i < info.Lines.Count; i++)
            {
                var line = info.Lines[i];
                if (line == null || line.IsSeparator || line.IsFillBar)
                    continue;
                if (line.CanToggle || (line.TerminalBlocks != null && line.TerminalBlocks.Count > 0))
                    rows.Add(line);
            }
            return rows;
        }

        static void DrawCargoActionRow(MySpriteDrawFrame frame, float x, float y, float width, OverlayBlockInfoLine line, bool selected, CargoDrawerMetrics dm)
        {
            float rowH = Math.Max(9f, dm.RowHeight - dm.Gap * 0.55f);
            var center = new Vector2(x + width * 0.5f, y + rowH * 0.5f);
            Color fill = selected ? UiMenuButtonActive : UiAccentGhost;
            Color border = selected ? UiSelected : UiAccentDim;
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, new Vector2(width, rowH), fill));
            DrawScreenRectBorder(frame, center, new Vector2(width, rowH), border, dm.Line);
            string text = CargoActionLineLabel(line);
            AddSprite(frame, new MySprite(SpriteType.TEXT, ShortenCargoActionText(text), new Vector2(x + dm.Gap * 1.6f, y + rowH * 0.12f), null, selected ? UiSelected : UiTextMuted, InfoDrawerTextFontId, TextAlignment.LEFT, dm.TinyText));
            if (line.IsFillBar)
                DrawInfoFillBar(frame, new Vector2(x + width * 0.58f, y + rowH * 0.54f), new Vector2(width * 0.34f, Math.Max(5f, dm.MicroBarHeight * 0.65f)), line.FillRatio, border, selected ? UiSelected : UiAccentSoft);
        }
        struct CargoWidgetRect
        {
            public float X;
            public float Y;
            public float Width;
            public float Height;

            public CargoWidgetRect(float x, float y, float width, float height)
            {
                X = x;
                Y = y;
                Width = width;
                Height = height;
            }
        }

        static void DrawCargoWidgetFrame(MySpriteDrawFrame frame, CargoWidgetRect widget, string label, string rightLabel = null)
        {
            var dm = BuildCargoDrawerMetrics(widget);
            var center = SnapPoint(new Vector2(widget.X + widget.Width * 0.5f, widget.Y + widget.Height * 0.5f));
            var size = new Vector2(SnapPixelSize(widget.Width), SnapPixelSize(widget.Height));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, size, UiPanelFill));
            DrawScreenRectBorder(frame, center, size, UiAccentDim);
            AddSprite(frame, new MySprite(SpriteType.TEXT, label, SnapPoint(new Vector2(widget.X + dm.Pad, widget.Y + dm.Gap * 0.25f)), null, UiTextMuted, InfoDrawerTextFontId, TextAlignment.LEFT, dm.TitleText));
            if (!string.IsNullOrEmpty(rightLabel))
            {
                float arrowSize = Math.Max(5f, dm.TitleText * 26f);
                AddSprite(frame, new MySprite(SpriteType.TEXT, rightLabel, SnapPoint(new Vector2(widget.X + widget.Width - dm.Pad - arrowSize, widget.Y + dm.Gap * 0.25f)), null, UiTextMuted, InfoDrawerTextFontId, TextAlignment.RIGHT, dm.TinyText));
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "Triangle", SnapPoint(new Vector2(widget.X + widget.Width - dm.Pad - arrowSize * 0.35f, widget.Y + dm.HeaderHeight * 0.48f)), new Vector2(arrowSize, arrowSize * 0.75f), UiTextMuted, null, TextAlignment.CENTER, (float)Math.PI));
            }
        }
        static void DrawCargoCapacityWidget(MySpriteDrawFrame frame, CargoWidgetRect widget, CargoPanelSummary summary, Color storage)
        {
            var metrics = UiLayout.BuildMetrics((int)widget.Width, (int)widget.Height);
            float barWidth = Math.Max(metrics.S(8f), widget.Width - metrics.S(14f));
            float barHeight = Math.Max(metrics.S(4f), widget.Height * 0.088f);
            float barLeft = widget.X + metrics.S(7f);
            float barCenterX = barLeft + barWidth * 0.5f;
            float totalY = widget.Y + metrics.S(23f) + barHeight * 0.5f;
            float spineY = widget.Y + widget.Height * 0.56f;
            float fillX = barLeft + barWidth * Clamp01(summary.FillRatio);
            fillX = Math.Max(barLeft + metrics.S(8f), Math.Min(barLeft + barWidth - metrics.S(8f), fillX));
            Color fillTextColor = GetCargoFillColor(storage, summary.FillRatio);

            AddSprite(frame, new MySprite(SpriteType.TEXT, FormatCargoVolume(summary.CurrentVolume) + "/" + FormatCargoVolume(summary.MaxVolume), new Vector2(barLeft, totalY - barHeight * 0.5f - metrics.S(10f)), null, UiTextMuted, InfoDrawerTextFontId, TextAlignment.LEFT, metrics.SmallText));
            DrawInfoBarGlyph(frame, new Vector2(barCenterX, totalY), barWidth, barHeight, summary.FillRatio, fillTextColor, new Color(storage.R, storage.G, storage.B, 42));
            AddSprite(frame, new MySprite(SpriteType.TEXT, ((int)Math.Round(summary.FillRatio * 100f)).ToString() + "%", new Vector2(fillX, totalY + barHeight * 0.5f + metrics.S(3f)), null, fillTextColor, InfoDrawerTextFontId, TextAlignment.CENTER, metrics.SmallText));

            DrawCargoCapacitySpine(frame, widget.X + metrics.S(7f), spineY, barWidth, barHeight, summary);

            float marker80 = barLeft + barWidth * 0.80f;
            float marker95 = barLeft + barWidth * 0.95f;
            DrawCargoThresholdMarker(frame, marker80, totalY, barHeight, "80%", CargoCautionColor());
            DrawCargoThresholdMarker(frame, marker95, totalY, barHeight, "95%", UiWarning);

            DrawCargoSpineLegend(frame, widget, storage);
        }

        static void DrawCargoThresholdMarker(MySpriteDrawFrame frame, float x, float y, float barHeight, string label, Color color)
        {
            float line = ClampFloat(barHeight * 0.12f, 1f, 2f);
            float textScale = ClampFloat(barHeight * 0.018f, 0.16f, 0.24f);
            float markerHeight = barHeight + ClampFloat(barHeight * 0.55f, 4f, 8f);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(x, y), new Vector2(line, markerHeight), color));
            float tipY = y - barHeight * 0.5f - ClampFloat(barHeight * 0.18f, 1f, 3f);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(x, tipY - ClampFloat(barHeight * 0.34f, 3f, 5f)), new Vector2(ClampFloat(barHeight * 0.90f, 8f, 13f), ClampFloat(barHeight * 0.72f, 6f, 10f)), color, null, TextAlignment.CENTER, (float)Math.PI));
            AddSprite(frame, new MySprite(SpriteType.TEXT, label, new Vector2(x, tipY - ClampFloat(barHeight * 1.25f, 12f, 17f)), null, color, InfoDrawerTextFontId, TextAlignment.CENTER, textScale));
        }
        static void DrawCargoTotalWidget(MySpriteDrawFrame frame, CargoWidgetRect widget, CargoPanelSummary summary, Color storage)
        {
            float contentTop = widget.Y + 18f;
            float barWidth = Math.Max(8f, widget.Width - 14f);
            float barHeight = Math.Max(8f, widget.Height * 0.24f);
            float barY = contentTop + barHeight * 0.5f;
            DrawInfoBarGlyph(frame, new Vector2(widget.X + widget.Width * 0.5f, barY), barWidth, barHeight, summary.FillRatio, GetCargoFillColor(storage, summary.FillRatio), new Color(storage.R, storage.G, storage.B, 42));

            float marker80 = widget.X + 7f + barWidth * 0.80f;
            float marker95 = widget.X + 7f + barWidth * 0.95f;
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(marker80, barY), new Vector2(1f, barHeight + 5f), CargoCautionColor()));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(marker95, barY), new Vector2(1f, barHeight + 5f), UiWarning));

            AddSprite(frame, new MySprite(SpriteType.TEXT, ((int)Math.Round(summary.FillRatio * 100f)).ToString() + "%", new Vector2(widget.X + widget.Width * 0.5f, widget.Y + widget.Height - 18f), null, UiText, InfoDrawerTextFontId, TextAlignment.CENTER, 0.42f));
            AddSprite(frame, new MySprite(SpriteType.TEXT, FormatCargoVolume(summary.CurrentVolume) + "/" + FormatCargoVolume(summary.MaxVolume), new Vector2(widget.X + widget.Width * 0.5f, widget.Y + widget.Height - 8f), null, UiTextMuted, InfoDrawerTextFontId, TextAlignment.CENTER, 0.25f));
        }

        static void DrawCargoSpineWidget(MySpriteDrawFrame frame, CargoWidgetRect widget, CargoPanelSummary summary, Color storage)
        {
            float barWidth = Math.Max(8f, widget.Width - 14f);
            float y = widget.Y + widget.Height * 0.48f;
            DrawCargoCapacitySpine(frame, widget.X + 7f, y, barWidth, Math.Max(8f, widget.Height * 0.18f), summary);

            DrawCargoSpineLegend(frame, widget, storage);
        }

        static void DrawCargoSpineLegend(MySpriteDrawFrame frame, CargoWidgetRect widget, Color storage)
        {
            var metrics = UiLayout.BuildMetrics((int)widget.Width, (int)widget.Height);
            float barWidth = Math.Max(metrics.S(8f), widget.Width - metrics.S(14f));
            string[] labels = new[] { "ORE", "ING", "CMP", "TLS", "CON", "OTH" };
            string[] categories = new[] { "ORE", "INGOT", "COMP", "TOOLS", "CONSUMABLE", "OTHER" };
            float keyY = widget.Y + widget.Height * 0.76f;
            float textY = keyY + metrics.S(13f);
            float keySize = metrics.S(18f);
            for (int i = 0; i < labels.Length; i++)
            {
                float x = widget.X + metrics.S(8f) + i * (barWidth / labels.Length);
                Color color = GetCargoCategoryColor(categories[i], storage);
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(x + keySize * 0.5f, keyY), new Vector2(keySize, keySize), color));
                AddSprite(frame, new MySprite(SpriteType.TEXT, labels[i], new Vector2(x, textY), null, UiTextMuted, InfoDrawerTextFontId, TextAlignment.LEFT, metrics.LargeText * 1.15f));
            }
        }

        static void DrawCargoTopItemsWidget(MySpriteDrawFrame frame, CargoWidgetRect widget, CargoPanelSummary summary, Color storage)
        {
            var metrics = UiLayout.BuildMetrics((int)widget.Width, (int)widget.Height);
            int shown = Math.Min(5, summary.TopItems.Count);
            float startY = widget.Y + metrics.S(20f);
            float rowGap = Math.Max(metrics.S(8f), (widget.Height - metrics.S(34f)) / 5f);
            float barWidth = Math.Max(metrics.S(18f), widget.Width * 0.52f);
            float textMaxWidth = Math.Max(metrics.S(10f), widget.Width - barWidth - metrics.S(26f));
            for (int i = 0; i < shown; i++)
            {
                var item = summary.TopItems[i];
                float y = startY + i * rowGap;
                float ratio = summary.CurrentVolume > 0f ? Clamp01(item.Volume / summary.CurrentVolume) : 0f;
                Color itemColor = GetCargoCategoryColor(item.Category, storage);
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(widget.X + metrics.S(7f), y), new Vector2(metrics.S(5f), metrics.S(5f)), itemColor));
                AddSprite(frame, new MySprite(SpriteType.TEXT, ShortenCargoName(item.Name, textMaxWidth), new Vector2(widget.X + metrics.S(14f), y - metrics.S(7f)), null, UiTextMuted, InfoDrawerTextFontId, TextAlignment.LEFT, metrics.SmallText));
                DrawInfoBarGlyph(frame, new Vector2(widget.X + widget.Width - metrics.S(7f) - barWidth * 0.5f, y), barWidth, metrics.S(4f), ratio, itemColor, UiAccentGhost);
            }

            if (summary.ItemTypeCount > shown)
                AddSprite(frame, new MySprite(SpriteType.TEXT, "+" + (summary.ItemTypeCount - shown).ToString(), new Vector2(widget.X + widget.Width - metrics.S(5f), widget.Y + widget.Height - metrics.S(10f)), null, UiSelected, InfoDrawerTextFontId, TextAlignment.RIGHT, metrics.MediumText));
        }

        static void DrawCargoFallbackGlyph(MySpriteDrawFrame frame, ScreenZone zone, Color storage, Color conveyor)
        {
            float baseY = zone.Y + zone.Height * 0.62f;
            float boxW = Math.Max(12f, zone.Width * 0.10f);
            float boxH = Math.Max(22f, zone.Height * 0.34f);
            for (int i = 0; i < 4; i++)
            {
                float x = zone.X + zone.Width * (0.22f + i * 0.14f);
                var center = new Vector2(x, baseY);
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, new Vector2(boxW, boxH), UiAccentGhost));
                DrawInfoBarGlyph(frame, center + new Vector2(0f, boxH * 0.18f), boxW * 0.72f, boxH * 0.52f, 0.28f + i * 0.16f, storage, new Color(storage.R, storage.G, storage.B, 45));
            }

            float railY = zone.Y + zone.Height * 0.38f;
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(zone.X + zone.Width * 0.50f, railY), new Vector2(zone.Width * 0.62f, 2f), conveyor));
            for (int i = 0; i < 6; i++)
            {
                float x = zone.X + zone.Width * (0.20f + i * 0.12f);
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(x, railY), new Vector2(6f, 6f), conveyor));
            }
        }

        public class CargoPanelSummary
        {
            public string Filter = "ALL";
            public string Focus = "ALL";
            public string DisplayLabel = "CARGO MIX";
            public float CurrentVolume;
            public float MaxVolume;
            public float FillRatio;
            public int ItemTypeCount;
            public int ReachableCount;
            public int IsolatedCount;
            public int FullCount;
            public int OfflineCount;
            public List<CargoPanelItem> TopItems = new List<CargoPanelItem>();
            public List<CargoPanelBlock> Blocks = new List<CargoPanelBlock>();
            public Dictionary<string, float> CategoryVolumes = new Dictionary<string, float>();
        }

        public class CargoPanelItem
        {
            public string Key;
            public string Name;
            public string Category;
            public string TypeId;
            public string SubtypeId;
            public float Amount;
            public float Volume;
            public float Mass;
        }

        public class CargoPanelBlock
        {
            public IMyCubeBlock Block;
            public float FillRatio;
            public bool Online;
            public bool Reachable;
            public bool Full;
            public List<string> Categories = new List<string>();
        }


        static CargoPanelSummary GetOrBuildCargoLoadSummaryForPanel(GridSchematicsLcdApp app, bool isCursorOnlyRender)
        {
            if (app == null)
                return BuildCargoSummaryForPanel(app, null);

            OverlayBlockInfo loadInfo = ResolveCargoLoadSummarySelectionInfo(app);
            string cacheKey = "load:" + BuildCargoSummaryCacheKey(app, loadInfo);
            // QW1: see GetOrBuildCargoSummaryForPanel — cache honored on all paths with a short TTL.
            if (app.Ui != null &&
                string.Equals(app.Ui.CachedCargoLoadSummaryKey, cacheKey, StringComparison.Ordinal) &&
                app.Ui.CachedCargoLoadSummary != null &&
                (isCursorOnlyRender || app.CurrentTick - app.Ui.CachedCargoLoadSummaryTick < CargoSummaryRefreshTicks))
            {
                return app.Ui.CachedCargoLoadSummary;
            }

            var summary = BuildCargoSummaryForPanel(app, loadInfo);
            if (app.Ui != null)
            {
                app.Ui.CachedCargoLoadSummaryKey = cacheKey;
                app.Ui.CachedCargoLoadSummary = summary;
                app.Ui.CachedCargoLoadSummaryTick = app.CurrentTick;
            }

            return summary;
        }
        static OverlayBlockInfo ResolveCargoLoadSummarySelectionInfo(GridSchematicsLcdApp app)
        {
            if (app == null || app.Ui == null || !IsCargoLoadSelectionScoped(app))
                return null;
            return BuildSelectedBlockStackInfo(app.Ui);
        }

        static bool IsCargoLoadSelectionScoped(GridSchematicsLcdApp app)
        {
            var ui = app != null ? app.Ui : null;
            return ui != null && ui.SelectedBlockStackItems != null && ui.SelectedBlockStackItems.Count > 0 && ui.SelectedBlockStackIndex != UiState.SelectedBlockStackAllIndex;
        }

        static bool IsCargoLoadCursorActive(GridSchematicsLcdApp app)
        {
            return app != null && app.Ui != null && app.Ui.CargoBlockCursorActiveUntilTick >= app.CurrentTick;
        }

        static string CargoLoadHeaderLabel(GridSchematicsLcdApp app)
        {
            if (!IsCargoLoadSelectionScoped(app))
                return "LOAD TOTALS";
            if (IsManualSelectedBlockStack(app.Ui))
            {
                int groupCount = GetManualBlockGroupCount(app.Ui);
                int groupIndex = GetSelectedManualGroupIndex(app.Ui);
                if (groupCount > 1 && groupIndex > 0)
                    return "GROUP" + (groupIndex == 2 ? "B" : "A") + " TOTALS";
                return "GROUP TOTALS";
            }
            if (app.Ui != null && app.Ui.SelectedBlockStackItems != null && app.Ui.SelectedBlockStackItems.Count == 1 && app.Ui.SelectedBlockStackIndex == 0)
                return "BLOCK TOTALS";
            return "STACK TOTALS";
        }

        static string CargoLoadSelectionLabel(GridSchematicsLcdApp app)
        {
            if (!IsCargoLoadSelectionScoped(app))
                return string.Empty;
            if (IsManualSelectedBlockStack(app.Ui))
            {
                int groupCount = GetManualBlockGroupCount(app.Ui);
                int groupIndex = GetSelectedManualGroupIndex(app.Ui);
                if (groupCount > 1 && groupIndex > 0)
                    return "GROUP" + (groupIndex == 2 ? "B" : "A");
                return "GROUP";
            }
            if (app.Ui != null && app.Ui.SelectedBlockStackItems != null && app.Ui.SelectedBlockStackItems.Count == 1 && app.Ui.SelectedBlockStackIndex == 0)
                return "BLOCK";
            return "STACK";
        }

        static string CargoLoadSendLabel(GridSchematicsLcdApp app)
        {
            var ui = app != null ? app.Ui : null;
            if (ui == null)
                return string.Empty;
            if (string.Equals(ui.CargoTransferCaptureTarget ?? string.Empty, "SOURCE", StringComparison.Ordinal))
                return "SEND TO TA >>";
            if (string.Equals(ui.CargoTransferCaptureTarget ?? string.Empty, "DEST", StringComparison.Ordinal))
                return "SEND TO TB >>";
            if (IsCargoLoadCursorActive(app) && IsManualSelectedBlockStack(ui))
            {
                int groupIndex = GetSelectedManualGroupIndex(ui);
                if (groupIndex == 1)
                    return "SEND TO GA >>";
                if (groupIndex == 2)
                    return "SEND TO GB >>";
            }
            return string.Empty;
        }
        static CargoPanelSummary GetOrBuildCargoSummaryForPanel(GridSchematicsLcdApp app, OverlayBlockInfo selectedInfo, bool isCursorOnlyRender)
        {
            if (app == null)
                return BuildCargoSummaryForPanel(app, selectedInfo);

            selectedInfo = ResolveTransferCargoMixSelectionInfo(app, selectedInfo);
            string cacheKey = BuildCargoSummaryCacheKey(app, selectedInfo);
            // QW1: honor the (resource-agnostic) cache on ALL render paths, bounded by a short TTL,
            // instead of only on cursor-only renders. Removes the per-render physical-group inventory
            // walk that rapid power/container switching otherwise forces every ~6 ticks.
            if (app.Ui != null &&
                string.Equals(app.Ui.CachedCargoSummaryKey, cacheKey, StringComparison.Ordinal) &&
                app.Ui.CachedCargoSummary != null &&
                (isCursorOnlyRender || app.CurrentTick - app.Ui.CachedCargoSummaryTick < CargoSummaryRefreshTicks))
            {
                return app.Ui.CachedCargoSummary;
            }

            var summary = BuildCargoSummaryForPanel(app, selectedInfo);
            if (app.Ui != null)
            {
                app.Ui.CachedCargoSummaryKey = cacheKey;
                app.Ui.CachedCargoSummary = summary;
                app.Ui.CachedCargoSummaryTick = app.CurrentTick;
            }

            return summary;
        }

        static OverlayBlockInfo ResolveTransferCargoMixSelectionInfo(GridSchematicsLcdApp app, OverlayBlockInfo fallback)
        {
            if (app == null || app.Ui == null || !string.Equals(app.Ui.CargoRightPanelMode ?? string.Empty, "TRANSFER", StringComparison.OrdinalIgnoreCase))
                return fallback;
            string target = string.Equals(app.Ui.CargoTransferMixViewTarget ?? "SOURCE", "DEST", StringComparison.Ordinal) ? "DEST" : "SOURCE";
            var items = target == "DEST" ? app.Ui.CargoTransferDestItems : app.Ui.CargoTransferSourceItems;
            if (items == null || items.Count == 0)
                return fallback;
            var info = new OverlayBlockInfo();
            info.Id = "transfer:" + target;
            info.Name = BuildTransferSelectionDisplayName(items, target);
            for (int i = 0; i < items.Count; i++)
            {
                var block = items[i] != null ? items[i].Block : null;
                if (block != null)
                    info.Blocks.Add(block);
            }
            return info.Blocks.Count > 0 ? info : fallback;
        }

        static string BuildTransferSelectionDisplayName(List<BlockStackItem> items, string target)
        {
            if (items == null || items.Count == 0)
                return target == "DEST" ? "DEST" : "SOURCE";
            if (items.Count == 1 && items[0] != null)
                return ShortenTransferSelectionLabel(items[0].Name, items[0].Block != null ? items[0].Block.DefinitionDisplayNameText : string.Empty, 5);
            return "GROUP";
        }
        static string BuildCargoSummaryCacheKey(GridSchematicsLcdApp app, OverlayBlockInfo selectedInfo)
        {
            string filter = app != null && app.Ui != null ? NormalizeCargoInfoSelector(app.Ui.CargoInfoFilter) : "ALL";
            string focus = app != null && app.Ui != null ? NormalizeCargoInfoSelector(app.Ui.CargoInfoFocus) : "ALL";
            string source = app != null && app.Ui != null ? NormalizeCargoInfoSource(app.Ui.CargoInfoSource) : "LOCAL";
            if (app == null || app.OwnerBlock == null)
                return "unbound:" + filter + ":" + focus + ":" + (selectedInfo != null ? selectedInfo.Id : "none");

            long panelId = app.OwnerBlock.EntityId;
            if (selectedInfo == null || selectedInfo.Blocks == null || selectedInfo.Blocks.Count <= 0)
                return "total:" + panelId + ":" + source + ":" + filter + ":" + focus + ":" + (selectedInfo != null ? selectedInfo.Id : "all");

            int hash = 17;
            hash = hash * 31 + (selectedInfo.Id == null ? 0 : selectedInfo.Id.GetHashCode());
            hash = hash * 31 + selectedInfo.Blocks.Count;
            for (int i = 0; i < selectedInfo.Blocks.Count; i++)
            {
                var block = selectedInfo.Blocks[i];
                long blockId = 0L;
                if (block != null)
                {
                    try
                    {
                        blockId = block.EntityId;
                    }
                    catch
                    {
                    }
                }
                hash = hash * 31 + (int)(blockId ^ (blockId >> 32));
            }

            return "selected:" + panelId + ":" + source + ":" + filter + ":" + focus + ":" + hash;
        }

        static string NormalizeCargoInfoSource(string source)
        {
            return string.Equals(source ?? string.Empty, "AUX", StringComparison.OrdinalIgnoreCase) ? "AUX" : "LOCAL";
        }

        static CargoPanelSummary BuildCargoSummaryForPanel(GridSchematicsLcdApp app, OverlayBlockInfo selectedInfo)
        {
            if (app == null || app.OwnerBlock == null || app.OwnerBlock.CubeGrid == null)
                return null;

            var summary = new CargoPanelSummary();
            summary.Filter = app != null && app.Ui != null ? NormalizeCargoInfoSelector(app.Ui.CargoInfoFilter) : "ALL";
            summary.Focus = app != null && app.Ui != null ? NormalizeCargoInfoSelector(app.Ui.CargoInfoFocus) : "ALL";
            bool auxiliarySource = app != null && app.Ui != null && string.Equals(NormalizeCargoInfoSource(app.Ui.CargoInfoSource), "AUX", StringComparison.Ordinal);
            summary.DisplayLabel = selectedInfo != null && !string.IsNullOrWhiteSpace(selectedInfo.Name) ? selectedInfo.Name : auxiliarySource ? "AUX CARGO" : "CARGO MIX";
            var topology = auxiliarySource ? null : app != null && app.ConstructCache != null ? app.ConstructCache.ConveyorNetwork : null;
            // QW4: build a block-id -> isWorking lookup once so reachability is O(1) per block instead of
            // an O(nodes) linear scan per block (the summary walk was O(blocks x nodes)).
            Dictionary<long, bool> reachMap = null;
            if (topology != null && topology.Nodes != null)
            {
                reachMap = new Dictionary<long, bool>(topology.Nodes.Count);
                for (int n = 0; n < topology.Nodes.Count; n++)
                {
                    var node = topology.Nodes[n];
                    if (node != null && !reachMap.ContainsKey(node.BlockEntityId))
                        reachMap[node.BlockEntityId] = node.IsWorking;
                }
            }
            var itemMap = new Dictionary<string, CargoPanelItem>();
            try
            {
                if (selectedInfo != null && selectedInfo.Blocks != null && selectedInfo.Blocks.Count > 0)
                {
                    for (int i = 0; i < selectedInfo.Blocks.Count; i++)
                    {
                        AddCargoBlockToSummary(selectedInfo.Blocks[i], summary, itemMap, topology, reachMap);
                    }
                }
                else
                {
                    var grids = auxiliarySource ? CollectAuxiliaryCargoSummaryGrids(app) : CollectLocalCargoSummaryGrids(app);
                    var blocks = new List<IMySlimBlock>();
                    for (int g = 0; g < grids.Count; g++)
                    {
                        var grid = grids[g];
                        if (grid == null)
                            continue;

                        blocks.Clear();
                        grid.GetBlocks(blocks, block => block != null && block.FatBlock != null && block.FatBlock.InventoryCount > 0);
                        for (int b = 0; b < blocks.Count; b++)
                        {
                            AddCargoBlockToSummary(blocks[b].FatBlock, summary, itemMap, topology, reachMap);
                        }
                    }
                }
            }
            catch
            {
                return null;
            }

            if (summary.MaxVolume > 0f)
                summary.FillRatio = Clamp01(summary.CurrentVolume / summary.MaxVolume);

            summary.Blocks.Sort(delegate(CargoPanelBlock a, CargoPanelBlock b)
            {
                return GetCargoBlockMaxVolume(b != null ? b.Block : null).CompareTo(GetCargoBlockMaxVolume(a != null ? a.Block : null));
            });

            foreach (var pair in itemMap)
            {
                summary.TopItems.Add(pair.Value);
            }

            summary.ItemTypeCount = summary.TopItems.Count;
            summary.TopItems.Sort(delegate(CargoPanelItem a, CargoPanelItem b)
            {
                return b.Volume.CompareTo(a.Volume);
            });

            return summary;
        }

        static List<IMyCubeGrid> CollectAuxiliaryCargoSummaryGrids(GridSchematicsLcdApp app)
        {
            if (app == null)
                return new List<IMyCubeGrid>();
            return app.CollectAuxiliaryStaticCargoGrids();
        }
        static List<IMyCubeGrid> CollectLocalCargoSummaryGrids(GridSchematicsLcdApp app)
        {
            var grids = new List<IMyCubeGrid>();
            if (app == null || app.OwnerBlock == null || app.OwnerBlock.CubeGrid == null)
                return grids;

            if (app.ConstructCache != null && app.ConstructCache.ConstructGrids != null && app.ConstructCache.ConstructGrids.Count > 0)
            {
                var seen = new HashSet<long>();
                for (int i = 0; i < app.ConstructCache.ConstructGrids.Count; i++)
                {
                    var grid = app.ConstructCache.ConstructGrids[i];
                    if (grid == null || grid.MarkedForClose || seen.Contains(grid.EntityId))
                        continue;
                    seen.Add(grid.EntityId);
                    grids.Add(grid);
                }
            }

            if (grids.Count == 0)
                grids.Add(app.OwnerBlock.CubeGrid);
            return grids;
        }
        static void AddCargoBlockToSummary(IMyCubeBlock fat, CargoPanelSummary summary, Dictionary<string, CargoPanelItem> itemMap, ConveyorTopology topology, Dictionary<long, bool> reachMap)
        {
            if (fat == null || summary == null || itemMap == null)
                return;

            var blockSummary = new CargoPanelBlock();
            blockSummary.Block = fat;
            blockSummary.Online = IsCargoBlockOnline(fat);
            blockSummary.Reachable = IsCargoBlockReachable(fat, topology, reachMap);

            try
            {
                float blockCurrent = 0f;
                float blockMax = 0f;
                int inventoryCount = fat.InventoryCount;
                for (int i = 0; i < inventoryCount; i++)
                {
                    var inventory = fat.GetInventory(i);
                    if (inventory == null)
                        continue;

                    float current = (float)inventory.CurrentVolume;
                    float max = (float)inventory.MaxVolume;
                    blockCurrent += current;
                    blockMax += max;
                    summary.CurrentVolume += current;
                    summary.MaxVolume += max;

                    var items = CargoInventoryItemScratch; // QW7: reuse one scratch list instead of allocating per inventory
                    items.Clear();
                    try
                    {
                        inventory.GetItems(items);
                    }
                    catch
                    {
                        continue;
                    }

                    for (int itemIndex = 0; itemIndex < items.Count; itemIndex++)
                    {
                        try
                        {
                            var item = items[itemIndex];
                            string subtype = item.Type.SubtypeId;
                            string typeId = item.Type.TypeId;
                            string category = CategorizeCargoItem(typeId, subtype);
                            float volume = EstimateCargoItemVolume(item);
                            if (volume <= 0f)
                                continue;

                            AddCargoCategoryVolume(summary, category, volume);
                            if (!blockSummary.Categories.Contains(category))
                                blockSummary.Categories.Add(category);

                            string key = typeId + "/" + subtype;
                            CargoPanelItem aggregate;
                            if (!itemMap.TryGetValue(key, out aggregate))
                            {
                                aggregate = new CargoPanelItem
                                {
                                    Key = key,
                                    Name = string.IsNullOrEmpty(subtype) ? typeId : subtype,
                                    Category = category,
                                    TypeId = typeId,
                                    SubtypeId = subtype,
                                    Amount = 0f,
                                    Volume = 0f,
                                    Mass = 0f
                                };
                                itemMap[key] = aggregate;
                            }

                            aggregate.Amount += (float)item.Amount;
                            aggregate.Volume += volume;
                            aggregate.Mass += (float)item.Amount;
                        }
                        catch
                        {
                        }
                    }
                }
                if (blockMax > 0f)
                    blockSummary.FillRatio = Clamp01(blockCurrent / blockMax);
                blockSummary.Full = blockSummary.FillRatio >= 0.95f;
            }
            catch
            {
            }

            if (blockSummary.Reachable) summary.ReachableCount++;
            else summary.IsolatedCount++;
            if (blockSummary.Full) summary.FullCount++;
            if (!blockSummary.Online) summary.OfflineCount++;
            summary.Blocks.Add(blockSummary);
        }

        static void AddCargoCategoryVolume(CargoPanelSummary summary, string category, float volume)
        {
            float existing;
            if (!summary.CategoryVolumes.TryGetValue(category, out existing))
                existing = 0f;
            summary.CategoryVolumes[category] = existing + volume;
        }

        static string CategorizeCargoItem(string typeId, string subtype)
        {
            string type = typeId ?? string.Empty;
            string sub = subtype ?? string.Empty;
            if (sub.IndexOf("Ice", StringComparison.OrdinalIgnoreCase) >= 0)
                return "ORE";
            if (type.IndexOf("Ore", StringComparison.OrdinalIgnoreCase) >= 0)
                return "ORE";
            if (type.IndexOf("Ingot", StringComparison.OrdinalIgnoreCase) >= 0)
                return "INGOT";
            if (type.IndexOf("Component", StringComparison.OrdinalIgnoreCase) >= 0)
                return "COMP";
            if (type.IndexOf("PhysicalGun", StringComparison.OrdinalIgnoreCase) >= 0 || type.IndexOf("Tool", StringComparison.OrdinalIgnoreCase) >= 0 || type.IndexOf("Weapon", StringComparison.OrdinalIgnoreCase) >= 0)
                return "TOOLS";
            if (type.IndexOf("Consumable", StringComparison.OrdinalIgnoreCase) >= 0 || type.IndexOf("GasContainer", StringComparison.OrdinalIgnoreCase) >= 0 || type.IndexOf("OxygenContainer", StringComparison.OrdinalIgnoreCase) >= 0)
                return "CONSUMABLE";
            return "OTHER";
        }

        static float EstimateCargoItemVolume(VRage.Game.ModAPI.Ingame.MyInventoryItem item)
        {
            try
            {
                return (float)item.Amount;
            }
            catch
            {
                return 0f;
            }
        }

        static void DrawCargoCapacitySpine(MySpriteDrawFrame frame, float x, float y, float width, float height, CargoPanelSummary summary)
        {
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(x + width * 0.5f, y), new Vector2(width, height), UiAccentGhost));
            float total = summary.CurrentVolume;
            if (total <= 0f)
                return;

            string[] categories = new[] { "ORE", "INGOT", "COMP", "TOOLS", "CONSUMABLE", "OTHER" };
            float cursor = x;
            for (int i = 0; i < categories.Length; i++)
            {
                float volume = GetCargoCategoryVolume(summary, categories[i]);
                if (volume <= 0f)
                    continue;

                float segmentWidth = width * Clamp01(volume / total);
                if (segmentWidth < 2f)
                    segmentWidth = 2f;
                if (cursor + segmentWidth > x + width)
                    segmentWidth = x + width - cursor;
                if (segmentWidth <= 0f)
                    break;

                Color color = GetCargoCategoryColor(categories[i], ResolveStorageSchematicColor());
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(cursor + segmentWidth * 0.5f, y), new Vector2(segmentWidth, height), color));
                cursor += segmentWidth;
            }
        }

        static float GetCargoCategoryVolume(CargoPanelSummary summary, string category)
        {
            float value;
            if (summary.CategoryVolumes.TryGetValue(category, out value))
                return value;
            return 0f;
        }

        static Color GetCargoCategoryColor(string category, Color storage)
        {
            if (category == "ORE")
                return new Color(180, 120, 55, 230);
            if (category == "INGOT")
                return new Color(150, 185, 205, 230);
            if (category == "COMP")
                return new Color(220, 230, 225, 230);
            if (category == "TOOLS")
                return new Color(220, 80, 75, 230);
            if (category == "CONSUMABLE")
                return new Color(90, 210, 240, 230);
            if (category == "OTHER")
                return new Color(130, 145, 150, 200);
            return storage;
        }

        static Color GetCargoMixItemColor(CargoPanelItem item, int rowIndex, string filter, Color storage)
        {
            filter = NormalizeCargoInfoSelector(filter);
            if (filter == "ALL")
                return GetCargoCategoryColor(item != null ? item.Category : string.Empty, storage);

            switch (PositiveModulo(rowIndex, 8))
            {
                case 0: return new Color(210, 125, 45, 230);
                case 1: return new Color(45, 205, 215, 230);
                case 2: return new Color(205, 0, 220, 230);
                case 3: return new Color(230, 225, 0, 230);
                case 4: return new Color(20, 135, 135, 230);
                case 5: return new Color(145, 150, 150, 230);
                case 6: return new Color(120, 210, 85, 230);
                default: return new Color(205, 95, 70, 230);
            }
        }

        static int PositiveModulo(int value, int modulo)
        {
            if (modulo <= 0)
                return 0;
            int result = value % modulo;
            return result < 0 ? result + modulo : result;
        }
        static Color GetCargoFillColor(Color storage, float ratio)
        {
            return storage;
        }

        static Color CargoCautionColor()
        {
            return new Color(205, 170, 45, 230);
        }

        static string ShortenCargoName(string name, float maxWidth)
        {
            if (string.IsNullOrEmpty(name))
                return "ITEM";
            string tag = CargoElementTag(name);
            if (!string.IsNullOrEmpty(tag))
                return tag;

            int maxChars = (int)Math.Floor(maxWidth / 5.2f);
            if (maxChars < 3)
                maxChars = 3;
            if (maxChars > 10)
                maxChars = 10;

            if (name.Length <= maxChars)
                return name.ToUpperInvariant();
            return name.Substring(0, maxChars - 1).ToUpperInvariant() + ".";
        }

        static string CargoElementTag(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            string value = name.ToUpperInvariant();
            if (value.IndexOf("IRON", StringComparison.Ordinal) >= 0)
                return "Fe";
            if (value.IndexOf("NICKEL", StringComparison.Ordinal) >= 0)
                return "Ni";
            if (value.IndexOf("COBALT", StringComparison.Ordinal) >= 0)
                return "Co";
            if (value.IndexOf("MAGNESIUM", StringComparison.Ordinal) >= 0)
                return "Mg";
            if (value.IndexOf("SILICON", StringComparison.Ordinal) >= 0)
                return "Si";
            if (value.IndexOf("SILVER", StringComparison.Ordinal) >= 0)
                return "Ag";
            if (value.IndexOf("GOLD", StringComparison.Ordinal) >= 0)
                return "Au";
            if (value.IndexOf("PLATINUM", StringComparison.Ordinal) >= 0)
                return "Pt";
            if (value.IndexOf("URANIUM", StringComparison.Ordinal) >= 0)
                return "U";
            if (value.IndexOf("STONE", StringComparison.Ordinal) >= 0)
                return "STN";
            if (value.IndexOf("ICE", StringComparison.Ordinal) >= 0)
                return "CONSUMABLE";
            return string.Empty;
        }


        static void DrawCargoReachabilityMini(MySpriteDrawFrame frame, Vector2 pos, float width, CargoPanelSummary summary, CargoDrawerMetrics dm)
        {
            int reachable = summary != null ? summary.ReachableCount : 0;
            int isolated = summary != null ? summary.IsolatedCount : 0;
            int total = Math.Max(1, reachable + isolated);
            float ratio = reachable / (float)total;
            AddSprite(frame, new MySprite(SpriteType.TEXT, "REACH", pos, null, UiTextMuted, InfoDrawerTextFontId, TextAlignment.LEFT, dm.TinyText));
            AddSprite(frame, new MySprite(SpriteType.TEXT, ((int)Math.Round(ratio * 100f)).ToString() + "%", new Vector2(pos.X + width * 0.48f, pos.Y), null, new Color(85, 220, 120, 235), InfoDrawerTextFontId, TextAlignment.RIGHT, dm.TinyText));
            AddSprite(frame, new MySprite(SpriteType.TEXT, "ISO " + ((int)Math.Round((1f - ratio) * 100f)).ToString() + "%", new Vector2(pos.X + width, pos.Y), null, UiWarning, InfoDrawerTextFontId, TextAlignment.RIGHT, dm.TinyText));
            DrawInfoBarGlyph(frame, new Vector2(pos.X + width * 0.5f, pos.Y + dm.RowHeight * 0.86f), width, dm.MicroBarHeight, ratio, new Color(85, 220, 120, 230), new Color(125, 35, 35, 150));
        }
        static void DrawCargoSaturationStrip(MySpriteDrawFrame frame, Vector2 pos, float width, float height, CargoPanelSummary summary)
        {
            if (summary == null || summary.Blocks == null || summary.Blocks.Count == 0 || height <= 0f)
                return;

            int lanes = Math.Min(summary.Blocks.Count, Math.Max(1, (int)(width / 18f)));
            float step = width / lanes;
            for (int lane = 0; lane < lanes; lane++)
            {
                int start = lane * summary.Blocks.Count / lanes;
                int end = Math.Max(start + 1, (lane + 1) * summary.Blocks.Count / lanes);
                float fill = 0f;
                bool isolated = false;
                bool focused = false;
                for (int i = start; i < end && i < summary.Blocks.Count; i++)
                {
                    var block = summary.Blocks[i];
                    fill += block.FillRatio;
                    isolated = isolated || !block.Reachable;
                    focused = focused || CargoPanelBlockMatchesFocus(block, summary.Focus);
                }
                fill /= Math.Max(1, end - start);
                float laneW = Math.Max(8f, step - 2f);
                float filledH = Math.Max(1f, height * Clamp01(fill));
                float x = pos.X + lane * step + laneW * 0.5f;
                Color color = isolated ? UiWarning : GetCargoFillColor(ResolveStorageSchematicColor(), fill);
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(x, pos.Y + height * 0.5f), new Vector2(laneW, height), UiAccentGhost));
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(x, pos.Y + height - filledH * 0.5f), new Vector2(laneW, filledH), color));
                if (focused)
                    DrawScreenRectBorder(frame, new Vector2(x, pos.Y + height * 0.5f), new Vector2(laneW + 1f, height), UiSelected, 1f);
            }
        }
        static void DrawCargoStackedBarFiltered(MySpriteDrawFrame frame, Vector2 pos, Vector2 size, CargoPanelSummary summary, string filter, Color storage)
        {
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", pos + size * 0.5f, size, UiAccentGhost));
            float total = GetCargoMixTotal(summary, filter);
            if (summary == null || total <= 0f)
                return;

            string[] categories = new[] { "ORE", "INGOT", "COMP", "TOOLS", "CONSUMABLE", "OTHER" };
            float cursor = pos.X;
            for (int i = 0; i < categories.Length; i++)
            {
                string category = categories[i];
                if (!CategoryMatchesCargoFilter(category, filter))
                    continue;
                float amount = GetCargoCategoryVolume(summary, category);
                if (amount <= 0f)
                    continue;
                float w = Math.Max(1f, size.X * Clamp01(amount / total));
                if (cursor + w > pos.X + size.X)
                    w = pos.X + size.X - cursor;
                if (w <= 0f)
                    break;
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(cursor + w * 0.5f, pos.Y + size.Y * 0.5f), new Vector2(w, size.Y), GetCargoCategoryColor(category, storage)));
                cursor += w;
            }
        }

        static float GetCargoMixTotal(CargoPanelSummary summary, string filter)
        {
            if (summary == null)
                return 0f;
            filter = NormalizeCargoInfoSelector(filter);
            if (filter != "ALL")
                return GetCargoCategoryVolume(summary, filter);
            string[] categories = new[] { "ORE", "INGOT", "COMP", "TOOLS", "CONSUMABLE", "OTHER" };
            float total = 0f;
            for (int i = 0; i < categories.Length; i++)
                total += GetCargoCategoryVolume(summary, categories[i]);
            return total;
        }

        static bool CategoryMatchesCargoFilter(string category, string filter)
        {
            filter = NormalizeCargoInfoSelector(filter);
            return filter == "ALL" || string.Equals(category, filter, StringComparison.Ordinal);
        }

        static bool CargoPanelBlockMatchesFocus(CargoPanelBlock block, string focus)
        {
            if (block == null)
                return false;
            focus = NormalizeCargoInfoSelector(focus);
            if (focus == "ALL") return true;
            if (focus == "REACHABLE") return block.Reachable;
            if (focus == "ISOLATED") return !block.Reachable;
            if (focus == "FULL") return block.Full;
            if (focus == "OFFLINE") return !block.Online;
            return block.Categories != null && block.Categories.Contains(focus);
        }

        static string NormalizeCargoInfoSelector(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "ALL";
            value = value.Trim().ToUpperInvariant();
            if (value == "COMPONENT" || value == "COMPONENTS") return "COMP";
            if (value == "AMMUNITION") return "TOOLS";
            if (value == "TOOL") return "TOOLS";
            if (value == "CONSUMABLES") return "CONSUMABLE";
            if (value == "REACH") return "REACHABLE";
            if (value == "ISOLATE") return "ISOLATED";
            if (value == "OFF") return "OFFLINE";
            if (value == "ALL" || value == "ORE" || value == "INGOT" || value == "COMP" || value == "TOOLS" || value == "CONSUMABLE" || value == "OTHER") return value;
            if (value == "REACHABLE" || value == "ISOLATED" || value == "FULL" || value == "OFFLINE") return value;
            return "ALL";
        }


        static bool CargoOverlayGroupMatchesDrawerFocus(CargoOverlayGroup group, UiState ui)
        {
            if (group == null || ui == null || ui.ActiveOverlay != OverlayMode.Cargo)
                return false;
            string focus = NormalizeCargoInfoSelector(ui.CargoInfoFocus);
            if (focus == "ALL")
                return false;
            if (focus == "FULL")
                return GetAverageFillRatio(group.FillRatios) >= 0.95f;
            if (focus == "OFFLINE")
                return HasDisabledCargoBlock(group);
            if (focus == "REACHABLE")
                return HasOnlineCargoBlock(group);
            if (focus == "ISOLATED")
                return HasDisabledCargoBlock(group);
            return CargoOverlayGroupHasCategory(group, focus);
        }

        static bool CargoOverlayGroupMatchesTransferSelection(CargoOverlayGroup group, UiState ui, string target)
        {
            if (group == null || group.Blocks == null || ui == null || ui.ActiveOverlay != OverlayMode.Cargo)
                return false;
            var items = string.Equals(target, "DEST", StringComparison.Ordinal) ? ui.CargoTransferDestItems : ui.CargoTransferSourceItems;
            if (items == null || items.Count == 0)
                return false;
            for (int i = 0; i < group.Blocks.Count; i++)
            {
                var block = group.Blocks[i];
                if (block == null)
                    continue;
                long id = block.EntityId;
                for (int j = 0; j < items.Count; j++)
                {
                    var selected = items[j] != null ? items[j].Block : null;
                    if (selected != null && selected.EntityId == id)
                        return true;
                }
            }
            return false;
        }
        static bool HasOnlineCargoBlock(CargoOverlayGroup group)
        {
            if (group == null || group.Blocks == null)
                return false;
            for (int i = 0; i < group.Blocks.Count; i++)
            {
                if (IsCargoBlockOnline(group.Blocks[i]))
                    return true;
            }
            return false;
        }

        static bool HasDisabledCargoBlock(CargoOverlayGroup group)
        {
            if (group == null || group.Blocks == null)
                return false;
            for (int i = 0; i < group.Blocks.Count; i++)
            {
                if (!IsCargoBlockOnline(group.Blocks[i]))
                    return true;
            }
            return false;
        }

        static bool CargoOverlayGroupHasCategory(CargoOverlayGroup group, string category)
        {
            if (group == null || group.Blocks == null)
                return false;
            category = NormalizeCargoInfoSelector(category);
            for (int i = 0; i < group.Blocks.Count; i++)
            {
                if (CargoBlockHasCategory(group.Blocks[i], category))
                    return true;
            }
            return false;
        }

        static bool CargoBlockHasCategory(IMyCubeBlock block, string category)
        {
            if (block == null || string.IsNullOrEmpty(category))
                return false;
            try
            {
                for (int invIndex = 0; invIndex < block.InventoryCount; invIndex++)
                {
                    var inventory = block.GetInventory(invIndex);
                    if (inventory == null)
                        continue;
                    var items = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
                    inventory.GetItems(items);
                    for (int i = 0; i < items.Count; i++)
                    {
                        var item = items[i];
                        if (CategorizeCargoItem(item.Type.TypeId, item.Type.SubtypeId) == category)
                            return true;
                    }
                }
            }
            catch
            {
            }
            return false;
        }
        static bool IsCargoBlockOnline(IMyCubeBlock block)
        {
            var functional = block as IMyFunctionalBlock;
            if (functional == null)
                return true;
            try
            {
                return functional.IsFunctional && functional.Enabled;
            }
            catch
            {
                return false;
            }
        }

        static bool IsCargoBlockReachable(IMyCubeBlock block, ConveyorTopology topology, Dictionary<long, bool> reachMap)
        {
            if (block == null)
                return false;
            if (topology == null || topology.Nodes == null)
                return IsCargoBlockOnline(block);
            if (reachMap != null)
            {
                bool working;
                return reachMap.TryGetValue(block.EntityId, out working) && working;
            }
            try
            {
                for (int i = 0; i < topology.Nodes.Count; i++)
                {
                    var node = topology.Nodes[i];
                    if (node != null && node.BlockEntityId == block.EntityId)
                        return node.IsWorking;
                }
            }
            catch
            {
            }
            return false;
        }

        static string CargoActionLineLabel(OverlayBlockInfoLine line)
        {
            if (line == null || string.IsNullOrWhiteSpace(line.Text))
                return "ACTION";
            string text = line.Text;
            if (text.StartsWith("Action: ", StringComparison.OrdinalIgnoreCase))
                text = text.Substring(8);
            if (line.TerminalBlocks != null && line.TerminalBlocks.Count > 1)
                text += " x" + line.TerminalBlocks.Count.ToString();
            return text;
        }

        static string ShortenCargoActionText(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "ACTION";
            text = text.ToUpperInvariant();
            return text.Length <= 24 ? text : text.Substring(0, 23) + ".";
        }

        static string CargoFilterDisplayLabel(string value)
        {
            value = NormalizeCargoInfoSelector(value);
            if (value == "COMP") return "COMPONENTS";
            if (value == "ALL") return "ALL";
            return value;
        }

        static string CargoFilterHeaderLabel(string value)
        {
            value = NormalizeCargoInfoSelector(value);
            if (value == "COMP") return "COMP";
            if (value == "CONSUMABLE") return "CONS";
            return CargoFilterDisplayLabel(value);
        }

        static string FormatCargoAmount(float amount)
        {
            if (amount >= 1000000f)
                return (amount / 1000000f).ToString("0.0") + "M";
            if (amount >= 1000f)
                return (amount / 1000f).ToString("0.0") + "k";
            return ((int)Math.Round(amount)).ToString();
        }
        static string FormatCargoVolume(float volume)
        {
            float liters = volume * 1000f;
            if (liters >= 1000000f)
                return (liters / 1000000f).ToString("0.0") + "ML";
            if (liters >= 1000f)
                return (liters / 1000f).ToString("0.0") + "kL";
            return ((int)Math.Round(liters)).ToString() + "L";
        }

    }
}























































