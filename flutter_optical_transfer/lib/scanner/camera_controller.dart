import 'package:flutter/material.dart';
import 'package:mobile_scanner/mobile_scanner.dart';

/// Feature C: Optics & Camera Hardware Controller
class OpticalCameraController {
  final MobileScannerController controller = MobileScannerController(
    detectionSpeed: DetectionSpeed.unrestricted,
    returnImage: false,
    torchEnabled: false,
  );

  bool isTorchOn = false;
  bool isFocusLocked = false;

  void toggleTorch() async {
    isTorchOn = !isTorchOn;
    await controller.toggleTorch();
  }

  void switchCamera() async {
    await controller.switchCamera();
  }

  void dispose() {
    controller.dispose();
  }
}
