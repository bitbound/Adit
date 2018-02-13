using Adit.Code.Shared;
using System;
using System.Collections.Generic;
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

namespace Adit.Controls
{
    /// <summary>
    /// Interaction logic for FlyoutNotification.xaml
    /// </summary>
    public partial class FlyoutNotification : UserControl
    {
        public static void Show(string message)
        {
            MainWindow.Current.Dispatcher.Invoke(() =>
            {
                TrayIcon.Icon.ShowCustomBalloon(new FlyoutNotification(message), PopupAnimation.Fade, 5000);
            });
        }
        private FlyoutNotification(string message)
        {
            InitializeComponent();
            textMessage.Text = message;
        }

        private void UserControl_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            (this.Parent as Popup).IsOpen = false;
        }
    }
}
