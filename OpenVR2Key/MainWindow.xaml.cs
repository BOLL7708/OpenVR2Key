using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.Versioning;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using EasyFramework;
using Brushes = System.Windows.Media.Brushes;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;

namespace OpenVR2Key
{
    [SupportedOSPlatform("windows7.0")]
    public partial class MainWindow : Window
    {
        private static Mutex _mutex = null;
        private readonly static string DEFAULT_KEY_LABEL = "Unbound: Click to bind keys to simulate";
        private MainController _controller;
        private List<BindingItem> _items = new List<BindingItem>();
        private object _activeElement;
        private string _currentlyRunningAppId = MainModel.CONFIG_DEFAULT;
        private HashSet<string> _activeKeys = new HashSet<string>();
        private bool _initDone = false;
        private bool _dashboardIsVisible = false;
        public MainWindow()
        {
            InitWindow();
            InitializeComponent();
            Title = Properties.Resources.AppName;

            // Prevent multiple instances
            WindowUtils.CheckIfAlreadyRunning(Properties.Resources.AppName);
            
            // Tray icon
            var icon = Properties.Resources.icon.Clone() as Icon;
            WindowUtils.CreateTrayIcon(this, icon, Properties.Resources.AppName, Properties.Resources.Version);

            _controller = new MainController
            {
                // Reports on the status of OpenVR
                StatusUpdateAction = (connected) =>
                {
                    Debug.WriteLine($"Status Update Action: connected={connected}");
                    var message = connected ? "Connected" : "Disconnected";
                    var color = connected ? Brushes.OliveDrab : Brushes.Tomato;
                    Dispatcher.Invoke(() =>
                    {
                        LabelOpenVr.Content = message;
                        LabelOpenVr.Background = color;
                        ButtonLaunchBindings.IsEnabled = connected;
                        if (!connected && _initDone && MainModel.LoadSetting(MainModel.Setting.ExitWithSteam)) {
                            WindowUtils.DestroyTrayIcon();
                            Application.Current.Shutdown();
                        }
                    });
                },

                // Triggered when a new scene app is detected
                AppUpdateAction = (appId) =>
                {
                    Debug.WriteLine($"App Update Action: appId={appId}");
                    _currentlyRunningAppId = appId;
                    var color = Brushes.OliveDrab;
                    if (appId == MainModel.CONFIG_DEFAULT)
                    {
                        color = Brushes.Gray;
                    }
                    var appIdFixed = appId.Replace("_", "__"); // Single underscores makes underlined chars
                    Dispatcher.Invoke(() =>
                    {
                        Debug.WriteLine($"Setting AppID to: {appId}");
                        LabelApplication.Content = appIdFixed;
                        LabelApplication.Background = color;
                    });
                },

                // We should update the text on the current binding we are recording
                KeyTextUpdateAction = (keyText, cancel) =>
                {
                    Debug.WriteLine($"Key Text Update Action: keyText={keyText}");
                    Dispatcher.Invoke(() =>
                    {
                        if (_activeElement != null)
                        {
                            (_activeElement as Label).Content = keyText;
                            if (cancel) UpdateLabel(_activeElement as Label, false);
                        }
                    });
                },

                // We have loaded a config
                ConfigRetrievedAction = (config, forceButtonOff) =>
                {
                    var loaded = config != null;
                    if (loaded) Debug.WriteLine($"Config Retrieved Action: count()={config.Count}");
                    Dispatcher.Invoke(() =>
                    {
                        if (loaded) InitList(config);
                        UpdateConfigButton(loaded, forceButtonOff);
                    });
                },

                KeyActivatedAction = (key, on) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        if (!_dashboardIsVisible) {
                            if (on) _activeKeys.Add(key);
                            else _activeKeys.Remove(key);
                            if (_activeKeys.Count > 0) LabelKeys.Content = string.Join(", ", _activeKeys);
                            else LabelKeys.Content = "None";
                            LabelKeys.ToolTip = "";
                            LabelKeys.Background = Brushes.Gray;
                        }
                    });
                },

