using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;
using System.Collections.Generic;
using System;

namespace GridSchematics
{
    public class ShipGrid
    {
        public MyCubeSize GridSize { get; private set; }
        public ScanView ProjectionView { get; private set; }
        public long BasisGridEntityId { get; private set; }
        public MatrixD ReferenceMatrix { get; private set; }
        public float BasisGridSizeMeters { get; private set; }
        public bool UsesRemoteReference { get; private set; }
        public string ReferenceName { get; private set; }
        public int GridCount { get; private set; }
        public int BlockCount { get; private set; }
        public Vector3I Min { get; private set; }
        public Vector3I Max { get; private set; }
        public Vector3I Size3D { get; private set; }
        public Vector2I Min2D { get; private set; }
        public Vector2I Max2D { get; private set; }
        public Vector2I Size2D { get; private set; }
        public int DepthMin { get; private set; }
        public int DepthMax { get; private set; }
        public int DepthSize { get; private set; }
        public int Subdivisions { get; private set; }
        public List<ProjectedBlock> Blocks { get; private set; }

        public bool IsEmpty
        {
            get { return Size2D.X <= 0 || Size2D.Y <= 0 || DepthSize <= 0; }
        }

        public ShipGrid()
        {
            Blocks = new List<ProjectedBlock>();
        }

        public ProjectionTransform CreateTransform(ScreenZone viewport)
        {
            return ProjectionTransform.Create(this, viewport, 0);
        }

        public ProjectionTransform CreateTransform(ScreenZone viewport, int rotationSteps)
        {
            return ProjectionTransform.Create(this, viewport, rotationSteps);
        }

        public static ShipGrid BuildFromConstruct(List<IMyCubeGrid> grids, IMyCubeGrid basisGrid, IMyCubeBlock referenceBlock, ScanView view)
        {
            var shipGrid = new ShipGrid();
            if (grids == null || grids.Count == 0)
            {
                shipGrid.GridSize = MyCubeSize.Large;
                shipGrid.ProjectionView = view;
                shipGrid.BasisGridEntityId = 0;
                shipGrid.ReferenceMatrix = MatrixD.Identity;
                shipGrid.BasisGridSizeMeters = 2.5f;
                shipGrid.UsesRemoteReference = false;
                shipGrid.ReferenceName = "Grid";
                shipGrid.GridCount = 0;
                shipGrid.BlockCount = 0;
                shipGrid.Subdivisions = 5;
                shipGrid.Min = Vector3I.Zero;
                shipGrid.Max = Vector3I.Zero;
                shipGrid.Size3D = Vector3I.Zero;
                shipGrid.Min2D = Vector2I.Zero;
                shipGrid.Max2D = Vector2I.Zero;
                shipGrid.Size2D = Vector2I.Zero;
                shipGrid.DepthMin = 0;
                shipGrid.DepthMax = -1;
                shipGrid.DepthSize = 0;
                return shipGrid;
            }

            if (basisGrid == null)
                basisGrid = grids[0];

            shipGrid.GridSize = basisGrid.GridSizeEnum;
            shipGrid.ProjectionView = view;
            shipGrid.BasisGridEntityId = basisGrid.EntityId;
            shipGrid.ReferenceMatrix = referenceBlock != null ? referenceBlock.WorldMatrix : basisGrid.WorldMatrix;
            shipGrid.BasisGridSizeMeters = basisGrid.GridSize;
            shipGrid.UsesRemoteReference = referenceBlock != null;
            shipGrid.ReferenceName = GetReferenceName(referenceBlock);
            shipGrid.GridCount = grids.Count;
            shipGrid.Subdivisions = shipGrid.GridSize == MyCubeSize.Large ? 5 : 1;

            var basisMatrix = shipGrid.ReferenceMatrix;
            float basisGridSize = shipGrid.BasisGridSizeMeters;

            foreach (var grid in grids)
            {
                if (grid == null || grid.MarkedForClose)
                    continue;

                var blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks);
                foreach (var block in blocks)
                {
                    var worldPosition = GridPositionToWorld(grid, block.Position);
                    var basisPosition = WorldToBasisGrid(worldPosition, basisMatrix, basisGridSize);
                    var projected = Project(basisPosition, view);
                    var depth = ProjectDepth(basisPosition, view);

                    shipGrid.Blocks.Add(new ProjectedBlock
                    {
                        GridEntityId = grid.EntityId,
                        BasisPosition = basisPosition,
                        Projected = projected,
                        Depth = depth,
                        HasFatBlock = block.FatBlock != null,
                        FatBlock = block.FatBlock
                    });
                }
            }

