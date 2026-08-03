import 'dart:typed_data';

/// Feature D: Image Preprocessing Pipeline (Otsu Adaptive Thresholding & Dewarping)
class ImageProcessor {
  /// Calculate Otsu threshold value for binarizing low-contrast screen photos
  static int otsuThreshold(Uint8List grayscale, int width, int height) {
    final histogram = List<int>.filled(256, 0);
    for (int i = 0; i < grayscale.length; i++) {
      histogram[grayscale[i]]++;
    }

    final total = width * height;
    double sum = 0;
    for (int t = 0; t < 256; t++) {
      sum += t * histogram[t];
    }

    double sumB = 0;
    int wB = 0;
    int wF = 0;
    double varMax = 0;
    int threshold = 128;

    for (int t = 0; t < 256; t++) {
      wB += histogram[t];
      if (wB == 0) continue;

      wF = total - wB;
      if (wF == 0) break;

      sumB += (t * histogram[t]);
      final mB = sumB / wB;
      final mF = (sum - sumB) / wF;

      final varBetween = wB.toDouble() * wF.toDouble() * (mB - mF) * (mB - mF);
      if (varBetween > varMax) {
        varMax = varBetween;
        threshold = t;
      }
    }

    return threshold;
  }

  /// Apply Otsu binarization filter
  static Uint8List binarize(Uint8List grayscale, int width, int height) {
    final threshold = otsuThreshold(grayscale, width, height);
    final binarized = Uint8List(grayscale.length);
    for (int i = 0; i < grayscale.length; i++) {
      binarized[i] = grayscale[i] > threshold ? 255 : 0;
    }
    return binarized;
  }
}