                // Dashboard Visible
                DashboardVisibleAction = (visible) => {
                    Dispatcher.Invoke(() =>
                    {
                        _dashboardIsVisible = visible;
                        if (visible)
                        {
                            LabelKeys.Content = "Blocked";
                            LabelKeys.ToolTip = "The SteamVR Dashboard is visible which will block input from this application.";
                            LabelKeys.Background = Brushes.Tomato;
                        }
                        else
                        {
                            LabelKeys.Content = "Unblocked";
                            LabelKeys.ToolTip = "";
                            LabelKeys.Background = Brushes.Gray;
                        }
                    });
                }
            };

            // Receives error messages from OpenVR
            _controller.SetDebugLogAction((message) =>
            {
                Dispatcher.Invoke(() =>
                {
                    WriteToLog(message);
                });
            });

            // Init the things
            var actionKeys = InitList();
            _controller.Init(actionKeys);
            InitSettings();
            
            _initDone = true;
        }

        private void WriteToLog(String message)
        {
            var time = DateTime.Now.ToString("HH:mm:ss");
            var oldLog = TextBoxLog.Text;
            var lines = oldLog.Split('\n');
            Array.Resize(ref lines, 3);
            var newLog = string.Join("\n", lines);
            TextBoxLog.Text = $"{time}: {message}\n{newLog}";
        }

        private void InitWindow()
        {
            var shouldMinimize = MainModel.LoadSetting(MainModel.Setting.Minimize);
            var onlyInTray = MainModel.LoadSetting(MainModel.Setting.Tray);
            if(shouldMinimize) WindowUtils.Minimize(this, !onlyInTray);
        }

        #region bindings

        // Fill list with entries
        private string[] InitList(Dictionary<string, Key[]> config = null)
        {
            var actionKeys = new List<string>();
            actionKeys.AddRange(GenerateActionKeyRange(16, 'L')); // Left
            actionKeys.AddRange(GenerateActionKeyRange(16, 'R')); // Right
            actionKeys.AddRange(GenerateActionKeyRange(16, 'C')); // Chord
            actionKeys.AddRange(GenerateActionKeyRange(16, 'T')); // Tracker
            string[] GenerateActionKeyRange(int count, char type)
            {
                var keys = new List<string>();
                for (var i = 1; i <= count; i++) keys.Add($"{type}{i}");
                return keys.ToArray();
            }

            if (config == null) config = new Dictionary<string, Key[]>();
            _items.Clear();
            foreach (var actionKey in actionKeys)
            {
                var text = config.ContainsKey(actionKey) ? _controller.GetKeysLabel(config[actionKey]) : string.Empty;
                if (text == string.Empty) text = DEFAULT_KEY_LABEL;
                _items.Add(new BindingItem()
                {
                    Key = actionKey,
                    Label = $"Key {actionKey}",
                    Text = text
                });
            }
            ItemsControlBindings.ItemsSource = null;
            ItemsControlBindings.ItemsSource = _items;

            return actionKeys.ToArray();
        }

        // Binding data class
        public class BindingItem
        {
            public string Key { get; set; }
            public string Label { get; set; }
            public string Text { get; set; }
        }
        #endregion

        #region events
        // All key down events in the app
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            if (!_controller.OnKeyDown(key))
            {
                string message = $"Key not mapped: " + key.ToString();
                WriteToLog(message);
            }

        }

        // All key up events in the app
        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;
            _controller.OnKeyUp(key);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            WindowUtils.DestroyTrayIcon();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            var onlyInTray = MainModel.LoadSetting(MainModel.Setting.Tray);
            WindowUtils.OnStateChange(this, !onlyInTray);
        }

        #endregion

        #region actions
        private void UpdateConfigButton(bool hasConfig, bool forceButtonOff=false)
        {
            Debug.WriteLine($"Update Config Button: {hasConfig}");
            if(!forceButtonOff && _controller.AppIsRunning())
            {
                ButtonAppBinding.Content = hasConfig ? "Remove app-specific config" : "Add app-specific config";
                ButtonAppBinding.IsEnabled = true;
                ButtonAppBinding.Tag = hasConfig;
            } else
            {
                ButtonAppBinding.Content = "No application running right now";
                ButtonAppBinding.IsEnabled = false;
                ButtonAppBinding.Tag = null;
            }
        }

        // Click to either create new config for current app or remove the existing config.
        private void Button_AppBinding_Click(object sender, RoutedEventArgs e)
        {
            var tag = (sender as Button).Tag;
            switch(tag)
            {
                case null:
                    // This should never happen as the button cannot be pressed while disabled.
                    break;
                case true:
                    var result = MessageBox.Show(
                        Application.Current.MainWindow,
                        "Are you sure you want to delete this configuration?",
                        "OpenVR2Key",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning
                    );
                    if (result == MessageBoxResult.Yes)
                    {
                        MainModel.ClearBindings();
                        MainModel.DeleteConfig();
                        _controller.LoadConfig(true); // Loads default
                        UpdateConfigButton(false);
                    }
                    break;
                case false:
                    MainModel.SetConfigName(_currentlyRunningAppId);
                    MainModel.StoreConfig(new Dictionary<string, Key[]>());
                    _controller.LoadConfig(); // Loads the empty new one
                    UpdateConfigButton(true);
                    break;
            }
        }

        // This should clear all bindings from the current config
        private void Button_ClearAll_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                Application.Current.MainWindow, 
                "Are you sure you want to clear all mappings in this configuration?",
                "OpenVR2Key",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );
            if(result == MessageBoxResult.Yes)
            {
                MainModel.ClearBindings();
                InitList();
            }
        }

        private void Button_Folder_Click(object sender, RoutedEventArgs e)
        {
            _controller.OpenConfigFolder();
        }
        
        private void Button_LaunchBindings_Click(object sender, RoutedEventArgs e)
        {
            _controller.LaunchBindings();
        }
        #endregion

        #region bindings
        // Main action that is clicked from the list to start and end registration of keys
        private void Label_RecordSave_Click(object sender, MouseButtonEventArgs e)
        {
            var element = sender as Label;
            var dataItem = element.DataContext as BindingItem;
            var active = _controller.ToggleRegisteringKey(dataItem.Key, element, out object activeElement);
            UpdateLabel(activeElement as Label, active);
            if (active) _activeElement = activeElement;
            else _activeElement = null;
        }

        private void UpdateLabel(Label label, bool active)
        {
            {
                label.Foreground = active ? Brushes.DarkRed : Brushes.Black;
                label.BorderBrush = active ? Brushes.Tomato : Brushes.DarkGray;
                label.Background = active ? Brushes.LightPink : Brushes.LightGray;
            }
        }

        private void Label_HighlightOn(object sender, RoutedEventArgs e)
        {
            if (_activeElement != sender) (sender as Label).Background = Brushes.WhiteSmoke;
        }

        private void Label_HighlightOff(object sender, RoutedEventArgs e)
        {
            if (_activeElement != sender) (sender as Label).Background = Brushes.LightGray;
        }

        // Clear the current binding
        private void Button_ClearCancel_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var dataItem = button.DataContext as BindingItem;
            MainModel.RemoveBinding(dataItem.Key);
            DockPanel sp = VisualTreeHelper.GetParent(button) as DockPanel;
            var element = sp.Children[2] as Label;
            element.Content = DEFAULT_KEY_LABEL;
        }
        #endregion

        #region settings
        // Load settings and apply them to the checkboxes
        private void InitSettings()
        {
            CheckBoxMinimize.IsChecked = MainModel.LoadSetting(MainModel.Setting.Minimize);
            CheckBoxTray.IsChecked = MainModel.LoadSetting(MainModel.Setting.Tray);
            CheckBoxExitWithSteamVr.IsChecked = MainModel.LoadSetting(MainModel.Setting.ExitWithSteam);
            CheckBoxDebugNotifications.IsChecked = MainModel.LoadSetting(MainModel.Setting.Notification);
            CheckBoxHapticFeedback.IsChecked = MainModel.LoadSetting(MainModel.Setting.Haptic);
#if DEBUG
            LabelVersion.Content = $"{MainModel.GetVersion()}d";
#else
            LabelVersion.Content = MainModel.GetVersion();
#endif
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

        private void ClickedUrl(object sender, RoutedEventArgs e)
        {
            var link = (Hyperlink)sender;
            MiscUtils.OpenUrl(link.NavigateUri.ToString());
        }
        #endregion
        
        private void CheckBox_ExitWithSteamVR_Checked(object sender, RoutedEventArgs e)
        {
            MainModel.UpdateSetting(MainModel.Setting.ExitWithSteam, CheckboxValue(e));
        }
    }
}
