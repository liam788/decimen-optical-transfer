# Step 1: Core Language Decision

**Status:** ✅ Confirmed  
**Decision:** **C++ (C++17 / C++20 Standard)**

---

## 1. Context & Requirements
Optical data transfer requires continuous processing of high-framerate camera streams (30–60 FPS), real-time QR detection/decoding via computer vision algorithms, and algebraic matrix calculations for fountain code erasure recovery.

Under these workloads, runtime characteristics are critical:
- **Frame Processing Budget:** 16.6 ms (at 60 FPS) to 33.3 ms (at 30 FPS).
- **GC Overhead:** Unacceptable. Garbage-collection pauses in managed runtimes (Java/Kotlin, C#, JS) create bursty frame drops and buffer exhaustion on OS capture threads.
- **Cross-Platform Target:** Must run with identical binary behavior on Android, iOS, iPadOS, macOS, Windows, and Linux.

---

## 2. Decision Rationale

### 2.1 Deterministic Memory & Execution
C++ provides explicit, deterministic allocation and deallocation. By utilizing pre-allocated double-buffered frame pools (~900 KB for 720p luminance) and zero-allocation hot paths, the application eliminates latency spikes and capture thread starvation.

### 2.2 Universal Interoperability (FFI / ABI)
C++ with an `extern "C"` export boundary is the lowest common denominator across all operating systems:
- **Android:** Direct binding via JNI or NDK (`NativeActivity` / C++ helper).
- **Apple (iOS/macOS):** Direct Swift C-interop via bridging headers or Swift Package Manager.
- **Windows:** Zero-overhead C# P/Invoke or C++/WinRT.
- **Linux:** Native C++ / Qt 6 / GTK.

### 2.3 Single Source of Truth
Encoding (Soliton distribution + PRNG + QR matrix generation) and decoding (Luminance extraction + ZXing decode + Incremental peeling solver) execute byte-identically across all 5 OSes, eliminating subtle platform-specific floating point or rounding discrepancies.

### 2.4 Hardware Vectorization
Direct access to SIMD intrinsics (ARM NEON for mobile, AVX2/SSE for x86) enables accelerated word-aligned XOR operations and rapid luminance extraction.
