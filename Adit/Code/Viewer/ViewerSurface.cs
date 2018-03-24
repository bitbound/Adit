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
        private Int32Rect imageRegion;
        private WriteableBitmap writeableBitmap;

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
        
        public void DrawImage(byte[] imageBytes)
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

                if (writeableBitmap == null)
                {
                    writeableBitmap = new WriteableBitmap(bitmapImage);
                }
                else
                {
                    imageRegion = new Int32Rect(
                        0,
                        0,
                        (int)(bitmapImage.PixelWidth),
                        (int)(bitmapImage.PixelHeight));
                    var pixels = new byte[(bitmapImage.PixelWidth * 4 * bitmapImage.PixelHeight * 4)];
                    bitmapImage.CopyPixels(pixels, bitmapImage.PixelWidth * 4, 0);
                    writeableBitmap.WritePixels(imageRegion, pixels, bitmapImage.PixelWidth * 4, AditViewer.NextDrawPoint.X, AditViewer.NextDrawPoint.Y); 
                }

                using (var context = drawingSurface.RenderOpen())
                {
                    if (Config.Current.IsViewerScaleToFit)
                    {
                        context.DrawImage(writeableBitmap, new Rect(0, 0, maxWidth, maxHeight));
                    }
                    else
                    {
                        context.DrawImage(writeableBitmap, new Rect(0, 0, writeableBitmap.Width, writeableBitmap.Height));
                    }
                }
            }
        }

        private void CalculateScaleTransform(double width, double height)
        {
            maxWidth = Math.Max(width, maxWidth);
            maxHeight = Math.Max(height, maxHeight);
            if (Config.Current.IsViewerScaleToFit)
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
