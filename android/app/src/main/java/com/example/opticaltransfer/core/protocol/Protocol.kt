package com.example.opticaltransfer.core.protocol

import java.io.ByteArrayInputStream
import java.io.ByteArrayOutputStream
import java.nio.ByteBuffer
import java.nio.ByteOrder
import java.security.MessageDigest
import java.util.zip.GZIPInputStream
import java.util.zip.GZIPOutputStream

const val HEADER_LEN = 20
const val MAX_FILE_BYTES = 1024 * 1024 * 1024 // 1 GB
const val FILE_HEADER_LEN = 49
val MAGIC0 = 0xD1.toByte()
val MAGIC1 = 0x0C.toByte()
val FILE_MAGIC = byteArrayOf(0x44, 0x43, 0x46, 0x32) // "DCF2"

data class FrameHeader(
    val sessionId: Int,
    val seq: Int,
    val k: Int,
    val blockLen: Int,
    val totalLen: Int,
    val payloadFnv: Long
)

data class PackedOpticalFile(
    val container: ByteArray,
    val useGzip: Boolean,
    val originalSize: Int,
    val transmittedSize: Int
)

data class OpticalFile(
    val name: String,
    val type: String,
    val bytes: ByteArray,
    val sha256: ByteArray,
    val useGzip: Boolean,
    val transmittedSize: Int
)

object Protocol {

    /** FNV-1a 32-bit hash matching protocol.ts */
    fun fnv1a(bytes: ByteArray): Long {
        var h = 0x811c9dc5L
        for (b in bytes) {
            val v = (b.toInt() and 0xFF).toLong()
            h = h xor v
            h = (h * 0x01000193L) and 0xFFFFFFFFL
        }
        return h
    }

    /** Compute SHA-256 digest */
    fun sha256(bytes: ByteArray): ByteArray {
        val md = MessageDigest.getInstance("SHA-256")
        return md.digest(bytes)
    }

    fun safeFileName(name: String): String {
        val base = name.split(Regex("[/\\\\]")).lastOrNull() ?: ""
        val cleaned = base.replace(Regex("[\\u0000-\\u001f\\u007f]"), "").trim()
        return if (cleaned.isEmpty() || cleaned == "." || cleaned == "..") "transfer.bin" else cleaned
    }

    /** Packs FrameHeader + block into 20-byte Little Endian header frame */
    fun packFrame(h: FrameHeader, block: ByteArray): ByteArray {
        val out = ByteArray(HEADER_LEN + block.size)
        out[0] = MAGIC0
        out[1] = MAGIC1
        val buffer = ByteBuffer.wrap(out).order(ByteOrder.LITTLE_ENDIAN)
        buffer.position(2)
        buffer.putShort(h.sessionId.toShort())
        buffer.putInt(h.seq)
        buffer.putShort(h.k.toShort())
        buffer.putShort(h.blockLen.toShort())
        buffer.putInt(h.totalLen)
        buffer.putInt(h.payloadFnv.toInt())
        System.arraycopy(block, 0, out, HEADER_LEN, block.size)
        return out
    }

    /** Parses binary frame bytes into FrameHeader & block */
    fun parseFrame(bytes: ByteArray): Pair<FrameHeader, ByteArray>? {
        if (bytes.size <= HEADER_LEN) return null
        if (bytes[0] != MAGIC0 || bytes[1] != MAGIC1) return null

        val buffer = ByteBuffer.wrap(bytes).order(ByteOrder.LITTLE_ENDIAN)
        buffer.position(2)
        val sessionId = buffer.short.toInt() and 0xFFFF
        val seq = buffer.int
        val k = buffer.short.toInt() and 0xFFFF
        val blockLen = buffer.short.toInt() and 0xFFFF
        val totalLen = buffer.int
        val payloadFnv = buffer.int.toLong() and 0xFFFFFFFFL

        if (k == 0 || blockLen == 0 || totalLen == 0) return null
        if (bytes.size != HEADER_LEN + blockLen) return null

        val header = FrameHeader(sessionId, seq, k, blockLen, totalLen, payloadFnv)
        val block = ByteArray(blockLen)
        System.arraycopy(bytes, HEADER_LEN, block, 0, blockLen)

        return Pair(header, block)
    }

