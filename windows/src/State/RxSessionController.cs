using System;
using System.Diagnostics;
using System.Threading;
using OpticalTransfer.Core;
using OpticalTransfer.HAL;

namespace OpticalTransfer.State
{
    public class RxSessionController : IDisposable
    {
        private class FrameSlot
        {
            public byte[] LuminanceBuffer;
            public int Width;
            public int Height;
            public int RowStride;
            public PixelFormat Format;
            public long TimestampUs;
        }

        private readonly object _lock = new object();
        private readonly object _queueLock = new object();
        private readonly ICameraProvider _cameraProvider;
        private readonly ISessionObserver _observer;
        private readonly IncrementalPeelingDecoder _decoder;

        private readonly FrameSlot[] _slotPool = new FrameSlot[2]
        {
            new FrameSlot { LuminanceBuffer = new byte[1280 * 720] },
            new FrameSlot { LuminanceBuffer = new byte[1280 * 720] }
        };

        private int _writeSlotIdx = 0;
        private int _readSlotIdx = 0;
        private bool _hasUnreadFrame = false;
        private readonly AutoResetEvent _queueSignal = new AutoResetEvent(false);

        private bool _isRunning;
        private bool _isPaused;
        private Thread _workerThread;
        private Stopwatch _sessionTimer;
        private SessionSnapshot _snapshot;

        public RxSessionController(ICameraProvider cameraProvider, ISessionObserver observer)
        {
            _cameraProvider = cameraProvider;
            _observer = observer;
            _decoder = new IncrementalPeelingDecoder();
            _snapshot = new SessionSnapshot
            {
                Role = SessionRole.Receiver,
                State = SessionState.Idle
            };
        }

        public bool Start(CameraConfig cameraConfig = null)
        {
            lock (_lock)
            {
                _decoder.Reset();
                _snapshot.State = SessionState.Configuring;
                _sessionTimer = Stopwatch.StartNew();

                _isRunning = true;
                _isPaused = false;
                _hasUnreadFrame = false;

                _workerThread = new Thread(WorkerDecodeLoop)
                {
                    IsBackground = true,
                    Name = "RxDecodeWorker"
                };
                _workerThread.Start();

                if (_cameraProvider != null)
                {
                    CameraError err = _cameraProvider.StartCameraStream(
                        cameraConfig ?? new CameraConfig(),
                        frame => HandleIncomingCameraFrame(frame),
                        error =>
                        {
                            lock (_lock)
                            {
                                _snapshot.State = SessionState.Failed;
                                _snapshot.Error.Code = SessionErrorCode.CameraStreamFailed;
                                _snapshot.Error.Message = "Camera Stream Error";
                                if (_observer != null) _observer.OnStateChanged(SessionState.Failed, _snapshot.Error);
                            }
                        }
                    );

                    if (err != CameraError.None)
                    {
                        _snapshot.State = SessionState.Failed;
                        _snapshot.Error.Code = SessionErrorCode.CameraDeviceUnavailable;
                        if (_observer != null) _observer.OnStateChanged(SessionState.Failed, _snapshot.Error);
                        return false;
                    }
                }

                _snapshot.State = SessionState.Transferring;
                if (_observer != null) _observer.OnStateChanged(SessionState.Transferring, _snapshot.Error);
                return true;
            }
        }

        public void Pause()
        {
            _isPaused = true;
            lock (_lock)
            {
                _snapshot.State = SessionState.Paused;
                if (_observer != null) _observer.OnStateChanged(SessionState.Paused, _snapshot.Error);
            }
        }

        public void Resume()
        {
            _isPaused = false;
            lock (_lock)
            {
                _snapshot.State = SessionState.Transferring;
                if (_observer != null) _observer.OnStateChanged(SessionState.Transferring, _snapshot.Error);
            }
        }

        public void Cancel()
        {
            _isRunning = false;
            _queueSignal.Set();

            if (_cameraProvider != null)
            {
                _cameraProvider.StopCameraStream();
            }
            if (_workerThread != null && _workerThread.IsAlive)
            {
                _workerThread.Join(500);
            }

            lock (_lock)
            {
                _snapshot.State = SessionState.Cancelled;
                if (_observer != null) _observer.OnStateChanged(SessionState.Cancelled, _snapshot.Error);
            }
        }

        public void IngestRawFrameBytes(byte[] frameBytes)
        {
            if (!_isRunning || _isPaused || frameBytes == null) return;

            lock (_lock)
            {
                _snapshot.RxStats.RawFramesReceived++;
                _snapshot.RxStats.QrDecodedCount++;
            }

            DecoderResult res = _decoder.ConsumeFrame(frameBytes);
            UpdateTelemetry();

            if (_decoder.IsComplete)
            {
                CheckAndFinalize();
            }
        }

