import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:qr_flutter/qr_flutter.dart';
import '../core/color_encoder.dart';
import 'adaptive_tuner.dart';

/// Feature B: Multi-Code Spatial Grid Widget (1x1, 2x2, 3x3 rendering)
class MultiQrGridWidget extends StatelessWidget {
  final List<Uint8List> frameChunks;
  final AdaptiveSettings settings;

  const MultiQrGridWidget({
    super.key,
    required this.frameChunks,
    required this.settings,
  });

  @override
  Widget build(BuildContext context) {
    if (frameChunks.isEmpty) {
      return const Center(
        child: Text("Preparing stream...", style: TextStyle(color: Colors.white70)),
      );
    }

    if (settings.useColorCodec) {
      return Container(
        padding: const EdgeInsets.all(12),
        decoration: BoxDecoration(
          color: Colors.white,
          borderRadius: BorderRadius.circular(16),
          boxShadow: [
            BoxShadow(
              color: Colors.black.withOpacity(0.3),
              blurRadius: 12,
              spreadRadius: 2,
            )
          ],
        ),
        child: CustomPaint(
          size: const Size(320, 320),
          painter: ColorMatrixCodec.getPainter(frameChunks.first, 32),
        ),
      );
    }

    final dim = settings.qrGridDimension;

    return Container(
      padding: const EdgeInsets.all(12),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(16),
        boxShadow: [
          BoxShadow(
            color: Colors.black.withOpacity(0.4),
            blurRadius: 16,
            spreadRadius: 2,
          )
        ],
      ),
      child: Column(
        children: List.generate(dim, (rowIndex) {
          return Expanded(
            child: Row(
              children: List.generate(dim, (colIndex) {
                final cellIndex = rowIndex * dim + colIndex;
                final chunk = frameChunks[cellIndex % frameChunks.length];
                final qrData = String.fromCharCodes(chunk);

                return Expanded(
                  child: Padding(
                    padding: const EdgeInsets.all(4.0),
                    child: QrImageView(
                      data: qrData,
                      version: QrVersions.auto,
                      errorCorrectionLevel: QrErrorCorrectLevel.L,
                      backgroundColor: Colors.white,
                      padding: const EdgeInsets.all(2),
                    ),
                  ),
                );
              }),
            ),
          );
        }),
      ),
    );
  }
}


