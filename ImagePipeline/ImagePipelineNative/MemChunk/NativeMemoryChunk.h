#pragma once

using namespace Platform;


namespace ImagePipelineNative
{
	public ref class NativeMemoryChunk sealed
	{
		public:	
			static int64 NativeAllocate(int size);

			static void NativeFree(int64 lpointer);

			static void NativeCopyToByteArray(int64 lpointer, const Array<uint8>^ byteArray, int offset, int count);

			static void NativeCopyFromByteArray(int64 lpointer, const Array<uint8>^ byteArray, int offset, int count);

			static void NativeMemcpy(int64 dst, int64 src, int count);

			static uint8 NativeReadByte(int64 lpointer);
	};
}