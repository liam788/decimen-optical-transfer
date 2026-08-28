using System;

namespace OpticalTransfer.HAL
{
    public enum CameraFacing
    {
        Back,
        Front,
        External
    }

    public enum PixelFormat
    {
        NV12,
        NV21,
        YUV420,
        BGRA32,
        Gray8
    }

    public enum CameraError
    {
        None,
        PermissionDenied,
        DeviceUnavailable,
        DeviceInUse,
        Unsupported
    }

    public class CameraConfig
    {
        public int PreferredWidth { get; set; }
        public int PreferredHeight { get; set; }
        public int PreferredFps { get; set; }
        public bool PreferFixedExposure { get; set; }
        public CameraFacing Facing { get; set; }

        public CameraConfig()
        {
            PreferredWidth = 1280;
            PreferredHeight = 720;
            PreferredFps = 30;
            PreferFixedExposure = true;
            Facing = CameraFacing.Back;
        }
    }

    public struct CameraFrame
    {
        public byte[] Data;
        public int Width;
        public int Height;
        public int RowStride;
        public PixelFormat Format;
        public long TimestampUs;
    }

    public interface ICameraProvider : IDisposable
    {
        CameraError StartCameraStream(
            CameraConfig config,
            Action<CameraFrame> onFrame,
            Action<CameraError> onError
        );

        void StopCameraStream();
        bool IsTorchSupported();
        bool SetTorch(bool on);
        bool HasMultipleCameras();
        bool SwitchCamera();
        string[] GetAvailableDevices();
        void SelectDevice(int index);
    }
}
