namespace GridSchematics
{
    public partial class GridSchematicsLcdApp
    {
        public bool CaptureLayoutSnapshotNow(string label)
        {
            return CaptureLayoutSnapshotNow(label, "full");
        }

        public bool CaptureLayoutSnapshotNow(string label, string scope)
        {
            if (!IsOwnerFunctional || !Config.Enabled || Surface == null)
                return false;

            RenderEngine.RequestLayoutSnapshot(label, scope);
            RenderPanel(false);
            return RenderEngine.LastSnapshotSpriteCount > 0;
        }
    }
}