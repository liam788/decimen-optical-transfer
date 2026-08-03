import 'dart:typed_data';
import 'dart:math' as math;
import 'protocol.dart';

/// Deterministic IEEE 754 float64 natural logarithm matching fountain.ts
double ieeeLog(double x) {
  if (x <= 0) return double.negativeInfinity;
  final bd = ByteData(8);
  bd.setFloat64(0, x, Endian.big);
  final bits = bd.getUint64(0, Endian.big);
  int exp = (bits >> 52) & 0x7FF;
  int mantissa = bits & 0x000FFFFFFFFFFFFF;

  if (exp == 0) {
    exp = -1022;
  } else {
    exp -= 1023;
    mantissa |= 0x0010000000000000;
  }

  double m = mantissa / 4503599627370496.0;
  double f = m - 1.0;
  double s = f / (2.0 + f);
  double z = s * s;
  double w = z * z;

  double num = s * (2.0 + z * (0.6666666666666666 + z * (0.4000000000000000 + z * (0.2857142857142857 + w * 0.2222222222222222))));
  return exp * 0.6931471805599453 + num;
}

/// LCG PRNG matching fountain.ts
class Lcg {
  int state;
  Lcg(this.state);

  int nextUint32() {
    state = ((state * 1664525) + 1013904223) & 0xFFFFFFFF;
    return state;
  }

  double nextFloat() {
    return nextUint32() / 4294967296.0;
  }
}

List<double> buildRobustSolitonCdf(int k, {double c = 0.1, double delta = 0.5}) {
  if (k <= 0) throw Exception("k must be > 0");

  final pdf = List<double>.filled(k + 1, 0.0);
  pdf[1] = 1.0 / k;
  for (int d = 2; d <= k; d++) {
    pdf[d] = 1.0 / (d * (d - 1));
  }

  final R = c * ieeeLog(k / delta) * math.sqrt(k);
  final pivot = (k / R).floor();

  for (int d = 1; d <= k; d++) {
    double tau = 0.0;
    if (d < pivot && d >= 1) {
      tau = R / (d * k);
    } else if (d == pivot) {
      tau = (R * ieeeLog(R / delta)) / k;
    }
    pdf[d] += tau;
  }

  double sum = 0.0;
  for (int d = 1; d <= k; d++) {
    sum += pdf[d];
  }
  for (int d = 1; d <= k; d++) {
    pdf[d] /= sum;
  }

  final cdf = List<double>.filled(k + 1, 0.0);
  double cumulative = 0.0;
  for (int d = 1; d <= k; d++) {
    cumulative += pdf[d];
    cdf[d] = cumulative;
  }
  cdf[k] = 1.0;
  return cdf;
}

int sampleDegree(Lcg prng, List<double> cdf) {
  final u = prng.nextFloat();
  int low = 1;
  int high = cdf.length - 1;
  while (low < high) {
    final mid = (low + high) >> 1;
    if (cdf[mid] >= u) {
      high = mid;
    } else {
      low = mid + 1;
    }
  }
  return low;
}

List<int> sampleBlockIndices(Lcg prng, int k, int degree) {
  final indices = <int>[];
  final pool = List<int>.generate(k, (i) => i);
  for (int i = 0; i < degree; i++) {
    final pickIndex = (prng.nextFloat() * (k - i)).floor();
    indices.add(pool[pickIndex]);
    pool[pickIndex] = pool[k - 1 - i];
  }
  return indices;
}

class FrameData {
  final int sessionId;
  final int seq;
  final int k;
  final int blockLen;
  final int totalLen;
  final int payloadFnv;
  final Uint8List payload;

  FrameData({
    required this.sessionId,
    required this.seq,
    required this.k,
    required this.blockLen,
    required this.totalLen,
    required this.payloadFnv,
    required this.payload,
  });

  Uint8List toBytes() {
    final out = Uint8List(headerLen + payload.length);
    final bd = ByteData.sublistView(out);
    out[0] = magic0;
    out[1] = magic1;
    bd.setUint16(2, sessionId, Endian.little);
    bd.setUint32(4, seq, Endian.little);
    bd.setUint16(8, k, Endian.little);
    bd.setUint16(10, blockLen, Endian.little);
    bd.setUint32(12, totalLen, Endian.little);
    bd.setUint32(16, payloadFnv, Endian.little);
    out.setRange(headerLen, out.length, payload);
    return out;
  }

