package com.example.opticaltransfer.core.codec

import android.graphics.Bitmap
import android.graphics.Color
import com.google.zxing.BarcodeFormat
import com.google.zxing.EncodeHintType
import com.google.zxing.qrcode.QRCodeWriter
import com.google.zxing.qrcode.decoder.ErrorCorrectionLevel

enum class MatrixGridMode(val rows: Int, val cols: Int) {
    SINGLE_1X1(1, 1),
    GRID_2X2(2, 2),
    GRID_3X3(3, 3)
}

enum class ColorCodecMode {
    MONOCHROME,
    COLOR_4_CHANNEL
}

data class QrFrameConfig(
    val gridMode: MatrixGridMode = MatrixGridMode.SINGLE_1X1,
    val colorMode: ColorCodecMode = ColorCodecMode.MONOCHROME,
    val targetFps: Int = 15
)

object QrMatrixEncoder {

    /**
     * Encodes a binary frame drop into ISO-8859-1 string suitable for ZXing QR transmission
     */
    fun encodeDropToLatin1Text(binaryDrop: ByteArray): String {
        return String(binaryDrop, Charsets.ISO_8859_1)
    }

    /**
     * Decodes QR text payload back into raw binary drop bytes
     */
    fun decodeLatin1TextToBytes(qrText: String): ByteArray {
        return qrText.toByteArray(Charsets.ISO_8859_1)
    }

    /**
     * Generates a high-contrast QR Code bitmap from binary frame drop
     */
    fun generateQrBitmap(binaryDrop: ByteArray, size: Int = 300): Bitmap? {
        return try {
            val text = encodeDropToLatin1Text(binaryDrop)
            val hints = mapOf<EncodeHintType, Any>(
                EncodeHintType.CHARACTER_SET to "ISO-8859-1",
                EncodeHintType.ERROR_CORRECTION to ErrorCorrectionLevel.L,
                EncodeHintType.MARGIN to 1
            )
            val writer = QRCodeWriter()
            val bitMatrix = writer.encode(text, BarcodeFormat.QR_CODE, size, size, hints)
            val width = bitMatrix.width
            val height = bitMatrix.height
            val bmp = Bitmap.createBitmap(width, height, Bitmap.Config.RGB_565)
            for (x in 0 until width) {
                for (y in 0 until height) {
                    bmp.setPixel(x, y, if (bitMatrix[x, y]) Color.BLACK else Color.WHITE)
                }
            }
            bmp
        } catch (_: Exception) {
            null
        }
    }

    /**
     * Combines multiple QR bitmaps into a spatial grid matrix bitmap (1x1, 2x2, 3x3)
     */
    fun combineGridBitmaps(bitmaps: List<Bitmap>, gridMode: MatrixGridMode, totalSize: Int): Bitmap {
        val composite = Bitmap.createBitmap(totalSize, totalSize, Bitmap.Config.ARGB_8888)
        val canvas = android.graphics.Canvas(composite)
        canvas.drawColor(Color.WHITE)

        val rows = gridMode.rows
        val cols = gridMode.cols
        val cellWidth = totalSize / cols
        val cellHeight = totalSize / rows

        var index = 0
        for (r in 0 until rows) {
            for (c in 0 until cols) {
                if (index < bitmaps.size) {
                    val bmp = bitmaps[index]
                    val left = c * cellWidth
                    val top = r * cellHeight
                    val dstRect = android.graphics.Rect(left, top, left + cellWidth, top + cellHeight)
                    canvas.drawBitmap(bmp, null, dstRect, null)
                }
                index++
            }
        }
        return composite
    }
}
