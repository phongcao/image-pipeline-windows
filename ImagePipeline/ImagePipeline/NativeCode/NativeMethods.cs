using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace ImagePipeline.NativeCode
{
    internal static class NativeMethods
    {
        private const string DllName = "ImagePipelineNative";

        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nativeFree(long lpointer);

        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern long nativeAllocate(int size);

        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nativeCopyToByteArray(long lpointer, byte[] byteArray, int offset, int count);

        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nativeCopyFromByteArray(long lpointer, byte[] byteArray, int offset, int count);

        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nativeMemcpy(long dst, long src, int count);

        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern byte nativeReadByte(long lpointer);

#if HAS_LIBJPEGTURBO
        [DllImport(DllName, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        public static extern void nativeTranscodeJpeg(
            IStream inputStream,
            IStream outputStream,
            int rotationAngle,
            int scaleNominator,
            int quality);
#endif // HAS_LIBJPEGTURBO
    }
}
