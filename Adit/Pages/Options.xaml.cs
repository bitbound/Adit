using Adit.Code.Shared;
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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Adit.Pages
{
    /// <summary>
    /// Interaction logic for OptionsMain.xaml
    /// </summary>
    public partial class Options : Page
    {
        public Options()
        {
            InitializeComponent();
        }

        private void HandleUAC_Click(object sender, MouseButtonEventArgs e)
        {
            Config.Current.IsUACHandled = (sender as Controls.ToggleSwitch).IsOn;
            Config.Save();
        }

        private void ServiceInstalled_Click(object sender, MouseButtonEventArgs e)
        {

        }
        private void ServiceRunning_Click(object sender, MouseButtonEventArgs e)
        {

        }

        //private void MenuUnattended_Click(object sender, RoutedEventArgs e)
        //{

        //    if (!WindowsIdentity.GetCurrent().Owner.IsWellKnown(WellKnownSidType.BuiltinAdministratorsSid))
        //    {
        //        System.Windows.MessageBox.Show("The client must be running as an administrator (i.e. elevated) in order to access unattended features.", "Elevation Required", MessageBoxButton.OK, MessageBoxImage.Warning);
        //        return;
        //    }
        //    new UnattendedWindow().ShowDialog();
        //}
        //private void MenuUAC_Click(object sender, RoutedEventArgs e)
        //{
        //    handleUAC = menuUAC.IsChecked;
        //}
        private void RefreshUI()
        {
            toggleUAC.IsOn = Config.Current.IsUACHandled;
        }

    }
}
