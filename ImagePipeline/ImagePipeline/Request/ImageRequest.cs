using FBCore.Common.Util;
using ImagePipeline.Common;
using ImagePipeline.Listener;
using ImageUtils;
using System;
using System.IO;

namespace ImagePipeline.Request
{
    /// <summary>
    /// Immutable object encapsulating everything pipeline has to know about requested image to proceed.
    /// </summary>
    public class ImageRequest
    {
        private readonly object _gate = new object();

        private FileSystemInfo _sourceFile;

        /// <summary>
        /// Cache choice
        /// </summary>
        public CacheChoice CacheChoice { get; }

        /// <summary>
        /// Source Uri
        /// </summary>
        public Uri SourceUri { get; }

        /// <summary>
        /// Source File - for local fetches only, lazily initialized
        /// </summary>
        public FileSystemInfo SourceFile
        {
            get
            {
                lock (_gate)
                {
                    if (_sourceFile == null)
                    {
                        _sourceFile = new DirectoryInfo(SourceUri.LocalPath);
                    }

                    return _sourceFile;
                }
            }
        }

        /// <summary>
        /// If set - the client will receive intermediate results
        /// </summary>
        public bool IsProgressiveRenderingEnabled { get; }

        /// <summary>
        /// If set the client will receive thumbnail previews for local images, before the whole image
        /// </summary>
        public bool IsLocalThumbnailPreviewsEnabled { get; }

        /// <summary>
        /// Image decode options
        /// </summary>
        public ImageDecodeOptions ImageDecodeOptions { get; }

        /// <summary>
        /// Resize options
        /// </summary>
        public ResizeOptions ResizeOptions { get; } = null;

        /// <summary>
        /// Is auto-rotate enabled?
        /// </summary>
        public bool IsAutoRotateEnabled { get; }

        /// <summary>
        /// Priority levels of this request.
        /// </summary>
        public int Priority { get; }

        /// <summary>
        /// Lowest level that is permitted to fetch an image from
        /// </summary>
        public RequestLevel LowestPermittedRequestLevel { get; }

        /// <summary>
        /// Whether the disk cache should be used for this request
        /// </summary>
        public bool IsDiskCacheEnabled { get; }

        /// <summary>
        /// Postprocessor to run on the output bitmap.
        /// </summary>
        public IPostprocessor Postprocessor { get; }

        /// <summary>
        /// Request listener to use for this image request
        /// </summary>
        public IRequestListener RequestListener { get; }

        /// <summary>
        /// Gets preferred width
        /// </summary>
        public int PreferredWidth
        {
            get
            {
                return (ResizeOptions != null) ? ResizeOptions.Width : (int)BitmapUtil.MAX_BITMAP_SIZE;
            }
        }

        /// <summary>
        /// Gets preferred height
        /// </summary>
        public int PreferredHeight
        {
            get
            {
                return (ResizeOptions != null) ? ResizeOptions.Height : (int)BitmapUtil.MAX_BITMAP_SIZE;
            }
        }

        /// <summary>
        /// Creates ImageRequest from uri
        /// </summary>
        /// <param name="uri">uri</param>
        /// <returns></returns>
        public static ImageRequest FromUri(Uri uri)
        {
            return (uri == null) ? null : ImageRequestBuilder.NewBuilderWithSource(uri).Build();
        }

        /// <summary>
        /// Creates ImageRequest from uri string
        /// </summary>
        /// <param name="uriString">uri string</param>
        /// <returns></returns>
        public static ImageRequest FromUri(string uriString)
        {
            if (uriString != null && uriString.Length != 0)
            {
                Uri uri = default(Uri);
                if (Uri.TryCreate(uriString, UriKind.RelativeOrAbsolute, out uri))
                {
                    return FromUri(uri);
                }
            }

            return null;
        }

        /// <summary>
        /// Instantiates the <see cref="ImageRequest"/>
        /// </summary>
        /// <param name="builder"></param>
        internal ImageRequest(ImageRequestBuilder builder)
        {
            CacheChoice = builder.CacheChoice;
            SourceUri = builder.SourceUri;

            IsProgressiveRenderingEnabled = builder.IsProgressiveRenderingEnabled;
            IsLocalThumbnailPreviewsEnabled = builder.IsLocalThumbnailPreviewsEnabled;

            ImageDecodeOptions = builder.ImageDecodeOptions;

            ResizeOptions = builder.ResizeOptions;
            IsAutoRotateEnabled = builder.IsAutoRotateEnabled;

            Priority = builder.Priority;
            LowestPermittedRequestLevel = builder.LowestPermittedRequestLevel;
            IsDiskCacheEnabled = builder.IsDiskCacheEnabled;

            Postprocessor = builder.Postprocessor;

            RequestListener = builder.RequestListener;
        }

        /// <summary>
        /// Custom Equals method
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        public override bool Equals(object o)
        {
            if (o.GetType() != typeof(ImageRequest)) 
            {
                return false;
            }

            ImageRequest request = (ImageRequest)o;
            return SourceUri.Equals(request.SourceUri) &&
                CacheChoice.Equals(request.CacheChoice) &&
                SourceFile.FullName.Equals(request.SourceFile.FullName);
        }

        /// <summary>
        /// Custom GetHashCode method
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return HashCodeUtil.HashCode(CacheChoice, SourceUri, SourceFile);
        }
    }

    /// <summary>
    /// An enum describing the cache choice.
    /// </summary>
    public enum CacheChoice
    {
        /// <summary>
        /// Indicates that this image should go in the small disk cache, if one is being used
        /// </summary>
        SMALL,

        /// <summary>
        /// Default
        /// </summary>
        DEFAULT,
    }

    /// <summary>
    /// Level down to we are willing to go in order to find an image. E.g., we might only want to go
    /// down to bitmap memory cache, and not check the disk cache or do a full fetch.
    /// </summary>
    public class RequestLevel
    {
        /// <summary>
        /// Fetch (from the network or local storage)
        /// </summary>
        public const int FULL_FETCH = 1;

        /// <summary>
        /// Disk caching
        /// </summary>
        public const int DISK_CACHE = 2;

        /// <summary>
        /// Encoded memory caching
        /// </summary>
        public const int ENCODED_MEMORY_CACHE = 3;

        /// <summary>
        /// Bitmap caching
        /// </summary>
        public const int BITMAP_MEMORY_CACHE = 4;

        private int _value;

        /// <summary>
        /// Instantiates the request level
        /// </summary>
        /// <param name="value"></param>
        public RequestLevel(int value)
        {
            _value = value;
        }

        /// <summary>
        /// Gets the request level value
        /// </summary>
        public int Value
        {
            get
            {
                return _value;
            }
        }

        /// <summary>
        /// Gets the max value
        /// </summary>
        /// <param name="requestLevel1"></param>
        /// <param name="requestLevel2"></param>
        /// <returns></returns>
        public static RequestLevel GetMax(RequestLevel requestLevel1, RequestLevel requestLevel2)
        {
            return requestLevel1.Value > requestLevel2.Value ? requestLevel1 : requestLevel2;
        }
    }
}
