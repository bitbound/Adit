using Adit.Code.Shared;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Adit.Code.Viewer
{
    public class ViewerSurface : FrameworkElement
    {
        private DrawingVisual drawingSurface;
        private VisualCollection children;
        private BitmapImage bitmapImage;
        private TranslateTransform translateTransform;
        private Rect imageRegion;
        private RenderTargetBitmap renderTargetBitmap;

        private double maxWidth = 0;
        private double maxHeight = 0;

        public ViewerSurface()
        {
            drawingSurface = new DrawingVisual();
            drawingSurface.Transform = new ScaleTransform(1, 1);
            children = new VisualCollection(this);
            children.Add(drawingSurface);
            this.SizeChanged += ViewerSurface_SizeChanged;
        }


        private void ViewerSurface_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CalculateScaleTransform(maxWidth, maxHeight);
        }
        
        public void DrawImage(Point startDrawingPoint, IEnumerable<byte> imageBytes)
        {
            using (var ms = new MemoryStream())
            {
                ms.Write(imageBytes.ToArray(), 0, imageBytes.Count());
                bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = ms;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                CalculateScaleTransform(bitmapImage.Width, bitmapImage.Height);

                renderTargetBitmap = new RenderTargetBitmap(
                    (int)(this.ActualWidth == 0 ? maxWidth : this.ActualWidth),
                    (int)(this.ActualHeight == 0 ? maxHeight : this.ActualHeight),
                    VisualTreeHelper.GetDpi(drawingSurface).PixelsPerInchX,
                    VisualTreeHelper.GetDpi(drawingSurface).PixelsPerInchY, PixelFormats.Default
                );
                renderTargetBitmap.Render(drawingSurface);

                translateTransform = new TranslateTransform(startDrawingPoint.X, startDrawingPoint.Y);
                imageRegion = new Rect(new Point(0, 0), new Point(bitmapImage.Width, bitmapImage.Height));
                using (var context = drawingSurface.RenderOpen())
                {
                    if (Config.Current.ViewerScaleToFit)
                    {
                        context.DrawImage(renderTargetBitmap, new Rect(0, 0, maxWidth, maxHeight));
                    }
                    else
                    {
                        context.DrawImage(renderTargetBitmap, new Rect(0, 0, renderTargetBitmap.Width, renderTargetBitmap.Height));
                    }
                    context.PushTransform(translateTransform);
                    context.DrawImage(bitmapImage, imageRegion);
                }
            }
        }

        private void CalculateScaleTransform(double width, double height)
        {
            maxWidth = Math.Max(width, maxWidth);
            maxHeight = Math.Max(height, maxHeight);
            if (Config.Current.ViewerScaleToFit)
            {
                this.Width = double.NaN;
                this.Height = double.NaN;
                (drawingSurface.Transform as ScaleTransform).ScaleX = this.ActualWidth / maxWidth;
                (drawingSurface.Transform as ScaleTransform).ScaleY = this.ActualHeight / maxHeight;
            }
            else
            {
                (drawingSurface.Transform as ScaleTransform).ScaleX = 1;
                (drawingSurface.Transform as ScaleTransform).ScaleY = 1;
                this.Width = maxWidth;
                this.Height = maxHeight;
            }
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
