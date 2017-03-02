/**
 * Copyright (c) 2015-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. An additional grant
 * of patent rights can be found in the PATENTS file in the same directory.
 */

#include "JpegTranscoder.h"

void NativeTranscodeJpeg(
	LPSTREAM inputStream,
	LPSTREAM outputStream,
	int rotationAngle,
	int scaleNominator,
	int quality)
{
	STATSTG stat_info;
	long bytesRead;
	long bytesWrite;

	inputStream->lpVtbl->Stat(inputStream, &stat_info, STATFLAG_NONAME);
	uint8_t* buffer = malloc(stat_info.cbSize.LowPart);
	inputStream->lpVtbl->Read(inputStream, buffer, stat_info.cbSize.LowPart, &bytesRead);
	outputStream->lpVtbl->Write(outputStream, buffer, stat_info.cbSize.LowPart, &bytesWrite);
}
