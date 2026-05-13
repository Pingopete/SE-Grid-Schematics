using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Models;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using VRageRender;

namespace GridSchematics
{
    public partial class GridSchematicsSession
    {
        const string PanelDiscoveryDebugCommand = "\\PanelTest";
        const int ModelWalkMaxSteps = 90;
        const int ModelWalkRefineSteps = 6;
        const int ModelGridMaxRadiusSteps = 90;
        const double ModelWalkStep = 0.02;
        const double ModelGridSampleStep = 0.035;
        const double ModelWalkCastDepth = 0.07;
        const double ModelWalkPlaneTolerance = 0.01;
        const double ModelWalkNormalDotThreshold = 0.995;
        const int ModelGridMinAcceptedSamples = 8;
        const double ModelWalkSurfaceRefineDepth = 0.16;
        const double PanelDiscoveryCursorSurfaceOffset = 0.018;
        const float PanelDiscoveryCursorHalfSize = 0.004f;
        const float PanelDiscoveryCursorThickness = 0.0016f;

        readonly List<Vector3D> _detectorDebugSamples = new List<Vector3D>();
        bool _panelDiscoveryDebugEnabled;
        bool _panelDiscoveryDebugCommandRegistered;
        bool _globalModelWalkCaptureWasPressed;
        bool _hasGlobalModelWalkDebugCorners;
        IMyEntity _globalModelWalkEntity;
        Vector3D _globalModelWalkTopLeft;
        Vector3D _globalModelWalkTopRight;
        Vector3D _globalModelWalkBottomRight;
        Vector3D _globalModelWalkBottomLeft;
        Vector3D _globalModelWalkNormal;

        internal static bool IsPanelDiscoveryDebugEnabled
        {
            get { return Instance != null && Instance._panelDiscoveryDebugEnabled; }
        }

