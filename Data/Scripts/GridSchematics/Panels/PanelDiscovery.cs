using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace PingoPete.GridSchematics
{
    public partial class GridSchematicsSession
    {
        // Current single tagged panel discovery; multipanel routing starts here.
        bool TryFindTaggedPanel()
        {
            HashSet<IMyEntity> entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities, e => e is IMyCubeGrid);

            foreach (var entity in entities)
            {
                var grid = entity as IMyCubeGrid;
                if (grid == null || grid.MarkedForClose)
                    continue;

                var cubeGrid = grid as MyCubeGrid;
                if (cubeGrid == null)
                    continue;

                foreach (var fat in cubeGrid.GetFatBlocks())
                {
                    var panel = fat as IMyTextPanel;
                    if (panel == null)
                        continue;

                    if ((panel.CustomName ?? "").IndexOf(TAG, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    _panel = panel;
                    _hostGrid = panel.CubeGrid;
                    return true;
                }
            }

            return false;
        }
    }
}
