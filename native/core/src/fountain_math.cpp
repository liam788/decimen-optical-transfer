#include "fountain_math.hpp"
#include <unordered_set>

namespace Optical::Core {

double FountainMath::dlog(double x) {
    int e = 0;
    double m = x;
    while (m >= 1.5) {
        m /= 2.0;
        e++;
    }
    while (m < 0.75) {
        m *= 2.0;
        e--;
    }
    double z = (m - 1.0) / (m + 1.0);
    double z2 = z * z;
    double term = z;
    double sum = 0.0;
    int n = 1;
    while (n <= 21) {
        sum += term / static_cast<double>(n);
        term *= z2;
        n += 2;
    }
    return static_cast<double>(e) * LN2 + 2.0 * sum;
}

std::vector<double> FountainMath::solitonCdf(uint32_t k) {
    if (k == 0) k = 1;
    std::vector<double> cdf(k, 0.0);
    if (k == 1) {
        cdf[0] = 1.0;
        return cdf;
    }

    double dk = static_cast<double>(k);
    double R = std::max(1.0, SOLITON_C * dlog(dk / SOLITON_DELTA) * std::sqrt(dk));
    uint32_t spike = std::min(k, static_cast<uint32_t>(std::ceil(dk / R)));
    double total = 0.0;

    for (uint32_t d = 1; d <= k; ++d) {
        double dd = static_cast<double>(d);
        double rho = (d == 1) ? (1.0 / dk) : (1.0 / (dd * (dd - 1.0)));
        double tau = 0.0;
        if (d < spike) {
            tau = R / (dd * dk);
        } else if (d == spike) {
            tau = (R * std::max(0.0, dlog(R / SOLITON_DELTA))) / dk;
        }
        total += rho + tau;
        cdf[d - 1] = total;
    }

    for (uint32_t i = 0; i < k; ++i) {
        cdf[i] /= total;
    }
    cdf[k - 1] = 1.0;
    return cdf;
}

uint32_t FountainMath::frameSeed(uint32_t session_id, uint32_t seq) {
    uint32_t h = ((session_id + 1) * 0x9E3779B1u) ^ (seq + 0x85EBCA6Bu);
    h = (h ^ (h >> 13)) * 0xC2B2AE35u;
    return h ^ (h >> 16);
}

uint32_t FountainMath::SplitMix32::next() {
    state += 0x9E3779B9u;
    uint32_t t = state ^ (state >> 16);
    t *= 0x21F0AAADu;
    t ^= (t >> 15);
    t *= 0x735A2D97u;
    t ^= (t >> 15);
    return t;
}

std::vector<uint32_t> FountainMath::frameIndices(
    uint32_t k,
    const std::vector<double>& cdf,
    uint32_t session_id,
    uint32_t seq
) {
    SplitMix32 rnd(frameSeed(session_id, seq));
    double u = static_cast<double>(rnd.next()) * 2.3283064365386963e-10; // 2^-32

    uint32_t lo = 0;
    uint32_t hi = k - 1;
    while (lo < hi) {
        uint32_t mid = (lo + hi) >> 1;
        if (cdf[mid] >= u) {
            hi = mid;
        } else {
            lo = mid + 1;
        }
    }
    uint32_t d = std::min(k, lo + 1);

    if (d > (k >> 3)) {
        std::vector<uint32_t> scratch(k);
        for (uint32_t i = 0; i < k; ++i) scratch[i] = i;
        std::vector<uint32_t> out(d);
        for (uint32_t i = 0; i < d; ++i) {
            uint32_t rem = k - i;
            uint32_t pick = rnd.next() % rem;
            uint32_t j = i + pick;
            std::swap(scratch[i], scratch[j]);
            out[i] = scratch[i];
        }
        return out;
    }

    std::unordered_set<uint32_t> picked;
    std::vector<uint32_t> out;
    out.reserve(d);
    while (out.size() < d) {
        uint32_t pick = rnd.next() % k;
        if (picked.insert(pick).second) {
            out.push_back(pick);
        }
    }
    return out;
}

void FountainMath::xorBlocks(uint8_t* dst, const uint8_t* src, size_t len) {
    size_t i = 0;
    // Word-sized XOR optimization
    while (i + sizeof(uint64_t) <= len) {
        uint64_t* d64 = reinterpret_cast<uint64_t*>(dst + i);
        const uint64_t* s64 = reinterpret_cast<const uint64_t*>(src + i);
        *d64 ^= *s64;
        i += sizeof(uint64_t);
    }
    while (i < len) {
        dst[i] ^= src[i];
        ++i;
    }
}

} // namespace Optical::Core
