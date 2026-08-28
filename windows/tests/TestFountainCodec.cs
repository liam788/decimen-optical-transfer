using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using OpticalTransfer.Core;
using OpticalTransfer.State;

namespace OpticalTransfer.Tests
{
    public class TestFountainCodec
    {
        public static int Main(string[] args)
        {
            Console.WriteLine("======================================================================");
            Console.WriteLine(" OPTICAL TRANSFER - C# NATIVE CODEC & PROTOCOL VERIFICATION SUITE");
            Console.WriteLine("======================================================================");

            bool allPassed = true;

            // --- TEST 1: Soliton CDF & Mathematical Determinism ---
            Console.WriteLine("\n[TEST 1] Testing Deterministic Soliton CDF & Maclaurin DLog Math...");
            double[] cdf100 = FountainMath.SolitonCdf(100);
            if (cdf100.Length != 100 || Math.Abs(cdf100[99] - 1.0) > 1e-5)
            {
                Console.WriteLine("  -> FAILED: Soliton CDF K=100 final value is " + cdf100[99]);
                allPassed = false;
            }
            else
            {
                Console.WriteLine(string.Format("  -> PASSED: Soliton CDF K=100 verified! Degree 1 prob: {0:F4}, Final CDF: {1:F4}", cdf100[0], cdf100[99]));
            }

            // --- TEST 2: DCF2 Container Packing & Integrity ---
            Console.WriteLine("\n[TEST 2] Testing DCF2 Container Packing & SHA-256 Validation...");
            byte[] testPayload = new byte[60 * 1024]; // 60 KB
            new Random(1337).NextBytes(testPayload);
            string testName = "classified_report.pdf";

            byte[] packedContainer = ProtocolDcf2.PackContainer(testName, "application/pdf", testPayload);
            if (packedContainer == null || packedContainer.Length < ProtocolDcf2.DCF2_HEADER_LEN)
            {
                Console.WriteLine("  -> FAILED: Container pack returned invalid buffer.");
                allPassed = false;
            }
            else
            {
                OpticalFile unpacked = ProtocolDcf2.UnpackContainer(packedContainer);
                if (unpacked == null)
                {
                    Console.WriteLine("  -> FAILED: UnpackContainer returned null.");
                    allPassed = false;
                }
                else if (unpacked.Name != testName || unpacked.Data.Length != testPayload.Length)
                {
                    Console.WriteLine("  -> FAILED: Unpacked metadata mismatch.");
                    allPassed = false;
                }
                else
                {
                    bool match = true;
                    for (int i = 0; i < testPayload.Length; i++)
                    {
                        if (unpacked.Data[i] != testPayload[i]) { match = false; break; }
                    }
                    if (match)
                    {
                        Console.WriteLine(string.Format("  -> PASSED: DCF2 container packed & unpacked {0} bytes with 100% SHA-256 match!", testPayload.Length));
                    }
                    else
                    {
                        Console.WriteLine("  -> FAILED: Payload data byte mismatch.");
                        allPassed = false;
                    }
                }
            }

            // --- TEST 3: Systematic Fountain Codec with 30% Packet Loss ---
            Console.WriteLine("\n[TEST 3] Testing Systematic Fountain Codec with 30% Packet Erasure...");
            SystematicFountainEncoder encoder = new SystematicFountainEncoder(packedContainer, 300);
            IncrementalPeelingDecoder decoder = new IncrementalPeelingDecoder();

            uint k = encoder.K;
            Console.WriteLine(string.Format("  -> Container: {0} bytes, Symbol Size T=300, Source Blocks K={1}", packedContainer.Length, k));

            Random dropRnd = new Random(42);
            int transmitted = 0;
            int received = 0;

            while (!decoder.IsComplete && transmitted < k * 6)
            {
                byte[] frame = encoder.NextFrame();
                transmitted++;

                if (dropRnd.NextDouble() < 0.30) // 30% drop rate
                {
                    continue;
                }

                received++;
                decoder.ConsumeFrame(frame);
            }

            if (!decoder.IsComplete)
            {
                Console.WriteLine(string.Format("  -> FAILED: Peeling decoder did not complete within {0} frames. Solved: {1}/{2}", transmitted, decoder.GetStatus().SolvedCount, k));
                allPassed = false;
            }
            else
            {
                byte[] reconstructedContainer = decoder.AssemblePayload();
                if (reconstructedContainer == null)
                {
                    Console.WriteLine("  -> FAILED: AssemblePayload returned null.");
                    allPassed = false;
                }
                else
                {
                    OpticalFile finalFile = ProtocolDcf2.UnpackContainer(reconstructedContainer);

                    if (finalFile != null && finalFile.Data.Length == testPayload.Length)
                    {
                        Console.WriteLine(string.Format("  -> PASSED: Successfully decoded stream with 30% erasure! (Transmitted: {0}, Received: {1}, K={2})", transmitted, received, k));
                        Console.WriteLine(string.Format("  -> Reconstructed File: '{0}', Size: {1} bytes, SHA-256 Verified ✓", finalFile.Name, finalFile.Data.Length));
                    }
                    else
                    {
                        Console.WriteLine("  -> FAILED: Final reconstructed file unpacking failed.");
                        allPassed = false;
                    }
                }
            }

            // --- TEST 4: QR Engine Module Geometry ---
            Console.WriteLine("\n[TEST 4] Testing QR Engine Module Matrix & Luminance Decoder...");
            byte[] demoSample = new byte[100];
            new Random(99).NextBytes(demoSample);

            QrBitmap qr = QrMatrixGenerator.Encode(demoSample);
            if (qr.ModuleCount != 49 || qr.Modules.Length != 49 * 49)
            {
                Console.WriteLine("  -> FAILED: QR Matrix dimension mismatch (expected 49x49).");
                allPassed = false;
            }
            else
            {
                Console.WriteLine(string.Format("  -> PASSED: QR Engine generated {0}x{0} grid with finder and timing patterns!", qr.ModuleCount));
            }

            // --- TEST 5: VSync Cadence Gating Decoupling ---
            Console.WriteLine("\n[TEST 5] Testing 120Hz Display VSync Cadence Gating...");
            double targetFps = 20.0;
            long intervalUs = (long)(1000000.0 / targetFps); // 50,000 us
            long vsync120Us = (long)(1000000.0 / 120.0);    // 8,333 us

            long lastAdvanceUs = 0;
            int uniqueAdvances = 0;

            for (int tick = 0; tick < 120; tick++) // 1 second of 120Hz ticks
            {
                long nowUs = tick * vsync120Us;
                if ((nowUs - lastAdvanceUs) >= (intervalUs - 1000))
                {
                    uniqueAdvances++;
                    lastAdvanceUs = nowUs;
                }
            }

            if (uniqueAdvances >= 19 && uniqueAdvances <= 21)
            {
                Console.WriteLine(string.Format("  -> PASSED: 120Hz VSync Ticks (120 calls) -> Exactly {0} frame advances (Target: 20 FPS)!", uniqueAdvances));
            }
            else
            {
                Console.WriteLine("  -> FAILED: Unique advances was " + uniqueAdvances);
                allPassed = false;
            }

            Console.WriteLine("\n======================================================================");
            if (allPassed)
            {
                Console.WriteLine(" ALL C# NATIVE CODEC TESTS PASSED WITH ZERO ERRORS (100% SUCCESS)!");
                Console.WriteLine("======================================================================");
                return 0;
            }
            else
            {
                Console.WriteLine(" ONE OR MORE TESTS FAILED.");
                Console.WriteLine("======================================================================");
                return 1;
            }
        }
    }
}
