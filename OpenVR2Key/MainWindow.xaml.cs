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
        private object activeElement;

        public MainWindow()
        {
            InitializeComponent();
            _controller = new MainController
            {
                statusUpdateAction = (connected) =>
                {
                    var message = connected ? "Connected" : "Disconnected";
                    var color = connected ? Brushes.OliveDrab : Brushes.Tomato;
                    Dispatcher.Invoke(() =>
                    {
                        Label_OpenVR.Content = message;
                        Label_OpenVR.Background = color;
                    });
                },
                appUpdateAction = (appId) =>
                {
                    var color = Brushes.OliveDrab;
                    if (appId.Length == 0)
                    {
                        color = Brushes.Tomato;
                        appId = "None";
                    }
                    Dispatcher.Invoke(() =>
                    {
                        Label_Application.Content = appId;
                        Label_Application.Background = color;
                    });
                },
                keyTextUpdateAction = (keyText) => {
                    Dispatcher.Invoke(() =>
                    {
                        if(activeElement != null) (activeElement as Label).Content = keyText;
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
            InitSettings();
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

        #region bindings
        public class BindingItem
        {
            public int Index { get; set; }
            public string Label { get; set; }
            public string Text { get; set; }
            public BindingState State { get; set; }
        }

        public enum BindingState
        {
            Unset, Set, Recording
        }
        #endregion

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
        #endregion

        #region actions
        private void Button_AppBinding_Click(object sender, RoutedEventArgs e)
        {
            
        }

        private void Button_ClearAll_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Label_RecordSave_Click(object sender, MouseButtonEventArgs e)
        {
            var element = sender as Label;
            var dataItem = element.DataContext as BindingItem;
            var active = _controller.ToggleRegisteringKey(dataItem.Index, element, out object activeElement);
            var label = activeElement as Label;
            label.Foreground = active ? Brushes.Tomato: Brushes.Black;
            label.BorderBrush = active ? Brushes.Tomato : Brushes.Gray;
            label.Background = active ? Brushes.LightPink : Brushes.Olive;
            if (active) this.activeElement = activeElement;
        }

        private void Button_ClearCancel_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var dataItem = button.DataContext as BindingItem;
            _controller.RemoveBinding(dataItem.Index);
            DockPanel sp = VisualTreeHelper.GetParent(button) as DockPanel;
            var element = sp.Children[2] as Label;
            element.Content = "[Not configured]";
        }
        #endregion

        #region settings
        private void InitSettings()
        {
            CheckBox_Minimize.IsChecked = MainModel.LoadSetting(MainModel.Setting.Minimize);
            CheckBox_Tray.IsChecked = MainModel.LoadSetting(MainModel.Setting.Tray);
            CheckBox_DebugNotifications.IsChecked = MainModel.LoadSetting(MainModel.Setting.Notification);
            CheckBox_HapticFeedback.IsChecked = MainModel.LoadSetting(MainModel.Setting.Haptic);
        }
        private bool CheckboxValue(RoutedEventArgs e)
        {
            var name = e.RoutedEvent.Name;
            return name == "Checked";
        }

        private void CheckBox_Minimize_Checked(object sender, RoutedEventArgs e)
        {
            MainModel.UpdateSetting(MainModel.Setting.Minimize, CheckboxValue(e));
        }
        
        private void CheckBox_Tray_Checked(object sender, RoutedEventArgs e)
        {
            MainModel.UpdateSetting(MainModel.Setting.Tray, CheckboxValue(e));
        }

        private void CheckBox_DebugNotifications_Checked(object sender, RoutedEventArgs e)
        {
            MainModel.UpdateSetting(MainModel.Setting.Notification, CheckboxValue(e));
        }

        private void CheckBox_HapticFeedback_Checked(object sender, RoutedEventArgs e)
        {
            MainModel.UpdateSetting(MainModel.Setting.Haptic, CheckboxValue(e));
        }
        #endregion
    }
}