        void RegisterPanelDiscoveryDebugCommand()
        {
            if (_panelDiscoveryDebugCommandRegistered || MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.MessageEntered += OnPanelDiscoveryDebugMessageEntered;
            _panelDiscoveryDebugCommandRegistered = true;
        }

        void UnregisterPanelDiscoveryDebugCommand()
        {
            if (!_panelDiscoveryDebugCommandRegistered || MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.MessageEntered -= OnPanelDiscoveryDebugMessageEntered;
            _panelDiscoveryDebugCommandRegistered = false;
        }

        void OnPanelDiscoveryDebugMessageEntered(string messageText, ref bool sendToOthers)
        {
            string command = messageText == null ? string.Empty : messageText.Trim();
            if (!command.Equals(PanelDiscoveryDebugCommand, StringComparison.OrdinalIgnoreCase) &&
                !command.Equals("/PanelTest", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            sendToOthers = false;
            _panelDiscoveryDebugEnabled = !_panelDiscoveryDebugEnabled;
            if (!_panelDiscoveryDebugEnabled)
            {
                _hasGlobalModelWalkDebugCorners = false;
                _globalModelWalkCaptureWasPressed = false;
            }

            try
            {
                string state = _panelDiscoveryDebugEnabled ? "ON" : "OFF";
                MyAPIGateway.Utilities.ShowMessage("Grid Schematics", "Panel discovery debug " + state);
                MyAPIGateway.Utilities.ShowNotification("Panel discovery debug " + state, 1600, "White");
            }
            catch
            {
            }
        }

        void DrawPanelDiscoveryDebug()
        {
            if (!_panelDiscoveryDebugEnabled)
                return;

            UpdateGlobalModelWalkDebugCapture();
            DrawGlobalModelWalkDebugEdge();
            DrawDetectorBoundsDebugForKnownPanels();
        }

        void DrawDetectorBoundsDebugForKnownPanels()
        {
            for (int i = 0; i < _apps.Count; i++)
                DrawDetectorBoundsDebugForApp(_apps[i]);

            for (int i = 0; i < _surfaceScriptApps.Count; i++)
                DrawDetectorBoundsDebugForApp(_surfaceScriptApps[i]);

            for (int i = _unsupportedSurfaceProbes.Count - 1; i >= 0; i--)
            {
                TouchScreenApiAdapter probe = _unsupportedSurfaceProbes[i];
                if (probe == null || probe.OwnerBlock == null || probe.OwnerBlock.MarkedForClose)
                {
                    _unsupportedSurfaceProbes.RemoveAt(i);
                    continue;
                }

                DrawDetectorBoundsDebugForProbe(probe);
            }
        }

        void DrawDetectorBoundsDebugForApp(GridSchematicsLcdApp app)
        {
            if (app == null || app.Panel == null || !app.Panel.IsFunctional)
                return;

            DrawDetectorBoundsDebugEdge(app.TouchInput);
            DrawModelWalkDebugEdge(app.TouchInput);
            DrawPanelCursorSurfaceDebugEdge(app.TouchInput);
            DrawPanelCursorSurfaceDebugInternals(app.TouchInput);
        }

        void DrawDetectorBoundsDebugForProbe(TouchScreenApiAdapter input)
        {
            if (input == null)
                return;

            DrawDetectorBoundsDebugEdge(input);
            DrawModelWalkDebugEdge(input);
            DrawPanelCursorSurfaceDebugEdge(input);
            DrawPanelCursorSurfaceDebugInternals(input);
        }

        void DrawModelWalkDebugEdge(TouchScreenApiAdapter input)
        {
            if (input == null)
                return;

            Vector3D topLeft;
            Vector3D topRight;
            Vector3D bottomRight;
            Vector3D bottomLeft;
            if (!input.TryGetModelWalkDebugCorners(out topLeft, out topRight, out bottomRight, out bottomLeft))
                return;

            var color = new Color(255, 70, 230, 235);
            float thickness = 0.005f;
            DrawWorldDebugLine(topLeft, topRight, thickness, color);
            DrawWorldDebugLine(topRight, bottomRight, thickness, color);
            DrawWorldDebugLine(bottomRight, bottomLeft, thickness, color);
            DrawWorldDebugLine(bottomLeft, topLeft, thickness, color);

            var crossColor = new Color(255, 250, 90, 245);
            DrawWorldDebugCross((topLeft + topRight + bottomRight + bottomLeft) * 0.25, 0.025f, 0.0035f, crossColor);
            DrawAimLockedPlaneCursor(topLeft, topRight, bottomRight, bottomLeft, Vector3D.Zero);
        }

        void DrawPanelCursorSurfaceDebugEdge(TouchScreenApiAdapter input)
        {
            if (input == null)
                return;

            Vector3D topLeft;
            Vector3D topRight;
            Vector3D bottomRight;
            Vector3D bottomLeft;
            PanelSurfaceCandidate candidate = input.LastPanelSurfaceCandidate;
            if (candidate.IsValid && input.TryGetLastPanelSurfaceCandidateCorners(out topLeft, out topRight, out bottomRight, out bottomLeft))
            {
                DrawPanelCursorSurfaceDebugEdge(topLeft, topRight, bottomRight, bottomLeft, GetPanelSurfaceCandidateDebugColor(candidate.Quality), 0.007f);
                return;
            }

            if (!input.TryGetPanelCursorSurfaceCorners(out topLeft, out topRight, out bottomRight, out bottomLeft))
                return;

            DrawPanelCursorSurfaceDebugEdge(topLeft, topRight, bottomRight, bottomLeft, new Color(80, 255, 145, 235), 0.006f);
        }

        static Color GetPanelSurfaceCandidateDebugColor(PanelSurfaceCandidateQuality quality)
        {
            if (quality == PanelSurfaceCandidateQuality.Good)
                return new Color(80, 255, 145, 245);
            if (quality == PanelSurfaceCandidateQuality.Questionable)
                return new Color(255, 185, 45, 245);
            if (quality == PanelSurfaceCandidateQuality.Bad)
                return new Color(255, 65, 65, 245);
            return new Color(180, 190, 205, 225);
        }

        static void DrawPanelCursorSurfaceDebugEdge(Vector3D topLeft, Vector3D topRight, Vector3D bottomRight, Vector3D bottomLeft, Color color, float thickness)
        {
            DrawWorldDebugLine(topLeft, topRight, thickness, color);
            DrawWorldDebugLine(topRight, bottomRight, thickness, color);
            DrawWorldDebugLine(bottomRight, bottomLeft, thickness, color);
            DrawWorldDebugLine(bottomLeft, topLeft, thickness, color);
        }

        void DrawPanelCursorSurfaceDebugInternals(TouchScreenApiAdapter input)
        {
            if (input == null)
                return;

            DrawPanelCursorRawAimDebug(input);

            PanelSurfaceCandidate candidate = input.LastPanelSurfaceCandidate;
            if (!candidate.IsValid)
                return;

            Vector3D normal = candidate.Normal;
            Vector3D axisA = candidate.AxisA;
            Vector3D axisB = candidate.AxisB;
            if (normal.LengthSquared() <= 0.000001 || axisA.LengthSquared() <= 0.000001 || axisB.LengthSquared() <= 0.000001)
                return;

            normal.Normalize();
            axisA.Normalize();
            axisB.Normalize();

            Vector3D debugOffset = normal * 0.03;
            Vector3D seed = candidate.Seed + debugOffset;
            Vector3D center = candidate.Center + debugOffset;
            double axisLength = Math.Min(Math.Max(candidate.Width, candidate.Height) * 0.22, 0.18);
            if (axisLength < 0.035)
                axisLength = 0.035;

            DrawWorldDebugCross(seed, 0.018f, 0.004f, new Color(255, 245, 70, 255));
            DrawWorldDebugCross(center, 0.026f, 0.004f, new Color(255, 255, 255, 255));
            DrawWorldDebugArrow(center, axisA, axisLength, new Color(255, 80, 80, 250));
            DrawWorldDebugArrow(center, axisB, axisLength, new Color(80, 255, 120, 250));
            DrawWorldDebugArrow(center, normal, Math.Min(axisLength * 0.7, 0.12), new Color(80, 160, 255, 250));

            int surfaceIndex = input.GetSurfaceIndex();
            DrawSurfaceIndexTicks(center - axisA * axisLength * 0.75 - axisB * axisLength * 0.75, axisA, axisB, normal, surfaceIndex);

            Vector3D planeHit;
            if (input.TryGetPanelCursorLastPlaneHit(out planeHit))
                DrawWorldDebugCross(planeHit + normal * 0.045, 0.014f, 0.003f, new Color(255, 255, 255, 210));

            Vector3D detectorHit;
            bool detectorInside;
            if (input.TryGetLastAimDetectorDebugHit(out detectorHit, out detectorInside))
            {
                DrawWorldDebugCross(detectorHit + normal * 0.055, 0.015f, 0.0035f, detectorInside ? new Color(60, 240, 255, 235) : new Color(255, 70, 70, 235));
            }
        }

        void DrawPanelCursorRawAimDebug(TouchScreenApiAdapter input)
        {
            if (input == null || MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return;

            var camera = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D rayOrigin = camera.Translation;
            Vector3D rayDirection = camera.Forward;
            if (rayDirection.LengthSquared() <= 0.000001)
                return;
            rayDirection.Normalize();

            Vector3D modelHit;
            Vector3D modelNormal;
            bool hasModelHit = input.TryGetCurrentModelAimDebugHit(out modelHit, out modelNormal);
            Vector3D detectorHit;
            bool hasDetectorHit = input.TryGetCurrentDetectorAimDebugHit(out detectorHit);

            if (hasModelHit)
            {
                Vector3D normal = modelNormal;
                if (normal.LengthSquared() > 0.000001)
                    normal.Normalize();
                else
                    normal = -rayDirection;

                DrawWorldDebugCross(modelHit + normal * 0.035, 0.019f, 0.004f, new Color(255, 255, 80, 245));
                DrawWorldDebugNormalTick(modelHit + normal * 0.04, normal, new Color(255, 255, 80, 230));
            }
            else
            {
                DrawWorldDebugCross(rayOrigin + rayDirection * 1.15, 0.018f, 0.003f, new Color(255, 80, 80, 210));
            }

            if (hasDetectorHit)
            {
                DrawWorldDebugCross(detectorHit - rayDirection * 0.025, 0.014f, 0.003f, new Color(60, 240, 255, 220));
            }
        }

        static void DrawWorldDebugNormalTick(Vector3D start, Vector3D normal, Color color)
        {
            if (normal.LengthSquared() <= 0.000001)
                return;

            normal.Normalize();
            DrawWorldDebugLine(start, start + normal * 0.055, 0.003f, color);
        }

        static void DrawSurfaceIndexTicks(Vector3D origin, Vector3D axisA, Vector3D axisB, Vector3D normal, int surfaceIndex)
        {
            int tickCount = surfaceIndex >= 0 ? Math.Min(surfaceIndex + 1, 8) : 1;
            float tickLength = 0.018f;
            float tickSpacing = 0.011f;
            var color = GetSurfaceIndexDebugColor(surfaceIndex);

            for (int i = 0; i < tickCount; i++)
            {
                Vector3D start = origin + axisA * (i * tickSpacing);
                DrawWorldDebugLine(start, start + axisB * tickLength + normal * 0.004, 0.003f, color);
            }
        }

        static Color GetSurfaceIndexDebugColor(int surfaceIndex)
        {
            int normalized = surfaceIndex;
            if (normalized < 0)
                normalized = 0;
            normalized %= 6;
            if (normalized == 0)
                return new Color(255, 245, 80, 250);
            if (normalized == 1)
                return new Color(80, 190, 255, 250);
            if (normalized == 2)
                return new Color(255, 110, 220, 250);
            if (normalized == 3)
                return new Color(120, 255, 120, 250);
            if (normalized == 4)
                return new Color(255, 150, 65, 250);
            return new Color(200, 160, 255, 250);
        }

        void UpdateGlobalModelWalkDebugCapture()
        {
            if (MyAPIGateway.Input == null || MyAPIGateway.Gui == null)
                return;

            bool pressed = false;
            try
            {
                pressed = !MyAPIGateway.Gui.IsCursorVisible && MyAPIGateway.Input.IsLeftMousePressed();
            }
            catch
            {
                pressed = false;
            }

            if (pressed && !_globalModelWalkCaptureWasPressed)
                _hasGlobalModelWalkDebugCorners = TryBuildGlobalModelWalkDebugCorners();

            _globalModelWalkCaptureWasPressed = pressed;
        }

        void DrawGlobalModelWalkDebugEdge()
        {
            if (!_hasGlobalModelWalkDebugCorners)
                return;

            var color = new Color(255, 70, 230, 255);
            float thickness = 0.008f;
            DrawWorldDebugLine(_globalModelWalkTopLeft, _globalModelWalkTopRight, thickness, color);
            DrawWorldDebugLine(_globalModelWalkTopRight, _globalModelWalkBottomRight, thickness, color);
            DrawWorldDebugLine(_globalModelWalkBottomRight, _globalModelWalkBottomLeft, thickness, color);
            DrawWorldDebugLine(_globalModelWalkBottomLeft, _globalModelWalkTopLeft, thickness, color);

            var crossColor = new Color(255, 250, 90, 245);
            DrawWorldDebugCross((_globalModelWalkTopLeft + _globalModelWalkTopRight + _globalModelWalkBottomRight + _globalModelWalkBottomLeft) * 0.25, 0.035f, 0.005f, crossColor);
            DrawAimLockedPlaneCursor(_globalModelWalkTopLeft, _globalModelWalkTopRight, _globalModelWalkBottomRight, _globalModelWalkBottomLeft, _globalModelWalkNormal);
        }

        bool TryBuildGlobalModelWalkDebugCorners()
        {
            _globalModelWalkTopLeft = Vector3D.Zero;
            _globalModelWalkTopRight = Vector3D.Zero;
            _globalModelWalkBottomRight = Vector3D.Zero;
            _globalModelWalkBottomLeft = Vector3D.Zero;
            _globalModelWalkNormal = Vector3D.Zero;
            _globalModelWalkEntity = null;

            Vector3D seed;
            Vector3D normal;
            IMyEntity entity;
            if (!TryGetGlobalModelAimHit(out entity, out seed, out normal))
                return false;

            if (normal.LengthSquared() <= 0.000001)
                return false;
            normal.Normalize();
            if (!TryRefineGlobalModelSeedToVisibleSurface(entity, ref seed, ref normal))
                return false;

            Vector3D axisA;
            Vector3D axisB;
            if (!TryBuildGlobalModelWalkAxes(entity, normal, out axisA, out axisB))
                return false;

            _globalModelWalkEntity = entity;
            double minA;
            double maxA;
            double minB;
            double maxB;
            if (!TryFindGlobalVisibleModelPlaneGridBounds(seed, normal, axisA, axisB, out minA, out maxA, out minB, out maxB))
                return false;

            _globalModelWalkTopLeft = seed + axisA * minA + axisB * maxB;
            _globalModelWalkTopRight = seed + axisA * maxA + axisB * maxB;
            _globalModelWalkBottomRight = seed + axisA * maxA + axisB * minB;
            _globalModelWalkBottomLeft = seed + axisA * minA + axisB * minB;
            _globalModelWalkNormal = normal;
            return true;
        }

        bool TryFindGlobalVisibleModelPlaneGridBounds(Vector3D seed, Vector3D normal, Vector3D axisA, Vector3D axisB, out double minA, out double maxA, out double minB, out double maxB)
        {
            minA = 0.0;
            maxA = 0.0;
            minB = 0.0;
            maxB = 0.0;

            bool foundAny = false;
            int accepted = 0;
            for (int b = -ModelGridMaxRadiusSteps; b <= ModelGridMaxRadiusSteps; b++)
            {
                bool rowFound = false;
                for (int a = -ModelGridMaxRadiusSteps; a <= ModelGridMaxRadiusSteps; a++)
                {
                    double offsetA = a * ModelGridSampleStep;
                    double offsetB = b * ModelGridSampleStep;
                    Vector3D target = seed + axisA * offsetA + axisB * offsetB;
                    if (!IsGlobalSameModelPlaneAt(target, seed, normal))
                        continue;

                    rowFound = true;
                    accepted++;
                    if (!foundAny)
                    {
                        minA = maxA = offsetA;
                        minB = maxB = offsetB;
                        foundAny = true;
                    }
                    else
                    {
                        if (offsetA < minA) minA = offsetA;
                        if (offsetA > maxA) maxA = offsetA;
                        if (offsetB < minB) minB = offsetB;
                        if (offsetB > maxB) maxB = offsetB;
                    }
                }

                if (foundAny && !rowFound && b * ModelGridSampleStep > maxB + ModelGridSampleStep)
                    break;
            }

            if (!foundAny || accepted < ModelGridMinAcceptedSamples)
                return false;

            minA = RefineGlobalGridEdge(seed, normal, axisA, axisB, minA, true);
            maxA = RefineGlobalGridEdge(seed, normal, axisA, axisB, maxA, true);
            minB = RefineGlobalGridEdge(seed, normal, axisB, axisA, minB, false);
            maxB = RefineGlobalGridEdge(seed, normal, axisB, axisA, maxB, false);

            return maxA - minA >= 0.08 && maxB - minB >= 0.08;
        }

        double RefineGlobalGridEdge(Vector3D seed, Vector3D normal, Vector3D edgeAxis, Vector3D crossAxis, double acceptedOffset, bool refineA)
        {
            double direction = acceptedOffset < 0.0 ? -1.0 : 1.0;
            double low = acceptedOffset;
            double high = acceptedOffset + direction * ModelGridSampleStep;

            for (int i = 0; i < ModelWalkRefineSteps; i++)
            {
                double mid = (low + high) * 0.5;
                Vector3D target = seed + edgeAxis * mid;
                if (IsGlobalSameModelPlaneAt(target, seed, normal))
                    low = mid;
                else
                    high = mid;
            }

            return low;
        }

        bool TryGetGlobalModelAimHit(out IMyEntity entity, out Vector3D hitPosition, out Vector3D hitNormal)
        {
            entity = null;
            hitPosition = Vector3D.Zero;
            hitNormal = Vector3D.Zero;
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null || MyAPIGateway.Physics == null)
                return false;

            var camera = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D rayOrigin = camera.Translation;
            Vector3D rayDirection = camera.Forward;
            if (rayDirection.LengthSquared() <= 0.000001)
                return false;
            rayDirection.Normalize();

            Vector3D rayEnd = rayOrigin + rayDirection * 12.0;
            IHitInfo physicsHit;
            if (!MyAPIGateway.Physics.CastRay(rayOrigin, rayEnd, out physicsHit) || physicsHit == null || physicsHit.HitEntity == null)
                return false;

            if (!TryResolveModelEntityFromPhysicsHit(physicsHit, rayDirection, out entity))
                return false;

            return TryGetGlobalModelSegmentHit(entity, rayOrigin, rayEnd, out hitPosition, out hitNormal);
        }

        bool TryRefineGlobalModelSeedToVisibleSurface(IMyEntity entity, ref Vector3D seed, ref Vector3D normal)
        {
            if (entity == null || normal.LengthSquared() <= 0.000001)
                return false;

            normal.Normalize();
            Vector3D toCamera = MyAPIGateway.Session != null && MyAPIGateway.Session.Camera != null
                ? MyAPIGateway.Session.Camera.WorldMatrix.Translation - seed
                : normal;
            Vector3D probeNormal = Vector3D.Dot(toCamera, normal) >= 0.0 ? normal : -normal;

            Vector3D hitPosition;
            Vector3D hitNormal;
            if (!TryGetGlobalModelSegmentHit(entity, seed + probeNormal * ModelWalkSurfaceRefineDepth, seed - probeNormal * ModelWalkSurfaceRefineDepth, out hitPosition, out hitNormal))
                return true;

            if (hitNormal.LengthSquared() <= 0.000001)
                return true;
            hitNormal.Normalize();

            double normalDot = Math.Abs(Vector3D.Dot(hitNormal, normal));
            if (normalDot < ModelWalkNormalDotThreshold)
                return true;

            Vector3D lateral = hitPosition - seed - normal * Vector3D.Dot(hitPosition - seed, normal);
            if (lateral.LengthSquared() > ModelWalkStep * ModelWalkStep)
                return true;

            seed = hitPosition;
            normal = hitNormal;
            if (Vector3D.Dot(probeNormal, normal) < 0.0)
                normal = -normal;
            return true;
        }

        static bool TryResolveModelEntityFromPhysicsHit(IHitInfo physicsHit, Vector3D rayDirection, out IMyEntity entity)
        {
            entity = null;
            if (physicsHit == null || physicsHit.HitEntity == null)
                return false;

            if (physicsHit.HitEntity.Model != null)
            {
                entity = physicsHit.HitEntity;
                return true;
            }

            var grid = physicsHit.HitEntity as IMyCubeGrid;
            if (grid == null)
                return false;

            try
            {
                Vector3D sample = physicsHit.Position + rayDirection * 0.06;
                Vector3I cell = grid.WorldToGridInteger(sample);
                IMyCubeBlock bestBlock = null;
                double bestDistanceSq = double.MaxValue;

                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            var slim = grid.GetCubeBlock(cell + new Vector3I(x, y, z));
                            if (slim == null || slim.FatBlock == null)
                                continue;

                            var blockEntity = slim.FatBlock as IMyEntity;
                            if (blockEntity == null || blockEntity.Model == null)
                                continue;

                            double distanceSq = Vector3D.DistanceSquared(blockEntity.WorldMatrix.Translation, physicsHit.Position);
                            if (distanceSq < bestDistanceSq)
                            {
                                bestDistanceSq = distanceSq;
                                bestBlock = slim.FatBlock;
                            }
                        }
                    }
                }

                entity = bestBlock as IMyEntity;
                return entity != null && entity.Model != null;
            }
            catch
            {
                return false;
            }
        }

        static bool TryBuildGlobalModelWalkAxes(IMyEntity entity, Vector3D normal, out Vector3D axisA, out Vector3D axisB)
        {
            axisA = Vector3D.Zero;
            axisB = Vector3D.Zero;
            if (entity == null)
                return false;

            MatrixD matrix = entity.WorldMatrix;
            Vector3D[] candidates = new Vector3D[]
            {
                matrix.Right,
                matrix.Up,
                matrix.Forward
            };

            double bestLengthSq = 0.0;
            for (int i = 0; i < candidates.Length; i++)
            {
                Vector3D candidate = candidates[i];
                if (candidate.LengthSquared() <= 0.000001)
                    continue;
                candidate.Normalize();
                Vector3D projected = candidate - normal * Vector3D.Dot(candidate, normal);
                double lengthSq = projected.LengthSquared();
                if (lengthSq > bestLengthSq)
                {
                    bestLengthSq = lengthSq;
                    axisA = projected;
                }
            }

            if (bestLengthSq <= 0.000001)
                return false;

            axisA.Normalize();
            axisB = Vector3D.Cross(normal, axisA);
            if (axisB.LengthSquared() <= 0.000001)
                return false;
            axisB.Normalize();
            return true;
        }

        bool IsGlobalSameModelPlaneAt(Vector3D target, Vector3D seed, Vector3D normal)
        {
            Vector3D hitPosition;
            Vector3D hitNormal;
            if (!TryGetGlobalVisibleModelPlaneProbeHit(target, normal, out hitPosition, out hitNormal))
                return false;

            if (hitNormal.LengthSquared() <= 0.000001)
                return false;
            hitNormal.Normalize();

            double normalDot = Math.Abs(Vector3D.Dot(hitNormal, normal));
            if (normalDot < ModelWalkNormalDotThreshold)
                return false;

            double planeDistance = Math.Abs(Vector3D.Dot(hitPosition - seed, normal));
            if (planeDistance > ModelWalkPlaneTolerance)
                return false;

            Vector3D lateral = hitPosition - target - normal * Vector3D.Dot(hitPosition - target, normal);
            return lateral.LengthSquared() <= ModelWalkStep * ModelWalkStep * 0.56;
        }

        bool TryGetGlobalVisibleModelPlaneProbeHit(Vector3D target, Vector3D normal, out Vector3D hitPosition, out Vector3D hitNormal)
        {
            hitPosition = Vector3D.Zero;
            hitNormal = Vector3D.Zero;
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null || _globalModelWalkEntity == null)
                return false;

            Vector3D toCamera = MyAPIGateway.Session.Camera.WorldMatrix.Translation - target;
            double side = Vector3D.Dot(toCamera, normal);
            Vector3D probeNormal = side >= 0.0 ? normal : -normal;
            return TryGetGlobalModelSegmentHit(_globalModelWalkEntity, target + probeNormal * ModelWalkCastDepth, target - probeNormal * ModelWalkCastDepth, out hitPosition, out hitNormal);
        }

