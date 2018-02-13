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
        }

        public void InputSurface_Unloaded(object sender, RoutedEventArgs e)
        {
            MainWindow.Current.PreviewKeyDown -= Window_PreviewKeyDown;
            MainWindow.Current.PreviewKeyUp -= Window_PreviewKeyUp;
            MainWindow.Current.LostFocus -= Window_LostFocus;
        }

        public void Window_LostFocus(object sender, RoutedEventArgs e)
        {
            AditViewer.SocketMessageHandler?.SendClearAllKeys();
        }


        public void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (inputSurface.IsVisible)
            {
                e.Handled = true;
                AditViewer.SocketMessageHandler?.SendKeyDown(e.Key);
            }
        }

        public void Window_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (inputSurface.IsVisible)
            {
                e.Handled = true;
                AditViewer.SocketMessageHandler?.SendKeyUp(e.Key);
            }
        }

        public void InputSurface_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (inputSurface.IsVisible)
            {
                e.Handled = true;
                AditViewer.SocketMessageHandler.SendMouseWheel(e.Delta);
            }
        }

        public void InputSurface_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (inputSurface.IsVisible)
            {
                e.Handled = true;
                var position = e.GetPosition(inputSurface);
                AditViewer.SocketMessageHandler.SendMouseLeftDown(position.X / inputSurface.ActualWidth, position.Y / inputSurface.ActualHeight);
            }
        }

        public void InputSurface_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (inputSurface.IsVisible)
            {
                e.Handled = true;
                var position = e.GetPosition(inputSurface);
                AditViewer.SocketMessageHandler.SendMouseLeftUp(position.X / inputSurface.ActualWidth, position.Y / inputSurface.ActualHeight);
            }
        }
        public void InputSurface_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (inputSurface.IsVisible)
            {
                e.Handled = true;
                var position = e.GetPosition(inputSurface);
                AditViewer.SocketMessageHandler.SendMouseRightDown(position.X / inputSurface.ActualWidth, position.Y / inputSurface.ActualHeight);
            }
        }
        public void InputSurface_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (inputSurface.IsVisible)
            {
                e.Handled = true;
                var position = e.GetPosition(inputSurface);
                AditViewer.SocketMessageHandler.SendMouseRightUp(position.X / inputSurface.ActualWidth, position.Y / inputSurface.ActualHeight);
            }
        }


        public void InputSurface_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (inputSurface.IsVisible)
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
}
