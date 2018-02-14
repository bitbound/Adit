using Adit.Pages;
using Adit.Code.Shared;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Media;
using Win32_Classes;
using System.Linq;

namespace Adit.Code.Client
{
    public class Capturer
    {
        Bitmap currentFrame;
        Bitmap lastFrame;
        Rectangle boundingBox;
        byte[] diffData;
        byte[] newImgData;
        byte[] rgbValues1;
        byte[] rgbValues2;
        BitmapData bd1;
        BitmapData bd2;
        // Offsets are the left and top edge of the screen, in case multiple monitor setups
        // create a situation where the edge of a monitor is in the negative.  This must
        // be converted to a 0-based max left/top to render images on the canvas properly.
        int offsetX = SystemInformation.VirtualScreen.Left;
        int offsetY = SystemInformation.VirtualScreen.Top;
        int totalHeight = SystemInformation.VirtualScreen.Height;
        int totalWidth = SystemInformation.VirtualScreen.Width;
        Graphics graphic;
        IntPtr hWnd;
        IntPtr hDC;
        IntPtr graphDC;
        User32.CursorInfo ci = new User32.CursorInfo();



        public Capturer()
        {
            currentFrame = new Bitmap(totalWidth, totalHeight);
            lastFrame = new Bitmap(totalWidth, totalHeight);
            hWnd = User32.GetDesktopWindow();
        }

        public void CaptureScreen()
        {
            lastFrame = (Bitmap)currentFrame.Clone();

            graphic = Graphics.FromImage(currentFrame);
            //hDC = User32.GetWindowDC(hWnd);
            //graphDC = graphic.GetHdc();
            try
            {
                //var copyResult = GDI32.BitBlt(graphDC, 0, 0, totalWidth, totalHeight, hDC, 0 + offsetX, 0 + offsetY, GDI32.TernaryRasterOperations.SRCCOPY | GDI32.TernaryRasterOperations.CAPTUREBLT);
                //if (!copyResult)
                //{
                //    graphic.ReleaseHdc(graphDC);
                //    graphic.Clear(System.Drawing.Color.White);
                //    var font = new Font(System.Drawing.FontFamily.GenericSansSerif, 30, System.Drawing.FontStyle.Bold);
                //    graphic.DrawString("Waiting for screen capture...", font, System.Drawing.Brushes.Black, new PointF((totalWidth / 2), totalHeight / 2), new StringFormat() { Alignment = StringAlignment.Center });
                //}
                //else
                //{
                //    graphic.ReleaseHdc(graphDC);
                //    User32.ReleaseDC(hWnd, hDC);
                //}

                graphic.CopyFromScreen(0 + offsetX, 0 + offsetY, 0, 0, new System.Drawing.Size(totalWidth, totalHeight));
                // Get cursor information to draw on the screenshot.
                ci.cbSize = Marshal.SizeOf(ci);
                User32.GetCursorInfo(out ci);
                if (ci.flags == User32.CURSOR_SHOWING)
                {
                    using (var icon = System.Drawing.Icon.FromHandle(ci.hCursor))
                    {
                        graphic.DrawIcon(icon, ci.ptScreenPos.x, ci.ptScreenPos.y);
                    }
                }
            }
            catch (Exception ex)
            {
                Utilities.WriteToLog(ex);
                //if (graphDC != IntPtr.Zero)
                //{
                //    graphic.ReleaseHdc(graphDC);
                //}
                //if (hDC != IntPtr.Zero)
                //{
                //    User32.ReleaseDC(hWnd, hDC);
                //}
            }
        }
        public MemoryStream GetFullscreenStream(byte[] metadataToPrepend)
        {
            var ms = new MemoryStream();
            ms.Write(metadataToPrepend, 0, 36);
            ms.Write(new byte[] { 0, 0, 0, 0, 0, 0 }, 0, 6);
            currentFrame.Save(ms, ImageFormat.Png);
            return ms;
        }
        public bool IsNewFrameDifferent()
        {
            diffData = GetDiffData();
            return diffData != null;
        }

