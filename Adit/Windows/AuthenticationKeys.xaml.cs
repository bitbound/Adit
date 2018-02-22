using Adit.Code.Server;
using Adit.Models;
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
using System.Windows.Shapes;

namespace Adit.Windows
{
    /// <summary>
    /// Interaction logic for AuthenticationKeys.xaml
    /// </summary>
    public partial class AuthenticationKeys : Window
    {
        public AuthenticationKeys()
        {
            InitializeComponent();
        }

        private void New_Click(object sender, RoutedEventArgs e)
        {
            Authentication.Current.Keys.Add(new AuthenticationKey());
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            foreach (AuthenticationKey item in datagridKeys.SelectedItems)
            {
                Authentication.Current.Keys.Remove(item);
            }
        }

        private void DatagridKeys_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            datagridKeys.GetBindingExpression(DataGrid.ItemsSourceProperty).UpdateSource();
            Authentication.Current.Save();
        }
    }
}
