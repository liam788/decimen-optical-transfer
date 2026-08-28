#include "fountain_codec.hpp"
#include <iostream>
#include <vector>
#include <cassert>
#include <random>

using namespace Optical::Core;

void test_clean_roundtrip() {
    std::cout << "[TEST] Running Clean Roundtrip Test..." << std::endl;
    std::vector<uint8_t> payload(10000); // 10 KB
    for (size_t i = 0; i < payload.size(); ++i) payload[i] = static_cast<uint8_t>(i & 0xFF);

    SystematicFountainEncoder encoder(payload.data(), payload.size(), 300);
    IncrementalPeelingDecoder decoder;

    uint32_t k = encoder.getK();
    std::cout << "  Payload: " << payload.size() << " B, K = " << k << " blocks" << std::endl;

    for (uint32_t i = 0; i < k; ++i) {
        std::vector<uint8_t> frame = encoder.nextFrame();
        decoder.consumeFrame(frame.data(), frame.size());
    }

    assert(decoder.isComplete());
    auto assembled = decoder.assemblePayload();
    assert(assembled.has_value());
    assert(assembled->size() == payload.size());
    assert(std::memcmp(assembled->data(), payload.data(), payload.size()) == 0);
    std::cout << "  -> PASSED: Clean roundtrip exact byte match!" << std::endl;
}

void test_erasure_recovery(float drop_rate) {
    std::cout << "[TEST] Running Erasure Recovery Test (Drop Rate: " << (drop_rate * 100) << "%)..." << std::endl;
    std::vector<uint8_t> payload(25000); // 25 KB
    for (size_t i = 0; i < payload.size(); ++i) payload[i] = static_cast<uint8_t>((i * 7 + 3) & 0xFF);

    SystematicFountainEncoder encoder(payload.data(), payload.size(), 300);
    IncrementalPeelingDecoder decoder;

    uint32_t k = encoder.getK();
    std::mt19937 rng(42);
    std::uniform_real_distribution<float> dist(0.0f, 1.0f);

    uint32_t transmitted = 0;
    uint32_t received = 0;

    while (!decoder.isComplete() && transmitted < k * 5) {
        std::vector<uint8_t> frame = encoder.nextFrame();
        transmitted++;

        if (dist(rng) >= drop_rate) {
            decoder.consumeFrame(frame.data(), frame.size());
            received++;
        }
    }

    assert(decoder.isComplete());
    auto assembled = decoder.assemblePayload();
    assert(assembled.has_value());
    assert(std::memcmp(assembled->data(), payload.data(), payload.size()) == 0);
    std::cout << "  -> PASSED: Recovered " << payload.size() << " B after " << transmitted << " frames (" << received << " received, K=" << k << ")!" << std::endl;
}

int main() {
    test_clean_roundtrip();
    test_erasure_recovery(0.10f); // 10% packet loss
    test_erasure_recovery(0.25f); // 25% packet loss
    test_erasure_recovery(0.40f); // 40% packet loss
    std::cout << "\nALL CODEC TESTS PASSED SUCCESSFULLY!" << std::endl;
    return 0;
}
