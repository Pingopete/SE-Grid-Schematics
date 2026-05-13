using Sandbox.ModAPI;
using System;
using VRageMath;

namespace GridSchematics
{
    public partial class GridSchematicsSession
    {
        const string PanelCalibrationDebugCommand = "\\PanelCalTest";

        bool _panelCalibrationDebugEnabled;
        bool _panelCalibrationDebugCommandRegistered;

        void RegisterPanelCalibrationDebugCommand()
        {
            if (_panelCalibrationDebugCommandRegistered || MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.MessageEntered += OnPanelCalibrationDebugMessageEntered;
            _panelCalibrationDebugCommandRegistered = true;
        }

        void UnregisterPanelCalibrationDebugCommand()
        {
            if (!_panelCalibrationDebugCommandRegistered || MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.MessageEntered -= OnPanelCalibrationDebugMessageEntered;
            _panelCalibrationDebugCommandRegistered = false;
        }

        void OnPanelCalibrationDebugMessageEntered(string messageText, ref bool sendToOthers)
        {
            string command = messageText == null ? string.Empty : messageText.Trim();
            string[] parts = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 ||
                !parts[0].Equals(PanelCalibrationDebugCommand, StringComparison.OrdinalIgnoreCase) &&
                !parts[0].Equals("/PanelCalTest", StringComparison.OrdinalIgnoreCase) &&
                !parts[0].Equals("GSDISPLAYCAL", StringComparison.OrdinalIgnoreCase) &&
                !parts[0].Equals("/GSDISPLAYCAL", StringComparison.OrdinalIgnoreCase) &&
                !parts[0].Equals("\\GSDISPLAYCAL", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            sendToOthers = false;
            HandlePanelCalibrationPrototypeCommand(parts, "GSDISPLAYCAL");
        }

        void DrawPanelCalibrationDebug()
        {
            if (!_panelCalibrationDebugEnabled)
                return;

            for (int i = 0; i < _apps.Count; i++)
                DrawPanelCalibrationDebugForApp(_apps[i]);

            for (int i = 0; i < _surfaceScriptApps.Count; i++)
                DrawPanelCalibrationDebugForApp(_surfaceScriptApps[i]);

            for (int i = _unsupportedSurfaceProbes.Count - 1; i >= 0; i--)
            {
                TouchScreenApiAdapter probe = _unsupportedSurfaceProbes[i];
                if (probe == null || probe.OwnerBlock == null || probe.OwnerBlock.MarkedForClose)
                {
                    _unsupportedSurfaceProbes.RemoveAt(i);
                    continue;
                }

                DrawPanelCalibrationDebugForInput(probe);
            }
        }

        void DrawPanelCalibrationDebugForApp(GridSchematicsLcdApp app)
        {
            if (app == null || app.Panel == null || !app.Panel.IsFunctional || app.TouchInput == null)
                return;

            DrawPanelCalibrationDebugForInput(app.TouchInput);
        }

        void DrawPanelCalibrationDebugForInput(TouchScreenApiAdapter input)
        {
            if (input == null)
                return;

            DrawPanelCalibrationRawHits(input);

            PanelSurfaceCandidate candidate = input.LastPanelSurfaceCandidate;
            if (!candidate.IsValid)
                return;

            Vector3D normal = candidate.Normal;
            Vector3D axisA = candidate.AxisA;
            Vector3D axisB = candidate.AxisB;
            if (normal.LengthSquared() <= 0.000001 ||
                axisA.LengthSquared() <= 0.000001 ||
                axisB.LengthSquared() <= 0.000001)
            {
                return;
            }

            normal.Normalize();
            axisA.Normalize();
            axisB.Normalize();

            int surfaceIndex = input.GetSurfaceIndex();
            double layerOffset = GetPanelCalibrationDebugLayerOffset(surfaceIndex);
            Vector3D offset = normal * layerOffset;
            Color color = GetPanelSurfaceCandidateDebugColor(candidate.Quality);

            Vector3D topLeft;
            Vector3D topRight;
            Vector3D bottomRight;
            Vector3D bottomLeft;
            BuildPanelCalibrationCandidateCorners(candidate, offset, out topLeft, out topRight, out bottomRight, out bottomLeft);
            DrawPanelCursorSurfaceDebugEdge(topLeft, topRight, bottomRight, bottomLeft, color, 0.0035f);
            DrawPanelCalibrationCornerMarks(topLeft, topRight, bottomRight, bottomLeft, color);

            Vector3D seed = candidate.Seed + offset;
            Vector3D center = candidate.Center + offset;
            double markerScale = Math.Min(Math.Max(candidate.Width, candidate.Height) * 0.18, 0.14);
            if (markerScale < 0.035)
                markerScale = 0.035;

            DrawWorldDebugCross(seed, 0.014f, 0.0038f, new Color(255, 245, 70, 245));
            DrawWorldDebugCross(center, 0.020f, 0.004f, new Color(255, 255, 255, 245));
            DrawWorldDebugArrow(center, axisA, markerScale, new Color(255, 80, 80, 245));
            DrawWorldDebugArrow(center, axisB, markerScale, new Color(80, 255, 120, 245));
            DrawWorldDebugArrow(center, normal, Math.Min(markerScale * 0.75, 0.10), new Color(80, 160, 255, 245));

            DrawPanelCalibrationScoreMeter(center, axisA, axisB, normal, markerScale, candidate.Score, color);
            DrawSurfaceIndexTicks(center - axisA * markerScale * 0.70 - axisB * markerScale * 0.70, axisA, axisB, normal, surfaceIndex);

            Vector3D planeHit;
            if (input.TryGetPanelCursorLastPlaneHit(out planeHit))
                DrawWorldDebugCross(planeHit + normal * (layerOffset + 0.012), 0.011f, 0.003f, new Color(255, 255, 255, 210));

            Vector3D detectorHit;
            bool detectorInside;
            if (input.TryGetLastAimDetectorDebugHit(out detectorHit, out detectorInside))
            {
                Color detectorColor = detectorInside ? new Color(60, 240, 255, 235) : new Color(255, 70, 70, 235);
                DrawWorldDebugCross(detectorHit + normal * (layerOffset + 0.018), 0.012f, 0.003f, detectorColor);
            }
        }

        void DrawPanelCalibrationRawHits(TouchScreenApiAdapter input)
        {
            if (input == null || MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return;

            var camera = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D rayDirection = camera.Forward;
            if (rayDirection.LengthSquared() <= 0.000001)
                return;
            rayDirection.Normalize();

            int surfaceIndex = input.GetSurfaceIndex();
            double layerOffset = GetPanelCalibrationDebugLayerOffset(surfaceIndex);

            Vector3D modelHit;
            Vector3D modelNormal;
            if (input.TryGetCurrentModelAimDebugHit(out modelHit, out modelNormal))
            {
                Vector3D normal = modelNormal;
                if (normal.LengthSquared() > 0.000001)
                    normal.Normalize();
                else
                    normal = -rayDirection;

                Vector3D hit = modelHit + normal * (layerOffset + 0.014);
                DrawWorldDebugCross(hit, 0.015f, 0.0035f, new Color(255, 245, 70, 235));
                DrawWorldDebugNormalTick(hit, normal, new Color(255, 245, 70, 220));
            }

            Vector3D detectorHit;
            if (input.TryGetCurrentDetectorAimDebugHit(out detectorHit))
            {
                DrawWorldDebugCross(detectorHit - rayDirection * 0.020, 0.010f, 0.0027f, new Color(60, 240, 255, 215));
            }
        }

        static void BuildPanelCalibrationCandidateCorners(PanelSurfaceCandidate candidate, Vector3D offset, out Vector3D topLeft, out Vector3D topRight, out Vector3D bottomRight, out Vector3D bottomLeft)
        {
            topLeft = candidate.Seed + candidate.AxisA * candidate.MinA + candidate.AxisB * candidate.MaxB + offset;
            topRight = candidate.Seed + candidate.AxisA * candidate.MaxA + candidate.AxisB * candidate.MaxB + offset;
            bottomRight = candidate.Seed + candidate.AxisA * candidate.MaxA + candidate.AxisB * candidate.MinB + offset;
            bottomLeft = candidate.Seed + candidate.AxisA * candidate.MinA + candidate.AxisB * candidate.MinB + offset;
        }

        static void DrawPanelCalibrationCornerMarks(Vector3D topLeft, Vector3D topRight, Vector3D bottomRight, Vector3D bottomLeft, Color color)
        {
            DrawPanelCalibrationCornerMark(topLeft, topRight - topLeft, bottomLeft - topLeft, color);
            DrawPanelCalibrationCornerMark(topRight, topLeft - topRight, bottomRight - topRight, color);
            DrawPanelCalibrationCornerMark(bottomRight, bottomLeft - bottomRight, topRight - bottomRight, color);
            DrawPanelCalibrationCornerMark(bottomLeft, bottomRight - bottomLeft, topLeft - bottomLeft, color);
        }

        static void DrawPanelCalibrationCornerMark(Vector3D corner, Vector3D edgeA, Vector3D edgeB, Color color)
        {
            double lenA = edgeA.Length();
            double lenB = edgeB.Length();
            if (lenA <= 0.0001 || lenB <= 0.0001)
                return;

            edgeA /= lenA;
            edgeB /= lenB;
            double markLength = Math.Min(Math.Min(lenA, lenB) * 0.08, 0.032);
            if (markLength < 0.010)
                markLength = 0.010;

            DrawWorldDebugLine(corner, corner + edgeA * markLength, 0.004f, color);
            DrawWorldDebugLine(corner, corner + edgeB * markLength, 0.004f, color);
        }

        static void DrawPanelCalibrationScoreMeter(Vector3D center, Vector3D axisA, Vector3D axisB, Vector3D normal, double markerScale, double score, Color color)
        {
            double width = Math.Min(Math.Max(markerScale * 1.15, 0.055), 0.18);
            Vector3D start = center - axisA * (width * 0.5) - axisB * Math.Min(markerScale * 0.48, 0.055) + normal * 0.006;
            Vector3D end = start + axisA * width;
            DrawWorldDebugLine(start, end, 0.006f, new Color(70, 80, 90, 210));

            double fill = score / 100.0;
            if (fill < 0.0) fill = 0.0;
            if (fill > 1.0) fill = 1.0;
            DrawWorldDebugLine(start, start + axisA * (width * fill), 0.0075f, color);

            for (int i = 0; i <= 4; i++)
            {
                Vector3D tick = start + axisA * (width * i / 4.0);
                DrawWorldDebugLine(tick - axisB * 0.006, tick + axisB * 0.006, 0.0022f, new Color(220, 230, 235, 190));
            }
        }

        static double GetPanelCalibrationDebugLayerOffset(int surfaceIndex)
        {
            int normalized = surfaceIndex < 0 ? 0 : surfaceIndex;
            return 0.018 + Math.Min(normalized, 8) * 0.007;
        }
    }
}
