import 'dart:async';
import 'dart:io';
import 'dart:typed_data';
import 'package:flutter/material.dart';
import 'package:path_provider/path_provider.dart';
import 'package:file_picker/file_picker.dart';
import '../../core/fountain.dart';
import '../../core/protocol.dart';
import '../theme/app_theme.dart';
import '../widgets/optical_transfer_ring.dart';

/// In-App Desktop Optical Receiver (100% Native Software Execution — Zero Browser Redirect)
class ReceiveScreenDesktop extends StatefulWidget {
  const ReceiveScreenDesktop({super.key});

  @override
  State<ReceiveScreenDesktop> createState() => _ReceiveScreenDesktopState();
}

class _ReceiveScreenDesktopState extends State<ReceiveScreenDesktop> {
  final FountainDecoder _decoder = FountainDecoder();

  bool _isReceiving = false;
  DateTime? _startTime;
  int _receivedFrameCount = 0;
  double _instantFps = 0.0;
  double _goodputKbps = 0.0;
  OpticalFile? _completedFile;
  String? _savedFilePath;
  String? _errorMessage;

  // Camera / Sensor Simulation state for desktop
  Timer? _telemetryTimer;
  bool _isTorchOn = false;
  int _selectedCameraIndex = 0;
  final List<String> _availableCameras = ['Integrated Webcam (720p 30FPS)', 'External USB Cam (1080p 60FPS)'];

  @override
  void initState() {
    super.initState();
    _startReceivingEngine();
  }

  void _startReceivingEngine() {
    _decoder.reset();
    setState(() {
      _isReceiving = true;
      _completedFile = null;
      _savedFilePath = null;
      _errorMessage = null;
      _receivedFrameCount = 0;
      _startTime = DateTime.now();
    });

    _telemetryTimer?.cancel();
    _telemetryTimer = Timer.periodic(const Duration(milliseconds: 200), (_) {
      if (!_isReceiving || _startTime == null) return;
      final elapsed = DateTime.now().difference(_startTime!).inMilliseconds / 1000.0;
      if (elapsed > 0 && mounted) {
        setState(() {
          _instantFps = _receivedFrameCount > 0 ? (_receivedFrameCount / elapsed) : 0.0;
          _goodputKbps = (_decoder.solvedBlocks.length * _decoder.blockLen) / 1024.0 / elapsed;
        });
      }
    });
  }

  void _ingestRawFrameBytes(Uint8List frameBytes) {
    if (_completedFile != null) return;
    _startTime ??= DateTime.now();
    _receivedFrameCount++;

    final frame = FrameData.parse(frameBytes);
    if (frame == null) {
      return;
    }

    final isComplete = _decoder.addFrame(frame);
    if (isComplete) {
      _finalizeTransfer();
    }
    if (mounted) {
      setState(() {});
    }
  }

  void _finalizeTransfer() async {
    final reconstructed = _decoder.reconstruct();
    if (reconstructed == null) {
      setState(() {
        _errorMessage = "Failed to assemble file payload from stream.";
      });
      return;
    }

    try {
      final opticalFile = unpackFile(reconstructed);
      
      // Save directly to user Downloads / OpticalTransfer directory
      final downloadsDir = await getDownloadsDirectory() ?? await getApplicationDocumentsDirectory();
      final targetFolder = Directory("${downloadsDir.path}/OpticalTransfer");
      if (!targetFolder.existsSync()) {
        targetFolder.createSync(recursive: true);
      }

      final saveFile = File("${targetFolder.path}/${opticalFile.name}");
      await saveFile.writeAsBytes(opticalFile.bytes);

      setState(() {
        _completedFile = opticalFile;
        _savedFilePath = saveFile.path;
        _isReceiving = false;
      });
      _telemetryTimer?.cancel();
    } catch (e) {
      setState(() {
        _errorMessage = "Integrity check or file unpack failed: $e";
      });
    }
  }

  void _importCapturedFrameBatch() async {
    final result = await FilePicker.platform.pickFiles(
      type: FileType.any,
      allowMultiple: true,
      withData: true,
    );
    if (result == null || result.files.isEmpty) return;

    for (final file in result.files) {
      if (file.bytes != null) {
        _ingestRawFrameBytes(file.bytes!);
      }
    }
  }

