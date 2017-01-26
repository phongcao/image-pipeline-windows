using ImagePipeline.Image;

namespace ImagePipeline.Decoder
{
    /// <summary>
    /// Progressive JPEG config.
    /// </summary>
    public interface IProgressiveJpegConfig
    {
        /// <summary>
        /// Gets the next scan-number that should be decoded after the given scan-number.
        /// </summary>
        int GetNextScanNumberToDecode(int scanNumber);

        /// <summary>
        /// Gets the quality information for the given scan-number.
        /// </summary>
        IQualityInfo GetQualityInfo(int scanNumber);
    }
}
