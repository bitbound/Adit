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
        private void RefreshUI()
        {
            toggleUAC.IsOn = Config.Current.IsUACHandled;
        }

    }
}
