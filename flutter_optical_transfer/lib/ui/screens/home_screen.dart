import 'dart:io';
import 'package:flutter/material.dart';
import '../../platform/apk_extractor.dart';
import '../../core/camera_tuner.dart';
import '../widgets/camera_tuning_dialog.dart';
import 'send_screen.dart';
import 'receive_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  List<AppItem> _installedApps = [];
  bool _loadingApps = false;

  @override
  void initState() {
    super.initState();
    if (Platform.isAndroid) {
      _loadAndroidApps();
    }
  }

  void _loadAndroidApps() async {
    setState(() => _loadingApps = true);
    final apps = await PlatformFilePicker.getInstalledAndroidApps();
    setState(() {
      _installedApps = apps;
      _loadingApps = false;
    });
  }

  void _selectFileAndSend() async {
    final file = await PlatformFilePicker.pickStandardFile();
    if (file != null && file.bytes != null && mounted) {
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

  void _sendApk(AppItem app) async {
    final bytes = await PlatformFilePicker.readApkBytes(app.apkPath);
    if (bytes != null && mounted) {
      Navigator.push(
        context,
        MaterialPageRoute(
          builder: (_) => SendScreen(
            fileName: "${app.name}.apk",
            fileType: "application/vnd.android.package-archive",
            fileBytes: bytes,
          ),
        ),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F172A), // Modern dark slate
      appBar: AppBar(
        title: const Text("Decimen Optical Transfer"),
        backgroundColor: const Color(0xFF1E293B),
        elevation: 0,
      ),
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(20.0),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.stretch,
            children: [
              // Main Action Buttons
              Row(
                children: [
                  Expanded(
                    child: ElevatedButton.icon(
                      style: ElevatedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 20),
                        backgroundColor: Colors.cyanAccent.shade700,
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                      ),
                      onPressed: _selectFileAndSend,
                      icon: const Icon(Icons.upload_file, color: Colors.white, size: 28),
                      label: const Text(
                        "Send File",
                        style: TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold),
                      ),
                    ),
                  ),
                  const SizedBox(width: 16),
                  Expanded(
                    child: ElevatedButton.icon(
                      style: ElevatedButton.styleFrom(
                        padding: const EdgeInsets.symmetric(vertical: 20),
                        backgroundColor: Colors.greenAccent.shade700,
                        shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
                      ),
                      onPressed: () {
                        Navigator.push(
                          context,
                          MaterialPageRoute(builder: (_) => const ReceiveScreen()),
                        );
                      },
                      icon: const Icon(Icons.camera_alt, color: Colors.white, size: 28),
                      label: const Text(
                        "Receive",
                        style: TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold),
                      ),
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 12),
              OutlinedButton.icon(
                style: OutlinedButton.styleFrom(
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  side: const BorderSide(color: Colors.cyanAccent),
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                ),
                onPressed: () {
                  final profile = CameraCapabilityProfile.analyze(
                    fps: 30.0,
                    torchAvailable: true,
                  );
                  CameraTuningDialog.show(context, profile);
                },
                icon: const Icon(Icons.speed, color: Colors.cyanAccent),
                label: const Text(
                  "Camera Diagnostics & Suggested Windows Settings",
                  style: TextStyle(color: Colors.cyanAccent, fontSize: 13, fontWeight: FontWeight.bold),
                ),
              ),

              const SizedBox(height: 24),

              // APK Extractor Section (Android)
              if (Platform.isAndroid) ...[
                const Text(
                  "Share Installed Android Apps (APK)",
                  style: TextStyle(color: Colors.white, fontSize: 16, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 12),
                Expanded(
                  child: _loadingApps
                      ? const Center(child: CircularProgressIndicator(color: Colors.cyanAccent))
                      : ListView.builder(
                          itemCount: _installedApps.length,
                          itemBuilder: (context, index) {
                            final app = _installedApps[index];
                            return Card(
                              color: const Color(0xFF1E293B),
                              margin: const EdgeInsets.symmetric(vertical: 6),
                              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                              child: ListTile(
                                leading: const CircleAvatar(
                                  backgroundColor: Colors.cyan,
                                  child: Icon(Icons.android, color: Colors.white),
                                ),
                                title: Text(app.name, style: const TextStyle(color: Colors.white)),
                                subtitle: Text(
                                  app.packageName,
                                  style: const TextStyle(color: Colors.white54, fontSize: 12),
                                ),
                                trailing: ElevatedButton(
                                  style: ElevatedButton.styleFrom(backgroundColor: Colors.cyanAccent.shade700),
                                  onPressed: () => _sendApk(app),
                                  child: const Text("Send APK", style: TextStyle(color: Colors.white)),
                                ),
                              ),
                            );
                          },
                        ),
                ),
              ] else ...[
                Expanded(
                  child: Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: const [
                        Icon(Icons.devices, size: 64, color: Colors.white38),
                        SizedBox(height: 16),
                        Text(
                          "Cross-Platform Light Transfer Active\nSelect any file to stream visual fountain codes.",
                          textAlign: TextAlign.center,
                          style: TextStyle(color: Colors.white70, fontSize: 15),
                        ),
                      ],
                    ),
                  ),
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
