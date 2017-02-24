using FBCore.Common.Internal;
using FBCore.Common.Util;
using ImagePipeline.Image;
using ImagePipeline.Memory;
using ImageUtils;
using System.Collections.Concurrent;
using System.IO;

namespace ImagePipeline.Decoder
{
    /// <summary>
    /// Progressively scans jpeg data and instructs caller when enough data is
    /// available to decode a partial image.
    ///
    /// <para />This class treats any sequence of bytes starting with 0xFFD8
    /// as a valid jpeg image.
    ///
    /// <para />Users should call ParseMoreData method each time new chunk of
    /// data is received. The buffer passed as a parameter should include entire
    /// image data received so far.
    /// </summary>
    public class ProgressiveJpegParser
    {
        /// <summary>
        /// Initial state of the parser. Next byte read by the parser
        /// should be 0xFF.
        /// </summary>
        private const int READ_FIRST_JPEG_BYTE = 0;

        /// <summary>
        /// Parser saw only one byte so far (0xFF). Next byte should
        /// be second byte of SOI marker.
        /// </summary>
        private const int READ_SECOND_JPEG_BYTE = 1;

        /// <summary>
        /// Next byte is either entropy coded data or first byte of
        /// a marker. First byte of marker cannot appear in entropy
        /// coded data, unless it is followed by 0x00 escape byte.
        /// </summary>
        private const int READ_MARKER_FIRST_BYTE_OR_ENTROPY_DATA = 2;

        /// <summary>
        /// Last read byte is 0xFF, possible start of marker (possible,
        /// because next byte might be "escape byte" or 0xFF again).
        /// </summary>
        private const int READ_MARKER_SECOND_BYTE = 3;

        /// <summary>
        /// Last two bytes constitute a marker that indicates start
        /// of a segment, the following two bytes denote 16-bit size
        /// of the segment.
        /// </summary>
        private const int READ_SIZE_FIRST_BYTE = 4;

        /// <summary>
        /// Last three bytes are marker and first byte of segment size,
        /// after reading next byte, bytes constituting remaining part
        /// of segment will be skipped.
        /// </summary>
        private const int READ_SIZE_SECOND_BYTE = 5;

        /// <summary>
        /// Parsed data is not a JPEG file.
        /// </summary>
        private const int NOT_A_JPEG = 6;

        /// <summary>
        /// The buffer size in bytes to use.
        /// </summary> 
        private const int BUFFER_SIZE = 16 * ByteConstants.KB;

        private int _parserState;
        private int _lastByteRead;

        /// <summary>
        /// Number of bytes consumed so far.
        /// </summary>
        private int _bytesParsed;

        /// <summary>
        /// Number of next fully parsed scan after reaching next SOS
        /// or EOI markers.
        /// </summary>
        private int _nextFullScanNumber;

        private int _bestScanNumber;

        private readonly IByteArrayPool _byteArrayPool;
        private readonly ConcurrentDictionary<int, int> _bestScanEndOffsetList;

        /// <summary>
        /// Instantiates the <see cref="ProgressiveJpegParser"/>.
        /// </summary>
        /// <param name="byteArrayPool"></param>
        public ProgressiveJpegParser(IByteArrayPool byteArrayPool)
        {
            _byteArrayPool = Preconditions.CheckNotNull(byteArrayPool);
            _bytesParsed = 0;
            _lastByteRead = 0;
            _nextFullScanNumber = 0;
            _bestScanNumber = 0;
            _parserState = READ_FIRST_JPEG_BYTE;
            _bestScanEndOffsetList = new ConcurrentDictionary<int, int>();
        }

        /// <summary>
        /// If this is the first time calling this method, the buffer will
        /// be checked to make sure it starts with SOI marker (0xffd8).
        /// If the image has been identified as a non-JPEG, data will be
        /// ignored and false will be returned immediately on all
        /// subsequent calls.
        ///
        /// This object maintains state of the position of the last read
        /// byte. On repeated calls to this method, it will continue from
        /// where it left off.
        /// </summary>
        /// <param name="encodedImage">
        /// Next set of bytes received by the caller.
        /// </param>
        /// <returns>true if a new full scan has been found.</returns>
        public bool ParseMoreData(EncodedImage encodedImage)
        {
            if (_parserState == NOT_A_JPEG)
            {
                return false;
            }

            int dataBufferSize = encodedImage.Size;

            // Is there any new data to parse?
            // _bytesParsed might be greater than size of dataBuffer - that
            // happens when we skip more data than is available to read
            // inside DoParseMoreData method.
            if (dataBufferSize <= _bytesParsed)
            {
                return false;
            }

            Stream bufferedDataStream = new PooledByteArrayBufferedInputStream(
                encodedImage.GetInputStream(),
                _byteArrayPool.Get(BUFFER_SIZE),
                _byteArrayPool);

            try
            {
                StreamUtil.Skip(bufferedDataStream, _bytesParsed);
                return DoParseMoreData(encodedImage, bufferedDataStream);
            }
            catch (IOException)
            {
                // Does not happen - streams returned by IPooledByteBuffers
                // do not throw IOExceptions.
                throw;
            }
            finally
            {
                Closeables.CloseQuietly(bufferedDataStream);
            }
        }

