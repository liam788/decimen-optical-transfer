import 'package:flutter/material.dart';

/// Feature A: Adaptive Optical Tuning Parameters
class AdaptiveSettings {
  int targetFps;
  int blockLen;
  int qrGridDimension; // 1 = 1x1, 2 = 2x2 grid, 3 = 3x3 grid
  double scaleFactor;
  bool useColorCodec;

  AdaptiveSettings({
    this.targetFps = 30,
    this.blockLen = 300,
    this.qrGridDimension = 1,
    this.scaleFactor = 1.0,
    this.useColorCodec = false,
  });

  /// Automatically tune settings based on screen width and device pixel ratio
  static AdaptiveSettings autoTune(BuildContext context) {
    final media = MediaQuery.of(context);
    final width = media.size.width;
    final isDesktop = width > 800;

    return AdaptiveSettings(
      targetFps: isDesktop ? 45 : 30,
      blockLen: isDesktop ? 450 : 250,
      qrGridDimension: isDesktop ? 2 : 1, // 2x2 on PC screen, 1x1 on mobile screen
      scaleFactor: isDesktop ? 1.2 : 1.0,
      useColorCodec: false,
    );
  }
}
