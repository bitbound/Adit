using Adit.Client_Code;
using Adit.Controls;
using Adit.Pages;
using Adit.Shared_Code;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Linq;

namespace Adit
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static MainWindow Current { get; private set; }
        public MainWindow()
        {
            Initializer.SetGlobalErrorHandler();
            Initializer.SetShutdownMode();
            InitializeComponent();
            Current = this;
            Settings.Load();
            Initializer.ParseCommandLineArgs(Environment.GetCommandLineArgs());

        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            TrayIcon.Icon?.ShowCustomBalloon(new ClosedToTrayBalloon(), PopupAnimation.Fade, 5000);
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Initializer.ProcessConfiguration();
            TrayIcon.Create();
            switch (Settings.StartupMode)
            {
                case Settings.StartupModes.FirstRun:
                    welcomeToggle.IsChecked = true;
                    mainFrame.Navigate(new Welcome());
                    break;
                case Settings.StartupModes.Client:
                    clientToggle.IsChecked = true;
                    mainFrame.Navigate(new ClientMain());
                    break;
                case Settings.StartupModes.Server:
                    serverToggle.IsChecked = true;
                    mainFrame.Navigate(new ServerMain());
                    break;
                case Settings.StartupModes.Viewer:
                    viewerToggle.IsChecked = true;
                    mainFrame.Navigate(new ViewerMain());
                    break;
                case Settings.StartupModes.Notifier:
                    break;
                default:
                    break;
            }
            //mainGrid.Children.Add(new ViewingCanvas());
            //var visual = new DrawingVisual();

            //using (var dc = visual.RenderOpen())
            //{
            //    dc.DrawLine(new Pen(Brushes.Black, 1), new Point(0, 0), new Point(400, 400));
            //    dc.DrawLine(new Pen(Brushes.Black, 1), new Point(0, 400), new Point(400, 0));
            //}
            //new RenderTargetBitmap(300, 300,)
            //mainImage.Source = new BitmapImage(visual);
        }

        private void Welcome_Click(object sender, RoutedEventArgs e)
        {
            toggleButtonClicked(sender as ToggleButton);
            mainFrame.Navigate(new Welcome());
        }

        private void Server_Click(object sender, RoutedEventArgs e)
        {
            toggleButtonClicked(sender as ToggleButton);
            mainFrame.Navigate(new ServerMain());
        }
        private void Client_Click(object sender, RoutedEventArgs e)
        {
            toggleButtonClicked(sender as ToggleButton);
            mainFrame.Navigate(new ClientMain());
        }
        private void Viewer_Click(object sender, RoutedEventArgs e)
        {
            toggleButtonClicked(sender as ToggleButton);
            mainFrame.Navigate(new ViewerMain());
        }
        private void Demo_Click(object sender, RoutedEventArgs e)
        {
            toggleButtonClicked(sender as ToggleButton);
            mainFrame.Navigate(new DemoMain());
        }

        private void About_Click(object sender, RoutedEventArgs e)
        {
            toggleButtonClicked(sender as ToggleButton);
            mainFrame.Navigate(new AboutMain());
        }
        private void toggleButtonClicked(ToggleButton sender)
        {
            foreach (ToggleButton button in sideMenuStack.Children)
            {
                button.IsChecked = false;
            }
            sender.IsChecked = true;
        }

    }
}
