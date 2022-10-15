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
        private static Dictionary<string, Tuple<Key[], VirtualKeyCode[], VirtualKeyCode[]>> _bindings = new Dictionary<string, Tuple<Key[], VirtualKeyCode[], VirtualKeyCode[]>>();
        
        /**
         * Store key codes as virtual key codes.
         */
        static public void RegisterBinding(string actionKey, HashSet<Key> keys)
        {
            var keysArr = new Key[keys.Count];
            keys.CopyTo(keysArr);
            var binding = MainUtils.ConvertKeys(keysArr);
            lock (_bindingsLock)
            {
                _bindings[actionKey] = binding;
                var config = new Dictionary<string, Key[]>();
                foreach (var key in _bindings.Keys)
                {
                    config.Add(key, _bindings[key].Item1);
                }
                StoreConfig(config);
            }
        }
        static private void RegisterBindings(Dictionary<string, Key[]> config)
        {
            var bindings = new Dictionary<string, Tuple<Key[], VirtualKeyCode[], VirtualKeyCode[]>>();
            foreach (var key in config.Keys)
            {
                var keys = config[key];
                var binding = MainUtils.ConvertKeys(keys);
                bindings[key] = binding;
            }
            lock (_bindingsLock)
            {
                _bindings = bindings;
            }
        }
        static public bool BindingExists(string actionKey)
        {
            lock (_bindingsLock)
            {
                return _bindings.ContainsKey(actionKey);
            }
        }
        public static Tuple<Key[], VirtualKeyCode[], VirtualKeyCode[]> GetBinding(string actionkey)
        {
            lock (_bindingsLock)
            {
                return _bindings[actionkey];
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
        static public void RemoveBinding(string actionKey)
        {
            lock (_bindingsLock)
            {
                _bindings.Remove(actionKey);
                var config = new Dictionary<string, Key[]>();
                foreach (var key in _bindings.Keys)
                {
                    config.Add(key, _bindings[key].Item1);
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

        static public string GetConfigFolderPath()
        {
            return $"{Directory.GetCurrentDirectory()}\\config\\";
        }

        static public void StoreConfig(Dictionary<string, Key[]> config = null, string configName = null)
        {
            if (config == null)
            {
                config = new Dictionary<string, Key[]>();
                lock (_bindingsLock)
                {
                    foreach (var key in _bindings.Keys)
                    {
                        config.Add(key, _bindings[key].Item1);
                    }
                }
            }
            if (configName == null) configName = _configName;
            var jsonString = JsonConvert.SerializeObject(config);
            var configDir = GetConfigFolderPath();
            var configFilePath = $"{configDir}{configName}.json";
            if (!Directory.Exists(configDir)) Directory.CreateDirectory(configDir);
            File.WriteAllText(configFilePath, jsonString);
        }

        static public void DeleteConfig(string configName = null)
        {
            if (configName == null) configName = _configName;
            var configDir = GetConfigFolderPath();
            var configFilePath = $"{configDir}{configName}.json";
            if(File.Exists(configFilePath))
            {
                File.Delete(configFilePath);
                _configName = CONFIG_DEFAULT;
            }
        }

        static public Dictionary<string, Key[]> RetrieveConfig(string configName = null)
        {
            if (configName == null) configName = _configName;
            CleanConfigName(ref configName);
            var configDir = $"{Directory.GetCurrentDirectory()}\\config\\";
            var configFilePath = $"{configDir}{configName}.json";
            var jsonString = File.Exists(configFilePath) ? File.ReadAllText(configFilePath) : null;
            if (jsonString != null)
            {
                var config = JsonConvert.DeserializeObject(jsonString, typeof(Dictionary<string, Key[]>)) as Dictionary<string, Key[]>;
                RegisterBindings(config);
                return config;
            }
            return null;
        }
        #endregion

        #region Settings
        public enum Setting
        {
            Minimize, Tray, Notification, Haptic, ExitWithSteam
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

        static public string GetVersion()
        {
            return (string)Properties.Resources.Version;
        }
        #endregion

    }
}
