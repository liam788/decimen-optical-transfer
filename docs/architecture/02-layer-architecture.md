# Step 2: 4-Layer Architecture Design

**Status:** ✅ Confirmed

---

## 1. Architectural Diagram

```
+-------------------------------------------------------------+
| Layer 1: Native UI / Presentation Layer                     |
| (Swift / Kotlin / C# / WinUI / Direct3D / Qt)               |
+------------------------------+------------------------------+
                               | (State updates, Commands)
+------------------------------v------------------------------+
| Layer 2: Application & State Layer (C++)                    |
| - TxSessionController & RxSessionController                 |
| - 1-Ahead TX Pre-generation Worker & VSync Cadence Gating   |
| - 2-Slot RX Dropping Queue & Worker Thread                  |
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

## 2. Layer Responsibilities

### Layer 1: Native UI / Presentation Layer
- Renders camera preview feed, alignment HUD, goodput meters, and progress bars.
- Hosts user interactions (file picker, camera facing toggle, torch switch, FPS preset picker).
- Driven natively in platform-idiomatic languages (SwiftUI, Jetpack Compose, WinUI 3, Qt).

### Layer 2: Application & State Layer (C++)
- Coordinates transmission and reception sessions.
- Manages thread isolation: background TX pre-generation worker and dedicated RX decoding worker.
- Implements 2-slot dropping queue ("latest frame wins") and VSync cadence gating.
- Computes real-time telemetry (instant FPS, decode FPS, goodput KB/s, rank %, ETA).
- Exposes a unified C-ABI export surface.

### Layer 3: Hardware Abstraction Layer / HAL (C++)
- Narrowly scoped: camera capture input and torch output only.
- Delivers raw, unconverted image buffers via borrowed pointers (`<0.3ms` copy budget).
- Does NOT perform QR decoding or filesystem operations.

### Layer 4: Core Computation Layer (C++)
- Pure, testable algorithmic computation.
- Systematic Fountain Codec (Soliton PRNG, systematic source blocks, repair droplets, peeling/GE solver).
- Protocol packing/unpacking (16-byte OTI packet headers, DCF2 containers, SHA-256 integrity).
- QR code matrix generation and ZXing barcode detection.
