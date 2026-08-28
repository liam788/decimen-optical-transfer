#pragma once

#include <cstdint>
#include <vector>
#include <optional>
#include <string>

namespace Optical::Core {

enum class QrEccLevel {
    L, // ~7% recovery (Default for fountain coding)
    M, // ~15%
    Q, // ~25%
    H  // ~30%
};

struct QrBitmap {
    std::vector<uint8_t> modules; // 1 byte per module: 0 = white, 1 = black
    int module_count = 0;         // Grid width & height
    QrEccLevel ecc_level = QrEccLevel::L;
};

enum class PixelFormat {
    NV12,
    NV21,
    YUV_420_888,
    BGRA,
    RGBA,
    Grayscale8
};

class IQrEngine {
public:
    virtual ~IQrEngine() = default;

    // Encodes binary payload into QrBitmap
    virtual QrBitmap encode(const uint8_t* data, size_t len, QrEccLevel ecc = QrEccLevel::L) = 0;

    // Detects and decodes QR binary payload from raw camera luminance / image buffer
    virtual std::optional<std::vector<uint8_t>> decode(
        const uint8_t* image_data,
        int width,
        int height,
        int row_stride,
        PixelFormat format
    ) = 0;
};

// Default high-performance software QR Engine
class DefaultQrEngine : public IQrEngine {
public:
    DefaultQrEngine();
    ~DefaultQrEngine() override = default;

    QrBitmap encode(const uint8_t* data, size_t len, QrEccLevel ecc = QrEccLevel::L) override;

    std::optional<std::vector<uint8_t>> decode(
        const uint8_t* image_data,
        int width,
        int height,
        int row_stride,
        PixelFormat format
    ) override;
};

} // namespace Optical::Core