        static bool TryGetGlobalModelSegmentHit(IMyEntity entity, Vector3D rayStart, Vector3D rayEnd, out Vector3D hitPosition, out Vector3D hitNormal)
        {
            hitPosition = Vector3D.Zero;
            hitNormal = Vector3D.Zero;
            if (entity == null || entity.Model == null)
                return false;

            try
            {
                var line = new LineD(rayStart, rayEnd);
                VRage.Game.Models.MyIntersectionResultLineTriangleEx? tri;
                if (!entity.GetIntersectionWithLine(ref line, out tri, IntersectionFlags.ALL_TRIANGLES) || !tri.HasValue)
                    return false;

                var hit = tri.Value;
                hitPosition = hit.IntersectionPointInWorldSpace;
                hitNormal = hit.NormalInWorldSpace;
                return true;
            }
            catch
            {
                return false;
            }
        }

        void DrawDetectorBoundsDebugEdge(TouchScreenApiAdapter input)
        {
            if (input == null)
                return;

            Vector3D topLeft;
            Vector3D topRight;
            Vector3D bottomRight;
            Vector3D bottomLeft;
            if (!input.TryGetDetectorBoundsDebugCorners(out topLeft, out topRight, out bottomRight, out bottomLeft))
                return;

            var color = new Color(20, 240, 255, 230);
            float thickness = 0.004f;
            DrawWorldDebugLine(topLeft, topRight, thickness, color);
            DrawWorldDebugLine(topRight, bottomRight, thickness, color);
            DrawWorldDebugLine(bottomRight, bottomLeft, thickness, color);
            DrawWorldDebugLine(bottomLeft, topLeft, thickness, color);

            input.CopyDetectorBoundsDebugSamples(_detectorDebugSamples);
            var sampleColor = new Color(255, 120, 40, 230);
            for (int i = 0; i < _detectorDebugSamples.Count; i++)
                DrawWorldDebugCross(_detectorDebugSamples[i], 0.012f, 0.0025f, sampleColor);
        }

