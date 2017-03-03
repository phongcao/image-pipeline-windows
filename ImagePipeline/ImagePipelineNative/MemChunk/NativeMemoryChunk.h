/**
 * Copyright (c) 2015-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. An additional grant
 * of patent rights can be found in the PATENTS file in the same directory.
 */

#include "common.h"

EXTERN_C_BEGIN

WIN_EXPORT int64_t nativeAllocate(int size);

WIN_EXPORT void nativeFree(int64_t lpointer);

WIN_EXPORT void nativeCopyToByteArray(int64_t lpointer, uint8_t* byteArray, int offset, int count);

WIN_EXPORT void nativeCopyFromByteArray(int64_t lpointer, uint8_t* byteArray, int offset, int count);

WIN_EXPORT void nativeMemcpy(int64_t dst, int64_t src, int count);

WIN_EXPORT uint8_t nativeReadByte(int64_t lpointer);

EXTERN_C_END
