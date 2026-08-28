using System;
using System.Collections.Generic;

namespace OpticalTransfer.Core
{
    /// <summary>
    /// Core mathematical primitives for Fountain Code (LT Code / Soliton Distribution)
    /// </summary>
    public static class FountainMath
    {
        public const double LN2 = 0.6931471805599453;
        public const double SOLITON_C = 0.1;
        public const double SOLITON_DELTA = 0.5;

        /// <summary>
        /// Deterministic log calculation via 21-term Maclaurin expansion
        /// </summary>
        public static double DLog(double x)
        {
            int e = 0;
            double m = x;
            while (m >= 1.5)
            {
                m /= 2.0;
                e++;
            }
            while (m < 0.75)
            {
                m *= 2.0;
                e--;
            }
            double z = (m - 1.0) / (m + 1.0);
            double z2 = z * z;
            double term = z;
            double sum = 0.0;
            int n = 1;
            while (n <= 21)
            {
                sum += term / (double)n;
                term *= z2;
                n += 2;
            }
            return (double)e * LN2 + 2.0 * sum;
        }

        /// <summary>
        /// Robust Soliton Distribution Cumulative Distribution Function (CDF)
        /// </summary>
        public static double[] SolitonCdf(uint k)
        {
            if (k == 0) k = 1;
            double[] cdf = new double[k];
            if (k == 1)
            {
                cdf[0] = 1.0;
                return cdf;
            }

            double dk = (double)k;
            double R = Math.Max(1.0, SOLITON_C * DLog(dk / SOLITON_DELTA) * Math.Sqrt(dk));
            uint spike = Math.Min(k, (uint)Math.Ceiling(dk / R));
            double total = 0.0;

            for (uint d = 1; d <= k; ++d)
            {
                double dd = (double)d;
                double rho = (d == 1) ? (1.0 / dk) : (1.0 / (dd * (dd - 1.0)));
                double tau = 0.0;
                if (d < spike)
                {
                    tau = R / (dd * dk);
                }
                else if (d == spike)
                {
                    tau = (R * Math.Max(0.0, DLog(R / SOLITON_DELTA))) / dk;
                }
                total += rho + tau;
                cdf[d - 1] = total;
            }

            for (uint i = 0; i < k; ++i)
            {
                cdf[i] /= total;
            }
            cdf[k - 1] = 1.0;
            return cdf;
        }

        public static uint FrameSeed(uint sessionId, uint seq)
        {
            uint h = ((sessionId + 1) * 0x9E3779B1u) ^ (seq + 0x85EBCA6Bu);
            h = (h ^ (h >> 13)) * 0xC2B2AE35u;
            return h ^ (h >> 16);
        }

        public class SplitMix32
        {
            private uint _state;

            public SplitMix32(uint seed)
            {
                _state = seed;
            }

            public uint Next()
            {
                _state += 0x9E3779B9u;
                uint t = _state ^ (_state >> 16);
                t *= 0x21F0AAADu;
                t ^= (t >> 15);
                t *= 0x735A2D97u;
                t ^= (t >> 15);
                return t;
            }
        }

        /// <summary>
        /// Generates the pseudo-random source block indices for a repair droplet
        /// </summary>
        public static uint[] FrameIndices(uint k, double[] cdf, uint sessionId, uint seq)
        {
            SplitMix32 rnd = new SplitMix32(FrameSeed(sessionId, seq));
            double u = (double)rnd.Next() * 2.3283064365386963e-10; // 2^-32

            uint lo = 0;
            uint hi = k - 1;
            while (lo < hi)
            {
                uint mid = (lo + hi) >> 1;
                if (cdf[mid] >= u)
                {
                    hi = mid;
                }
                else
                {
                    lo = mid + 1;
                }
            }
            uint d = Math.Min(k, lo + 1);

            if (d > (k >> 3))
            {
                uint[] scratch = new uint[k];
                for (uint i = 0; i < k; ++i) scratch[i] = i;
                uint[] result = new uint[d];
                for (uint i = 0; i < d; ++i)
                {
                    uint rem = k - i;
                    uint pick = rnd.Next() % rem;
                    uint j = i + pick;
                    uint temp = scratch[i];
                    scratch[i] = scratch[j];
                    scratch[j] = temp;
                    result[i] = scratch[i];
                }
                return result;
            }

            HashSet<uint> picked = new HashSet<uint>();
            uint[] outIndices = new uint[d];
            int count = 0;
            while (count < d)
            {
                uint pick = rnd.Next() % k;
                if (picked.Add(pick))
                {
                    outIndices[count++] = pick;
                }
            }
            return outIndices;
        }

        /// <summary>
        /// Accelerated 64-bit word-aligned memory XOR
        /// </summary>
        public static void XorBlocks(byte[] dst, byte[] src, int len)
        {
            if (dst == null || src == null) return;
            int length = Math.Min(len, Math.Min(dst.Length, src.Length));
            int i = 0;

            // Process in 8-byte (64-bit) chunks
            int chunks = length / 8;
            for (int c = 0; c < chunks; c++)
            {
                int offset = c * 8;
                ulong dVal = BitConverter.ToUInt64(dst, offset);
                ulong sVal = BitConverter.ToUInt64(src, offset);
                ulong xorVal = dVal ^ sVal;
                byte[] bytes = BitConverter.GetBytes(xorVal);
                Buffer.BlockCopy(bytes, 0, dst, offset, 8);
            }
            i = chunks * 8;

            while (i < length)
            {
                dst[i] ^= src[i];
                i++;
            }
        }
    }
}
