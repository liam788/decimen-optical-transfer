using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace OpticalTransfer.HAL
{
    public class WindowsCameraProvider : ICameraProvider
    {
        private bool _isRunning;
        private Thread _captureThread;
        private Action<CameraFrame> _onFrame;
        private Action<CameraError> _onError;
        private CameraConfig _config;
        private bool _isTorchOn;
        private int _selectedDeviceIndex;
        private readonly List<string> _devices = new List<string>
        {
            "Integrated Webcam (HD 720p 30FPS)",
            "External Optical Capture Cam (1080p 60FPS)",
            "Direct Simulated Optical Stream"
        };

        public CameraError StartCameraStream(
            CameraConfig config,
            Action<CameraFrame> onFrame,
            Action<CameraError> onError
        )
        {
            _config = config ?? new CameraConfig();
            _onFrame = onFrame;
            _onError = onError;

            if (_isRunning)
            {
                StopCameraStream();
            }

            _isRunning = true;
            _captureThread = new Thread(CaptureLoop)
            {
                IsBackground = true,
                Name = "WindowsCameraCaptureThread",
                Priority = ThreadPriority.AboveNormal
            };
            _captureThread.Start();

            return CameraError.None;
        }

        public void StopCameraStream()
        {
            _isRunning = false;
            if (_captureThread != null && _captureThread.IsAlive)
            {
                _captureThread.Join(500);
            }
            _captureThread = null;
        }

        public bool IsTorchSupported()
        {
            return true;
        }

        public bool SetTorch(bool on)
        {
            _isTorchOn = on;
            return true;
        }

        public bool HasMultipleCameras()
        {
            return _devices.Count > 1;
        }

        public bool SwitchCamera()
        {
            _selectedDeviceIndex = (_selectedDeviceIndex + 1) % _devices.Count;
            return true;
        }

        public string[] GetAvailableDevices()
        {
            return _devices.ToArray();
        }

        public void SelectDevice(int index)
        {
            if (index >= 0 && index < _devices.Count)
            {
                _selectedDeviceIndex = index;
            }
        }

        private void CaptureLoop()
        {
            int width = _config.PreferredWidth > 0 ? _config.PreferredWidth : 1280;
            int height = _config.PreferredHeight > 0 ? _config.PreferredHeight : 720;
            int stride = width;
            byte[] frameBuffer = new byte[width * height];
            Stopwatch sw = Stopwatch.StartNew();

            int targetFps = _config.PreferredFps > 0 ? _config.PreferredFps : 30;
            int intervalMs = 1000 / targetFps;

            while (_isRunning)
            {
                long startMs = sw.ElapsedMilliseconds;

                // Deliver frame on native capture thread: Must be non-blocking (<0.3ms copy budget)
                if (_onFrame != null)
                {
                    CameraFrame frame = new CameraFrame
                    {
                        Data = frameBuffer,
                        Width = width,
                        Height = height,
                        RowStride = stride,
                        Format = PixelFormat.Gray8,
                        TimestampUs = sw.ElapsedMilliseconds * 1000
                    };
                    _onFrame(frame);
                }

                long elapsed = sw.ElapsedMilliseconds - startMs;
                int sleepMs = (int)(intervalMs - elapsed);
                if (sleepMs > 0)
                {
                    Thread.Sleep(sleepMs);
                }
            }
        }

        public void Dispose()
        {
            StopCameraStream();
        }
    }
}
