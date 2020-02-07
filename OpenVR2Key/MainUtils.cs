using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;
using WindowsInput.Native;

namespace OpenVR2Key
{
    public static class MainUtils
    {
        private static Dictionary<Key, VirtualKeyCode> translationTable = new Dictionary<Key, VirtualKeyCode>() {
            { Key.LeftAlt, VirtualKeyCode.LMENU },
            { Key.RightAlt, VirtualKeyCode.RMENU },
            { Key.LeftCtrl, VirtualKeyCode.LCONTROL },
            { Key.RightCtrl, VirtualKeyCode.RCONTROL },
            { Key.LeftShift, VirtualKeyCode.LSHIFT },
            { Key.RightShift, VirtualKeyCode.RSHIFT }
        };

        public static VirtualKeyCode MatchVirtualKey(Key key)
        {
            Debug.WriteLine(key.ToString());
            var keyStr = key.ToString().ToUpper();

            // Check for direct translation
            if (translationTable.ContainsKey(key)) return translationTable[key];
            // Character keys which come in as A-Z
            if (keyStr.Length == 1) keyStr = $"VK_{keyStr}";
            // Number keys which come in as D0-D9
            else if (keyStr.Length == 2 && keyStr[0] == 'D' && Char.IsDigit(keyStr[1])) keyStr = $"VK_{keyStr[1]}";

            var success = Enum.TryParse(keyStr, out VirtualKeyCode result);
            return success ? result : 0;
        }
    }
}
