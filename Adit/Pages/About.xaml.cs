using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// Interaction logic for AboutMain.xaml
    /// </summary>
    public partial class About : Page
    {
        public About()
        {
            InitializeComponent();
        }
        public string Version
        {
            get
            {
                return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            }
        }
        private void HyperAditWebsite_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://lucent.rocks");
        }

        private void HyperIcons8_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://icons8.com");
        }

        private void NotifyIcon_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("http://www.hardcodet.net/wpf-notifyicon");
        }
    }
}
