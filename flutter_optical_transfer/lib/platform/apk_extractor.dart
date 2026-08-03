import 'dart:io';
import 'dart:typed_data';
import 'package:flutter/services.dart';
import 'package:file_picker/file_picker.dart';
import 'package:path/path.dart' as p;

class AppItem {
  final String name;
  final String packageName;
  final String apkPath;
  final int size;

  AppItem({
    required this.name,
    required this.packageName,
    required this.apkPath,
    required this.size,
  });
}

class PlatformFilePicker {
  static const MethodChannel _channel = MethodChannel('optical_transfer/apk');

  /// Fetch installed Android apps (APKs) via native MethodChannel
  static Future<List<AppItem>> getInstalledAndroidApps() async {
    if (!Platform.isAndroid) return [];

    try {
      final List<dynamic> apps = await _channel.invokeMethod('getInstalledApps');
      return apps.map((map) {
        return AppItem(
          name: map['name'] ?? 'Unknown App',
          packageName: map['packageName'] ?? '',
          apkPath: map['apkPath'] ?? '',
          size: map['size'] ?? 0,
        );
      }).toList();
    } catch (_) {
      return [];
    }
  }

  /// Pick any standard file (PDF, image, video, document, archive)
  static Future<PlatformFile?> pickStandardFile() async {
    final result = await FilePicker.platform.pickFiles(
      type: FileType.any,
      withData: true,
    );
    return result?.files.first;
  }

  /// Read APK bytes from path for transfer
  static Future<Uint8List?> readApkBytes(String apkPath) async {
    try {
      final file = File(apkPath);
      return await file.readAsBytes();
    } catch (_) {
      return null;
    }
  }
}
