/**
 * Copyright (c) 2015-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. An additional grant
 * of patent rights can be found in the PATENTS file in the same directory.
 */

#ifndef ARM

#include "JpegTranscoder.h"
#include "transformations.h"
#include "exceptions.h"
#include "jpeg/jpeg_codec.h"

using facebook::imagepipeline::getRotationTypeFromDegrees;
using facebook::imagepipeline::RotationType;
using facebook::imagepipeline::ScaleFactor;
using facebook::imagepipeline::jpeg::transformJpeg;

void nativeTranscodeJpeg(
	LPSTREAM is,
	LPSTREAM os,
	int rotationAngle,
	int scaleNominator,
	int quality)
{
	ScaleFactor scale_factor
	{ 
		(uint8_t)scaleNominator, 8
	};

	RotationType rotation_type = getRotationTypeFromDegrees(rotationAngle);
	transformJpeg(
		is,
		os,
		rotation_type,
		scale_factor,
		quality);
}

#endif // ARM
