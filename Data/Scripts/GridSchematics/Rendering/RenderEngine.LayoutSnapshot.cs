using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace GridSchematics
{
    public static partial class RenderEngine
    {
        const string LayoutSnapshotFileName = "GridSchematics_LcdSnapshot.cs";
        static readonly List<MySprite> LayoutSnapshotSprites = new List<MySprite>(512);
        static bool LayoutSnapshotRequested;
        static bool LayoutSnapshotCollecting;
        static string LayoutSnapshotScope = "full";
        static string LayoutSnapshotRequestedScope = "full";
        static string LayoutSnapshotLabel = "GridSchematics";
        static Vector2 LayoutSnapshotSurfaceSize;
        static Vector2 LayoutSnapshotTextureSize;
        static string LastLayoutSnapshotFileName = string.Empty;
        static int LastLayoutSnapshotSpriteCount;

        public static void RequestLayoutSnapshot(string label)
        {
            RequestLayoutSnapshot(label, "full");
        }

        public static void RequestLayoutSnapshot(string label, string scope)
        {
            LayoutSnapshotRequested = true;
            LayoutSnapshotRequestedScope = NormalizeSnapshotScope(scope);
            LayoutSnapshotLabel = string.IsNullOrWhiteSpace(label) ? "GridSchematics" : SanitizeSnapshotLabel(label);
            LastLayoutSnapshotFileName = string.Empty;
            LastLayoutSnapshotSpriteCount = 0;
        }

        public static string LastSnapshotFileName
        {
            get { return LastLayoutSnapshotFileName; }
        }

        public static int LastSnapshotSpriteCount
        {
            get { return LastLayoutSnapshotSpriteCount; }
        }

        static void BeginLayoutSnapshotFrame(GridSchematicsLcdApp app, IMyTextSurface surface, Vector2 surfaceSize)
        {
            if (!LayoutSnapshotRequested)
                return;

            LayoutSnapshotRequested = false;
            LayoutSnapshotCollecting = true;
            LayoutSnapshotScope = LayoutSnapshotRequestedScope;
            LayoutSnapshotSprites.Clear();
            LayoutSnapshotSurfaceSize = surfaceSize;
            LayoutSnapshotTextureSize = surface != null ? surface.TextureSize : surfaceSize;
            if (app != null && app.OwnerBlock != null && !string.IsNullOrWhiteSpace(app.OwnerBlock.CustomName))
                LayoutSnapshotLabel = SanitizeSnapshotLabel(app.OwnerBlock.CustomName);
        }

        static void CaptureLayoutSnapshotSprite(MySprite sprite)
        {
            if (!LayoutSnapshotCollecting || !ShouldCaptureLayoutSnapshotSprite(sprite))
                return;

            LayoutSnapshotSprites.Add(sprite);
        }

        static bool ShouldCaptureLayoutSnapshotSprite(MySprite sprite)
        {
            if (LayoutSnapshotScope == "full")
                return true;

            if (!sprite.Position.HasValue)
                return false;

            if (LayoutSnapshotScope == "drawer")
            {
                float top = LayoutSnapshotSurfaceSize.Y * 0.585f;
                return sprite.Position.Value.Y >= top;
            }

            if (LayoutSnapshotScope == "editor")
            {
                float chromeBottom = LayoutSnapshotSurfaceSize.Y * 0.045f;
                float drawerTop = LayoutSnapshotSurfaceSize.Y * 0.585f;
                float y = sprite.Position.Value.Y;
                return y <= chromeBottom || y >= drawerTop;
            }

            return true;
        }

        static void CancelLayoutSnapshotFrame()
        {
            LayoutSnapshotCollecting = false;
            LayoutSnapshotSprites.Clear();
        }

        static void CompleteLayoutSnapshotFrame()
        {
            if (!LayoutSnapshotCollecting)
                return;

            LayoutSnapshotCollecting = false;
            LastLayoutSnapshotSpriteCount = LayoutSnapshotSprites.Count;
            if (LayoutSnapshotSprites.Count == 0 || MyAPIGateway.Utilities == null)
            {
                LayoutSnapshotSprites.Clear();
                return;
            }

            try
            {
                using (TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(LayoutSnapshotFileName, typeof(GridSchematicsSession)))
                {
                    writer.Write(BuildLayoutSnapshotText());
                }

                LastLayoutSnapshotFileName = LayoutSnapshotFileName;
            }
            catch
            {
                LastLayoutSnapshotFileName = string.Empty;
            }
            finally
            {
                LayoutSnapshotSprites.Clear();
            }
        }

        static string BuildLayoutSnapshotText()
        {
            var sb = new StringBuilder(LayoutSnapshotSprites.Count * 180);
            sb.AppendLine("// -- LCD Snapshot --");
            sb.AppendLine("// Captured: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
            sb.AppendLine("// @SnapshotTag: " + LayoutSnapshotLabel);
            sb.AppendLine("// Scope: " + LayoutSnapshotScope);
            sb.AppendLine("// Surface: " + FormatSnapshotVector(LayoutSnapshotSurfaceSize) + " | Texture: " + FormatSnapshotVector(LayoutSnapshotTextureSize));
            sb.AppendLine("// Sprites: " + LayoutSnapshotSprites.Count.ToString(CultureInfo.InvariantCulture));
            sb.AppendLine("// FontIds: " + BuildSnapshotFontIdList());
            sb.AppendLine("// Paste this into SE Sprite LCD Layout Tool via Edit -> Paste Layout Code.");
            sb.AppendLine("// If full snapshots overflow the tool, capture /GSLCDSNAP editor or /GSLCDSNAP drawer.");
            sb.AppendLine();
            sb.AppendLine("using Sandbox.ModAPI;");
            sb.AppendLine("using VRage.Game.GUI.TextPanel;");
            sb.AppendLine("using VRageMath;");
            sb.AppendLine();
            sb.AppendLine("public void DrawSnapshot(IMyTextSurface surface)");
            sb.AppendLine("{");
            sb.AppendLine("    using (var frame = surface.DrawFrame())");
            sb.AppendLine("    {");

            for (int i = 0; i < LayoutSnapshotSprites.Count; i++)
            {
                AppendSnapshotSprite(sb, LayoutSnapshotSprites[i], i + 1);
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");
            return sb.ToString();
        }


        static string BuildSnapshotFontIdList()
        {
            var fontIds = new List<string>();
            for (int i = 0; i < LayoutSnapshotSprites.Count; i++)
            {
                string fontId = LayoutSnapshotSprites[i].FontId;
                if (string.IsNullOrWhiteSpace(fontId))
                    continue;

                bool exists = false;
                for (int j = 0; j < fontIds.Count; j++)
                {
                    if (string.Equals(fontIds[j], fontId, StringComparison.OrdinalIgnoreCase))
                    {
                        exists = true;
                        break;
                    }
                }

                if (!exists)
                    fontIds.Add(fontId);
            }

            if (fontIds.Count == 0)
                return "(none)";

            fontIds.Sort(StringComparer.OrdinalIgnoreCase);
            var sb = new StringBuilder();
            for (int i = 0; i < fontIds.Count; i++)
            {
                if (i > 0)
                    sb.Append(", ");
                sb.Append(fontIds[i]);
            }
            return sb.ToString();
        }
        static void AppendSnapshotSprite(StringBuilder sb, MySprite sprite, int index)
        {
            sb.AppendLine("        // [" + index.ToString(CultureInfo.InvariantCulture) + "] " + EscapeSnapshotString(sprite.Data));
            sb.AppendLine("        frame.Add(new MySprite");
            sb.AppendLine("        {");
            sb.AppendLine("            Type           = SpriteType." + sprite.Type + ",");
            sb.AppendLine("            Data           = \"" + EscapeSnapshotString(sprite.Data) + "\",");
            if (sprite.Position.HasValue)
                sb.AppendLine("            Position       = new Vector2(" + FormatSnapshotFloat(sprite.Position.Value.X) + "f, " + FormatSnapshotFloat(sprite.Position.Value.Y) + "f),");
            Vector2? size = sprite.Size;
            if (!size.HasValue && sprite.Type == SpriteType.TEXT)
                size = EstimateSnapshotTextSize(sprite);
            if (size.HasValue)
                sb.AppendLine("            Size           = new Vector2(" + FormatSnapshotFloat(size.Value.X) + "f, " + FormatSnapshotFloat(size.Value.Y) + "f),");
            if (sprite.Color.HasValue)
            {
                var color = sprite.Color.Value;
                sb.AppendLine("            Color          = new Color(" + color.R + ", " + color.G + ", " + color.B + ", " + color.A + "),");
            }
            if (!string.IsNullOrEmpty(sprite.FontId))
                sb.AppendLine("            FontId         = \"" + EscapeSnapshotString(sprite.FontId) + "\",");
            sb.AppendLine("            Alignment      = TextAlignment." + sprite.Alignment + ",");
            sb.AppendLine("            RotationOrScale = " + FormatSnapshotFloat(sprite.RotationOrScale) + "f,");
            sb.AppendLine("        });");
            sb.AppendLine();
        }


        static Vector2 EstimateSnapshotTextSize(MySprite sprite)
        {
            string text = sprite.Data ?? string.Empty;
            float scale = sprite.RotationOrScale;
            if (scale <= 0f)
                scale = 1f;

            string[] lines = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);
            float width = 1f;
            for (int i = 0; i < lines.Length; i++)
                width = Math.Max(width, EstimateUiTextWidth(lines[i], scale, sprite.FontId));

            float lineHeight = 28f * scale;
            float height = Math.Max(1f, lineHeight * Math.Max(1, lines.Length));
            float padding = Math.Max(1f, 2f * scale);
            return new Vector2(width + padding * 2f, height + padding * 2f);
        }
        static string FormatSnapshotVector(Vector2 value)
        {
            return FormatSnapshotFloat(value.X) + "x" + FormatSnapshotFloat(value.Y);
        }

        static string FormatSnapshotFloat(float value)
        {
            return value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        static string EscapeSnapshotString(string value)
        {
            if (value == null)
                return string.Empty;

            var sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (c == '\\')
                    sb.Append("\\\\");
                else if (c == '"')
                    sb.Append("\\\"");
                else if (c == '\r')
                    sb.Append("\\r");
                else if (c == '\n')
                    sb.Append("\\n");
                else if (c >= 0x7f)
                    sb.Append("\\u" + ((int)c).ToString("X4", CultureInfo.InvariantCulture));
                else
                    sb.Append(c);
            }
            return sb.ToString();
        }

        static string SanitizeSnapshotLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
                return "GridSchematics";

            var sb = new StringBuilder(label.Length);
            for (int i = 0; i < label.Length; i++)
            {
                char c = label[i];
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == ' ')
                    sb.Append(c);
            }

            return sb.Length == 0 ? "GridSchematics" : sb.ToString().Trim();
        }

        static string NormalizeSnapshotScope(string scope)
        {
            if (string.IsNullOrWhiteSpace(scope))
                return "full";

            scope = scope.Trim().ToLowerInvariant();
            if (scope == "drawer" || scope == "info" || scope == "panel")
                return "drawer";
            if (scope == "editor" || scope == "ui" || scope == "chrome")
                return "editor";
            return "full";
        }
    }
}



