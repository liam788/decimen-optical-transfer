# Universal Optical Transfer (Fountain-Coded Visual Data Stream)

[![C++17](https://img.shields.io/badge/C%2B%2B-17%2F20-blue.svg)](native/)
[![Architecture](https://img.shields.io/badge/Architecture-4--Layer-green.svg)](docs/architecture/)
[![Status](https://img.shields.io/badge/Verification-100%25%20Passing-brightgreen.svg)](scripts/verify.py)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Transfers files between devices (Android, iOS, Windows, macOS, Linux) with **zero network connection, zero cloud servers, zero Bluetooth/Wi-Fi pairing**, using nothing but **a screen and a camera**.

---

## 🌟 Architectural Overview

```
+-------------------------------------------------------------+
| Layer 1: Native UI / Presentation Layer                     |
| (SwiftUI / Jetpack Compose / WinUI 3 / Direct3D / Qt 6)     |
+------------------------------+------------------------------+
                               | (State updates, Commands)
+------------------------------v------------------------------+
| Layer 2: Application & State Layer (C++)                    |
| - TxSessionController & RxSessionController                 |
| - 1-Ahead TX Pre-generation Worker & VSync Cadence Gating   |
| - 2-Slot RX Dropping Queue & Dedicated Worker Thread        |
| - ESI Deduplication, Progress & Telemetry Engine            |
+---------------+-----------------------------+---------------+
                |                             |
  (Raw Frames)  |                             | (Encode/Decode)
+---------------v-------------+ +-------------v---------------+
| Layer 3: Hardware           | | Layer 4: Core Computation   |
| Abstraction Layer (HAL)     | | Layer (C++)                 |
| - ICameraProvider           | | - Systematic Fountain Codec |
| - Torch & Camera Switching  | | - QR Matrix Generator       |
| - Borrowed Frame Delivery   | | - ZXing Decode Wrapper      |
+-----------------------------+ +-----------------------------+
```

---

## 📁 Repository Structure

| Directory | Purpose & Contents |
|---|---|
| [`native/`](native/) | **Universal C++17 library:** Core algorithms (Fountain codec, DCF2 protocol, QR engine), HAL interfaces, State Layer session controllers, and C-ABI export bindings (`optical_transfer.dll` / `.so` / `.dylib`). |
| [`docs/architecture/`](docs/architecture/) | **Master Architecture Specifications (Steps 1–5):** Complete rationale, layer designs, platform mappings, HAL contracts, and state models. |
| [`scripts/`](scripts/) | **Verification & Automation Tools:** Headless simulation test suite (`verify.py`) testing Soliton math, erasure tolerance, VSync gating, and DCF2 checksums. |
| [`flutter_optical_transfer/`](flutter_optical_transfer/) | Universal cross-platform Flutter application (Android, Windows, macOS, iOS). |
| [`android/`](android/) | Standalone native Android implementation in Kotlin & Jetpack Compose. |
| [`decimen-optical-transfer/`](decimen-optical-transfer/) | Core Web TypeScript & WebAssembly reference implementation. |
| [`released/`](released/) | Prebuilt standalone single-file HTML distributions (`decimen-sender.html` and `decimen-receiver.html`). |

---

## 🚀 Key Technical Features

1. **Systematic Fountain Codec:** Transmits original source packets directly ($ESI \in [0, K-1]$), followed by Soliton-distributed pseudo-random linear repair droplets ($ESI \ge K$).
2. **VSync Cadence Gating:** Decouples 60Hz/120Hz display refresh loops from the target 20 FPS transmission cycle rate, eliminating frame over-cycling and stutter.
3. **2-Slot Atomic Dropping Queue:** Camera capture threads copy luminance into a pre-allocated pool in $< 0.3\text{ ms}$, ensuring the capture pipeline never stalls and the decoder always processes the newest frame.
4. **Self-Describing OTI Header:** Every frame embeds a 16-byte fixed binary header, enabling receivers to hot-plug and begin decoding mid-stream.
5. **Universal C-ABI Interoperability:** Clean `extern "C"` surface binding into Swift (iOS/macOS), Kotlin/JNI (Android), and C# P/Invoke (Windows).

---

## 📥 Direct Downloads

| Platform | Package | Download Link | Notes |
|---|---|---|---|
| 🪟 **Windows App** | `decimen-windows.zip` | [📥 Download Windows App](https://github.com/liam788/decimen-optical-transfer/releases/latest/download/decimen-windows.zip) | 100% In-App Receiver (Extract and run `.exe`) |
| 🌐 **Web Sender** | `decimen-sender.html` | [📥 Download Web Sender](https://github.com/liam788/decimen-optical-transfer/releases/latest/download/decimen-sender.html) | Single-file zero-install offline HTML |
| 🌐 **Web Receiver** | `decimen-receiver.html` | [📥 Download Web Receiver](https://github.com/liam788/decimen-optical-transfer/releases/latest/download/decimen-receiver.html) | Inlined ZXing WASM scanner |
| 🤖 **Android** | `app-release.apk` | [📥 Download Android APK](https://github.com/liam788/decimen-optical-transfer/releases/latest/download/app-release.apk) | Direct APK sideload for Android 5.0+ |

See the full distribution catalog in [`released/README.md`](released/README.md).

---


## 🛠️ Quick Start & Verification

### Running the Verification Test Suite
```bash
python scripts/verify.py
```

### Building the Native C++ Library with CMake
```bash
cd native
mkdir build && cd build
cmake .. -DCMAKE_BUILD_TYPE=Release
cmake --build . --config Release
```

---

## 📚 Architecture Documentation

- [01-core-language.md](docs/architecture/01-core-language.md) — Step 1: C++17/20 Core Language Decision
- [02-layer-architecture.md](docs/architecture/02-layer-architecture.md) — Step 2: 4-Layer System Architecture
- [03-per-os-languages.md](docs/architecture/03-per-os-languages.md) — Step 3: Per-OS Language & Technology Mapping
- [04-hal-contract.md](docs/architecture/04-hal-contract.md) — Step 4: Hardware Abstraction Layer (HAL) Contract
- [05-state-layer.md](docs/architecture/05-state-layer.md) — Step 5: Application & State Layer Specification
- [Architecture Index](docs/architecture/README.md) — Master Architecture Guide
