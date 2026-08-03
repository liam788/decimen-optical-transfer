import 'dart:typed_data';
import 'package:flutter_test/flutter_test.dart';
import 'package:flutter_optical_transfer/core/protocol.dart';
import 'package:flutter_optical_transfer/core/fountain.dart';

void main() {
  group('Protocol Wire Format Tests', () {
    test('FNV-1a hash calculation', () {
      final testData = Uint8List.fromList([0x44, 0x43, 0x46, 0x32]);
      final hash = fnv1a(testData);
      expect(hash, isA<int>());
    });

    test('Pack and Unpack File Integrity', () {
      final sampleBytes = Uint8List.fromList(List.generate(1024, (i) => i % 256));
      final packed = packFile('test_file.txt', 'text/plain', sampleBytes);
      expect(packed.container.isNotEmpty, true);

      final unpacked = unpackFile(packed.container);
      expect(unpacked.name, 'test_file.txt');
      expect(unpacked.bytes, sampleBytes);
    });
  });

  group('Fountain Encoder & Decoder Tests', () {
    test('Encode and Decode Stream Peeling', () {
      final data = Uint8List.fromList(List.generate(4000, (i) => (i * 17) % 256));
      final encoder = FountainEncoder(data, targetBlockLen: 200);
      final decoder = FountainDecoder();

      int framesSent = 0;
      bool completed = false;

      while (!completed && framesSent < 500) {
        final frame = encoder.nextFrame();
        completed = decoder.addFrame(frame);
        framesSent++;
      }

      expect(completed, true);
      final reconstructed = decoder.reconstruct();
      expect(reconstructed, isNotNull);
      expect(reconstructed, data);
    });
  });
}
