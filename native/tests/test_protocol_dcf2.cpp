#include "protocol_dcf2.hpp"
#include <iostream>
#include <cassert>
#include <cstring>

using namespace Optical::Core;

int main() {
    std::cout << "[TEST] Running Protocol DCF2 Container Test..." << std::endl;
    std::string filename = "important_data.pdf";
    std::string mime = "application/pdf";
    std::vector<uint8_t> raw_data(5000);
    for (size_t i = 0; i < raw_data.size(); ++i) raw_data[i] = static_cast<uint8_t>(i % 251);

    std::vector<uint8_t> container = Protocol::packContainer(filename, mime, raw_data.data(), raw_data.size());
    assert(container.size() > raw_data.size());

    auto unpacked = Protocol::unpackContainer(container.data(), container.size());
    assert(unpacked.has_value());
    assert(unpacked->name == filename);
    assert(unpacked->mime_type == mime);
    assert(unpacked->data.size() == raw_data.size());
    assert(std::memcmp(unpacked->data.data(), raw_data.data(), raw_data.size()) == 0);

    std::cout << "  -> PASSED: Container packing, unpacking & SHA-256 validation verified!" << std::endl;
    return 0;
}
