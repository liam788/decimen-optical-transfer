package com.example.opticaltransfer.sender

import com.example.opticaltransfer.core.codec.MatrixGridMode
import com.example.opticaltransfer.core.codec.QrFrameConfig
import com.example.opticaltransfer.core.codec.QrMatrixEncoder
import com.example.opticaltransfer.core.fountain.EncodedDrop
import com.example.opticaltransfer.core.fountain.FountainEncoder
import kotlinx.coroutines.*
import kotlinx.coroutines.flow.MutableStateFlow
import kotlinx.coroutines.flow.StateFlow
import kotlinx.coroutines.flow.asStateFlow

data class SenderState(
    val isStreaming: Boolean = false,
    val fileName: String = "",
    val fileSize: Long = 0L,
    val totalBlocks: Int = 0,
    val dropsSent: Int = 0,
    val currentDropText: String = "",
    val currentGridDropTexts: List<String> = emptyList(),
    val config: QrFrameConfig = QrFrameConfig()
)

class SenderController {
    private val _state = MutableStateFlow(SenderState())
    val state: StateFlow<SenderState> = _state.asStateFlow()

    private var encoder: FountainEncoder? = null
    private var streamingJob: Job? = null

    fun prepareFile(fileName: String, fileBytes: ByteArray, blockSize: Int = 256) {
        stopStreaming()
        val fEncoder = FountainEncoder(fileBytes, fileName, blockSize)
        encoder = fEncoder

        _state.value = _state.value.copy(
            isStreaming = false,
            fileName = fileName,
            fileSize = fEncoder.fileSize,
            totalBlocks = fEncoder.totalBlocks,
            dropsSent = 0,
            currentDropText = "",
            currentGridDropTexts = emptyList()
        )
    }

    fun updateConfig(config: QrFrameConfig) {
        _state.value = _state.value.copy(config = config)
    }

    fun startStreaming(scope: CoroutineScope) {
        val fEncoder = encoder ?: return
        if (_state.value.isStreaming) return

        _state.value = _state.value.copy(isStreaming = true)

        streamingJob = scope.launch(Dispatchers.Default) {
            var sentCount = 0
            val config = _state.value.config
            val gridCells = config.gridMode.rows * config.gridMode.cols
            val delayMs = (1000 / config.targetFps.coerceIn(1, 60)).toLong()

            while (isActive && _state.value.isStreaming) {
                val drops = mutableListOf<EncodedDrop>()
                val dropTexts = mutableListOf<String>()

                for (i in 0 until gridCells) {
                    val drop = fEncoder.nextDrop()
                    drops.add(drop)
                    val text = QrMatrixEncoder.encodeDropToText(drop.toBinary())
                    dropTexts.add(text)
                    sentCount++
                }

                _state.value = _state.value.copy(
                    dropsSent = sentCount,
                    currentDropText = dropTexts.firstOrNull() ?: "",
                    currentGridDropTexts = dropTexts
                )

                delay(delayMs)
            }
        }
    }

    fun stopStreaming() {
        streamingJob?.cancel()
        streamingJob = null
        _state.value = _state.value.copy(isStreaming = false)
    }
}
