#include "fountain_codec.hpp"
#include <random>
#include <cstring>
#include <algorithm>

namespace Optical::Core {

SystematicFountainEncoder::SystematicFountainEncoder(const uint8_t* data, size_t len, uint16_t target_symbol_size)
    : payload_(data, data + len),
      symbol_size_(target_symbol_size),
      current_seq_(0)
{
    if (payload_.empty()) {
        k_ = 1;
    } else {
        k_ = static_cast<uint32_t>((payload_.size() + symbol_size_ - 1) / symbol_size_);
        if (k_ == 0) k_ = 1;
    }

    std::random_device rd;
    std::mt19937 gen(rd());
    std::uniform_int_distribution<uint32_t> dis(1, 65535);
    session_id_ = dis(gen);

    cdf_ = FountainMath::solitonCdf(k_);
}

std::vector<uint8_t> SystematicFountainEncoder::nextFrame() {
    uint32_t seq = current_seq_++;

    std::vector<uint8_t> block_data(symbol_size_, 0);

    if (seq < k_) {
        // Systematic transmission: direct chunk of source payload
        size_t start = static_cast<size_t>(seq) * symbol_size_;
        size_t end = std::min(start + symbol_size_, payload_.size());
        if (start < payload_.size()) {
            std::memcpy(block_data.data(), payload_.data() + start, end - start);
        }
    } else {
        // Repair transmission: Soliton-selected linear combination (XOR)
        std::vector<uint32_t> indices = FountainMath::frameIndices(k_, cdf_, session_id_, seq);
        for (uint32_t idx : indices) {
            size_t start = static_cast<size_t>(idx) * symbol_size_;
            size_t end = std::min(start + symbol_size_, payload_.size());
            if (start < payload_.size()) {
                FountainMath::xorBlocks(block_data.data(), payload_.data() + start, end - start);
            }
        }
    }

    OtiFrameHeader header;
    header.magic = OTI_MAGIC;
    header.session_id = static_cast<uint16_t>(session_id_ & 0xFFFF);
    header.total_payload_bytes = static_cast<uint32_t>(payload_.size());
    header.symbol_size_bytes = symbol_size_;
    header.total_source_symbols = static_cast<uint16_t>(k_ & 0xFFFF);
    header.seq = static_cast<uint16_t>(seq & 0xFFFF);
    header.header_crc16 = 0;

    return Protocol::packFrame(header, block_data.data());
}

IncrementalPeelingDecoder::IncrementalPeelingDecoder() {
    reset();
}

void IncrementalPeelingDecoder::reset() {
    session_id_ = 0;
    k_ = 0;
    symbol_size_ = 0;
    total_payload_bytes_ = 0;
    cdf_.clear();
    solved_blocks_.clear();
    by_block_map_.clear();
    all_equations_.clear();
    seen_seqs_.clear();
    received_frames_count_ = 0;
}

DecoderResult IncrementalPeelingDecoder::consumeFrame(const uint8_t* frame_bytes, size_t len) {
    OtiFrameHeader header;
    const uint8_t* block_data = nullptr;
    if (!Protocol::parseFrame(frame_bytes, len, header, block_data)) {
        return DecoderResult::CorruptedSymbol;
    }

    // Reset if session salt changes
    if (session_id_ != 0 && (header.session_id != session_id_ || header.total_source_symbols != k_ || header.total_payload_bytes != total_payload_bytes_)) {
        reset();
    }

    if (session_id_ == 0) {
        session_id_ = header.session_id;
        k_ = header.total_source_symbols;
        symbol_size_ = header.symbol_size_bytes;
        total_payload_bytes_ = header.total_payload_bytes;
        cdf_ = FountainMath::solitonCdf(k_);
    }

    if (seen_seqs_.count(header.seq) > 0) {
        return DecoderResult::DuplicateSymbol;
    }
    seen_seqs_.insert(header.seq);
    received_frames_count_++;

    if (isComplete()) {
        return DecoderResult::Solved;
    }

    std::set<uint32_t> indices;
    if (header.seq < k_) {
        indices.insert(header.seq);
    } else {
        std::vector<uint32_t> ind_vec = FountainMath::frameIndices(k_, cdf_, session_id_, header.seq);
        indices.insert(ind_vec.begin(), ind_vec.end());
    }

    std::vector<uint8_t> current_payload(block_data, block_data + symbol_size_);

    // Eliminate already solved blocks
    auto it = indices.begin();
    while (it != indices.end()) {
        uint32_t b = *it;
        auto solved_it = solved_blocks_.find(b);
        if (solved_it != solved_blocks_.end()) {
            FountainMath::xorBlocks(current_payload.data(), solved_it->second.data(), symbol_size_);
            it = indices.erase(it);
        } else {
            ++it;
        }
    }

    if (indices.empty()) {
        return isComplete() ? DecoderResult::Solved : DecoderResult::SymbolIngested;
    }

    if (indices.size() == 1) {
        resolveBlock(*indices.begin(), current_payload);
    } else {
        auto eq = std::make_shared<PendingEquation>();
        eq->indices = std::move(indices);
        eq->data = std::move(current_payload);
        all_equations_.push_back(eq);

        for (uint32_t b : eq->indices) {
            by_block_map_[b].push_back(eq);
        }

        // Trigger sparse Gaussian elimination if we have enough symbols but peeling stalled
        if (received_frames_count_ >= k_ + 2 && !isComplete()) {
            runGaussianEliminationFallback();
        }
    }

    return isComplete() ? DecoderResult::Solved : DecoderResult::SymbolIngested;
}

void IncrementalPeelingDecoder::resolveBlock(uint32_t b0, const std::vector<uint8_t>& w0) {
    std::deque<std::pair<uint32_t, std::vector<uint8_t>>> queue;
    queue.push_back({b0, w0});

    while (!queue.empty()) {
        auto [b, w] = queue.front();
        queue.pop_front();

        if (solved_blocks_.count(b) > 0) continue;
        solved_blocks_[b] = w;

        auto waiting_it = by_block_map_.find(b);
        if (waiting_it == by_block_map_.end()) continue;

        auto waiting_list = std::move(waiting_it->second);
        by_block_map_.erase(waiting_it);

        for (auto& eq : waiting_list) {
            FountainMath::xorBlocks(eq->data.data(), w.data(), symbol_size_);
            eq->indices.erase(b);

            if (eq->indices.size() == 1) {
                uint32_t resolved_idx = *eq->indices.begin();
                if (solved_blocks_.count(resolved_idx) == 0) {
                    queue.push_back({resolved_idx, eq->data});
                }
            }
        }
    }
}

bool IncrementalPeelingDecoder::runGaussianEliminationFallback() {
    // Filter active unresolved equations
    std::vector<std::shared_ptr<PendingEquation>> active_eqs;
    for (auto& eq : all_equations_) {
        if (!eq->indices.empty()) {
            active_eqs.push_back(eq);
        }
    }

    if (active_eqs.empty()) return false;

    // Fast sparse elimination pass
    for (size_t i = 0; i < active_eqs.size(); ++i) {
        if (active_eqs[i]->indices.empty()) continue;
        uint32_t pivot = *active_eqs[i]->indices.begin();

        for (size_t j = i + 1; j < active_eqs.size(); ++j) {
            if (active_eqs[j]->indices.count(pivot) > 0) {
                // XOR row j with row i
                for (uint32_t idx : active_eqs[i]->indices) {
                    if (active_eqs[j]->indices.count(idx) > 0) {
                        active_eqs[j]->indices.erase(idx);
                    } else {
                        active_eqs[j]->indices.insert(idx);
                    }
                }
                FountainMath::xorBlocks(active_eqs[j]->data.data(), active_eqs[i]->data.data(), symbol_size_);

                if (active_eqs[j]->indices.size() == 1) {
                    resolveBlock(*active_eqs[j]->indices.begin(), active_eqs[j]->data);
                }
            }
        }
    }
    return isComplete();
}

bool IncrementalPeelingDecoder::isComplete() const {
    return k_ > 0 && solved_blocks_.size() >= k_;
}

DecoderStatus IncrementalPeelingDecoder::getStatus() const {
    DecoderStatus status;
    status.k = k_;
    status.solved_count = static_cast<uint32_t>(solved_blocks_.size());
    status.received_count = received_frames_count_;
    status.progress_percentage = (k_ > 0) ? (static_cast<float>(solved_blocks_.size()) / static_cast<float>(k_) * 100.0f) : 0.0f;
    status.is_complete = isComplete();
    status.session_id = session_id_;
    return status;
}

std::optional<std::vector<uint8_t>> IncrementalPeelingDecoder::assemblePayload() const {
    if (!isComplete()) return std::nullopt;

    std::vector<uint8_t> payload(total_payload_bytes_);
    for (uint32_t b = 0; b < k_; ++b) {
        auto it = solved_blocks_.find(b);
        if (it == solved_blocks_.end()) return std::nullopt;

        size_t start = static_cast<size_t>(b) * symbol_size_;
        size_t len = std::min(static_cast<size_t>(symbol_size_), total_payload_bytes_ - start);
        if (len > 0) {
            std::memcpy(payload.data() + start, it->second.data(), len);
        }
    }
    return payload;
}

} // namespace Optical::Core