        static void DrawWorldDebugQuad(Vector3D topLeft, Vector3D topRight, Vector3D bottomRight, Vector3D bottomLeft, Color color)
        {
            Vector3D right = topRight - topLeft;
            Vector3D up = topLeft - bottomLeft;
            float width = (float)right.Length();
            float height = (float)up.Length();
            if (width <= 0.0001f || height <= 0.0001f)
                return;

            right /= width;
            up /= height;
            Vector3D center = (topLeft + topRight + bottomRight + bottomLeft) * 0.25;
            MyTransparentGeometry.AddBillboardOriented(CalibrationDebugSquareMaterial, color.ToVector4(), center, right, up, width, height);
        }

        static void DrawAimLockedPlaneCursor(Vector3D topLeft, Vector3D topRight, Vector3D bottomRight, Vector3D bottomLeft, Vector3D preferredNormal)
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return;

            Vector3D axisX = topRight - topLeft;
            Vector3D axisY = topLeft - bottomLeft;
            double width = axisX.Length();
            double height = axisY.Length();
            if (width <= 0.0001 || height <= 0.0001)
                return;

            axisX /= width;
            axisY /= height;

            Vector3D center = (topLeft + topRight + bottomRight + bottomLeft) * 0.25;
            Vector3D normal = preferredNormal;
            if (normal.LengthSquared() <= 0.000001)
                normal = Vector3D.Cross(axisX, axisY);
            if (normal.LengthSquared() <= 0.000001)
                return;
            normal.Normalize();

