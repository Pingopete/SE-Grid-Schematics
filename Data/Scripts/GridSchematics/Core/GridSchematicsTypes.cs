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
    }
}