            shipGrid.BlockCount = shipGrid.Blocks.Count;
            if (shipGrid.Blocks.Count == 0)
            {
                shipGrid.Min = Vector3I.Zero;
                shipGrid.Max = Vector3I.Zero;
                shipGrid.Size3D = Vector3I.Zero;
                shipGrid.Min2D = Vector2I.Zero;
                shipGrid.Max2D = Vector2I.Zero;
                shipGrid.Size2D = Vector2I.Zero;
                shipGrid.DepthMin = 0;
                shipGrid.DepthMax = -1;
                shipGrid.DepthSize = 0;
                return shipGrid;
            }

            var min3 = new Vector3I(int.MaxValue, int.MaxValue, int.MaxValue);
            var max3 = new Vector3I(int.MinValue, int.MinValue, int.MinValue);
            var min2 = new Vector2I(int.MaxValue, int.MaxValue);
            var max2 = new Vector2I(int.MinValue, int.MinValue);
            int depthMin = int.MaxValue;
            int depthMax = int.MinValue;

            foreach (var block in shipGrid.Blocks)
            {
                var position = block.BasisPosition;
                if (position.X < min3.X) min3.X = position.X;
                if (position.Y < min3.Y) min3.Y = position.Y;
                if (position.Z < min3.Z) min3.Z = position.Z;
                if (position.X > max3.X) max3.X = position.X;
                if (position.Y > max3.Y) max3.Y = position.Y;
                if (position.Z > max3.Z) max3.Z = position.Z;

                var projected = block.Projected;
                if (projected.X < min2.X) min2.X = projected.X;
                if (projected.Y < min2.Y) min2.Y = projected.Y;
                if (projected.X > max2.X) max2.X = projected.X;
                if (projected.Y > max2.Y) max2.Y = projected.Y;

                int depth = block.Depth;
                if (depth < depthMin) depthMin = depth;
                if (depth > depthMax) depthMax = depth;
            }

            shipGrid.Min = min3;
            shipGrid.Max = max3;
            shipGrid.Size3D = max3 - min3 + Vector3I.One;
            shipGrid.Min2D = min2;
            shipGrid.Max2D = max2;
            shipGrid.Size2D = max2 - min2 + Vector2I.One;
            shipGrid.DepthMin = depthMin;
            shipGrid.DepthMax = depthMax;
            shipGrid.DepthSize = depthMax - depthMin + 1;

            return shipGrid;
        }

        public static ShipGrid BuildFromConstruct(List<IMyCubeGrid> grids, IMyCubeGrid basisGrid, ScanView view)
        {
            return BuildFromConstruct(grids, basisGrid, null, view);
        }

        static string GetReferenceName(IMyCubeBlock referenceBlock)
        {
            var terminal = referenceBlock as Sandbox.ModAPI.IMyTerminalBlock;
            if (terminal != null && !string.IsNullOrWhiteSpace(terminal.CustomName))
                return terminal.CustomName;

            return referenceBlock != null ? "Remote Control" : "Grid";
        }

        static Vector3D GridPositionToWorld(IMyCubeGrid grid, Vector3I position)
        {
            var local = new Vector3D(position.X * grid.GridSize, position.Y * grid.GridSize, position.Z * grid.GridSize);
            return Vector3D.Transform(local, grid.WorldMatrix);
        }

        static Vector3I WorldToBasisGrid(Vector3D worldPosition, MatrixD basisMatrix, float basisGridSize)
        {
            if (basisGridSize <= 0f)
                basisGridSize = 2.5f;

            var offset = worldPosition - basisMatrix.Translation;
            var x = Vector3D.Dot(offset, basisMatrix.Right) / basisGridSize;
            var y = Vector3D.Dot(offset, basisMatrix.Up) / basisGridSize;
            var z = Vector3D.Dot(offset, basisMatrix.Forward) / basisGridSize;

            return new Vector3I(
                (int)Math.Round(x),
                (int)Math.Round(y),
                (int)Math.Round(z)
            );
        }

        public static Vector2I Project(Vector3I position, ScanView view)
        {
            switch (view)
            {
                case ScanView.Front:
                    return new Vector2I(position.X, position.Y);
                case ScanView.Side:
                    return new Vector2I(position.Z, position.Y);
                default:
                    return new Vector2I(position.X, position.Z);
            }
        }

        public static int ProjectDepth(Vector3I position, ScanView view)
        {
            switch (view)
            {
                case ScanView.Front:
                    return position.Z;
                case ScanView.Side:
                    return position.X;
                default:
                    return position.Y;
            }
        }

        public Vector3D BasisToWorld(Vector3D basisPosition)
        {
            return ReferenceMatrix.Translation +
                ReferenceMatrix.Right * (basisPosition.X * BasisGridSizeMeters) +
                ReferenceMatrix.Up * (basisPosition.Y * BasisGridSizeMeters) +
                ReferenceMatrix.Forward * (basisPosition.Z * BasisGridSizeMeters);
        }