            var camera = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D rayOrigin = camera.Translation;
            Vector3D rayDirection = camera.Forward;
            if (rayDirection.LengthSquared() <= 0.000001)
                return;
            rayDirection.Normalize();

            if (Vector3D.Dot(camera.Translation - center, normal) < 0.0)
                normal = -normal;

            double denom = Vector3D.Dot(rayDirection, normal);
            if (Math.Abs(denom) <= 0.000001)
                return;

            double rayDistance = Vector3D.Dot(center - rayOrigin, normal) / denom;
            if (rayDistance <= 0.0)
                return;

            Vector3D planeHit = rayOrigin + rayDirection * rayDistance;
            Vector3D delta = planeHit - center;
            double localX = Vector3D.Dot(delta, axisX);
            double localY = Vector3D.Dot(delta, axisY);
            if (localX < width * -0.5 || localX > width * 0.5 ||
                localY < height * -0.5 || localY > height * 0.5)
            {
                return;
            }

            Vector3D cursorCenter = center + axisX * localX + axisY * localY + normal * PanelDiscoveryCursorSurfaceOffset;
            var cursorColor = new Color(255, 250, 90, 255);
            DrawFlatCursorBillboard(cursorCenter, axisX, axisY, cursorColor);
        }

        static void DrawFlatCursorBillboard(Vector3D center, Vector3D axisX, Vector3D axisY, Color color)
        {
            MyTransparentGeometry.AddBillboardOriented(
                CalibrationDebugSquareMaterial,
                color.ToVector4(),
                center,
                axisX,
                axisY,
                PanelDiscoveryCursorHalfSize * 2f,
                PanelDiscoveryCursorThickness);

            MyTransparentGeometry.AddBillboardOriented(
                CalibrationDebugSquareMaterial,
                color.ToVector4(),
                center,
                axisX,
                axisY,
                PanelDiscoveryCursorThickness,
                PanelDiscoveryCursorHalfSize * 2f);
        }

