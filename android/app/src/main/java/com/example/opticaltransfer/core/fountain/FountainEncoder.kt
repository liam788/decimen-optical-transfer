package com.example.opticaltransfer.core.fountain

import com.example.opticaltransfer.core.protocol.FrameHeader
import com.example.opticaltransfer.core.protocol.Protocol
import kotlin.math.ceil
import kotlin.math.min
import kotlin.random.Random

class FountainEncoder(
    val payload: ByteArray,
    val targetBlockLen: Int = 400
) {
    val k: Int = ceil(payload.size.toDouble() / targetBlockLen.toDouble()).toInt().coerceIn(1, 65535)
    val blockLen: Int = ceil(payload.size.toDouble() / k.toDouble()).toInt()
    val sessionId: Int = Random.nextInt(1, 65535)
    val cdf: DoubleArray = FountainMath.solitonCdf(k)
    val payloadFnv: Long = Protocol.fnv1a(payload)

    private var currentSeq = 0

    fun nextFrameBytes(): ByteArray {
        currentSeq++
        val indices = FountainMath.frameIndices(k, cdf, sessionId, currentSeq)

        val frameBlock = ByteArray(blockLen)
        for (idx in indices) {
            val start = idx * blockLen
            val end = min(start + blockLen, payload.size)
            for (i in 0 until (end - start)) {
                frameBlock[i] = (frameBlock[i].toInt() xor payload[start + i].toInt()).toByte()
            }
        }

        val header = FrameHeader(
            sessionId = sessionId,
            seq = currentSeq,
            k = k,
            blockLen = blockLen,
            totalLen = payload.size,
            payloadFnv = payloadFnv
        )

        return Protocol.packFrame(header, frameBlock)
    }
}
