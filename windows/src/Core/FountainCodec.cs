using System;
using System.Collections.Generic;

namespace OpticalTransfer.Core
{
    public enum DecoderResult
    {
        NeedMoreSymbols,
        SymbolIngested,
        DuplicateSymbol,
        Solved,
        CorruptedSymbol
    }

    public class DecoderStatus
    {
        public uint K { get; set; }
        public uint SolvedCount { get; set; }
        public uint ReceivedCount { get; set; }
        public float ProgressPercentage { get; set; }
        public bool IsComplete { get; set; }
        public uint SessionId { get; set; }
    }

    public class SystematicFountainEncoder
    {
        private readonly byte[] _payload;
        private readonly ushort _symbolSize;
        private readonly uint _k;
        private readonly uint _sessionId;
        private readonly double[] _cdf;
        private uint _currentSeq;

        public SystematicFountainEncoder(byte[] data, ushort targetSymbolSize = 300)
        {
            _payload = data ?? new byte[0];
            _symbolSize = targetSymbolSize;
            _currentSeq = 0;

            if (_payload.Length == 0)
            {
                _k = 1;
            }
            else
            {
                _k = (uint)((_payload.Length + _symbolSize - 1) / _symbolSize);
                if (_k == 0) _k = 1;
            }

            Random rnd = new Random();
            _sessionId = (uint)rnd.Next(1, 65535);
            _cdf = FountainMath.SolitonCdf(_k);
        }

        public uint K { get { return _k; } }
        public ushort SymbolSize { get { return _symbolSize; } }
        public uint SessionId { get { return _sessionId; } }
        public uint TotalBytes { get { return (uint)_payload.Length; } }
        public uint CurrentSeq { get { return _currentSeq; } }

        public byte[] NextFrame()
        {
            uint seq = _currentSeq++;
            byte[] blockData = new byte[_symbolSize];

            if (seq < _k)
            {
                // Systematic transmission: direct chunk of source payload
                int start = (int)seq * _symbolSize;
                int end = Math.Min(start + _symbolSize, _payload.Length);
                if (start < _payload.Length)
                {
                    Buffer.BlockCopy(_payload, start, blockData, 0, end - start);
                }
            }
            else
            {
                // Repair transmission: Soliton-selected linear combination (XOR)
                uint[] indices = FountainMath.FrameIndices(_k, _cdf, _sessionId, seq);
                foreach (uint idx in indices)
                {
                    int start = (int)idx * _symbolSize;
                    int end = Math.Min(start + _symbolSize, _payload.Length);
                    if (start < _payload.Length)
                    {
                        byte[] temp = new byte[_symbolSize];
                        Buffer.BlockCopy(_payload, start, temp, 0, end - start);
                        FountainMath.XorBlocks(blockData, temp, end - start);
                    }
                }
            }

            OtiFrameHeader header = new OtiFrameHeader
            {
                Magic = ProtocolDcf2.OTI_MAGIC,
                SessionId = (ushort)(_sessionId & 0xFFFF),
                TotalPayloadBytes = (uint)_payload.Length,
                SymbolSizeBytes = _symbolSize,
                TotalSourceSymbols = (ushort)(_k & 0xFFFF),
                Seq = (ushort)(seq & 0xFFFF),
                HeaderCrc16 = 0
            };

            return ProtocolDcf2.PackFrame(header, blockData);
        }
    }

    public class IncrementalPeelingDecoder
    {
        private class PendingEquation
        {
            public HashSet<uint> Indices = new HashSet<uint>();
            public byte[] Data;
        }

        private uint _sessionId;
        private uint _k;
        private ushort _symbolSize;
        private uint _totalPayloadBytes;
        private double[] _cdf;

        private readonly Dictionary<uint, byte[]> _solvedBlocks = new Dictionary<uint, byte[]>();
        private readonly Dictionary<uint, List<PendingEquation>> _byBlockMap = new Dictionary<uint, List<PendingEquation>>();
        private readonly HashSet<uint> _seenSeqs = new HashSet<uint>();
        private uint _receivedFramesCount;

        public IncrementalPeelingDecoder()
        {
            Reset();
        }

        public void Reset()
        {
            _sessionId = 0;
            _k = 0;
            _symbolSize = 0;
            _totalPayloadBytes = 0;
            _cdf = null;
            _solvedBlocks.Clear();
            _byBlockMap.Clear();
            _seenSeqs.Clear();
            _receivedFramesCount = 0;
        }

        public bool IsComplete
        {
            get { return _k > 0 && _solvedBlocks.Count >= _k; }
        }

