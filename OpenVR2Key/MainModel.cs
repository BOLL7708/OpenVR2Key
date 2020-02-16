using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenVR2Key
{
    static class MainModel
    {
        public enum Setting
        {
            Minimize, Tray, Notification, Haptic
        }

        private static readonly Properties.Settings p = Properties.Settings.Default;

        static public void UpdateSetting(Setting setting, bool value)
        {
            var propertyName = Enum.GetName(typeof(Setting), setting);
            p[propertyName] = value;
            p.Save();
        }

        static public bool LoadSetting(Setting setting)
        {
            var propertyName = Enum.GetName(typeof(Setting), setting);
            return (bool) p[propertyName];
        }
    }
}
