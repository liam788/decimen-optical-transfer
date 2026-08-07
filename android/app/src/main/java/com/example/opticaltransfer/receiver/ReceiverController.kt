package com.example.opticaltransfer.receiver

import android.content.Context
import com.example.opticaltransfer.core.codec.QrMatrixEncoder
import com.example.opticaltransfer.core.fountain.DecoderStatus
import com.example.opticaltransfer.core.fountain.FountainDecoder
import com.example.opticaltransfer.platform.ApkInstaller
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow
import java.io.File

data class ReceiverState(
    val isScanning: Boolean = false,
    val totalBlocks: Int = 0,
    val solvedBlocks: Int = 0,
    val dropsReceived: Int = 0,
    val progressPercent: Float = 0f,
    val goodputKbps: Float = 0f,
    val fileName: String = "",
    val fileSize: Long = 0L,
    val isCompleted: Boolean = false,
    val savedFile: File? = null,
    val isApk: Boolean = false,
    val error: String? = null
)

class ReceiverController {
    private val _state = MutableStateFlow(ReceiverState())
    val state: StateFlow<ReceiverState> = _state.asStateFlow()

    private val fountainDecoder = FountainDecoder()
    private var startTimeMs: Long = 0L

    fun startScanning() {
        fountainDecoder.reset()
        startTimeMs = System.currentTimeMillis()
        _state.value = ReceiverState(isScanning = true)
    }

    fun stopScanning() {
        _state.value = _state.value.copy(isScanning = false)
    }

    @Synchronized
    fun processScannedQrBytes(context: Context, rawBytes: ByteArray) {
        if (!_state.value.isScanning || _state.value.isCompleted) return

        val status: DecoderStatus = fountainDecoder.addFrameBytes(rawBytes) ?: return

        val elapsedTimeSec = ((System.currentTimeMillis() - startTimeMs).coerceAtLeast(100)) / 1000f
        val bytesSoFar = (status.solvedCount * fountainDecoder.blockLen).toLong()
        val goodputKbps = (bytesSoFar / 1024f) / elapsedTimeSec

        _state.value = _state.value.copy(
            totalBlocks = status.k,
            solvedBlocks = status.solvedCount,
            dropsReceived = status.receivedCount,
            progressPercent = status.progressPercent,
            goodputKbps = goodputKbps
        )

        if (status.isComplete && !_state.value.isCompleted) {
            val opticalFile = fountainDecoder.assembleFile()
            if (opticalFile != null) {
                val savedFile = ApkInstaller.saveToDownloads(context, opticalFile.name, opticalFile.bytes)
                val isApk = opticalFile.name.lowercase().endsWith(".apk")
                _state.value = _state.value.copy(
                    fileName = opticalFile.name,
                    fileSize = opticalFile.bytes.size.toLong(),
                    isApk = isApk,
                    isCompleted = true,
                    isScanning = false,
                    savedFile = savedFile,
                    error = if (savedFile == null) "Failed to save file to Downloads" else null
                )
            } else {
                _state.value = _state.value.copy(
                    error = "File assembly or SHA-256 integrity check failed."
                )
            }
        }
    }

    fun processScannedQrText(context: Context, qrText: String) {
        val bytes = QrMatrixEncoder.decodeLatin1TextToBytes(qrText)
        processScannedQrBytes(context, bytes)
    }
}
