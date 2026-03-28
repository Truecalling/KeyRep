using System;
using System.Runtime.InteropServices;

namespace KeyRep
{
    internal static class NativeKeyboard
    {
        private const uint KeyeventfKeyup = 0x0002;
        private const uint MapvkVkToVsc = 0;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern uint MapVirtualKey(uint uCode, uint uMapType);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        public static void TapVirtualKey(ushort virtualKey)
        {
            var vk = (byte)(virtualKey & 0xFF);
            var scan = (byte)(MapVirtualKey(vk, MapvkVkToVsc) & 0xFF);
            keybd_event(vk, scan, 0, UIntPtr.Zero);
            keybd_event(vk, scan, KeyeventfKeyup, UIntPtr.Zero);
        }

        public static void TapChord(ushort modifierVirtualKey, ushort keyVirtualKey) =>
            TapChord(stackalloc ushort[] { modifierVirtualKey }, keyVirtualKey);

        public static void TapChord(ReadOnlySpan<ushort> modifiers, ushort keyVirtualKey)
        {
            Span<byte> modVk = stackalloc byte[2];
            Span<byte> modScan = stackalloc byte[2];
            var n = 0;
            foreach (var m in modifiers)
            {
                if (m == 0)
                    continue;
                var b = (byte)(m & 0xFF);
                var dup = false;
                for (var i = 0; i < n; i++)
                {
                    if (modVk[i] == b)
                    {
                        dup = true;
                        break;
                    }
                }

                if (dup || n >= 2)
                    continue;
                modVk[n] = b;
                modScan[n] = (byte)(MapVirtualKey(b, MapvkVkToVsc) & 0xFF);
                n++;
            }

            var vk = (byte)(keyVirtualKey & 0xFF);
            var scan = (byte)(MapVirtualKey(vk, MapvkVkToVsc) & 0xFF);

            for (var i = 0; i < n; i++)
                keybd_event(modVk[i], modScan[i], 0, UIntPtr.Zero);

            keybd_event(vk, scan, 0, UIntPtr.Zero);
            keybd_event(vk, scan, KeyeventfKeyup, UIntPtr.Zero);

            for (var i = n - 1; i >= 0; i--)
                keybd_event(modVk[i], modScan[i], KeyeventfKeyup, UIntPtr.Zero);
        }
    }
}
