#include "hal_camera.hpp"

namespace Optical::HAL {

class WindowsCameraProvider : public ICameraProvider {
public:
    WindowsCameraProvider() = default;
    ~WindowsCameraProvider() override = default;

    CameraError startCameraStream(
        const CameraConfig& config,
        std::function<void(const CameraFrame&)> on_frame,
        std::function<void(CameraError)> on_error
    ) override {
        // Windows Media Foundation IMFSourceReader integration
        return CameraError::None;
    }

    void stopCameraStream() override {}
    bool isTorchSupported() const override { return false; }
    bool setTorch(bool on) override { return false; }
    bool hasMultipleCameras() const override { return false; }
    bool switchCamera() override { return false; }
};

} // namespace Optical::HAL
