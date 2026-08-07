package com.example.opticaltransfer.core.codec

import android.graphics.Bitmap
import android.graphics.Color
import android.util.Base64

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
     * Converts a binary drop into a Base64 string payload with preamble
     */
    fun encodeDropToText(binaryDrop: ByteArray): String {
        return "OPT1:" + Base64.encodeToString(binaryDrop, Base64.NO_WRAP)
    }

    /**
     * Decodes QR text payload back into binary drop bytes
     */
    fun decodeTextToDrop(qrText: String): ByteArray? {
        if (!qrText.startsWith("OPT1:")) return null
        val b64 = qrText.substring(5)
        return try {
            Base64.decode(b64, Base64.NO_WRAP)
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

    /**
     * Encodes 2 separate drops into a single 4-color RGB bitmap (Red channel = drop 1, Blue channel = drop 2)
     */
    fun createColor4ChannelBitmap(monoBitmap1: Bitmap, monoBitmap2: Bitmap): Bitmap {
        val width = monoBitmap1.width
        val height = monoBitmap1.height
        val result = Bitmap.createBitmap(width, height, Bitmap.Config.ARGB_8888)

        val pixels1 = IntArray(width * height)
        val pixels2 = IntArray(width * height)
        val resultPixels = IntArray(width * height)

        monoBitmap1.getPixels(pixels1, 0, width, 0, 0, width, height)
        monoBitmap2.getPixels(pixels2, 0, width, 0, 0, width, height)

        for (i in pixels1.indices) {
            val isDark1 = (Color.red(pixels1[i]) < 128)
            val isDark2 = (Color.red(pixels2[i]) < 128)

            val r = if (isDark1) 255 else 0
            val b = if (isDark2) 255 else 0
            val g = 0
            resultPixels[i] = Color.rgb(r, g, b)
        }

        result.setPixels(resultPixels, 0, width, 0, 0, width, height)
        return result
    }
}
