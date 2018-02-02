using System;
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
                    var ca = new ColorAnimation(Colors.DeepSkyBlue, TimeSpan.FromSeconds(.25));
                    borderToggle.Background.BeginAnimation(SolidColorBrush.ColorProperty, ca);
                    var da = new DoubleAnimation(10, TimeSpan.FromSeconds(.25));
                    translateTransform.BeginAnimation(TranslateTransform.XProperty, da);
                }
                else
                {
                    buttonToggle.Tag = "Off";
                    borderToggle.Background = new SolidColorBrush(Colors.DeepSkyBlue);
                    var ca = new ColorAnimation(Colors.Gray, TimeSpan.FromSeconds(.25));
                    borderToggle.Background.BeginAnimation(SolidColorBrush.ColorProperty, ca);
                    var da = new DoubleAnimation(-10, TimeSpan.FromSeconds(.25));
                    translateTransform.BeginAnimation(TranslateTransform.XProperty, da);
                }
                Switched(this, EventArgs.Empty);
            }
        }

        private void buttonToggle_Click(object sender, RoutedEventArgs e)
        {
            if (buttonToggle.Tag.ToString() == "Off")
            {
                buttonToggle.Tag = "On";
                borderToggle.Background = new SolidColorBrush(Colors.Gray);
                var ca = new ColorAnimation(Colors.SkyBlue, TimeSpan.FromSeconds(.25));
                borderToggle.Background.BeginAnimation(SolidColorBrush.ColorProperty, ca);
                var da = new DoubleAnimation(10, TimeSpan.FromSeconds(.25));
                translateTransform.BeginAnimation(TranslateTransform.XProperty, da);
            }
            else
            {
                buttonToggle.Tag = "Off";
                borderToggle.Background = new SolidColorBrush(Colors.SkyBlue);
                var ca = new ColorAnimation(Colors.Gray, TimeSpan.FromSeconds(.25));
                borderToggle.Background.BeginAnimation(SolidColorBrush.ColorProperty, ca);
                var da = new DoubleAnimation(-10, TimeSpan.FromSeconds(.25));
                translateTransform.BeginAnimation(TranslateTransform.XProperty, da);
            }
            Switched(this, EventArgs.Empty);
        }
    }
}