        public DecoderResult ConsumeFrame(byte[] frameBytes)
        {
            OtiFrameHeader header;
            byte[] blockData;
            if (!ProtocolDcf2.ParseFrame(frameBytes, out header, out blockData))
            {
                return DecoderResult.CorruptedSymbol;
            }

            // Reset if session salt changes
            if (_sessionId != 0 && (header.SessionId != _sessionId || header.TotalSourceSymbols != _k || header.TotalPayloadBytes != _totalPayloadBytes))
            {
                Reset();
            }

            if (_sessionId == 0)
            {
                _sessionId = header.SessionId;
                _k = header.TotalSourceSymbols;
                _symbolSize = header.SymbolSizeBytes;
                _totalPayloadBytes = header.TotalPayloadBytes;
                _cdf = FountainMath.SolitonCdf(_k);
            }

            if (_seenSeqs.Contains(header.Seq))
            {
                return DecoderResult.DuplicateSymbol;
            }
            _seenSeqs.Add(header.Seq);
            _receivedFramesCount++;

            if (IsComplete)
            {
                return DecoderResult.Solved;
            }

            HashSet<uint> indices = new HashSet<uint>();
            if (header.Seq < _k)
            {
                indices.Add(header.Seq);
            }
            else
            {
                uint[] indVec = FountainMath.FrameIndices(_k, _cdf, _sessionId, header.Seq);
                foreach (uint u in indVec) indices.Add(u);
            }

            byte[] currentPayload = new byte[_symbolSize];
            Buffer.BlockCopy(blockData, 0, currentPayload, 0, _symbolSize);

            // Eliminate already solved blocks
            List<uint> toRemove = new List<uint>();
            foreach (uint b in indices)
            {
                if (_solvedBlocks.ContainsKey(b))
                {
                    FountainMath.XorBlocks(currentPayload, _solvedBlocks[b], _symbolSize);
                    toRemove.Add(b);
                }
            }
            foreach (uint b in toRemove)
            {
                indices.Remove(b);
            }

            if (indices.Count == 0)
            {
                return IsComplete ? DecoderResult.Solved : DecoderResult.SymbolIngested;
            }

            if (indices.Count == 1)
            {
                uint solvedIdx = 0;
                foreach (uint idx in indices) { solvedIdx = idx; break; }
                ResolveBlock(solvedIdx, currentPayload);
            }
            else
            {
                PendingEquation eq = new PendingEquation
                {
                    Indices = indices,
                    Data = currentPayload
                };

                foreach (uint b in eq.Indices)
                {
                    if (!_byBlockMap.ContainsKey(b))
                    {
                        _byBlockMap[b] = new List<PendingEquation>();
                    }
                    _byBlockMap[b].Add(eq);
                }
            }

            return IsComplete ? DecoderResult.Solved : DecoderResult.SymbolIngested;
        }

        private void ResolveBlock(uint b0, byte[] w0)
        {
            Queue<KeyValuePair<uint, byte[]>> queue = new Queue<KeyValuePair<uint, byte[]>>();
            queue.Enqueue(new KeyValuePair<uint, byte[]>(b0, w0));

            while (queue.Count > 0)
            {
                KeyValuePair<uint, byte[]> item = queue.Dequeue();
                uint b = item.Key;
                byte[] w = item.Value;

                if (_solvedBlocks.ContainsKey(b)) continue;
                _solvedBlocks[b] = w;

                if (!_byBlockMap.ContainsKey(b)) continue;

                List<PendingEquation> waitingList = _byBlockMap[b];
                _byBlockMap.Remove(b);

                foreach (PendingEquation eq in waitingList)
                {
                    if (eq.Indices.Contains(b))
                    {
                        FountainMath.XorBlocks(eq.Data, w, _symbolSize);
                        eq.Indices.Remove(b);

                        if (eq.Indices.Count == 1)
                        {
                            uint resolvedIdx = 0;
                            foreach (uint idx in eq.Indices) { resolvedIdx = idx; break; }
                            if (!_solvedBlocks.ContainsKey(resolvedIdx))
                            {
                                byte[] copyData = new byte[_symbolSize];
                                Buffer.BlockCopy(eq.Data, 0, copyData, 0, _symbolSize);
                                queue.Enqueue(new KeyValuePair<uint, byte[]>(resolvedIdx, copyData));
                            }
                        }
                    }
                }
            }
        }

        public DecoderStatus GetStatus()
        {
            return new DecoderStatus
            {
                K = _k,
                SolvedCount = (uint)_solvedBlocks.Count,
                ReceivedCount = _receivedFramesCount,
                ProgressPercentage = (_k > 0) ? ((float)_solvedBlocks.Count / (float)_k * 100.0f) : 0.0f,
                IsComplete = IsComplete,
                SessionId = _sessionId
            };
        }

        public byte[] AssemblePayload()
        {
            if (!IsComplete) return null;

            byte[] payload = new byte[_totalPayloadBytes];
            for (uint b = 0; b < _k; ++b)
            {
                if (!_solvedBlocks.ContainsKey(b)) return null;

                int start = (int)b * _symbolSize;
                int len = Math.Min((int)_symbolSize, (int)_totalPayloadBytes - start);
                if (len > 0)
                {
                    Buffer.BlockCopy(_solvedBlocks[b], 0, payload, start, len);
                }
            }
            return payload;
        }
    }
}
