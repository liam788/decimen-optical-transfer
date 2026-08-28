#pragma once

#include "protocol_dcf2.hpp"
#include "fountain_math.hpp"
#include <cstdint>
#include <vector>
#include <map>
#include <set>
#include <deque>
#include <memory>

namespace Optical::Core {

enum class DecoderResult {
    NeedMoreSymbols,
    SymbolIngested,
    DuplicateSymbol,
    Solved,
    CorruptedSymbol
};

struct DecoderStatus {
    uint32_t k = 0;
    uint32_t solved_count = 0;
    uint32_t received_count = 0;
    float progress_percentage = 0.0f;
    bool is_complete = false;
    uint32_t session_id = 0;
};

class SystematicFountainEncoder {
public:
    SystematicFountainEncoder(const uint8_t* data, size_t len, uint16_t target_symbol_size = 300);

    // Advances to next ESI and returns packed 16B OTI frame
    std::vector<uint8_t> nextFrame();

    uint32_t getK() const { return k_; }
    uint16_t getSymbolSize() const { return symbol_size_; }
    uint32_t getSessionId() const { return session_id_; }
    uint32_t getTotalBytes() const { return static_cast<uint32_t>(payload_.size()); }
    uint32_t getCurrentSeq() const { return current_seq_; }

private:
    std::vector<uint8_t> payload_;
    uint32_t k_;
    uint16_t symbol_size_;
    uint32_t session_id_;
    std::vector<double> cdf_;
    uint32_t current_seq_;
};

class IncrementalPeelingDecoder {
public:
    IncrementalPeelingDecoder();
    void reset();

    // Consumes a raw frame (with 16B OTI header)
    DecoderResult consumeFrame(const uint8_t* frame_bytes, size_t len);

    bool isComplete() const;
    DecoderStatus getStatus() const;
    std::optional<std::vector<uint8_t>> assemblePayload() const;

private:
    struct PendingEquation {
        std::set<uint32_t> indices;
        std::vector<uint8_t> data;
    };

    void resolveBlock(uint32_t block_index, const std::vector<uint8_t>& data);
    bool runGaussianEliminationFallback();

    uint32_t session_id_;
    uint32_t k_;
    uint16_t symbol_size_;
    uint32_t total_payload_bytes_;
    std::vector<double> cdf_;

    std::map<uint32_t, std::vector<uint8_t>> solved_blocks_;
    std::map<uint32_t, std::vector<std::shared_ptr<PendingEquation>>> by_block_map_;
    std::vector<std::shared_ptr<PendingEquation>> all_equations_;
    std::set<uint32_t> seen_seqs_;
    uint32_t received_frames_count_;
};

} // namespace Optical::Core
