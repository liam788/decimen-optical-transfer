using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using OpticalTransfer.UI.Theme;

namespace OpticalTransfer.UI.Controls
{
    public class OpticalTransferRing : FrameworkElement
    {
        public static readonly DependencyProperty ProgressProperty =
            DependencyProperty.Register("Progress", typeof(double), typeof(OpticalTransferRing),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty SolvedCountProperty =
            DependencyProperty.Register("SolvedCount", typeof(uint), typeof(OpticalTransferRing),
                new FrameworkPropertyMetadata(0u, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TotalKProperty =
            DependencyProperty.Register("TotalK", typeof(uint), typeof(OpticalTransferRing),
                new FrameworkPropertyMetadata(1u, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsTransferringProperty =
            DependencyProperty.Register("IsTransferring", typeof(bool), typeof(OpticalTransferRing),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty IsCompleteProperty =
            DependencyProperty.Register("IsComplete", typeof(bool), typeof(OpticalTransferRing),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.AffectsRender));

        public double Progress
        {
            get { return (double)GetValue(ProgressProperty); }
            set { SetValue(ProgressProperty, value); }
        }

        public uint SolvedCount
        {
            get { return (uint)GetValue(SolvedCountProperty); }
            set { SetValue(SolvedCountProperty, value); }
        }

        public uint TotalK
        {
            get { return (uint)GetValue(TotalKProperty); }
            set { SetValue(TotalKProperty, value); }
        }

        public bool IsTransferring
        {
            get { return (bool)GetValue(IsTransferringProperty); }
            set { SetValue(IsTransferringProperty, value); }
        }

        public bool IsComplete
        {
            get { return (bool)GetValue(IsCompleteProperty); }
            set { SetValue(IsCompleteProperty, value); }
        }

        public OpticalTransferRing()
        {
            Width = 260;
            Height = 260;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            Point center = new Point(w / 2.0, h / 2.0);
            double radius = Math.Min(w, h) / 2.0 - 16;
            if (radius <= 0) return;

            // 1. Background Track
            Pen trackPen = new Pen(BrandColors.BorderBrush, 6.0);
            dc.DrawEllipse(null, trackPen, center, radius, radius);

            // 2. Active Progress Arc
            double progress = Math.Max(0.0, Math.Min(1.0, Progress));
            if (progress > 0.001)
            {
                Brush progressBrush = IsComplete ? BrandColors.SuccessBrush : BrandColors.OpticalGreenBrush;
                Pen arcPen = new Pen(progressBrush, 8.0);
                arcPen.StartLineCap = PenLineCap.Round;
                arcPen.EndLineCap = PenLineCap.Round;

                if (progress >= 0.999)
                {
                    dc.DrawEllipse(null, arcPen, center, radius, radius);
                }
                else
                {
                    double startAngle = -90.0;
                    double sweepAngle = progress * 360.0;
                    double endAngle = startAngle + sweepAngle;

                    double startRad = startAngle * Math.PI / 180.0;
                    double endRad = endAngle * Math.PI / 180.0;

                    Point startPoint = new Point(center.X + radius * Math.Cos(startRad), center.Y + radius * Math.Sin(startRad));
                    Point endPoint = new Point(center.X + radius * Math.Cos(endRad), center.Y + radius * Math.Sin(endRad));

                    StreamGeometry geom = new StreamGeometry();
                    using (StreamGeometryContext ctx = geom.Open())
                    {
                        ctx.BeginFigure(startPoint, false, false);
                        ctx.ArcTo(endPoint, new Size(radius, radius), 0.0, sweepAngle > 180.0, SweepDirection.Clockwise, true, false);
                    }
                    geom.Freeze();
                    dc.DrawGeometry(null, arcPen, geom);
                }
            }

            // 3. Center Text / Metrics
            if (IsComplete)
            {
                FormattedText completeText = new FormattedText(
                    "TRANSFER COMPLETE",
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI, Inter"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                    14,
                    BrandColors.SuccessBrush,
                    1.0
                );
                dc.DrawText(completeText, new Point(center.X - completeText.Width / 2.0, center.Y - completeText.Height / 2.0));
            }
            else
            {
                int pct = (int)(progress * 100.0);
                FormattedText pctText = new FormattedText(
                    string.Format("{0}%", pct),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI, Space Grotesk, Inter"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                    38,
                    BrandColors.TextEmphasisBrush,
                    1.0
                );
                dc.DrawText(pctText, new Point(center.X - pctText.Width / 2.0, center.Y - pctText.Height / 2.0 - 12));

                uint solved = SolvedCount;
                uint total = TotalK > 0 ? TotalK : 1;
                FormattedText rankText = new FormattedText(
                    string.Format("{0} / {1} BLOCKS", solved, total),
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    new Typeface(new FontFamily("Segoe UI, Inter"), FontStyles.Normal, FontWeights.SemiBold, FontStretches.Normal),
                    11,
                    BrandColors.OpticalGreenBrush,
                    1.0
                );
                dc.DrawText(rankText, new Point(center.X - rankText.Width / 2.0, center.Y + 22));
            }
        }
    }
}
