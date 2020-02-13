using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace OpenVR2Key
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainController controller;
        public MainWindow()
        {
            InitializeComponent();
            controller = new MainController
            {
                statusUpdateAction = (connected) =>
                {
                    var message = connected ? "Connected" : "Disconnected";
                    var color = connected ? Colors.OliveDrab : Colors.Tomato;
                    Dispatcher.Invoke(() =>
                    {
                        Label_OpenVR.Content = message;
                        Label_OpenVR.Background = new SolidColorBrush(color);
                    });
                },
                appUpdateAction = (appId) =>
                {
                    var color = Colors.OliveDrab;
                    if (appId.Length == 0)
                    {
                        color = Colors.Tomato;
                        appId = "None";
                    }
                    Dispatcher.Invoke(() =>
                    {
                        Label_Application.Content = appId;
                        Label_Application.Background = new SolidColorBrush(color);
                    });
                }
            };
            controller.SetDebugLogAction((message) =>{
                Dispatcher.Invoke(() =>
                {
                    var time = DateTime.Now.ToString("HH:mm:ss");
                    var oldLog = TextBox_Log.Text;
                    var lines = oldLog.Split('\n');
                    Array.Resize(ref lines, 3);
                    var newLog = String.Join("\n", lines);
                    TextBox_Log.Text = $"{time}: {message}\n{newLog}";
                });
            });
            controller.Init();
        }

        // TODO: Use this for every input
        private void TextBlock_Test_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine(((TextBlock)sender).Name); // TODO: Parse the key number from the name of the element? Or use the tag or whatever.
            controller.ToggleRegisteringKey((TextBlock)sender);
        }

        #region events
        protected override void OnKeyDown(KeyEventArgs e)
        {
            controller.OnKeyDown(e.Key);

            // TODO: Doesn't seem like this is preventing the ALT behavior at all. https://stackoverflow.com/a/2277355
            if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt) e.Handled = true;
            else base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            controller.OnKeyUp(e.Key);

            // TODO: Fix Alt behavior
            if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt) e.Handled = true;
            else base.OnKeyUp(e);
        }
        #endregion
    }
}
