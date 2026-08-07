package com.example.opticaltransfer.core.fountain

import java.security.MessageDigest
import java.nio.ByteBuffer
import java.nio.ByteOrder
import kotlin.math.min
import kotlin.random.Random

data class EncodedDrop(
    val dropIndex: Int,
    val totalBlocks: Int,
    val fileSize: Long,
    val seed: Long,
    val fileName: String,
    val fileChecksum: String,
    val payload: ByteArray
) {
    /**
     * Serializes drop into a compact binary byte array suitable for QR transmission
     */
    fun toBinary(): ByteArray {
        val nameBytes = fileName.toByteArray(Charsets.UTF_8)
        val nameLen = nameBytes.size.coerceAtMost(255)
        val checksumBytes = fileChecksum.take(8).toByteArray(Charsets.UTF_8) // 8 char hex prefix

        val buffer = ByteBuffer.allocate(4 + 4 + 4 + 8 + 8 + 1 + nameLen + 8 + payload.size)
            .order(ByteOrder.BIG_ENDIAN)

        buffer.put("FOUN".toByteArray(Charsets.US_ASCII)) // Magic header
        buffer.putInt(dropIndex)
        buffer.putInt(totalBlocks)
        buffer.putLong(fileSize)
        buffer.putLong(seed)
        buffer.put(nameLen.toByte())
        buffer.put(nameBytes, 0, nameLen)
        buffer.put(checksumBytes)
        buffer.put(payload)

        return buffer.array()
    }

    companion object {
        fun fromBinary(bytes: ByteArray): EncodedDrop? {
            if (bytes.size < 30) return null
            val buffer = ByteBuffer.wrap(bytes).order(ByteOrder.BIG_ENDIAN)

            val magic = ByteArray(4)
            buffer.get(magic)
            if (String(magic, Charsets.US_ASCII) != "FOUN") return null

            val dropIndex = buffer.int
            val totalBlocks = buffer.int
            val fileSize = buffer.long
            val seed = buffer.long

            val nameLen = buffer.get().toInt() and 0xFF
            val nameBytes = ByteArray(nameLen)
            buffer.get(nameBytes)
            val fileName = String(nameBytes, Charsets.UTF_8)

            val checksumBytes = ByteArray(8)
            buffer.get(checksumBytes)
            val fileChecksum = String(checksumBytes, Charsets.UTF_8)

            val payload = ByteArray(buffer.remaining())
            buffer.get(payload)

            return EncodedDrop(
                dropIndex = dropIndex,
                totalBlocks = totalBlocks,
                fileSize = fileSize,
                seed = seed,
                fileName = fileName,
                fileChecksum = fileChecksum,
                payload = payload
            )
        }
    }
}

class FountainEncoder(
    val fileData: ByteArray,
    val fileName: String,
    val blockSize: Int = 256
) {
    val fileSize: Long = fileData.size.toLong()
    val totalBlocks: Int = ((fileData.size + blockSize - 1) / blockSize).coerceAtLeast(1)
    val fileChecksum: String = computeSha256(fileData)

    private val blocks = Array(totalBlocks) { i ->
        val start = i * blockSize
        val end = min(start + blockSize, fileData.size)
        val block = ByteArray(blockSize)
        System.arraycopy(fileData, start, block, 0, end - start)
        block
    }

    private var currentDropSeq = 0

    /**
     * Generate next fountain drop drop
     */
    fun nextDrop(): EncodedDrop {
        currentDropSeq++
        val seed = currentDropSeq.toLong() * 2654435761L
        val rng = Random(seed)

        // Select degree d using modified Soliton distribution
        val degree = selectDegree(rng, totalBlocks)
        val selectedIndices = selectBlockIndices(rng, totalBlocks, degree)

        val payload = ByteArray(blockSize)
        for (idx in selectedIndices) {
            val srcBlock = blocks[idx]
            for (b in 0 until blockSize) {
                payload[b] = (payload[b].toInt() xor srcBlock[b].toInt()).toByte()
            }
        }

        return EncodedDrop(
            dropIndex = currentDropSeq,
            totalBlocks = totalBlocks,
            fileSize = fileSize,
            seed = seed,
            fileName = fileName,
            fileChecksum = fileChecksum,
            payload = payload
        )
    }

    companion object {
        fun computeSha256(data: ByteArray): String {
            val digest = MessageDigest.getInstance("SHA-256")
            val hash = digest.digest(data)
            return hash.joinToString("") { "%02x".format(it) }
        }

        fun selectDegree(rng: Random, k: Int): Int {
            if (k <= 1) return 1
            val r = rng.nextDouble()
            return when {
                r < 0.40 -> 1
                r < 0.70 -> 2
                r < 0.85 -> 3
                r < 0.95 -> (4..min(10, k)).random(rng)
                else -> k
            }
        }

        fun selectBlockIndices(rng: Random, k: Int, degree: Int): List<Int> {
            val indices = mutableSetOf<Int>()
            var attempts = 0
            while (indices.size < degree && indices.size < k && attempts < 100) {
                indices.add(rng.nextInt(k))
                attempts++
            }
            return indices.toList()
        }
    }
}
