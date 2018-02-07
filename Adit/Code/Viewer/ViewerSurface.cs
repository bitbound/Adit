using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace Adit.Code.Viewer
{
    public class ViewerSurface : FrameworkElement
    {
        private VisualCollection children;
        public DrawingVisual DrawingSurface { get; set; } = new DrawingVisual();

        public ViewerSurface()
        {
            children = new VisualCollection(this);
            children.Add(DrawingSurface);

            //using (var dc = DrawingSurface.RenderOpen())
            //{
            //    dc.DrawLine(new Pen(Brushes.Black, 1), new Point(0, 0), new Point(400, 400));
            //    dc.DrawLine(new Pen(Brushes.Black, 1), new Point(0, 400), new Point(400, 0));
            //}
            //mainGrid.Children.Add(new ViewingCanvas());
            //var visual = new DrawingVisual();

            //using (var dc = visual.RenderOpen())
            //{
            //    dc.DrawLine(new Pen(Brushes.Black, 1), new Point(0, 0), new Point(400, 400));
            //    dc.DrawLine(new Pen(Brushes.Black, 1), new Point(0, 400), new Point(400, 0));
            //}
            //new RenderTargetBitmap(300, 300,)
            //mainImage.Source = new BitmapImage(visual);
        }
        public void DrawImage(byte[] imageBytes)
        {
        }
        protected override int VisualChildrenCount
        {
            get { return children.Count; }
        }

        protected override Visual GetVisualChild(int index)
        {
            if (index < 0 || index >= children.Count)
            {
                throw new ArgumentOutOfRangeException();
            }

            return children[index];
        }
    }
}
