/**
 * Copyright (c) 2015-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. An additional grant
 * of patent rights can be found in the PATENTS file in the same directory.
 */

#include "Common.h"

YG_EXTERN_C_BEGIN

WIN_EXPORT int64_t NativeAllocate(int size);

WIN_EXPORT void NativeFree(int64_t lpointer);

WIN_EXPORT void NativeCopyToByteArray(int64_t lpointer, uint8_t* byteArray, int offset, int count);

WIN_EXPORT void NativeCopyFromByteArray(int64_t lpointer, uint8_t* byteArray, int offset, int count);

WIN_EXPORT void NativeMemcpy(int64_t dst, int64_t src, int count);

WIN_EXPORT uint8_t NativeReadByte(int64_t lpointer);

YG_EXTERN_C_END
