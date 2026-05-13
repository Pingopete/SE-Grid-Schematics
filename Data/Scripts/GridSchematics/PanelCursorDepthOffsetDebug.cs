using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using VRageRender;

namespace GridSchematics
{
    public partial class GridSchematicsSession
    {
        const string PanelCursorDepthOffsetCommandName = "GSDEPTHCAL";
        const string PanelCursorDepthOffsetFileName = "PanelCursorDepthOffsetCatalog.txt";
        const string PanelCursorDepthOffsetSeparator = "----- Grid Schematics panel cursor depth offset -----";
        const double PanelCursorDepthOffsetDebugBoundsPadding = 0.45;

        bool _panelCursorDepthOffsetEnabled;
        bool _panelCursorDepthOffsetCommandRegistered;
        bool _panelCursorDepthOffsetCaptureWasPressed;
        bool _panelCursorDepthOffsetIndexDecreaseWasPressed;
        bool _panelCursorDepthOffsetIndexIncreaseWasPressed;
        bool _panelCursorDepthOffsetScrollInitialized;
        int _panelCursorDepthOffsetLastScrollValue;
        IMyCubeBlock _panelCursorDepthOffsetOwnerBlock;
        int _panelCursorDepthOffsetScreenIndex = -1;
        double _panelCursorDepthOffsetValue;
        readonly Dictionary<string, PanelCursorDepthOffsetDraft> _panelCursorDepthOffsetDrafts = new Dictionary<string, PanelCursorDepthOffsetDraft>();

        class PanelCursorDepthOffsetDraft
        {
            public long BlockEntityId;
            public int ScreenIndex;
            public double DepthOffset;
            public bool HasOrigin;
            public Vector3D SeedLocal;
            public Vector3D NormalLocal;
            public Vector3D AxisALocal;
            public Vector3D AxisBLocal;
            public double MinA;
            public double MaxA;
            public double MinB;
            public double MaxB;
        }

        public bool IsPanelCursorDepthOffsetCalibrationActive
        {
            get { return _panelCursorDepthOffsetEnabled; }
        }

        public bool IsPanelCursorDepthOffsetCalibrationSelectedSurface(IMyCubeBlock block, int surfaceIndex)
        {
            if (!_panelCursorDepthOffsetEnabled || block == null || _panelCursorDepthOffsetOwnerBlock == null)
                return false;

            return block.EntityId == _panelCursorDepthOffsetOwnerBlock.EntityId &&
                surfaceIndex >= 0 &&
                surfaceIndex == _panelCursorDepthOffsetScreenIndex;
        }

        public bool IsPanelCursorDepthOffsetCalibrationPeerSurface(IMyCubeBlock block)
        {
            return _panelCursorDepthOffsetEnabled &&
                block != null &&
                _panelCursorDepthOffsetOwnerBlock != null &&
                block.EntityId == _panelCursorDepthOffsetOwnerBlock.EntityId;
        }

        public int GetPanelCursorDepthOffsetCalibrationScreenIndex(IMyCubeBlock block, int fallbackSurfaceIndex)
        {
            if (IsPanelCursorDepthOffsetCalibrationPeerSurface(block) && _panelCursorDepthOffsetScreenIndex >= 0)
                return _panelCursorDepthOffsetScreenIndex;

            return fallbackSurfaceIndex;
        }

