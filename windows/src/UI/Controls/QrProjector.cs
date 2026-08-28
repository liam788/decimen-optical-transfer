using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using OpticalTransfer.Core;
using OpticalTransfer.UI.Theme;

namespace OpticalTransfer.UI.Controls
{
    public class QrProjector : Control
    {
        private Image _imageControl;
        private Border _borderContainer;

        public static readonly DependencyProperty CurrentFrameProperty =
            DependencyProperty.Register("CurrentFrame", typeof(QrBitmap), typeof(QrProjector),
                new FrameworkPropertyMetadata(null, (d, e) => ((QrProjector)d).UpdateFrame((QrBitmap)e.NewValue)));

        public QrBitmap CurrentFrame
        {
            get { return (QrBitmap)GetValue(CurrentFrameProperty); }
            set { SetValue(CurrentFrameProperty, value); }
        }

        public QrProjector()
        {
            Background = BrandColors.OpticalBlackBrush;
            _imageControl = new Image
            {
                Stretch = Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            RenderOptions.SetBitmapScalingMode(_imageControl, BitmapScalingMode.NearestNeighbor);

            _borderContainer = new Border
            {
                Background = Brushes.White,
                Padding = new Thickness(16),
                CornerRadius = new CornerRadius(12),
                Child = _imageControl,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };

            AddVisualChild(_borderContainer);
        }

        public void SetFrame(QrBitmap bitmap)
        {
            if (bitmap == null) return;
            WriteableBitmap wbm = bitmap.ToBitmap(8, 2);
            _imageControl.Source = wbm;
        }

        private void UpdateFrame(QrBitmap bitmap)
        {
            SetFrame(bitmap);
        }

        protected override int VisualChildrenCount
        {
            get { return 1; }
        }

        protected override Visual GetVisualChild(int index)
        {
            return _borderContainer;
        }

        protected override Size MeasureOverride(Size constraint)
        {
            _borderContainer.Measure(constraint);
            return _borderContainer.DesiredSize;
        }

        protected override Size ArrangeOverride(Size arrangeBounds)
        {
            _borderContainer.Arrange(new Rect(arrangeBounds));
            return arrangeBounds;
        }
    }
}
