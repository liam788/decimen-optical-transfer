# Step 5: Application & State Layer Design

**Status:** ✅ Confirmed

---

## 1. Transmitter Pipeline (1-Ahead Pre-Generation & VSync Decoupling)

```
[120Hz UI VSync]  --Tick 1 (0ms)-----> [Interval Met: Advance to Frame 1] -> Signals Worker for Frame 2
                  --Tick 2 (8.3ms)---> [Gate: Return Frame 1]
                  --Tick 3 (16.6ms)--> [Gate: Return Frame 1]
                  --Tick 4 (25.0ms)--> [Gate: Return Frame 1]
                  --Tick 5 (33.3ms)--> [Gate: Return Frame 1]
                  --Tick 6 (41.6ms)--> [Gate: Return Frame 1]
                  --Tick 7 (50.0ms)--> [Interval Met: Advance to Frame 2] -> Signals Worker for Frame 3
```

- **Cadence Gating:** `getNextFrame()` gates on `(now - last_advance) >= 1.0 / target_fps`. High-refresh displays (90Hz/120Hz) do not over-cycle frames.
- **Ping-Pong Buffer:** While the UI displays frame $N$, the background worker pre-generates frame $N+1$ off the UI thread.

---

## 2. Receiver Pipeline (2-Slot Dropping Queue & Peeling Solver)

```
[OS Camera Capture Thread]
   │
   │ 1. Borrowed CameraFrame (Y-Plane luminance)
   ▼
[2-Slot Dropping Queue] (Pre-allocated ~921 KB each; zero alloc during capture)
   │
   │ 2. Worker pops latest; skips intermediate stale frames
   ▼
[RX Worker Thread]
   │
   ├──> zxing-cpp ImageView (wraps reusable buffer, 0 alloc)
   │       │
   │       └──> Raw Droplet Bytes (316 bytes: 16B OTI Header + 300B Payload)
   │
   ├──> ESI Dedup Filter (Bitset check: is symbol already in solver?)
   │       │
   │       └──> IFountainDecoder (incremental peeling solver + equation bank)
   ▼
[Reconstructed Payload Buffer] (Allocated once upon metadata parsing)
```

- **2-Slot Queue:** Atomic swap between reader and writer slots ensures the camera thread never blocks and decode always processes the newest frame.
- **Single-Block Ceiling:** Payloads $\le 300\text{ KB}$ are encoded in a single block ($K \le 1000$ at $T=300\text{ bytes}$). Payloads $> 300\text{ KB}$ partition into $150\text{ KB}$ blocks ($K=500$).

---

## 3. C-ABI Interop Surface

```c
// Transmitter
OPTICAL_API OpticalTxHandle Optical_Tx_Create(const uint8_t* data, size_t len, const char* name, float fps, ...);
OPTICAL_API int Optical_Tx_GetNextFrame(OpticalTxHandle handle, uint8_t* out_modules, int* out_size);
OPTICAL_API void Optical_Tx_Destroy(OpticalTxHandle handle);

// Receiver
OPTICAL_API OpticalRxHandle Optical_Rx_Create(StateChangedCallback on_state, ProgressCallback on_progress, CompleteCallback on_complete, void* user_data);
OPTICAL_API int Optical_Rx_Start(OpticalRxHandle handle, int camera_facing);
OPTICAL_API int Optical_Rx_ToggleTorch(OpticalRxHandle handle, int enable);
OPTICAL_API void Optical_Rx_Destroy(OpticalRxHandle handle);
```
