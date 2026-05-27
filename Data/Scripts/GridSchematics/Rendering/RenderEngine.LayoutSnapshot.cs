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
            sb.AppendLine("// Paste this into SE Sprite LCD Layout Tool via Edit -> Paste Layout Code.");
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

        static void AppendSnapshotSprite(StringBuilder sb, MySprite sprite, int index)
        {
            sb.AppendLine("        // [" + index.ToString(CultureInfo.InvariantCulture) + "] " + EscapeSnapshotString(sprite.Data));
            sb.AppendLine("        frame.Add(new MySprite");
            sb.AppendLine("        {");
            sb.AppendLine("            Type           = SpriteType." + sprite.Type + ",");
            sb.AppendLine("            Data           = \"" + EscapeSnapshotString(sprite.Data) + "\",");
            if (sprite.Position.HasValue)
                sb.AppendLine("            Position       = new Vector2(" + FormatSnapshotFloat(sprite.Position.Value.X) + "f, " + FormatSnapshotFloat(sprite.Position.Value.Y) + "f),");
            if (sprite.Size.HasValue)
                sb.AppendLine("            Size           = new Vector2(" + FormatSnapshotFloat(sprite.Size.Value.X) + "f, " + FormatSnapshotFloat(sprite.Size.Value.Y) + "f),");
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
            return "full";
        }
    }
}