#pragma once

#include "protocol_dcf2.hpp"
#include "qr_engine.hpp"
#include <cstdint>
#include <string>
#include <vector>

namespace Optical::State {

enum class SessionRole {
    Transmitter,
    Receiver
};

enum class SessionState {
    Idle,
    Configuring,
    Transferring,
    Paused,
    Completed,
    Failed,
    Cancelled
};

enum class SessionErrorCode {
    None = 0,
    CameraPermissionDenied,
    CameraDeviceUnavailable,
    CameraInUse,
    CameraStreamFailed,
    InvalidPayloadHeader,
    ChecksumMismatch,
    DecoderExhausted,
    IncompatibleProtocolVersion,
    BufferOverflow,
    Timeout,
    UserCancelled
};

struct SessionError {
    SessionErrorCode code = SessionErrorCode::None;
    std::string message;
};

struct TransferMetadata {
    std::string file_name;
    uint64_t file_size_bytes = 0;
    uint32_t crc32 = 0;
    uint8_t sha256[32] = {0};
    uint16_t symbol_size_bytes = 300;
    uint16_t total_source_symbols = 0;
    uint8_t total_blocks = 1;
};

struct TransmitterStats {
    uint32_t symbols_emitted = 0;
    uint32_t current_frame_index = 0;
    float target_fps = 20.0f;
    float actual_fps = 0.0f;
    int64_t elapsed_duration_ms = 0;
};

struct ReceiverStats {
    uint32_t raw_frames_received = 0;
    uint32_t frames_dropped_queue = 0;
    uint32_t qr_decoded_count = 0;
    uint32_t qr_decode_failures = 0;
    uint32_t duplicate_symbols = 0;
    uint32_t valid_symbols_ingested = 0;
    uint32_t symbols_required = 0;
    uint32_t current_rank = 0;
    float progress_percentage = 0.0f;
    float instant_fps = 0.0f;
    float decode_fps = 0.0f;
    float goodput_kbps = 0.0f;
    int64_t estimated_time_remaining_ms = 0;
    int64_t elapsed_duration_ms = 0;
};

struct SessionSnapshot {
    SessionRole role = SessionRole::Transmitter;
    SessionState state = SessionState::Idle;
    SessionError error;
    TransferMetadata metadata;
    TransmitterStats tx_stats;
    ReceiverStats rx_stats;
};

} // namespace Optical::State
