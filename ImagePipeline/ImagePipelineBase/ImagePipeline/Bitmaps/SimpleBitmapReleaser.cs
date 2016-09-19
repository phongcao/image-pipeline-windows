using FBCore.Common.References;
using Windows.Graphics.Imaging;

namespace ImagePipelineBase.ImagePipeline.Bitmaps
{
    /// <summary>
    /// A releaser that just recycles (frees) bitmap memory immediately.
    /// </summary>
    public class SimpleBitmapReleaser : IResourceReleaser<SoftwareBitmap>
    {
        private static SimpleBitmapReleaser _instance;

        /// <summary>
        /// Gets singleton
        /// </summary>
        /// <returns></returns>
        public static SimpleBitmapReleaser GetInstance()
        {
            if (_instance == null)
            {
                _instance = new SimpleBitmapReleaser();
            }

            return _instance;
        }

        private SimpleBitmapReleaser() { }

        /// <summary>
        /// Releases bitmap
        /// </summary>
        /// <param name="value"></param>
        public void Release(SoftwareBitmap value)
        {
            value.Dispose();
        }
    }
}
