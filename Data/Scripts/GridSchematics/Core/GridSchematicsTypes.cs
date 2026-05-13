using VRageMath;

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