        void RegisterPanelCursorDepthOffsetDebugCommand()
        {
            if (_panelCursorDepthOffsetCommandRegistered || MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.MessageEntered += OnPanelCursorDepthOffsetDebugMessageEntered;
            _panelCursorDepthOffsetCommandRegistered = true;
        }

        void UnregisterPanelCursorDepthOffsetDebugCommand()
        {
            if (!_panelCursorDepthOffsetCommandRegistered || MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.MessageEntered -= OnPanelCursorDepthOffsetDebugMessageEntered;
            _panelCursorDepthOffsetCommandRegistered = false;
        }

        void OnPanelCursorDepthOffsetDebugMessageEntered(string messageText, ref bool sendToOthers)
        {
            string command = messageText == null ? string.Empty : messageText.Trim();
            string[] parts = command.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 ||
                !parts[0].Equals(PanelCursorDepthOffsetCommandName, StringComparison.OrdinalIgnoreCase) &&
                !parts[0].Equals("/" + PanelCursorDepthOffsetCommandName, StringComparison.OrdinalIgnoreCase) &&
                !parts[0].Equals("\\" + PanelCursorDepthOffsetCommandName, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            sendToOthers = false;
            HandlePanelCursorDepthOffsetCommand(parts);
        }

        void HandlePanelCursorDepthOffsetCommand(string[] parts)
        {
            if (parts.Length <= 1)
            {
                if (_panelCursorDepthOffsetEnabled)
                    DisablePanelCursorDepthOffsetCalibration();
                else
                    EnablePanelCursorDepthOffsetCalibration();
                return;
            }

            if (!_panelCursorDepthOffsetEnabled)
                EnablePanelCursorDepthOffsetCalibration();

            string subCommand = parts[1].Trim();
            if (subCommand.Equals("help", StringComparison.OrdinalIgnoreCase))
            {
                ShowPolygonDebugMessage("/" + PanelCursorDepthOffsetCommandName + ": LMB target, scroll depth, [/] screen, SHIFT+LMB save");
                return;
            }

            if (subCommand.Equals("save", StringComparison.OrdinalIgnoreCase) ||
                subCommand.Equals("confirm", StringComparison.OrdinalIgnoreCase))
            {
                SavePanelCursorDepthOffsetCalibration();
                return;
            }

            if (subCommand.Equals("screen", StringComparison.OrdinalIgnoreCase))
            {
                int screenIndex;
                if (parts.Length < 3 || !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out screenIndex))
                {
                    ShowPolygonDebugMessage("Usage: /" + PanelCursorDepthOffsetCommandName + " screen <lcdSurfaceIndex>");
                    return;
                }

                StoreCurrentPanelCursorDepthOffsetDraft();
                _panelCursorDepthOffsetScreenIndex = Math.Max(0, screenIndex);
                LoadPanelCursorDepthOffsetForSelection();
                ApplyPanelCursorDepthOffsetToSelectedInput();
                ShowPolygonDebugMessage("Depth calibration screen index " + _panelCursorDepthOffsetScreenIndex.ToString(CultureInfo.InvariantCulture) + " selected");
                return;
            }

            if (subCommand.Equals("depth", StringComparison.OrdinalIgnoreCase))
            {
                double depth;
                if (parts.Length < 3 || !TryParsePolygonDebugDouble(parts[2], out depth))
                {
                    ShowPolygonDebugMessage("Usage: /" + PanelCursorDepthOffsetCommandName + " depth <meters>");
                    return;
                }

                _panelCursorDepthOffsetValue = depth;
                StoreCurrentPanelCursorDepthOffsetDraft();
                ApplyPanelCursorDepthOffsetToSelectedInput();
                ShowPolygonDebugMessage("Depth offset " + FormatPolygonDebugDouble(_panelCursorDepthOffsetValue));
                return;
            }

            if (subCommand.Equals("target", StringComparison.OrdinalIgnoreCase) ||
                subCommand.Equals("focus", StringComparison.OrdinalIgnoreCase))
            {
                TargetAimedPanelCursorDepthOffsetInput();
                return;
            }

            ShowPolygonDebugMessage("/" + PanelCursorDepthOffsetCommandName + " commands: help, target, screen <i>, depth <m>, save");
        }

        void EnablePanelCursorDepthOffsetCalibration()
        {
            _panelCursorDepthOffsetEnabled = true;
            _panelDiscoveryPolygonDebugEnabled = false;
            _panelCalibrationDebugEnabled = false;
            ClearPanelCursorDrawCache();
            ClearConstructMouseCursorFocus();
            _panelCursorDepthOffsetCaptureWasPressed = false;
            _panelCursorDepthOffsetIndexDecreaseWasPressed = false;
            _panelCursorDepthOffsetIndexIncreaseWasPressed = false;
            _panelCursorDepthOffsetScrollInitialized = false;

            if (!TargetAimedPanelCursorDepthOffsetInput())
                ShowPolygonDebugMessage("Depth calibration ON - aim at a Grid Schematics panel and click");
        }

        void DisablePanelCursorDepthOffsetCalibration()
        {
            _panelCursorDepthOffsetEnabled = false;
            _panelCursorDepthOffsetOwnerBlock = null;
            _panelCursorDepthOffsetScreenIndex = -1;
            _panelCursorDepthOffsetValue = 0.0;
            _panelCursorDepthOffsetCaptureWasPressed = false;
            _panelCursorDepthOffsetIndexDecreaseWasPressed = false;
            _panelCursorDepthOffsetIndexIncreaseWasPressed = false;
            _panelCursorDepthOffsetScrollInitialized = false;
            ShowPolygonDebugMessage("Depth calibration OFF");
        }

        void UpdatePanelCursorDepthOffsetDebug()
        {
            if (!_panelCursorDepthOffsetEnabled)
                return;

            UpdatePanelCursorDepthOffsetHotkeys();
            UpdatePanelCursorDepthOffsetCapture();
            UpdatePanelCursorDepthOffsetScroll();
        }

        void DrawPanelCursorDepthOffsetDebug()
        {
            if (!_panelCursorDepthOffsetEnabled ||
                _panelCursorDepthOffsetOwnerBlock == null ||
                _panelCursorDepthOffsetScreenIndex < 0 ||
                MyAPIGateway.Session == null ||
                MyAPIGateway.Session.Camera == null)
            {
                return;
            }

            PanelCursorCatalogCalibration calibration;
            if (!TryGetPanelCursorDepthOffsetWorkingCalibration(_panelCursorDepthOffsetOwnerBlock, _panelCursorDepthOffsetScreenIndex, out calibration))
                return;

            IMyEntity ownerEntity = _panelCursorDepthOffsetOwnerBlock as IMyEntity;
            if (ownerEntity == null)
                return;

            Vector3D localNormal = calibration.NormalLocal;
            if (localNormal.LengthSquared() <= 0.000001)
                return;
            localNormal.Normalize();

            MatrixD world = ownerEntity.WorldMatrix;
            Vector3D seed = Vector3D.Transform(calibration.SeedLocal + localNormal * _panelCursorDepthOffsetValue, world);
            Vector3D normal = Vector3D.TransformNormal(localNormal, world);
            Vector3D axisA = Vector3D.TransformNormal(calibration.AxisALocal, world);
            Vector3D axisB = Vector3D.TransformNormal(calibration.AxisBLocal, world);
            if (normal.LengthSquared() <= 0.000001 || axisA.LengthSquared() <= 0.000001 || axisB.LengthSquared() <= 0.000001)
                return;

            normal.Normalize();
            axisA.Normalize();
            axisB.Normalize();

            MatrixD camera = MyAPIGateway.Session.Camera.WorldMatrix;
            Vector3D rayOrigin = camera.Translation;
            Vector3D rayDirection = camera.Forward;
            if (rayDirection.LengthSquared() <= 0.000001)
                return;
            rayDirection.Normalize();

            if (Vector3D.Dot(rayOrigin - seed, normal) < 0.0)
                normal = -normal;

            double denom = Vector3D.Dot(rayDirection, normal);
            if (Math.Abs(denom) <= 0.000001)
                return;

            double distance = Vector3D.Dot(seed - rayOrigin, normal) / denom;
            if (distance <= 0.0 || distance > 12.0)
                return;

            Vector3D hit = rayOrigin + rayDirection * distance;
            Vector3D delta = hit - seed;
            double localA = Vector3D.Dot(delta, axisA);
            double localB = Vector3D.Dot(delta, axisB);
            if (localA < calibration.MinA - PanelCursorDepthOffsetDebugBoundsPadding ||
                localA > calibration.MaxA + PanelCursorDepthOffsetDebugBoundsPadding ||
                localB < calibration.MinB - PanelCursorDepthOffsetDebugBoundsPadding ||
                localB > calibration.MaxB + PanelCursorDepthOffsetDebugBoundsPadding)
            {
                return;
            }

            DrawPanelCursorDepthOffsetSurfaceCross(hit + normal * 0.0008, normal, axisA, axisB, 0.0065f, 0.0017f, new Color(255, 245, 70, 245));
        }

        static void DrawPanelCursorDepthOffsetSurfaceCross(Vector3D center, Vector3D normal, Vector3D preferredAxisA, Vector3D preferredAxisB, float size, float thickness, Color color)
        {
            if (normal.LengthSquared() <= 0.000001 || preferredAxisA.LengthSquared() <= 0.000001 || preferredAxisB.LengthSquared() <= 0.000001)
                return;

            normal.Normalize();
            Vector3D axisA = preferredAxisA - normal * Vector3D.Dot(preferredAxisA, normal);
            if (axisA.LengthSquared() <= 0.000001)
                axisA = Vector3D.Cross(preferredAxisB, normal);
            if (axisA.LengthSquared() <= 0.000001)
                return;
            axisA.Normalize();

            Vector3D axisB = Vector3D.Cross(normal, axisA);
            if (axisB.LengthSquared() <= 0.000001)
                return;
            axisB.Normalize();
            if (Vector3D.Dot(axisB, preferredAxisB) < 0.0)
                axisB = -axisB;

            Vector4 drawColor = color.ToVector4();
            float length = size * 2.0f;
            MyTransparentGeometry.AddBillboardOriented(CalibrationDebugSquareMaterial, drawColor, center, axisA, axisB, length, thickness);
            MyTransparentGeometry.AddBillboardOriented(CalibrationDebugSquareMaterial, drawColor, center, axisB, -axisA, length, thickness);
        }

        void UpdatePanelCursorDepthOffsetHotkeys()
        {
            if (MyAPIGateway.Input == null || IsGameGuiCursorVisible())
                return;

            bool indexDecreasePressed = IsPolygonDebugKeyPressed(VRage.Input.MyKeys.OemOpenBrackets);
            bool indexIncreasePressed = IsPolygonDebugKeyPressed(VRage.Input.MyKeys.OemCloseBrackets);

            if (indexDecreasePressed && !_panelCursorDepthOffsetIndexDecreaseWasPressed)
                CyclePanelCursorDepthOffsetScreenIndex(-1);

            if (indexIncreasePressed && !_panelCursorDepthOffsetIndexIncreaseWasPressed)
                CyclePanelCursorDepthOffsetScreenIndex(1);

            _panelCursorDepthOffsetIndexDecreaseWasPressed = indexDecreasePressed;
            _panelCursorDepthOffsetIndexIncreaseWasPressed = indexIncreasePressed;
        }

        void UpdatePanelCursorDepthOffsetCapture()
        {
            if (MyAPIGateway.Input == null || !IsManualCalibrationMouseInputArmed())
                return;

            bool pressed = IsCurrentLeftMousePressed();
            if (pressed && !_panelCursorDepthOffsetCaptureWasPressed)
            {
                if (IsCurrentShiftPressed())
                    SavePanelCursorDepthOffsetCalibration();
                else
                    TargetAimedPanelCursorDepthOffsetInput();
            }

            _panelCursorDepthOffsetCaptureWasPressed = pressed;
        }

        void UpdatePanelCursorDepthOffsetScroll()
        {
            if (_panelCursorDepthOffsetOwnerBlock == null || MyAPIGateway.Input == null || IsGameGuiCursorVisible())
            {
                _panelCursorDepthOffsetScrollInitialized = false;
                return;
            }

            int wheelValue;
            try
            {
                wheelValue = MyAPIGateway.Input.MouseScrollWheelValue();
            }
            catch
            {
                _panelCursorDepthOffsetScrollInitialized = false;
                return;
            }

            if (!_panelCursorDepthOffsetScrollInitialized)
            {
                _panelCursorDepthOffsetLastScrollValue = wheelValue;
                _panelCursorDepthOffsetScrollInitialized = true;
                return;
            }

            int delta = wheelValue - _panelCursorDepthOffsetLastScrollValue;
            _panelCursorDepthOffsetLastScrollValue = wheelValue;
            if (delta == 0)
                return;

            int direction = delta > 0 ? 1 : -1;
            double step = 0.001;
            if (IsCurrentShiftPressed())
                step = 0.00025;
            else if (IsCurrentControlPressed())
                step = 0.005;

            _panelCursorDepthOffsetValue += direction * step;
            StoreCurrentPanelCursorDepthOffsetDraft();
            ApplyPanelCursorDepthOffsetToSelectedInput();
            ShowPolygonDebugMessage("Depth offset " + FormatPolygonDebugDouble(_panelCursorDepthOffsetValue));
        }

        bool TargetAimedPanelCursorDepthOffsetInput()
        {
            StoreCurrentPanelCursorDepthOffsetDraft();

            TouchScreenApiAdapter input;
            if (!TryFindAimedCalibrationInput(null, out input) || input == null || input.OwnerBlock == null)
                return false;

            bool sameOwner = _panelCursorDepthOffsetOwnerBlock != null &&
                _panelCursorDepthOffsetOwnerBlock.EntityId == input.OwnerBlock.EntityId &&
                _panelCursorDepthOffsetScreenIndex >= 0;

            _panelCursorDepthOffsetOwnerBlock = input.OwnerBlock;
            if (!sameOwner)
            {
                _panelCursorDepthOffsetScreenIndex = input.GetSurfaceIndex();
                if (_panelCursorDepthOffsetScreenIndex < 0)
                    _panelCursorDepthOffsetScreenIndex = 0;
            }

            LoadPanelCursorDepthOffsetForSelection();
            CapturePanelCursorDepthOffsetOriginFromAim(input);
            ApplyPanelCursorDepthOffsetToSelectedInput();
            ShowPolygonDebugMessage("Depth calibration target: screen " + _panelCursorDepthOffsetScreenIndex.ToString(CultureInfo.InvariantCulture) + ", depth " + FormatPolygonDebugDouble(_panelCursorDepthOffsetValue));
            return true;
        }

        void CyclePanelCursorDepthOffsetScreenIndex(int direction)
        {
            if (_panelCursorDepthOffsetOwnerBlock == null)
            {
                ShowPolygonDebugMessage("Target a panel before cycling depth calibration index");
                return;
            }

            StoreCurrentPanelCursorDepthOffsetDraft();

            int surfaceCount = GetBlockSurfaceCount(_panelCursorDepthOffsetOwnerBlock);
            if (surfaceCount <= 0)
                surfaceCount = 1;

            int current = _panelCursorDepthOffsetScreenIndex;
            if (current < 0 || current >= surfaceCount)
                current = 0;

            int next = (current + direction) % surfaceCount;
            if (next < 0)
                next += surfaceCount;

            _panelCursorDepthOffsetScreenIndex = next;
            LoadPanelCursorDepthOffsetForSelection();
            ApplyPanelCursorDepthOffsetToSelectedInput();
            ShowPolygonDebugMessage("Depth calibration screen index " + next.ToString(CultureInfo.InvariantCulture) + ", depth " + FormatPolygonDebugDouble(_panelCursorDepthOffsetValue));
        }

        void LoadPanelCursorDepthOffsetForSelection()
        {
            _panelCursorDepthOffsetValue = 0.0;
            if (_panelCursorDepthOffsetOwnerBlock == null || _panelCursorDepthOffsetScreenIndex < 0)
                return;

            PanelCursorDepthOffsetDraft draft;
            if (TryGetPanelCursorDepthOffsetDraft(_panelCursorDepthOffsetOwnerBlock, _panelCursorDepthOffsetScreenIndex, out draft))
            {
                _panelCursorDepthOffsetValue = draft.DepthOffset;
                return;
            }

            double depth;
            if (TryReadPanelCursorDepthOffset(GetBlockDefinitionId(_panelCursorDepthOffsetOwnerBlock), _panelCursorDepthOffsetScreenIndex, out depth))
            {
                _panelCursorDepthOffsetValue = depth;
                return;
            }

            PanelCursorCatalogCalibration calibration;
            if (TryGetPanelCursorDepthOffsetWorkingCalibration(_panelCursorDepthOffsetOwnerBlock, _panelCursorDepthOffsetScreenIndex, out calibration))
                _panelCursorDepthOffsetValue = calibration.DepthOffset;
        }

        bool ApplyPanelCursorDepthOffsetToSelectedInput()
        {
            if (_panelCursorDepthOffsetOwnerBlock == null || _panelCursorDepthOffsetScreenIndex < 0)
                return false;

            TouchScreenApiAdapter input;
            GridSchematicsLcdApp app;
            if (!TryFindCalibrationInputForOwner(_apps, _panelCursorDepthOffsetOwnerBlock, _panelCursorDepthOffsetScreenIndex, out input, out app) &&
                !TryFindCalibrationInputForOwner(_surfaceScriptApps, _panelCursorDepthOffsetOwnerBlock, _panelCursorDepthOffsetScreenIndex, out input, out app) &&
                !TryFindCalibrationProbeForOwner(_panelCursorDepthOffsetOwnerBlock, _panelCursorDepthOffsetScreenIndex, out input))
            {
                return false;
            }

            PanelCursorCatalogCalibration calibration;
            if (!TryGetPanelCursorDepthOffsetWorkingCalibration(_panelCursorDepthOffsetOwnerBlock, _panelCursorDepthOffsetScreenIndex, out calibration))
                return false;

            Vector3D seedLocal = calibration.SeedLocal;
            Vector3D normalLocal = calibration.NormalLocal;
            if (normalLocal.LengthSquared() <= 0.000001)
                return false;
            normalLocal.Normalize();
            seedLocal += normalLocal * _panelCursorDepthOffsetValue;

            input.SetManualPanelCursorSurfaceLocal(seedLocal, normalLocal, calibration.AxisALocal, calibration.AxisBLocal, calibration.MinA, calibration.MaxA, calibration.MinB, calibration.MaxB);
            if (app != null)
                app.NoteManualLowLevelCalibrationApplied();
            return true;
        }

        bool SavePanelCursorDepthOffsetCalibration()
        {
            if (_panelCursorDepthOffsetOwnerBlock == null || _panelCursorDepthOffsetScreenIndex < 0)
            {
                ShowPolygonDebugMessage("No depth calibration target selected");
                return false;
            }

            StoreCurrentPanelCursorDepthOffsetDraft();

            string blockDefinition = GetBlockDefinitionId(_panelCursorDepthOffsetOwnerBlock);
            if (string.IsNullOrEmpty(blockDefinition) || MyAPIGateway.Utilities == null)
                return false;

            var draftIndexes = new List<int>();
            foreach (var pair in _panelCursorDepthOffsetDrafts)
            {
                PanelCursorDepthOffsetDraft draft = pair.Value;
                if (draft != null &&
                    draft.BlockEntityId == _panelCursorDepthOffsetOwnerBlock.EntityId &&
                    draft.ScreenIndex >= 0 &&
                    !draftIndexes.Contains(draft.ScreenIndex))
                {
                    draftIndexes.Add(draft.ScreenIndex);
                }
            }

            draftIndexes.Sort();
            if (draftIndexes.Count == 0)
                draftIndexes.Add(_panelCursorDepthOffsetScreenIndex);

            string existing = string.Empty;
            TryReadPanelCursorDepthOffsetCatalog(out existing);
            string compacted = existing;
            for (int i = 0; i < draftIndexes.Count; i++)
            {
                if (!string.IsNullOrEmpty(compacted))
                    RemovePanelCursorDepthOffsetEntry(compacted, blockDefinition, draftIndexes[i], out compacted);
            }

            try
            {
                using (TextWriter writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(PanelCursorDepthOffsetFileName, typeof(GridSchematicsSession)))
                {
                    if (!string.IsNullOrEmpty(compacted))
                    {
                        writer.Write(compacted);
                        if (!compacted.EndsWith("\n", StringComparison.Ordinal))
                            writer.WriteLine();
                        writer.WriteLine();
                    }

                    for (int i = 0; i < draftIndexes.Count; i++)
                    {
                        PanelCursorDepthOffsetDraft draft;
                        if (!TryGetPanelCursorDepthOffsetDraft(_panelCursorDepthOffsetOwnerBlock, draftIndexes[i], out draft))
                            continue;

                        writer.WriteLine(PanelCursorDepthOffsetSeparator);
                        writer.Write(BuildPanelCursorDepthOffsetEntry(_panelCursorDepthOffsetOwnerBlock, draft.ScreenIndex, draft.DepthOffset));
                        writer.WriteLine();
                    }
                }

                ApplyPanelCursorDepthOffsetToSelectedInput();
                ShowPolygonDebugMessage("Depth offset drafts saved to " + PanelCursorDepthOffsetFileName + " (" + draftIndexes.Count.ToString(CultureInfo.InvariantCulture) + " screen" + (draftIndexes.Count == 1 ? "" : "s") + ")");
                return true;
            }
            catch
            {
                ShowPolygonDebugMessage("Could not save depth offset");
                return false;
            }
        }

        string BuildPanelCursorDepthOffsetEntry(IMyCubeBlock block, int screenIndex, double depthOffset)
        {
            string blockDisplayName = "Unknown";
            IMyEntity entity = block as IMyEntity;
            if (entity != null && !string.IsNullOrEmpty(entity.DisplayName))
                blockDisplayName = entity.DisplayName;

            return
                "GridSchematicsPanelCursorDepthOffset\n" +
                "BlockDefinitionId=" + GetBlockDefinitionId(block) + "\n" +
                "BlockDisplayName=" + blockDisplayName + "\n" +
                "ScreenIndex=" + screenIndex.ToString(CultureInfo.InvariantCulture) + "\n" +
                "CursorDepthOffset=" + FormatPolygonDebugDouble(depthOffset) + "\n";
        }

        static string GetPanelCursorDepthOffsetDraftKey(IMyCubeBlock block, int screenIndex)
        {
            if (block == null || screenIndex < 0)
                return string.Empty;

            return block.EntityId.ToString(CultureInfo.InvariantCulture) + ":" + screenIndex.ToString(CultureInfo.InvariantCulture);
        }

        void StoreCurrentPanelCursorDepthOffsetDraft()
        {
            if (_panelCursorDepthOffsetOwnerBlock == null || _panelCursorDepthOffsetScreenIndex < 0)
                return;

            string key = GetPanelCursorDepthOffsetDraftKey(_panelCursorDepthOffsetOwnerBlock, _panelCursorDepthOffsetScreenIndex);
            if (string.IsNullOrEmpty(key))
                return;

            PanelCursorDepthOffsetDraft draft;
            if (!_panelCursorDepthOffsetDrafts.TryGetValue(key, out draft) || draft == null)
            {
                draft = new PanelCursorDepthOffsetDraft();
                _panelCursorDepthOffsetDrafts[key] = draft;
            }

            draft.BlockEntityId = _panelCursorDepthOffsetOwnerBlock.EntityId;
            draft.ScreenIndex = _panelCursorDepthOffsetScreenIndex;
            draft.DepthOffset = _panelCursorDepthOffsetValue;
        }

        bool TryGetPanelCursorDepthOffsetDraft(IMyCubeBlock block, int screenIndex, out PanelCursorDepthOffsetDraft draft)
        {
            draft = null;
            string key = GetPanelCursorDepthOffsetDraftKey(block, screenIndex);
            return !string.IsNullOrEmpty(key) && _panelCursorDepthOffsetDrafts.TryGetValue(key, out draft) && draft != null;
        }

        bool CapturePanelCursorDepthOffsetOriginFromAim(TouchScreenApiAdapter input)
        {
            if (input == null || _panelCursorDepthOffsetOwnerBlock == null || _panelCursorDepthOffsetScreenIndex < 0)
                return false;

            PanelCursorCatalogCalibration calibration;
            if (!TryGetPanelCursorCatalogCalibration(_panelCursorDepthOffsetOwnerBlock, _panelCursorDepthOffsetScreenIndex, out calibration))
            {
                Vector3D seed;
                Vector3D normal;
                Vector3D axisA;
                Vector3D axisB;
                double minA;
                double maxA;
                double minB;
                double maxB;
                if (!input.TryGetStoredPanelCursorSurfaceCalibration(out seed, out normal, out axisA, out axisB, out minA, out maxA, out minB, out maxB) ||
                    !TryBuildPanelCursorDepthOffsetCalibrationFromWorld(_panelCursorDepthOffsetOwnerBlock, seed, normal, axisA, axisB, minA, maxA, minB, maxB, out calibration))
                {
                    return false;
                }
            }

            Vector3D hitPosition;
            Vector3D hitNormal;
            if (!input.TryGetCurrentModelAimDebugHit(out hitPosition, out hitNormal))
                return false;

            if (hitNormal.LengthSquared() <= 0.000001)
                return false;
            hitNormal.Normalize();
            if (MyAPIGateway.Session != null && MyAPIGateway.Session.Camera != null &&
                Vector3D.Dot(MyAPIGateway.Session.Camera.WorldMatrix.Translation - hitPosition, hitNormal) < 0.0)
            {
                hitNormal = -hitNormal;
            }

            IMyEntity ownerEntity = _panelCursorDepthOffsetOwnerBlock as IMyEntity;
            if (ownerEntity == null)
                return false;

            MatrixD inverseBasis = MatrixD.Invert(ownerEntity.WorldMatrix);
            MatrixD inverseBasisNormal = MatrixD.Transpose(ownerEntity.WorldMatrix);
            calibration.SeedLocal = Vector3D.Transform(hitPosition, inverseBasis);
            calibration.NormalLocal = Vector3D.TransformNormal(hitNormal, inverseBasisNormal);
            if (calibration.NormalLocal.LengthSquared() <= 0.000001)
                return false;
            calibration.NormalLocal.Normalize();

            string key = GetPanelCursorDepthOffsetDraftKey(_panelCursorDepthOffsetOwnerBlock, _panelCursorDepthOffsetScreenIndex);
            if (string.IsNullOrEmpty(key))
                return false;

            _panelCursorDepthOffsetDrafts[key] = new PanelCursorDepthOffsetDraft
            {
                BlockEntityId = _panelCursorDepthOffsetOwnerBlock.EntityId,
                ScreenIndex = _panelCursorDepthOffsetScreenIndex,
                DepthOffset = _panelCursorDepthOffsetValue,
                HasOrigin = true,
                SeedLocal = calibration.SeedLocal,
                NormalLocal = calibration.NormalLocal,
                AxisALocal = calibration.AxisALocal,
                AxisBLocal = calibration.AxisBLocal,
                MinA = calibration.MinA,
                MaxA = calibration.MaxA,
                MinB = calibration.MinB,
                MaxB = calibration.MaxB
            };

            return true;
        }

        bool TryReadPanelCursorDepthOffset(string blockDefinition, int screenIndex, out double depthOffset)
        {
            depthOffset = 0.0;
            string catalog;
            if (string.IsNullOrEmpty(blockDefinition) || screenIndex < 0 || !TryReadPanelCursorDepthOffsetCatalog(out catalog) || string.IsNullOrEmpty(catalog))
                return false;

            string[] entries = catalog.Split(new string[] { PanelCursorDepthOffsetSeparator }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = entries.Length - 1; i >= 0; i--)
            {
                string entry = entries[i];
                if (!string.Equals(GetCalibrationCatalogValue(entry, "BlockDefinitionId"), blockDefinition, StringComparison.Ordinal))
                    continue;

                int entryScreenIndex;
                if (!int.TryParse(GetCalibrationCatalogValue(entry, "ScreenIndex"), NumberStyles.Integer, CultureInfo.InvariantCulture, out entryScreenIndex) ||
                    entryScreenIndex != screenIndex)
                {
                    continue;
                }

                return TryParsePolygonDebugDouble(GetCalibrationCatalogValue(entry, "CursorDepthOffset"), out depthOffset);
            }

            return false;
        }

        bool TryReadPanelCursorDepthOffsetCatalog(out string catalog)
        {
            catalog = string.Empty;
            if (MyAPIGateway.Utilities == null)
                return false;

            try
            {
                using (TextReader reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(PanelCursorDepthOffsetFileName, typeof(GridSchematicsSession)))
                {
                    catalog = reader.ReadToEnd();
                }

                return true;
            }
            catch
            {
                catalog = string.Empty;
                return false;
            }
        }

        static bool RemovePanelCursorDepthOffsetEntry(string catalog, string blockDefinition, int screenIndex, out string updatedCatalog)
        {
            updatedCatalog = string.Empty;
            if (string.IsNullOrEmpty(catalog))
                return false;

            bool removed = false;
            var builder = new System.Text.StringBuilder();
            string[] entries = catalog.Split(new string[] { PanelCursorDepthOffsetSeparator }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                string trimmed = entries[i] == null ? string.Empty : entries[i].Trim();
                if (string.IsNullOrEmpty(trimmed))
                    continue;

                int entryScreenIndex;
                bool match =
                    string.Equals(GetCalibrationCatalogValue(trimmed, "BlockDefinitionId"), blockDefinition, StringComparison.Ordinal) &&
                    int.TryParse(GetCalibrationCatalogValue(trimmed, "ScreenIndex"), NumberStyles.Integer, CultureInfo.InvariantCulture, out entryScreenIndex) &&
                    entryScreenIndex == screenIndex;
                if (match)
                {
                    removed = true;
                    continue;
                }

                if (builder.Length > 0)
                    builder.AppendLine();
                builder.AppendLine(PanelCursorDepthOffsetSeparator);
                builder.Append(trimmed);
                builder.AppendLine();
            }

            updatedCatalog = builder.ToString();
            return removed;
        }

        struct PanelCursorCatalogCalibration
        {
            public Vector3D SeedLocal;
            public Vector3D NormalLocal;
            public Vector3D AxisALocal;
            public Vector3D AxisBLocal;
            public double MinA;
            public double MaxA;
            public double MinB;
            public double MaxB;
            public double DepthOffset;
        }

        bool TryGetPanelCursorCatalogCalibration(IMyCubeBlock block, int screenIndex, out PanelCursorCatalogCalibration calibration)
        {
            calibration = new PanelCursorCatalogCalibration();
            if (block == null || screenIndex < 0)
                return false;

            string blockDefinition = GetBlockDefinitionId(block);
            string catalog;
            if (string.IsNullOrEmpty(blockDefinition) || !TryReadPanelCalibrationCatalog(out catalog) || string.IsNullOrEmpty(catalog))
                return false;

            string[] entries = catalog.Split(new string[] { PanelCalibrationCatalogSeparator }, StringSplitOptions.RemoveEmptyEntries);
            string best = null;
            for (int i = 0; i < entries.Length; i++)
            {
                string entry = entries[i];
                if (!string.Equals(GetCalibrationCatalogValue(entry, "BlockDefinitionId"), blockDefinition, StringComparison.Ordinal))
                    continue;

                int entryScreenIndex;
                if (!int.TryParse(GetCalibrationCatalogValue(entry, "ScreenIndex"), NumberStyles.Integer, CultureInfo.InvariantCulture, out entryScreenIndex))
                    entryScreenIndex = -1;

                if (entryScreenIndex == screenIndex)
                    best = entry;
            }

            if (string.IsNullOrEmpty(best))
                return false;

            if (!TryParseCalibrationCatalogVector(GetCalibrationCatalogValue(best, "SeedLocal"), out calibration.SeedLocal) ||
                !TryParseCalibrationCatalogVector(GetCalibrationCatalogValue(best, "NormalLocal"), out calibration.NormalLocal) ||
                !TryParseCalibrationCatalogVector(GetCalibrationCatalogValue(best, "AxisALocal"), out calibration.AxisALocal) ||
                !TryParseCalibrationCatalogVector(GetCalibrationCatalogValue(best, "AxisBLocal"), out calibration.AxisBLocal) ||
                !TryParsePolygonDebugDouble(GetCalibrationCatalogValue(best, "FinalMinA"), out calibration.MinA) ||
                !TryParsePolygonDebugDouble(GetCalibrationCatalogValue(best, "FinalMaxA"), out calibration.MaxA) ||
                !TryParsePolygonDebugDouble(GetCalibrationCatalogValue(best, "FinalMinB"), out calibration.MinB) ||
                !TryParsePolygonDebugDouble(GetCalibrationCatalogValue(best, "FinalMaxB"), out calibration.MaxB))
            {
                return false;
            }

            TryParsePolygonDebugDouble(GetCalibrationCatalogValue(best, "CursorDepthOffset"), out calibration.DepthOffset);
            return true;
        }

        bool TryGetPanelCursorDepthOffsetWorkingCalibration(IMyCubeBlock block, int screenIndex, out PanelCursorCatalogCalibration calibration)
        {
            PanelCursorDepthOffsetDraft draft;
            if (TryGetPanelCursorDepthOffsetDraft(block, screenIndex, out draft) && draft.HasOrigin)
            {
                calibration = new PanelCursorCatalogCalibration
                {
                    SeedLocal = draft.SeedLocal,
                    NormalLocal = draft.NormalLocal,
                    AxisALocal = draft.AxisALocal,
                    AxisBLocal = draft.AxisBLocal,
                    MinA = draft.MinA,
                    MaxA = draft.MaxA,
                    MinB = draft.MinB,
                    MaxB = draft.MaxB,
                    DepthOffset = draft.DepthOffset
                };

                return true;
            }

            if (TryGetPanelCursorCatalogCalibration(block, screenIndex, out calibration))
                return true;

            TouchScreenApiAdapter input;
            GridSchematicsLcdApp app;
            if ((TryFindCalibrationInputForOwner(_apps, block, screenIndex, out input, out app) ||
                TryFindCalibrationInputForOwner(_surfaceScriptApps, block, screenIndex, out input, out app) ||
                TryFindCalibrationProbeForOwner(block, screenIndex, out input)) &&
                input != null)
            {
                if (!input.HasStoredPanelCursorSurface)
                    TryApplyStoredPanelCursorCalibration(input);

                Vector3D seed;
                Vector3D normal;
                Vector3D axisA;
                Vector3D axisB;
                double minA;
                double maxA;
                double minB;
                double maxB;
                if (input.TryGetStoredPanelCursorSurfaceCalibration(out seed, out normal, out axisA, out axisB, out minA, out maxA, out minB, out maxB))
                    return TryBuildPanelCursorDepthOffsetCalibrationFromWorld(block, seed, normal, axisA, axisB, minA, maxA, minB, maxB, out calibration);
            }

            calibration = new PanelCursorCatalogCalibration();
            return false;
        }

        static bool TryBuildPanelCursorDepthOffsetCalibrationFromWorld(IMyCubeBlock block, Vector3D seed, Vector3D normal, Vector3D axisA, Vector3D axisB, double minA, double maxA, double minB, double maxB, out PanelCursorCatalogCalibration calibration)
        {
            calibration = new PanelCursorCatalogCalibration();
            IMyEntity entity = block as IMyEntity;
            if (entity == null)
                return false;

            if (normal.LengthSquared() <= 0.000001 || axisA.LengthSquared() <= 0.000001 || axisB.LengthSquared() <= 0.000001)
                return false;

            MatrixD inverseBasis = MatrixD.Invert(entity.WorldMatrix);
            MatrixD inverseBasisNormal = MatrixD.Transpose(entity.WorldMatrix);
            calibration.SeedLocal = Vector3D.Transform(seed, inverseBasis);
            calibration.NormalLocal = Vector3D.TransformNormal(normal, inverseBasisNormal);
            calibration.AxisALocal = Vector3D.TransformNormal(axisA, inverseBasisNormal);
            calibration.AxisBLocal = Vector3D.TransformNormal(axisB, inverseBasisNormal);
            calibration.MinA = minA;
            calibration.MaxA = maxA;
            calibration.MinB = minB;
            calibration.MaxB = maxB;
            calibration.DepthOffset = 0.0;
            return true;
        }
    }
}