        public Vector3D Unproject(float projectedX, float projectedY, float depth, ScanView view)
        {
            switch (view)
            {
                case ScanView.Front:
                    return new Vector3D(projectedX, projectedY, depth);
                case ScanView.Side:
                    return new Vector3D(depth, projectedY, projectedX);
                default:
                    return new Vector3D(projectedX, depth, projectedY);
            }
        }
    }

    public struct ProjectedBlock
    {
        public long GridEntityId;
        public Vector3I BasisPosition;
        public Vector2I Projected;
        public int Depth;
        public bool HasFatBlock;
        public IMyCubeBlock FatBlock;
    }

    public struct ProjectionTransform
    {
        public bool IsValid;
        public Vector2 Origin;
        public float CellSize;
        public float GridWidth;
        public float GridHeight;
        public int SourceWidth;
        public int SourceHeight;
        public int RotationSteps;

        public static ProjectionTransform Create(ShipGrid shipGrid, ScreenZone viewport, int rotationSteps)
        {
            var transform = new ProjectionTransform();
            transform.IsValid = false;

            if (shipGrid == null || shipGrid.IsEmpty || viewport.Width <= 0 || viewport.Height <= 0)
                return transform;

            const float margin = 8f;
            rotationSteps = NormalizeRotation(rotationSteps);
            int sourceWidth = Math.Max(1, shipGrid.Size2D.X);
            int sourceHeight = Math.Max(1, shipGrid.Size2D.Y);
            int displayWidth = rotationSteps % 2 == 0 ? sourceWidth : sourceHeight;
            int displayHeight = rotationSteps % 2 == 0 ? sourceHeight : sourceWidth;
            float availableWidth = Math.Max(1f, viewport.Width - margin * 2f);
            float availableHeight = Math.Max(1f, viewport.Height - margin * 2f);
            float cellByWidth = availableWidth / displayWidth;
            float cellByHeight = availableHeight / displayHeight;
            float cell = Math.Min(cellByWidth, cellByHeight);

            transform.CellSize = cell;
            transform.GridWidth = cell * displayWidth;
            transform.GridHeight = cell * displayHeight;
            transform.SourceWidth = sourceWidth;
            transform.SourceHeight = sourceHeight;
            transform.RotationSteps = rotationSteps;
            transform.Origin = new Vector2(
                viewport.X + (viewport.Width - transform.GridWidth) * 0.5f,
                viewport.Y + (viewport.Height - transform.GridHeight) * 0.5f
            );
            transform.IsValid = true;
            return transform;
        }

        public Vector2 ProjectCellCenter(ShipGrid shipGrid, Vector2I projected)
        {
            float localX = projected.X - shipGrid.Min2D.X + 0.5f;
            float localY = shipGrid.Max2D.Y - projected.Y + 0.5f;
            return ProjectLocalPoint(localX, localY);
        }

        public float BoundaryX(int indexFromMin)
        {
            return Origin.X + indexFromMin * CellSize;
        }

        public float BoundaryY(int indexFromMin)
        {
            return Origin.Y + GridHeight - indexFromMin * CellSize;
        }

        public Vector2 ProjectLocalPoint(float localX, float localY)
        {
            float x;
            float y;
            switch (RotationSteps)
            {
                case 1:
                    x = SourceHeight - localY;
                    y = localX;
                    break;
                case 2:
                    x = SourceWidth - localX;
                    y = SourceHeight - localY;
                    break;
                case 3:
                    x = localY;
                    y = SourceWidth - localX;
                    break;
                default:
                    x = localX;
                    y = localY;
                    break;
            }

            return new Vector2(Origin.X + x * CellSize, Origin.Y + y * CellSize);
        }

        public bool TryScreenToLocal(Vector2 screenPoint, out float localX, out float localY)
        {
            localX = 0f;
            localY = 0f;
            if (!IsValid || CellSize <= 0f)
                return false;

            float x = (screenPoint.X - Origin.X) / CellSize;
            float y = (screenPoint.Y - Origin.Y) / CellSize;

            switch (RotationSteps)
            {
                case 1:
                    localX = y;
                    localY = SourceHeight - x;
                    break;
                case 2:
                    localX = SourceWidth - x;
                    localY = SourceHeight - y;
                    break;
                case 3:
                    localX = SourceWidth - y;
                    localY = x;
                    break;
                default:
                    localX = x;
                    localY = y;
                    break;
            }

            return true;
        }

        public Vector2 ProjectScanSampleCenter(int x, int y, int resolution)
        {
            float localX = (x + 0.5f) / Math.Max(1f, resolution) * SourceWidth;
            float localY = SourceHeight - (y + 0.5f) / Math.Max(1f, resolution) * SourceHeight;
            return ProjectLocalPoint(localX, localY);
        }

        public Vector2 GetRotatedSize(float widthPx, float heightPx)
        {
            return RotationSteps % 2 == 0 ? new Vector2(widthPx, heightPx) : new Vector2(heightPx, widthPx);
        }

        static int NormalizeRotation(int value)
        {
            int normalized = value % 4;
            if (normalized < 0)
                normalized += 4;
            return normalized;
        }
    }
}
