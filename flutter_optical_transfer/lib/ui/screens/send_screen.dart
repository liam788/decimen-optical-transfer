import 'dart:async';
import 'dart:math' as math;
import 'dart:typed_data';
import 'package:flutter/material.dart';
import '../../core/fountain.dart';
import '../../core/protocol.dart';
import '../../sender/adaptive_tuner.dart';
import '../../sender/multi_qr_grid.dart';

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
  late FountainEncoder _encoder;
  late PackedOpticalFile _packedFile;
  late AdaptiveSettings _settings;
  Timer? _streamTimer;
  List<Uint8List> _currentFrameChunks = [];
  int _frameIndex = 0;
  bool _initializedSettings = false;

  @override
  void initState() {
    super.initState();
    _packedFile = packFile(widget.fileName, widget.fileType, widget.fileBytes);
    _encoder = FountainEncoder(_packedFile.container, targetBlockLen: 300);
    _settings = AdaptiveSettings(targetFps: 30, blockLen: 300, qrGridDimension: 1);
    _startStreaming();
  }

  @override
  void didChangeDependencies() {
    super.didChangeDependencies();
    if (!_initializedSettings) {
      _settings = AdaptiveSettings.autoTune(context);
      _initializedSettings = true;
    }
  }

  void _startStreaming() {
    _streamTimer?.cancel();
    final interval = (1000 / _settings.targetFps).round();
    _streamTimer = Timer.periodic(Duration(milliseconds: interval), (_) {
      final chunks = <Uint8List>[];
      final count = _settings.qrGridDimension * _settings.qrGridDimension;
      for (int i = 0; i < count; i++) {
        chunks.add(_encoder.nextFrame().toBytes());
      }
      setState(() {
        _currentFrameChunks = chunks;
        _frameIndex++;
      });
    });
  }

  @override
  void dispose() {
    _streamTimer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: const Color(0xFF0F172A),
      appBar: AppBar(
        title: Text("Streaming: ${widget.fileName}"),
        backgroundColor: const Color(0xFF1E293B),
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
                color: const Color(0xFF1E293B),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Row(
                mainAxisAlignment: MainAxisAlignment.spaceBetween,
                children: [
                  Text(
                    "Size: ${(_packedFile.transmittedSize / 1024).toStringAsFixed(1)} KB",
                    style: const TextStyle(color: Colors.white70),
                  ),
                  Text(
                    "Frames Sent: $_frameIndex",
                    style: const TextStyle(color: Colors.cyanAccent, fontWeight: FontWeight.bold),
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
                      child: SizedBox(
                        width: qrSquareSize,
                        height: qrSquareSize,
                        child: MultiQrGridWidget(
                          frameChunks: _currentFrameChunks,
                          settings: _settings,
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
              color: const Color(0xFF1E293B),
              child: Column(
                children: [
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceAround,
                    children: [
                      // Grid Dimension Switcher (1x1, 2x2, 3x3)
                      DropdownButton<int>(
                        value: _settings.qrGridDimension,
                        dropdownColor: const Color(0xFF1E293B),
                        style: const TextStyle(color: Colors.white),
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
                        dropdownColor: const Color(0xFF1E293B),
                        style: const TextStyle(color: Colors.white),
                        items: const [
                          DropdownMenuItem(value: 15, child: Text("15 FPS")),
                          DropdownMenuItem(value: 30, child: Text("30 FPS")),
                          DropdownMenuItem(value: 60, child: Text("60 FPS")),
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
                          const Text("Color", style: TextStyle(color: Colors.white, fontSize: 12)),
                          Switch(
                            value: _settings.useColorCodec,
                            activeColor: Colors.cyanAccent,
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

