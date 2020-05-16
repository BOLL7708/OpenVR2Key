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
            { Key.RightShift, VirtualKeyCode.RSHIFT },
            { Key.System, VirtualKeyCode.MENU }
        };
        private static Dictionary<Key, VirtualKeyCode> translationTableKeys = new Dictionary<Key, VirtualKeyCode>()
        {
            { Key.PageUp, VirtualKeyCode.PRIOR },
            { Key.PageDown, VirtualKeyCode.NEXT },
            { Key.MediaNextTrack, VirtualKeyCode.MEDIA_NEXT_TRACK },
            { Key.MediaPreviousTrack, VirtualKeyCode.MEDIA_PREV_TRACK },
            { Key.MediaPlayPause, VirtualKeyCode.MEDIA_PLAY_PAUSE },
            { Key.MediaStop, VirtualKeyCode.MEDIA_STOP },
            { Key.SelectMedia, VirtualKeyCode.LAUNCH_MEDIA_SELECT },
            { Key.VolumeMute, VirtualKeyCode.VOLUME_MUTE },
            { Key.VolumeUp, VirtualKeyCode.VOLUME_UP },
            { Key.VolumeDown, VirtualKeyCode.VOLUME_DOWN },
            { Key.OemClear, VirtualKeyCode.OEM_CLEAR},
            { Key.OemComma, VirtualKeyCode.OEM_COMMA },
            { Key.OemMinus, VirtualKeyCode.OEM_MINUS},
            { Key.OemPeriod, VirtualKeyCode.OEM_PERIOD},
            { Key.OemPlus, VirtualKeyCode.OEM_PLUS},
            /*
             * References for virtual key codes.
             * https://docs.microsoft.com/en-us/windows/win32/inputdev/virtual-key-codes
             * https://sites.google.com/site/douglaslash/Home/programming/c-notes--snippets/c-keycodes
             * http://www.kbdedit.com/manual/low_level_vk_list.html
             */
            { Key.OemOpenBrackets, VirtualKeyCode.OEM_4},
            { Key.OemQuestion, VirtualKeyCode.OEM_2},
            { Key.OemQuotes, VirtualKeyCode.OEM_7},
            { Key.OemBackslash, VirtualKeyCode.OEM_102}
        };

        /**
         * Match a key code against a virtual key code, also check if modifier
         */
        public static Tuple<VirtualKeyCode, bool> MatchVirtualKey(Key key)
        {
            Debug.WriteLine(key.ToString());
            var keyStr = key.ToString().ToUpper();

            // Check for direct translation
            if (translationTableKeys.ContainsKey(key))
            {
                return new Tuple<VirtualKeyCode, bool>(translationTableKeys[key], false);
            }

            // Check for translation as modifier key
            if (translationTableModifiers.ContainsKey(key))
            {
                return new Tuple<VirtualKeyCode, bool>(translationTableModifiers[key], true);
            }

            // Character keys which come in as A-Z
            if (keyStr.Length == 1) keyStr = $"VK_{keyStr}";

            // Number keys which come in as D0-D9
            else if (keyStr.Length == 2 && keyStr[0] == 'D' && Char.IsDigit(keyStr[1]))
            {
                keyStr = $"VK_{keyStr[1]}";
            }
            // OEM Number keys (these are weird and some require direct mapping from translation dictionaries translationTables above)
            else if (keyStr.StartsWith("OEM") && (int.TryParse(keyStr.Substring(3), out _)))
            {
                keyStr = $"OEM_{keyStr.Substring(3)}";
            }
            var success = Enum.TryParse(keyStr, out VirtualKeyCode result);
            if (!success)
            {
                Debug.WriteLine("Key not found.");
            }
            return success ? new Tuple<VirtualKeyCode, bool>(result, false) : null;
            // If no key found, returns null
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
