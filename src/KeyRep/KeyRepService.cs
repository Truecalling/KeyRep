using System;
using VoK.Sdk;
using VoK.Sdk.Ddo;

namespace KeyRep
{
    public sealed class KeyRepService : IDisposable
    {
        private readonly IDdoGameDataProvider _ddo;
        private readonly IGameDataProvider _game;

        public KeyRepService(IDdoGameDataProvider ddo)
        {
            _ddo = ddo;
            _game = ddo;
        }

        public static bool WindowsKeySendSupported => OperatingSystem.IsWindows();

        public bool HotKeysEnabled => _game.HotKeysEnabled;

        public bool TryGetHotbarCommand(int bar1Based, int slot1Based, out uint command, out string? error)
        {
            command = 0;
            error = null;
            if (bar1Based < 1 || slot1Based < 1)
            {
                error = "Bar and slot must be 1 or greater.";
                return false;
            }

            IHotbarSlot?[] map;
            try
            {
                map = _ddo.GetHotbarMap();
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            if (map == null || map.Length == 0)
            {
                error = "Hotbar map is empty. Log in on a character and wait for bars to sync (restart DH after login if needed).";
                return false;
            }

            var bar0 = bar1Based - 1;
            var slot0 = slot1Based - 1;
            var idx = slot0 + bar0 * 10;
            if (idx < 0 || idx >= map.Length)
            {
                error = $"Bar {bar1Based} slot {slot1Based} is out of range for this map (length {map.Length}).";
                return false;
            }

            var slot = map[idx];
            if (slot == null)
            {
                error = "That hotbar cell is empty.";
                return false;
            }

            try
            {
                var c = slot.GetInputAction();
                if (c == null)
                {
                    error = "That slot has no SendInput command (GetInputAction returned null).";
                    return false;
                }

                command = c.Value;
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        public void SendDdo(uint inputCommand, bool sendRelease)
        {
            try
            {
                _ddo.SendInput(inputCommand);
            }
            catch
            {
                /* */
            }

            if (!sendRelease)
                return;

            try
            {
                _ddo.ReleaseInput(inputCommand);
            }
            catch
            {
                /* */
            }
        }

        public void SendWindowsKey(int modifierKeyCode1, int modifierKeyCode2, int windowsFormsKeyCode)
        {
            if (!OperatingSystem.IsWindows())
                return;
            try
            {
                var key = (ushort)(windowsFormsKeyCode & 0xFFFF);
                Span<ushort> mods = stackalloc ushort[2];
                var n = 0;
                if (modifierKeyCode1 != 0)
                    mods[n++] = (ushort)(modifierKeyCode1 & 0xFFFF);
                if (modifierKeyCode2 != 0 && modifierKeyCode2 != modifierKeyCode1)
                    mods[n++] = (ushort)(modifierKeyCode2 & 0xFFFF);
                if (n == 0)
                    NativeKeyboard.TapVirtualKey(key);
                else
                    NativeKeyboard.TapChord(mods[..n], key);
            }
            catch
            {
                /* */
            }
        }

        public void Dispose()
        {
        }
    }
}
