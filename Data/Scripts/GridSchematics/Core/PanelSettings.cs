using System;
using VRageMath;

namespace PingoPete.GridSchematics
{
    public partial class GridSchematicsSession
    {
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
    }
}
