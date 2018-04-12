using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Adit.Code.Shared
{
    public class CursorMap
    {
        public string WindowsCursorName { get; private set; }
        public int CursorHandle { get; private set; }
        public string CSSCursorName { get; private set; }

        public static string GetCSSNameByHandle(int windowsHandle)
        {
            return AllCursors.Find(x => x?.CursorHandle == windowsHandle)?.CSSCursorName;
        }

        public static List<CursorMap> AllCursors
        {
            get
            {
                return new List<CursorMap>()
                {
                    new CursorMap() {
                        WindowsCursorName= "AppStarting",
                        CursorHandle= 65561,
                        CSSCursorName= "wait"
                    },
                    new CursorMap() {
                        WindowsCursorName= "Arrow",
                        CursorHandle= 65539,
                        CSSCursorName= "default"
                    },
                    new CursorMap() {
                        WindowsCursorName= "Arrow",
                        CursorHandle= 0,
                        CSSCursorName= "default"
                    },
                    new CursorMap() {
                        WindowsCursorName= "ArrowCD",
                        CursorHandle= 65571,
                        CSSCursorName= "progress"
                    },
                    new CursorMap() {
                        WindowsCursorName= "Cross",
                        CursorHandle= 65545,
                        CSSCursorName= "crosshair"
                    },
                    new CursorMap() {
                        WindowsCursorName= "Hand",
                        CursorHandle= 65567,
                        CSSCursorName= "pointer"
                    },
                    new CursorMap() {
                        WindowsCursorName= "IBeam",
                        CursorHandle= 65541,
                        CSSCursorName= "text"
                    },
                    new CursorMap() {
                        WindowsCursorName= "No",
                        CursorHandle= 65559,
                        CSSCursorName= "not-allowed"
                    },
                    new CursorMap() {
                        WindowsCursorName= "None",
                        CursorHandle= -1,
                        CSSCursorName= "default"
                    },
                    new CursorMap() {
                        WindowsCursorName= "Pen",
                        CursorHandle= 65565,
                        CSSCursorName= "default"
                    },
                    new CursorMap() {
                        WindowsCursorName= "ScrollAll",
                        CursorHandle= 473367439,
                        CSSCursorName= "all-scroll"
                    },
                    new CursorMap() {
                        WindowsCursorName= "ScrollE",
                        CursorHandle= 305726465,
                        CSSCursorName= "e-resize"
                    },
                    new CursorMap() {
                        WindowsCursorName= "ScrollN",
                        CursorHandle= 362612325,
                        CSSCursorName= "n-resize"
                    },
                    new CursorMap() {
                        WindowsCursorName= "ScrollNE",
                        CursorHandle= 3150655,
                        CSSCursorName= "ne-resize"
                    },
                    new CursorMap() {
                        WindowsCursorName= "ScrollNS",
                        CursorHandle= 136186903,
                        CSSCursorName= "ns-resize"
                    },
                    new CursorMap() {
                        WindowsCursorName= "ScrollNW",
                        CursorHandle= 111937855,
                        CSSCursorName= "nw-resize"
                    },
                    new CursorMap() {
                        WindowsCursorName= "ScrollS",
                        CursorHandle= 316083885,
                        CSSCursorName= "s-resize"
                    },
                    new CursorMap() {
                        WindowsCursorName= "ScrollSE",
                        CursorHandle= 691277305,
                        CSSCursorName= "se-resize"
                    },
                    new CursorMap() {
                        WindowsCursorName= "ScrollSW",
                        CursorHandle= 182915687,
                        CSSCursorName= "sw-resize"
                    },
                    new CursorMap() {
                        WindowsCursorName= "ScrollW",
                        CursorHandle= 46994179,
                        CSSCursorName= "w-resize"
                    },
                    new CursorMap() {
                        WindowsCursorName= "ScrollWE",
                        CursorHandle= 67310275,
                        CSSCursorName= "ew-resize"
                    },
                    new CursorMap() {
                        WindowsCursorName= "SizeAll",
                        CursorHandle= 65557,
                        CSSCursorName= "move"
                    },
                    new CursorMap() {
                        WindowsCursorName= "SizeNESW",
                        CursorHandle= 65551,
                        CSSCursorName= "nesw-resize"
                    },
                    new CursorMap() {
                        WindowsCursorName= "SizeNS",
                        CursorHandle= 65555,
                        CSSCursorName= "ns-resize"
                    },
                    new CursorMap() {
                        WindowsCursorName= "SizeNWSE",
                        CursorHandle= 65549,
                        CSSCursorName= "nwse-resize"
                    },
                    new CursorMap() {
                        WindowsCursorName= "SizeWE",
                        CursorHandle= 65553,
                        CSSCursorName= "ew-resize"
                    },
                    new CursorMap() {
                        WindowsCursorName= "UpArrow",
                        CursorHandle= 65547,
                        CSSCursorName= "n-resize"
                    },
                    new CursorMap() {
                        WindowsCursorName= "Wait",
                        CursorHandle= 65543,
                        CSSCursorName= "wait"
                    }
                };
            }
        }
    }
}