        static void DrawWorldDebugCross(Vector3D center, float size, float thickness, Color color)
        {
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return;

            var camera = MyAPIGateway.Session.Camera.WorldMatrix;
            DrawWorldDebugLine(center - camera.Right * size, center + camera.Right * size, thickness, color);
            DrawWorldDebugLine(center - camera.Up * size, center + camera.Up * size, thickness, color);
        }

        static void DrawWorldDebugArrow(Vector3D start, Vector3D direction, double length, Color color)
        {
            if (direction.LengthSquared() <= 0.000001 || length <= 0.0001)
                return;

            direction.Normalize();
            Vector3D end = start + direction * length;
            DrawWorldDebugLine(start, end, 0.004f, color);

            Vector3D cameraUp = MyAPIGateway.Session != null && MyAPIGateway.Session.Camera != null
                ? MyAPIGateway.Session.Camera.WorldMatrix.Up
                : Vector3D.Up;
            Vector3D side = Vector3D.Cross(direction, cameraUp);
            if (side.LengthSquared() <= 0.000001)
                side = Vector3D.Cross(direction, Vector3D.Right);
            if (side.LengthSquared() <= 0.000001)
                return;
            side.Normalize();

            double head = Math.Min(length * 0.28, 0.035);
            DrawWorldDebugLine(end, end - direction * head + side * head * 0.45, 0.003f, color);
            DrawWorldDebugLine(end, end - direction * head - side * head * 0.45, 0.003f, color);
        }

