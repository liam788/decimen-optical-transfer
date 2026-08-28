#pragma once

#include "qr_engine.hpp"
#include <cstdint>
#include <functional>
#include <memory>
#include <string>

namespace Optical::HAL {

struct CameraConfig {
    int preferred_width = 1280;
    int preferred_height = 720;
    int preferred_fps = 30;
    bool prefer_fixed_exposure = true;
    int facing = 0; // 0 = Back (default), 1 = Front
};

struct CameraFrame {
    const uint8_t* data = nullptr; // Borrowed pointer, valid only during callback
    int width = 0;
    int height = 0;
    int row_stride = 0;
    Core::PixelFormat format = Core::PixelFormat::NV12;
    int64_t timestamp_us = 0;
};

enum class CameraFacing {
    Back,
    Front
};

enum class CameraError {
    None = 0,
    PermissionDenied,
    DeviceUnavailable,
    DeviceInUse,
    Unsupported
};

class ICameraProvider {
public:
    virtual ~ICameraProvider() = default;

    virtual CameraError startCameraStream(
        const CameraConfig& config,
        std::function<void(const CameraFrame&)> on_frame,
        std::function<void(CameraError)> on_error
    ) = 0;

    virtual void stopCameraStream() = 0;

    virtual bool isTorchSupported() const = 0;
    virtual bool setTorch(bool on) = 0;

    virtual bool hasMultipleCameras() const = 0;
    virtual bool switchCamera() = 0;
};

} // namespace Optical::HAL
