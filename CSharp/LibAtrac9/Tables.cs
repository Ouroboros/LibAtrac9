﻿using System;

namespace LibAtrac9
{
    internal static class Tables
    {
        static Tables()
        {
            var huffmanSpectrumA = HuffmanCodebooks.GenerateHuffmanCodebooks(HuffmanSpectrumACodes, HuffmanSpectrumABits, HuffmanSpectrumAGroupSizes);
            var huffmanSpectrumB = HuffmanCodebooks.GenerateHuffmanCodebooks(HuffmanSpectrumBCodes, HuffmanSpectrumBBits, HuffmanSpectrumBGroupSizes);
            HuffmanSpectrum = new[] { huffmanSpectrumA, huffmanSpectrumB };

            HuffmanScaleFactorsUnsigned = HuffmanCodebooks.GenerateHuffmanCodebooks(HuffmanScaleFactorsACodes, HuffmanScaleFactorsABits, HuffmanScaleFactorsGroupSizes);
            HuffmanScaleFactorsSigned = HuffmanCodebooks.GenerateHuffmanCodebooks(HuffmanScaleFactorsBCodes, HuffmanScaleFactorsBBits, HuffmanScaleFactorsGroupSizes);
        }

        public static int MaxHuffPrecision(bool highSampleRate) => highSampleRate ? 1 : 7;
        public static int MinBandCount(bool highSampleRate) => highSampleRate ? 1 : 3;
        public static int MaxExtensionBand(bool highSampleRate) => highSampleRate ? 16 : 18;

        public static readonly int[] SampleRates =
        {
            11025, 12000, 16000, 22050, 24000, 32000, 44100, 48000,
            44100, 48000, 64000, 88200, 96000, 128000, 176400, 192000
        };

        public static readonly byte[] SamplingRateIndexToFrameSamplesPower = { 6, 6, 7, 7, 7, 8, 8, 8, 6, 6, 7, 7, 7, 8, 8, 8 };

        // From sampling rate index
        public static readonly byte[] MaxBandCount = { 8, 8, 12, 12, 12, 18, 18, 18, 8, 8, 12, 12, 12, 16, 16, 16 };
        public static readonly byte[] BandToQuantUnitCount = { 0, 4, 8, 10, 12, 13, 14, 15, 16, 18, 20, 21, 22, 23, 24, 25, 26, 28, 30 };

        public static readonly byte[] QuantUnitToCoeffCount =
        {
            02, 02, 02, 02, 02, 02, 02, 02, 04, 04, 04, 04, 08, 08, 08,
            08, 08, 08, 08, 08, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16
        };

        public static readonly short[] QuantUnitToCoeffIndex =
        {
            0, 2, 4, 6, 8, 10, 12, 14, 16, 20, 24, 28, 32, 40, 48, 56,
            64, 72, 80, 88, 96, 112, 128, 144, 160, 176, 192, 208, 224, 240, 256
        };

        public static readonly byte[] QuantUnitToCodebookIndex =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2,
            2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3
        };

        public static readonly ChannelConfig[] ChannelConfig =
        {
            new ChannelConfig(BlockType.Mono),
            new ChannelConfig(BlockType.Mono, BlockType.Mono),
            new ChannelConfig(BlockType.Stereo),
            new ChannelConfig(BlockType.Stereo, BlockType.Mono, BlockType.LFE, BlockType.Stereo),
            new ChannelConfig(BlockType.Stereo, BlockType.Mono, BlockType.LFE, BlockType.Stereo, BlockType.Stereo),
            new ChannelConfig(BlockType.Stereo, BlockType.Stereo)
        };

        public static readonly HuffmanCodebook[] HuffmanScaleFactorsUnsigned;
        public static readonly HuffmanCodebook[] HuffmanScaleFactorsSigned;
        public static readonly HuffmanCodebook[][][] HuffmanSpectrum;

        public static readonly double[][] ImdctWindow = { GenerateImdctWindow(6), GenerateImdctWindow(7), GenerateImdctWindow(8) };

        public static readonly double[] SpectrumScale = Generate(32, SpectrumScaleFunction);
        public static readonly double[] QuantizerStepSize = Generate(16, QuantizerStepSizeFunction);
        public static readonly double[] QuantizerFineStepSize = Generate(16, QuantizerFineStepSizeFunction);

        public static readonly byte[][] GradientCurves = BitAllocation.GenerateGradientCurves();

