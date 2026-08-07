package com.example.opticaltransfer.core.fountain

import java.security.MessageDigest
import kotlin.math.min
import kotlin.random.Random

data class DecoderProgress(
    val totalBlocks: Int,
    val solvedBlocks: Int,
    val totalDropsReceived: Int,
    val bytesReceived: Long,
    val fileSize: Long,
    val isComplete: Boolean,
    val fileName: String
)

class FountainDecoder {
    private var totalBlocks: Int = 0
    private var fileSize: Long = 0L
    private var fileName: String = ""
    private var expectedChecksumPrefix: String = ""
    private var blockSize: Int = 0

    private val solvedBlocksMap = mutableMapOf<Int, ByteArray>()
    private val equationsList = mutableListOf<Equation>()
    private var totalDropsReceived = 0

    private data class Equation(
        val dropSeq: Int,
        var indices: HashSet<Int>,
        val payload: ByteArray
    )

    @Synchronized
    fun processDrop(drop: EncodedDrop): DecoderProgress {
        totalDropsReceived++

        if (totalBlocks == 0) {
            totalBlocks = drop.totalBlocks
            fileSize = drop.fileSize
            fileName = drop.fileName
            expectedChecksumPrefix = drop.fileChecksum
            blockSize = drop.payload.size
        }

        val rng = Random(drop.seed)
        val degree = FountainEncoder.selectDegree(rng, totalBlocks)
        val indices = FountainEncoder.selectBlockIndices(rng, totalBlocks, degree).toHashSet()

        // Simplify drop payload using already solved blocks
        val currentPayload = drop.payload.copyOf()
        val remainingIndices = HashSet<Int>()

        for (idx in indices) {
            if (solvedBlocksMap.containsKey(idx)) {
                val solved = solvedBlocksMap[idx]!!
                for (b in 0 until blockSize) {
                    currentPayload[b] = (currentPayload[b].toInt() xor solved[b].toInt()).toByte()
                }
            } else {
                remainingIndices.add(idx)
            }
        }

        if (remainingIndices.isNotEmpty()) {
            val eq = Equation(drop.dropIndex, remainingIndices, currentPayload)
            equationsList.add(eq)
            solveEquations()
        }

        val solvedCount = solvedBlocksMap.size
        val isDone = solvedCount >= totalBlocks

        return DecoderProgress(
            totalBlocks = totalBlocks,
            solvedBlocks = solvedCount,
            totalDropsReceived = totalDropsReceived,
            bytesReceived = (solvedCount * blockSize).toLong().coerceAtMost(fileSize),
            fileSize = fileSize,
            isComplete = isDone,
            fileName = fileName
        )
    }

    private fun solveEquations() {
        var changed = true
        while (changed) {
            changed = false
            val iterator = equationsList.iterator()

            val newlySolved = mutableListOf<Pair<Int, ByteArray>>()

            while (iterator.hasNext()) {
                val eq = iterator.next()

                // Remove solved indices
                eq.indices.removeAll { solvedBlocksMap.containsKey(it) }

                if (eq.indices.isEmpty()) {
                    iterator.remove()
                } else if (eq.indices.size == 1) {
                    val solvedIdx = eq.indices.first()
                    newlySolved.add(solvedIdx to eq.payload.copyOf())
                    iterator.remove()
                    changed = true
                }
            }

            for ((idx, block) in newlySolved) {
                solvedBlocksMap[idx] = block
                // Perform back-substitution on remaining equations
                for (eq in equationsList) {
                    if (eq.indices.contains(idx)) {
                        eq.indices.remove(idx)
                        for (b in 0 until blockSize) {
                            eq.payload[b] = (eq.payload[b].toInt() xor block[b].toInt()).toByte()
                        }
                    }
                }
            }
        }
    }

    fun isComplete(): Boolean = totalBlocks > 0 && solvedBlocksMap.size >= totalBlocks

    fun assembleFile(): ByteArray? {
        if (!isComplete()) return null

        val result = ByteArray(fileSize.toInt())
        for (i in 0 until totalBlocks) {
            val block = solvedBlocksMap[i] ?: return null
            val start = i * blockSize
            val length = min(blockSize, result.size - start)
            System.arraycopy(block, 0, result, start, length)
        }

        // Verify SHA-256 checksum prefix
        val actualChecksum = FountainEncoder.computeSha256(result)
        if (!actualChecksum.startsWith(expectedChecksumPrefix)) {
            // Checksum failed
            return null
        }

        return result
    }

    fun reset() {
        totalBlocks = 0
        fileSize = 0L
        fileName = ""
        expectedChecksumPrefix = ""
        blockSize = 0
        solvedBlocksMap.clear()
        equationsList.clear()
        totalDropsReceived = 0
    }
}
