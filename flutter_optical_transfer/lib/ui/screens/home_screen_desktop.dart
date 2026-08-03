import 'dart:io';
import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:file_picker/file_picker.dart';
import 'send_screen.dart';
import 'receive_screen_desktop.dart';

/// Desktop home screen — Send & Receive UI for Windows/macOS/Linux.
class HomeScreenDesktop extends StatelessWidget {
  const HomeScreenDesktop({super.key});

  void _selectFileAndSend(BuildContext context) async {
    final result = await FilePicker.platform.pickFiles(
      type: FileType.any,
      withData: true,
    );
    if (result == null || result.files.isEmpty) return;
    final file = result.files.first;
    if (file.bytes == null) return;

    if (context.mounted) {
      Navigator.push(
        context,
        MaterialPageRoute(
          builder: (_) => SendScreen(
            fileName: file.name,
            fileType: file.extension ?? 'application/octet-stream',
            fileBytes: file.bytes!,
          ),
        ),
      );
    }
  }

  void _openReceiveScreen(BuildContext context) {
    Navigator.push(
      context,
      MaterialPageRoute(
        builder: (_) => const ReceiveScreenDesktop(),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F172A),
      appBar: AppBar(
        title: const Text('Decimen Optical Transfer'),
        backgroundColor: const Color(0xFF1E293B),
        elevation: 0,
      ),
      body: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.symmetric(horizontal: 40.0, vertical: 32.0),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              // App icon / hero
              Container(
                width: 90,
                height: 90,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  gradient: LinearGradient(
                    colors: [Colors.cyanAccent.shade700, Colors.blueAccent.shade700],
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                  ),
                ),
                child: const Icon(Icons.wifi_tethering, size: 48, color: Colors.white),
              ),
              const SizedBox(height: 24),
              const Text(
                'Decimen Optical Transfer',
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 28,
                  fontWeight: FontWeight.bold,
                  letterSpacing: 1.2,
                ),
              ),
              const SizedBox(height: 8),
              const Text(
                'Zero-network file transfer via fountain-coded light streams',
                style: TextStyle(color: Colors.white54, fontSize: 15),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 40),

              // Action buttons row (Send & Receive)
              Wrap(
                spacing: 20,
                runSpacing: 20,
                alignment: WrapAlignment.center,
                children: [
                  // Send button card
                  SizedBox(
                    width: 300,
                    height: 56,
                    child: ElevatedButton.icon(
                      style: ElevatedButton.styleFrom(
                        backgroundColor: Colors.cyanAccent.shade700,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(16),
                        ),
                      ),
                      onPressed: () => _selectFileAndSend(context),
                      icon: const Icon(Icons.upload_file, color: Colors.white, size: 24),
                      label: const Text(
                        'Send File via Light',
                        style: TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold),
                      ),
                    ),
                  ),

                  // Receive button card
                  SizedBox(
                    width: 300,
                    height: 56,
                    child: ElevatedButton.icon(
                      style: ElevatedButton.styleFrom(
                        backgroundColor: Colors.tealAccent.shade700,
                        shape: RoundedRectangleBorder(
                          borderRadius: BorderRadius.circular(16),
                        ),
                      ),
                      onPressed: () => _openReceiveScreen(context),
                      icon: const Icon(Icons.qr_code_scanner, color: Colors.white, size: 24),
                      label: const Text(
                        'Receive File on Windows',
                        style: TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold),
                      ),
                    ),
                  ),
                ],
              ),

              const SizedBox(height: 36),

              // Info card
              Container(
                width: 480,
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: const Color(0xFF1E293B),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: Colors.white12),
                ),
                child: Row(
                  children: const [
                    Icon(Icons.info_outline, color: Colors.cyanAccent, size: 24),
                    SizedBox(width: 12),
                    Expanded(
                      child: Text(
                        'To send to phone: Click "Send File via Light" & scan with phone.\nTo receive from phone: Click "Receive File on Windows" & point phone screen at webcam.',
                        style: TextStyle(color: Colors.white70, fontSize: 13, height: 1.4),
                      ),
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

