package com.example.opticaltransfer.core.fountain

import kotlin.math.max
import kotlin.math.min
import kotlin.math.sqrt

const val LN2 = 0.6931471805599453
const val SOLITON_C = 0.1
const val SOLITON_DELTA = 0.5

object FountainMath {

    /**
     * Deterministic natural log matching dlog() in fountain.ts
     */
    fun dlog(x: Double): Double {
        var e = 0
        var m = x
        while (m >= 1.5) {
            m /= 2.0
            e++
        }
        while (m < 0.75) {
            m *= 2.0
            e--
        }
        val z = (m - 1.0) / (m + 1.0)
        val z2 = z * z
        var term = z
        var sum = 0.0
        var n = 1
        while (n <= 21) {
            sum += term / n
            term *= z2
            n += 2
        }
        return e * LN2 + 2.0 * sum
    }

    /**
     * Robust Soliton degree CDF for k blocks matching fountain.ts
     */
    fun solitonCdf(k: Int): DoubleArray {
        val cdf = DoubleArray(k)
        if (k == 1) {
            cdf[0] = 1.0
            return cdf
        }
        val R = max(1.0, SOLITON_C * dlog(k.toDouble() / SOLITON_DELTA) * sqrt(k.toDouble()))
        val spike = min(k, kotlin.math.ceil(k / R).toInt())
        var total = 0.0
        for (d in 1..k) {
            val rho = if (d == 1) 1.0 / k else 1.0 / (d.toDouble() * (d - 1))
            var tau = 0.0
            if (d < spike) {
                tau = R / (d.toDouble() * k)
            } else if (d == spike) {
                tau = (R * max(0.0, dlog(R / SOLITON_DELTA))) / k
            }
            total += rho + tau
            cdf[d - 1] = total
        }
        for (i in 0 until k) {
            cdf[i] = cdf[i] / total
        }
        cdf[k - 1] = 1.0
        return cdf
    }

    fun frameSeed(sessionId: Int, seq: Int): Int {
        var h = ((sessionId + 1) * 0x9e3779b1.toInt()) xor (seq + 0x85ebca6b.toInt())
        h = (h xor h.ushr(13)) * 0xc2b2ae35.toInt()
        return h xor h.ushr(16)
    }

    fun splitmix32(seed: Int): () -> Long {
        var s = seed
        return {
            s = s + 0x9e3779b9.toInt()
            var t = s xor s.ushr(16)
            t = t * 0x21f0aaad.toInt()
            t = t xor t.ushr(15)
            t = t * 0x735a2d97.toInt()
            t = t xor t.ushr(15)
            (t.toLong() and 0xFFFFFFFFL)
        }
    }

    /**
     * Block indices derived deterministically for frame seq matching fountain.ts
     */
    fun frameIndices(k: Int, cdf: DoubleArray, sessionId: Int, seq: Int): List<Int> {
        val rnd = splitmix32(frameSeed(sessionId, seq))
        val u = rnd().toDouble() * 2.3283064365386963e-10 // 2^-32

        var lo = 0
        var hi = k - 1
        while (lo < hi) {
            val mid = (lo + hi) shr 1
            if (cdf[mid] >= u) {
                hi = mid
            } else {
                lo = mid + 1
            }
        }
        val d = min(k, lo + 1)

        if (d > (k shr 3)) {
            val scratch = IntArray(k) { it }
            val out = IntArray(d)
            for (i in 0 until d) {
                val rem = (k - i).toLong()
                val pick = (rnd() % rem).toInt()
                val j = i + pick
                val t = scratch[i]
                scratch[i] = scratch[j]
                scratch[j] = t
                out[i] = scratch[i]
            }
            return out.toList()
        }

        val set = mutableSetOf<Int>()
        while (set.size < d) {
            val pick = (rnd() % k.toLong()).toInt()
            set.add(pick)
        }
        return set.toList()
    }
}
