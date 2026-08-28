import 'package:flutter/material.dart';
import '../theme/app_theme.dart';

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
                color: isComplete ? AppColors.success : AppColors.opticalGreen,
                width: 2.5,
              ),
              borderRadius: BorderRadius.circular(20),
              boxShadow: [
                BoxShadow(
                  color: (isComplete ? AppColors.success : AppColors.opticalGreen).withOpacity(0.25),
                  blurRadius: 16,
                  spreadRadius: 2,
                )
              ],
            ),
            child: isComplete
                ? const Center(
                    child: Icon(Icons.check_circle_outline, size: 80, color: AppColors.success),
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
              color: AppColors.secondaryBackground.withOpacity(0.9),
              borderRadius: BorderRadius.circular(14),
              border: Border.all(color: AppColors.border),
            ),
            child: Row(
              mainAxisAlignment: MainAxisAlignment.spaceBetween,
              children: [
                Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      "FPS: ${currentFps.toStringAsFixed(1)}",
                      style: const TextStyle(color: AppColors.textEmphasis, fontWeight: FontWeight.bold),
                    ),
                    Text(
                      "Speed: ${goodputKbps.toStringAsFixed(1)} KB/s",
                      style: const TextStyle(color: AppColors.opticalGreen, fontSize: 12, fontWeight: FontWeight.w600),
                    ),
                  ],
                ),
                Column(
                  crossAxisAlignment: CrossAxisAlignment.end,
                  children: [
                    Text(
                      "Blocks: $solvedBlocks / $totalBlocks",
                      style: const TextStyle(color: AppColors.textEmphasis, fontWeight: FontWeight.bold),
                    ),
                    Text(
                      "${(progress * 100).toStringAsFixed(1)}%",
                      style: TextStyle(
                        color: isComplete ? AppColors.success : AppColors.opticalGreen,
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
                  minHeight: 10,
                  backgroundColor: AppColors.border,
                  valueColor: AlwaysStoppedAnimation<Color>(
                    isComplete ? AppColors.success : AppColors.opticalGreen,
                  ),
                ),
              ),
              const SizedBox(height: 8),
              Text(
                isComplete
                    ? "Transfer Complete! Verifying SHA-256..."
                    : "Align camera with sending screen",
                style: const TextStyle(color: AppColors.textSecondary, fontSize: 13),
              ),
            ],
          ),
        ),
      ],
    );
  }
}