        /// <summary>
        /// Parses more data from inputStream.
        /// </summary>
        /// <param name="encodedImage">The encoded image.</param>
        /// <param name="inputStream">
        /// Instance of buffered pooled byte buffer input stream.
        /// </param>
        private bool DoParseMoreData(EncodedImage encodedImage, Stream inputStream)
        {
            int oldBestScanNumber = _bestScanNumber;

            try
            {
                int nextByte;
                while (_parserState != NOT_A_JPEG && (nextByte = inputStream.ReadByte()) != -1)
                {
                    _bytesParsed++;

                    switch (_parserState)
                    {
                        case READ_FIRST_JPEG_BYTE:
                            if (nextByte == JfifUtil.MARKER_FIRST_BYTE)
                            {
                                _parserState = READ_SECOND_JPEG_BYTE;
                            }
                            else
                            {
                                _parserState = NOT_A_JPEG;
                            }

                            break;

                        case READ_SECOND_JPEG_BYTE:
                            if (nextByte == JfifUtil.MARKER_SOI)
                            {
                                _parserState = READ_MARKER_FIRST_BYTE_OR_ENTROPY_DATA;
                            }
                            else
                            {
                                _parserState = NOT_A_JPEG;
                            }

                            break;

                        case READ_MARKER_FIRST_BYTE_OR_ENTROPY_DATA:
                            if (nextByte == JfifUtil.MARKER_FIRST_BYTE)
                            {
                                _parserState = READ_MARKER_SECOND_BYTE;
                            }
                            break;

                        case READ_MARKER_SECOND_BYTE:
                            if (nextByte == JfifUtil.MARKER_FIRST_BYTE)
                            {
                                _parserState = READ_MARKER_SECOND_BYTE;
                            }
                            else if (nextByte == JfifUtil.MARKER_ESCAPE_BYTE)
                            {
                                _parserState = READ_MARKER_FIRST_BYTE_OR_ENTROPY_DATA;
                            }
                            else
                            {
                                if (nextByte == JfifUtil.MARKER_SOS || nextByte == JfifUtil.MARKER_EOI)
                                {
                                    NewScanOrImageEndFound(encodedImage, _bytesParsed - 2);
                                }

                                if (DoesMarkerStartSegment(nextByte))
                                {
                                    _parserState = READ_SIZE_FIRST_BYTE;
                                }
                                else
                                {
                                    _parserState = READ_MARKER_FIRST_BYTE_OR_ENTROPY_DATA;
                                }
                            }

                            break;

                        case READ_SIZE_FIRST_BYTE:
                            _parserState = READ_SIZE_SECOND_BYTE;
                            break;

                        case READ_SIZE_SECOND_BYTE:
                            int size = (_lastByteRead << 8) + nextByte;

                            // We need to jump after the end of the segment - skip size-2
                            // next bytes.
                            // We might want to skip more data than is available to read,
                            // in which case we will consume entire data in inputStream
                            // and exit this function before entering another iteration
                            // of the loop.
                            int bytesToSkip = size - 2;
                            StreamUtil.Skip(inputStream, bytesToSkip);
                            _bytesParsed += bytesToSkip;
                            _parserState = READ_MARKER_FIRST_BYTE_OR_ENTROPY_DATA;
                            break;

                        case NOT_A_JPEG:
                        default:
                            Preconditions.CheckState(false);
                            break;
                    }

                    _lastByteRead = nextByte;
                }
            }
            catch (IOException)
            {
                // does not happen, input stream returned by pooled byte buffer does not 
                // throw IOExceptions.
                throw;
            }

            return _parserState != NOT_A_JPEG && _bestScanNumber != oldBestScanNumber;
        }

        /// <summary>
        /// Not every marker is followed by associated segment.
        /// </summary>
        private static bool DoesMarkerStartSegment(int markerSecondByte)
        {
            if (markerSecondByte == JfifUtil.MARKER_TEM)
            {
                return false;
            }

            if (markerSecondByte >= JfifUtil.MARKER_RST0 && markerSecondByte <= JfifUtil.MARKER_RST7)
            {
                return false;
            }

            return markerSecondByte != JfifUtil.MARKER_EOI && markerSecondByte != JfifUtil.MARKER_SOI;
        }

        private void NewScanOrImageEndFound(EncodedImage encodedImage, int offset)
        {
            if (_nextFullScanNumber > 0)
            {
                _bestScanEndOffsetList[encodedImage.Size] = offset;
            }

            _bestScanNumber = _nextFullScanNumber++;
        }

        /// <summary>
        /// Returns true if the input byte array is jpeg. Otherwise false.
        /// </summary>
        public bool IsJpeg
        {
            get
            {
                return _bytesParsed > 1 && _parserState != NOT_A_JPEG;
            }
        }

        /// <summary>
        /// Gets the best scan end offset.
        /// </summary>
        /// <returns>
        /// Offset at which parsed data should be cut to decode best available
        /// partial result.
        /// </returns>
        public int GetBestScanEndOffset(EncodedImage encodedImage)
        {
            int bestScanEndOffset = 0;
            _bestScanEndOffsetList.TryGetValue(encodedImage.Size, out bestScanEndOffset);
            return bestScanEndOffset;
        }

        /// <summary>
        /// Gets the current best scan number.
        /// </summary>
        /// <returns>
        /// Number of the best scan found so far.
        /// </returns>
        public int BestScanNumber
        {
            get
            {
                return _bestScanNumber;
            }
        }
    }
}
