using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace GridSchematics
{
    public enum ScanView
    {
        Top,
        Front,
        Side
    }

    public class ScanCache
    {
        public long ConstructId { get; }
        public Dictionary<ScanView, ViewScanData> ViewData { get; }
        public Dictionary<ScanView, RawRaycastScanData> RaycastData { get; }
        public Dictionary<ScanView, ShipGrid> ProjectedGrids { get; }
        public List<IMyCubeGrid> ConstructGrids { get; }
        public List<IMyCubeGrid> HullScanTargetGrids { get; }
        public HashSet<long> ValidHullScanGridIds { get; }
        HashSet<long> ConnectorHullScanGridIds { get; set; }
        ConnectorScanDiscoveryDiagnostics LastConnectorDiscoveryDiagnostics { get; set; }
        public RaycastScanDiagnostics LastRaycastDiagnostics { get; private set; }
        public ConveyorTopology ConveyorNetwork { get; set; }
        public IMyCubeGrid BasisGrid { get; private set; }
        public IMyRemoteControl ReferenceRemoteControl { get; private set; }
        public bool StartupScanCompleted { get; private set; }
        public DateTime LastUpdatedUtc { get; private set; }

        public ScanCache(long constructId)
        {
            ConstructId = constructId;
            ViewData = new Dictionary<ScanView, ViewScanData>();
            RaycastData = new Dictionary<ScanView, RawRaycastScanData>();
            ProjectedGrids = new Dictionary<ScanView, ShipGrid>();
            ConstructGrids = new List<IMyCubeGrid>();
            HullScanTargetGrids = new List<IMyCubeGrid>();
            ValidHullScanGridIds = new HashSet<long>();
            ConnectorHullScanGridIds = new HashSet<long>();
            LastConnectorDiscoveryDiagnostics = new ConnectorScanDiscoveryDiagnostics();
            LastRaycastDiagnostics = new RaycastScanDiagnostics();
            ConveyorNetwork = null;
            StartupScanCompleted = false;
            LastUpdatedUtc = DateTime.UtcNow;
        }

        public ViewScanData GetViewData(ScanView view)
        {
            ViewScanData data;
            if (!ViewData.TryGetValue(view, out data))
            {
                data = new ViewScanData(view);
                ViewData[view] = data;
            }
            return data;
        }

        public RawRaycastScanData GetRaycastData(ScanView view)
        {
            RawRaycastScanData data;
            RaycastData.TryGetValue(view, out data);
            return data;
        }

        public bool HasReadyRaycastData(ScanView view)
        {
            var data = GetRaycastData(view);
            return data != null && data.IsReady && data.Samples != null && data.Resolution > 0;
        }

        public ConveyorTopology GetOrCreateConveyorNetwork(IMyCubeGrid grid, bool force)
        {
            if (force || ConveyorNetwork == null)
            {
                RefreshConstructGrids(grid);
                ConveyorNetwork = ConveyorTopology.Discover(ConstructGrids, BasisGrid ?? grid, ReferenceRemoteControl);
            }
            return ConveyorNetwork;
        }

        public ConveyorTopology GetOrCreateConveyorNetwork(IMyCubeGrid grid)
        {
            return GetOrCreateConveyorNetwork(grid, false);
        }

        public ShipGrid ShipGrid { get; private set; }

        public ShipGrid UpdateConstructProjection(IMyCubeGrid rootGrid, ScanView view, bool force)
        {
            if (rootGrid == null)
                return ShipGrid;

            ShipGrid projectedGrid;
            if (force || !ProjectedGrids.TryGetValue(view, out projectedGrid) || projectedGrid == null)
            {
                RefreshConstructGrids(rootGrid);
                projectedGrid = ShipGrid.BuildFromConstruct(ConstructGrids, BasisGrid ?? rootGrid, ReferenceRemoteControl, view);
                ProjectedGrids[view] = projectedGrid;
                MarkUpdated();
            }
            ShipGrid = projectedGrid;
            return ShipGrid;
        }

        public void UpdateCachedRaycastScans(IMyCubeGrid rootGrid, int resolution)
        {
            UpdateCachedRaycastScan(rootGrid, ScanView.Top, resolution);
            UpdateCachedRaycastScan(rootGrid, ScanView.Side, resolution);
            UpdateCachedRaycastScan(rootGrid, ScanView.Front, resolution);
        }

        public RawRaycastScanData UpdateCachedRaycastScan(IMyCubeGrid rootGrid, ScanView view, int resolution)
        {
            if (rootGrid == null)
                return null;

            if (resolution <= 0)
                resolution = 256;

            RefreshConstructGrids(rootGrid);
            var projectedGrid = ShipGrid.BuildFromConstruct(ConstructGrids, BasisGrid ?? rootGrid, ReferenceRemoteControl, view);
            ProjectedGrids[view] = projectedGrid;

            var data = BuildRawPhysicsRaycastScan(rootGrid, projectedGrid, view, resolution);
            RaycastData[view] = data;
            MarkUpdated();
            return data;
        }

        public IncrementalRaycastScanJob BeginIncrementalRaycastScans(IMyCubeGrid rootGrid, int resolution)
        {
            if (resolution <= 0)
                resolution = 256;

            return new IncrementalRaycastScanJob(this, rootGrid, resolution);
        }

        void RefreshConstructGrids(IMyCubeGrid rootGrid)
        {
            ConstructGrids.Clear();
            HullScanTargetGrids.Clear();
            ValidHullScanGridIds.Clear();
            ConnectorHullScanGridIds.Clear();
            BasisGrid = rootGrid;
            ReferenceRemoteControl = null;
            if (LastConnectorDiscoveryDiagnostics == null)
                LastConnectorDiscoveryDiagnostics = new ConnectorScanDiscoveryDiagnostics();

            var entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities, entity => entity is IMyCubeGrid);

            var constructSeedGrids = BuildSameConstructSeedGrids(rootGrid, entities);
            var connectorSearchSeedGrids = constructSeedGrids;

            if (constructSeedGrids.Count == 0)
            {
                constructSeedGrids.Add(rootGrid);
            }

            var connectorAttachedGrids = BuildConnectorAttachedGridSet(rootGrid, connectorSearchSeedGrids, entities);
            for (int i = 0; i < constructSeedGrids.Count; i++)
            {
                var grid = constructSeedGrids[i];
                if (grid != null && !grid.MarkedForClose)
                    ConstructGrids.Add(grid);
            }

            if (ConstructGrids.Count == 0)
                ConstructGrids.Add(rootGrid);

            BasisGrid = SelectBasisGrid(ConstructGrids, rootGrid);

            for (int i = 0; i < ConstructGrids.Count; i++)
            {
                if (ConstructGrids[i] != null)
                {
                    HullScanTargetGrids.Add(ConstructGrids[i]);
                    ValidHullScanGridIds.Add(ConstructGrids[i].EntityId);
                }
            }

            foreach (var entity in entities)
            {
                var grid = entity as IMyCubeGrid;
                if (grid == null || grid.MarkedForClose || !connectorAttachedGrids.Contains(grid.EntityId))
                    continue;
                if (ValidHullScanGridIds.Contains(grid.EntityId))
                    continue;
                HullScanTargetGrids.Add(grid);
                ValidHullScanGridIds.Add(grid.EntityId);
                ConnectorHullScanGridIds.Add(grid.EntityId);
            }

            ReferenceRemoteControl = FindFirstRemoteControl(BasisGrid);
        }

        List<IMyCubeGrid> BuildMainConstructSeedGrids(IMyCubeGrid rootGrid, HashSet<IMyEntity> entities)
        {
            var constructSeedGrids = new List<IMyCubeGrid>();
            if (rootGrid == null)
                return constructSeedGrids;

            try
            {
                if (MyAPIGateway.GridGroups != null)
                    MyAPIGateway.GridGroups.GetGroup(rootGrid, GridLinkTypeEnum.Mechanical, constructSeedGrids);
            }
            catch
            {
                constructSeedGrids.Clear();
            }

            if (constructSeedGrids.Count > 0)
                return FilterLiveUniqueGrids(constructSeedGrids, rootGrid);

            constructSeedGrids.Add(rootGrid);
            return FilterLiveUniqueGrids(constructSeedGrids, rootGrid);
        }

        List<IMyCubeGrid> BuildSameConstructSeedGrids(IMyCubeGrid rootGrid, HashSet<IMyEntity> entities)
        {
            var seeds = new List<IMyCubeGrid>();
            if (rootGrid == null)
                return seeds;

            if (entities == null)
            {
                seeds.Add(rootGrid);
                return seeds;
            }

            foreach (var entity in entities)
            {
                var grid = entity as IMyCubeGrid;
                if (grid == null || grid.MarkedForClose)
                    continue;
                if (rootGrid != null && !IsGridStatic(rootGrid) && grid.EntityId != rootGrid.EntityId && IsGridStatic(grid))
                    continue;

                bool sameConstruct = false;
                try
                {
                    sameConstruct = grid.IsSameConstructAs(rootGrid);
                }
                catch
                {
                    sameConstruct = ReferenceEquals(grid, rootGrid);
                }

                if (sameConstruct)
                    seeds.Add(grid);
            }

            return FilterLiveUniqueGrids(seeds, rootGrid);
        }

        static List<IMyCubeGrid> FilterLiveUniqueGrids(List<IMyCubeGrid> grids, IMyCubeGrid fallback)
        {
            var result = new List<IMyCubeGrid>();
            var seen = new HashSet<long>();
            if (grids != null)
            {
                for (int i = 0; i < grids.Count; i++)
                {
                    var grid = grids[i];
                    if (grid == null || grid.MarkedForClose || seen.Contains(grid.EntityId))
                        continue;

                    seen.Add(grid.EntityId);
                    result.Add(grid);
                }
            }

            if (result.Count == 0 && fallback != null && !fallback.MarkedForClose)
                result.Add(fallback);

            return result;
        }

        static IMyCubeGrid SelectBasisGrid(List<IMyCubeGrid> constructGrids, IMyCubeGrid fallback)
        {
            if (constructGrids == null || constructGrids.Count == 0)
                return fallback;

            IMyCubeGrid largeGrid = SelectLargestGridBySize(constructGrids, fallback, MyCubeSize.Large);
            if (largeGrid != null)
                return largeGrid;

            IMyCubeGrid smallGrid = SelectLargestGridBySize(constructGrids, fallback, MyCubeSize.Small);
            if (smallGrid != null)
                return smallGrid;

            return fallback;
        }

        static IMyCubeGrid SelectLargestGridBySize(List<IMyCubeGrid> grids, IMyCubeGrid fallback, MyCubeSize sizeClass)
        {
            IMyCubeGrid best = fallback;
            double bestVolume = -1.0;
            for (int i = 0; i < grids.Count; i++)
            {
                var grid = grids[i];
                if (grid == null || grid.MarkedForClose)
                    continue;
                if (grid.GridSizeEnum != sizeClass)
                    continue;

                var size = grid.WorldAABB.Size;
                double volume = size.X * size.Y * size.Z;
                if (volume > bestVolume)
                {
                    bestVolume = volume;
                    best = grid;
                }
            }

            return bestVolume >= 0.0 ? best : null;
        }

        HashSet<long> BuildConnectorAttachedGridSet(IMyCubeGrid rootGrid, List<IMyCubeGrid> constructSeedGrids, HashSet<IMyEntity> entities)
        {
            var attached = new HashSet<long>();
            var diagnostics = LastConnectorDiscoveryDiagnostics;
            if (diagnostics != null)
                diagnostics.Reset();

            if (rootGrid == null || entities == null)
                return attached;

            var gridsById = new Dictionary<long, IMyCubeGrid>();
            foreach (var entity in entities)
            {
                var grid = entity as IMyCubeGrid;
                if (grid != null && !grid.MarkedForClose)
                    gridsById[grid.EntityId] = grid;
            }

            var queue = new Queue<IMyCubeGrid>();
            var visited = new HashSet<long>();
            if (constructSeedGrids != null && constructSeedGrids.Count > 0)
            {
                for (int i = 0; i < constructSeedGrids.Count; i++)
                {
                    var seedGrid = constructSeedGrids[i];
                    if (seedGrid == null || seedGrid.MarkedForClose || visited.Contains(seedGrid.EntityId))
                        continue;

                    queue.Enqueue(seedGrid);
                    visited.Add(seedGrid.EntityId);
                }
            }

            if (queue.Count == 0)
            {
                queue.Enqueue(rootGrid);
                visited.Add(rootGrid.EntityId);
            }

            while (queue.Count > 0)
            {
                var grid = queue.Dequeue();
                if (diagnostics != null)
                    diagnostics.GridsVisited++;

                var blocks = new List<IMySlimBlock>();
                try
                {
                    grid.GetBlocks(blocks, block => block != null && block.FatBlock is IMyShipConnector);
                }
                catch
                {
                    continue;
                }

                for (int i = 0; i < blocks.Count; i++)
                {
                    var connector = blocks[i].FatBlock as IMyShipConnector;
                    if (connector == null)
                        continue;

                    if (diagnostics != null)
                        diagnostics.ConnectorsFound++;

                    IMyShipConnector other = null;
                    try
                    {
                        other = connector.OtherConnector;
                    }
                    catch
                    {
                    }

                    if (other != null && diagnostics != null)
                        diagnostics.LinkedConnectorsFound++;

                    var otherGrid = other != null ? other.CubeGrid : null;
                    if (otherGrid == null || otherGrid.MarkedForClose || otherGrid.EntityId == rootGrid.EntityId || IsGridStatic(otherGrid))
                    {
                        if (otherGrid == null && diagnostics != null)
                            diagnostics.NullOtherGridCount++;
                        continue;
                    }

                    if (diagnostics != null && !attached.Contains(otherGrid.EntityId))
                        diagnostics.AddAttachedGrid(GetGridDebugName(otherGrid));

                    attached.Add(otherGrid.EntityId);
                    if (!visited.Contains(otherGrid.EntityId))
                    {
                        visited.Add(otherGrid.EntityId);
                        queue.Enqueue(otherGrid);
                    }
                }
            }

            return attached;
        }

        static bool IsGridStatic(IMyCubeGrid grid)
        {
            if (grid == null)
                return false;

            try
            {
                return grid.IsStatic;
            }
            catch
            {
                return false;
            }
        }

        IMyRemoteControl FindReferenceRemoteControl(List<IMyCubeGrid> constructGrids, IMyCubeGrid rootGrid, IMyCubeGrid basisGrid)
        {
            var remote = FindFirstRemoteControl(rootGrid);
            if (remote != null)
                return remote;

            if (basisGrid != null && (rootGrid == null || basisGrid.EntityId != rootGrid.EntityId))
            {
                remote = FindFirstRemoteControl(basisGrid);
                if (remote != null)
                    return remote;
            }

            if (constructGrids == null)
                return null;

            for (int i = 0; i < constructGrids.Count; i++)
            {
                var grid = constructGrids[i];
                if (grid == null || grid.MarkedForClose)
                    continue;

                if (rootGrid != null && grid.EntityId == rootGrid.EntityId)
                    continue;
                if (basisGrid != null && grid.EntityId == basisGrid.EntityId)
                    continue;

                remote = FindFirstRemoteControl(grid);
                if (remote != null)
                    return remote;
            }

            return null;
        }

        IMyRemoteControl FindFirstRemoteControl(IMyCubeGrid mainGrid)
        {
            if (mainGrid == null || mainGrid.MarkedForClose)
                return null;

            var blocks = new List<IMySlimBlock>();
            mainGrid.GetBlocks(blocks, block => block != null && block.FatBlock is IMyRemoteControl);
            for (int i = 0; i < blocks.Count; i++)
            {
                var remote = blocks[i].FatBlock as IMyRemoteControl;
                if (remote != null && !remote.MarkedForClose)
                    return remote;
            }

            return null;
        }

        public void UpdateViewScan(IMyCubeGrid grid, ScanView view, int resolution)
        {
            var scanData = GetViewData(view);
            if (resolution <= 0)
                resolution = 256;

            int cellCount = resolution * resolution;
            if (scanData.OccupancyMask == null || scanData.OccupancyMask.Length != cellCount)
            {
                scanData.OccupancyMask = new byte[cellCount];
                scanData.DepthBuffer = new float[cellCount];
            }

            scanData.Resolution = resolution;
            scanData.IsReady = false;

            var shipGrid = UpdateConstructProjection(grid, view, false);
            if (shipGrid == null || shipGrid.IsEmpty)
                return;

            if (shipGrid.Blocks.Count == 0)
                return;

            var buckets = new Dictionary<Vector2I, List<int>>();
            foreach (var block in shipGrid.Blocks)
            {
                List<int> list;
                if (!buckets.TryGetValue(block.Projected, out list))
                {
                    list = new List<int>();
                    buckets[block.Projected] = list;
                }
                list.Add(block.Depth);
            }

            foreach (var list in buckets.Values)
            {
                list.Sort((a, b) => b.CompareTo(a));
            }

            float widthRange = shipGrid.Size2D.X;
            float heightRange = shipGrid.Size2D.Y;
            if (widthRange <= 0 || heightRange <= 0)
                return;

            for (int iy = 0; iy < resolution; iy++)
            {
                float ty = iy / (float)resolution;
                int projectedY = shipGrid.Min2D.Y + (int)(ty * (shipGrid.Size2D.Y - 1));
                if (projectedY > shipGrid.Max2D.Y)
                    projectedY = shipGrid.Max2D.Y;

                for (int ix = 0; ix < resolution; ix++)
                {
                    float tx = ix / (float)resolution;
                    int projectedX = shipGrid.Min2D.X + (int)(tx * (shipGrid.Size2D.X - 1));
                    if (projectedX > shipGrid.Max2D.X)
                        projectedX = shipGrid.Max2D.X;

                    int index = iy * resolution + ix;
                    var projected = new Vector2I(projectedX, projectedY);
                    List<int> depths;
                    if (!buckets.TryGetValue(projected, out depths) || depths.Count == 0)
                    {
                        scanData.OccupancyMask[index] = 0;
                        scanData.DepthBuffer[index] = 0f;
                        continue;
                    }

                    int hitDepth = depths[0];
                    scanData.OccupancyMask[index] = 1;
                    scanData.DepthBuffer[index] = (hitDepth - shipGrid.DepthMin) / (float)shipGrid.DepthSize;
                }
            }

            scanData.IsReady = true;
        }

        public void MarkUpdated()
        {
            LastUpdatedUtc = DateTime.UtcNow;
        }

        public void MarkStartupScanCompleted()
        {
            StartupScanCompleted = true;
        }

        public string BuildScanSignature(IMyCubeGrid rootGrid)
        {
            RefreshConstructGrids(rootGrid);
            unchecked
            {
                ulong hash = 1469598103934665603UL;
                AccumulateHash(ref hash, ConstructId);
                AccumulateHash(ref hash, rootGrid != null ? rootGrid.EntityId : 0);
                AccumulateHash(ref hash, ConstructGrids.Count);
                AccumulateHash(ref hash, ValidHullScanGridIds.Count);
                AccumulateHash(ref hash, HullScanTargetGrids.Count);

                for (int i = 0; i < ConstructGrids.Count; i++)
                    AccumulateGridSignature(ref hash, ConstructGrids[i]);
                for (int i = 0; i < HullScanTargetGrids.Count; i++)
                    AccumulateGridSignature(ref hash, HullScanTargetGrids[i]);

                return hash.ToString("X");
            }
        }

        static void AccumulateGridSignature(ref ulong hash, IMyCubeGrid grid)
        {
            if (grid == null)
            {
                AccumulateHash(ref hash, 0);
                return;
            }

            AccumulateHash(ref hash, grid.EntityId);
            AccumulateHash(ref hash, IsGridStatic(grid) ? 1 : 0);
            AccumulateHash(ref hash, (int)grid.GridSizeEnum);

            var blocks = new List<IMySlimBlock>();
            try
            {
                grid.GetBlocks(blocks);
            }
            catch
            {
            }

            AccumulateHash(ref hash, blocks.Count);
            for (int i = 0; i < blocks.Count; i++)
            {
                var block = blocks[i];
                if (block == null)
                    continue;

                AccumulateHash(ref hash, block.Position.X);
                AccumulateHash(ref hash, block.Position.Y);
                AccumulateHash(ref hash, block.Position.Z);
                AccumulateHash(ref hash, block.Min.X);
                AccumulateHash(ref hash, block.Min.Y);
                AccumulateHash(ref hash, block.Min.Z);
                AccumulateHash(ref hash, block.Max.X);
                AccumulateHash(ref hash, block.Max.Y);
                AccumulateHash(ref hash, block.Max.Z);
            }
        }

        static void AccumulateHash(ref ulong hash, long value)
        {
            unchecked
            {
                ulong v = (ulong)value;
                for (int i = 0; i < 8; i++)
                {
                    hash ^= (byte)(v & 0xff);
                    hash *= 1099511628211UL;
                    v >>= 8;
                }
            }
        }

        RawRaycastScanData BuildRawPhysicsRaycastScan(IMyCubeGrid rootGrid, ShipGrid shipGrid, ScanView view, int resolution)
        {
            var data = new RawRaycastScanData(view, resolution);
            if (rootGrid == null || shipGrid == null || shipGrid.IsEmpty || shipGrid.Blocks.Count == 0 || MyAPIGateway.Physics == null)
                return data;

            var hullScanGrid = ShipGrid.BuildFromConstruct(HullScanTargetGrids, BasisGrid ?? rootGrid, ReferenceRemoteControl, view);
            if (hullScanGrid == null || hullScanGrid.IsEmpty)
                hullScanGrid = shipGrid;

            int resolutionMax = Math.Max(1, resolution - 1);
            float sampleMinX;
            float sampleMaxX;
            float sampleMinY;
            float sampleMaxY;
            GetDefaultViewportSampleBounds(shipGrid, out sampleMinX, out sampleMaxX, out sampleMinY, out sampleMaxY);
            float depthStart = hullScanGrid.DepthMin - 1.0f;
            float depthEnd = hullScanGrid.DepthMax + 1.0f;
            data.SetSampleBounds(sampleMinX, sampleMaxX, sampleMinY, sampleMaxY, depthStart, depthEnd);
            ResetRaycastDiagnostics(view);
            int raycastLayer = GetRaycastLayer();
            var hits = new List<IHitInfo>(16);
            var hitDistances = new List<float>(16);
            var projectedOccupancy = BuildProjectedOccupancySet(hullScanGrid);
            float sampleStepX = resolutionMax > 0 ? (sampleMaxX - sampleMinX) / resolutionMax : 0f;
            float sampleStepY = resolutionMax > 0 ? (sampleMaxY - sampleMinY) / resolutionMax : 0f;

            for (int y = 0; y < resolution; y++)
            {
                float ty = y / (float)resolutionMax;
                float sampleY = sampleMinY + ty * (sampleMaxY - sampleMinY);

                for (int x = 0; x < resolution; x++)
                {
                    float tx = x / (float)resolutionMax;
                    float sampleX = sampleMinX + tx * (sampleMaxX - sampleMinX);
                    var sample = CastProjectedPhysicsRay(rootGrid, shipGrid, view, sampleX, sampleY, sampleStepX, sampleStepY, depthStart, depthEnd, raycastLayer, hits, hitDistances, projectedOccupancy);
                    if (!sample.HasHit)
                        continue;

                    int index = y * resolution + x;
                    data.Samples[index] = sample;
                    data.HitSampleCount++;

                    if (sample.HitCount > data.MaxHitCount)
                        data.MaxHitCount = sample.HitCount;
                    if (sample.Thickness > data.MaxThickness)
                        data.MaxThickness = sample.Thickness;
                }
            }

            data.IsReady = true;
            data.ScannedUtc = DateTime.UtcNow;
            return data;
        }

        HashSet<Vector2I> BuildProjectedOccupancySet(ShipGrid shipGrid)
        {
            var occupied = new HashSet<Vector2I>();
            if (shipGrid == null || shipGrid.Blocks == null)
                return occupied;

            for (int i = 0; i < shipGrid.Blocks.Count; i++)
            {
                occupied.Add(shipGrid.Blocks[i].Projected);
            }

            return occupied;
        }

        int GetRaycastLayer()
        {
            try
            {
                int layer = MyAPIGateway.Physics.GetCollisionLayer("DefaultCollisionLayer");
                if (layer >= 0)
                    return layer;
            }
            catch
            {
            }

            return 15;
        }

        RawRaycastSample CastProjectedPhysicsRay(IMyCubeGrid rootGrid, ShipGrid shipGrid, ScanView view, float sampleX, float sampleY, float sampleStepX, float sampleStepY, float depthStart, float depthEnd, int raycastLayer, List<IHitInfo> hits, List<float> hitDistances, HashSet<Vector2I> projectedOccupancy)
        {
            var sample = CastProjectedPhysicsRaySingle(rootGrid, shipGrid, view, sampleX, sampleY, depthStart, depthEnd, raycastLayer, hits, hitDistances);
            if (!HasProjectedStructureNear(projectedOccupancy, sampleX, sampleY) || !ShouldRefineRaycastSample(sample))
                return sample;

            float offsetX = Math.Abs(sampleStepX) * 0.35f;
            float offsetY = Math.Abs(sampleStepY) * 0.35f;
            if (offsetX <= 0f && offsetY <= 0f)
                return sample;

            var best = sample;
            if (offsetX > 0f)
            {
                best = ChooseStrongerSample(best, CastProjectedPhysicsRaySingle(rootGrid, shipGrid, view, sampleX - offsetX, sampleY, depthStart, depthEnd, raycastLayer, hits, hitDistances));
                best = ChooseStrongerSample(best, CastProjectedPhysicsRaySingle(rootGrid, shipGrid, view, sampleX + offsetX, sampleY, depthStart, depthEnd, raycastLayer, hits, hitDistances));
            }

            if (offsetY > 0f)
            {
                best = ChooseStrongerSample(best, CastProjectedPhysicsRaySingle(rootGrid, shipGrid, view, sampleX, sampleY - offsetY, depthStart, depthEnd, raycastLayer, hits, hitDistances));
                best = ChooseStrongerSample(best, CastProjectedPhysicsRaySingle(rootGrid, shipGrid, view, sampleX, sampleY + offsetY, depthStart, depthEnd, raycastLayer, hits, hitDistances));
            }

            return best;
        }

        bool ShouldRefineRaycastSample(RawRaycastSample sample)
        {
            if (!sample.HasHit)
                return true;

            return sample.HitCount <= 1 || sample.Thickness <= 0.02f;
        }

        bool HasProjectedStructureNear(HashSet<Vector2I> projectedOccupancy, float sampleX, float sampleY)
        {
            if (projectedOccupancy == null || projectedOccupancy.Count == 0)
                return false;

            int centerX = (int)Math.Floor(sampleX + 0.5f);
            int centerY = (int)Math.Floor(sampleY + 0.5f);
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    if (projectedOccupancy.Contains(new Vector2I(centerX + dx, centerY + dy)))
                        return true;
                }
            }

            return false;
        }

        RawRaycastSample ChooseStrongerSample(RawRaycastSample current, RawRaycastSample candidate)
        {
            if (!candidate.HasHit)
                return current;

            if (!current.HasHit)
                return candidate;

            if (candidate.HitCount > current.HitCount)
                return candidate;

            if (candidate.HitCount == current.HitCount && candidate.Thickness > current.Thickness)
                return candidate;

            return current;
        }

        RawRaycastSample CastProjectedPhysicsRaySingle(IMyCubeGrid rootGrid, ShipGrid shipGrid, ScanView view, float sampleX, float sampleY, float depthStart, float depthEnd, int raycastLayer, List<IHitInfo> hits, List<float> hitDistances)
        {
            var sample = new RawRaycastSample
            {
                HitCount = 0,
                FirstDistance = float.MaxValue,
                LastDistance = float.MinValue
            };

            hits.Clear();
            hitDistances.Clear();

            var from = shipGrid.BasisToWorld(shipGrid.Unproject(sampleX, sampleY, depthStart, view));
            var to = shipGrid.BasisToWorld(shipGrid.Unproject(sampleX, sampleY, depthEnd, view));

            try
            {
                MyAPIGateway.Physics.CastRay(from, to, hits, raycastLayer);
            }
            catch
            {
                return sample;
            }

            if (LastRaycastDiagnostics != null)
                LastRaycastDiagnostics.RaysCast++;

            if (hits.Count == 0)
                return sample;

            if (LastRaycastDiagnostics != null)
            {
                LastRaycastDiagnostics.RaysWithPhysicsHit++;
                LastRaycastDiagnostics.PhysicsHitCount += hits.Count;
            }

            var ray = to - from;
            double rayLengthSquared = ray.LengthSquared();
            if (rayLengthSquared <= 0.0001)
                return sample;

            for (int i = 0; i < hits.Count; i++)
            {
                var hit = hits[i];
                if (!IsAcceptedRaycastHit(hit))
                    continue;

                float distance = (float)(Vector3D.Dot(hit.Position - from, ray) / rayLengthSquared);
                sample.HitCount++;
                hitDistances.Add(distance);

                if (distance < sample.FirstDistance)
                    sample.FirstDistance = distance;
                if (distance > sample.LastDistance)
                    sample.LastDistance = distance;
            }

            if (!sample.HasHit)
            {
                sample.FirstDistance = 0f;
                sample.LastDistance = 0f;
            }
            else
            {
                if (LastRaycastDiagnostics != null)
                    LastRaycastDiagnostics.RaysWithAcceptedHit++;
                PopulateSegmentProfile(ref sample, hitDistances);
            }

            return sample;
        }

        void GetDefaultViewportSampleBounds(ShipGrid shipGrid, out float sampleMinX, out float sampleMaxX, out float sampleMinY, out float sampleMaxY)
        {
            sampleMinX = shipGrid.Min2D.X - 0.5f;
            sampleMaxX = shipGrid.Max2D.X + 0.5f;
            sampleMinY = shipGrid.Min2D.Y - 0.5f;
            sampleMaxY = shipGrid.Max2D.Y + 0.5f;
        }

        void PopulateSegmentProfile(ref RawRaycastSample sample, List<float> hitDistances)
        {
            if (hitDistances == null || hitDistances.Count == 0)
                return;

            hitDistances.Sort();
            const float segmentGap = 0.035f;
            int segmentCount = 1;
            float segmentStart = hitDistances[0];
            float previous = hitDistances[0];
            float largestSolid = 0f;
            float largestVoid = Math.Max(0f, segmentStart);

            for (int i = 1; i < hitDistances.Count; i++)
            {
                float current = hitDistances[i];
                float gap = current - previous;
                if (gap > segmentGap)
                {
                    segmentCount++;
                    float solidSpan = previous - segmentStart;
                    if (solidSpan > largestSolid)
                        largestSolid = solidSpan;
                    if (gap > largestVoid)
                        largestVoid = gap;
                    segmentStart = current;
                }

                previous = current;
            }

            float finalSolidSpan = previous - segmentStart;
            if (finalSolidSpan > largestSolid)
                largestSolid = finalSolidSpan;

            float trailingVoid = Math.Max(0f, 1f - previous);
            if (trailingVoid > largestVoid)
                largestVoid = trailingVoid;

            sample.SegmentCount = segmentCount;
            sample.TransitionComplexity = Math.Max(0, segmentCount - 1);
            sample.LargestSolidSegment = largestSolid;
            sample.LargestVoidSegment = largestVoid;
        }

        bool IsAcceptedRaycastHit(IHitInfo hit)
        {
            if (hit == null || hit.HitEntity == null)
            {
                if (LastRaycastDiagnostics != null)
                    LastRaycastDiagnostics.RejectedNoGridHitCount++;
                return false;
            }

            var grid = ResolveHitGrid(hit.HitEntity);
            if (grid == null || grid.MarkedForClose)
            {
                if (LastRaycastDiagnostics != null)
                    LastRaycastDiagnostics.RejectedNoGridHitCount++;
                return false;
            }

            if (!IsHullScanGrid(grid.EntityId))
            {
                if (LastRaycastDiagnostics != null)
                    LastRaycastDiagnostics.RejectedOutsideTargetHitCount++;
                return false;
            }

            if (LastRaycastDiagnostics != null)
            {
                LastRaycastDiagnostics.AcceptedHitCount++;
                if (ConnectorHullScanGridIds != null && ConnectorHullScanGridIds.Contains(grid.EntityId))
                    LastRaycastDiagnostics.AcceptedConnectorHitCount++;
            }
            return true;
        }

        bool IsHullScanGrid(long entityId)
        {
            return ValidHullScanGridIds.Contains(entityId);
        }

        void ResetRaycastDiagnostics(ScanView view)
        {
            if (LastRaycastDiagnostics == null)
                LastRaycastDiagnostics = new RaycastScanDiagnostics();

            int connectorTargetCount = CountConnectorHullTargets();
            LastRaycastDiagnostics.Reset(view, HullScanTargetGrids.Count, connectorTargetCount, BuildHullTargetSummary(), LastConnectorDiscoveryDiagnostics);
        }

        int CountConnectorHullTargets()
        {
            if (HullScanTargetGrids == null || ConstructGrids == null)
                return 0;

            var constructIds = new HashSet<long>();
            for (int i = 0; i < ConstructGrids.Count; i++)
            {
                if (ConstructGrids[i] != null)
                    constructIds.Add(ConstructGrids[i].EntityId);
            }

            int count = 0;
            for (int i = 0; i < HullScanTargetGrids.Count; i++)
            {
                var grid = HullScanTargetGrids[i];
                if (grid != null && !constructIds.Contains(grid.EntityId))
                    count++;
            }

            return count;
        }

        string BuildHullTargetSummary()
        {
            if (HullScanTargetGrids == null || HullScanTargetGrids.Count == 0)
                return "none";

            var builder = new StringBuilder();
            int shown = 0;
            for (int i = 0; i < HullScanTargetGrids.Count && shown < 4; i++)
            {
                var grid = HullScanTargetGrids[i];
                if (grid == null)
                    continue;

                if (shown > 0)
                    builder.Append(", ");

                builder.Append(GetGridDebugName(grid));
                shown++;
            }

            int remaining = HullScanTargetGrids.Count - shown;
            if (remaining > 0)
                builder.Append(" +").Append(remaining);

            return builder.Length > 0 ? builder.ToString() : "none";
        }

        static string GetGridDebugName(IMyCubeGrid grid)
        {
            if (grid == null)
                return "null";

            string name = null;
            try
            {
                name = grid.DisplayName;
            }
            catch
            {
            }

            if (string.IsNullOrEmpty(name))
                name = grid.EntityId.ToString();

            return name + (IsGridStatic(grid) ? " [static]" : "");
        }

        IMyCubeGrid ResolveHitGrid(IMyEntity entity)
        {
            var current = entity;
            while (current != null)
            {
                var grid = current as IMyCubeGrid;
                if (grid != null)
                    return grid;

                var block = current as IMyCubeBlock;
                if (block != null)
                    return block.CubeGrid;

                current = current.Parent;
            }

            return null;
        }

        public class IncrementalRaycastScanJob
        {
            enum JobStage
            {
                Preparing,
                Scanning,
                Composing,
                Caching,
                Complete
            }

            readonly ScanCache _cache;
            readonly IMyCubeGrid _rootGrid;
            readonly int _resolution;
            readonly ScanView[] _views = new[] { ScanView.Top, ScanView.Side, ScanView.Front };
            readonly List<IHitInfo> _hits = new List<IHitInfo>(16);
            readonly List<float> _hitDistances = new List<float>(16);

            JobStage _stage = JobStage.Preparing;
            int _viewIndex;
            int _sampleIndex;
            int _composeIndex;
            int _axisTotalSamples;
            int _resolutionMax;
            int _raycastLayer;
            float _sampleMinX;
            float _sampleMaxX;
            float _sampleMinY;
            float _sampleMaxY;
            float _depthStart;
            float _depthEnd;
            float _sampleStepX;
            float _sampleStepY;
            ShipGrid _shipGrid;
            RawRaycastScanData _data;
            HashSet<Vector2I> _projectedOccupancy;

            public bool IsComplete
            {
                get { return _stage == JobStage.Complete; }
            }

            public string StageLabel
            {
                get
                {
                    if (_stage == JobStage.Caching)
                        return "Updating topology cache";
                    if (_stage == JobStage.Complete)
                        return "Complete";

                    string axis = CurrentAxisLabel;
                    if (_stage == JobStage.Composing)
                        return "Composing " + axis;
                    if (_stage == JobStage.Scanning)
                        return "Scanning " + axis;
                    return "Preparing " + axis;
                }
            }

            public string CurrentAxisLabel
            {
                get
                {
                    var view = CurrentView;
                    if (view == ScanView.Top)
                        return "TOP";
                    if (view == ScanView.Side)
                        return "SIDE";
                    return "FRONT";
                }
            }

            public float AxisProgress
            {
                get
                {
                    if (_stage == JobStage.Complete)
                        return 1f;
                    if (_stage == JobStage.Caching)
                        return 1f;
                    if (_axisTotalSamples <= 0)
                        return 0f;
                    if (_stage == JobStage.Composing)
                        return Clamp01(_composeIndex / (float)_axisTotalSamples);
                    if (_stage == JobStage.Scanning)
                        return Clamp01(_sampleIndex / (float)_axisTotalSamples);
                    return 0f;
                }
            }

            public float OverallProgress
            {
                get
                {
                    if (_stage == JobStage.Complete)
                        return 1f;
                    if (_stage == JobStage.Caching)
                        return 0.97f;

                    float axisWeight = 0.32f;
                    float completed = _viewIndex * axisWeight;
                    if (_stage == JobStage.Scanning)
                        completed += AxisProgress * axisWeight * 0.82f;
                    else if (_stage == JobStage.Composing)
                        completed += axisWeight * 0.82f + AxisProgress * axisWeight * 0.18f;

                    return Clamp01(completed);
                }
            }

            ScanView CurrentView
            {
                get
                {
                    if (_viewIndex < 0)
                        return _views[0];
                    if (_viewIndex >= _views.Length)
                        return _views[_views.Length - 1];
                    return _views[_viewIndex];
                }
            }

            public IncrementalRaycastScanJob(ScanCache cache, IMyCubeGrid rootGrid, int resolution)
            {
                _cache = cache;
                _rootGrid = rootGrid;
                _resolution = Math.Max(1, resolution);
                _axisTotalSamples = Math.Max(1, _resolution * _resolution);
            }

            public bool Advance(int scanSampleBudget, int composeSampleBudget)
            {
                if (IsComplete)
                    return false;

                bool progressed = false;
                if (scanSampleBudget < 1)
                    scanSampleBudget = 1;
                if (composeSampleBudget < 1)
                    composeSampleBudget = 1;

                while (!IsComplete)
                {
                    if (_stage == JobStage.Preparing)
                    {
                        PrepareCurrentAxis();
                        progressed = true;
                        if (_stage == JobStage.Scanning)
                            continue;
                        return progressed;
                    }

                    if (_stage == JobStage.Scanning)
                    {
                        progressed = AdvanceScanning(scanSampleBudget) || progressed;
                        return progressed;
                    }

                    if (_stage == JobStage.Composing)
                    {
                        progressed = AdvanceComposing(composeSampleBudget) || progressed;
                        return progressed;
                    }

                    if (_stage == JobStage.Caching)
                    {
                        FinishCaching();
                        progressed = true;
                        return progressed;
                    }
                }

                return progressed;
            }

            void PrepareCurrentAxis()
            {
                if (_rootGrid == null || _rootGrid.MarkedForClose || _cache == null)
                {
                    _stage = JobStage.Complete;
                    return;
                }

                if (_viewIndex >= _views.Length)
                {
                    _stage = JobStage.Caching;
                    return;
                }

                _cache.RefreshConstructGrids(_rootGrid);
                var view = CurrentView;
                _shipGrid = ShipGrid.BuildFromConstruct(_cache.ConstructGrids, _cache.BasisGrid ?? _rootGrid, _cache.ReferenceRemoteControl, view);
                _cache.ProjectedGrids[view] = _shipGrid;
                _cache.ShipGrid = _shipGrid;
                _data = new RawRaycastScanData(view, _resolution);
                _sampleIndex = 0;
                _composeIndex = 0;
                _axisTotalSamples = Math.Max(1, _resolution * _resolution);

                if (_shipGrid == null || _shipGrid.IsEmpty || _shipGrid.Blocks.Count == 0 || MyAPIGateway.Physics == null)
                {
                    _stage = JobStage.Composing;
                    return;
                }

                var hullScanGrid = ShipGrid.BuildFromConstruct(_cache.HullScanTargetGrids, _cache.BasisGrid ?? _rootGrid, _cache.ReferenceRemoteControl, view);
                if (hullScanGrid == null || hullScanGrid.IsEmpty)
                    hullScanGrid = _shipGrid;

                _resolutionMax = Math.Max(1, _resolution - 1);
                _cache.GetDefaultViewportSampleBounds(_shipGrid, out _sampleMinX, out _sampleMaxX, out _sampleMinY, out _sampleMaxY);
                _depthStart = hullScanGrid.DepthMin - 1.0f;
                _depthEnd = hullScanGrid.DepthMax + 1.0f;
                _data.SetSampleBounds(_sampleMinX, _sampleMaxX, _sampleMinY, _sampleMaxY, _depthStart, _depthEnd);
                _cache.ResetRaycastDiagnostics(view);
                _raycastLayer = _cache.GetRaycastLayer();
                _projectedOccupancy = _cache.BuildProjectedOccupancySet(hullScanGrid);
                _sampleStepX = _resolutionMax > 0 ? (_sampleMaxX - _sampleMinX) / _resolutionMax : 0f;
                _sampleStepY = _resolutionMax > 0 ? (_sampleMaxY - _sampleMinY) / _resolutionMax : 0f;
                _stage = JobStage.Scanning;
            }

            bool AdvanceScanning(int sampleBudget)
            {
                if (_data == null)
                {
                    _stage = JobStage.Preparing;
                    return false;
                }

                int processed = 0;
                while (processed < sampleBudget && _sampleIndex < _axisTotalSamples)
                {
                    int y = _sampleIndex / _resolution;
                    int x = _sampleIndex - y * _resolution;
                    float tx = _resolutionMax > 0 ? x / (float)_resolutionMax : 0f;
                    float ty = _resolutionMax > 0 ? y / (float)_resolutionMax : 0f;
                    float sampleX = _sampleMinX + tx * (_sampleMaxX - _sampleMinX);
                    float sampleY = _sampleMinY + ty * (_sampleMaxY - _sampleMinY);
                    var sample = _cache.CastProjectedPhysicsRay(_rootGrid, _shipGrid, CurrentView, sampleX, sampleY, _sampleStepX, _sampleStepY, _depthStart, _depthEnd, _raycastLayer, _hits, _hitDistances, _projectedOccupancy);
                    if (sample.HasHit)
                        _data.Samples[_sampleIndex] = sample;

                    _sampleIndex++;
                    processed++;
                }

                if (_sampleIndex >= _axisTotalSamples)
                    _stage = JobStage.Composing;

                return processed > 0;
            }

            bool AdvanceComposing(int composeBudget)
            {
                if (_data == null)
                {
                    _stage = JobStage.Preparing;
                    return false;
                }

                int processed = 0;
                while (processed < composeBudget && _composeIndex < _axisTotalSamples)
                {
                    var sample = _data.Samples[_composeIndex];
                    if (sample.HasHit)
                    {
                        _data.HitSampleCount++;
                        if (sample.HitCount > _data.MaxHitCount)
                            _data.MaxHitCount = sample.HitCount;
                        if (sample.Thickness > _data.MaxThickness)
                            _data.MaxThickness = sample.Thickness;
                    }

                    _composeIndex++;
                    processed++;
                }

                if (_composeIndex >= _axisTotalSamples)
                {
                    _data.IsReady = true;
                    _data.ScannedUtc = DateTime.UtcNow;
                    _cache.RaycastData[CurrentView] = _data;
                    _cache.MarkUpdated();
                    _viewIndex++;
                    _stage = _viewIndex >= _views.Length ? JobStage.Caching : JobStage.Preparing;
                }

                return processed > 0;
            }

            void FinishCaching()
            {
                if (_cache != null && _rootGrid != null && !_rootGrid.MarkedForClose)
                {
                    _cache.RefreshConstructGrids(_rootGrid);
                    _cache.ConveyorNetwork = ConveyorTopology.Discover(_cache.ConstructGrids, _cache.BasisGrid ?? _rootGrid, _cache.ReferenceRemoteControl);
                    _cache.MarkUpdated();
                }

                _stage = JobStage.Complete;
            }

            static float Clamp01(float value)
            {
                if (value < 0f)
                    return 0f;
                if (value > 1f)
                    return 1f;
                return value;
            }
        }
    }

    public class RaycastScanDiagnostics
    {
        public ScanView View;
        public int HullTargetGridCount;
        public int ConnectorTargetGridCount;
        public int ConnectorGridsVisited;
        public int ConnectorsFound;
        public int LinkedConnectorsFound;
        public int NullOtherGridCount;
        public int RaysCast;
        public int RaysWithPhysicsHit;
        public int RaysWithAcceptedHit;
        public int PhysicsHitCount;
        public int AcceptedHitCount;
        public int AcceptedConnectorHitCount;
        public int RejectedNoGridHitCount;
        public int RejectedOutsideTargetHitCount;
        public string TargetSummary;
        public string ConnectorSummary;

        public void Reset(ScanView view, int hullTargetGridCount, int connectorTargetGridCount, string targetSummary, ConnectorScanDiscoveryDiagnostics connectorDiagnostics)
        {
            View = view;
            HullTargetGridCount = hullTargetGridCount;
            ConnectorTargetGridCount = connectorTargetGridCount;
            ConnectorGridsVisited = connectorDiagnostics != null ? connectorDiagnostics.GridsVisited : 0;
            ConnectorsFound = connectorDiagnostics != null ? connectorDiagnostics.ConnectorsFound : 0;
            LinkedConnectorsFound = connectorDiagnostics != null ? connectorDiagnostics.LinkedConnectorsFound : 0;
            NullOtherGridCount = connectorDiagnostics != null ? connectorDiagnostics.NullOtherGridCount : 0;
            RaysCast = 0;
            RaysWithPhysicsHit = 0;
            RaysWithAcceptedHit = 0;
            PhysicsHitCount = 0;
            AcceptedHitCount = 0;
            AcceptedConnectorHitCount = 0;
            RejectedNoGridHitCount = 0;
            RejectedOutsideTargetHitCount = 0;
            TargetSummary = string.IsNullOrEmpty(targetSummary) ? "none" : targetSummary;
            ConnectorSummary = connectorDiagnostics != null ? connectorDiagnostics.AttachedGridSummary : "none";
        }
    }

    public class ConnectorScanDiscoveryDiagnostics
    {
        const int MaxSummaryItems = 4;
        public int GridsVisited;
        public int ConnectorsFound;
        public int LinkedConnectorsFound;
        public int NullOtherGridCount;
        public string AttachedGridSummary;
        int _attachedGridCount;

        public void Reset()
        {
            GridsVisited = 0;
            ConnectorsFound = 0;
            LinkedConnectorsFound = 0;
            NullOtherGridCount = 0;
            AttachedGridSummary = "none";
            _attachedGridCount = 0;
        }

        public void AddAttachedGrid(string name)
        {
            if (string.IsNullOrEmpty(name))
                name = "grid";

            if (_attachedGridCount == 0)
            {
                AttachedGridSummary = name;
            }
            else if (_attachedGridCount < MaxSummaryItems)
            {
                AttachedGridSummary += ", " + name;
            }
            else if (_attachedGridCount == MaxSummaryItems)
            {
                AttachedGridSummary += " +more";
            }

            _attachedGridCount++;
        }
    }

    public struct RawRaycastSample
    {
        public int HitCount;
        public float FirstDistance;
        public float LastDistance;
        public int SegmentCount;
        public int TransitionComplexity;
        public float LargestSolidSegment;
        public float LargestVoidSegment;

        public bool HasHit
        {
            get { return HitCount > 0; }
        }

        public float Thickness
        {
            get { return HitCount > 0 ? LastDistance - FirstDistance : 0f; }
        }
    }

    public class RawRaycastScanData
    {
        public ScanView View { get; private set; }
        public int Resolution { get; private set; }
        public bool IsReady { get; set; }
        public DateTime ScannedUtc { get; set; }
        public RawRaycastSample[] Samples { get; private set; }
        public int HitSampleCount { get; set; }
        public int MaxHitCount { get; set; }
        public float MaxThickness { get; set; }
        public float SampleMinX { get; private set; }
        public float SampleMaxX { get; private set; }
        public float SampleMinY { get; private set; }
        public float SampleMaxY { get; private set; }
        public float DepthStart { get; private set; }
        public float DepthEnd { get; private set; }

        public RawRaycastScanData(ScanView view, int resolution)
        {
            View = view;
            Resolution = resolution;
            IsReady = false;
            ScannedUtc = DateTime.MinValue;
            Samples = new RawRaycastSample[Math.Max(1, resolution * resolution)];
            HitSampleCount = 0;
            MaxHitCount = 0;
            MaxThickness = 0f;
            SampleMinX = 0f;
            SampleMaxX = Math.Max(1, resolution);
            SampleMinY = 0f;
            SampleMaxY = Math.Max(1, resolution);
            DepthStart = 0f;
            DepthEnd = 1f;
        }

        public void SetSampleBounds(float sampleMinX, float sampleMaxX, float sampleMinY, float sampleMaxY, float depthStart, float depthEnd)
        {
            SampleMinX = sampleMinX;
            SampleMaxX = sampleMaxX;
            SampleMinY = sampleMinY;
            SampleMaxY = sampleMaxY;
            DepthStart = depthStart;
            DepthEnd = depthEnd;
        }
    }

    public class ViewScanData
    {
        public ScanView View { get; }
        public int Resolution { get; set; }
        public bool IsReady { get; set; }
        public byte[] OccupancyMask { get; set; }
        public float[] DepthBuffer { get; set; }

        public ViewScanData(ScanView view)
        {
            View = view;
            Resolution = 256;
            IsReady = false;
            OccupancyMask = Array.Empty<byte>();
            DepthBuffer = Array.Empty<float>();
        }
    }
}
