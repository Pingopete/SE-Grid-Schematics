using Sandbox.ModAPI;
using System;

namespace GridSchematics
{
    public partial class GridSchematicsSession
    {
        const string PanelLayoutSnapshotCommand = "/GSLCDSNAP";
        bool _panelLayoutSnapshotCommandRegistered;

        void RegisterPanelLayoutSnapshotCommand()
        {
            if (_panelLayoutSnapshotCommandRegistered || MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.MessageEntered += OnPanelLayoutSnapshotMessageEntered;
            _panelLayoutSnapshotCommandRegistered = true;
        }

        void UnregisterPanelLayoutSnapshotCommand()
        {
            if (!_panelLayoutSnapshotCommandRegistered || MyAPIGateway.Utilities == null)
                return;

            MyAPIGateway.Utilities.MessageEntered -= OnPanelLayoutSnapshotMessageEntered;
            _panelLayoutSnapshotCommandRegistered = false;
        }

        void OnPanelLayoutSnapshotMessageEntered(string messageText, ref bool sendToOthers)
        {
            string text = messageText == null ? string.Empty : messageText.Trim();
            string command;
            string scope;
            if (!TryParseLayoutSnapshotCommand(text, out command, out scope))
                return;

            sendToOthers = false;
            GridSchematicsLcdApp app = FindLayoutSnapshotTargetApp();
            if (app == null)
            {
                ShowLayoutSnapshotMessage("No Grid Schematics panel found.");
                return;
            }

            string label = app.OwnerBlock != null ? app.OwnerBlock.CustomName : "GridSchematics";
            bool captured = app.CaptureLayoutSnapshotNow(label, scope);
            if (!captured)
            {
                ShowLayoutSnapshotMessage("Snapshot failed or produced no sprites.");
                return;
            }

            ShowLayoutSnapshotMessage("Snapshot saved: " + RenderEngine.LastSnapshotFileName + " (" + RenderEngine.LastSnapshotSpriteCount + " sprites, " + scope + ")");
        }

        static bool TryParseLayoutSnapshotCommand(string text, out string command, out string scope)
        {
            command = string.Empty;
            scope = "full";
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string[] parts = text.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return false;

            command = parts[0];
            if (!command.Equals(PanelLayoutSnapshotCommand, StringComparison.OrdinalIgnoreCase) &&
                !command.Equals("\\GSLCDSNAP", StringComparison.OrdinalIgnoreCase) &&
                !command.Equals("/GSSNAPSHOT", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (parts.Length > 1)
                scope = NormalizeLayoutSnapshotScope(parts[1]);
            return true;
        }

        static string NormalizeLayoutSnapshotScope(string scope)
        {
            if (string.IsNullOrWhiteSpace(scope))
                return "full";

            scope = scope.Trim().ToLowerInvariant();
            if (scope == "drawer" || scope == "info" || scope == "panel")
                return "drawer";
            return "full";
        }

        GridSchematicsLcdApp FindLayoutSnapshotTargetApp()
        {
            GridSchematicsLcdApp app;
            PanelCursorWorldDrawData cursor;
            if (TryFindPanelSurfaceCursorApp(out app, out cursor) && app != null)
                return app;

            app = FindAimCursorApp();
            if (app != null)
                return app;

            for (int i = _apps.Count - 1; i >= 0; i--)
            {
                app = _apps[i];
                if (app != null && app.IsOwnerFunctional && app.Config.Enabled)
                    return app;
            }

            for (int i = _surfaceScriptApps.Count - 1; i >= 0; i--)
            {
                app = _surfaceScriptApps[i];
                if (app != null && app.IsOwnerFunctional && app.Config.Enabled)
                    return app;
            }

            return null;
        }

        static void ShowLayoutSnapshotMessage(string text)
        {
            try
            {
                if (MyAPIGateway.Utilities == null)
                    return;

                MyAPIGateway.Utilities.ShowMessage("Grid Schematics", text);
                MyAPIGateway.Utilities.ShowNotification(text, 1800, "White");
            }
            catch
            {
            }
        }
    }
}