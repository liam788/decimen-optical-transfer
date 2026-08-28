# Step 4: Hardware Abstraction Layer (HAL) Contract

**Status:** ✅ Confirmed

---

## 1. Interface Definition

```cpp
struct CameraConfig {
    int preferred_width;        // e.g. 1280
    int preferred_height;       // e.g. 720
    int preferred_fps;          // e.g. 30
    bool prefer_fixed_exposure; // Lock exposure to eliminate motion blur
    CameraFacing facing;        // Defaults to Back on mobile
};

struct CameraFrame {
    const uint8_t* data;        // Borrowed pointer, valid only during callback
    int width;
    int height;
    int row_stride;             // Row stride including OS padding
    PixelFormat format;         // NV12, NV21, YUV_420_888, BGRA
    int64_t timestamp_us;
};

enum class CameraFacing { Back, Front };
enum class CameraError { None, PermissionDenied, DeviceUnavailable, DeviceInUse, Unsupported };

class ICameraProvider {
public:
    virtual ~ICameraProvider() = default;

    virtual CameraError startCameraStream(
        const CameraConfig& config,
        std::function<void(const CameraFrame&)> on_frame,
        std::function<void(CameraError)> on_error
    ) = 0;

    virtual void stopCameraStream() = 0;

    virtual bool isTorchSupported() const = 0;
    virtual bool setTorch(bool on) = 0;

    virtual bool hasMultipleCameras() const = 0;
    virtual bool switchCamera() = 0;
};
```

---

## 2. Buffer Lifecycle & Critical Rules
- **Non-Blocking Callback:** `on_frame` runs on the OS capture thread (e.g. `AVCaptureSession` queue, Android `HandlerThread`). It must return in **$< 0.3\text{ ms}$**.
- **No Format Conversion in HAL:** HAL passes native pixel buffers through unconverted. `zxing-cpp` reads the luminance plane (Y-plane) directly.
- **Screen-Out Scope:** The UI layer blits `QrBitmap` grids onto hardware-accelerated surfaces (Direct3D/Metal/OpenGL). Screen rendering is not part of the HAL.
