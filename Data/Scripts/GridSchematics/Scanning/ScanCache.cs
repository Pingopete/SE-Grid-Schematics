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

        // QW5/QW8: reused world-entity scratch set so RefreshConstructGrids does not allocate a fresh
        // HashSet every call (it runs several times per scan), and so a single snapshot can be shared.
        readonly HashSet<IMyEntity> _scratchEntities = new HashSet<IMyEntity>();

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

        public bool IsConnectorHullScanGrid(long entityId)
        {
            return ConnectorHullScanGridIds != null && ConnectorHullScanGridIds.Contains(entityId);
        }

        public int ConnectorHullScanGridCount
        {
            get { return ConnectorHullScanGridIds != null ? ConnectorHullScanGridIds.Count : 0; }
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
            return UpdateConstructProjection(rootGrid, view, force, true);
        }

        // QW8: split "re-discover construct membership" (an O(world) GetEntities sweep + IsSameConstructAs +
        // connector BFS) from "rebuild the motion-invariant projection". The projection can refresh on the
        // fast cadence while membership re-discovery runs only ~1x/sec, instead of sweeping the world every
        // ~12 ticks per panel. A fresh/empty cache still re-discovers regardless of the flag.
        public ShipGrid UpdateConstructProjection(IMyCubeGrid rootGrid, ScanView view, bool force, bool rediscover)
        {
            if (rootGrid == null)
                return ShipGrid;

            ShipGrid projectedGrid;
            if (force || !ProjectedGrids.TryGetValue(view, out projectedGrid) || projectedGrid == null)
            {
                if (rediscover || ConstructGrids.Count == 0 || BasisGrid == null)
                    RefreshConstructGrids(rootGrid);
                projectedGrid = ShipGrid.BuildFromConstruct(ConstructGrids, BasisGrid ?? rootGrid, ReferenceRemoteControl, view);
                ProjectedGrids[view] = projectedGrid;
                MarkUpdated();
            }
            ShipGrid = projectedGrid;
            return ShipGrid;
        }

        public void UpdateCachedRaycastScans(IMyCubeGrid rootGrid, int resolution, bool superSampling = true)
        {
            // QW8: take one world-entity snapshot and share it across all three view scans instead of
            // sweeping the world once per view.
            var entities = _scratchEntities;
            entities.Clear();
            if (MyAPIGateway.Entities != null)
                MyAPIGateway.Entities.GetEntities(entities, entity => entity is IMyCubeGrid);

            UpdateCachedRaycastScan(rootGrid, ScanView.Top, resolution, entities, superSampling);
            UpdateCachedRaycastScan(rootGrid, ScanView.Side, resolution, entities, superSampling);
            UpdateCachedRaycastScan(rootGrid, ScanView.Front, resolution, entities, superSampling);
        }

        public RawRaycastScanData UpdateCachedRaycastScan(IMyCubeGrid rootGrid, ScanView view, int resolution)
        {
            return UpdateCachedRaycastScan(rootGrid, view, resolution, null, true);
        }

        public RawRaycastScanData UpdateCachedRaycastScan(IMyCubeGrid rootGrid, ScanView view, int resolution, HashSet<IMyEntity> providedEntities, bool superSampling = true)
        {
            if (rootGrid == null)
                return null;

            if (resolution <= 0)
                resolution = 256;

            RefreshConstructGrids(rootGrid, providedEntities);
            var projectedGrid = ShipGrid.BuildFromConstruct(ConstructGrids, BasisGrid ?? rootGrid, ReferenceRemoteControl, view);
            ProjectedGrids[view] = projectedGrid;

            var data = BuildRawPhysicsRaycastScan(rootGrid, projectedGrid, view, resolution, superSampling);
            RaycastData[view] = data;
            MarkUpdated();
            return data;
        }

        public IncrementalRaycastScanJob BeginIncrementalRaycastScans(IMyCubeGrid rootGrid, int resolution, bool superSampling = true)
        {
            if (resolution <= 0)
                resolution = 256;

            return new IncrementalRaycastScanJob(this, rootGrid, resolution, superSampling);
        }

        void RefreshConstructGrids(IMyCubeGrid rootGrid)
        {
            RefreshConstructGrids(rootGrid, null);
        }

        // QW8: callers that need several construct refreshes close together (e.g. the 3-view cached
        // raycast scan) can pass one shared world-entity snapshot to avoid re-sweeping the world each time.
        void RefreshConstructGrids(IMyCubeGrid rootGrid, HashSet<IMyEntity> providedEntities)
        {
            ConstructGrids.Clear();
            HullScanTargetGrids.Clear();
            ValidHullScanGridIds.Clear();
            ConnectorHullScanGridIds.Clear();
            BasisGrid = rootGrid;
            ReferenceRemoteControl = null;
            if (LastConnectorDiscoveryDiagnostics == null)
                LastConnectorDiscoveryDiagnostics = new ConnectorScanDiscoveryDiagnostics();

            HashSet<IMyEntity> entities;
            if (providedEntities != null)
            {
                entities = providedEntities;
            }
            else
            {
                entities = _scratchEntities;
                entities.Clear();
                MyAPIGateway.Entities.GetEntities(entities, entity => entity is IMyCubeGrid);
            }

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

            var blocks = new List<IMySlimBlock>(); // QW5: reused across BFS iterations instead of one alloc per visited grid
            while (queue.Count > 0)
            {
                var grid = queue.Dequeue();
                if (diagnostics != null)
                    diagnostics.GridsVisited++;

                blocks.Clear();
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

                // Coarse, HOST-SCOPED signature: identity + block count + integer bounding box of each
                // same-construct grid, sorted by EntityId. Deliberately excludes per-block positions AND
                // connector-attached grids (which reconnect asynchronously on world load). This lets an
                // unchanged ship reliably re-match its saved scan across reloads, while genuine structural
                // edits (blocks added / removed / grid resized) still invalidate it.
                var constructGrids = new List<IMyCubeGrid>(ConstructGrids);
                SortGridsByEntityId(constructGrids);
                AccumulateHash(ref hash, constructGrids.Count);
                for (int i = 0; i < constructGrids.Count; i++)
                    AccumulateGridSignature(ref hash, constructGrids[i]);

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
            AccumulateHash(ref hash, (int)grid.GridSizeEnum);
            AccumulateHash(ref hash, grid.Min.X);
            AccumulateHash(ref hash, grid.Min.Y);
            AccumulateHash(ref hash, grid.Min.Z);
            AccumulateHash(ref hash, grid.Max.X);
            AccumulateHash(ref hash, grid.Max.Y);
            AccumulateHash(ref hash, grid.Max.Z);

            var blocks = new List<IMySlimBlock>();
            try
            {
                grid.GetBlocks(blocks);
            }
            catch
            {
            }

            AccumulateHash(ref hash, blocks.Count);
        }

        static void SortGridsByEntityId(List<IMyCubeGrid> grids)
        {
            if (grids == null)
                return;

            grids.Sort((a, b) =>
            {
                long aId = a != null ? a.EntityId : 0;
                long bId = b != null ? b.EntityId : 0;
                return aId.CompareTo(bId);
            });
        }

        static int CompareSlimBlocks(IMySlimBlock a, IMySlimBlock b)
        {
            if (ReferenceEquals(a, b))
                return 0;
            if (a == null)
                return -1;
            if (b == null)
                return 1;

            int cmp = a.Position.X.CompareTo(b.Position.X);
            if (cmp != 0)
                return cmp;
            cmp = a.Position.Y.CompareTo(b.Position.Y);
            if (cmp != 0)
                return cmp;
            cmp = a.Position.Z.CompareTo(b.Position.Z);
            if (cmp != 0)
                return cmp;

            cmp = a.Min.X.CompareTo(b.Min.X);
            if (cmp != 0)
                return cmp;
            cmp = a.Min.Y.CompareTo(b.Min.Y);
            if (cmp != 0)
                return cmp;
            cmp = a.Min.Z.CompareTo(b.Min.Z);
            if (cmp != 0)
                return cmp;

            cmp = a.Max.X.CompareTo(b.Max.X);
            if (cmp != 0)
                return cmp;
            cmp = a.Max.Y.CompareTo(b.Max.Y);
            if (cmp != 0)
                return cmp;
            return a.Max.Z.CompareTo(b.Max.Z);
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

        RawRaycastScanData BuildRawPhysicsRaycastScan(IMyCubeGrid rootGrid, ShipGrid shipGrid, ScanView view, int resolution, bool superSampling)
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
            var intervals = new List<float>(16);
            var intervalScratch = new List<float>(16);
            var projectedOccupancy = ProjectedOccupancyMap.Build(hullScanGrid);
            var profileBuilder = new ScanDepthProfile.Builder(resolution * resolution);
            var frame = BuildRayBasisFrame(rootGrid, shipGrid);
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
                    var sample = CastProjectedPhysicsRay(shipGrid, view, sampleX, sampleY, sampleStepX, sampleStepY, depthStart, depthEnd, raycastLayer, ref frame, hits, hitDistances, projectedOccupancy, superSampling, intervals, intervalScratch);
                    if (!sample.HasHit)
                        continue;

                    int index = y * resolution + x;
                    data.Samples[index] = sample;
                    data.HitSampleCount++;
                    profileBuilder.AppendCell(index, intervals);

                    if (sample.HitCount > data.MaxHitCount)
                        data.MaxHitCount = sample.HitCount;
                    if (sample.Thickness > data.MaxThickness)
                        data.MaxThickness = sample.Thickness;
                }
            }

            data.DepthProfile = profileBuilder.Finish();
            data.IsReady = true;
            data.ScannedUtc = DateTime.UtcNow;
            return data;
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

        RawRaycastSample CastProjectedPhysicsRay(ShipGrid shipGrid, ScanView view, float sampleX, float sampleY, float sampleStepX, float sampleStepY, float depthStart, float depthEnd, int raycastLayer, ref RayBasisFrame frame, List<IHitInfo> hits, List<float> hitDistances, ProjectedOccupancyMap projectedOccupancy, bool superSampling, List<float> intervals, List<float> intervalScratch)
        {
            var sample = CastProjectedPhysicsRaySingle(shipGrid, view, sampleX, sampleY, depthStart, depthEnd, raycastLayer, ref frame, hits, hitDistances, intervals);
            // SUPER_SAMPLING ON = legacy refinement predicate (fidelity mode, default).
            // OFF = recovery-only: extra rays are spent only when the center ray missed entirely.
            bool wantRefine = superSampling ? ShouldRefineRaycastSample(sample) : !sample.HasHit;
            if (!wantRefine || projectedOccupancy == null || !projectedOccupancy.IsNearStructure(sampleX, sampleY))
                return sample;

            float offsetX = Math.Abs(sampleStepX) * 0.35f;
            float offsetY = Math.Abs(sampleStepY) * 0.35f;
            if (offsetX <= 0f && offsetY <= 0f)
                return sample;

            var best = sample;
            if (offsetX > 0f)
            {
                best = RefineSample(best, shipGrid, view, sampleX - offsetX, sampleY, depthStart, depthEnd, raycastLayer, ref frame, hits, hitDistances, intervals, intervalScratch);
                best = RefineSample(best, shipGrid, view, sampleX + offsetX, sampleY, depthStart, depthEnd, raycastLayer, ref frame, hits, hitDistances, intervals, intervalScratch);
            }

            if (offsetY > 0f)
            {
                best = RefineSample(best, shipGrid, view, sampleX, sampleY - offsetY, depthStart, depthEnd, raycastLayer, ref frame, hits, hitDistances, intervals, intervalScratch);
                best = RefineSample(best, shipGrid, view, sampleX, sampleY + offsetY, depthStart, depthEnd, raycastLayer, ref frame, hits, hitDistances, intervals, intervalScratch);
            }

            return best;
        }

        // Casts one refinement ray; if it wins, its depth intervals replace the current best's.
        RawRaycastSample RefineSample(RawRaycastSample best, ShipGrid shipGrid, ScanView view, float sampleX, float sampleY, float depthStart, float depthEnd, int raycastLayer, ref RayBasisFrame frame, List<IHitInfo> hits, List<float> hitDistances, List<float> intervals, List<float> intervalScratch)
        {
            var candidate = CastProjectedPhysicsRaySingle(shipGrid, view, sampleX, sampleY, depthStart, depthEnd, raycastLayer, ref frame, hits, hitDistances, intervalScratch);
            if (!CandidateSampleWins(best, candidate))
                return best;

            if (intervals != null && intervalScratch != null)
            {
                intervals.Clear();
                for (int i = 0; i < intervalScratch.Count; i++)
                    intervals.Add(intervalScratch[i]);
            }

            return candidate;
        }

        bool ShouldRefineRaycastSample(RawRaycastSample sample)
        {
            if (!sample.HasHit)
                return true;

            return sample.HitCount <= 1 || sample.Thickness <= 0.02f;
        }

        static bool CandidateSampleWins(RawRaycastSample current, RawRaycastSample candidate)
        {
            if (!candidate.HasHit)
                return false;

            if (!current.HasHit)
                return true;

            if (candidate.HitCount > current.HitCount)
                return true;

            return candidate.HitCount == current.HitCount && candidate.Thickness > current.Thickness;
        }

        public RayBasisFrame BuildRayBasisFrame(IMyCubeGrid rootGrid, ShipGrid shipGrid)
        {
            MatrixD referenceMatrix = GetCurrentReferenceMatrix(rootGrid, shipGrid);
            float gridSize = shipGrid != null && shipGrid.BasisGridSizeMeters > 0f ? shipGrid.BasisGridSizeMeters : 2.5f;
            return new RayBasisFrame
            {
                Origin = referenceMatrix.Translation,
                Right = referenceMatrix.Right * gridSize,
                Up = referenceMatrix.Up * gridSize,
                Forward = referenceMatrix.Forward * gridSize
            };
        }

        RawRaycastSample CastProjectedPhysicsRaySingle(ShipGrid shipGrid, ScanView view, float sampleX, float sampleY, float depthStart, float depthEnd, int raycastLayer, ref RayBasisFrame frame, List<IHitInfo> hits, List<float> hitDistances, List<float> intervals)
        {
            var sample = new RawRaycastSample
            {
                HitCount = 0,
                FirstDistance = float.MaxValue,
                LastDistance = float.MinValue
            };

            hits.Clear();
            hitDistances.Clear();
            if (intervals != null)
                intervals.Clear();

            var from = frame.ToWorld(shipGrid.Unproject(sampleX, sampleY, depthStart, view));
            var to = frame.ToWorld(shipGrid.Unproject(sampleX, sampleY, depthEnd, view));

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
                PopulateSegmentProfile(ref sample, hitDistances, intervals);
            }

            return sample;
        }

        MatrixD GetCurrentReferenceMatrix(IMyCubeGrid rootGrid, ShipGrid shipGrid)
        {
            try
            {
                if (ReferenceRemoteControl != null && !ReferenceRemoteControl.MarkedForClose)
                    return ReferenceRemoteControl.WorldMatrix;

                if (BasisGrid != null && !BasisGrid.MarkedForClose)
                    return BasisGrid.WorldMatrix;

                if (rootGrid != null && !rootGrid.MarkedForClose)
                    return rootGrid.WorldMatrix;
            }
            catch
            {
            }

            return shipGrid != null ? shipGrid.ReferenceMatrix : MatrixD.Identity;
        }
        void GetDefaultViewportSampleBounds(ShipGrid shipGrid, out float sampleMinX, out float sampleMaxX, out float sampleMinY, out float sampleMaxY)
        {
            sampleMinX = shipGrid.Min2D.X - 0.5f;
            sampleMaxX = shipGrid.Max2D.X + 0.5f;
            sampleMinY = shipGrid.Min2D.Y - 0.5f;
            sampleMaxY = shipGrid.Max2D.Y + 0.5f;
        }

        // Collapses the sorted hit sequence into the sample summary AND (when a list is provided)
        // emits the solid depth intervals — one (start, end) pair per contiguous segment — that feed
        // the ScanDepthProfile slice store.
        void PopulateSegmentProfile(ref RawRaycastSample sample, List<float> hitDistances, List<float> intervals)
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
                    if (intervals != null)
                    {
                        intervals.Add(segmentStart);
                        intervals.Add(previous);
                    }
                    segmentStart = current;
                }

                previous = current;
            }

            float finalSolidSpan = previous - segmentStart;
            if (finalSolidSpan > largestSolid)
                largestSolid = finalSolidSpan;
            if (intervals != null)
            {
                intervals.Add(segmentStart);
                intervals.Add(previous);
            }

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
                Caching,
                Complete
            }

            // R6: the per-tick scan budget is time-boxed rather than a fixed ray count, and shared
            // session-globally so two panels scanning at once cannot double the frame cost.
            const double ScanTickBudgetMs = 1.25;
            const int MinCellsPerTick = 256;
            const int MaxCellsPerTick = 4096;
            static readonly System.Diagnostics.Stopwatch ScanBudgetTimer = new System.Diagnostics.Stopwatch();
            static int _globalBudgetTick = -1;
            static int _globalCellsThisTick;

            readonly ScanCache _cache;
            readonly IMyCubeGrid _rootGrid;
            readonly int _resolution;
            readonly bool _superSampling;
            readonly ScanView[] _views = new[] { ScanView.Top, ScanView.Side, ScanView.Front };
            readonly List<IHitInfo> _hits = new List<IHitInfo>(16);
            readonly List<float> _hitDistances = new List<float>(16);
            readonly List<float> _intervals = new List<float>(16);
            readonly List<float> _intervalScratch = new List<float>(16);

            JobStage _stage = JobStage.Preparing;
            int _viewIndex;
            int _sampleIndex;
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
            ProjectedOccupancyMap _projectedOccupancy;
            ScanDepthProfile.Builder _profileBuilder;
            RayBasisFrame _rayFrame;

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
                        completed += AxisProgress * axisWeight;

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

            public IncrementalRaycastScanJob(ScanCache cache, IMyCubeGrid rootGrid, int resolution, bool superSampling)
            {
                _cache = cache;
                _rootGrid = rootGrid;
                _resolution = Math.Max(1, resolution);
                _superSampling = superSampling;
                _axisTotalSamples = Math.Max(1, _resolution * _resolution);
            }

            public bool Advance(int tick)
            {
                if (IsComplete)
                    return false;

                if (tick != _globalBudgetTick)
                {
                    _globalBudgetTick = tick;
                    _globalCellsThisTick = 0;
                }

                bool progressed = false;
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
                        progressed = AdvanceScanning() || progressed;
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
                _axisTotalSamples = Math.Max(1, _resolution * _resolution);
                _profileBuilder = new ScanDepthProfile.Builder(_axisTotalSamples);

                if (_shipGrid == null || _shipGrid.IsEmpty || _shipGrid.Blocks.Count == 0 || MyAPIGateway.Physics == null)
                {
                    FinalizeAxis();
                    return;
                }

                // R3: the hull-target projection only differs from the construct projection when
                // connector-attached grids are present — skip the second full block walk otherwise.
                ShipGrid hullScanGrid;
                if (_cache.HullScanTargetGrids.Count == _cache.ConstructGrids.Count)
                {
                    hullScanGrid = _shipGrid;
                }
                else
                {
                    hullScanGrid = ShipGrid.BuildFromConstruct(_cache.HullScanTargetGrids, _cache.BasisGrid ?? _rootGrid, _cache.ReferenceRemoteControl, view);
                    if (hullScanGrid == null || hullScanGrid.IsEmpty)
                        hullScanGrid = _shipGrid;
                }

                _resolutionMax = Math.Max(1, _resolution - 1);
                _cache.GetDefaultViewportSampleBounds(_shipGrid, out _sampleMinX, out _sampleMaxX, out _sampleMinY, out _sampleMaxY);
                _depthStart = hullScanGrid.DepthMin - 1.0f;
                _depthEnd = hullScanGrid.DepthMax + 1.0f;
                _data.SetSampleBounds(_sampleMinX, _sampleMaxX, _sampleMinY, _sampleMaxY, _depthStart, _depthEnd);
                _cache.ResetRaycastDiagnostics(view);
                _raycastLayer = _cache.GetRaycastLayer();
                _projectedOccupancy = ProjectedOccupancyMap.Build(hullScanGrid);
                _sampleStepX = _resolutionMax > 0 ? (_sampleMaxX - _sampleMinX) / _resolutionMax : 0f;
                _sampleStepY = _resolutionMax > 0 ? (_sampleMaxY - _sampleMinY) / _resolutionMax : 0f;
                _stage = JobStage.Scanning;
            }

            bool AdvanceScanning()
            {
                if (_data == null)
                {
                    _stage = JobStage.Preparing;
                    return false;
                }

                // R4: resolve the reference matrix once per tick slice — the grid cannot move
                // mid-tick on the sim thread — instead of twice per ray.
                _rayFrame = _cache.BuildRayBasisFrame(_rootGrid, _shipGrid);
                ScanBudgetTimer.Restart();
                int processed = 0;
                while (_sampleIndex < _axisTotalSamples)
                {
                    if (_globalCellsThisTick >= MaxCellsPerTick)
                        break;
                    if (processed >= MinCellsPerTick && (processed & 31) == 0 &&
                        ScanBudgetTimer.Elapsed.TotalMilliseconds > ScanTickBudgetMs)
                        break;

                    int y = _sampleIndex / _resolution;
                    int x = _sampleIndex - y * _resolution;
                    float tx = _resolutionMax > 0 ? x / (float)_resolutionMax : 0f;
                    float ty = _resolutionMax > 0 ? y / (float)_resolutionMax : 0f;
                    float sampleX = _sampleMinX + tx * (_sampleMaxX - _sampleMinX);
                    float sampleY = _sampleMinY + ty * (_sampleMaxY - _sampleMinY);
                    var sample = _cache.CastProjectedPhysicsRay(_shipGrid, CurrentView, sampleX, sampleY, _sampleStepX, _sampleStepY, _depthStart, _depthEnd, _raycastLayer, ref _rayFrame, _hits, _hitDistances, _projectedOccupancy, _superSampling, _intervals, _intervalScratch);
                    if (sample.HasHit)
                    {
                        // R5: accumulate the view statistics inline (the old Composing stage was a
                        // redundant second pass over every sample).
                        _data.Samples[_sampleIndex] = sample;
                        _data.HitSampleCount++;
                        if (sample.HitCount > _data.MaxHitCount)
                            _data.MaxHitCount = sample.HitCount;
                        if (sample.Thickness > _data.MaxThickness)
                            _data.MaxThickness = sample.Thickness;
                        _profileBuilder.AppendCell(_sampleIndex, _intervals);
                    }

                    _sampleIndex++;
                    processed++;
                    _globalCellsThisTick++;
                }

                if (_sampleIndex >= _axisTotalSamples)
                    FinalizeAxis();

                return processed > 0;
            }

            void FinalizeAxis()
            {
                if (_data != null)
                {
                    _data.DepthProfile = _profileBuilder != null ? _profileBuilder.Finish() : null;
                    _data.IsReady = true;
                    _data.ScannedUtc = DateTime.UtcNow;
                    _cache.RaycastData[CurrentView] = _data;
                    _cache.MarkUpdated();
                }

                _profileBuilder = null;
                _viewIndex++;
                _stage = _viewIndex >= _views.Length ? JobStage.Caching : JobStage.Preparing;
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

    // Per-cell solid depth spans captured during the scan (normalized 0..1 along DepthStart..DepthEnd).
    // This is the depth-resolved store that makes slice rendering possible: a slice is a masked read of
    // these intervals, and the full-range read reproduces the legacy thickness/occupancy summary.
    public class ScanDepthProfile
    {
        public int CellCount { get; private set; }
        // PairOffsets[cell] = index of the cell's first interval pair; PairOffsets[CellCount] = total pairs.
        public int[] PairOffsets { get; private set; }
        // Flattened (start, end) pairs.
        public float[] Bounds { get; private set; }

        ScanDepthProfile(int cellCount, int[] pairOffsets, float[] bounds)
        {
            CellCount = cellCount;
            PairOffsets = pairOffsets;
            Bounds = bounds;
        }

        public int GetPairStart(int cell)
        {
            return PairOffsets[cell];
        }

        public int GetPairCount(int cell)
        {
            return PairOffsets[cell + 1] - PairOffsets[cell];
        }

        // Total solid depth within [rangeStart, rangeEnd], clipped exactly against the cell's intervals.
        public float GetSolidInRange(int cell, float rangeStart, float rangeEnd)
        {
            int start = PairOffsets[cell];
            int end = PairOffsets[cell + 1];
            float total = 0f;
            for (int p = start; p < end; p++)
            {
                float a = Bounds[p * 2];
                float b = Bounds[p * 2 + 1];
                float lo = a > rangeStart ? a : rangeStart;
                float hi = b < rangeEnd ? b : rangeEnd;
                if (hi > lo)
                    total += hi - lo;
            }
            return total;
        }

        // Number of intervals overlapping [rangeStart, rangeEnd] (structural density within a slice).
        public int GetIntervalCountInRange(int cell, float rangeStart, float rangeEnd)
        {
            int start = PairOffsets[cell];
            int end = PairOffsets[cell + 1];
            int count = 0;
            for (int p = start; p < end; p++)
            {
                if (Bounds[p * 2 + 1] >= rangeStart && Bounds[p * 2] <= rangeEnd)
                    count++;
            }
            return count;
        }

        public bool HasSolidInRange(int cell, float rangeStart, float rangeEnd)
        {
            int start = PairOffsets[cell];
            int end = PairOffsets[cell + 1];
            for (int p = start; p < end; p++)
            {
                if (Bounds[p * 2 + 1] >= rangeStart && Bounds[p * 2] <= rangeEnd)
                    return true;
            }
            return false;
        }

        // Cells MUST be appended in ascending index order; skipped cells hold zero intervals.
        public class Builder
        {
            readonly int _cellCount;
            readonly int[] _pairOffsets;
            readonly List<float> _bounds;
            int _cursor = -1;

            public Builder(int cellCount)
            {
                _cellCount = Math.Max(1, cellCount);
                _pairOffsets = new int[_cellCount + 1];
                _bounds = new List<float>(_cellCount / 2);
            }

            public bool AppendCell(int cellIndex, List<float> intervalPairs)
            {
                if (cellIndex <= _cursor || cellIndex >= _cellCount)
                    return false;

                int pairsSoFar = _bounds.Count >> 1;
                for (int i = _cursor + 1; i <= cellIndex; i++)
                    _pairOffsets[i] = pairsSoFar;

                if (intervalPairs != null)
                {
                    for (int i = 0; i + 1 < intervalPairs.Count; i += 2)
                    {
                        _bounds.Add(intervalPairs[i]);
                        _bounds.Add(intervalPairs[i + 1]);
                    }
                }

                _cursor = cellIndex;
                return true;
            }

            public ScanDepthProfile Finish()
            {
                int total = _bounds.Count >> 1;
                for (int i = _cursor + 1; i <= _cellCount; i++)
                    _pairOffsets[i] = total;
                return new ScanDepthProfile(_cellCount, _pairOffsets, _bounds.ToArray());
            }
        }
    }

    // R2: pre-dilated occupancy bitmap. Replaces the per-cell 3x3 HashSet<Vector2I> probe
    // (up to 9 hash lookups per ray cell) with a single array read.
    public class ProjectedOccupancyMap
    {
        bool[] _cells;
        int _minX;
        int _minY;
        int _width;
        int _height;

        public static ProjectedOccupancyMap Build(ShipGrid shipGrid)
        {
            var map = new ProjectedOccupancyMap();
            if (shipGrid == null || shipGrid.IsEmpty || shipGrid.Blocks == null || shipGrid.Blocks.Count == 0)
                return map;

            map._minX = shipGrid.Min2D.X - 2;
            map._minY = shipGrid.Min2D.Y - 2;
            map._width = shipGrid.Max2D.X - shipGrid.Min2D.X + 5;
            map._height = shipGrid.Max2D.Y - shipGrid.Min2D.Y + 5;
            if (map._width <= 0 || map._height <= 0 || (long)map._width * map._height > 64000000L)
            {
                map._cells = null;
                return map;
            }

            map._cells = new bool[map._width * map._height];
            for (int i = 0; i < shipGrid.Blocks.Count; i++)
            {
                var projected = shipGrid.Blocks[i].Projected;
                for (int dy = -1; dy <= 1; dy++)
                {
                    int y = projected.Y + dy - map._minY;
                    if (y < 0 || y >= map._height)
                        continue;
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        int x = projected.X + dx - map._minX;
                        if (x < 0 || x >= map._width)
                            continue;
                        map._cells[y * map._width + x] = true;
                    }
                }
            }

            return map;
        }

        public bool IsNearStructure(float sampleX, float sampleY)
        {
            if (_cells == null)
                return false;

            int x = (int)Math.Floor(sampleX + 0.5f) - _minX;
            int y = (int)Math.Floor(sampleY + 0.5f) - _minY;
            if (x < 0 || y < 0 || x >= _width || y >= _height)
                return false;

            return _cells[y * _width + x];
        }
    }

    // R4: reference matrix + grid size resolved once per tick slice instead of twice per ray.
    public struct RayBasisFrame
    {
        public Vector3D Origin;
        public Vector3D Right;
        public Vector3D Up;
        public Vector3D Forward;

        public Vector3D ToWorld(Vector3D basisPosition)
        {
            return Origin +
                Right * basisPosition.X +
                Up * basisPosition.Y +
                Forward * basisPosition.Z;
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
        // Depth-resolved solid spans per cell; null on legacy data (pre-v12 loads).
        public ScanDepthProfile DepthProfile { get; set; }

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




