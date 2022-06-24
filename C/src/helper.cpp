#include <Windows.h>
#include <stdio.h>
#include "libatrac9.h"

#define CHUNK_RIFF 'FFIR'
#define CHUNK_WAVE 'EVAW'
#define CHUNK_FMT  ' tmf'
#define CHUNK_FACT 'tcaf'
#define CHUNK_DATA 'atad'

typedef struct
{
    ULONG   RIFF;                    // TAG4('RIFF')
    ULONG   Size;
    ULONG   WAVE;                    // TAG4('WAVE')
    ULONG   fmt;                     // TAG4('fmt ')
    ULONG   FormatLength;
    USHORT  FormatTag;
    USHORT  Channels;
    ULONG   SamplesPerSec;
    ULONG   AvgBytesPerSec;
    USHORT  BlockAlign;
    USHORT  BitsPerSample;
    ULONG   data;                    // TAG4('data')
    ULONG   DataSize;

} WAV_HEADER, *PWAV_HEADER;

typedef struct
{
    ULONG   ID;
    ULONG   Size;
} *PWAVE_CHUNK_HEADER;

typedef struct
{
    ULONG SampleCount;
    ULONG InputOverlapDelaySamples;
    ULONG EncoderDelaySamples;

} *PAT9_FACT_CHUNK;

int LIBATRAC9_API Atrac9DecodeBuffer(void* at9Buffer, int at9BufferSize, void** outputBuffer, int *outputSize, int* wfxFormatOffset, int* dataOffset)
{
    PVOID               handle;
    PBYTE               buffer, end;
    PBYTE               configData;
    Atrac9ConfigData    info;
    PBYTE               data;
    PAT9_FACT_CHUNK     factChunk;
    PWAVE_CHUNK_HEADER  chunk;

    *outputBuffer = nullptr;
    *outputSize = 0;

    buffer = (PBYTE)at9Buffer;
    end = buffer + at9BufferSize;

    chunk = (PWAVE_CHUNK_HEADER)buffer;

    if (chunk->ID != CHUNK_RIFF || chunk[1].ID != CHUNK_WAVE)
        return -1;

    if (chunk->Size + 8 != at9BufferSize)
        return -2;

    configData = nullptr;
    factChunk = nullptr;
    data = nullptr;
    for (PBYTE p = buffer + 0x0C; p < end; )
    {
        chunk = (PWAVE_CHUNK_HEADER)p;
        p += chunk->Size + sizeof(*chunk);

        switch (chunk->ID)
        {
            case CHUNK_FMT:
                if (chunk->Size == 0x34)
                    configData = (PBYTE)chunk + 0x34;
                break;

            case CHUNK_FACT:
                factChunk = (PAT9_FACT_CHUNK)(chunk + 1);
                break;

            case CHUNK_DATA:
                data = (PBYTE)(chunk + 1);
                break;
        }
    }

    if (configData == nullptr)
        return -3;

    if (factChunk == nullptr)
        return -4;

    if (data == nullptr)
        return -5;

    int result;

    handle = Atrac9GetHandle();
    if (handle == nullptr)
        return -6;

    result = Atrac9InitDecoder(handle, configData);
    if (result != 0)
        goto CLEANUP;

    result = Atrac9GetCodecInfo(handle, &info);
    if (result != 0)
        goto CLEANUP;

    // printf("SampleRateIndex             = %d\n", info.SampleRateIndex);
    // printf("ChannelConfigIndex          = %d\n", info.ChannelConfigIndex);
    // printf("FrameBytes                  = %d\n", info.FrameBytes);
    // printf("SuperframeIndex             = %d\n", info.SuperframeIndex);
    // printf("ChannelConfig.BlockCount    = %d\n", info.ChannelConfig.BlockCount);
    // printf("ChannelConfig.ChannelCount  = %d\n", info.ChannelConfig.ChannelCount);
    // printf("ChannelCount                = %d\n", info.ChannelCount);
    // printf("SampleRate                  = %d\n", info.SampleRate);
    // printf("HighSampleRate              = %d\n", info.HighSampleRate);
    // printf("FramesPerSuperframe         = %d\n", info.FramesPerSuperframe);
    // printf("FrameSamplesPower           = %d\n", info.FrameSamplesPower);
    // printf("FrameSamples                = %d\n", info.FrameSamples);
    // printf("SuperframeBytes             = %d\n", info.SuperframeBytes);
    // printf("SuperframeSamples           = %d\n", info.SuperframeSamples);

    PWAV_HEADER wavheader;
    ULONG       sampleCount = factChunk->SampleCount;
    ULONG       superframeSamples = info.FrameSamples * info.FramesPerSuperframe;
    ULONG       pcmBufferSize = (factChunk->SampleCount + factChunk->EncoderDelaySamples + superframeSamples) * sizeof(SHORT) * info.ChannelCount + sizeof(*wavheader);
    PSHORT      pcmBuffer = (PSHORT)malloc(pcmBufferSize);

    wavheader = (PWAV_HEADER)pcmBuffer;

    PSHORT out = (PSHORT)(wavheader + 1);

    ULONG samples = 0;

    for (ULONG superFrameIndex = 0; data < end; )
    {
        ULONG bytesRead;

        result = 0;
        PBYTE p = data;
        for (ULONG i = 0; i != info.FramesPerSuperframe; i++)
        {
            result = Atrac9Decode(handle, p, out, (PINT)&bytesRead);
            if (result != 0)
                break;

            p += bytesRead;
            out += info.FrameSamples * info.ChannelCount;
            samples += info.FrameSamples * info.ChannelCount;

            //printf("frame: %d offset: %X superframeSize: %X samples: 0x%X %d / %d\n", superFrameIndex++, p - buffer, info.SuperframeBytes, samples - factChunk->EncoderDelaySamples, samples - factChunk->EncoderDelaySamples, sampleCount);
        }

        if (result != 0)
            break;

        data += info.SuperframeBytes;
    }

    if (result < 0)
    {
        free(pcmBuffer);
    }
    else
    {
        ULONG dataSize = (PBYTE)(out - info.FrameSamples) - (PBYTE)pcmBuffer;

        wavheader->RIFF             = CHUNK_RIFF;
        wavheader->Size             = sizeof(*wavheader) - 8 + dataSize;
        wavheader->WAVE             = CHUNK_WAVE;
        wavheader->fmt              = CHUNK_FMT;
        wavheader->FormatLength     = 0x10;
        wavheader->FormatTag        = 0x01;
        wavheader->Channels         = info.ChannelCount;
        wavheader->SamplesPerSec    = info.SampleRate;
        wavheader->AvgBytesPerSec   = info.SampleRate * sizeof(SHORT);
        wavheader->BlockAlign       = 2;
        wavheader->BitsPerSample    = 16;
        wavheader->data             = CHUNK_DATA;
        wavheader->DataSize         = dataSize;

        *outputBuffer = pcmBuffer;
        *outputSize = wavheader->Size + 8;

        if (wfxFormatOffset != nullptr)
            *wfxFormatOffset = (PBYTE)&wavheader->FormatTag - (PBYTE)wavheader;

        if (dataOffset != nullptr)
            *dataOffset = (PBYTE)(wavheader + 1) - (PBYTE)wavheader;
    }

CLEANUP:
    Atrac9ReleaseHandle(handle);

    return result;
}

void LIBATRAC9_API Atrac9FreeBuffer(void* buffer)
{
    free(buffer);
}
