#include <Windows.h>
#include "decinit.h"
#include "decoder.h"
#include "libatrac9.h"
#include "structures.h"
#include <stdlib.h>
#include <string.h>

void* LIBATRAC9_API Atrac9GetHandle()
{
	return calloc(1, sizeof(Atrac9Handle));
}

void LIBATRAC9_API Atrac9ReleaseHandle(void* handle)
{
	free(handle);
}

int LIBATRAC9_API Atrac9InitDecoder(void* handle, unsigned char * pConfigData)
{
	return InitDecoder(handle, pConfigData, 16);
}

int LIBATRAC9_API Atrac9Decode(void* handle, const unsigned char *pAtrac9Buffer, short *pPcmBuffer, int *pNBytesUsed)
{
	return Decode(handle, pAtrac9Buffer, (unsigned char*)pPcmBuffer, pNBytesUsed);
}

int LIBATRAC9_API Atrac9GetCodecInfo(void* handle, Atrac9ConfigData* pCodecInfo)
{
	return GetCodecInfo(handle, pCodecInfo);
}

BOOL NTAPI DllMain(PVOID BaseAddress, ULONG Reason, PVOID Reserved)
{
    switch (Reason)
    {
        case DLL_PROCESS_ATTACH:
            InitTables();
            break;

        case DLL_PROCESS_DETACH:
            break;
    }

    return TRUE;
}
