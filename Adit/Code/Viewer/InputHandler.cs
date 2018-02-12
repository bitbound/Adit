using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Adit.Code.Viewer
{
    public class InputHandler
    {
        private FrameworkElement inputSurface;
        private DateTime lastPointerInput;
        public InputHandler(FrameworkElement inputSurface)
        {
            this.inputSurface = inputSurface;
            inputSurface.PreviewMouseMove += InputSurface_PreviewMouseMove;
        }

        private void InputSurface_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (DateTime.Now - lastPointerInput > TimeSpan.FromMilliseconds(50))
            {
                lastPointerInput = DateTime.Now;
                var position = e.GetPosition(inputSurface);
                AditViewer.SocketMessageHandler.SendMouseMove(position.X / inputSurface.ActualWidth, position.Y / inputSurface.ActualHeight);
            }
        }
    }
}
