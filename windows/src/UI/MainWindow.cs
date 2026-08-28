using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Microsoft.Win32;
using OpticalTransfer.Core;
using OpticalTransfer.HAL;
using OpticalTransfer.State;
using OpticalTransfer.UI.Controls;
using OpticalTransfer.UI.Theme;

namespace OpticalTransfer.UI
{
    public class MainWindow : Window, ISessionObserver
    {
        // Controllers & HAL
        private readonly WindowsCameraProvider _cameraProvider;
        private TxSessionController _txController;
        private RxSessionController _rxController;

        // UI Views
        private Grid _mainGrid;
        private StackPanel _navPanel;
        private ContentControl _viewContainer;

        private Grid _dashboardView;
        private Grid _receiveView;
        private Grid _sendView;
        private Grid _historyView;

        // Receiver UI Elements
        private OpticalTransferRing _rxRing;
        private Border _rxViewfinderBorder;
        private Grid _rxNoCameraPlaceholder;
        private TextBlock _rxInstantFpsText;
        private TextBlock _rxGoodputText;
        private TextBlock _rxIngestedText;
        private TextBlock _rxSolvedRankText;
        private Border _rxFileCard;
        private TextBlock _rxFileNameText;
        private TextBlock _rxFileDetailsText;
        private TextBlock _rxFilePathText;
        private Button _rxShowInExplorerBtn;
        private ComboBox _rxCameraCombo;
        private string _lastSavedFilePath = null;

        // Sender UI Elements
        private QrProjector _txProjector;
        private Border _txPlaceholderCard;
        private TextBlock _txFileNameText;
        private TextBlock _txFileSizeText;
        private TextBlock _txKBlocksText;
        private TextBlock _txFpsText;
        private Slider _txFpsSlider;
        private Button _txStartBtn;
        private Button _txPauseBtn;
        private Button _txDetachBtn;
        private ProjectorWindow _detachedProjector;
        private byte[] _selectedFileBytes;
        private string _selectedFileName;

        // State Tracking
        private int _currentNavIndex = 0;
        private readonly Button[] _navButtons = new Button[4];
        private readonly Border[] _navIndicators = new Border[4];

        public MainWindow()
        {
            Title = "Optical Transfer - Zero-Network Visual Data Stream";
            Width = 980;
            Height = 640;
            MinWidth = 760;
            MinHeight = 500;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = BrandColors.OpticalBlackBrush;

            // Set official application logo icon
            BitmapImage logo = LogoAsset.GetLogoImage();
            if (logo != null)
            {
                this.Icon = logo;
            }

            _cameraProvider = new WindowsCameraProvider();
            _txController = new TxSessionController(this);
            _rxController = new RxSessionController(_cameraProvider, this);

            InitializeLayout();
            NavigateTo(0);
        }

        private void InitializeLayout()
        {
            _mainGrid = new Grid();
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(220) });
            _mainGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // 1. Left Navigation Rail
            Border navBorder = new Border
            {
                Background = BrandColors.SecondaryBgBrush,
                BorderBrush = BrandColors.BorderBrush,
                BorderThickness = new Thickness(0, 0, 1, 0)
            };

            Grid navGrid = new Grid();
            navGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            navGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Items
            navGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Status Footer

