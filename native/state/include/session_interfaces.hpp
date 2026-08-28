#pragma once

#include "session_models.hpp"
#include "hal_camera.hpp"
#include "fountain_codec.hpp"
#include "qr_engine.hpp"
#include <memory>
#include <functional>

namespace Optical::State {

class ISessionObserver {
public:
    virtual ~ISessionObserver() = default;
    virtual void onStateChanged(SessionState new_state, const SessionError& error) = 0;
    virtual void onProgressUpdated(const SessionSnapshot& snapshot) = 0;
    virtual void onFrameReady(const Core::QrBitmap& bitmap) = 0;
    virtual void onTransferCompleted(const std::vector<uint8_t>& payload, const TransferMetadata& meta) = 0;
};

class ITransmitSession {
public:
    virtual ~ITransmitSession() = default;
    virtual bool start(const std::vector<uint8_t>& file_bytes, const std::string& file_name, float fps_preset) = 0;
    virtual void setFps(float fps) = 0;
    virtual void pause() = 0;
    virtual void resume() = 0;
    virtual void cancel() = 0;
    virtual Core::QrBitmap getNextFrame() = 0; // Cadence-gated for 60/120Hz VSync
    virtual SessionSnapshot getSnapshot() const = 0;
};

class IReceiveSession {
public:
    virtual ~IReceiveSession() = default;
    virtual bool start(const HAL::CameraConfig& camera_config) = 0;
    virtual void pause() = 0;
    virtual void resume() = 0;
    virtual void cancel() = 0;
    virtual bool hasMultipleCameras() const = 0;
    virtual bool switchCamera() = 0;
    virtual bool isTorchSupported() const = 0;
    virtual bool toggleTorch(bool on) = 0;
    virtual SessionSnapshot getSnapshot() const = 0;
};

class SessionFactory {
public:
    static std::shared_ptr<ITransmitSession> createTransmitSession(
        std::shared_ptr<Core::IQrEngine> qr_engine,
        std::shared_ptr<ISessionObserver> observer
    );

    static std::shared_ptr<IReceiveSession> createReceiveSession(
        std::shared_ptr<HAL::ICameraProvider> camera_provider,
        std::shared_ptr<Core::IQrEngine> qr_engine,
        std::shared_ptr<ISessionObserver> observer
    );
};

} // namespace Optical::State
