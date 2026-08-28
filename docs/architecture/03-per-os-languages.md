# Step 3: Per-OS Language & Technology Mapping

**Status:** ✅ Confirmed

---

## 1. Platform Technology Matrix

| Platform | Host UI Language | UI Framework | Interop Boundary | HAL Implementation |
|---|---|---|---|---|
| **Android** | Kotlin | Jetpack Compose | JNI / NDK (`extern "C"`) | NDK `ACameraManager` / `AImageReader` |
| **iOS / iPadOS** | Swift | SwiftUI | Swift C-Interop / Bridging Header | `AVFoundation` (`AVCaptureVideoDataOutput`) |
| **macOS** | Swift / Obj-C | SwiftUI / AppKit | Swift C-Interop | `AVFoundation` |
| **Windows** | C# / C++ | WinUI 3 / WPF | P/Invoke (`optical_transfer.dll`) | Windows Media Foundation (`IMFSourceReader`) |
| **Linux** | C++ / Python | Qt 6 / GTK 4 | Native C++ / `ctypes` | Video4Linux2 (`V4L2`) |

---

## 2. Interop Principles
1. **Zero JNI Overhead on Hot Paths:** Camera frames are captured in native C++ NDK code and fed directly into the State Layer queue without crossing into the Java/Kotlin runtime.
2. **Swift Direct Interop:** Swift imports the C header directly (`optical_transfer_c_api.h`) via SwiftPM or bridging header without manual wrapper boilerplate.
3. **C# P/Invoke:** Windows UI loads `optical_transfer.dll` and binds delegates to state callbacks.
