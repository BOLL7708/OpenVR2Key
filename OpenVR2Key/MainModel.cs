using GregsStack.InputSimulatorStandard.Native;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace OpenVR2Key
{
    static class MainModel
    {
        #region bindings
        public static readonly string CONFIG_DEFAULT = "default";
        private static readonly object _bindingsLock = new object();
        private static Dictionary<int, Tuple<Key[], VirtualKeyCode[], VirtualKeyCode[]>> _bindings = new Dictionary<int, Tuple<Key[], VirtualKeyCode[], VirtualKeyCode[]>>();
        
        /**
         * Store key codes as virtual key codes.
         */
        static public void RegisterBinding(int keyNumber, HashSet<Key> keys)
        {
            var keysArr = new Key[keys.Count];
            keys.CopyTo(keysArr);
            var binding = MainUtils.ConvertKeys(keysArr);
            lock (_bindingsLock)
            {
                _bindings[keyNumber] = binding;
                var config = new Dictionary<int, Key[]>();
                foreach (var index in _bindings.Keys)
                {
                    config.Add(index, _bindings[index].Item1);
                }
                StoreConfig(config);
            }
        }
        static private void RegisterBindings(Dictionary<int, Key[]> config)
        {
            var bindings = new Dictionary<int, Tuple<Key[], VirtualKeyCode[], VirtualKeyCode[]>>();
            foreach (var keyNumber in config.Keys)
            {
                var keys = config[keyNumber];
                var binding = MainUtils.ConvertKeys(keys);
                bindings[keyNumber] = binding;
            }
            lock (_bindingsLock)
            {
                _bindings = bindings;
            }
        }
        static public bool BindingExists(int index)
        {
            lock (_bindingsLock)
            {
                return _bindings.ContainsKey(index);
            }
        }
        public static Tuple<Key[], VirtualKeyCode[], VirtualKeyCode[]> GetBinding(int keyNumber)
        {
            lock (_bindingsLock)
            {
                return _bindings[keyNumber];
            }
        }
        static public void ClearBindings()
        {
            lock (_bindingsLock)
            {
                _bindings.Clear();
            }
            StoreConfig();
        }
        static public void RemoveBinding(int keyNumber)
        {
            lock (_bindingsLock)
            {
                _bindings.Remove(keyNumber);
                var config = new Dictionary<int, Key[]>();
                foreach (var index in _bindings.Keys)
                {
                    config.Add(index, _bindings[index].Item1);
                }
                StoreConfig(config);
            }
        }
        #endregion

        #region config
        static private string _configName = CONFIG_DEFAULT;

        static public void SetConfigName(string configName)
        {
            CleanConfigName(ref configName);
            _configName = configName;
        }

        static public bool IsDefaultConfig()
        {
            return _configName == CONFIG_DEFAULT;
        }

        static private void CleanConfigName(ref string configName)
        {
            Regex rgx = new Regex(@"[^a-zA-Z0-9\.]");
            var cleaned = rgx.Replace(configName, String.Empty).Trim(new char[] { '.' });
            configName = cleaned == String.Empty ? CONFIG_DEFAULT : cleaned;
        }

        static public void StoreConfig(Dictionary<int, Key[]> config = null, string configName = null)
        {
            if (config == null)
            {
                config = new Dictionary<int, Key[]>();
                lock (_bindingsLock)
                {
                    foreach (var index in _bindings.Keys)
                    {
                        config.Add(index, _bindings[index].Item1);
                    }
                }
            }
            if (configName == null) configName = _configName;
            var jsonString = JsonConvert.SerializeObject(config);
            var configDir = $"{Directory.GetCurrentDirectory()}\\config\\";
            var configFilePath = $"{configDir}{configName}.json";
            if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);
            File.WriteAllText(configFilePath, jsonString);
        }

        static public void DeleteConfig(string configName = null)
        {
            if (configName == null) configName = _configName;
            var configDir = $"{Directory.GetCurrentDirectory()}\\config\\";
            var configFilePath = $"{configDir}{configName}.json";
            if(File.Exists(configFilePath))
            {
                File.Delete(configFilePath);
                _configName = CONFIG_DEFAULT;
            }
        }

        static public Dictionary<int, Key[]> RetrieveConfig(string configName = null)
        {
            if (configName == null) configName = _configName;
            var configDir = $"{Directory.GetCurrentDirectory()}\\config\\";
            var configFilePath = $"{configDir}{configName}.json";
            var jsonString = File.Exists(configFilePath) ? File.ReadAllText(configFilePath) : null;
            if (jsonString != null)
            {
                var config = JsonConvert.DeserializeObject(jsonString, typeof(Dictionary<int, Key[]>)) as Dictionary<int, Key[]>;
                RegisterBindings(config);
                return config;
            }
            return null;
        }
        #endregion

        #region Settings
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
            return (bool)p[propertyName];
        }
        #endregion

    }
}
