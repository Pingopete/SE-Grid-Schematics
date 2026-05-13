using Sandbox.ModAPI;
using Sandbox.Game;
using Sandbox.Game.Entities;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PingoPete.GridSchematics
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class GridSchematicsSession : MySessionComponentBase
    {
        const string TAG = "[GRID_SCHEMATICS]";
        const int MAX_RES = 512;
        const int DEFAULT_RES = 256;
        const int SETTINGS_CHECK_EVERY_TICKS = 180;
        const float LCD_MARGIN = 1f;

        enum ScanView
        {
            Top,
            Front,
            Side
        }

        enum FillMode
        {
            Solid,
            Thickness,
            Density
        }

        struct LineSeg
        {
            public Vector2 A;
            public Vector2 B;

            public LineSeg(Vector2 a, Vector2 b)
            {
                A = a;
                B = b;
            }
        }

        struct RayMetrics
        {
            public bool Occupied;
            public float Thickness;
            public float Density;
        }

        int _tick;
        bool _initialized;
        bool _startupPingSent;

        string _lastAppliedCustomData = "";

        IMyTextPanel _panel;
        IMyCubeGrid _hostGrid;
        IMyCubeGrid _basisGrid;
        Vector3D _scanCenter;

        readonly List<IMyCubeGrid> _constructGrids = new List<IMyCubeGrid>();
        readonly HashSet<long> _constructGridIds = new HashSet<long>();
        BoundingBoxD _constructAabb;
        int _constructGridCount;

        Vector3D _axisLong;
        Vector3D _axisMid;
        Vector3D _axisShort;

        readonly List<IHitInfo> _hits = new List<IHitInfo>();
        readonly List<LineSeg> _msLines = new List<LineSeg>(65536);

        readonly bool[,] _occupied = new bool[MAX_RES, MAX_RES];
        readonly float[,] _thickness = new float[MAX_RES, MAX_RES];
        readonly float[,] _density = new float[MAX_RES, MAX_RES];

        readonly StringBuilder _text = new StringBuilder(4096);
        readonly Stopwatch _sw = new Stopwatch();

        int _activeRes = DEFAULT_RES;
        ScanView _activeView = ScanView.Top;
        FillMode _fillMode = FillMode.Thickness;

        int _lastRays;
        int _lastHits;
        int _lastMarchLines;
        double _lastScanMs;
        double _lastMarchMs;
        float _maxThickness;
        float _maxDensity;
        string _lastStatus = "Waiting for panel named [GRID_SCHEMATICS]";

        public override void UpdateAfterSimulation()
        {
            if (MyAPIGateway.Session == null)
                return;

            _tick++;

            if (!_startupPingSent)
            {
                _startupPingSent = true;
                DebugOut("Session running");
            }

            if (!_initialized)
            {
                _initialized = true;
                FindPanelAndScan(true);
                return;
            }

            if (_tick % SETTINGS_CHECK_EVERY_TICKS == 0)
            {
                FindPanelAndScan(false);
            }
        }

        void DebugOut(string msg)
        {
            string line = "[GridSchematics] " + msg;
            MyLog.Default.WriteLineAndConsole(line);

            try
            {
                if (MyAPIGateway.Utilities != null)
                    MyAPIGateway.Utilities.ShowNotification(line, 2500, MyFontEnum.Green);
            }
            catch
            {
            }
        }

        void FindPanelAndScan(bool forceRescan)
        {
            try
            {
                _panel = null;
                _hostGrid = null;
                _basisGrid = null;
                _constructGrids.Clear();
                _constructGridIds.Clear();
                _constructGridCount = 0;

                HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(entities, e => e is IMyCubeGrid);

                foreach (var entity in entities)
                {
                    var grid = entity as IMyCubeGrid;
                    if (grid == null || grid.MarkedForClose)
                        continue;

                    var cubeGrid = grid as MyCubeGrid;
                    if (cubeGrid == null)
                        continue;

                    foreach (var fat in cubeGrid.GetFatBlocks())
                    {
                        var p = fat as IMyTextPanel;
                        if (p == null)
                            continue;

                        if ((p.CustomName ?? "").IndexOf(TAG, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            _panel = p;
                            _hostGrid = p.CubeGrid;
                            break;
                        }
                    }

                    if (_panel != null)
                        break;
                }

                if (_panel == null || _hostGrid == null)
                {
                    _lastStatus = "No LCD/text panel found with name tag " + TAG;
                    DebugOut(_lastStatus);
                    return;
                }

                string currentCustomData = (_panel.CustomData ?? "").Trim();
                bool settingsChanged = !string.Equals(
                    currentCustomData,
                    _lastAppliedCustomData,
                    StringComparison.Ordinal
                );

                if (!forceRescan && !settingsChanged)
                    return;

                _lastAppliedCustomData = currentCustomData;
                ParsePanelSettings();

                if (!BuildConstructGridSet())
                {
                    _lastStatus = "Failed to build construct grid set";
                    TryWriteTextOnly(BuildAsciiReport());
                    DebugOut(_lastStatus);
                    return;
                }

                ChooseBasisGridAndAxes();
                ScanOrthographic();
                BuildMarchingSquaresLines();
                DrawPreview();
            }
            catch (Exception e)
            {
                _lastStatus = "ERROR:\n" + e;
                TryWriteTextOnly(_lastStatus);
                DebugOut("Exception: " + e.Message);
                MyLog.Default.WriteLineAndConsole("[GridSchematics] " + e);
            }
        }

        bool BuildConstructGridSet()
        {
            _constructGrids.Clear();
            _constructGridIds.Clear();
            _constructGridCount = 0;

            if (_hostGrid == null)
                return false;

            bool haveBounds = false;
            BoundingBoxD combined = new BoundingBoxD();

            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities, e => e is IMyCubeGrid);

            foreach (var entity in entities)
            {
                var grid = entity as IMyCubeGrid;
                if (grid == null || grid.MarkedForClose)
                    continue;

                bool sameConstruct = false;
                try
                {
                    sameConstruct = grid.IsSameConstructAs(_hostGrid);
                }
                catch
                {
                    sameConstruct = ReferenceEquals(grid, _hostGrid);
                }

                if (!sameConstruct)
                    continue;

                _constructGrids.Add(grid);
                _constructGridIds.Add(grid.EntityId);
                _constructGridCount++;

                if (!haveBounds)
                {
                    combined = grid.WorldAABB;
                    haveBounds = true;
                }
                else
                {
                    combined.Include(grid.WorldAABB);
                }
            }

            if (!haveBounds || _constructGridCount == 0)
                return false;

            _constructAabb = combined;
            _scanCenter = combined.Center;
            return true;
        }

        void ChooseBasisGridAndAxes()
        {
            _basisGrid = _hostGrid;

            double bestVolume = -1.0;
            for (int i = 0; i < _constructGrids.Count; i++)
            {
                var g = _constructGrids[i];
                var b = g.WorldAABB;
                Vector3D s = b.Size;
                double v = s.X * s.Y * s.Z;
                if (v > bestVolume)
                {
                    bestVolume = v;
                    _basisGrid = g;
                }
            }

            MatrixD wm = (_basisGrid ?? _hostGrid).WorldMatrix;
            Vector3D a = wm.Right;
            Vector3D b2 = wm.Up;
            Vector3D c = wm.Forward;

            double ea = ProjectedHalfExtent(_constructAabb, _scanCenter, a);
            double eb = ProjectedHalfExtent(_constructAabb, _scanCenter, b2);
            double ec = ProjectedHalfExtent(_constructAabb, _scanCenter, c);

            Vector3D[] axes = new Vector3D[] { a, b2, c };
            double[] exts = new double[] { ea, eb, ec };

            for (int i = 0; i < 3; i++)
            {
                for (int j = i + 1; j < 3; j++)
                {
                    if (exts[j] > exts[i])
                    {
                        double te = exts[i];
                        exts[i] = exts[j];
                        exts[j] = te;

                        Vector3D ta = axes[i];
                        axes[i] = axes[j];
                        axes[j] = ta;
                    }
                }
            }

            _axisLong = axes[0];
            _axisMid = axes[1];
            _axisShort = axes[2];
        }

        void ParsePanelSettings()
        {
            _activeView = ScanView.Top;
            _activeRes = DEFAULT_RES;
            _fillMode = FillMode.Thickness;

            string cd = _panel != null ? (_panel.CustomData ?? "") : "";
            if (string.IsNullOrWhiteSpace(cd))
                return;

            string[] lines = cd.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < lines.Length; i++)
            {
                string raw = lines[i];
                if (string.IsNullOrWhiteSpace(raw))
                    continue;

                int eq = raw.IndexOf('=');
                if (eq < 0)
                    continue;

                string key = raw.Substring(0, eq).Trim().ToUpperInvariant();
                string val = raw.Substring(eq + 1).Trim().ToUpperInvariant();

                if (key == "VIEW")
                {
                    if (val == "TOP") _activeView = ScanView.Top;
                    else if (val == "FRONT") _activeView = ScanView.Front;
                    else if (val == "SIDE") _activeView = ScanView.Side;
                }
                else if (key == "RES")
                {
                    int parsed;
                    if (int.TryParse(val, out parsed))
                    {
                        if (parsed <= 64) _activeRes = 64;
                        else if (parsed <= 96) _activeRes = 96;
                        else if (parsed <= 128) _activeRes = 128;
                        else if (parsed <= 192) _activeRes = 192;
                        else if (parsed <= 256) _activeRes = 256;
                        else if (parsed <= 384) _activeRes = 384;
                        else _activeRes = 512;
                    }
                }
                else if (key == "FILLMODE")
                {
                    if (val == "SOLID") _fillMode = FillMode.Solid;
                    else if (val == "THICKNESS") _fillMode = FillMode.Thickness;
                    else if (val == "DENSITY") _fillMode = FillMode.Density;
                }
            }
        }

        void GetViewAxes(out Vector3D viewRight, out Vector3D viewUp2D, out Vector3D viewDepth)
        {
            switch (_activeView)
            {
                default:
                case ScanView.Top:
                    viewRight = _axisMid;
                    viewUp2D = _axisLong;
                    viewDepth = _axisShort;
                    break;

                case ScanView.Front:
                    viewRight = _axisMid;
                    viewUp2D = _axisShort;
                    viewDepth = _axisLong;
                    break;

                case ScanView.Side:
                    viewRight = _axisLong;
                    viewUp2D = _axisShort;
                    viewDepth = _axisMid;
                    break;
            }
        }

        void ScanOrthographic()
        {
            Array.Clear(_occupied, 0, _occupied.Length);
            Array.Clear(_thickness, 0, _thickness.Length);
            Array.Clear(_density, 0, _density.Length);
            _msLines.Clear();

            _lastRays = 0;
            _lastHits = 0;
            _lastMarchLines = 0;
            _maxThickness = 0f;
            _maxDensity = 0f;

            Vector3D viewRight;
            Vector3D viewUp2D;
            Vector3D viewDepth;
            GetViewAxes(out viewRight, out viewUp2D, out viewDepth);

            BoundingBoxD box = _constructAabb;
            Vector3D center = _scanCenter;

            double extentRight = ProjectedHalfExtent(box, center, viewRight) * 1.01;
            double extentUp = ProjectedHalfExtent(box, center, viewUp2D) * 1.01;
            double extentDepth = ProjectedHalfExtent(box, center, viewDepth) * 1.20 + 8.0;

            if (extentRight < 1) extentRight = 1;
            if (extentUp < 1) extentUp = 1;
            if (extentDepth < 5) extentDepth = 5;

            _sw.Reset();
            _sw.Start();

            for (int y = 0; y < _activeRes; y++)
            {
                double fy = ((y + 0.5) / _activeRes - 0.5) * 2.0;

                for (int x = 0; x < _activeRes; x++)
                {
                    double fx = ((x + 0.5) / _activeRes - 0.5) * 2.0;
                    Vector3D planePoint = center + viewRight * (fx * extentRight) + viewUp2D * (fy * extentUp);
                    Vector3D start = planePoint + viewDepth * extentDepth;
                    Vector3D end = planePoint - viewDepth * extentDepth;

                    _lastRays++;

                    RayMetrics rm = RayMeasureConstruct(start, end);
                    if (rm.Occupied)
                    {
                        _occupied[x, y] = true;
                        _thickness[x, y] = rm.Thickness;
                        _density[x, y] = rm.Density;
                        _lastHits++;

                        if (rm.Thickness > _maxThickness) _maxThickness = rm.Thickness;
                        if (rm.Density > _maxDensity) _maxDensity = rm.Density;
                    }
                }
            }

            _sw.Stop();
            _lastScanMs = _sw.Elapsed.TotalMilliseconds;
            _lastStatus = "Scan complete";
        }

        RayMetrics RayMeasureConstruct(Vector3D start, Vector3D end)
        {
            RayMetrics result = new RayMetrics();
            _hits.Clear();

            try
            {
                MyAPIGateway.Physics.CastRay(start, end, _hits);
            }
            catch (Exception e)
            {
                _lastStatus = "CastRay failed: " + e.Message;
                return result;
            }

            if (_hits.Count == 0)
                return result;

            double rayLen = Vector3D.Distance(start, end);
            double first = double.MaxValue;
            double last = double.MinValue;
            int validCount = 0;

            for (int i = 0; i < _hits.Count; i++)
            {
                IMyEntity ent = _hits[i].HitEntity;
                bool valid = false;

                while (ent != null)
                {
                    var grid = ent as IMyCubeGrid;
                    if (grid != null && _constructGridIds.Contains(grid.EntityId))
                    {
                        valid = true;
                        break;
                    }

                    ent = ent.Parent;
                }

                if (!valid)
                    continue;

                double d = Vector3D.Distance(start, _hits[i].Position);
                validCount++;

                if (d < first) first = d;
                if (d > last) last = d;
            }

            if (validCount <= 0)
                return result;

            result.Occupied = true;
            result.Density = validCount;

            if (last >= first)
                result.Thickness = (float)Math.Min(rayLen, last - first);
            else
                result.Thickness = 0f;

            return result;
        }

        void BuildMarchingSquaresLines()
        {
            _msLines.Clear();

            _sw.Reset();
            _sw.Start();

            for (int y = 0; y < _activeRes - 1; y++)
            {
                for (int x = 0; x < _activeRes - 1; x++)
                {
                    bool bl = _occupied[x, y];
                    bool br = _occupied[x + 1, y];
                    bool tr = _occupied[x + 1, y + 1];
                    bool tl = _occupied[x, y + 1];

                    int mask = 0;
                    if (bl) mask |= 1;
                    if (br) mask |= 2;
                    if (tr) mask |= 4;
                    if (tl) mask |= 8;

                    if (mask == 0 || mask == 15)
                        continue;

                    Vector2 pL = new Vector2(x, y + 0.5f);
                    Vector2 pR = new Vector2(x + 1f, y + 0.5f);
                    Vector2 pB = new Vector2(x + 0.5f, y);
                    Vector2 pT = new Vector2(x + 0.5f, y + 1f);

                    switch (mask)
                    {
                        case 1:  AddMsLine(pL, pB); break;
                        case 2:  AddMsLine(pB, pR); break;
                        case 3:  AddMsLine(pL, pR); break;
                        case 4:  AddMsLine(pR, pT); break;
                        case 5:  AddMsLine(pL, pT); AddMsLine(pB, pR); break;
                        case 6:  AddMsLine(pB, pT); break;
                        case 7:  AddMsLine(pL, pT); break;
                        case 8:  AddMsLine(pT, pL); break;
                        case 9:  AddMsLine(pT, pB); break;
                        case 10: AddMsLine(pT, pR); AddMsLine(pL, pB); break;
                        case 11: AddMsLine(pT, pR); break;
                        case 12: AddMsLine(pR, pL); break;
                        case 13: AddMsLine(pB, pR); break;
                        case 14: AddMsLine(pL, pB); break;
                    }
                }
            }

            _sw.Stop();
            _lastMarchMs = _sw.Elapsed.TotalMilliseconds;
            _lastMarchLines = _msLines.Count;
        }

        void AddMsLine(Vector2 a, Vector2 b)
        {
            _msLines.Add(new LineSeg(a, b));
        }

        double ProjectedHalfExtent(BoundingBoxD box, Vector3D center, Vector3D axis)
        {
            Vector3D[] corners = box.GetCorners();
            double max = 0;

            for (int i = 0; i < corners.Length; i++)
            {
                double d = Math.Abs(Vector3D.Dot(corners[i] - center, axis));
                if (d > max)
                    max = d;
            }

            return max;
        }

        void DrawPreview()
        {
            if (_panel == null)
                return;

            var surface = _panel as IMyTextSurface;
            if (surface == null)
            {
                TryWriteTextOnly(BuildAsciiReport());
                return;
            }

            try
            {
                surface.ContentType = ContentType.SCRIPT;
                surface.Script = "";
                surface.ScriptBackgroundColor = Color.Black;

                Vector2 size = surface.SurfaceSize;

                float usableW = size.X - LCD_MARGIN * 2f;
                float usableH = size.Y - LCD_MARGIN * 2f;

                float scaleX = usableW / _activeRes;
                float scaleY = usableH / _activeRes;
                float scale = Math.Min(scaleX, scaleY);

                float drawW = _activeRes * scale;
                float drawH = _activeRes * scale;

                Vector2 origin = new Vector2(
                    LCD_MARGIN + (usableW - drawW) * 0.5f,
                    LCD_MARGIN + (usableH - drawH) * 0.5f
                );

                using (var frame = surface.DrawFrame())
                {
                    frame.Add(new MySprite(
                        SpriteType.TEXTURE,
                        "SquareSimple",
                        size * 0.5f,
                        size,
                        Color.Black
                    ));

                    for (int y = 0; y < _activeRes; y++)
                    {
                        for (int x = 0; x < _activeRes; x++)
                        {
                            if (!_occupied[x, y])
                                continue;

                            float intensity = 1f;

                            switch (_fillMode)
                            {
                                case FillMode.Solid:
                                    intensity = 0.55f;
                                    break;

                                case FillMode.Thickness:
                                    intensity = _maxThickness > 0.0001f ? (_thickness[x, y] / _maxThickness) : 0f;
                                    intensity = 0.06f + intensity * 0.94f;
                                    break;

                                case FillMode.Density:
                                    intensity = _maxDensity > 0.0001f ? (_density[x, y] / _maxDensity) : 0f;
                                    intensity = 0.06f + intensity * 0.94f;
                                    break;
                            }

                            byte c = (byte)MathHelper.Clamp((int)(intensity * 255f), 0, 255);

                            Vector2 pos = origin + new Vector2(
                                (x + 0.5f) * scale,
                                (_activeRes - y - 0.5f) * scale
                            );

                            frame.Add(new MySprite(
                                SpriteType.TEXTURE,
                                "SquareSimple",
                                pos,
                                new Vector2(Math.Max(1f, scale * 1.02f), Math.Max(1f, scale * 1.02f)),
                                new Color(c, c, c, c)
                            ));
                        }
                    }

                    float lineThickness = Math.Max(0.5f, scale * 0.22f);
                    for (int i = 0; i < _msLines.Count; i++)
                    {
                        Vector2 a = MsToScreen(_msLines[i].A, origin, scale);
                        Vector2 b = MsToScreen(_msLines[i].B, origin, scale);
                        AddLine(frame, a, b, Color.White, lineThickness);
                    }

                    string report =
                        "SCHEMA\n" +
                        _activeView + "\n" +
                        _fillMode + "\n" +
                        _activeRes + "\n" +
                        "G:" + _constructGridCount + "\n" +
                        "R:" + _lastRays + "\n" +
                        "H:" + _lastHits + "\n" +
                        "L:" + _lastMarchLines + "\n" +
                        "S:" + _lastScanMs.ToString("0") + "ms\n" +
                        "M:" + _lastMarchMs.ToString("0") + "ms";

                    frame.Add(new MySprite(
                        SpriteType.TEXT,
                        report,
                        new Vector2(4f, 4f),
                        null,
                        Color.Lime,
                        "Debug",
                        TextAlignment.LEFT,
                        0.42f
                    ));
                }
            }
            catch (Exception e)
            {
                _lastStatus = "Draw failed: " + e.Message;
                TryWriteTextOnly(BuildAsciiReport());
            }
        }

        Vector2 MsToScreen(Vector2 p, Vector2 origin, float scale)
        {
            return origin + new Vector2(p.X * scale, (_activeRes - p.Y) * scale);
        }

        void AddLine(MySpriteDrawFrame frame, Vector2 a, Vector2 b, Color color, float thickness)
        {
            Vector2 d = b - a;
            float len = d.Length();
            if (len < 0.001f)
                return;

            float rot = (float)Math.Atan2(d.Y, d.X);

            frame.Add(new MySprite(
                SpriteType.TEXTURE,
                "SquareSimple",
                (a + b) * 0.5f,
                new Vector2(len, thickness),
                color,
                null,
                TextAlignment.CENTER,
                rot
            ));
        }

        string BuildAsciiReport()
        {
            _text.Clear();
            _text.AppendLine("GRID SCHEMATICS");
            _text.AppendLine("View: " + _activeView);
            _text.AppendLine("Fill: " + _fillMode);
            _text.AppendLine("Res: " + _activeRes + "x" + _activeRes);
            _text.AppendLine("Construct Grids: " + _constructGridCount);
            _text.AppendLine("Rays: " + _lastRays);
            _text.AppendLine("Hits: " + _lastHits);
            _text.AppendLine("MS Lines: " + _lastMarchLines);
            _text.AppendLine("Scan: " + _lastScanMs.ToString("0.00") + " ms");
            _text.AppendLine("March: " + _lastMarchMs.ToString("0.00") + " ms");
            _text.AppendLine("Max Thickness: " + _maxThickness.ToString("0.00"));
            _text.AppendLine("Max Density: " + _maxDensity.ToString("0.00"));
            _text.AppendLine(_lastStatus);
            return _text.ToString();
        }

        void TryWriteTextOnly(string s)
        {
            try
            {
                if (_panel != null)
                {
                    _panel.ContentType = ContentType.TEXT_AND_IMAGE;
                    _panel.WriteText(s ?? "");
                }
            }
            catch
            {
            }
        }
    }
}
