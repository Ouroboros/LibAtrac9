#include "tables.h"

//double MdctWindow[3][256];
//double ImdctWindow[3][256];
double SinTables[9][256];
double CosTables[9][256];
int ShuffleTables[9][256];

const ChannelConfig ChannelConfigs[6] =
{
	{1, 1, {Mono}},
	{2, 2, {Mono, Mono}},
	{1, 2, {Stereo}},
	{4, 6, {Stereo, Mono, LFE, Stereo}},
	{5, 8, {Stereo, Mono, LFE, Stereo, Stereo}},
	{2, 4, {Stereo, Stereo}},
};

const unsigned char MaxHuffPrecision[2] = { 7, 1 };
const unsigned char MinBandCount[2] = { 3, 1 };
const unsigned char MaxExtensionBand[2] = { 18, 16 };

const unsigned char SamplingRateIndexToFrameSamplesPower[16] =
{ 6, 6, 7, 7, 7, 8, 8, 8, 6, 6, 7, 7, 7, 8, 8, 8 };

const unsigned char MaxBandCount[16] =
{ 8, 8, 12, 12, 12, 18, 18, 18, 8, 8, 12, 12, 12, 16, 16, 16 };

const unsigned char BandToQuantUnitCount[19] =
{ 0, 4, 8, 10, 12, 13, 14, 15, 16, 18, 20, 21, 22, 23, 24, 25, 26, 28, 30 };

const unsigned char QuantUnitToCoeffCount[30] =
{
	2, 2, 2, 2, 2,  2,  2,  2,  4,  4,  4,  4,  8,  8,  8,
	8, 8, 8, 8, 8, 16, 16, 16, 16, 16, 16, 16, 16, 16, 16
};

const int QuantUnitToCoeffIndex[31] =
{
	0, 2, 4, 6, 8, 10, 12, 14, 16, 20, 24, 28, 32, 40, 48, 56,
	64, 72, 80, 88, 96, 112, 128, 144, 160, 176, 192, 208, 224, 240, 256
};

const unsigned char QuantUnitToCodebookIndex[30] =
{
	0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2,
	2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3
};

const int SampleRates[16] =
{
	11025, 12000, 16000, 22050, 24000, 32000, 44100, 48000,
	44100, 48000, 64000, 88200, 96000, 128000, 176400, 192000
};

const unsigned char ScaleFactorWeights[8][32] = {
	{ 0, 0, 0, 1, 1, 2, 2, 2, 2, 2, 2, 3, 2, 3, 3, 4, 4, 4, 4, 4, 4, 5, 5, 6, 6,
	7, 7, 8, 10, 12, 12, 12 },
	{ 3, 2, 2, 1, 1, 1, 1, 1, 0, 1, 1, 1, 0, 0, 0, 1, 0, 1, 1, 1, 1, 1, 1, 2, 3,
	3, 4, 5, 7, 10, 10, 10 },
	{ 0, 2, 4, 5, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 6, 7, 7,
	7, 7, 8, 9, 12, 12, 12 },
	{ 0, 1, 1, 2, 2, 2, 3, 3, 3, 3, 3, 4, 4, 4, 5, 5, 5, 6, 6, 6, 6, 7, 8, 8, 10,
	11, 11, 12, 13, 13, 13, 13 },
	{ 0, 2, 2, 3, 3, 4, 4, 5, 4, 5, 5, 5, 5, 6, 7, 8, 8, 8, 8, 9, 9, 9, 10, 10,
	11, 12, 12, 13, 13, 14, 14, 14 },
	{ 1, 1, 0, 0, 0, 0, 1, 0, 0, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 3, 3, 3, 4, 4, 5,
	6, 7, 7, 9, 11, 11, 11 },
	{ 0, 5, 8, 10, 11, 11, 12, 12, 12, 13, 13, 13, 13, 13, 13, 13, 13, 13, 13,
	13, 13, 13, 13, 13, 12, 12, 12, 12, 13, 15, 15, 15 },
	{ 0, 2, 3, 4, 5, 6, 6, 7, 7, 8, 8, 8, 9, 9, 10, 10, 10, 11, 11, 11, 11, 11,
	11, 12, 12, 12, 12, 13, 13, 15, 15, 15 }
};

const double SpectrumScale[32] =
{
	3.0517578125e-5, 6.1035156250e-5, 1.2207031250e-4, 2.4414062500e-4,
	4.8828125000e-4, 9.7656250000e-4, 1.9531250000e-3, 3.9062500000e-3,
	7.8125000000e-3, 1.5625000000e-2, 3.1250000000e-2, 6.2500000000e-2,
	1.2500000000e-1, 2.5000000000e-1, 5.0000000000e-1, 1.0000000000e+0,
	2.0000000000e+0, 4.0000000000e+0, 8.0000000000e+0, 1.6000000000e+1,
	3.2000000000e+1, 6.4000000000e+1, 1.2800000000e+2, 2.5600000000e+2,
	5.1200000000e+2, 1.0240000000e+3, 2.0480000000e+3, 4.0960000000e+3,
	8.1920000000e+3, 1.6384000000e+4, 3.2768000000e+4, 6.5536000000e+4
};

const double QuantizerInverseStepSize[16] =
{
	0.5, 1.5, 3.5, 7.5, 15.5, 31.5, 63.5, 127.5,
	255.5, 511.5, 1023.5, 2047.5, 4095.5, 8191.5, 16383.5, 32767.5
};

const double QuantizerStepSize[16] =
{
	2.0000000000000000e+0, 6.6666666666666663e-1, 2.8571428571428570e-1, 1.3333333333333333e-1,
	6.4516129032258063e-2, 3.1746031746031744e-2, 1.5748031496062992e-2, 7.8431372549019607e-3,
	3.9138943248532287e-3, 1.9550342130987292e-3, 9.7703957010258913e-4, 4.8840048840048840e-4,
	2.4417043096081065e-4, 1.2207776353537203e-4, 6.1037018951994385e-5, 3.0518043793392844e-5
};

const double QuantizerFineStepSize[16] =
{
	3.0518043793392844e-05, 1.0172681264464281e-05, 4.3597205419132631e-06, 2.0345362528928561e-06,
	9.8445302559331759e-07, 4.8441339354591809e-07, 2.4029955742829012e-07, 1.1967860311134448e-07,
	5.9722199204291275e-08, 2.9831909866464167e-08, 1.4908668194134265e-08, 7.4525137468602791e-09,
	3.7258019525568114e-09, 1.8627872668859698e-09, 9.3136520869755679e-10, 4.6567549848772173e-10
};
