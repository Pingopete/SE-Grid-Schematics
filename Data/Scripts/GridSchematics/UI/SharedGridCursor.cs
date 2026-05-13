namespace GridSchematics
{
    public struct SharedGridCursor
    {
        public bool Active;
        public long ConstructId;
        public long SourcePanelId;
        public double X;
        public double Y;
        public double Z;
        public bool HasX;
        public bool HasY;
        public bool HasZ;
        public bool DirectX;
        public bool DirectY;
        public bool DirectZ;
        public int LastUpdatedTick;

        public bool IsFromPanel(long panelId)
        {
            return Active && SourcePanelId == panelId;
        }
    }
}
