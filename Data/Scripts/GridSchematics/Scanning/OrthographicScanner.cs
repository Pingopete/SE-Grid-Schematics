using Sandbox.ModAPI;
using System;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace PingoPete.GridSchematics
{
    public partial class GridSchematicsSession
    {
        // Samples occupied cells by casting orthographic physics rays through the construct.
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
    }
}
