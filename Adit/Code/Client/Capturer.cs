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
using System.Threading.Tasks;

namespace Adit.Code.Client
{
    public class Capturer
    {
        Bitmap currentFrame;
        Bitmap lastFrame;
        Rectangle boundingBox;
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
        string desktopName;
        System.Drawing.Point captureStartPoint;


        public Capturer()
        {
            currentFrame = new Bitmap(totalWidth, totalHeight);
            lastFrame = new Bitmap(totalWidth, totalHeight);
            hWnd = User32.GetDesktopWindow();
        }

        public void CaptureScreen()
        {
            desktopName = User32.GetCurrentDesktop();
            lastFrame = (Bitmap)currentFrame.Clone();
            graphic = Graphics.FromImage(currentFrame);
            hDC = User32.GetWindowDC(hWnd);
            graphDC = graphic.GetHdc();
            try
            {
                var copyResult = GDI32.BitBlt(graphDC, 0, 0, totalWidth, totalHeight, hDC, 0 + offsetX, 0 + offsetY, GDI32.TernaryRasterOperations.SRCCOPY | GDI32.TernaryRasterOperations.CAPTUREBLT);
                if (!copyResult)
                {
                    graphic.ReleaseHdc(graphDC);
                    graphic.Clear(System.Drawing.Color.White);
                    var font = new Font(System.Drawing.FontFamily.GenericSansSerif, 30, System.Drawing.FontStyle.Bold);
                    graphic.DrawString("Waiting for screen capture...", font, System.Drawing.Brushes.Black, new PointF((totalWidth / 2), totalHeight / 2), new StringFormat() { Alignment = StringAlignment.Center });

                    if (!AditClient.DesktopSwitchPending)
                    {
                        SwitchDesktops();
                    }
                }
                else
                {
                    graphic.ReleaseHdc(graphDC);
                    User32.ReleaseDC(hWnd, hDC);
                }

                //graphic.CopyFromScreen(0 + offsetX, 0 + offsetY, 0, 0, new System.Drawing.Size(totalWidth, totalHeight));

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
                if (graphDC != IntPtr.Zero)
                {
                    graphic.ReleaseHdc(graphDC);
                }
                if (hDC != IntPtr.Zero)
                {
                    User32.ReleaseDC(hWnd, hDC);
                }
                if (!AditClient.DesktopSwitchPending)
                {
                    SwitchDesktops();
                }
            }
        }

        private void SwitchDesktops()
        {
            AditClient.DesktopSwitchPending = true;
            Utilities.WriteToLog($"Desktop switch initiated to {desktopName}.");

            var procInfo = new ADVAPI32.PROCESS_INFORMATION();
            var sessionID = Guid.NewGuid().ToString();
            if (ADVAPI32.OpenInteractiveProcess(Path.Combine(Utilities.ProgramFolder, "Adit.exe") + $" -upgrade {AditClient.SessionID}", desktopName, out procInfo))
            {
                AditClient.SocketMessageHandler.SendDesktopSwitch();
                return;
            }
            else
            {
                var error = Marshal.GetLastWin32Error();
                if (error == 6)
                {
                    Utilities.WriteToLog("Desktop switch failed during a Windows session change.");
                }
                else
                {
                    Utilities.WriteToLog(new Exception("Failed to switch desktops.  Error: " + error.ToString()));
                }
                return;
            }
        }
        public byte[] GetCapture(bool fullscreen)
        {
            if (fullscreen)
            {
                using (var ms = new MemoryStream())
                {
                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param = new EncoderParameter[] { new EncoderParameter(Encoder.Quality, 1) };
                    currentFrame.Save(ms, ImageCodecInfo.GetImageEncoders().FirstOrDefault(x=>x.FormatID == ImageFormat.Png.Guid), encoderParams);
                    var messageHeader = new byte[7];
                    messageHeader[0] = 1;
                    return messageHeader.Concat(ms.ToArray()).ToArray();
                }
            }
            else
            {
                using (var ms = new MemoryStream())
                {
                    using (var croppedFrame = currentFrame.Clone(boundingBox, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
                    {
                        var encoderParams = new EncoderParameters(1);
                        encoderParams.Param = new EncoderParameter[] { new EncoderParameter(Encoder.Quality, 1) };
                        croppedFrame.Save(ms, ImageCodecInfo.GetImageEncoders().FirstOrDefault(x => x.FormatID == ImageFormat.Png.Guid), encoderParams);
                        // Byte array that indicates top left coordinates of the image.
                        var messageHeader = new byte[7];
                        messageHeader[0] = 1;
                        messageHeader[1] = (byte)(captureStartPoint.X % 1000000 / 10000);
                        messageHeader[2] = (byte)(captureStartPoint.X % 10000 / 100);
                        messageHeader[3] = (byte)(captureStartPoint.X % 100);
                        messageHeader[4] = (byte)(captureStartPoint.Y % 1000000 / 10000);
                        messageHeader[5] = (byte)(captureStartPoint.Y % 10000 / 100);
                        messageHeader[6] = (byte)(captureStartPoint.Y % 100);

                        return messageHeader.Concat(ms.ToArray()).ToArray();
                    }
                }
            }
            
        }

        public bool IsNewFrameDifferent()
        {
            return GetDiffData();
        }


        private bool GetDiffData()
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
                try
                {
                    currentFrame.UnlockBits(bd1);
                    bd1 = currentFrame.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, currentFrame.PixelFormat);
                }
                catch
                {
                    return false;
                }
            }
            try
            {
                bd2 = lastFrame.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, lastFrame.PixelFormat);
            }
            catch
            {
                try
                {
                    lastFrame.UnlockBits(bd2);
                    bd2 = lastFrame.LockBits(new System.Drawing.Rectangle(0, 0, width, height), ImageLockMode.ReadOnly, lastFrame.PixelFormat);
                }
                catch
                {
                    return false;
                }
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

                // Point that indicates top left coordinates of the image.
                captureStartPoint = new System.Drawing.Point(left, top);


                boundingBox = new System.Drawing.Rectangle(left, top, right - left, bottom - top);
                currentFrame.UnlockBits(bd1);
                lastFrame.UnlockBits(bd2);

                return true;
            }
            else
            {
                currentFrame.UnlockBits(bd1);
                lastFrame.UnlockBits(bd2);
                return false;
            }
        }
    }
}
