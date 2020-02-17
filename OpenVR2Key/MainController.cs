using BOLL7708;
using GregsStack.InputSimulatorStandard;
using GregsStack.InputSimulatorStandard.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Input;
using Valve.VR;

namespace OpenVR2Key
{
    class MainController
    {
        private EasyOpenVRSingleton _ovr = EasyOpenVRSingleton.Instance;
        private InputSimulator _sim = new InputSimulator();

        // Active key registration
        private int _registeringKey = 0;
        private object _registeringElement = null;
        private HashSet<Key> _keys = new HashSet<Key>();
        private HashSet<Key> _keysDown = new HashSet<Key>();

        // Actions
        public Action<bool> StatusUpdateAction { get; set; } = (status) => { Debug.WriteLine("No status action set."); };
        public Action<string> AppUpdateAction { get; set; } = (appId) => { Debug.WriteLine("No appID action set."); };
        public Action<string> KeyTextUpdateAction { get; set; } = (status) => { Debug.WriteLine("No key text action set."); };
        public Action<Dictionary<int, Key[]>> ConfigRetrievedAction { get; set; } = (config) => { Debug.WriteLine("No config loaded."); };

        // Other
        private string _currentApplicationId = "";
        private ulong _inputSourceHandleLeft = 0, _inputSourceHandleRight = 0;
        private ulong _notificationOverlayHandle = 0;
        private NotificationBitmap_t _notificationBitmap = new NotificationBitmap_t();

        public MainController()
        {
        }

        public void Init()
        {
            StatusUpdateAction.Invoke(false);
            AppUpdateAction.Invoke(_currentApplicationId);
            var workerThread = new Thread(WorkerThread);
            workerThread.Start();
        }
        public void SetDebugLogAction(Action<string> action)
        {
            _ovr.SetDebugLogAction(action);
        }

        #region bindings
        public bool ToggleRegisteringKey(int index, object sender, out object activeElement)
        {
            var active = _registeringKey == 0;
            if (active)
            {
                _registeringKey = index;
                _registeringElement = sender;
                _keysDown.Clear();
                _keys.Clear();
                activeElement = sender;
            }
            else
            {
                activeElement = _registeringElement;
                MainModel.RegisterBinding(_registeringKey, _keys); // TODO: Should only save existing configs
                _registeringKey = 0;
                _registeringElement = null;
            }
            return active;
        }

        // TODO: Should be used to interrupt recording keys
        private void StopRegisteringKeys()
        {
            // TODO: We need to be able to affect the GUI here to turn off recording.
            _keysDown.Clear();
            _keys.Clear();
            _registeringKey = 0;
            _registeringElement = null;
        }

        // Add incoming keys to the current binding
        public void OnKeyDown(Key key)
        {
            if (MainUtils.MatchVirtualKey(key) != null)
            {
                if (_keysDown.Count == 0) _keys.Clear();
                _keys.Add(key);
                _keysDown.Add(key);
                UpdateCurrentObject();
            }
        }
        public void OnKeyUp(Key key)
        {
            if (key == Key.RightAlt) _keysDown.Remove(Key.LeftCtrl); // Because AltGr records as RightAlt+LeftCtrl
            _keysDown.Remove(key);
            UpdateCurrentObject();
        }

        // Send text to UI to update label
        private void UpdateCurrentObject()
        {
            KeyTextUpdateAction.Invoke(GetKeysLabel());
        }

        // Generate label text from keys
        public string GetKeysLabel(Key[] keys = null)
        {
            if (keys == null)
            {
                keys = new Key[_keys.Count];
                _keys.CopyTo(keys);
            }
            List<string> result = new List<string>();
            foreach (Key k in keys)
            {
                result.Add(k.ToString());
            }
            return String.Join(" + ", result.ToArray());
        }

        #endregion