        private static double QuantizerStepSizeFunction(int x) => 2.0 / ((1 << (x + 1)) - 1);
        private static double QuantizerFineStepSizeFunction(int x) => QuantizerStepSizeFunction(x) / ushort.MaxValue;
        private static double SpectrumScaleFunction(int x) => Math.Pow(2, x - 15);

        private static double[] GenerateImdctWindow(int frameSizePower)
        {
            int frameSize = 1 << frameSizePower;
            var output = new double[frameSize];

            double[] a1 = GenerateMdctWindow(frameSizePower);

            for (int i = 0; i < frameSize; i++)
            {
                output[i] = a1[i] / (a1[frameSize - 1 - i] * a1[frameSize - 1 - i] + a1[i] * a1[i]);
            }
            return output;
        }

        private static double[] GenerateMdctWindow(int frameSizePower)
        {
            int frameSize = 1 << frameSizePower;
            var output = new double[frameSize];

            for (int i = 0; i < frameSize; i++)
            {
                output[i] = (Math.Sin(((i + 0.5) / frameSize - 0.5) * Math.PI) + 1.0) * 0.5;
            }

            return output;
        }

        private static T[] Generate<T>(int count, Func<int, T> elementGenerator)
        {
            var table = new T[count];
            for (int i = 0; i < count; i++)
            {
                table[i] = elementGenerator(i);
            }
            return table;
        }

        public static readonly byte[][] ScaleFactorWeights =
        {
            new byte[] {
                0, 0, 0, 1, 1, 2, 2, 2, 2, 2, 2, 3, 2, 3, 3, 4, 4, 4, 4, 4, 4, 5, 5, 6, 6, 7, 7, 8, 10, 12, 12, 12
            }, new byte[] {
                3, 2, 2, 1, 1, 1, 1, 1, 0, 1, 1, 1, 0, 0, 0, 1, 0, 1, 1, 1, 1, 1, 1, 2, 3, 3, 4, 5, 7, 10, 10, 10
            }, new byte[] {
                0, 2, 4, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7, 7, 7, 8, 9, 12, 12, 12
            }, new byte[] {
                0, 1, 1, 2, 2, 2, 3, 3, 3, 3, 3, 4, 4, 4, 5, 5, 5, 6, 6, 6, 6, 7, 8, 8, 10, 11, 11, 12, 13, 13, 13, 13
            }, new byte[] {
                0, 2, 2, 3, 3, 4, 4, 5, 4, 5, 5, 5, 5, 6, 7, 8, 8, 8, 8, 9, 9, 9, 10, 10, 11, 12, 12, 13, 13, 14, 14, 14
            }, new byte[] {
                1, 1, 0, 0, 0, 0, 1, 0, 0, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 4, 4, 5, 6, 7, 7, 9, 11, 11, 11
            }, new byte[] {
                0, 5, 8, 10, 11, 11, 12, 12, 12, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13, 12, 12, 12,
                12, 13, 15, 15, 15
            }, new byte[] {
                0, 2, 3, 4, 5, 6, 6, 7, 7, 8, 8, 8, 9, 9, 10, 10, 10, 11, 11, 11, 11, 11, 11, 12, 12, 12, 12, 13, 13,
                15, 15, 15
            }
        };

        public static readonly byte[][] BexGroupInfo =
        {
            new byte[] {16, 21, 0},
            new byte[] {18, 22, 1},
            new byte[] {20, 22, 2},
            new byte[] {21, 22, 3},
            new byte[] {21, 22, 3},
            new byte[] {23, 24, 4},
            new byte[] {23, 24, 4},
            new byte[] {24, 24, 5}
        };

        // [mode][bands]
        public static readonly byte[][] BexEncodedValueCounts =
        {
            new byte[] {0, 0, 0, 4, 4, 2},
            new byte[] {0, 0, 0, 0, 0, 0},
            new byte[] {0, 0, 0, 2, 2, 1},
            new byte[] {0, 0, 0, 2, 2, 2},
            new byte[] {1, 1, 1, 0, 0, 0}
        };

