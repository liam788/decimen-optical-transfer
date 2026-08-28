using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using OpticalTransfer.Core;
using OpticalTransfer.UI.Controls;
using OpticalTransfer.UI.Theme;

namespace OpticalTransfer.UI
{
    public class ProjectorWindow : Window
    {
        private readonly QrProjector _projector;
        private readonly TextBlock _statusText;

        public ProjectorWindow()
        {
            Title = "Optical Stream Projector - Fullscreen Optical Transfer";
            Width = 800;
            Height = 800;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = BrandColors.OpticalBlackBrush;

            Grid mainGrid = new Grid();
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _projector = new QrProjector();
            Grid.SetRow(_projector, 0);
            mainGrid.Children.Add(_projector);

            Border bottomBar = new Border
            {
                Background = BrandColors.SecondaryBgBrush,
                BorderBrush = BrandColors.BorderBrush,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16, 12, 16, 12)
            };

            DockPanel dock = new DockPanel();
            _statusText = new TextBlock
            {
                Text = "Press F11 for Fullscreen • ESC to Exit",
                Foreground = BrandColors.TextMutedBrush,
                FontSize = 12,
                VerticalAlignment = VerticalAlignment.Center
            };
            DockPanel.SetDock(_statusText, Dock.Left);
            dock.Children.Add(_statusText);

            Button closeBtn = new Button
            {
                Content = "Close Projector",
                Background = BrandColors.SurfaceElevatedBrush,
                Foreground = BrandColors.TextPrimaryBrush,
                BorderBrush = BrandColors.BorderBrush,
                Padding = new Thickness(12, 6, 12, 6),
                HorizontalAlignment = HorizontalAlignment.Right
            };
            closeBtn.Click += (s, e) => Close();
            DockPanel.SetDock(closeBtn, Dock.Right);
            dock.Children.Add(closeBtn);

            bottomBar.Child = dock;
            Grid.SetRow(bottomBar, 1);
            mainGrid.Children.Add(bottomBar);

            Content = mainGrid;

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.F11)
                {
                    if (WindowState == WindowState.Maximized && WindowStyle == WindowStyle.None)
                    {
                        WindowStyle = WindowStyle.SingleBorderWindow;
                        WindowState = WindowState.Normal;
                    }
                    else
                    {
                        WindowStyle = WindowStyle.None;
                        WindowState = WindowState.Maximized;
                    }
                }
                else if (e.Key == Key.Escape)
                {
                    Close();
                }
            };
        }

        public void UpdateFrame(QrBitmap bitmap, string status)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                _projector.SetFrame(bitmap);
                if (!string.IsNullOrEmpty(status))
                {
                    _statusText.Text = status + " • Press F11 for Fullscreen • ESC to Exit";
                }
            }));
        }
    }
}
