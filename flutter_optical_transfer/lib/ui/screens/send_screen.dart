import 'dart:async';
import 'dart:math' as math;
import 'dart:typed_data';
import 'package:flutter/material.dart';
import '../../core/fountain.dart';
import '../../core/protocol.dart';
import '../../sender/adaptive_tuner.dart';
import '../../sender/multi_qr_grid.dart';
import '../theme/app_theme.dart';

class SendScreen extends StatefulWidget {
  final String fileName;
  final String fileType;
  final Uint8List fileBytes;

  const SendScreen({
    super.key,
    required this.fileName,
    required this.fileType,
    required this.fileBytes,
  });

  @override
  State<SendScreen> createState() => _SendScreenState();
}

class _SendScreenState extends State<SendScreen> {
  FountainEncoder? _encoder;
  PackedOpticalFile? _packedFile;
  late AdaptiveSettings _settings;
  Timer? _streamTimer;
  List<Uint8List> _currentFrameChunks = [];
  int _frameIndex = 0;
  bool _initializedSettings = false;
  String? _errorMessage;

  @override
  void initState() {
    super.initState();
    try {
      _packedFile = packFile(widget.fileName, widget.fileType, widget.fileBytes);
      _encoder = FountainEncoder(_packedFile!.container, targetBlockLen: 300);
      _settings = AdaptiveSettings(targetFps: 30, blockLen: 300, qrGridDimension: 1);
      _startStreaming();
    } catch (e) {
      _errorMessage = e.toString().replaceAll('Exception: ', '');
    }
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (!_initializedSettings && _errorMessage == null) {
      _settings = AdaptiveSettings.autoTune(context);
      _initializedSettings = true;
    }
  }

  void _startStreaming() {
    if (_encoder == null) return;
    _streamTimer?.cancel();
    final interval = (1000 / _settings.targetFps).round();
    _streamTimer = Timer.periodic(Duration(milliseconds: interval), (_) {
      if (_encoder == null) return;
      try {
        final chunks = <Uint8List>[];
        final count = _settings.qrGridDimension * _settings.qrGridDimension;
        for (int i = 0; i < count; i++) {
          chunks.add(_encoder!.nextFrame().toBytes());
        }
        if (mounted) {
          setState(() {
            _currentFrameChunks = chunks;
            _frameIndex++;
          });
        }
      } catch (e) {
        // Frame generation error handling
      }
    });
  }

  @override
  void dispose() {
    _streamTimer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    if (_errorMessage != null) {
      return Scaffold(
        backgroundColor: AppColors.opticalBlack,
        appBar: AppBar(
          title: const Text("Error Preparing File"),
          backgroundColor: AppColors.secondaryBackground,
        ),
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(32.0),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Icon(Icons.error_outline, color: AppColors.error, size: 64),
                const SizedBox(height: 16),
                Text(
                  _errorMessage!,
                  style: const TextStyle(color: AppColors.textEmphasis, fontSize: 18),
                  textAlign: TextAlign.center,
                ),
                const SizedBox(height: 24),
                ElevatedButton.icon(
                  onPressed: () => Navigator.pop(context),
                  icon: const Icon(Icons.arrow_back),
                  label: const Text("Go Back & Select Another File"),
                ),
              ],
            ),
          ),
        ),
      );
    }

    return Scaffold(
      backgroundColor: AppColors.opticalBlack,
      appBar: AppBar(
        title: Text("Streaming: ${widget.fileName}"),
        backgroundColor: AppColors.secondaryBackground,
      ),
      body: SafeArea(
        child: Column(
          children: [
            const SizedBox(height: 12),

            // Stream Info Card
            Container(
              margin: const EdgeInsets.symmetric(horizontal: 20),
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: AppColors.surface,
                borderRadius: BorderRadius.circular(12),
                border: Border.all(color: AppColors.border),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    "Size: ${((_packedFile?.transmittedSize ?? 0) / 1024).toStringAsFixed(1)} KB",
                    style: const TextStyle(color: AppColors.textSecondary),
                  ),
                  Text(
                    "Frames Sent: $_frameIndex",
                    style: const TextStyle(color: AppColors.opticalGreen, fontWeight: FontWeight.bold),
                  ),
                ],
              ),
            ),

            // Multi-QR Code Visual Stream Area — Perfectly bounded to fit screen
            Expanded(
              child: Padding(
                padding: const EdgeInsets.all(16.0),
                child: LayoutBuilder(
                  builder: (context, constraints) {
                    final maxAvailableSide = math.min(constraints.maxWidth, constraints.maxHeight);
                    final qrSquareSize = math.max(100.0, maxAvailableSide);
                    return Center(
                      child: Container(
                        padding: const EdgeInsets.all(16),
                        decoration: BoxDecoration(
                          color: AppColors.surface,
                          borderRadius: BorderRadius.circular(16),
                          border: Border.all(color: AppColors.border),
                        ),
                        child: SizedBox(
                          width: qrSquareSize - 32,
                          height: qrSquareSize - 32,
                          child: MultiQrGridWidget(
                            frameChunks: _currentFrameChunks,
                            settings: _settings,
                          ),
                        ),
                      ),
                    );
                  },
                ),
              ),
            ),

            // Controls Panel (Tuning A, B, F)
            Container(
              padding: const EdgeInsets.all(16),
              decoration: const BoxDecoration(
                color: AppColors.secondaryBackground,
                border: Border(top: BorderSide(color: AppColors.border)),
              ),
              child: Column(
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceAround,
                    children: [
                      // Grid Dimension Switcher (1x1, 2x2, 3x3)
                      DropdownButton<int>(
                        value: _settings.qrGridDimension,
                        dropdownColor: AppColors.secondaryBackground,
                        style: const TextStyle(color: AppColors.textPrimary),
                        items: const [
                          DropdownMenuItem(value: 1, child: Text("1x1 (1 QR)")),
                          DropdownMenuItem(value: 2, child: Text("2x2 Grid (4 QR)")),
                          DropdownMenuItem(value: 3, child: Text("3x3 Grid (9 QR)")),
                        ],
                        onChanged: (val) {
                          if (val != null) {
                            setState(() {
                              _settings.qrGridDimension = val;
                            });
                            _startStreaming();
                          }
                        },
                      ),

                      // FPS Target Switcher
                      DropdownButton<int>(
                        value: _settings.targetFps,
                        dropdownColor: AppColors.secondaryBackground,
                        style: const TextStyle(color: AppColors.textPrimary),
                        items: const [
                          DropdownMenuItem(value: 15, child: Text("15 FPS (Safe)")),
                          DropdownMenuItem(value: 30, child: Text("30 FPS (Standard)")),
                          DropdownMenuItem(value: 60, child: Text("60 FPS (Fast)")),
                        ],
                        onChanged: (val) {
                          if (val != null) {
                            setState(() {
                              _settings.targetFps = val;
                            });
                            _startStreaming();
                          }
                        },
                      ),

                      // Color Codec Toggle (Feature F)
                      Row(
                        children: [
                          const Text("Color", style: TextStyle(color: AppColors.textSecondary, fontSize: 12)),
                          Switch(
                            value: _settings.useColorCodec,
                            activeColor: AppColors.opticalGreen,
                            onChanged: (val) {
                              setState(() {
                                _settings.useColorCodec = val;
                              });
                            },
                          ),
                        ],
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}



