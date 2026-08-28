using System;
using OpticalTransfer.Core;

namespace OpticalTransfer.State
{
    public enum SessionState
    {
        Idle,
        Configuring,
        Transferring,
        Paused,
        Completed,
        Failed,
        Cancelled
    }

    public enum SessionRole
    {
        Transmitter,
        Receiver
    }

    public enum SessionErrorCode
    {
        None,
        CameraPermissionDenied,
        CameraDeviceUnavailable,
        CameraStreamFailed,
        InvalidPayload,
        DecoderTimeout,
        IntegrityVerificationFailed
    }

    public class SessionError
    {
        public SessionErrorCode Code { get; set; }
        public string Message { get; set; }

        public SessionError()
        {
            Code = SessionErrorCode.None;
            Message = "";
        }
    }

    public class FileMetadata
    {
        public string FileName { get; set; }
        public string MimeType { get; set; }
        public long FileSizeBytes { get; set; }
        public ushort TotalSourceSymbols { get; set; }
        public byte[] Sha256 { get; set; }

        public FileMetadata()
        {
            FileName = "";
            MimeType = "";
            Sha256 = new byte[32];
        }
    }

    public class TxStats
    {
        public float TargetFps { get; set; }
        public float ActualFps { get; set; }
        public uint SymbolsEmitted { get; set; }
        public uint CurrentFrameIndex { get; set; }
        public long ElapsedDurationMs { get; set; }

        public TxStats()
        {
            TargetFps = 20.0f;
        }
    }

    public class RxStats
    {
        public float InstantFps { get; set; }
        public float DecodeFps { get; set; }
        public float GoodputKbps { get; set; }
        public uint RawFramesReceived { get; set; }
        public uint QrDecodedCount { get; set; }
        public uint QrDecodeFailures { get; set; }
        public uint FramesDroppedQueue { get; set; }
        public uint CurrentRank { get; set; }
        public uint SymbolsRequired { get; set; }
        public float ProgressPercentage { get; set; }
        public long ElapsedDurationMs { get; set; }
    }

    public class SessionSnapshot
    {
        public SessionRole Role { get; set; }
        public SessionState State { get; set; }
        public SessionError Error { get; set; }
        public FileMetadata Metadata { get; set; }
        public TxStats TxStats { get; set; }
        public RxStats RxStats { get; set; }

        public SessionSnapshot()
        {
            State = SessionState.Idle;
            Error = new SessionError();
            Metadata = new FileMetadata();
            TxStats = new TxStats();
            RxStats = new RxStats();
        }
    }

    public interface ISessionObserver
    {
        void OnStateChanged(SessionState state, SessionError error);
        void OnProgressUpdated(SessionSnapshot snapshot);
        void OnFrameReady(QrBitmap frame);
        void OnTransferCompleted(byte[] payload, FileMetadata metadata);
    }
}
