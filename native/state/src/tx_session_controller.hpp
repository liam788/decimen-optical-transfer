#pragma once

#include "session_interfaces.hpp"
#include "fountain_codec.hpp"
#include <thread>
#include <atomic>
#include <mutex>
#include <condition_variable>
#include <chrono>

namespace Optical::State {

class TxSessionController : public ITransmitSession {
public:
    TxSessionController(
        std::shared_ptr<Core::IQrEngine> qr_engine,
        std::shared_ptr<ISessionObserver> observer
    );
    ~TxSessionController() override;

    bool start(const std::vector<uint8_t>& file_bytes, const std::string& file_name, float fps_preset) override;
    void setFps(float fps) override;
    void pause() override;
    void resume() override;
    void cancel() override;
    Core::QrBitmap getNextFrame() override;
    SessionSnapshot getSnapshot() const override;

private:
    void pregenerateLoop();
    void pushPacingLoop();

    std::shared_ptr<Core::IQrEngine> qr_engine_;
    std::shared_ptr<ISessionObserver> observer_;

    std::unique_ptr<Core::SystematicFountainEncoder> encoder_;
    SessionSnapshot snapshot_;
    mutable std::mutex mutex_;

    std::atomic<bool> is_running_{false};
    std::atomic<bool> is_paused_{false};
    std::atomic<float> target_fps_{20.0f};

    // 1-ahead ping-pong double buffer
    Core::QrBitmap active_frame_;
    Core::QrBitmap staged_frame_;
    std::atomic<bool> has_staged_frame_{false};
    std::mutex stage_mutex_;
    std::condition_variable stage_cv_;

    // Timing & Cadence gating
    std::chrono::steady_clock::time_point last_advance_time_;
    std::chrono::steady_clock::time_point session_start_time_;

    std::thread pregen_thread_;
    std::thread push_pacing_thread_;
};

} // namespace Optical::State