            // Brand Header with Official Logo Icon
            StackPanel headerStack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(18, 22, 18, 22),
                VerticalAlignment = VerticalAlignment.Center
            };

            Border iconBorder = new Border
            {
                Width = 36,
                Height = 36,
                Background = BrandColors.SurfaceElevatedBrush,
                CornerRadius = new CornerRadius(8),
                BorderBrush = BrandColors.BorderBrush,
                BorderThickness = new Thickness(1),
                ClipToBounds = true
            };

            BitmapImage logoImg = LogoAsset.GetLogoImage();
            if (logoImg != null)
            {
                Image img = new Image
                {
                    Source = logoImg,
                    Width = 32,
                    Height = 32,
                    Stretch = Stretch.Uniform,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                iconBorder.Child = img;
            }
            else
            {
                TextBlock iconText = new TextBlock
                {
                    Text = "OT",
                    Foreground = BrandColors.OpticalGreenBrush,
                    FontWeight = FontWeights.Bold,
                    FontSize = 14,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                iconBorder.Child = iconText;
            }
            headerStack.Children.Add(iconBorder);

            StackPanel titleStack = new StackPanel { Margin = new Thickness(12, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };
            TextBlock brandTitle = new TextBlock
            {
                Text = "OPTICAL",
                Foreground = BrandColors.TextEmphasisBrush,
                FontWeight = FontWeights.Bold,
                FontSize = 13
            };
            TextBlock brandSub = new TextBlock
            {
                Text = "TRANSFER",
                Foreground = BrandColors.OpticalGreenBrush,
                FontWeight = FontWeights.Bold,
                FontSize = 11
            };
            titleStack.Children.Add(brandTitle);
            titleStack.Children.Add(brandSub);
            headerStack.Children.Add(titleStack);

            Grid.SetRow(headerStack, 0);
            navGrid.Children.Add(headerStack);

            // Nav Items
            _navPanel = new StackPanel { Margin = new Thickness(10, 8, 10, 8) };
            _navButtons[0] = CreateNavButton(0, "Dashboard", out _navIndicators[0]);
            _navButtons[1] = CreateNavButton(1, "Receive Stream", out _navIndicators[1]);
            _navButtons[2] = CreateNavButton(2, "Send Stream", out _navIndicators[2]);
            _navButtons[3] = CreateNavButton(3, "Transfers & History", out _navIndicators[3]);

            for (int i = 0; i < _navButtons.Length; i++)
            {
                _navPanel.Children.Add(_navButtons[i]);
            }

            Grid.SetRow(_navPanel, 1);
            navGrid.Children.Add(_navPanel);

            // Footer
            Border footerBorder = new Border
            {
                BorderBrush = BrandColors.BorderBrush,
                BorderThickness = new Thickness(0, 1, 0, 0),
                Padding = new Thickness(16, 14, 16, 14)
            };
            StackPanel footerStack = new StackPanel { Orientation = Orientation.Horizontal };
            Border statusDot = new Border
            {
                Width = 8,
                Height = 8,
                CornerRadius = new CornerRadius(4),
                Background = BrandColors.SuccessBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0)
            };
            TextBlock statusLabel = new TextBlock
            {
                Text = "Engine Ready • Air-Gapped",
                Foreground = BrandColors.TextMutedBrush,
                FontSize = 11,
                VerticalAlignment = VerticalAlignment.Center
            };
            footerStack.Children.Add(statusDot);
            footerStack.Children.Add(statusLabel);
            footerBorder.Child = footerStack;

            Grid.SetRow(footerBorder, 2);
            navGrid.Children.Add(footerBorder);

            navBorder.Child = navGrid;
            Grid.SetColumn(navBorder, 0);
            _mainGrid.Children.Add(navBorder);

            // 2. Main Content Container
            _viewContainer = new ContentControl();
            Grid.SetColumn(_viewContainer, 1);
            _mainGrid.Children.Add(_viewContainer);

            // Initialize all view instances
            BuildDashboardView();
            BuildReceiveView();
            BuildSendView();
            BuildHistoryView();

            Content = _mainGrid;
        }

        private Button CreateNavButton(int index, string label, out Border indicator)
        {
            int capturedIndex = index;
            Button btn = new Button
            {
                Height = 38,
                Margin = new Thickness(0, 3, 0, 3),
                Style = Styles.CreateNavButtonStyle()
            };

            Grid g = new Grid();
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(4) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            indicator = new Border
            {
                Background = BrandColors.OpticalGreenBrush,
                CornerRadius = new CornerRadius(2),
                Visibility = Visibility.Collapsed
            };
            Grid.SetColumn(indicator, 0);
            g.Children.Add(indicator);

            TextBlock tb = new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 13
            };
            Grid.SetColumn(tb, 2);
            g.Children.Add(tb);

            btn.Content = g;
            btn.Click += (s, e) => NavigateTo(capturedIndex);
            return btn;
        }

        private void NavigateTo(int index)
        {
            _currentNavIndex = index;
            for (int i = 0; i < _navButtons.Length; i++)
            {
                bool isSel = (i == index);
                _navButtons[i].Background = isSel ? BrandColors.SurfaceElevatedBrush : Brushes.Transparent;
                _navButtons[i].Foreground = isSel ? BrandColors.TextEmphasisBrush : BrandColors.TextSecondaryBrush;
                _navButtons[i].BorderBrush = isSel ? BrandColors.BorderDisabledBrush : Brushes.Transparent;
                _navIndicators[i].Visibility = isSel ? Visibility.Visible : Visibility.Collapsed;
            }

            switch (index)
            {
                case 0: _viewContainer.Content = _dashboardView; break;
                case 1:
                    _viewContainer.Content = _receiveView;
                    StartReceiveSession();
                    break;
                case 2: _viewContainer.Content = _sendView; break;
                case 3: _viewContainer.Content = _historyView; break;
            }
        }

        #region Dashboard View
        private void BuildDashboardView()
        {
            _dashboardView = new Grid { Margin = new Thickness(24) };
            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            StackPanel stack = new StackPanel();

            // Hero Card
            Border heroCard = new Border
            {
                Background = BrandColors.SurfaceCardBrush,
                BorderBrush = BrandColors.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(24)
            };

            StackPanel heroContent = new StackPanel();
            TextBlock heroTitle = new TextBlock
            {
                Text = "Zero-Network Optical Transfer",
                Foreground = BrandColors.TextEmphasisBrush,
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 6)
            };
            TextBlock heroDesc = new TextBlock
            {
                Text = "Transfer files directly between PC, Phone, and Mac using fountain-coded light streams. No Wi-Fi, no Bluetooth, no cloud pairing.",
                Foreground = BrandColors.TextSecondaryBrush,
                FontSize = 13,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 18)
            };

            WrapPanel heroBtns = new WrapPanel();
            Button sendBtn = new Button
            {
                Content = "Send File via Screen",
                Style = Styles.CreatePrimaryButtonStyle(),
                Margin = new Thickness(0, 0, 12, 8)
            };
            sendBtn.Click += (s, e) =>
            {
                NavigateTo(2);
                BrowseAndSendFile();
            };

            Button rxBtn = new Button
            {
                Content = "Receive from Camera",
                Style = Styles.CreateSecondaryButtonStyle(),
                Margin = new Thickness(0, 0, 12, 8)
            };
            rxBtn.Click += (s, e) => NavigateTo(1);

            heroBtns.Children.Add(sendBtn);
            heroBtns.Children.Add(rxBtn);

            heroContent.Children.Add(heroTitle);
            heroContent.Children.Add(heroDesc);
            heroContent.Children.Add(heroBtns);
            heroCard.Child = heroContent;
            stack.Children.Add(heroCard);

            // Workflows Section
            TextBlock workflowLabel = new TextBlock
            {
                Text = "WORKFLOWS & DIAGNOSTICS",
                Foreground = BrandColors.TextMutedBrush,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 24, 0, 12)
            };
            stack.Children.Add(workflowLabel);

            Grid gridCards = new Grid();
            gridCards.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            gridCards.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            gridCards.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            Border card1 = CreateFeatureCard(
                "PC to Mobile Stream",
                "Select any file on Windows. The screen projects rapid fountain QR droplets for your phone camera to scan.",
                "Start Send Stream",
                new Action(() => { NavigateTo(2); BrowseAndSendFile(); })
            );
            Grid.SetColumn(card1, 0);
            gridCards.Children.Add(card1);

            Border card2 = CreateFeatureCard(
                "Mobile to PC Stream",
                "Hold your phone screen facing your webcam. The in-app receiver captures and reassembles files automatically.",
                "Open Receiver",
                new Action(() => NavigateTo(1))
            );
            Grid.SetColumn(card2, 2);
            gridCards.Children.Add(card2);

            stack.Children.Add(gridCards);
            scroll.Content = stack;
            _dashboardView.Children.Add(scroll);
        }

        private Border CreateFeatureCard(string title, string desc, string actionText, Action action)
        {
            Border b = new Border
            {
                Background = BrandColors.SurfaceCardBrush,
                BorderBrush = BrandColors.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(20)
            };

            StackPanel sp = new StackPanel();
            TextBlock t = new TextBlock
            {
                Text = title,
                Foreground = BrandColors.TextEmphasisBrush,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 6)
            };
            TextBlock d = new TextBlock
            {
                Text = desc,
                Foreground = BrandColors.TextSecondaryBrush,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 16)
            };
            Button actBtn = new Button
            {
                Content = actionText + "  →",
                Background = Brushes.Transparent,
                Foreground = BrandColors.OpticalGreenBrush,
                BorderThickness = new Thickness(0),
                FontWeight = FontWeights.Bold,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Left,
                Cursor = Cursors.Hand
            };
            actBtn.Click += (s, e) => action();

            sp.Children.Add(t);
            sp.Children.Add(d);
            sp.Children.Add(actBtn);
            b.Child = sp;
            return b;
        }
        #endregion

        #region Receive View
        private void BuildReceiveView()
        {
            _receiveView = new Grid();
            _receiveView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _receiveView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });

            // Left: Viewfinder & Ring Canvas
            Grid leftGrid = new Grid { Margin = new Thickness(18) };
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            leftGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            _rxViewfinderBorder = new Border
            {
                Background = BrandColors.SurfaceCardBrush,
                BorderBrush = BrandColors.BorderBrush,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(14),
                ClipToBounds = true
            };

            Grid vfGrid = new Grid();
            vfGrid.Children.Add(new ViewfinderHud());

            _rxRing = new OpticalTransferRing();
            vfGrid.Children.Add(_rxRing);

            // Placeholder when no camera is detected
            _rxNoCameraPlaceholder = new Grid
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Visibility = Visibility.Collapsed
            };
            StackPanel noCamStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            TextBlock noCamTitle = new TextBlock
            {
                Text = "No Physical Webcam Detected",
                Foreground = BrandColors.TextEmphasisBrush,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };
            TextBlock noCamSub = new TextBlock
            {
                Text = "Connect a USB webcam or click below to ingest captured frame files directly.",
                Foreground = BrandColors.TextSecondaryBrush,
                FontSize = 12,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                MaxWidth = 340,
                Margin = new Thickness(0, 0, 0, 16)
            };
            Button ingestDirectBtn = new Button
            {
                Content = "📥 Ingest Droplet / Frame Files",
                Style = Styles.CreatePrimaryButtonStyle(),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            ingestDirectBtn.Click += (s, e) => BrowseAndIngestFrames();
            noCamStack.Children.Add(noCamTitle);
            noCamStack.Children.Add(noCamSub);
            noCamStack.Children.Add(ingestDirectBtn);
            _rxNoCameraPlaceholder.Children.Add(noCamStack);
            vfGrid.Children.Add(_rxNoCameraPlaceholder);

            // Signal Badge (Top Left)
            Border badge = new Border
            {
                Background = BrandColors.SecondaryBgBrush,
                BorderBrush = BrandColors.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10, 5, 10, 5),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(14)
            };
            StackPanel badgeStack = new StackPanel { Orientation = Orientation.Horizontal };
            Border badgeDot = new Border
            {
                Width = 8,
                Height = 8,
                CornerRadius = new CornerRadius(4),
                Background = BrandColors.OpticalGreenBrush,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 6, 0)
            };
            TextBlock badgeLabel = new TextBlock
            {
                Text = "SCANNER READY",
                Foreground = BrandColors.TextPrimaryBrush,
                FontSize = 11,
                FontWeight = FontWeights.Bold
            };
            badgeStack.Children.Add(badgeDot);
            badgeStack.Children.Add(badgeLabel);
            badge.Child = badgeStack;
            vfGrid.Children.Add(badge);

            // Camera Selector (Top Right)
            _rxCameraCombo = new ComboBox
            {
                Background = BrandColors.SecondaryBgBrush,
                Foreground = BrandColors.TextPrimaryBrush,
                BorderBrush = BrandColors.BorderBrush,
                Width = 160,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(14)
            };
            _rxCameraCombo.SelectionChanged += (s, e) =>
            {
                if (_rxCameraCombo.SelectedIndex >= 0)
                {
                    _cameraProvider.SelectDevice(_rxCameraCombo.SelectedIndex);
                }
            };
            vfGrid.Children.Add(_rxCameraCombo);

            _rxViewfinderBorder.Child = vfGrid;
            Grid.SetRow(_rxViewfinderBorder, 0);
            leftGrid.Children.Add(_rxViewfinderBorder);

            // Bottom Action Controls (Responsive WrapPanel)
            WrapPanel bottomActions = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 14, 0, 0)
            };

            Button ingestBtn = new Button
            {
                Content = "📥 Ingest Frame Chunks",
                Style = Styles.CreateSecondaryButtonStyle(),
                Margin = new Thickness(0, 0, 10, 6)
            };
            ingestBtn.Click += (s, e) => BrowseAndIngestFrames();

            Button resetBtn = new Button
            {
                Content = "🔄 Reset Session",
                Style = Styles.CreateSecondaryButtonStyle(),
                Margin = new Thickness(0, 0, 10, 6)
            };
            resetBtn.Click += (s, e) => StartReceiveSession();

            bottomActions.Children.Add(ingestBtn);
            bottomActions.Children.Add(resetBtn);
            Grid.SetRow(bottomActions, 1);
            leftGrid.Children.Add(bottomActions);

            Grid.SetColumn(leftGrid, 0);
            _receiveView.Children.Add(leftGrid);

            // Right: Telemetry & File Inspection Panel
            Border rightPanel = new Border
            {
                Background = BrandColors.SecondaryBgBrush,
                BorderBrush = BrandColors.BorderBrush,
                BorderThickness = new Thickness(1, 0, 0, 0),
                Padding = new Thickness(18)
            };

            ScrollViewer scroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            StackPanel rightStack = new StackPanel();
            TextBlock telemHeader = new TextBlock
            {
                Text = "REAL-TIME TELEMETRY",
                Foreground = BrandColors.TextMutedBrush,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 14)
            };
            rightStack.Children.Add(telemHeader);

            // Metrics Grid (2x2)
            Grid mGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
            mGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
            mGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            mGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            mGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var t1 = CreateMetricTile("CAPTURE FPS", out _rxInstantFpsText, "0.0", "Hz", BrandColors.OpticalGreenBrush);
            Grid.SetRow(t1, 0); Grid.SetColumn(t1, 0);
            mGrid.Children.Add(t1);

            var t2 = CreateMetricTile("GOODPUT", out _rxGoodputText, "0.0", "KB/s", BrandColors.TextEmphasisBrush);
            Grid.SetRow(t2, 0); Grid.SetColumn(t2, 2);
            mGrid.Children.Add(t2);

            var t3 = CreateMetricTile("INGESTED", out _rxIngestedText, "0", "frames", BrandColors.TextPrimaryBrush);
            Grid.SetRow(t3, 2); Grid.SetColumn(t3, 0);
            mGrid.Children.Add(t3);

            var t4 = CreateMetricTile("SOLVED RANK", out _rxSolvedRankText, "0", "blocks", BrandColors.DarkTransferBrush);
            Grid.SetRow(t4, 2); Grid.SetColumn(t4, 2);
            mGrid.Children.Add(t4);

            rightStack.Children.Add(mGrid);

            // Reconstructed File Card
            _rxFileCard = new Border
            {
                Background = BrandColors.SurfaceCardBrush,
                BorderBrush = BrandColors.SuccessBrush,
                BorderThickness = new Thickness(1.5),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14),
                Visibility = Visibility.Collapsed
            };

            StackPanel fileStack = new StackPanel();
            TextBlock recHeader = new TextBlock
            {
                Text = "RECONSTRUCTED FILE",
                Foreground = BrandColors.SuccessBrush,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            };
            _rxFileNameText = new TextBlock
            {
                Text = "document.pdf",
                Foreground = BrandColors.TextEmphasisBrush,
                FontSize = 13,
                FontWeight = FontWeights.Bold
            };
            _rxFileDetailsText = new TextBlock
            {
                Text = "120 KB • SHA-256 Verified ✓",
                Foreground = BrandColors.SuccessBrush,
                FontSize = 11,
                Margin = new Thickness(0, 3, 0, 6)
            };
            _rxFilePathText = new TextBlock
            {
                Text = "Saved to: Downloads/OpticalTransfer",
                Foreground = BrandColors.TextMutedBrush,
                FontSize = 10,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };

            _rxShowInExplorerBtn = new Button
            {
                Content = "📁 Show in Explorer",
                Style = Styles.CreatePrimaryButtonStyle()
            };
            _rxShowInExplorerBtn.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_lastSavedFilePath) && File.Exists(_lastSavedFilePath))
                {
                    Process.Start("explorer.exe", string.Format("/select,\"{0}\"", _lastSavedFilePath));
                }
            };

            fileStack.Children.Add(recHeader);
            fileStack.Children.Add(_rxFileNameText);
            fileStack.Children.Add(_rxFileDetailsText);
            fileStack.Children.Add(_rxFilePathText);
            fileStack.Children.Add(_rxShowInExplorerBtn);
            _rxFileCard.Child = fileStack;
            rightStack.Children.Add(_rxFileCard);

            scroll.Content = rightStack;
            rightPanel.Child = scroll;
            Grid.SetColumn(rightPanel, 1);
            _receiveView.Children.Add(rightPanel);
        }

        private void StartReceiveSession()
        {
            _rxFileCard.Visibility = Visibility.Collapsed;
            _rxRing.Progress = 0;
            _rxRing.SolvedCount = 0;
            _rxInstantFpsText.Text = "0.0";
            _rxGoodputText.Text = "0.0";
            _rxIngestedText.Text = "0";
            _rxSolvedRankText.Text = "0";

            string[] devices = _cameraProvider.GetAvailableDevices();
            _rxCameraCombo.Items.Clear();
            if (devices != null && devices.Length > 0)
            {
                foreach (string d in devices) _rxCameraCombo.Items.Add(d);
                _rxCameraCombo.SelectedIndex = 0;
                _rxNoCameraPlaceholder.Visibility = Visibility.Collapsed;
                _rxRing.Visibility = Visibility.Visible;
                _rxController.Start();
            }
            else
            {
                _rxCameraCombo.Items.Add("No Camera Found");
                _rxCameraCombo.SelectedIndex = 0;
                _rxNoCameraPlaceholder.Visibility = Visibility.Visible;
                _rxRing.Visibility = Visibility.Collapsed;
            }
        }

        private Border CreateMetricTile(string label, out TextBlock valBlock, string initialVal, string unit, Brush color)
        {
            Border b = new Border
            {
                Background = BrandColors.SurfaceCardBrush,
                BorderBrush = BrandColors.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(10)
            };

            StackPanel sp = new StackPanel();
            TextBlock lbl = new TextBlock
            {
                Text = label,
                Foreground = BrandColors.TextMutedBrush,
                FontSize = 9,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 4)
            };
            StackPanel valSp = new StackPanel { Orientation = Orientation.Horizontal };
            valBlock = new TextBlock
            {
                Text = initialVal,
                Foreground = color,
                FontSize = 18,
                FontWeight = FontWeights.Bold
            };
            TextBlock u = new TextBlock
            {
                Text = " " + unit,
                Foreground = BrandColors.TextMutedBrush,
                FontSize = 10,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(3, 0, 0, 2)
            };

            valSp.Children.Add(valBlock);
            valSp.Children.Add(u);
            sp.Children.Add(lbl);
            sp.Children.Add(valSp);
            b.Child = sp;
            return b;
        }

        private void BrowseAndIngestFrames()
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Select Frame Bytes or Droplet File",
                Multiselect = true
            };
            if (dlg.ShowDialog() == true)
            {
                foreach (string file in dlg.FileNames)
                {
                    byte[] bytes = File.ReadAllBytes(file);
                    _rxController.IngestRawFrameBytes(bytes);
                }
            }
        }
        #endregion

        #region Send View
        private void BuildSendView()
        {
            _sendView = new Grid();
            _sendView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            _sendView.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(300) });

            // Left: QR Projector Canvas (Responsive Grid)
            Border projectorCard = new Border
            {
                Background = BrandColors.SurfaceCardBrush,
                BorderBrush = BrandColors.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(14),
                Margin = new Thickness(18),
                Padding = new Thickness(16)
            };

            Grid pGrid = new Grid();
            pGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            pGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Placeholder Card when idle
            _txPlaceholderCard = new Border
            {
                Background = BrandColors.SecondaryBgBrush,
                BorderBrush = BrandColors.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(24),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                MaxWidth = 420
            };
            StackPanel phStack = new StackPanel { HorizontalAlignment = HorizontalAlignment.Center };
            TextBlock phIcon = new TextBlock
            {
                Text = "📤",
                FontSize = 40,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 10)
            };
            TextBlock phTitle = new TextBlock
            {
                Text = "No Active Transmission",
                Foreground = BrandColors.TextEmphasisBrush,
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6)
            };
            TextBlock phSub = new TextBlock
            {
                Text = "Select a file to chunk and stream optical fountain droplets via screen.",
                Foreground = BrandColors.TextSecondaryBrush,
                FontSize = 12,
                TextAlignment = TextAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 16)
            };
            Button phBrowseBtn = new Button
            {
                Content = "📂 Select File to Transmit",
                Style = Styles.CreatePrimaryButtonStyle(),
                HorizontalAlignment = HorizontalAlignment.Center
            };
            phBrowseBtn.Click += (s, e) => BrowseAndSendFile();

            phStack.Children.Add(phIcon);
            phStack.Children.Add(phTitle);
            phStack.Children.Add(phSub);
            phStack.Children.Add(phBrowseBtn);
            _txPlaceholderCard.Child = phStack;

            _txProjector = new QrProjector
            {
                Visibility = Visibility.Collapsed,
                MaxWidth = 420,
                MaxHeight = 420
            };

            Grid.SetRow(_txPlaceholderCard, 0);
            Grid.SetRow(_txProjector, 0);
            pGrid.Children.Add(_txPlaceholderCard);
            pGrid.Children.Add(_txProjector);

            // Responsive Controls WrapPanel
            WrapPanel bottomControls = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 14, 0, 0)
            };

            _txStartBtn = new Button
            {
                Content = "▶ Start Stream",
                Style = Styles.CreatePrimaryButtonStyle(),
                Margin = new Thickness(0, 0, 8, 6)
            };
            _txStartBtn.Click += (s, e) => StartTransmission();

            _txPauseBtn = new Button
            {
                Content = "⏸ Pause",
                Style = Styles.CreateSecondaryButtonStyle(),
                Margin = new Thickness(0, 0, 8, 6)
            };
            _txPauseBtn.Click += (s, e) => _txController.Pause();

            _txDetachBtn = new Button
            {
                Content = "⛶ Detach (F11)",
                Style = Styles.CreateSecondaryButtonStyle(),
                Margin = new Thickness(0, 0, 8, 6)
            };
            _txDetachBtn.Click += (s, e) =>
            {
                if (_detachedProjector == null || !_detachedProjector.IsVisible)
                {
                    _detachedProjector = new ProjectorWindow();
                    _detachedProjector.Closed += (s2, e2) => _detachedProjector = null;
                    _detachedProjector.Show();
                }
            };

            bottomControls.Children.Add(_txStartBtn);
            bottomControls.Children.Add(_txPauseBtn);
            bottomControls.Children.Add(_txDetachBtn);
            Grid.SetRow(bottomControls, 1);
            pGrid.Children.Add(bottomControls);

            projectorCard.Child = pGrid;
            Grid.SetColumn(projectorCard, 0);
            _sendView.Children.Add(projectorCard);

            // Right: File Parameters & Tuning Panel
            Border sidePanel = new Border
            {
                Background = BrandColors.SecondaryBgBrush,
                BorderBrush = BrandColors.BorderBrush,
                BorderThickness = new Thickness(1, 0, 0, 0),
                Padding = new Thickness(18)
            };

            ScrollViewer sideScroll = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            StackPanel sideStack = new StackPanel();
            TextBlock tuningHeader = new TextBlock
            {
                Text = "STREAM PARAMETERS & FILE",
                Foreground = BrandColors.TextMutedBrush,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 14)
            };
            sideStack.Children.Add(tuningHeader);

            // Select File Box
            Border fileBox = new Border
            {
                Background = BrandColors.SurfaceCardBrush,
                BorderBrush = BrandColors.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(14),
                Margin = new Thickness(0, 0, 0, 16)
            };
            StackPanel fbStack = new StackPanel();
            _txFileNameText = new TextBlock
            {
                Text = "No file selected",
                Foreground = BrandColors.TextEmphasisBrush,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                TextTrimming = TextTrimming.CharacterEllipsis
            };
            _txFileSizeText = new TextBlock
            {
                Text = "Select a file to chunk into packets",
                Foreground = BrandColors.TextSecondaryBrush,
                FontSize = 11,
                Margin = new Thickness(0, 3, 0, 6)
            };
            _txKBlocksText = new TextBlock
            {
                Text = "Source Blocks K: --",
                Foreground = BrandColors.OpticalGreenBrush,
                FontSize = 11
            };

            Button pickBtn = new Button
            {
                Content = "📂 Browse File...",
                Style = Styles.CreateSecondaryButtonStyle(),
                Margin = new Thickness(0, 10, 0, 0)
            };
            pickBtn.Click += (s, e) => BrowseAndSendFile();

            fbStack.Children.Add(_txFileNameText);
            fbStack.Children.Add(_txFileSizeText);
            fbStack.Children.Add(_txKBlocksText);
            fbStack.Children.Add(pickBtn);
            fileBox.Child = fbStack;
            sideStack.Children.Add(fileBox);

            // FPS Speed Slider
            TextBlock fpsHeader = new TextBlock
            {
                Text = "STREAM CADENCE & SPEED",
                Foreground = BrandColors.TextMutedBrush,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 6)
            };
            sideStack.Children.Add(fpsHeader);

            StackPanel fpsValSp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 6) };
            _txFpsText = new TextBlock
            {
                Text = "20 FPS (50 ms / frame)",
                Foreground = BrandColors.OpticalGreenBrush,
                FontSize = 12,
                FontWeight = FontWeights.Bold
            };
            fpsValSp.Children.Add(_txFpsText);
            sideStack.Children.Add(fpsValSp);

            _txFpsSlider = new Slider
            {
                Minimum = 5,
                Maximum = 60,
                Value = 20,
                IsSnapToTickEnabled = true,
                TickFrequency = 5,
                Margin = new Thickness(0, 0, 0, 12)
            };
            _txFpsSlider.ValueChanged += (s, e) =>
            {
                int fps = (int)_txFpsSlider.Value;
                int ms = 1000 / fps;
                _txFpsText.Text = string.Format("{0} FPS ({1} ms / frame)", fps, ms);
                _txController.SetFps(fps);
            };
            sideStack.Children.Add(_txFpsSlider);

            // Preset Buttons
            WrapPanel presets = new WrapPanel { Margin = new Thickness(0, 0, 0, 16) };
            int[] presetVals = new int[] { 10, 20, 30, 60 };
            foreach (int p in presetVals)
            {
                Button pb = new Button
                {
                    Content = string.Format("{0} FPS", p),
                    Style = Styles.CreateSecondaryButtonStyle(),
                    Margin = new Thickness(0, 0, 6, 6)
                };
                int val = p;
                pb.Click += (s, e) => _txFpsSlider.Value = val;
                presets.Children.Add(pb);
            }
            sideStack.Children.Add(presets);

            sideScroll.Content = sideStack;
            sidePanel.Child = sideScroll;
            Grid.SetColumn(sidePanel, 1);
            _sendView.Children.Add(sidePanel);
        }

        private void BrowseAndSendFile()
        {
            OpenFileDialog dlg = new OpenFileDialog
            {
                Title = "Select File to Project as Optical Stream"
            };
            if (dlg.ShowDialog() == true)
            {
                _selectedFileBytes = File.ReadAllBytes(dlg.FileName);
                _selectedFileName = Path.GetFileName(dlg.FileName);

                _txFileNameText.Text = _selectedFileName;
                _txFileSizeText.Text = string.Format("{0:F1} KB ({1:N0} bytes)", (_selectedFileBytes.Length / 1024.0), _selectedFileBytes.Length);
                uint k = (uint)Math.Ceiling(_selectedFileBytes.Length / 300.0);
                if (k == 0) k = 1;
                _txKBlocksText.Text = string.Format("Source Blocks K: {0} (Symbol Size T: 300B)", k);

                StartTransmission();
            }
        }

        private void StartTransmission()
        {
            if (_selectedFileBytes == null || _selectedFileBytes.Length == 0)
            {
                _selectedFileBytes = new byte[60 * 1024];
                new Random().NextBytes(_selectedFileBytes);
                _selectedFileName = "demo_airgapped_data.bin";

                _txFileNameText.Text = _selectedFileName;
                _txFileSizeText.Text = "60.0 KB (Demo Stream)";
                _txKBlocksText.Text = "Source Blocks K: 205 (Symbol Size T: 300B)";
            }

            _txPlaceholderCard.Visibility = Visibility.Collapsed;
            _txProjector.Visibility = Visibility.Visible;
            _txController.Start(_selectedFileBytes, _selectedFileName, (float)_txFpsSlider.Value);
        }
        #endregion

        #region History View
        private void BuildHistoryView()
        {
            _historyView = new Grid { Margin = new Thickness(24) };
            StackPanel stack = new StackPanel();

            Grid topGrid = new Grid { Margin = new Thickness(0, 0, 0, 16) };
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            topGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            TextBlock histLabel = new TextBlock
            {
                Text = "TRANSFERS & DOWNLOADS",
                Foreground = BrandColors.TextMutedBrush,
                FontSize = 11,
                FontWeight = FontWeights.Bold
            };
            Grid.SetColumn(histLabel, 0);
            topGrid.Children.Add(histLabel);

            Button openFolderBtn = new Button
            {
                Content = "📁 Open Downloads Folder",
                Style = Styles.CreateSecondaryButtonStyle()
            };
            openFolderBtn.Click += (s, e) =>
            {
                string dlDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "OpticalTransfer");
                if (!Directory.Exists(dlDir)) Directory.CreateDirectory(dlDir);
                Process.Start("explorer.exe", dlDir);
            };
            Grid.SetColumn(openFolderBtn, 1);
            topGrid.Children.Add(openFolderBtn);

            stack.Children.Add(topGrid);

            Border histCard = new Border
            {
                Background = BrandColors.SurfaceCardBrush,
                BorderBrush = BrandColors.BorderBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = new Thickness(24),
                Height = 360
            };

            TextBlock desc = new TextBlock
            {
                Text = "Received files are automatically saved to your Downloads/OpticalTransfer directory.\nZero cloud servers or network trace.",
                Foreground = BrandColors.TextMutedBrush,
                FontSize = 13,
                TextAlignment = TextAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            histCard.Child = desc;
            stack.Children.Add(histCard);

            _historyView.Children.Add(stack);
        }
        #endregion

        #region ISessionObserver Callbacks
        public void OnStateChanged(SessionState state, SessionError error)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                // UI state sync
            }));
        }

        public void OnProgressUpdated(SessionSnapshot snapshot)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                if (snapshot.Role == SessionRole.Receiver)
                {
                    _rxRing.Progress = snapshot.RxStats.ProgressPercentage / 100.0;
                    _rxRing.SolvedCount = snapshot.RxStats.CurrentRank;
                    _rxRing.TotalK = snapshot.RxStats.SymbolsRequired;
                    _rxRing.IsTransferring = (snapshot.State == SessionState.Transferring);
                    _rxRing.IsComplete = (snapshot.State == SessionState.Completed);

                    _rxInstantFpsText.Text = snapshot.RxStats.InstantFps.ToString("F1");
                    _rxGoodputText.Text = snapshot.RxStats.GoodputKbps.ToString("F1");
                    _rxIngestedText.Text = snapshot.RxStats.RawFramesReceived.ToString();
                    _rxSolvedRankText.Text = string.Format("{0} / {1}", snapshot.RxStats.CurrentRank, snapshot.RxStats.SymbolsRequired);
                }
            }));
        }

        public void OnFrameReady(QrBitmap frame)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                _txProjector.SetFrame(frame);
                if (_detachedProjector != null && _detachedProjector.IsVisible)
                {
                    _detachedProjector.UpdateFrame(frame, string.Format("{0} FPS Stream", _txFpsSlider.Value));
                }
            }));
        }

        public void OnTransferCompleted(byte[] payload, FileMetadata metadata)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                string targetDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads", "OpticalTransfer");
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                string safeName = string.IsNullOrEmpty(metadata.FileName) ? "received_file.bin" : metadata.FileName;
                string filePath = Path.Combine(targetDir, safeName);
                File.WriteAllBytes(filePath, payload);

                _lastSavedFilePath = filePath;
                _rxFileCard.Visibility = Visibility.Visible;
                _rxFileNameText.Text = safeName;
                _rxFileDetailsText.Text = string.Format("{0:F1} KB • SHA-256 Verified ✓", (payload.Length / 1024.0));
                _rxFilePathText.Text = string.Format("Saved to: {0}", filePath);
            }));
        }
        #endregion

        protected override void OnClosed(EventArgs e)
        {
            _txController.Dispose();
            _rxController.Dispose();
            _cameraProvider.Dispose();
            base.OnClosed(e);
        }
    }
}
