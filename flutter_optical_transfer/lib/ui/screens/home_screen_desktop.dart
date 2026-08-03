import 'dart:io';
import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:file_picker/file_picker.dart';
import 'send_screen.dart';

/// Desktop home screen — Send-only UI for Windows/macOS/Linux.
/// Camera receive requires mobile app (Android/iOS) or web app.
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
        child: Padding(
          padding: const EdgeInsets.all(40.0),
          child: Column(
            mainAxisAlignment: MainAxisAlignment.center,
            children: [
              // App icon / hero
              Container(
                width: 100,
                height: 100,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  gradient: LinearGradient(
                    colors: [Colors.cyanAccent.shade700, Colors.blueAccent.shade700],
                    begin: Alignment.topLeft,
                    end: Alignment.bottomRight,
                  ),
                ),
                child: const Icon(Icons.wifi_tethering, size: 52, color: Colors.white),
              ),
              const SizedBox(height: 32),
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
              const SizedBox(height: 48),

              // Send button
              SizedBox(
                width: 320,
                height: 56,
                child: ElevatedButton.icon(
                  style: ElevatedButton.styleFrom(
                    backgroundColor: Colors.cyanAccent.shade700,
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(16),
                    ),
                  ),
                  onPressed: () => _selectFileAndSend(context),
                  icon: const Icon(Icons.upload_file, color: Colors.white, size: 26),
                  label: const Text(
                    'Select File & Send via Light',
                    style: TextStyle(color: Colors.white, fontSize: 17, fontWeight: FontWeight.bold),
                  ),
                ),
              ),

              const SizedBox(height: 32),

              // Info about receive
              Container(
                width: 320,
                padding: const EdgeInsets.all(16),
                decoration: BoxDecoration(
                  color: const Color(0xFF1E293B),
                  borderRadius: BorderRadius.circular(12),
                  border: Border.all(color: Colors.white12),
                ),
                child: Column(
                  children: const [
                    Icon(Icons.info_outline, color: Colors.cyanAccent, size: 22),
                    SizedBox(height: 8),
                    Text(
                      'To receive files, use the Android/iOS mobile app or open the web app (decimen-receiver.html) on a device with a camera.',
                      style: TextStyle(color: Colors.white60, fontSize: 13),
                      textAlign: TextAlign.center,
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
