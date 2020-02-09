using BOLL7708;
using GregsStack.InputSimulatorStandard;
using GregsStack.InputSimulatorStandard.Native;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Input;
using Valve.VR;
using System.Windows.Media;
using System.Windows.Threading;

namespace OpenVR2Key
{
    class MainController
    {
        private EasyOpenVRSingleton ovr = EasyOpenVRSingleton.Instance;
        private InputSimulator sim = new InputSimulator();

        private int registeringKey = 0;
        private TextBlock registeringElement = null;
        private HashSet<Key> keys = new HashSet<Key>();
        private HashSet<Key> keysDown = new HashSet<Key>();
        private Dictionary<int, Tuple<VirtualKeyCode[], VirtualKeyCode[]>> bindings = new Dictionary<int, Tuple<VirtualKeyCode[], VirtualKeyCode[]>>();

        private readonly object bindingsLock = new object();
        public Action<bool> statusUpdateAction { get; set; } = (status) => { Debug.WriteLine("No status action set."); };
        public Action<string> appUpdateAction { get; set; } = (appId) => { Debug.WriteLine("No appID action set."); };
        private string currentApplicationId = "";

        public MainController()
        {
        }

        public void Init()
        {
            statusUpdateAction.Invoke(false);
            appUpdateAction.Invoke(currentApplicationId);
            var workerThread = new Thread(WorkerThread);
            workerThread.Start();
        }

        #region bindings
        public void ToggleRegisteringKey(TextBlock sender)
        {
            if (registeringKey == 0)
            {
                registeringKey = 1;
                registeringElement = sender;
                keysDown.Clear();
                keys.Clear();
            }
            else
            {
                RegisterKeyBinding(registeringKey, keys);
                registeringKey = 0;
                registeringElement = null;
            }
        }
        public void OnKeyDown(Key key)
        {
            if (MainUtils.MatchVirtualKey(key) != null)
            {
                if (keysDown.Count == 0) keys.Clear();
                keys.Add(key);
                keysDown.Add(key);
                UpdateCurrentObject();
            }
        }
        public void OnKeyUp(Key key)
        {
            if (key == Key.RightAlt) keysDown.Remove(Key.LeftCtrl); // Because AltGr records as RightAlt+LeftCtrl
            keysDown.Remove(key);
            UpdateCurrentObject();
        }
        private void UpdateCurrentObject()
        {
            if (registeringElement != null) registeringElement.Text = GetKeysLabel();
        }

        private string GetKeysLabel()
        {
            List<string> result = new List<string>();
            foreach (Key k in keys)
            {
                result.Add(k.ToString());
            }
            return String.Join(" + ", result.ToArray());
        }

        /**
         * Store key codes as virtual key codes.
         */
        public void RegisterKeyBinding(int keyNumber, HashSet<Key> keys)
        {
            var keysArr = new Key[keys.Count];
            keys.CopyTo(keysArr);
            var binding = MainUtils.ConvertKeys(keysArr);
            lock (bindingsLock)
            {
                bindings[keyNumber] = binding;
            }
        }

        public void RegisterKeyBindings(Dictionary<int, Tuple<VirtualKeyCode[], VirtualKeyCode[]>> bindings)
        {
            lock (bindingsLock)
            {
                this.bindings = bindings;
            }
        }

        public void ClearBindings()
        {
            lock (bindingsLock)
            {
                bindings.Clear();
            }
        }


        #endregion

