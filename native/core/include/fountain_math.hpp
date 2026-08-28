#pragma once

#include <cstdint>
#include <vector>
#include <cmath>
#include <algorithm>

namespace Optical::Core {

constexpr double LN2 = 0.6931471805599453;
constexpr double SOLITON_C = 0.1;
constexpr double SOLITON_DELTA = 0.5;

class FountainMath {
public:
    // Deterministic natural logarithm via series expansion
    static double dlog(double x);

    // Robust Soliton degree distribution CDF for K blocks
    static std::vector<double> solitonCdf(uint32_t k);

    // Deterministic frame seed from (session_id, seq)
    static uint32_t frameSeed(uint32_t session_id, uint32_t seq);

    // SplitMix32 pseudo-random generator state
    struct SplitMix32 {
        uint32_t state;
        explicit SplitMix32(uint32_t seed) : state(seed) {}
        uint32_t next();
    };

    // Computes deterministic source block indices for a given frame seq
    static std::vector<uint32_t> frameIndices(
        uint32_t k,
        const std::vector<double>& cdf,
        uint32_t session_id,
        uint32_t seq
    );

    // Fast XOR operation: dst ^= src
    static void xorBlocks(uint8_t* dst, const uint8_t* src, size_t len);
};

} // namespace Optical::Core
