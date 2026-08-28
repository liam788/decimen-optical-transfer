using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace OpticalTransfer.Core
{
    public struct OtiFrameHeader
    {
        public ushort Magic;                // 0x4F54 ('OT')
        public ushort SessionId;            // Random session salt
        public uint TotalPayloadBytes;      // Total DCF2 container size
        public ushort SymbolSizeBytes;      // e.g. 300 bytes
        public ushort TotalSourceSymbols;   // K
        public ushort Seq;                  // ESI (0..K-1: systematic, >=K: repair)
        public ushort HeaderCrc16;          // CRC16 of first 14 bytes
    }

    public class OpticalFile
    {
        public string Name { get; set; }
        public string MimeType { get; set; }
        public byte[] Data { get; set; }
        public byte[] Sha256 { get; set; }
        public bool IsGzipped { get; set; }
        public uint OriginalSize { get; set; }
    }

    public static class ProtocolDcf2
    {
        public const ushort OTI_MAGIC = 0x544F; // 'OT' little-endian
        public const int OTI_HEADER_LEN = 16;
        public static readonly byte[] DCF2_MAGIC = new byte[] { 0x44, 0x43, 0x46, 0x32 }; // 'DCF2'
        public const int DCF2_HEADER_LEN = 49;

        public static ushort Crc16(byte[] data, int offset, int len)
        {
            ushort crc = 0xFFFF;
            for (int i = 0; i < len; ++i)
            {
                crc ^= (ushort)(data[offset + i] << 8);
                for (int j = 0; j < 8; ++j)
                {
                    if ((crc & 0x8000) != 0)
                    {
                        crc = (ushort)((crc << 1) ^ 0x1021);
                    }
                    else
                    {
                        crc <<= 1;
                    }
                }
            }
            return crc;
        }

        public static byte[] ComputeSha256(byte[] data, int offset, int len)
        {
            using (SHA256 sha = SHA256.Create())
            {
                return sha.ComputeHash(data, offset, len);
            }
        }

        public static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "transfer.bin";
            string baseName = Path.GetFileName(name);
            char[] invalidChars = Path.GetInvalidFileNameChars();
            StringBuilder sb = new StringBuilder();
            foreach (char c in baseName)
            {
                if (c >= 32 && c != 127 && Array.IndexOf(invalidChars, c) < 0)
                {
                    sb.Append(c);
                }
            }
            string cleaned = sb.ToString();
            if (string.IsNullOrEmpty(cleaned) || cleaned == "." || cleaned == "..")
            {
                return "transfer.bin";
            }
            return cleaned;
        }

        public static byte[] PackFrame(OtiFrameHeader header, byte[] blockData)
        {
            byte[] outBytes = new byte[OTI_HEADER_LEN + header.SymbolSizeBytes];
            
            // Serialize first 14 bytes
            Buffer.BlockCopy(BitConverter.GetBytes(OTI_MAGIC), 0, outBytes, 0, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(header.SessionId), 0, outBytes, 2, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(header.TotalPayloadBytes), 0, outBytes, 4, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(header.SymbolSizeBytes), 0, outBytes, 8, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(header.TotalSourceSymbols), 0, outBytes, 10, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(header.Seq), 0, outBytes, 12, 2);

            ushort crc = Crc16(outBytes, 0, 14);
            Buffer.BlockCopy(BitConverter.GetBytes(crc), 0, outBytes, 14, 2);

            if (blockData != null && header.SymbolSizeBytes > 0)
            {
                int copyLen = Math.Min(blockData.Length, (int)header.SymbolSizeBytes);
                Buffer.BlockCopy(blockData, 0, outBytes, OTI_HEADER_LEN, copyLen);
            }
            return outBytes;
        }

        public static bool ParseFrame(byte[] frameBytes, out OtiFrameHeader header, out byte[] blockData)
        {
            header = default(OtiFrameHeader);
            blockData = null;

            if (frameBytes == null || frameBytes.Length < OTI_HEADER_LEN) return false;

            ushort magic = BitConverter.ToUInt16(frameBytes, 0);
            if (magic != OTI_MAGIC) return false;

            header.Magic = magic;
            header.SessionId = BitConverter.ToUInt16(frameBytes, 2);
            header.TotalPayloadBytes = BitConverter.ToUInt32(frameBytes, 4);
            header.SymbolSizeBytes = BitConverter.ToUInt16(frameBytes, 8);
            header.TotalSourceSymbols = BitConverter.ToUInt16(frameBytes, 10);
            header.Seq = BitConverter.ToUInt16(frameBytes, 12);
            header.HeaderCrc16 = BitConverter.ToUInt16(frameBytes, 14);

            if (header.TotalSourceSymbols == 0 || header.SymbolSizeBytes == 0) return false;

            ushort computedCrc = Crc16(frameBytes, 0, 14);
            if (header.HeaderCrc16 != computedCrc) return false;

            if (frameBytes.Length < OTI_HEADER_LEN + header.SymbolSizeBytes) return false;

            blockData = new byte[header.SymbolSizeBytes];
            Buffer.BlockCopy(frameBytes, OTI_HEADER_LEN, blockData, 0, header.SymbolSizeBytes);
            return true;
        }

        public static byte[] PackContainer(string name, string mimeType, byte[] data)
        {
            string safeName = SanitizeFileName(name);
            string safeType = string.IsNullOrEmpty(mimeType) ? "application/octet-stream" : mimeType;

            byte[] nameBytes = Encoding.UTF8.GetBytes(safeName);
            byte[] typeBytes = Encoding.UTF8.GetBytes(safeType);

            ushort nameLen = (ushort)nameBytes.Length;
            ushort typeLen = (ushort)typeBytes.Length;
            uint origSize = data != null ? (uint)data.Length : 0;
            uint transSize = origSize;

            byte[] digest = ComputeSha256(data ?? new byte[0], 0, (int)origSize);

            byte[] container = new byte[DCF2_HEADER_LEN + nameLen + typeLen + transSize];
            Buffer.BlockCopy(DCF2_MAGIC, 0, container, 0, 4);

            container[4] = 0; // use_gzip = 0
            Buffer.BlockCopy(BitConverter.GetBytes(nameLen), 0, container, 5, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(typeLen), 0, container, 7, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(origSize), 0, container, 9, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(transSize), 0, container, 13, 4);
            Buffer.BlockCopy(digest, 0, container, 17, 32);

            int offset = DCF2_HEADER_LEN;
            Buffer.BlockCopy(nameBytes, 0, container, offset, nameLen);
            offset += nameLen;
            Buffer.BlockCopy(typeBytes, 0, container, offset, typeLen);
            offset += typeLen;

            if (data != null && origSize > 0)
            {
                Buffer.BlockCopy(data, 0, container, offset, (int)origSize);
            }
            return container;
        }

        public static OpticalFile UnpackContainer(byte[] containerBytes)
        {
            if (containerBytes == null || containerBytes.Length < DCF2_HEADER_LEN) return null;

            for (int i = 0; i < 4; i++)
            {
                if (containerBytes[i] != DCF2_MAGIC[i]) return null;
            }

            bool isGzipped = containerBytes[4] == 1;
            ushort nameLen = BitConverter.ToUInt16(containerBytes, 5);
            ushort typeLen = BitConverter.ToUInt16(containerBytes, 7);
            uint origSize = BitConverter.ToUInt32(containerBytes, 9);
            uint transSize = BitConverter.ToUInt32(containerBytes, 13);

            byte[] expectedSha = new byte[32];
            Buffer.BlockCopy(containerBytes, 17, expectedSha, 0, 32);

            int expectedTotal = DCF2_HEADER_LEN + nameLen + typeLen + (int)transSize;
            if (containerBytes.Length < expectedTotal) return null;

            int offset = DCF2_HEADER_LEN;
            string name = Encoding.UTF8.GetString(containerBytes, offset, nameLen);
            offset += nameLen;
            string mime = Encoding.UTF8.GetString(containerBytes, offset, typeLen);
            offset += typeLen;

            byte[] payload = new byte[transSize];
            Buffer.BlockCopy(containerBytes, offset, payload, 0, (int)transSize);

            byte[] actualSha = ComputeSha256(payload, 0, payload.Length);
            for (int i = 0; i < 32; i++)
            {
                if (expectedSha[i] != actualSha[i]) return null;
            }

            return new OpticalFile
            {
                Name = SanitizeFileName(name),
                MimeType = mime,
                Data = payload,
                Sha256 = actualSha,
                IsGzipped = isGzipped,
                OriginalSize = origSize
            };
        }
    }
}
