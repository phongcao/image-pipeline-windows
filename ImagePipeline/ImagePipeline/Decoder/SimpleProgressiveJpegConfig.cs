using FBCore.Common.Internal;
using ImagePipeline.Image;
using System.Collections.Generic;

namespace ImagePipeline.Decoder
{
    /// <summary>
    /// Simple progressive jpeg configuration.
    /// </summary>
    public class SimpleProgressiveJpegConfig : IProgressiveJpegConfig
    {
        private readonly IDynamicValueConfig _dynamicValueConfig;

        /// <summary>
        /// Instantiates the <see cref="SimpleProgressiveJpegConfig"/>.
        /// </summary>
        public SimpleProgressiveJpegConfig() : 
            this(new DefaultDynamicValueConfig())
        {
        }

        /// <summary>
        /// Instantiates the <see cref="SimpleProgressiveJpegConfig"/>.
        /// </summary>
        public SimpleProgressiveJpegConfig(IDynamicValueConfig dynamicValueConfig)
        {
            _dynamicValueConfig = Preconditions.CheckNotNull(dynamicValueConfig);
        }

        /// <summary>
        /// Gets the next scan-number that should be decoded after the given
        /// scan-number.
        /// </summary>
        public int GetNextScanNumberToDecode(int scanNumber)
        {
            IList<int> scansToDecode = _dynamicValueConfig.GetScansToDecode();
            if (scansToDecode == null || scansToDecode.Count == 0)
            {
                return scanNumber + 1;
            }

            int size = scansToDecode.Count;
            for (int i = 0; i < size; i++)
            {
                int val = scansToDecode[i];
                if (val > scanNumber)
                {
                    return val;
                }
            }

            return int.MaxValue;
        }

        /// <summary>
        /// Gets the quality information for the given scan-number.
        /// </summary>
        public IQualityInfo GetQualityInfo(int scanNumber)
        {
            return ImmutableQualityInfo.of(
                scanNumber,
                /* isOfGoodEnoughQuality */ scanNumber >= _dynamicValueConfig.GoodEnoughScanNumber,
                /* isOfFullQuality */ false);
        }

        /// <summary>
        /// Dynamic value configuration interface.
        /// </summary>
        public interface IDynamicValueConfig
        {
            /// <summary>
            /// Gets scans to decode.
            /// </summary>
            IList<int> GetScansToDecode();

            /// <summary>
            /// Gets good enough scan number.
            /// </summary>
            int GoodEnoughScanNumber { get; }
        }

        private class DefaultDynamicValueConfig : IDynamicValueConfig
        {
            public IList<int> GetScansToDecode()
            {
                return new List<int>();
            }

            public int GoodEnoughScanNumber
            {
                get
                {
                    return 0;
                }
            }
        }
    }
}