        static void DrawWorldDebugLine(Vector3D start, Vector3D end, float thickness, Color color)
        {
            Vector3D axis = end - start;
            double length = axis.Length();
            if (length <= 0.0001)
                return;

            axis /= length;
            Vector3D center = (start + end) * 0.5;
            var drawColor = color.ToVector4();
            Vector3D up = MyAPIGateway.Session != null && MyAPIGateway.Session.Camera != null
                ? Vector3D.Cross(MyAPIGateway.Session.Camera.WorldMatrix.Forward, axis)
                : Vector3D.Up;
            if (up.LengthSquared() <= 0.000001)
                up = Vector3D.Up;
            up.Normalize();
            MyTransparentGeometry.AddBillboardOriented(CalibrationDebugSquareMaterial, drawColor, center, axis, up, (float)length, thickness);
        }
    }

    public partial class TouchScreenApiAdapter
    {
        const int ModelWalkMaxSteps = 90;
        const int ModelWalkRefineSteps = 6;
        const double ModelWalkStep = 0.02;
        const double ModelWalkCastDepth = 0.07;
        const double ModelWalkPlaneTolerance = 0.01;
        const double ModelWalkNormalDotThreshold = 0.995;

        bool _hasModelWalkDebugCorners;
        Vector3D _modelWalkTopLeft;
        Vector3D _modelWalkTopRight;
        Vector3D _modelWalkBottomRight;
        Vector3D _modelWalkBottomLeft;
        bool _modelWalkCaptureWasPressed;

        public bool TryGetDetectorBoundsDebugCorners(out Vector3D topLeft, out Vector3D topRight, out Vector3D bottomRight, out Vector3D bottomLeft)
        {
            topLeft = Vector3D.Zero;
            topRight = Vector3D.Zero;
            bottomRight = Vector3D.Zero;
            bottomLeft = Vector3D.Zero;

            if (_panel == null || !_hasDetectorBounds || _detectorBoundsSampleCount < DetectorBoundsMinSamples)
                return false;

            if (_hasDetectorPlaneBounds)
            {
                topLeft = _detectorPlaneCenter + _detectorPlaneRight * _detectorPlaneMinX + _detectorPlaneUp * _detectorPlaneMaxY;
                topRight = _detectorPlaneCenter + _detectorPlaneRight * _detectorPlaneMaxX + _detectorPlaneUp * _detectorPlaneMaxY;
                bottomRight = _detectorPlaneCenter + _detectorPlaneRight * _detectorPlaneMaxX + _detectorPlaneUp * _detectorPlaneMinY;
                bottomLeft = _detectorPlaneCenter + _detectorPlaneRight * _detectorPlaneMinX + _detectorPlaneUp * _detectorPlaneMinY;
                OffsetDetectorDebugCornersTowardCamera(ref topLeft, ref topRight, ref bottomRight, ref bottomLeft);
                return true;
            }

            MatrixD panelMatrix = _panel.WorldMatrix;
            double localZ = (_detectorMinZ + _detectorMaxZ) * 0.5;
            topLeft = Vector3D.Transform(new Vector3D(_detectorMinX, _detectorMaxY, localZ), panelMatrix);
            topRight = Vector3D.Transform(new Vector3D(_detectorMaxX, _detectorMaxY, localZ), panelMatrix);
            bottomRight = Vector3D.Transform(new Vector3D(_detectorMaxX, _detectorMinY, localZ), panelMatrix);
            bottomLeft = Vector3D.Transform(new Vector3D(_detectorMinX, _detectorMinY, localZ), panelMatrix);

            OffsetDetectorDebugCornersTowardCamera(ref topLeft, ref topRight, ref bottomRight, ref bottomLeft);
            return true;
        }

        static void OffsetDetectorDebugCornersTowardCamera(ref Vector3D topLeft, ref Vector3D topRight, ref Vector3D bottomRight, ref Vector3D bottomLeft)
        {
            if (MyAPIGateway.Session != null && MyAPIGateway.Session.Camera != null)
            {
                Vector3D center = (topLeft + topRight + bottomRight + bottomLeft) * 0.25;
                Vector3D toCamera = MyAPIGateway.Session.Camera.WorldMatrix.Translation - center;
                if (toCamera.LengthSquared() > 0.000001)
                {
                    toCamera.Normalize();
                    Vector3D offset = toCamera * 0.012;
                    topLeft += offset;
                    topRight += offset;
                    bottomRight += offset;
                    bottomLeft += offset;
                }
            }
        }

        public void CopyDetectorBoundsDebugSamples(List<Vector3D> samples)
        {
            if (samples == null)
                return;

            samples.Clear();
            for (int i = 0; i < _detectorDebugSamples.Count; i++)
                samples.Add(_detectorDebugSamples[i].Position);
        }

        public bool TryGetModelWalkDebugCorners(out Vector3D topLeft, out Vector3D topRight, out Vector3D bottomRight, out Vector3D bottomLeft)
        {
            topLeft = _modelWalkTopLeft;
            topRight = _modelWalkTopRight;
            bottomRight = _modelWalkBottomRight;
            bottomLeft = _modelWalkBottomLeft;
            return _hasModelWalkDebugCorners;
        }

        void UpdateModelWalkDebugCapture()
        {
            if (!GridSchematicsSession.IsPanelDiscoveryDebugEnabled)
            {
                _modelWalkCaptureWasPressed = false;
                _hasModelWalkDebugCorners = false;
                return;
            }

            if (MyAPIGateway.Input == null || MyAPIGateway.Gui == null)
                return;

            bool pressed = false;
            try
            {
                pressed = !MyAPIGateway.Gui.IsCursorVisible && MyAPIGateway.Input.IsLeftMousePressed();
            }
            catch
            {
                pressed = false;
            }

            if (pressed && !_modelWalkCaptureWasPressed)
                _hasModelWalkDebugCorners = TryBuildModelWalkDebugCorners();

            _modelWalkCaptureWasPressed = pressed;
        }

        bool TryBuildModelWalkDebugCorners()
        {
            _modelWalkTopLeft = Vector3D.Zero;
            _modelWalkTopRight = Vector3D.Zero;
            _modelWalkBottomRight = Vector3D.Zero;
            _modelWalkBottomLeft = Vector3D.Zero;

            Vector3D seed;
            Vector3D normal;
            if (!TryGetPanelModelAimHit(out seed, out normal))
                return false;

            if (normal.LengthSquared() <= 0.000001)
                return false;
            normal.Normalize();

            Vector3D axisA;
            Vector3D axisB;
            if (!TryBuildModelWalkAxes(normal, out axisA, out axisB))
                return false;

            double minA = -WalkSameModelPlaneDistance(seed, normal, -axisA);
            double maxA = WalkSameModelPlaneDistance(seed, normal, axisA);
            double minB = -WalkSameModelPlaneDistance(seed, normal, -axisB);
            double maxB = WalkSameModelPlaneDistance(seed, normal, axisB);
            if (maxA - minA < 0.08 || maxB - minB < 0.08)
                return false;

            Vector3D offset = normal * 0.01;
            if (MyAPIGateway.Session != null && MyAPIGateway.Session.Camera != null &&
                Vector3D.Dot(MyAPIGateway.Session.Camera.WorldMatrix.Translation - seed, offset) < 0.0)
            {
                offset = -offset;
            }

            _modelWalkTopLeft = seed + axisA * minA + axisB * maxB + offset;
            _modelWalkTopRight = seed + axisA * maxA + axisB * maxB + offset;
            _modelWalkBottomRight = seed + axisA * maxA + axisB * minB + offset;
            _modelWalkBottomLeft = seed + axisA * minA + axisB * minB + offset;
            return true;
        }

        bool TryBuildModelWalkAxes(Vector3D normal, out Vector3D axisA, out Vector3D axisB)
        {
            axisA = Vector3D.Zero;
            axisB = Vector3D.Zero;
            if (_panel == null)
                return false;

            var panelMatrix = _panel.WorldMatrix;
            Vector3D[] candidates = new Vector3D[]
            {
                panelMatrix.Right,
                panelMatrix.Up,
                panelMatrix.Forward
            };

            double bestLengthSq = 0.0;
            for (int i = 0; i < candidates.Length; i++)
            {
                Vector3D candidate = candidates[i];
                if (candidate.LengthSquared() <= 0.000001)
                    continue;
                candidate.Normalize();
                Vector3D projected = candidate - normal * Vector3D.Dot(candidate, normal);
                double lengthSq = projected.LengthSquared();
                if (lengthSq > bestLengthSq)
                {
                    bestLengthSq = lengthSq;
                    axisA = projected;
                }
            }

            if (bestLengthSq <= 0.000001)
                return false;

            axisA.Normalize();
            axisB = Vector3D.Cross(normal, axisA);
            if (axisB.LengthSquared() <= 0.000001)
                return false;
            axisB.Normalize();
            return true;
        }

        double WalkSameModelPlaneDistance(Vector3D seed, Vector3D normal, Vector3D direction)
        {
            if (direction.LengthSquared() <= 0.000001)
                return 0.0;
            direction.Normalize();

            double distance = 0.0;
            double rejectedDistance = 0.0;
            for (int step = 1; step <= ModelWalkMaxSteps; step++)
            {
                double nextDistance = step * ModelWalkStep;
                Vector3D target = seed + direction * nextDistance;
                if (!IsSameModelPlaneAt(target, seed, normal))
                {
                    rejectedDistance = nextDistance;
                    break;
                }

                distance = nextDistance;
            }

            if (rejectedDistance > distance)
                distance = RefineModelWalkEdgeDistance(seed, normal, direction, distance, rejectedDistance);

            return distance;
        }

        double RefineModelWalkEdgeDistance(Vector3D seed, Vector3D normal, Vector3D direction, double acceptedDistance, double rejectedDistance)
        {
            double low = acceptedDistance;
            double high = rejectedDistance;
            for (int i = 0; i < ModelWalkRefineSteps; i++)
            {
                double mid = (low + high) * 0.5;
                if (IsSameModelPlaneAt(seed + direction * mid, seed, normal))
                    low = mid;
                else
                    high = mid;
            }

            return low;
        }

        bool IsSameModelPlaneAt(Vector3D target, Vector3D seed, Vector3D normal)
        {
            Vector3D hitPosition;
            Vector3D hitNormal;
            if (!TryGetPanelVisibleModelPlaneProbeHit(target, normal, out hitPosition, out hitNormal))
                return false;

            if (hitNormal.LengthSquared() <= 0.000001)
                return false;
            hitNormal.Normalize();

            double normalDot = Math.Abs(Vector3D.Dot(hitNormal, normal));
            if (normalDot < ModelWalkNormalDotThreshold)
                return false;

            double planeDistance = Math.Abs(Vector3D.Dot(hitPosition - seed, normal));
            if (planeDistance > ModelWalkPlaneTolerance)
                return false;

            Vector3D lateral = hitPosition - target - normal * Vector3D.Dot(hitPosition - target, normal);
            return lateral.LengthSquared() <= ModelWalkStep * ModelWalkStep * 0.56;
        }

        bool TryGetPanelVisibleModelPlaneProbeHit(Vector3D target, Vector3D normal, out Vector3D hitPosition, out Vector3D hitNormal)
        {
            hitPosition = Vector3D.Zero;
            hitNormal = Vector3D.Zero;
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return false;

            Vector3D toCamera = MyAPIGateway.Session.Camera.WorldMatrix.Translation - target;
            double side = Vector3D.Dot(toCamera, normal);
            Vector3D probeNormal = side >= 0.0 ? normal : -normal;
            return TryGetPanelModelSegmentHit(target + probeNormal * ModelWalkCastDepth, target - probeNormal * ModelWalkCastDepth, out hitPosition, out hitNormal);
        }

        bool TryGetPanelModelAimHit(out Vector3D hitPosition, out Vector3D hitNormal)
        {
            hitPosition = Vector3D.Zero;
            hitNormal = Vector3D.Zero;
            if (MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return false;

            var camera = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D rayOrigin = camera.Translation;
            Vector3D rayDirection = camera.Forward;
            if (rayDirection.LengthSquared() <= 0.000001)
                return false;

            rayDirection.Normalize();
            return TryGetPanelModelSegmentHit(rayOrigin, rayOrigin + rayDirection * 12.0, out hitPosition, out hitNormal);
        }

        bool TryGetPanelModelSegmentHit(Vector3D rayStart, Vector3D rayEnd, out Vector3D hitPosition, out Vector3D hitNormal)
        {
            hitPosition = Vector3D.Zero;
            hitNormal = Vector3D.Zero;
            if (_panel == null)
                return false;

            try
            {
                var entity = _panel as IMyEntity;
                if (entity == null || entity.Model == null)
                    return false;

                var line = new LineD(rayStart, rayEnd);
                MyIntersectionResultLineTriangleEx? tri;
                if (!entity.GetIntersectionWithLine(ref line, out tri, IntersectionFlags.ALL_TRIANGLES) || !tri.HasValue)
                    return false;

                var hit = tri.Value;
                hitPosition = hit.IntersectionPointInWorldSpace;
                hitNormal = hit.NormalInWorldSpace;
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
