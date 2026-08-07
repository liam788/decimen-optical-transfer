package com.example.opticaltransfer.core.fountain

import com.example.opticaltransfer.core.protocol.FrameHeader
import com.example.opticaltransfer.core.protocol.OpticalFile
import com.example.opticaltransfer.core.protocol.Protocol
import kotlin.math.min

data class DecoderStatus(
    val k: Int,
    val solvedCount: Int,
    val receivedCount: Int,
    val progressPercent: Float,
    val isComplete: Boolean,
    val streamIdentity: String
)

private data class PendingFrame(
    val indices: MutableSet<Int>,
    val payload: ByteArray
)

class FountainDecoder {
    var sessionId: Int? = null
        private set
    var k: Int = 0
        private set
    var blockLen: Int = 0
        private set
    var totalLen: Int = 0
        private set
    var payloadFnv: Long = 0L
        private set
    private var cdf: DoubleArray? = null

    private val solvedBlocks = mutableMapOf<Int, ByteArray>()
    private val byBlockMap = mutableMapOf<Int, MutableSet<PendingFrame>>()
    private val seenSeqs = mutableSetOf<Int>()
    private var receivedFramesCount = 0

    @Synchronized
    fun reset() {
        sessionId = null
        k = 0
        blockLen = 0
        totalLen = 0
        payloadFnv = 0L
        cdf = null
        solvedBlocks.clear()
        byBlockMap.clear()
        seenSeqs.clear()
        receivedFramesCount = 0
    }

    @Synchronized
    fun addFrameBytes(bytes: ByteArray): DecoderStatus? {
        val parsed = Protocol.parseFrame(bytes) ?: return null
        val header = parsed.first
        val blockData = parsed.second

        // Reset if new stream session or mismatched identity
        if (sessionId != null && (header.sessionId != sessionId || header.k != k || header.totalLen != totalLen || header.payloadFnv != payloadFnv)) {
            reset()
        }

        if (sessionId == null) {
            sessionId = header.sessionId
            k = header.k
            blockLen = header.blockLen
            totalLen = header.totalLen
            payloadFnv = header.payloadFnv
            cdf = FountainMath.solitonCdf(k)
        }

        if (seenSeqs.contains(header.seq)) {
            return getStatus()
        }
        seenSeqs.add(header.seq)
        receivedFramesCount++

        if (isComplete()) return getStatus()

        val indices = FountainMath.frameIndices(k, cdf!!, sessionId!!, header.seq).toMutableSet()
        val currentPayload = blockData.copyOf()

        val iterator = indices.iterator()
        while (iterator.hasNext()) {
            val b = iterator.next()
            val solved = solvedBlocks[b]
            if (solved != null) {
                for (i in 0 until blockLen) {
                    currentPayload[i] = (currentPayload[i].toInt() xor solved[i].toInt()).toByte()
                }
                iterator.remove()
            }
        }

        if (indices.isEmpty()) return getStatus()

        if (indices.size == 1) {
            resolveBlock(indices.first(), currentPayload)
        } else {
            val pf = PendingFrame(indices, currentPayload)
            for (b in indices) {
                val set = byBlockMap.getOrPut(b) { mutableSetOf() }
                set.add(pf)
            }
        }

        return getStatus()
    }

    private fun resolveBlock(b0: Int, w0: ByteArray) {
        val queue = ArrayDeque<Pair<Int, ByteArray>>()
        queue.addLast(Pair(b0, w0))

        while (queue.isNotEmpty()) {
            val (b, w) = queue.removeFirst()
            if (solvedBlocks.containsKey(b)) continue

            solvedBlocks[b] = w

            val waiting = byBlockMap.remove(b) ?: continue
            for (pf in waiting) {
                for (i in 0 until blockLen) {
                    pf.payload[i] = (pf.payload[i].toInt() xor w[i].toInt()).toByte()
                }
                pf.indices.remove(b)

                if (pf.indices.size == 1) {
                    val r = pf.indices.first()
                    byBlockMap[r]?.remove(pf)
                    if (!solvedBlocks.containsKey(r)) {
                        queue.addLast(Pair(r, pf.payload))
                    }
                }
            }
        }
    }

    fun isComplete(): Boolean = k > 0 && solvedBlocks.size >= k

    fun getStatus(): DecoderStatus {
        val count = solvedBlocks.size
        val percent = if (k > 0) (count.toFloat() / k.toFloat()) * 100f else 0f
        val identity = if (sessionId != null) "$sessionId:$k:$blockLen:$totalLen:$payloadFnv" else ""

        return DecoderStatus(
            k = k,
            solvedCount = count,
            receivedCount = receivedFramesCount,
            progressPercent = percent,
            isComplete = isComplete(),
            streamIdentity = identity
        )
    }

    fun assembleFile(): OpticalFile? {
        if (!isComplete()) return null

        val container = ByteArray(totalLen)
        for (b in 0 until k) {
            val block = solvedBlocks[b] ?: return null
            val start = b * blockLen
            val len = min(blockLen, totalLen - start)
            if (len > 0) {
                System.arraycopy(block, 0, container, start, len)
            }
        }

        // Verify FNV-1a hash
        if (Protocol.fnv1a(container) != payloadFnv) {
            return null
        }

        return try {
            Protocol.unpackFile(container)
        } catch (_: Exception) {
            null
        }
    }
}
