using System;
using BinaryResource;
using Cache.Disk;

namespace ImagePipelineBase.Tests.Cache.Disk
{
    /// <summary>
    /// Mock implementation of <see cref="IEntry"/> 
    /// </summary>
    class MockEntry : IEntry
    {
        private DateTime _timestamp;
        private long _size;

        /// <summary>
        /// The id representing the resource
        /// </summary>
        public string Id
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Gets the resoure
        /// </summary>
        public IBinaryResource Resource
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Calculated on first time and never changes so it can be used as immutable
        /// </summary>
        public DateTime Timestamp
        {
            get
            {
                return _timestamp;
            }
        }

        /// <summary>
        /// Mocks the timestamp
        /// </summary>
        /// <param name="timestamp"></param>
        public void SetTimeStamp(DateTime timestamp)
        {
            _timestamp = timestamp;
        }

        /// <summary>
        /// Calculated on first time and never changes so it can be used as immutable
        /// </summary>
        public long GetSize()
        {
            return _size;
        }

        /// <summary>
        /// Mocks the size
        /// </summary>
        /// <param name="size"></param>
        public void SetSize(long size)
        {
            _size = size;
        }
    }
}