  static FrameData? parse(Uint8List bytes) {
    if (bytes.length < headerLen) return null;
    if (bytes[0] != magic0 || bytes[1] != magic1) return null;

    final bd = ByteData.sublistView(bytes);
    final sessionId = bd.getUint16(2, Endian.little);
    final seq = bd.getUint32(4, Endian.little);
    final k = bd.getUint16(8, Endian.little);
    final blockLen = bd.getUint16(10, Endian.little);
    final totalLen = bd.getUint32(12, Endian.little);
    final payloadFnv = bd.getUint32(16, Endian.little);

    if (bytes.length < headerLen + blockLen) return null;
    final payload = bytes.sublist(headerLen, headerLen + blockLen);

    return FrameData(
      sessionId: sessionId,
      seq: seq,
      k: k,
      blockLen: blockLen,
      totalLen: totalLen,
      payloadFnv: payloadFnv,
      payload: payload,
    );
  }
}

/// Fountain Luby Transform Encoder
class FountainEncoder {
  final Uint8List data;
  final int blockLen;
  final int k;
  final int sessionId;
  final List<double> cdf;
  final int payloadFnv;
  int _seq = 0;

  FountainEncoder._({
    required this.data,
    required this.blockLen,
    required this.k,
    required this.sessionId,
    required this.cdf,
    required this.payloadFnv,
  });

  factory FountainEncoder(Uint8List data, {int targetBlockLen = 400}) {
    final totalLen = data.length;
    final k = (totalLen / targetBlockLen).ceil().clamp(1, 65535);
    final actualBlockLen = (totalLen / k).ceil();

    final cdf = buildRobustSolitonCdf(k);
    final sessionId = math.Random().nextInt(65535);
    final fnv = fnv1a(data);

    return FountainEncoder._(
      data: data,
      blockLen: actualBlockLen,
      k: k,
      sessionId: sessionId,
      cdf: cdf,
      payloadFnv: fnv,
    );
  }

  FrameData nextFrame() {
    _seq++;
    final prng = Lcg(_seq);
    final degree = sampleDegree(prng, cdf);
    final indices = sampleBlockIndices(prng, k, degree);

    final payload = Uint8List(blockLen);
    for (final idx in indices) {
      final start = idx * blockLen;
      final end = math.min(start + blockLen, data.length);
      for (int i = 0; i < end - start; i++) {
        payload[i] ^= data[start + i];
      }
    }

    return FrameData(
      sessionId: sessionId,
      seq: _seq,
      k: k,
      blockLen: blockLen,
      totalLen: data.length,
      payloadFnv: payloadFnv,
      payload: payload,
    );
  }
}

/// Fountain Luby Transform Peeling Decoder
class FountainDecoder {
  int? sessionId;
  int k = 0;
  int blockLen = 0;
  int totalLen = 0;
  int expectedFnv = 0;
  List<double>? cdf;

  final Map<int, Uint8List> solvedBlocks = {};
  final Set<int> receivedSeqs = {};
  int receivedFramesCount = 0;

  void reset() {
    sessionId = null;
    k = 0;
    blockLen = 0;
    totalLen = 0;
    expectedFnv = 0;
    cdf = null;
    solvedBlocks.clear();
    receivedSeqs.clear();
    receivedFramesCount = 0;
  }

  bool addFrame(FrameData frame) {
    if (sessionId != null && frame.sessionId != sessionId) {
      reset(); // New session started
    }

    if (sessionId == null) {
      sessionId = frame.sessionId;
      k = frame.k;
      blockLen = frame.blockLen;
      totalLen = frame.totalLen;
      expectedFnv = frame.payloadFnv;
      cdf = buildRobustSolitonCdf(k);
    }

    if (receivedSeqs.contains(frame.seq)) return false;
    receivedSeqs.add(frame.seq);
    receivedFramesCount++;

    final prng = Lcg(frame.seq);
    final degree = sampleDegree(prng, cdf!);
    final indices = sampleBlockIndices(prng, k, degree);

    Uint8List currentPayload = Uint8List.fromList(frame.payload);
    final unsolvedIndices = <int>[];

    for (final idx in indices) {
      if (solvedBlocks.containsKey(idx)) {
        final solved = solvedBlocks[idx]!;
        for (int i = 0; i < blockLen; i++) {
          currentPayload[i] ^= solved[i];
        }
      } else {
        unsolvedIndices.add(idx);
      }
    }

    if (unsolvedIndices.length == 1) {
      final newSolvedIdx = unsolvedIndices.first;
      solvedBlocks[newSolvedIdx] = currentPayload;
    }

    return solvedBlocks.length == k;
  }

  double get progress => k == 0 ? 0.0 : (solvedBlocks.length / k).clamp(0.0, 1.0);
  bool get isComplete => k > 0 && solvedBlocks.length == k;

  Uint8List? reconstruct() {
    if (!isComplete) return null;
    final out = Uint8List(totalLen);
    for (int i = 0; i < k; i++) {
      final block = solvedBlocks[i]!;
      final start = i * blockLen;
      final count = math.min(blockLen, totalLen - start);
      out.setRange(start, start + count, block.sublist(0, count));
    }

    if (fnv1a(out) != expectedFnv) {
      return null;
    }

    return out;
  }
}
