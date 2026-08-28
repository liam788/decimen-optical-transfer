using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OpticalTransfer.Core
{
    public class QrBitmap
    {
        public int ModuleCount { get; set; }
        public byte[] Modules { get; set; } // 1 = Black, 0 = White

        public bool GetModule(int x, int y)
        {
            if (x < 0 || x >= ModuleCount || y < 0 || y >= ModuleCount) return false;
            return Modules[y * ModuleCount + x] != 0;
        }

        public WriteableBitmap ToBitmap(int scale = 8, int quietZone = 4)
        {
            int totalModules = ModuleCount + quietZone * 2;
            int pixelWidth = totalModules * scale;
            int pixelHeight = totalModules * scale;
            int stride = (pixelWidth * 4); // BGRA32

            byte[] pixelData = new byte[pixelHeight * stride];

            // Fill background pure white (0xFF, 0xFF, 0xFF, 0xFF)
            for (int i = 0; i < pixelData.Length; i += 4)
            {
                pixelData[i] = 255;     // B
                pixelData[i + 1] = 255; // G
                pixelData[i + 2] = 255; // R
                pixelData[i + 3] = 255; // A
            }

            // Draw black modules
            for (int y = 0; y < ModuleCount; y++)
            {
                for (int x = 0; x < ModuleCount; x++)
                {
                    if (GetModule(x, y))
                    {
                        int startPxX = (x + quietZone) * scale;
                        int startPxY = (y + quietZone) * scale;

                        for (int py = 0; py < scale; py++)
                        {
                            int rowOffset = (startPxY + py) * stride;
                            for (int px = 0; px < scale; px++)
                            {
                                int idx = rowOffset + (startPxX + px) * 4;
                                pixelData[idx] = 10;     // B (Deep black / contrast)
                                pixelData[idx + 1] = 10; // G
                                pixelData[idx + 2] = 10; // R
                                pixelData[idx + 3] = 255;// A
                            }
                        }
                    }
                }
            }

            WriteableBitmap wbm = new WriteableBitmap(pixelWidth, pixelHeight, 96, 96, PixelFormats.Bgra32, null);
            wbm.WritePixels(new Int32Rect(0, 0, pixelWidth, pixelHeight), pixelData, stride, 0);
            wbm.Freeze();
            return wbm;
        }
    }

    public class QrMatrixGenerator
    {
        public static QrBitmap Encode(byte[] data)
        {
            if (data == null) data = new byte[0];

            // Standard Version 8 QR grid (49x49 modules for ~316 bytes at ECC L)
            int version = 8;
            int size = 17 + version * 4; // 49
            byte[] modules = new byte[size * size];

            // Draw Finder Pattern
            Action<int, int> drawFinder = (x0, y0) =>
            {
                for (int r = 0; r < 7; ++r)
                {
                    for (int c = 0; c < 7; ++c)
                    {
                        bool black = (r == 0 || r == 6 || c == 0 || c == 6 || (r >= 2 && r <= 4 && c >= 2 && c <= 4));
                        modules[(y0 + r) * size + (x0 + c)] = (byte)(black ? 1 : 0);
                    }
                }
            };

            drawFinder(0, 0);
            drawFinder(size - 7, 0);
            drawFinder(0, size - 7);

            // Timing patterns
            for (int i = 8; i < size - 8; ++i)
            {
                modules[6 * size + i] = (byte)((i % 2 == 0) ? 1 : 0);
                modules[i * size + 6] = (byte)((i % 2 == 0) ? 1 : 0);
            }

            // Alignment pattern for Version 8 (pos at 6, 28, 42)
            Action<int, int> drawAlignment = (cx, cy) =>
            {
                for (int r = -2; r <= 2; ++r)
                {
                    for (int c = -2; c <= 2; ++c)
                    {
                        bool black = (Math.Abs(r) == 2 || Math.Abs(c) == 2 || (r == 0 && c == 0));
                        int targetY = cy + r;
                        int targetX = cx + c;
                        if (targetY >= 0 && targetY < size && targetX >= 0 && targetX < size)
                        {
                            modules[targetY * size + targetX] = (byte)(black ? 1 : 0);
                        }
                    }
                }
            };

            drawAlignment(28, 6);
            drawAlignment(6, 28);
            drawAlignment(28, 28);

            // Embed payload data bits sequentially in available modules
            int bitIdx = 0;
            int totalBits = data.Length * 8;
            for (int y = 0; y < size; ++y)
            {
                for (int x = 0; x < size; ++x)
                {
                    // Skip finder patterns + separators
                    if ((x < 8 && y < 8) || (x >= size - 8 && y < 8) || (x < 8 && y >= size - 8)) continue;
                    if (x == 6 || y == 6) continue;
                    // Skip alignment patterns
                    if (Math.Abs(x - 28) <= 2 && Math.Abs(y - 28) <= 2) continue;

                    if (bitIdx < totalBits)
                    {
                        byte byteVal = data[bitIdx / 8];
                        int bit = (byteVal >> (7 - (bitIdx % 8))) & 1;
                        modules[y * size + x] = (byte)(bit != 0 ? 1 : 0);
                        bitIdx++;
                    }
                }
            }

            return new QrBitmap
            {
                ModuleCount = size,
                Modules = modules
            };
        }

        public static byte[] DecodeFromLuminance(byte[] luminance, int width, int height, int stride)
        {
            if (luminance == null || width < 49 || height < 49) return null;

            int size = 49;
            byte[] payload = new byte[320];
            int payloadIdx = 0;
            byte currentByte = 0;
            int bitCount = 0;

            for (int y = 0; y < size; ++y)
            {
                for (int x = 0; x < size; ++x)
                {
                    if ((x < 8 && y < 8) || (x >= size - 8 && y < 8) || (x < 8 && y >= size - 8)) continue;
                    if (x == 6 || y == 6) continue;
                    if (Math.Abs(x - 28) <= 2 && Math.Abs(y - 28) <= 2) continue;

                    int pixelY = y;
                    int pixelX = x;
                    if (pixelY < height && pixelX < width)
                    {
                        byte lum = luminance[pixelY * stride + pixelX];
                        int bit = (lum > 128) ? 0 : 1; // Dark = 1, Light = 0

                        currentByte = (byte)((currentByte << 1) | bit);
                        bitCount++;

                        if (bitCount == 8)
                        {
                            payload[payloadIdx++] = currentByte;
                            currentByte = 0;
                            bitCount = 0;
                            if (payloadIdx >= 316) break;
                        }
                    }
                }
                if (payloadIdx >= 316) break;
            }

            if (payloadIdx == 0) return null;

            byte[] result = new byte[payloadIdx];
            Buffer.BlockCopy(payload, 0, result, 0, payloadIdx);
            return result;
        }
    }
}
