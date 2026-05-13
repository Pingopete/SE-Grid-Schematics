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

        static void DrawCargoWidgetFrame(MySpriteDrawFrame frame, CargoWidgetRect widget, string label)
        {
            var metrics = UiLayout.BuildMetrics((int)widget.Width, (int)widget.Height);
            var center = SnapPoint(new Vector2(widget.X + widget.Width * 0.5f, widget.Y + widget.Height * 0.5f));
            var size = new Vector2(SnapPixelSize(widget.Width), SnapPixelSize(widget.Height));
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", center, size, UiPanelFill));
            DrawScreenRectBorder(frame, center, size, UiAccentDim);
            AddSprite(frame, new MySprite(SpriteType.TEXT, label, SnapPoint(new Vector2(widget.X + metrics.S(5f), widget.Y + metrics.S(1f))), null, UiTextMuted, CurrentTextFontId, TextAlignment.LEFT, metrics.SmallText));
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

            AddSprite(frame, new MySprite(SpriteType.TEXT, FormatCargoVolume(summary.CurrentVolume) + "/" + FormatCargoVolume(summary.MaxVolume), new Vector2(barLeft, totalY - barHeight * 0.5f - metrics.S(10f)), null, UiTextMuted, CurrentTextFontId, TextAlignment.LEFT, metrics.SmallText));
            DrawInfoBarGlyph(frame, new Vector2(barCenterX, totalY), barWidth, barHeight, summary.FillRatio, fillTextColor, new Color(storage.R, storage.G, storage.B, 42));
            AddSprite(frame, new MySprite(SpriteType.TEXT, ((int)Math.Round(summary.FillRatio * 100f)).ToString() + "%", new Vector2(fillX, totalY + barHeight * 0.5f + metrics.S(3f)), null, fillTextColor, CurrentTextFontId, TextAlignment.CENTER, metrics.SmallText));

            DrawCargoCapacitySpine(frame, widget.X + metrics.S(7f), spineY, barWidth, barHeight, summary);

            float marker80 = barLeft + barWidth * 0.80f;
            float marker95 = barLeft + barWidth * 0.95f;
            DrawCargoThresholdMarker(frame, marker80, totalY, barHeight, "80%", CargoCautionColor());
            DrawCargoThresholdMarker(frame, marker95, totalY, barHeight, "95%", UiWarning);

            DrawCargoSpineLegend(frame, widget, storage);
        }

        static void DrawCargoThresholdMarker(MySpriteDrawFrame frame, float x, float y, float barHeight, string label, Color color)
        {
            var metrics = UiLayout.BuildMetrics(512, 512);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(x, y), new Vector2(metrics.S(2f), barHeight + metrics.S(5f)), color));
            float tipY = y - barHeight * 0.5f - metrics.S(2f);
            AddSprite(frame, new MySprite(SpriteType.TEXTURE, "Triangle", new Vector2(x, tipY - metrics.S(4f)), new Vector2(metrics.S(12f), metrics.S(9f)), color, null, TextAlignment.CENTER, (float)Math.PI));
            AddSprite(frame, new MySprite(SpriteType.TEXT, label, new Vector2(x, tipY - metrics.S(19f)), null, color, CurrentTextFontId, TextAlignment.CENTER, metrics.SmallText));
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

            AddSprite(frame, new MySprite(SpriteType.TEXT, ((int)Math.Round(summary.FillRatio * 100f)).ToString() + "%", new Vector2(widget.X + widget.Width * 0.5f, widget.Y + widget.Height - 18f), null, UiText, CurrentTextFontId, TextAlignment.CENTER, 0.42f));
            AddSprite(frame, new MySprite(SpriteType.TEXT, FormatCargoVolume(summary.CurrentVolume) + "/" + FormatCargoVolume(summary.MaxVolume), new Vector2(widget.X + widget.Width * 0.5f, widget.Y + widget.Height - 8f), null, UiTextMuted, CurrentTextFontId, TextAlignment.CENTER, 0.25f));
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
            string[] labels = new[] { "ORE", "ING", "CMP", "AM", "ICE", "OTH" };
            string[] categories = new[] { "ORE", "INGOT", "COMP", "AMMO", "ICE", "OTHER" };
            float keyY = widget.Y + widget.Height * 0.76f;
            float textY = keyY + metrics.S(13f);
            float keySize = metrics.S(18f);
            for (int i = 0; i < labels.Length; i++)
            {
                float x = widget.X + metrics.S(8f) + i * (barWidth / labels.Length);
                Color color = GetCargoCategoryColor(categories[i], storage);
                AddSprite(frame, new MySprite(SpriteType.TEXTURE, "SquareSimple", new Vector2(x + keySize * 0.5f, keyY), new Vector2(keySize, keySize), color));
                AddSprite(frame, new MySprite(SpriteType.TEXT, labels[i], new Vector2(x, textY), null, UiTextMuted, CurrentTextFontId, TextAlignment.LEFT, metrics.LargeText * 1.15f));
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
                AddSprite(frame, new MySprite(SpriteType.TEXT, ShortenCargoName(item.Name, textMaxWidth), new Vector2(widget.X + metrics.S(14f), y - metrics.S(7f)), null, UiTextMuted, CurrentTextFontId, TextAlignment.LEFT, metrics.SmallText));
                DrawInfoBarGlyph(frame, new Vector2(widget.X + widget.Width - metrics.S(7f) - barWidth * 0.5f, y), barWidth, metrics.S(4f), ratio, itemColor, UiAccentGhost);
            }

            if (summary.ItemTypeCount > shown)
                AddSprite(frame, new MySprite(SpriteType.TEXT, "+" + (summary.ItemTypeCount - shown).ToString(), new Vector2(widget.X + widget.Width - metrics.S(5f), widget.Y + widget.Height - metrics.S(10f)), null, UiSelected, CurrentTextFontId, TextAlignment.RIGHT, metrics.MediumText));
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
            public float CurrentVolume;
            public float MaxVolume;
            public float FillRatio;
            public int ItemTypeCount;
            public List<CargoPanelItem> TopItems = new List<CargoPanelItem>();
            public Dictionary<string, float> CategoryVolumes = new Dictionary<string, float>();
        }

        public class CargoPanelItem
        {
            public string Name;
            public string Category;
            public float Volume;
        }

        static CargoPanelSummary GetOrBuildCargoSummaryForPanel(GridSchematicsLcdApp app, OverlayBlockInfo selectedInfo, bool isCursorOnlyRender)
        {
            if (app == null)
                return BuildCargoSummaryForPanel(app, selectedInfo);

            string cacheKey = BuildCargoSummaryCacheKey(app, selectedInfo);
            if (isCursorOnlyRender && app.Ui != null &&
                string.Equals(app.Ui.CachedCargoSummaryKey, cacheKey, StringComparison.Ordinal) &&
                app.Ui.CachedCargoSummary != null)
            {
                return app.Ui.CachedCargoSummary;
            }

            var summary = BuildCargoSummaryForPanel(app, selectedInfo);
            if (app.Ui != null)
            {
                app.Ui.CachedCargoSummaryKey = cacheKey;
                app.Ui.CachedCargoSummary = summary;
            }

            return summary;
        }

        static string BuildCargoSummaryCacheKey(GridSchematicsLcdApp app, OverlayBlockInfo selectedInfo)
        {
            if (app == null || app.OwnerBlock == null)
                return "unbound:" + (selectedInfo != null ? selectedInfo.Id : "none");

            long panelId = app.OwnerBlock.EntityId;
            if (selectedInfo == null || selectedInfo.Blocks == null || selectedInfo.Blocks.Count <= 0)
                return "total:" + panelId + ":" + (selectedInfo != null ? selectedInfo.Id : "all");

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

            return "selected:" + panelId + ":" + hash;
        }

        static CargoPanelSummary BuildCargoSummaryForPanel(GridSchematicsLcdApp app, OverlayBlockInfo selectedInfo)
        {
            if (app == null || app.OwnerBlock == null || app.OwnerBlock.CubeGrid == null)
                return null;

            var summary = new CargoPanelSummary();
            var itemMap = new Dictionary<string, CargoPanelItem>();
            try
            {
                if (selectedInfo != null && selectedInfo.Blocks != null && selectedInfo.Blocks.Count > 0)
                {
                    for (int i = 0; i < selectedInfo.Blocks.Count; i++)
                    {
                        AddCargoBlockToSummary(selectedInfo.Blocks[i], summary, itemMap);
                    }
                }
                else
                {
                    var grids = new List<IMyCubeGrid>();
                    MyAPIGateway.GridGroups.GetGroup(app.OwnerBlock.CubeGrid, GridLinkTypeEnum.Physical, grids);
                    if (grids.Count == 0)
                        grids.Add(app.OwnerBlock.CubeGrid);

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
                            AddCargoBlockToSummary(blocks[b].FatBlock, summary, itemMap);
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

        static void AddCargoBlockToSummary(IMyCubeBlock fat, CargoPanelSummary summary, Dictionary<string, CargoPanelItem> itemMap)
        {
            if (fat == null || summary == null || itemMap == null)
                return;

            try
            {
                int inventoryCount = fat.InventoryCount;
                for (int i = 0; i < inventoryCount; i++)
                {
                    var inventory = fat.GetInventory(i);
                    if (inventory == null)
                        continue;

                    summary.CurrentVolume += (float)inventory.CurrentVolume;
                    summary.MaxVolume += (float)inventory.MaxVolume;

                    var items = new List<VRage.Game.ModAPI.Ingame.MyInventoryItem>();
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

                            string key = typeId + "/" + subtype;
                            CargoPanelItem aggregate;
                            if (!itemMap.TryGetValue(key, out aggregate))
                            {
                                aggregate = new CargoPanelItem
                                {
                                    Name = string.IsNullOrEmpty(subtype) ? typeId : subtype,
                                    Category = category,
                                    Volume = 0f
                                };
                                itemMap[key] = aggregate;
                            }

                            aggregate.Volume += volume;
                        }
                        catch
                        {
                        }
                    }
                }
            }
            catch
            {
            }
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
                return "ICE";
            if (type.IndexOf("Ore", StringComparison.OrdinalIgnoreCase) >= 0)
                return "ORE";
            if (type.IndexOf("Ingot", StringComparison.OrdinalIgnoreCase) >= 0)
                return "INGOT";
            if (type.IndexOf("Component", StringComparison.OrdinalIgnoreCase) >= 0)
                return "COMP";
            if (type.IndexOf("Ammo", StringComparison.OrdinalIgnoreCase) >= 0)
                return "AMMO";
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

            string[] categories = new[] { "ORE", "INGOT", "COMP", "AMMO", "ICE", "OTHER" };
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
            if (category == "AMMO")
                return new Color(220, 80, 75, 230);
            if (category == "ICE")
                return new Color(90, 210, 240, 230);
            if (category == "OTHER")
                return new Color(130, 145, 150, 200);
            return storage;
        }

        static Color GetCargoFillColor(Color storage, float ratio)
        {
            if (ratio >= 0.95f)
                return new Color(230, 70, 55, 240);
            if (ratio >= 0.80f)
                return CargoCautionColor();
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
                return "ICE";
            return string.Empty;
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
