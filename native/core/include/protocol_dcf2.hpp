#pragma once

#include <cstdint>
#include <string>
#include <vector>
#include <optional>

namespace Optical::Core {

constexpr uint16_t OTI_MAGIC = 0x4454; // 'DT' (Decimen Transfer)
constexpr size_t OTI_HEADER_LEN = 16;
constexpr size_t DCF2_HEADER_LEN = 49;
constexpr uint8_t DCF2_MAGIC[4] = {'D', 'C', 'F', '2'};

#pragma pack(push, 1)
struct OtiFrameHeader {
    uint16_t magic;                // 0x4454
    uint16_t session_id;           // Random session identifier
    uint32_t total_payload_bytes;  // Full file/container length
    uint16_t symbol_size_bytes;    // T (e.g. 300)
    uint16_t total_source_symbols; // K
    uint16_t seq;                  // ESI: 0..K-1 systematic, >= K repair
    uint16_t header_crc16;         // Protects header integrity
};
#pragma pack(pop)

struct OpticalFile {
    std::string name;
    std::string mime_type;
    std::vector<uint8_t> data;
    uint8_t sha256[32];
    bool is_gzipped;
    uint32_t original_size;
};

class Protocol {
public:
    // FNV-1a 32-bit hash
    static uint32_t fnv1a(const uint8_t* data, size_t len);

    // CRC16 and CRC32
    static uint16_t crc16(const uint8_t* data, size_t len);
    static uint32_t crc32(const uint8_t* data, size_t len);

    // SHA-256 digest computation
    static void sha256(const uint8_t* data, size_t len, uint8_t out[32]);

    // Sanitize filename
    static std::string sanitizeFileName(const std::string& name);

    // Frame packing and parsing
    static std::vector<uint8_t> packFrame(const OtiFrameHeader& header, const uint8_t* block_data);
    static bool parseFrame(const uint8_t* frame_bytes, size_t frame_len, OtiFrameHeader& out_header, const uint8_t*& out_block_ptr);

    // DCF2 Container packaging and unpackaging
    static std::vector<uint8_t> packContainer(
        const std::string& name,
        const std::string& mime_type,
        const uint8_t* data,
        size_t len
    );
    static std::optional<OpticalFile> unpackContainer(const uint8_t* container_bytes, size_t len);
};

} // namespace Optical::Core
