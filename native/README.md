# Universal Optical Transfer — Native C++ Library

This directory contains the universal, high-performance C++ Core, Hardware Abstraction Layer (HAL), Application & State Layer, and C-ABI export bindings.

---

## 📁 Directory Layout

```
native/
├── CMakeLists.txt                         # Root CMake build configuration
├── README.md                              # This document
├── core/                                  # Pure C++ computational core
│   ├── include/
│   │   ├── fountain_math.hpp              # Soliton CDF, PRNG, and word-aligned XOR math
│   │   ├── fountain_codec.hpp             # Systematic encoder & peeling/GE decoder
│   │   ├── protocol_dcf2.hpp              # DCF2 container & 16-byte OTI packet framing
│   │   └── qr_engine.hpp                  # QR matrix generation & ZXing decode wrapper
│   └── src/
│       ├── fountain_math.cpp
│       ├── fountain_codec.cpp
│       ├── protocol_dcf2.cpp
│       └── qr_engine.cpp
├── hal/                                   # Hardware Abstraction Layer
│   ├── include/
│   │   └── hal_camera.hpp                 # ICameraProvider, CameraFrame, CameraConfig
│   └── src/
│       ├── mock_camera_provider.cpp       # Synthetic camera for unit & CI testing
│       └── windows_camera_provider.cpp    # Windows Media Foundation implementation
├── state/                                 # Application & State Layer
│   ├── include/
│   │   ├── session_models.hpp             # State enums, telemetry, metrics, snapshots
│   │   └── session_interfaces.hpp         # ISessionObserver, ITransmitSession, IReceiveSession
│   └── src/
│       ├── tx_session_controller.hpp
│       ├── tx_session_controller.cpp      # 1-ahead TX worker & vsync cadence gating
│       ├── rx_session_controller.hpp
│       ├── rx_session_controller.cpp      # 2-slot dropping queue & RX decode worker
│       └── session_factory.cpp            # Top-level dependency injector
├── interop/                               # C-ABI boundary for Native UI host bindings
│   ├── include/
│   │   └── optical_transfer_c_api.h       # Plain C header (Swift / JNI / PInvoke friendly)
│   └── src/
│       └── optical_transfer_c_api.cpp     # C wrapper implementation
└── tests/                                 # Automated unit & end-to-end test suite
    ├── test_fountain_codec.cpp            # Codec correctness & erasure tolerance tests
    └── test_protocol_dcf2.cpp             # Container packing, GZIP, and SHA-256 tests
```

---

## 🛠️ Building with CMake

```bash
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --config Release
```

### Generated CMake Targets:
- `optical_core` (Static Library)
- `optical_hal` (Static Library)
- `optical_state` (Static Library)
- `optical_transfer` (Shared Library with `OPTICAL_API` exports)
- `test_fountain_codec` (Unit test executable)
- `test_protocol_dcf2` (Unit test executable)

---

## 🔌 Interoperability & Bindings

The C header in [`interop/include/optical_transfer_c_api.h`](interop/include/optical_transfer_c_api.h) can be directly imported or bridged into:
- **Android (Kotlin):** JNI via `System.loadLibrary("optical_transfer")`
- **iOS/macOS (Swift):** Direct C-interop via bridging header or Swift Package Manager
- **Windows (C#):** P/Invoke `[DllImport("optical_transfer.dll")]`
- **Linux (C++/Python):** Native C++ linking or Python `ctypes`
