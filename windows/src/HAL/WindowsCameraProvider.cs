using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Media.Imaging;

namespace OpticalTransfer.HAL
{
    public class WindowsCameraProvider : ICameraProvider
    {
        // Windows Video for Windows (VFW / avicap32.dll) Native APIs
        [DllImport("avicap32.dll", EntryPoint = "capGetDriverDescriptionA")]
        private static extern bool capGetDriverDescription(
            short wDriverIndex,
            [MarshalAs(UnmanagedType.LPStr)] StringBuilder lpszName,
            int cbName,
            [MarshalAs(UnmanagedType.LPStr)] StringBuilder lpszVer,
            int cbVer
        );

        [DllImport("avicap32.dll", EntryPoint = "capCreateCaptureWindowA")]
        private static extern IntPtr capCreateCaptureWindow(
            string lpszWindowName,
            int dwStyle,
            int x, int y, int nWidth, int nHeight,
            IntPtr hWnd,
            int nID
        );

        [DllImport("user32.dll")]
        private static extern bool DestroyWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        private const uint WM_CAP_START = 0x400;
        private const uint WM_CAP_DRIVER_CONNECT = WM_CAP_START + 10;
        private const uint WM_CAP_DRIVER_DISCONNECT = WM_CAP_START + 11;
        private const uint WM_CAP_SET_PREVIEW = WM_CAP_START + 50;
        private const uint WM_CAP_SET_PREVIEWRATE = WM_CAP_START + 52;
        private const uint WM_CAP_GRAB_FRAME = WM_CAP_START + 60;
        private const uint WM_CAP_SET_CALLBACK_FRAME = WM_CAP_START + 5;

        private bool _isRunning;
        private IntPtr _hWndCap = IntPtr.Zero;
        private Action<CameraFrame> _onFrame;
        private Action<CameraError> _onError;
        private Action<WriteableBitmap> _onPreviewBitmap;
        private CameraConfig _config;
        private int _selectedDeviceIndex = 0;
        private Thread _workerThread;

        private List<string> _detectedDevices = new List<string>();

        public WindowsCameraProvider()
        {
            RefreshDevices();
        }

        public void SetPreviewCallback(Action<WriteableBitmap> onPreview)
        {
            _onPreviewBitmap = onPreview;
        }

        public void RefreshDevices()
        {
            _detectedDevices.Clear();
            StringBuilder name = new StringBuilder(256);
            StringBuilder ver = new StringBuilder(256);

            for (short i = 0; i < 10; i++)
            {
                if (capGetDriverDescription(i, name, 256, ver, 256))
                {
                    string devName = name.ToString().Trim();
                    if (!string.IsNullOrEmpty(devName))
                    {
                        _detectedDevices.Add(devName);
                    }
                }
            }
        }

        public CameraError StartCameraStream(
            CameraConfig config,
            Action<CameraFrame> onFrame,
            Action<CameraError> onError
        )
        {
            _config = config ?? new CameraConfig();
            _onFrame = onFrame;
            _onError = onError;

            RefreshDevices();
            if (_detectedDevices.Count == 0)
            {
                // No physical camera found
                if (_onError != null)
                {
                    _onError(CameraError.DeviceUnavailable);
                }
                return CameraError.DeviceUnavailable;
            }

            StopCameraStream();

            _isRunning = true;
            _workerThread = new Thread(CameraLoop)
            {
                IsBackground = true,
                Name = "WindowsWebcamWorker"
            };
            _workerThread.Start();

            return CameraError.None;
        }

        public void StopCameraStream()
        {
            _isRunning = false;
            if (_hWndCap != IntPtr.Zero)
            {
                SendMessage(_hWndCap, WM_CAP_DRIVER_DISCONNECT, IntPtr.Zero, IntPtr.Zero);
                DestroyWindow(_hWndCap);
                _hWndCap = IntPtr.Zero;
            }
            if (_workerThread != null && _workerThread.IsAlive)
            {
                _workerThread.Join(500);
            }
            _workerThread = null;
        }

        public bool IsTorchSupported() { return false; }
        public bool SetTorch(bool on) { return false; }
        public bool HasMultipleCameras() { return _detectedDevices.Count > 1; }

        public bool SwitchCamera()
        {
            if (_detectedDevices.Count <= 1) return false;
            _selectedDeviceIndex = (_selectedDeviceIndex + 1) % _detectedDevices.Count;
            if (_isRunning)
            {
                StartCameraStream(_config, _onFrame, _onError);
            }
            return true;
        }

        public string[] GetAvailableDevices()
        {
            RefreshDevices();
            return _detectedDevices.ToArray();
        }

        public void SelectDevice(int index)
        {
            if (index >= 0 && index < _detectedDevices.Count)
            {
                _selectedDeviceIndex = index;
                if (_isRunning)
                {
                    StartCameraStream(_config, _onFrame, _onError);
                }
            }
        }

        private void CameraLoop()
        {
            int width = _config.PreferredWidth > 0 ? _config.PreferredWidth : 640;
            int height = _config.PreferredHeight > 0 ? _config.PreferredHeight : 480;

            _hWndCap = capCreateCaptureWindow(
                "CaptureWindow",
                0,
                0, 0, width, height,
                IntPtr.Zero,
                0
            );

            if (_hWndCap == IntPtr.Zero)
            {
                if (_onError != null) _onError(CameraError.DeviceUnavailable);
                return;
            }

            IntPtr connectResult = SendMessage(_hWndCap, WM_CAP_DRIVER_CONNECT, (IntPtr)_selectedDeviceIndex, IntPtr.Zero);
            if (connectResult == IntPtr.Zero)
            {
                DestroyWindow(_hWndCap);
                _hWndCap = IntPtr.Zero;
                if (_onError != null) _onError(CameraError.DeviceUnavailable);
                return;
            }

            SendMessage(_hWndCap, WM_CAP_SET_PREVIEWRATE, (IntPtr)33, IntPtr.Zero); // ~30 FPS
            SendMessage(_hWndCap, WM_CAP_SET_PREVIEW, (IntPtr)1, IntPtr.Zero);

            Stopwatch sw = Stopwatch.StartNew();

            while (_isRunning)
            {
                long startMs = sw.ElapsedMilliseconds;

                // Grab single frame from device
                SendMessage(_hWndCap, WM_CAP_GRAB_FRAME, IntPtr.Zero, IntPtr.Zero);

                // Wait interval
                int targetFps = _config.PreferredFps > 0 ? _config.PreferredFps : 30;
                int intervalMs = 1000 / targetFps;
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
