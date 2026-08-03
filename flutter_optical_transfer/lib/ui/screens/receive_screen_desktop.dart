import 'dart:io';
import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:path_provider/path_provider.dart';

/// Desktop Receive Screen — Provides Optical Stream Receiving capabilities for Windows.
class ReceiveScreenDesktop extends StatefulWidget {
  const ReceiveScreenDesktop({super.key});

  @override
  State<ReceiveScreenDesktop> createState() => _ReceiveScreenDesktopState();
}

class _ReceiveScreenDesktopState extends State<ReceiveScreenDesktop> {
  bool _isOpeningBrowser = false;
  String? _statusMessage;

  Future<void> _launchCameraWebReceiver() async {
    setState(() {
      _isOpeningBrowser = true;
      _statusMessage = 'Launching camera optical receiver...';
    });

    try {
      // 1. Extract asset html to temp directory
      final tempDir = await getTemporaryDirectory();
      final targetFile = File('${tempDir.path}/decimen-receiver.html');

      // Load from Flutter rootBundle assets
      final htmlBytes = await rootBundle.load('assets/decimen-receiver.html');
      await targetFile.writeAsBytes(
        htmlBytes.buffer.asUint8List(htmlBytes.offsetInBytes, htmlBytes.lengthInBytes),
        flush: true,
      );

      // 2. Open HTML in default Windows browser (Edge/Chrome/Firefox)
      if (Platform.isWindows) {
        await Process.run('cmd', ['/c', 'start', '', targetFile.path]);
      } else {
        await Process.run('open', [targetFile.path]);
      }

      setState(() {
        _statusMessage = 'Camera Receiver opened in browser!';
      });
    } catch (e) {
      // Fallback: try starting directly or opening release URL
      try {
        await Process.run('cmd', [
          '/c',
          'start',
          'https://github.com/liam788/decimen-optical-transfer/releases/latest/download/decimen-receiver.html'
        ]);
        setState(() {
          _statusMessage = 'Opened Online Optical Receiver fallback!';
        });
      } catch (err) {
        setState(() {
          _statusMessage = 'Failed to launch receiver: $e';
        });
      }
    } finally {
      if (mounted) {
        setState(() {
          _isOpeningBrowser = false;
        });
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F172A),
      appBar: AppBar(
        title: const Text('Receive Files via Light Stream'),
        backgroundColor: const Color(0xFF1E293B),
        elevation: 0,
      ),
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(32.0),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              // Hero Icon
              Container(
                width: 90,
                height: 90,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  gradient: LinearGradient(
                    colors: [Colors.greenAccent.shade700, Colors.tealAccent.shade700],
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                  ),
                ),
                child: const Icon(Icons.qr_code_scanner, size: 48, color: Colors.white),
              ),

              const SizedBox(height: 24),

              const Text(
                'Receive Stream on Windows',
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 26,
                  fontWeight: FontWeight.bold,
                ),
              ),

              const SizedBox(height: 8),

              const Text(
                'Capture fountain-coded optical frames from sending phones or screens',
                style: TextStyle(color: Colors.white54, fontSize: 14),
                textAlign: TextAlign.center,
              ),

              const SizedBox(height: 36),

              // Action Card 1: Launch Camera Receiver in Browser
              Container(
                width: 480,
                padding: const EdgeInsets.all(24),
                decoration: BoxDecoration(
                  color: const Color(0xFF1E293B),
                  borderRadius: BorderRadius.circular(16),
                  border: Border.all(color: Colors.tealAccent.withOpacity(0.3)),
                  boxShadow: [
                    BoxShadow(
                      color: Colors.black.withOpacity(0.3),
                      blurRadius: 10,
                      spreadRadius: 1,
                    )
                  ],
                ),
                child: Column(
                  children: [
                    const Icon(Icons.camera_front, size: 40, color: Colors.tealAccent),
                    const SizedBox(height: 12),
                    const Text(
                      'High-Speed Camera Receiver',
                      style: TextStyle(
                        color: Colors.white,
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      'Uses your PC webcam to scan and decode optical light streams in real-time with zero network connection.',
                      style: TextStyle(color: Colors.white60, fontSize: 13),
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: 20),
                    SizedBox(
                      width: 320,
                      height: 48,
                      child: ElevatedButton.icon(
                        style: ElevatedButton.styleFrom(
                          backgroundColor: Colors.tealAccent.shade700,
                          shape: RoundedRectangleBorder(
                            borderRadius: BorderRadius.circular(12),
                          ),
                        ),
                        onPressed: _isOpeningBrowser ? null : _launchCameraWebReceiver,
                        icon: _isOpeningBrowser
                            ? const SizedBox(
                                width: 20,
                                height: 20,
                                child: CircularProgressIndicator(
                                  strokeWidth: 2,
                                  color: Colors.white,
                                ),
                              )
                            : const Icon(Icons.open_in_browser, color: Colors.white),
                        label: Text(
                          _isOpeningBrowser ? 'Opening Receiver...' : 'Open Camera Optical Receiver',
                          style: const TextStyle(
                            color: Colors.white,
                            fontSize: 15,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                      ),
                    ),
                  ],
                ),
              ),

              if (_statusMessage != null) ...[
                const SizedBox(height: 16),
                Text(
                  _statusMessage!,
                  style: const TextStyle(color: Colors.tealAccent, fontSize: 14),
                ),
              ],

              const SizedBox(height: 32),

              // Instructions Card
              Container(
                width: 480,
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: const Color(0xFF1E293B).withOpacity(0.6),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: Colors.white12),
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: const [
                    Row(
                      children: [
                        Icon(Icons.lightbulb_outline, color: Colors.amberAccent, size: 20),
                        SizedBox(width: 8),
                        Text(
                          'How to Receive:',
                          style: TextStyle(color: Colors.white, fontWeight: FontWeight.bold, fontSize: 14),
                        ),
                      ],
                    ),
                    SizedBox(height: 8),
                    Text(
                      '1. Click "Open Camera Optical Receiver" above.\n'
                      '2. Grant camera access when prompted.\n'
                      '3. Point your phone\'s streaming screen at the PC webcam.\n'
                      '4. The file will reconstruct automatically as frames are captured!',
                      style: TextStyle(color: Colors.white70, fontSize: 13, height: 1.5),
                    ),
                  ],
                ),
              ),
            ],
          ),
        ),
      ),
    );
  }
}
