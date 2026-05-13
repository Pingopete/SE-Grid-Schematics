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
    public partial class GridSchematicsSession
    {
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
    }
}
