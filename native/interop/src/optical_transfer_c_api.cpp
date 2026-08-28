#include "optical_transfer_c_api.h"
#include "session_interfaces.hpp"
#include "session_factory.cpp"
#include "mock_camera_provider.cpp"
#include "qr_engine.hpp"
#include <cstring>
#include <memory>

using namespace Optical;

class CBridgeObserver : public State::ISessionObserver {
public:
    CBridgeObserver(
        StateChangedCallback on_state,
        ProgressCallback on_progress,
        FrameReadyCallback on_frame,
        CompleteCallback on_complete,
        void* user_data
    ) : on_state_(on_state),
        on_progress_(on_progress),
        on_frame_(on_frame),
        on_complete_(on_complete),
        user_data_(user_data) {}

    void onStateChanged(State::SessionState new_state, const State::SessionError& error) override {
        if (on_state_) {
            on_state_(static_cast<int>(new_state), static_cast<int>(error.code), error.message.c_str(), user_data_);
        }
    }

    void onProgressUpdated(const State::SessionSnapshot& snapshot) override {
        if (on_progress_) {
            if (snapshot.role == State::SessionRole::Transmitter) {
                on_progress_(0.0f, snapshot.tx_stats.actual_fps, 0.0f, user_data_);
            } else {
                on_progress_(
                    snapshot.rx_stats.progress_percentage,
                    snapshot.rx_stats.instant_fps,
                    snapshot.rx_stats.goodput_kbps,
                    user_data_
                );
            }
        }
    }

    void onFrameReady(const Core::QrBitmap& bitmap) override {
        if (on_frame_) {
            on_frame_(bitmap.modules.data(), bitmap.module_count, user_data_);
        }
    }

    void onTransferCompleted(const std::vector<uint8_t>& payload, const State::TransferMetadata& meta) override {
        if (on_complete_) {
            on_complete_(payload.data(), payload.size(), meta.file_name.c_str(), user_data_);
        }
    }

private:
    StateChangedCallback on_state_;
    ProgressCallback on_progress_;
    FrameReadyCallback on_frame_;
    CompleteCallback on_complete_;
    void* user_data_;
};

struct NativeTxContext {
    std::shared_ptr<State::ITransmitSession> session;
    std::shared_ptr<CBridgeObserver> observer;
};

struct NativeRxContext {
    std::shared_ptr<State::IReceiveSession> session;
    std::shared_ptr<CBridgeObserver> observer;
};

