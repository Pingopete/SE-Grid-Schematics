using Sandbox.ModAPI;
using Sandbox.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace PingoPete.GridSchematics
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public partial class GridSchematicsSession : MySessionComponentBase
    {
        const string TAG = "[GRID_SCHEMATICS]";
        const int MAX_RES = 512;
        const int DEFAULT_RES = 256;
        const int SETTINGS_CHECK_EVERY_TICKS = 180;
        const float LCD_MARGIN = 1f;

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

                if (!TryFindTaggedPanel())
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
    }
}
