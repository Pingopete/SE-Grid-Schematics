using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace GridSchematics
{
    public enum ConveyorNodeKind
    {
        Block,
        LineEndpoint
    }

    public class ConveyorTopology
    {
        public List<ConveyorNode> Nodes { get; private set; }
        public List<ConveyorPort> Ports { get; private set; }
        public List<ConveyorEdge> Edges { get; private set; }
        public List<ConveyorLineRun> Lines { get; private set; }

        public ConveyorTopology()
        {
            Nodes = new List<ConveyorNode>();
            Ports = new List<ConveyorPort>();
            Edges = new List<ConveyorEdge>();
            Lines = new List<ConveyorLineRun>();
        }

        public static ConveyorTopology Discover(List<IMyCubeGrid> constructGrids, IMyCubeGrid basisGrid, IMyCubeBlock referenceBlock)
        {
            var topology = new ConveyorTopology();
            if (constructGrids == null || constructGrids.Count == 0)
                return topology;

            if (basisGrid == null)
                basisGrid = constructGrids[0];

            var basisMatrix = referenceBlock != null ? referenceBlock.WorldMatrix : basisGrid.WorldMatrix;
            float basisGridSize = basisGrid.GridSize > 0f ? basisGrid.GridSize : 2.5f;

            var nodesById = new Dictionary<long, ConveyorNode>();
            var blocks = new List<IMySlimBlock>();
            for (int gridIndex = 0; gridIndex < constructGrids.Count; gridIndex++)
            {
                var grid = constructGrids[gridIndex];
                if (grid == null || grid.MarkedForClose)
                    continue;

                blocks.Clear();
                grid.GetBlocks(blocks, block => block != null && block.FatBlock != null);
                for (int i = 0; i < blocks.Count; i++)
                {
                    var slim = blocks[i];
                    var fat = slim.FatBlock;
                    if (fat == null || (!IsPublicConveyorParticipant(fat) && !IsLargeVisualAnchorBlock(slim, fat)))
                        continue;

                    AddBlockNode(topology, nodesById, grid, slim, fat, basisMatrix, basisGridSize);
                }

                DiscoverObjectBuilderLines(topology, grid, basisMatrix, basisGridSize);
            }

            return topology;
        }

        public static ConveyorTopology Discover(IMyCubeGrid grid)
        {
            var grids = new List<IMyCubeGrid>();
            if (grid != null)
                grids.Add(grid);
            return Discover(grids, grid, null);
        }

        static void AddBlockNode(
            ConveyorTopology topology,
            Dictionary<long, ConveyorNode> nodesById,
            IMyCubeGrid grid,
            IMySlimBlock slim,
            IMyCubeBlock fat,
            MatrixD basisMatrix,
            float basisGridSize)
        {
            if (nodesById.ContainsKey(fat.EntityId))
                return;

            var basisPosition = WorldToBasisGrid(GridPositionToWorld(grid, slim.Position), basisMatrix, basisGridSize);
            var basisCenter = WorldToBasisPoint(GridPositionToWorld(grid, GetSlimCenter(slim)), basisMatrix, basisGridSize);
            var node = new ConveyorNode
            {
                BlockEntityId = fat.EntityId,
                GridEntityId = grid.EntityId,
                BlockType = fat.BlockDefinition.TypeIdString + "/" + fat.BlockDefinition.SubtypeName,
                DisplayName = GetDisplayName(fat),
                Position = fat.GetPosition(),
                GridPosition = slim.Position,
                GridMin = slim.Min,
                GridMax = slim.Max,
                BasisPosition = basisPosition,
                BasisCenter = basisCenter,
                Orientation = fat.WorldMatrix.Forward,
                Kind = ConveyorNodeKind.Block,
                IsFunctional = IsFunctional(fat),
                IsWorking = IsWorking(fat)
            };

            nodesById[node.BlockEntityId] = node;
            topology.Nodes.Add(node);
        }

        static void DiscoverObjectBuilderLines(ConveyorTopology topology, IMyCubeGrid grid, MatrixD basisMatrix, float basisGridSize)
        {
            MyObjectBuilder_CubeGrid builder = null;
            try
            {
                builder = grid.GetObjectBuilder(false) as MyObjectBuilder_CubeGrid;
            }
            catch
            {
            }

            if (builder == null || builder.ConveyorLines == null)
                return;

            for (int i = 0; i < builder.ConveyorLines.Count; i++)
            {
                var line = builder.ConveyorLines[i];
                if (line == null)
                    continue;

                var run = BuildLineRun(grid, basisMatrix, basisGridSize, i, line);
                topology.Lines.Add(run);

                AddLineEndpoint(topology, grid, run, line.StartPosition, line.StartDirection, true);
                AddLineEndpoint(topology, grid, run, line.EndPosition, line.EndDirection, false);
            }
        }

        static ConveyorLineRun BuildLineRun(IMyCubeGrid grid, MatrixD basisMatrix, float basisGridSize, int index, MyObjectBuilder_ConveyorLine line)
        {
            var run = new ConveyorLineRun
            {
                LineKey = unchecked((int)(grid.EntityId ^ (long)(index + 1) * 397L)),
                GridEntityId = grid.EntityId,
                IsFunctional = true,
                IsWorking = line.ConveyorLineConductivity != MyObjectBuilder_ConveyorLine.LineConductivity.NONE,
                IsDisconnected = line.ConveyorLineConductivity == MyObjectBuilder_ConveyorLine.LineConductivity.NONE,
                LineType = line.ConveyorLineType.ToString(),
                Conductivity = line.ConveyorLineConductivity.ToString(),
                GridPositions = new List<Vector3I>(),
                BasisPositions = new List<Vector3I>()
            };

            var points = BuildGridLinePoints(line);
            for (int i = 0; i < points.Count; i++)
            {
                run.GridPositions.Add(points[i]);
                run.BasisPositions.Add(WorldToBasisGrid(GridPositionToWorld(grid, points[i]), basisMatrix, basisGridSize));
            }

            return run;
        }

        static List<Vector3I> BuildGridLinePoints(MyObjectBuilder_ConveyorLine line)
        {
            var points = new List<Vector3I>();
            var current = ToVector3I(line.StartPosition);
            var end = ToVector3I(line.EndPosition);
            points.Add(current);

            if (line.Sections != null && line.Sections.Count > 0)
            {
                for (int i = 0; i < line.Sections.Count; i++)
                {
                    var section = line.Sections[i];
                    var direction = Base6Directions.GetIntVector(section.Direction);
                    int length = Math.Max(0, section.Length);
                    for (int step = 0; step < length; step++)
                    {
                        current += direction;
                        AddDistinct(points, current);
                    }
                }
            }

            if (points.Count == 1 || points[points.Count - 1] != end)
                AddStraightFallback(points, current, end);

            return points;
        }

        static void AddStraightFallback(List<Vector3I> points, Vector3I start, Vector3I end)
        {
            var delta = end - start;
            int steps = Math.Max(Math.Abs(delta.X), Math.Max(Math.Abs(delta.Y), Math.Abs(delta.Z)));
            if (steps <= 0)
                return;

            for (int i = 1; i <= steps; i++)
            {
                var p = new Vector3I(
                    start.X + (int)Math.Round(delta.X * (i / (float)steps)),
                    start.Y + (int)Math.Round(delta.Y * (i / (float)steps)),
                    start.Z + (int)Math.Round(delta.Z * (i / (float)steps)));
                AddDistinct(points, p);
            }
        }

        static void AddDistinct(List<Vector3I> points, Vector3I point)
        {
            if (points.Count == 0 || points[points.Count - 1] != point)
                points.Add(point);
        }

        static void AddLineEndpoint(ConveyorTopology topology, IMyCubeGrid grid, ConveyorLineRun run, Vector3I gridPosition, Base6Directions.Direction direction, bool isStart)
        {
            var basisPosition = run.BasisPositions.Count > 0
                ? isStart ? run.BasisPositions[0] : run.BasisPositions[run.BasisPositions.Count - 1]
                : gridPosition;

            topology.Ports.Add(new ConveyorPort
            {
                Id = topology.Ports.Count,
                NodeId = 0,
                PortIndex = isStart ? 0 : 1,
                GridEntityId = grid.EntityId,
                LocalGridPosition = gridPosition,
                NeighborGridPosition = gridPosition + Base6Directions.GetIntVector(direction),
                BasisPosition = basisPosition,
                Direction = direction.ToString(),
                DirectionVector = Base6Directions.GetIntVector(direction),
                HasLine = true,
                IsConnected = run.IsWorking,
                LineKey = run.LineKey,
                LineType = run.LineType,
                Conductivity = run.Conductivity
            });
        }

        static bool IsPublicConveyorParticipant(IMyCubeBlock block)
        {
            if (block as IMyConveyor != null)
                return true;
            if (block as IMyConveyorTube != null)
                return true;
            if (block as IMyConveyorSorter != null)
                return true;
            if (block as IMyShipConnector != null)
                return true;

            if (HasInventory(block))
                return true;

            return false;
        }

        static bool IsLargeVisualAnchorBlock(IMySlimBlock slim, IMyCubeBlock block)
        {
            if (slim == null || block == null)
                return false;

            if (block as IMyTerminalBlock == null && block as IMyFunctionalBlock == null)
                return false;

            int sx = Math.Abs(slim.Max.X - slim.Min.X) + 1;
            int sy = Math.Abs(slim.Max.Y - slim.Min.Y) + 1;
            int sz = Math.Abs(slim.Max.Z - slim.Min.Z) + 1;
            return sx * sy * sz > 1;
        }

        static bool HasInventory(IMyCubeBlock block)
        {
            try
            {
                return block != null && block.InventoryCount > 0;
            }
            catch
            {
                return false;
            }
        }

        static Vector3I ToVector3I(Vector3I value)
        {
            return value;
        }

        static string GetDisplayName(IMyCubeBlock block)
        {
            var terminal = block as IMyTerminalBlock;
            if (terminal != null && !string.IsNullOrWhiteSpace(terminal.CustomName))
                return terminal.CustomName;

            return block.BlockDefinition.SubtypeName;
        }

        static bool IsFunctional(IMyCubeBlock block)
        {
            var functional = block as IMyFunctionalBlock;
            return functional == null || functional.IsFunctional;
        }

        static bool IsWorking(IMyCubeBlock block)
        {
            var functional = block as IMyFunctionalBlock;
            return functional == null || functional.IsWorking;
        }

        static Vector3D GridPositionToWorld(IMyCubeGrid grid, Vector3I position)
        {
            var local = new Vector3D(position.X * grid.GridSize, position.Y * grid.GridSize, position.Z * grid.GridSize);
            return Vector3D.Transform(local, grid.WorldMatrix);
        }

        static Vector3D GridPositionToWorld(IMyCubeGrid grid, Vector3D position)
        {
            var local = new Vector3D(position.X * grid.GridSize, position.Y * grid.GridSize, position.Z * grid.GridSize);
            return Vector3D.Transform(local, grid.WorldMatrix);
        }

        static Vector3D GetSlimCenter(IMySlimBlock slim)
        {
            var min = slim.Min;
            var max = slim.Max;
            return new Vector3D(
                (min.X + max.X) * 0.5,
                (min.Y + max.Y) * 0.5,
                (min.Z + max.Z) * 0.5);
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

        static Vector3D WorldToBasisPoint(Vector3D worldPosition, MatrixD basisMatrix, float basisGridSize)
        {
            if (basisGridSize <= 0f)
                basisGridSize = 2.5f;

            var offset = worldPosition - basisMatrix.Translation;
            return new Vector3D(
                Vector3D.Dot(offset, basisMatrix.Right) / basisGridSize,
                Vector3D.Dot(offset, basisMatrix.Up) / basisGridSize,
                Vector3D.Dot(offset, basisMatrix.Forward) / basisGridSize);
        }
    }

    public class ConveyorNode
    {
        public long BlockEntityId;
        public long GridEntityId;
        public string BlockType;
        public string DisplayName;
        public Vector3D Position;
        public Vector3I GridPosition;
        public Vector3I GridMin;
        public Vector3I GridMax;
        public Vector3I BasisPosition;
        public Vector3D BasisCenter;
        public Vector3D Orientation;
        public ConveyorNodeKind Kind;
        public bool IsFunctional;
        public bool IsWorking;
    }

    public class ConveyorPort
    {
        public int Id;
        public long NodeId;
        public int PortIndex;
        public long GridEntityId;
        public Vector3I LocalGridPosition;
        public Vector3I NeighborGridPosition;
        public Vector3I BasisPosition;
        public string Direction;
        public Vector3I DirectionVector;
        public bool HasLine;
        public bool IsConnected;
        public int LineKey;
        public string LineType;
        public string Conductivity;
    }

    public class ConveyorEdge
    {
        public long FromId;
        public long ToId;
        public int LineKey;
        public bool IsFunctional;
        public bool IsWorking;
        public bool IsDisconnected;
        public string LineType;
        public string Conductivity;
    }

    public class ConveyorLineRun
    {
        public int LineKey;
        public long GridEntityId;
        public bool IsFunctional;
        public bool IsWorking;
        public bool IsDisconnected;
        public string LineType;
        public string Conductivity;
        public List<Vector3I> GridPositions;
        public List<Vector3I> BasisPositions;
    }
}
