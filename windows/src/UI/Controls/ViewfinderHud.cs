using System;
using System.Windows;
using System.Windows.Media;
using OpticalTransfer.UI.Theme;

namespace OpticalTransfer.UI.Controls
{
    public class ViewfinderHud : FrameworkElement
    {
        public ViewfinderHud()
        {
            IsHitTestVisible = false;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            Point center = new Point(w / 2.0, h / 2.0);

            // 1. Crosshairs
            Brush axisBrush = new SolidColorBrush(Color.FromArgb(40, 152, 184, 120));
            Pen axisPen = new Pen(axisBrush, 1.0);
            dc.DrawLine(axisPen, new Point(center.X - 180, center.Y), new Point(center.X + 180, center.Y));
            dc.DrawLine(axisPen, new Point(center.X, center.Y - 180), new Point(center.X, center.Y + 180));

            // 2. Corner Brackets (340x340 bounding box)
            double bWidth = 340.0;
            double bHeight = 340.0;
            double left = center.X - bWidth / 2.0;
            double top = center.Y - bHeight / 2.0;
            double right = left + bWidth;
            double bottom = top + bHeight;
            double bSize = 24.0;

            Brush bracketBrush = new SolidColorBrush(Color.FromArgb(180, 152, 184, 120));
            Pen bracketPen = new Pen(bracketBrush, 2.0);

            // Top-Left
            dc.DrawLine(bracketPen, new Point(left, top), new Point(left + bSize, top));
            dc.DrawLine(bracketPen, new Point(left, top), new Point(left, top + bSize));

            // Top-Right
            dc.DrawLine(bracketPen, new Point(right, top), new Point(right - bSize, top));
            dc.DrawLine(bracketPen, new Point(right, top), new Point(right, top + bSize));

            // Bottom-Left
            dc.DrawLine(bracketPen, new Point(left, bottom), new Point(left + bSize, bottom));
            dc.DrawLine(bracketPen, new Point(left, bottom), new Point(left, bottom - bSize));

            // Bottom-Right
            dc.DrawLine(bracketPen, new Point(right, bottom), new Point(right - bSize, bottom));
            dc.DrawLine(bracketPen, new Point(right, bottom), new Point(right, bottom - bSize));
        }
    }
}
