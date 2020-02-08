using BOLL7708;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows.Input;
using Valve.VR;
using WindowsInput;
using WindowsInput.Native;

namespace OpenVR2Key
{
    class MainController
    {
        private EasyOpenVRSingleton ovr = EasyOpenVRSingleton.Instance;
        private Dictionary<int, Tuple<VirtualKeyCode[], VirtualKeyCode[]>> bindings = new Dictionary<int, Tuple<VirtualKeyCode[], VirtualKeyCode[]>>();
        private readonly object bindingsLock = new object();
        private InputSimulator sim = new InputSimulator();
        public MainController()
        {
            var workerThread = new Thread(WorkerThread);
            workerThread.Start();
        }

        /**
         * Store incoming key codes as virtual key codes.
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
            lock(bindingsLock)
            {
                this.bindings = bindings;
            }
        }

        public void ClearBindings()
        {
            lock(bindingsLock)
            {
                bindings.Clear();
            }
        }

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
                        initComplete = true;
                    }

                    var vrEvents = ovr.GetNewEvents();
                    foreach (var e in vrEvents)
                    {
                        var message = Enum.GetName(typeof(EVREventType), e.eventType);
                        Debug.WriteLine(message);
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
            // OpenVR.System.TriggerHapticPulse(ovr.GetIndexForControllerRole(ETrackedControllerRole.LeftHand), 0, 10000); // This works: https://github.com/ValveSoftware/openvr/wiki/IVRSystem::TriggerHapticPulse
            lock(bindingsLock)
            {
                if(bindings.ContainsKey(index))
                {
                    var binding = bindings[index];
                    SimulateKeyPress(data, binding);
                }
            }
        }

        private void SimulateKeyPress(InputDigitalActionData_t data, Tuple<VirtualKeyCode[], VirtualKeyCode[]> binding)
        {
            // TODO: I don't seem to get modifiers+key to work with this nor .ModifiedKeyStroke
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

        public void TestStuff()
        {
            var values = Enum.GetValues(typeof(ETrackedDeviceProperty));
            foreach(ETrackedDeviceProperty i in values)
            {
                var name = Enum.GetName(typeof(ETrackedDeviceProperty), i);
                if (name.Contains("_String")) ovr.GetStringTrackedDeviceProperty(0, i);
                else if (name.Contains("_Float")) ovr.GetFloatTrackedDeviceProperty(0, i);
                else if (name.Contains("_Bool")) ovr.GetBooleanTrackedDeviceProperty(0, i);
            }
        }
    }
}
