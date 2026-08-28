#include "hal_camera.hpp"
#include <thread>
#include <atomic>
#include <chrono>
#include <vector>

namespace Optical::HAL {

class MockCameraProvider : public ICameraProvider {
public:
    MockCameraProvider() : is_running_(false), torch_on_(false), camera_facing_(CameraFacing::Back) {}
    ~MockCameraProvider() override { stopCameraStream(); }

    CameraError startCameraStream(
        const CameraConfig& config,
        std::function<void(const CameraFrame&)> on_frame,
        std::function<void(CameraError)> on_error
    ) override {
        if (is_running_) return CameraError::DeviceInUse;

        on_frame_ = std::move(on_frame);
        on_error_ = std::move(on_error);
        is_running_ = true;

        // Allocate synthetic 1280x720 luminance test buffer
        width_ = config.preferred_width;
        height_ = config.preferred_height;
        buffer_.assign(width_ * height_, 255); // White background

        stream_thread_ = std::thread([this, config]() {
            int interval_ms = 1000 / (config.preferred_fps > 0 ? config.preferred_fps : 30);
            while (is_running_) {
                auto start_time = std::chrono::steady_clock::now();

                CameraFrame frame;
                frame.data = buffer_.data();
                frame.width = width_;
                frame.height = height_;
                frame.row_stride = width_;
                frame.format = Core::PixelFormat::Grayscale8;
                frame.timestamp_us = std::chrono::duration_cast<std::chrono::microseconds>(
                    start_time.time_since_epoch()
                ).count();

                if (on_frame_) {
                    on_frame_(frame);
                }

                auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(
                    std::chrono::steady_clock::now() - start_time
                ).count();

                int sleep_dur = interval_ms - static_cast<int>(elapsed);
                if (sleep_dur > 0) {
                    std::this_thread::sleep_for(std::chrono::milliseconds(sleep_dur));
                }
            }
        });

        return CameraError::None;
    }

    void stopCameraStream() override {
        is_running_ = false;
        if (stream_thread_.joinable()) {
            stream_thread_.join();
        }
    }

    bool isTorchSupported() const override { return true; }
    bool setTorch(bool on) override { torch_on_ = on; return true; }
    bool hasMultipleCameras() const override { return true; }
    bool switchCamera() override {
        camera_facing_ = (camera_facing_ == CameraFacing::Back) ? CameraFacing::Front : CameraFacing::Back;
        return true;
    }

    // Direct injection helper for automated tests
    void injectFrameData(const uint8_t* data, size_t len) {
        if (buffer_.size() >= len) {
            std::memcpy(buffer_.data(), data, len);
        }
    }

private:
    std::atomic<bool> is_running_;
    bool torch_on_;
    CameraFacing camera_facing_;
    int width_ = 1280;
    int height_ = 720;
    std::vector<uint8_t> buffer_;
    std::thread stream_thread_;
    std::function<void(const CameraFrame&)> on_frame_;
    std::function<void(CameraError)> on_error_;
};

} // namespace Optical::HAL
