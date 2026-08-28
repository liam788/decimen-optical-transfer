import 'package:flutter/material.dart';

/// Optical Transfer Official Brand Palette & Design System
class AppColors {
  // Brand Greens
  static const Color opticalGreen = Color(0xFF98B878);    // Main brand accent
  static const Color transferGreen = Color(0xFF88A868);   // Hover / active
  static const Color deepOptical = Color(0xFF6F914F);     // Strong accent
  static const Color darkTransfer = Color(0xFF5C7F3F);    // Accessible buttons / text
  static const Color forestOptical = Color(0xFF3F6126);   // High-contrast green

  // Dark Neutrals
  static const Color opticalBlack = Color(0xFF0A0A0A);    // Main background (Black 95)
  static const Color pureBlack = Color(0xFF000000);       // Maximum black (Black 100)
  static const Color secondaryBackground = Color(0xFF111311); // Sidebar (Black 90)
  static const Color surface = Color(0xFF181B18);         // Cards & panels (Black 85)
  static const Color surfaceElevated = Color(0xFF202420); // Elevated cards (Black 80)
  static const Color border = Color(0xFF2B302B);          // Dividers (Gray 70)
  static const Color borderDisabled = Color(0xFF3A403A);  // Disabled borders (Gray 60)

  // Typography Colors
  static const Color textPrimary = Color(0xFFF5F7F2);     // Main readable text (White 95)
  static const Color textSecondary = Color(0xFFB7BDB7);   // Secondary text (Gray 20)
  static const Color textMuted = Color(0xFF737A73);       // Muted metadata (Gray 40)
  static const Color textEmphasis = Color(0xFFFFFFFF);    // Maximum emphasis (White 100)

  // Semantic Feedback Colors
  static const Color success = Color(0xFF5E9F62);
  static const Color successLight = Color(0xA9D3A5);
  static const Color warning = Color(0xFFD4A84F);
  static const Color error = Color(0xFFC85A57);
  static const Color info = Color(0xFF668FA8);
}

class AppTheme {
  static ThemeData get darkTheme {
    return ThemeData(
      brightness: Brightness.dark,
      scaffoldBackgroundColor: AppColors.opticalBlack,
      primaryColor: AppColors.darkTransfer,
      colorScheme: const ColorScheme.dark(
        primary: AppColors.darkTransfer,
        secondary: AppColors.opticalGreen,
        surface: AppColors.surface,
        background: AppColors.opticalBlack,
        error: AppColors.error,
        onPrimary: AppColors.textEmphasis,
        onSecondary: AppColors.opticalBlack,
        onSurface: AppColors.textPrimary,
        onBackground: AppColors.textPrimary,
      ),
      fontFamily: 'Inter',
      appBarTheme: const AppBarTheme(
        backgroundColor: AppColors.secondaryBackground,
        elevation: 0,
        centerTitle: false,
        titleTextStyle: TextStyle(
          color: AppColors.textEmphasis,
          fontSize: 18,
          fontWeight: FontWeight.w600,
          letterSpacing: 0.5,
        ),
        iconTheme: IconThemeData(color: AppColors.textPrimary),
      ),
      cardTheme: CardTheme(
        color: AppColors.surface,
        elevation: 0,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(12),
          side: const BorderSide(color: AppColors.border, width: 1),
        ),
        margin: EdgeInsets.zero,
      ),
      elevatedButtonTheme: ElevatedButtonThemeData(
        style: ElevatedButton.styleFrom(
          backgroundColor: AppColors.darkTransfer,
          foregroundColor: AppColors.textEmphasis,
          elevation: 0,
          padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 16),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
          ),
          textStyle: const TextStyle(
            fontSize: 15,
            fontWeight: FontWeight.w600,
            letterSpacing: 0.5,
          ),
        ),
      ),
      outlinedButtonTheme: OutlinedButtonThemeData(
        style: OutlinedButton.styleFrom(
          backgroundColor: AppColors.surfaceElevated,
          foregroundColor: AppColors.textPrimary,
          side: const BorderSide(color: AppColors.borderDisabled, width: 1),
          padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 14),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(8),
          ),
          textStyle: const TextStyle(
            fontSize: 14,
            fontWeight: FontWeight.w500,
          ),
        ),
      ),
      dividerTheme: const DividerThemeData(
        color: AppColors.border,
        thickness: 1,
        space: 1,
      ),
      useMaterial3: true,
    );
  }
}
