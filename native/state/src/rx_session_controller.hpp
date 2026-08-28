#pragma once

#include "session_interfaces.hpp"
#include "fountain_codec.hpp"
#include "hal_camera.hpp"
#include <thread>
#include <atomic>
#include <mutex>
#include <condition_variable>
#include <vector>

namespace Optical::State {

class RxSessionController : public IReceiveSession {
public:
    RxSessionController(
        std::shared_ptr<HAL::ICameraProvider> camera_provider,
        std::shared_ptr<Core::IQrEngine> qr_engine,
        std::shared_ptr<ISessionObserver> observer
    );
    ~RxSessionController() override;

    bool start(const HAL::CameraConfig& camera_config) override;
    void pause() override;
    void resume() override;
    void cancel() override;

    bool hasMultipleCameras() const override;
    bool switchCamera() override;
    bool isTorchSupported() const override;
    bool toggleTorch(bool on) override;

    SessionSnapshot getSnapshot() const override;

private:
    void workerDecodeLoop();
    void handleIncomingCameraFrame(const HAL::CameraFrame& frame);

    std::shared_ptr<HAL::ICameraProvider> camera_provider_;
    std::shared_ptr<Core::IQrEngine> qr_engine_;
    std::shared_ptr<ISessionObserver> observer_;

    std::unique_ptr<Core::IncrementalPeelingDecoder> decoder_;
    SessionSnapshot snapshot_;
    mutable std::mutex mutex_;

    std::atomic<bool> is_running_{false};
    std::atomic<bool> is_paused_{false};

    // 2-Slot Dropping Queue
    struct FrameSlot {
        std::vector<uint8_t> luminance_buffer;
        int width = 0;
        int height = 0;
        int row_stride = 0;
        Core::PixelFormat format = Core::PixelFormat::NV12;
        int64_t timestamp_us = 0;
    };

    FrameSlot slot_pool_[2];
    std::atomic<int> write_slot_idx_{0};
    std::atomic<int> read_slot_idx_{1};
    std::atomic<bool> has_unread_frame_{false};
    std::mutex queue_mutex_;
    std::condition_variable queue_cv_;

    std::thread worker_thread_;
    std::chrono::steady_clock::time_point session_start_time_;
};

} // namespace Optical::State
