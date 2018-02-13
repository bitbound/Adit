using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Adit.Code.Viewer
{
    public class InputHandler
    {
        private FrameworkElement inputSurface;
        private DateTime lastPointerMove;
        public InputHandler(FrameworkElement inputSurface)
        {
            this.inputSurface = inputSurface;
            inputSurface.PreviewMouseMove += InputSurface_PreviewMouseMove;
            inputSurface.PreviewMouseLeftButtonDown += InputSurface_PreviewMouseLeftButtonDown;
            inputSurface.PreviewMouseLeftButtonUp += InputSurface_PreviewMouseLeftButtonUp;
            inputSurface.PreviewMouseRightButtonDown += InputSurface_PreviewMouseRightButtonDown;
            inputSurface.PreviewMouseRightButtonUp += InputSurface_PreviewMouseRightButtonUp;
            inputSurface.PreviewMouseWheel += InputSurface_PreviewMouseWheel;
            MainWindow.Current.PreviewKeyDown -= Window_PreviewKeyDown;
            MainWindow.Current.PreviewKeyDown += Window_PreviewKeyDown;
            MainWindow.Current.PreviewKeyUp -= Window_PreviewKeyUp;
            MainWindow.Current.PreviewKeyUp += Window_PreviewKeyUp;
            MainWindow.Current.LostFocus -= Window_LostFocus;
            MainWindow.Current.LostFocus += Window_LostFocus;
        }

        private void Window_LostFocus(object sender, RoutedEventArgs e)
        {
            AditViewer.SocketMessageHandler.SendClearAllKeys();
        }


        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            AditViewer.SocketMessageHandler.SendKeyDown(e.Key);
        }

        private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            e.Handled = true;
            AditViewer.SocketMessageHandler.SendKeyUp(e.Key);
        }

        private void InputSurface_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            e.Handled = true;
            AditViewer.SocketMessageHandler.SendMouseWheel(e.Delta);
        }

        private void InputSurface_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            var position = e.GetPosition(inputSurface);
            AditViewer.SocketMessageHandler.SendMouseLeftDown(position.X, position.Y);
        }

        private void InputSurface_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            var position = e.GetPosition(inputSurface);
            AditViewer.SocketMessageHandler.SendMouseLeftUp(position.X, position.Y);
        }
        private void InputSurface_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            var position = e.GetPosition(inputSurface);
            AditViewer.SocketMessageHandler.SendMouseRightDown(position.X, position.Y);
        }
        private void InputSurface_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
            var position = e.GetPosition(inputSurface);
            AditViewer.SocketMessageHandler.SendMouseRightUp(position.X, position.Y);
        }


        private void InputSurface_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            e.Handled = true;
            if (DateTime.Now - lastPointerMove > TimeSpan.FromMilliseconds(50))
            {
                lastPointerMove = DateTime.Now;
                var position = e.GetPosition(inputSurface);
                AditViewer.SocketMessageHandler.SendMouseMove(position.X / inputSurface.ActualWidth, position.Y / inputSurface.ActualHeight);
            }
        }
    }
}
