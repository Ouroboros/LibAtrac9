#pragma once
#ifdef __cplusplus
extern "C" {
#endif

#ifdef _MSC_VER
#ifdef COMPILING_DLL 
#define DLLEXPORT __declspec(dllexport)
#else
#define DLLEXPORT __declspec(dllimport)  
#endif
#else
#define DLLEXPORT
#endif

#define LIBATRAC9_API __stdcall

#define ATRAC9_CONFIG_DATA_SIZE 4

#include "structures.h"

typedef ConfigData Atrac9ConfigData;

void* LIBATRAC9_API Atrac9GetHandle(void);
void LIBATRAC9_API Atrac9ReleaseHandle(void* handle);

int LIBATRAC9_API Atrac9InitDecoder(void* handle, unsigned char *pConfigData);
int LIBATRAC9_API Atrac9Decode(void* handle, const unsigned char *pAtrac9Buffer, short *pPcmBuffer, int *pNBytesUsed);

int LIBATRAC9_API Atrac9GetCodecInfo(void* handle, Atrac9ConfigData *pCodecInfo);

int LIBATRAC9_API Atrac9DecodeBuffer(void* at9Buffer, int at9BufferSize, void** outputBuffer, int *outputSize, int* wfxFormatOffset, int* dataOffset);
void LIBATRAC9_API Atrac9FreeBuffer(void* buffer);

#ifdef __cplusplus
}
#endif
