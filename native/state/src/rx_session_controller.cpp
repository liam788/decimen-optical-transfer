#include "rx_session_controller.hpp"
#include <chrono>

namespace Optical::State {

RxSessionController::RxSessionController(
    std::shared_ptr<HAL::ICameraProvider> camera_provider,
    std::shared_ptr<Core::IQrEngine> qr_engine,
    std::shared_ptr<ISessionObserver> observer
) : camera_provider_(std::move(camera_provider)),
    qr_engine_(std::move(qr_engine)),
    observer_(std::move(observer)),
    decoder_(std::make_unique<Core::IncrementalPeelingDecoder>())
{
    snapshot_.role = SessionRole::Receiver;
    snapshot_.state = SessionState::Idle;
}

RxSessionController::~RxSessionController() {
    cancel();
}

bool RxSessionController::start(const HAL::CameraConfig& camera_config) {
    std::lock_guard<std::mutex> lock(mutex_);
    decoder_->reset();
    snapshot_.state = SessionState::Configuring;
    session_start_time_ = std::chrono::steady_clock::now();

    is_running_ = true;
    is_paused_ = false;

    // Start background decode worker
    worker_thread_ = std::thread(&RxSessionController::workerDecodeLoop, this);

    // Start HAL camera stream
    HAL::CameraError err = camera_provider_->startCameraStream(
        camera_config,
        [this](const HAL::CameraFrame& frame) { handleIncomingCameraFrame(frame); },
        [this](HAL::CameraError error) {
            std::lock_guard<std::mutex> err_lock(mutex_);
            snapshot_.state = SessionState::Failed;
            snapshot_.error.code = SessionErrorCode::CameraStreamFailed;
            snapshot_.error.message = "HAL Camera Stream Error";
            if (observer_) observer_->onStateChanged(SessionState::Failed, snapshot_.error);
        }
    );

    if (err != HAL::CameraError::None) {
        snapshot_.state = SessionState::Failed;
        snapshot_.error.code = SessionErrorCode::CameraDeviceUnavailable;
        if (observer_) observer_->onStateChanged(SessionState::Failed, snapshot_.error);
        return false;
    }

    snapshot_.state = SessionState::Transferring;
    if (observer_) observer_->onStateChanged(SessionState::Transferring, snapshot_.error);
    return true;
}

void RxSessionController::pause() {
    is_paused_ = true;
    std::lock_guard<std::mutex> lock(mutex_);
    snapshot_.state = SessionState::Paused;
    if (observer_) observer_->onStateChanged(SessionState::Paused, snapshot_.error);
}

void RxSessionController::resume() {
    is_paused_ = false;
    std::lock_guard<std::mutex> lock(mutex_);
    snapshot_.state = SessionState::Transferring;
    if (observer_) observer_->onStateChanged(SessionState::Transferring, snapshot_.error);
}

void RxSessionController::cancel() {
    is_running_ = false;
    queue_cv_.notify_all();

    if (camera_provider_) {
        camera_provider_->stopCameraStream();
    }
    if (worker_thread_.joinable()) {
        worker_thread_.join();
    }

    std::lock_guard<std::mutex> lock(mutex_);
    snapshot_.state = SessionState::Cancelled;
    if (observer_) observer_->onStateChanged(SessionState::Cancelled, snapshot_.error);
}

bool RxSessionController::hasMultipleCameras() const {
    return camera_provider_ ? camera_provider_->hasMultipleCameras() : false;
}

bool RxSessionController::switchCamera() {
    return camera_provider_ ? camera_provider_->switchCamera() : false;
}

bool RxSessionController::isTorchSupported() const {
    return camera_provider_ ? camera_provider_->isTorchSupported() : false;
}

bool RxSessionController::toggleTorch(bool on) {
    return camera_provider_ ? camera_provider_->setTorch(on) : false;
}

void RxSessionController::handleIncomingCameraFrame(const HAL::CameraFrame& frame) {
    if (!is_running_ || is_paused_) return;

    // Hot path executed on native capture thread: Must be < 0.3ms
    int w_idx = write_slot_idx_.load();
    FrameSlot& slot = slot_pool_[w_idx];

    size_t plane_bytes = static_cast<size_t>(frame.height * frame.row_stride);
    if (slot.luminance_buffer.size() < plane_bytes) {
        slot.luminance_buffer.resize(plane_bytes);
    }

    std::memcpy(slot.luminance_buffer.data(), frame.data, plane_bytes);
    slot.width = frame.width;
    slot.height = frame.height;
    slot.row_stride = frame.row_stride;
    slot.format = frame.format;
    slot.timestamp_us = frame.timestamp_us;

    // Atomically swap write/read slots
    read_slot_idx_.store(w_idx);
    write_slot_idx_.store(1 - w_idx);

    if (has_unread_frame_.load()) {
        std::lock_guard<std::mutex> lock(mutex_);
        snapshot_.rx_stats.frames_dropped_queue++;
    }

    has_unread_frame_.store(true);
    queue_cv_.notify_one();
}

void RxSessionController::workerDecodeLoop() {
    while (is_running_) {
        std::unique_lock<std::mutex> q_lock(queue_mutex_);
        queue_cv_.wait(q_lock, [this]() { return has_unread_frame_.load() || !is_running_.load(); });
        if (!is_running_) break;

        has_unread_frame_.store(false);
        int r_idx = read_slot_idx_.load();
        const FrameSlot& slot = slot_pool_[r_idx];

        q_lock.unlock();

        {
            std::lock_guard<std::mutex> lock(mutex_);
            snapshot_.rx_stats.raw_frames_received++;
        }

        // Run ZXing barcode decode on luminance buffer
        auto decoded_bytes = qr_engine_->decode(
            slot.luminance_buffer.data(),
            slot.width,
            slot.height,
            slot.row_stride,
            slot.format
        );

        if (!decoded_bytes.has_value()) {
            std::lock_guard<std::mutex> lock(mutex_);
            snapshot_.rx_stats.qr_decode_failures++;
            continue;
        }

        {
            std::lock_guard<std::mutex> lock(mutex_);
            snapshot_.rx_stats.qr_decoded_count++;
        }

        // Feed extracted droplet into Incremental Peeling Decoder
        Core::DecoderResult res = decoder_->consumeFrame(
            decoded_bytes->data(),
            decoded_bytes->size()
        );

        // Update Metrology
        Core::DecoderStatus d_status = decoder_->getStatus();
        {
            std::lock_guard<std::mutex> lock(mutex_);
            snapshot_.rx_stats.symbols_required = d_status.k;
            snapshot_.rx_stats.current_rank = d_status.solved_count;
            snapshot_.rx_stats.progress_percentage = d_status.progress_percentage;

            auto now = std::chrono::steady_clock::now();
            auto elapsed_ms = std::chrono::duration_cast<std::chrono::milliseconds>(now - session_start_time_).count();
            snapshot_.rx_stats.elapsed_duration_ms = elapsed_ms;

            if (elapsed_ms > 0) {
                snapshot_.rx_stats.instant_fps = static_cast<float>(snapshot_.rx_stats.raw_frames_received) / (static_cast<float>(elapsed_ms) / 1000.0f);
                snapshot_.rx_stats.decode_fps = static_cast<float>(snapshot_.rx_stats.qr_decoded_count) / (static_cast<float>(elapsed_ms) / 1000.0f);
                snapshot_.rx_stats.goodput_kbps = (static_cast<float>(d_status.solved_count * 300) / 1024.0f) / (static_cast<float>(elapsed_ms) / 1000.0f);
            }
        }

        if (observer_) {
            observer_->onProgressUpdated(getSnapshot());
        }

        // Check completion & verify payload
        if (decoder_->isComplete()) {
            auto payload_opt = decoder_->assemblePayload();
            if (payload_opt.has_value()) {
                auto file_opt = Core::Protocol::unpackContainer(payload_opt->data(), payload_opt->size());
                if (file_opt.has_value()) {
                    std::lock_guard<std::mutex> lock(mutex_);
                    snapshot_.state = SessionState::Completed;
                    snapshot_.metadata.file_name = file_opt->name;
                    snapshot_.metadata.file_size_bytes = file_opt->data.size();
                    std::memcpy(snapshot_.metadata.sha256, file_opt->sha256, 32);

                    if (observer_) {
                        observer_->onStateChanged(SessionState::Completed, snapshot_.error);
                        observer_->onTransferCompleted(file_opt->data, snapshot_.metadata);
                    }
                    break;
                }
            }
        }
    }
}

SessionSnapshot RxSessionController::getSnapshot() const {
    std::lock_guard<std::mutex> lock(mutex_);
    return snapshot_;
}

} // namespace Optical::State
