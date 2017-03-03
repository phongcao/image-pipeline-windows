/*
 * Copyright (c) 2015-present, Facebook, Inc.
 * All rights reserved.
 *
 * This source code is licensed under the BSD-style license found in the
 * LICENSE file in the root directory of this source tree. An additional grant
 * of patent rights can be found in the PATENTS file in the same directory.
 */

#include <stdexcept>
#include "exceptions.h"

using namespace std;

namespace facebook 
{
	namespace imagepipeline 
	{
		void safeThrowException(const char* msg)
		{
			throw exception(msg);
		}
	} 
}
