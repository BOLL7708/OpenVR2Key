using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using WindowsInput;

namespace OpenVR2Key
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var controller = new MainController();
        }
        
        private int registeringKey = 0;
        private object registeringObject = null;
        private HashSet<Key> keys = new HashSet<Key>();
        private HashSet<Key> keysDown = new HashSet<Key>();
        private InputSimulator sim = new InputSimulator();

        // TODO: Use this for every input
        private void TextBlock_Test_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine(((TextBlock)sender).Name); // TODO: Parse the key number from the name of the element

            if(registeringKey == 0)
            {
                registeringKey = 1;
                registeringObject = sender;
            } else
            {
                registeringKey = 0;
                registeringObject = null;
                // TODO: Send the binding to MainController, and update config file?
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (MainUtils.MatchVirtualKey(e.Key) != 0)
            {
                if (keysDown.Count == 0) keys.Clear();
                keys.Add(e.Key);
                keysDown.Add(e.Key);
                UpdateCurrentObject();
            }

            // Doesn't seem like this is preventing the ALT behavior at all. https://stackoverflow.com/a/2277355
            if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt) e.Handled = true;
            else base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            keysDown.Remove(e.Key);
            UpdateCurrentObject();

            if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt) e.Handled = true;
            else base.OnKeyUp(e);
        }

        private void UpdateCurrentObject()
        {
            if(registeringObject != null) ((TextBlock)registeringObject).Text = GetKeysLabel();
        }

        private string GetKeysLabel()
        {
            List<string> result = new List<string>();
            foreach(Key k in keys)
            {
                result.Add(k.ToString());
            }
            return String.Join(" + ", result.ToArray<string>());
        }
    }
}
