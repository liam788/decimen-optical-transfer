package com.example.opticaltransfer

import com.example.opticaltransfer.core.fountain.FountainDecoder
import com.example.opticaltransfer.core.fountain.FountainEncoder
import com.example.opticaltransfer.core.protocol.Protocol
import org.junit.Assert.*
import org.junit.Test
import java.util.Random

class FountainUnitTest {

    @Test
    fun testProtocolPackUnpackFile() {
        val testBytes = "Hello Decimen Universal Optical Transfer!".toByteArray(Charsets.UTF_8)
        val packed = Protocol.packFile("hello.txt", "text/plain", testBytes)
        val unpacked = Protocol.unpackFile(packed.container)

        assertEquals("hello.txt", unpacked.name)
        assertEquals("text/plain", unpacked.type)
        assertArrayEquals(testBytes, unpacked.bytes)
    }

    @Test
    fun testFountainEncodeDecodeUniversalProtocol() {
        val testSize = 10000 // 10 KB test binary
        val randomData = ByteArray(testSize)
        Random(12345).nextBytes(randomData)

        val packed = Protocol.packFile("data.bin", "application/octet-stream", randomData)
        val encoder = FountainEncoder(packed.container, targetBlockLen = 400)
        val decoder = FountainDecoder()

        var dropsProcessed = 0
        var isComplete = false

        while (!isComplete && dropsProcessed < 500) {
            val frameBytes = encoder.nextFrameBytes()
            val parsed = Protocol.parseFrame(frameBytes)
            assertNotNull("Frame 20-byte header parse must succeed", parsed)

            val status = decoder.addFrameBytes(frameBytes)
            assertNotNull("Decoder status should not be null", status)

            dropsProcessed++
            if (status!!.isComplete) {
                isComplete = true
            }
        }

        assertTrue("Fountain decoder should reach 100% completion", isComplete)
        val opticalFile = decoder.assembleFile()
        assertNotNull("Reconstructed OpticalFile should not be null", opticalFile)
        assertEquals("data.bin", opticalFile!!.name)
        assertArrayEquals("Reconstructed bytes must match original input bytes 100%", randomData, opticalFile.bytes)
    }
}
