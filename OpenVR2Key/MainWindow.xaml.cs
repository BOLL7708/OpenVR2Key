using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

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
            controller = new MainController();
        }

        // TODO: Use this for every input
        private void TextBlock_Test_MouseUp(object sender, MouseButtonEventArgs e)
        {
            Debug.WriteLine(((TextBlock)sender).Name); // TODO: Parse the key number from the name of the element
            controller.ToggleRegisteringKey((TextBlock)sender);
        }

        #region events
        protected override void OnKeyDown(KeyEventArgs e)
        {
            controller.OnKeyDown(e.Key);

            // Doesn't seem like this is preventing the ALT behavior at all. https://stackoverflow.com/a/2277355
            if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt) e.Handled = true;
            else base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            controller.OnKeyUp(e.Key);

            if (e.Key == Key.LeftAlt || e.Key == Key.RightAlt) e.Handled = true;
            else base.OnKeyUp(e);
        }
        #endregion
    }
}
