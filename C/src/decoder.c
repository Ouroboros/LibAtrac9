#include "decoder.h"
#include "band_extension.h"
#include "bit_reader.h"
#include "imdct.h"
#include "quantization.h"
#include "tables.h"
#include "unpack.h"
#include "utility.h"
#include <string.h>

static At9Status DecodeFrame(Atrac9Handle* handle, Frame* frame, BitReaderCxt* br);
static void ImdctBlock(Atrac9Handle* handle, Block* block);
static void ApplyIntensityStereo(Block* block);
static void PcmFloatToShort(Frame* frame, short* pcmOut);

At9Status Decode(Atrac9Handle* handle, const unsigned char* audio, unsigned char* pcm, int* bytesUsed)
{
	BitReaderCxt br;
	InitBitReaderCxt(&br, audio);
	ERROR_CHECK(DecodeFrame(handle, &handle->Frame, &br));

	PcmFloatToShort(&handle->Frame, (short*)pcm);

	*bytesUsed = br.Position / 8;
	return ERR_SUCCESS;
}

static At9Status DecodeFrame(Atrac9Handle* handle, Frame* frame, BitReaderCxt* br)
{
	ERROR_CHECK(UnpackFrame(frame, br));

	for (int i = 0; i < frame->Config->ChannelConfig.BlockCount; i++)
	{
		Block* block = &frame->Blocks[i];

		DequantizeSpectra(block);
		ApplyIntensityStereo(block);
		ScaleSpectrumBlock(block);
		ApplyBandExtension(block);
		ImdctBlock(handle, block);
	}

	return ERR_SUCCESS;
}

void PcmFloatToShort(Frame* frame, short* pcmOut)
{
	const int channelCount = frame->Config->ChannelCount;
	const int sampleCount = frame->Config->FrameSamples;
	Channel** channels = frame->Channels;
	int i = 0;

	for (int smpl = 0; smpl < sampleCount; smpl++)
	{
		for (int ch = 0; ch < channelCount; ch++, i++)
		{
			pcmOut[i] = Clamp16(Round(channels[ch]->Pcm[smpl]));
		}
	}
}

static void ImdctBlock(Atrac9Handle* handle, Block* block)
{
	for (int i = 0; i < block->ChannelCount; i++)
	{
		Channel* channel = &block->Channels[i];

		RunImdct(handle, &channel->Mdct, channel->Spectra, channel->Pcm);
	}
}

static void ApplyIntensityStereo(Block* block)
{
	if (block->BlockType != Stereo) return;

	const int totalUnits = block->QuantizationUnitCount;
	const int stereoUnits = block->StereoQuantizationUnit;
	if (stereoUnits >= totalUnits) return;

	Channel* source = &block->Channels[block->PrimaryChannelIndex == 0 ? 0 : 1];
	Channel* dest = &block->Channels[block->PrimaryChannelIndex == 0 ? 1 : 0];

	for (int i = stereoUnits; i < totalUnits; i++)
	{
		const int sign = block->JointStereoSigns[i];
		for (int sb = QuantUnitToCoeffIndex[i]; sb < QuantUnitToCoeffIndex[i + 1]; sb++)
		{
			if (sign > 0)
			{
				dest->Spectra[sb] = -source->Spectra[sb];
			}
			else
			{
				dest->Spectra[sb] = source->Spectra[sb];
			}
		}
	}
}

int GetCodecInfo(Atrac9Handle* handle, ConfigData* pCodecInfo)
{
    *pCodecInfo = handle->Config;
	return ERR_SUCCESS;
}
