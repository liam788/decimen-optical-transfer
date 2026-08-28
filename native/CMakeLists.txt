cmake_minimum_required(VERSION 3.15)
project(UniversalOpticalTransfer VERSION 1.0.0 LANGUAGES C CXX)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_POSITION_INDEPENDENT_CODE ON)

# Include directories
include_directories(
    core/include
    hal/include
    state/include
    interop/include
)

# 1. Core Computation Library
add_library(optical_core STATIC
    core/src/fountain_math.cpp
    core/src/protocol_dcf2.cpp
    core/src/fountain_codec.cpp
    core/src/qr_engine.cpp
)

# 2. HAL Library
add_library(optical_hal STATIC
    hal/src/mock_camera_provider.cpp
    hal/src/windows_camera_provider.cpp
)
target_link_libraries(optical_hal PUBLIC optical_core)

# 3. Application & State Layer Library
add_library(optical_state STATIC
    state/src/tx_session_controller.cpp
    state/src/rx_session_controller.cpp
    state/src/session_factory.cpp
)
target_link_libraries(optical_state PUBLIC optical_core optical_hal)

# 4. C-ABI Interop Export Shared Library
add_library(optical_transfer SHARED
    interop/src/optical_transfer_c_api.cpp
)
target_compile_definitions(optical_transfer PRIVATE OPTICAL_TRANSFER_EXPORTS)
target_link_libraries(optical_transfer PRIVATE optical_state optical_hal optical_core)

# 5. Unit Tests
add_executable(test_fountain_codec tests/test_fountain_codec.cpp)
target_link_libraries(test_fountain_codec PRIVATE optical_core)

add_executable(test_protocol_dcf2 tests/test_protocol_dcf2.cpp)
target_link_libraries(test_protocol_dcf2 PRIVATE optical_core)
