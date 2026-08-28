using System.Windows.Media;

namespace OpticalTransfer.UI.Theme
{
    public static class BrandColors
    {
        // Core Brand Palette
        public static readonly Color OpticalBlack = Color.FromRgb(10, 10, 10);       // #0A0A0A
        public static readonly Color PureBlack = Color.FromRgb(0, 0, 0);             // #000000
        public static readonly Color OpticalGreen = Color.FromRgb(152, 184, 120);    // #98B878
        public static readonly Color TransferGreen = Color.FromRgb(136, 168, 104);   // #88A868
        public static readonly Color DeepOptical = Color.FromRgb(111, 145, 79);      // #6F914F
        public static readonly Color DarkTransfer = Color.FromRgb(92, 127, 63);      // #5C7F3F
        public static readonly Color ForestOptical = Color.FromRgb(63, 97, 38);      // #3F6126
        public static readonly Color OpticalWhite = Color.FromRgb(255, 255, 255);    // #FFFFFF
        public static readonly Color SoftWhite = Color.FromRgb(245, 247, 242);       // #F5F7F2

        // Extended Dark Neutral Palette
        public static readonly Color Black90 = Color.FromRgb(17, 19, 17);            // #111311 Secondary background
        public static readonly Color Black85 = Color.FromRgb(24, 27, 24);            // #181B18 Cards / panels
        public static readonly Color Black80 = Color.FromRgb(32, 36, 32);            // #202420 Elevated surfaces
        public static readonly Color Gray70 = Color.FromRgb(43, 48, 43);             // #2B302B Borders / separators
        public static readonly Color Gray60 = Color.FromRgb(58, 64, 58);             // #3A403A Disabled borders
        public static readonly Color Gray50 = Color.FromRgb(85, 92, 85);             // #555C55 Secondary text
        public static readonly Color Gray40 = Color.FromRgb(115, 122, 115);          // #737A73 Muted text
        public static readonly Color Gray20 = Color.FromRgb(183, 189, 183);          // #B7BDB7 Readable text

        // Semantic Colors
        public static readonly Color Success = Color.FromRgb(94, 159, 98);           // #5E9F62
        public static readonly Color Warning = Color.FromRgb(212, 168, 79);          // #D4A84F
        public static readonly Color Error = Color.FromRgb(200, 90, 87);             // #C85A57
        public static readonly Color Info = Color.FromRgb(102, 143, 168);            // #668FA8

        // Solid Brushes
        public static readonly SolidColorBrush OpticalBlackBrush = new SolidColorBrush(OpticalBlack);
        public static readonly SolidColorBrush SecondaryBgBrush = new SolidColorBrush(Black90);
        public static readonly SolidColorBrush SurfaceCardBrush = new SolidColorBrush(Black85);
        public static readonly SolidColorBrush SurfaceElevatedBrush = new SolidColorBrush(Black80);
        public static readonly SolidColorBrush BorderBrush = new SolidColorBrush(Gray70);
        public static readonly SolidColorBrush BorderDisabledBrush = new SolidColorBrush(Gray60);
        public static readonly SolidColorBrush OpticalGreenBrush = new SolidColorBrush(OpticalGreen);
        public static readonly SolidColorBrush DarkTransferBrush = new SolidColorBrush(DarkTransfer);
        public static readonly SolidColorBrush TextEmphasisBrush = new SolidColorBrush(OpticalWhite);
        public static readonly SolidColorBrush TextPrimaryBrush = new SolidColorBrush(SoftWhite);
        public static readonly SolidColorBrush TextSecondaryBrush = new SolidColorBrush(Gray20);
        public static readonly SolidColorBrush TextMutedBrush = new SolidColorBrush(Gray40);
        public static readonly SolidColorBrush SuccessBrush = new SolidColorBrush(Success);
        public static readonly SolidColorBrush ErrorBrush = new SolidColorBrush(Error);
        public static readonly SolidColorBrush WarningBrush = new SolidColorBrush(Warning);

        static BrandColors()
        {
            OpticalBlackBrush.Freeze();
            SecondaryBgBrush.Freeze();
            SurfaceCardBrush.Freeze();
            SurfaceElevatedBrush.Freeze();
            BorderBrush.Freeze();
            BorderDisabledBrush.Freeze();
            OpticalGreenBrush.Freeze();
            DarkTransferBrush.Freeze();
            TextEmphasisBrush.Freeze();
            TextPrimaryBrush.Freeze();
            TextSecondaryBrush.Freeze();
            TextMutedBrush.Freeze();
            SuccessBrush.Freeze();
            ErrorBrush.Freeze();
            WarningBrush.Freeze();
        }
    }
}
