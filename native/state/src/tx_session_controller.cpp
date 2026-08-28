#include "tx_session_controller.hpp"

namespace Optical::State {

TxSessionController::TxSessionController(
    std::shared_ptr<Core::IQrEngine> qr_engine,
    std::shared_ptr<ISessionObserver> observer
) : qr_engine_(std::move(qr_engine)), observer_(std::move(observer)) {
    snapshot_.role = SessionRole::Transmitter;
    snapshot_.state = SessionState::Idle;
}

TxSessionController::~TxSessionController() {
    cancel();
}

bool TxSessionController::start(const std::vector<uint8_t>& file_bytes, const std::string& file_name, float fps_preset) {
    std::lock_guard<std::mutex> lock(mutex_);
    if (file_bytes.empty()) return false;

    // Pack into DCF2 Container
    std::vector<uint8_t> container = Core::Protocol::packContainer(file_name, "", file_bytes.data(), file_bytes.size());
    encoder_ = std::make_unique<Core::SystematicFountainEncoder>(container.data(), container.size(), 300);

    target_fps_ = (fps_preset > 0.0f) ? fps_preset : 20.0f;
    snapshot_.metadata.file_name = file_name;
    snapshot_.metadata.file_size_bytes = file_bytes.size();
    snapshot_.metadata.total_source_symbols = static_cast<uint16_t>(encoder_->getK());
    snapshot_.tx_stats.target_fps = target_fps_;
    snapshot_.state = SessionState::Transferring;

    session_start_time_ = std::chrono::steady_clock::now();
    last_advance_time_ = session_start_time_;

    // Generate initial frame synchronously for immediate display
    std::vector<uint8_t> initial_packet = encoder_->nextFrame();
    active_frame_ = qr_engine_->encode(initial_packet.data(), initial_packet.size(), Core::QrEccLevel::L);
    snapshot_.tx_stats.symbols_emitted = 1;
    snapshot_.tx_stats.current_frame_index = 1;

    is_running_ = true;
    is_paused_ = false;

    pregen_thread_ = std::thread(&TxSessionController::pregenerateLoop, this);
    push_pacing_thread_ = std::thread(&TxSessionController::pushPacingLoop, this);

    if (observer_) {
        observer_->onStateChanged(SessionState::Transferring, snapshot_.error);
        observer_->onFrameReady(active_frame_);
    }
    return true;
}

void TxSessionController::setFps(float fps) {
    if (fps > 0.0f) {
        target_fps_ = fps;
        std::lock_guard<std::mutex> lock(mutex_);
        snapshot_.tx_stats.target_fps = fps;
    }
}

void TxSessionController::pause() {
    is_paused_ = true;
    std::lock_guard<std::mutex> lock(mutex_);
    snapshot_.state = SessionState::Paused;
    if (observer_) observer_->onStateChanged(SessionState::Paused, snapshot_.error);
}

void TxSessionController::resume() {
    is_paused_ = false;
    std::lock_guard<std::mutex> lock(mutex_);
    snapshot_.state = SessionState::Transferring;
    if (observer_) observer_->onStateChanged(SessionState::Transferring, snapshot_.error);
}

void TxSessionController::cancel() {
    is_running_ = false;
    stage_cv_.notify_all();

    if (pregen_thread_.joinable()) pregen_thread_.join();
    if (push_pacing_thread_.joinable()) push_pacing_thread_.join();

    std::lock_guard<std::mutex> lock(mutex_);
    snapshot_.state = SessionState::Cancelled;
    if (observer_) observer_->onStateChanged(SessionState::Cancelled, snapshot_.error);
}

void TxSessionController::pregenerateLoop() {
    while (is_running_) {
        std::unique_lock<std::mutex> lock(stage_mutex_);
        stage_cv_.wait(lock, [this]() { return !has_staged_frame_.load() || !is_running_.load(); });
        if (!is_running_) break;

        // Generate next droplet & QR matrix off UI thread
        std::vector<uint8_t> next_packet;
        {
            std::lock_guard<std::mutex> enc_lock(mutex_);
            if (encoder_) {
                next_packet = encoder_->nextFrame();
            }
        }

        if (!next_packet.empty()) {
            Core::QrBitmap next_qr = qr_engine_->encode(next_packet.data(), next_packet.size(), Core::QrEccLevel::L);
            staged_frame_ = std::move(next_qr);
            has_staged_frame_ = true;
        }
    }
}

void TxSessionController::pushPacingLoop() {
    while (is_running_) {
        float fps = target_fps_.load();
        int interval_ms = (fps > 0.0f) ? static_cast<int>(1000.0f / fps) : 50;
        std::this_thread::sleep_for(std::chrono::milliseconds(interval_ms));

        if (!is_running_ || is_paused_) continue;

        // Advance frame and notify observer
        Core::QrBitmap frame = getNextFrame();
        if (observer_) {
            observer_->onFrameReady(frame);
            observer_->onProgressUpdated(getSnapshot());
        }
    }
}

Core::QrBitmap TxSessionController::getNextFrame() {
    std::lock_guard<std::mutex> lock(mutex_);
    if (!is_running_ || is_paused_) {
        return active_frame_;
    }

    auto now = std::chrono::steady_clock::now();
    auto elapsed_us = std::chrono::duration_cast<std::chrono::microseconds>(now - last_advance_time_).count();
    int64_t target_interval_us = static_cast<int64_t>(1000000.0f / target_fps_.load());

    // Cadence gating: return current frame if called earlier than target interval (e.g. 120Hz VSync)
    if (elapsed_us < target_interval_us) {
        return active_frame_;
    }

    // Interval met: swap in staged frame
    if (has_staged_frame_.load()) {
        std::lock_guard<std::mutex> stage_lock(stage_mutex_);
        active_frame_ = std::move(staged_frame_);
        has_staged_frame_ = false;
        stage_cv_.notify_one(); // Signal pregen worker for next frame

        last_advance_time_ = now;
        snapshot_.tx_stats.symbols_emitted++;
        snapshot_.tx_stats.current_frame_index++;

        auto total_elapsed_ms = std::chrono::duration_cast<std::chrono::milliseconds>(now - session_start_time_).count();
        snapshot_.tx_stats.elapsed_duration_ms = total_elapsed_ms;
        if (total_elapsed_ms > 0) {
            snapshot_.tx_stats.actual_fps = static_cast<float>(snapshot_.tx_stats.symbols_emitted) / (static_cast<float>(total_elapsed_ms) / 1000.0f);
        }
    }

    return active_frame_;
}

SessionSnapshot TxSessionController::getSnapshot() const {
    std::lock_guard<std::mutex> lock(mutex_);
    return snapshot_;
}

} // namespace Optical::State