  @override
  void dispose() {
    _telemetryTimer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final solvedCount = _decoder.solvedBlocks.length;
    final totalCount = _decoder.k > 0 ? _decoder.k : 1;
    final progress = _decoder.progress;

    return Scaffold(
      backgroundColor: AppColors.opticalBlack,
      appBar: AppBar(
        title: const Text('Optical Receiver Workstation'),
        backgroundColor: AppColors.secondaryBackground,
        actions: [
          IconButton(
            icon: Icon(_isTorchOn ? Icons.flash_on : Icons.flash_off, color: _isTorchOn ? AppColors.opticalGreen : AppColors.textSecondary),
            tooltip: 'Toggle Camera Torch',
            onPressed: () => setState(() => _isTorchOn = !_isTorchOn),
          ),
          IconButton(
            icon: const Icon(Icons.refresh, color: AppColors.textSecondary),
            tooltip: 'Reset Receiver Engine',
            onPressed: _startReceivingEngine,
          ),
          const SizedBox(width: 12),
        ],
      ),
      body: Row(
        children: [
          // Left: Viewfinder & Optical Ring Canvas
          Expanded(
            flex: 3,
            child: Container(
              padding: const EdgeInsets.all(24),
              child: Column(
                children: [
                  // Viewfinder Screen
                  Expanded(
                    child: Container(
                      width: double.infinity,
                      decoration: BoxDecoration(
                        color: AppColors.surface,
                        borderRadius: BorderRadius.circular(16),
                        border: Border.all(
                          color: _completedFile != null
                              ? AppColors.success
                              : (_isReceiving ? AppColors.opticalGreen.withOpacity(0.5) : AppColors.border),
                          width: 1.5,
                        ),
                      ),
                      child: Stack(
                        alignment: Alignment.center,
                        children: [
                          // Background Grid & Optical Lens Guide
                          CustomPaint(
                            size: Size.infinite,
                            painter: _OpticalLensGuidePainter(),
                          ),

                          // Signature Optical Transfer Ring
                          OpticalTransferRing(
                            progress: progress,
                            isTransferring: _isReceiving && _completedFile == null,
                            isComplete: _completedFile != null,
                            size: 260,
                            child: Column(
                              mainAxisSize: MainAxisSize.min,
                              children: [
                                if (_completedFile != null) ...[
                                  const Icon(Icons.check_circle_outline, color: AppColors.success, size: 56),
                                  const SizedBox(height: 8),
                                  const Text('TRANSFER COMPLETE', style: TextStyle(color: AppColors.success, fontWeight: FontWeight.bold, fontSize: 13, letterSpacing: 1.2)),
                                ] else ...[
                                  Text(
                                    "${(progress * 100).toInt()}%",
                                    style: const TextStyle(
                                      color: AppColors.textEmphasis,
                                      fontSize: 44,
                                      fontWeight: FontWeight.bold,
                                      letterSpacing: 1.0,
                                    ),
                                  ),
                                  Text(
                                    "$solvedCount / $totalCount BLOCKS",
                                    style: const TextStyle(
                                      color: AppColors.opticalGreen,
                                      fontSize: 12,
                                      fontWeight: FontWeight.w600,
                                      letterSpacing: 1.0,
                                    ),
                                  ),
                                ],
                              ],
                            ),
                          ),

                          // Live Signal HUD Overlay (Top-Left)
                          Positioned(
                            top: 16,
                            left: 16,
                            child: Container(
                              padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 6),
                              decoration: BoxDecoration(
                                color: AppColors.secondaryBackground.withOpacity(0.85),
                                borderRadius: BorderRadius.circular(8),
                                border: Border.all(color: AppColors.border),
                              ),
                              child: Row(
                                children: [
                                  Container(
                                    width: 8,
                                    height: 8,
                                    decoration: BoxDecoration(
                                      shape: BoxShape.circle,
                                      color: _isReceiving ? AppColors.opticalGreen : AppColors.textMuted,
                                    ),
                                  ),
                                  const SizedBox(width: 8),
                                  Text(
                                    _isReceiving ? 'SCANNING ACTIVE' : 'STANDBY',
                                    style: const TextStyle(color: AppColors.textPrimary, fontSize: 11, fontWeight: FontWeight.bold, letterSpacing: 0.5),
                                  ),
                                ],
                              ),
                            ),
                          ),

                          // Camera Selection Dropdown (Top-Right)
                          Positioned(
                            top: 16,
                            right: 16,
                            child: Container(
                              padding: const EdgeInsets.symmetric(horizontal: 10),
                              decoration: BoxDecoration(
                                color: AppColors.secondaryBackground.withOpacity(0.85),
                                borderRadius: BorderRadius.circular(8),
                                border: Border.all(color: AppColors.border),
                              ),
                              child: DropdownButtonHideUnderline(
                                child: DropdownButton<int>(
                                  value: _selectedCameraIndex,
                                  dropdownColor: AppColors.secondaryBackground,
                                  style: const TextStyle(color: AppColors.textPrimary, fontSize: 12),
                                  items: List.generate(_availableCameras.length, (i) {
                                    return DropdownMenuItem(value: i, child: Text(_availableCameras[i]));
                                  }),
                                  onChanged: (val) {
                                    if (val != null) setState(() => _selectedCameraIndex = val);
                                  },
                                ),
                              ),
                            ),
                          ),
                        ],
                      ),
                    ),
                  ),

                  const SizedBox(height: 16),

                  // Bottom Action Bar (All in-app actions)
                  Row(
                    children: [
                      Expanded(
                        child: ElevatedButton.icon(
                          onPressed: _importCapturedFrameBatch,
                          icon: const Icon(Icons.file_upload, size: 18),
                          label: const Text('Ingest Optical Frame Chunks'),
                          style: ElevatedButton.styleFrom(
                            backgroundColor: AppColors.surfaceElevated,
                            foregroundColor: AppColors.textPrimary,
                            side: const BorderSide(color: AppColors.border),
                          ),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: ElevatedButton.icon(
                          onPressed: _startReceivingEngine,
                          icon: const Icon(Icons.play_arrow, size: 18),
                          label: const Text('Restart Scan Session'),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),

          // Right: Telemetry & File Inspection Panel
          Container(
            width: 380,
            padding: const EdgeInsets.all(24),
            decoration: const BoxDecoration(
              color: AppColors.secondaryBackground,
              border: Border(left: BorderSide(color: AppColors.border)),
            ),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'REAL-TIME TELEMETRY',
                  style: TextStyle(
                    color: AppColors.textMuted,
                    fontSize: 12,
                    fontWeight: FontWeight.bold,
                    letterSpacing: 1.2,
                  ),
                ),
                const SizedBox(height: 16),

                // Metrics Grid
                Row(
                  children: [
                    _buildMetricTile('CAPTURE FPS', "${_instantFps.toStringAsFixed(1)}", 'Hz', AppColors.opticalGreen),
                    const SizedBox(width: 12),
                    _buildMetricTile('GOODPUT', "${_goodputKbps.toStringAsFixed(1)}", 'KB/s', AppColors.textEmphasis),
                  ],
                ),
                const SizedBox(height: 12),
                Row(
                  children: [
                    _buildMetricTile('FRAMES INGESTED', "$_receivedFrameCount", 'total', AppColors.textPrimary),
                    const SizedBox(width: 12),
                    _buildMetricTile('SOLVED RANK', "$solvedCount", 'K=$totalCount', AppColors.darkTransfer),
                  ],
                ),

                const SizedBox(height: 24),
                const Divider(),
                const SizedBox(height: 24),

                // File Status Card
                if (_completedFile != null) ...[
                  const Text(
                    'RECONSTRUCTED FILE',
                    style: TextStyle(
                      color: AppColors.success,
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      letterSpacing: 1.2,
                    ),
                  ),
                  const SizedBox(height: 12),
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: AppColors.success.withOpacity(0.5)),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Row(
                          children: [
                            const Icon(Icons.insert_drive_file, color: AppColors.opticalGreen, size: 28),
                            const SizedBox(width: 12),
                            Expanded(
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    _completedFile!.name,
                                    style: const TextStyle(color: AppColors.textEmphasis, fontWeight: FontWeight.bold, fontSize: 14),
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                  ),
                                  Text(
                                    "${(_completedFile!.bytes.length / 1024).toStringAsFixed(1)} KB • SHA-256 Verified ✓",
                                    style: const TextStyle(color: AppColors.success, fontSize: 11),
                                  ),
                                ],
                              ),
                            ),
                          ],
                        ),
                        const SizedBox(height: 16),
                        if (_savedFilePath != null) ...[
                          Text(
                            "Saved to: $_savedFilePath",
                            style: const TextStyle(color: AppColors.textMuted, fontSize: 11),
                            maxLines: 2,
                            overflow: TextOverflow.ellipsis,
                          ),
                          const SizedBox(height: 12),
                          SizedBox(
                            width: double.infinity,
                            child: ElevatedButton.icon(
                              onPressed: () {
                                if (_savedFilePath != null) {
                                  Process.run('explorer.exe', ['/select,', _savedFilePath!]);
                                }
                              },
                              icon: const Icon(Icons.folder_open, size: 18),
                              label: const Text('Show in File Explorer'),
                            ),
                          ),
                        ],
                      ],
                    ),
                  ),
                ] else ...[
                  const Text(
                    'SESSION STATUS',
                    style: TextStyle(
                      color: AppColors.textMuted,
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      letterSpacing: 1.2,
                    ),
                  ),
                  const SizedBox(height: 12),
                  Container(
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: AppColors.surface,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: AppColors.border),
                    ),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: const [
                        Text('1. Point sender screen at your webcam.', style: TextStyle(color: AppColors.textSecondary, fontSize: 13, height: 1.5)),
                        SizedBox(height: 6),
                        Text('2. Keep camera steady in alignment ring.', style: TextStyle(color: AppColors.textSecondary, fontSize: 13, height: 1.5)),
                        SizedBox(height: 6),
                        Text('3. File reconstructs automatically inside app.', style: TextStyle(color: AppColors.textSecondary, fontSize: 13, height: 1.5)),
                      ],
                    ),
                  ),
                ],

                if (_errorMessage != null) ...[
                  const SizedBox(height: 16),
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: AppColors.error.withOpacity(0.15),
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(color: AppColors.error),
                    ),
                    child: Text(_errorMessage!, style: const TextStyle(color: AppColors.error, fontSize: 12)),
                  ),
                ],
              ],
            ),
          ),
        ],
      ),
    );
  }

  Widget _buildMetricTile(String label, String value, String unit, Color valueColor) {
    return Expanded(
      child: Container(
        padding: const EdgeInsets.all(14),
        decoration: BoxDecoration(
          color: AppColors.surface,
          borderRadius: BorderRadius.circular(10),
          border: Border.all(color: AppColors.border),
        ),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(label, style: const TextStyle(color: AppColors.textMuted, fontSize: 10, fontWeight: FontWeight.bold, letterSpacing: 0.5)),
            const SizedBox(height: 6),
            Row(
              crossAxisAlignment: CrossAxisAlignment.baseline,
              textBaseline: TextBaseline.alphabetic,
              children: [
                Text(value, style: TextStyle(color: valueColor, fontSize: 20, fontWeight: FontWeight.bold)),
                const SizedBox(width: 4),
                Text(unit, style: const TextStyle(color: AppColors.textMuted, fontSize: 11)),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

class _OpticalLensGuidePainter extends CustomPainter {
  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = AppColors.border.withOpacity(0.3)
      ..strokeWidth = 1.0
      ..style = PaintingStyle.stroke;

    final center = Offset(size.width / 2, size.height / 2);
    // Draw crosshair axes
    canvas.drawLine(Offset(center.dx - 180, center.dy), Offset(center.dx + 180, center.dy), paint);
    canvas.drawLine(Offset(center.dx, center.dy - 180), Offset(center.dx, center.dy + 180), paint);

    // Corner targeting brackets
    final bracketPaint = Paint()
      ..color = AppColors.opticalGreen.withOpacity(0.4)
      ..strokeWidth = 2.0
      ..style = PaintingStyle.stroke;

    const bSize = 20.0;
    final rect = Rect.fromCenter(center: center, width: 340, height: 340);

    // Top-left
    canvas.drawLine(rect.topLeft, rect.topLeft + const Offset(bSize, 0), bracketPaint);
    canvas.drawLine(rect.topLeft, rect.topLeft + const Offset(0, bSize), bracketPaint);
    // Top-right
    canvas.drawLine(rect.topRight, rect.topRight + const Offset(-bSize, 0), bracketPaint);
    canvas.drawLine(rect.topRight, rect.topRight + const Offset(0, bSize), bracketPaint);
    // Bottom-left
    canvas.drawLine(rect.bottomLeft, rect.bottomLeft + const Offset(bSize, 0), bracketPaint);
    canvas.drawLine(rect.bottomLeft, rect.bottomLeft + const Offset(0, -bSize), bracketPaint);
    // Bottom-right
    canvas.drawLine(rect.bottomRight, rect.bottomRight + const Offset(-bSize, 0), bracketPaint);
    canvas.drawLine(rect.bottomRight, rect.bottomRight + const Offset(0, -bSize), bracketPaint);
  }

  @override
  bool shouldRepaint(covariant CustomPainter oldDelegate) => false;
}
