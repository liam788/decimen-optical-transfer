#include "session_interfaces.hpp"
#include "tx_session_controller.hpp"
#include "rx_session_controller.hpp"

namespace Optical::State {

std::shared_ptr<ITransmitSession> SessionFactory::createTransmitSession(
    std::shared_ptr<Core::IQrEngine> qr_engine,
    std::shared_ptr<ISessionObserver> observer
) {
    return std::make_shared<TxSessionController>(std::move(qr_engine), std::move(observer));
}

std::shared_ptr<IReceiveSession> SessionFactory::createReceiveSession(
    std::shared_ptr<HAL::ICameraProvider> camera_provider,
    std::shared_ptr<Core::IQrEngine> qr_engine,
    std::shared_ptr<ISessionObserver> observer
) {
    return std::make_shared<RxSessionController>(
        std::move(camera_provider),
        std::move(qr_engine),
        std::move(observer)
    );
}

} // namespace Optical::State
