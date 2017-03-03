/**
 * Copyright (c) 2015-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. An additional grant
 * of patent rights can be found in the PATENTS file in the same directory.
 */

#include "NativeMemoryChunk.h"

int64_t nativeAllocate(int size)
{
	void* pointer = malloc(size);
	if (!pointer)
	{
		return 0;
	}

	return PTR_TO_LONG(pointer);
}

void nativeFree(int64_t lpointer)
{
	free(LONG_TO_PTR(lpointer));
}

void nativeCopyToByteArray(int64_t lpointer, uint8_t* byteArray, int offset, int count)
{
	memcpy(byteArray, LONG_TO_PTR(lpointer + offset), count);
}

void nativeCopyFromByteArray(int64_t lpointer, uint8_t* byteArray, int offset, int count)
{
	memcpy(LONG_TO_PTR(lpointer + offset), byteArray, count);
}

void nativeMemcpy(int64_t dst, int64_t src, int count)
{
	memcpy(LONG_TO_PTR(dst), LONG_TO_PTR(src), count);
}

uint8_t nativeReadByte(int64_t lpointer)
{
	return *((uint8_t*)LONG_TO_PTR(lpointer));
}
