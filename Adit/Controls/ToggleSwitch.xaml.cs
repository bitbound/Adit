using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Adit.Controls
{
    public partial class ToggleSwitch : UserControl
    {
        public ToggleSwitch()
        {
            InitializeComponent();
            Switched = new EventHandler((send, arg) => { });
        }

        public EventHandler Switched;

        public bool IsOn
        {
            get
            {
                if (buttonToggle.Tag.ToString() == "On")
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            set
            {
                if (value)
                {
                    buttonToggle.Tag = "On";
                    borderToggle.Background = new SolidColorBrush(Colors.Gray);
                    var ca = new ColorAnimation(Colors.DeepSkyBlue, TimeSpan.FromSeconds(.25), FillBehavior.HoldEnd);
                    borderToggle.Background.BeginAnimation(SolidColorBrush.ColorProperty, ca);
                    var da = new DoubleAnimation(10, TimeSpan.FromSeconds(.25), FillBehavior.HoldEnd);
                    translateTransform.BeginAnimation(TranslateTransform.XProperty, da);
                }
                else
                {
                    buttonToggle.Tag = "Off";
                    borderToggle.Background = new SolidColorBrush(Colors.DeepSkyBlue);
                    var ca = new ColorAnimation(Colors.Gray, TimeSpan.FromSeconds(.25), FillBehavior.HoldEnd);
                    borderToggle.Background.BeginAnimation(SolidColorBrush.ColorProperty, ca);
                    var da = new DoubleAnimation(-10, TimeSpan.FromSeconds(.25), FillBehavior.HoldEnd);
                    translateTransform.BeginAnimation(TranslateTransform.XProperty, da);
                }
                Switched(this, EventArgs.Empty);
            }
        }

        private void ButtonToggle_Click(object sender, RoutedEventArgs e)
        {
            this.IsEnabled = false;
            IsOn = !IsOn;
            Task.Run(() => 
            {
                System.Threading.Thread.Sleep(500);
                this.Dispatcher.Invoke(() => this.IsEnabled = true);
            });
        }
    }
}