        private void HandleIncomingCameraFrame(CameraFrame frame)
        {
            if (!_isRunning || _isPaused || frame.Data == null) return;

            // Hot path on OS capture thread: Copy luminance buffer in <0.3ms
            int wIdx = _writeSlotIdx;
            FrameSlot slot = _slotPool[wIdx];

            int planeBytes = frame.Height * frame.RowStride;
            if (slot.LuminanceBuffer.Length < planeBytes)
            {
                slot.LuminanceBuffer = new byte[planeBytes];
            }

            Buffer.BlockCopy(frame.Data, 0, slot.LuminanceBuffer, 0, planeBytes);
            slot.Width = frame.Width;
            slot.Height = frame.Height;
            slot.RowStride = frame.RowStride;
            slot.Format = frame.Format;
            slot.TimestampUs = frame.TimestampUs;

            // Atomically swap write/read slots
            _readSlotIdx = wIdx;
            _writeSlotIdx = 1 - wIdx;

            if (_hasUnreadFrame)
            {
                lock (_lock)
                {
                    _snapshot.RxStats.FramesDroppedQueue++;
                }
            }

            _hasUnreadFrame = true;
            _queueSignal.Set();
        }

        private void WorkerDecodeLoop()
        {
            while (_isRunning)
            {
                _queueSignal.WaitOne(100);
                if (!_isRunning) break;

                if (!_hasUnreadFrame) continue;

                FrameSlot slot;
                lock (_queueLock)
                {
                    _hasUnreadFrame = false;
                    slot = _slotPool[_readSlotIdx];
                }

                lock (_lock)
                {
                    _snapshot.RxStats.RawFramesReceived++;
                }

                // Decode QR from luminance
                byte[] decodedBytes = QrMatrixGenerator.DecodeFromLuminance(
                    slot.LuminanceBuffer,
                    slot.Width,
                    slot.Height,
                    slot.RowStride
                );

                if (decodedBytes == null || decodedBytes.Length == 0)
                {
                    lock (_lock)
                    {
                        _snapshot.RxStats.QrDecodeFailures++;
                    }
                    continue;
                }

                lock (_lock)
                {
                    _snapshot.RxStats.QrDecodedCount++;
                }

                _decoder.ConsumeFrame(decodedBytes);
                UpdateTelemetry();

                if (_decoder.IsComplete)
                {
                    CheckAndFinalize();
                    break;
                }
            }
        }

        private void UpdateTelemetry()
        {
            DecoderStatus dStatus = _decoder.GetStatus();
            lock (_lock)
            {
                _snapshot.RxStats.SymbolsRequired = dStatus.K;
                _snapshot.RxStats.CurrentRank = dStatus.SolvedCount;
                _snapshot.RxStats.ProgressPercentage = dStatus.ProgressPercentage;

                if (_sessionTimer != null)
                {
                    long elapsedMs = _sessionTimer.ElapsedMilliseconds;
                    _snapshot.RxStats.ElapsedDurationMs = elapsedMs;

                    if (elapsedMs > 0)
                    {
                        float elapsedSec = (float)elapsedMs / 1000.0f;
                        _snapshot.RxStats.InstantFps = (float)_snapshot.RxStats.RawFramesReceived / elapsedSec;
                        _snapshot.RxStats.DecodeFps = (float)_snapshot.RxStats.QrDecodedCount / elapsedSec;
                        _snapshot.RxStats.GoodputKbps = ((float)(dStatus.SolvedCount * 300) / 1024.0f) / elapsedSec;
                    }
                }
            }

            if (_observer != null)
            {
                _observer.OnProgressUpdated(GetSnapshot());
            }
        }

        private void CheckAndFinalize()
        {
            byte[] assembled = _decoder.AssemblePayload();
            if (assembled != null)
            {
                OpticalFile file = ProtocolDcf2.UnpackContainer(assembled);
                if (file != null)
                {
                    lock (_lock)
                    {
                        _snapshot.State = SessionState.Completed;
                        _snapshot.Metadata.FileName = file.Name;
                        _snapshot.Metadata.MimeType = file.MimeType;
                        _snapshot.Metadata.FileSizeBytes = file.Data.Length;
                        _snapshot.Metadata.Sha256 = file.Sha256;
                    }

                    if (_observer != null)
                    {
                        _observer.OnStateChanged(SessionState.Completed, _snapshot.Error);
                        _observer.OnTransferCompleted(file.Data, _snapshot.Metadata);
                    }
                }
            }
        }

        public SessionSnapshot GetSnapshot()
        {
            lock (_lock)
            {
                return new SessionSnapshot
                {
                    Role = _snapshot.Role,
                    State = _snapshot.State,
                    Error = _snapshot.Error,
                    Metadata = _snapshot.Metadata,
                    TxStats = _snapshot.TxStats,
                    RxStats = _snapshot.RxStats
                };
            }
        }

        public void Dispose()
        {
            Cancel();
            _queueSignal.Dispose();
        }
    }
}
