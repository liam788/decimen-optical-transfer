// Windows stub for mobile_scanner — camera scanning not supported on Windows desktop.
// The ReceiveScreen is hidden on Windows; this file satisfies the import.
library mobile_scanner;

class BarcodeCapture {
  final List<Barcode> barcodes = const [];
}

class Barcode {
  final String? rawValue = null;
}

class MobileScannerController {
  MobileScannerController({
    DetectionSpeed detectionSpeed = DetectionSpeed.normal,
    bool returnImage = false,
    bool torchEnabled = false,
  });

  Future<void> toggleTorch() async {}
  Future<void> switchCamera() async {}
  void dispose() {}
}

class MobileScanner extends StatelessWidget {
  final MobileScannerController? controller;
  final void Function(BarcodeCapture)? onDetect;
  const MobileScanner({super.key, this.controller, this.onDetect});

  @override
  Widget build(BuildContext context) {
    return const Center(
      child: Text(
        'Camera scanning is not supported on Windows.\nUse the web or mobile app to receive files.',
        style: TextStyle(color: Colors.white70),
        textAlign: TextAlign.center,
      ),
    );
  }
}

enum DetectionSpeed { unrestricted, normal, noDuplicates }

// Re-export Flutter to satisfy imports
import 'package:flutter/material.dart';
