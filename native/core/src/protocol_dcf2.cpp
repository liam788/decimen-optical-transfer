#include "protocol_dcf2.hpp"
#include <cstring>
#include <sstream>
#include <iomanip>

namespace Optical::Core {

// Simple SHA-256 implementation
namespace {
    inline uint32_t rotr(uint32_t x, uint32_t n) { return (x >> n) | (x << (32 - n)); }
    inline uint32_t ch(uint32_t x, uint32_t y, uint32_t z) { return (x & y) ^ (~x & z); }
    inline uint32_t maj(uint32_t x, uint32_t y, uint32_t z) { return (x & y) ^ (x & z) ^ (y & z); }
    inline uint32_t sig0(uint32_t x) { return rotr(x, 2) ^ rotr(x, 13) ^ rotr(x, 22); }
    inline uint32_t sig1(uint32_t x) { return rotr(x, 6) ^ rotr(x, 11) ^ rotr(x, 25); }
    inline uint32_t gam0(uint32_t x) { return rotr(x, 7) ^ rotr(x, 18) ^ (x >> 3); }
    inline uint32_t gam1(uint32_t x) { return rotr(x, 17) ^ rotr(x, 19) ^ (x >> 10); }

    const uint32_t K256[64] = {
        0x428a2f98, 0x71374491, 0xb5c0fbcf, 0xe9b5dba5, 0x3956c25b, 0x59f111f1, 0x923f82a4, 0xab1c5ed5,
        0xd807aa98, 0x12835b01, 0x243185be, 0x550c7dc3, 0x72be5d74, 0x80deb1fe, 0x9bdc06a7, 0xc19bf174,
        0xe49b69c1, 0xefbe4786, 0x0fc19dc6, 0x240ca1cc, 0x2de92c6f, 0x4a7484aa, 0x5cb0a9dc, 0x76f988da,
        0x983e5152, 0xa831c66d, 0xb00327c8, 0xbf597fc7, 0xc6e00bf3, 0xd5a79147, 0x06ca6351, 0x14292967,
        0x27b70a85, 0x2e1b2138, 0x4d2c6dfc, 0x53380d13, 0x650a7354, 0x766a0abb, 0x81c2c92e, 0x92722c85,
        0xa2bfe8a1, 0xa81a664b, 0xc24b8b70, 0xc76c51a3, 0xd192e819, 0xd6990624, 0xf40e3585, 0x106aa070,
        0x19a4c116, 0x1e376c08, 0x2748774c, 0x34b0bcb5, 0x391c0cb3, 0x4ed8aa4a, 0x5b9cca4f, 0x682e6ff3,
        0x748f82ee, 0x78a5636f, 0x84c87814, 0x8cc70208, 0x90befffa, 0xa4506ceb, 0xbef9a3f7, 0xc67178f2
    };
}

void Protocol::sha256(const uint8_t* data, size_t len, uint8_t out[32]) {
    uint32_t h[8] = {
        0x6a09e667, 0xbb67ae85, 0x3c6ef372, 0xa54ff53a,
        0x510e527f, 0x9b05688c, 0x1f83d9ab, 0x5be0cd19
    };

    uint64_t total_bits = len * 8ULL;
    std::vector<uint8_t> padded(data, data + len);
    padded.push_back(0x80);
    while ((padded.size() % 64) != 56) {
        padded.push_back(0x00);
    }
    for (int i = 7; i >= 0; --i) {
        padded.push_back(static_cast<uint8_t>((total_bits >> (i * 8)) & 0xFF));
    }

    for (size_t chunk = 0; chunk < padded.size(); chunk += 64) {
        uint32_t w[64];
        for (int i = 0; i < 16; ++i) {
            w[i] = (static_cast<uint32_t>(padded[chunk + i * 4]) << 24) |
                   (static_cast<uint32_t>(padded[chunk + i * 4 + 1]) << 16) |
                   (static_cast<uint32_t>(padded[chunk + i * 4 + 2]) << 8) |
                   (static_cast<uint32_t>(padded[chunk + i * 4 + 3]));
        }
        for (int i = 16; i < 64; ++i) {
            w[i] = gam1(w[i - 2]) + w[i - 7] + gam0(w[i - 15]) + w[i - 16];
        }

        uint32_t a = h[0], b = h[1], c = h[2], d = h[3];
        uint32_t e = h[4], f = h[5], g = h[6], hv = h[7];

        for (int i = 0; i < 64; ++i) {
            uint32_t t1 = hv + sig1(e) + ch(e, f, g) + K256[i] + w[i];
            uint32_t t2 = sig0(a) + maj(a, b, c);
            hv = g; g = f; f = e; e = d + t1;
            d = c; c = b; b = a; a = t1 + t2;
        }

        h[0] += a; h[1] += b; h[2] += c; h[3] += d;
        h[4] += e; h[5] += f; h[6] += g; h[7] += hv;
    }

    for (int i = 0; i < 8; ++i) {
        out[i * 4]     = static_cast<uint8_t>((h[i] >> 24) & 0xFF);
        out[i * 4 + 1] = static_cast<uint8_t>((h[i] >> 16) & 0xFF);
        out[i * 4 + 2] = static_cast<uint8_t>((h[i] >> 8) & 0xFF);
        out[i * 4 + 3] = static_cast<uint8_t>(h[i] & 0xFF);
    }
}

uint32_t Protocol::fnv1a(const uint8_t* data, size_t len) {
    uint32_t h = 0x811C9DC5u;
    for (size_t i = 0; i < len; ++i) {
        h ^= data[i];
        h *= 0x01000193u;
    }
    return h;
}

uint16_t Protocol::crc16(const uint8_t* data, size_t len) {
    uint16_t crc = 0xFFFF;
    for (size_t i = 0; i < len; ++i) {
        crc ^= (static_cast<uint16_t>(data[i]) << 8);
        for (int j = 0; j < 8; ++j) {
            if (crc & 0x8000) {
                crc = (crc << 1) ^ 0x1021;
            } else {
                crc <<= 1;
            }
        }
    }
    return crc;
}

uint32_t Protocol::crc32(const uint8_t* data, size_t len) {
    uint32_t crc = 0xFFFFFFFFu;
    for (size_t i = 0; i < len; ++i) {
        crc ^= data[i];
        for (int j = 0; j < 8; ++j) {
            crc = (crc >> 1) ^ (0xEDB88320u & (-(crc & 1)));
        }
    }
    return ~crc;
}

std::string Protocol::sanitizeFileName(const std::string& name) {
    std::string cleaned;
    size_t last_slash = name.find_last_of("/\\");
    std::string base = (last_slash == std::string::npos) ? name : name.substr(last_slash + 1);
    for (char c : base) {
        if (static_cast<unsigned char>(c) >= 32 && c != 127) {
            cleaned.push_back(c);
        }
    }
    if (cleaned.empty() || cleaned == "." || cleaned == "..") {
        return "transfer.bin";
    }
    return cleaned;
}

std::vector<uint8_t> Protocol::packFrame(const OtiFrameHeader& header, const uint8_t* block_data) {
    std::vector<uint8_t> out(OTI_HEADER_LEN + header.symbol_size_bytes);
    OtiFrameHeader h = header;
    h.magic = OTI_MAGIC;
    h.header_crc16 = 0;
    
    // Calculate CRC16 of first 14 bytes
    h.header_crc16 = crc16(reinterpret_cast<const uint8_t*>(&h), 14);
    
    std::memcpy(out.data(), &h, OTI_HEADER_LEN);
    if (block_data && header.symbol_size_bytes > 0) {
        std::memcpy(out.data() + OTI_HEADER_LEN, block_data, header.symbol_size_bytes);
    }
    return out;
}

bool Protocol::parseFrame(const uint8_t* frame_bytes, size_t frame_len, OtiFrameHeader& out_header, const uint8_t*& out_block_ptr) {
    if (!frame_bytes || frame_len < OTI_HEADER_LEN) return false;
    std::memcpy(&out_header, frame_bytes, OTI_HEADER_LEN);
    if (out_header.magic != OTI_MAGIC) return false;
    if (out_header.total_source_symbols == 0 || out_header.symbol_size_bytes == 0) return false;
    
    uint16_t expected_crc = out_header.header_crc16;
    uint16_t computed_crc = crc16(frame_bytes, 14);
    if (expected_crc != computed_crc) return false;

    if (frame_len < OTI_HEADER_LEN + out_header.symbol_size_bytes) return false;
    out_block_ptr = frame_bytes + OTI_HEADER_LEN;
    return true;
}

std::vector<uint8_t> Protocol::packContainer(
    const std::string& name,
    const std::string& mime_type,
    const uint8_t* data,
    size_t len
) {
    std::string safe_name = sanitizeFileName(name);
    std::string safe_type = mime_type.empty() ? "application/octet-stream" : mime_type;

    uint16_t name_len = static_cast<uint16_t>(safe_name.size());
    uint16_t type_len = static_cast<uint16_t>(safe_type.size());
    uint32_t orig_size = static_cast<uint32_t>(len);
    uint32_t transmitted_size = orig_size;

    uint8_t digest[32];
    sha256(data, len, digest);

    std::vector<uint8_t> container(DCF2_HEADER_LEN + name_len + type_len + transmitted_size);
    std::memcpy(container.data(), DCF2_MAGIC, 4);
    
    container[4] = 0; // use_gzip = false (uncompressed direct stream)
    *reinterpret_cast<uint16_t*>(container.data() + 5) = name_len;
    *reinterpret_cast<uint16_t*>(container.data() + 7) = type_len;
    *reinterpret_cast<uint32_t*>(container.data() + 9) = orig_size;
    *reinterpret_cast<uint32_t*>(container.data() + 13) = transmitted_size;
    std::memcpy(container.data() + 17, digest, 32);

    size_t offset = DCF2_HEADER_LEN;
    std::memcpy(container.data() + offset, safe_name.data(), name_len);
    offset += name_len;
    std::memcpy(container.data() + offset, safe_type.data(), type_len);
    offset += type_len;
    if (len > 0) {
        std::memcpy(container.data() + offset, data, len);
    }
    return container;
}

std::optional<OpticalFile> Protocol::unpackContainer(const uint8_t* container_bytes, size_t len) {
    if (!container_bytes || len < DCF2_HEADER_LEN) return std::nullopt;
    if (std::memcmp(container_bytes, DCF2_MAGIC, 4) != 0) return std::nullopt;

    bool is_gzipped = (container_bytes[4] == 1);
    uint16_t name_len = *reinterpret_cast<const uint16_t*>(container_bytes + 5);
    uint16_t type_len = *reinterpret_cast<const uint16_t*>(container_bytes + 7);
    uint32_t orig_size = *reinterpret_cast<const uint32_t*>(container_bytes + 9);
    uint32_t trans_size = *reinterpret_cast<const uint32_t*>(container_bytes + 13);

    const uint8_t* expected_sha = container_bytes + 17;
    size_t expected_total = DCF2_HEADER_LEN + name_len + type_len + trans_size;
    if (len < expected_total) return std::nullopt;

    size_t offset = DCF2_HEADER_LEN;
    std::string name(reinterpret_cast<const char*>(container_bytes + offset), name_len);
    offset += name_len;
    std::string mime(reinterpret_cast<const char*>(container_bytes + offset), type_len);
    offset += type_len;

    std::vector<uint8_t> payload(container_bytes + offset, container_bytes + offset + trans_size);

    uint8_t actual_sha[32];
    sha256(payload.data(), payload.size(), actual_sha);
    if (std::memcmp(actual_sha, expected_sha, 32) != 0) {
        return std::nullopt;
    }

    OpticalFile file;
    file.name = sanitizeFileName(name);
    file.mime_type = mime;
    file.data = std::move(payload);
    std::memcpy(file.sha256, actual_sha, 32);
    file.is_gzipped = is_gzipped;
    file.original_size = orig_size;
    return file;
}

} // namespace Optical::Core
