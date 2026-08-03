import 'dart:typed_data';
import 'package:flutter/material.dart';

/// Feature F: High-Density Color Channel Matrix Codec (2 bits per module)
/// Colors: 00 -> White, 01 -> Red, 10 -> Green, 11 -> Blue
class ColorMatrixCodec {
  static const int moduleSize = 8; // Pixel size per color module

  static List<Color> get palette => const [
    Colors.white,
    Colors.red,
    Colors.green,
    Colors.blue,
  ];

  /// Encode byte stream into a color matrix raster (width x height modules)
  static List<int> encodeToColorIndices(Uint8List data) {
    final indices = <int>[];
    for (int i = 0; i < data.length; i++) {
      final b = data[i];
      indices.add((b >> 6) & 0x03);
      indices.add((b >> 4) & 0x03);
      indices.add((b >> 2) & 0x03);
      indices.add(b & 0x03);
    }
    return indices;
  }

  /// Custom Painter to draw high-density Color Matrix on Flutter Canvas
  static CustomPainter getPainter(Uint8List data, int gridWidth) {
    return _ColorMatrixPainter(data, gridWidth);
  }
}

class _ColorMatrixPainter extends CustomPainter {
  final Uint8List data;
  final int gridWidth;

  _ColorMatrixPainter(this.data, this.gridWidth);

  @override
  void paint(Canvas canvas, Size size) {
    final indices = ColorMatrixCodec.encodeToColorIndices(data);
    final total = indices.length;
    if (total == 0) return;

    final cellWidth = size.width / gridWidth;
    final gridHeight = (total / gridWidth).ceil();
    final cellHeight = size.height / gridHeight;

    final paint = Paint()..style = PaintingStyle.fill;

    for (int i = 0; i < total; i++) {
      final col = i % gridWidth;
      final row = i ~/ gridWidth;
      paint.color = ColorMatrixCodec.palette[indices[i]];
      canvas.drawRect(
        Rect.fromLTWH(col * cellWidth, row * cellHeight, cellWidth, cellHeight),
        paint,
      );
    }
  }

  @override
  bool shouldRepaint(covariant _ColorMatrixPainter oldDelegate) => true;
}
