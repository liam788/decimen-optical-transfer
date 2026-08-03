import 'dart:typed_data';
import 'dart:convert';
import 'package:crypto/crypto.dart';
import 'package:archive/archive.dart';

const int headerLen = 20;
const int maxFileBytes = 1024 * 1024 * 1024; // 1 GB max (1024 MB)
const int fileHeaderLen = 49;
const int magic0 = 0xD1;
const int magic1 = 0x0C;
final Uint8List fileMagic = Uint8List.fromList([0x44, 0x43, 0x46, 0x32]); // "DCF2"

enum CompressionMode { none, gzip }

class PackedOpticalFile {
  final Uint8List container;
  final CompressionMode compression;
  final int originalSize;
  final int transmittedSize;

  PackedOpticalFile({
    required this.container,
    required this.compression,
    required this.originalSize,
    required this.transmittedSize,
  });
}

class OpticalFile {
  final String name;
  final String type;
  final Uint8List bytes;
  final Uint8List sha256;
  final CompressionMode compression;
  final int transmittedSize;

  OpticalFile({
    required this.name,
    required this.type,
    required this.bytes,
    required this.sha256,
    required this.compression,
    required this.transmittedSize,
  });
}

/// Calculate FNV-1a hash matching protocol.ts
int fnv1a(Uint8List bytes) {
  int hash = 0x811c9dc5;
  for (int i = 0; i < bytes.length; i++) {
    hash ^= bytes[i];
    // 32-bit integer multiplication 0x01000193 = 16777619
    hash = (hash * 16777619) & 0xFFFFFFFF;
  }
  return hash;
}

/// Calculate SHA-256 hash
Uint8List calculateSha256(Uint8List bytes) {
  return Uint8List.fromList(sha256.convert(bytes).bytes);
}

/// Sanitize file name for safe filesystem handling
String safeFileName(String name) {
  final base = name.split(RegExp(r'[/\\]')).last;
  final cleaned = base.replaceAll(RegExp(r'[\u0000-\u001f\u007f]'), '').trim();
  if (cleaned.isEmpty || cleaned == '.' || cleaned == '..') {
    return 'transfer.bin';
  }
  return cleaned;
}

/// Pack file into DCF2 container format
PackedOpticalFile packFile(String name, String type, Uint8List bytes) {
  if (bytes.isEmpty) throw Exception('Choose a non-empty file.');
  if (bytes.length > maxFileBytes) {
    throw Exception('Files are limited to 1 GB.');
  }

  final nameBytes = utf8.encode(safeFileName(name));
  final typeBytes = utf8.encode(type.isEmpty ? 'application/octet-stream' : type);
  
  if (nameBytes.length > 0xFFFF || typeBytes.length > 0xFFFF) {
    throw Exception('The file name or media type is too long.');
  }

  // Attempt GZIP compression if data is >= 768 bytes
  bool useGzip = false;
  Uint8List transmitted = bytes;
  CompressionMode mode = CompressionMode.none;

  if (bytes.length >= 768) {
    try {
      final compressed = Uint8List.fromList(GZipEncoder().encode(bytes)!);
      if (compressed.length + 64 < bytes.length) {
        transmitted = compressed;
        useGzip = true;
        mode = CompressionMode.gzip;
      }
    } catch (_) {
      transmitted = bytes;
    }
  }

  final sha = calculateSha256(bytes);
  final out = Uint8List(fileHeaderLen + nameBytes.length + typeBytes.length + transmitted.length);
  final bd = ByteData.sublistView(out);

  out.setRange(0, 4, fileMagic);
  bd.setUint8(4, useGzip ? 1 : 0);
  bd.setUint16(5, nameBytes.length, Endian.little);
  bd.setUint16(7, typeBytes.length, Endian.little);
  bd.setUint32(9, bytes.length, Endian.little);
  bd.setUint32(13, transmitted.length, Endian.little);
  out.setRange(17, 49, sha);

  int offset = fileHeaderLen;
  out.setRange(offset, offset + nameBytes.length, nameBytes);
  offset += nameBytes.length;

  out.setRange(offset, offset + typeBytes.length, typeBytes);
  offset += typeBytes.length;

  out.setRange(offset, offset + transmitted.length, transmitted);

  return PackedOpticalFile(
    container: out,
    compression: mode,
    originalSize: bytes.length,
    transmittedSize: transmitted.length,
  );
}

/// Unpack DCF2 container payload
OpticalFile unpackFile(Uint8List container) {
  if (container.length < fileHeaderLen) {
    throw Exception('Container payload is too small.');
  }

  for (int i = 0; i < 4; i++) {
    if (container[i] != fileMagic[i]) {
      throw Exception('Invalid file container header.');
    }
  }

  final bd = ByteData.sublistView(container);
  final useGzip = bd.getUint8(4) != 0;
  final nameLen = bd.getUint16(5, Endian.little);
  final typeLen = bd.getUint16(7, Endian.little);
  final declOriginalSize = bd.getUint32(9, Endian.little);
  final declTransmittedSize = bd.getUint32(13, Endian.little);

  final sha256Expected = container.sublist(17, 49);

  int offset = fileHeaderLen;
  if (offset + nameLen + typeLen + declTransmittedSize > container.length) {
    throw Exception('Container truncated or corrupted.');
  }

  final nameBytes = container.sublist(offset, offset + nameLen);
  offset += nameLen;

  final typeBytes = container.sublist(offset, offset + typeLen);
  offset += typeLen;

  final payloadBytes = container.sublist(offset, offset + declTransmittedSize);

  final name = safeFileName(utf8.decode(nameBytes, allowMalformed: true));
  final type = utf8.decode(typeBytes, allowMalformed: true);

  Uint8List decompressed;
  if (useGzip) {
    decompressed = Uint8List.fromList(GZipDecoder().decodeBytes(payloadBytes));
  } else {
    decompressed = payloadBytes;
  }

  if (decompressed.length != declOriginalSize) {
    throw Exception('Decompressed length mismatch.');
  }

  final actualSha = calculateSha256(decompressed);
  for (int i = 0; i < 32; i++) {
    if (actualSha[i] != sha256Expected[i]) {
      throw Exception('SHA-256 integrity check failed!');
    }
  }

  return OpticalFile(
    name: name,
    type: type,
    bytes: decompressed,
    sha256: actualSha,
    compression: useGzip ? CompressionMode.gzip : CompressionMode.none,
    transmittedSize: payloadBytes.length,
  );
}
