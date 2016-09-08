#include "pch.h"
#include <string.h>
#include <stdint.h>
#include "NativeMemoryChunk.h"

#define ARRAY_SIZE(a) (sizeof(a) / sizeof((a)[0]))
#define LONG_TO_PTR(j) ((void*) (intptr_t) (j))
#define PTR_TO_LONG(p) ((int64) (intptr_t) (p))

using namespace ImagePipelineNative;


int64 NativeMemoryChunk::NativeAllocate(int size)
{
	void* pointer = malloc(size);
	if (!pointer) 
	{
		throw ref new Exception(-1, "Could not allocate memory");
		return 0;
	}

	return PTR_TO_LONG(pointer);
}

void NativeMemoryChunk::NativeFree(int64 lpointer)
{
	free(LONG_TO_PTR(lpointer));
}

void NativeMemoryChunk::NativeCopyToByteArray(int64 lpointer, const Array<uint8>^ byteArray, int offset, int count)
{
	CopyMemory(byteArray->Data, LONG_TO_PTR(lpointer + offset), count);
}

void NativeMemoryChunk::NativeCopyFromByteArray(int64 lpointer, const Array<uint8>^ byteArray, int offset, int count)
{
	CopyMemory(LONG_TO_PTR(lpointer + offset), byteArray->Data, count);
}

void NativeMemoryChunk::NativeMemcpy(int64 dst, int64 src, int count)
{
	memcpy(LONG_TO_PTR(dst), LONG_TO_PTR(src), count);
}

uint8 NativeMemoryChunk::NativeReadByte(int64 lpointer)
{
	return *((uint8*)LONG_TO_PTR(lpointer));
}
