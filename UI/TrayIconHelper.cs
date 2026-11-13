using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace WhoEatWifi.UI
{
    public static class TrayIconHelper
    {
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        public static Icon CreateWifiIcon()
        {
            try
            {
                using (Bitmap bmp = new Bitmap(32, 32))
                {
                    using (Graphics g = Graphics.FromImage(bmp))
                    {
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.Clear(Color.Transparent);

                        // Draw blue rounded square badge
                        using (GraphicsPath path = new GraphicsPath())
                        {
                            int radius = 8;
                            path.AddArc(0, 0, radius, radius, 180, 90);
                            path.AddArc(31 - radius, 0, radius, radius, 270, 90);
                            path.AddArc(31 - radius, 31 - radius, radius, radius, 0, 90);
                            path.AddArc(0, 31 - radius, radius, radius, 90, 90);
                            path.CloseFigure();

                            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(37, 99, 235))) // #2563EB
                            {
                                g.FillPath(bgBrush, path);
                            }
                        }

                        // Draw white Wi-Fi signal arcs
                        using (Pen whitePen = new Pen(Color.White, 2.5f))
                        {
                            whitePen.StartCap = LineCap.Round;
                            whitePen.EndCap = LineCap.Round;

                            // Outer arc
                            g.DrawArc(whitePen, 6, 7, 20, 20, 220, 100);
                            // Middle arc
                            g.DrawArc(whitePen, 10, 12, 12, 12, 220, 100);
                            // Dot
                            using (SolidBrush dotBrush = new SolidBrush(Color.White))
                            {
                                g.FillEllipse(dotBrush, 14, 21, 4, 4);
                            }
                        }
                    }

                    IntPtr hIcon = bmp.GetHicon();
                    Icon icon = (Icon)Icon.FromHandle(hIcon).Clone();
                    DestroyIcon(hIcon);
                    return icon;
                }
            }
            catch
            {
                return SystemIcons.Application;
            }
        }
    }
}