        #region worker
        private void WorkerThread()
        {
            Thread.CurrentThread.IsBackground = true;
            bool initComplete = false;
            while (true)
            {
                Thread.Sleep(10);
                if (_ovr.IsInitialized())
                {
                    if (!initComplete)
                    {
                        _ovr.LoadAppManifest("./app.vrmanifest");
                        _ovr.LoadActionManifest("./actions.json");
                        RegisterActions();
                        UpdateAppId();
                        StatusUpdateAction.Invoke(true);
                        UpdateInputSourceHandles();
                        _notificationOverlayHandle = _ovr.InitNotificationOverlay("OpenVR2Key");
                        /* // TODO: Bitmap loads but it crashes on trying to use it for the notification. Cannot read from protected memory.
                        var bitmapPath = $"{Directory.GetCurrentDirectory()}\\icon.png";
                        notificationBitmap = EasyOpenVRSingleton.BitmapUtils.NotificationBitmapFromBitmap(new System.Drawing.Bitmap(bitmapPath));
                        */
                        initComplete = true;
                    }

                    var vrEvents = _ovr.GetNewEvents();
                    foreach (var e in vrEvents)
                    {
                        var message = Enum.GetName(typeof(EVREventType), e.eventType);
                        Debug.WriteLine(message);

                        switch ((EVREventType)e.eventType)
                        {
                            case EVREventType.VREvent_Quit:
                                initComplete = false;
                                _ovr.AcknowledgeShutdown();
                                _ovr.Shutdown();
                                StatusUpdateAction.Invoke(false);
                                break;
                            case EVREventType.VREvent_SceneApplicationChanged:
                                UpdateAppId();
                                break;
                            case EVREventType.VREvent_TrackedDeviceRoleChanged:
                            case EVREventType.VREvent_TrackedDeviceUpdated:
                            case EVREventType.VREvent_TrackedDeviceActivated:
                                UpdateInputSourceHandles();
                                break;
                        }
                    }

                    _ovr.UpdateActionStates(new ulong[] {
                        _inputSourceHandleLeft,
                        _inputSourceHandleRight
                    });
                }
                else
                {
                    _ovr.Init();
                    Thread.Sleep(1000);
                }
            }
        }

        // Controller roles have updated, refresh controller handles
        private void UpdateInputSourceHandles()
        {
            _inputSourceHandleLeft = _ovr.GetInputSourceHandle("/user/hand/left");
            _inputSourceHandleRight = _ovr.GetInputSourceHandle("/user/hand/right");
        }

        // New app is running, distribute new app ID
        private void UpdateAppId()
        {
            StopRegisteringKeys();
            _currentApplicationId = _ovr.GetRunningApplicationId();
            if (_currentApplicationId == String.Empty) _currentApplicationId = MainModel.CONFIG_DEFAULT;
            AppUpdateAction.Invoke(_currentApplicationId);
            var config = MainModel.RetrieveConfig();
            if (config != null)
            {
                Debug.WriteLine($"Config for {_currentApplicationId} found.");
                ConfigRetrievedAction.Invoke(config);
            }
        }
        #endregion

        #region vr_input

        // Register all actions with the input system
        private void RegisterActions()
        {
            _ovr.RegisterActionSet("/actions/default");
            for (var i = 1; i <= 32; i++)
            {
                int localI = i;
                _ovr.RegisterDigitalAction($"/actions/default/in/key{i}", (data, handle) => { OnAction(localI, data, handle); });
            }
        }

        // Action was triggered, handle it
        private void OnAction(int index, InputDigitalActionData_t data, ulong inputSourceHandle)
        {
            var inputName = inputSourceHandle == _inputSourceHandleLeft ? "Left" :
                inputSourceHandle == _inputSourceHandleRight ? "Right" :
                "N/A";
            Debug.WriteLine($"Key{index} - {inputName} : " + (data.bState ? "PRESSED" : "RELEASED"));
            if (MainModel.BindingExists(index))
            {
                if (data.bState)
                {
                    if (MainModel.LoadSetting(MainModel.Setting.Haptic))
                    {
                        if (inputSourceHandle == _inputSourceHandleLeft) _ovr.TriggerHapticPulseInController(ETrackedControllerRole.LeftHand);
                        if (inputSourceHandle == _inputSourceHandleRight) _ovr.TriggerHapticPulseInController(ETrackedControllerRole.RightHand);
                    }
                    if (MainModel.LoadSetting(MainModel.Setting.Notification))
                    {
                        _ovr.EnqueueNotification(_notificationOverlayHandle, $"{inputName} Controller activated: Key {index}", _notificationBitmap);
                    }
                }
                var binding = MainModel.GetBinding(index);
                SimulateKeyPress(data, binding);
            }
        }
        #endregion

        #region keyboard_out

        // Simulate a keyboard press
        private void SimulateKeyPress(InputDigitalActionData_t data, Tuple<Key[], VirtualKeyCode[], VirtualKeyCode[]> binding)
        {
            if (data.bState)
            {
                foreach (var vk in binding.Item2) _sim.Keyboard.KeyDown(vk);
                foreach (var vk in binding.Item3) _sim.Keyboard.KeyDown(vk);
            }
            else
            {
                foreach (var vk in binding.Item3) _sim.Keyboard.KeyUp(vk);
                foreach (var vk in binding.Item2) _sim.Keyboard.KeyUp(vk);
            }
        }
        #endregion
    }
}
