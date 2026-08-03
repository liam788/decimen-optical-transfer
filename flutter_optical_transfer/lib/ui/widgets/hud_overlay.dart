import 'package:flutter/material.dart';

/// Feature E: HUD Overlay & Signal Quality Goodput Meter
class HudOverlayWidget extends StatelessWidget {
  final double progress;
  final int solvedBlocks;
  final int totalBlocks;
  final double currentFps;
  final double goodputKbps;
  final bool isComplete;

  const HudOverlayWidget({
    super.key,
    required this.progress,
    required this.solvedBlocks,
    required this.totalBlocks,
    required this.currentFps,
    required this.goodputKbps,
    required this.isComplete,
  });

  @override
  Widget build(BuildContext context) {
    return Stack(
      children: [
        // Center alignment targeting frame
        Center(
          child: Container(
            width: 280,
            height: 280,
            decoration: BoxDecoration(
              border: Border.all(
                color: isComplete ? Colors.greenAccent : Colors.cyanAccent,
                width: 3,
              ),
              borderRadius: BorderRadius.circular(20),
              boxShadow: [
                BoxShadow(
                  color: (isComplete ? Colors.greenAccent : Colors.cyanAccent).withOpacity(0.3),
                  blurRadius: 16,
                  spreadRadius: 4,
                )
              ],
            ),
            child: isComplete
                ? const Center(
                    child: Icon(Icons.check_circle, size: 80, color: Colors.greenAccent),
                  )
                : null,
          ),
        ),

        // Top Status HUD Bar
        Positioned(
          top: 40,
          left: 16,
          right: 16,
          child: Container(
            padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
            decoration: BoxDecoration(
              color: Colors.black.withOpacity(0.75),
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: Colors.white24),
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      "FPS: ${currentFps.toStringAsFixed(1)}",
                      style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
                    ),
                    Text(
                      "Speed: ${goodputKbps.toStringAsFixed(1)} KB/s",
                      style: const TextStyle(color: Colors.cyanAccent, fontSize: 12),
                    ),
                  ],
                ),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(
                      "Blocks: $solvedBlocks / $totalBlocks",
                      style: const TextStyle(color: Colors.white, fontWeight: FontWeight.bold),
                    ),
                    Text(
                      "${(progress * 100).toStringAsFixed(1)}%",
                      style: TextStyle(
                        color: isComplete ? Colors.greenAccent : Colors.orangeAccent,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ],
                ),
              ],
            ),
          ),
        ),

        // Bottom Progress Bar
        Positioned(
          bottom: 40,
          left: 24,
          right: 24,
          child: Column(
            children: [
              ClipRRect(
                borderRadius: BorderRadius.circular(10),
                child: LinearProgressIndicator(
                  value: progress,
                  minHeight: 12,
                  backgroundColor: Colors.white24,
                  valueColor: AlwaysStoppedAnimation<Color>(
                    isComplete ? Colors.greenAccent : Colors.cyanAccent,
                  ),
                ),
              ),
              const SizedBox(height: 8),
              Text(
                isComplete
                    ? "Transfer Complete! Verifying SHA-256..."
                    : "Align camera with sending screen",
                style: const TextStyle(color: Colors.white, fontSize: 13),
              ),
            ],
          ),
        ),
      ],
    );
  }
}
