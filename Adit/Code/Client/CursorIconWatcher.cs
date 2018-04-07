using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Windows.Threading;
using Win32_Classes;

namespace Adit.Code.Client
{
    /// <summary>
    /// A class that can be used to watch for cursor icon changes.
    /// </summary>
    public class CursorIconWatcher
    {
        public static CursorIconWatcher Current { get; private set; } = new CursorIconWatcher();
        public event EventHandler<Icon> OnChange;
        private System.Timers.Timer ChangeTimer { get; set; }
        private Icon PreviousIcon { get; set; }
        private User32.CursorInfo CursorInfo;
        private CursorIconWatcher()
        {
            ChangeTimer = new System.Timers.Timer(1);
            ChangeTimer.Elapsed += ChangeTimer_Elapsed;
            ChangeTimer.Start();
        }

        private void ChangeTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (OnChange == null)
            {
                return;
            }
            try
            {
                CursorInfo = new User32.CursorInfo();
                CursorInfo.cbSize = Marshal.SizeOf(CursorInfo);
                User32.GetCursorInfo(out CursorInfo);
                if (CursorInfo.flags == User32.CURSOR_SHOWING)
                {
                    using (var icon = Icon.FromHandle(CursorInfo.hCursor))
                    {
                        if (AreIconsDifferent(PreviousIcon, icon))
                        {
                            PreviousIcon = icon;
                            OnChange(this, icon);
                        }
                    }
                }
            }
            catch{ }
        }

        private bool AreIconsDifferent(Icon original, Icon current)
        {
            if (original?.Size != current?.Size)
            {
                return true;
            }
            using (var bitmap1 = original.ToBitmap())
            {
                using (var bitmap2 = current.ToBitmap())
                {
                    using (var ms1 = new System.IO.MemoryStream())
                    {
                        using (var ms2 = new System.IO.MemoryStream())
                        {
                            bitmap1.Save(ms1, System.Drawing.Imaging.ImageFormat.Jpeg);
                            bitmap2.Save(ms2, System.Drawing.Imaging.ImageFormat.Jpeg);
                            ms1.Position = 0;
                            ms2.Position = 0;
                            for (int i = 0; i < ms1.Length; i++)
                            {
                                if (ms1.ReadByte() != ms2.ReadByte())
                                {
                                    return true;
                                }
                            }
                        }
                        return false;
                    }
                }
            }
        }
    }
}