        private void WorkerThread()
        {
            Thread.CurrentThread.IsBackground = true;
            bool initComplete = false;
            while (true)
            {
                Thread.Sleep(10);
                if (ovr.IsInitialized())
                {
                    if (!initComplete)
                    {
                        ovr.LoadAppManifest("./app.vrmanifest");
                        ovr.LoadActionManifest("./actions.json");
                        RegisterActions();
                        currentApplicationId = ovr.GetRunningApplicationId();
                        appUpdateAction.Invoke(currentApplicationId);
                        statusUpdateAction.Invoke(true);
                        initComplete = true;
                    }

                    var vrEvents = ovr.GetNewEvents();
                    foreach (var e in vrEvents)
                    {
                        var message = Enum.GetName(typeof(EVREventType), e.eventType);
                        Debug.WriteLine(message);

                        switch((EVREventType) e.eventType)
                        {
                            case EVREventType.VREvent_Quit:
                                initComplete = false;
                                ovr.AcknowledgeShutdown();
                                ovr.Shutdown();
                                statusUpdateAction.Invoke(false);
                                break;
                            case EVREventType.VREvent_SceneApplicationChanged:
                                currentApplicationId = ovr.GetRunningApplicationId();
                                appUpdateAction.Invoke(currentApplicationId);
                                break;
                        }
                    }

                    ovr.UpdateActionStates();
                }
                else
                {
                    ovr.Init();
                    Thread.Sleep(1000);
                }
            }
        }

        #region vr_input
        private void RegisterActions()
        {
            ovr.RegisterActionSet("/actions/default");
            ovr.RegisterDigitalAction("/actions/default/in/key1", (data) => { OnAction(1, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key2", (data) => { OnAction(2, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key3", (data) => { OnAction(3, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key4", (data) => { OnAction(4, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key5", (data) => { OnAction(5, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key6", (data) => { OnAction(6, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key7", (data) => { OnAction(7, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key8", (data) => { OnAction(8, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key9", (data) => { OnAction(9, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key10", (data) => { OnAction(10, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key11", (data) => { OnAction(11, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key12", (data) => { OnAction(12, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key13", (data) => { OnAction(13, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key14", (data) => { OnAction(14, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key15", (data) => { OnAction(15, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key16", (data) => { OnAction(16, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key17", (data) => { OnAction(17, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key18", (data) => { OnAction(18, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key19", (data) => { OnAction(19, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key20", (data) => { OnAction(20, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key21", (data) => { OnAction(21, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key22", (data) => { OnAction(22, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key23", (data) => { OnAction(23, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key24", (data) => { OnAction(24, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key25", (data) => { OnAction(25, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key26", (data) => { OnAction(26, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key27", (data) => { OnAction(27, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key28", (data) => { OnAction(28, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key29", (data) => { OnAction(29, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key30", (data) => { OnAction(30, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key31", (data) => { OnAction(31, data); });
            ovr.RegisterDigitalAction("/actions/default/in/key32", (data) => { OnAction(32, data); });
        }

        private void OnAction(int index, InputDigitalActionData_t data)
        {
            Debug.WriteLine($"Key{index} - " + (data.bState ? "PRESSED" : "RELEASED"));
            lock (bindingsLock)
            {
                if (bindings.ContainsKey(index))
                {
                    var binding = bindings[index];
                    SimulateKeyPress(data, binding);
                }
            }
        }
        #endregion

        #region keyboard_out
        private void SimulateKeyPress(InputDigitalActionData_t data, Tuple<VirtualKeyCode[], VirtualKeyCode[]> binding)
        {
            if (data.bState)
            {
                foreach (var vk in binding.Item1) sim.Keyboard.KeyDown(vk);
                foreach (var vk in binding.Item2) sim.Keyboard.KeyDown(vk);
            }
            else
            {
                foreach (var vk in binding.Item2) sim.Keyboard.KeyUp(vk);
                foreach (var vk in binding.Item1) sim.Keyboard.KeyUp(vk);
            }
        }
        #endregion

        public void TestStuff()
        {
            var values = Enum.GetValues(typeof(ETrackedDeviceProperty));
            foreach (ETrackedDeviceProperty i in values)
            {
                var name = Enum.GetName(typeof(ETrackedDeviceProperty), i);
                if (name.Contains("_String")) ovr.GetStringTrackedDeviceProperty(0, i);
                else if (name.Contains("_Float")) ovr.GetFloatTrackedDeviceProperty(0, i);
                else if (name.Contains("_Bool")) ovr.GetBooleanTrackedDeviceProperty(0, i);
            }
        }
    }
}
