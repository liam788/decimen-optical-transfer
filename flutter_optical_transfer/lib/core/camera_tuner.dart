import 'dart:ui';

/// Auto-detects receiver camera capabilities and generates optimal
/// settings profiles for external sending devices (especially Windows PC).
class CameraCapabilityProfile {
  final double measuredFps;
  final bool hasTorch;
  final Size resolution;
  final String performanceTier;

  // Suggested Windows / Sender Device Settings
  final String recommendedGrid; // '1x1 Single', '2x2 Grid', '3x3 Matrix'
  final int recommendedFps;
  final int recommendedBlockSize;
  final String recommendedColorMode;
  final String tips;

  CameraCapabilityProfile({
    required this.measuredFps,
    required this.hasTorch,
    required this.resolution,
    required this.performanceTier,
    required this.recommendedGrid,
    required this.recommendedFps,
    required this.recommendedBlockSize,
    required this.recommendedColorMode,
    required this.tips,
  });

  factory CameraCapabilityProfile.analyze({
    required double fps,
    required bool torchAvailable,
    Size? previewSize,
  }) {
    final size = previewSize ?? const Size(1920, 1080);
    final isHighRes = size.width >= 1920 || size.height >= 1920;

    String tier;
    String grid;
    int targetFps;
    int blockSize;
    String colorMode;
    String tips;

    if (fps >= 45 && isHighRes) {
      tier = 'Ultra High Performance';
      grid = '3x3 Matrix (9 Streams)';
      targetFps = 60;
      blockSize = 1024;
      colorMode = '4-Color RGB Matrix (2x Boost)';
      tips = 'Your Android camera has ultra-high throughput! On Windows, set display to 60 FPS and use 2x2 or 3x3 Grid mode for maximum speed (up to 1.5 MB/s).';
    } else if (fps >= 25) {
      tier = 'High Performance';
      grid = '2x2 Grid (4 Streams)';
      targetFps = 30;
      blockSize = 768;
      colorMode = '4-Color or Monochrome';
      tips = 'Set Windows display sender to 2x2 Grid @ 30 FPS. Hold camera 30–40 cm from PC screen with monitor brightness at 80%+.';
    } else if (fps >= 15) {
      tier = 'Standard Performance';
      grid = '1x1 Single QR or 2x2 Grid';
      targetFps = 20;
      blockSize = 512;
      colorMode = 'Monochrome (High Contrast)';
      tips = 'Set Windows display sender to 1x1 Single QR @ 20 FPS. Ensure room has good ambient lighting and no screen reflection.';
    } else {
      tier = 'Basic / Low FPS';
      grid = '1x1 Single QR';
      targetFps = 15;
      blockSize = 384;
      colorMode = 'Monochrome (Black & White)';
      tips = 'Set Windows sender to 1x1 Single QR @ 15 FPS with block size 384. Keep phone stationary for smooth frame detection.';
    }

    return CameraCapabilityProfile(
      measuredFps: fps == 0 ? 30.0 : fps,
      hasTorch: torchAvailable,
      resolution: size,
      performanceTier: tier,
      recommendedGrid: grid,
      recommendedFps: targetFps,
      recommendedBlockSize: blockSize,
      recommendedColorMode: colorMode,
      tips: tips,
    );
  }
}
