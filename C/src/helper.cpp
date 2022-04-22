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
    Atrac9CodecInfo     info;
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

    PWAV_HEADER wavheader;
    ULONG       sampleCount = factChunk->SampleCount;
    ULONG       superframeSamples = info.frameSamples * info.framesInSuperframe;
    ULONG       pcmBufferSize = (factChunk->SampleCount + factChunk->EncoderDelaySamples + superframeSamples) * sizeof(SHORT) * info.channels + sizeof(*wavheader);
    PSHORT      pcmBuffer = (PSHORT)malloc(pcmBufferSize);

    wavheader = (PWAV_HEADER)pcmBuffer;

    PSHORT out = (PSHORT)(wavheader + 1);

    ULONG samples = 0;

    for (ULONG frameIndex = 0; data < end; frameIndex++)
    {
        ULONG bytesRead;
        result = Atrac9Decode(handle, data, out, (PINT)&bytesRead);
        if (result != 0)
            break;

        out += info.frameSamples;
        data += bytesRead;
        samples += info.frameSamples;

        //printf("offset: %X superframeSize: %X samples: 0x%X %d / %d\n", data - buffer, info.superframeSize, samples - factChunk->EncoderDelaySamples, samples - factChunk->EncoderDelaySamples, sampleCount);

        ULONG remainning = end - data;

        if (remainning >= 4 && *(PULONG)data == 0x01010101)
            break;

        switch (remainning)
        {
            case 1:
                if (data[0] == 0x01)
                    break;
                continue;
                
            case 2:
                if (*(PUSHORT)data == 0x0101)
                    break;
                continue;

            case 3:
                if ((*(PULONG)data & 0x00FFFFFF) == 0x010101)
                    break;

                continue;

            default:
                continue;
        }

        break;
    }

    if (result < 0)
    {
        free(pcmBuffer);
    }
    else
    {
        ULONG dataSize = (PBYTE)(out - info.frameSamples) - (PBYTE)pcmBuffer;

        wavheader->RIFF             = CHUNK_RIFF;
        wavheader->Size             = sizeof(*wavheader) - 8 + dataSize;
        wavheader->WAVE             = CHUNK_WAVE;
        wavheader->fmt              = CHUNK_FMT;
        wavheader->FormatLength     = 0x10;
        wavheader->FormatTag        = 0x01;
        wavheader->Channels         = info.channels;
        wavheader->SamplesPerSec    = info.samplingRate;
        wavheader->AvgBytesPerSec   = info.samplingRate * sizeof(SHORT);
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
