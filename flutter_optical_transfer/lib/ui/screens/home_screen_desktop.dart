import 'dart:io';
import 'package:flutter/material.dart';
import 'package:file_picker/file_picker.dart';
import 'package:path_provider/path_provider.dart';
import '../theme/app_theme.dart';
import 'send_screen.dart';
import 'receive_screen_desktop.dart';


/// Professional Windows Desktop Workstation with Brand Design System
class HomeScreenDesktop extends StatefulWidget {
  const HomeScreenDesktop({super.key});

  @override
  State<HomeScreenDesktop> createState() => _HomeScreenDesktopState();
}

class _HomeScreenDesktopState extends State<HomeScreenDesktop> {
  int _selectedNavIndex = 0;

  void _selectFileAndSend() async {
    final result = await FilePicker.platform.pickFiles(
      type: FileType.any,
      withData: true,
    );
    if (result == null || result.files.isEmpty) return;
    final file = result.files.first;
    if (file.bytes == null) return;

    if (mounted) {
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
      backgroundColor: AppColors.opticalBlack,
      body: Row(
        children: [
          // 1. Desktop Left Navigation Rail (Section 24 of Brand Guide)
          Container(
            width: 260,
            decoration: const BoxDecoration(
              color: AppColors.secondaryBackground,
              border: Border(right: BorderSide(color: AppColors.border, width: 1)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                // Brand Header with Monogram
                Padding(
                  padding: const EdgeInsets.symmetric(horizontal: 24, vertical: 28),
                  child: Row(
                    children: [
                      Image.asset(
                        'assets/branding/app_icon_64.png',
                        width: 36,
                        height: 36,
                        errorBuilder: (_, __, ___) => Container(
                          width: 36,
                          height: 36,
                          decoration: BoxDecoration(
                            color: AppColors.surfaceElevated,
                            borderRadius: BorderRadius.circular(8),
                          ),
                          child: const Icon(Icons.blur_on, color: AppColors.opticalGreen),
                        ),
                      ),
                      const SizedBox(width: 14),
                      Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: const [
                          Text(
                            'OPTICAL',
                            style: TextStyle(
                              color: AppColors.textEmphasis,
                              fontSize: 14,
                              fontWeight: FontWeight.bold,
                              letterSpacing: 2.0,
                            ),
                          ),
                          Text(
                            'TRANSFER',
                            style: TextStyle(
                              color: AppColors.opticalGreen,
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                              letterSpacing: 1.5,
                            ),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),

                const Divider(height: 1),
                const SizedBox(height: 16),

                // Navigation Items
                _buildNavItem(0, Icons.dashboard_outlined, 'Dashboard'),
                _buildNavItem(1, Icons.qr_code_scanner, 'Receive Stream'),
                _buildNavItem(2, Icons.send_outlined, 'Send Stream'),
                _buildNavItem(3, Icons.folder_outlined, 'Transfers & History'),

                const Spacer(),
                const Divider(height: 1),

                // Footer Status
                Padding(
                  padding: const EdgeInsets.all(20),
                  child: Row(
                    children: [
                      Container(
                        width: 8,
                        height: 8,
                        decoration: const BoxDecoration(
                          shape: BoxShape.circle,
                          color: AppColors.success,
                        ),
                      ),
                      const SizedBox(width: 10),
                      const Text(
                        'Engine Ready • Air-Gapped',
                        style: TextStyle(color: AppColors.textMuted, fontSize: 11),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),

          // 2. Main Content Workspace
          Expanded(
            child: IndexedStack(
              index: _selectedNavIndex,
              children: [
                _buildDashboardView(),
                const ReceiveScreenDesktop(),
                _buildSendLandingView(),
                _buildHistoryView(),
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildNavItem(int index, IconData icon, String title) {
    final isSelected = _selectedNavIndex == index;
    return Container(
      margin: const EdgeInsets.symmetric(horizontal: 12, vertical: 4),
      decoration: BoxDecoration(
        color: isSelected ? AppColors.surfaceElevated : Colors.transparent,
        borderRadius: BorderRadius.circular(8),
        border: isSelected ? Border.all(color: AppColors.borderDisabled) : null,
      ),
      child: ListTile(
        dense: true,
        leading: Icon(
          icon,
          color: isSelected ? AppColors.opticalGreen : AppColors.textSecondary,
          size: 20,
        ),
        title: Text(
          title,
          style: TextStyle(
            color: isSelected ? AppColors.textEmphasis : AppColors.textSecondary,
            fontSize: 14,
            fontWeight: isSelected ? FontWeight.w600 : FontWeight.w400,
          ),
        ),
        onTap: () => setState(() => _selectedNavIndex = index),
      ),
    );
  }

  Widget _buildDashboardView() {
    return SingleChildScrollView(
      padding: const EdgeInsets.all(36),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          // Hero Banner
          Container(
            width: double.infinity,
            padding: const EdgeInsets.all(32),
            decoration: BoxDecoration(
              color: AppColors.surface,
              borderRadius: BorderRadius.circular(16),
              border: Border.all(color: AppColors.border),
            ),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Zero-Network Optical Transfer',
                        style: TextStyle(
                          color: AppColors.textEmphasis,
                          fontSize: 26,
                          fontWeight: FontWeight.bold,
                          letterSpacing: 0.5,
                        ),
                      ),
                      const SizedBox(height: 8),
                      const Text(
                        'Transfer files directly between PC, Phone, and Mac using fountain-coded light streams. No Wi-Fi, no Bluetooth, no cloud pairing.',
                        style: TextStyle(color: AppColors.textSecondary, fontSize: 14, height: 1.5),
                      ),
                      const SizedBox(height: 24),
                      Row(
                        children: [
                          ElevatedButton.icon(
                            onPressed: _selectFileAndSend,
                            icon: const Icon(Icons.upload_file, size: 18),
                            label: const Text('Send File via Screen'),
                          ),
                          const SizedBox(width: 16),
                          OutlinedButton.icon(
                            onPressed: () => setState(() => _selectedNavIndex = 1),
                            icon: const Icon(Icons.qr_code_scanner, size: 18, color: AppColors.opticalGreen),
                            label: const Text('Receive from Camera'),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
                const SizedBox(width: 32),
                Image.asset(
                  'assets/branding/app_icon_192.png',
                  width: 120,
                  height: 120,
                  errorBuilder: (_, __, ___) => const SizedBox.shrink(),
                ),
              ],
            ),
          ),

          const SizedBox(height: 32),

          // Features & Workflows Grid
          const Text(
            'WORKFLOWS & DIAGNOSTICS',
            style: TextStyle(
              color: AppColors.textMuted,
              fontSize: 12,
              fontWeight: FontWeight.bold,
              letterSpacing: 1.2,
            ),
          ),
          const SizedBox(height: 16),

          Row(
            children: [
              _buildFeatureCard(
                icon: Icons.send_to_mobile,
                title: 'PC to Mobile Stream',
                desc: 'Select any file on Windows. The screen projects rapid fountain QR droplets for your phone camera to scan.',
                actionText: 'Start Send',
                onAction: _selectFileAndSend,
              ),
              const SizedBox(width: 20),
              _buildFeatureCard(
                icon: Icons.camera_enhance,
                title: 'Mobile to PC Stream',
                desc: 'Hold your phone screen facing your webcam. The built-in in-app receiver captures and reassembles files automatically.',
                actionText: 'Open Receiver',
                onAction: () => setState(() => _selectedNavIndex = 1),
              ),
            ],
          ),
        ],
      ),
    );
  }

  Widget _buildFeatureCard({
    required IconData icon,
    required String title,
    required String desc,
    required String actionText,
    required VoidCallback onAction,
  }) {
    return Expanded(
      child: Container(
        padding: const EdgeInsets.all(24),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: AppColors.border),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Container(
              padding: const EdgeInsets.all(10),
              decoration: BoxDecoration(
                color: AppColors.surfaceElevated,
                borderRadius: BorderRadius.circular(8),
              ),
              child: Icon(icon, color: AppColors.opticalGreen, size: 24),
            ),
            const SizedBox(height: 16),
            Text(
              title,
              style: const TextStyle(
                color: AppColors.textEmphasis,
                fontSize: 16,
                fontWeight: FontWeight.bold,
              ),
            ),
            const SizedBox(height: 8),
            Text(
              desc,
              style: const TextStyle(color: AppColors.textSecondary, fontSize: 13, height: 1.4),
            ),
            const SizedBox(height: 20),
            TextButton.icon(
              onPressed: onAction,
              icon: const Icon(Icons.arrow_forward, size: 16, color: AppColors.opticalGreen),
              label: Text(actionText, style: const TextStyle(color: AppColors.opticalGreen, fontWeight: FontWeight.bold)),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildSendLandingView() {
    return Center(
      child: Container(
        width: 540,
        padding: const EdgeInsets.all(36),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(16),
          border: Border.all(color: AppColors.border),
        ),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Container(
              padding: const EdgeInsets.all(16),
              decoration: const BoxDecoration(
                color: AppColors.surfaceElevated,
                shape: BoxShape.circle,
              ),
              child: const Icon(Icons.cloud_upload_outlined, color: AppColors.opticalGreen, size: 48),
            ),
            const SizedBox(height: 20),
            const Text(
              'Select File to Transmit',
              style: TextStyle(color: AppColors.textEmphasis, fontSize: 20, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 8),
            const Text(
              'Choose any document, image, archive, or binary. The app will chunk and encode it into a continuous fountain-coded visual stream.',
              style: TextStyle(color: AppColors.textSecondary, fontSize: 13),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 28),
            SizedBox(
              width: 280,
              height: 48,
              child: ElevatedButton.icon(
                onPressed: _selectFileAndSend,
                icon: const Icon(Icons.file_open, size: 18),
                label: const Text('Browse Files on Windows'),
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildHistoryView() {
    return Padding(
      padding: const EdgeInsets.all(36),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            mainAxisAlignment: MainAxisAlignment.spaceBetween,
            children: [
              const Text(
                'TRANSFERS & DOWNLOADS',
                style: TextStyle(color: AppColors.textMuted, fontSize: 12, fontWeight: FontWeight.bold, letterSpacing: 1.2),
              ),
              ElevatedButton.icon(
                onPressed: () async {
                  final dir = await getDownloadsDirectory() ?? await getApplicationDocumentsDirectory();
                  final target = Directory("${dir.path}/OpticalTransfer");
                  if (!target.existsSync()) target.createSync(recursive: true);
                  Process.run('explorer.exe', [target.path]);
                },
                icon: const Icon(Icons.folder, size: 16),
                label: const Text('Open OpticalTransfer Folder'),
                style: ElevatedButton.styleFrom(
                  backgroundColor: AppColors.surfaceElevated,
                  foregroundColor: AppColors.textPrimary,
                  side: const BorderSide(color: AppColors.border),
                ),
              ),
            ],
          ),
          const SizedBox(height: 20),
          Expanded(
            child: Container(
              width: double.infinity,
              padding: const EdgeInsets.all(24),
              decoration: BoxDecoration(
                color: AppColors.surface,
                borderRadius: BorderRadius.circular(14),
                border: Border.all(color: AppColors.border),
              ),
              child: const Center(
                child: Text(
                  'Received files are automatically saved to your Downloads/OpticalTransfer directory.\nZero cloud servers or network trace.',
                  style: TextStyle(color: AppColors.textMuted, fontSize: 13, height: 1.6),
                  textAlign: TextAlign.center,
                ),
              ),
            ),
          ),
        ],
      ),
    );
  }
}

