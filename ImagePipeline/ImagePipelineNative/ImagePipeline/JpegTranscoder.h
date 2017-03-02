/**
 * Copyright (c) 2015-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. An additional grant
 * of patent rights can be found in the PATENTS file in the same directory.
 */

#include <ocidl.h>
#include "Common.h"

EXTERN_C_BEGIN

WIN_EXPORT void NativeTranscodeJpeg(
	LPSTREAM inputStream,
	LPSTREAM outputStream,
	int rotationAngle,
	int scaleNominator,
	int quality);

EXTERN_C_END