        public MemoryStream GetDiffStream(byte[] metadataToPrepend)
        {
            var ms = new MemoryStream();
            using (var croppedFrame = currentFrame.Clone(boundingBox, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            {
                // Add x,y coordinates of top-left of image so receiver knows where to draw it.
                ms.Write(metadataToPrepend, 0, 36);
                ms.Write(diffData, 0, 6);
                croppedFrame.Save(ms, ImageFormat.Png);
            }
            return ms;
        }

        private byte[] GetDiffData()
        {
            if (currentFrame.Height != lastFrame.Height || currentFrame.Width != lastFrame.Width)
            {
                throw new Exception("Bitmaps are not of equal dimensions.");
            }
            if (!Bitmap.IsAlphaPixelFormat(currentFrame.PixelFormat) || !Bitmap.IsAlphaPixelFormat(lastFrame.PixelFormat) ||
                !Bitmap.IsCanonicalPixelFormat(currentFrame.PixelFormat) || !Bitmap.IsCanonicalPixelFormat(lastFrame.PixelFormat))
            {
                throw new Exception("Bitmaps must be 32 bits per pixel and contain alpha channel.");
            }
            var width = currentFrame.Width;
            var height = currentFrame.Height;
            int left = int.MaxValue;
            int top = int.MaxValue;
            int right = int.MinValue;
            int bottom = int.MinValue;

            try
            {
                bd1 = currentFrame.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, currentFrame.PixelFormat);
            }
            catch
            {
                currentFrame.UnlockBits(bd1);
                bd1 = currentFrame.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, currentFrame.PixelFormat);
            }
            try
            {
                bd2 = lastFrame.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, lastFrame.PixelFormat);
            }
            catch
            {
                lastFrame.UnlockBits(bd2);
                bd2 = lastFrame.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, lastFrame.PixelFormat);
            }
            // Get the address of the first line.
            IntPtr ptr1 = bd1.Scan0;
            IntPtr ptr2 = bd2.Scan0;

            // Declare an array to hold the bytes of the bitmap.
            int arraySize = Math.Abs(bd1.Stride) * currentFrame.Height;
            rgbValues1 = new byte[arraySize];
            rgbValues2 = new byte[arraySize];

            // Copy the RGBA values into the array.
            Marshal.Copy(ptr1, rgbValues1, 0, arraySize);
            Marshal.Copy(ptr2, rgbValues2, 0, arraySize);

            // Check RGBA value for each pixel.
            for (int counter = 0; counter < rgbValues1.Length - 4; counter += 4)
            {
                if (rgbValues1[counter] != rgbValues2[counter] ||
                    rgbValues1[counter + 1] != rgbValues2[counter + 1] ||
                    rgbValues1[counter + 2] != rgbValues2[counter + 2] ||
                    rgbValues1[counter + 3] != rgbValues2[counter + 3])
                {
                    // Change was found.
                    var pixel = counter / 4;
                    var row = (int)Math.Floor((double)pixel / bd1.Width);
                    var column = pixel % bd1.Width;
                    if (row < top)
                    {
                        top = row;
                    }
                    if (row > bottom)
                    {
                        bottom = row;
                    }
                    if (column < left)
                    {
                        left = column;
                    }
                    if (column > right)
                    {
                        right = column;
                    }
                }
            }
            if (left < right && top < bottom)
            {
                // Bounding box is valid.

                left = Math.Max(left - 20, 0);
                top = Math.Max(top - 20, 0);
                right = Math.Min(right + 20, totalWidth);
                bottom = Math.Min(bottom + 20, totalHeight);

                // Byte array that indicates top left coordinates of the image.
                newImgData = new byte[6];
                newImgData[0] = (byte)(left % 1000000 / 10000);
                newImgData[1] = (byte)(left % 10000 / 100);
                newImgData[2] = (byte)(left % 100);
                newImgData[3] = (byte)(top % 1000000 / 10000);
                newImgData[4] = (byte)(top % 10000 / 100);
                newImgData[5] = (byte)(top % 100);


                boundingBox = new System.Drawing.Rectangle(left, top, right - left, bottom - top);
                currentFrame.UnlockBits(bd1);
                lastFrame.UnlockBits(bd2);
                return newImgData;
            }
            else
            {
                currentFrame.UnlockBits(bd1);
                lastFrame.UnlockBits(bd2);
                return null;
            }
        }
    }
}
