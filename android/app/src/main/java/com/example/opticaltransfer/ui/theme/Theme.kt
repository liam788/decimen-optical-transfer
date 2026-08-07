package com.example.opticaltransfer.ui.theme

import androidx.compose.foundation.isSystemInDarkTheme
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.darkColorScheme
import androidx.compose.runtime.Composable

private val DarkColorScheme = darkColorScheme(
    primary = CyanAccent,
    secondary = TealAccent,
    tertiary = BlueAccent,
    background = DeepSlateBackground,
    surface = SlateSurface,
    onPrimary = DeepSlateBackground,
    onSecondary = DeepSlateBackground,
    onBackground = TextPrimary,
    onSurface = TextPrimary,
    surfaceVariant = SlateSurfaceLight,
    onSurfaceVariant = TextSecondary
)

@Composable
fun OpticalTransferTheme(
    darkTheme: Boolean = isSystemInDarkTheme(),
    content: @Composable () -> Unit
) {
    MaterialTheme(
        colorScheme = DarkColorScheme,
        content = content
    )
}
