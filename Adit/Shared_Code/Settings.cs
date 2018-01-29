using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Shared_Code
{
    public static class Settings
    {
        public static StartupModes StartupMode { get; set; } = StartupModes.ModeSelect;

        public static void Load() { }
        public static void Save() { }
        public enum StartupModes
        {
            ModeSelect,
            Client,
            Server,
            Viewer,
            Notifier
        }
    }
}
