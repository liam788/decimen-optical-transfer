import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import '../../core/camera_tuner.dart';

class CameraTuningDialog extends StatelessWidget {
  final CameraCapabilityProfile profile;

  const CameraTuningDialog({super.key, required this.profile});

  static void show(BuildContext context, CameraCapabilityProfile profile) {
    showDialog(
      context: context,
      builder: (context) => CameraTuningDialog(profile: profile),
    );
  }

  @override
  Widget build(BuildContext context) {
    return AlertDialog(
      backgroundColor: const Color(0xFF0F172A),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(16)),
      title: Row(
        children: [
          const Icon(Icons.speed, color: Colors.cyanAccent, size: 28),
          const SizedBox(width: 10),
          const Expanded(
            child: Text(
              'Camera Auto-Detect & Windows Settings Profile',
              style: TextStyle(color: Colors.white, fontSize: 18, fontWeight: FontWeight.bold),
            ),
          ),
        ],
      ),
      content: SingleChildScrollView(
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          mainAxisSize: MainAxisSize.min,
          children: [
            // Performance Badge
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
              decoration: BoxDecoration(
                color: Colors.cyanAccent.withOpacity(0.15),
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: Colors.cyanAccent.withOpacity(0.4)),
              ),
              child: Row(
                children: [
                  const Icon(Icons.check_circle_outline, color: Colors.cyanAccent, size: 18),
                  const SizedBox(width: 8),
                  Text(
                    'Detected Tier: ${profile.performanceTier}',
                    style: const TextStyle(color: Colors.cyanAccent, fontWeight: FontWeight.bold, fontSize: 13),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 16),

            // Camera Specs Section
            const Text(
              '📷 Android Camera Specs',
              style: TextStyle(color: Colors.white70, fontSize: 14, fontWeight: FontWeight.w600),
            ),
            const SizedBox(height: 8),
            _specRow('Measured Capture Rate', '${profile.measuredFps.toStringAsFixed(1)} FPS'),
            _specRow('Flashlight / Torch', profile.hasTorch ? 'Supported ✓' : 'Not Available'),
            _specRow('Resolution Tier', '${profile.resolution.width.toInt()}x${profile.resolution.height.toInt()}'),
            const Divider(color: Colors.white24, height: 24),

            // Suggested Windows PC Settings
            const Text(
              '💻 Recommended Settings for Windows Sender',
              style: TextStyle(color: Colors.greenAccent, fontSize: 14, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 10),
            _settingTile(Icons.grid_4x4, 'Grid Layout', profile.recommendedGrid),
            _settingTile(Icons.motion_photos_on, 'Windows Display FPS', '${profile.recommendedFps} FPS'),
            _settingTile(Icons.data_object, 'Block Payload Size', '${profile.recommendedBlockSize} Bytes / frame'),
            _settingTile(Icons.palette, 'Channel Mode', profile.recommendedColorMode),
            const SizedBox(height: 12),

            // Pro Tip Card
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: const Color(0xFF1E293B),
                borderRadius: BorderRadius.circular(8),
              ),
              child: Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Icon(Icons.lightbulb_outline, color: Colors.amberAccent, size: 20),
                  const SizedBox(width: 8),
                  Expanded(
                    child: Text(
                      profile.tips,
                      style: const TextStyle(color: Colors.white90, fontSize: 12, height: 1.4),
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
      actions: [
        TextButton.icon(
          icon: const Icon(Icons.copy, size: 16, color: Colors.cyanAccent),
          label: const Text('Copy Profile Summary', style: TextStyle(color: Colors.cyanAccent)),
          onPressed: () {
            final text = '''
Decimen Optical Transfer - Windows Profile Recommendation:
• Camera Tier: ${profile.performanceTier} (${profile.measuredFps.toStringAsFixed(1)} FPS)
• Recommended Grid: ${profile.recommendedGrid}
• Recommended Windows FPS: ${profile.recommendedFps} FPS
• Recommended Block Size: ${profile.recommendedBlockSize} Bytes
• Recommended Mode: ${profile.recommendedColorMode}
• Tip: ${profile.tips}
''';
            Clipboard.setData(ClipboardData(text: text));
            ScaffoldMessenger.of(context).showSnackBar(
              const SnackBar(content: Text('Windows recommended settings copied to clipboard!')),
            );
          },
        ),
        ElevatedButton(
          style: ElevatedButton.styleFrom(backgroundColor: Colors.cyanAccent.shade700),
          onPressed: () => Navigator.pop(context),
          child: const Text('Got It', style: TextStyle(color: Colors.black, fontWeight: FontWeight.bold)),
        ),
      ],
    );
  }

  Widget _specRow(String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 2.0),
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(label, style: const TextStyle(color: Colors.white60, fontSize: 13)),
          Text(value, style: const TextStyle(color: Colors.white, fontSize: 13, fontWeight: FontWeight.w500)),
        ],
      ),
    );
  }

  Widget _settingTile(IconData icon, String label, String value) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 4.0),
      child: Row(
        children: [
          Icon(icon, size: 18, color: Colors.greenAccent),
          const SizedBox(width: 8),
          Text(label, style: const TextStyle(color: Colors.white70, fontSize: 13)),
          const Spacer(),
          Text(value, style: const TextStyle(color: Colors.greenAccent, fontSize: 13, fontWeight: FontWeight.bold)),
        ],
      ),
    );
  }
}
