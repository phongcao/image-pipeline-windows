/*
 * Copyright (c) 2015-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. An additional grant
 * of patent rights can be found in the PATENTS file in the same directory.
 */

#include <stdio.h>

#include <jpeglib.h>
#include <jerror.h>

#include "jpeg_error_handler.h"
#include "jpeg_stream_wrappers.h"

namespace facebook 
{
	namespace imagepipeline 
	{
		namespace jpeg 
		{
			/**
			 * Initialize input stream
			 */
			static void isInitSource(j_decompress_ptr dinfo)
			{
				try
				{
					JpegInputStreamWrapper* src = (JpegInputStreamWrapper*)dinfo->src;
					src->start = true;
					src->buffer = (JOCTET*)(*dinfo->mem->alloc_small)(
						(j_common_ptr)dinfo,
						JPOOL_PERMANENT,
						STREAM_BUFFER_SIZE * sizeof(JOCTET));

					if (src->buffer == nullptr)
					{
						jpegSafeThrow(
							(j_common_ptr)dinfo,
							"Failed to allocate memory for read buffer");
					}
				}
				catch (int)
				{
					jpegCleanup((JpegErrorHandler*)dinfo->err);
				}
			}

			/*
			 * Fill the input buffer --- called whenever buffer is emptied.
			 */
			static boolean isFillInputBuffer(j_decompress_ptr dinfo)
			{
				try
				{
					JpegInputStreamWrapper* src = (JpegInputStreamWrapper*)dinfo->src;
					ULONG nbytes = 0;
					src->inputStream->Read(src->readBuffer, STREAM_BUFFER_SIZE, &nbytes);

					if (nbytes <= 0)
					{
						if (src->start)
						{
							ERREXIT(dinfo, JERR_INPUT_EMPTY);
						}

						src->buffer[0] = (JOCTET)0xFF;
						src->buffer[1] = (JOCTET)JPEG_EOI;
						nbytes = 2;
					}
					else
					{
						memcpy(src->buffer, src->readBuffer, STREAM_BUFFER_SIZE);
					}

					src->public_fields.next_input_byte = src->buffer;
					src->public_fields.bytes_in_buffer = nbytes;
					src->start = false;
					return true;
				}
				catch (int)
				{
					jpegCleanup((JpegErrorHandler*)dinfo->err);
				}
			}

			/*
			 * Skip data --- used to skip over a potentially large amount of
			 * uninteresting data (such as an APPn marker).
			 */
			static void isSkipInputData(j_decompress_ptr dinfo, long num_bytes)
			{
				try
				{
					JpegInputStreamWrapper* src = (JpegInputStreamWrapper*)dinfo->src;
					if (num_bytes > 0)
					{
						if (src->public_fields.bytes_in_buffer > (unsigned long)num_bytes)
						{
							src->public_fields.next_input_byte += (size_t)num_bytes;
							src->public_fields.bytes_in_buffer -= (size_t)num_bytes;
						}
						else
						{
							long to_skip = num_bytes - (long)src->public_fields.bytes_in_buffer;
							LARGE_INTEGER li_to_skip;
							li_to_skip.QuadPart = to_skip;
							ULARGE_INTEGER new_postion;

							// We could at least try to skip appropriate amout of bytes...
							// TODO: 3752653
							src->inputStream->Seek(li_to_skip, STREAM_SEEK_CUR, &new_postion);
							src->public_fields.next_input_byte = nullptr;
							src->public_fields.bytes_in_buffer = 0;
						}
					}
				}
				catch (int)
				{
					jpegCleanup((JpegErrorHandler*)dinfo->err);
				}
			}

			/*
			 * Terminate source --- called by jpeg_finish_decompress
			 * after all data has been read.  Often a no-op.
			 */
			static void isTermSource(j_decompress_ptr dinfo) 
			{
				/* no work necessary here */
			}

			JpegInputStreamWrapper::JpegInputStreamWrapper(LPSTREAM inputStream) : 
				inputStream(inputStream)
			{
				public_fields.init_source = isInitSource;
				public_fields.fill_input_buffer = isFillInputBuffer;
				public_fields.skip_input_data = isSkipInputData;
				public_fields.resync_to_restart = jpeg_resync_to_restart; /* use default method */
				public_fields.term_source = isTermSource;
				public_fields.bytes_in_buffer = 0; // forces fill_input_buffer on first read
				public_fields.next_input_byte = NULL; // until buffer loaded
			}

			/**
			 * Initialize output stream.
			 */
			static void osInitDestination(j_compress_ptr cinfo) 
			{
				try
				{
					JpegOutputStreamWrapper* dest = (JpegOutputStreamWrapper*)cinfo->dest;

					// Allocate the output buffer --- it will be released when done with image
					dest->buffer = (JOCTET *)(*cinfo->mem->alloc_small)(
						(j_common_ptr)cinfo,
						JPOOL_IMAGE,
						STREAM_BUFFER_SIZE * sizeof(JOCTET));

					if (dest->buffer == NULL)
					{
						jpegSafeThrow(
							(j_common_ptr)cinfo,
							"Failed to allcoate memory for byte buffer.");
					}

					dest->public_fields.next_output_byte = dest->buffer;
					dest->public_fields.free_in_buffer = STREAM_BUFFER_SIZE;
				}
				catch (int)
				{
					jpegCleanup((JpegErrorHandler*)cinfo->err);
				}
			}

			/**
			 * Empty the output buffer --- called whenever buffer fills up.
			 */
			static boolean osEmptyOutputBuffer(j_compress_ptr cinfo) 
			{
				try
				{
					JpegOutputStreamWrapper* dest = (JpegOutputStreamWrapper*)cinfo->dest;
					memcpy(dest->writeBuffer, dest->buffer, STREAM_BUFFER_SIZE);
					ULONG nbytes = 0;
					dest->outputStream->Write(dest->writeBuffer, STREAM_BUFFER_SIZE, &nbytes);
					dest->public_fields.next_output_byte = dest->buffer;
					dest->public_fields.free_in_buffer = STREAM_BUFFER_SIZE;
					return true;
				}
				catch (int)
				{
					jpegCleanup((JpegErrorHandler*)cinfo->err);
				}
			}

			/**
			 * Terminate destination --- called by jpeg_finish_compress
			 * after all data has been written.
			 */
			static void osTermDestination(j_compress_ptr cinfo) 
			{
				try
				{
					JpegOutputStreamWrapper* dest = (JpegOutputStreamWrapper*)cinfo->dest;
					size_t datacount = STREAM_BUFFER_SIZE - dest->public_fields.free_in_buffer;

					if (datacount > 0)
					{
						memcpy(dest->writeBuffer, dest->buffer, datacount);
						ULONG nbytes = 0;
						dest->outputStream->Write(dest->writeBuffer, (ULONG)datacount, &nbytes);
					}
				}
				catch (int)
				{
					jpegCleanup((JpegErrorHandler*)cinfo->err);
				}
			}

			JpegOutputStreamWrapper::JpegOutputStreamWrapper(LPSTREAM output_stream) : 
				outputStream(output_stream) 
			{
				public_fields.init_destination = osInitDestination;
				public_fields.empty_output_buffer = osEmptyOutputBuffer;
				public_fields.term_destination = osTermDestination;
			}
		} 
	} 
}
