using System;
using System.Runtime.InteropServices;
using System.Globalization;

namespace PackageExplorer {
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct WINDOWPLACEMENT {
        public int length;
        public int flags;
        public int showCmd;

        public POINT minPosition;
        public POINT maxPosition;
        public RECT normalPosition;

        public override string ToString() {
            return String.Format(
                CultureInfo.InvariantCulture,
                "{0}|{1}|{2}|{3}|{4}|{5}",
                length,
                flags,
                showCmd,
                minPosition,
                maxPosition,
                normalPosition);
        }

        public static WINDOWPLACEMENT Parse(string s) {
            string[] parts = s.Split('|');

            if (parts.Length != 6) {
                return new WINDOWPLACEMENT();
            }

            int flength = int.Parse(parts[0], CultureInfo.InvariantCulture);
            int fflags = int.Parse(parts[1], CultureInfo.InvariantCulture);
            int fshowCmd = int.Parse(parts[2], CultureInfo.InvariantCulture);
            POINT fminPosition = POINT.Parse(parts[3]);
            POINT fmaxPosition = POINT.Parse(parts[4]);
            RECT fnormalPosition = RECT.Parse(parts[5]);

            return new WINDOWPLACEMENT {
                length = flength,
                flags = fflags,
                showCmd = fshowCmd,
                minPosition = fminPosition,
                maxPosition = fmaxPosition,
                normalPosition = fnormalPosition};
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Width;
        public int Height;

        public RECT(int left, int top, int width, int height)
        {
            this.Left = left;
            this.Top = top;
            this.Width = width;
            this.Height = height;
        }

        public override string ToString() {
            return String.Format(CultureInfo.InvariantCulture, "{0};{1};{2};{3}", Left, Top, Width, Height);
        }

        public static RECT Parse(string s) {
            int[] ss = Array.ConvertAll<string, int>(s.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries), v => int.Parse(v, CultureInfo.InvariantCulture));
            return ss.Length == 4 ? new RECT(ss[0], ss[1], ss[2], ss[3]) : new RECT();
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT
    {
        public int X;
        public int Y;

        public POINT(int x, int y)
        {
            this.X = x;
            this.Y = y;
        }

        public override string ToString() {
            return String.Format(CultureInfo.InvariantCulture, "{0};{1}", X, Y);
        }

        public static POINT Parse(string s) {
            int[] ss = Array.ConvertAll<string, int>(s.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries), v => int.Parse(v, CultureInfo.InvariantCulture));
            return ss.Length == 2 ? new POINT(ss[0], ss[1]) : new POINT();
        }
    }
}