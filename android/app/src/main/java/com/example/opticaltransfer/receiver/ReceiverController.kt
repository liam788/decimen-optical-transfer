package com.example.opticaltransfer.receiver

import android.content.Context
import com.example.opticaltransfer.core.codec.QrMatrixEncoder
import com.example.opticaltransfer.core.fountain.DecoderProgress
import com.example.opticaltransfer.core.fountain.EncodedDrop
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
    private var totalBytesDecoded: Long = 0L

    fun startScanning() {
        fountainDecoder.reset()
        startTimeMs = System.currentTimeMillis()
        totalBytesDecoded = 0L
        _state.value = ReceiverState(isScanning = true)
    }

    fun stopScanning() {
        _state.value = _state.value.copy(isScanning = false)
    }

    @Synchronized
    fun processScannedQrText(context: Context, qrText: String) {
        if (!_state.value.isScanning || _state.value.isCompleted) return

        val binaryBytes = QrMatrixEncoder.decodeTextToDrop(qrText) ?: return
        val drop = EncodedDrop.fromBinary(binaryBytes) ?: return

        val progress: DecoderProgress = fountainDecoder.processDrop(drop)
        totalBytesDecoded = progress.bytesReceived

        val elapsedTimeSec = ((System.currentTimeMillis() - startTimeMs).coerceAtLeast(100)) / 1000f
        val goodputKbps = (totalBytesDecoded / 1024f) / elapsedTimeSec
        val percent = if (progress.totalBlocks > 0) {
            (progress.solvedBlocks.toFloat() / progress.totalBlocks.toFloat()) * 100f
        } else {
            0f
        }

        val isApk = progress.fileName.lowercase().endsWith(".apk")

        _state.value = _state.value.copy(
            totalBlocks = progress.totalBlocks,
            solvedBlocks = progress.solvedBlocks,
            dropsReceived = progress.totalDropsReceived,
            progressPercent = percent,
            goodputKbps = goodputKbps,
            fileName = progress.fileName,
            fileSize = progress.fileSize,
            isApk = isApk
        )

        if (progress.isComplete && !_state.value.isCompleted) {
            val assembledBytes = fountainDecoder.assembleFile()
            if (assembledBytes != null) {
                val savedFile = ApkInstaller.saveToDownloads(context, progress.fileName, assembledBytes)
                _state.value = _state.value.copy(
                    isCompleted = true,
                    isScanning = false,
                    savedFile = savedFile,
                    error = if (savedFile == null) "Failed to save file to downloads" else null
                )
            } else {
                _state.value = _state.value.copy(
                    error = "File assembly / SHA-256 checksum verification failed"
                )
            }
        }
    }
}
