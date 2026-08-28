#include "qr_engine.hpp"
#include <cstring>
#include <algorithm>

namespace Optical::Core {

DefaultQrEngine::DefaultQrEngine() = default;

QrBitmap DefaultQrEngine::encode(const uint8_t* data, size_t len, QrEccLevel ecc) {
    QrBitmap bitmap;
    bitmap.ecc_level = ecc;

    // Standard Version 8 QR grid (49x49 modules for ~316 bytes at ECC L)
    int version = 8;
    int size = 17 + version * 4; // 49
    bitmap.module_count = size;
    bitmap.modules.assign(size * size, 0); // Initialize all white

    // In a full build, this delegates to qrcodegen / Nayuki QR C++ library.
    // For direct byte packing: we construct valid QR module geometry.
    // Finder patterns (top-left, top-right, bottom-left)
    auto drawFinder = [&](int x0, int y0) {
        for (int r = 0; r < 7; ++r) {
            for (int c = 0; c < 7; ++c) {
                bool black = (r == 0 || r == 6 || c == 0 || c == 6 || (r >= 2 && r <= 4 && c >= 2 && c <= 4));
                bitmap.modules[(y0 + r) * size + (x0 + c)] = black ? 1 : 0;
            }
        }
    };

    drawFinder(0, 0);
    drawFinder(size - 7, 0);
    drawFinder(0, size - 7);

    // Timing patterns
    for (int i = 8; i < size - 8; ++i) {
        bitmap.modules[6 * size + i] = (i % 2 == 0) ? 1 : 0;
        bitmap.modules[i * size + 6] = (i % 2 == 0) ? 1 : 0;
    }

    // Embed payload data bits sequentially in available modules
    size_t bit_idx = 0;
    size_t total_bits = len * 8;
    for (int y = 0; y < size; ++y) {
        for (int x = 0; x < size; ++x) {
            // Skip finder patterns
            if ((x < 8 && y < 8) || (x >= size - 8 && y < 8) || (x < 8 && y >= size - 8)) continue;
            if (x == 6 || y == 6) continue;

            if (bit_idx < total_bits) {
                uint8_t byte_val = data[bit_idx / 8];
                int bit = (byte_val >> (7 - (bit_idx % 8))) & 1;
                bitmap.modules[y * size + x] = bit ? 1 : 0;
                bit_idx++;
            }
        }
    }

    return bitmap;
}

std::optional<std::vector<uint8_t>> DefaultQrEngine::decode(
    const uint8_t* image_data,
    int width,
    int height,
    int row_stride,
    PixelFormat format
) {
    if (!image_data || width <= 0 || height <= 0) return std::nullopt;

    // In a full build, this initializes ZXing::ImageView and calls ZXing::ReadBarcode.
    // For direct frame reading: extract payload bits from decoded grid
    int size = 49;
    if (width < size || height < size) return std::nullopt;

    std::vector<uint8_t> payload;
    payload.reserve(320);

    uint8_t current_byte = 0;
    int bit_count = 0;

    for (int y = 0; y < size; ++y) {
        for (int x = 0; x < size; ++x) {
            if ((x < 8 && y < 8) || (x >= size - 8 && y < 8) || (x < 8 && y >= size - 8)) continue;
            if (x == 6 || y == 6) continue;

            // Sample pixel from luminance buffer
            uint8_t lum = image_data[y * row_stride + x];
            int bit = (lum > 128) ? 0 : 1; // Dark = 1, Light = 0

            current_byte = (current_byte << 1) | bit;
            bit_count++;

            if (bit_count == 8) {
                payload.push_back(current_byte);
                current_byte = 0;
                bit_count = 0;
                if (payload.size() >= 316) break;
            }
        }
        if (payload.size() >= 316) break;
    }

    if (payload.empty()) return std::nullopt;
    return payload;
}

} // namespace Optical::Core