        // [mode][bands][valueIndex]
        public static readonly byte[][][] BexDataLengths =
        {
            new[] {
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {5, 4, 3, 3},
                new byte[] {4, 4, 3, 4},
                new byte[] {4, 5, 0, 0}
            }, new[] {
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0}
            }, new[] {
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {6, 6, 0, 0},
                new byte[] {6, 6, 0, 0},
                new byte[] {6, 0, 0, 0}
            }, new[] {
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {4, 4, 0, 0},
                new byte[] {4, 4, 0, 0},
                new byte[] {4, 4, 0, 0}
            }, new[] {
                new byte[] {3, 0, 0, 0},
                new byte[] {3, 0, 0, 0},
                new byte[] {3, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0},
                new byte[] {0, 0, 0, 0}
            }
        };

        public static readonly double[][] BexMode0Bands3 =
        {
            new[] {
                0.000000e+0, 1.988220e-1, 2.514343e-1, 2.960510e-1,
                3.263550e-1, 3.771362e-1, 3.786926e-1, 4.540405e-1,
                4.877625e-1, 5.262451e-1, 5.447083e-1, 5.737000e-1,
                6.212158e-1, 6.222839e-1, 6.560974e-1, 6.896667e-1,
                7.555542e-1, 7.677917e-1, 7.918091e-1, 7.971497e-1,
                8.188171e-1, 8.446045e-1, 9.790649e-1, 9.822083e-1,
                9.846191e-1, 9.859314e-1, 9.863586e-1, 9.863892e-1,
                9.873352e-1, 9.881287e-1, 9.898682e-1, 9.913330e-1
            }, new[] {
                0.000000e+0, 9.982910e-1, 7.592773e-2, 7.179565e-1,
                9.851379e-1, 5.340271e-1, 9.013672e-1, 6.349182e-1,
                7.226257e-1, 1.948547e-1, 7.628174e-1, 9.873657e-1,
                8.112183e-1, 2.715454e-1, 9.734192e-1, 1.443787e-1,
                4.640198e-1, 3.249207e-1, 3.790894e-1, 8.276367e-2,
                5.954590e-1, 2.864380e-1, 9.806824e-1, 7.929077e-1,
                6.292114e-1, 4.887085e-1, 2.905273e-1, 1.301880e-1,
                3.140869e-1, 5.482483e-1, 4.210815e-1, 1.182861e-1
            }, new[] {
                0.000000e+0, 3.155518e-2, 8.581543e-2, 1.364746e-1,
                1.858826e-1, 2.368469e-1, 2.888184e-1, 3.432617e-1,
                4.012451e-1, 4.623108e-1, 5.271301e-1, 5.954895e-1,
                6.681213e-1, 7.448425e-1, 8.245239e-1, 9.097290e-1
            }, new[] {
                0.000000e+0, 4.418945e-2, 1.303711e-1, 2.273560e-1,
                3.395996e-1, 4.735718e-1, 6.267090e-1, 8.003845e-1
            }, new[] {
                0.000000e+0, 2.804565e-2, 9.683228e-2, 1.849976e-1,
                3.005981e-1, 4.470520e-1, 6.168518e-1, 8.007813e-1
            }
        };

        public static readonly double[][] BexMode0Bands4 =
        {
            new[] {
                0.000000e+0, 2.708740e-1, 3.479614e-1, 3.578186e-1,
                5.083618e-1, 5.299072e-1, 5.819092e-1, 6.381836e-1,
                7.276917e-1, 7.595520e-1, 7.878723e-1, 9.707336e-1,
                9.713135e-1, 9.736023e-1, 9.759827e-1, 9.832458e-1
            }, new[] {
                0.000000e+0, 2.330627e-1, 5.891418e-1, 7.170410e-1,
                2.036438e-1, 1.613464e-1, 6.668701e-1, 9.481201e-1,
                9.769897e-1, 5.111694e-1, 3.522644e-1, 8.209534e-1,
                2.933960e-1, 9.757690e-1, 5.289917e-1, 4.372253e-1
            }, new[] {
                0.000000e+0, 4.360962e-2, 1.056519e-1, 1.590576e-1,
                2.078857e-1, 2.572937e-1, 3.082581e-1, 3.616028e-1,
                4.191589e-1, 4.792175e-1, 5.438538e-1, 6.125183e-1,
                6.841125e-1, 7.589417e-1, 8.365173e-1, 9.148254e-1
            }, new[] {
                0.000000e+0, 4.074097e-2, 1.164551e-1, 2.077026e-1,
                3.184509e-1, 4.532166e-1, 6.124268e-1, 7.932129e-1
            }, new[] {
                0.000000e+0, 8.880615e-3, 2.932739e-2, 5.593872e-2,
                8.825684e-2, 1.259155e-1, 1.721497e-1, 2.270813e-1,
                2.901611e-1, 3.579712e-1, 4.334106e-1, 5.147095e-1,
                6.023254e-1, 6.956177e-1, 7.952881e-1, 8.977356e-1
            }
        };

        public static readonly double[][] BexMode0Bands5 =
        {
            new[] {
                0.000000e+0, 7.379150e-2, 1.806335e-1, 2.687073e-1,
                3.407898e-1, 4.047546e-1, 4.621887e-1, 5.168762e-1,
                5.703125e-1, 6.237488e-1, 6.763611e-1, 7.288208e-1,
                7.808533e-1, 8.337708e-1, 8.874512e-1, 9.418030e-1
            }, new[] {
                0.000000e+0, 7.980347e-2, 1.615295e-1, 1.665649e-1,
                1.822205e-1, 2.185669e-1, 2.292175e-1, 2.456665e-1,
                2.666321e-1, 3.306580e-1, 3.330688e-1, 3.765259e-1,
                4.085083e-1, 4.400024e-1, 4.407654e-1, 4.817505e-1,
                4.924011e-1, 5.320740e-1, 5.893860e-1, 6.131287e-1,
                6.212463e-1, 6.278076e-1, 6.308899e-1, 7.660828e-1,
                7.850647e-1, 7.910461e-1, 7.929382e-1, 8.038330e-1,
                9.834900e-1, 9.846191e-1, 9.852295e-1, 9.862671e-1
            }, new[] {
                0.000000e+0, 6.084290e-1, 3.672791e-1, 3.151855e-1,
                1.488953e-1, 2.571716e-1, 5.103455e-1, 3.311157e-1,
                5.426025e-2, 4.254456e-1, 7.998352e-1, 7.873230e-1,
                5.418701e-1, 2.925110e-1, 8.468628e-2, 1.410522e-1,
                9.819641e-1, 9.609070e-1, 3.530884e-2, 9.729004e-2,
                5.758362e-1, 9.941711e-1, 7.215576e-1, 7.183228e-1,
                2.028809e-1, 9.588623e-2, 2.032166e-1, 1.338806e-1,
                5.003357e-1, 1.874390e-1, 9.804993e-1, 1.107788e-1
            }
        };

        public static readonly double[] BexMode2Scale =
        {
            4.272461e-4, 1.312256e-3, 2.441406e-3, 3.692627e-3,
            4.913330e-3, 6.134033e-3, 7.507324e-3, 8.972168e-3,
            1.049805e-2, 1.223755e-2, 1.406860e-2, 1.599121e-2,
            1.800537e-2, 2.026367e-2, 2.264404e-2, 2.517700e-2,
            2.792358e-2, 3.073120e-2, 3.344727e-2, 3.631592e-2,
            3.952026e-2, 4.275513e-2, 4.608154e-2, 4.968262e-2,
            5.355835e-2, 5.783081e-2, 6.195068e-2, 6.677246e-2,
            7.196045e-2, 7.745361e-2, 8.319092e-2, 8.993530e-2,
            9.759521e-2, 1.056213e-1, 1.138916e-1, 1.236267e-1,
            1.348267e-1, 1.470337e-1, 1.603394e-1, 1.755676e-1,
            1.905823e-1, 2.071228e-1, 2.245178e-1, 2.444153e-1,
            2.658997e-1, 2.897644e-1, 3.146057e-1, 3.450012e-1,
            3.766174e-1, 4.122620e-1, 4.505615e-1, 4.893799e-1,
            5.305481e-1, 5.731201e-1, 6.157837e-1, 6.580811e-1,
            6.985168e-1, 7.435303e-1, 7.865906e-1, 8.302612e-1,
            8.718567e-1, 9.125671e-1, 9.575806e-1, 9.996643e-1
        };

        public static readonly double[] BexMode3Initial =
        {
            3.491211e-1, 5.371094e-1, 6.782227e-1, 7.910156e-1,
            9.057617e-1, 1.024902e+0, 1.156250e+0, 1.290527e+0,
            1.458984e+0, 1.664551e+0, 1.929688e+0, 2.278320e+0,
            2.831543e+0, 3.659180e+0, 5.257813e+0, 8.373047e+0
        };

        public static readonly double[] BexMode3Rate =
        {
            -2.913818e-1, -2.541504e-1, -1.664429e-1, -1.476440e-1,
            -1.342163e-1, -1.220703e-1, -1.117554e-1, -1.026611e-1,
            -9.436035e-2, -8.483887e-2, -7.476807e-2, -6.304932e-2,
            -4.492188e-2, -2.447510e-2, +1.831055e-4, +4.174805e-2
        };

        public static readonly double[] BexMode4Multiplier =
        {
            3.610229e-2, 1.260681e-1, 2.227478e-1, 3.338318e-1,
            4.662170e-1, 6.221313e-1, 7.989197e-1, 9.939575e-1
        };

        private static readonly byte[][] HuffmanScaleFactorsABits =
        {
            null,
            HuffmanCodebooks.ScaleFactorsA1Bits, HuffmanCodebooks.ScaleFactorsA2Bits, HuffmanCodebooks.ScaleFactorsA3Bits,
            HuffmanCodebooks.ScaleFactorsA4Bits, HuffmanCodebooks.ScaleFactorsA5Bits, HuffmanCodebooks.ScaleFactorsA6Bits
        };

        private static readonly short[][] HuffmanScaleFactorsACodes =
        {
            null,
            HuffmanCodebooks.ScaleFactorsA1Codes, HuffmanCodebooks.ScaleFactorsA2Codes, HuffmanCodebooks.ScaleFactorsA3Codes,
            HuffmanCodebooks.ScaleFactorsA4Codes, HuffmanCodebooks.ScaleFactorsA5Codes, HuffmanCodebooks.ScaleFactorsA6Codes
        };

        private static readonly byte[][] HuffmanScaleFactorsBBits =
        {
            null, null,
            HuffmanCodebooks.ScaleFactorsB2Bits, HuffmanCodebooks.ScaleFactorsB3Bits, HuffmanCodebooks.ScaleFactorsB4Bits, HuffmanCodebooks.ScaleFactorsB5Bits
        };

        private static readonly short[][] HuffmanScaleFactorsBCodes =
        {
            null, null,
            HuffmanCodebooks.ScaleFactorsB2Codes, HuffmanCodebooks.ScaleFactorsB3Codes, HuffmanCodebooks.ScaleFactorsB4Codes, HuffmanCodebooks.ScaleFactorsB5Codes
        };

        private static readonly byte[] HuffmanScaleFactorsGroupSizes = { 0, 0, 0, 0, 0, 0, 0 };

        private static readonly byte[][][] HuffmanSpectrumABits =
        {
            null,
            null,
            new[] {HuffmanCodebooks.SpectrumA21Bits, HuffmanCodebooks.SpectrumA22Bits, HuffmanCodebooks.SpectrumA23Bits, HuffmanCodebooks.SpectrumA24Bits},
            new[] {HuffmanCodebooks.SpectrumA31Bits, HuffmanCodebooks.SpectrumA32Bits, HuffmanCodebooks.SpectrumA33Bits, HuffmanCodebooks.SpectrumA34Bits},
            new[] {HuffmanCodebooks.SpectrumA41Bits, HuffmanCodebooks.SpectrumA42Bits, HuffmanCodebooks.SpectrumA43Bits, HuffmanCodebooks.SpectrumA44Bits},
            new[] {HuffmanCodebooks.SpectrumA51Bits, HuffmanCodebooks.SpectrumA52Bits, HuffmanCodebooks.SpectrumA53Bits, HuffmanCodebooks.SpectrumA54Bits},
            new[] {HuffmanCodebooks.SpectrumA61Bits, HuffmanCodebooks.SpectrumA62Bits, HuffmanCodebooks.SpectrumA63Bits, HuffmanCodebooks.SpectrumA64Bits},
            new[] {HuffmanCodebooks.SpectrumA71Bits, HuffmanCodebooks.SpectrumA72Bits, HuffmanCodebooks.SpectrumA73Bits, HuffmanCodebooks.SpectrumA74Bits}
        };

        private static readonly short[][][] HuffmanSpectrumACodes =
        {
            null,
            null,
            new[] {HuffmanCodebooks.SpectrumA21Codes, HuffmanCodebooks.SpectrumA22Codes, HuffmanCodebooks.SpectrumA23Codes, HuffmanCodebooks.SpectrumA24Codes},
            new[] {HuffmanCodebooks.SpectrumA31Codes, HuffmanCodebooks.SpectrumA32Codes, HuffmanCodebooks.SpectrumA33Codes, HuffmanCodebooks.SpectrumA34Codes},
            new[] {HuffmanCodebooks.SpectrumA41Codes, HuffmanCodebooks.SpectrumA42Codes, HuffmanCodebooks.SpectrumA43Codes, HuffmanCodebooks.SpectrumA44Codes},
            new[] {HuffmanCodebooks.SpectrumA51Codes, HuffmanCodebooks.SpectrumA52Codes, HuffmanCodebooks.SpectrumA53Codes, HuffmanCodebooks.SpectrumA54Codes},
            new[] {HuffmanCodebooks.SpectrumA61Codes, HuffmanCodebooks.SpectrumA62Codes, HuffmanCodebooks.SpectrumA63Codes, HuffmanCodebooks.SpectrumA64Codes},
            new[] {HuffmanCodebooks.SpectrumA71Codes, HuffmanCodebooks.SpectrumA72Codes, HuffmanCodebooks.SpectrumA73Codes, HuffmanCodebooks.SpectrumA74Codes}
        };

        private static readonly byte[][] HuffmanSpectrumAGroupSizes =
        {
            new byte[] {0, 0, 0, 0},
            new byte[] {0, 0, 0, 0},
            new byte[] {1, 2, 2, 2},
            new byte[] {1, 1, 1, 1},
            new byte[] {1, 1, 1, 1},
            new byte[] {0, 0, 0, 0},
            new byte[] {0, 0, 0, 0},
            new byte[] {0, 0, 0, 0}
        };

        private static readonly byte[][][] HuffmanSpectrumBBits =
        {
            null,
            null,
            new[] {null, HuffmanCodebooks.SpectrumB22Bits, HuffmanCodebooks.SpectrumB23Bits, HuffmanCodebooks.SpectrumB24Bits},
            new[] {null, HuffmanCodebooks.SpectrumB32Bits, HuffmanCodebooks.SpectrumB33Bits, HuffmanCodebooks.SpectrumB34Bits},
            new[] {null, HuffmanCodebooks.SpectrumB42Bits, HuffmanCodebooks.SpectrumB43Bits, HuffmanCodebooks.SpectrumB44Bits},
            new[] {null, HuffmanCodebooks.SpectrumB52Bits, HuffmanCodebooks.SpectrumB53Bits, HuffmanCodebooks.SpectrumB54Bits},
            new[] {null, HuffmanCodebooks.SpectrumB62Bits, HuffmanCodebooks.SpectrumB63Bits, HuffmanCodebooks.SpectrumB64Bits},
            new[] {null, HuffmanCodebooks.SpectrumB72Bits, HuffmanCodebooks.SpectrumB73Bits, HuffmanCodebooks.SpectrumB74Bits}
        };

        private static readonly short[][][] HuffmanSpectrumBCodes =
        {
            null,
            null,
            new[] {null, HuffmanCodebooks.SpectrumB22Codes, HuffmanCodebooks.SpectrumB23Codes, HuffmanCodebooks.SpectrumB24Codes},
            new[] {null, HuffmanCodebooks.SpectrumB32Codes, HuffmanCodebooks.SpectrumB33Codes, HuffmanCodebooks.SpectrumB34Codes},
            new[] {null, HuffmanCodebooks.SpectrumB42Codes, HuffmanCodebooks.SpectrumB43Codes, HuffmanCodebooks.SpectrumB44Codes},
            new[] {null, HuffmanCodebooks.SpectrumB52Codes, HuffmanCodebooks.SpectrumB53Codes, HuffmanCodebooks.SpectrumB54Codes},
            new[] {null, HuffmanCodebooks.SpectrumB62Codes, HuffmanCodebooks.SpectrumB63Codes, HuffmanCodebooks.SpectrumB64Codes},
            new[] {null, HuffmanCodebooks.SpectrumB72Codes, HuffmanCodebooks.SpectrumB73Codes, HuffmanCodebooks.SpectrumB74Codes}
        };

        private static readonly byte[][] HuffmanSpectrumBGroupSizes =
        {
            new byte[] {0, 0, 0, 0},
            new byte[] {0, 0, 0, 0},
            new byte[] {0, 2, 2, 2},
            new byte[] {0, 1, 1, 1},
            new byte[] {0, 1, 1, 1},
            new byte[] {0, 0, 0, 0},
            new byte[] {0, 0, 0, 0},
            new byte[] {0, 0, 0, 0}
        };
    }
}
