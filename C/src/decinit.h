#pragma once

#include "error_codes.h"
#include "structures.h"

void InitTables();
At9Status InitDecoder(Atrac9Handle* handle, unsigned char * configData, int wlength);
