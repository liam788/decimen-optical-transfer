#pragma once

#include <stdint.h>
#include <stddef.h>

#ifdef _WIN32
  #ifdef OPTICAL_TRANSFER_EXPORTS
    #define OPTICAL_API __declspec(dllexport)
  #else
    #define OPTICAL_API __declspec(dllimport)
  #endif
#else
  #define OPTICAL_API __attribute__((visibility("default")))
#endif

#ifdef __cplusplus
extern "C" {
#endif

// Opaque Session Handles
typedef void* OpticalTxHandle;
typedef void* OpticalRxHandle;

// C-ABI Callbacks
typedef void (*StateChangedCallback)(int state, int error_code, const char* error_msg, void* user_data);
typedef void (*ProgressCallback)(float progress_pct, float fps, float kbps, void* user_data);
typedef void (*FrameReadyCallback)(const uint8_t* modules, int module_count, void* user_data);
typedef void (*CompleteCallback)(const uint8_t* data, size_t len, const char* filename, void* user_data);

// --- Transmitter APIs ---
OPTICAL_API OpticalTxHandle Optical_Tx_Create(
    const uint8_t* data,
    size_t len,
    const char* filename,
    float fps,
    StateChangedCallback on_state,
    ProgressCallback on_progress,
    FrameReadyCallback on_frame,
    void* user_data
);

OPTICAL_API void Optical_Tx_SetFps(OpticalTxHandle handle, float fps);
OPTICAL_API void Optical_Tx_Pause(OpticalTxHandle handle);
OPTICAL_API void Optical_Tx_Resume(OpticalTxHandle handle);
OPTICAL_API void Optical_Tx_Cancel(OpticalTxHandle handle);
OPTICAL_API int  Optical_Tx_GetNextFrame(OpticalTxHandle handle, uint8_t* out_modules, int* out_size);
OPTICAL_API void Optical_Tx_Destroy(OpticalTxHandle handle);

// --- Receiver APIs ---
OPTICAL_API OpticalRxHandle Optical_Rx_Create(
    StateChangedCallback on_state,
    ProgressCallback on_progress,
    CompleteCallback on_complete,
    void* user_data
);

OPTICAL_API int  Optical_Rx_Start(OpticalRxHandle handle, int camera_facing);
OPTICAL_API void Optical_Rx_Pause(OpticalRxHandle handle);
OPTICAL_API void Optical_Rx_Resume(OpticalRxHandle handle);
OPTICAL_API void Optical_Rx_Cancel(OpticalRxHandle handle);

OPTICAL_API int  Optical_Rx_HasMultipleCameras(OpticalRxHandle handle);
OPTICAL_API int  Optical_Rx_SwitchCamera(OpticalRxHandle handle);
OPTICAL_API int  Optical_Rx_IsTorchSupported(OpticalRxHandle handle);
OPTICAL_API int  Optical_Rx_ToggleTorch(OpticalRxHandle handle, int enable);

OPTICAL_API void Optical_Rx_Destroy(OpticalRxHandle handle);

#ifdef __cplusplus
}
#endif
