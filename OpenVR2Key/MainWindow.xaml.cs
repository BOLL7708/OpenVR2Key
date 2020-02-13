using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private MainController _controller;
        private List<BindingItem> _items = new List<BindingItem>();
        public MainWindow()
        {
            InitializeComponent();
            _controller = new MainController
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
            _controller.SetDebugLogAction((message) =>{
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
            _controller.Init();
            InitList();
        }

        private void InitList()
        {
            for(var i=1; i<=32; i++)
            {
                _items.Add(new BindingItem() {
                    Index = i,
                    Label = $"Key {i}",
                    Text = "Load from config."
                });
            }
            ItemsControl_Bindings.ItemsSource = _items;
        }

        public class BindingItem
        {
            public int Index { get; set; }
            public string Label { get; set; }
            public string Text { get; set; }
        }

        #region events
        protected override void OnKeyDown(KeyEventArgs e)
        {
            _controller.OnKeyDown(e.Key);

            // TODO: Doesn't seem like this is preventing the ALT behavior at all. https://stackoverflow.com/a/2277355
            if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt) e.Handled = true;
            else base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            _controller.OnKeyUp(e.Key);

            // TODO: Fix Alt behavior
            if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt) e.Handled = true;
            else base.OnKeyUp(e);
        }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // TODO: Doesn't even register? Supposed to fix enter/space triggering buttons: https://stackoverflow.com/a/17977670
            if (e.Key == Key.Enter || e.Key == Key.Space)
            {
                Debug.WriteLine($"Should block this?! {e.Key}");
                e.Handled = true;
            }
        }
        #endregion

        #region buttons
        private void Button_AppBinding_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Button_ClearAll_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_RecordSave_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var dataItem = button.DataContext as BindingItem;
            var active = _controller.ToggleRegisteringKey(dataItem.Index, button, out Button activeButton);
            activeButton.Foreground = active ? Brushes.Tomato: Brushes.Black;
            activeButton.BorderBrush = active ? Brushes.Tomato : Brushes.Gray;
        }

        private void Button_ClearCancel_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var dataItem = button.DataContext as BindingItem;
            _controller.RemoveBinding(dataItem.Index);
            DockPanel sp = VisualTreeHelper.GetParent(button) as DockPanel;
            Button bindingButton = sp.Children[2] as Button;
            bindingButton.Content = "[Not configured]";
        }
        #endregion
    }
}
