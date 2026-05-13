using Sandbox.ModAPI;
using System;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using IngameTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace GridSchematics
{
    public struct PanelCursorWorldDrawData
    {
        public Vector3D PlaneHit;
        public Vector3D Normal;
        public Vector3D AxisA;
        public Vector3D AxisB;
        public Vector2 RawSurfacePoint;
        public double LocalA;
        public double LocalB;
        public double MinA;
        public double MaxA;
        public double MinB;
        public double MaxB;
    }

    public enum PanelSurfaceCandidateQuality
    {
        None,
        Bad,
        Questionable,
        Good
    }

    public struct PanelSurfaceCandidate
    {
        public bool IsValid;
        public Vector3D Seed;
        public Vector3D Center;
        public Vector3D Normal;
        public Vector3D AxisA;
        public Vector3D AxisB;
        public double MinA;
        public double MaxA;
        public double MinB;
        public double MaxB;
        public double Width;
        public double Height;
        public double Score;
        public PanelSurfaceCandidateQuality Quality;
        public string StatusText;
        public string Source;
    }

    public partial class TouchScreenApiAdapter
    {
        const double PanelCursorMinimumSurfaceSize = 0.06;
        const double PanelCursorOutsideTolerance = 0.012;
        const double PanelCursorStoredOutsideTolerance = 0.0;
        const double PanelCursorVisualOverlapMargin = 0.006;
        const double PanelCursorRediscoverMargin = 0.035;
        const double PanelCursorPlaneTolerance = 0.012;
        const int PanelCursorRediscoverCooldownTicks = 20;
        const double PanelSurfaceCandidateGoodScore = 80.0;
        const double PanelSurfaceCandidateQuestionableScore = 45.0;

        bool _hasPanelCursorSurface;
        IMyEntity _panelCursorEntity;
        Vector3D _panelCursorSeed;
        Vector3D _panelCursorNormal;
        Vector3D _panelCursorAxisA;
        Vector3D _panelCursorAxisB;
        double _panelCursorMinA;
        double _panelCursorMaxA;
        double _panelCursorMinB;
        double _panelCursorMaxB;
        Vector3D _panelCursorLastPlaneHit;
        int _panelCursorRediscoverCooldown;
        bool _panelCursorSurfaceIsModelWalk;
        bool _hasStoredPanelCursorSurface;
        bool _hasLocalPanelCursorSurface;
        Vector3D _panelCursorSeedLocal;
        Vector3D _panelCursorNormalLocal;
        Vector3D _panelCursorAxisALocal;
        Vector3D _panelCursorAxisBLocal;
        PanelSurfaceCandidate _lastPanelSurfaceCandidate;

        struct PanelCursorAxisFrame
        {
            public Vector3D AxisA;
            public Vector3D AxisB;
            public string Source;
        }

        public PanelSurfaceCandidate LastPanelSurfaceCandidate
        {
            get { return _lastPanelSurfaceCandidate; }
        }

        public bool HasStoredPanelCursorSurface
        {
            get { return _hasStoredPanelCursorSurface; }
        }

        public void ResetPanelCursorSurfaceCalibration()
        {
            ClearPanelCursorSurface();
        }

        public bool TryGetStoredPanelCursorSurfaceCalibration(out Vector3D seed, out Vector3D normal, out Vector3D axisA, out Vector3D axisB, out double minA, out double maxA, out double minB, out double maxB)
        {
            if (!RefreshStoredPanelCursorSurfaceFromOwner())
            {
                seed = Vector3D.Zero;
                normal = Vector3D.Zero;
                axisA = Vector3D.Zero;
                axisB = Vector3D.Zero;
                minA = 0.0;
                maxA = 0.0;
                minB = 0.0;
                maxB = 0.0;
                return false;
            }

            seed = _panelCursorSeed;
            normal = _panelCursorNormal;
            axisA = _panelCursorAxisA;
            axisB = _panelCursorAxisB;
            minA = _panelCursorMinA;
            maxA = _panelCursorMaxA;
            minB = _panelCursorMinB;
            maxB = _panelCursorMaxB;
            return _hasStoredPanelCursorSurface && _hasPanelCursorSurface;
        }

        bool TryGetPolygonDiscoveredAimSurfacePoint(Vector3D rayOrigin, Vector3D rayDirection, out Vector2 surfacePoint, out bool visualCursorOnScreen)
        {
            surfacePoint = Vector2.Zero;
            visualCursorOnScreen = false;

            if (_panel == null || _surface == null)
                return false;

            if (_panelCursorRediscoverCooldown > 0)
                _panelCursorRediscoverCooldown--;

            return _hasStoredPanelCursorSurface &&
                TryProjectRayToPanelCursorSurface(rayOrigin, rayDirection, false, out surfacePoint, out visualCursorOnScreen);
        }

        bool IsMultiSurfaceOrUnsupportedPanel()
        {
            if ((_panel as IMyTextPanel) == null)
                return true;

            try
            {
                var provider = _panel as Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;
                return provider != null && provider.SurfaceCount > 1;
            }
            catch
            {
                return false;
            }
        }

        Vector3D GetPolygonDiscoveredLastPlaneHit()
        {
            return _panelCursorLastPlaneHit;
        }

        public int GetSurfaceIndex()
        {
            if (_panel == null || _surface == null)
                return -1;

            try
            {
                var provider = _panel as Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;
                if (provider == null)
                    return -1;

                for (int i = 0; i < provider.SurfaceCount; i++)
                {
                    if (object.ReferenceEquals(provider.GetSurface(i), _surface))
                        return i;
                }
            }
            catch
            {
            }

            return -1;
        }

        public string GetSurfaceDisplayLabel()
        {
            if (_surface == null)
                return "surface";

            try
            {
                string displayName = _surface.DisplayName ?? string.Empty;
                if (!string.IsNullOrEmpty(displayName))
                    return displayName;
            }
            catch
            {
            }

            try
            {
                string name = _surface.Name ?? string.Empty;
                if (!string.IsNullOrEmpty(name))
                    return name;
            }
            catch
            {
            }

            return "surface";
        }

        public bool TryRefreshPanelCursorWorldDrawData(out PanelCursorWorldDrawData data)
        {
            data = new PanelCursorWorldDrawData();
            if (_panel == null || _surface == null || MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return false;

            var camera = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D rayOrigin = camera.Translation;
            Vector3D rayDirection = camera.Forward;
            if (rayDirection.LengthSquared() <= 0.000001)
                return false;
            rayDirection.Normalize();

            if (!TryProjectRayToPanelCursorSurfaceForVisualDraw(rayOrigin, rayDirection, out data))
                return false;

            return true;
        }

        bool TryProjectRayToPanelCursorSurfaceForVisualDraw(Vector3D rayOrigin, Vector3D rayDirection, out PanelCursorWorldDrawData data)
        {
            data = new PanelCursorWorldDrawData();

            if (_hasStoredPanelCursorSurface && !RefreshStoredPanelCursorSurfaceFromOwner())
                return false;

            if (!_hasPanelCursorSurface || _surface == null || _panelCursorNormal.LengthSquared() <= 0.000001)
                return false;

            Vector3D normal = _panelCursorNormal;
            normal.Normalize();
            if (Vector3D.Dot(rayOrigin - _panelCursorSeed, normal) < 0.0)
                normal = -normal;

            double denom = Vector3D.Dot(rayDirection, normal);
            if (Math.Abs(denom) <= 0.000001)
                return false;

            double rayDistance = Vector3D.Dot(_panelCursorSeed - rayOrigin, normal) / denom;
            if (rayDistance <= 0.05 || rayDistance > 12.0)
                return false;

            Vector3D planeHit = rayOrigin + rayDirection * rayDistance;
            Vector3D delta = planeHit - _panelCursorSeed;
            double planeError = Math.Abs(Vector3D.Dot(delta, _panelCursorNormal));
            if (planeError > PanelCursorPlaneTolerance)
                return false;

            double localA = Vector3D.Dot(delta, _panelCursorAxisA);
            double localB = Vector3D.Dot(delta, _panelCursorAxisB);
            double width = _panelCursorMaxA - _panelCursorMinA;
            double height = _panelCursorMaxB - _panelCursorMinB;
            if (width <= 0.0001 || height <= 0.0001)
                return false;

            bool overlapsVisualBounds = localA >= _panelCursorMinA - PanelCursorVisualOverlapMargin &&
                localA <= _panelCursorMaxA + PanelCursorVisualOverlapMargin &&
                localB >= _panelCursorMinB - PanelCursorVisualOverlapMargin &&
                localB <= _panelCursorMaxB + PanelCursorVisualOverlapMargin;
            if (!overlapsVisualBounds)
                return false;

            double u = (localA - _panelCursorMinA) / width;
            double v = 1.0 - (localB - _panelCursorMinB) / height;

            data.PlaneHit = planeHit;
            data.Normal = _panelCursorNormal;
            data.AxisA = _panelCursorAxisA;
            data.AxisB = _panelCursorAxisB;
            data.RawSurfacePoint = new Vector2((float)(u * _surface.SurfaceSize.X), (float)(v * _surface.SurfaceSize.Y));
            data.LocalA = localA;
            data.LocalB = localB;
            data.MinA = _panelCursorMinA;
            data.MaxA = _panelCursorMaxA;
            data.MinB = _panelCursorMinB;
            data.MaxB = _panelCursorMaxB;
            return true;
        }

        bool TryBuildPanelCursorSurfaceFromAim(Vector3D rayOrigin, Vector3D rayDirection)
        {
            ClearPanelCursorSurface();

            Vector3D seed;
            Vector3D normal;
            if (!TryGetPanelModelSegmentHit(rayOrigin, rayOrigin + rayDirection * 12.0, out seed, out normal))
                return false;

            if (normal.LengthSquared() <= 0.000001)
                return false;
            normal.Normalize();
            if (Vector3D.Dot(rayOrigin - seed, normal) < 0.0)
                normal = -normal;

            return TryBuildPanelCursorSurfaceFromModelHit(seed, normal);
        }

        bool TryBuildPanelCursorSurfaceFromModelHit(Vector3D seed, Vector3D normal)
        {
            PanelSurfaceCandidate candidate;
            if (!TryBuildPanelCursorSurfaceCandidateFromModelHit(seed, normal, out candidate))
                return false;

            ScorePanelCursorSurfaceCandidate(ref candidate);
            _lastPanelSurfaceCandidate = candidate;
            if (candidate.Quality == PanelSurfaceCandidateQuality.Bad)
                return false;

            CommitPanelCursorSurfaceCandidate(candidate, true);
            return true;
        }

        bool TryBuildPanelCursorSurfaceCandidateFromModelHit(Vector3D seed, Vector3D normal, out PanelSurfaceCandidate candidate)
        {
            candidate = new PanelSurfaceCandidate
            {
                IsValid = false,
                Quality = PanelSurfaceCandidateQuality.None,
                StatusText = "No candidate",
                Source = "ModelWalk"
            };

            if (normal.LengthSquared() <= 0.000001)
                return false;
            normal.Normalize();

            Vector3D axisA;
            Vector3D axisB;
            if (!TryBuildPanelCursorAxes(normal, out axisA, out axisB))
                return false;

            if (!TryBuildPanelCursorSurfaceCandidateFromAxisFrame(seed, normal, new PanelCursorAxisFrame
            {
                AxisA = axisA,
                AxisB = axisB,
                Source = "ModelWalk"
            }, out candidate))
            {
                return false;
            }

            return true;
        }

        bool TryBuildPanelCursorSurfaceCandidateFromAxisFrame(Vector3D seed, Vector3D normal, PanelCursorAxisFrame frame, out PanelSurfaceCandidate candidate)
        {
            candidate = new PanelSurfaceCandidate
            {
                IsValid = false,
                Quality = PanelSurfaceCandidateQuality.None,
                StatusText = "No candidate",
                Source = frame.Source
            };

            Vector3D axisA = frame.AxisA;
            Vector3D axisB = frame.AxisB;
            if (axisA.LengthSquared() <= 0.000001 || axisB.LengthSquared() <= 0.000001)
                return false;
            axisA.Normalize();
            axisB.Normalize();

            double minA = -WalkSameModelPlaneDistance(seed, normal, -axisA);
            double maxA = WalkSameModelPlaneDistance(seed, normal, axisA);
            double minB = -WalkSameModelPlaneDistance(seed, normal, -axisB);
            double maxB = WalkSameModelPlaneDistance(seed, normal, axisB);

            if (maxA - minA < PanelCursorMinimumSurfaceSize || maxB - minB < PanelCursorMinimumSurfaceSize)
                return false;

            candidate.IsValid = true;
            candidate.Seed = seed;
            candidate.Center = seed + axisA * ((minA + maxA) * 0.5) + axisB * ((minB + maxB) * 0.5);
            candidate.Normal = normal;
            candidate.AxisA = axisA;
            candidate.AxisB = axisB;
            candidate.MinA = minA;
            candidate.MaxA = maxA;
            candidate.MinB = minB;
            candidate.MaxB = maxB;
            candidate.Width = maxA - minA;
            candidate.Height = maxB - minB;
            return true;
        }

        void ScorePanelCursorSurfaceCandidate(ref PanelSurfaceCandidate candidate)
        {
            if (!candidate.IsValid)
            {
                candidate.Score = 0.0;
                candidate.Quality = PanelSurfaceCandidateQuality.Bad;
                candidate.StatusText = "No valid surface";
                return;
            }

            double score = 100.0;
            string issue = string.Empty;

            if (candidate.Width < PanelCursorMinimumSurfaceSize || candidate.Height < PanelCursorMinimumSurfaceSize)
            {
                candidate.Score = 0.0;
                candidate.Quality = PanelSurfaceCandidateQuality.Bad;
                candidate.StatusText = "Surface too small";
                return;
            }

            if (candidate.Normal.LengthSquared() <= 0.000001 ||
                candidate.AxisA.LengthSquared() <= 0.000001 ||
                candidate.AxisB.LengthSquared() <= 0.000001)
            {
                candidate.Score = 0.0;
                candidate.Quality = PanelSurfaceCandidateQuality.Bad;
                candidate.StatusText = "Invalid surface axes";
                return;
            }

            Vector3D normal = candidate.Normal;
            Vector3D axisA = candidate.AxisA;
            Vector3D axisB = candidate.AxisB;
            normal.Normalize();
            axisA.Normalize();
            axisB.Normalize();

            double axisDot = Math.Abs(Vector3D.Dot(axisA, axisB));
            if (axisDot > 0.18)
            {
                score -= 55.0;
                issue = "Axes skewed";
            }
            else if (axisDot > 0.08)
            {
                score -= 20.0;
                issue = "Axes slightly skewed";
            }

            double flatnessA = Math.Abs(Vector3D.Dot(axisA, normal));
            double flatnessB = Math.Abs(Vector3D.Dot(axisB, normal));
            if (flatnessA > 0.04 || flatnessB > 0.04)
            {
                score -= 35.0;
                if (string.IsNullOrEmpty(issue))
                    issue = "Axes not planar";
            }

            double skinny = candidate.Width > candidate.Height ? candidate.Width / candidate.Height : candidate.Height / candidate.Width;
            if (skinny > 12.0)
            {
                score -= 45.0;
                if (string.IsNullOrEmpty(issue))
                    issue = "Bounds too skinny";
            }
            else if (skinny > 6.0)
            {
                score -= 20.0;
                if (string.IsNullOrEmpty(issue))
                    issue = "Bounds very narrow";
            }

            if (_surface != null && _surface.SurfaceSize.X > 0f && _surface.SurfaceSize.Y > 0f)
            {
                double discoveredAspect = candidate.Width / candidate.Height;
                double surfaceAspect = _surface.SurfaceSize.X / _surface.SurfaceSize.Y;
                double aspectRatio = discoveredAspect > surfaceAspect ? discoveredAspect / surfaceAspect : surfaceAspect / discoveredAspect;
                if (aspectRatio > 3.0)
                {
                    score -= 35.0;
                    if (string.IsNullOrEmpty(issue))
                        issue = "Aspect mismatch";
                }
                else if (aspectRatio > 1.8)
                {
                    score -= 15.0;
                    if (string.IsNullOrEmpty(issue))
                        issue = "Aspect differs";
                }
            }

            if (MyAPIGateway.Session != null && MyAPIGateway.Session.Camera != null)
            {
                var camera = MyAPIGateway.Session.Camera.WorldMatrix;
                Vector3D toCamera = camera.Translation - candidate.Center;
                if (toCamera.LengthSquared() > 0.000001)
                {
                    toCamera.Normalize();
                    double cameraFacing = Vector3D.Dot(toCamera, normal);
                    if (cameraFacing < 0.04)
                    {
                        score -= 30.0;
                        if (string.IsNullOrEmpty(issue))
                            issue = "Surface faces away";
                    }

                    Vector3D cameraForward = camera.Forward;
                    if (cameraForward.LengthSquared() > 0.000001)
                    {
                        cameraForward.Normalize();
                        double viewAngle = -Vector3D.Dot(cameraForward, normal);
                        if (viewAngle < 0.08)
                        {
                            score -= 25.0;
                            if (string.IsNullOrEmpty(issue))
                                issue = "View angle too shallow";
                        }
                    }
                }
            }

            if (score < 0.0)
                score = 0.0;

            candidate.Score = score;
            if (score >= PanelSurfaceCandidateGoodScore)
            {
                candidate.Quality = PanelSurfaceCandidateQuality.Good;
                candidate.StatusText = "Surface candidate good";
            }
            else if (score >= PanelSurfaceCandidateQuestionableScore)
            {
                candidate.Quality = PanelSurfaceCandidateQuality.Questionable;
                candidate.StatusText = string.IsNullOrEmpty(issue) ? "Surface candidate needs review" : issue;
            }
            else
            {
                candidate.Quality = PanelSurfaceCandidateQuality.Bad;
                candidate.StatusText = string.IsNullOrEmpty(issue) ? "Surface candidate rejected" : issue;
            }
        }

        void CommitPanelCursorSurfaceCandidate(PanelSurfaceCandidate candidate, bool isModelWalk)
        {
            _hasLocalPanelCursorSurface = false;
            _panelCursorSeedLocal = Vector3D.Zero;
            _panelCursorNormalLocal = Vector3D.Zero;
            _panelCursorAxisALocal = Vector3D.Zero;
            _panelCursorAxisBLocal = Vector3D.Zero;
            _panelCursorEntity = _panel as IMyEntity;
            _panelCursorSeed = candidate.Seed;
            _panelCursorNormal = candidate.Normal;
            _panelCursorAxisA = candidate.AxisA;
            _panelCursorAxisB = candidate.AxisB;
            _panelCursorMinA = candidate.MinA;
            _panelCursorMaxA = candidate.MaxA;
            _panelCursorMinB = candidate.MinB;
            _panelCursorMaxB = candidate.MaxB;
            _panelCursorLastPlaneHit = candidate.Seed;
            _hasPanelCursorSurface = true;
            _panelCursorSurfaceIsModelWalk = isModelWalk;
            _hasStoredPanelCursorSurface = false;
        }

        public void SetManualPanelCursorSurface(Vector3D seed, Vector3D normal, Vector3D axisA, Vector3D axisB, double minA, double maxA, double minB, double maxB)
        {
            if (normal.LengthSquared() <= 0.000001 || axisA.LengthSquared() <= 0.000001 || axisB.LengthSquared() <= 0.000001)
                return;

            normal.Normalize();
            axisA.Normalize();
            axisB.Normalize();

            IMyEntity entity = _panel as IMyEntity;
            if (entity != null)
            {
                MatrixD inverseBasis = MatrixD.Invert(entity.WorldMatrix);
                MatrixD inverseBasisNormal = MatrixD.Transpose(entity.WorldMatrix);
                SetManualPanelCursorSurfaceLocal(
                    Vector3D.Transform(seed, inverseBasis),
                    Vector3D.TransformNormal(normal, inverseBasisNormal),
                    Vector3D.TransformNormal(axisA, inverseBasisNormal),
                    Vector3D.TransformNormal(axisB, inverseBasisNormal),
                    minA,
                    maxA,
                    minB,
                    maxB);
                return;
            }

            _hasLocalPanelCursorSurface = false;
            _panelCursorSeedLocal = Vector3D.Zero;
            _panelCursorNormalLocal = Vector3D.Zero;
            _panelCursorAxisALocal = Vector3D.Zero;
            _panelCursorAxisBLocal = Vector3D.Zero;
            _panelCursorEntity = _panel as IMyEntity;
            _panelCursorSeed = seed;
            _panelCursorNormal = normal;
            _panelCursorAxisA = axisA;
            _panelCursorAxisB = axisB;
            _panelCursorMinA = minA;
            _panelCursorMaxA = maxA;
            _panelCursorMinB = minB;
            _panelCursorMaxB = maxB;
            _panelCursorLastPlaneHit = seed;
            _hasPanelCursorSurface = true;
            _panelCursorSurfaceIsModelWalk = true;
            _hasStoredPanelCursorSurface = true;
            _lastPanelSurfaceCandidate = new PanelSurfaceCandidate
            {
                IsValid = true,
                Seed = seed,
                Center = seed + axisA * ((minA + maxA) * 0.5) + axisB * ((minB + maxB) * 0.5),
                Normal = normal,
                AxisA = axisA,
                AxisB = axisB,
                MinA = minA,
                MaxA = maxA,
                MinB = minB,
                MaxB = maxB,
                Width = maxA - minA,
                Height = maxB - minB,
                Score = 100.0,
                Quality = PanelSurfaceCandidateQuality.Good,
                StatusText = "Manual calibration applied",
                Source = "Manual"
            };
        }

        public void SetManualPanelCursorSurfaceLocal(Vector3D seedLocal, Vector3D normalLocal, Vector3D axisALocal, Vector3D axisBLocal, double minA, double maxA, double minB, double maxB)
        {
            if (normalLocal.LengthSquared() <= 0.000001 || axisALocal.LengthSquared() <= 0.000001 || axisBLocal.LengthSquared() <= 0.000001)
                return;

            normalLocal.Normalize();
            axisALocal.Normalize();
            axisBLocal.Normalize();

            _panelCursorEntity = _panel as IMyEntity;
            _panelCursorSeedLocal = seedLocal;
            _panelCursorNormalLocal = normalLocal;
            _panelCursorAxisALocal = axisALocal;
            _panelCursorAxisBLocal = axisBLocal;
            _panelCursorMinA = minA;
            _panelCursorMaxA = maxA;
            _panelCursorMinB = minB;
            _panelCursorMaxB = maxB;
            _panelCursorSurfaceIsModelWalk = true;
            _hasStoredPanelCursorSurface = true;
            _hasLocalPanelCursorSurface = true;
            if (!RefreshStoredPanelCursorSurfaceFromOwner())
                _hasPanelCursorSurface = false;
        }

        bool RefreshStoredPanelCursorSurfaceFromOwner()
        {
            if (!_hasStoredPanelCursorSurface)
                return _hasPanelCursorSurface;

            if (!_hasLocalPanelCursorSurface)
                return _hasPanelCursorSurface;

            IMyEntity entity = _panelCursorEntity != null ? _panelCursorEntity : _panel as IMyEntity;
            if (entity == null)
                return false;

            double lastA = 0.0;
            double lastB = 0.0;
            bool hasLastLocalHit = _hasPanelCursorSurface &&
                _panelCursorAxisA.LengthSquared() > 0.000001 &&
                _panelCursorAxisB.LengthSquared() > 0.000001;
            if (hasLastLocalHit)
            {
                Vector3D previousDelta = _panelCursorLastPlaneHit - _panelCursorSeed;
                lastA = Vector3D.Dot(previousDelta, _panelCursorAxisA);
                lastB = Vector3D.Dot(previousDelta, _panelCursorAxisB);
            }

            MatrixD world = entity.WorldMatrix;
            Vector3D seed = Vector3D.Transform(_panelCursorSeedLocal, world);
            Vector3D normal = Vector3D.TransformNormal(_panelCursorNormalLocal, world);
            Vector3D axisA = Vector3D.TransformNormal(_panelCursorAxisALocal, world);
            Vector3D axisB = Vector3D.TransformNormal(_panelCursorAxisBLocal, world);
            if (normal.LengthSquared() <= 0.000001 || axisA.LengthSquared() <= 0.000001 || axisB.LengthSquared() <= 0.000001)
                return false;

            normal.Normalize();
            axisA.Normalize();
            axisB.Normalize();

            _panelCursorEntity = entity;
            _panelCursorSeed = seed;
            _panelCursorNormal = normal;
            _panelCursorAxisA = axisA;
            _panelCursorAxisB = axisB;
            _panelCursorLastPlaneHit = hasLastLocalHit ? seed + axisA * lastA + axisB * lastB : seed;
            _hasPanelCursorSurface = true;
            _lastPanelSurfaceCandidate = new PanelSurfaceCandidate
            {
                IsValid = true,
                Seed = seed,
                Center = seed + axisA * ((_panelCursorMinA + _panelCursorMaxA) * 0.5) + axisB * ((_panelCursorMinB + _panelCursorMaxB) * 0.5),
                Normal = normal,
                AxisA = axisA,
                AxisB = axisB,
                MinA = _panelCursorMinA,
                MaxA = _panelCursorMaxA,
                MinB = _panelCursorMinB,
                MaxB = _panelCursorMaxB,
                Width = _panelCursorMaxA - _panelCursorMinA,
                Height = _panelCursorMaxB - _panelCursorMinB,
                Score = 100.0,
                Quality = PanelSurfaceCandidateQuality.Good,
                StatusText = "Manual calibration applied",
                Source = "Manual"
            };
            return true;
        }

        void ClearPanelCursorSurface()
        {
            _hasPanelCursorSurface = false;
            _panelCursorSurfaceIsModelWalk = false;
            _hasStoredPanelCursorSurface = false;
            _hasLocalPanelCursorSurface = false;
            _panelCursorEntity = null;
            _panelCursorSeed = Vector3D.Zero;
            _panelCursorNormal = Vector3D.Zero;
            _panelCursorAxisA = Vector3D.Zero;
            _panelCursorAxisB = Vector3D.Zero;
            _panelCursorSeedLocal = Vector3D.Zero;
            _panelCursorNormalLocal = Vector3D.Zero;
            _panelCursorAxisALocal = Vector3D.Zero;
            _panelCursorAxisBLocal = Vector3D.Zero;
            _panelCursorMinA = 0.0;
            _panelCursorMaxA = 0.0;
            _panelCursorMinB = 0.0;
            _panelCursorMaxB = 0.0;
            _panelCursorLastPlaneHit = Vector3D.Zero;
            _lastPanelSurfaceCandidate = new PanelSurfaceCandidate
            {
                IsValid = false,
                Quality = PanelSurfaceCandidateQuality.None,
                StatusText = "No candidate",
                Source = string.Empty
            };
        }

        bool TryBuildPanelCursorAxes(Vector3D normal, out Vector3D axisA, out Vector3D axisB)
        {
            axisA = Vector3D.Zero;
            axisB = Vector3D.Zero;
            if (_panel == null)
                return false;

            var matrix = _panel.WorldMatrix;
            Vector3D right = matrix.Right;
            Vector3D up = matrix.Up;
            if (right.LengthSquared() <= 0.000001 || up.LengthSquared() <= 0.000001)
                return false;
            right.Normalize();
            up.Normalize();

            axisA = right - normal * Vector3D.Dot(right, normal);
            if (axisA.LengthSquared() <= 0.000001)
                axisA = up - normal * Vector3D.Dot(up, normal);
            if (axisA.LengthSquared() <= 0.000001)
                return false;
            axisA.Normalize();
            if (Vector3D.Dot(axisA, right) < 0.0)
                axisA = -axisA;

            axisB = up - normal * Vector3D.Dot(up, normal);
            if (axisB.LengthSquared() <= 0.000001)
                axisB = Vector3D.Cross(normal, axisA);
            if (axisB.LengthSquared() <= 0.000001)
                return false;
            axisB.Normalize();
            if (Vector3D.Dot(axisB, up) < 0.0)
                axisB = -axisB;

            return Math.Abs(Vector3D.Dot(axisA, axisB)) < 0.08;
        }

        bool TryProjectRayToPanelCursorSurface(Vector3D rayOrigin, Vector3D rayDirection, bool allowRediscoverOutside, out Vector2 surfacePoint, out bool visualCursorOnScreen)
        {
            surfacePoint = Vector2.Zero;
            visualCursorOnScreen = false;

            if (_hasStoredPanelCursorSurface && !RefreshStoredPanelCursorSurfaceFromOwner())
                return false;

            if (!_hasPanelCursorSurface || _surface == null || _panelCursorNormal.LengthSquared() <= 0.000001)
                return false;

            Vector3D normal = _panelCursorNormal;
            normal.Normalize();
            if (Vector3D.Dot(rayOrigin - _panelCursorSeed, normal) < 0.0)
                normal = -normal;

            double denom = Vector3D.Dot(rayDirection, normal);
            if (Math.Abs(denom) <= 0.000001)
                return false;

            double rayDistance = Vector3D.Dot(_panelCursorSeed - rayOrigin, normal) / denom;
            if (rayDistance <= 0.05 || rayDistance > 12.0)
                return false;

            Vector3D planeHit = rayOrigin + rayDirection * rayDistance;
            Vector3D delta = planeHit - _panelCursorSeed;
            double planeError = Math.Abs(Vector3D.Dot(delta, _panelCursorNormal));
            if (planeError > PanelCursorPlaneTolerance)
                return false;

            double localA = Vector3D.Dot(delta, _panelCursorAxisA);
            double localB = Vector3D.Dot(delta, _panelCursorAxisB);
            double width = _panelCursorMaxA - _panelCursorMinA;
            double height = _panelCursorMaxB - _panelCursorMinB;
            if (width <= 0.0001 || height <= 0.0001)
                return false;

            double outsideTolerance = _hasStoredPanelCursorSurface ? PanelCursorStoredOutsideTolerance : PanelCursorOutsideTolerance;
            bool inside = localA >= _panelCursorMinA - outsideTolerance &&
                localA <= _panelCursorMaxA + outsideTolerance &&
                localB >= _panelCursorMinB - outsideTolerance &&
                localB <= _panelCursorMaxB + outsideTolerance;
            if (!inside)
            {
                if (allowRediscoverOutside && !_hasStoredPanelCursorSurface && IsOutsidePanelCursorRediscoverMargin(localA, localB))
                    ClearPanelCursorSurface();
                return false;
            }

            double u = (localA - _panelCursorMinA) / width;
            double v = 1.0 - (localB - _panelCursorMinB) / height;
            u = Clamp01(u);
            v = Clamp01(v);

            surfacePoint = new Vector2((float)(u * _surface.SurfaceSize.X), (float)(v * _surface.SurfaceSize.Y));
            visualCursorOnScreen = true;
            _panelCursorLastPlaneHit = planeHit;
            return true;
        }

        bool IsOutsidePanelCursorRediscoverMargin(double localA, double localB)
        {
            return localA < _panelCursorMinA - PanelCursorRediscoverMargin ||
                localA > _panelCursorMaxA + PanelCursorRediscoverMargin ||
                localB < _panelCursorMinB - PanelCursorRediscoverMargin ||
                localB > _panelCursorMaxB + PanelCursorRediscoverMargin;
        }

        public bool TryGetPanelCursorSurfaceCorners(out Vector3D topLeft, out Vector3D topRight, out Vector3D bottomRight, out Vector3D bottomLeft)
        {
            topLeft = Vector3D.Zero;
            topRight = Vector3D.Zero;
            bottomRight = Vector3D.Zero;
            bottomLeft = Vector3D.Zero;

            if (_hasStoredPanelCursorSurface && !RefreshStoredPanelCursorSurfaceFromOwner())
                return false;

            if (!_hasPanelCursorSurface)
                return false;

            topLeft = BuildPanelCursorSurfacePoint(_panelCursorMinA, _panelCursorMaxB);
            topRight = BuildPanelCursorSurfacePoint(_panelCursorMaxA, _panelCursorMaxB);
            bottomRight = BuildPanelCursorSurfacePoint(_panelCursorMaxA, _panelCursorMinB);
            bottomLeft = BuildPanelCursorSurfacePoint(_panelCursorMinA, _panelCursorMinB);
            return true;
        }

        public bool TryGetLastPanelSurfaceCandidateCorners(out Vector3D topLeft, out Vector3D topRight, out Vector3D bottomRight, out Vector3D bottomLeft)
        {
            topLeft = Vector3D.Zero;
            topRight = Vector3D.Zero;
            bottomRight = Vector3D.Zero;
            bottomLeft = Vector3D.Zero;

            if (_hasStoredPanelCursorSurface && !RefreshStoredPanelCursorSurfaceFromOwner())
                return false;

            PanelSurfaceCandidate candidate = _lastPanelSurfaceCandidate;
            if (!candidate.IsValid)
                return false;

            Vector3D normal = candidate.Normal;
            if (normal.LengthSquared() > 0.000001)
                normal.Normalize();

            Vector3D offset = normal * 0.014;
            topLeft = candidate.Seed + candidate.AxisA * candidate.MinA + candidate.AxisB * candidate.MaxB + offset;
            topRight = candidate.Seed + candidate.AxisA * candidate.MaxA + candidate.AxisB * candidate.MaxB + offset;
            bottomRight = candidate.Seed + candidate.AxisA * candidate.MaxA + candidate.AxisB * candidate.MinB + offset;
            bottomLeft = candidate.Seed + candidate.AxisA * candidate.MinA + candidate.AxisB * candidate.MinB + offset;
            return true;
        }

        public bool TryGetPanelCursorLastPlaneHit(out Vector3D planeHit)
        {
            if (_hasStoredPanelCursorSurface)
                RefreshStoredPanelCursorSurfaceFromOwner();

            planeHit = _panelCursorLastPlaneHit;
            return _hasPanelCursorSurface || _lastPanelSurfaceCandidate.IsValid;
        }

        public bool TryGetCurrentModelAimDebugHit(out Vector3D hitPosition, out Vector3D hitNormal)
        {
            hitPosition = Vector3D.Zero;
            hitNormal = Vector3D.Zero;

            if (_panel == null || MyAPIGateway.Session == null || MyAPIGateway.Session.Camera == null)
                return false;

            var camera = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D rayOrigin = camera.Translation;
            Vector3D rayDirection = camera.Forward;
            if (rayDirection.LengthSquared() <= 0.000001)
                return false;
            rayDirection.Normalize();

            return TryGetPanelModelSegmentHit(rayOrigin, rayOrigin + rayDirection * 12.0, out hitPosition, out hitNormal);
        }

        public bool TryEnsurePanelCursorSurface()
        {
            if (_hasPanelCursorSurface)
                return true;

            if (_panel == null || _surface == null)
                return false;

            try
            {
                Vector3D topLeft;
                Vector3D topRight;
                Vector3D bottomRight;
                Vector3D bottomLeft;
                if (!TryGetStablePhysicalDisplayCorners(out topLeft, out topRight, out bottomRight, out bottomLeft))
                    return false;

                Vector3D right = topRight - topLeft;
                Vector3D up = topLeft - bottomLeft;
                double faceWidth = right.Length();
                double faceHeight = up.Length();
                if (faceWidth <= 0.0001 || faceHeight <= 0.0001)
                    return false;
                right.Normalize();
                up.Normalize();
                Vector3D normal = Vector3D.Cross(right, topLeft - bottomLeft);
                if (normal.LengthSquared() <= 0.000001)
                    return false;
                normal.Normalize();
                Vector3D center = (topLeft + topRight + bottomRight + bottomLeft) * 0.25;

                _panelCursorEntity = _panel as IMyEntity;
                _panelCursorSeed = center;
                _panelCursorNormal = normal;
                _panelCursorAxisA = right;
                _panelCursorAxisB = up;
                _panelCursorMinA = -faceWidth * 0.5;
                _panelCursorMaxA = faceWidth * 0.5;
                _panelCursorMinB = -faceHeight * 0.5;
                _panelCursorMaxB = faceHeight * 0.5;
                _panelCursorLastPlaneHit = center;
                _hasPanelCursorSurface = true;
                _panelCursorSurfaceIsModelWalk = false;
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryRefinePanelCursorSurfaceForRawPoint(Vector2 rawSurfacePoint)
        {
            if (_panelCursorSurfaceIsModelWalk)
                return true;

            Vector3D topLeft;
            Vector3D topRight;
            Vector3D bottomRight;
            Vector3D bottomLeft;
            if (!TryGetStablePhysicalDisplayCorners(out topLeft, out topRight, out bottomRight, out bottomLeft))
                return false;

            Vector2 surfaceSize = _surface != null ? _surface.SurfaceSize : Vector2.Zero;
            if (surfaceSize.X <= 0f || surfaceSize.Y <= 0f)
                return false;

            double u = Clamp01(rawSurfacePoint.X / surfaceSize.X);
            double v = Clamp01(rawSurfacePoint.Y / surfaceSize.Y);
            const double EdgeProbeInset = 0.08;
            if (u < EdgeProbeInset) u = EdgeProbeInset;
            if (u > 1.0 - EdgeProbeInset) u = 1.0 - EdgeProbeInset;
            if (v < EdgeProbeInset) v = EdgeProbeInset;
            if (v > 1.0 - EdgeProbeInset) v = 1.0 - EdgeProbeInset;

            Vector3D right = topRight - topLeft;
            Vector3D down = bottomLeft - topLeft;
            Vector3D roughPoint = topLeft + right * u + down * v;
            Vector3D normal = Vector3D.Cross(right, down);
            if (normal.LengthSquared() <= 0.000001)
                return false;
            normal.Normalize();

            if (TryBuildPanelCursorSurfaceFromProbe(roughPoint, normal))
                return true;

            Vector3D center = (topLeft + topRight + bottomRight + bottomLeft) * 0.25;
            return TryBuildPanelCursorSurfaceFromProbe(center, normal);
        }

        bool TryBuildPanelCursorSurfaceFromProbe(Vector3D roughPoint, Vector3D normal)
        {
            if (normal.LengthSquared() <= 0.000001)
                return false;
            normal.Normalize();

            const double probeDepth = 0.18;
            Vector3D hitPosition;
            Vector3D hitNormal;
            if (!TryGetPanelModelSegmentHit(roughPoint + normal * probeDepth, roughPoint - normal * probeDepth, out hitPosition, out hitNormal) &&
                !TryGetPanelModelSegmentHit(roughPoint - normal * probeDepth, roughPoint + normal * probeDepth, out hitPosition, out hitNormal))
            {
                return false;
            }

            if (hitNormal.LengthSquared() <= 0.000001)
                return false;
            hitNormal.Normalize();
            if (Vector3D.Dot(hitNormal, normal) < 0.0)
                hitNormal = -hitNormal;

            return TryBuildPanelCursorSurfaceFromModelHit(hitPosition, hitNormal);
        }

        public bool TryGetStablePhysicalDisplayCorners(out Vector3D topLeft, out Vector3D topRight, out Vector3D bottomRight, out Vector3D bottomLeft)
        {
            topLeft = Vector3D.Zero;
            topRight = Vector3D.Zero;
            bottomRight = Vector3D.Zero;
            bottomLeft = Vector3D.Zero;

            if (_panel == null || _surface == null)
                return false;

            MatrixD panelMatrix = _panel.WorldMatrix;
            BoundingBoxD box = _panel.LocalAABB;
            Vector3D right = panelMatrix.Right;
            Vector3D up = panelMatrix.Up;
            Vector3D localCenter = (box.Min + box.Max) * 0.5;
            double faceWidth = Math.Max(0.01, Math.Abs(box.Max.X - box.Min.X));
            double faceHeight = Math.Max(0.01, Math.Abs(box.Max.Y - box.Min.Y));
            Vector3D forwardFaceCenter = panelMatrix.Translation + right * localCenter.X + up * localCenter.Y + panelMatrix.Backward * box.Min.Z;
            Vector3D backwardFaceCenter = panelMatrix.Translation + right * localCenter.X + up * localCenter.Y + panelMatrix.Backward * box.Max.Z;

            Vector3D cameraPosition = Vector3D.Zero;
            bool hasCamera = MyAPIGateway.Session != null && MyAPIGateway.Session.Camera != null;
            if (hasCamera)
                cameraPosition = MyAPIGateway.Session.Camera.WorldMatrix.Translation;

            if (hasCamera)
            {
                double forwardFacing = Vector3D.Dot(cameraPosition - forwardFaceCenter, panelMatrix.Forward);
                double backwardFacing = Vector3D.Dot(cameraPosition - backwardFaceCenter, panelMatrix.Backward);
                if (backwardFacing > forwardFacing)
                    return TryBuildDiscoveredDisplayCorners(backwardFaceCenter, panelMatrix.Backward, -right, up, faceWidth, faceHeight, out topLeft, out topRight, out bottomRight, out bottomLeft);
            }

            return TryBuildDiscoveredDisplayCorners(forwardFaceCenter, panelMatrix.Forward, right, up, faceWidth, faceHeight, out topLeft, out topRight, out bottomRight, out bottomLeft);
        }

        public bool TryGetPanelCursorWorldDrawData(out PanelCursorWorldDrawData data)
        {
            data = new PanelCursorWorldDrawData();
            if (_hasStoredPanelCursorSurface && !RefreshStoredPanelCursorSurfaceFromOwner())
                return false;

            if (!_hasPanelCursorSurface || !IsAimCursorActive || !IsVisualCursorOnScreen || !IsCursorOnScreen)
                return false;

            return TryBuildPanelCursorWorldDrawData(out data);
        }

        bool TryBuildPanelCursorWorldDrawData(out PanelCursorWorldDrawData data)
        {
            data = new PanelCursorWorldDrawData();
            if (_hasStoredPanelCursorSurface && !RefreshStoredPanelCursorSurfaceFromOwner())
                return false;

            if (!_hasPanelCursorSurface)
                return false;

            Vector3D delta = _panelCursorLastPlaneHit - _panelCursorSeed;
            data.PlaneHit = _panelCursorLastPlaneHit;
            data.Normal = _panelCursorNormal;
            data.AxisA = _panelCursorAxisA;
            data.AxisB = _panelCursorAxisB;
            data.RawSurfacePoint = RawCursorPosition;
            data.LocalA = Vector3D.Dot(delta, _panelCursorAxisA);
            data.LocalB = Vector3D.Dot(delta, _panelCursorAxisB);
            data.MinA = _panelCursorMinA;
            data.MaxA = _panelCursorMaxA;
            data.MinB = _panelCursorMinB;
            data.MaxB = _panelCursorMaxB;
            return true;
        }

        public bool TryBuildPanelCursorWorldDrawData(Vector2 rawSurfacePoint, out PanelCursorWorldDrawData data)
        {
            data = new PanelCursorWorldDrawData();
            if (_hasStoredPanelCursorSurface && !RefreshStoredPanelCursorSurfaceFromOwner())
                return false;

            if (!_hasPanelCursorSurface)
                return false;

            if (_surface == null || _surface.SurfaceSize.X <= 0f || _surface.SurfaceSize.Y <= 0f)
                return false;

            double width = _panelCursorMaxA - _panelCursorMinA;
            double height = _panelCursorMaxB - _panelCursorMinB;
            if (width <= 0.0001 || height <= 0.0001)
                return false;

            double u = rawSurfacePoint.X / _surface.SurfaceSize.X;
            double v = rawSurfacePoint.Y / _surface.SurfaceSize.Y;
            u = Clamp01(u);
            v = Clamp01(v);
            double localA = _panelCursorMinA + width * u;
            double localB = _panelCursorMaxB - height * v;

            data.PlaneHit = _panelCursorSeed + _panelCursorAxisA * localA + _panelCursorAxisB * localB;
            data.Normal = _panelCursorNormal;
            data.AxisA = _panelCursorAxisA;
            data.AxisB = _panelCursorAxisB;
            data.RawSurfacePoint = rawSurfacePoint;
            data.LocalA = localA;
            data.LocalB = localB;
            data.MinA = _panelCursorMinA;
            data.MaxA = _panelCursorMaxA;
            data.MinB = _panelCursorMinB;
            data.MaxB = _panelCursorMaxB;
            return true;
        }

        Vector3D BuildPanelCursorSurfacePoint(double localA, double localB)
        {
            Vector3D normal = _panelCursorNormal;
            if (normal.LengthSquared() > 0.000001)
                normal.Normalize();
            return _panelCursorSeed + _panelCursorAxisA * localA + _panelCursorAxisB * localB + normal * 0.01;
        }
    }
}
