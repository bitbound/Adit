using Adit.Client_Code;
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
    /// Interaction logic for ViewerMain.xaml
    /// </summary>
    public partial class ViewerMain : Page
    {
        public static ViewerMain Current { get; set; }
        public Viewer Viewer { get; set; }

        public ViewerMain()
        {
            InitializeComponent();
            Current = this;
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            Viewer = new Viewer();
            viewerFrame.Children.Add(Viewer);
        }
  
        public void DrawImageCall(byte[] imageBytes)
        {
            this.Dispatcher.Invoke(() => Viewer.DrawImage(imageBytes));
        }
    }
}
