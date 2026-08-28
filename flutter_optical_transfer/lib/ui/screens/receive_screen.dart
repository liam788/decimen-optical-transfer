import 'dart:io';
import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';
import 'package:path_provider/path_provider.dart';
import 'package:share_plus/share_plus.dart';
import '../../core/fountain.dart';
import '../../core/protocol.dart';
import '../../core/camera_tuner.dart';
import '../../scanner/camera_controller.dart';
import '../widgets/hud_overlay.dart';
import '../widgets/camera_tuning_dialog.dart';
import '../theme/app_theme.dart';

class ReceiveScreen extends StatefulWidget {
  const ReceiveScreen({super.key});

  @override
  State<ReceiveScreen> createState() => _ReceiveScreenState();
}

class _ReceiveScreenState extends State<ReceiveScreen> {
  final OpticalCameraController _cameraController = OpticalCameraController();
  final FountainDecoder _decoder = FountainDecoder();

  int _frameCount = 0;
  DateTime? _startTime;
  double _currentFps = 0.0;
  double _goodputKbps = 0.0;
  OpticalFile? _recoveredFile;
  bool _isSaving = false;

  void _onBarcodeDetected(BarcodeCapture capture) {
    if (_recoveredFile != null) return; // Already finished

    _startTime ??= DateTime.now();
    _frameCount++;

    final now = DateTime.now();
    final elapsedSec = now.difference(_startTime!).inMilliseconds / 1000.0;
    if (elapsedSec > 0) {
      _currentFps = _frameCount / elapsedSec;
    }

    for (final barcode in capture.barcodes) {
      final rawValue = barcode.rawValue;
      if (rawValue == null) continue;

      // Extract frame bytes
      final bytes = Uint8List.fromList(rawValue.codeUnits);
      final frame = FrameData.parse(bytes);
      if (frame == null) continue;

      final complete = _decoder.addFrame(frame);
      if (elapsedSec > 0) {
        _goodputKbps = (_decoder.solvedBlocks.length * _decoder.blockLen) / 1024.0 / elapsedSec;
      }

      if (complete) {
        _handleCompletion();
        break;
      }
    }

    setState(() {});
  }

  void _handleCompletion() {
    final reconstructed = _decoder.reconstruct();
    if (reconstructed != null) {
      try {
        final file = unpackFile(reconstructed);
        setState(() {
          _recoveredFile = file;
        });
        _showSuccessDialog(file);
      } catch (e) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text("Error unpacking file: $e")),
        );
      }
    }
  }

  void _showSuccessDialog(OpticalFile file) {
    showDialog(
      context: context,
      barrierDismissible: false,
      builder: (context) => AlertDialog(
        backgroundColor: AppColors.surface,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(14),
          side: const BorderSide(color: AppColors.border),
        ),
        title: const Text("File Recovered Successfully!", style: TextStyle(color: AppColors.success, fontWeight: FontWeight.bold)),
        content: Column(
          mainAxisSize: MainAxisSize.min,
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text("File Name: ${file.name}", style: const TextStyle(color: AppColors.textEmphasis, fontWeight: FontWeight.bold)),
            const SizedBox(height: 8),
            Text("Size: ${(file.bytes.length / 1024).toStringAsFixed(1)} KB", style: const TextStyle(color: AppColors.textSecondary)),
            const SizedBox(height: 8),
            const Text("SHA-256 Verified ✓", style: TextStyle(color: AppColors.opticalGreen)),
          ],
        ),
        actions: [
          TextButton(
            onPressed: () => _saveAndShareFile(file),
            child: const Text("Save & Share", style: TextStyle(color: AppColors.opticalGreen)),
          ),
          ElevatedButton(
            style: ElevatedButton.styleFrom(backgroundColor: AppColors.darkTransfer),
            onPressed: () {
              Navigator.pop(context);
              Navigator.pop(context);
            },
            child: const Text("Done", style: TextStyle(color: Colors.white)),
          ),
        ],
      ),
    );
  }


  void _saveAndShareFile(OpticalFile file) async {
    setState(() => _isSaving = true);
    final dir = await getApplicationDocumentsDirectory();
    final savePath = "${dir.path}/${file.name}";
    final localFile = File(savePath);
    await localFile.writeAsBytes(file.bytes);

    setState(() => _isSaving = false);
    await Share.shareXFiles([XFile(savePath)], text: "Decimen Received File: ${file.name}");
  }

  @override
  void dispose() {
    _cameraController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.black,
      body: Stack(
        children: [
          // Camera Stream Scanner
          MobileScanner(
            controller: _cameraController.controller,
            onDetect: _onBarcodeDetected,
          ),

          // Feature E: Real-time HUD & Signal Meter Overlay
          HudOverlayWidget(
            progress: _decoder.progress,
            solvedBlocks: _decoder.solvedBlocks.length,
            totalBlocks: _decoder.k == 0 ? 1 : _decoder.k,
            currentFps: _currentFps,
            goodputKbps: _goodputKbps,
            isComplete: _recoveredFile != null,
          ),

          // Top Camera Controls Action Bar (Feature C)
          Positioned(
            top: 40,
            right: 16,
            child: Row(
              children: [
                IconButton(
                  icon: const Icon(Icons.tune, color: Colors.cyanAccent),
                  tooltip: 'Auto-Detect & Windows Settings',
                  onPressed: () {
                    final profile = CameraCapabilityProfile.analyze(
                      fps: _currentFps,
                      torchAvailable: true,
                    );
                    CameraTuningDialog.show(context, profile);
                  },
                ),
                IconButton(
                  icon: Icon(
                    _cameraController.isTorchOn ? Icons.flash_on : Icons.flash_off,
                    color: Colors.white,
                  ),
                  onPressed: () {
                    setState(() {
                      _cameraController.toggleTorch();
                    });
                  },
                ),
                IconButton(
                  icon: const Icon(Icons.flip_camera_ios, color: Colors.white),
                  onPressed: () {
                    _cameraController.switchCamera();
                  },
                ),
                IconButton(
                  icon: const Icon(Icons.close, color: Colors.white),
                  onPressed: () => Navigator.pop(context),
                ),
              ],
            ),
          ),
        ],
      ),
    );
  }
}
