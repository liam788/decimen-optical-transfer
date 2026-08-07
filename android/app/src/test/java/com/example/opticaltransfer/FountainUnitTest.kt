package com.example.opticaltransfer

import com.example.opticaltransfer.core.fountain.FountainDecoder
import com.example.opticaltransfer.core.fountain.FountainEncoder
import org.junit.Assert.*
import org.junit.Test
import java.util.Random

class FountainUnitTest {

    @Test
    fun testFountainEncodeDecodeRandomData() {
        val testSize = 5000 // 5 KB test payload
        val randomData = ByteArray(testSize)
        Random(42).nextBytes(randomData)

        val fileName = "test_data.bin"
        val encoder = FountainEncoder(randomData, fileName, blockSize = 256)
        val decoder = FountainDecoder()

        var dropsProcessed = 0
        var isComplete = false

        while (!isComplete && dropsProcessed < 500) {
            val drop = encoder.nextDrop()
            val binary = drop.toBinary()
            val decodedDrop = com.example.opticaltransfer.core.fountain.EncodedDrop.fromBinary(binary)

            assertNotNull("Drop binary deserialization should succeed", decodedDrop)
            val progress = decoder.processDrop(decodedDrop!!)
            dropsProcessed++
            if (progress.isComplete) {
                isComplete = true
            }
        }

        assertTrue("Fountain decoder should complete reconstruction", isComplete)
        val reconstructed = decoder.assembleFile()
        assertNotNull("Reconstructed payload should not be null", reconstructed)
        assertArrayEquals("Reconstructed bytes must match original input bytes exactly", randomData, reconstructed)
    }
}
