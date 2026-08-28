using System;
using System.Diagnostics;
using System.Threading;
using OpticalTransfer.Core;

namespace OpticalTransfer.State
{
    public class TxSessionController : IDisposable
    {
        private readonly object _lock = new object();
        private readonly object _stageLock = new object();
        private readonly ISessionObserver _observer;

        private SystematicFountainEncoder _encoder;
        private QrBitmap _activeFrame;
        private QrBitmap _stagedFrame;
        private bool _hasStagedFrame;

        private float _targetFps = 20.0f;
        private bool _isRunning;
        private bool _isPaused;

        private Thread _pregenThread;
        private Thread _pushPacingThread;
        private readonly AutoResetEvent _stageSignal = new AutoResetEvent(false);

        private Stopwatch _sessionTimer;
        private long _lastAdvanceUs;
        private SessionSnapshot _snapshot;

        public TxSessionController(ISessionObserver observer)
        {
            _observer = observer;
            _snapshot = new SessionSnapshot
            {
                Role = SessionRole.Transmitter,
                State = SessionState.Idle
            };
        }

        public bool Start(byte[] fileBytes, string fileName, float fpsPreset = 20.0f)
        {
            lock (_lock)
            {
                if (fileBytes == null || fileBytes.Length == 0) return false;

                // Pack into DCF2 Container
                byte[] container = ProtocolDcf2.PackContainer(fileName, "", fileBytes);
                _encoder = new SystematicFountainEncoder(container, 300);

                _targetFps = (fpsPreset > 0.0f) ? fpsPreset : 20.0f;
                _snapshot.Metadata.FileName = fileName;
                _snapshot.Metadata.FileSizeBytes = fileBytes.Length;
                _snapshot.Metadata.TotalSourceSymbols = (ushort)_encoder.K;
                _snapshot.TxStats.TargetFps = _targetFps;
                _snapshot.State = SessionState.Transferring;

                _sessionTimer = Stopwatch.StartNew();
                _lastAdvanceUs = _sessionTimer.ElapsedTicks * 1000000 / Stopwatch.Frequency;

                // Generate initial frame synchronously for immediate display
                byte[] initialPacket = _encoder.NextFrame();
                _activeFrame = QrMatrixGenerator.Encode(initialPacket);
                _snapshot.TxStats.SymbolsEmitted = 1;
                _snapshot.TxStats.CurrentFrameIndex = 1;

                _isRunning = true;
                _isPaused = false;
                _hasStagedFrame = false;

                _pregenThread = new Thread(PregenerateLoop)
                {
                    IsBackground = true,
                    Name = "TxPregenWorker"
                };
                _pregenThread.Start();

                _pushPacingThread = new Thread(PushPacingLoop)
                {
                    IsBackground = true,
                    Name = "TxPacingWorker"
                };
                _pushPacingThread.Start();

                if (_observer != null)
                {
                    _observer.OnStateChanged(SessionState.Transferring, _snapshot.Error);
                    _observer.OnFrameReady(_activeFrame);
                }
                return true;
            }
        }

        public void SetFps(float fps)
        {
            if (fps > 0.0f)
            {
                _targetFps = fps;
                lock (_lock)
                {
                    _snapshot.TxStats.TargetFps = fps;
                }
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
            _stageSignal.Set();

            if (_pregenThread != null && _pregenThread.IsAlive) _pregenThread.Join(500);
            if (_pushPacingThread != null && _pushPacingThread.IsAlive) _pushPacingThread.Join(500);

            lock (_lock)
            {
                _snapshot.State = SessionState.Cancelled;
                if (_observer != null) _observer.OnStateChanged(SessionState.Cancelled, _snapshot.Error);
            }
        }

        private void PregenerateLoop()
        {
            while (_isRunning)
            {
                if (_hasStagedFrame)
                {
                    _stageSignal.WaitOne(100);
                    if (!_isRunning) break;
                    continue;
                }

                byte[] nextPacket = null;
                lock (_lock)
                {
                    if (_encoder != null)
                    {
                        nextPacket = _encoder.NextFrame();
                    }
                }

                if (nextPacket != null && nextPacket.Length > 0)
                {
                    QrBitmap nextQr = QrMatrixGenerator.Encode(nextPacket);
                    lock (_stageLock)
                    {
                        _stagedFrame = nextQr;
                        _hasStagedFrame = true;
                    }
                }
            }
        }

        private void PushPacingLoop()
        {
            while (_isRunning)
            {
                float fps = _targetFps;
                int intervalMs = (fps > 0.0f) ? (int)(1000.0f / fps) : 50;
                Thread.Sleep(Math.Max(5, intervalMs));

                if (!_isRunning || _isPaused) continue;

                QrBitmap frame = GetNextFrame();
                if (_observer != null)
                {
                    _observer.OnFrameReady(frame);
                    _observer.OnProgressUpdated(GetSnapshot());
                }
            }
        }

        public QrBitmap GetNextFrame()
        {
            lock (_lock)
            {
                if (!_isRunning || _isPaused)
                {
                    return _activeFrame;
                }

                long nowUs = _sessionTimer.ElapsedTicks * 1000000 / Stopwatch.Frequency;
                long elapsedUs = nowUs - _lastAdvanceUs;
                long targetIntervalUs = (long)(1000000.0f / _targetFps);

                // Cadence gating: return active frame if called earlier than target interval
                if (elapsedUs < targetIntervalUs)
                {
                    return _activeFrame;
                }

                // Interval met: swap in staged frame
                if (_hasStagedFrame)
                {
                    lock (_stageLock)
                    {
                        _activeFrame = _stagedFrame;
                        _hasStagedFrame = false;
                        _stageSignal.Set(); // Signal worker for next frame
                    }

                    _lastAdvanceUs = nowUs;
                    _snapshot.TxStats.SymbolsEmitted++;
                    _snapshot.TxStats.CurrentFrameIndex++;

                    long totalElapsedMs = _sessionTimer.ElapsedMilliseconds;
                    _snapshot.TxStats.ElapsedDurationMs = totalElapsedMs;
                    if (totalElapsedMs > 0)
                    {
                        _snapshot.TxStats.ActualFps = (float)_snapshot.TxStats.SymbolsEmitted / ((float)totalElapsedMs / 1000.0f);
                    }
                }

                return _activeFrame;
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
            _stageSignal.Dispose();
        }
    }
}
