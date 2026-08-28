import 'dart:math' as math;
import 'package:flutter/material.dart';
import '../theme/app_theme.dart';

/// Signature UI Motif: The Optical Transfer Ring (Section 14 of Brand Guide)
class OpticalTransferRing extends StatefulWidget {
  final double progress; // 0.0 to 1.0
  final bool isTransferring;
  final bool isComplete;
  final bool isFailed;
  final double size;
  final Widget? child;

  const OpticalTransferRing({
    super.key,
    required this.progress,
    this.isTransferring = false,
    this.isComplete = false,
    this.isFailed = false,
    this.size = 200.0,
    this.child,
  });

  @override
  State<OpticalTransferRing> createState() => _OpticalTransferRingState();
}

class _OpticalTransferRingState extends State<OpticalTransferRing>
    with SingleTickerProviderStateMixin {
  late AnimationController _animController;

  @override
  void initState() {
    super.initState();
    _animController = AnimationController(
      vsync: this,
      duration: const Duration(seconds: 4),
    );
    if (widget.isTransferring) {
      _animController.repeat();
    }
  }

  @override
  void didUpdateWidget(OpticalTransferRing oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (widget.isTransferring && !_animController.isAnimating) {
      _animController.repeat();
    } else if (!widget.isTransferring && _animController.isAnimating) {
      _animController.stop();
    }
  }

  @override
  void dispose() {
    _animController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    Color ringColor = AppColors.opticalGreen;
    if (widget.isComplete) {
      ringColor = AppColors.success;
    } else if (widget.isFailed) {
      ringColor = AppColors.error;
    }

    return AnimatedBuilder(
      animation: _animController,
      builder: (context, _) {
        return CustomPaint(
          size: Size(widget.size, widget.size),
          painter: _RingPainter(
            progress: widget.progress,
            rotation: _animController.value * 2 * math.pi,
            isTransferring: widget.isTransferring,
            isComplete: widget.isComplete,
            ringColor: ringColor,
          ),
          child: SizedBox(
            width: widget.size,
            height: widget.size,
            child: Center(child: widget.child),
          ),
        );
      },
    );
  }
}

class _RingPainter extends CustomPainter {
  final double progress;
  final double rotation;
  final bool isTransferring;
  final bool isComplete;
  final Color ringColor;

  _RingPainter({
    required this.progress,
    required this.rotation,
    required this.isTransferring,
    required this.isComplete,
    required this.ringColor,
  });

  @override
  void paint(Canvas canvas, Size size) {
    final center = Offset(size.width / 2, size.height / 2);
    final radius = (size.width - 24) / 2;

    // 1. Base Outer Track Ring
    final trackPaint = Paint()
      ..color = AppColors.border
      ..strokeWidth = 3.0
      ..style = PaintingStyle.stroke;
    canvas.drawCircle(center, radius, trackPaint);

    // 2. Secondary Inner Orbit Guide (Optical Element)
    final innerGuidePaint = Paint()
      ..color = AppColors.surfaceElevated.withOpacity(0.6)
      ..strokeWidth = 1.0
      ..style = PaintingStyle.stroke;
    canvas.drawCircle(center, radius - 12, innerGuidePaint);

    // 3. Active Progress Arc
    final progressPaint = Paint()
      ..color = ringColor
      ..strokeWidth = 6.0
      ..strokeCap = StrokeCap.round
      ..style = PaintingStyle.stroke;

    final sweepAngle = (progress.clamp(0.0, 1.0)) * 2 * math.pi;
    canvas.drawArc(
      Rect.fromCircle(center: center, radius: radius),
      -math.pi / 2,
      sweepAngle,
      false,
      progressPaint,
    );

    // 4. Subtle Orbital Nodes (Transfer Element)
    if (isTransferring) {
      final nodePaint = Paint()
        ..color = ringColor.withOpacity(0.8)
        ..style = PaintingStyle.fill;

      for (int i = 0; i < 3; i++) {
        final nodeAngle = rotation + (i * (2 * math.pi / 3));
        final nodePos = Offset(
          center.dx + (radius - 12) * math.cos(nodeAngle),
          center.dy + (radius - 12) * math.sin(nodeAngle),
        );
        canvas.drawCircle(nodePos, 2.5, nodePaint);
      }
    }
  }

  @override
  bool shouldRepaint(covariant _RingPainter oldDelegate) {
    return oldDelegate.progress != progress ||
        oldDelegate.rotation != rotation ||
        oldDelegate.isTransferring != isTransferring ||
        oldDelegate.isComplete != isComplete;
  }
}
