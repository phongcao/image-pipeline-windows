/*
 * Copyright (c) 2015-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. An additional grant
 * of patent rights can be found in the PATENTS file in the same directory.
 */

#ifdef HAS_LIBJPEGTURBO

#include <stdio.h>
#include <setjmp.h>
#include <stdexcept>

#include <jpeglib.h>
#include "jpeg_error_handler.h"

using namespace std;

namespace facebook 
{
	namespace imagepipeline 
	{
		namespace jpeg 
		{
			JpegErrorHandler::JpegErrorHandler() : dinfoPtr(nullptr), cinfoPtr(nullptr) 
			{
				jpeg_std_error(&pub);
				pub.error_exit = jpegThrow;
			}

			void JpegErrorHandler::setDecompressStruct(jpeg_decompress_struct& dinfo) 
			{
				dinfo.err = &pub;
				dinfoPtr = &dinfo;
			}

			void JpegErrorHandler::setCompressStruct(jpeg_compress_struct& cinfo) 
			{
				cinfo.err = &pub;
				cinfoPtr = &cinfo;
			}

			/**
			 * private method doing the actual cleanup:
			 *  - free jpeg-turbo data structures
			 *  - jump back to place determined by setjmpBuffer
			 */
			void jpegCleanup(JpegErrorHandler* error_handler) 
			{
				if (error_handler->dinfoPtr) 
				{
					jpeg_destroy_decompress(error_handler->dinfoPtr);
					error_handler->dinfoPtr = nullptr;
				}

				if (error_handler->cinfoPtr) 
				{
					jpeg_destroy_compress(error_handler->cinfoPtr);
					error_handler->cinfoPtr = nullptr;
				}

				longjmp(error_handler->setjmpBuffer, 1);
			}

			void jpegThrow(j_common_ptr cinfo) 
			{
				// Create and throw the jpeg-turbo error message
				char buffer[JMSG_LENGTH_MAX];
				(*cinfo->err->format_message) (cinfo, buffer);
				jpegSafeThrow(cinfo, buffer);
			}

			void jpegSafeThrow(
				j_common_ptr cinfo,
				const char* msg) 
			{
				JpegErrorHandler* error_handler = (JpegErrorHandler*)cinfo->err;
				throw exception(msg);
				jpegCleanup(error_handler);
			}
		} 
	} 
}

#endif // HAS_LIBJPEGTURBO
