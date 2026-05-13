using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using VRage.Utils;

namespace GridSchematics
{
    public class PanelInteractionInputBlocker
    {
        readonly ushort _channel;
        bool _registered;

        public PanelInteractionInputBlocker(ushort channel)
        {
            _channel = channel;
        }

        public void Init()
        {
            try
            {
                if (MyAPIGateway.Multiplayer != null &&
                    MyAPIGateway.Multiplayer.MultiplayerActive &&
                    MyAPIGateway.Multiplayer.IsServer &&
                    !_registered)
                {
                    MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(_channel, HandleMessage);
                    _registered = true;
                }
            }
            catch
            {
                _registered = false;
            }
        }

        public void Dispose()
        {
            try
            {
                if (_registered && MyAPIGateway.Multiplayer != null)
                    MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(_channel, HandleMessage);
            }
            catch
            {
            }

            _registered = false;
        }

        public void SetControlsEnabled(List<string> controls, long playerId, bool enabled)
        {
            if (controls == null || controls.Count == 0 || playerId == 0)
                return;

            try
            {
                if (MyAPIGateway.Multiplayer == null ||
                    !MyAPIGateway.Multiplayer.MultiplayerActive ||
                    MyAPIGateway.Multiplayer.IsServer)
                {
                    ApplyControlsEnabled(controls, playerId, enabled);
                    return;
                }

                byte[] bytes = BuildMessageBytes(controls, playerId, enabled);
                MyAPIGateway.Multiplayer.SendMessageToServer(_channel, bytes);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("[Grid Schematics] Panel input block failed: " + e.Message);
            }
        }

        void HandleMessage(ushort channel, byte[] data, ulong sender, bool fromServer)
        {
            try
            {
                if (fromServer || MyAPIGateway.Multiplayer == null || !MyAPIGateway.Multiplayer.IsServer)
                    return;

                List<string> controls;
                long playerId;
                bool enabled;
                if (!TryParseMessageBytes(data, out controls, out playerId, out enabled))
                    return;

                ApplyControlsEnabled(controls, playerId, enabled);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("[Grid Schematics] Panel input message failed: " + e.Message);
            }
        }

        static byte[] BuildMessageBytes(List<string> controls, long playerId, bool enabled)
        {
            var builder = new StringBuilder();
            builder.Append(enabled ? "1" : "0").Append('\n');
            builder.Append(playerId.ToString(CultureInfo.InvariantCulture)).Append('\n');
            for (int i = 0; i < controls.Count; i++)
            {
                string control = controls[i];
                if (!string.IsNullOrEmpty(control))
                    builder.Append(control).Append('\n');
            }

            return Encoding.UTF8.GetBytes(builder.ToString());
        }

        static bool TryParseMessageBytes(byte[] data, out List<string> controls, out long playerId, out bool enabled)
        {
            controls = null;
            playerId = 0;
            enabled = true;
            if (data == null || data.Length == 0)
                return false;

            string text = Encoding.UTF8.GetString(data);
            string[] lines = text.Split('\n');
            if (lines.Length < 3)
                return false;

            enabled = lines[0].Trim() == "1";
            if (!long.TryParse(lines[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out playerId) || playerId == 0)
                return false;

            controls = new List<string>();
            for (int i = 2; i < lines.Length; i++)
            {
                string control = lines[i].Trim();
                if (control.Length > 0)
                    controls.Add(control);
            }

            return controls.Count > 0;
        }

        static void ApplyControlsEnabled(List<string> controls, long playerId, bool enabled)
        {
            if (controls == null || playerId == 0)
                return;

            for (int i = 0; i < controls.Count; i++)
            {
                string control = controls[i];
                if (!string.IsNullOrEmpty(control))
                    MyVisualScriptLogicProvider.SetPlayerInputBlacklistState(control, playerId, enabled);
            }
        }
    }
}
