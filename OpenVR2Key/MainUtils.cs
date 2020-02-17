using GregsStack.InputSimulatorStandard.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;

namespace OpenVR2Key
{
    public static class MainUtils
    {
        private static Dictionary<Key, VirtualKeyCode> translationTableModifiers = new Dictionary<Key, VirtualKeyCode>() {
            { Key.LeftAlt, VirtualKeyCode.LMENU },
            { Key.RightAlt, VirtualKeyCode.RMENU },
            { Key.LeftCtrl, VirtualKeyCode.LCONTROL },
            { Key.RightCtrl, VirtualKeyCode.RCONTROL },
            { Key.LeftShift, VirtualKeyCode.LSHIFT },
            { Key.RightShift, VirtualKeyCode.RSHIFT }
        };
        private static Dictionary<Key, VirtualKeyCode> translationTableKeys = new Dictionary<Key, VirtualKeyCode>()
        {
            { Key.MediaNextTrack, VirtualKeyCode.MEDIA_NEXT_TRACK },
            { Key.MediaPreviousTrack, VirtualKeyCode.MEDIA_PREV_TRACK },
            { Key.MediaPlayPause, VirtualKeyCode.MEDIA_PLAY_PAUSE },
            { Key.MediaStop, VirtualKeyCode.MEDIA_STOP },
            { Key.SelectMedia, VirtualKeyCode.LAUNCH_MEDIA_SELECT },
            { Key.VolumeMute, VirtualKeyCode.VOLUME_MUTE },
            { Key.VolumeUp, VirtualKeyCode.VOLUME_UP },
            { Key.VolumeDown, VirtualKeyCode.VOLUME_DOWN }
        };

        /**
         * Match a key code against a virtual key code, also check if modifier
         */
        public static Tuple<VirtualKeyCode, bool> MatchVirtualKey(Key key)
        {
            Debug.WriteLine(key.ToString());
            var keyStr = key.ToString().ToUpper();

            // Check for direct translation
            if (translationTableKeys.ContainsKey(key)) return new Tuple<VirtualKeyCode, bool>(translationTableKeys[key], false);
            if (translationTableModifiers.ContainsKey(key)) return new Tuple<VirtualKeyCode, bool>(translationTableModifiers[key], true);
            // Character keys which come in as A-Z
            if (keyStr.Length == 1) keyStr = $"VK_{keyStr}";
            // Number keys which come in as D0-D9
            else if (keyStr.Length == 2 && keyStr[0] == 'D' && Char.IsDigit(keyStr[1])) keyStr = $"VK_{keyStr[1]}";

            var success = Enum.TryParse(keyStr, out VirtualKeyCode result);
            return success ? new Tuple<VirtualKeyCode, bool>(result, false) : null;
        }

        /**
         * Match incoming key codes to virtual ones, sort out modifiers
         */
        public static Tuple<Key[], VirtualKeyCode[], VirtualKeyCode[]> ConvertKeys(Key[] keys)
        {
            var vModifiers = new List<VirtualKeyCode>();
            var vKeys = new List<VirtualKeyCode>();
            for (var i = 0; i < keys.Length; i++)
            {
                var match = MatchVirtualKey(keys[i]);
                if (match != null)
                {
                    if (match.Item2) vModifiers.Add(match.Item1);
                    else vKeys.Add(match.Item1);
                }
            }
            return new Tuple<Key[], VirtualKeyCode[], VirtualKeyCode[]>(keys, vModifiers.ToArray(), vKeys.ToArray());
        }
    }
}
