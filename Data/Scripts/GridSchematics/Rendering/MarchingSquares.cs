using VRageMath;

namespace PingoPete.GridSchematics
{
    public partial class GridSchematicsSession
    {
        void BuildMarchingSquaresLines()
        {
            _msLines.Clear();

            _sw.Reset();
            _sw.Start();

            for (int y = 0; y < _activeRes - 1; y++)
            {
                for (int x = 0; x < _activeRes - 1; x++)
                {
                    bool bl = _occupied[x, y];
                    bool br = _occupied[x + 1, y];
                    bool tr = _occupied[x + 1, y + 1];
                    bool tl = _occupied[x, y + 1];

                    int mask = 0;
                    if (bl) mask |= 1;
                    if (br) mask |= 2;
                    if (tr) mask |= 4;
                    if (tl) mask |= 8;

                    if (mask == 0 || mask == 15)
                        continue;

                    Vector2 pL = new Vector2(x, y + 0.5f);
                    Vector2 pR = new Vector2(x + 1f, y + 0.5f);
                    Vector2 pB = new Vector2(x + 0.5f, y);
                    Vector2 pT = new Vector2(x + 0.5f, y + 1f);

                    switch (mask)
                    {
                        case 1:  AddMsLine(pL, pB); break;
                        case 2:  AddMsLine(pB, pR); break;
                        case 3:  AddMsLine(pL, pR); break;
                        case 4:  AddMsLine(pR, pT); break;
                        case 5:  AddMsLine(pL, pT); AddMsLine(pB, pR); break;
                        case 6:  AddMsLine(pB, pT); break;
                        case 7:  AddMsLine(pL, pT); break;
                        case 8:  AddMsLine(pT, pL); break;
                        case 9:  AddMsLine(pT, pB); break;
                        case 10: AddMsLine(pT, pR); AddMsLine(pL, pB); break;
                        case 11: AddMsLine(pT, pR); break;
                        case 12: AddMsLine(pR, pL); break;
                        case 13: AddMsLine(pB, pR); break;
                        case 14: AddMsLine(pL, pB); break;
                    }
                }
            }

            _sw.Stop();
            _lastMarchMs = _sw.Elapsed.TotalMilliseconds;
            _lastMarchLines = _msLines.Count;
        }

        void AddMsLine(Vector2 a, Vector2 b)
        {
            _msLines.Add(new LineSeg(a, b));
        }
    }
}
