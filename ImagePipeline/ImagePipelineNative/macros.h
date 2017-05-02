/**
 * Copyright (c) 2015-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. An additional grant
 * of patent rights can be found in the PATENTS file in the same directory.
 */

#pragma once

#ifdef __cplusplus
#define EXTERN_C_BEGIN extern "C" {
#define EXTERN_C_END }
#else
#define EXTERN_C_BEGIN
#define EXTERN_C_END
#endif

#ifdef _WINDLL
#define WIN_EXPORT __declspec(dllexport)
#else
#define WIN_EXPORT
#endif

#define ARRAY_SIZE(a) (sizeof(a) / sizeof((a)[0]))
#define LONG_TO_PTR(j) ((void*) (intptr_t) (j))
#define PTR_TO_LONG(p) ((int64_t) (intptr_t) (p))

#define STREAM_BUFFER_SIZE 4 * 1024
