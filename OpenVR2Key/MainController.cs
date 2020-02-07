using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BOLL7708;
using Valve.VR;
using WindowsInput;
using System.Windows.Input;

namespace OpenVR2Key
{
    class MainController
    {
        private EasyOpenVRSingleton ovr = EasyOpenVRSingleton.Instance;
        private Dictionary<int, HashSet<Key>> bindings = new Dictionary<int, HashSet<Key>>();
        private readonly object bindingsLock = new object();
        public MainController()
        {
            var workerThread = new Thread(WorkerThread);
            workerThread.Start();
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
                } else
                {
                    ovr.Init();
                    Thread.Sleep(1000);
                }
            }
        }

        private void RegisterActions()
        {
            ovr.RegisterActionSet("/actions/default");
            ovr.RegisterDigitalAction("/actions/default/in/enable_chord", (data) => { Pressed("Key0", data); });
            ovr.RegisterDigitalAction("/actions/default/in/key1", (data) => { DoStuff(data); });
            ovr.RegisterDigitalAction("/actions/default/in/key2", (data) => { Pressed("Key2", data); });
            ovr.RegisterDigitalAction("/actions/default/in/key3", (data) => { Pressed("Key3", data); });
            ovr.RegisterDigitalAction("/actions/default/in/key4", (data) => { Pressed("Key4", data); });
            ovr.RegisterDigitalAction("/actions/default/in/key5", (data) => { Pressed("Key5", data); });
            ovr.RegisterDigitalAction("/actions/default/in/key6", (data) => { Pressed("Key6", data); });
            ovr.RegisterDigitalAction("/actions/default/in/key7", (data) => { Pressed("Key7", data); });
            ovr.RegisterDigitalAction("/actions/default/in/key8", (data) => { Pressed("Key8", data); });
            ovr.RegisterDigitalAction("/actions/default/in/key9", (data) => { Pressed("Key9", data); });
            ovr.RegisterDigitalAction("/actions/default/in/key10", (data) => { Pressed("Key10", data); });
            ovr.RegisterDigitalAction("/actions/default/in/key11", (data) => { Pressed("Key11", data); });
            ovr.RegisterDigitalAction("/actions/default/in/key12", (data) => { Pressed("Key12", data); });
            ovr.RegisterDigitalAction("/actions/default/in/key13", (data) => { Pressed("Key13", data); });
            ovr.RegisterDigitalAction("/actions/default/in/key14", (data) => { Pressed("Key14", data); });
            ovr.RegisterDigitalAction("/actions/default/in/key15", (data) => { Pressed("Key15", data); });
            ovr.RegisterDigitalAction("/actions/default/in/key16", (data) => { Pressed("Key16", data); });
        }

        private void Pressed(string label, InputDigitalActionData_t data)
        {
            Debug.WriteLine($"{label} - "+(data.bState ? "PRESSED" : "RELEASED"));
            // OpenVR.System.TriggerHapticPulse(ovr.GetIndexForControllerRole(ETrackedControllerRole.LeftHand), 0, 10000); // This works: https://github.com/ValveSoftware/openvr/wiki/IVRSystem::TriggerHapticPulse
            // ovr.GetRunningApplicationId();
        }

        private void DoStuff(InputDigitalActionData_t data)
        {
            var sim = new InputSimulator();
            if(data.bState)
            {
                sim.Keyboard.KeyDown(WindowsInput.Native.VirtualKeyCode.F8);
            } else
            {
                sim.Keyboard.KeyUp(WindowsInput.Native.VirtualKeyCode.F8);
            }
        }

        public void RegisterKeyBinding(int keyNumber, HashSet<Key> keys)
        {
            lock(bindingsLock)
            {
                bindings[keyNumber] = keys;
            }
        }
    }
}