    /** Packs raw file into DCF2 container format matching protocol.ts */
    fun packFile(name: String, type: String, bytes: ByteArray): PackedOpticalFile {
        require(bytes.isNotEmpty()) { "Choose a non-empty file." }
        val cleanName = safeFileName(name)
        val cleanType = if (type.isBlank()) "application/octet-stream" else type

        val nameBytes = cleanName.toByteArray(Charsets.UTF_8)
        val typeBytes = cleanType.toByteArray(Charsets.UTF_8)

        var tryGzip = bytes.size >= 768
        var transmitted = bytes
        var useGzip = false

        if (tryGzip) {
            try {
                val bos = ByteArrayOutputStream()
                GZIPOutputStream(bos).use { gzos -> gzos.write(bytes) }
                val compressed = bos.toByteArray()
                if (compressed.size + 64 < bytes.size) {
                    transmitted = compressed
                    useGzip = true
                }
            } catch (_: Exception) {}
        }

        val digestBytes = sha256(bytes)
        val out = ByteArray(FILE_HEADER_LEN + nameBytes.size + typeBytes.size + transmitted.size)
        System.arraycopy(FILE_MAGIC, 0, out, 0, 4)

        val buffer = ByteBuffer.wrap(out).order(ByteOrder.LITTLE_ENDIAN)
        buffer.position(4)
        buffer.put(if (useGzip) 1.toByte() else 0.toByte())
        buffer.putShort(nameBytes.size.toShort())
        buffer.putShort(typeBytes.size.toShort())
        buffer.putInt(bytes.size)
        buffer.putInt(transmitted.size)
        System.arraycopy(digestBytes, 0, out, 17, 32)

        var offset = FILE_HEADER_LEN
        System.arraycopy(nameBytes, 0, out, offset, nameBytes.size)
        offset += nameBytes.size
        System.arraycopy(typeBytes, 0, out, offset, typeBytes.size)
        offset += typeBytes.size
        System.arraycopy(transmitted, 0, out, offset, transmitted.size)

        return PackedOpticalFile(
            container = out,
            useGzip = useGzip,
            originalSize = bytes.size,
            transmittedSize = transmitted.size
        )
    }

    /** Unpacks DCF2 container payload */
    fun unpackFile(container: ByteArray): OpticalFile {
        require(container.size >= FILE_HEADER_LEN) { "File header is incomplete." }
        for (i in 0 until 4) {
            require(container[i] == FILE_MAGIC[i]) { "File header is invalid." }
        }

        val buffer = ByteBuffer.wrap(container).order(ByteOrder.LITTLE_ENDIAN)
        buffer.position(4)
        val useGzip = buffer.get().toInt() == 1
        val nameLen = buffer.short.toInt() and 0xFFFF
        val typeLen = buffer.short.toInt() and 0xFFFF
        val fileLength = buffer.int
        val transmittedLength = buffer.int

        val sha256Expected = ByteArray(32)
        System.arraycopy(container, 17, sha256Expected, 0, 32)

        val dataOffset = FILE_HEADER_LEN + nameLen + typeLen
        require(dataOffset + transmittedLength == container.size) { "File length does not match container header." }

        val nameBytes = ByteArray(nameLen)
        System.arraycopy(container, FILE_HEADER_LEN, nameBytes, 0, nameLen)

        val typeBytes = ByteArray(typeLen)
        System.arraycopy(container, FILE_HEADER_LEN + nameLen, typeBytes, 0, typeLen)

        val transmitted = ByteArray(transmittedLength)
        System.arraycopy(container, dataOffset, transmitted, 0, transmittedLength)

        val decompressed = if (useGzip) {
            val bis = ByteArrayInputStream(transmitted)
            val gzis = GZIPInputStream(bis)
            gzis.readBytes()
        } else {
            transmitted
        }

        require(decompressed.size == fileLength) { "Decompressed file length mismatch." }

        val actualSha = sha256(decompressed)
        for (i in 0 until 32) {
            require(actualSha[i] == sha256Expected[i]) { "SHA-256 integrity check failed." }
        }

        return OpticalFile(
            name = safeFileName(String(nameBytes, Charsets.UTF_8)),
            type = String(typeBytes, Charsets.UTF_8),
            bytes = decompressed,
            sha256 = actualSha,
            useGzip = useGzip,
            transmittedSize = transmittedLength
        )
    }
}
