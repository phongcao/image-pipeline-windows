using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace ImagePipeline.NativeCode
{
    internal static class NativeMethods
    {
        private const string DllName = "ImagePipelineNative";

        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void NativeFree(long lpointer);

        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern long NativeAllocate(int size);

        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void NativeCopyToByteArray(long lpointer, byte[] byteArray, int offset, int count);

        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void NativeCopyFromByteArray(long lpointer, byte[] byteArray, int offset, int count);

        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void NativeMemcpy(long dst, long src, int count);

        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern byte NativeReadByte(long lpointer);

        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void NativeTranscodeJpeg(
            IStream inputStream,
            IStream outputStream,
            int rotationAngle,
            int scaleNominator,
            int quality);
    }
}
