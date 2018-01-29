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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
        
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            TrayIcon.Create();
            switch (Settings.StartupMode)
            {
                case Settings.StartupModes.ModeSelect:
                    mainFrame.Navigate(new ModeSelect());
                    break;
                case Settings.StartupModes.Client:
                    mainFrame.Navigate(new ClientMain());
                    break;
                case Settings.StartupModes.Server:
                    mainFrame.Navigate(new ServerMain());
                    break;
                case Settings.StartupModes.Viewer:
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
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            //e.Cancel = true;
            TrayIcon.Icon?.ShowCustomBalloon(new ClosedToTrayBalloon(), PopupAnimation.Fade, 5000);
            //this?.Hide();
        }
    }
}