extern "C" {

OpticalTxHandle Optical_Tx_Create(
    const uint8_t* data,
    size_t len,
    const char* filename,
    float fps,
    StateChangedCallback on_state,
    ProgressCallback on_progress,
    FrameReadyCallback on_frame,
    void* user_data
) {
    if (!data || len == 0) return nullptr;

    auto observer = std::make_shared<CBridgeObserver>(on_state, on_progress, on_frame, nullptr, user_data);
    auto qr_engine = std::make_shared<Core::DefaultQrEngine>();
    auto session = State::SessionFactory::createTransmitSession(qr_engine, observer);

    std::vector<uint8_t> file_bytes(data, data + len);
    std::string name = filename ? filename : "transfer.bin";

    if (!session->start(file_bytes, name, fps)) {
        return nullptr;
    }

    auto ctx = new NativeTxContext{session, observer};
    return reinterpret_cast<OpticalTxHandle>(ctx);
}

void Optical_Tx_SetFps(OpticalTxHandle handle, float fps) {
    if (!handle) return;
    auto ctx = reinterpret_cast<NativeTxContext*>(handle);
    ctx->session->setFps(fps);
}

void Optical_Tx_Pause(OpticalTxHandle handle) {
    if (!handle) return;
    auto ctx = reinterpret_cast<NativeTxContext*>(handle);
    ctx->session->pause();
}

void Optical_Tx_Resume(OpticalTxHandle handle) {
    if (!handle) return;
    auto ctx = reinterpret_cast<NativeTxContext*>(handle);
    ctx->session->resume();
}

void Optical_Tx_Cancel(OpticalTxHandle handle) {
    if (!handle) return;
    auto ctx = reinterpret_cast<NativeTxContext*>(handle);
    ctx->session->cancel();
}

int Optical_Tx_GetNextFrame(OpticalTxHandle handle, uint8_t* out_modules, int* out_size) {
    if (!handle || !out_modules || !out_size) return 0;
    auto ctx = reinterpret_cast<NativeTxContext*>(handle);
    Core::QrBitmap frame = ctx->session->getNextFrame();
    *out_size = frame.module_count;
    std::memcpy(out_modules, frame.modules.data(), frame.modules.size());
    return 1;
}

void Optical_Tx_Destroy(OpticalTxHandle handle) {
    if (!handle) return;
    auto ctx = reinterpret_cast<NativeTxContext*>(handle);
    ctx->session->cancel();
    delete ctx;
}

OpticalRxHandle Optical_Rx_Create(
    StateChangedCallback on_state,
    ProgressCallback on_progress,
    CompleteCallback on_complete,
    void* user_data
) {
    auto observer = std::make_shared<CBridgeObserver>(on_state, on_progress, nullptr, on_complete, user_data);
    auto camera_provider = std::make_shared<HAL::MockCameraProvider>();
    auto qr_engine = std::make_shared<Core::DefaultQrEngine>();
    auto session = State::SessionFactory::createReceiveSession(camera_provider, qr_engine, observer);

    auto ctx = new NativeRxContext{session, observer};
    return reinterpret_cast<OpticalRxHandle>(ctx);
}

int Optical_Rx_Start(OpticalRxHandle handle, int camera_facing) {
    if (!handle) return 0;
    auto ctx = reinterpret_cast<NativeRxContext*>(handle);
    HAL::CameraConfig cfg;
    cfg.facing = camera_facing;
    return ctx->session->start(cfg) ? 1 : 0;
}

void Optical_Rx_Pause(OpticalRxHandle handle) {
    if (!handle) return;
    auto ctx = reinterpret_cast<NativeRxContext*>(handle);
    ctx->session->pause();
}

void Optical_Rx_Resume(OpticalRxHandle handle) {
    if (!handle) return;
    auto ctx = reinterpret_cast<NativeRxContext*>(handle);
    ctx->session->resume();
}

void Optical_Rx_Cancel(OpticalRxHandle handle) {
    if (!handle) return;
    auto ctx = reinterpret_cast<NativeRxContext*>(handle);
    ctx->session->cancel();
}

int Optical_Rx_HasMultipleCameras(OpticalRxHandle handle) {
    if (!handle) return 0;
    auto ctx = reinterpret_cast<NativeRxContext*>(handle);
    return ctx->session->hasMultipleCameras() ? 1 : 0;
}

int Optical_Rx_SwitchCamera(OpticalRxHandle handle) {
    if (!handle) return 0;
    auto ctx = reinterpret_cast<NativeRxContext*>(handle);
    return ctx->session->switchCamera() ? 1 : 0;
}

int Optical_Rx_IsTorchSupported(OpticalRxHandle handle) {
    if (!handle) return 0;
    auto ctx = reinterpret_cast<NativeRxContext*>(handle);
    return ctx->session->isTorchSupported() ? 1 : 0;
}

int Optical_Rx_ToggleTorch(OpticalRxHandle handle, int enable) {
    if (!handle) return 0;
    auto ctx = reinterpret_cast<NativeRxContext*>(handle);
    return ctx->session->toggleTorch(enable != 0) ? 1 : 0;
}

void Optical_Rx_Destroy(OpticalRxHandle handle) {
    if (!handle) return;
    auto ctx = reinterpret_cast<NativeRxContext*>(handle);
    ctx->session->cancel();
    delete ctx;
}

} // extern "C"
