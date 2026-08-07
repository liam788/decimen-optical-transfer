package com.example.opticaltransfer.core.codec

import android.graphics.Bitmap

object OtsuBinarizer {

    /**
     * Compute optimal Otsu threshold for a 1D byte array of luminance (Y channel) values
     */
    fun computeThreshold(luminance: ByteArray, width: Int, height: Int): Int {
        val histogram = IntArray(256)
        val total = width * height

        for (i in 0 until total) {
            val pixel = luminance[i].toInt() and 0xFF
            histogram[pixel]++
        }

        var sum = 0.0
        for (t in 0..255) {
            sum += t * histogram[t]
        }

        var sumB = 0.0
        var wB = 0
        var wF = 0
        var varMax = 0.0
        var threshold = 128

        for (t in 0..255) {
            wB += histogram[t]
            if (wB == 0) continue
            wF = total - wB
            if (wF == 0) break

            sumB += (t * histogram[t]).toDouble()

            val mB = sumB / wB
            val mF = (sum - sumB) / wF

            val varBetween = wB.toDouble() * wF.toDouble() * (mB - mF) * (mB - mF)

            if (varBetween > varMax) {
                varMax = varBetween
                threshold = t
            }
        }

        return threshold
    }

    /**
     * Binarizes YUV luminance buffer into black-and-white byte array using calculated Otsu threshold
     */
    fun binarizeLuminance(luminance: ByteArray, width: Int, height: Int): ByteArray {
        val threshold = computeThreshold(luminance, width, height)
        val output = ByteArray(width * height)
        for (i in output.indices) {
            val valPixel = luminance[i].toInt() and 0xFF
            output[i] = if (valPixel < threshold) 0.toByte() else 255.toByte()
        }
        return output
    }
}
