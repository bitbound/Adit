using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Adit.Pages
{
    /// <summary>
    /// Interaction logic for Notifier.xaml
    /// </summary>
    public partial class Notifier : Page
    {
        DispatcherTimer timer = new DispatcherTimer();
        public static Notifier Current { get; set; }
        public Notifier()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Current = this;
            MainWindow.Current.sideMenuStack.Visibility = Visibility.Collapsed;
            MainWindow.Current.WindowStyle = WindowStyle.None;
            MainWindow.Current.ShowInTaskbar = false;
            MainWindow.Current.Topmost = true;
            MainWindow.Current.Background = new SolidColorBrush(Colors.Transparent);
            MainWindow.Current.ResizeMode = ResizeMode.NoResize;
            MainWindow.Current.SizeToContent = SizeToContent.WidthAndHeight;
            MainWindow.Current.MouseLeftButtonDown += (send,args) => { MainWindow.Current.DragMove(); };
            timer.Interval = TimeSpan.FromSeconds(3);
            timer.Tick += (sen, args) =>
            {
                var procs = System.Diagnostics.Process.GetProcessesByName("Adit_Service");
                if (procs.Where(proc => proc.SessionId == System.Diagnostics.Process.GetCurrentProcess().SessionId).Count() == 0)
                {
                    //MainWindow.Current.Close();
                }
            };
            timer.Start();

            var wa = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            MainWindow.Current.Left = wa.Right - MainWindow.Current.ActualWidth;
            MainWindow.Current.Top = wa.Bottom - MainWindow.Current.ActualHeight;
        }
    }
}
